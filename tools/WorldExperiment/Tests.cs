using System.Text.Json;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>The verification list of the task (section 13), evaluated on a built and simulated world, its report and the
/// development experiment's output. Each check states what it measured.</summary>
public static class Tests
{
    public static List<CheckResult> Run(World w, Dictionary<string, object> report, RealPack.LoadResult real, JsonDocument? development)
    {
        var r = new List<CheckResult>();
        void Check(string id, string name, bool ok, string detail) => r.Add(new CheckResult(id, name, ok, detail));
        var cfg = w.Config; var npcs = w.People.Values.Where(p => p.Origin == Origin.Generated).ToList();
        var initialNpcs = npcs.Where(p => p.CreatedSeason == 0).ToList();
        var states = (List<Dictionary<string, object>>)report["seasons_state"];
        var initial = (Dictionary<string, object>)report["initial_state"];
        var census = (Dictionary<string, int>)initial["census"];
        var simulation = w.Events.Where(e => e.Era == Era.Simulation).ToList();
        int Count(EventType t) => simulation.Count(e => e.Type == t);
        var seasonFailures = (Dictionary<string, List<string>>)report["season_invariant_failures"];
        var initialFailures = (List<string>)report["initial_invariant_failures"];
        var everyone = w.People.Values.ToList();

        // ---- ratings and development
        Check("R01", "Overall within 1-99 (all people, every season)", initialFailures.Concat(seasonFailures.Values.SelectMany(x => x)).All(f => !f.Contains("overall")),
            $"max overall now {everyone.Max(w.Overall)}, min {everyone.Min(w.Overall)}");
        Check("R02", "Potential within 1-99 and never below Overall", initialFailures.Concat(seasonFailures.Values.SelectMany(x => x)).All(f => !f.Contains("potential")),
            $"{everyone.Count(p => p.Core.Potential < w.Overall(p))} below now");
        var cells = development?.RootElement.GetProperty("rows").EnumerateArray().ToList() ?? [];
        var max95 = cells.Where(c => c.GetProperty("case").GetString() == "B").Max(c => (double?)c.GetProperty("reached_95_pct").GetDouble()) ?? 0;
        var worldMax = everyone.Max(p => p.MaxOverall);
        Check("R03", "Ratings above 93 are reachable", max95 > 0 || worldMax >= 94,
            $"case B reaches 95 in up to {max95}% of runs; world max overall reached {worldMax}");
        Check("R04", "Every NPC starts at Overall <= 82 (initial cohort and every intake)", npcs.All(p => p.InitialOverall <= cfg.Population.InitialOverallCap),
            $"{npcs.Count} NPCs created, max initial {npcs.Max(p => p.InitialOverall)}");
        Check("R05", "NPC potential may exceed 82", initialNpcs.Count(p => p.InitialPotential > 82) > 0,
            $"{initialNpcs.Count(p => p.InitialPotential > 82)} of {initialNpcs.Count} initial NPCs; max {initialNpcs.Max(p => p.InitialPotential)}");
        Check("R06", "NPCs can develop beyond 82", npcs.Count(p => p.MaxOverall > 82) > 0, $"{npcs.Count(p => p.MaxOverall > 82)} NPCs reached 83+ (max {npcs.Max(p => p.MaxOverall)})");
        var reachRates = cells.Select(c => c.GetProperty("reached_initial_potential_pct").GetDouble()).ToList();
        var prospects = initialNpcs.Where(p => p.InitialPotential >= 85 && p.InitialPotential - p.InitialOverall >= 10).ToList();
        Check("R07", "No player is guaranteed to reach potential", reachRates.Count > 0 && reachRates.All(x => x < 100) && prospects.Any(p => p.MaxOverall < p.InitialPotential),
            $"reach rate across cells {reachRates.DefaultIfEmpty().Min()}-{reachRates.DefaultIfEmpty().Max()}%; {prospects.Count(p => p.MaxOverall < p.InitialPotential)} of {prospects.Count} world prospects (potential 85+, gap 10+) below it");
        double Mean(string traits, string c, string env) => cells.Single(x => x.GetProperty("traits").GetString() == traits && x.GetProperty("case").GetString() == c
            && x.GetProperty("environment").GetString() == env).GetProperty("final").GetProperty("mean").GetDouble();
        var ordered = cells.Count > 0 && new[] { "A", "B", "C", "D" }.All(c => Mean("neutral", c, "strong") > Mean("neutral", c, "average") && Mean("neutral", c, "average") > Mean("neutral", c, "poor")
            && Mean("neutral", c, "regular") > Mean("neutral", c, "bench"));
        Check("R08", "Coaching and playing time change development", ordered,
            cells.Count == 0 ? "no development output" : $"case B final mean poor/average/strong {Mean("neutral", "B", "poor")}/{Mean("neutral", "B", "average")}/{Mean("neutral", "B", "strong")}; bench/regular {Mean("neutral", "B", "bench")}/{Mean("neutral", "B", "regular")}");
        double Spread(Person p) { var s = DevelopmentV2.Skills(p.Skills); var m = s.Average(); return Math.Sqrt(s.Sum(x => (x - m) * (x - m)) / 5); }
        var diverged = cells.Where(c => c.GetProperty("traits").GetString() == "neutral").Max(c => (double?)c.GetProperty("skill_sd_final_mean").GetDouble()) ?? 0;
        Check("R09", "Skills develop differently", diverged > 0.5, $"neutral cases start with five equal skills; mean final skill SD up to {diverged}; world NPC mean skill SD {Math.Round(npcs.Average(p => Spread(p.Core)), 2)}");
        var det = (Dictionary<string, object>)report["determinism"];
        Check("R10", "The same seed reproduces the same careers", (bool)det["replay_equal"], $"replay {((string)det["replay_hash"])[..12]}");

        // ---- real-team rosters
        var realTeams = w.Teams.Values.Where(t => t.Real).ToList();
        Check("T01", "Verified real teams start with five real players", real.Teams > 0 && real.Starters == real.Teams * 5 && !initialFailures.Any(f => f.Contains("starting five")),
            $"{real.Teams} playable verified teams, {real.Starters} real starters, {real.Substitutes} real substitutes");
        Check("T02", "Every starter has a valid role", initialFailures.All(f => !f.Contains("cannot play") && !f.Contains("starter at creation")), $"{initialFailures.Count} initial invariant failures in total");
        var npcInLineup = states.Sum(s => (int)s["real_team_npc_in_lineup"]) + (int)initial["real_team_npc_in_lineup"];
        Check("T03", "No NPC ever fills a real team's starting slot", npcInLineup == 0, $"{npcInLineup} NPC starter slots on real teams over {states.Count + 1} snapshots");
        Check("T04", "Source observations stay unchanged through the simulation", (bool)report["snapshot_history_unchanged"],
            $"{w.Events.Count(e => e.Era == Era.Snapshot && e.Type == EventType.RealRegistrationObserved)} real registration observations");
        var academyPlayers = npcs.Where(p => p.Status == Status.Employed && w.Teams[p.TeamId!].Layer == Layer.Academy).ToList();
        Check("T05", "Academy affiliation is distinct from senior employment", initialFailures.Concat(seasonFailures.Values.SelectMany(x => x)).All(f => !f.Contains("senior registration")),
            $"{academyPlayers.Count} academy players now, {academyPlayers.Count(p => p.SeniorRegistration)} also registered with the senior squad");
        Check("T06", "Squads never exceed 16 registered players", initialFailures.Concat(seasonFailures.Values.SelectMany(x => x)).All(f => !f.Contains("registered (max")),
            $"max registered now {w.Teams.Values.Where(t => !t.Dissolved).Max(WorldBuilder.RegisteredCount)}");
        Check("T07", "Missing source evidence is reported, not invented", real.ExcludedTeams > 0 && real.RatingMismatches == 0,
            $"{real.ExcludedTeams} teams excluded ({string.Join(", ", real.ExcludedByStatus.Select(x => $"{x.Key} {x.Value}"))}); {real.RatingMismatches} Overall mismatches against the research ratings");

        // ---- the NPC population
        Check("N01", "Exactly 4,000 unique NPCs at creation", initialNpcs.Count == cfg.Population.NpcCount && initialNpcs.Select(p => p.Id).Distinct().Count() == initialNpcs.Count,
            $"{initialNpcs.Count} NPCs");
        var fa0 = (int)initial["npc_free_agents"];
        Check("N02", "Initial free agents fewer than 400 (target 320)", fa0 < 400 && fa0 == cfg.Population.NpcCount * cfg.Population.FreeAgentTargetPercent / 100, $"{fa0} ({initial["npc_free_agent_percent"]}%)");
        Check("N03", "No NPC counted twice (census adds up)", census.Values.Sum() == cfg.Population.NpcCount, $"census total {census.Values.Sum()}");
        Check("N04", "Every NPC in exactly one valid employment state", initialFailures.Count == 0 && seasonFailures.Count == 0,
            $"{initialFailures.Count} initial and {seasonFailures.Values.Sum(x => x.Count)} season invariant failures");
        Check("N05", "Amateur players are employed, not free agents", census["amateur"] > 0 && initialNpcs.Where(p => p.Status == Status.Employed && w.Teams[p.TeamId!].Layer == Layer.Amateur).All(p => p.ContractId is not null),
            $"{census["amateur"]} amateur players under amateur contracts at creation");
        Check("N06", "Academy players are not automatically substitutes", census["academy"] > 0 && (int)initial["academy_senior_registered"] < census["academy"],
            $"{census["academy"]} academy players, {initial["academy_senior_registered"]} also on a senior list");
        Check("N07", "Reserves are employed by their team", census["fictional_pro_reserves"] > 0, $"{census["fictional_pro_reserves"]} fictional pro reserves, all under contract (invariants)");
        Check("N08", "Fictional starting lineups cover all five roles", initialFailures.All(f => !f.Contains("starter at creation")), "checked by the creation invariants");
        var gen = (Dictionary<string, object>)report["generation"];
        Check("N09", "Generated identities never collide with real ones", (int)gen["handles_matching_real"] == 0 && (int)gen["ids_with_real_prefix"] == 0 && (int)gen["unique_handles"] == initialNpcs.Count,
            $"{gen["unique_handles"]} unique handles; {gen["handles_matching_real"]} match one of {w.RealHandles.Count} real handles");
        var nationalities = w.Content.Nationalities.Select(n => n.Id).ToHashSet();
        Check("N10", "Nationality, age and role distributions are valid", initialNpcs.All(p => p.CreatedSeason - p.Core.BornSeason is >= 16 and <= 34)
            && initialNpcs.GroupBy(p => p.Core.Role).All(g => g.Count() == cfg.Population.NpcCount / 5) && initialNpcs.All(p => nationalities.Contains(p.Core.Nationality)),
            $"roles {string.Join("/", initialNpcs.GroupBy(p => p.Core.Role).OrderBy(g => Roles.All.IndexOf(g.Key)).Select(g => g.Count()))}; ages {initialNpcs.Min(p => p.CreatedSeason - p.Core.BornSeason)}-{initialNpcs.Max(p => p.CreatedSeason - p.Core.BornSeason)}");

        // ---- the market lifecycle (simulated seasons)
        var intake = simulation.Where(e => e.Type == EventType.GeneratedProspect).Select(e => e.PersonId).ToHashSet();
        var intakeSigned = simulation.Count(e => intake.Contains(e.PersonId) && e.Type is EventType.FirstAmateurContract or EventType.FirstProfessionalContract or EventType.AcademyRecruitment);
        Check("M01", "A never-contracted prospect enters the market and signs", intake.Count > 0 && intakeSigned > 0, $"{intake.Count} prospects entered; {intakeSigned} first contracts among them");
        Check("M02", "Contracts expire into free agency", Count(EventType.ContractExpired) > 0, $"{Count(EventType.ContractExpired)} expiries; {Count(EventType.ContractExtension)} extensions");
        Check("M03", "Players are released or dismissed into free agency", Count(EventType.Released) + Count(EventType.Dismissed) > 0, $"{Count(EventType.Released)} released, {Count(EventType.Dismissed)} dismissed");
        Check("M04", "Free agents sign new teams", Count(EventType.Signed) + Count(EventType.FirstAmateurContract) + Count(EventType.FirstProfessionalContract) > 0,
            $"{Count(EventType.Signed)} signings, {Count(EventType.FirstAmateurContract)} first amateur and {Count(EventType.FirstProfessionalContract)} first professional contracts");
        var history = (List<string>)report["final_history_violations"];
        Check("M05", "Team changes never duplicate ownership (history replay)", history.Count == 0 && Count(EventType.Transfer) > 0, $"{Count(EventType.Transfer)} transfers; {history.Count} history violations");
        var upward = simulation.Count(e => e.Reason.StartsWith("recruited from a lower tier to a professional team", StringComparison.Ordinal));
        Check("M06", "Amateur or semi-pro players are recruited professionally", upward > 0, $"{upward} moves from amateur/semi-pro to professional teams");
        Check("M07", "Academy players are promoted", Count(EventType.SeniorPromotion) > 0, $"{Count(EventType.SeniorPromotion)} promotions, {Count(EventType.AcademyNotRetained)} not retained");
        Check("M08", "Players retire", Count(EventType.Retired) > 0, $"{Count(EventType.Retired)} retirements ({simulation.Count(e => e.Type == EventType.Retired && w.People[e.PersonId].Origin == Origin.Real)} real)");
        var filled = states.Sum(s => ((Dictionary<string, int>)s["flows"]).GetValueOrDefault("starter_filled"));
        Check("M09", "Teams lose players and fill the vacancies", filled > 0, $"{filled} starting vacancies filled in the windows");
        var series = (List<double>)report["free_agent_percent_series"];
        Check("M10", "The free-agent share moves with the market", series.Distinct().Count() > 1, $"NPC free-agent % by season: {string.Join(" ", series)}");
        Check("M11", "Historical events are append-only", (bool)report["snapshot_history_unchanged"] && history.Count == 0, "snapshot-era hash unchanged; replay consistent");

        // ---- persistence
        var persistence = (Dictionary<string, object>)report["persistence"];
        Check("P01", "Save then load reproduces the state", (bool)persistence["initial_reload_equal"] && (bool)persistence["final_reload_equal"] && (bool)persistence["continue_after_reload_equal"],
            $"initial {persistence["initial_bytes"]:N0} B, final {persistence["final_bytes"]:N0} B");
        Check("P02", "Pack order does not change the world", (bool)det["shuffled_pack_initial_equal"], "teams of the pack shuffled before loading");
        return r;
    }
}
