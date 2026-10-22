using ManagementGame.Domain;

namespace WorldExperiment;

public enum Origin { Generated, Real }
/// <summary>Authoritative employment state. Starting, reserve and academy participation are separate (team and lineup).</summary>
public enum Status { Employed, FreeAgent, Retired }
public enum Layer { RealPro, FictionalPro, Academy, SemiPro, Amateur }
public enum TeamStrategy { Contend, Balanced, Develop, Conserve }
public enum Intensity { Low, Normal, High }
public enum Focus { Role, Specialist, Weakness }
/// <summary>PreWorld: a generated NPC's fictional backstory before the world starts. Snapshot: the state at the as-of day
/// (for real players, observed sources). Simulation: what happened in simulated seasons.</summary>
public enum Era { PreWorld, Snapshot, Simulation }
public enum EventType
{
    GeneratedProspect, FirstAmateurContract, FirstProfessionalContract, AcademyRecruitment, Signed, Transfer, ContractExtension,
    ContractExpired, Released, Dismissed, VoluntaryDeparture, TeamDissolved, SeniorPromotion, AcademyNotRetained, Retired,
    RealRegistrationObserved, SnapshotEmployment
}

/// <summary>Keyed draws on top of the production rng-v1 (SHA-256 of seed, domain, key and purpose). Doubles use basic IEEE
/// arithmetic only (the experiment; a production port would use the integer helpers).</summary>
public static class Draw
{
    public static int Int(ulong seed, string domain, string key, string purpose, int min, int max) =>
        min + KeyedRandom.Range(seed, domain, key, purpose, max - min + 1);
    public static double Unit(ulong seed, string domain, string key, string purpose) =>
        (KeyedRandom.Range(seed, domain, key, purpose, 1_000_000) + 0.5) / 1_000_000.0;
    /// <summary>Approximately standard normal: the Irwin-Hall sum of four keyed uniforms, rescaled to unit variance.</summary>
    public static double Normal(ulong seed, string domain, string key, string purpose)
    {
        double s = 0;
        for (var i = 0; i < 4; i++) s += Unit(seed, domain, key, purpose + ":n" + i);
        return (s - 2.0) / Math.Sqrt(4.0 / 12.0);
    }
    public static bool Chance(ulong seed, string domain, string key, string purpose, double p) => Unit(seed, domain, key, purpose) < p;
    public static double Bell(ulong seed, string domain, string key, string purpose, double min, double max) =>
        min + (max - min) * (Unit(seed, domain, key, purpose) + Unit(seed, domain, key, purpose + ":2")) / 2;
    public static double Between(ulong seed, string domain, string key, string purpose, double min, double max) =>
        min + (max - min) * Unit(seed, domain, key, purpose);
    public static T Weighted<T>(ulong seed, string domain, string key, string purpose, IReadOnlyList<T> items, Func<T, int> weight)
    {
        var total = items.Sum(weight);
        var roll = KeyedRandom.Range(seed, domain, key, purpose, total);
        foreach (var item in items) { roll -= weight(item); if (roll < 0) return item; }
        return items[^1];
    }
}

/// <summary>Hidden, simulation-only development traits. For a real player every trait is the neutral default and
/// marked as a simulation assumption, never a fact about the person.</summary>
public sealed class DevTraits
{
    public double PeakAge { get; set; } = 24.5;
    public double Volatility { get; set; } = 1.0;
    public double Trainability { get; set; } = 1.0;
    public double[] Affinity { get; set; } = [1, 1, 1, 1, 1];
    public double[] Profile { get; set; } = [0, 0, 0, 0, 0];
    public bool LateBloomer { get; set; }
    public Focus Focus { get; set; } = Focus.Role;
}

public sealed class WPerson
{
    public required string Id { get; init; }
    public Origin Origin { get; init; }
    public required Person Core { get; set; }
    public required DevTraits Dev { get; init; }
    /// <summary>The latent talent ceiling (hidden). Potential is min(ceiling, overall + remaining capacity) and never below the Overall.</summary>
    public int Ceiling { get; set; }
    public double[] Progress { get; set; } = new double[5];
    public Status Status { get; set; }
    public string? TeamId { get; set; }
    public string? ContractId { get; set; }
    /// <summary>An academy player also registered with the senior squad (an overlapping registration, not employment).</summary>
    public bool SeniorRegistration { get; set; }
    public int ContractSeasons { get; set; }
    public int MatchesTotal { get; set; }
    public int SeasonMatches { get; set; }
    public double SeasonShare { get; set; }
    public int SeasonMorale { get; set; } = 55;
    public int? FreeAgentSince { get; set; }
    public int InitialOverall { get; set; }
    public int InitialPotential { get; set; }
    public int MaxOverall { get; set; }
    public string Profile { get; init; } = "";
    public string? ProviderId { get; init; }
    public string? EvidenceClass { get; init; }
    public bool AgeAssumed { get; init; }
    public int? RetiredSeason { get; set; }
    public int Breakthroughs { get; set; }
    public int Plateaus { get; set; }
    public int Setbacks { get; set; }
    public bool MovedThisWindow { get; set; }
    /// <summary>The person has held a professional contract (real tier a/b or fictional pro), now or before.</summary>
    public bool EverProfessional { get; set; }
    /// <summary>The daily wage of the person's last contract (0: never contracted): a released player's reservation wage.</summary>
    public long LastSalary { get; set; }
    public int CreatedSeason { get; init; }
}

public sealed class WTeam
{
    public required string Id { get; init; }
    public required string OrgId { get; init; }
    public required string Name { get; init; }
    public Layer Layer { get; init; }
    public required string LeagueId { get; init; }
    /// <summary>Economy key: the layer, or Real:{tier} for a real team.</summary>
    public required string Key { get; init; }
    public bool Real { get; init; }
    public string? ProviderTeamId { get; init; }
    public string? ParentTeamId { get; init; }
    public string? AcademyTeamId { get; set; }
    public List<string> Squad { get; } = [];
    public List<string> SeniorRegistered { get; } = [];
    public Dictionary<string, string?> Lineup { get; set; } = [];
    public int SquadTarget { get; set; }
    public int RealNpcSeats { get; set; }
    public long Revenue { get; set; }
    public long Funds { get; set; }
    public TeamStrategy Strategy { get; set; }
    public int YouthFocus { get; set; }
    public int CoachDevelopment { get; set; }
    public Intensity Intensity { get; set; }
    public int Reputation { get; set; }
    public bool Dissolved { get; set; }
    /// <summary>EXIT policy: the season a real organization withdrew (null: never; not written then, so v1 snapshots are
    /// byte-identical).</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? WithdrawnSeason { get; set; }
    public int FoundedSeason { get; init; }
    public double SeasonWinShare { get; set; } = 0.5;
    public int SeasonGames { get; set; }
    public int Place { get; set; }
}

public sealed class WContract
{
    public required string Id { get; init; }
    public required string PersonId { get; init; }
    public required string TeamId { get; init; }
    public int StartSeason { get; init; }
    public int EndSeason { get; set; }
    public long Salary { get; set; }
    public bool Active { get; set; } = true;
    public bool Assumed { get; init; }
}

public sealed record CareerEvent(string Id, string PersonId, EventType Type, string? From, string? To, int Season, string Reason,
    string? ContractId, Era Era);

public sealed class World
{
    public required ExperimentConfig Config { get; init; }
    public required Content Content { get; init; }
    public required ulong Seed { get; init; }
    public int Season { get; set; }
    public SortedDictionary<string, WPerson> People { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, WTeam> Teams { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, WContract> Contracts { get; } = new(StringComparer.Ordinal);
    public List<CareerEvent> Events { get; } = [];
    public long NextEvent { get; set; }
    public long NextContract { get; set; }
    public long NextPerson { get; set; }
    public long NextTeam { get; set; }
    public HashSet<string> UsedHandles { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RealHandles { get; init; } = new(StringComparer.Ordinal);
    public HashSet<string> UsedTeamNames { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RealTeamNames { get; init; } = new(StringComparer.Ordinal);
    public string AsOf { get; init; } = "";
    public List<Dictionary<string, object>> SeasonMetrics { get; } = [];
    public List<string> Warnings { get; } = [];
    /// <summary>The open market as the transfer window opens (after the season's exits and intake), per season.</summary>
    public List<Dictionary<string, object>> PreWindow { get; } = [];
    /// <summary>Entry pathways of the Pathways intake mode (decision experiments only; null otherwise).</summary>
    public Pathway[]? Pathways { get; set; }
    /// <summary>Scouting noise per (season, scouting layer, person): two keyed standard normals (overall, potential). A
    /// cache of a pure keyed function, so it never changes a result; not saved.</summary>
    public Dictionary<string, (double Overall, double Potential)> ScoutNoise { get; } = new(StringComparer.Ordinal);

    public (double Overall, double Potential) Noise(Layer scout, WPerson p, int season)
    {
        var key = $"{season}:{scout}:{p.Id}";
        if (!ScoutNoise.TryGetValue(key, out var z))
            ScoutNoise[key] = z = (Draw.Normal(Seed, "xscout", key, "overall"), Draw.Normal(Seed, "xscout", key, "potential"));
        return z;
    }

    public int Age(WPerson p) => Season - p.Core.BornSeason;
    public int Overall(WPerson p) => Ratings.Rating(p.Core, Content);
    public int InRole(WPerson p, string role) => Ratings.InRole(p.Core, role, Content);

    public CareerEvent Record(string personId, EventType type, string? from, string? to, int season, string reason, string? contract, Era era)
    {
        var e = new CareerEvent($"ev:{++NextEvent:D8}", personId, type, from, to, season, reason, contract, era);
        Events.Add(e);
        return e;
    }
}
