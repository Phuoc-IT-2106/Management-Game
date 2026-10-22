using System.Collections.Immutable;
using System.Diagnostics;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>Phase B: the development experiment. Representative players under poor, average and strong environments and
/// three playing-time levels, many keyed replicates each, for dev2 and for the production model (rules v4).</summary>
public static class DevelopmentExperiment
{
    public sealed record Case(string Id, int Age, int Overall, int Ceiling, string Note);
    public sealed record Env(string Id, int Coach, Intensity Intensity, double Share);

    public static readonly Case[] Cases =
    [
        new("A", 17, 55, 90, "young academy-level prospect, high ceiling"),
        new("B", 18, 65, 95, "wonderkid: 65 now, ceiling 95"),
        new("C", 20, 70, 82, "solid young professional"),
        new("D", 22, 75, 88, "developing starter, late window"),
        new("E", 24, 78, 84, "near the peak"),
        new("F", 26, 80, 82, "established professional"),
        new("G", 29, 85, 85, "veteran at his ceiling")
    ];
    public static readonly Env[] Environments =
    [
        new("poor", 30, Intensity.Normal, 0.3), new("average", 60, Intensity.Normal, 0.7), new("strong", 90, Intensity.High, 1.0),
        new("bench", 60, Intensity.Normal, 0.05), new("rotation", 60, Intensity.Normal, 0.5), new("regular", 60, Intensity.Normal, 1.0)
    ];

    private static WPerson Make(World w, Case c, string id, bool population)
    {
        var content = w.Content; var d = w.Config.Development; var pop = w.Config.Population;
        var core = new Person(id, "player:experiment", id, id, "XX", 1 - c.Age, "mid", "", new Skills(c.Overall, c.Overall, c.Overall, c.Overall, c.Overall),
            new Mind(60, 60, 60, 60, 55, 50), 0, 90, 0, 60, 50);
        var dev = new DevTraits();
        if (population)
        {
            var seed = w.Seed;
            dev.PeakAge = Draw.Bell(seed, "xexp", id, "peak", pop.PeakAge[0], pop.PeakAge[1]);
            dev.Volatility = Draw.Bell(seed, "xexp", id, "vol", pop.Volatility[0], pop.Volatility[1]);
            dev.Trainability = Draw.Bell(seed, "xexp", id, "train", pop.Trainability[0], pop.Trainability[1]);
            dev.Focus = Enum.Parse<Focus>(Draw.Weighted(seed, "xexp", id, "focus", pop.Focus, f => f.Weight).Id, true);
            dev.Profile = Enumerable.Range(0, 5).Select(i => Draw.Between(seed, "xexp", id, "profile" + i, -6, 6)).ToArray();
            dev.Affinity = dev.Profile.Select(x => 1 + x / 30).ToArray();
            if (c.Age >= pop.LateBloomerAges[0] && c.Age <= pop.LateBloomerAges[1] && Draw.Chance(seed, "xexp", id, "late", pop.LateBloomerPercent / 100.0))
            { dev.LateBloomer = true; dev.PeakAge = Draw.Between(seed, "xexp", id, "late-peak", pop.LateBloomerPeak[0], pop.LateBloomerPeak[1]); }
            var range = content.Generation.Personality;
            core = core with { Mind = core.Mind with { Professionalism = Generator.Bell(seed, "xexp", id, "prof", range), Ambition = Generator.Bell(seed, "xexp", id, "amb", range) } };
        }
        var p = new WPerson { Id = id, Core = core, Dev = dev, Origin = Origin.Generated, Ceiling = c.Ceiling };
        p.Core = core with { Potential = DevelopmentV2.Potential(d, p, c.Overall, c.Age) };
        p.InitialPotential = p.Core.Potential;
        return p;
    }

    public static Dictionary<string, object> Run(World w, int replicates, int seasons)
    {
        var d = w.Config.Development; var content = w.Content;
        var rows = new List<Dictionary<string, object>>();
        var watch = Stopwatch.StartNew();
        foreach (var traits in new[] { "neutral", "population" })
            foreach (var c in Cases)
                foreach (var env in Environments)
                {
                    var finals = new List<int>(); var reached = 0; var reached95 = 0; var everBreak = 0; var stagnated = 0; var spread = new List<double>();
                    var potentialEnd = new List<int>(); var bySeason = Enumerable.Range(0, seasons).Select(_ => new List<int>()).ToArray();
                    var prodFinals = new List<int>(); var prodReached = 0;
                    for (var r = 0; r < replicates; r++)
                    {
                        var id = $"exp:{traits}:{c.Id}:{env.Id}:{r:D5}";
                        var p = Make(w, c, id, traits == "population");
                        var target = p.InitialPotential; var run = 0; var stuck = false; var hit = false; var last = c.Overall;
                        for (var k = 1; k <= seasons; k++)
                        {
                            var o = DevelopmentV2.Season(d, content, w.Seed, p, c.Age + k - 1, k, new DevEnvironment(env.Coach, env.Intensity, env.Share, (int)Math.Round(env.Share * 45), 55 + (int)(10 * env.Share)));
                            bySeason[k - 1].Add(o.OverallAfter);
                            if (o.OverallAfter >= target) hit = true;
                            if (c.Age + k - 1 <= 23 && o.OverallAfter <= last) { run++; if (run >= 3) stuck = true; } else run = 0;
                            last = o.OverallAfter;
                        }
                        var final = Ratings.Rating(p.Core, content);
                        finals.Add(final); potentialEnd.Add(p.Core.Potential);
                        if (hit) reached++;
                        if (p.MaxOverall >= 95) reached95++;
                        if (p.Breakthroughs > 0) everBreak++;
                        if (stuck) stagnated++;
                        var s = DevelopmentV2.Skills(p.Core.Skills); var mean = s.Average();
                        spread.Add(Math.Sqrt(s.Sum(x => (x - mean) * (x - mean)) / 5));
                        // The production model (rules v4), the same person and environment.
                        var prod = new Person(id, "player:experiment", id, id, "XX", 1 - c.Age, "mid", "", new Skills(c.Overall, c.Overall, c.Overall, c.Overall, c.Overall),
                            p.Core.Mind, c.Ceiling, 90, 0, 60, 50);
                        var prodHit = false;
                        for (var k = 1; k <= seasons; k++)
                        {
                            prod = ProductionDevelopment.Season(content, w.Seed, prod, k, env.Coach, (int)Math.Round(env.Share * 100));
                            if (Ratings.Rating(prod, content) >= c.Ceiling) prodHit = true;
                        }
                        prodFinals.Add(Ratings.Rating(prod, content));
                        if (prodHit) prodReached++;
                    }
                    finals.Sort(); prodFinals.Sort();
                    rows.Add(new Dictionary<string, object>
                    {
                        ["traits"] = traits, ["case"] = c.Id, ["age"] = c.Age, ["overall"] = c.Overall, ["ceiling"] = c.Ceiling,
                        ["initial_potential"] = Make(w, c, $"exp:probe:{c.Id}", false).InitialPotential, ["environment"] = env.Id,
                        ["final"] = Reports.Summary(finals),
                        ["by_season_p50"] = bySeason.Select(x => Reports.Pct(x.OrderBy(v => v).ToList(), 50)).ToArray(),
                        ["by_season_p10"] = bySeason.Select(x => Reports.Pct(x.OrderBy(v => v).ToList(), 10)).ToArray(),
                        ["by_season_p90"] = bySeason.Select(x => Reports.Pct(x.OrderBy(v => v).ToList(), 90)).ToArray(),
                        ["reached_initial_potential_pct"] = Math.Round(100.0 * reached / replicates, 1),
                        ["reached_95_pct"] = Math.Round(100.0 * reached95 / replicates, 1),
                        ["breakthrough_pct"] = Math.Round(100.0 * everBreak / replicates, 1),
                        ["stagnation_pct"] = Math.Round(100.0 * stagnated / replicates, 1),
                        ["skill_sd_final_mean"] = Math.Round(spread.Average(), 2),
                        ["potential_end_mean"] = Math.Round(potentialEnd.Average(), 1),
                        ["production_final"] = Reports.Summary(prodFinals),
                        ["production_reached_ceiling_pct"] = Math.Round(100.0 * prodReached / replicates, 1)
                    });
                }
        return new Dictionary<string, object>
        {
            ["model"] = d.Version, ["replicates"] = replicates, ["seasons"] = seasons, ["matches_per_full_season"] = 45,
            ["environments"] = Environments, ["cases"] = Cases, ["rows"] = rows, ["seconds"] = Math.Round(watch.Elapsed.TotalSeconds, 2)
        };
    }
}

/// <summary>Micro-benchmarks of production code at the experiment's scale (no production change): the rival lineup
/// search at squad sizes 8, 12 and 16, and the snapshot reader's size limits.</summary>
public static class ProductionBench
{
    public static Dictionary<string, object> Run(Content content, ulong seed)
    {
        var results = new Dictionary<string, object>();
        foreach (var size in new[] { 6, 8, 10, 12, 16 })
        {
            var people = Enumerable.Range(0, size).Select(i => Generator.Player(seed, content, $"person:b{size:D2}{i:D3}", Roles.All[i % 5], 1)).ToImmutableArray();
            var rival = new Rival("rival:bench", "Bench", people, [], Generator.Coach(seed, content, "coach:bench"), 0, 50, Posture.Balanced, Strategy.Balanced, 50, 50, 0);
            var reps = size <= 10 ? 200 : size <= 12 ? 20 : 3;
            Rivals.Lineup(rival, content);
            var watch = Stopwatch.StartNew();
            for (var r = 0; r < reps; r++) Rivals.Lineup(rival, content);
            results[$"rivals_lineup_ms_squad_{size}"] = Math.Round(watch.Elapsed.TotalMilliseconds / reps, 3);
        }
        results["strict_json_file_limit_bytes"] = 8_000_000;
        results["strict_json_array_limit"] = 20_000;
        return results;
    }
}
