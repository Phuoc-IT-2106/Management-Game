using Godot;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ManagementGame.UiKit;

/// <summary>Internal scene only. Never constructs a gameplay session or loads campaign content.</summary>
public partial class UiLab : Control
{
    private UiContext ui = null!;
    private Control layout = null!;
    private VBoxContainer specimens = null!;
    private ScrollContainer scroll = null!;
    private Label feedback = null!;
    private EntityRow? focusRow;
    private Button? nextFocus;
    private string page = "identity", brand = "neutral";
    private UiDensity density = UiDensity.Default;
    private double textScale = 1;
    private readonly List<string> checks = [];
    private static readonly string[] Pages = ["identity", "interaction", "documents", "branding"];
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];
    private bool IsCapture => Arg("--lab-output") is not null;

    public override async void _Ready()
    {
        try
        {
            GetWindow().ContentScaleSize = Vector2I.Zero; // inspect real viewport reflow, not stretch a fixed design canvas
            if (Arg("--lab-viewport") is { } requested)
            {
                // A per-case render viewport avoids desktop work-area clamping (e.g. 1080 -> 1050).
                // Each case lays out at its own dimensions; there is no shared fixed design canvas.
                var parts = requested.Split('x');
                GetWindow().ContentScaleSize = new Vector2I(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
                GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
                GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
            }
            page = Arg("--lab-page") ?? page; brand = Arg("--lab-brand") ?? brand;
            if (!Pages.Contains(page)) throw new ArgumentException("Unknown lab page.");
            if (Arg("--lab-density") is { } d) density = Enum.Parse<UiDensity>(d, true);
            if (Arg("--lab-text-scale") is { } s) textScale = double.Parse(s, CultureInfo.InvariantCulture);
            Build();
            await Settle();
            FocusFirst();
            if (OS.GetCmdlineUserArgs().Contains("--lab-verify")) await Verify();
            if (IsCapture)
            {
                // Explicit native focus; stationary pointer outside specimens avoids accidental hover differences.
                if (focusRow is not null) focusRow.Identity.GrabFocus();
                else layout.FindNextValidFocus()?.GrabFocus();
                Input.WarpMouse(Vector2.Zero);
                await Settle();
                var scrollFraction = double.Parse(Arg("--lab-scroll") ?? "0", CultureInfo.InvariantCulture);
                if (scrollFraction < 0 || scrollFraction > 1) throw new ArgumentOutOfRangeException("lab-scroll");
                scroll.ScrollVertical = (int)Math.Round((scroll.GetVScrollBar().MaxValue - scroll.GetVScrollBar().Page) * scrollFraction);
                await Settle();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Capture();
                CollectRetiredSpecimens();
                GetTree().Quit(0);
            }
            else if (OS.GetCmdlineUserArgs().Contains("--lab-verify")) { CollectRetiredSpecimens(); GetTree().Quit(0); }
        }
        catch (Exception e) { GD.PrintErr("UI_KIT_FAIL " + e); GetTree().Quit(1); }
    }

    private void Build()
    {
        if (layout is not null) { RemoveChild(layout); layout.Free(); }
        focusRow = null; nextFocus = null;
        ui = new UiContext(new UiTokens(density, textScale, reducedMotion: true)); Theme = ui.Theme;
        RenderingServer.SetDefaultClearColor(UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceBase)));
        var margin = new MarginContainer(); layout = margin; AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, ui.Tokens.Space(SpaceRole.SpaceWorkspace));
        var shell = ui.Stack(); margin.AddChild(shell);
        shell.AddChild(SemanticText.Create(ui, "Internal UI Lab", TypographyRole.WorkspaceTitle));
        shell.AddChild(SemanticText.Create(ui, UiTokens.Notice + " · " + PresentationText.FixtureNotice, TypographyRole.Annotation, ColorRole.StateWarning));
        var pages = ui.Flow(); shell.AddChild(pages);
        foreach (var key in Pages) pages.AddChild(ui.Button((page == key ? "[Selected] " : "") + key, () => { page = key; Build(); FocusFirst(); }));
        var options = ui.Flow(); shell.AddChild(options);
        options.AddChild(ui.Button("Brand: " + brand, () => { brand = LabFixtures.BrandKeys[(LabFixtures.BrandKeys.IndexOf(brand) + 1) % LabFixtures.BrandKeys.Length]; Build(); FocusFirst(); }));
        options.AddChild(ui.Button("Density: " + density, () => { density = (UiDensity)(((int)density + 1) % Enum.GetValues<UiDensity>().Length); Build(); FocusFirst(); }));
        options.AddChild(ui.Button("Text: " + textScale.ToString("0.##", CultureInfo.InvariantCulture) + "x", () => { textScale = textScale == 1 ? 1.25 : textScale == 1.25 ? 1.5 : 1; Build(); FocusFirst(); }));
        var help = SemanticText.Create(ui, "Tab / Shift+Tab · Enter / Space · no motion", TypographyRole.Annotation, ColorRole.TextMuted);
        help.CustomMinimumSize = new Vector2(UiTokens.MinimumColumnWidth, 0); options.AddChild(help);
        scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true, FocusMode = FocusModeEnum.All, DrawFocusBorder = true };
        scroll.GuiInput += ScrollKeys;
        shell.AddChild(scroll); specimens = ui.Stack(); scroll.AddChild(specimens);
        switch (page) { case "identity": IdentityPage(); break; case "interaction": InteractionPage(); break; case "documents": DocumentsPage(); break; case "branding": BrandingPage(); break; }
        feedback = SemanticText.Create(ui, "Presentation-only intents · no gameplay commands", TypographyRole.Annotation, ColorRole.TextSecondary); shell.AddChild(feedback);
    }
    // Verification replaces whole finite specimen pages. Finalize retired resource wrappers
    // while the native engine is still alive, not during process teardown.
    private static void CollectRetiredSpecimens() { GC.Collect(); GC.WaitForPendingFinalizers(); }
    private void FocusFirst() => layout.FindNextValidFocus()?.GrabFocus();
    private void ScrollKeys(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true } key) return;
        var step = Math.Max(ui.Tokens.ControlHeight, (int)scroll.Size.Y - ui.Tokens.ControlHeight);
        switch (key.Keycode)
        {
            case Key.Pagedown: scroll.ScrollVertical += step; break;
            case Key.Pageup: scroll.ScrollVertical -= step; break;
            case Key.Home: scroll.ScrollVertical = 0; break;
            case Key.End: scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue; break;
            default: return;
        }
        scroll.AcceptEvent();
    }
    private void Report(UiIntent intent) => feedback.Text = $"Intent: {intent.Kind} · target {intent.TargetId} · revision {intent.Revision}";
    private void Add(Control child) => specimens.AddChild(child);
    private EntityView CompanyRef(OrganizationIdentityView identity) => new(identity.Id, identity.ShortName, EntityKind.Company, "Fixture context", identity.Revision);

    private void IdentityPage()
    {
        var identity = LabFixtures.Organization(brand, longName: true);
        Add(NavigationContext.Create(ui, identity, [CompanyRef(identity), new("scope:fixture", "UI Kit", EntityKind.Department, "Internal specimens", 7)], Report));
        Add(SectionHeader.Create(ui, "Values and evidence", "SemanticText · ResourceValue · TimeMarker · ConfidenceIndicator"));
        Add(ResourceValue.Create(ui, new("Observed amount", 1234567890.12m, "TEST", InformationState.Known, "fixed specimen")));
        Add(ResourceValue.Create(ui, new("Committed forecast", -9876543.21m, "TEST", InformationState.Estimated, "next review")));
        Add(ResourceValue.Create(ui, new("Unobserved value", null, "TEST", InformationState.Unknown, "no evidence")));
        Add(TimeMarker.Create(ui, 12, "Next review"));
        Add(ConfidenceIndicator.Create(ui, InformationState.Estimated, "Moderate confidence · fixed source revision 7"));
        Add(ConfidenceIndicator.Create(ui, InformationState.Unknown, "No observation supplied"));
        Add(SectionHeader.Create(ui, "System status", "Status is independent of organization color and information quality."));
        var status = ui.Flow(); foreach (var value in Enum.GetValues<DomainStatus>()) status.AddChild(StatusBadge.Create(ui, value)); Add(status);
        Add(ValidationMessage.Create(ui, "Rejected: observation changed; review the current terms."));
        Add(SectionHeader.Create(ui, "Typography and semantic markers", "Text/symbol baseline; no portrait or image dependency."));
        foreach (var role in Enum.GetValues<TypographyRole>()) Add(SemanticText.Create(ui, role + " · TEST_COMPANY · tiếng Việt 0123456789", role));
        var icons = ui.Flow(); foreach (var kind in Enum.GetNames<EntityKind>().Append("UnrecognizedKey")) icons.AddChild(IconPresentation.Create(ui, kind)); Add(icons);
    }
    private void InteractionPage()
    {
        Add(SectionHeader.Create(ui, "Entity identity and interaction states", "Real native hover, keyboard focus and press; selection and uncertainty have explicit labels."));
        focusRow = new EntityRow(); focusRow.Bind(ui, LabFixtures.Person, Report); focusRow.Identity.SetSelected(true); Add(focusRow);
        nextFocus = ui.Button("Next focus target", () => feedback.Text = "Keyboard activation received; no gameplay action."); Add(nextFocus);
        var states = ui.Flow();
        var pressed = ui.Button("Pressed / toggled specimen", () => { }); pressed.ToggleMode = true; pressed.SetPressedNoSignal(true); states.AddChild(pressed);
        states.AddChild(ui.Button("Default / hover-capable", () => feedback.Text = "Activation acknowledged."));
        var disabled = ui.Button("Disabled · no current authority", () => { }); disabled.Disabled = true; states.AddChild(disabled); Add(states);
        foreach (var availability in new[] { Availability.Loading, Availability.Unavailable, Availability.Error, Availability.Empty })
        {
            var row = new EntityRow(); row.Bind(ui, LabFixtures.Person with { Id = "specimen:" + availability, Name = availability + " entity", Availability = availability,
                Reason = "Explicit state; no actionable record", Information = InformationState.Unknown }, Report); Add(row);
        }
        Add(AlertItem.Create(ui, LabFixtures.Alert, Report)); Add(TimelineEvent.Create(ui, LabFixtures.Event, Report));
    }
    private void DocumentsPage()
    {
        Add(DocumentView.Create(ui, LabFixtures.Document)); Add(ComparisonView.Create(ui, LabFixtures.Comparison));
        Add(SectionHeader.Create(ui, "CommitmentReview and action result", "A presentation intent never commits gameplay."));
        foreach (var phase in Enum.GetValues<ActionPhase>())
        {
            var control = new CommitmentReview(); var data = LabFixtures.Commitment(phase);
            control.Bind(ui, data, intent => { Report(intent); control.Bind(ui, data with { Phase = ActionPhase.Rejected, Result = "Fixture rejection: review a fresh revision before retrying." }, null); });
            Add(ui.Panel(control));
        }
        Add(DocumentView.Create(ui, LabFixtures.Document with { Availability = Availability.Loading }));
        Add(ComparisonView.Create(ui, LabFixtures.Comparison with { Lines = [] }));
    }
    private void BrandingPage()
    {
        Add(SectionHeader.Create(ui, "Brand safety specimens", "Organization accents never replace warning, error or focus roles. Raw colors remain unchanged."));
        foreach (var key in LabFixtures.BrandKeys)
        {
            var org = LabFixtures.Organization(key); var resolved = BrandResolver.Resolve(org, ui.Tokens);
            Add(NavigationContext.Create(ui, org, [], null));
            Add(SemanticText.Create(ui, $"{key} · raw {org.PrimaryColor?.ToHex() ?? "absent"} / {org.SecondaryColor?.ToHex() ?? "absent"} · resolved {resolved.AccentOrganization.ToHex()} / {resolved.AccentOrganizationSecondary.ToHex()} · adjusted {resolved.PrimaryAdjusted}/{resolved.SecondaryAdjusted}", TypographyRole.Annotation));
            Add(ManagementGame.UiKit.StatusIndicator.Create(ui, DomainStatus.Warning, "System warning remains independently labelled"));
        }
    }
    private async Task Settle() { for (var i = 0; i < UiTokens.CaptureSettleFrames; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); checks.Add(label); }

    private async Task Verify()
    {
        var capturePage = page;
        page = "interaction"; Build(); await Settle();
        var row = focusRow!; var initialNodes = CountNodes(row); var staleCalls = 0; var currentCalls = 0; UiIntent? last = null;
        row.Bind(ui, LabFixtures.Person, _ => staleCalls++); row.Identity.SetSelected(true); row.Identity.TooltipText = "obsolete";
        for (var i = 0; i < 100; i++) row.Bind(ui, LabFixtures.Person with { Id = "person:fixture:002", Name = "RENAMED_PERSON", Revision = 8 + i }, value => { currentCalls++; last = value; });
        row.Identity.EmitSignal(BaseButton.SignalName.Pressed);
        Check(staleCalls == 0 && currentCalls == 1 && last?.TargetId == "person:fixture:002" && last.Revision == 107, "rebind emits current stable ID/revision once");
        Check(!row.Identity.Binding.Selected && !row.Identity.ButtonPressed && row.Identity.TooltipText == "", "rebind clears selection, pressed and tooltip");
        Check(CountNodes(row) == initialNodes, "100 rebinds retain bounded node count");
        row.Bind(ui, LabFixtures.Person with { Availability = Availability.Unavailable }, _ => currentCalls++);
        row.Identity.EmitSignal(BaseButton.SignalName.Pressed);
        Check(currentCalls == 1 && row.Identity.Disabled, "unavailable rejects activation even on signal path");
        row.Bind(ui, LabFixtures.Person, Report); row.Identity.SetSelected(true); row.Identity.GrabFocus();
        Check(row.Identity.HasFocus() && row.Identity.Binding.Selected && row.Identity.Binding.Current?.Information == InformationState.Estimated, "selected + focused + estimated remain independent");
        Check(row.Identity.GetThemeStylebox("focus") is StyleBoxFlat focus && focus.BorderWidthLeft >= UiTokens.FocusWidth, "native visible focus style resolves");
        Check(ui.Theme.GetFontSize("font_size", "Body") == ui.Tokens.FontSize(TypographyRole.Body), "typography theme variation resolves token size");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, Pressed = false });
        Check(nextFocus!.HasFocus(), "synthetic Tab traverses to next native control");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, ShiftPressed = true, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Tab, ShiftPressed = true, Pressed = false });
        Check(row.Identity.HasFocus(), "synthetic Shift+Tab restores entity focus");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, Pressed = false });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(feedback.Text.Contains(LabFixtures.Person.Id, StringComparison.Ordinal), "synthetic Enter activates stable entity intent");
        var temporary = new EntityLabel(); temporary.Bind(ui, LabFixtures.Person, _ => staleCalls++); specimens.AddChild(temporary);
        specimens.RemoveChild(temporary); Check(!temporary.Binding.CanActivate, "tree exit releases callback binding"); temporary.Free();
        var commitment = new CommitmentReview(); specimens.AddChild(commitment); var commits = 0;
        commitment.Bind(ui, LabFixtures.Commitment(), _ => commits++);
        commitment.ActionButton.EmitSignal(BaseButton.SignalName.Pressed); commitment.ActionButton.EmitSignal(BaseButton.SignalName.Pressed);
        Check(commits == 1 && commitment.ActionButton.Disabled, "pending commitment cannot emit a duplicate intent");
        commitment.Bind(ui, LabFixtures.Commitment(ActionPhase.Rejected), _ => commits++);
        commitment.ActionButton.EmitSignal(BaseButton.SignalName.Pressed); Check(commits == 1, "rejected commitment requires explicit fresh review");
        try { commitment.Bind(ui, LabFixtures.Commitment() with { Revision = 6 }, _ => commits++); Check(false, "older commitment must be rejected"); }
        catch (ArgumentException) { Check(commitment.ActionButton.Disabled && commitment.ActionButton.Binding.Current?.Revision == 7, "stale commitment preserves rejected state"); }
        specimens.RemoveChild(commitment); commitment.Free();
        foreach (var specimenPage in Pages)
        {
            page = specimenPage; Build(); await Settle(); VerifyLayout();
            Check(true, "container layout within viewport: " + page);
        }
        page = "identity"; Build(); await Settle(); scroll.GrabFocus();
        Check(scroll.HasFocus() && scroll.GetThemeStylebox("focus") is StyleBoxFlat readingFocus && readingFocus.BorderWidthLeft >= UiTokens.FocusWidth, "static reading region has native focus outline");
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.End, Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.End, Pressed = false });
        Check(scroll.ScrollVertical > 0 || specimens.Size.Y <= scroll.Size.Y, "keyboard End reaches static specimen content");
        page = capturePage; Build(); await Settle();
        if (focusRow is not null) focusRow.Identity.GrabFocus();
        GD.Print($"UI_KIT_GODOT_PASS {checks.Count}");
    }
    private void VerifyLayout()
    {
        var viewport = GetViewportRect();
        foreach (var node in Descendants(layout).OfType<Control>().Where(c => c.IsVisibleInTree() && c is Label or Button or PanelContainer))
        {
            var rect = node.GetGlobalRect();
            if (rect.Position.X < -UiTokens.LayoutTolerance || rect.End.X > viewport.Size.X + UiTokens.LayoutTolerance)
                throw new InvalidOperationException($"Horizontal overflow: {node.GetPath()} {rect} viewport={viewport.Size}");
        }
        Check(scroll.Size.Y > ui.Tokens.ControlHeight, "scrollable specimen region remains accessible: " + page);
    }
    private static IEnumerable<Node> Descendants(Node node) { foreach (var child in node.GetChildren()) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    private static int CountNodes(Node node) => 1 + Descendants(node).Count();
    private void Capture()
    {
        var output = Arg("--lab-output")!; System.IO.Directory.CreateDirectory(output);
        using var texture = GetViewport().GetTexture().GetImage();
        var key = $"{page}-{brand}-{density}-{textScale.ToString("0.##", CultureInfo.InvariantCulture)}-{texture.GetWidth()}x{texture.GetHeight()}-scroll{Arg("--lab-scroll") ?? "0"}";
        var file = System.IO.Path.Combine(output, key + ".png");
        if (texture.SavePng(file) != Error.Ok) throw new IOException("Screenshot write failed.");
        var fixtureJson = JsonSerializer.Serialize(LabFixtures.Organization(brand, page == "identity"));
        var manifest = new
        {
            Status = "GOLDEN CANDIDATE — NOT APPROVED", Commit = Arg("--lab-commit") ?? "unrecorded",
            SourceDigest = Arg("--lab-source-digest") ?? "unrecorded", Dirty = Arg("--lab-dirty") ?? "unrecorded",
            Viewport = new { Width = texture.GetWidth(), Height = texture.GetHeight() }, Page = page,
            NativeWindow = new { Width = GetWindow().Size.X, Height = GetWindow().Size.Y },
            ThemeVersion = UiTokens.Version, TokenVersion = UiTokens.Version, FixtureVersion = LabFixtures.Version,
            FixtureId = "DEV_ORG_001", FixtureDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixtureJson))).ToLowerInvariant(),
            BrandVariant = brand, Density = density.ToString(), TextScale = textScale, ReducedMotion = true,
            ComponentState = page == "interaction" ? "selected + focused + estimated; warning; pressed; disabled; loading; unavailable; error; empty" : "fixed specimens; initial native focus",
            CaptureTrigger = "explicit lab-output; settled process frames and frame_post_draw", LayoutSettleFrames = UiTokens.CaptureSettleFrames,
            ScrollFraction = Arg("--lab-scroll") ?? "0", ScrollPixels = scroll.ScrollVertical,
            Engine = Engine.GetVersionInfo()["string"].AsString(), Runtime = System.Environment.Version.ToString(), OS = OS.GetName(),
            Renderer = RenderingServer.GetCurrentRenderingMethod(), FontStrategy = string.Join(", ", UiTokens.UiFontNames),
            ResolvedUiFont = ui.Theme.DefaultFont.GetFontName(), PhysicalInputVerified = false, DpiGatePassed = false,
            Checks = checks, ControlCount = Descendants(this).OfType<Control>().Count(),
            Image = System.IO.Path.GetFileName(file), ImageSha256 = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(file))).ToLowerInvariant()
        };
        System.IO.File.WriteAllText(System.IO.Path.Combine(output, key + ".json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("UI_KIT_CAPTURE " + key);
    }
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape }) { GetViewport().SetInputAsHandled(); GetTree().Quit(); }
    }
}
