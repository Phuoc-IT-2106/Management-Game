using System.Diagnostics;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>D-H3 measurements (EXPERIMENT): the S1/S2/S3 benchmark over a simulated campaign, the crash-consistency
/// scenarios by fault injection, and real process-kill trials.</summary>
public static class PersistenceRuns
{
    private static double Ms(Stopwatch w) => Math.Round(w.Elapsed.TotalMilliseconds, 1);

    public static IWorldStore Make(string name, string dir) => name switch
    {
        "S1" => new S1Store(Path.Combine(dir, "s1"), false),
        "S1-gzip" => new S1Store(Path.Combine(dir, "s1gz"), true),
        "S2" => new S2Store(Path.Combine(dir, "s2")),
        "S2-reverse-order" => new S2Store(Path.Combine(dir, "s2r"), journalFirst: false),
        "S3-sqlite" => new S3Store(Path.Combine(dir, "s3", "world.db")),
        _ => throw new ArgumentException(name)
    };

    private static void Close(IWorldStore s) { if (s is S3Store x) x.Close(); }

    /// <summary>Unique, contiguous event ids 1..NextEvent.</summary>
    public static bool EventsSound(World w) =>
        w.Events.Count == w.NextEvent && w.Events.Select(StoreFiles.Seq).SequenceEqual(Enumerable.Range(1, w.Events.Count).Select(x => (long)x));

    // ------------------------------------------------------------------ benchmark
    public static Dictionary<string, object> Bench(World w, Calibration cal, StoreContext ctx, int seasons, int[] checkpoints, string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        var names = new[] { "S1", "S1-gzip", "S2", "S3-sqlite" };
        var stores = names.ToDictionary(n => n, n => Make(n, dir));
        var saves = names.ToDictionary(n => n, _ => new List<double>());
        var rows = new List<Dictionary<string, object>>();
        var rng = new Random(20261015);
        Dictionary<string, World>? keep = null; string? liveDigest11 = null;
        for (var s = 0; s <= seasons; s++)
        {
            if (s > 0) SeasonSim.Play(w, cal);
            if (keep is not null && s == 11)
            {
                liveDigest11 = Decisions.Digest(w);
                foreach (var k in keep.Values) SeasonSim.Play(k, cal);
            }
            foreach (var n in names)
            {
                var sw = Stopwatch.StartNew(); stores[n].Save(w); saves[n].Add(Ms(sw));
            }
            if (!checkpoints.Contains(s)) continue;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var liveHash = Persistence.Hash(Persistence.Serialize(w));
            var row = new Dictionary<string, object>
            {
                ["season"] = s, ["people"] = w.People.Count, ["teams"] = w.Teams.Count, ["contracts"] = w.Contracts.Count, ["events"] = w.Events.Count,
                ["largest_array"] = new[] { w.People.Count, w.Teams.Count, w.Contracts.Count, w.Events.Count }.Max(),
                ["events_this_season"] = w.Events.Count(e => e.Season == s && e.Era == Era.Simulation)
            };
            var persons = w.People.Keys.OrderBy(_ => rng.Next()).Take(200).ToArray();
            var team = w.Teams.Values.Where(t => t.Real).OrderBy(t => t.Id, StringComparer.Ordinal).First().Id;
            foreach (var n in names)
            {
                var store = stores[n];
                var block = new Dictionary<string, object> { ["bytes"] = store.Bytes(), ["save_ms"] = saves[n][^1] };
                if (store is S2Store s2) { block["snapshot_bytes"] = s2.SnapshotBytes(); block["journal_bytes"] = s2.JournalBytes(); }
                Close(store);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                var before = GC.GetTotalMemory(true);
                var sw = Stopwatch.StartNew();
                var (loaded, report) = store.Load(ctx);
                block["load_ms"] = Ms(sw);
                block["memory_after_load_mb"] = Math.Round((GC.GetTotalMemory(true) - before) / 1e6, 1);
                block["load_actions"] = report.Actions;
                block["state_hash_equal"] = Persistence.Hash(Persistence.Serialize(loaded)) == liveHash;
                if (s >= 10) block["history_violations"] = Checks.History(loaded).Count;
                block["events_sound"] = EventsSound(loaded);
                // history queries over the loaded world: an index by person, then 200 careers
                sw.Restart(); var index = Reports.EventsByPerson(loaded); block["query_index_build_ms"] = Ms(sw);
                sw.Restart(); var found = persons.Sum(p => index.GetValueOrDefault(p)?.Count ?? 0); block["query_200_careers_indexed_ms"] = Ms(sw);
                sw.Restart(); var transfers = loaded.Events.Count(e => e.Season == Math.Max(1, s) && e.Type == EventType.Transfer); block["query_season_transfers_scan_ms"] = Ms(sw);
                sw.Restart(); var membership = loaded.Events.Count(e => e.From == team || e.To == team); block["query_team_history_scan_ms"] = Ms(sw);
                block["query_results"] = new[] { found, transfers, membership };
                if (s == 10 && n is "S2" or "S3-sqlite") { keep ??= []; keep[n] = loaded; }
                loaded = null!; index = null!;
                // state-only loads and history straight from storage
                if (store is S2Store x2)
                {
                    GC.Collect(); before = GC.GetTotalMemory(true);
                    sw.Restart(); var (state, _) = x2.Load(ctx, stateOnly: true); block["state_only_load_ms"] = Ms(sw);
                    block["state_only_memory_mb"] = Math.Round((GC.GetTotalMemory(true) - before) / 1e6, 1); state = null!;
                    sw.Restart(); var c = x2.CareerFromDisk(persons[0]); block["query_1_career_from_journal_ms"] = Ms(sw);
                }
                if (store is S3Store x3)
                {
                    GC.Collect(); before = GC.GetTotalMemory(true);
                    sw.Restart(); var (state, _) = x3.Load(ctx, stateOnly: true); block["state_only_load_ms"] = Ms(sw);
                    block["state_only_memory_mb"] = Math.Round((GC.GetTotalMemory(true) - before) / 1e6, 1); state = null!;
                    sw.Restart(); var c = persons.Sum(p => x3.Career(p)); block["query_200_careers_sql_ms"] = Ms(sw);
                    sw.Restart(); var t2 = x3.Transfers(Math.Max(1, s)); block["query_season_transfers_sql_ms"] = Ms(sw);
                    sw.Restart(); var m2 = x3.TeamHistory(team); block["query_team_history_sql_ms"] = Ms(sw);
                    block["sql_results"] = new[] { c, t2, m2 };
                }
                row[n] = block;
            }
            rows.Add(row);
            Console.WriteLine($"persist bench season {s}: " + string.Join(", ", names.Select(n => $"{n} {((Dictionary<string, object>)row[n])["bytes"]:N0} B save {saves[n][^1]} ms load {((Dictionary<string, object>)row[n])["load_ms"]} ms")));
        }
        var continuation = keep?.ToDictionary(k => k.Key, k => (object)(Decisions.Digest(k.Value) == liveDigest11)) ?? [];
        foreach (var n in names) Close(stores[n]);
        return new Dictionary<string, object>
        {
            ["sqlite_version"] = S3Store.Version, ["checkpoints"] = rows, ["save_ms_by_season"] = saves,
            ["continue_after_load_equal_s10_to_s11"] = continuation
        };
    }

    // ------------------------------------------------------------------ fault-injection scenarios
    public sealed record Scenario(string Id, string Store, string What, bool Pass, string Evidence);

    /// <summary>Each scenario: a clean save at season S, one more season played (P transfers in it), a save interrupted at a
    /// chosen point (or a corruption), then a fresh process-like reload with recovery, and checks of ownership, history,
    /// duplicates; then the season replayed and saved again (retry).</summary>
    public static List<Scenario> CrashTests(byte[] baseSnapshot, Calibration cal, StoreContext ctx, string dir)
    {
        var results = new List<Scenario>();
        World Fresh() => Persistence.Restore(baseSnapshot, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
        var probe = Fresh(); var s0 = probe.Season;
        SeasonSim.Play(probe, cal);
        var next = probe; var nextDigest = Decisions.Digest(next);
        var move = next.Events.First(e => e.Season == s0 + 1 && e.Era == Era.Simulation && e.Type == EventType.Transfer);
        var p = move.PersonId; var formerTeam = move.From; var newTeam = move.To;
        var baseDigest = Decisions.Digest(Fresh());
        string Where(World w) => w.People[p].TeamId ?? "-";
        string LastHistoryTeam(World w) => w.Events.Where(e => e.PersonId == p && e.Type is not (EventType.RealRegistrationObserved or EventType.ContractExtension)).Select(e => e.To ?? "-").LastOrDefault() ?? "-";

        void Run(string id, string storeName, string what, CrashPoint crash, Action<string>? corrupt, int expectSeason, bool expectLoad = true, bool naive = false)
        {
            var d = Path.Combine(dir, id);
            if (Directory.Exists(d)) Directory.Delete(d, true);
            var store = Make(storeName, d);
            var w = Fresh(); store.Save(w);                     // acknowledged save at season s0
            SeasonSim.Play(w, cal);                             // season s0 + 1 in memory: P moves formerTeam -> newTeam
            try { store.Save(w, crash); } catch (SimulatedCrash) { }
            Close(store);
            corrupt?.Invoke(d);
            var restarted = Make(storeName, d);               // a new process: no in-memory writer state
            var evidence = new List<string>();
            bool pass;
            if (naive && restarted is S2Store n2)
            {
                var bad = n2.NaiveLoad(ctx);
                var violations = Checks.History(bad);
                evidence.Add($"naive loader: state season {bad.Season}, P at {(Where(bad) == formerTeam ? "former team" : Where(bad) == newTeam ? "new team" : Where(bad))}, history ends at {(LastHistoryTeam(bad) == formerTeam ? "former team" : LastHistoryTeam(bad) == newTeam ? "new team" : LastHistoryTeam(bad))}, {violations.Count} history violations");
            }
            try
            {
                var sw = Stopwatch.StartNew();
                var (loaded, report) = restarted.Load(ctx);
                var recoveryMs = Ms(sw);
                var violations = Checks.History(loaded).Count;
                var sound = EventsSound(loaded);
                var digestOk = Decisions.Digest(loaded) == (loaded.Season == s0 ? baseDigest : nextDigest);
                var loadedSeason = loaded.Season; var position = Where(loaded);
                evidence.Add($"loaded {report.Source} season {loaded.Season} in {recoveryMs} ms; P at {(Where(loaded) == formerTeam ? "former team" : Where(loaded) == newTeam ? "new team" : Where(loaded))}; history replay violations {violations}; ids unique and contiguous {sound}; state equals the acknowledged save {digestOk}");
                if (report.Actions.Count > 0) evidence.Add("recovery: " + string.Join("; ", report.Actions));
                // retry: replay the lost season from what was loaded and save again
                var retryOk = true;
                if (loaded.Season == s0)
                {
                    SeasonSim.Play(loaded, cal);
                    restarted.Save(loaded); Close(restarted);
                    var reopened = Make(storeName, d);
                    var (again, _) = reopened.Load(ctx); Close(reopened);
                    retryOk = Decisions.Digest(again) == nextDigest && EventsSound(again) && Checks.History(again).Count == 0;
                    evidence.Add($"retry (season replayed and saved): equals the uninterrupted season {Decisions.Digest(again) == nextDigest}, no duplicate events {EventsSound(again)}");
                }
                pass = expectLoad && loadedSeason == expectSeason && violations == 0 && sound && digestOk && retryOk
                       && position == (loadedSeason == s0 ? formerTeam : newTeam);
            }
            catch (InvalidDataException e)
            {
                evidence.Add("load refused: " + e.Message);
                pass = !expectLoad;
            }
            Close(restarted);
            results.Add(new Scenario(id, storeName, what, pass, string.Join(" | ", evidence)));
            Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {id} {storeName}: {what}" + (pass ? "" : " -- " + string.Join(" | ", evidence)));
        }

        void FlipJournalByte(string d, double at)
        {
            var path = Path.Combine(d, "s2", "journal.log"); var b = File.ReadAllBytes(path);
            var i = (int)(b.Length * at); while (b[i] is (byte)'\t' or (byte)'\n') i++;
            b[i] = b[i] == (byte)'a' ? (byte)'b' : (byte)'a'; File.WriteAllBytes(path, b);
        }
        void FlipFileByte(string path, double at)
        {
            var b = File.ReadAllBytes(path); var i = Math.Max(200, (int)(b.Length * at)); b[i] ^= 0x20; File.WriteAllBytes(path, b);
        }

        // S1: state and history in one atomic file
        Run("S1-T1", "S1", "crash while writing the temporary snapshot", CrashPoint.MidSnapshotWrite, null, s0);
        Run("S1-T2", "S1", "crash after the temporary snapshot, before the replace", CrashPoint.AfterSnapshotTemp, null, s0);
        Run("S1-T3", "S1", "primary snapshot corrupted after a completed save (rolls back one save)", CrashPoint.None, d => FlipFileByte(Path.Combine(d, "s1", "world.s1"), 0.5), s0);
        Run("S1-T4", "S1", "completed save (control)", CrashPoint.None, null, s0 + 1);
        // S2: snapshot + journal, journal first (the critical scenario of the task, section 17)
        Run("S2-T1", "S2", "transfer written to the journal, crash before the snapshot commit (naive loader shown first)", CrashPoint.AfterJournalAppend, null, s0, naive: true);
        Run("S2-T2", "S2", "torn journal append (half the season's records, last one partial)", CrashPoint.MidJournalAppend, null, s0);
        Run("S2-T3", "S2", "journal appended, crash while writing the temporary snapshot", CrashPoint.MidSnapshotWrite, null, s0);
        Run("S2-T4", "S2", "journal appended, crash after the temporary snapshot, before the replace", CrashPoint.AfterSnapshotTemp, null, s0);
        Run("S2-T5", "S2", "completed save (control)", CrashPoint.None, null, s0 + 1);
        Run("S2-T6", "S2", "primary snapshot corrupted after a completed save (rolls back one save, journal truncated to match)", CrashPoint.None, d => FlipFileByte(Path.Combine(d, "s2", "state.s2"), 0.5), s0);
        Run("S2-T7", "S2", "a committed journal record corrupted (history before every checkpoint): detection, fail closed", CrashPoint.None, d => FlipJournalByte(d, 0.3), s0, expectLoad: false);
        // S2 with the snapshot written first (the wrong order): detected and rolled back, never loaded inconsistent
        Run("S2R-T1", "S2-reverse-order", "snapshot committed, crash before the journal append (naive loader shown first)", CrashPoint.AfterSnapshotBeforeJournal, null, s0, naive: true);
        // S3: one transaction (an in-process failure rolls back; real kills are in the kill trials)
        Run("S3-T1", "S3-sqlite", "failure inside the transaction before COMMIT (rollback)", CrashPoint.AfterJournalAppend, null, s0);
        Run("S3-T2", "S3-sqlite", "completed save (control)", CrashPoint.None, null, s0 + 1);
        Run("S3-T3", "S3-sqlite", "a byte flipped inside the stored state blob: detection, fail closed", CrashPoint.None, d =>
        {
            var path = Path.Combine(d, "s3", "world.db"); var b = File.ReadAllBytes(path);
            var marker = System.Text.Encoding.UTF8.GetBytes("\"People\":[");
            // every copy (freed pages keep older blobs), so the live blob is certainly hit
            var hits = 0;
            for (var at = b.AsSpan().IndexOf(marker); at >= 0 && at + marker.Length + 50 < b.Length; hits++)
            {
                b[at + marker.Length + 50] ^= 0x01;
                var next = b.AsSpan(at + 1).IndexOf(marker); at = next < 0 ? -1 : at + 1 + next;
            }
            if (hits == 0) throw new InvalidOperationException("state blob not found");
            File.WriteAllBytes(path, b);
        }, s0, expectLoad: false);
        Run("S3-T3b", "S3-sqlite", "B-tree page headers damaged (pages 2-5): detection, fail closed", CrashPoint.None, d =>
        {
            var path = Path.Combine(d, "s3", "world.db"); var b = File.ReadAllBytes(path);
            for (var page = 1; page <= 4; page++) b[page * 4096] = 0x00;
            File.WriteAllBytes(path, b);
        }, s0, expectLoad: false);
        Run("S3-T4", "S3-sqlite", "silent content change inside one event row (structurally valid; simulated by a direct UPDATE): detection, fail closed", CrashPoint.None, d =>
        {
            using var db = new SqliteDb(Path.Combine(d, "s3", "world.db"));
            db.Exec("UPDATE events SET dst = 'team:tampered' WHERE seq = 100");
        }, s0, expectLoad: false);

        // Deduplication: an acknowledged save retried (the same world saved twice)
        foreach (var name in new[] { "S1", "S2", "S3-sqlite" })
        {
            var d = Path.Combine(dir, "dup-" + name); if (Directory.Exists(d)) Directory.Delete(d, true);
            var store = Make(name, d); var w = Fresh(); store.Save(w); SeasonSim.Play(w, cal); store.Save(w); store.Save(w); Close(store);
            var fresh = Make(name, d); var (again, _) = fresh.Load(ctx); Close(fresh);
            var ok = EventsSound(again) && Decisions.Digest(again) == nextDigest;
            results.Add(new Scenario("DUP-" + name, name, "an acknowledged save retried (saved twice)", ok, $"events {again.Events.Count} = counter {again.NextEvent}; unique and contiguous {EventsSound(again)}; state equals the season {Decisions.Digest(again) == nextDigest}"));
            Console.WriteLine($"{(ok ? "PASS" : "FAIL")} DUP-{name}");
        }
        return results;
    }

    // ------------------------------------------------------------------ process-kill trials
    public sealed record KillTrial(string Store, int Trial, int KillAfterSave, int DelayMs, bool Killed, int AckedSeason, int LoadedSeason, bool Pass, string Evidence);

    /// <summary>The child side: recover the store, then play and save seasons, writing an acknowledgement (season, digest,
    /// event counter) with a flushed append after each completed save.</summary>
    public static int Child(string storeName, string dir, int seasons, Calibration cal, StoreContext ctx)
    {
        var store = Make(storeName, dir);
        var (w, _) = store.Load(ctx);
        Console.WriteLine($"LOADED {w.Season}"); Console.Out.Flush();
        for (var k = 0; k < seasons; k++)
        {
            SeasonSim.Play(w, cal);
            var digest = Decisions.Digest(w);
            Console.WriteLine($"SAVING {w.Season}"); Console.Out.Flush();
            store.Save(w);
            using (var ack = new FileStream(Path.Combine(dir, "acks.txt"), FileMode.Append, FileAccess.Write, FileShare.None))
            {
                var line = System.Text.Encoding.ASCII.GetBytes($"{w.Season}|{digest}|{w.NextEvent}\n");
                ack.Write(line); ack.Flush(true);
            }
            Console.WriteLine($"ACK {w.Season}"); Console.Out.Flush();
        }
        Close(store);
        return 0;
    }

    public static List<KillTrial> KillTests(byte[] baseSnapshot, Calibration cal, StoreContext ctx, string dir, string exe, int trials)
    {
        var saveMs = new Dictionary<string, double>();
        var results = new List<KillTrial>();
        var baseWorld = Persistence.Restore(baseSnapshot, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
        var baseSeason = baseWorld.Season; var baseDigest = Decisions.Digest(baseWorld);
        foreach (var name in new[] { "S1", "S2", "S3-sqlite" })
        {
            var template = Path.Combine(dir, "base-" + name);
            if (Directory.Exists(template)) Directory.Delete(template, true);
            var s = Make(name, template); var timer = Stopwatch.StartNew(); s.Save(baseWorld); saveMs[name] = timer.Elapsed.TotalMilliseconds; Close(s);
            for (var t = 0; t < trials; t++)
            {
                var trialDir = Path.Combine(dir, $"{name}-{t:D2}");
                if (Directory.Exists(trialDir)) Directory.Delete(trialDir, true);
                Copy(template, trialDir);
                var killAfter = 1 + t % 2;   // kill during the first or the second save
                var delay = (int)Math.Round(saveMs[name] * 1.3 * ((t * 7919 % 100) / 100.0));
                var psi = new ProcessStartInfo(exe, $"persist-child --store {name} --dir \"{trialDir}\" --seasons 3")
                { RedirectStandardOutput = true, UseShellExecute = false, WorkingDirectory = Directory.GetCurrentDirectory() };
                using var child = Process.Start(psi)!;
                var killed = false; var saving = 0;
                string? line;
                while ((line = child.StandardOutput.ReadLine()) is not null)
                {
                    if (!line.StartsWith("SAVING", StringComparison.Ordinal)) continue;
                    if (++saving != killAfter) continue;
                    Thread.Sleep(delay);
                    try { child.Kill(entireProcessTree: true); killed = true; } catch (InvalidOperationException) { }
                    break;
                }
                child.WaitForExit();
                var acks = File.Exists(Path.Combine(trialDir, "acks.txt")) ? File.ReadAllLines(Path.Combine(trialDir, "acks.txt")).Where(x => x.Length > 0).ToList() : [];
                var ackSeason = baseSeason; var ackDigest = baseDigest; long ackEvents = baseWorld.NextEvent;
                if (acks.Count > 0) { var f = acks[^1].Split('|'); ackSeason = int.Parse(f[0]); ackDigest = f[1]; ackEvents = long.Parse(f[2]); }
                var evidence = new List<string>(); var pass = false; var loadedSeason = -1;
                try
                {
                    var store = Make(name, trialDir);
                    var (loaded, report) = store.Load(ctx); Close(store);
                    loadedSeason = loaded.Season;
                    var violations = Checks.History(loaded).Count; var sound = EventsSound(loaded);
                    var exact = loaded.Season != ackSeason || Decisions.Digest(loaded) == ackDigest;
                    pass = (loaded.Season == ackSeason || loaded.Season == ackSeason + 1) && violations == 0 && sound && exact && loaded.NextEvent >= ackEvents;
                    evidence.Add($"acknowledged season {ackSeason}, loaded season {loaded.Season}; history violations {violations}; ids sound {sound}; equals the acknowledged state {exact}; acknowledged events kept {loaded.NextEvent >= ackEvents}");
                    if (report.Actions.Count > 0) evidence.Add("recovery: " + string.Join("; ", report.Actions));
                }
                catch (Exception e) when (e is InvalidDataException or IOException) { evidence.Add("load failed: " + e.Message); }
                results.Add(new KillTrial(name, t, killAfter, delay, killed, ackSeason, loadedSeason, pass, string.Join(" | ", evidence)));
                Console.WriteLine($"{(pass ? "PASS" : "FAIL")} kill {name} #{t} after save {killAfter} +{delay} ms (killed {killed}): {string.Join(" | ", evidence)}");
            }
        }
        return results;
    }

    private static void Copy(string from, string to)
    {
        foreach (var d in Directory.GetDirectories(from, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(d.Replace(from, to));
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from, "*", SearchOption.AllDirectories)) File.Copy(f, f.Replace(from, to), true);
    }
}
