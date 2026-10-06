using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Absolute days form consecutive seasons; nothing ends the campaign by date.</summary>
public static class Seasons
{
    public static int Of(int day, Balance b) => (day - 1) / b.SeasonLength + 1;
    public static int Start(int season, Balance b) => (season - 1) * b.SeasonLength + 1;
    public static int End(int season, Balance b) => season * b.SeasonLength;
    public static int DayOf(int day, Balance b) => (day - 1) % b.SeasonLength + 1;
    public static string FixturePrefix(int season) => $"fixture:s{season}:";

    /// <summary>Each rival is met twice per season in a seeded order; the second half carries higher stakes.</summary>
    public static ImmutableArray<Fixture> Schedule(ulong seed, int season, ImmutableArray<Rival> rivals, Balance b)
    {
        var ids = rivals.Select(r => r.Id).Order(StringComparer.Ordinal).ToArray();
        var first = Generator.Shuffle(seed, $"schedule:{season}:first", ids);
        var second = Generator.Shuffle(seed, $"schedule:{season}:second", ids);
        var order = first.Concat(second).ToArray();
        var start = Start(season, b) + b.FirstMatchDay - 1;
        return order.Select((rival, k) =>
        {
            var importance = k >= order.Length / 2 ? 2 : 1;
            return new Fixture($"{FixturePrefix(season)}{k + 1:D2}", start + k * b.MatchInterval, rival, importance, b.PrizeBase * importance);
        }).ToImmutableArray();
    }
}

/// <summary>Seeded, order-independent generation of people and organizations from content pools.</summary>
public static class Generator
{
    public const string Roles = "ABCDE";
    public static int Roll(ulong seed, string domain, string key, string purpose, AttributeRange range) =>
        range.Min + KeyedRandom.Range(seed, domain, key, purpose, range.Max - range.Min + 1);

    public static T[] Shuffle<T>(ulong seed, string key, IReadOnlyList<T> items)
    {
        var result = items.ToArray();
        for (var i = result.Length - 1; i > 0; i--)
        {
            var j = KeyedRandom.Range(seed, "shuffle", key, "swap:" + i, i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    // Pools are sorted first so definition order never changes generated identities.
    public static string Name(ulong seed, NamePool pool, string key)
    {
        var given = pool.Given.Order(StringComparer.Ordinal).ToArray();
        var family = pool.Family.Order(StringComparer.Ordinal).ToArray();
        return given[KeyedRandom.Range(seed, "names", key, "given", given.Length)] + " " +
            family[KeyedRandom.Range(seed, "names", key, "family", family.Length)];
    }

    public static Person Player(ulong seed, Content content, string id, string role, AttributeRange talent)
    {
        int Attribute(string purpose) => Roll(seed, "player", id, purpose, talent);
        return new Person(id, "player:generated", Name(seed, content.Names, id), role,
            Attribute("execution"), Attribute("adaptability"), Attribute("consistency"),
            Roll(seed, "player", id, "readiness", new AttributeRange(80, 98)));
    }

    public static Coach Coach(ulong seed, Content content, string id, int skillShift = 0)
    {
        var g = content.Generation;
        int Skill(string purpose) => Math.Clamp(Roll(seed, "coach", id, purpose, g.CoachSkill) + skillShift, 1, 100);
        return new Coach(id, Name(seed, content.Names, id), Skill("preparation"), Skill("analysis"), Skill("adaptability"),
            Math.Clamp(Roll(seed, "coach", id, "capacity", g.CoachCapacity) + skillShift, 20, 1000));
    }

    public static ImmutableArray<Rival> Rivals(ulong seed, Content content)
    {
        var names = Shuffle(seed, "rivals", content.OrganizationNames.Order(StringComparer.Ordinal).ToArray());
        return Enumerable.Range(1, content.Balance.RivalCount).Select(i =>
        {
            var id = $"rival:r{i:D2}";
            int Skill(string purpose) => Roll(seed, "rival", id, purpose, content.Generation.Rival);
            return new Rival(id, names[i - 1], Skill("strength"), Skill("analysis"), Skill("adaptability"),
                (Posture)KeyedRandom.Range(seed, "rival", id, "posture", 3), 40, KeyedRandom.Range(seed, "rival", id, "need", 2));
        }).ToImmutableArray();
    }
}

/// <summary>Development salary curve; values are minor currency units per day.</summary>
public static class Pay
{
    public static long Player(int execution) => (long)execution * execution / 5;
    public static long Coach(Coach coach) => (coach.Preparation + coach.Analysis + coach.Adaptability) * 5L + coach.Capacity * 2L;
    /// <summary>A renewing person asks for at least their current pay, scaled by the company's standing.</summary>
    public static long Demand(long current, long market, int reputation) => Math.Max(current, Numbers.Divide(market * (100 + reputation / 5), 100));
    // Text stored in campaign state must not depend on the host culture.
    public static string Money(long minor) => (minor / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture) + " CU";
    public static string Count(long value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
}
