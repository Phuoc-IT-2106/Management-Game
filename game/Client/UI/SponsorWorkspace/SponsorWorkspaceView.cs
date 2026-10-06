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
    private VBoxContainer terms = null!, evidence = null!;
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
            var shell = ui.Stack(); margin.AddChild(shell);
            heading = SemanticText.Create(ui, "[CO] " + s.CompanyName, TypographyRole.CompanyIdentity);
            heading.AutowrapMode = TextServer.AutowrapMode.Off; heading.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            heading.TooltipText = s.CompanyName; shell.AddChild(heading);
            shell.AddChild(SemanticText.Create(ui, "Business & Finance  /  Sponsorship  /  Commitment review", TypographyRole.Label));
            time = SemanticText.Create(ui, "", TypographyRole.Annotation, ColorRole.TextSecondary); shell.AddChild(time);
            var columns = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; shell.AddChild(columns);
            TermsScroll = ReadingScroll(); EvidenceScroll = ReadingScroll(); columns.AddChild(TermsScroll); columns.AddChild(EvidenceScroll);
            terms = ui.Stack(); evidence = ui.Stack(); TermsScroll.AddChild(terms); EvidenceScroll.AddChild(evidence);
            feedback = ValidationMessage.Create(ui, "", false); shell.AddChild(feedback);
            actions = new HBoxContainer(); shell.AddChild(actions);
            shell.AddChild(SemanticText.Create(ui, UiTokens.Notice, TypographyRole.Annotation, ColorRole.TextMuted));
        }
        heading.Text = "[CO] " + s.CompanyName; heading.TooltipText = s.CompanyName;
        time.Text = $"Day {s.Day} · " + PresentationText.Time(s.NextCheckpoint, "Next competition") + " · " + PresentationText.FixtureNotice;
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
                terms.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known, "Limit: two active sponsors, including the initial agreement. No termination action is supported."));
                if (o.Availability != "Signed") terms.AddChild(ComparisonView.Create(ui, new("Accept", "Leave available",
                    [new("Income and obligation", SponsorPresentation.Money(o.Payment) + " each scheduled receipt; +" + o.Load + " ongoing load.",
                    "No new income or load; opportunity may expire or be claimed."),
                 new("Future flexibility", "Uses the available active agreement slot.", "Retains the slot for another offer.")])));
            }
            else terms.AddChild(ValidationMessage.Create(ui, "This offer no longer exists. Return to Business & Finance."));
        }
        document?.SetMeta("revision", s.Revision);
        Measure("profile-document", section); section = System.Diagnostics.Stopwatch.GetTimestamp();
        var nextEvidenceKey = JsonSerializer.Serialize(new { s.CampaignId, s.CompanyId, s.CompanyName, s.Cash, s.Load, s.Capacity, s.CommittedForecast, s.Reputation, s.Agreements });
        if (evidenceKey != nextEvidenceKey)
        {
            Clear(evidence); evidenceKey = nextEvidenceKey;
            evidence.AddChild(SectionHeader.Create(ui, "Company trade-off", "Is the scheduled income worth the ongoing delivery obligation?"));
            evidence.AddChild(ResourceValue.Create(ui, new("Cash now", s.Cash / 100m, "CU", InformationState.Known, "Signing itself pays nothing now.")));
            evidence.AddChild(SemanticText.Create(ui, $"Current load {s.Load} / capacity {s.Capacity}"));
            evidence.AddChild(ConfidenceIndicator.Create(ui, InformationState.Known,
                "Sponsor load shares capacity with preparation. When total load exceeds capacity, future preparation converts more slowly; completed work is retained."));
            evidence.AddChild(ConfidenceIndicator.Create(ui, InformationState.Unknown,
                "Future wins and total bonuses. No exact future performance or revenue estimate is available."));
            evidence.AddChild(ResourceValue.Create(ui, SponsorPresentation.Forecast(s)));
            evidence.AddChild(SectionHeader.Create(ui, "Supporting evidence"));
            evidence.AddChild(SemanticText.Create(ui, "Company: " + s.CompanyName, TypographyRole.Label));
            evidence.AddChild(SemanticText.Create(ui, "Current reputation: " + s.Reputation + ". Signing itself grants no reputation or audience.", TypographyRole.Label));
            foreach (var a in s.Agreements)
            {
                var next = a.Payments.FirstOrDefault(x => x.Remaining > 0);
                evidence.AddChild(SemanticText.Create(ui, a.Name + $" · load {a.Load} through day {a.EndDay}" +
                    (next is null ? " · no outstanding base receipts" : $" · next receipt day {next.DueDay}: " + SponsorPresentation.Money(next.Remaining)), TypographyRole.Label));
            }
        }
        Measure("profile-evidence", section); section = System.Diagnostics.Stopwatch.GetTimestamp();
        var rejected = Presenter.Phase == SponsorPhase.Rejected;
        feedback.Text = (rejected ? "[!] " : "[i] ") + Presenter.Feedback;
        ui.Tone(feedback, rejected ? ColorRole.SystemError : ColorRole.TextSecondary);
        Clear(actions);
        BackButton = ui.Button(Presenter.Phase == SponsorPhase.Confirm ? "Keep reviewing · Esc" : "Return to company · Esc", () => Act("back")); actions.AddChild(BackButton);
        if (Presenter.Phase == SponsorPhase.Confirm && o is not null)
        {
            Commitment = new(); actions.AddChild(Commitment);
            Commitment.Bind(ui, new(o.OfferId, s.Revision, "Accept " + o.Name + " · current data revision " + s.Revision,
                $"+{o.Load} load now through day {o.EndDay}; {SponsorPresentation.Money(o.Payment)} each scheduled receipt. Future wins unknown.",
                ActionPhase.Ready, "Material commitment; no termination action."), _ => Act("commit"));
        }
        else
        {
            ReviewButton = ui.Button(Presenter.Phase == SponsorPhase.Accepted ? "Agreement signed" : "Review acceptance", () => Act("review"));
            ReviewButton.Disabled = !Presenter.CanReview; actions.AddChild(ReviewButton);
        }
        Measure("profile-actions", section);
        Measure("bind", start);
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
