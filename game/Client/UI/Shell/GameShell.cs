using Godot;
using ManagementGame.Application;
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
    private VBoxContainer frame = null!, navList = null!;
    private Control content = null!, overlay = null!;
    private HBoxContainer topBar = null!;
    private Label message = null!;
    private Button continueButton = null!;
    private PanelContainer nav = null!;
    private readonly Dictionary<string, Button> navButtons = [];
    private readonly Dictionary<string, Label> navLabels = [], navCounts = [];
    private PanelContainer toast = null!;

    private static string IconFor(string section) => section switch
    {
        ShellSections.Inbox => "inbox", ShellSections.Squad => "squad", ShellSections.Competition => "competition", ShellSections.Commercial => "commercial",
        ShellSections.Finance => "finance", ShellSections.Staff => "staff", ShellSections.Company => "company", _ => "portal"
    };
    private static readonly (string Id, string Glyph, string Label)[] Sections =
    [
        (ShellSections.Portal, "⌂", "Portal"), (ShellSections.Inbox, "✉", "Inbox"), (ShellSections.Squad, "♟", "Squad"),
        (ShellSections.Competition, "⚑", "Competition"), (ShellSections.Commercial, "¤", "Commercial"), (ShellSections.Finance, "Σ", "Finance"),
        (ShellSections.Staff, "◎", "Staff"), (ShellSections.Company, "▣", "Company")
    ];
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
    private static string Money(long minor) => SponsorPresentation.Money(minor);
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
        var name = new LineEdit { PlaceholderText = "Company name (e.g. Hanoi Signal)", Name = "CompanyName" };
        var seed = new LineEdit { Text = "20261004", Name = "Seed" };
        column.AddChild(Field("Company name", name)); column.AddChild(Field("World seed", seed));
        var feedback = SemanticText.Create(ui, "", TypographyRole.Label, ColorRole.TextSecondary); column.AddChild(feedback);
        var actions = ui.Flow(); column.AddChild(actions);
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
        actions.AddChild(ui.Button("Internal console", () => GetTree().ChangeSceneToFile("res://Main.tscn")));
        column.AddChild(ui.Watermark());
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
        bar.AddThemeStyleboxOverride("panel", Edge(ColorRole.SurfaceInset, bottom: true, horizontal: ShellLayout.ContentPadding, vertical: 6));
        bar.AddChild(topBar); frame.AddChild(bar);
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddThemeConstantOverride("separation", 0); frame.AddChild(body);
        navList = ui.Stack(); navList.AddThemeConstantOverride("separation", 4);
        nav = new PanelContainer { Name = "Rail", SizeFlagsHorizontal = SizeFlags.Fill };
        nav.AddThemeStyleboxOverride("panel", Edge(ColorRole.SurfaceInset, right: true, horizontal: 8, vertical: 12));
        nav.AddChild(navList); body.AddChild(nav);
        var area = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true }; body.AddChild(area);
        content = new Control { Name = "Content" }; area.AddChild(content); content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Stop reasons appear as a toast outside the layout. The development marker lives in the rail footer (BuildNav).
        message = SemanticText.Create(ui, "", TypographyRole.Annotation, ColorRole.TextSecondary);
        toast = new PanelContainer { Name = "Toast", Visible = false, MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(360, 0) };
        var toastBox = UiTheme.Box(ui.Tokens, ColorRole.SurfaceOverlay); toastBox.BorderColor = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.Divider));
        toast.AddThemeStyleboxOverride("panel", toastBox); toast.AddChild(message);
        // The toast sits over the top of the content (the Portal header band) so it never hides a decision or a value.
        area.AddChild(toast); toast.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize, ShellLayout.ContentPadding + 4);
        toast.GrowHorizontal = GrowDirection.Both; toast.GrowVertical = GrowDirection.End;
        BuildNav();
    }
    /// <summary>Flat shell surface with one divider edge; regions are separated by surfaces and lines, not boxes.</summary>
    private StyleBoxFlat Edge(ColorRole surface, bool top = false, bool bottom = false, bool right = false, int horizontal = 0, int vertical = 0) => new()
    {
        BgColor = UiTheme.ToGodot(ui.Tokens.Color(surface)), BorderColor = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.Divider)),
        BorderWidthTop = top ? 1 : 0, BorderWidthBottom = bottom ? 1 : 0, BorderWidthRight = right ? 1 : 0,
        ContentMarginLeft = horizontal, ContentMarginRight = horizontal, ContentMarginTop = vertical, ContentMarginBottom = vertical
    };
    private void BuildNav()
    {
        ClearChildren(navList); navButtons.Clear(); navLabels.Clear(); navCounts.Clear();
        nav.CustomMinimumSize = new Vector2(navCollapsed ? ShellLayout.RailCollapsedWidth : ShellLayout.RailWidth, 0);
        var roomy = ShellLayout.Roomy(GetViewportRect().Size);
        for (var i = 0; i < Sections.Length; i++)
        {
            var (id, _, label) = Sections[i];
            var button = new CardButton(12, 0) { ThemeTypeVariation = "Nav", Name = "Nav_" + id, TooltipText = $"{label} (Alt+{i + 1})",
                MinimumHeight = ShellLayout.NavItemHeight(roomy) };
            var row = CardButton.Passive(new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = navCollapsed ? BoxContainer.AlignmentMode.Center : BoxContainer.AlignmentMode.Begin });
            row.AddThemeConstantOverride("separation", 12);
            row.AddChild(UiIcon.Create(ui, IconFor(id), 22));
            var name = SemanticText.Create(ui, label, TypographyRole.Label); name.AutowrapMode = TextServer.AutowrapMode.Off; name.Visible = !navCollapsed;
            name.SizeFlagsVertical = SizeFlags.ShrinkCenter; row.AddChild(name);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            // Unread count as a small pill so it reads as a count, not as part of the label.
            var count = SemanticText.Create(ui, "", TypographyRole.Strong, ColorRole.TextPrimary); count.AutowrapMode = TextServer.AutowrapMode.Off;
            count.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Caption)); count.HorizontalAlignment = HorizontalAlignment.Center;
            var pill = new StyleBoxFlat { BgColor = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SelectedSurface)) }; pill.SetCornerRadiusAll(UiTokens.CornerRadius * 2);
            pill.ContentMarginLeft = pill.ContentMarginRight = 7; count.AddThemeStyleboxOverride("normal", pill);
            count.SizeFlagsVertical = SizeFlags.ShrinkCenter; count.Visible = false; row.AddChild(count);
            button.Body.AddChild(row); button.Body.SizeFlagsVertical = SizeFlags.ExpandFill;
            button.Pressed += () => Navigate(id);
            navButtons[id] = button; navLabels[id] = name; navCounts[id] = count; navList.AddChild(button);
        }
        navList.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        if (DevelopmentMarker() is { } marker) navList.AddChild(marker);
        var toggle = new Button { Name = "NavToggle", Text = navCollapsed ? "»" : "« Collapse", ThemeTypeVariation = "Nav", FocusMode = FocusModeEnum.All,
            Alignment = navCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 36),
            TooltipText = navCollapsed ? "Expand navigation" : "Collapse navigation" };
        toggle.AddThemeColorOverride("font_color", UiTheme.ToGodot(ui.Tokens.Color(ColorRole.TextSecondary)));
        toggle.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Annotation));
        toggle.Pressed += () => { navCollapsed = !navCollapsed; BuildNav(); UpdateNav(); };
        navList.AddChild(toggle);
    }
    /// <summary>The single development marker (thesis P5), in the rail footer where it never overlaps gameplay content.
    /// Debug builds only: a release export (OS.IsDebugBuild() false) shows no marker at all.</summary>
    private Control? DevelopmentMarker()
    {
        if (!OS.IsDebugBuild()) return null;
        var box = new PanelContainer { Name = "DevelopmentMarker", TooltipText = PresentationText.DevelopmentWatermark, MouseFilter = MouseFilterEnum.Pass };
        var frame = new StyleBoxFlat { DrawCenter = false, BorderColor = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.Divider)) };
        frame.SetBorderWidthAll(1); frame.SetCornerRadiusAll(UiTokens.CornerRadius); frame.SetContentMarginAll(6);
        box.AddThemeStyleboxOverride("panel", frame);
        var text = SemanticText.Create(ui, navCollapsed ? "DEV" : "Development build\nNoncanonical fixture", TypographyRole.Caption, ColorRole.TextMuted);
        text.AutowrapMode = TextServer.AutowrapMode.Off; text.HorizontalAlignment = navCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        box.AddChild(text);
        return box;
    }
    private void UpdateNav()
    {
        var unread = view.Inbox.Count(x => !seenInbox.Contains(x.Id));
        foreach (var (id, _, _) in Sections)
        {
            navCounts[id].Text = id == ShellSections.Inbox && unread > 0 ? unread.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            navCounts[id].Visible = !navCollapsed && navCounts[id].Text.Length > 0;
            navButtons[id].ThemeTypeVariation = id == section ? "NavSelected" : "Nav";
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
        var identity = new HBoxContainer { Name = "Identity", SizeFlagsVertical = SizeFlags.ShrinkCenter }; identity.AddThemeConstantOverride("separation", 10);
        identity.AddChild(assets.Create(model.Company.CrestAssetId, new Vector2(ShellLayout.CrestTopBar * 100f / 120, ShellLayout.CrestTopBar)));
        var names = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter }; names.AddThemeConstantOverride("separation", 0);
        // The player's own name, in the reference's uppercase identity style; long names wrap to two lines instead of being cut.
        var companyName = model.Company.Name;
        var company = SemanticText.Create(ui, companyName.ToUpperInvariant(), TypographyRole.Strong); company.Name = "CompanyName"; company.TooltipText = companyName;
        if (companyName.Length > 18) { company.CustomMinimumSize = new Vector2(roomy ? 260 : 190, 0); company.MaxLinesVisible = 2; company.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; company.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(TypographyRole.Annotation)); }
        else { company.AutowrapMode = TextServer.AutowrapMode.Off; company.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; }
        names.AddChild(company);
        if (companyName.Length <= 18 || roomy) names.AddChild(Fixed(SemanticText.Create(ui, "ESPORTS ORGANIZATION", TypographyRole.Caption, ColorRole.TextMuted)));
        identity.AddChild(names); topBar.AddChild(identity);
        topBar.AddChild(Divider());
        var phase = model.Time.Phase switch { SeasonPhase.FirstHalf => "First half", SeasonPhase.SecondHalf => "Second half", _ => "Season complete" };
        var weeks = (model.Time.SeasonLength + PortalProjection.WeekLength - 1) / PortalProjection.WeekLength;
        topBar.AddChild(Fact(null, $"SEASON {model.Time.Season}", $"Week {(model.Time.DayOfSeason - 1) / PortalProjection.WeekLength + 1} of {weeks}", ColorRole.TextPrimary));
        topBar.AddChild(Divider());
        topBar.AddChild(Fact("calendar", $"DAY {model.Time.DayOfSeason} / {model.Time.SeasonLength}", phase, ColorRole.TextSecondary, TypographyRole.Caption, valueFirst: true));
        topBar.AddChild(Divider());
        // Cash is a known value, not a state: neutral primary text. The financial condition carries the semantic colour.
        var cash = Fact("cash", "CASH", PortalScreen.Cu(model.Finance.Cash), ColorRole.TextPrimary); cash.Name = "TopBarCash";
        topBar.AddChild(cash);
        topBar.AddChild(Divider());
        var (condition, tone) = PortalScreen.Condition(model.Finance.Condition);
        var state = Fact("chart", roomy ? "FINANCIAL CONDITION" : "FINANCES", condition, tone); state.Name = "TopBarCondition";
        topBar.AddChild(state);
        topBar.AddChild(Divider());
        var blocking = model.Decisions.Count(d => d.Urgency == TaskUrgency.Blocking);
        var attention = new CardButton(6, 2) { Name = "Attention", ThemeTypeVariation = "Entity", SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            SizeFlagsVertical = SizeFlags.ShrinkCenter, TooltipText = PortalScreen.PendingText(model.Decisions.Length) };
        var pending = new HBoxContainer(); pending.AddThemeConstantOverride("separation", 8);
        pending.AddChild(UiIcon.Create(ui, "decisions", 22, ColorRole.TextSecondary));
        var words = new VBoxContainer(); words.AddThemeConstantOverride("separation", 2);
        words.AddChild(Fixed(SemanticText.Create(ui, roomy ? "PENDING DECISIONS" : "DECISIONS", TypographyRole.Caption, ColorRole.TextSecondary)));
        // Badge tone follows urgency: critical only when something blocks, amber while matters are due, neutral when clear.
        var badge = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin, Name = "TopBarDecisions" };
        var badgeTone = blocking > 0 ? ColorRole.StateCritical : model.Decisions.IsEmpty ? ColorRole.SelectedSurface : ColorRole.StateWarning;
        var badgeBox = new StyleBoxFlat { BgColor = UiTheme.ToGodot(model.Decisions.IsEmpty ? ui.Tokens.Color(badgeTone) : ui.Tokens.Color(badgeTone).Mix(ui.Tokens.Color(ColorRole.SurfaceBase), .55)) };
        badgeBox.SetCornerRadiusAll(UiTokens.CornerRadius); badgeBox.ContentMarginLeft = badgeBox.ContentMarginRight = 7; badgeBox.ContentMarginTop = badgeBox.ContentMarginBottom = 0;
        if (!model.Decisions.IsEmpty) { badgeBox.BorderColor = UiTheme.ToGodot(ui.Tokens.Color(badgeTone)); badgeBox.SetBorderWidthAll(1); }
        badge.AddThemeStyleboxOverride("panel", badgeBox);
        var count = model.Decisions.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var countLabel = Fixed(SemanticText.Create(ui, blocking > 0 ? $"{PresentationText.WarningMarker} {count}" : count, TypographyRole.Strong)); countLabel.Name = "TopBarDecisionCount";
        badge.AddChild(countLabel);
        words.AddChild(badge); pending.AddChild(words);
        attention.Body.AddChild(CardButton.Passive(pending));
        foreach (var child in Descendants(pending).OfType<Control>()) child.MouseFilter = MouseFilterEnum.Ignore;
        attention.Pressed += () => Navigate(ShellSections.Portal);
        topBar.AddChild(attention);
        topBar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        topBar.AddChild(BarButton("Save", "save", () => Notify(session!.Save("campaign").Message, false)));
        topBar.AddChild(BarButton("Load", "load", () => { var r = session!.Load("campaign"); requestId = Composition.LastUiCommand(session); Refresh(); Notify(r.Message, !r.Accepted); }));
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
        topBar.AddChild(continueButton);
    }
    /// <summary>Top-bar fact: optional icon, uppercase caption, value in its semantic tone (reference layout).</summary>
    private HBoxContainer Fact(string? icon, string caption, string value, ColorRole tone, TypographyRole valueRole = TypographyRole.Strong, bool valueFirst = false)
    {
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter }; row.AddThemeConstantOverride("separation", 10);
        if (icon is not null) row.AddChild(UiIcon.Create(ui, icon, 22, ColorRole.TextMuted));
        var fact = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter }; fact.AddThemeConstantOverride("separation", 1);
        var head = Fixed(SemanticText.Create(ui, caption, valueFirst ? TypographyRole.Strong : TypographyRole.Caption, valueFirst ? ColorRole.TextPrimary : ColorRole.TextSecondary));
        var tail = Fixed(SemanticText.Create(ui, value, valueFirst ? valueRole : TypographyRole.Strong, tone));
        fact.AddChild(head); fact.AddChild(tail);
        row.AddChild(fact);
        return row;
    }
    private ColorRect Divider() => new()
    {
        Color = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.Divider)), CustomMinimumSize = new Vector2(1, 36),
        SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore
    };
    private CardButton BarButton(string text, string icon, Action action)
    {
        var button = new CardButton(12, 4) { Name = text, ThemeTypeVariation = "", SizeFlagsHorizontal = SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ShrinkCenter, MinimumHeight = 40 };
        var row = CardButton.Passive(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = SizeFlags.ExpandFill });
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(UiIcon.Create(ui, icon, 18));
        var label = SemanticText.Create(ui, text, TypographyRole.Label); label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(label);
        button.Body.AddChild(row); button.Body.SizeFlagsVertical = SizeFlags.ExpandFill;
        button.Pressed += action;
        return button;
    }
    private static Label Fixed(Label label)
    {
        label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; return label;
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
        Refresh(); Notify(response.Message, !response.Accepted);
        return response;
    }
    private void Notify(string text, bool warning)
    {
        message.Text = (warning ? PresentationText.WarningMarker : PresentationText.NeutralMarker) + " " + text; message.Visible = text.Length > 0;
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
        if (target == ShellSections.Squad && view.People.Any(p => p.Id == id)) squadSelection = id;
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

    /// <summary>Material commitments are confirmed in an overlay that keeps the shell visible behind it.</summary>
    private void Confirm(string title, string body, string action, Action commit)
    {
        ShowOverlay(out var panel);
        var stack = ui.Stack(); panel.AddChild(stack);
        stack.AddChild(SectionHeader.Create(ui, title));
        stack.AddChild(SemanticText.Create(ui, body));
        var buttons = ui.Flow(); stack.AddChild(buttons);
        var cancel = ui.Button("Cancel · Esc", CloseOverlay); cancel.Name = "ConfirmCancel"; buttons.AddChild(cancel);
        var confirm = ui.PrimaryButton(action, () => { CloseOverlay(); commit(); }); confirm.Name = "ConfirmAccept"; buttons.AddChild(confirm);
        cancel.GrabFocus();
    }
    private void ShowOverlay(out PanelContainer panel)
    {
        ClearChildren(overlay); overlay.Visible = true;
        overlay.AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Stop });
        ((ColorRect)overlay.GetChild(0)).SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer(); overlay.AddChild(center); center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panel = new PanelContainer { ThemeTypeVariation = ColorRole.SurfaceOverlay.ToString() };
        panel.CustomMinimumSize = new Vector2(ui.Tokens.Size(SizeRole.ReadingMeasure) * 0.75f, 0); center.AddChild(panel);
    }
    private void CloseOverlay() { if (overlay is null) return; ClearChildren(overlay); overlay.Visible = false; }

    /// <summary>Offers open the verified sponsor decision workspace over the shell and return to Commercial.</summary>
    private void OpenOffer(string offerId)
    {
        var sponsors = (ISponsorSession)session!;
        ClearChildren(overlay); overlay.Visible = true;
        var backdrop = new ColorRect { Color = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase)) };
        overlay.AddChild(backdrop); backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var workspace = new SponsorWorkspaceView { Name = "OfferWorkspace" };
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
