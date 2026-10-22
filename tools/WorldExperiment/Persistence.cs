using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>An EXPERIMENTAL world snapshot (not a production save format): the whole world as one JSON document, people,
/// teams, contracts and the append-only career history in ID order. Used to measure size and save/load time and to check
/// that a reload reproduces the same state.</summary>
public static class Persistence
{
    public sealed class Snapshot
    {
        public string Format { get; set; } = "world-experiment-snapshot";
        public string Version { get; set; } = "";
        public ulong Seed { get; set; }
        public int Season { get; set; }
        public string AsOf { get; set; } = "";
        public long NextEvent { get; set; }
        public long NextContract { get; set; }
        public long NextPerson { get; set; }
        public long NextTeam { get; set; }
        public List<WPerson> People { get; set; } = [];
        public List<WTeam> Teams { get; set; } = [];
        public List<WContract> Contracts { get; set; } = [];
        public List<CareerEvent> Events { get; set; } = [];
        public List<string> UsedHandles { get; set; } = [];
        public List<string> UsedTeamNames { get; set; } = [];
    }

    public static readonly JsonSerializerOptions Options = new()
    {
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        Converters = { new JsonStringEnumConverter() }, MaxDepth = 64
    };

    public static Snapshot Capture(World w) => new()
    {
        Version = w.Config.Version, Seed = w.Seed, Season = w.Season, AsOf = w.AsOf, NextEvent = w.NextEvent, NextContract = w.NextContract,
        NextPerson = w.NextPerson, NextTeam = w.NextTeam, People = [.. w.People.Values], Teams = [.. w.Teams.Values], Contracts = [.. w.Contracts.Values],
        Events = [.. w.Events], UsedHandles = [.. w.UsedHandles.Order(StringComparer.Ordinal)], UsedTeamNames = [.. w.UsedTeamNames.Order(StringComparer.Ordinal)]
    };

    public static byte[] Serialize(World w) => JsonSerializer.SerializeToUtf8Bytes(Capture(w), Options);
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static World Restore(byte[] bytes, ExperimentConfig config, Content content, HashSet<string> realHandles, HashSet<string> realTeams)
    {
        var s = JsonSerializer.Deserialize<Snapshot>(bytes, Options) ?? throw new InvalidDataException("Empty snapshot.");
        var w = new World { Config = config, Content = content, Seed = s.Seed, AsOf = s.AsOf, RealHandles = realHandles, RealTeamNames = realTeams };
        w.Season = s.Season; w.NextEvent = s.NextEvent; w.NextContract = s.NextContract; w.NextPerson = s.NextPerson; w.NextTeam = s.NextTeam;
        foreach (var p in s.People) w.People[p.Id] = p;
        foreach (var t in s.Teams) w.Teams[t.Id] = t;
        foreach (var c in s.Contracts) w.Contracts[c.Id] = c;
        w.Events.AddRange(s.Events);
        foreach (var h in s.UsedHandles) w.UsedHandles.Add(h);
        foreach (var n in s.UsedTeamNames) w.UsedTeamNames.Add(n);
        return w;
    }
}
