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
    private string contentPath = "", saveDirectory = "";
    private long requestId;
    private string section = ShellSections.Portal;
    private bool navCollapsed;
    private readonly HashSet<string> seenInbox = [];
    private VBoxContainer frame = null!, navList = null!;
    private Control content = null!, overlay = null!;
    private HBoxContainer topBar = null!;
    private Label message = null!;
    private Button continueButton = null!;
    private PanelContainer nav = null!;
    private readonly Dictionary<string, Button> navButtons = [];

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
        requestId = 0; seenInbox.Clear(); section = ShellSections.Portal;
        BuildFrame(); Refresh();
    }
    private void LoadSaved()
    {
        session = Composition.Create(contentPath, saveDirectory, 20261004);
        var result = session.Load("campaign");
        requestId = Composition.LastUiCommand(session); seenInbox.Clear(); section = ShellSections.Portal;
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
        var margin = Margin(); AddChild(margin); MoveChild(margin, 0);
        frame = ui.Stack(); margin.AddChild(frame);
        topBar = new HBoxContainer(); topBar.AddThemeConstantOverride("separation", ui.Tokens.Space(SpaceRole.SpaceGroup));
        frame.AddChild(ui.Panel(topBar, ColorRole.SurfaceRaised));
        message = SemanticText.Create(ui, "", TypographyRole.Label, ColorRole.TextSecondary); message.Visible = false; frame.AddChild(message);
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; frame.AddChild(body);
        navList = ui.Stack(); nav = ui.Panel(navList, ColorRole.SurfaceInset); nav.SizeFlagsHorizontal = SizeFlags.Fill; body.AddChild(nav);
        content = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true }; body.AddChild(content);
        frame.AddChild(ui.Watermark());
        BuildNav();
    }
    private void BuildNav()
    {
        ClearChildren(navList); navButtons.Clear();
        nav.CustomMinimumSize = new Vector2(navCollapsed ? ui.Tokens.ControlHeight * 1.6f : ui.Tokens.Size(SizeRole.InspectorWidth) * 0.55f, 0);
        var toggle = ui.Button(navCollapsed ? "»" : "« Collapse", () => { navCollapsed = !navCollapsed; BuildNav(); UpdateNav(); });
        toggle.Name = "NavToggle"; toggle.CustomMinimumSize = new Vector2(0, ui.Tokens.ControlHeight); toggle.TooltipText = navCollapsed ? "Expand navigation" : "";
        navList.AddChild(toggle);
        for (var i = 0; i < Sections.Length; i++)
        {
            var (id, glyph, label) = Sections[i];
            var button = new Button { ThemeTypeVariation = "Entity", Alignment = HorizontalAlignment.Left, FocusMode = FocusModeEnum.All,
                CustomMinimumSize = new Vector2(0, ui.Tokens.ControlHeight), TooltipText = $"{label} (Alt+{i + 1})", Name = "Nav_" + id };
            button.Pressed += () => Navigate(id);
            navButtons[id] = button; navList.AddChild(button);
        }
    }
    private void UpdateNav()
    {
        var unread = view.Inbox.Count(x => !seenInbox.Contains(x.Id));
        foreach (var (id, glyph, label) in Sections)
        {
            var button = navButtons[id];
            var text = id == ShellSections.Inbox && unread > 0 ? $"{label} · {unread}" : label;
            button.Text = navCollapsed ? glyph : $"{glyph}  {text}";
            button.ThemeTypeVariation = id == section ? "SelectedEntity" : "Entity";
        }
    }
    private void BuildTopBar()
    {
        ClearChildren(topBar);
        var brand = SemanticText.Create(ui, PresentationText.Monogram(view.Company), TypographyRole.Label, ColorRole.TextOnAccent);
        brand.HorizontalAlignment = HorizontalAlignment.Center; brand.AutowrapMode = TextServer.AutowrapMode.Off;
        var swatch = ui.Panel(brand, ColorRole.SurfaceInset); swatch.SizeFlagsHorizontal = SizeFlags.ShrinkBegin; swatch.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var accent = UiTheme.Box(ui.Tokens, ColorRole.SurfaceInset); accent.BgColor = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.AccentOrganization));
        swatch.AddThemeStyleboxOverride("panel", accent); topBar.AddChild(swatch);
        var identity = ui.Stack(); identity.SizeFlagsHorizontal = SizeFlags.ExpandFill; topBar.AddChild(identity);
        var company = SemanticText.Create(ui, view.Company, TypographyRole.CompanyIdentity); company.AutowrapMode = TextServer.AutowrapMode.Off;
        company.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; identity.AddChild(company);
        var date = SemanticText.Create(ui, $"Season {view.Season} · day {view.DayOfSeason} of {view.SeasonLength}", TypographyRole.Label, ColorRole.TextSecondary);
        date.AutowrapMode = TextServer.AutowrapMode.Off; identity.AddChild(date);
        // Top-bar facts never wrap: a shrinking column must not turn into one character per line.
        var money = ui.Stack(); money.SizeFlagsHorizontal = SizeFlags.ShrinkEnd; money.SizeFlagsVertical = SizeFlags.ShrinkCenter; topBar.AddChild(money);
        money.AddChild(Fixed(SemanticText.Create(ui, Money(view.Cash), TypographyRole.Data)));
        money.AddChild(Fixed(SemanticText.Create(ui, view.Status == "Stable" ? "Stable finances" : $"{PresentationText.WarningMarker} {view.Status}", TypographyRole.Annotation,
            view.Status == "Stable" ? ColorRole.TextSecondary : ColorRole.StateWarning)));
        var tasks = PortalTasks.Build(view);
        var blocking = tasks.Count(t => t.Urgency == TaskUrgency.Blocking);
        var attention = ui.Button(tasks.Length == 0 ? "No pending decisions" : $"{(blocking > 0 ? PresentationText.WarningMarker : PresentationText.NeutralMarker)} {tasks.Length} decision{(tasks.Length == 1 ? "" : "s")}", () => Navigate(ShellSections.Portal));
        attention.Name = "Attention"; attention.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        if (blocking > 0) Tone(attention, ColorRole.StateWarning);
        topBar.AddChild(attention);
        var save = ui.Button("Save", () => Notify(session!.Save("campaign").Message, false)); save.SizeFlagsVertical = SizeFlags.ShrinkCenter; save.Name = "Save"; topBar.AddChild(save);
        var load = ui.Button("Load", () => { var r = session!.Load("campaign"); requestId = Composition.LastUiCommand(session); Refresh(); Notify(r.Message, !r.Accepted); });
        load.SizeFlagsVertical = SizeFlags.ShrinkCenter; load.Name = "Load"; topBar.AddChild(load);
        var blocker = PortalTasks.Blocker(view);
        continueButton = ui.PrimaryButton(blocker is null ? "Continue ▸" : "Prepare match ▸", Continue);
        continueButton.Name = "Continue"; continueButton.SizeFlagsVertical = SizeFlags.ShrinkCenter; continueButton.TooltipText = "Ctrl+Enter";
        topBar.AddChild(continueButton);
    }
    private static Label Fixed(Label label)
    {
        label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsHorizontal = SizeFlags.ShrinkEnd; return label;
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
        ui.Tone(message, warning ? ColorRole.StateWarning : ColorRole.TextSecondary);
        UiMotion.Highlight(ui, message);
    }
    private void Refresh()
    {
        view = session!.Observe();
        BuildTopBar(); UpdateNav(); ShowSection();
    }
    private void Navigate(string id)
    {
        // Embedded workspaces (Company, offers) commit through the same session, so always re-observe.
        section = id; CloseOverlay(); view = session!.Observe(); BuildTopBar(); UpdateNav(); ShowSection();
        navButtons[id].GrabFocus();
    }
    private void ShowSection()
    {
        ClearChildren(content);
        var screen = section switch
        {
            ShellSections.Inbox => InboxScreen(), ShellSections.Squad => SquadScreen(), ShellSections.Competition => CompetitionScreen(),
            ShellSections.Commercial => CommercialScreen(), ShellSections.Finance => FinanceScreen(), ShellSections.Staff => StaffScreen(),
            ShellSections.Company => CompanyScreen(), _ => PortalScreen()
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
