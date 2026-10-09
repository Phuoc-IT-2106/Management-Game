using Godot;
using ManagementGame.Application;
using ManagementGame.Formatting;
using ManagementGame.OperatingMap;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Genre shell (S1–S4): top bar with Continue, collapsible section rail, one content area, overlays.
/// It reads observations and submits typed decisions only; it never computes gameplay.</summary>
public partial class GameShell : Control
{
    private readonly UiContext ui = new(new UiTokens(UiDensity.Compact));
    private IGameSession? session;
    private Situation view = null!;
    /// <summary>The one Portal read model per observation; the top bar and the Portal both render it, so they never disagree.</summary>
    private PortalView portal = null!;
    /// <summary>What the player last saw, so a refresh can acknowledge what changed (P7).</summary>
    private PortalView? seen;
    private string contentPath = "", saveDirectory = "";
    private long requestId;
    private string section = ShellSections.Portal;
    private bool navCollapsed, portalDecisionsExpanded;
    private IdentityAssets assets = null!;
    private readonly HashSet<string> seenInbox = [];
    private VBoxContainer frame = null!, railIdentity = null!;
    /// <summary>The rail's navigation region: the eight items spread evenly, the notice dock under them.</summary>
    private SpreadColumn navList = null!;
    private Control content = null!, overlay = null!;
    private HBoxContainer topBar = null!;
    private Label message = null!;
    private Button continueButton = null!, navToggle = null!;
    private PanelContainer nav = null!;
    private readonly Dictionary<string, Button> navButtons = [];
    private readonly Dictionary<string, Label> navLabels = [], navCounts = [];
    private PanelContainer toast = null!;
    /// <summary>The content area, where the notice floats when the rail is collapsed.</summary>
    private Control contentArea = null!;

    private static readonly (string Id, string Glyph, string Label)[] Sections =
    [
        (ShellSections.Portal, "⌂", "Portal"), (ShellSections.Inbox, "✉", "Inbox"), (ShellSections.Squad, "♟", "Squad"),
        (ShellSections.Competition, "⚑", "Competition"), (ShellSections.Commercial, "¤", "Commercial"), (ShellSections.Finance, "Σ", "Finance"),
        (ShellSections.Staff, "◎", "Staff"), (ShellSections.Company, "▣", "Company")
    ];
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
    private string SavePath => System.IO.Path.Combine(saveDirectory, "campaign.save.json");

    public override async void _Ready()
    {
        Theme = ui.Theme;
        RenderingServer.SetDefaultClearColor(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase)));
        contentPath = Arg("--content") ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../content/fixture.json"));
        saveDirectory = Arg("--saves") ?? ProjectSettings.GlobalizePath("user://saves");
        if (Arg("--shell-viewport") is { } viewport)
        {
            var parts = viewport.Split('x');
            GetWindow().ContentScaleSize = new(int.Parse(parts[0]), int.Parse(parts[1]));
            GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
        }
        if (Arg("--shell-output") is not null) UiTokens.PreferReducedMotion = true;
        // Development diagnostics stay out of player space: debug builds carry them in the log and the window title.
        if (OS.IsDebugBuild())
        {
            GD.Print("DEVELOPMENT BUILD: " + PresentationText.DevelopmentWatermark);
            GetWindow().Title = ProjectSettings.GetSetting("application/config/name").AsString() + " — " + PresentationText.DevelopmentWatermark;
        }
        overlay = new Control { MouseFilter = MouseFilterEnum.Stop, Visible = false };
        if (OS.GetCmdlineUserArgs().Contains("--shell-new")) StartCampaign("", 20261004);
        else ShowStart();
        AddChild(overlay); overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (OS.GetCmdlineUserArgs().Contains("--shell-verify") || Arg("--shell-output") is not null) await RunHarness();
    }

    // ---------- Start screen ----------
    private void ShowStart()
    {
        ClearChildren(this, keep: overlay);
        var margin = Margin(); AddChild(margin); MoveChild(margin, 0);
        var column = ui.Stack(); column.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; column.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        column.CustomMinimumSize = new Vector2(ui.Tokens.Size(SizeRole.ReadingMeasure) * 0.7f, 0);
        margin.AddChild(column);
        column.AddChild(SemanticText.Create(ui, "Strategic Company Simulator", TypographyRole.WorkspaceTitle));
        column.AddChild(SemanticText.Create(ui, "Build an esports organization season after season: people, sponsors, rivals and money.", TypographyRole.Body, ColorRole.TextSecondary));
        // The one decision here (found a company) sits in a premium region (UI polish standard §4).
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        column.AddChild(PanelFrame.Major(ui, "NewCampaignRegion", SectionHeader.Region(ui, "company", "New campaign", roomy, "", null, ColorRole.ActionPrimary, FrameTier.Premium),
            ShellLayout.RegionPadding(true), out var form, 10, FrameTier.Premium));
        var name = new LineEdit { PlaceholderText = "Company name (e.g. Hanoi Signal)", Name = "CompanyName" };
        var seed = new LineEdit { Text = "20261004", Name = "Seed" };
        form.AddChild(Field("Company name", name)); form.AddChild(Field("World seed", seed));
        var feedback = SemanticText.Create(ui, "", TypographyRole.Label, ColorRole.TextSecondary); form.AddChild(feedback);
        var actions = ui.Flow(); form.AddChild(actions);
        var start = ui.PrimaryButton("New campaign", () =>
        {
            // The company is the player's own: a campaign starts with the name they choose, never a content placeholder.
            if (name.Text.Trim().Length == 0) { feedback.Text = PresentationText.WarningMarker + " Name your company to begin."; name.GrabFocus(); return; }
            if (!ulong.TryParse(seed.Text.Trim(), out var value)) { feedback.Text = PresentationText.WarningMarker + " Enter a whole-number seed."; return; }
            try { StartCampaign(name.Text, value); }
            catch (Exception e) when (e is ManagementGame.Domain.RuleViolation or InvalidDataException) { feedback.Text = PresentationText.WarningMarker + " " + e.Message; }
        });
        start.Name = "NewCampaign"; actions.AddChild(start);
        var load = ui.Button("Continue saved campaign", LoadSaved); load.Name = "LoadSaved"; load.Disabled = !System.IO.File.Exists(SavePath); actions.AddChild(load);
        // The internal console is development tooling: reachable in debug builds, absent from a player's release build.
        if (OS.IsDebugBuild()) actions.AddChild(ui.Button("Internal console", () => GetTree().ChangeSceneToFile("res://Main.tscn")));
        start.GrabFocus();
    }
    private Control Field(string label, Control input)
    {
        var box = ui.Stack(); box.AddChild(SemanticText.Create(ui, label, TypographyRole.Label, ColorRole.TextSecondary)); box.AddChild(input);
        input.CustomMinimumSize = new Vector2(0, ui.Tokens.ControlHeight); return box;
    }
    private void StartCampaign(string companyName, ulong seed)
    {
        session = Composition.Create(contentPath, saveDirectory, seed, companyName);
        requestId = 0; seenInbox.Clear(); section = ShellSections.Portal; portalDecisionsExpanded = false; seen = null;
        BuildFrame(); Refresh();
    }
    private void LoadSaved()
    {
        session = Composition.Create(contentPath, saveDirectory, 20261004);
        var result = session.Load("campaign");
        requestId = Composition.LastUiCommand(session); seenInbox.Clear(); section = ShellSections.Portal; seen = null;
        BuildFrame(); Refresh(); Notify(result.Message, !result.Accepted);
    }

    // ---------- Frame ----------
    private MarginContainer Margin()
    {
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, ui.Tokens.Space(SpaceRole.SpaceWorkspace));
        return margin;
    }
    private static void ClearChildren(Node node, Node? keep = null)
    {
        foreach (var child in node.GetChildren()) if (child != keep) { node.RemoveChild(child); child.QueueFree(); }
    }
    private void BuildFrame()
    {
        ClearChildren(this, keep: overlay);
        frame = new VBoxContainer { Name = "Frame" }; frame.AddThemeConstantOverride("separation", 0);
        AddChild(frame); MoveChild(frame, 0); frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // S1 top bar: one compact row spanning the window (approved Portal reference).
        topBar = new HBoxContainer { Name = "TopBar" }; topBar.AddThemeConstantOverride("separation", ShellLayout.BarGap);
        var bar = new PanelContainer { CustomMinimumSize = new Vector2(0, ShellLayout.TopBarHeight) };
        bar.AddThemeStyleboxOverride("panel", Surfaces.Bar(ui.Tokens, ShellLayout.ContentPadding, 6));
        bar.AddChild(topBar); frame.AddChild(bar);
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 0); frame.AddChild(body);
        // The rail: identity, the navigation region (items spread evenly over the notice dock), Collapse. Containers place
        // everything; no item has a fixed coordinate.
        navList = new SpreadColumn { Name = "NavItems", SizeFlagsVertical = SizeFlags.ExpandFill, MinGap = ShellLayout.NavMinGap };
        railIdentity = new VBoxContainer { Name = "RailIdentity", MouseFilter = MouseFilterEnum.Ignore }; railIdentity.AddThemeConstantOverride("separation", 6);
        // The company's identity sits on the rail's header plate (the section-header material, cut at every corner): its
        // lower inset line is the transition into navigation. Its inset matches the items', so the columns line up.
        var identityPlate = new PanelContainer { Name = "RailIdentityPlate", MouseFilter = MouseFilterEnum.Ignore };
        var plateStyle = Surfaces.Header(ui.Tokens, ShellLayout.NavInset, 6, Surfaces.CardCut); plateStyle.ContentMarginBottom += 3;
        identityPlate.AddThemeStyleboxOverride("panel", plateStyle); identityPlate.AddChild(railIdentity);
        navToggle = new Button { Name = "NavToggle", ThemeTypeVariation = "Nav", FocusMode = FocusModeEnum.All };
        navToggle.AddThemeColorOverride("font_color", UiTheme.ToGodot(ui.Tokens.Color(ColorRole.TextSecondary)));
        navToggle.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Annotation));
        navToggle.Pressed += () => { navCollapsed = !navCollapsed; BuildNav(); BuildRailIdentity(portal); UpdateNav(); };
        var rail = new VBoxContainer(); rail.AddThemeConstantOverride("separation", ShellLayout.RailGap);
        rail.AddChild(identityPlate); rail.AddChild(navList); rail.AddChild(navToggle);
        nav = new PanelContainer { Name = "Rail", SizeFlagsHorizontal = SizeFlags.Fill };
        nav.AddThemeStyleboxOverride("panel", Surfaces.Bar(ui.Tokens, ShellLayout.RailInset, ShellLayout.RailInsetVertical));
        nav.AddChild(rail); body.AddChild(nav);
        var area = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true }; body.AddChild(area);
        contentArea = area;
        // Level 0: the room behind the panels. A faint cool light from the upper centre over blue-black graphite and a
        // darker floor, so raised panels have something to sit above. Procedural, no art, no facts.
        var room = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase));
        var backdrop = Backdrop.Create(room, room.Lerp(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.FrameStroke)), .08f), new Color(0, 0, 0, .35f),
            new Backdrop.Glow(new(.55f, -.15f), .9f, UiTheme.ToGodot(ui.Tokens.Color(ColorRole.FrameHighlight)) with { A = .10f }));
        backdrop.Name = "RoomBackdrop"; area.AddChild(backdrop); backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        content = new Control { Name = "Content" }; area.AddChild(content); content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Player feedback (warnings, rejections, save and load) appears as a toast outside the layout. Continue's own
        // narration is not shown (Send): what changed reaches the player as decisions, news and the inbox.
        message = SemanticText.Create(ui, "", TypographyRole.Annotation, ColorRole.TextSecondary);
        message.MaxLinesVisible = 3; message.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        toast = new PanelContainer { Name = "Toast", Visible = false, MouseFilter = MouseFilterEnum.Pass };
        toast.AddThemeStyleboxOverride("panel", SurfaceStyle.Create(Surfaces.Tag(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceOverlay)), UiTheme.ToGodot(ui.Tokens.Color(ColorRole.Divider)), 0, 0).Spec
            with { Cuts = Cuts.Diagonal(Surfaces.ControlCut) }, ui.Tokens.Space(SpaceRole.SpaceGroup), ui.Tokens.Space(SpaceRole.SpaceRelated)));
        toast.AddChild(message);
        BuildNav();
    }
    private void BuildNav()
    {
        toast.GetParent()?.RemoveChild(toast);
        ClearChildren(navList); navButtons.Clear(); navLabels.Clear(); navCounts.Clear();
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        nav.CustomMinimumSize = new Vector2(navCollapsed ? ShellLayout.RailCollapsedWidth(roomy) : ShellLayout.RailWidth(roomy), 0);
        navList.MaxGap = ShellLayout.NavMaxGap(roomy);
        var slotSize = new Vector2(ShellLayout.NavArt(roomy), ShellLayout.NavArt(roomy));
        for (var i = 0; i < Sections.Length; i++)
        {
            var (id, _, label) = Sections[i];
            // One component per section: art slot, label, optional unread badge. Every state keeps the same height and inset.
            var button = new CardButton(navCollapsed ? ShellLayout.NavInsetCollapsed : ShellLayout.NavInset, 0) { ThemeTypeVariation = "Nav", Name = "Nav_" + id,
                TooltipText = $"{label} (Alt+{i + 1})", MinimumHeight = ShellLayout.NavItemHeight(roomy) };
            var row = CardButton.Passive(new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = navCollapsed ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin });
            row.AddThemeConstantOverride("separation", ShellLayout.NavGap(roomy));
            // The section's approved module art, drawn by its visible content at one perceived size. It sits straight on
            // the item's own surface (flat, the hover plate or the brass selection): no housing or backdrop behind it, so
            // its transparent canvas always reads as the rail. A missing asset shows the concept's own vector icon in the
            // same fixed slot, which keeps every label on one column.
            var slot = new CenterContainer { Name = "NavIcon", CustomMinimumSize = slotSize, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            slot.AddChild(SemanticAssets.ForSection(id) is { } concept
                ? SemanticArt.Create(concept, slotSize, fitContent: true, optical: ShellLayout.NavOptical(roomy))
                : UiIcon.Create(ui, id, (int)(slotSize.X * .6f)));
            row.AddChild(slot);
            var name = SemanticText.Create(ui, label, TypographyRole.Label); name.AutowrapMode = TextServer.AutowrapMode.Off; name.Visible = !navCollapsed;
            name.SizeFlagsVertical = SizeFlags.ShrinkCenter; name.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(name);
            // Unread count as a small pill at the item's right, so it reads as a count, not as part of the label.
            var count = SemanticText.Create(ui, "", TypographyRole.Strong, ColorRole.TextPrimary); count.AutowrapMode = TextServer.AutowrapMode.Off;
            count.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Caption));
            count.HorizontalAlignment = HorizontalAlignment.Center; count.VerticalAlignment = VerticalAlignment.Center;
            count.AddThemeStyleboxOverride("normal", Surfaces.Tag(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SelectedSurface)), Colors.Transparent, 6, 0));
            count.CustomMinimumSize = new Vector2(24, 20); count.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            count.SizeFlagsVertical = SizeFlags.ShrinkCenter; count.Visible = false; count.Name = "NavCount"; row.AddChild(count);
            button.Body.AddChild(row); button.Body.SizeFlagsVertical = SizeFlags.ExpandFill;
            button.Pressed += () => Navigate(id);
            navButtons[id] = button; navLabels[id] = name; navCounts[id] = count; navList.AddChild(button);
        }
        // Player notices (warnings, rejections, save and load) dock at the bottom of the rail's free space, above Collapse,
        // where they never cover content; anchored there, a notice never makes the rail taller. The dock keeps room for two
        // lines and takes whatever height the items' gaps leave. A collapsed rail has no room: the notice floats at the
        // bottom right of the content instead.
        var free = new Control { Name = "RailFree", MouseFilter = MouseFilterEnum.Ignore, ClipContents = true,
            CustomMinimumSize = new Vector2(0, navCollapsed ? 0 : ShellLayout.NoticeDock) };
        navList.AddChild(free); navList.Tail = free;
        if (!navCollapsed)
        {
            toast.CustomMinimumSize = Vector2.Zero; free.AddChild(toast);
            toast.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide, LayoutPresetMode.Minsize);
            toast.GrowVertical = GrowDirection.Begin;
        }
        else
        {
            toast.CustomMinimumSize = new Vector2(360, 0); contentArea.AddChild(toast);
            toast.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.KeepSize, ShellLayout.ContentPadding + 4);
            toast.GrowHorizontal = GrowDirection.Begin; toast.GrowVertical = GrowDirection.Begin;
        }
        navToggle.Text = navCollapsed ? "»" : "« Collapse";
        navToggle.Alignment = navCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        navToggle.TooltipText = navCollapsed ? "Expand navigation" : "Collapse navigation";
        navToggle.CustomMinimumSize = new Vector2(0, ShellLayout.NavToggleHeight(roomy));
    }
    /// <summary>The company's identity heads the rail (reference layout): its generated arms by stable ID, the largest
    /// identity mark in the shell, and the player's own name, uppercase, wrapping to two lines rather than being cut. No
    /// generic descriptor repeats what every campaign is. A roomy rail stacks the arms above the name; a compact rail sets
    /// the arms in the art column and the name on the label column, so the identity stays short and aligned with the
    /// items. A collapsed rail keeps the arms only.</summary>
    private void BuildRailIdentity(PortalView model)
    {
        ClearChildren(railIdentity);
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var size = navCollapsed ? ShellLayout.CrestCollapsed : ShellLayout.CrestRail(roomy);
        var crest = assets.Create(model.Company.CrestAssetId, new Vector2(size * 100f / 120, size)); crest.Name = "RailCrest";
        crest.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; crest.TooltipText = model.Company.Name;
        var company = SemanticText.Create(ui, model.Company.Name.ToUpperInvariant(), TypographyRole.Strong); company.Name = "CompanyName";
        // A trimmed name stays whole in its tooltip (the label takes hover for it, nothing else).
        company.TooltipText = model.Company.Name; company.MouseFilter = MouseFilterEnum.Pass; company.Visible = !navCollapsed;
        company.MaxLinesVisible = 2; company.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        if (model.Company.Name.Length > 16) company.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Annotation));
        if (roomy || navCollapsed)
        {
            company.HorizontalAlignment = HorizontalAlignment.Center;
            railIdentity.AddChild(crest); railIdentity.AddChild(company);
            return;
        }
        var row = new HBoxContainer { Name = "RailIdentityRow", MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", ShellLayout.NavGap(roomy));
        var column = new CenterContainer { CustomMinimumSize = new Vector2(ShellLayout.NavArt(roomy), 0), MouseFilter = MouseFilterEnum.Ignore };
        column.AddChild(crest); row.AddChild(column);
        company.SizeFlagsVertical = SizeFlags.ShrinkCenter; row.AddChild(company);
        railIdentity.AddChild(row);
    }
    private void UpdateNav()
    {
        var unread = view.Inbox.Count(x => !seenInbox.Contains(x.Id));
        for (var i = 0; i < Sections.Length; i++)
        {
            var (id, _, label) = Sections[i];
            var count = id == ShellSections.Inbox ? unread : 0;
            navCounts[id].Text = count > 0 ? count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            navCounts[id].Visible = !navCollapsed && count > 0;
            // The tooltip carries the unread count too, so it stays reachable on a collapsed rail, where the badge is hidden.
            navButtons[id].TooltipText = count > 0 ? $"{label}: {count} unread (Alt+{i + 1})" : $"{label} (Alt+{i + 1})";
            navButtons[id].ThemeTypeVariation = id == section ? "NavSelected" : "Nav";
            // The current section's label steps up to the semibold face; others stay quieter in secondary text.
            navLabels[id].ThemeTypeVariation = id == section ? nameof(TypographyRole.Strong) : nameof(TypographyRole.Label);
            ui.Tone(navLabels[id], id == section ? ColorRole.TextPrimary : ColorRole.TextSecondary);
            // The current section's icon takes the selection gold; the label stays high-contrast primary.
            if (Descendants(navButtons[id]).OfType<UiIcon>().FirstOrDefault() is { } icon)
                icon.SetColor(UiTheme.ToGodot(ui.Tokens.Color(id == section ? ColorRole.ActionPrimary : ColorRole.TextSecondary)));
        }
    }
    private void BuildTopBar(PortalView model)
    {
        ClearChildren(topBar);
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        assets = new IdentityAssets(ui.Tokens, model.Company.CrestAssetId);
        BuildRailIdentity(model);
        var time = model.Time;
        var phase = time.Phase switch { SeasonPhase.FirstHalf => "First half", SeasonPhase.SecondHalf => "Second half", _ => "Season complete" };
        var weeks = (time.SeasonLength + PortalProjection.WeekLength - 1) / PortalProjection.WeekLength;
        // Two regions. A: the game state as one row of metrics (UI polish standard §6, top bar): each a caps label over a
        // prominent value with subordinate context after it. Metrics keep their content width and share the free width as
        // equal gaps, so the row spans the bar without dividers and without a large gap before the actions. B: the global
        // actions grouped at the right, Continue last and strongest.
        var info = new HBoxContainer { Name = "TopBarInfo", SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        info.AddThemeConstantOverride("separation", 0);
        topBar.AddChild(info);
        void Add(Control metric)
        {
            if (info.GetChildCount() > 0)
                info.AddChild(new Control { Name = "MetricGap" + info.GetChildCount(), SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore,
                    CustomMinimumSize = new Vector2(ShellLayout.MetricMinGap, 0) });
            info.AddChild(metric);
        }
        var week = (time.DayOfSeason - 1) / PortalProjection.WeekLength + 1;
        Add(Metric("TopBarSeason", "Season " + Figures.Count(time.Season), Value("Week " + Figures.Count(week), "TopBarWeek"), Context("of " + Figures.Count(weeks))));
        // The day leads; the season length and the half are its context.
        Add(Metric("TopBarDay", "Day", Value(Figures.Count(time.DayOfSeason), "TopBarDayValue"), Context("/ " + Figures.Count(time.SeasonLength)),
            Break(), Context(phase, "TopBarPhase")));
        // The next fixture where the bar has room (the Portal always shows it); a long name is bounded, never wrapped, and
        // whole in the tooltip.
        if (roomy && model.NextMatch is { } next)
        {
            var opponent = Value(next.Opponent.Name, "TopBarOpponent", TypographyRole.StatusValue);
            var natural = Face(TypographyRole.StatusValue).GetStringSize(next.Opponent.Name, HorizontalAlignment.Left, -1, ui.Tokens.FontSize(TypographyRole.StatusValue)).X;
            opponent.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            opponent.CustomMinimumSize = new Vector2(MathF.Ceiling(Math.Min(natural, ShellLayout.MetricTextMax)), 0);
            var match = Metric("TopBarNextMatch", "Next match", opponent, Context(PortalScreen.Until(next.Day, time.Day), "TopBarMatchWhen"));
            match.TooltipText = $"vs {next.Opponent.Name}, day {next.DayOfSeason}";
            Add(match);
        }
        // Cash is a known value, not a state: neutral primary text, whole dollars (millions compact); the exact amount is in
        // the tooltip. The financial condition carries the semantic colour.
        var cash = Metric("TopBarCash", "Cash", Value(Currency.Compact(model.Finance.Cash), "TopBarCashValue"));
        cash.TooltipText = $"Cash {Currency.Format(model.Finance.Cash)}, known and settled";
        Add(cash);
        var (condition, tone) = PortalScreen.Condition(model.Finance.Condition);
        Add(Metric("TopBarCondition", roomy ? "Financial condition" : "Finances", Value(condition, "TopBarConditionValue", TypographyRole.StatusValue, tone)));
        Add(PendingMetric(model, roomy));
        var actions = new HBoxContainer { Name = "TopBarActions", MouseFilter = MouseFilterEnum.Ignore }; actions.AddThemeConstantOverride("separation", ShellLayout.BarGap);
        topBar.AddChild(actions);
        actions.AddChild(BarButton("Save", "save", () => Notify(session!.Save("campaign").Message, false)));
        actions.AddChild(BarButton("Load", "load", () => { var r = session!.Load("campaign"); requestId = Composition.LastUiCommand(session); Refresh(); Notify(r.Message, !r.Accepted); }));
        var blocker = model.Decisions.FirstOrDefault(d => d.Urgency == TaskUrgency.Blocking && d.Section == ShellSections.Competition);
        continueButton = new CardButton(16, 4) { Name = "Continue", ThemeTypeVariation = "PrimaryAction", TooltipText = "Ctrl+Enter",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter, MinimumWidth = roomy ? 192 : 148, MinimumHeight = 44 };
        var go = CardButton.Passive(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = SizeFlags.ExpandFill });
        go.AddThemeConstantOverride("separation", 10);
        var goText = SemanticText.Create(ui, blocker is null ? "Continue" : "Prepare match", TypographyRole.SectionTitle, ColorRole.TextOnAction);
        goText.AutowrapMode = TextServer.AutowrapMode.Off; goText.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; goText.SizeFlagsVertical = SizeFlags.ShrinkCenter; go.AddChild(goText);
        go.AddChild(UiIcon.Create(ui, "arrow", 20, ColorRole.TextOnAction));
        ((CardButton)continueButton).Body.AddChild(go); ((CardButton)continueButton).Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        continueButton.Pressed += Continue;
        actions.AddChild(continueButton);
    }
    /// <summary>A top-bar metric: a caps label over one line of a prominent value and its subordinate context. Every item on
    /// the line sits on the value face's baseline (<see cref="Seat"/>), and every line is one value line tall, so labels,
    /// values and context form three straight rows across the bar whatever each metric holds.</summary>
    private VBoxContainer Metric(string name, string label, params Control[] line)
    {
        var metric = new VBoxContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        metric.AddThemeConstantOverride("separation", ShellLayout.MetricLabelGap);
        var caption = SemanticText.Create(ui, label.ToUpperInvariant(), TypographyRole.MetricLabel, ColorRole.TextMuted);
        caption.Name = name + "Label"; caption.AutowrapMode = TextServer.AutowrapMode.Off; caption.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        metric.AddChild(caption);
        var row = new HBoxContainer { Name = name + "Line", MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0, MathF.Ceiling(Face(TypographyRole.MetricValue).GetHeight(ui.Tokens.FontSize(TypographyRole.MetricValue)))) };
        row.AddThemeConstantOverride("separation", ShellLayout.MetricContextGap);
        foreach (var item in line) row.AddChild(item is Label text ? Seat(text) : item);
        metric.AddChild(row);
        return metric;
    }
    /// <summary>The metric's value: semibold tabular figures (<see cref="TypographyRole.MetricValue"/>), or a word value
    /// such as a status (<see cref="TypographyRole.StatusValue"/>). Tone is semantic only where the value is a state.</summary>
    private Label Value(string text, string name, TypographyRole role = TypographyRole.MetricValue, ColorRole tone = ColorRole.TextPrimary)
    {
        var label = SemanticText.Create(ui, text, role, tone); label.Name = name.Length > 0 ? name : role.ToString();
        label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        return label;
    }
    /// <summary>Subordinate context after a value: smaller, regular, muted.</summary>
    private Label Context(string text, string name = "", ColorRole tone = ColorRole.TextMuted) => Value(text, name, TypographyRole.MetricContext, tone);
    /// <summary>A wider break between two pieces of context on one metric line.</summary>
    private static Control Break() => new() { Name = "Break", MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(ShellLayout.MetricContextGap, 0) };
    private Font Face(TypographyRole role) => ui.Theme.GetFont("font", role.ToString());
    /// <summary>Baseline seating: bottom-aligned on the metric line, raised by the difference between the value face's
    /// descent and its own, so a 14 px context and a 16 px status share the 20 px value's baseline.</summary>
    private MarginContainer Seat(Label label)
    {
        var role = Enum.Parse<TypographyRole>(label.ThemeTypeVariation);
        var seat = new MarginContainer { Name = "Seat_" + label.Name, MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkEnd };
        var lift = Face(TypographyRole.MetricValue).GetDescent(ui.Tokens.FontSize(TypographyRole.MetricValue)) - Face(role).GetDescent(ui.Tokens.FontSize(role));
        seat.AddThemeConstantOverride("margin_bottom", (int)MathF.Round(lift));
        seat.AddChild(label);
        return seat;
    }
    /// <summary>Pending decisions as a metric like the others: the count (the Portal's own total of key decisions, so the
    /// two never disagree) and what it means now. Amber only when a decision blocks Continue; zero reads as all clear. The
    /// metric opens the Portal's key decisions.</summary>
    private CardButton PendingMetric(PortalView model, bool roomy)
    {
        var total = model.Decisions.Length;
        var blocking = model.Decisions.Count(d => d.Urgency == TaskUrgency.Blocking);
        var due = model.Decisions.Where(d => d.DueDay is not null).Select(d => d.DueDay!.Value).DefaultIfEmpty(int.MaxValue).Min();
        var (state, tone) = total == 0 ? ("all clear", ColorRole.TextMuted)
            : blocking > 0 ? ($"{PresentationText.WarningMarker} {Figures.Count(blocking)} blocking", ColorRole.StateWarning)
            : due == int.MaxValue ? ("open", ColorRole.TextMuted)
            : due <= model.Time.Day ? ("due today", ColorRole.TextMuted)
            : ("next due " + PortalScreen.Until(due, model.Time.Day), ColorRole.TextMuted);
        var metric = Metric("TopBarDecisions", roomy ? "Pending decisions" : "Decisions",
            Value(Figures.Capped(total), "TopBarDecisionCount", tone: total == 0 ? ColorRole.TextSecondary : ColorRole.TextPrimary),
            Context(state, "TopBarDecisionState", tone));
        var button = new CardButton(6, 2) { Name = "Attention", ThemeTypeVariation = "Entity", SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = PortalScreen.PendingText(total) + (blocking > 0 ? $"; {Figures.Count(blocking)} must be settled before Continue" : "") };
        button.Body.AddChild(CardButton.Passive(metric));
        foreach (var child in Descendants(metric).OfType<Control>().Prepend(metric)) child.MouseFilter = MouseFilterEnum.Ignore;
        button.Pressed += () => Navigate(ShellSections.Portal);
        return button;
    }
    /// <summary>A secondary global action: graphite plate, icon and a semibold label on the bar's action baseline.</summary>
    private CardButton BarButton(string text, string icon, Action action)
    {
        var button = new CardButton(12, 4) { Name = text, ThemeTypeVariation = "", SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter, MinimumHeight = 40 };
        var row = CardButton.Passive(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = SizeFlags.ExpandFill });
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(UiIcon.Create(ui, icon, 18));
        var label = SemanticText.Create(ui, text, TypographyRole.Strong); label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(label);
        button.Body.AddChild(row); button.Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        button.Pressed += action;
        return button;
    }
    /// <summary>Semantic state tone only; the shared component style is unchanged.</summary>
    private void Tone(Button button, ColorRole role)
    {
        foreach (var item in new[] { "font_color", "font_hover_color", "font_focus_color" }) button.AddThemeColorOverride(item, UiTheme.ToGodot(ui.Tokens.Color(role)));
    }

    // ---------- Actions ----------
    private void Continue()
    {
        if (PortalTasks.Blocker(view) is { } blocker) { Navigate(blocker.Section); Notify("Before continuing: " + blocker.Text.ToLowerInvariant() + ".", true); return; }
        Send(new AdvanceDecision());
    }
    private Response Send(Decision decision)
    {
        var response = session!.Submit(new Request("ui:" + ++requestId, view.Revision, decision));
        Refresh();
        // Advancing time is acknowledged by what changed (decisions, news, the inbox count), not by narration.
        if (!response.Accepted || decision is not AdvanceDecision) Notify(response.Message, !response.Accepted);
        return response;
    }
    private void Notify(string text, bool warning)
    {
        message.Text = (warning ? PresentationText.WarningMarker : PresentationText.NeutralMarker) + " " + text; message.Visible = text.Length > 0;
        toast.TooltipText = text;
        toast.Visible = message.Visible;
        ui.Tone(message, warning ? ColorRole.StateWarning : ColorRole.TextPrimary);
        UiMotion.Highlight(ui, toast);
        // Acknowledgements fade from view after a short read; hiding is not motion, so reduced motion keeps it.
        var shown = message.Text;
        GetTree().CreateTimer(warning ? 8 : 5).Timeout += () => { if (IsInstanceValid(toast) && message.Text == shown) toast.Visible = false; };
    }
    private void Refresh()
    {
        Observe();
        BuildTopBar(portal); UpdateNav(); ShowSection();
        Acknowledge();
    }
    private void Navigate(string id)
    {
        // Embedded workspaces (Company, offers) commit through the same session, so always re-observe.
        section = id; CloseOverlay(); Observe(); BuildTopBar(portal); UpdateNav(); ShowSection();
        Acknowledge();
        navButtons[id].GrabFocus();
    }
    private void Observe() { view = session!.Observe(); portal = PortalProjection.Build(view); }
    /// <summary>Explanatory motion (thesis P4/P7): values and matters that changed since the player last looked flash once
    /// (cash, financial condition, the decision count, new decision cards). Reduced motion makes this a no-op.</summary>
    private void Acknowledge()
    {
        var before = seen; seen = portal;
        if (before is null) return;
        void Flash(Node root, string name) { if (Descendants(root).OfType<Control>().FirstOrDefault(c => c.Name == name) is { } c) UiMotion.Highlight(ui, c); }
        if (before.Finance.Cash != portal.Finance.Cash) { Flash(topBar, "TopBarCash"); Flash(content, "MoneyCash"); }
        if (before.Finance.Condition != portal.Finance.Condition) Flash(topBar, "TopBarCondition");
        if (before.Decisions.Length != portal.Decisions.Length) { Flash(topBar, "TopBarDecisions"); Flash(content, "PortalDecisionCount"); }
        if (before.Season.Won + before.Season.Lost != portal.Season.Won + portal.Season.Lost) Flash(content, "RecordRing");
        var known = before.Decisions.Select(d => d.Id).ToHashSet();
        foreach (var card in Descendants(content).OfType<CardButton>().Where(c => c.HasMeta("decision_id") && !known.Contains(c.GetMeta("decision_id").AsString())))
            UiMotion.Highlight(ui, card);
    }
    /// <summary>Portal routes carry a section and a stable target ID; display text never decides where to go.</summary>
    private void OpenTarget(string target, string id)
    {
        if (view.Inbox.Any(r => r.Id == id)) seenInbox.Add(id);
        if (target == ShellSections.Squad && (view.People.Any(p => p.Id == id) || view.Candidates.Any(c => c.Id == id))) squadSelection = id;
        if (target == ShellSections.Staff && view.CoachCandidates.Any(c => c.Id == id)) coachSelection = id;
        Navigate(target);
        if (target == ShellSections.Commercial && view.Offers.Any(o => o.Id == id && o.Availability == "Available")) OpenOffer(id);
    }
    private Control HomeScreen() => PortalScreen.Create(ui, assets, portal, ShellLayout.Roomy(GetViewportRect().Size),
        new PortalScreen.Routes(OpenTarget, id => !seenInbox.Contains(id), () =>
        {
            portalDecisionsExpanded = !portalDecisionsExpanded; ShowSection();
            Descendants(content).OfType<Button>().FirstOrDefault(b => b.Name == "PortalDecisionsMore")?.GrabFocus();
        }, portalDecisionsExpanded));
    private void ShowSection()
    {
        ClearChildren(content);
        var screen = section switch
        {
            ShellSections.Inbox => InboxScreen(), ShellSections.Squad => SquadScreen(), ShellSections.Competition => CompetitionScreen(),
            ShellSections.Commercial => CommercialScreen(), ShellSections.Finance => FinanceScreen(), ShellSections.Staff => StaffScreen(),
            ShellSections.Company => CompanyScreen(), _ => HomeScreen()
        };
        screen.Name = "Screen_" + section;
        content.AddChild(screen); screen.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        UiMotion.Reveal(ui, screen);
    }

    /// <summary>Material commitments are confirmed in an overlay that keeps the shell visible behind it: a premium region
    /// whose header plate names the commitment (UI polish standard §4).</summary>
    private void Confirm(string title, string body, string action, Action commit)
    {
        var stack = ShowOverlay(title);
        stack.AddChild(SemanticText.Create(ui, body));
        var buttons = ui.Flow(); stack.AddChild(buttons);
        var cancel = ui.Button("Cancel · Esc", CloseOverlay); cancel.Name = "ConfirmCancel"; buttons.AddChild(cancel);
        var confirm = ui.PrimaryButton(action, () => { CloseOverlay(); commit(); }); confirm.Name = "ConfirmAccept"; buttons.AddChild(confirm);
        cancel.GrabFocus();
    }
    private VBoxContainer ShowOverlay(string title)
    {
        ClearChildren(overlay); overlay.Visible = true;
        overlay.AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Stop });
        ((ColorRect)overlay.GetChild(0)).SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); overlay.AddChild(center); center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        var panel = PanelFrame.Major(ui, "ConfirmPanel", SectionHeader.Region(ui, "alert", title, roomy, "", null, ColorRole.ActionPrimary, FrameTier.Premium),
            ShellLayout.RegionPadding(true), out var body, 12, FrameTier.Premium);
        panel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter; panel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        panel.CustomMinimumSize = new Vector2(ui.Tokens.Size(SizeRole.ReadingMeasure) * 0.75f, 0); center.AddChild(panel);
        return body;
    }
    private void CloseOverlay() { if (overlay is null) return; ClearChildren(overlay); overlay.Visible = false; }

    /// <summary>Offers open the verified sponsor decision workspace over the shell and return to Commercial.</summary>
    private void OpenOffer(string offerId)
    {
        var sponsors = (ISponsorSession)session!;
        ClearChildren(overlay); overlay.Visible = true;
        var backdrop = new ColorRect { Color = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase)) };
        overlay.AddChild(backdrop); backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var workspace = new SponsorWorkspaceView { Name = "OfferWorkspace", ShowWatermark = false };
        var origin = new MapReturn(view.Company, MapFixtures.Business, "matter:" + offerId, true, "shell:commercial", 0);
        workspace.Configure(new SponsorPresenter(sponsors, sponsors.ObserveSponsors(), offerId, origin), () =>
        {
            CloseOverlay(); requestId = Math.Max(requestId, Composition.LastUiCommand(session!)); Refresh(); Navigate(ShellSections.Commercial);
        });
        overlay.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false } key || session is null || frame is null) return;
        if (key.Keycode == Key.Escape && overlay.Visible && overlay.GetNodeOrNull("OfferWorkspace") is null) { CloseOverlay(); GetViewport().SetInputAsHandled(); return; }
        if (overlay.Visible) return;
        if (key.CtrlPressed && key.Keycode is Key.Enter or Key.KpEnter) { Continue(); GetViewport().SetInputAsHandled(); return; }
        if (key.AltPressed && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key8) { Navigate(Sections[(int)(key.Keycode - Key.Key1)].Id); GetViewport().SetInputAsHandled(); }
    }
}
