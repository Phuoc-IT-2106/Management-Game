using Godot;
using System.Diagnostics;
using System.Text.Json;

public partial class DenseUi : VBoxContainer
{
    private FakeRow[] _rows = [], _view = [];
    private Tree _table = null!;
    private Label _status = null!;
    private Button _action = null!;
    private LineEdit _searchBox = null!;
    private CheckBox _filter = null!;
    private OptionButton _size = null!;
    private HFlowContainer _toolbar = null!;
    private int _page, _column, _queryVersion, _boundVersion, _selected;
    private bool _descending, _binding;
    private string _search = "";
    private readonly List<double> _frames = [];
    private readonly List<object> _results = [];
    private readonly List<string> _checks = [];
    private bool _measuring;
    private int _actionCount, _lastActionId;
    private const int PageSize = 100;
    private static readonly string[] Headers = ["ID", "Name", "Group", "Score", "Amount", "Active", "Date", "Optional", "Region", "Rank", "Load", "Note"];

    public override async void _Ready()
    {
        try
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            AddChild(new Label { Text = "QG-02 | Synthetic data only | 100 rows per page | click headings to sort", AutowrapMode=TextServer.AutowrapMode.WordSmart });
            var toolbar = _toolbar = new HFlowContainer();
            AddChild(toolbar);
            var size = _size = new OptionButton();
            foreach (var n in new[] {1000,10000,100000}) size.AddItem(n.ToString());
            size.ItemSelected += async i => { _rows = SyntheticTable.Generate(new[] {1000,10000,100000}[(int)i]); _page = 0; await Refresh(); };
            toolbar.AddChild(size);
            _searchBox = new LineEdit { PlaceholderText = "Search synthetic name", CustomMinimumSize=new Vector2(220,0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _searchBox.TextChanged += SearchChanged;
            toolbar.AddChild(_searchBox);
            _filter = new CheckBox { Text = "Active only" };
            _filter.Toggled += FilterChanged;
            toolbar.AddChild(_filter);
            var previous = new Button { Text = "Previous" };
            previous.Pressed += () => { _page = Math.Max(0,_page-1); Bind(); };
            toolbar.AddChild(previous);
            var next = new Button { Text = "Next" };
            next.Pressed += () => { _page = Math.Min(Math.Max(0,(_view.Length-1)/PageSize),_page+1); Bind(); };
            toolbar.AddChild(next);
            _action = new Button { Text = "Act on selected ID", Disabled = true };
            _action.Pressed += Act;
            toolbar.AddChild(_action);
            _status = new Label { Text = "Loading", AutowrapMode=TextServer.AutowrapMode.WordSmart };
            AddChild(_status);
            MakeTable();
            _rows = SyntheticTable.Generate(1000);
            await Refresh();
            if (OS.GetCmdlineUserArgs().Contains("--benchmark")) await Benchmark();
            if (OS.GetCmdlineUserArgs().Contains("--layout-check")) await LayoutCheck();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private void MakeTable()
    {
        _table = new Tree { Columns = 12, HideRoot = true, ColumnTitlesVisible = true,
            SelectMode = Tree.SelectModeEnum.Row, SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill };
        for (int i = 0; i < 12; i++)
        {
            _table.SetColumnTitle(i, Headers[i]);
            _table.SetColumnCustomMinimumWidth(i, i == 1 ? 230 : i == 11 ? 210 : i == 6 ? 110 : 85);
            _table.SetColumnExpand(i, false);
        }
        _table.ColumnTitleClicked += Sort;
        _table.ItemSelected += Selected;
        _table.ItemActivated += Act;
        AddChild(_table);
    }

    private void DetachTable()
    {
        _table.ColumnTitleClicked -= Sort;
        _table.ItemSelected -= Selected;
        _table.ItemActivated -= Act;
        RemoveChild(_table);
        _table.QueueFree();
    }

    public override void _ExitTree()
    {
        _queryVersion++;
        _searchBox.TextChanged -= SearchChanged;
        _filter.Toggled -= FilterChanged;
        _action.Pressed -= Act;
        _table.ColumnTitleClicked -= Sort;
        _table.ItemSelected -= Selected;
        _table.ItemActivated -= Act;
    }

    public override void _Process(double delta) { if (_measuring) _frames.Add(delta * 1000); }
    private async void Sort(long column, long mouse) { _descending = _column == (int)column && !_descending; _column=(int)column; _page=0; await Refresh(); }
    private async void SearchChanged(string text) { _search=text; _page=0; await Refresh(150); }
    private async void FilterChanged(bool enabled) { _page=0; await Refresh(); }

    private async Task<double> Refresh(int delay = 0)
    {
        int version = ++_queryVersion;
        _action.Disabled = true;
        _status.Text = "Query pending";
        var rows = _rows; int column = _column; bool descending = _descending;
        string search = _search; bool active = _filter.ButtonPressed;
        var time = Stopwatch.StartNew();
        if (delay > 0) await Task.Delay(delay);
        if (!IsInsideTree() || version != _queryVersion) return time.Elapsed.TotalMilliseconds;
        var view = await Task.Run(() => SyntheticTable.Query(rows,column,descending,search,active));
        if (!IsInsideTree() || version != _queryVersion) return time.Elapsed.TotalMilliseconds;
        _view=view; _boundVersion=version;
        Bind();
        return time.Elapsed.TotalMilliseconds;
    }

    private void Bind()
    {
        _binding=true;
        _table.Clear();
        var root=_table.CreateItem();
        foreach (var row in _view.Skip(_page*PageSize).Take(PageSize))
        {
            var item=_table.CreateItem(root);
            string[] cells=row.Cells();
            for (int c=0;c<12;c++) { item.SetText(c,cells[c]); item.SetTooltipText(c,cells[c]); }
            item.SetMetadata(0,row.Id);
            if (row.Id==_selected) item.Select(0);
        }
        _binding=false;
        _action.Disabled = _boundVersion != _queryVersion || !_view.Any(r=>r.Id==_selected);
        _status.Text=$"{_view.Length:N0} results / page {_page+1} / selected ID {_selected} / revision {_boundVersion}";
    }

    private void Selected()
    {
        if (_binding) return;
        _selected=(int)(_table.GetSelected()?.GetMetadata(0) ?? 0);
        _action.Disabled=_boundVersion!=_queryVersion || !_view.Any(r=>r.Id==_selected);
        _status.Text=$"Selected ID {_selected}";
    }

    private void Act()
    {
        if (_boundVersion!=_queryVersion || !_view.Any(r=>r.Id==_selected)) return;
        _lastActionId=_selected; _actionCount++;
        _status.Text=$"Synthetic action on ID {_selected} (count {_actionCount})";
    }

    private void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); _checks.Add(name); }
    private static double P95(List<double> values) => values.Order().ElementAt(Math.Max(0,(int)Math.Ceiling(values.Count*.95)-1));
    private static int Controls(Node node) => (node is Control ? 1 : 0)+node.GetChildren().Sum(Controls);
    private async Task Frames(int count=2) { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }

    private async Task Benchmark()
    {
        foreach (int count in new[] {1000,10000,100000})
        {
            _size.Select(count==1000?0:count==10000?1:2);
            _rows=SyntheticTable.Generate(count); _search=""; _filter.ButtonPressed=false;
            _column=0; _descending=false; _page=0;
            double cold=await Refresh(); await Frames(5);
            _selected=17; Bind();
            _column=3; _descending=true; await Refresh(); Act();
            Check(_lastActionId==17,$"{count}: reorder action retains ID");
            var expected=_rows.OrderByDescending(r=>r.Score).ThenBy(r=>r.Id).Select(r=>r.Id);
            Check(_view.Select(r=>r.Id).SequenceEqual(expected),$"{count}: numeric sort/ties oracle");
            _column=7; await Refresh();
            Check(_view.TakeWhile(r=>r.Optional.HasValue).Count()==_rows.Count(r=>r.Optional.HasValue),$"{count}: unknowns last descending");
            _search="FIXTURE 0000"; _filter.SetPressedNoSignal(true); await Refresh();
            Check(_view.Select(r=>r.Id).Order().SequenceEqual(_rows.Where(r=>r.Id<100 && r.Active).Select(r=>r.Id)), $"{count}: search/filter composition");
            int before=_actionCount;
            _search="does-not-exist"; var pending=Refresh(30); Act();
            Check(_actionCount==before,$"{count}: stale action rejected");
            _search="Fixture 000017"; var latest=Refresh(); await Task.WhenAll(pending,latest);
            Check(_view.Length==1 && _view[0].Id==17,$"{count}: latest query wins");
            _search="none"; await Refresh(); Act();
            Check(_actionCount==before,$"{count}: filtered-out action rejected");
            _search=""; _filter.SetPressedNoSignal(false); _column=0; _descending=false; await Refresh();
            _table.GrabFocus(); _table.GetRoot().GetFirstChild().Select(0); await Frames();
            Input.ParseInputEvent(new InputEventKey { Keycode=Key.Down, Pressed=true });
            Input.ParseInputEvent(new InputEventKey { Keycode=Key.Down, Pressed=false });
            await Frames(); Check(_selected==2,$"{count}: injected keyboard down selects ID 2");
            var first=_table.GetRoot().GetFirstChild();
            Vector2 point=GetViewport().GetFinalTransform() * (_table.GlobalPosition+_table.GetItemAreaRect(first).Position+new Vector2(20,10));
            Input.ParseInputEvent(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=MouseButton.Left, Pressed=true });
            Input.ParseInputEvent(new InputEventMouseButton { Position=point, GlobalPosition=point, ButtonIndex=MouseButton.Left, Pressed=false });
            await Frames(); Check(_selected==1,$"{count}: injected mouse selects ID 1");
            before=_actionCount;
            Input.ParseInputEvent(new InputEventKey { Keycode=Key.Enter, Pressed=true });
            Input.ParseInputEvent(new InputEventKey { Keycode=Key.Enter, Pressed=false });
            await Frames(); Check(_lastActionId==1 && _actionCount==before+1,$"{count}: keyboard activation exactly once");
            var timings=new List<double>();
            _frames.Clear(); _measuring=true;
            for(int i=0;i<30;i++) { _column=i%12; _descending=i%2==0; _filter.SetPressedNoSignal(i%3==0); timings.Add(await Refresh()); await Frames(); }
            _filter.SetPressedNoSignal(false); _column=0; _descending=false; await Refresh(); await Frames(5);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long memoryBefore=GC.GetTotalMemory(true); int controlsBefore=Controls(this);
            double objectsBefore=Performance.GetMonitor(Performance.Monitor.ObjectCount);
            var navigation=new List<object>();
            for(int i=0;i<100;i++)
            {
                DetachTable(); MakeTable(); _page=i%Math.Max(1,count/PageSize); Bind(); await Frames();
                if(i%10==9) { GC.Collect(); navigation.Add(new { cycle=i+1, managedBytes=GC.GetTotalMemory(true), controls=Controls(this), objects=Performance.GetMonitor(Performance.Monitor.ObjectCount) }); }
            }
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); await Frames();
            long memoryAfter=GC.GetTotalMemory(true); int controlsAfter=Controls(this);
            Check(controlsAfter==controlsBefore,$"{count}: bounded controls after 100 table navigation cycles");
            _selected=_view[_page*PageSize].Id; Bind(); before=_actionCount;
            _table.EmitSignal(Tree.SignalName.ItemActivated);
            Check(_actionCount==before+1 && _lastActionId==_selected,$"{count}: no duplicate activation after navigation");
            // Warm queries/navigation are reported separately from steady frames.
            _frames.Clear();
            // 100 seconds per dataset = five minutes of rendered resize/scroll interaction.
            var duration=Stopwatch.StartNew(); int iteration=0;
            var inputTimes=new List<double>();
            while(duration.Elapsed.TotalSeconds<100)
            {
                var time=Stopwatch.StartNew();
                _page=(iteration++)%Math.Max(1,count/PageSize); Bind();
                await Frames(); inputTimes.Add(time.Elapsed.TotalMilliseconds);
                if(iteration%30==0)
                {
                    GetWindow().Size=new[] { new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(2560,1440) }[(iteration/30)%3];
                    GetWindow().ContentScaleFactor=new[] {1f,1.25f,1.5f,2f}[(iteration/30)%4];
                }
                _table.ScrollToItem(_table.GetRoot().GetFirstChild());
                await ToSignal(GetTree().CreateTimer(.08),SceneTreeTimer.SignalName.Timeout);
            }
            _measuring=false;
            var result=new { rows=count, coldQueryMs=cold, queryMs=timings, queryP95Ms=P95(timings),
                frameMs=_frames.ToArray(), frameP95Ms=P95(_frames), inputAckMs=inputTimes, inputP95Ms=P95(inputTimes),
                managedBefore=memoryBefore, managedAfter=memoryAfter, retainedRatio=(double)memoryAfter/memoryBefore,
                controlsBefore,controlsAfter, objectsBefore, objectsAfter=Performance.GetMonitor(Performance.Monitor.ObjectCount),
                liveTreeItems=101, navigation, interactionSeconds=duration.Elapsed.TotalSeconds,
                privateBytes=Process.GetCurrentProcess().PrivateMemorySize64 };
            _results.Add(result);
            GD.Print($"QG02_DATASET {count} query_p95_ms={P95(timings):F3} frame_p95_ms={P95(_frames):F3} controls={controlsAfter}");
        }
        GetWindow().Size=new Vector2I(1280,720); GetWindow().ContentScaleFactor=1; _page=0; Bind(); await Frames();
        if(DisplayServer.GetName()!="headless")
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("user://qg02-render.png");
        }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("user://qg02.json"),JsonSerializer.Serialize(new { fixture="synthetic-arithmetic-v1",checks=_checks,results=_results }, new JsonSerializerOptions { WriteIndented=true }));
        GD.Print($"QG02_CHECKS_PASS {_checks.Count}"); GetTree().Quit();
    }

    private async Task LayoutCheck()
    {
        _rows=SyntheticTable.Generate(100000); _size.Select(2); await Refresh();
        foreach(var size in new[] {new Vector2I(1280,720),new Vector2I(1920,1080),new Vector2I(2560,1440)})
        foreach(float scale in new[] {1f,1.25f,1.5f,2f})
        {
            GetWindow().Size=size; GetWindow().ContentScaleFactor=scale; await Frames(8);
            Rect2 viewport=GetViewportRect();
            foreach(Control item in _toolbar.GetChildren())
                Check(viewport.Encloses(item.GetGlobalRect()),$"{size}/{scale}: {item.GetType().Name} reachable");
            Check(_table.Size.Y>100,$"{size}/{scale}: table remains visible");
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng($"user://qg02-layout-{size.X}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png");
        }
        GD.Print($"QG02_LAYOUT_PASS {_checks.Count}"); GetTree().Quit();
    }
}
