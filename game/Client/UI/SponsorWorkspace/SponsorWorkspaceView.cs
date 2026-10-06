using Godot;
using ManagementGame.UiKit;
using ManagementGame.Application;
using System.Text.Json;

namespace ManagementGame.SponsorWorkspace;

public partial class SponsorWorkspaceView : Control
{
    private readonly UiContext ui = new(new UiTokens(UiDensity.Compact));
    public SponsorPresenter Presenter { get; private set; } = null!;
    private Action? returned;
    private Control? layout;
    private VBoxContainer shell = null!, reading = null!, terms = null!, support = null!, evidence = null!;
    private HBoxContainer columns = null!;
    private Control filler = null!;
    private bool fitQueued;
    private HBoxContainer actions = null!;
    private Label heading = null!, time = null!, feedback = null!;
    private string? termsKey, evidenceKey;
    private PanelContainer? document;
    public Button ReviewButton { get; private set; } = null!;
    public Button BackButton { get; private set; } = null!;
    public CommitmentReview? Commitment { get; private set; }
    public ScrollContainer TermsScroll { get; private set; } = null!;
    public ScrollContainer EvidenceScroll { get; private set; } = null!;
    public Dictionary<string, List<double>> Timings { get; } = [];
    /// <summary>Unused height inside the reading region; positive means dead space between content and actions.</summary>
    public float ReadingSlack => columns.Size.Y - ReadingContentHeight;
    private float ReadingContentHeight => Math.Max(reading.GetCombinedMinimumSize().Y, evidence.GetCombinedMinimumSize().Y);
    public void Configure(SponsorPresenter presenter, Action back) { Presenter = presenter; returned = back; }
    public override void _Ready() => Render();
    public override void _ExitTree() => returned = null;
    public void Render()
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var section = System.Diagnostics.Stopwatch.GetTimestamp();
        Theme = ui.Theme; Commitment = null;
        var s = Presenter.Snapshot; var o = Presenter.Offer;
        if (layout is null)
        {
            var margin = new MarginContainer(); layout = margin; AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, ui.Tokens.Space(SpaceRole.SpaceWorkspace));
            shell = ui.Stack(); margin.AddChild(shell);
            heading = SemanticText.Create(ui, s.CompanyName, TypographyRole.CompanyIdentity);
            heading.AutowrapMode = TextServer.AutowrapMode.Off; heading.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            heading.TooltipText = s.CompanyName; shell.AddChild(heading);
            shell.AddChild(SemanticText.Create(ui, "Business & Finance  /  Sponsorship  /  Commitment review", TypographyRole.Label));
            time = SemanticText.Create(ui, "", TypographyRole.Annotation, ColorRole.TextSecondary); shell.AddChild(time);
            // Reading region sizes to its content up to the available height, so the decision
            // follows the evidence directly; spare height collects below the actions instead.
            columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.Fill }; shell.AddChild(columns);
            TermsScroll = ReadingScroll(); EvidenceScroll = ReadingScroll(); columns.AddChild(TermsScroll); columns.AddChild(EvidenceScroll);
            reading = ui.Stack(); terms = ui.Stack(); support = ui.Stack(); evidence = ui.Stack();
            reading.AddChild(terms); reading.AddChild(support); TermsScroll.AddChild(reading); EvidenceScroll.AddChild(evidence);
            feedback = ValidationMessage.Create(ui, "", false); shell.AddChild(feedback);
            actions = new HBoxContainer(); shell.AddChild(actions);
            filler = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; shell.AddChild(filler);
            shell.AddChild(ui.Watermark());
            foreach (var region in new Control[] { reading, evidence, feedback, actions }) region.MinimumSizeChanged += QueueFit;
            Resized += QueueFit;
        }
        heading.Text = s.CompanyName; heading.TooltipText = s.CompanyName;
        time.Text = $"Day {s.Day} · " + PresentationText.Time(s.NextCheckpoint, "Next competition");
        Measure("profile-shell", section); section = System.Diagnostics.Stopwatch.GetTimestamp();
        // Specific, bounded display records. Revision alone never identifies data.
        // Action binding below always uses the current presenter, ID and revision.
        var nextTermsKey = JsonSerializer.Serialize(new { s.CampaignId, s.CompanyId, Offer = o });
        if (termsKey != nextTermsKey)
        {
            Clear(terms); document = null; termsKey = nextTermsKey;
            if (o is not null)
            {
                document = DocumentView.Create(ui, SponsorPresentation.Document(s, o)); terms.AddChild(document);
                terms.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known, $"Limit: {s.SponsorSlots} active sponsors, including existing agreements. No termination action is supported."));
                if (o.Availability != "Signed") terms.AddChild(ComparisonView.Create(ui, new("Accept", "Leave available",
                    [new("Income and obligation", SponsorPresentation.Money(o.Payment) + " each scheduled receipt; +" + o.Load + " ongoing load.",
                    "No new income or load; opportunity may expire or be claimed."),
                 new("Future flexibility", "Uses the available active agreement slot.", "Retains the slot for another offer.")])));
            }
            else terms.AddChild(ValidationMessage.Create(ui, "This offer no longer exists. Return to Business & Finance."));
        }
        document?.SetMeta("revision", s.Revision);
        Measure("profile-document", section); section = System.Diagnostics.Stopwatch.GetTimestamp();
        var nextEvidenceKey = JsonSerializer.Serialize(new { s.CampaignId, s.CompanyId, s.CompanyName, s.Cash, s.Load, s.Capacity, s.CommittedForecast, s.Reputation, s.Agreements,
            s.Day, s.LastDay, s.LoadByDay, s.CompetitionDays, o?.OfferId, o?.CanAccept, o?.LoadIfAccepted, o?.ForecastIfAccepted, o?.LoadByDayIfAccepted, o?.ScheduledPayments });
        var evidenceChanged = evidenceKey is not null && evidenceKey != nextEvidenceKey;
        if (evidenceKey != nextEvidenceKey)
        {
            Clear(evidence); Clear(support); evidenceKey = nextEvidenceKey;
            evidence.AddChild(SectionHeader.Create(ui, "Company trade-off", "Is the scheduled income worth the ongoing delivery obligation?"));
            evidence.AddChild(ResourceValue.Create(ui, new("Cash now", s.Cash / 100m, "CU", InformationState.Known, "Signing itself pays nothing now.")));
            evidence.AddChild(SemanticText.Create(ui, $"Load now: {s.Load} / capacity {s.Capacity}", TypographyRole.Data,
                s.Load > s.Capacity ? ColorRole.StateWarning : ColorRole.TextPrimary));
            if (s.Load > s.Capacity)
                evidence.AddChild(SemanticText.Create(ui, $"{PresentationText.WarningMarker} Over capacity by {s.Load - s.Capacity} now.", TypographyRole.Label, ColorRole.StateWarning));
            if (o is not null && o.CanAccept && o.LoadIfAccepted is { } after)
            {
                var over = after - s.Capacity;
                evidence.AddChild(SemanticText.Create(ui, $"Load if accepted: {after} / capacity {s.Capacity}", TypographyRole.Data,
                    over > 0 ? ColorRole.StateWarning : ColorRole.TextPrimary));
                evidence.AddChild(SemanticText.Create(ui, over > 0
                    ? $"{PresentationText.WarningMarker} Over capacity by {over} from acceptance through day {o.EndDay}."
                    : $"{PresentationText.NeutralMarker} Within capacity; {-over} remaining after acceptance.", TypographyRole.Label, over > 0 ? ColorRole.StateWarning : ColorRole.TextSecondary));
            }
            evidence.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known,
                "Sponsor load shares capacity with preparation. When total load exceeds capacity, future preparation converts more slowly; completed work is retained."));
            evidence.AddChild(ConfidenceIndicator.Create(ui, InformationState.Unknown,
                "Future wins and total bonuses. No exact future performance or revenue estimate is available."));
            evidence.AddChild(ResourceValue.Create(ui, SponsorPresentation.Forecast(s)));
            if (o is not null && o.CanAccept && o.ForecastIfAccepted is { } forecastIfAccepted)
                evidence.AddChild(ResourceValue.Create(ui, SponsorPresentation.ForecastIfAccepted(forecastIfAccepted)));
            evidence.AddChild(DayTrack.Create(ui, SponsorPresentation.Track(s, o)));
            support.AddChild(SectionHeader.Create(ui, "Supporting evidence"));
            support.AddChild(SemanticText.Create(ui, "Company: " + s.CompanyName, TypographyRole.Label));
            support.AddChild(SemanticText.Create(ui, "Current reputation: " + s.Reputation + ". Signing itself grants no reputation or audience.", TypographyRole.Label));
            foreach (var a in s.Agreements)
            {
                var next = a.Payments.FirstOrDefault(x => x.Remaining > 0);
                support.AddChild(SemanticText.Create(ui, a.Name + $" · load {a.Load} through day {a.EndDay}" +
                    (next is null ? " · no outstanding base receipts" : $" · next receipt day {next.DueDay}: " + SponsorPresentation.Money(next.Remaining)), TypographyRole.Label));
            }
        }
        Measure("profile-evidence", section); section = System.Diagnostics.Stopwatch.GetTimestamp();
        var rejected = Presenter.Phase == SponsorPhase.Rejected;
        var nextFeedback = (rejected ? PresentationText.WarningMarker : PresentationText.NeutralMarker) + " " + Presenter.Feedback;
        var feedbackChanged = feedback.Text.Length > 0 && feedback.Text != nextFeedback;
        feedback.Text = nextFeedback;
        ui.Tone(feedback, rejected ? ColorRole.SystemError : ColorRole.TextSecondary);
        Clear(actions);
        BackButton = ui.Button(Presenter.Phase == SponsorPhase.Confirm ? "Keep reviewing · Esc" : "Return to company · Esc", () => Act("back"));
        BackButton.SizeFlagsVertical = SizeFlags.ShrinkEnd; actions.AddChild(BackButton);
        if (Presenter.Phase == SponsorPhase.Confirm && o is not null)
        {
            Commitment = new(); actions.AddChild(Commitment);
            // Revision stays in binding metadata; players see terms, not data versions.
            Commitment.Bind(ui, new(o.OfferId, s.Revision, "Accept " + o.Name + " on the terms shown",
                $"+{o.Load} load now through day {o.EndDay}; {SponsorPresentation.Money(o.Payment)} each scheduled receipt. Future wins unknown.",
                ActionPhase.Ready, "Material commitment; no termination action."), _ => Act("commit"));
        }
        else
        {
            ReviewButton = ui.PrimaryButton(Presenter.Phase == SponsorPhase.Accepted ? "Agreement signed" : "Review acceptance", () => Act("review"));
            ReviewButton.Disabled = !Presenter.CanReview; actions.AddChild(ReviewButton);
        }
        Measure("profile-actions", section);
        Measure("bind", start);
        // Acknowledge changed authoritative state (refresh after commit/rejection), never the first bind.
        if (evidenceChanged) UiMotion.Highlight(ui, evidence);
        if (feedbackChanged) UiMotion.Highlight(ui, feedback);
        QueueFit();
    }
    private void QueueFit()
    {
        if (fitQueued || shell is null) return;
        fitQueued = true; Callable.From(Fit).CallDeferred();
    }
    private void Fit()
    {
        fitQueued = false;
        if (!IsInstanceValid(shell)) return;
        var visible = shell.GetChildren().OfType<Control>().Where(x => x.Visible).ToArray();
        var others = visible.Where(x => x != columns && x != filler).Sum(x => x.GetCombinedMinimumSize().Y);
        // Measure against the anchored view, never the shell: a taller action area can stretch
        // the shell past the viewport, and measuring that stretched size would preserve overflow.
        var available = Size.Y - 2 * ui.Tokens.Space(SpaceRole.SpaceWorkspace) - others - shell.GetThemeConstant("separation") * (visible.Length - 1);
        var height = Math.Max(0, Math.Min(ReadingContentHeight, available));
        if (Math.Abs(columns.CustomMinimumSize.Y - height) > UiTokens.LayoutTolerance) columns.CustomMinimumSize = new Vector2(0, height);
    }
    private static void Clear(Node region)
    {
        foreach (var child in region.GetChildren()) { region.RemoveChild(child); child.QueueFree(); }
    }
    private ScrollContainer ReadingScroll()
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
            FocusMode = FocusModeEnum.All
        };
        scroll.GuiInput += e =>
        {
            if (e is not InputEventKey { Pressed: true } k || !scroll.HasFocus()) return;
            if (k.Keycode == Key.End) scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
            else if (k.Keycode == Key.Home) scroll.ScrollVertical = 0;
            else if (k.Keycode == Key.Pagedown) scroll.ScrollVertical += (int)scroll.Size.Y;
            else if (k.Keycode == Key.Pageup) scroll.ScrollVertical -= (int)scroll.Size.Y;
            else return;
            scroll.AcceptEvent();
        };
        return scroll;
    }
    public void Act(string kind)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!Presenter.Handle(new(kind, Presenter.OfferId, Presenter.Snapshot.Revision))) return;
        Measure(kind, start);
        if (Presenter.Phase == SponsorPhase.Closed) { returned?.Invoke(); return; }
        Render();
        // Keep final commitment a separate activation, including repeated Enter key events.
        BackButton.GrabFocus();
    }
    private void Measure(string name, long start)
    {
        if (!Timings.TryGetValue(name, out var values)) Timings[name] = values = [];
        values.Add(System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
    }
    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) { GetViewport().SetInputAsHandled(); Act("back"); }
    }
}
