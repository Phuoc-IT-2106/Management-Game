using System.Collections.Immutable;
using ManagementGame.Domain;

namespace ManagementGame.Application;

public static class Observations
{
    private static readonly (string Prefix, string Kind)[] Kinds =
    [
        ("notice:offer:", "Offer"), ("notice:negotiation:", "Negotiation"), ("notice:contract:", "Contract"),
        ("notice:renewal:", "Renewal"), ("notice:season:", "Season"), ("result:", "Match"), ("claim:", "Market"),
        ("lapse:", "Market"), ("meta:", "Meta"), ("commercial:", "Commercial")
    ];
    /// <summary>World-originated messages, newest first; the player's own decisions are not inbox mail.</summary>
    public static ImmutableArray<InboxRow> Inbox(Company c) => c.Reviews
        .Select(r => (Review: r, Kind: Kinds.FirstOrDefault(k => r.Id.StartsWith(k.Prefix, StringComparison.Ordinal)).Kind))
        .Where(x => x.Kind is not null).Reverse().Take(40)
        .Select(x => new InboxRow(x.Review.Id, x.Review.Day, x.Kind!, x.Review.Text, x.Review.CauseId)).ToImmutableArray();
    private static string Outcome(CompetitiveOutcome r) => r.PlanCause == "forfeit" ? "Forfeit" : r.Won ? "Victory" : "Defeat";
    public static ImmutableArray<FixtureRow> FixtureRows(Campaign s) => s.World.Fixtures.Select(f =>
    {
        var result = s.World.Results.FirstOrDefault(r => r.FixtureId == f.Id);
        return new FixtureRow(f.Id, f.Day, s.World.Rivals.Single(r => r.Id == f.RivalId).Name, f.Importance, result is null ? "" : Outcome(result));
    }).ToImmutableArray();
    public static ImmutableArray<RivalRow> RivalRows(Campaign s) => s.World.Rivals.OrderBy(r => r.Name, StringComparer.Ordinal).Select(r =>
    {
        var played = s.World.Results.Where(x => x.RivalId == r.Id && s.World.Fixtures.Any(f => f.Id == x.FixtureId)).ToArray();
        return new RivalRow(r.Id, r.Name, played.Count(x => x.Won), played.Count(x => !x.Won),
            s.World.Fixtures.Count(f => f.RivalId == r.Id && !s.World.Results.Any(x => x.FixtureId == f.Id)));
    }).ToImmutableArray();
    public static Situation Build(Campaign s, Content content)
    {
        var c = s.Company; var w = s.World; var b = content.Balance; var day = w.Calendar.Day;
        var fixture = Simulation.NextFixture(s);
        var rival = fixture is null ? null : w.Rivals.Single(r => r.Id == fixture.RivalId);
        var width = Math.Max(4, (100 - c.Information) / 4);
        var estimate = rival is null ? 0 : rival.Strength + KeyedRandom.Range(s.Execution.Seed, "information",
            $"player:{rival.Id}:{w.Results.Length}", "strength-estimate", width * 2 + 1) - width;
        var estimatedPosture = rival is null ? Posture.Balanced : (Posture)(((int)rival.Posture +
            (KeyedRandom.Range(s.Execution.Seed, "information", $"player:{rival.Id}:{w.Results.Length}", "tendency", 100) < c.Information ? 0 : 1)) % 3);
        bool Renewable(Employment contract) => contract.EndDay - day <= b.RenewalWindow;
        var people = c.People.OrderBy(p => p.Role, StringComparer.Ordinal).ThenBy(p => p.Id, StringComparer.Ordinal).Select(p =>
        {
            var contract = c.Employment.Single(x => x.PersonId == p.Id);
            return new PersonRow(p.Id, p.Name, p.Role, p.Execution, p.Readiness, contract.Salary, contract.ReleaseCost,
                c.People.Any(x => x.Id != p.Id && x.Role == p.Role), contract.EndDay, Simulation.RenewalDemand(c, contract), Renewable(contract));
        }).ToImmutableArray();
        var coachContract = c.Employment.Single(x => x.PersonId == c.Coach.Id);
        static string Band(int value) => $"{Math.Max(0, value / 10 * 10 - 10)}–{Math.Min(100, value / 10 * 10 + 20)} (estimate)";
        var active = Market.ActiveSponsors(c, day);
        string Availability(Offer o) => o.ClaimedBy is not null ? o.ClaimedBy == c.Id ? "Signed" : "Claimed by rival"
            : o.Deadline < day ? "Expired" : c.Reputation < o.MinimumReputation ? "Reputation too low"
            : c.Sponsors.Any(x => x.BrandId == o.BrandId && x.EndDay >= day) ? "Brand already a partner"
            : active >= b.SponsorSlots ? "No free sponsor slot" : "Available";
        var brands = content.Brands.OrderBy(x => x.MinimumReputation).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x =>
        {
            var fair = Market.FairPayment(x, c.Reputation, c.Audience);
            var status = !Market.Approachable(x, c) ? $"Out of reach: needs reputation {x.MinimumReputation}, audience {x.MinimumAudience:N0}"
                : Market.Blocked(x, c, w, day) ?? (Market.Qualifies(x, c) ? "Open to proposals" : "Open, but values you below its tier");
            return new BrandRow(x.Id, x.Name, x.Sector, x.MinimumReputation, x.MinimumAudience, status,
                Numbers.Divide(fair * 90, 100), Numbers.Divide(fair * 110, 100), x.DurationDays.Order().ToImmutableArray());
        }).ToImmutableArray();
        var view = new Situation(s.Execution.Revision, day, Seasons.Of(day, b), Seasons.DayOf(day, b), b.SeasonLength, c.Name, c.Recovery.ToString(),
            c.Cash, Finance.Forecast(c, day), c.Reputation, c.Audience, Simulation.Load(c, b, day), c.Coach.Capacity,
            c.Coach.Name, (Delegation)c.Authority.Mode, (Risk)c.Authority.RiskCeiling, fixture?.Id ?? "", fixture?.Day ?? 0,
            rival?.Name ?? "No match until next season", rival is null ? "—" : $"{Math.Max(0, estimate - width)}–{Math.Min(100, estimate + width)} (estimate)",
            estimatedPosture.ToString(), c.Information >= 65 ? "Moderate confidence" : "Low confidence", w.Meta.ToString(),
            c.Plan is null ? null : new PlanView(c.Plan.FixtureId, c.Plan.Execution, c.Plan.Opponent, c.Plan.Meta, (Risk)c.Plan.Posture, c.Plan.Lineup),
            null, "", people,
            w.Candidates.Where(x => x.Deadline >= day).Select(x => new CandidateRow(x.Person.Id, x.Person.Name, x.Person.Role,
                Band(x.Person.Execution), x.Fee, x.Salary, x.Deadline)).ToImmutableArray(),
            w.Offers.Select(o => new SponsorRow(o.Id, o.Name, o.Payment, o.WinBonus, o.Load, o.Deadline, o.MinimumReputation, Availability(o), o.Origin.ToString(), o.DurationDays)).ToImmutableArray(),
            c.Sponsors.Where(x => x.EndDay >= day).Select(x => $"{x.Name}: {x.Payment / 100m:N2} CU weekly, delivery load {x.Load}, win bonus {x.WinBonus / 100m:N2} CU, ends day {x.EndDay}").ToImmutableArray(),
            c.FinancialItems.Where(x => x.Remaining > 0).OrderBy(x => x.DueDay).ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new BillRow(x.Id, x.CauseId, x.DueDay, x.Remaining, x.Incoming, x.MissedDay)).ToImmutableArray(),
            w.Results.Select(r => new ResultRow(r.Id, r.Day, w.Rivals.Single(x => x.Id == r.RivalId).Name, r.PlanCause == "forfeit" ? "Forfeit" : r.Won ? "Victory" : "Defeat", r.Posture.ToString())).ToImmutableArray(),
            c.Reviews.TakeLast(24).Reverse().Select(r => $"Day {r.Day} · {r.Text} [cause: {r.CauseId}]").ToImmutableArray(),
            string.Join("; ", c.PendingEffects.Select(e => $"Day {e.DueDay}: reputation {e.Reputation:+#;-#;0}, audience {e.Audience:+#;-#;0}")), content.Label,
            new StaffRow(c.Coach.Id, c.Coach.Name, coachContract.Salary, coachContract.EndDay, Simulation.RenewalDemand(c, coachContract), Renewable(coachContract)),
            w.CoachCandidates.Where(x => x.Deadline >= day).Select(x => new CoachCandidateRow(x.Coach.Id, x.Coach.Name,
                Band((x.Coach.Preparation + x.Coach.Analysis + x.Coach.Adaptability) / 3), x.Fee, x.Salary, x.Deadline)).ToImmutableArray(),
            brands, w.Negotiations.Select(n => new NegotiationRow(n.Id, Market.Brand(content, n.BrandId).Name, n.Payment, n.DurationDays, n.ResponseDay)).ToImmutableArray(),
            b.SponsorSlots, active, Inbox(c), FixtureRows(s), RivalRows(s));
        var recommendation = CoachPolicy.Recommend(view);
        return view with { Recommendation = recommendation, RecommendationReason = recommendation is null
            ? view.NextFixtureId.Length == 0 ? "No match to prepare until next season." : "No legal lineup: sign players to cover every role A–E."
            : $"Coach uses observed readiness, opponent estimates and the public meta. Suggested {recommendation.Execution}/{recommendation.Opponent}/{recommendation.Meta}% allocation; {view.Confidence.ToLowerInvariant()}. Hidden opponent preparation and variance are unknown." };
    }
}

// The policy can only receive the same actor-safe observations presented to the player.
public static class CoachPolicy
{
    public static PlanView? Recommend(Situation view)
    {
        if (view.NextFixtureId.Length == 0) return null;
        var lineup = view.People.GroupBy(p => p.Role).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(p => p.Execution * p.Readiness).ThenBy(p => p.Id, StringComparer.Ordinal).First().Id).ToImmutableArray();
        if (lineup.Length != 5) return null;
        var meta = view.Meta == "Aggressive";
        var posture = meta && view.Ceiling == Risk.Aggressive ? Risk.Aggressive : view.Ceiling == Risk.Conservative ? Risk.Conservative : Risk.Balanced;
        return new PlanView(view.NextFixtureId, meta ? 25 : 50, meta ? 25 : 30, meta ? 50 : 20, posture, lineup);
    }
}
