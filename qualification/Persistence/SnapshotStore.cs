using System.Security.Cryptography;
using System.Text.Json;

namespace Qualification.Persistence;

public sealed record Snapshot(int SchemaVersion,string ContentId,long Revision,long CompanyCounter,long WorldDay,string Checksum);
public sealed class InvalidSnapshotException(string message) : IOException(message);
public sealed record RecoveryCandidate(string Path,long Revision);

// Fixed fixture slot, single writer, two validated backup generations; no migrations.
public sealed class SnapshotStore(string directory)
{
    public string Primary => Path.Combine(directory,"snapshot.json");
    public string Backup => Primary+".bak1";
    public string OlderBackup => Primary+".bak2";
    public string Temp => Primary+".tmp";
    public const string ContentIdentity="synthetic-content-v1-sha256:6e39";
    public static Snapshot Create(long revision) => Seal(new(1,ContentIdentity,revision,checked(revision*7),checked(revision+1),""));
    public static Snapshot Seal(Snapshot value) => value with {Checksum=Digest(value)};

    private static string Digest(Snapshot value)
    {
        using var buffer=new MemoryStream();
        using(var writer=new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject(); writer.WriteNumber("SchemaVersion",value.SchemaVersion);
            writer.WriteString("ContentId",value.ContentId); writer.WriteNumber("Revision",value.Revision);
            writer.WriteNumber("CompanyCounter",value.CompanyCounter); writer.WriteNumber("WorldDay",value.WorldDay);
            writer.WriteEndObject();
        }
        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }

    private static void Validate(Snapshot value)
    {
        if(value.SchemaVersion!=1) throw new InvalidSnapshotException($"Unsupported schema {value.SchemaVersion}; no downgrade or migration");
        if(value.ContentId!=ContentIdentity) throw new InvalidSnapshotException("Exact fixture content unavailable; no substitution");
        if(value.Revision<0 || value.Revision==long.MaxValue || value.WorldDay!=value.Revision+1)
            throw new InvalidSnapshotException("Invalid revision/day relationship");
        if(value.Checksum!=Digest(value)) throw new InvalidSnapshotException("Checksum mismatch");
    }

    public static Snapshot Read(string path)
    {
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(file.Length is <=0 or >65536) throw new InvalidSnapshotException("Snapshot size outside fixture bound");
        using var buffer=new MemoryStream(); file.CopyTo(buffer);
        try
        {
            using var json=JsonDocument.Parse(buffer.ToArray(),new JsonDocumentOptions {MaxDepth=8});
            if(json.RootElement.ValueKind!=JsonValueKind.Object) throw new InvalidSnapshotException("Expected object");
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var field in json.RootElement.EnumerateObject())
                if(!names.Add(field.Name)) throw new InvalidSnapshotException("Duplicate JSON field");
            if(!names.SetEquals(new[] {"SchemaVersion","ContentId","Revision","CompanyCounter","WorldDay","Checksum"}))
                throw new InvalidSnapshotException("Missing or unknown JSON fields");
            var snapshot=json.RootElement.Deserialize<Snapshot>() ?? throw new InvalidSnapshotException("Null snapshot");
            Validate(snapshot); return snapshot;
        }
        catch(JsonException error) { throw new InvalidSnapshotException("Malformed JSON: "+error.Message); }
    }

    // Read never silently substitutes a backup. Caller must explicitly choose recovery.
    public Snapshot LoadPrimary() => Read(Primary);
    public RecoveryCandidate[] FindRecoveryCandidates() => new[] {Backup,OlderBackup}.Select(path =>
    {
        try {return new RecoveryCandidate(path,Read(path).Revision);}
        catch(IOException) {return null;}
        catch(UnauthorizedAccessException) {return null;}
    }).Where(x=>x is not null).Select(x=>x!).ToArray();

    public void Save(Snapshot value,Action<string>? checkpoint=null,int? simulatedDiskLimit=null)
    {
        Validate(value);
        Directory.CreateDirectory(directory);
        using var slotLock=new FileStream(Primary+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        bool exists=File.Exists(Primary);
        if(exists && Read(Primary).Revision>=value.Revision) throw new InvalidSnapshotException("Revision must advance");
        checkpoint?.Invoke("before-temp");
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(value);
        using(var file=new FileStream(Temp,FileMode.Create,FileAccess.Write,FileShare.None))
        {
            int half=bytes.Length/2;
            if(simulatedDiskLimit is int limit)
            {
                file.Write(bytes.AsSpan(0,Math.Min(limit,bytes.Length)));
                throw new IOException("Injected ERROR_DISK_FULL after partial temporary write",unchecked((int)0x80070070));
            }
            file.Write(bytes.AsSpan(0,half)); checkpoint?.Invoke("partial-temp");
            file.Write(bytes.AsSpan(half)); checkpoint?.Invoke("after-temp-write");
            file.Flush(flushToDisk:true); checkpoint?.Invoke("after-flush");
        }
        Read(Temp); checkpoint?.Invoke("after-validation");
        // Preserve the older known-good generation before File.Replace creates bak1.
        if(exists && File.Exists(Backup))
        {
            Snapshot? previous=null;
            try {previous=Read(Backup);} catch(InvalidSnapshotException) { /* Keep bak2; never rotate corruption. */ }
            if(previous is not null)
            {
                string backupTemp=OlderBackup+".tmp";
                using(var src=new FileStream(Backup,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(var dst=new FileStream(backupTemp,FileMode.Create,FileAccess.Write,FileShare.None))
                {src.CopyTo(dst); dst.Flush(true);}
                Read(backupTemp); checkpoint?.Invoke("before-backup-publish");
                File.Move(backupTemp,OlderBackup,overwrite:true);
            }
        }
        checkpoint?.Invoke("after-backup");
        checkpoint?.Invoke("before-publish");
        // No delete-primary/copy fallback. All files are on the same directory/volume.
        if(exists) File.Replace(Temp,Primary,Backup,ignoreMetadataErrors:false);
        else File.Move(Temp,Primary);
        checkpoint?.Invoke("after-publish");
        Read(Primary);
    }
}
