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
        Require(c.Format == "management-game-development-content" && c.Schema == 2 && c.RulesVersion == "slice-rules-v2", "unsupported schema/rules");
        Require(c.Label.Contains("NONCANONICAL", StringComparison.Ordinal), "fixture label required");
        Require(c.DefaultCompanyName.Trim().Length is > 0 and <= 60, "default company name");
        var b = c.Balance;
        Require(b.SeasonLength is >= 28 and <= 400 && b.FirstMatchDay >= 3 && b.MatchInterval is >= 2 and <= 30 && b.RivalCount is >= 2 and <= 12, "season shape");
        Require(b.FirstMatchDay + b.MatchInterval * (2 * b.RivalCount - 1) <= b.SeasonLength - 2, "season schedule must fit inside the season");
        Require(b.StartingCash is >= 0 and <= 1_000_000_000 && b.PrizeBase is >= 0 and <= 1_000_000, "balance cash");
        Require(b.PreparationScale is > 0 and <= 1000 && b.MatchScale is > 0 and <= 20000 && b.Variance is > 0 and <= 20 && b.RivalCadence > 0, "balance arithmetic bounds");
        Require(b.Reputation is >= 0 and <= 100 && b.Audience is >= 0 and <= 1_000_000 && b.Information is >= 0 and <= 100 && b.BaseLoad is >= 0 and <= 1000, "balance resource bounds");
        Require(b.MetaDayOfSeason is >= 1 && b.MetaDayOfSeason <= b.SeasonLength && b.ReputationGain >= 0 && b.AudienceGain >= 0, "meta and commercial bounds");
        Require(b.SponsorSlots is >= 1 and <= 8 && b.MaxOpenOffers is >= 1 and <= 8 && b.MarketInterval is >= 1 and <= 28 && b.OfferWindow is >= 1 and <= 28
            && b.NegotiationDelay is >= 1 and <= 14 && b.RejectCooldown is >= 1 and <= 112, "sponsor market bounds");
        Require(b.RosterSize is >= 5 and <= 12 && b.MaxRoster >= b.RosterSize && b.MaxRoster <= 16 && b.CandidateCount is >= 1 and <= 12
            && b.CoachCandidateCount is >= 1 and <= 8 && b.TalentRefreshInterval is >= 7 and <= 112, "talent market bounds");
        Require(b.RenewalWindow is >= 7 && b.RenewalWindow < b.SeasonLength && b.StartingContractSeasons is >= 1 and <= 3, "contract bounds");
        bool Range(AttributeRange r, int min, int max) => r.Min >= min && r.Max <= max && r.Min <= r.Max;
        var g = c.Generation;
        Require(Range(g.Talent, 1, 100) && Range(g.Prospect, 1, 100) && Range(g.CoachSkill, 1, 100) && Range(g.CoachCapacity, 20, 1000) && Range(g.Rival, 6, 95), "generation ranges");
        bool Name(string value) => value.Trim().Length is > 0 and <= 40;
        Require(c.Names.Given.Length >= 10 && c.Names.Family.Length >= 10 && c.Names.Given.All(Name) && c.Names.Family.All(Name), "name pools need at least ten valid entries each");
        Require(c.Names.Given.Distinct(StringComparer.Ordinal).Count() == c.Names.Given.Length && c.Names.Family.Distinct(StringComparer.Ordinal).Count() == c.Names.Family.Length, "name pools must not repeat");
        Require(c.OrganizationNames.Length >= b.RivalCount && c.OrganizationNames.All(x => x.Trim().Length is > 0 and <= 60)
            && c.OrganizationNames.Distinct(StringComparer.Ordinal).Count() == c.OrganizationNames.Length, "organization names cover every rival without repeats");
        Require(c.Brands.Length >= 6 && c.Brands.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() == c.Brands.Length
            && c.Brands.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() == c.Brands.Length, "brand pool needs six unique brands");
        foreach (var brand in c.Brands)
            Require(Id(brand.Id) && brand.Name.Trim().Length is > 0 and <= 60 && brand.Sector.Trim().Length is > 0 and <= 40
                && brand.MinimumReputation is >= 0 and <= 100 && brand.MinimumAudience is >= 0 and <= 10_000_000
                && brand.BasePayment is > 0 and <= 1_000_000 && brand.WinBonus is >= 0 and <= 1_000_000 && brand.Load is >= 0 and <= 100
                && brand.DurationDays.Length > 0 && brand.DurationDays.All(d => d is >= 7 and <= 400) && brand.DurationDays.Distinct().Count() == brand.DurationDays.Length, "brand " + brand.Id);
        Require(c.Brands.Any(x => x.MinimumReputation <= b.Reputation && x.MinimumAudience <= b.Audience), "at least one brand must sponsor a new company");
    }
    public static bool Id(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or ':' or '-' or '_');
}
