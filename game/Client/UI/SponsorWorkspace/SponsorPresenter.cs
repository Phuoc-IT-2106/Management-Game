using System.Collections.Immutable;
using ManagementGame.Application;
using ManagementGame.UiKit;
using ManagementGame.OperatingMap;
using System.Globalization;

namespace ManagementGame.SponsorWorkspace;

public enum SponsorPhase { Review, Confirm, Pending, Accepted, Rejected, Closed }
public sealed class SponsorPresenter(ISponsorSession session, SponsorSnapshot snapshot, string offerId, MapReturn origin)
{
    private bool dispatching;
    public Dictionary<string, List<double>> Timings { get; } = [];
    private void Measure(string name, long start)
    { if (!Timings.TryGetValue(name, out var values)) Timings[name] = values = []; values.Add(System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
    public SponsorSnapshot Snapshot { get; private set; } = snapshot;
    public string OfferId { get; } = offerId;
    public MapReturn Origin { get; } = origin;
    public SponsorTerms? Offer => Snapshot.Offers.FirstOrDefault(o => o.OfferId == OfferId);
    public SponsorPhase Phase { get; private set; } = SponsorPhase.Review;
    public string Feedback { get; private set; } = "Review current terms before making a commitment.";
    public bool CanReview => !dispatching && (Phase is SponsorPhase.Review or SponsorPhase.Rejected) && Offer?.CanAccept == true;
    public bool Handle(UiIntent intent)
    {
        if (dispatching || Phase == SponsorPhase.Closed || intent.TargetId != OfferId || intent.Revision != Snapshot.Revision) return false;
        switch (intent.Kind)
        {
            case "review" when CanReview:
                Phase = SponsorPhase.Confirm; Feedback = "Accept this offer? Delivery load starts now; cash arrives only on scheduled dates."; return true;
            case "back":
                Phase = Phase == SponsorPhase.Confirm ? SponsorPhase.Review : SponsorPhase.Closed; return true;
            case "commit" when Phase == SponsorPhase.Confirm && Offer?.CanAccept == true:
                dispatching = true; Phase = SponsorPhase.Pending;
                try
                {
                    var start = System.Diagnostics.Stopwatch.GetTimestamp();
                    var response = session.Submit(new Request("sponsor-ui:" + Guid.NewGuid().ToString("N"), Snapshot.Revision, new SponsorDecision(OfferId)));
                    Measure("command-acknowledgement", start);
                    start = System.Diagnostics.Stopwatch.GetTimestamp();
                    Snapshot = session.ObserveSponsors();
                    Measure("post-command-refresh", start);
                    Phase = response.Accepted ? SponsorPhase.Accepted : SponsorPhase.Rejected;
                    Feedback = response.Accepted
                        ? "Agreement signed. Delivery load applies now. Scheduled payments have not been added to Cash."
                        : response.Message + " Current data refreshed; review again before committing.";
                    return true;
                }
                finally { dispatching = false; }
            default: return false;
        }
    }
    public void Refresh()
    {
        // Rendering/rebinding during Submit must not release the in-flight latch.
        if (dispatching || Phase == SponsorPhase.Closed) return;
        Snapshot = session.ObserveSponsors(); Phase = SponsorPhase.Review;
        Feedback = "Current data refreshed. Review the terms again.";
    }
}

public static class SponsorPresentation
{
    public static string Money(long minor) => (minor / 100m).ToString("N2", CultureInfo.InvariantCulture) + " CU";
    public static string Dates(SponsorTerms offer) => offer.ScheduledPayments.IsEmpty
        ? "No actionable payment schedule; offer unavailable."
        : string.Join(", ", offer.ScheduledPayments.Select(x => "day " + x.DueDay));
    public static DocumentData Document(SponsorSnapshot s, SponsorTerms o) => new(o.OfferId, s.Revision,
        o.Name, "Commercial offer · " + o.Availability,
        [new("Scheduled receipt", Money(o.Payment) + " each · " + Dates(o)),
         new("Agreement ends", "Day " + o.EndDay + " inclusive"),
         new("Delivery obligation", "+" + o.Load + " organizational load from acceptance through end day"),
         new("Win bonus", Money(o.WinBonus) + " per win while active; due the following day"),
         new("Accept by", "Day " + o.Deadline + " · minimum reputation " + o.MinimumReputation)]);
    public static DayTrackData Track(SponsorSnapshot s, SponsorTerms? o)
    {
        var proposing = o is { CanAccept: true } && !o.LoadByDayIfAccepted.IsEmpty;
        var rows = ImmutableArray.CreateBuilder<DayRow>();
        rows.Add(new("Receipts · signed", s.Agreements.SelectMany(a => a.Payments.Where(p => p.Remaining > 0).Select(p => p.DueDay)).ToImmutableArray(), DayMarkShape.Filled));
        if (proposing) rows.Add(new("Receipts · this offer", o!.ScheduledPayments.Select(p => p.DueDay).ToImmutableArray(), DayMarkShape.Outlined));
        rows.Add(new("Competition", s.CompetitionDays, DayMarkShape.Diamond));
        return new("When does load exceed capacity, and when do receipts and matches fall?", s.Day, s.LastDay, s.Capacity, "capacity",
            "Load now", s.LoadByDay, "If accepted", proposing ? o!.LoadByDayIfAccepted : [], rows.ToImmutable(),
            "Projected from today's roster and agreements; signing or releasing players changes daily load.");
    }
    public static ResourceView ForecastIfAccepted(long forecast) => new("Forecast if accepted", forecast / 100m,
        "CU", InformationState.Estimated, "Same 7-day window, adding this offer's receipts due in it. Excludes unearned wins.");
    public static ResourceView Forecast(SponsorSnapshot s) => new("Committed cash forecast", s.CommittedForecast / 100m,
        "CU", InformationState.Estimated, "Next 7 days; signed items only. Excludes unearned wins and unsigned offers.");

    public static MapSnapshot Company(SponsorSnapshot s, string sessionKey)
    {
        var identity = new OrganizationIdentityView(s.CompanyId, s.CompanyName, "", null, null, null, s.Revision, true);
        var scopes = new MapScope[] {
            new(s.CompanyId, null, s.CompanyName, ScopeKind.Company, "Company commitments support and constrain the primary team."),
            new(MapFixtures.Portfolio, s.CompanyId, "Competitive Portfolio", ScopeKind.Portfolio, "The current owned competitive business."),
            new(MapFixtures.Esports, MapFixtures.Portfolio, "Esports", ScopeKind.Division, "Current operating domain."),
            new(MapFixtures.Discipline, MapFixtures.Esports, "Development Discipline", ScopeKind.Discipline, PresentationText.FixtureNotice),
            new(MapFixtures.Team, MapFixtures.Discipline, "Primary Team", ScopeKind.Team, "Sponsor delivery load shares capacity with future preparation."),
            new(MapFixtures.Business, s.CompanyId, "Business & Finance", ScopeKind.Function, "Review sponsorship commitments and their payment timing."),
            new(MapFixtures.Affairs, s.CompanyId, "Affairs & Time", ScopeKind.Function, "Offer deadlines and the next outstanding sponsor receipt.") };
        var matters = s.Offers.Take(3).Select(o => new MapSituation("matter:" + o.OfferId, MapFixtures.Business,
            o.Name, "Sponsor", o.CanAccept ? Attention.High : Attention.Normal, o.Availability, o.Availability == "Signed" ? o.EndDay : o.Deadline,
            (o.Availability == "Signed" ? "Signed agreement: " : "Review ") + Money(o.Payment) + " scheduled receipts against " + o.Load + " delivery load.",
            "Load applies now; payments follow the agreement schedule. Future wins are unknown.", o.OfferId,
            "Sponsor / Commercial Commitment", InformationState.Known, "Offer terms are known. Acceptance does not immediately change Cash.")).ToList();
        var next = s.Agreements.SelectMany(a => a.Payments.Where(p => p.Remaining > 0).Select(p => (a, p)))
            .OrderBy(x => x.p.DueDay).ThenBy(x => x.p.Id, StringComparer.Ordinal).FirstOrDefault();
        if (next.a is not null)
            matters.Add(new("matter:" + next.p.Id, MapFixtures.Business, "Sponsor receipt · " + next.a.Name,
                "Receipt", Attention.Normal, "Scheduled", next.p.DueDay, Money(next.p.Remaining) + " remains receivable.",
                "Finance settles due items when time advances; scheduled is not received.", next.a.Id,
                "Scheduled sponsor receipt", InformationState.Known, "Signed agreement: " + next.a.Name));
        return new(sessionKey, s.Revision, identity, s.Day, s.NextCheckpoint, [..scopes],
            [new(MapFixtures.Business, MapFixtures.Team, "Supports / constrains"), new(MapFixtures.Affairs, s.CompanyId, "References situations across")], [..matters]);
    }
}
