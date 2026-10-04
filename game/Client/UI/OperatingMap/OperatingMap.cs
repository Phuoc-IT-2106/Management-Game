using Godot;
using ManagementGame.UiKit;
using ManagementGame.OperatingMap;
using System.Diagnostics;

/// <summary>Bounded development projection. Never constructs a campaign or submits a command.</summary>
public partial class OperatingMap : Control
{
    private readonly UiContext ui = new(new UiTokens(UiDensity.Compact));
    private MapNavigation navigation = null!;
    private Control layout = null!;
    private VBoxContainer map = null!, list = null!, inspector = null!, header = null!, affairs = null!;
    private ScrollContainer mapScroll = null!, inspectorScroll = null!;
    private Label path = null!, notice = null!;
    private Button mode = null!, openEntry = null!;
    private readonly Dictionary<string, EntityLabel> targets = [];
    private readonly Dictionary<string, List<double>> timings = [];
    private int intentCount;
    private string originFocus = "", scenario = "normal";
    private bool isWorkspace;
    private MapSnapshot Data => navigation.Snapshot;
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
    private bool VerifyRequested => OS.GetCmdlineUserArgs().Contains("--map-verify");
    private void Measure(string key, long start)
    { if (!timings.TryGetValue(key, out var values)) timings[key] = values = []; values.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }

    public override async void _Ready()
    {
        try
        {
            GetWindow().ContentScaleSize = Vector2I.Zero;
            if (Arg("--map-viewport") is { } viewport)
            {
                var parts = viewport.Split('x'); GetWindow().ContentScaleSize = new(int.Parse(parts[0]), int.Parse(parts[1]));
                GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
                GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
            }
            scenario = Arg("--map-case") ?? "normal";
            navigation = new(MapFixtures.Create(scenario));
            var start = Stopwatch.GetTimestamp(); Build(); Measure("initial-bind", start);
            await Settle();
            if (VerifyRequested) await VerifyNative();
            ApplyScenario(); await Settle();
            FocusTarget(originFocus.Length > 0 ? originFocus : "map:" + navigation.ScopeId);
            if (Arg("--map-output") is not null)
            {
                Input.WarpMouse(Vector2.Zero); await Settle(); VerifyLayout();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Capture();
            }
            if (VerifyRequested || Arg("--map-output") is not null)
            { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GetTree().Quit(); }
        }
        catch (Exception e) { GD.PrintErr("OPERATING_MAP_FAIL " + e); GetTree().Quit(1); }
    }

    private void Build()
    {
        if (layout is not null) { RemoveChild(layout); layout.QueueFree(); }
        targets.Clear(); Theme = ui.Theme;
        RenderingServer.SetDefaultClearColor(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase)));
        var margin = new MarginContainer(); layout = margin; AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, ui.Tokens.Space(SpaceRole.SpaceWorkspace));
        var shell = ui.Stack(); margin.AddChild(shell);
        header = ui.Stack(); shell.AddChild(header);
        header.AddChild(NavigationContext.Create(ui, Data.Organization, [], null));
        var bar = ui.Flow(); shell.AddChild(bar);
        bar.AddChild(ui.Button("Company context", () => { if (navigation.Return is not null) Back(); Select(Data.Organization.Id); }));
        bar.AddChild(ui.Button("Back / parent · Esc", Back));
        mode = ui.Button("Use ownership list", ToggleMode); bar.AddChild(mode);
        bar.AddChild(ui.Button("Affairs / time", () => affairs.Visible = !affairs.Visible));
        var time = SemanticText.Create(ui, $"Fixture day {Data.Day} · " + PresentationText.Time(Data.NextCheckpoint, "Next checkpoint"), TypographyRole.Label, ColorRole.TextSecondary);
        time.CustomMinimumSize = new(UiTokens.MinimumColumnWidth, 0); bar.AddChild(time);
        path = SemanticText.Create(ui, "", TypographyRole.Label); shell.AddChild(path);
        notice = ValidationMessage.Create(ui, "", false); notice.Visible = false; shell.AddChild(notice);
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; shell.AddChild(body);
        mapScroll = ReadingScroll(); body.AddChild(mapScroll);
        var mapHost = ui.Stack(); mapScroll.AddChild(mapHost);
        map = ui.Stack(); list = ui.Stack(); mapHost.AddChild(map); mapHost.AddChild(list);
        BuildMap(); BuildList();
        inspectorScroll = ReadingScroll(); inspectorScroll.CustomMinimumSize = new(ui.Tokens.Size(SizeRole.InspectorWidth), 0);
        inspectorScroll.SizeFlagsHorizontal = SizeFlags.Fill; body.AddChild(inspectorScroll);
        inspector = ui.Stack(); inspectorScroll.AddChild(inspector);
        affairs = ui.Stack(); shell.AddChild(affairs); BuildAffairs(); affairs.Visible = false;
        shell.AddChild(SemanticText.Create(ui, UiTokens.Notice + " · Map/list: same context · Tab/Arrows: focus · Enter: inspect · Esc: return", TypographyRole.Annotation, ColorRole.TextMuted));
        if (GetViewportRect().Size.X < 2 * UiTokens.MinimumColumnWidth + ui.Tokens.Size(SizeRole.InspectorWidth)) navigation.ListMode = true;
        isWorkspace = false; RefreshSelection();
    }
    private ScrollContainer ReadingScroll()
    {
        var scroll = new ScrollContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, FocusMode = FocusModeEnum.All };
        scroll.GuiInput += e =>
        {
            if (e is not InputEventKey { Pressed: true } key || !scroll.HasFocus()) return;
            var next = key.Keycode switch { Key.Home => 0, Key.End => (int)scroll.GetVScrollBar().MaxValue,
                Key.Pagedown => scroll.ScrollVertical + (int)scroll.Size.Y, Key.Pageup => scroll.ScrollVertical - (int)scroll.Size.Y, _ => -1 };
            if (next < 0 && key.Keycode != Key.Pageup) return;
            scroll.ScrollVertical = Math.Max(0, next); scroll.AcceptEvent();
        };
        return scroll;
    }
    private EntityView ScopeEntity(MapScope scope) => new(scope.Id, scope.Name,
        scope.Kind == ScopeKind.Company ? EntityKind.Company : scope.Kind == ScopeKind.Function ? EntityKind.Department : scope.Kind == ScopeKind.Team ? EntityKind.Team : EntityKind.Division,
        scope.Kind.ToString(), Data.Revision);
    private bool Emphasized(MapSituation matter) => Data.Situations.OrderBy(x => x.Priority).ThenBy(x => x.DueDay ?? int.MaxValue).ThenBy(x => x.Id, StringComparer.Ordinal)
        .Where(x => x.Priority is Attention.Critical or Attention.High).Take(2).Any(x => x.Id == matter.Id);
    private EntityView SituationEntity(MapSituation matter) => new(matter.Id, matter.Name, EntityKind.Decision,
        $"{matter.Priority} · {matter.Status}", Data.Revision, matter.Information,
        Emphasized(matter) ? matter.Priority == Attention.Critical ? DomainStatus.Critical : DomainStatus.Warning : DomainStatus.Neutral);
    private MatterView Matter(MapSituation matter) => new(SituationEntity(matter), MatterClass.Actionable, matter.Reason, matter.DueDay, matter.Consequence);
    private EntityLabel Target(EntityView entity, string region)
    {
        var label = new EntityLabel(); var key = region + ":" + entity.Id;
        label.Bind(ui, entity, intent => { intentCount++; originFocus = key; Select(intent.TargetId, intent.Revision); });
        targets[key] = label;
        // Explicit arrows within the current representation; focus and selection remain separate.
        label.GuiInput += e =>
        {
            if (e is not InputEventKey { Pressed: true } k || k.Keycode is not (Key.Up or Key.Down)) return;
            var items = targets.Where(x => x.Key.StartsWith(region + ":", StringComparison.Ordinal) && x.Value.IsVisibleInTree()).Select(x => x.Value).ToArray();
            var index = Array.IndexOf(items, label); var delta = k.Keycode == Key.Down ? 1 : -1;
            if (index >= 0 && items.Length > 0) items[Math.Clamp(index + delta, 0, items.Length - 1)].GrabFocus();
            label.AcceptEvent();
        };
        return label;
    }
    private void BuildMap()
    {
        map.AddChild(SectionHeader.Create(ui, "Company operating map", "Owns the portfolio · shared functions support its scopes"));
        map.AddChild(Target(ScopeEntity(Data.Scope(Data.Organization.Id)), "map"));
        var lanes = new HBoxContainer(); map.AddChild(lanes);
        var portfolio = ui.Stack(); var functions = ui.Stack(); lanes.AddChild(portfolio); lanes.AddChild(functions);
        portfolio.AddChild(SectionHeader.Create(ui, "Operating portfolio", "Owned business scopes"));
        foreach (var scope in Data.Scopes.Where(x => x.Kind is not (ScopeKind.Company or ScopeKind.Function)))
        {
            var box = ui.Stack();
            if (scope.Id != MapFixtures.Portfolio) box.AddChild(SemanticText.Create(ui, "↓ contains", TypographyRole.Annotation, ColorRole.TextSecondary));
            box.AddChild(Target(ScopeEntity(scope), "map"));
            AddAttached(box, scope.Id, "map"); portfolio.AddChild(box);
        }
        functions.AddChild(SectionHeader.Create(ui, "Corporate functions", "Shared support capabilities"));
        foreach (var scope in Data.Scopes.Where(x => x.Kind == ScopeKind.Function))
        {
            var box = ui.Stack(); box.AddChild(Target(ScopeEntity(scope), "map"));
            foreach (var link in Data.Links.Where(x => x.From == scope.Id))
                box.AddChild(SemanticText.Create(ui, link.Meaning + " → " + (link.To == Data.Organization.Id ? "company scopes" : Data.Scope(link.To).Name), TypographyRole.Annotation, ColorRole.TextSecondary));
            AddAttached(box, scope.Id, "map"); functions.AddChild(ui.Panel(box, ColorRole.SurfaceInset));
        }
        if (Data.Situations.IsEmpty) map.AddChild(ValidationMessage.Create(ui, "No active situations. Company structure and the next checkpoint remain available.", false));
    }
    private void AddAttached(VBoxContainer box, string scopeId, string region)
    {
        foreach (var matter in Data.Situations.Where(x => x.ScopeId == scopeId).OrderBy(x => x.Priority))
        {
            var marker = Target(SituationEntity(matter), region); box.AddChild(marker);
            if (Emphasized(matter)) box.AddChild(StatusBadge.Create(ui, SituationEntity(matter).Status, matter.Priority + " · " + PresentationText.Time(matter.DueDay, "Review")));
        }
    }
    private void BuildList()
    {
        list.AddChild(SectionHeader.Create(ui, "Ownership list", "Same scopes and situations · indentation means ownership"));
        foreach (var scope in Data.Scopes.Where(x => x.Kind != ScopeKind.Function)) AddListScope(scope);
        list.AddChild(SectionHeader.Create(ui, "Company functions", "Support capabilities; separate from the owned portfolio"));
        foreach (var scope in Data.Scopes.Where(x => x.Kind == ScopeKind.Function)) AddListScope(scope);
    }
    private void AddListScope(MapScope scope)
    {
        var margin = new MarginContainer(); margin.AddThemeConstantOverride("margin_left", (Data.Path(scope.Id).Length - 1) * ui.Tokens.Space(SpaceRole.SpaceGroup));
        var row = ui.Stack(); margin.AddChild(row); row.AddChild(Target(ScopeEntity(scope), "list")); AddAttached(row, scope.Id, "list"); list.AddChild(margin);
    }
    private void BuildAffairs()
    {
        affairs.AddChild(SectionHeader.Create(ui, "Affairs & time", PresentationText.Time(Data.NextCheckpoint, "Next competitive checkpoint") + " · select a dated matter to locate its scope"));
        var strip = new HBoxContainer(); affairs.AddChild(strip);
        foreach (var matter in Data.Situations.OrderBy(x => x.DueDay ?? int.MaxValue))
        {
            var item = TimelineEvent.Create(ui, Matter(matter) with { Why = Data.Scope(matter.ScopeId).Name, Consequence = "" }, intent =>
            { intentCount++; originFocus = "affairs:" + matter.Id; Select(intent.TargetId, intent.Revision); });
            strip.AddChild(item);
            var button = Descendants(item).OfType<EntityLabel>().Single(); targets["affairs:" + matter.Id] = button;
        }
        if (Data.Situations.IsEmpty) affairs.AddChild(SemanticText.Create(ui, "No active situations. Next checkpoint remains scheduled."));
    }
    private void Select(string id, long? revision = null)
    {
        var start = Stopwatch.GetTimestamp(); navigation.Select(id, revision ?? Data.Revision); RefreshSelection(); Measure("selection-update", start);
        if (originFocus.StartsWith("inspector:", StringComparison.Ordinal) && navigation.Situation is not null) openEntry.GrabFocus();
    }
    private void RefreshSelection()
    {
        var start = Stopwatch.GetTimestamp();
        map.Visible = !navigation.ListMode && !isWorkspace; list.Visible = navigation.ListMode && !isWorkspace;
        mode.Text = navigation.ListMode ? "Use operating map" : "Use ownership list"; mode.Disabled = isWorkspace;
        foreach (var target in targets.Values)
            target.SetSelected(target.Binding.Current?.Id == navigation.ScopeId || target.Binding.Current?.Id == navigation.SituationId);
        path.Text = "Company context / " + string.Join(" / ", Data.Path(navigation.ScopeId).Skip(1).Select(x => x.Name)) +
            (navigation.Situation is { } matter ? " / " + matter.Name : "");
        notice.Text = navigation.Notice; notice.Visible = navigation.Notice.Length > 0;
        Measure("map-list-synchronization", start); BindInspector();
    }
    private void ClearInspector()
    {
        foreach (var key in targets.Keys.Where(x => x.StartsWith("inspector:", StringComparison.Ordinal)).ToArray()) targets.Remove(key);
        foreach (var child in inspector.GetChildren()) { inspector.RemoveChild(child); child.QueueFree(); }
    }
    private void BindInspector()
    {
        var start = Stopwatch.GetTimestamp(); ClearInspector(); inspectorScroll.ScrollVertical = 0;
        var scope = Data.Scope(navigation.ScopeId);
        inspector.AddChild(SectionHeader.Create(ui, navigation.Situation is null ? scope.Name : "Situation context", scope.Kind + " · " + (scope.Kind == ScopeKind.Company ? "Company root" : scope.Name)));
        if (navigation.Situation is { } matter)
        {
            inspector.AddChild(AlertItem.Create(ui, Matter(matter), null));
            inspector.AddChild(ConfidenceIndicator.Create(ui, matter.Information, matter.Evidence));
            inspector.AddChild(SemanticText.Create(ui, "Affected scope: " + scope.Name + " · Category: " + matter.Category, TypographyRole.Label));
            openEntry = ui.Button("Open decision entry", Enter); inspector.AddChild(openEntry);
            inspector.AddChild(SemanticText.Create(ui, matter.WorkspaceKind + " · read-only prototype", TypographyRole.Annotation, ColorRole.TextSecondary));
        }
        else
        {
            inspector.AddChild(SemanticText.Create(ui, scope.Description));
            var related = scope.Kind is ScopeKind.Company or ScopeKind.Function
                ? Data.Situations.Where(x => scope.Kind == ScopeKind.Company || scope.Id == MapFixtures.Affairs || x.ScopeId == scope.Id || scope.Id == MapFixtures.Talent && x.Category == "Talent")
                : Data.Situations.Where(x => Data.Path(x.ScopeId).Any(s => s.Id == scope.Id));
            var ordered = related.OrderBy(x => x.Priority).ThenBy(x => x.DueDay ?? int.MaxValue).ToArray();
            inspector.AddChild(SectionHeader.Create(ui, ordered.Length == 0 ? "No active situations" : "Situations in this context"));
            foreach (var situation in ordered) inspector.AddChild(Target(SituationEntity(situation), "inspector"));
            if (ordered.All(x => x.Priority >= Attention.Normal)) inspector.AddChild(ValidationMessage.Create(ui, "No urgent situations. Inspect structure or the next checkpoint.", false));
            foreach (var link in Data.Links.Where(x => x.From == scope.Id || x.To == scope.Id))
            {
                inspector.AddChild(SemanticText.Create(ui, Data.Scope(link.From).Name + " " + link.Meaning.ToLowerInvariant() + " " + Data.Scope(link.To).Name, TypographyRole.Label));
                var other = link.From == scope.Id ? link.To : link.From; inspector.AddChild(Target(ScopeEntity(Data.Scope(other)), "inspector"));
            }
            inspector.AddChild(TimeMarker.Create(ui, Data.NextCheckpoint, "Next competitive checkpoint"));
        }
        Measure("inspector-rebind", start);
    }
    private void ToggleMode()
    {
        if (isWorkspace) return;
        navigation.ListMode = !navigation.ListMode; RefreshSelection();
        originFocus = (navigation.ListMode ? "list:" : "map:") + (navigation.SituationId ?? navigation.ScopeId); FocusTarget(originFocus);
    }
    private void Enter()
    {
        var start = Stopwatch.GetTimestamp();
        if (navigation.Situation is not { } matter || !navigation.Enter(matter.TargetId, Data.Revision, originFocus, mapScroll.ScrollVertical)) return;
        isWorkspace = true; map.Visible = list.Visible = false; mode.Disabled = true; ClearInspector();
        // The entry occupies the map reading region, keeping company and selected scope visible.
        var workspace = ui.Stack(); workspace.Name = "DecisionEntry"; map.GetParent().AddChild(workspace);
        workspace.AddChild(SectionHeader.Create(ui, matter.WorkspaceKind, "Decision workspace entry · read-only prototype"));
        workspace.AddChild(DocumentView.Create(ui, new(matter.TargetId, Data.Revision, matter.Name, PresentationText.FixtureNotice,
            [new("Company scope", Data.Scope(matter.ScopeId).Name), new("Why now", matter.Reason), new("Trade-off", matter.Consequence),
             new("Evidence", PresentationText.Information(matter.Information) + " · " + matter.Evidence),
             new("Handoff", "Company, affected scope and originating situation are retained. No command is submitted.")])));
        var back = ui.Button("Return to originating context", Back); workspace.AddChild(back); back.GrabFocus();
        inspector.AddChild(SectionHeader.Create(ui, "Origin retained", Data.Scope(navigation.ScopeId).Name));
        inspector.AddChild(SemanticText.Create(ui, matter.Name));
        inspector.AddChild(ValidationMessage.Create(ui, "This entry cannot commit, negotiate, sign, release or advance time.", false));
        Measure("decision-entry", start);
    }
    private void Back()
    {
        var start = Stopwatch.GetTimestamp(); var origin = navigation.Back();
        if (isWorkspace)
        {
            var entry = map.GetParent().GetNodeOrNull<Control>("DecisionEntry");
            if (entry is not null) { entry.GetParent().RemoveChild(entry); entry.QueueFree(); }
        }
        isWorkspace = false; RefreshSelection();
        originFocus = origin?.FocusKey ?? (navigation.ListMode ? "list:" : "map:") + navigation.ScopeId;
        FocusTarget(originFocus); if (origin is not null) mapScroll.ScrollVertical = origin.Scroll;
        Measure("return-navigation", start);
    }
    private void FocusTarget(string key)
    {
        if (targets.TryGetValue(key, out var target) && target.IsVisibleInTree()) target.GrabFocus();
        else if (key.StartsWith("inspector:", StringComparison.Ordinal) && navigation.Situation is not null && !isWorkspace) openEntry.GrabFocus();
        else if (targets.TryGetValue((navigation.ListMode ? "list:" : "map:") + (navigation.SituationId ?? navigation.ScopeId), out target) && target.IsVisibleInTree()) target.GrabFocus();
    }
    private void ApplyScenario()
    {
        navigation = new(MapFixtures.Create(scenario)); Build(); originFocus = "map:" + Data.Organization.Id;
        if (scenario is "sponsor" or "decision" or "return" or "list" or "affairs") { originFocus = "map:matter:sponsor"; Select("matter:sponsor"); }
        if (scenario == "competitive") { originFocus = "map:matter:preparation"; Select("matter:preparation"); }
        if (scenario == "list") ToggleMode();
        if (scenario is "decision" or "return") Enter();
        if (scenario == "return") Back();
        if (scenario == "affairs") affairs.Visible = true;
    }
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) { Back(); GetViewport().SetInputAsHandled(); }
    }
}
