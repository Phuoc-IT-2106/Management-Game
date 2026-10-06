using System.Text;
using System.Text.Json;
using ManagementGame.Application;
using ManagementGame.Domain;

namespace ManagementGame.Infrastructure;

public sealed record SaveEnvelope(string Format, int Schema, string Rules, string Rng, string CanonicalVersion,
    string ContentHash, string GameplayHash, CampaignDto Snapshot, string Checksum);

public sealed class SnapshotStore(string directory, LoadedContent content) : ISnapshotStore
{
    public string PrimaryPath(string slot)
    {
        if (slot.Length is < 1 or > 32 || slot.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new ArgumentException("Invalid save slot.");
        return Path.Combine(Path.GetFullPath(directory), slot + ".save.json");
    }
    public void Save(string slot, Campaign campaign)
    {
        SnapshotValidation.Validate(campaign, content);
        var path = PrimaryPath(slot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var slotLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(path)) _ = Read(path); // Do not rotate corrupted evidence over a good backup.
        var envelope = new SaveEnvelope("management-game-save", 1, campaign.Execution.RulesVersion, KeyedRandom.Version,
            Canonical.Version, content.Hash, Canonical.Hash(campaign), CampaignDto.From(campaign), "");
        envelope = envelope with { Checksum = Canonical.Digest(Canonical.Json(envelope)) };
        var bytes = Encoding.UTF8.GetBytes(Canonical.Json(envelope));
        var temp = path + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        { file.Write(bytes); file.Flush(true); }
        _ = Read(temp);
        if (File.Exists(path))
        {
            var backup = path + ".bak1";
            if (File.Exists(backup))
            {
                try
                {
                    _ = Read(backup);
                    using (var input = File.OpenRead(backup))
                    using (var output = new FileStream(path + ".bak2.tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                    { input.CopyTo(output); output.Flush(true); }
                    File.Move(path + ".bak2.tmp", path + ".bak2", true);
                }
                catch (InvalidDataException) { /* Preserve the older valid backup rather than rotating a corrupt bak1. */ }
            }
            File.Replace(temp, path, backup);
        }
        else File.Move(temp, path);
        _ = Read(path);
    }
    public Campaign Load(string slot, bool backup = false)
    {
        var path = PrimaryPath(slot);
        if (!backup) return Read(path);
        foreach (var suffix in new[] { ".bak1", ".bak2" })
        {
            try { return Read(path + suffix); }
            catch (Exception e) when (e is IOException or InvalidDataException) { }
        }
        throw new InvalidDataException("No validated backup is available.");
    }
    public Campaign Read(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Save does not exist.", path);
        if (info.Length > 8_000_000) throw new InvalidDataException("Save exceeds fixture byte limit.");
        var envelope = StrictJson.Read<SaveEnvelope>(File.ReadAllBytes(path));
        if (envelope.Format != "management-game-save" || envelope.Schema != 1 || envelope.Rules != content.Definition.RulesVersion
            || envelope.Rng != KeyedRandom.Version || envelope.CanonicalVersion != Canonical.Version || envelope.ContentHash != content.Hash)
            throw new InvalidDataException("Unsupported save schema/rules/content identity. This development build supports only its exact identity.");
        if (envelope.Checksum != Canonical.Digest(Canonical.Json(envelope with { Checksum = "" }))) throw new InvalidDataException("Save checksum mismatch.");
        try
        {
            var candidate = envelope.Snapshot.ToDomain();
            SnapshotValidation.Validate(candidate, content);
            if (Canonical.Hash(candidate) != envelope.GameplayHash) throw new InvalidDataException("Gameplay hash mismatch.");
            return candidate;
        }
        catch (Exception e) when (e is NullReferenceException or InvalidOperationException or ArgumentException or OverflowException)
        { throw new InvalidDataException("Malformed snapshot references or values: " + e.Message, e); }
    }
}
