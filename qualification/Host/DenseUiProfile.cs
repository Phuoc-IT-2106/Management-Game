using Godot;
using System.Diagnostics;
using System.Text.Json;

// Follow-up instrumentation only. No simulation, persistence or production UI.
public partial class DenseUi
{
    private bool _profileActive, _profileLight;
    private long _profilePrevious;
    private Rid _profileViewport;
    private readonly List<double> _profileDelta = new(8192), _profileWall = new(8192),
        _profileProcess = new(8192), _profileRenderCpu = new(8192), _profileRenderGpu = new(8192),
        _profileSetup = new(8192), _profileDrawCalls = new(8192), _profileBindMs = new(512),
        _profileBindBytes = new(512), _profileQueryMs = new(512);

    private void ProfileFrame(double delta)
    {
        if (!_profileActive) return;
        long now = Stopwatch.GetTimestamp();
        if (_profilePrevious != 0) {
            _profileDelta.Add(delta * 1000);
            _profileWall.Add(Stopwatch.GetElapsedTime(_profilePrevious, now).TotalMilliseconds);
            if (!_profileLight) {
                _profileProcess.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000);
                _profileRenderCpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeCpu(_profileViewport));
                _profileRenderGpu.Add(RenderingServer.ViewportGetMeasuredRenderTimeGpu(_profileViewport));
                _profileSetup.Add(RenderingServer.GetFrameSetupTimeCpu());
                _profileDrawCalls.Add(Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
            }
        }
        _profilePrevious = now;
    }

    private static object Distribution(List<double> values) => new {
        count=values.Count, p50=values.Count==0 ? (double?)null : values.Order().ElementAt(values.Count/2),
        p95=values.Count==0 ? (double?)null : P95(values),
        max=values.Count==0 ? (double?)null : values.Max(), samples=values.ToArray()
    };

    private async Task ProfileWorkflows()
    {
        var args=OS.GetCmdlineUserArgs();
        _profileLight=args.Contains("--profile-light");
        if(args.Contains("--profile-no-vsync")) DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        _profileViewport=GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(_profileViewport,!_profileLight);
        var results=new List<object>();
        foreach(int count in new[] {1000,10000,100000}) {
            _size.Select(count==1000?0:count==10000?1:2);
            _rows=SyntheticTable.Generate(count);
            foreach(string workflow in new[] {"idle","paging","scrolling","sorting","filtering","search-binding","row-rebuild-binding","resize-layout"}) {
                _search=""; _filter.SetPressedNoSignal(false); _column=0; _descending=false; _page=0;
                GetWindow().Size=new Vector2I(1280,720); GetWindow().ContentScaleFactor=1;
                await Refresh(); await Frames(60);
                foreach(var list in new[] {_profileDelta,_profileWall,_profileProcess,_profileRenderCpu,_profileRenderGpu,_profileSetup,_profileDrawCalls,_profileBindMs,_profileBindBytes,_profileQueryMs}) list.Clear();
                _profilePrevious=0;
                long allocated=GC.GetTotalAllocatedBytes(true);
                var gcBefore=new[] {GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
                var process=Process.GetCurrentProcess(); var cpu=process.TotalProcessorTime;
                var response=new List<double>();
                var duration=Stopwatch.StartNew(); int iteration=0;
                _profileActive=true;
                while(duration.Elapsed.TotalSeconds<4) {
                    var started=Stopwatch.GetTimestamp();
                    switch(workflow) {
                        case "paging": _page=iteration%Math.Max(1,count/PageSize); Bind(); break;
                        case "scrolling": _table.ScrollToItem(_table.GetRoot().GetChild(iteration%2==0?0:PageSize-1)); break;
                        case "sorting": _column=iteration%12; _descending=iteration%2==0; await Refresh(); break;
                        case "filtering": _filter.SetPressedNoSignal(iteration%2==0); await Refresh(); break;
                        case "search-binding": _search=iteration%2==0?"Fixture 00":"Fixture"; await Refresh(150); break;
                        case "row-rebuild-binding": Bind(); break;
                        case "resize-layout": GetWindow().Size=iteration%2==0?new Vector2I(1280,720):new Vector2I(1920,1080); break;
                    }
                    await Frames();
                    if(workflow!="idle") response.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                    iteration++;
                    await ToSignal(GetTree().CreateTimer(.08),SceneTreeTimer.SignalName.Timeout);
                }
                _profileActive=false;
                double seconds=duration.Elapsed.TotalSeconds;
                long allocationBytes=GC.GetTotalAllocatedBytes(true)-allocated;
                var gcDelta=Enumerable.Range(0,3).Select(i=>GC.CollectionCount(i)-gcBefore[i]).ToArray();
                double cpuMs=(process.TotalProcessorTime-cpu).TotalMilliseconds;
                results.Add(new {rows=count,workflow,seconds,iterations=iteration,allocationBytes,gcDelta,cpuMs,
                    deltaMs=Distribution(_profileDelta),wallMs=Distribution(_profileWall),processMs=Distribution(_profileProcess),
                    renderCpuMs=Distribution(_profileRenderCpu),renderGpuMs=Distribution(_profileRenderGpu),renderSetupMs=Distribution(_profileSetup),
                    drawCalls=Distribution(_profileDrawCalls),bindMs=Distribution(_profileBindMs),bindBytes=Distribution(_profileBindBytes),
                    workerQueryMs=Distribution(_profileQueryMs),responseProxyMs=Distribution(response)});
                process.Dispose();
                GD.Print($"PROFILE {count} {workflow} delta_p95={P95(_profileDelta):F3} wall_p95={P95(_profileWall):F3} bind_p95={(_profileBindMs.Count>0?P95(_profileBindMs):0):F3}");
            }
        }
        string output=args.FirstOrDefault(a=>a.StartsWith("--profile-output="))?.Split('=',2)[1] ?? "user://qg02-profile.json";
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(output),JsonSerializer.Serialize(new {
            schema="followup-profile-v1",debugBuild=OS.IsDebugBuild(),lightInstrumentation=_profileLight,reuseRows=_reuseRows,
            godot=Engine.GetVersionInfo()["string"].AsString(), runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            display=DisplayServer.GetName(),vsync=DisplayServer.WindowGetVsyncMode().ToString(),maxFps=Engine.MaxFps,
            screenCount=DisplayServer.GetScreenCount(),screenDpi=DisplayServer.ScreenGetDpi(),screenScale=DisplayServer.ScreenGetScale(),
            refreshHz=DisplayServer.ScreenGetRefreshRate(),results
        },new JsonSerializerOptions{WriteIndented=true}));
        GD.Print("QG02_PROFILE_COMPLETE"); GetTree().Quit();
    }
}
