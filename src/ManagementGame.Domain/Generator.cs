using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Seeded, order-independent generation of people and organizations from content tables (rules v4). Every draw is
/// keyed by the entity ID and a purpose, so one draw never shifts another and the same seed regenerates the same world.
/// Pools are sorted before use, so content order never changes a generated identity.</summary>
public static class Generator
{
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

    /// <summary>One item by weight; items keep their given order, so callers pass a deterministic order.</summary>
    public static T Weighted<T>(ulong seed, string domain, string key, string purpose, IReadOnlyList<T> items, Func<T, int> weight)
    {
        var total = items.Sum(weight);
        if (total <= 0) throw new RuleViolation("Generation table has no weight.");
        var roll = KeyedRandom.Range(seed, domain, key, purpose, total);
        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll < 0) return item;
        }
        return items[^1];
    }
    private static string Pick(ulong seed, string domain, string key, string purpose, ImmutableArray<string> pool)
    {
        var sorted = pool.Order(StringComparer.Ordinal).ToArray();
        return sorted[KeyedRandom.Range(seed, domain, key, purpose, sorted.Length)];
    }

    /// <summary>A nationality by weight and a name from its pools.</summary>
    public static (string Nationality, string Name) Identity(ulong seed, Content content, string id)
    {
        var nationality = Weighted(seed, "names", id, "nationality", content.Nationalities.OrderBy(n => n.Id, StringComparer.Ordinal).ToArray(), n => n.Weight);
        var given = Pick(seed, "names", id, "given", nationality.Given);
        var family = Pick(seed, "names", id, "family", nationality.Family);
        return (nationality.Id, nationality.FamilyFirst ? family + " " + given : given + " " + family);
    }
    public static string Handle(ulong seed, Content content, string id) =>
        Pick(seed, "names", id, "handle-prefix", content.Handles.Prefixes) + Pick(seed, "names", id, "handle-suffix", content.Handles.Suffixes);

    private static AgeProfile Profile(Generation g, int age) =>
        g.AgeProfiles.OrderBy(a => a.MaxAge).FirstOrDefault(a => age <= a.MaxAge) ?? g.AgeProfiles.MaxBy(a => a.MaxAge)!;

    /// <summary>A player of a role. Age from the age table (or <paramref name="age"/>); ability band by weight, shifted by
    /// age and <paramref name="bandShift"/>; skills = band + archetype offset + keyed jitter; potential = rating + the age's
    /// headroom; popularity follows rating and prime age.</summary>
    public static Person Player(ulong seed, Content content, string id, string role, int season, int? age = null, int bandShift = 0)
    {
        var g = content.Generation;
        var (nationality, name) = Identity(seed, content, id);
        var years = age ?? Weighted(seed, "player", id, "age", g.Ages.OrderBy(a => a.Value).ToArray(), a => a.Weight).Value;
        var profile = Profile(g, years);
        var band = Weighted(seed, "player", id, "band", g.AbilityBands.OrderBy(b => b.Value).ToArray(), b => b.Weight).Value + profile.Shift + bandShift;
        var archetype = Weighted(seed, "player", id, "archetype", g.Archetypes.Where(a => a.Roles.Contains(role)).OrderBy(a => a.Id, StringComparer.Ordinal).ToArray(), a => a.Weight);
        int Skill(int i) => Math.Clamp(band + archetype.Offsets[i] + KeyedRandom.Range(seed, "player", id, "skill:" + i, g.SkillJitter * 2 + 1) - g.SkillJitter, 1, 99);
        int Trait(string purpose, AttributeRange range, int offset = 0) => Math.Clamp(Roll(seed, "player", id, purpose, range) + offset, 1, 99);
        var others = Roles.All.Where(r => r != role).ToArray();
        var secondary = KeyedRandom.Range(seed, "player", id, "secondary", 10_000) < g.SecondaryRoleChance
            ? others[KeyedRandom.Range(seed, "player", id, "secondary-role", others.Length)] : "";
        var person = new Person(id, "player:generated", name, Handle(seed, content, id), nationality, season - years, role, secondary,
            new Skills(Skill(0), Skill(1), Skill(2), Skill(3), Skill(4)),
            new Mind(Trait("consistency", g.Mind), Trait("composure", g.Mind), Trait("communication", g.Mind, archetype.Communication),
                Trait("professionalism", g.Personality), Trait("ambition", g.Personality), Trait("loyalty", g.Personality)),
            0, Roll(seed, "player", id, "readiness", new AttributeRange(80, 98)), 0, Roll(seed, "player", id, "morale", new AttributeRange(50, 70)), 0);
        var rating = Ratings.Rating(person, content);
        var potential = Math.Clamp(rating + Roll(seed, "player", id, "potential", new AttributeRange(profile.HeadroomMin, profile.HeadroomMax)), rating, 99);
        var prime = years is >= 22 and <= 27 ? 5 : 0;
        var popularity = Math.Clamp((rating - 50) * 3 / 2 + prime + KeyedRandom.Range(seed, "player", id, "popularity", 17) - 8, 1, 99);
        return person with { Potential = potential, Popularity = popularity };
    }

    public static Coach Coach(ulong seed, Content content, string id, int skillShift = 0)
    {
        var g = content.Generation;
        int Skill(string purpose) => Math.Clamp(Roll(seed, "coach", id, purpose, g.CoachSkill) + skillShift, 1, 100);
        return new Coach(id, Identity(seed, content, id).Name, Skill("preparation"), Skill("analysis"), Skill("adaptability"),
            Math.Clamp(Roll(seed, "coach", id, "capacity", g.CoachCapacity) + skillShift, 20, 1000), Skill("development"));
    }
}
