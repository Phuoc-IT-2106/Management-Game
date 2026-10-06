using System.Collections.Immutable;
using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;
using ManagementGame.OperatingMap;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace ManagementGame.SponsorWorkspace;

// Internal deterministic native harness; production entry is Main's shared session.
public partial class SponsorVerification : Control
{
    private IGameSession session = null!;
    private SponsorCompanyHost host = null!;
    private SponsorWorkspaceView View => Descendants(this).OfType<SponsorWorkspaceView>().Single();
    private readonly List<string> checks = [];
    private readonly Dictionary<string, List<double>> samples = [];
    private int retainedBefore, retainedAfter;
    private string OfferId => ((ISponsorSession)session).ObserveSponsors().Offers[0].OfferId;
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
    private static IEnumerable<Node> Descendants(Node n) { foreach (var c in n.GetChildren()) { yield return c; foreach (var x in Descendants(c)) yield return x; } }
    private async Task Settle(int count = 5) { for(var i=0;i<count;i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Check(bool result, string name) { if(!result) throw new InvalidOperationException(name); checks.Add(name); GD.Print("PASS " + name); }
    private void Measure(string name, long start) { if(!samples.TryGetValue(name, out var values)) samples[name] = values = []; values.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    private async Task Setup()
    {
        foreach(var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        session = Composition.Create(Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../content/fixture.json")),
            Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../artifacts/sponsor-workspace/native-saves")), 20261004);
        host = new SponsorCompanyHost(); host.Configure(session, () => {}); AddChild(host); host.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        await Settle();
    }
    private EntityLabel Matter() => Descendants(host).OfType<EntityLabel>().First(x => x.IsVisibleInTree() && x.Binding.Current?.Id == "matter:" + OfferId);
    private Button Entry() => Descendants(host).OfType<Button>().First(x => x.IsVisibleInTree() && x.Text == "Open decision entry");
    private async Task Open()
    {
        var start = Stopwatch.GetTimestamp(); Matter().EmitSignal(BaseButton.SignalName.Pressed); Entry().EmitSignal(BaseButton.SignalName.Pressed);
        Measure("enter-work", start); await Settle();
    }
    private async Task Key(Key key, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode=key, ShiftPressed=shift, Pressed=true }); await Settle(1);
        Input.ParseInputEvent(new InputEventKey { Keycode=key, ShiftPressed=shift, Pressed=false }); await Settle(1);
    }
    private async Task Pointer(Button button)
    {
        var point = button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left, Position=point, GlobalPosition=point, Pressed=true }, true); await Settle(1);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex=MouseButton.Left, Position=point, GlobalPosition=point, Pressed=false }, true); await Settle(1);
    }
    private async Task Verify()
    {
        await Setup(); var hash = Composition.Hash(session);
        Check(Matter().GetParent() is not null, "live list-first sponsor matter exists");
        await Pointer(Matter()); await Settle(); Entry().GrabFocus(); await Key(Godot.Key.Enter); await Settle();
        Check(View.Presenter.Origin.ListMode, "pointer + Enter enters from live hierarchy");
        View.ReviewButton.GrabFocus(); await Key(Godot.Key.Tab, true); await Key(Godot.Key.Tab);
        Check(View.ReviewButton.HasFocus(), "native Tab and Shift+Tab traverse actions");
        Check(View.ReviewButton.GetThemeStylebox("focus") is StyleBoxFlat focus && focus.BorderWidthLeft >= UiTokens.FocusWidth, "visible canonical focus");
        await Key(Godot.Key.Space); await Settle(); Check(View.Presenter.Phase == SponsorPhase.Confirm, "Space opens material confirmation");
        await Key(Godot.Key.Escape); await Settle(); Check(View.Presenter.Phase == SponsorPhase.Review, "Escape confirmation restores review");
        View.EvidenceScroll.GrabFocus(); await Key(Godot.Key.End);
        Check(View.EvidenceScroll.ScrollVertical > 0 || View.EvidenceScroll.GetChild<Control>(0).Size.Y <= View.EvidenceScroll.Size.Y, "keyboard scroll reaches supporting evidence");
        await Key(Godot.Key.Escape); await Settle();
        Check(!Descendants(host).OfType<SponsorWorkspaceView>().Any() && Composition.Hash(session) == hash, "cancel returns without gameplay mutation");
        Check(Matter().HasFocus(), "return restores matter focus");
        var toggle = Descendants(host).OfType<Button>().First(x => x.IsVisibleInTree() && x.Text.Contains("map", StringComparison.OrdinalIgnoreCase) && x.Text.StartsWith("Use"));
        toggle.EmitSignal(BaseButton.SignalName.Pressed); await Settle(); await Open();
        Check(!View.Presenter.Origin.ListMode, "optional map opens same live presenter");
        View.Act("back"); await Settle();
        Check(Matter().HasFocus(), "map return restores matter focus");
        await Open(); View.Act("review");
        session.Submit(new("native:external", ((ISponsorSession)session).ObserveSponsors().Revision, new CoachDecision(Delegation.Manual, Risk.Balanced)));
        var staleHash=Composition.Hash(session); View.Commitment!.ActionButton.GrabFocus(); await Key(Godot.Key.Enter); await Settle();
        Check(View.Presenter.Phase == SponsorPhase.Rejected && View.Presenter.Feedback.Contains("Stale view") && Composition.Hash(session)==staleHash, "native stale rejection and refresh");
        Check(View.Commitment is null && View.ReviewButton.Text == "Review acceptance", "rejection requires fresh review");
        View.Act("review"); var old = View.Commitment!.ActionButton; old.GrabFocus(); await Key(Godot.Key.Space); await Settle();
        Check(View.Presenter.Phase == SponsorPhase.Accepted && View.ReviewButton.Disabled, "Space commits once and success disables action");
        var acceptedHash=Composition.Hash(session); for(var i=0;i<8;i++) View.ReviewButton.EmitSignal(BaseButton.SignalName.Pressed);
        Check(Composition.Hash(session)==acceptedHash, "native button spam cannot duplicate commitment");
        Check(session.Save("native-sponsor").Accepted && session.Load("native-sponsor").Accepted && Composition.Hash(session)==acceptedHash, "live schema-1 save/load after native commitment");
        View.Act("back"); await Settle(); Check(Matter().Text.Contains("Signed"), "return displays updated company matter");
        await Setup(); await Open(); for(var i=0;i<5;i++) { View.Render(); await Settle(1); }
        retainedBefore=Descendants(View).OfType<Control>().Count();
        for(var i=0;i<25;i++)
        {
            var start=Stopwatch.GetTimestamp(); View.Render(); Measure("evidence-rebind-work",start);
            start=Stopwatch.GetTimestamp(); View.Act("review"); Measure("confirmation-work",start);
            View.Act("back"); await Settle(1);
        }
        await Settle(); retainedAfter=Descendants(View).OfType<Control>().Count();
        Check(retainedBefore==retainedAfter, "25 rebind/confirmation cycles retain Controls");
        for(var i=0;i<25;i++) { var start=Stopwatch.GetTimestamp(); View.Act("back"); await Settle(); await Open(); Measure("enter-back-plus-layout",start); }
        Check(Descendants(View).OfType<Control>().Count()==retainedBefore, "25 enter/back cycles retain Controls and one workspace");
        foreach(var kv in View.Timings) samples["view-"+kv.Key]=kv.Value;
    }
    private void LayoutCheck()
    {
        var bounds=GetViewportRect();
        Check(Descendants(View).OfType<Control>().Where(x=>x.IsVisibleInTree()).All(x=>x.GetGlobalRect().Position.X >= -2 && x.GetGlobalRect().End.X <= bounds.End.X+2), "no horizontal workspace overflow");
        Check(bounds.Encloses(View.BackButton.GetGlobalRect()), "back remains visible");
        var action=View.Commitment?.ActionButton ?? View.ReviewButton;
        Check(bounds.Encloses(action.GetGlobalRect()), "commit/review action remains visible");
        var doc=Descendants(View).OfType<PanelContainer>().FirstOrDefault(x=>x.HasMeta("document_id"));
        Check(doc is not null && View.TermsScroll.GetGlobalRect().Encloses(doc.GetGlobalRect()), "all core contract terms visible above fold");
    }
    public override async void _Ready()
    {
        try
        {
            var size=(Arg("--sponsor-viewport")??"1280x720").Split('x');
            GetWindow().ContentScaleSize=new(int.Parse(size[0]),int.Parse(size[1])); GetWindow().ContentScaleMode=Window.ContentScaleModeEnum.Viewport;
            GetWindow().ContentScaleAspect=Window.ContentScaleAspectEnum.Ignore;
            RenderingServer.SetDefaultClearColor(UiTheme.ToGodot(new UiTokens().Color(ColorRole.SurfaceBase)));
            if(OS.GetCmdlineUserArgs().Contains("--sponsor-verify")) await Verify();
            await Setup(); var initial=Stopwatch.GetTimestamp(); await Open(); Measure("initial-bind-plus-layout",initial);
            var scenario=Arg("--sponsor-case")??"review";
            if(scenario=="long")
            {
                var s=View.Presenter.Snapshot; s=s with { CompanyName="DEVELOPMENT COMPANY · Tổ chức phát triển thương mại và thể thao quốc tế · Long identity review",
                    Offers=s.Offers.Select(o=>o with { Name="DEVELOPMENT SPONSOR · International Commercial and Competitive Technology Partnership" }).ToImmutableArray() };
                View.Configure(new((ISponsorSession)session,s,OfferId,View.Presenter.Origin),()=>{}); View.Render();
            }
            if(scenario is "confirmation" or "success" or "stale") View.Act("review");
            if(scenario=="stale") session.Submit(new("capture:external",0,new CoachDecision(Delegation.Manual,Risk.Balanced)));
            if(scenario is "success" or "stale") { var start=Stopwatch.GetTimestamp(); View.Act("commit"); Measure("command-and-refresh-work",start); }
            await Settle(12); View.BackButton.GrabFocus(); LayoutCheck();
            if(Arg("--sponsor-output") is { } output)
            {
                Directory.CreateDirectory(output); Input.WarpMouse(Vector2.Zero); await Settle(3);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=GetViewport().GetTexture().GetImage(); var key=scenario+"-"+size[0]+"x"+size[1];
                var file=Path.Combine(output,key+".png"); if(image.SavePng(file)!=Error.Ok) throw new IOException("PNG capture failed");
                var manifest=new { Case=scenario, Kind="GOLDEN CANDIDATE", Commit=Arg("--sponsor-commit"), SourceDigest=Arg("--sponsor-source-digest"), Dirty=true,
                    Viewport=new { Width=image.GetWidth(),Height=image.GetHeight() }, Window=new { GetWindow().Size }, Engine=Engine.GetVersionInfo()["string"].AsString(),
                    Theme=UiTokens.Notice, Density="Compact", TextScale=1, Locale="en", Fixture=PresentationText.FixtureNotice, Seed=20261004,
                    Snapshot=View.Presenter.Snapshot, State=View.Presenter.Phase.ToString(), View.Presenter.Origin, Checks=checks,
                    RetainedControls=new { Before=retainedBefore,After=retainedAfter }, Timings=samples,
                    WorkspaceWork=View.Timings, ApplicationWork=View.Presenter.Timings,
                    ImageHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))), GameplayHash=Composition.Hash(session),
                    Limits="Synthetic native input; no physical input/DPI or human usability acceptance. Debug local timings, not QG-02 pass." };
                File.WriteAllText(Path.Combine(output,key+".json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions { WriteIndented=true }));
            }
            GD.Print("SPONSOR_NATIVE_PASS "+checks.Count); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GetTree().Quit();
        }
        catch(Exception e) { GD.PrintErr("SPONSOR_NATIVE_FAIL "+e); GetTree().Quit(1); }
    }
}

