using System.Collections.Immutable;
using ManagementGame.Domain;

namespace ManagementGame.Application;

public static class Observations
{
    public static Situation Build(Campaign s, Content content)
    {
        var c = s.Company; var w = s.World; var day = w.Calendar.Day;
        var fixture = Simulation.NextFixture(s);
        var rival = fixture is null ? null : w.Rivals.Single(r => r.Id == fixture.RivalId);
        var width = Math.Max(4, (100 - c.Information) / 4);
        var estimate = rival is null ? 0 : rival.Strength + KeyedRandom.Range(s.Execution.Seed, "information",
            $"player:{rival.Id}:{w.Results.Length}", "strength-estimate", width * 2 + 1) - width;
        var estimatedPosture = rival is null ? Posture.Balanced : (Posture)(((int)rival.Posture +
            (KeyedRandom.Range(s.Execution.Seed, "information", $"player:{rival.Id}:{w.Results.Length}", "tendency", 100) < c.Information ? 0 : 1)) % 3);
        var people = c.People.OrderBy(p => p.Role, StringComparer.Ordinal).ThenBy(p => p.Id, StringComparer.Ordinal).Select(p =>
        {
            var contract = c.Employment.Single(x => x.PersonId == p.Id);
            return new PersonRow(p.Id, p.Name, p.Role, p.Execution, p.Readiness, contract.Salary, contract.ReleaseCost,
                c.People.Any(x => x.Id != p.Id && x.Role == p.Role));
        }).ToImmutableArray();
        var view = new Situation(s.Execution.Revision, day, c.Name, c.Recovery.ToString(), w.Finished,
            c.Cash, Finance.Forecast(c, day), c.Reputation, c.Audience, Simulation.Load(c, content.Balance, day), c.Coach.Capacity,
            c.Coach.Name, (Delegation)c.Authority.Mode, (Risk)c.Authority.RiskCeiling, fixture?.Id ?? "", fixture?.Day ?? 0,
            rival?.Name ?? "No remaining match", rival is null ? "—" : $"{Math.Max(0, estimate - width)}–{Math.Min(100, estimate + width)} (estimate)",
            estimatedPosture.ToString(), c.Information >= 65 ? "Moderate confidence" : "Low confidence", w.Meta.ToString(),
            c.Plan is null ? null : new PlanView(c.Plan.FixtureId, c.Plan.Execution, c.Plan.Opponent, c.Plan.Meta, (Risk)c.Plan.Posture, c.Plan.Lineup),
            null, "", people,
            w.Candidates.Where(x => x.Deadline >= day).Select(x => new CandidateRow(x.Person.Id, x.Person.Name, x.Person.Role,
                $"{Math.Max(0, x.Person.Execution / 10 * 10 - 10)}–{Math.Min(100, x.Person.Execution / 10 * 10 + 20)} (estimate)", x.Fee, x.Salary, x.Deadline)).ToImmutableArray(),
            w.Offers.Select(o => new SponsorRow(o.Id, o.Name, o.Payment, o.WinBonus, o.Load, o.Deadline, o.MinimumReputation,
                o.ClaimedBy is not null ? o.ClaimedBy == c.Id ? "Signed" : "Claimed by rival" : o.Deadline < day ? "Expired" : c.Reputation < o.MinimumReputation ? "Reputation too low" : "Available")).ToImmutableArray(),
            c.Sponsors.Where(x => x.EndDay >= day).Select(x => $"{x.Name}: delivery load {x.Load}, win bonus {x.WinBonus} minor units, ends day {x.EndDay}").ToImmutableArray(),
            c.FinancialItems.Where(x => x.Remaining > 0).OrderBy(x => x.DueDay).ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new BillRow(x.Id, x.CauseId, x.DueDay, x.Remaining, x.Incoming, x.MissedDay)).ToImmutableArray(),
            w.Results.Select(r => new ResultRow(r.Id, r.Day, w.Rivals.Single(x => x.Id == r.RivalId).Name, r.Won ? "Victory" : "Defeat", r.Posture.ToString())).ToImmutableArray(),
            c.Reviews.TakeLast(24).Reverse().Select(r => $"Day {r.Day} · {r.Text} [cause: {r.CauseId}]").ToImmutableArray(),
            string.Join("; ", c.PendingEffects.Select(e => $"Day {e.DueDay}: reputation {e.Reputation:+#;-#;0}, audience {e.Audience:+#;-#;0}")), content.Label);
        var recommendation = CoachPolicy.Recommend(view);
        return view with { Recommendation = recommendation, RecommendationReason = recommendation is null ? "No match to prepare." :
            $"Coach uses observed readiness, opponent estimates and the public meta. Suggested {recommendation.Execution}/{recommendation.Opponent}/{recommendation.Meta}% allocation; {view.Confidence.ToLowerInvariant()}. Hidden opponent preparation and variance are unknown." };
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
