using Godot;
using ManagementGame.Application;
using ManagementGame.OperatingMap;
using ManagementGame.SponsorWorkspace;
using ManagementGame.UiKit;

/// <summary>DEVELOPMENT / NONCANONICAL COS-05 experiment. Explicit scene entry only.
/// One disposable fixture session; presentation uses actor-safe observations only.</summary>
public partial class CompanyOperatingField : Control
{
    private readonly UiContext ui = new(new UiTokens(UiDensity.Compact));
    private readonly string sessionKey = Guid.NewGuid().ToString("N");
    private IGameSession session = null!;
    private ISponsorSession sponsors = null!;
    private SponsorSnapshot snapshot = null!;
    private Situation situation = null!;
    private MapNavigation navigation = null!;
    private SponsorTerms Offer => snapshot.Offers.Single(o => o.OfferId == offerId);
    private string offerId = "";
    private Control page = null!;
    private SponsorWorkspaceView? workspace;
    private Label context = null!;
    private Button entry = null!;
    private readonly Dictionary<string, Button> targets = [];
    private string disclosure = "";
    private bool rebuilding;
    private Rect2 companyBounds, ownedBounds;
    private float consequenceY;
    private bool Wide => Size.X >= 1600 && Size.Y >= 920;
    private Color Tone(ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));
    private static string? Arg(string key) => OS.GetCmdlineUserArgs().FirstOrDefault(x => x.StartsWith(key + "=", StringComparison.Ordinal))?[(key.Length + 1)..];

    public override async void _Ready()
    {
        try
        {
            GetWindow().ContentScaleSize = Vector2I.Zero;
            if (Arg("--field-viewport") is { } viewport)
            {
                var parts = viewport.Split('x');
                GetWindow().ContentScaleSize = new(int.Parse(parts[0]), int.Parse(parts[1]));
                GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
                GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
            }
            session = Composition.Create(Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../content/fixture.json")),
                Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "../../artifacts/company-operating-field/saves")), 20261004);
            sponsors = (ISponsorSession)session;
            Observe();
            // Offers are generated per seed: take the first actionable offer by stable ordinal ID, never by array position.
            offerId = snapshot.Offers.Where(o => o.CanAccept).Select(o => o.OfferId).Order(StringComparer.Ordinal).First();
            navigation = new(Projection());
            navigation.Select("matter:" + offerId, snapshot.Revision);
            Theme = ui.Theme;
            RenderingServer.SetDefaultClearColor(Tone(ColorRole.SurfaceBase));
            Build();
            Resized += Reflow;
            await Settle();
            if (Arg("--field-output") is { } output) await Capture(output);
            if (OS.GetCmdlineUserArgs().Contains("--field-verify")) await Verify();
            if (OS.GetCmdlineUserArgs().Contains("--field-verify") || Arg("--field-output") is not null)
            {
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GetTree().Quit();
            }
        }
        catch (Exception e) { GD.PrintErr("COMPANY_FIELD_FAIL " + e); GetTree().Quit(1); }
    }

    private void Observe()
    {
        snapshot = sponsors.ObserveSponsors(); situation = session.Observe();
        if (snapshot.Revision != situation.Revision || snapshot.Day != situation.Day || snapshot.CompanyName != situation.Company)
            throw new InvalidOperationException("Company observations must belong to the same session and revision.");
    }
    private MapSnapshot Projection()
    {
        var map = SponsorPresentation.Company(snapshot, sessionKey);
        return map with
        {
            Scopes = map.Scopes.Add(new(MapFixtures.Talent, snapshot.CompanyId, "Talent & Performance", ScopeKind.Function,
                "Company roster and coach serve the owned primary team.")),
            Links = map.Links.Add(new(MapFixtures.Talent, MapFixtures.Team, "Coach and roster serve"))
        };
    }
    private void Reflow()
    {
        if (rebuilding || page is null) return;
        var focus = targets.FirstOrDefault(x => x.Value.HasFocus()).Key;
        Build();
        if (focus is not null && targets.TryGetValue(focus, out var target)) target.GrabFocus();
    }
    private void Build()
    {
        rebuilding = true;
        if (page is not null) { RemoveChild(page); page.QueueFree(); }
        targets.Clear();
        page = new Control(); AddChild(page); page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        page.Visible = workspace is null;
        var w = Size.X; var h = Size.Y;
        companyBounds = new(24, 124, w - 48, Wide ? 450 : h - 218);
        ownedBounds = new(Wide ? 568 : 444, 208, Wide ? 668 : 446, 162);
        consequenceY = 378;
        Text("Player company", 24, 14, 400, 22, TypographyRole.Annotation, ColorRole.TextMuted);
        Text(snapshot.CompanyName, 24, 37, w - 680, 34, TypographyRole.CompanyIdentity);
        Text($"Day {snapshot.Day}   /   " + PresentationText.Time(snapshot.NextCheckpoint, "Next competition"),
            w - 602, 34, 578, 32, TypographyRole.SectionTitle);
        context = Text("", 24, 84, w - 340, 28, TypographyRole.Label, ColorRole.TextSecondary);
        Button("Ownership & functions", "structure", w - 282, 79, 258, 34, () => Show("structure"));
        if (disclosure.Length > 0) BuildDisclosure();
        else
        {
            BuildField();
            if (Wide) BuildEvidence(28, 588, w - 56);
        }
        Button("Affairs & timing", "timing", 24, h - 76, 200, 34, () => Show("timing"));
        var receipt = snapshot.Agreements.SelectMany(a => a.Payments).Where(p => p.Remaining > 0).OrderBy(p => p.DueDay).FirstOrDefault();
        Text(receipt is null ? "No outstanding signed receipt" : $"Next signed receipt · day {receipt.DueDay} · {SponsorPresentation.Money(receipt.Remaining)}",
            242, h - 75, w - 550, 34, TypographyRole.Label, ColorRole.TextSecondary);
        if (disclosure.Length > 0) Button("Return to operating field", "close", w - 282, h - 76, 258, 34, CloseDisclosure);
        else Button("Sponsor situation", "sponsor", w - 282, h - 76, 258, 34, () => Select("matter:" + offerId));
        Text("DEVELOPMENT / NONCANONICAL · COS-05 Direction A · isolated session · provisional theme", 24, h - 30, w - 48, 22,
            TypographyRole.Annotation, ColorRole.TextMuted);
        RefreshSelection(); QueueRedraw(); rebuilding = false;
    }
    private void BuildField()
    {
        var w = Size.X; var end = companyBounds.End.Y; var leftWidth = Wide ? 440 : 344;
        Text("Shared company capability", 48, 142, leftWidth, 22, TypographyRole.Annotation, ColorRole.TextMuted);
        ScopeButton(MapFixtures.Business, 48, 166, leftWidth, 34);
        Text("Sponsor commitment", 48, 221, leftWidth, 24, TypographyRole.Label, ColorRole.TextSecondary);
        Button(Offer.Name, "matter:" + offerId, 48, 250, leftWidth, 42, () => Select("matter:" + offerId));
        Text(Offer.Availability == "Signed" ? $"Signed · through day {Offer.EndDay}" : $"{Offer.Availability} · accept by day {Offer.Deadline}",
            48, 299, leftWidth, 26, TypographyRole.Label, ColorRole.StateWarning);
        Text($"{SponsorPresentation.Money(Offer.Payment)} each scheduled receipt", 48, 340, leftWidth, 28, TypographyRole.Data);
        Text($"+{Offer.Load} delivery load through day {Offer.EndDay}", 48, consequenceY, leftWidth, 32, TypographyRole.Label);
        // Contract lengths vary, so summarize the schedule to fit one fixed line; the workspace lists every date.
        var receipts = Offer.ScheduledPayments;
        Text(receipts.IsEmpty ? "No scheduled receipts" : $"{receipts.Length} weekly receipts · day {receipts[0].DueDay}–{receipts[^1].DueDay}",
            48, 436, leftWidth, 26, TypographyRole.Label, ColorRole.TextSecondary);
        Text("Signing pays no cash now.", 48, 462, leftWidth, 26, TypographyRole.Label, ColorRole.TextSecondary);

        var teamWidth = Wide ? 620 : 398;
        var x = ownedBounds.Position.X + 24; var width = w - x - 48;
        Text("Owned competitive operation", x, 142, teamWidth, 22, TypographyRole.Annotation, ColorRole.TextMuted);
        ScopeButton(MapFixtures.Portfolio, x, 166, teamWidth, 34);
        Text("Esports   /   Development Discipline", x, 223, teamWidth, 26, TypographyRole.Label, ColorRole.TextSecondary);
        ScopeButton(MapFixtures.Team, x, 265, teamWidth, 36);
        Text(situation.CommittedPlan is null ? "Preparation · no plan committed" : "Preparation · plan committed",
            x, 308, teamWidth, 28, TypographyRole.Body);
        Text(snapshot.NextCheckpoint is { } day ? $"Day {day} · vs {situation.Opponent}" : "No remaining competition",
            x, 342, teamWidth, 28, TypographyRole.Label, ColorRole.TextSecondary);

        var talentX = Wide ? w - 580 : w - 340;
        Text("Shared company capability", talentX, 142, Wide ? 508 : 268, 22, TypographyRole.Annotation, ColorRole.TextMuted);
        ScopeButton(MapFixtures.Talent, talentX, 166, Wide ? 508 : 268, 34);
        Text("Coach & roster serve the team", talentX, 265, Wide ? 508 : 268, 36, TypographyRole.Label, ColorRole.TextSecondary);
        Text($"Head Coach · {situation.Coach}", talentX, 308, Wide ? 508 : 268, 28, TypographyRole.Label);
        Text($"{situation.People.Length} players · {situation.Delegation} authority", talentX, 342, Wide ? 508 : 268, 28, TypographyRole.Label, ColorRole.TextSecondary);

        var after = Offer.CanAccept ? Offer.LoadIfAccepted : null;
        Text(after is { } load ? $"Shared capacity   {snapshot.Load} now  →  {load} if accepted  /  {snapshot.Capacity} capacity"
            : $"Shared capacity   {snapshot.Load} current load  /  {snapshot.Capacity} capacity", x, consequenceY, width, 32, TypographyRole.Data,
            (after ?? snapshot.Load) > snapshot.Capacity ? ColorRole.StateWarning : ColorRole.TextPrimary);
        Text((after ?? snapshot.Load) > snapshot.Capacity
                ? (after is not null ? "If accepted: " : "Current load: ") + "over capacity; future preparation converts more slowly. Completed work is retained."
                : "Sponsor delivery shares company capacity with future preparation.",
            x, consequenceY + 39, width, 45, TypographyRole.Label, ColorRole.TextSecondary);
        Text("Unknown · future wins and total bonuses", x, consequenceY + 89, width, 26, TypographyRole.Label, ColorRole.InformationUnknown);
        Text(Offer.Availability == "Signed" ? "Agreement signed. Review the refreshed company obligation."
            : "The choice: scheduled income in return for ongoing delivery load.", Wide ? x : 48, end - 67, Wide ? width : w - 440, 42, TypographyRole.Body);
        entry = Button(Offer.Availability == "Signed" ? "Open signed agreement" : "Open Sponsor decision →", "entry", Wide ? 48 : w - 366, end - 67, Wide ? leftWidth : 318, 42, OpenWorkspace);
    }

    private void BuildEvidence(float x, float y, float width)
    {
        var track = DayTrack.Create(ui, SponsorPresentation.Track(snapshot, Offer));
        var trackSize = new Vector2(width * .66f - 44, 0);
        track.MinimumSizeChanged += () => track.SetDeferred(Control.PropertyName.Size, trackSize);
        page.AddChild(track); track.Position = new(x + 20, y); track.Size = trackSize;
        track.SetDeferred(Control.PropertyName.Size, trackSize);
        var right = x + width * .69f; var rw = width * .31f - 24;
        Text("What changes financially?", right, y, rw, 30, TypographyRole.SectionTitle);
        Text($"Cash now · {SponsorPresentation.Money(snapshot.Cash)}", right, y + 43, rw, 28, TypographyRole.Data);
        Text($"Signed 7-day forecast · {SponsorPresentation.Money(snapshot.CommittedForecast)}", right, y + 87, rw, 48, TypographyRole.Data, ColorRole.InformationEstimated);
        if (Offer.CanAccept && Offer.ForecastIfAccepted is { } forecast)
            Text($"If accepted · {SponsorPresentation.Money(forecast)}", right, y + 144, rw, 28, TypographyRole.Data, ColorRole.InformationEstimated);
        Text("Estimated · same seven-day window. Excludes unearned wins; only the selected offer is added to the conditional forecast.",
            right, y + 192, rw, 70, TypographyRole.Label, ColorRole.TextSecondary);
        Text($"Known terms · {SponsorPresentation.Money(Offer.WinBonus)} per future win while active. Due the following day; a win is not guaranteed.",
            right, y + 278, rw, 70, TypographyRole.Label, ColorRole.TextSecondary);
    }
    private void BuildDisclosure()
    {
        var stack = ui.Stack(); var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        page.AddChild(scroll); scroll.Position = new(48, 144); scroll.Size = new(Size.X - 96, Size.Y - 250); scroll.AddChild(stack);
        stack.AddChild(SectionHeader.Create(ui, disclosure == "structure" ? "Company ownership & shared functions" : "Affairs & timing",
            "Inspection only · selection does not advance time or commit a decision."));
        if (disclosure == "structure")
        {
            foreach (var scope in navigation.Snapshot.Scopes)
            {
                var entity = new EntityLabel();
                entity.Bind(ui, new(scope.Id, string.Join(" → ", navigation.Snapshot.Path(scope.Id).Select(p => p.Name)),
                    scope.Kind == ScopeKind.Function ? EntityKind.Department : EntityKind.Company, scope.Kind.ToString(), snapshot.Revision),
                    intent => { navigation.Select(intent.TargetId, intent.Revision); CloseDisclosure(); });
                stack.AddChild(entity);
            }
        }
        else
        {
            stack.AddChild(TimeMarker.Create(ui, snapshot.NextCheckpoint, "Next competition"));
            stack.AddChild(TimeMarker.Create(ui, Offer.Deadline, "Selected offer deadline"));
            stack.AddChild(ui.Button(Offer.Name + " · select Sponsor situation", () => { navigation.Select("matter:" + offerId, snapshot.Revision); CloseDisclosure(); }));
            stack.AddChild(DayTrack.Create(ui, SponsorPresentation.Track(snapshot, Offer)));
            foreach (var agreement in snapshot.Agreements)
                foreach (var receipt in agreement.Payments.Where(p => p.Remaining > 0))
                    stack.AddChild(SemanticText.Create(ui, $"{agreement.Name} · day {receipt.DueDay} · {SponsorPresentation.Money(receipt.Remaining)} receivable", TypographyRole.Label));
            stack.AddChild(SemanticText.Create(ui, "Scheduled is not received. This prototype leaves time progression in the existing management client.", TypographyRole.Label));
        }
    }
    private void Select(string id) { navigation.Select(id, snapshot.Revision); RefreshSelection(); }
    private void RefreshSelection()
    {
        context.Text = "Selected · " + string.Join(" / ", navigation.Snapshot.Path(navigation.ScopeId).Skip(1).Select(s => s.Name).DefaultIfEmpty("Company"))
            + (navigation.Situation is { } matter ? " / " + matter.Name : "");
        foreach (var (key, target) in targets)
            target.ThemeTypeVariation = key == (navigation.SituationId ?? navigation.ScopeId) ? "SelectedEntity" : "";
    }
    private void Show(string kind) { disclosure = kind; Build(); targets["close"].GrabFocus(); }
    private void CloseDisclosure()
    {
        var origin = disclosure; disclosure = ""; Build();
        targets[origin == "structure" ? "structure" : "timing"].GrabFocus();
    }
    private void OpenWorkspace()
    {
        if (workspace is not null) return;
        Select("matter:" + offerId);
        if (!navigation.Enter(offerId, snapshot.Revision, "entry", 0)) return;
        page.Hide(); QueueRedraw();
        workspace = new SponsorWorkspaceView();
        workspace.Configure(new(sponsors, snapshot, offerId, navigation.Return!), () =>
        {
            var old = workspace!; workspace = null; RemoveChild(old); old.QueueFree();
            navigation.Back(); Observe(); navigation.Replace(Projection()); Build(); entry.GrabFocus();
        });
        AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        workspace.BackButton.GrabFocus();
    }
    private Label Text(string value, float x, float y, float width, float height, TypographyRole role, ColorRole tone = ColorRole.TextPrimary)
    {
        var label = SemanticText.Create(ui, value, role, tone); page.AddChild(label);
        var requested = new Vector2(width, height);
        // Native wrapping first learns the column width, then recomputes its minimum height.
        // Reapply the bounded rectangle after that deferred measurement (no clipping/ellipsis).
        label.MinimumSizeChanged += () => label.SetDeferred(Control.PropertyName.Size, requested);
        label.Position = new(x, y); label.Size = requested;
        label.SetDeferred(Control.PropertyName.Size, requested);
        label.SetMeta("field_text", true); return label;
    }
    private Button Button(string text, string key, float x, float y, float width, float height, Action action)
    {
        var button = ui.Button(text, action); page.AddChild(button); button.Position = new(x, y); button.Size = new(width, height);
        targets[key] = button; return button;
    }
    private void ScopeButton(string id, float x, float y, float width, float height)
    {
        var button = Button(navigation.Snapshot.Scope(id).Name, id, x, y, width, height, () => Select(id));
        button.Flat = true; button.Alignment = HorizontalAlignment.Left;
    }
    public override void _Draw()
    {
        if (page is null || workspace is not null) return;
        DrawLine(new(24, 73), new(Size.X - 24, 73), Tone(ColorRole.Border));
        DrawRect(companyBounds, Tone(ColorRole.SurfaceInset));
        DrawLine(companyBounds.Position, new(companyBounds.End.X, companyBounds.Position.Y), Tone(ColorRole.Border));
        if (disclosure.Length > 0) return;
        DrawRect(ownedBounds, Tone(ColorRole.SurfaceBase));
        DrawLine(ownedBounds.Position, new(ownedBounds.Position.X, ownedBounds.End.Y), Tone(ColorRole.Border));
        // Only semantic geometry: ownership enclosure and the selected delivery-to-capacity link.
        var from = new Vector2(Wide ? 500 : 400, consequenceY + 16);
        var to = new Vector2(ownedBounds.Position.X + 12, consequenceY + 16);
        DrawLine(from, to, Tone(ColorRole.StateWarning), 2);
        DrawLine(to, to + new Vector2(-7, -5), Tone(ColorRole.StateWarning), 2);
        DrawLine(to, to + new Vector2(-7, 5), Tone(ColorRole.StateWarning), 2);
        if (!Wide) DrawLine(new(48, companyBounds.End.Y - 83), new(Size.X - 48, companyBounds.End.Y - 83), Tone(ColorRole.Border));
    }
    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (workspace is null && disclosure.Length > 0 && e is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        { GetViewport().SetInputAsHandled(); CloseDisclosure(); }
    }
}
