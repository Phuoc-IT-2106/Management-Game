using System.Diagnostics;
using System.Text.Json;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>An entry pathway of the Pathways intake mode: the calibrated profile the entrant is drawn from and its age range.</summary>
public sealed record Pathway(string Id, int Weight, string Profile, int AgeMin, int AgeMax);

/// <summary>The decision experiments' own versioned settings (config/decisions.v1.json): checkpoints, entry pathways and the
/// D1a ceiling room. Nothing here is approved balance.</summary>
public sealed record DecisionConfig(string Version, string Status, int[] Checkpoints, Pathway[] Pathways, double D1aCeilingSoftRoom, double[] D1aTail)
{
    public static DecisionConfig Load(string path) =>
        JsonSerializer.Deserialize<DecisionConfig>(File.ReadAllBytes(path)) ?? throw new InvalidDataException("Empty decision config.");
}

/// <summary>The four-decision verification runs (EXPERIMENT): one variant over a list of seeds, several seasons each,
/// with aggregate metrics at the checkpoints and a light series every season. Variants change only the named policy;
/// every other coefficient is the v1 experiment's.</summary>
public static class Decisions
{
    public sealed record Variant(string Label, string Policy, string Intake, string Ceiling, string? ElitePath);

    public static ExperimentConfig Apply(ExperimentConfig c, Variant v, DecisionConfig d)
    {
        c = c with { World = c.World with { RealStarterPolicy = v.Policy } };
        c = c with { Market = c.Market with { IntakeMode = v.Intake } };
        if (v.Ceiling == "d1a") c = c with { Population = c.Population with { CeilingSoftRoom = d.D1aCeilingSoftRoom } };      // sensitivity: wider room
        if (v.Ceiling == "d1a-tail") c = c with { Population = c.Population with { CeilingTail = d.D1aTail } };            // D1a candidate
        return c with { Version = c.Version + $" / decisions {d.Version} {v.Label}" };
    }

    public static Dictionary<string, RealPack.EliteAdjustment>? LoadElite(string? path)
    {
        if (path is null) return null;
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        return doc.RootElement.GetProperty("players").EnumerateObject().ToDictionary(x => x.Name,
            x => new RealPack.EliteAdjustment(x.Value.GetProperty("delta").GetInt32(), x.Value.GetProperty("potential").GetInt32()), StringComparer.Ordinal);
    }

    private sealed class Tracker
    {
        public Dictionary<string, int> FirstIncomplete { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> FirstNpcStarter { get; } = new(StringComparer.Ordinal);
        public int ConservationViolations { get; set; }
        public List<Dictionary<string, int>> Flows { get; } = [];
    }

    private static double Round(double x, int d = 2) => double.IsNaN(x) ? double.NaN : Math.Round(x, d);
    private static double Mean(IEnumerable<double> x) { var l = x.ToList(); return l.Count == 0 ? double.NaN : l.Average(); }
    private static double Pct(int part, int whole) => whole == 0 ? double.NaN : Math.Round(100.0 * part / whole, 2);

    public static Dictionary<string, object> Run(World w, Calibration cal, DecisionConfig d, int seasons)
    {
        var tr = new Tracker();
        var checkpoints = new List<Dictionary<string, object>>();
        var series = new List<Dictionary<string, object>>();
        var times = new List<double>();
        var watch = new Stopwatch();
        var marks = d.Checkpoints.Where(x => x <= seasons).ToHashSet();
        if (marks.Contains(0)) checkpoints.Add(Checkpoint(w, tr, false));
        var activeBefore = w.People.Values.Count(p => p.Origin == Origin.Generated && p.Status != Status.Retired);
        for (var s = 1; s <= seasons; s++)
        {
            watch.Restart();
            var flows = SeasonSim.Play(w, cal);
            times.Add(Math.Round(watch.Elapsed.TotalSeconds, 3));
            tr.Flows.Add(flows);
            var activeNpc = w.People.Values.Count(p => p.Origin == Origin.Generated && p.Status != Status.Retired);
            var retiredNpc = flows.GetValueOrDefault("retired_npc"); var intake = flows.GetValueOrDefault("intake");
            var conserved = activeNpc == activeBefore - retiredNpc + intake;
            if (!conserved) tr.ConservationViolations++;
            foreach (var t in w.Teams.Values.Where(t => t.Real))
            {
                if (!tr.FirstIncomplete.ContainsKey(t.Id) && (t.WithdrawnSeason == s || !t.Dissolved && Roles.All.Any(r => t.Lineup.GetValueOrDefault(r) is null)))
                    tr.FirstIncomplete[t.Id] = s;
                if (!tr.FirstNpcStarter.ContainsKey(t.Id) && !t.Dissolved && t.Lineup.Values.Any(v => v is not null && w.People[v].Origin != Origin.Real))
                    tr.FirstNpcStarter[t.Id] = s;
            }
            var pre = w.PreWindow[^1];
            var liveReal = w.Teams.Values.Where(t => t.Real && !t.Dissolved).ToList();
            series.Add(new Dictionary<string, object>
            {
                ["season"] = s, ["seconds"] = times[^1], ["npc_active"] = activeNpc, ["npc_retired"] = retiredNpc, ["real_retired"] = flows.GetValueOrDefault("retired_real"),
                ["intake"] = intake, ["conserved"] = conserved,
                ["npc_fa_post"] = w.People.Values.Count(p => p.Origin == Origin.Generated && p.Status == Status.FreeAgent),
                ["npc_fa_pct_post"] = Pct(w.People.Values.Count(p => p.Origin == Origin.Generated && p.Status == Status.FreeAgent), activeNpc),
                ["npc_fa_pre"] = pre["npc_free_agents"], ["npc_fa_pct_pre"] = pre["npc_percent"],
                ["real_teams_active"] = liveReal.Count,
                ["real_teams_with_vacancy"] = liveReal.Count(t => Roles.All.Any(r => t.Lineup.GetValueOrDefault(r) is null)),
                ["real_npc_starters"] = liveReal.Sum(t => t.Lineup.Values.Count(v => v is not null && w.People[v].Origin != Origin.Real)),
                ["real_active"] = w.People.Values.Count(p => p.Origin == Origin.Real && p.Status != Status.Retired),
                ["flows"] = flows.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value)
            });
            activeBefore = activeNpc;
            if (marks.Contains(s)) checkpoints.Add(Checkpoint(w, tr, s == seasons));
        }
        return new Dictionary<string, object>
        {
            ["seasons"] = seasons, ["checkpoints"] = checkpoints, ["series"] = series,
            ["conservation_violations"] = tr.ConservationViolations,
            ["first_incomplete_season"] = w.Teams.Values.Where(t => t.Real).OrderBy(t => t.Id, StringComparer.Ordinal)
                .Select(t => tr.FirstIncomplete.TryGetValue(t.Id, out var x) ? x : 0).ToArray(),
            ["first_npc_starter_season"] = w.Teams.Values.Where(t => t.Real).OrderBy(t => t.Id, StringComparer.Ordinal)
                .Select(t => tr.FirstNpcStarter.TryGetValue(t.Id, out var x) ? x : 0).ToArray(),
            ["real_team_tiers"] = w.Teams.Values.Where(t => t.Real).OrderBy(t => t.Id, StringComparer.Ordinal).Select(t => t.Key[5..]).ToArray(),
            ["season_seconds_mean"] = times.Count == 0 ? 0 : Math.Round(times.Average(), 3), ["season_seconds_max"] = times.Count == 0 ? 0 : times.Max()
        };
    }

    private static Dictionary<string, object> Summary(IEnumerable<int> values) => Reports.Summary(values);

    /// <summary>A streaming digest of the whole world state (people, teams, contracts, history), in ID order, for replay
    /// checks without materializing a snapshot.</summary>
    public static string Digest(World w)
    {
        using var h = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        void Add(string x) => h.AppendData(System.Text.Encoding.UTF8.GetBytes(x + "\n"));
        Add($"season {w.Season} {w.NextEvent} {w.NextContract} {w.NextPerson} {w.NextTeam}");
        foreach (var p in w.People.Values)
        {
            var k = p.Core.Skills;
            Add($"{p.Id}|{p.Status}|{p.TeamId}|{p.ContractId}|{k.Mechanics},{k.EarlyGame},{k.Teamfight},{k.Macro},{k.ChampionPool}|{p.Core.Potential}|{p.Ceiling}|{p.FreeAgentSince}|{p.RetiredSeason}|{p.MaxOverall}");
        }
        foreach (var t in w.Teams.Values)
            Add($"{t.Id}|{t.Dissolved}|{t.WithdrawnSeason}|{t.Funds}|{t.Reputation}|{string.Join(",", t.Squad)}|{string.Join(",", Roles.All.Select(r => t.Lineup.GetValueOrDefault(r)))}");
        foreach (var c in w.Contracts.Values) Add($"{c.Id}|{c.PersonId}|{c.TeamId}|{c.StartSeason}|{c.EndSeason}|{c.Salary}|{c.Active}");
        foreach (var e in w.Events) Add($"{e.Id}|{e.PersonId}|{e.Type}|{e.From}|{e.To}|{e.Season}|{e.Reason}|{e.ContractId}|{e.Era}");
        return Convert.ToHexString(h.GetHashAndReset()).ToLowerInvariant();
    }

    /// <summary>Aggregate state at a checkpoint (after the season's window). Real teams, the NPC population, the market,
    /// ratings and integrity. Aggregates only; no names.</summary>
    private static Dictionary<string, object> Checkpoint(World w, Tracker tr, bool final)
    {
        var content = w.Content; var s = w.Season;
        var active = w.People.Values.Where(p => p.Status != Status.Retired).ToList();
        var npc = active.Where(p => p.Origin == Origin.Generated).ToList();
        var real = active.Where(p => p.Origin == Origin.Real).ToList();
        var realTeams = w.Teams.Values.Where(t => t.Real).ToList();
        var liveReal = realTeams.Where(t => !t.Dissolved).ToList();
        var liveTeams = w.Teams.Values.Where(t => !t.Dissolved).ToList();
        int NpcStarters(WTeam t) => t.Lineup.Values.Count(v => v is not null && w.People[v].Origin != Origin.Real);
        bool Five(WTeam t) => Roles.All.All(r => t.Lineup.GetValueOrDefault(r) is not null);
        var fa = npc.Where(p => p.Status == Status.FreeAgent).ToList();
        var overall = active.ToDictionary(p => p.Id, w.Overall, StringComparer.Ordinal);

        Dictionary<string, object> TierBlock(IEnumerable<WTeam> group)
        {
            var all = group.ToList(); var live = all.Where(t => !t.Dissolved).ToList();
            return new Dictionary<string, object>
            {
                ["teams"] = all.Count, ["active"] = live.Count, ["withdrawn"] = all.Count(t => t.Dissolved),
                ["authentic_five"] = live.Count(t => Five(t) && NpcStarters(t) == 0), ["fielding_five"] = live.Count(Five),
                ["with_vacancy"] = live.Count(t => !Five(t)), ["npc_starters"] = live.Sum(NpcStarters),
                ["vacant_slots"] = live.Sum(t => Roles.All.Count(r => t.Lineup.GetValueOrDefault(r) is null)),
                ["mean_strength"] = live.Count == 0 ? double.NaN : Round(live.Average(t => Lineups.Strength(w, t))),
                ["mean_squad"] = live.Count == 0 ? double.NaN : Round(live.Average(t => t.Squad.Count)),
                ["real_players_in_squads"] = live.Sum(t => t.Squad.Count(id => w.People[id].Origin == Origin.Real)),
                ["npc_in_squads"] = live.Sum(t => t.Squad.Count(id => w.People[id].Origin == Origin.Generated)),
                ["mean_funds"] = live.Count == 0 ? double.NaN : Round(live.Average(t => (double)t.Funds), 0),
                ["negative_funds"] = live.Count(t => t.Funds < 0),
                ["wage_to_revenue"] = live.Count == 0 ? double.NaN : Round(live.Average(t => (double)WorldBuilder.Wages(w, t) * w.Config.SeasonDays / Math.Max(1, t.Revenue)), 3),
                ["mean_win_share"] = live.Count == 0 ? double.NaN : Round(live.Average(t => t.SeasonWinShare), 3)
            };
        }

        var realLeagues = realTeams.GroupBy(t => t.LeagueId).ToList();
        var fictionalPro = liveTeams.Where(t => t.Layer == Layer.FictionalPro).ToList();
        var realA = liveReal.Where(t => t.Key == "Real:a").ToList();
        var flowsSoFar = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var f in tr.Flows) foreach (var (k, v) in f) flowsSoFar[k] = flowsSoFar.GetValueOrDefault(k) + v;
        var layers = new[] { "FictionalPro", "Academy", "SemiPro", "Amateur" };
        var allOverall = overall.Values.ToList();
        var npcOverall = npc.Select(p => overall[p.Id]).ToList();
        var realOverall = real.Select(p => overall[p.Id]).ToList();
        int Count(IEnumerable<int> x, int min) => x.Count(v => v >= min);
        var everyone = w.People.Values.ToList();
        var cohort = everyone.Where(p => p.Origin == Origin.Generated && p.CreatedSeason == 0 && p.InitialPotential >= 85 && p.CreatedSeason - p.Core.BornSeason <= 21).ToList();
        var employedSalary = active.Where(p => p.Status == Status.Employed).ToList();
        string Stage(int age) => age <= 21 ? "16-21" : age <= 27 ? "22-27" : "28+";
        var invariants = Checks.Invariants(w, initial: false);
        var pre = w.PreWindow.Count > 0 ? w.PreWindow[^1] : null;

        var result = new Dictionary<string, object>
        {
            ["season"] = s,
            // ---- D-H1: real teams
            ["real_teams"] = TierBlock(realTeams),
            ["real_by_tier"] = realTeams.GroupBy(t => t.Key[5..]).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => (object)TierBlock(g)),
            ["real_never_incomplete"] = realTeams.Count(t => !tr.FirstIncomplete.ContainsKey(t.Id)),
            ["real_vacancy_by_role"] = Roles.All.ToDictionary(r => r, r => liveReal.Count(t => t.Lineup.GetValueOrDefault(r) is null)),
            ["real_leagues"] = realLeagues.Count, ["real_leagues_with_2_plus"] = realLeagues.Count(g => g.Count(t => !t.Dissolved) >= 2),
            ["real_leagues_with_4_plus"] = realLeagues.Count(g => g.Count(t => !t.Dissolved) >= 4),
            ["real_players_active"] = real.Count, ["real_players_retired"] = everyone.Count(p => p.Origin == Origin.Real && p.Status == Status.Retired),
            ["real_free_agents"] = real.Count(p => p.Status == Status.FreeAgent),
            ["real_starter_share_real"] = Pct(liveReal.Sum(t => t.Lineup.Values.Count(v => v is not null && w.People[v].Origin == Origin.Real)), liveReal.Count * 5),
            ["npc_75_plus_on_real_teams"] = npc.Count(p => p.Status == Status.Employed && w.Teams[p.TeamId!].Real && overall[p.Id] >= 75),
            ["fictional_pro_mean_strength"] = fictionalPro.Count == 0 ? double.NaN : Round(fictionalPro.Average(t => Lineups.Strength(w, t))),
            ["real_a_mean_strength"] = realA.Count == 0 ? double.NaN : Round(realA.Average(t => Lineups.Strength(w, t))),
            ["real_a_top_strength"] = realA.Count == 0 ? double.NaN : Round(realA.Max(t => Lineups.Strength(w, t))),
            ["flows_cumulative"] = flowsSoFar.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value),
            // ---- E2/F1: population and market
            ["npc_active"] = npc.Count, ["npc_created_total"] = everyone.Count(p => p.Origin == Origin.Generated),
            ["npc_retired_total"] = everyone.Count(p => p.Origin == Origin.Generated && p.Status == Status.Retired),
            ["npc_fa"] = fa.Count, ["npc_fa_pct"] = Pct(fa.Count, npc.Count),
            ["npc_fa_pre_window"] = pre?["npc_free_agents"] ?? 0, ["npc_fa_pct_pre_window"] = pre?["npc_percent"] ?? 0.0,
            ["pre_window_70_plus"] = pre?["overall_70_plus"] ?? 0, ["pre_window_potential_85_plus"] = pre?["potential_85_plus"] ?? 0,
            ["fa_by_role"] = Roles.All.ToDictionary(r => r, r => (object)new Dictionary<string, int>
            {
                ["all"] = fa.Count(p => p.Core.Role == r), ["60_plus"] = fa.Count(p => p.Core.Role == r && overall[p.Id] >= 60),
                ["70_plus"] = fa.Count(p => p.Core.Role == r && overall[p.Id] >= 70)
            }),
            ["fa_overall"] = Summary(fa.Select(p => overall[p.Id])), ["fa_potential"] = Summary(fa.Select(p => p.Core.Potential)),
            ["fa_potential_80_plus"] = fa.Count(p => p.Core.Potential >= 80), ["fa_potential_85_plus"] = fa.Count(p => p.Core.Potential >= 85),
            ["fa_unemployed_seasons"] = fa.GroupBy(p => Math.Min(3, s - (p.FreeAgentSince ?? s))).OrderBy(g => g.Key).ToDictionary(g => g.Key == 3 ? "3+" : g.Key.ToString(), g => g.Count()),
            ["fa_mean_asking_wage"] = fa.Count == 0 ? double.NaN : Round(fa.Average(p => (double)Pay.Player(p.Core, s + 1, content)), 1),
            ["mean_salary_by_layer"] = liveTeams.GroupBy(t => t.Real ? "Real" : t.Key).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key,
                g => Round(Mean(g.SelectMany(t => t.Squad).Select(id => (double)w.Contracts[w.People[id].ContractId!].Salary)), 1)),
            ["vacant_squad_seats"] = layers.ToDictionary(l => l, l => liveTeams.Where(t => t.Key == l).Sum(t => Math.Max(0, t.SquadTarget - t.Squad.Count))),
            ["teams_without_five"] = layers.ToDictionary(l => l, l => liveTeams.Count(t => t.Key == l && !Five(t))),
            ["teams_active_by_layer"] = layers.ToDictionary(l => l, l => liveTeams.Count(t => t.Key == l)),
            ["npc_age"] = Summary(npc.Select(w.Age)), ["npc_stage_share"] = npc.GroupBy(p => Stage(w.Age(p))).OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => Pct(g.Count(), npc.Count)),
            ["fictional_pro_starter_age_mean"] = Round(Mean(fictionalPro.SelectMany(t => t.Lineup.Values).Where(v => v is not null).Select(v => (double)w.Age(w.People[v!])))),
            ["fictional_pro_starter_overall_mean"] = Round(Mean(fictionalPro.SelectMany(t => t.Lineup.Values).Where(v => v is not null).Select(v => (double)overall[v!]))),
            ["semi_pro_starter_overall_mean"] = Round(Mean(liveTeams.Where(t => t.Layer == Layer.SemiPro).SelectMany(t => t.Lineup.Values).Where(v => v is not null).Select(v => (double)overall[v!]))),
            // ---- D1: ratings and development
            ["overall_all"] = Summary(allOverall), ["overall_npc"] = Summary(npcOverall), ["overall_real"] = Summary(realOverall),
            ["count_85_plus"] = Count(allOverall, 85), ["count_90_plus"] = Count(allOverall, 90), ["count_94_plus"] = Count(allOverall, 94), ["count_97_plus"] = Count(allOverall, 97),
            ["npc_90_plus"] = Count(npcOverall, 90), ["npc_94_plus"] = Count(npcOverall, 94), ["npc_97_plus"] = Count(npcOverall, 97),
            ["real_90_plus"] = Count(realOverall, 90), ["real_94_plus"] = Count(realOverall, 94), ["real_97_plus"] = Count(realOverall, 97),
            ["ever_94_plus"] = everyone.Count(p => p.MaxOverall >= 94), ["ever_97_plus"] = everyone.Count(p => p.MaxOverall >= 97),
            ["npc_ever_94_plus"] = everyone.Count(p => p.Origin == Origin.Generated && p.MaxOverall >= 94),
            ["npc_ever_above_82"] = everyone.Count(p => p.Origin == Origin.Generated && p.MaxOverall > 82),
            ["npc_initial_above_82"] = everyone.Count(p => p.Origin == Origin.Generated && p.InitialOverall > w.Config.Population.InitialOverallCap),
            ["npc_created_max_initial_overall"] = everyone.Where(p => p.Origin == Origin.Generated).Max(p => p.InitialOverall),
            ["potential_npc"] = Summary(npc.Select(p => p.Core.Potential)),
            ["npc_potential_90_plus"] = npc.Count(p => p.Core.Potential >= 90), ["npc_potential_95_plus"] = npc.Count(p => p.Core.Potential >= 95),
            ["npc_potential_97_plus"] = npc.Count(p => p.Core.Potential >= 97), ["npc_potential_99"] = npc.Count(p => p.Core.Potential == 99),
            ["created_npc_initial_potential_95_plus"] = everyone.Count(p => p.Origin == Origin.Generated && p.InitialPotential >= 95),
            ["created_npc_initial_potential_97_plus"] = everyone.Count(p => p.Origin == Origin.Generated && p.InitialPotential >= 97),
            ["created_npc_initial_potential_99"] = everyone.Count(p => p.Origin == Origin.Generated && p.InitialPotential == 99),
            ["near_potential_85_plus"] = active.Count(p => p.Core.Potential >= 85 && overall[p.Id] >= p.Core.Potential - 1),
            ["potential_below_overall"] = active.Count(p => p.Core.Potential < overall[p.Id]),
            ["max_overall"] = allOverall.Count == 0 ? 0 : allOverall.Max(), ["max_potential"] = active.Count == 0 ? 0 : active.Max(p => p.Core.Potential),
            ["top50_by_role_all"] = Roles.All.ToDictionary(r => r, r => Round(active.Where(p => p.Core.Role == r).Select(p => (double)overall[p.Id]).OrderByDescending(x => x).Take(50).DefaultIfEmpty(0).Average())),
            ["top50_by_role_npc"] = Roles.All.ToDictionary(r => r, r => Round(npc.Where(p => p.Core.Role == r).Select(p => (double)overall[p.Id]).OrderByDescending(x => x).Take(50).DefaultIfEmpty(0).Average())),
            ["prospect_cohort"] = new Dictionary<string, object>
            {
                ["n"] = cohort.Count, ["mean_gain_best"] = cohort.Count == 0 ? double.NaN : Round(cohort.Average(p => p.MaxOverall - p.InitialOverall)),
                ["reached_potential_pct"] = Pct(cohort.Count(p => p.MaxOverall >= p.InitialPotential - 1), cohort.Count),
                ["retired_pct"] = Pct(cohort.Count(p => p.Status == Status.Retired), cohort.Count),
                ["best_max"] = cohort.Count == 0 ? 0 : cohort.Max(p => p.MaxOverall)
            },
            ["salary_90_plus_mean"] = Round(Mean(employedSalary.Where(p => overall[p.Id] >= 90).Select(p => (double)w.Contracts[p.ContractId!].Salary)), 1),
            ["salary_80_89_mean"] = Round(Mean(employedSalary.Where(p => overall[p.Id] is >= 80 and < 90).Select(p => (double)w.Contracts[p.ContractId!].Salary)), 1),
            ["asking_wage_90_plus_mean"] = Round(Mean(active.Where(p => overall[p.Id] >= 90).Select(p => (double)Pay.Player(p.Core, s + 1, content))), 1),
            // ---- integrity
            ["invariant_failures"] = invariants.Count, ["invariant_samples"] = invariants.Take(5).ToList(),
            ["max_registered"] = liveTeams.Count == 0 ? 0 : liveTeams.Max(WorldBuilder.RegisteredCount),
            ["conservation_violations"] = tr.ConservationViolations,
            ["people_records"] = w.People.Count, ["team_records"] = w.Teams.Count, ["contract_records"] = w.Contracts.Count, ["events"] = w.Events.Count,
            ["unique_person_ids"] = w.People.Keys.Distinct(StringComparer.Ordinal).Count() == w.People.Count
        };
        if (final) result["history_violations"] = Checks.History(w).Count;
        return result;
    }
}
