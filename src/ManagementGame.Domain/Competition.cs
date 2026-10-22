namespace ManagementGame.Domain;

// Pure resolver: receives competitive inputs only; cannot mutate finance or commercial state. RivalStrength is the rival's
// derived strength (Rivals.Strength), computed by the caller from its players.
public sealed record CompetitionInput(ulong Seed, Fixture Fixture, Plan Plan, Work Work,
    System.Collections.Immutable.ImmutableArray<Person> Lineup, System.Collections.Immutable.ImmutableArray<int> Ratings, Coach Coach, Rival Rival, Posture Meta,
    int RepeatedPosture, Balance Balance, int Familiarity, int RivalStrength);

/// <summary>Match resolution (rules v4). A series is best-of-n games. Each game's strength difference is a fixed part
/// (rating, preparation, matchup, adaptation, familiarity, leadership, style, composure) plus that game's keyed variance;
/// the logistic of the difference over <c>LogitScale</c> is the game win probability.</summary>
public static class Competition
{
    /// <summary>Each game's variance (±points). Wider variance favours the underdog: the logistic flattens toward even. A
    /// balanced posture plays at <c>Variance</c>; conservative is 4 narrower, aggressive 4 wider; a consistent lineup
    /// narrows it (a point per 10 of mean consistency above 50).</summary>
    public static int Spread(Balance b, Posture posture, int meanConsistency) => Math.Max(1, b.Variance + ((int)posture - 1) * 4 - (meanConsistency - 50) / 10);

    public static CompetitiveOutcome Resolve(CompetitionInput input)
    {
        var x = input; var b = x.Balance; var n = x.Lineup.Length;
        // Rating in role, times readiness and form (−10..+10 percent).
        var baseValue = (int)Numbers.Divide(x.Lineup.Select((p, i) => (long)x.Ratings[i] * p.Readiness * (100 + p.Form)).Sum(), n * 100L * 100);
        var opponentMatch = x.Plan.ExpectedOpponent == x.Rival.Posture ? 100 : 25;
        // Meta work pays more in a shifted meta, and as much more again as the head coach reads it (Adaptability, ±50%).
        var prep = (int)Numbers.Divide(x.Work.Execution + x.Work.Opponent * opponentMatch / 100
            + x.Work.Meta * (x.Meta == Posture.Balanced ? 70 : 120) / 100 * (50 + x.Coach.Adaptability) / 100, b.PreparationScale);
        var matchup = x.Plan.Posture == x.Meta ? 3 : -1;
        // Champion pools and the coach's meta reading; a rival with analysis punishes a repeated posture.
        var adaptation = (int)Numbers.Divide(x.Lineup.Sum(p => (long)p.Skills.ChampionPool) / n + x.Coach.Adaptability, 30)
            - x.RepeatedPosture * x.Rival.Coach.Analysis / 30;
        // Integration: 0..+4 from the lineup's mean familiarity.
        var familiarity = x.Familiarity / 25;
        // Someone must make the calls: the lineup's best communicator, a point per 5 above or below 80 (−3..+2). 80 is about
        // the median best communicator of a generated squad, so the term centres on zero.
        var caller = x.Lineup.Max(p => p.Mind.Communication);
        var leadership = Math.Clamp((int)Numbers.Divide(caller - 80L, 5), -3, 2);
        // Style: an early-game lineup suits an aggressive meta, a teamfight lineup a conservative one (±3).
        var early = x.Lineup.Sum(p => p.Skills.EarlyGame - p.Skills.Teamfight) / n;
        var style = Math.Clamp(early * (x.Meta == Posture.Aggressive ? 1 : x.Meta == Posture.Conservative ? -1 : 0) / 4, -3, 3);
        // Composure counts when the stakes are high: a point per 4 of the lineup's mean composure above or below 60 (±3).
        var composure = x.Fixture.Importance >= 2 ? Math.Clamp((int)Numbers.Divide(x.Lineup.Sum(p => (long)p.Mind.Composure) - 60L * n, 4L * n), -3, 3) : 0;
        var difference = baseValue - x.RivalStrength + prep + matchup + adaptation + familiarity + leadership + style + composure;
        var spread = Spread(b, x.Plan.Posture, x.Lineup.Sum(p => p.Mind.Consistency) / n);
        var (won, lost, firstVariance) = Play(x.Seed, "competition", x.Fixture.Id, difference, spread, x.Fixture.Games, b.LogitScale, b.SeriesForm);
        var probability = Logistic.Series(Logistic.Probability(Logistic.Logit(difference, b.LogitScale)), x.Fixture.Games);
        return new CompetitiveOutcome("result:" + x.Fixture.Id, x.Fixture.Id, x.Fixture.Day, x.Rival.Id, won > lost, x.Plan.Posture,
            probability, baseValue, prep, matchup, adaptation, firstVariance, x.Fixture.Importance, x.Plan.CauseId,
            familiarity, style, composure, leadership, won, lost);
    }

    /// <summary>Plays a best-of-<paramref name="games"/> series game by game until one side has won the majority. The day's
    /// form (one keyed draw within ±<paramref name="form"/>) is shared by every game; each game adds its own keyed variance
    /// and result draw. Returns games won and lost by the first side and game one's match-day variance (form included).</summary>
    public static (int Won, int Lost, int FirstVariance) Play(ulong seed, string domain, string fixtureId, int difference, int spread, int games, int logitScale, int form)
    {
        var need = games / 2 + 1; int won = 0, lost = 0, first = 0;
        var day = form <= 0 ? 0 : KeyedRandom.Range(seed, domain, fixtureId, "form", form * 2 + 1) - form;
        for (var g = 1; won < need && lost < need; g++)
        {
            var variance = day + KeyedRandom.Range(seed, domain, fixtureId, "variance:" + g, spread * 2 + 1) - spread;
            if (g == 1) first = variance;
            var probability = Logistic.Probability(Logistic.Logit(difference + variance, logitScale));
            if (KeyedRandom.Range(seed, domain, fixtureId, "result:" + g, 1_000_000) < probability) won++; else lost++;
        }
        return (won, lost, first);
    }

    /// <summary>A league match between two rivals, from their derived strengths alone (their preparation is in Edge).</summary>
    public static LeagueResult Rivals(ulong seed, LeagueFixture fixture, Rival home, Rival away, Content content)
    {
        var b = content.Balance;
        var (won, lost, _) = Play(seed, "league", fixture.Id, Domain.Rivals.Strength(home, content) - Domain.Rivals.Strength(away, content),
            Spread(b, Posture.Balanced, 50), fixture.Games, b.LogitScale, b.SeriesForm);
        return new LeagueResult("result:" + fixture.Id, fixture.Id, fixture.Day, home.Id, away.Id, won, lost);
    }
}

public static class Commercial
{
    /// <summary>A result's commercial follow-through. Reputation moves with how unexpected the result was, and every win adds
    /// <c>ReputationWinBase</c> × its importance. The audience moves more with a popular lineup: ×(1 + (the lineup's mean
    /// popularity − 50) / 200).</summary>
    public static Company Consume(Company c, CompetitiveOutcome result, Balance b, int popularity = 50)
    {
        if (c.CommercialReceipts.Contains(result.Id)) return c;
        var surprise = result.Won ? 1_000_000 - result.Probability : result.Probability;
        var rep = (int)Numbers.Divide((long)b.ReputationGain * result.Importance * surprise * (result.Won ? 100 - c.Reputation : c.Reputation), 100_000_000)
            + (result.Won ? b.ReputationWinBase * result.Importance : 0);
        var audience = (int)Numbers.Divide((long)b.AudienceGain * result.Importance * surprise * (150 + popularity), 200_000_000);
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
