using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagementGame.Application;
using ManagementGame.Domain;

namespace ManagementGame.Infrastructure;

public sealed record LoadedContent(Content Definition, string Hash);
public static class StrictJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true, MaxDepth = 48
    };
    public static T Read<T>(byte[] bytes)
    {
        if (bytes.Length > 8_000_000) throw new InvalidDataException("File exceeds the development fixture limit.");
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 48 });
            Check(document.RootElement);
            return JsonSerializer.Deserialize<T>(bytes, Options) ?? throw new InvalidDataException("Empty JSON value.");
        }
        catch (JsonException e) { throw new InvalidDataException("Invalid JSON/schema: " + e.Message, e); }
    }
    private static void Check(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            { if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON field: " + property.Name); Check(property.Value); }
        }
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 20000) throw new InvalidDataException("Array exceeds fixture limit.");
            foreach (var item in value.EnumerateArray()) Check(item);
        }
        if (value.ValueKind == JsonValueKind.String && value.GetString()!.Length > 4096) throw new InvalidDataException("String exceeds fixture limit.");
    }
}
public static class ContentLoader
{
    public static LoadedContent Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var content = StrictJson.Read<Content>(bytes);
        try { Validate(content); }
        catch (Exception e) when (e is NullReferenceException or InvalidOperationException or ArgumentException)
        { throw new InvalidDataException("content/fixture.json: missing/invalid required value: " + e.Message, e); }
        var hash = Canonical.Digest(Encoding.UTF8.GetString(bytes));
        var expected = File.ReadAllText(Path.ChangeExtension(path, ".sha256")).Trim();
        if (hash != expected) throw new InvalidDataException("Content manifest SHA-256 mismatch: " + path);
        return new LoadedContent(content, hash);
    }
    public static void Validate(Content c)
    {
        void Require(bool valid, string field) { if (!valid) throw new InvalidDataException("content/fixture.json: " + field); }
        Require(c.Format == "management-game-development-content" && c.Schema == 1 && c.RulesVersion == "slice-rules-v1", "unsupported schema/rules");
        Require(c.Label.Contains("NONCANONICAL", StringComparison.Ordinal), "fixture label required");
        var b = c.Balance;
        Require(b.Horizon is >= 25 and <= 60 && b.StartingCash is >= 0 and <= 1_000_000_000 && b.OperatingCost >= 0, "balance cash/horizon");
        Require(b.PreparationScale is > 0 and <= 1000 && b.MatchScale is > 0 and <= 20000 && b.Variance is > 0 and <= 20 && b.RivalCadence > 0, "balance arithmetic bounds");
        Require(b.Reputation is >= 0 and <= 100 && b.Audience is >= 0 and <= 1_000_000 && b.Information is >= 0 and <= 100 && b.BaseLoad is >= 0 and <= 1000, "balance resource bounds");
        Require(c.Players.Length == 6 && c.Rivals.Length == 2 && c.Fixtures.Length >= 2 && c.Fixtures.Length <= 8, "bounded roster/rival/competition counts");
        var ids = c.Players.Select(p => p.Person.Id).Concat(c.Candidates.Select(x => x.Person.Id)).Concat(c.Rivals.Select(r => r.Id))
            .Concat(c.Fixtures.Select(f => f.Id)).Concat(c.Offers.Select(o => o.Id)).Append(c.Coach.Id).Append(c.InitialSponsor.Id).ToArray();
        Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length && ids.All(Id), "IDs must be unique lowercase stable IDs");
        foreach (var p in c.Players.Select(x => x.Person).Concat(c.Candidates.Select(x => x.Person)))
            Require(Id(p.DefinitionId) && p.Name.Length is > 0 and <= 120 && "ABCDE".Contains(p.Role, StringComparison.Ordinal) && p.Role.Length == 1
                && p.Execution is >= 0 and <= 100 && p.Adaptability is >= 0 and <= 100 && p.Consistency is >= 0 and <= 100 && p.Readiness is > 0 and <= 100, "person " + p.Id);
        Require(c.Players.Select(p => p.Person.Role).Distinct().Count() == 5, "five-role coverage");
        Require(c.Coach.Capacity is > 0 and <= 1000 && c.Coach.Preparation is > 0 and <= 100 && c.Coach.Analysis is >= 0 and <= 100 && c.Coach.Adaptability is >= 0 and <= 100, "coach bounds");
        Require(c.Players.All(p => p.Salary is >= 0 and <= 100000 && p.ReleaseCost is >= 0 and <= 100000), "employment terms");
        Require(c.Candidates.All(p => p.Fee is >= 0 and <= 100000 && p.Salary is >= 0 and <= 100000 && p.ReleaseCost is >= 0 and <= 100000 && p.Deadline <= b.Horizon), "candidate terms");
        Require(c.Fixtures.Select(f => f.Day).Distinct().Count() == c.Fixtures.Length, "one match per day");
        foreach (var f in c.Fixtures) Require(f.Day >= 3 && f.Day < b.Horizon && c.Rivals.Any(r => r.Id == f.RivalId) && f.Importance is > 0 and <= 3 && f.Prize is >= 0 and <= 100000, "fixture " + f.Id);
        foreach (var r in c.Rivals) Require(r.Strength is > 0 and <= 100 && r.Analysis is >= 0 and <= 100 && r.Adaptability is >= 0 and <= 100 && Enum.IsDefined(r.Posture) && r.Budget >= 0, "rival " + r.Id);
        foreach (var o in c.Offers.Append(c.InitialSponsor)) Require(o.Payment is >= 0 and <= 100000 && o.WinBonus is >= 0 and <= 100000 && o.Load is >= 0 and <= 100 && o.Deadline >= 1 && o.EndDay <= b.Horizon && o.EndDay >= o.Deadline && o.MinimumReputation is >= 0 and <= 100 && o.ClaimedBy is null, "sponsor " + o.Id);
    }
    public static bool Id(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or ':' or '-' or '_');
}
