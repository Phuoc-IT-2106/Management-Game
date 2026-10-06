using System.Collections.Immutable;
using ManagementGame.Domain;

namespace ManagementGame.Application;

public sealed record SponsorPayment(string Id, int DueDay, long Amount, long Remaining);
public sealed record SponsorTerms(string OfferId, string Name, int Deadline, int EndDay,
    long Payment, long WinBonus, int Load, int MinimumReputation, bool CanAccept,
    string Availability, ImmutableArray<SponsorPayment> ScheduledPayments,
    int? LoadIfAccepted = null, long? ForecastIfAccepted = null);
public sealed record SponsorAgreement(string Id, string Name, int EndDay, int Load,
    long WinBonus, ImmutableArray<SponsorPayment> Payments);
public sealed record SponsorSnapshot(string CampaignId, string CompanyId, string CompanyName,
    long Revision, int Day, int? NextCheckpoint, long Cash, long CommittedForecast,
    int Load, int Capacity, int Reputation, ImmutableArray<SponsorTerms> Offers,
    ImmutableArray<SponsorAgreement> Agreements);

public interface ISponsorSession
{
    SponsorSnapshot ObserveSponsors();
    Response Submit(Request request);
}

public sealed partial class Session : ISponsorSession
{
    public SponsorSnapshot ObserveSponsors()
    {
        lock (gate) return SponsorProjection.Build(state, content);
    }
}

public static class SponsorProjection
{
    // Only player-owned finances and visible offer terms; no rival private inputs.
    public static SponsorSnapshot Build(Campaign state, Content content)
    {
        var c = state.Company; var w = state.World; var day = w.Calendar.Day;
        var agreements = c.Sponsors.Select(x => new SponsorAgreement(x.Id, x.Name, x.EndDay, x.Load, x.WinBonus,
            c.FinancialItems.Where(i => i.CauseId == x.Id && i.Incoming)
                .OrderBy(i => i.DueDay).ThenBy(i => i.Id, StringComparer.Ordinal)
                .Select(i => new SponsorPayment(i.Id, i.DueDay, i.Amount, i.Remaining)).ToImmutableArray())).ToImmutableArray();
        var offers = w.Offers.Select(o =>
        {
            // Reuse the pure authoritative transition for eligibility and schedule preview.
            // No new legality or payment cadence calculation, receipts or RNG in projection.
            Campaign? preview = null; var reason = "Available";
            try { preview = Simulation.Apply(state, new AcceptSponsor(o.Id), content, "projection:sponsor"); }
            catch (RuleViolation e) { reason = e.Message; }
            var signed = agreements.FirstOrDefault(x => x.Id == "agreement:" + o.Id);
            if (o.ClaimedBy is not null) reason = o.ClaimedBy == c.Id ? "Signed" : "Claimed by rival";
            var payments = signed?.Payments ?? (preview is null ? ImmutableArray<SponsorPayment>.Empty :
                preview.Company.FinancialItems.Where(i => i.CauseId == "agreement:" + o.Id)
                    .Select(i => new SponsorPayment(i.Id, i.DueDay, i.Amount, i.Remaining)).ToImmutableArray());
            // Consequence preview comes from the same authoritative transition, never UI arithmetic.
            return new SponsorTerms(o.Id, o.Name, o.Deadline, o.EndDay, o.Payment, o.WinBonus,
                o.Load, o.MinimumReputation, preview is not null, reason, payments,
                preview is null ? null : Simulation.Load(preview.Company, content.Balance, day),
                preview is null ? null : Finance.Forecast(preview.Company, day));
        }).ToImmutableArray();
        return new(state.Execution.CampaignId, c.Id, c.Name, state.Execution.Revision, day,
            Simulation.NextFixture(state)?.Day, c.Cash, Finance.Forecast(c, day),
            Simulation.Load(c, content.Balance, day), c.Coach.Capacity, c.Reputation, offers, agreements);
    }
}
