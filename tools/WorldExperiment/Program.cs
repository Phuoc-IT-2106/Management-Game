using System.Diagnostics;
using System.Text.Json;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;
using WorldExperiment;
using World = WorldExperiment.World;
using Calibration = WorldExperiment.Calibration;

// World experiment (EXPERIMENT, not production): dev2 development, the real-roster pack and the 4,000-NPC world.
//   dotnet run --project tools/WorldExperiment -c Release -- develop|world|test|bench|all [--seasons N] [--replicates N] [--out DIR]
var root = FindRoot();
var command = args.Length > 0 ? args[0] : "all";
string Option(string name, string fallback) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
var dataWorld = Path.Combine(root, "tools", "lol-data-ingestion", "data", "world");
var output = Option("--out", Path.Combine(dataWorld, "experiment"));
Directory.CreateDirectory(output);
var seasons = int.Parse(Option("--seasons", "10"));
var replicates = int.Parse(Option("--replicates", "1000"));
var config = ExperimentConfig.Load(Option("--config", Path.Combine(root, "tools", "WorldExperiment", "config", "world-experiment.v1.json")));
// Sensitivity variants (never the rules): another seed, a fixed intake, or real teams allowed to start NPCs.
config = config with { Seed = ulong.Parse(Option("--seed", config.Seed.ToString())) };
var variant = Option("--variant", "baseline");
if (variant == "fixed-intake") config = config with { Market = config.Market with { IntakeMode = "Fixed" } };
if (variant == "npc-fallback") config = config with { World = config.World with { RealStarterPolicy = "NPC_FALLBACK" } };
var content = ContentLoader.Load(Path.Combine(root, "content", "fixture.json"));
var pack = Directory.GetFiles(dataWorld, "real_roster_snapshot_*.json").Order(StringComparer.Ordinal).Last();
var calibration = Calibration.Load(Path.Combine(dataWorld, "npc_calibration_2026.json"));
var (realHandles, realTeams) = RealPack.Exclusions(Path.Combine(dataWorld, "real_handle_exclusions.json"));
var json = new JsonSerializerOptions { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
void Write(string name, object value) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(value, json));

(World World, RealPack.LoadResult Real, WorldBuilder.BuildResult Build) Create(bool shuffle = false)
{
    var w = new World { Config = config, Content = content.Definition, Seed = config.Seed, AsOf = "2026-10-13", RealHandles = realHandles, RealTeamNames = realTeams };
    var real = RealPack.Load(w, pack, shuffle);
    var build = WorldBuilder.Build(w, calibration);
    return (w, real, build);
}

var exit = 0;
if (command == "decide")
{
    // Decision verification runs (HYBRID_WORLD_DECISION_VERIFICATION): one variant over a seed list.
    //   decide --label B --policy NPC_FALLBACK --intake OneToOne --ceiling base|d1a [--elite FILE] --seeds 1-48 --seasons 20 --out DIR
    var decisions = DecisionConfig.Load(Option("--decisions", Path.Combine(root, "tools", "WorldExperiment", "config", "decisions.v1.json")));
    var elitePath = Option("--elite", "");
    var v = new Decisions.Variant(Option("--label", "A"), Option("--policy", "STRICT"), Option("--intake", "OneToOne"), Option("--ceiling", "base"),
        elitePath.Length > 0 ? elitePath : null);
    var elite = Decisions.LoadElite(v.ElitePath);
    var seedText = Option("--seeds", config.Seed.ToString());
    var seedList = seedText.Contains('-') ? Enumerable.Range(int.Parse(seedText.Split('-')[0]), int.Parse(seedText.Split('-')[1]) - int.Parse(seedText.Split('-')[0]) + 1).Select(x => (ulong)x).ToList()
        : seedText.Split(',').Select(ulong.Parse).ToList();
    var dir = Path.Combine(output, v.Label);
    Directory.CreateDirectory(dir);
    foreach (var seed in seedList)
    {
        var file = Path.Combine(dir, $"seed-{seed:D3}.json");
        if (File.Exists(file) && args.Contains("--resume")) continue;
        var cfg = Decisions.Apply(config with { Seed = seed }, v, decisions);
        var total = Stopwatch.StartNew();
        var w = new World { Config = cfg, Content = content.Definition, Seed = seed, AsOf = "2026-10-13", RealHandles = realHandles, RealTeamNames = realTeams };
        if (v.Intake == "Pathways") w.Pathways = decisions.Pathways;
        var real = RealPack.Load(w, pack, false, elite);
        WorldBuilder.Build(w, calibration);
        var createSeconds = Math.Round(total.Elapsed.TotalSeconds, 3);
        var initialFailures = Checks.Invariants(w, initial: true);
        var digest0 = Decisions.Digest(w);
        var result = Decisions.Run(w, calibration, decisions, seasons);
        result["digest_s0"] = digest0;
        result["variant"] = v; result["seed"] = seed; result["config_version"] = cfg.Version; result["content_hash"] = content.Hash;
        result["pack_sha256"] = real.PackSha256; result["real_pack"] = new Dictionary<string, int> { ["teams"] = real.Teams, ["players"] = real.Players, ["rating_mismatches"] = real.RatingMismatches };
        result["elite_adjusted_players"] = elite?.Count ?? 0;
        result["initial_invariant_failures"] = initialFailures.Count;
        result["create_seconds"] = createSeconds; result["total_seconds"] = Math.Round(total.Elapsed.TotalSeconds, 3);
        result["digest"] = Decisions.Digest(w);
        result["peak_working_set_bytes"] = Process.GetCurrentProcess().PeakWorkingSet64;
        File.WriteAllText(file, JsonSerializer.Serialize(result, json));
        var last = (Dictionary<string, object>)((List<Dictionary<string, object>>)result["checkpoints"])[^1];
        var rt = (Dictionary<string, object>)last["real_teams"];
        Console.WriteLine($"{v.Label} seed {seed}: {result["total_seconds"]} s; real active {rt["active"]}, authentic five {rt["authentic_five"]}, fielding five {rt["fielding_five"]}; NPC FA {last["npc_fa_pct"]}%; invariants {last["invariant_failures"]}; conservation {result["conservation_violations"]}");
    }
    return 0;
}
if (command is "persist" or "persist-crash" or "persist-kill" or "persist-child")
{
    // D-H3 persistence alternatives (HYBRID_WORLD_DECISION_VERIFICATION): benchmark, fault injection, process kills.
    var decisions = DecisionConfig.Load(Option("--decisions", Path.Combine(root, "tools", "WorldExperiment", "config", "decisions.v1.json")));
    var storeConfig = Decisions.Apply(config, new Decisions.Variant("persist", Option("--policy", "NPC_FALLBACK"), "OneToOne", "base", null), decisions);
    var ctx = new StoreContext(storeConfig, content.Definition, realHandles, realTeams);
    var dir = Option("--dir", Path.Combine(output, "persistence"));
    if (command == "persist-child") return PersistenceRuns.Child(Option("--store", "S2"), dir, int.Parse(Option("--seasons", "3")), calibration, ctx);
    var world = new World { Config = storeConfig, Content = content.Definition, Seed = storeConfig.Seed, AsOf = "2026-10-13", RealHandles = realHandles, RealTeamNames = realTeams };
    RealPack.Load(world, pack);
    WorldBuilder.Build(world, calibration);
    if (command == "persist")
    {
        var bench = PersistenceRuns.Bench(world, calibration, ctx, seasons, [0, 1, 5, 10, 20], Path.Combine(dir, "bench"));
        Write("persistence_bench.json", bench);
        return 0;
    }
    var baseSeasons = int.Parse(Option("--base-seasons", "10"));
    for (var s = 1; s <= baseSeasons; s++) SeasonSim.Play(world, calibration);
    var baseBytes = Persistence.Serialize(world);
    world = null!;
    if (command == "persist-crash")
    {
        var scenarios = PersistenceRuns.CrashTests(baseBytes, calibration, ctx, Path.Combine(dir, "crash"));
        Write("persistence_crash.json", scenarios);
        Console.WriteLine($"{scenarios.Count(x => x.Pass)}/{scenarios.Count} scenarios passed");
        return scenarios.All(x => x.Pass) ? 0 : 1;
    }
    var kills = PersistenceRuns.KillTests(baseBytes, calibration, ctx, Path.Combine(dir, "kill"), Environment.ProcessPath!, int.Parse(Option("--trials", "8")));
    Write("persistence_kill.json", kills);
    Console.WriteLine($"{kills.Count(x => x.Pass)}/{kills.Count} kill trials passed ({kills.Count(x => x.Killed)} killed)");
    return kills.All(x => x.Pass) ? 0 : 1;
}
if (command is "develop" or "all")
{
    var w = new World { Config = config, Content = content.Definition, Seed = config.Seed };
    var result = DevelopmentExperiment.Run(w, replicates, 8);
    Write("development_experiment.json", result);
    Console.WriteLine($"development: {((List<Dictionary<string, object>>)result["rows"]).Count} cells x {replicates} replicates in {result["seconds"]} s");
}
if (command is "bench" or "all")
{
    var bench = ProductionBench.Run(content.Definition, config.Seed);
    Write("production_bench.json", bench);
    Console.WriteLine("bench: " + string.Join(", ", bench.Select(x => $"{x.Key}={x.Value}")));
}
if (command is "world" or "all" or "test")
{
    var timings = new Dictionary<string, double>();
    var process = Process.GetCurrentProcess();
    GC.Collect();
    var memoryBefore = GC.GetTotalMemory(true);
    var watch = Stopwatch.StartNew();
    var (w, real, build) = Create();
    timings["generation_and_allocation_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var memoryWorld = GC.GetTotalMemory(true) - memoryBefore;
    var initialInvariants = Checks.Invariants(w, initial: true);
    var initialHistory = Checks.History(w);
    var initialState = Reports.State(w);
    var generation = Reports.Generation(w);
    var snapshotEvents = w.Events.Where(e => e.Era != Era.Simulation).ToList();
    var snapshotHash = Checks.EventsHash(snapshotEvents);

    // Persistence at the start: save, load, save again.
    watch.Restart(); var bytes = Persistence.Serialize(w); timings["save_serialize_initial_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var initialFile = Path.Combine(output, "world_snapshot_s0.json");
    watch.Restart(); File.WriteAllBytes(initialFile, bytes); timings["save_write_initial_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    watch.Restart(); var loaded = Persistence.Restore(File.ReadAllBytes(initialFile), config, content.Definition, realHandles, realTeams);
    timings["load_initial_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var initialHash = Persistence.Hash(bytes);
    var reloadHash = Persistence.Hash(Persistence.Serialize(loaded));

    // Queries: free agents by role and level; scouting young high-potential mids across the whole population.
    watch.Restart();
    var faQueries = 0;
    for (var r = 0; r < 1000; r++)
        faQueries += w.People.Values.Count(p => p.Status == Status.FreeAgent && p.Core.Role == Roles.All[r % 5] && w.Overall(p) >= 60);
    timings["query_free_agents_by_role_ms"] = Math.Round(watch.Elapsed.TotalMilliseconds / 1000, 4);
    watch.Restart();
    var scouted = 0;
    for (var r = 0; r < 200; r++)
        scouted += w.People.Values.Where(p => p.Status != Status.Retired && p.Core.Role == "mid" && w.Age(p) <= 21 && p.Core.Potential >= 80)
            .OrderByDescending(p => p.Core.Potential).ThenBy(p => p.Id, StringComparer.Ordinal).Take(20).Count();
    timings["query_scouting_young_mid_top20_ms"] = Math.Round(watch.Elapsed.TotalMilliseconds / 200, 4);

    // Seasons.
    var seasonStates = new List<Dictionary<string, object>>();
    var seasonFailures = new Dictionary<string, List<string>>();
    var seasonTimes = new List<double>();
    var snapshotStable = true;
    var faSeries = new List<double>();
    for (var s = 1; s <= seasons; s++)
    {
        watch.Restart();
        var flows = SeasonSim.Play(w, calibration);
        seasonTimes.Add(Math.Round(watch.Elapsed.TotalSeconds, 3));
        var state = Reports.State(w, flows);
        seasonStates.Add(state);
        faSeries.Add((double)state["npc_free_agent_percent"]);
        var failures = Checks.Invariants(w, initial: false);
        if (failures.Count > 0) seasonFailures[$"s{s}"] = failures;
        snapshotStable &= Checks.EventsHash(w.Events.Where(e => e.Era != Era.Simulation)) == snapshotHash;
    }
    timings["season_mean_s"] = seasonTimes.Count > 0 ? Math.Round(seasonTimes.Average(), 3) : 0;
    timings["season_max_s"] = seasonTimes.Count > 0 ? seasonTimes.Max() : 0;
    var history = Checks.History(w);
    watch.Restart(); var finalBytes = Persistence.Serialize(w); timings["save_serialize_final_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var finalFile = Path.Combine(output, $"world_snapshot_s{seasons}.json");
    File.WriteAllBytes(finalFile, finalBytes);
    watch.Restart(); var finalLoaded = Persistence.Restore(File.ReadAllBytes(finalFile), config, content.Definition, realHandles, realTeams);
    timings["load_final_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var finalHash = Persistence.Hash(finalBytes);
    var finalReloadHash = Persistence.Hash(Persistence.Serialize(finalLoaded));
    // Continue one season from the reloaded snapshot and from the live world: they must agree.
    var continuedLive = Persistence.Hash(Persistence.Serialize(PlayOne(w)));
    var continuedLoaded = Persistence.Hash(Persistence.Serialize(PlayOne(finalLoaded)));
    var peak = process.PeakWorkingSet64;

    // Determinism: a second world from the same seed; order independence: the pack's teams shuffled.
    watch.Restart();
    var (w2, _, _) = Create();
    for (var s = 1; s <= seasons; s++) SeasonSim.Play(w2, calibration);
    PlayOne(w2);
    var replayHash = Persistence.Hash(Persistence.Serialize(w2));
    timings["replay_s"] = Math.Round(watch.Elapsed.TotalSeconds, 3);
    var (w3, _, _) = Create(shuffle: true);
    var shuffledHash = Persistence.Hash(Persistence.Serialize(w3));

    var report = new Dictionary<string, object>
    {
        ["experiment"] = config.Version, ["status"] = config.Status, ["seed"] = config.Seed, ["variant"] = variant, ["as_of"] = w.AsOf, ["seasons"] = seasons,
        ["development_cohorts"] = Reports.Cohorts(w),
        ["content_hash"] = content.Hash, ["calibration"] = calibration.Version + " / " + calibration.RatingModel,
        ["real_pack"] = real, ["build"] = build, ["generation"] = generation, ["initial_state"] = initialState,
        ["initial_invariant_failures"] = initialInvariants, ["initial_history_violations"] = initialHistory,
        ["seasons_state"] = seasonStates, ["season_invariant_failures"] = seasonFailures, ["final_history_violations"] = history,
        ["snapshot_history_unchanged"] = snapshotStable, ["warnings"] = w.Warnings,
        ["persistence"] = new Dictionary<string, object>
        {
            ["initial_bytes"] = bytes.Length, ["final_bytes"] = finalBytes.Length, ["initial_hash"] = initialHash, ["initial_reload_equal"] = initialHash == reloadHash,
            ["final_hash"] = finalHash, ["final_reload_equal"] = finalHash == finalReloadHash, ["continue_after_reload_equal"] = continuedLive == continuedLoaded,
            ["people"] = w.People.Count, ["teams"] = w.Teams.Count, ["contracts"] = w.Contracts.Count, ["events"] = w.Events.Count,
            ["largest_array"] = new[] { w.People.Count, w.Teams.Count, w.Contracts.Count, w.Events.Count }.Max()
        },
        ["determinism"] = new Dictionary<string, object>
        {
            ["replay_hash"] = replayHash, ["live_hash_after_extra_season"] = continuedLive, ["replay_equal"] = replayHash == continuedLive,
            ["shuffled_pack_initial_equal"] = shuffledHash == initialHash
        },
        ["timings"] = timings, ["season_seconds"] = seasonTimes,
        ["memory"] = new Dictionary<string, object> { ["world_managed_bytes"] = memoryWorld, ["peak_working_set_bytes"] = peak },
        ["queries"] = new Dictionary<string, object> { ["free_agent_matches_total"] = faQueries, ["scouted_total"] = scouted },
        ["free_agent_percent_series"] = faSeries, ["pre_window_market"] = w.PreWindow
    };
    Write("world_report.json", report);
    Console.WriteLine($"world: {generation["count"]} NPCs, FA {initialState["npc_free_agents"]} ({initialState["npc_free_agent_percent"]}%), real players {real.Players} on {real.Teams} teams");
    Console.WriteLine($"invariants at start: {initialInvariants.Count} failures; history: {initialHistory.Count}; seasons with failures: {seasonFailures.Count}; final history: {history.Count}");
    Console.WriteLine($"timings: {string.Join(", ", timings.Select(x => $"{x.Key}={x.Value}"))}");
    Console.WriteLine($"snapshot {bytes.Length:N0} -> {finalBytes.Length:N0} bytes; reload equal {initialHash == reloadHash}/{finalHash == finalReloadHash}; continue equal {continuedLive == continuedLoaded}; replay equal {replayHash == continuedLive}; shuffled equal {shuffledHash == initialHash}");
    Console.WriteLine($"FA % by season: {string.Join(" ", faSeries)}");
    if (command == "test")
    {
        var development = File.Exists(Path.Combine(output, "development_experiment.json"))
            ? JsonDocument.Parse(File.ReadAllBytes(Path.Combine(output, "development_experiment.json"))) : null;
        var results = Tests.Run(w, report, real, development);
        Write("test_results.json", results);
        foreach (var r in results) Console.WriteLine($"{(r.Passed ? "PASS" : "FAIL")} {r.Id} {r.Name}: {r.Detail}");
        Console.WriteLine($"{results.Count(r => r.Passed)}/{results.Count} passed");
        exit = results.All(r => r.Passed) ? 0 : 1;
    }
}
return exit;

World PlayOne(World world) { SeasonSim.Play(world, calibration); return world; }

static string FindRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ManagementGame.slnx"))) dir = dir.Parent;
    return dir?.FullName ?? throw new DirectoryNotFoundException("Run inside the repository.");
}
