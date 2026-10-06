namespace ManagementGame.Domain;

// Pure resolver: receives competitive inputs only; cannot mutate finance or commercial state.
public sealed record CompetitionInput(ulong Seed, Fixture Fixture, Plan Plan, Work Work,
    System.Collections.Immutable.ImmutableArray<Person> Lineup, Coach Coach, Rival Rival, Posture Meta,
    int RepeatedPosture, Balance Balance);

public static class Competition
{
    public static CompetitiveOutcome Resolve(CompetitionInput input)
    {
        var x = input;
        var baseValue = (int)Numbers.Divide(x.Lineup.Sum(p => (long)p.Execution * p.Readiness), x.Lineup.Length * 100);
        var opponentMatch = x.Plan.ExpectedOpponent == x.Rival.Posture ? 100 : 25;
        var prep = (int)Numbers.Divide(x.Work.Execution + x.Work.Opponent * opponentMatch / 100
            + x.Work.Meta * (x.Meta == Posture.Balanced ? 70 : 120) / 100, x.Balance.PreparationScale);
        var matchup = x.Plan.Posture == x.Meta ? 3 : -1;
        var adaptation = (int)Numbers.Divide(x.Lineup.Sum(p => (long)p.Adaptability) / x.Lineup.Length + x.Coach.Adaptability, 30)
            - x.RepeatedPosture * x.Rival.Analysis / 30;
        var consistency = x.Lineup.Sum(p => p.Consistency) / x.Lineup.Length;
        var spread = Math.Max(1, x.Balance.Variance + (int)x.Plan.Posture * 4 - consistency / 20);
        var variance = KeyedRandom.Range(x.Seed, "competition", x.Fixture.Id, "variance", spread * 2 + 1) - spread;
        var probability = Math.Clamp(500_000 + (baseValue - x.Rival.Strength + prep + matchup + adaptation + variance) * x.Balance.MatchScale, 50_000, 950_000);
        var won = KeyedRandom.Range(x.Seed, "competition", x.Fixture.Id, "result", 1_000_000) < probability;
        return new CompetitiveOutcome("result:" + x.Fixture.Id, x.Fixture.Id, x.Fixture.Day, x.Rival.Id, won, x.Plan.Posture,
            probability, baseValue, prep, matchup, adaptation, variance, x.Fixture.Importance, x.Plan.CauseId);
    }
}

public static class Commercial
{
    public static Company Consume(Company c, CompetitiveOutcome result, Balance b)
    {
        if (c.CommercialReceipts.Contains(result.Id)) return c;
        var surprise = result.Won ? 1_000_000 - result.Probability : result.Probability;
        var rep = (int)Numbers.Divide((long)b.ReputationGain * result.Importance * surprise * (result.Won ? 100 - c.Reputation : c.Reputation), 100_000_000);
        var audience = (int)Numbers.Divide((long)b.AudienceGain * result.Importance * surprise, 1_000_000);
        var effect = new CommercialEffect("commercial:" + result.Id, result.Id, result.Day + 1, result.Won ? rep : -rep, result.Won ? audience : -audience);
        return c with { PendingEffects = c.PendingEffects.Add(effect), CommercialReceipts = c.CommercialReceipts.Add(result.Id) };
    }
    public static Company ApplyDue(Company c, int day)
    {
        foreach (var effect in c.PendingEffects.Where(e => e.DueDay <= day).OrderBy(e => e.Id, StringComparer.Ordinal))
            c = c with { Reputation = Math.Clamp(c.Reputation + effect.Reputation, 0, 100), Audience = Math.Max(0, checked(c.Audience + effect.Audience)),
                PendingEffects = c.PendingEffects.Remove(effect), Reviews = c.Reviews.Add(new ReviewEntry(effect.Id, day, effect.CauseId,
                    $"Commercial follow-through: reputation {effect.Reputation:+#;-#;0}, audience {effect.Audience:+#;-#;0}. Offers still require signed terms; this creates no cash.")) };
        return c;
    }
}
