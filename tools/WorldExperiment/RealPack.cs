using System.Text.Json;
using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>Loads the local real-roster pack (data/world/real_roster_snapshot_*.json, written by build_real_rosters.py):
/// only the playable verified teams (five verified starters, not stale) and their registered squads enter the world.
/// Ratings come from the research model; personality, contracts, wages and development traits are unknown and take
/// neutral simulation defaults that are marked as assumptions.</summary>
public static class RealPack
{
    public sealed record LoadResult(int Teams, int Players, int Starters, int Substitutes, int RatingMismatches, int RoleFromOtherRating,
        int AgeAssumed, int PotentialAssumed, int ExcludedTeams, Dictionary<string, int> ExcludedByStatus, int SubstitutesWithoutRole, string PackSha256);

    private static string RoleOf(string? label) => (label ?? "").ToLowerInvariant() switch
    {
        "top" => "top", "jungle" or "jun" or "jng" => "jungle", "mid" => "mid", "bot" or "adc" => "bot", "support" or "sup" => "support", _ => ""
    };

    /// <summary>A post-hoc elite re-scaling of one real player (D1b experiment only): every skill moves by Delta, so the
    /// role-weighted Overall moves by Delta, and the potential is replaced. Never written back to the pack.</summary>
    public sealed record EliteAdjustment(int Delta, int Potential);

    public static LoadResult Load(World w, string path, bool shuffle = false, IReadOnlyDictionary<string, EliteAdjustment>? elite = null)
    {
        var bytes = File.ReadAllBytes(path);
        using var doc = JsonDocument.Parse(bytes);
        var root = doc.RootElement;
        var players = root.GetProperty("players");
        var teams = root.GetProperty("teams").EnumerateArray().ToList();
        if (shuffle) teams = Generator.Shuffle(w.Seed, "pack-order", teams).ToList();   // order independence check
        var history = root.GetProperty("historical_memberships").EnumerateArray().ToList();
        var excluded = new Dictionary<string, int>(StringComparer.Ordinal);
        int mismatches = 0, otherRole = 0, ageAssumed = 0, potentialAssumed = 0, starters = 0, subs = 0, loadedTeams = 0, noRole = 0;
        var content = w.Content; var d = w.Config.Development;
        foreach (var t in teams)
        {
            var status = t.GetProperty("status").GetString()!;
            if (!t.GetProperty("playable").GetBoolean())
            {
                var label = status == "VERIFIED" ? "VERIFIED_STALE" : status;
                excluded[label] = excluded.GetValueOrDefault(label) + 1;
                continue;
            }
            var providerTeam = t.GetProperty("team_id").GetString()!;
            var tier = t.GetProperty("tier").GetString() ?? "d";
            var key = "Real:" + tier;
            var team = new WTeam
            {
                Id = "team:real:ps:" + providerTeam, OrgId = "org:real:ps:" + providerTeam, Name = t.GetProperty("name").GetString() ?? providerTeam,
                Layer = Layer.RealPro, LeagueId = "league:real:" + (t.GetProperty("league_id").GetString() ?? "unknown"), Key = key, Real = true,
                ProviderTeamId = providerTeam, FoundedSeason = 0
            };
            w.Teams[team.Id] = team; loadedTeams++;
            var lineup = new Dictionary<string, string?>(StringComparer.Ordinal);
            var starterIds = new List<(string Role, string Pid)>();
            foreach (var s in t.GetProperty("starters").EnumerateArray())
                starterIds.Add((s.GetProperty("role").GetString()!, s.GetProperty("player_id").GetString()!));
            var subIds = t.GetProperty("substitutes").EnumerateArray().Select(x => x.GetString()!).ToList();
            foreach (var (pid, starterRole) in starterIds.Select(x => (x.Pid, (string?)x.Role)).Concat(subIds.Select(x => (x, (string?)null))))
            {
                var rec = players.GetProperty(pid);
                var rating = rec.GetProperty("rating");
                var primary = RoleOf(rating.GetProperty("primary_role").GetString());
                if (primary.Length == 0) primary = RoleOf(rec.TryGetProperty("provider_role", out var pr) && pr.ValueKind == JsonValueKind.String ? pr.GetString() : null);
                var role = starterRole ?? primary;
                if (role.Length == 0) { noRole++; continue; }
                int[] skills = [rating.GetProperty("mechanics").GetInt32(), rating.GetProperty("early_game").GetInt32(), rating.GetProperty("teamfight").GetInt32(),
                    rating.GetProperty("macro").GetInt32(), rating.GetProperty("champion_pool").GetInt32()];
                var researchOverall = rating.GetProperty("overall_rating").GetInt32();
                if (role != primary)
                {
                    var label = char.ToUpperInvariant(role[0]) + role[1..];
                    if (rating.GetProperty("role_ratings").TryGetProperty(label, out var rr))
                    {
                        skills = rr.GetProperty("attributes").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                        researchOverall = rr.GetProperty("overall").GetInt32();
                        otherRole++;
                    }
                    else role = primary;
                }
                int? eliteDelta = null; var elitePotential = 0;
                if (elite is not null && role == primary && elite.TryGetValue(pid, out var adjust))
                {
                    skills = skills.Select(x => Math.Clamp(x + adjust.Delta, 1, 99)).ToArray();
                    researchOverall += adjust.Delta; eliteDelta = adjust.Delta; elitePotential = adjust.Potential;
                }
                var ageEl = rating.GetProperty("age");
                var assumedAge = ageEl.ValueKind != JsonValueKind.Number;
                var age = assumedAge ? 24 : ageEl.GetInt32();
                if (assumedAge) ageAssumed++;
                var potEl = rating.GetProperty("potential");
                var id = "player:real:ps:" + pid;
                var handle = rec.GetProperty("handle").GetString() ?? pid;
                var nat = rec.TryGetProperty("nationality", out var n) && n.ValueKind == JsonValueKind.String && n.GetString()!.Length == 2 ? n.GetString()! : "XX";
                var core = new Person(id, "player:real:pandascore", handle, handle, nat, 0 - age, role, "",
                    new Skills(skills[0], skills[1], skills[2], skills[3], skills[4]), new Mind(50, 50, 50, 50, 50, 50), 0, 90, 0, 60, 50);
                var overall = Ratings.Rating(core, content);
                if (overall != researchOverall) mismatches++;
                int potential;
                if (potEl.ValueKind == JsonValueKind.Number && role == primary) potential = Math.Max(overall, potEl.GetInt32());
                else { potential = overall; potentialAssumed++; }
                if (eliteDelta is not null) potential = Math.Clamp(Math.Max(overall, elitePotential), overall, 99);
                var weights = content.RoleWeights.Single(x => x.Role == role).Weights.Select(x => x / 10_000.0).ToArray();
                var mean = Enumerable.Range(0, 5).Sum(i => weights[i] * skills[i]);
                var person = new WPerson
                {
                    Id = id, Origin = Origin.Real, Core = core with { Potential = potential }, Profile = "REAL", ProviderId = pid,
                    EvidenceClass = rating.GetProperty("rating_evidence_class").GetString(), AgeAssumed = assumedAge,
                    Dev = new DevTraits { Profile = skills.Select(x => x - mean).ToArray() },
                    Status = Status.Employed, TeamId = team.Id, Ceiling = potential, InitialOverall = overall, InitialPotential = potential,
                    MaxOverall = overall, CreatedSeason = 0
                };
                w.People[id] = person;
                team.Squad.Add(id);
                if (starterRole is not null) { lineup[starterRole] = id; starters++; } else subs++;
                w.UsedHandles.Add(NpcGenerator.Normalize(handle));
            }
            team.Lineup = lineup;
            w.UsedTeamNames.Add(NpcGenerator.Normalize(team.Name));
        }
        // Source observations: every 2026 stage registration of a player now in the world, as immutable snapshot events.
        foreach (var h in history.OrderBy(h => h.GetProperty("player_id").GetString(), StringComparer.Ordinal)
                     .ThenBy(h => h.GetProperty("begin").GetString(), StringComparer.Ordinal).ThenBy(h => h.GetProperty("stage_id").GetString(), StringComparer.Ordinal))
        {
            var id = "player:real:ps:" + h.GetProperty("player_id").GetString();
            if (!w.People.ContainsKey(id)) continue;
            w.Record(id, EventType.RealRegistrationObserved, null, "team:real:ps:" + h.GetProperty("team_id").GetString(), 0,
                $"PandaScore stage registration {h.GetProperty("stage_id").GetString()} ({h.GetProperty("begin").GetString()}..{h.GetProperty("end").GetString()})",
                null, Era.Snapshot);
        }
        return new LoadResult(loadedTeams, starters + subs, starters, subs, mismatches, otherRole, ageAssumed, potentialAssumed,
            excluded.Values.Sum(), excluded, noRole, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant());
    }

    public static (HashSet<string> Handles, HashSet<string> Teams) Exclusions(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        return (doc.RootElement.GetProperty("handles").EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal),
            doc.RootElement.GetProperty("team_names").EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal));
    }
}
