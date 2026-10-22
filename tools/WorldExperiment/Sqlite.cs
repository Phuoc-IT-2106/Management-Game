using System.Runtime.InteropServices;
using System.Text;

namespace WorldExperiment;

/// <summary>A minimal binding to the SQLite library that ships with Windows 10 and later (winsqlite3.dll), for the S3
/// persistence measurement only (EXPERIMENT). The repository's restricted package feed has no SQLite package, so a
/// production adoption would need a dependency decision (Microsoft.Data.Sqlite or this system library).</summary>
internal static class SqliteNative
{
    private const string Lib = "winsqlite3.dll";
    public const int Ok = 0, Row = 100, Done = 101;
    public const int OpenReadWrite = 0x2, OpenCreate = 0x4, OpenFullMutex = 0x10000;
    public static readonly IntPtr Transient = new(-1);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_close_v2(IntPtr db);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr arg, out IntPtr error);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int bytes, out IntPtr stmt, IntPtr tail);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_step(IntPtr stmt);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_reset(IntPtr stmt);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int bytes, IntPtr destructor);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_bind_blob(IntPtr stmt, int index, byte[] value, int bytes, IntPtr destructor);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_bind_null(IntPtr stmt, int index);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern long sqlite3_column_int64(IntPtr stmt, int column);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern IntPtr sqlite3_column_text(IntPtr stmt, int column);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern IntPtr sqlite3_column_blob(IntPtr stmt, int column);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_column_bytes(IntPtr stmt, int column);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern int sqlite3_column_type(IntPtr stmt, int column);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern IntPtr sqlite3_errmsg(IntPtr db);
    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)] public static extern IntPtr sqlite3_libversion();

    public static byte[] Z(string s) { var b = Encoding.UTF8.GetBytes(s); Array.Resize(ref b, b.Length + 1); return b; }
    public static string? Text(IntPtr p) => p == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(p);
}

internal sealed class SqliteDb : IDisposable
{
    private IntPtr handle;
    public SqliteDb(string path)
    {
        var rc = SqliteNative.sqlite3_open_v2(SqliteNative.Z(path), out handle, SqliteNative.OpenReadWrite | SqliteNative.OpenCreate | SqliteNative.OpenFullMutex, IntPtr.Zero);
        if (rc != SqliteNative.Ok) throw new IOException($"sqlite open {rc}: {Error()}");
    }
    public static string Version => SqliteNative.Text(SqliteNative.sqlite3_libversion()) ?? "?";
    public string Error() => SqliteNative.Text(SqliteNative.sqlite3_errmsg(handle)) ?? "";
    public void Exec(string sql)
    {
        var rc = SqliteNative.sqlite3_exec(handle, SqliteNative.Z(sql), IntPtr.Zero, IntPtr.Zero, out _);
        if (rc != SqliteNative.Ok) throw new IOException($"sqlite exec {rc}: {Error()} ({sql})");
    }
    public SqliteStmt Prepare(string sql)
    {
        var bytes = SqliteNative.Z(sql);
        var rc = SqliteNative.sqlite3_prepare_v2(handle, bytes, bytes.Length, out var stmt, IntPtr.Zero);
        if (rc != SqliteNative.Ok) throw new IOException($"sqlite prepare {rc}: {Error()} ({sql})");
        return new SqliteStmt(this, stmt);
    }
    public long Scalar(string sql) { using var s = Prepare(sql); return s.Step() ? s.Int64(0) : 0; }
    public string? ScalarText(string sql) { using var s = Prepare(sql); return s.Step() ? s.Text(0) : null; }
    public void Dispose() { if (handle != IntPtr.Zero) { SqliteNative.sqlite3_close_v2(handle); handle = IntPtr.Zero; } }
}

internal sealed class SqliteStmt(SqliteDb db, IntPtr stmt) : IDisposable
{
    private IntPtr stmt = stmt;
    public SqliteStmt Bind(int i, string? v)
    {
        if (v is null) SqliteNative.sqlite3_bind_null(stmt, i);
        else { var b = Encoding.UTF8.GetBytes(v); SqliteNative.sqlite3_bind_text(stmt, i, b, b.Length, SqliteNative.Transient); }
        return this;
    }
    public SqliteStmt Bind(int i, long v) { SqliteNative.sqlite3_bind_int64(stmt, i, v); return this; }
    public SqliteStmt Bind(int i, byte[] v) { SqliteNative.sqlite3_bind_blob(stmt, i, v, v.Length, SqliteNative.Transient); return this; }
    public bool Step()
    {
        var rc = SqliteNative.sqlite3_step(stmt);
        if (rc == SqliteNative.Row) return true;
        if (rc == SqliteNative.Done) return false;
        throw new IOException($"sqlite step {rc}: {db.Error()}");
    }
    public void Run() { Step(); SqliteNative.sqlite3_reset(stmt); }
    public void Reset() => SqliteNative.sqlite3_reset(stmt);
    public long Int64(int c) => SqliteNative.sqlite3_column_int64(stmt, c);
    public string? Text(int c) => SqliteNative.sqlite3_column_type(stmt, c) == 5 ? null : SqliteNative.Text(SqliteNative.sqlite3_column_text(stmt, c));
    public byte[] Blob(int c)
    {
        var n = SqliteNative.sqlite3_column_bytes(stmt, c);
        var p = SqliteNative.sqlite3_column_blob(stmt, c);
        var b = new byte[n];
        if (n > 0) Marshal.Copy(p, b, 0, n);
        return b;
    }
    public void Dispose() { if (stmt != IntPtr.Zero) { SqliteNative.sqlite3_finalize(stmt); stmt = IntPtr.Zero; } }
}
