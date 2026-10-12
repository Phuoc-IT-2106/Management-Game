namespace ManagementGame.Domain;

// Pure resolver: receives competitive inputs only; cannot mutate finance or commercial state.
public sealed record CompetitionInput(ulong Seed, Fixture Fixture, Plan Plan, Work Work,
    System.Collections.Immutable.ImmutableArray<Person> Lineup, System.Collections.Immutable.ImmutableArray<int> Ratings, Coach Coach, Rival Rival, Posture Meta,
    int RepeatedPosture, Balance Balance, int Familiarity);

/// <summary>Match resolution (rules v4). A series is best-of-n games. Each game's strength difference is a fixed part
/// (rating, preparation, matchup, adaptation, familiarity, leadership, style, composure) plus that game's keyed variance;
/// the logistic of the difference over <c>LogitScale</c> is the game win probability.</summary>
public static class Competition
{
    /// <summary>Wider variance favours the underdog: the logistic flattens toward even. Consistency narrows it, an aggressive
    /// posture widens it.</summary>
    public static int Spread(Balance b, Posture posture, int meanConsistency) => Math.Max(1, b.Variance + (int)posture * 4 - (meanConsistency - 50) / 10);

    public static CompetitiveOutcome Resolve(CompetitionInput input)
    {
        var x = input; var b = x.Balance; var n = x.Lineup.Length;
        // Rating in role, times readiness and form (−10..+10 percent).
        var baseValue = (int)Numbers.Divide(x.Lineup.Select((p, i) => (long)x.Ratings[i] * p.Readiness * (100 + p.Form)).Sum(), n * 100L * 100);
        var opponentMatch = x.Plan.ExpectedOpponent == x.Rival.Posture ? 100 : 25;
        var prep = (int)Numbers.Divide(x.Work.Execution + x.Work.Opponent * opponentMatch / 100
            + x.Work.Meta * (x.Meta == Posture.Balanced ? 70 : 120) / 100, b.PreparationScale);
        var matchup = x.Plan.Posture == x.Meta ? 3 : -1;
        // Champion pools and the coach's meta reading; a rival with analysis punishes a repeated posture.
        var adaptation = (int)Numbers.Divide(x.Lineup.Sum(p => (long)p.Skills.ChampionPool) / n + x.Coach.Adaptability, 30)
            - x.RepeatedPosture * x.Rival.Analysis / 30;
        // Integration: 0..+4 from the lineup's mean familiarity.
        var familiarity = x.Familiarity / 25;
        // Someone must make the calls: below 60 communication the team loses up to 3.
        var caller = x.Lineup.Max(p => p.Mind.Communication);
        var leadership = caller >= 60 ? 0 : -Math.Min(3, (60 - caller + 9) / 10);
        // Style: an early-game lineup suits an aggressive meta, a teamfight lineup a conservative one (±3).
        var early = x.Lineup.Sum(p => p.Skills.EarlyGame - p.Skills.Teamfight) / n;
        var style = Math.Clamp(early * (x.Meta == Posture.Aggressive ? 1 : x.Meta == Posture.Conservative ? -1 : 0) / 4, -3, 3);
        // Composure counts when the stakes are high (±2).
        var composure = x.Fixture.Importance >= 2 ? Math.Clamp((x.Lineup.Sum(p => p.Mind.Composure) / n - 50) / 12, -2, 2) : 0;
        var difference = baseValue - x.Rival.Strength + prep + matchup + adaptation + familiarity + leadership + style + composure;
        var spread = Spread(b, x.Plan.Posture, x.Lineup.Sum(p => p.Mind.Consistency) / n);
        var (won, lost, firstVariance) = Play(x.Seed, "competition", x.Fixture.Id, difference, spread, x.Fixture.Games, b.LogitScale);
        var probability = Logistic.Series(Logistic.Probability(Logistic.Logit(difference, b.LogitScale)), x.Fixture.Games);
        return new CompetitiveOutcome("result:" + x.Fixture.Id, x.Fixture.Id, x.Fixture.Day, x.Rival.Id, won > lost, x.Plan.Posture,
            probability, baseValue, prep, matchup, adaptation, firstVariance, x.Fixture.Importance, x.Plan.CauseId,
            familiarity, style, composure, leadership, won, lost);
    }

    /// <summary>Plays a best-of-<paramref name="games"/> series game by game, each with its own keyed variance and result
    /// draw, until one side has won the majority. Returns games won and lost by the first side and game one's variance.</summary>
    public static (int Won, int Lost, int FirstVariance) Play(ulong seed, string domain, string fixtureId, int difference, int spread, int games, int logitScale)
    {
        var need = games / 2 + 1; int won = 0, lost = 0, first = 0;
        for (var g = 1; won < need && lost < need; g++)
        {
            var variance = KeyedRandom.Range(seed, domain, fixtureId, "variance:" + g, spread * 2 + 1) - spread;
            if (g == 1) first = variance;
            var probability = Logistic.Probability(Logistic.Logit(difference + variance, logitScale));
            if (KeyedRandom.Range(seed, domain, fixtureId, "result:" + g, 1_000_000) < probability) won++; else lost++;
        }
        return (won, lost, first);
    }

    /// <summary>A league match between two rivals, from their strengths alone (compressed rivals have no preparation).</summary>
    public static LeagueResult Rivals(ulong seed, LeagueFixture fixture, Rival home, Rival away, Balance b)
    {
        var (won, lost, _) = Play(seed, "league", fixture.Id, home.Strength - away.Strength, Spread(b, Posture.Balanced, 50), fixture.Games, b.LogitScale);
        return new LeagueResult("result:" + fixture.Id, fixture.Id, fixture.Day, home.Id, away.Id, won, lost);
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
