using System.Text;
using System.Text.Json;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>Aggregate calibration (data/world/npc_calibration_2026.json, written by world_calibration.py): Overall quantiles
/// and age histograms per PandaScore tier. No person-level data.</summary>
public sealed class Calibration
{
    public required Dictionary<string, double[]> Quantiles { get; init; }   // tier -> q1..q99 (index p - 1)
    public required Dictionary<string, Dictionary<int, int>> Ages { get; init; }
    public required string Version { get; init; }
    public required string RatingModel { get; init; }

    public static Calibration Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = doc.RootElement;
        var quantiles = new Dictionary<string, double[]>(StringComparer.Ordinal);
        var ages = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);
        foreach (var tier in root.GetProperty("tiers").EnumerateObject())
        {
            var q = tier.Value.GetProperty("overall_quantiles");
            quantiles[tier.Name] = Enumerable.Range(1, 99).Select(p => q.GetProperty(p.ToString()).GetDouble()).ToArray();
            ages[tier.Name] = tier.Value.GetProperty("age_histogram").EnumerateObject().ToDictionary(x => int.Parse(x.Name), x => x.Value.GetInt32());
        }
        return new Calibration { Quantiles = quantiles, Ages = ages, Version = root.GetProperty("version").GetString()!,
            RatingModel = root.GetProperty("rating_model_version").GetString()! };
    }

    /// <summary>The quantile function of a tier's Overall, linear between percentiles and flat beyond p1 and p99.</summary>
    public double Q(string tier, double u)
    {
        var q = Quantiles[tier];
        var x = Math.Clamp(u * 100, 1, 99);
        var lo = (int)Math.Floor(x);
        var hi = Math.Min(99, lo + 1);
        return q[lo - 1] + (q[hi - 1] - q[lo - 1]) * (x - lo);
    }

    /// <summary>A draw from the tier's distribution, shifted, truncated to [min, max] by rescaling the uniform (no spike at
    /// the bounds): u is mapped into the quantile range that lands inside the bounds.</summary>
    public double Truncated(string tier, double u, double shift, double min, double max)
    {
        double Find(double target)
        {
            double a = 0, b = 1;
            for (var i = 0; i < 40; i++) { var m = (a + b) / 2; if (Q(tier, m) + shift < target) a = m; else b = m; }
            return (a + b) / 2;
        }
        var uMin = Q(tier, 0) + shift >= min ? 0 : Find(min);
        var uMax = Q(tier, 1) + shift <= max ? 1 : Find(max);
        return Math.Clamp(Q(tier, uMin + u * (uMax - uMin)) + shift, min, max);
    }
}

/// <summary>Fictional NPC generation for the experiment. Identity draws reuse the production name pools
/// (<see cref="Generator.Identity"/>); handles come from a larger syllable grammar and are unique among all NPCs and
/// distinct from every real handle in the staging data; ability comes from the aggregate calibration. Nationality never
/// enters an ability, trait or potential draw.</summary>
public static class NpcGenerator
{
    private static readonly string[] Onsets = ["b", "d", "f", "g", "h", "k", "l", "m", "n", "p", "r", "s", "t", "v", "z", "br", "dr", "gr",
        "kr", "tr", "st", "sk", "sh", "ch", "th", "vr", "zy", "ny", "ly", "ky", "j", "w", "x", "q"];
    private static readonly string[] Vowels = ["a", "e", "i", "o", "u", "ae", "ai", "ei", "io", "ou", "y", "ia"];
    private static readonly string[] Codas = ["n", "r", "s", "x", "k", "l", "m", "th", "rn", "sk", "z", "nd"];

    public static string Normalize(string handle)
    {
        var sb = new StringBuilder();
        foreach (var ch in handle.ToLowerInvariant()) if (ch is >= 'a' and <= 'z' or >= '0' and <= '9') sb.Append(ch);
        return sb.ToString();
    }

    private static string Candidate(World w, string id, int attempt)
    {
        var seed = w.Seed; var key = $"{id}:{attempt}";
        string Pick(string[] pool, string purpose) => pool[KeyedRandom.Range(seed, "xhandle", key, purpose, pool.Length)];
        var style = Draw.Int(seed, "xhandle", key, "style", 0, 99);
        var sb = new StringBuilder();
        if (style < 20)
        {
            var h = w.Content.Handles;
            sb.Append(h.Prefixes[KeyedRandom.Range(seed, "xhandle", key, "prefix", h.Prefixes.Length)]);
            sb.Append(h.Suffixes[KeyedRandom.Range(seed, "xhandle", key, "suffix", h.Suffixes.Length)]);
            sb.Append(Pick(Onsets, "tail-onset")).Append(Pick(Vowels, "tail-vowel"));
        }
        else
        {
            var syllables = style < 35 ? 1 : style < 80 ? 2 : 3;
            for (var s = 0; s < syllables; s++) sb.Append(Pick(Onsets, "onset" + s)).Append(Pick(Vowels, "vowel" + s));
            if (syllables == 1 || Draw.Chance(seed, "xhandle", key, "coda?", 0.5)) sb.Append(Pick(Codas, "coda"));
        }
        var text = sb.ToString();
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    public static string UniqueHandle(World w, string id)
    {
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var handle = Candidate(w, id, attempt);
            var n = Normalize(handle);
            if (n.Length is < 3 or > 12 || w.UsedHandles.Contains(n) || w.RealHandles.Contains(n)
                || w.Config.Population.HandleBlocklist.Any(b => n.Contains(b, StringComparison.Ordinal))) continue;
            w.UsedHandles.Add(n);
            return handle;
        }
        throw new InvalidOperationException("No unique handle for " + id);
    }

    public static string UniqueTeamName(World w, string key)
    {
        var t = w.Config.World.TeamNames;
        for (var attempt = 0; attempt < 500; attempt++)
        {
            var k = $"{key}:{attempt}";
            var adj = t.Adjectives[KeyedRandom.Range(w.Seed, "xteam", k, "adj", t.Adjectives.Length)];
            var noun = t.Nouns[KeyedRandom.Range(w.Seed, "xteam", k, "noun", t.Nouns.Length)];
            var suffix = t.Suffixes[KeyedRandom.Range(w.Seed, "xteam", k, "suffix", t.Suffixes.Length)];
            var name = suffix.Length == 0 ? $"{adj} {noun}" : $"{adj} {noun} {suffix}";
            var n = Normalize(name);
            var core = Normalize($"{adj} {noun}");
            if (w.UsedTeamNames.Contains(core) || w.RealTeamNames.Contains(n) || w.RealTeamNames.Contains(core)) continue;
            w.UsedTeamNames.Add(core);
            return name;
        }
        throw new InvalidOperationException("No unique team name for " + key);
    }

    private static int AgeFrom(World w, Calibration cal, ProfileConfig profile, string id)
    {
        var pop = w.Config.Population;
        if (profile.AgeTier.Length == 0 || !cal.Ages.ContainsKey(profile.AgeTier))
            return Draw.Weighted(w.Seed, "xgen", id, "age", pop.ProspectAges, a => a.Weight).Value;
        var hist = cal.Ages[profile.AgeTier].Where(x => x.Key >= profile.AgeMin && x.Key <= profile.AgeMax).OrderBy(x => x.Key).ToArray();
        return Draw.Weighted(w.Seed, "xgen", id, "age", hist, x => x.Value).Key;
    }

    /// <summary>One fictional player. The Overall is drawn from the profile's calibrated tier mixture, shifted down for young
    /// ages and truncated to [floor, cap]; skills = the level plus the archetype profile centred by the role weights (so
    /// the profile does not move the rating); the Overall is then recomputed with the production formula and held at the
    /// cap. Potential = min(ceiling, Overall + remaining capacity), ceiling = Overall + age headroom x talent.</summary>
    public static WPerson Create(World w, Calibration cal, string id, string role, ProfileConfig profile, int season, int? fixedAge = null,
        TierWeight[]? tiers = null)
    {
        var seed = w.Seed; var content = w.Content; var pop = w.Config.Population; var d = w.Config.Development;
        var (nationality, name) = Generator.Identity(seed, content, id);
        var handle = UniqueHandle(w, id);
        var age = fixedAge ?? AgeFrom(w, cal, profile, id);
        var tier = Draw.Weighted(seed, "xgen", id, "tier", tiers ?? profile.Tiers, t => t.Weight).Tier;
        var shift = pop.YoungShift.FirstOrDefault(x => x.Value == age)?.Weight ?? 0;
        var cap = pop.InitialOverallCap; var floor = pop.OverallFloor;
        var level = cal.Truncated(tier, Draw.Unit(seed, "xgen", id, "level"), shift, floor, cap);

        var g = content.Generation;
        var archetype = Draw.Weighted(seed, "xgen", id, "archetype",
            g.Archetypes.Where(a => a.Roles.Contains(role)).OrderBy(a => a.Id, StringComparer.Ordinal).ToArray(), a => a.Weight);
        var weights = content.RoleWeights.Single(x => x.Role == role).Weights.Select(x => x / 10_000.0).ToArray();
        var raw = Enumerable.Range(0, 5).Select(i => archetype.Offsets[i] + (double)Draw.Int(seed, "xgen", id, "jitter" + i, -g.SkillJitter, g.SkillJitter)).ToArray();
        var centre = Enumerable.Range(0, 5).Sum(i => weights[i] * raw[i]);
        var profileOffsets = raw.Select(x => x - centre).ToArray();
        var skills = profileOffsets.Select(x => Math.Clamp((int)Math.Round(level + x, MidpointRounding.ToEven), 1, 99)).ToArray();

        int Trait(string purpose, AttributeRange range, int offset = 0) => Math.Clamp(Generator.Bell(seed, "xgen", id, purpose, range) + offset, 1, 99);
        var others = Roles.All.Where(r => r != role).ToArray();
        var secondary = KeyedRandom.Range(seed, "xgen", id, "secondary", 10_000) < g.SecondaryRoleChance
            ? others[KeyedRandom.Range(seed, "xgen", id, "secondary-role", others.Length)] : "";
        var core = new Person(id, "player:generated:experiment", name, handle, nationality, season - age, role, secondary,
            new Skills(skills[0], skills[1], skills[2], skills[3], skills[4]),
            new Mind(Trait("consistency", g.Mind), Trait("composure", g.Mind), Trait("communication", g.Mind, archetype.Communication),
                Trait("professionalism", g.Personality), Trait("ambition", g.Personality), Trait("loyalty", g.Personality)),
            0, Draw.Int(seed, "xgen", id, "readiness", 80, 98), 0, Draw.Int(seed, "xgen", id, "morale", 50, 70), 0);
        core = Hold(core, content, floor, cap);
        var overall = Ratings.Rating(core, content);

        var dev = new DevTraits
        {
            PeakAge = Draw.Bell(seed, "xgen", id, "peak", pop.PeakAge[0], pop.PeakAge[1]),
            Volatility = Draw.Bell(seed, "xgen", id, "volatility", pop.Volatility[0], pop.Volatility[1]),
            Trainability = Draw.Bell(seed, "xgen", id, "trainability", pop.Trainability[0], pop.Trainability[1]),
            Affinity = Enumerable.Range(0, 5).Select(i => Math.Clamp(1 + profileOffsets[i] / 30 + Draw.Between(seed, "xgen", id, "affinity" + i, -0.1, 0.1), 0.6, 1.4)).ToArray(),
            Profile = profileOffsets,
            Focus = Enum.Parse<Focus>(Draw.Weighted(seed, "xgen", id, "focus", pop.Focus, f => f.Weight).Id, true)
        };
        var headroom = Curve.At(pop.HeadroomByAge, age) * Curve.At(pop.TalentQuantiles, Draw.Unit(seed, "xgen", id, "talent"));
        if (age >= pop.LateBloomerAges[0] && age <= pop.LateBloomerAges[1] && Draw.Chance(seed, "xgen", id, "late-bloomer", pop.LateBloomerPercent / 100.0))
        {
            dev.LateBloomer = true;
            dev.PeakAge = Draw.Between(seed, "xgen", id, "late-peak", pop.LateBloomerPeak[0], pop.LateBloomerPeak[1]);
            headroom += Draw.Int(seed, "xgen", id, "late-extra", pop.LateBloomerExtra[0], pop.LateBloomerExtra[1]);
        }
        var person = new WPerson { Id = id, Origin = Origin.Generated, Core = core, Dev = dev, Profile = profile.Id, CreatedSeason = season };
        // The ceiling approaches 99 without reaching it by a hard clamp: above CeilingSoftStart the remaining room is
        // compressed as room * x / (x + room), so exceptional ceilings stay rare and 99 is not a spike of clamped values.
        var ceilingRaw = overall + headroom;
        if (ceilingRaw > pop.CeilingSoftStart)
        {
            var x = ceilingRaw - pop.CeilingSoftStart;
            ceilingRaw = pop.CeilingSoftStart + pop.CeilingSoftRoom * x / (x + pop.CeilingSoftRoom);
            // D1a tail (decision experiment only; null in v1): above CeilingTail[0] the asymptote below 99 is replaced by a
            // straight line that reaches 99 at raw room CeilingTail[1], so 99 is attainable by the most extreme draws while
            // every ceiling below CeilingTail[0] is unchanged.
            if (pop.CeilingTail is [var from, var xAt99] && ceilingRaw > from)
            {
                var above = from - pop.CeilingSoftStart;
                var xFrom = above * pop.CeilingSoftRoom / (pop.CeilingSoftRoom - above);
                ceilingRaw = Math.Min(99, from + (99 - from) * (x - xFrom) / (xAt99 - xFrom));
            }
        }
        person.Ceiling = Math.Clamp((int)Math.Round(ceilingRaw, MidpointRounding.ToEven), overall, 99);
        var potential = DevelopmentV2.Potential(d, person, overall, age);
        var prime = age is >= 22 and <= 27 ? 5 : 0;
        var popularity = Math.Clamp((overall - 50) * 3 / 2 + prime + KeyedRandom.Range(seed, "xgen", id, "popularity", 17) - 8, 1, 99);
        person.Core = core with { Potential = potential, Popularity = popularity };
        person.InitialOverall = overall; person.InitialPotential = potential; person.MaxOverall = overall;
        return person;
    }

    /// <summary>Holds the production Overall inside [floor, cap]: lowers the highest-weighted skill (or raises it) one point
    /// at a time. Deterministic; changes nothing inside the bounds.</summary>
    public static Person Hold(Person core, Content content, int floor, int cap)
    {
        var weights = content.RoleWeights.Single(x => x.Role == core.Role).Weights;
        var order = Enumerable.Range(0, 5).OrderByDescending(i => weights[i]).ThenBy(i => i).ToArray();
        var s = DevelopmentV2.Skills(core.Skills);
        for (var guard = 0; guard < 500; guard++)
        {
            var overall = Ratings.Rating(core with { Skills = new Skills(s[0], s[1], s[2], s[3], s[4]) }, content);
            if (overall > cap) { var i = order.First(k => s[k] > 1); s[i]--; }
            else if (overall < floor) { var i = order.First(k => s[k] < 99); s[i]++; }
            else break;
        }
        return core with { Skills = new Skills(s[0], s[1], s[2], s[3], s[4]) };
    }
}
