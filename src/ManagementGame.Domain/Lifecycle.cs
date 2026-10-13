using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>The player lifecycle (rules v4 phase 4): development and retirement at season end, youth intake into a free-agent
/// pool that persists, and the condition that matches and conduct change during a season (form, morale, popularity).
/// Every draw is keyed by the person and the season or day, so the same seed tells the same careers.</summary>
public static class Careers
{
    public const string DevelopmentPrefix = Market.NoticePrefix + "development:", ConductPrefix = Market.NoticePrefix + "conduct:";

    // ---------- development ----------
    /// <summary>The season's change of a person's primary-role rating, before the potential ceiling: a keyed draw in the age's
    /// band, then scaled. Growth scales with professionalism, the head coach's development and the share of the season's
    /// matches played (each ±25 percentage points around a neutral 50); professionalism also slows decline.</summary>
    public static int Change(ulong seed, Person p, int season, Lifecycle life, int coachDevelopment, int playedPercent)
    {
        var age = Ratings.Age(p, season);
        var band = life.Development.OrderBy(x => x.MaxAge).FirstOrDefault(x => age <= x.MaxAge) ?? life.Development.MaxBy(x => x.MaxAge)!;
        var draw = band.Min + KeyedRandom.Range(seed, "development", $"{season}:{p.Id}", "change", band.Max - band.Min + 1);
        var factor = draw >= 0 ? 100 + (p.Mind.Professionalism - 50) / 2 + (coachDevelopment - 50) / 2 + (playedPercent - 50) / 2
            : 100 - (p.Mind.Professionalism - 50) / 2;
        return (int)Numbers.Divide((long)draw * Math.Max(0, factor), 100);
    }

    /// <summary>A rating change applied through the skills: every skill moves by it, so the role rating moves by about as
    /// much. Growth never takes the primary-role rating above the potential ceiling; decline has no floor but 1.</summary>
    public static Person Develop(Person p, int change, Content content)
    {
        Person Shifted(int d) => p with { Skills = new Skills(Skill(p.Skills.Mechanics + d), Skill(p.Skills.EarlyGame + d), Skill(p.Skills.Teamfight + d),
            Skill(p.Skills.Macro + d), Skill(p.Skills.ChampionPool + d)) };
        if (change < 0) return Shifted(change);
        for (var d = change; d > 0; d--)
            if (Shifted(d) is var next && Ratings.Rating(next, content) <= p.Potential) return next;
        return p;
    }
    private static int Skill(int value) => Math.Clamp(value, 1, 99);

    /// <summary>Season end for the company: its players develop with their playing time and the head coach, the season's
    /// match counts restart, and one notice says how each player's rating changed.</summary>
    public static Company SeasonDevelopment(Company c, ulong seed, Content content, int season, int day, int matches)
    {
        var changes = new List<string>();
        var people = c.People.Select(p =>
        {
            var played = c.Employment.Single(e => e.PersonId == p.Id).Played;
            var before = Ratings.Rating(p, content);
            var next = Develop(p, Change(seed, p, season, content.Lifecycle, c.Coach.Development, matches == 0 ? 0 : Math.Min(100, played * 100 / matches)), content);
            var after = Ratings.Rating(next, content);
            if (after != before) changes.Add($"{p.Handle} {after - before:+#;-#} to {after}");
            return next;
        }).ToImmutableArray();
        c = c with { People = people, Employment = [.. c.Employment.Select(e => e with { Played = 0 })] };
        return changes.Count == 0 ? c : c with { Reviews = c.Reviews.Add(new ReviewEntry($"{DevelopmentPrefix}s{season}:{day}", day, $"season:{season}",
            $"Season {season} development: {string.Join(", ", changes)}. Age, professionalism, the head coach and playing time shape it.")) };
    }

    // ---------- retirement and the open market ----------
    /// <summary>The chance, in basis points, that a player of this age and rating retires this season: the calibrated table by
    /// age, scaled <c>RetirementRatingPercent</c> percent per point of rating below (more) or above (less) the pivot, between
    /// a quarter and three times. At the table's last age everyone retires.</summary>
    public static int RetirementChance(Person p, int age, Content content)
    {
        var life = content.Lifecycle; var table = life.RetirementByAge;
        if (age >= table.Max(x => x.Value)) return 10_000;
        if (age < table.Min(x => x.Value)) return 0;
        var basis = table.Where(x => x.Value <= age).MaxBy(x => x.Value)!.Weight;
        var scale = Math.Clamp(100 + (life.RetirementPivot - Ratings.Rating(p, content)) * life.RetirementRatingPercent, 25, 300);
        return (int)Math.Min(10_000, Numbers.Divide((long)basis * scale, 100));
    }
    public static bool Retires(ulong seed, Person p, int age, Content content, string occasion) =>
        KeyedRandom.Range(seed, "retirement", $"{occasion}:{p.Id}", "retire", 10_000) < RetirementChance(p, age, content);

    /// <summary>A free agent's listing: the market's asking terms for the person as they are now.</summary>
    public static Candidate Listing(Person p, int season, Content content)
    {
        var salary = Pay.Player(p, season, content);
        return new Candidate(p, Pay.Fee(p, season, content), salary, salary * 3);
    }

    /// <summary>Season end on the open market: free agents develop (without a coach or matches) and age; some retire; the
    /// season's youth intake arrives, one role after another; above the cap, the oldest retire (the least potential first
    /// among equals), so the cap sheds veterans rather than selecting for ability; and every listing follows its player.
    /// Retirements are recorded moves from the open market.</summary>
    public static World SeasonMarket(World w, ulong seed, Content content, int season, int day)
    {
        var life = content.Lifecycle; var next = season + 1; var cause = $"retirement:s{season}";
        World Retire(World world, Candidate x) => Market.Moved(world with { Candidates = world.Candidates.Remove(x) }, day, cause, x.Person, MoveReason.Retired, null, null);
        w = w with { Candidates = [.. w.Candidates.Select(x => x with { Person = Develop(x.Person, Change(seed, x.Person, season, life, 50, 0), content) })] };
        foreach (var x in w.Candidates.OrderBy(x => x.Person.Id, StringComparer.Ordinal).ToArray())
            if (Retires(seed, x.Person, Ratings.Age(x.Person, next), content, $"s{season}")) w = Retire(w, x);
        var id = w.NextId;
        var first = KeyedRandom.Range(seed, "intake", $"s{season}", "first-role", Roles.All.Length);
        var ages = life.IntakeAges.OrderBy(a => a.Value).ToArray();
        var intake = Enumerable.Range(0, life.YouthIntake).Select(i =>
        {
            var personId = $"person:g{++id:D5}";
            var age = Generator.Weighted(seed, "intake", personId, "age", ages, a => a.Weight).Value;
            return Listing(Generator.Player(seed, content, personId, Roles.All[(first + i) % Roles.All.Length], next, age), next, content);
        }).ToArray();
        w = w with { Candidates = w.Candidates.AddRange(intake), NextId = id };
        foreach (var x in w.Candidates.OrderByDescending(x => Ratings.Age(x.Person, next)).ThenBy(x => x.Person.Potential).ThenBy(x => x.Person.Id, StringComparer.Ordinal)
            .Take(Math.Max(0, w.Candidates.Length - life.PoolCap)).ToArray())
            w = Retire(w, x);
        return w with { Candidates = [.. w.Candidates.Select(x => Listing(x.Person, next, content))] };
    }

    // ---------- condition during the season ----------
    /// <summary>After a match: the lineup's form moves with the result, more when it was unexpected; its morale with the
    /// result; its popularity with a win, more in a bigger match. Players left out lose morale and a little popularity.</summary>
    public static Company AfterMatch(Company c, CompetitiveOutcome outcome, ImmutableArray<string> lineup, Lifecycle life)
    {
        var surprise = outcome.Won ? 1_000_000 - outcome.Probability : outcome.Probability;
        var form = life.FormStep + (int)Numbers.Divide((long)life.FormSurprise * surprise, 1_000_000);
        return c with { People = [.. c.People.Select(p => lineup.Contains(p.Id)
            ? p with { Form = Math.Clamp(p.Form + (outcome.Won ? form : -form), -10, 10), Morale = Math.Clamp(p.Morale + (outcome.Won ? life.MoraleWin : -life.MoraleLoss), 0, 100),
                Popularity = outcome.Won ? Math.Min(100, p.Popularity + life.PopularityWin * outcome.Importance) : p.Popularity }
            : p with { Morale = Math.Max(0, p.Morale - life.MoraleBench), Popularity = Math.Max(0, p.Popularity - life.PopularityBench) })] };
    }

    /// <summary>Every <c>DecayDays</c>: form moves one step back toward steady, morale settles toward its resting level.</summary>
    public static Company Settle(Company c, Lifecycle life) => c with { People = [.. c.People.Select(p => p with
    {
        Form = p.Form - Math.Sign(p.Form),
        Morale = p.Morale < life.MoraleRest ? Math.Min(life.MoraleRest, p.Morale + life.MoraleSettle) : Math.Max(life.MoraleRest, p.Morale - life.MoraleSettle)
    })] };

    /// <summary>A player whose morale is low recovers readiness at half speed (on even days only).</summary>
    public static bool Recovers(Person p, Lifecycle life, int day) => p.Morale >= life.MoraleLow || day % 2 == 0;

    /// <summary>A player who will not renew: low morale and enough ambition to look elsewhere.</summary>
    public static bool RefusesRenewal(Person p, Lifecycle life) => p.Morale < life.MoraleLow && p.Mind.Ambition >= life.RefusalAmbition;

    /// <summary>Every <c>ConductInterval</c> days each player may break team rules, more likely the less professional they are:
    /// the club's reputation, the player's fans and morale pay, and the company is told.</summary>
    public static Company Conduct(Company c, ulong seed, Lifecycle life, int day)
    {
        foreach (var id in c.People.Select(p => p.Id).Order(StringComparer.Ordinal).ToArray())
        {
            var p = c.People.Single(x => x.Id == id);
            var chance = Math.Max(0, life.ConductThreshold - p.Mind.Professionalism) * life.ConductRate;
            if (KeyedRandom.Range(seed, "conduct", $"{day}:{id}", "incident", 10_000) >= chance) continue;
            c = c with
            {
                People = c.People.Replace(p, p with { Popularity = Math.Max(0, p.Popularity - life.ConductPopularity), Morale = Math.Max(0, p.Morale - life.ConductMorale) }),
                Reputation = Math.Max(0, c.Reputation - life.ConductReputation),
                Reviews = c.Reviews.Add(new ReviewEntry($"{ConductPrefix}{id}:{day}", day, id,
                    $"{p.Handle} was disciplined for breaking team rules. Reputation −{life.ConductReputation}; fans cooled (popularity −{life.ConductPopularity})."))
            };
        }
        return c;
    }

    /// <summary>The sponsors' star bonus in percent: <c>StarBonusPercent</c> for each player at <c>StarPopularity</c> or above,
    /// for at most <c>StarBonusCap</c> of them.</summary>
    public static int StarBonus(Company c, Lifecycle life) => Math.Min(life.StarBonusCap, c.People.Count(p => p.Popularity >= life.StarPopularity)) * life.StarBonusPercent;

    /// <summary>The direction a player's rating is expected to take from their age alone, as the public reads it.</summary>
    public static string Trend(Person p, int season, Lifecycle life)
    {
        var age = Ratings.Age(p, season);
        var band = life.Development.OrderBy(x => x.MaxAge).FirstOrDefault(x => age <= x.MaxAge) ?? life.Development.MaxBy(x => x.MaxAge)!;
        return band.Min + band.Max > 0 ? "Improving" : band.Min + band.Max < 0 ? "Declining" : "At peak";
    }
}
