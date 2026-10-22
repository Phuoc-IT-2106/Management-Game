using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WorldExperiment;

/// <summary>Where a simulated crash interrupts a save (fault injection; the files are left exactly as they are).</summary>
public enum CrashPoint { None, MidSnapshotWrite, AfterSnapshotTemp, MidJournalAppend, AfterJournalAppend, AfterSnapshotBeforeJournal }

public sealed class SimulatedCrash(CrashPoint point) : Exception($"simulated crash at {point}")
{
    public CrashPoint Point { get; } = point;
}

public sealed record StoreContext(ExperimentConfig Config, ManagementGame.Domain.Content Content, HashSet<string> RealHandles, HashSet<string> RealTeams);

/// <summary>What a load did: the file it accepted, the recovery actions it took and the checkpoint it ended at.</summary>
public sealed record LoadReport(string Source, int Season, long CheckpointSeq, List<string> Actions);

/// <summary>The three persistence alternatives of D-H3 (EXPERIMENT, not a production save format):
/// S1 one authoritative snapshot holding state and full history (optionally gzip-compressed);
/// S2 a state snapshot plus an append-only, hash-chained journal of career events, the snapshot naming its journal
/// checkpoint (sequence and chain head), written journal first;
/// S3 one SQLite database holding the state blob and the event table, both written in one transaction.
/// The snapshot files of S1 and S2 follow the production SnapshotStore protocol: a temporary file flushed to disk, then
/// File.Replace with two backup generations.</summary>
public interface IWorldStore
{
    string Name { get; }
    void Save(World w, CrashPoint crash = CrashPoint.None);
    (World World, LoadReport Report) Load(StoreContext ctx);
    long Bytes();
}

public static class StoreFiles
{
    public static string Sha(byte[] b) => Convert.ToHexString(SHA256.HashData(b)).ToLowerInvariant();

    public static byte[] Frame(string format, byte[] payload, int season, long seq, string chain)
    {
        var header = Encoding.ASCII.GetBytes($"{format}|1|{Sha(payload)}|{payload.Length}|{season}|{seq}|{chain}\n");
        var all = new byte[header.Length + payload.Length];
        header.CopyTo(all, 0); payload.CopyTo(all, header.Length);
        return all;
    }

    /// <summary>Reads and checks a framed file: format, length and SHA-256. Returns null when it is missing or invalid.</summary>
    public static (byte[] Payload, int Season, long Seq, string Chain)? Read(string path, string format)
    {
        if (!File.Exists(path)) return null;
        var all = File.ReadAllBytes(path);
        var end = Array.IndexOf(all, (byte)'\n');
        if (end < 0) return null;
        var parts = Encoding.ASCII.GetString(all, 0, end).Split('|');
        if (parts.Length != 7 || parts[0] != format || !int.TryParse(parts[3], out var length) || all.Length - end - 1 != length) return null;
        var payload = all.AsSpan(end + 1).ToArray();
        if (Sha(payload) != parts[2]) return null;
        return (payload, int.Parse(parts[4]), long.Parse(parts[5]), parts[6]);
    }

    /// <summary>Writes a framed file through a flushed temporary file and File.Replace with .bak1/.bak2 (the production
    /// SnapshotStore protocol), with crash injection between the steps.</summary>
    public static void Commit(string primary, byte[] framed, string format, CrashPoint crash)
    {
        var temp = primary + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (crash == CrashPoint.MidSnapshotWrite) { file.Write(framed, 0, framed.Length / 2); file.Flush(true); throw new SimulatedCrash(crash); }
            file.Write(framed); file.Flush(true);
        }
        if (crash == CrashPoint.AfterSnapshotTemp) throw new SimulatedCrash(crash);
        if (File.Exists(primary))
        {
            var bak1 = primary + ".bak1";
            if (File.Exists(bak1) && Read(bak1, format) is not null)
            {
                using (var input = File.OpenRead(bak1))
                using (var output = new FileStream(primary + ".bak2.tmp", FileMode.Create, FileAccess.Write, FileShare.None)) { input.CopyTo(output); output.Flush(true); }
                File.Move(primary + ".bak2.tmp", primary + ".bak2", true);
            }
            File.Replace(temp, primary, bak1);
        }
        else File.Move(temp, primary);
    }

    public static long Seq(CareerEvent e) => long.Parse(e.Id.AsSpan(3));

    public static byte[] StateOnly(World w)
    {
        var s = Persistence.Capture(w);
        s.Events = [];
        return JsonSerializer.SerializeToUtf8Bytes(s, Persistence.Options);
    }

    public static long DirBytes(string dir) => Directory.Exists(dir) ? new DirectoryInfo(dir).GetFiles().Sum(f => f.Length) : 0;
}

/// <summary>S1: one snapshot with state and history (the experiment's full JSON), optionally gzip-compressed.</summary>
public sealed class S1Store(string dir, bool gzip) : IWorldStore
{
    private string Format => gzip ? "S1GZ" : "S1";
    private string Primary => Path.Combine(dir, "world.s1");
    public string Name => gzip ? "S1-gzip" : "S1";
    public long Bytes() => File.Exists(Primary) ? new FileInfo(Primary).Length : 0;
    public long AllBytes() => StoreFiles.DirBytes(dir);

    public void Save(World w, CrashPoint crash = CrashPoint.None)
    {
        Directory.CreateDirectory(dir);
        var payload = Persistence.Serialize(w);
        if (gzip)
        {
            using var ms = new MemoryStream();
            using (var z = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true)) z.Write(payload);
            payload = ms.ToArray();
        }
        StoreFiles.Commit(Primary, StoreFiles.Frame(Format, payload, w.Season, w.NextEvent, "-"), Format, crash);
    }

    public (World World, LoadReport Report) Load(StoreContext ctx)
    {
        var actions = new List<string>();
        if (File.Exists(Primary + ".tmp")) { File.Delete(Primary + ".tmp"); actions.Add("removed an unfinished temporary file"); }
        foreach (var path in new[] { Primary, Primary + ".bak1", Primary + ".bak2" })
        {
            var read = StoreFiles.Read(path, Format);
            if (read is null) { if (File.Exists(path)) actions.Add($"rejected {Path.GetFileName(path)} (checksum or frame)"); continue; }
            var payload = read.Value.Payload;
            if (gzip)
            {
                using var z = new GZipStream(new MemoryStream(payload), CompressionMode.Decompress);
                using var ms = new MemoryStream(); z.CopyTo(ms); payload = ms.ToArray();
            }
            var world = Persistence.Restore(payload, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
            if (path != Primary) actions.Add($"rolled back to {Path.GetFileName(path)}");
            return (world, new LoadReport(Path.GetFileName(path), world.Season, world.NextEvent, actions));
        }
        throw new InvalidDataException("S1: no valid snapshot. " + string.Join("; ", actions));
    }
}

/// <summary>S2: a state snapshot (no history) naming its journal checkpoint, plus an append-only journal of career events.
/// Each journal record carries its sequence number and a SHA-256 chain over every record before it. The save writes the
/// journal first and the snapshot second (journalFirst = false reverses that, to test the wrong order). On load the
/// snapshot is the authority: records after its checkpoint are an unfinished save and are truncated; a journal that
/// does not reach the checkpoint, or whose chain differs there, rejects that snapshot and an older backup is tried.</summary>
public sealed class S2Store(string dir, bool journalFirst = true) : IWorldStore
{
    private const string Format = "S2";
    private string Primary => Path.Combine(dir, "state.s2");
    private string Journal => Path.Combine(dir, "journal.log");
    public string Name => journalFirst ? "S2" : "S2-reverse-order";
    public long Bytes() => (File.Exists(Primary) ? new FileInfo(Primary).Length : 0) + (File.Exists(Journal) ? new FileInfo(Journal).Length : 0);
    public long JournalBytes() => File.Exists(Journal) ? new FileInfo(Journal).Length : 0;
    public long SnapshotBytes() => File.Exists(Primary) ? new FileInfo(Primary).Length : 0;

    public static string Escape(string? s) => s is null ? "" : s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n");
    public static string? Unescape(string s, bool nullable) => nullable && s.Length == 0 ? null : s.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
    public static string Chain(string previous, string body) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previous + "|" + body)), 0, 16).ToLowerInvariant();

    public sealed record JournalScan(List<CareerEvent> Events, List<(long Seq, string Chain, long EndOffset)> Marks, long ValidBytes, long TotalBytes, string? Stop);

    /// <summary>Reads the journal's valid prefix: contiguous sequence numbers and an unbroken chain.</summary>
    public JournalScan Scan(long upTo = long.MaxValue, bool keepMarks = true)
    {
        var events = new List<CareerEvent>(); var marks = new List<(long, string, long)>();
        if (!File.Exists(Journal)) return new JournalScan(events, marks, 0, 0, null);
        var bytes = File.ReadAllBytes(Journal);
        long offset = 0, expected = 1; var chain = "genesis"; string? stop = null;
        while (offset < bytes.Length)
        {
            var end = Array.IndexOf(bytes, (byte)'\n', (int)offset);
            if (end < 0) { stop = "torn record at the end"; break; }
            var line = Encoding.UTF8.GetString(bytes, (int)offset, end - (int)offset);
            var f = line.Split('\t');
            if (f.Length != 11 || !long.TryParse(f[0], out var seq)) { stop = $"malformed record at byte {offset}"; break; }
            if (seq != expected) { stop = $"sequence {seq}, expected {expected}"; break; }
            var body = line[..line.LastIndexOf('\t')];
            var next = Chain(chain, body);
            if (next != f[10]) { stop = $"chain mismatch at sequence {seq}"; break; }
            if (seq > upTo) break;
            chain = next; expected++;
            events.Add(new CareerEvent(f[1], f[2], Enum.Parse<EventType>(f[3]), Unescape(f[4], true), Unescape(f[5], true), int.Parse(f[6]),
                Unescape(f[7], false)!, Unescape(f[8], true), Enum.Parse<Era>(f[9])));
            offset = end + 1;
            if (keepMarks) marks.Add((seq, chain, offset));
        }
        return new JournalScan(events, marks, offset, bytes.Length, stop);
    }

    /// <summary>The writer's view of the journal head, kept in memory after a load or a save (a new process scans once).</summary>
    private (long Seq, string Chain)? head;

    private (long Seq, string Chain) Head()
    {
        if (head is { } known) return known;
        var scan = Scan(keepMarks: true);
        head = scan.Marks.Count == 0 ? (0, "genesis") : (scan.Marks[^1].Seq, scan.Marks[^1].Chain);
        return head.Value;
    }

    private (long Seq, string Chain) Append(World w, CrashPoint crash)
    {
        var (last, chain) = Head();
        var block = new StringBuilder();
        foreach (var e in w.Events.Where(e => StoreFiles.Seq(e) > last))   // deduplication: only records past the journal's head
        {
            var seq = StoreFiles.Seq(e);
            if (seq != last + 1) throw new InvalidDataException($"journal gap: {seq} after {last}");
            var body = string.Join('\t', seq, Escape(e.Id), Escape(e.PersonId), e.Type, Escape(e.From), Escape(e.To), e.Season, Escape(e.Reason), Escape(e.ContractId), e.Era);
            chain = Chain(chain, body);
            block.Append(body).Append('\t').Append(chain).Append('\n');
            last = seq;
        }
        var bytes = Encoding.UTF8.GetBytes(block.ToString());
        using (var file = new FileStream(Journal, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            if (crash == CrashPoint.MidJournalAppend) { file.Write(bytes, 0, bytes.Length / 2); file.Flush(true); throw new SimulatedCrash(crash); }
            file.Write(bytes); file.Flush(true);
        }
        head = (last, chain);
        if (crash == CrashPoint.AfterJournalAppend) throw new SimulatedCrash(crash);
        return (last, chain);
    }

    public void Save(World w, CrashPoint crash = CrashPoint.None)
    {
        Directory.CreateDirectory(dir);
        if (journalFirst)
        {
            var (seq, chain) = Append(w, crash);
            StoreFiles.Commit(Primary, StoreFiles.Frame(Format, StoreFiles.StateOnly(w), w.Season, seq, chain), Format, crash);
        }
        else
        {
            // The wrong order (tested on purpose): the snapshot names a checkpoint the journal does not hold yet.
            var (head, chain) = Head();
            foreach (var e in w.Events.Where(e => StoreFiles.Seq(e) > head))
                chain = Chain(chain, string.Join('\t', StoreFiles.Seq(e), Escape(e.Id), Escape(e.PersonId), e.Type, Escape(e.From), Escape(e.To), e.Season, Escape(e.Reason), Escape(e.ContractId), e.Era));
            StoreFiles.Commit(Primary, StoreFiles.Frame(Format, StoreFiles.StateOnly(w), w.Season, w.NextEvent, chain), Format, crash == CrashPoint.AfterSnapshotBeforeJournal ? CrashPoint.None : crash);
            if (crash == CrashPoint.AfterSnapshotBeforeJournal) throw new SimulatedCrash(crash);
            Append(w, CrashPoint.None);
        }
    }

    /// <summary>The load with recovery (the snapshot is the authority; see the class summary).</summary>
    public (World World, LoadReport Report) Load(StoreContext ctx) => Load(ctx, stateOnly: false);

    public (World World, LoadReport Report) Load(StoreContext ctx, bool stateOnly)
    {
        var actions = new List<string>();
        if (File.Exists(Primary + ".tmp")) { File.Delete(Primary + ".tmp"); actions.Add("removed an unfinished temporary file"); }
        var scan = Scan(keepMarks: true);
        if (scan.Stop is not null) actions.Add("journal valid prefix ends: " + scan.Stop);
        foreach (var path in new[] { Primary, Primary + ".bak1", Primary + ".bak2" })
        {
            var read = StoreFiles.Read(path, Format);
            if (read is null) { if (File.Exists(path)) actions.Add($"rejected {Path.GetFileName(path)} (checksum or frame)"); continue; }
            var (payload, season, seq, chain) = read.Value;
            var mark = seq == 0 ? (0L, "genesis", 0L) : scan.Marks.FirstOrDefault(m => m.Seq == seq);
            if (seq != 0 && mark.Item1 != seq) { actions.Add($"rejected {Path.GetFileName(path)}: its checkpoint {seq} is beyond the journal's valid prefix"); continue; }
            if (mark.Item2 != chain) { actions.Add($"rejected {Path.GetFileName(path)}: chain differs at checkpoint {seq}"); continue; }
            if (scan.TotalBytes > mark.Item3)
            {
                using (var file = new FileStream(Journal, FileMode.Open, FileAccess.Write, FileShare.None)) { file.SetLength(mark.Item3); file.Flush(true); }
                actions.Add($"truncated the journal after checkpoint {seq}: {scan.TotalBytes - mark.Item3} bytes ({scan.Marks.Count(m => m.Seq > seq)} complete records) of an unfinished save");
            }
            var world = Persistence.Restore(payload, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
            if (world.NextEvent != seq) throw new InvalidDataException($"S2: state counter {world.NextEvent} differs from checkpoint {seq}");
            if (!stateOnly) world.Events.AddRange(scan.Events.TakeWhile(e => StoreFiles.Seq(e) <= seq));
            if (path != Primary) actions.Add($"rolled back to {Path.GetFileName(path)}");
            head = (seq, chain);
            return (world, new LoadReport(Path.GetFileName(path), season, seq, actions));
        }
        throw new InvalidDataException("S2: no snapshot agrees with the journal. " + string.Join("; ", actions));
    }

    /// <summary>A loader WITHOUT the checkpoint rule (to demonstrate the failure it prevents): the primary snapshot plus
    /// every readable journal record.</summary>
    public World NaiveLoad(StoreContext ctx)
    {
        var read = StoreFiles.Read(Primary, Format) ?? throw new InvalidDataException("no primary");
        var world = Persistence.Restore(read.Payload, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
        world.Events.AddRange(Scan(keepMarks: false).Events);
        return world;
    }

    /// <summary>A history query straight from the journal file, without loading the state (one sequential scan).</summary>
    public int CareerFromDisk(string personId) => Scan(keepMarks: false).Events.Count(e => e.PersonId == personId);
}

/// <summary>S3: one SQLite database (WAL, synchronous FULL): the state blob and its checkpoint in one row, the career events
/// in an indexed table; a save is one transaction.</summary>
public sealed class S3Store : IWorldStore, IDisposable
{
    private readonly string path;
    private SqliteDb? db;
    public string Name => "S3-sqlite";
    public S3Store(string path) { this.path = path; Directory.CreateDirectory(Path.GetDirectoryName(path)!); }
    private SqliteDb Db()
    {
        if (db is not null) return db;
        db = new SqliteDb(path);
        db.Exec("PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL;");
        db.Exec("CREATE TABLE IF NOT EXISTS state(id INTEGER PRIMARY KEY CHECK(id = 1), season INTEGER NOT NULL, seq INTEGER NOT NULL, sha TEXT NOT NULL, snapshot BLOB NOT NULL);" +
                "CREATE TABLE IF NOT EXISTS events(seq INTEGER PRIMARY KEY, id TEXT NOT NULL UNIQUE, person TEXT NOT NULL, type TEXT NOT NULL, src TEXT, dst TEXT, season INTEGER NOT NULL, reason TEXT NOT NULL, contract TEXT, era TEXT NOT NULL);" +
                "CREATE INDEX IF NOT EXISTS events_person ON events(person, seq); CREATE INDEX IF NOT EXISTS events_season ON events(season, type);");
        return db;
    }
    public void Close() { db?.Dispose(); db = null; }
    public void Dispose() => Close();
    public long Bytes() => (File.Exists(path) ? new FileInfo(path).Length : 0) + (File.Exists(path + "-wal") ? new FileInfo(path + "-wal").Length : 0);
    public static string Version => SqliteDb.Version;

    public void Save(World w, CrashPoint crash = CrashPoint.None)
    {
        var d = Db();
        var state = StoreFiles.StateOnly(w);
        d.Exec("BEGIN IMMEDIATE");
        try
        {
            var last = d.Scalar("SELECT COALESCE(MAX(seq), 0) FROM events");
            using (var insert = d.Prepare("INSERT INTO events(seq, id, person, type, src, dst, season, reason, contract, era) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10)"))
                foreach (var e in w.Events.Where(e => StoreFiles.Seq(e) > last))
                {
                    insert.Bind(1, StoreFiles.Seq(e)).Bind(2, e.Id).Bind(3, e.PersonId).Bind(4, e.Type.ToString()).Bind(5, e.From).Bind(6, e.To)
                        .Bind(7, e.Season).Bind(8, e.Reason).Bind(9, e.ContractId).Bind(10, e.Era.ToString());
                    insert.Run();
                }
            using (var put = d.Prepare("INSERT OR REPLACE INTO state(id, season, seq, sha, snapshot) VALUES (1, ?1, ?2, ?3, ?4)"))
                put.Bind(1, w.Season).Bind(2, w.NextEvent).Bind(3, StoreFiles.Sha(state)).Bind(4, state).Run();
            if (crash != CrashPoint.None) throw new SimulatedCrash(crash);
            d.Exec("COMMIT");
            d.Exec("PRAGMA wal_checkpoint(TRUNCATE)");   // fold the WAL into the database after each save (part of the save cost)
        }
        catch
        {
            // An in-process failure rolls back; a killed process leaves the WAL for SQLite to recover on the next open.
            try { d.Exec("ROLLBACK"); } catch (IOException) { }
            throw;
        }
    }

    public (World World, LoadReport Report) Load(StoreContext ctx) => Load(ctx, stateOnly: false);

    public (World World, LoadReport Report) Load(StoreContext ctx, bool stateOnly)
    {
        var d = Db(); var actions = new List<string>();
        var check = d.ScalarText("PRAGMA quick_check");
        if (check != "ok") throw new InvalidDataException("S3: quick_check " + check);
        int season; long seq; string sha; byte[] blob;
        using (var s = d.Prepare("SELECT season, seq, sha, snapshot FROM state WHERE id = 1"))
        {
            if (!s.Step()) throw new InvalidDataException("S3: no state row");
            season = (int)s.Int64(0); seq = s.Int64(1); sha = s.Text(2)!; blob = s.Blob(3);
        }
        if (StoreFiles.Sha(blob) != sha) throw new InvalidDataException("S3: state checksum mismatch");
        var max = d.Scalar("SELECT COALESCE(MAX(seq), 0) FROM events"); var count = d.Scalar("SELECT COUNT(*) FROM events");
        if (max != seq || count != seq) throw new InvalidDataException($"S3: events {count} (max {max}) differ from checkpoint {seq}");
        var world = Persistence.Restore(blob, ctx.Config, ctx.Content, ctx.RealHandles, ctx.RealTeams);
        if (!stateOnly)
            using (var s = d.Prepare("SELECT id, person, type, src, dst, season, reason, contract, era FROM events ORDER BY seq"))
                while (s.Step())
                    world.Events.Add(new CareerEvent(s.Text(0)!, s.Text(1)!, Enum.Parse<EventType>(s.Text(2)!), s.Text(3), s.Text(4), (int)s.Int64(5), s.Text(6)!, s.Text(7), Enum.Parse<Era>(s.Text(8)!)));
        return (world, new LoadReport(Path.GetFileName(path), season, seq, actions));
    }

    public int Career(string personId)
    {
        using var s = Db().Prepare("SELECT COUNT(*) FROM events WHERE person = ?1");
        s.Bind(1, personId);
        return s.Step() ? (int)s.Int64(0) : 0;
    }
    public int Transfers(int season)
    {
        using var s = Db().Prepare("SELECT COUNT(*) FROM events WHERE season = ?1 AND type = 'Transfer'");
        s.Bind(1, season);
        return s.Step() ? (int)s.Int64(0) : 0;
    }
    public int TeamHistory(string team)
    {
        using var s = Db().Prepare("SELECT COUNT(*) FROM events WHERE src = ?1 OR dst = ?1");
        s.Bind(1, team);
        return s.Step() ? (int)s.Int64(0) : 0;
    }
}
