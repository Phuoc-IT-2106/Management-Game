using ManagementGame.Domain;

namespace WorldExperiment;

public sealed record CheckResult(string Id, string Name, bool Passed, string Detail);

/// <summary>World invariants (run at creation and after every season) and the history-consistency replay.</summary>
public static class Checks
{
    public static List<string> Invariants(World w, bool initial)
    {
        var f = new List<string>();
        void Fail(string message) { if (f.Count < 50) f.Add(message); else if (f.Count == 50) f.Add("..."); }
        var cfg = w.Config;
        var memberships = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var t in w.Teams.Values) foreach (var id in t.Squad) memberships[id] = memberships.GetValueOrDefault(id) + 1;
        foreach (var p in w.People.Values)
        {
            var o = w.Overall(p);
            if (o is < 1 or > 99) Fail($"{p.Id}: overall {o} outside 1-99");
            if (p.Core.Potential < o || p.Core.Potential > 99) Fail($"{p.Id}: potential {p.Core.Potential} vs overall {o}");
            if (DevelopmentV2.Skills(p.Core.Skills).Any(x => x is < 1 or > 99)) Fail($"{p.Id}: skill outside 1-99");
            if (p.Origin == Origin.Generated && !p.Id.StartsWith(WorldBuilder.NpcPrefix, StringComparison.Ordinal)) Fail($"{p.Id}: NPC id shape");
            if (p.Origin == Origin.Real && !p.Id.StartsWith("player:real:ps:", StringComparison.Ordinal)) Fail($"{p.Id}: real id shape");
            switch (p.Status)
            {
                case Status.Employed:
                    if (p.TeamId is null || !w.Teams.TryGetValue(p.TeamId, out var team) || team.Dissolved || !team.Squad.Contains(p.Id)) { Fail($"{p.Id}: employed without a squad place"); break; }
                    if (p.ContractId is null || !w.Contracts.TryGetValue(p.ContractId, out var c) || !c.Active || c.PersonId != p.Id || c.TeamId != p.TeamId) Fail($"{p.Id}: contract mismatch");
                    if (memberships.GetValueOrDefault(p.Id) != 1) Fail($"{p.Id}: in {memberships.GetValueOrDefault(p.Id)} squads");
                    if (p.SeniorRegistration && (team.Layer != Layer.Academy || !w.Teams[team.ParentTeamId!].SeniorRegistered.Contains(p.Id))) Fail($"{p.Id}: senior registration without academy employment");
                    break;
                default:
                    if (p.TeamId is not null || p.ContractId is not null || memberships.ContainsKey(p.Id)) Fail($"{p.Id}: {p.Status} but attached to a team");
                    break;
            }
        }
        foreach (var c in w.Contracts.Values.Where(c => c.Active))
            if (!w.People.TryGetValue(c.PersonId, out var p) || p.ContractId != c.Id || p.Status != Status.Employed) Fail($"{c.Id}: active contract without its employee");
        foreach (var t in w.Teams.Values.Where(t => !t.Dissolved))
        {
            if (t.Squad.Distinct(StringComparer.Ordinal).Count() != t.Squad.Count) Fail($"{t.Id}: duplicate squad entry");
            if (WorldBuilder.RegisteredCount(t) > cfg.World.MaxSquad) Fail($"{t.Id}: {WorldBuilder.RegisteredCount(t)} registered (max {cfg.World.MaxSquad})");
            var lineup = t.Lineup.Values.Where(x => x is not null).Select(x => x!).ToList();
            if (lineup.Distinct(StringComparer.Ordinal).Count() != lineup.Count) Fail($"{t.Id}: one person in two lineup slots");
            foreach (var (role, id) in t.Lineup)
            {
                if (id is null) { if (initial) Fail($"{t.Id}: no {role} starter at creation"); continue; }
                if (!t.Squad.Contains(id) && !t.SeniorRegistered.Contains(id)) Fail($"{t.Id}: starter {id} not registered");
                if (t.Real && w.People[id].Origin != Origin.Real && cfg.World.RealStarterPolicy is "STRICT" or "EXIT") Fail($"{t.Id}: NPC {id} in a real team's starting five");
                if (w.InRole(w.People[id], role) <= 0) Fail($"{t.Id}: {id} cannot play {role}");
            }
            if (initial && t.Lineup.Count != 5) Fail($"{t.Id}: lineup has {t.Lineup.Count} roles");
        }
        var handles = w.People.Values.Where(p => p.Origin == Origin.Generated).Select(p => NpcGenerator.Normalize(p.Core.Handle)).ToList();
        if (handles.Distinct(StringComparer.Ordinal).Count() != handles.Count) Fail("NPC handles are not unique");
        if (handles.Any(w.RealHandles.Contains)) Fail("an NPC handle matches a real handle");
        if (w.People.Values.Any(p => p.Origin == Origin.Generated && p.InitialOverall > cfg.Population.InitialOverallCap)) Fail("an NPC started above the cap");
        if (initial)
        {
            var npcs = w.People.Values.Count(p => p.Origin == Origin.Generated);
            if (npcs != cfg.Population.NpcCount) Fail($"{npcs} NPCs, expected {cfg.Population.NpcCount}");
            var fa = w.People.Values.Count(p => p.Origin == Origin.Generated && p.Status == Status.FreeAgent);
            if (fa * 100 >= cfg.Population.NpcCount * cfg.Population.FreeAgentHardLimitPercent) Fail($"{fa} free agents: not below {cfg.Population.FreeAgentHardLimitPercent}%");
        }
        return f;
    }

    /// <summary>Replays every person's history in order and checks it tells their current state: a signing only from the
    /// open market, a move or an exit only from the team the person is with, nothing after retirement. Returns the
    /// violations (person: reason).</summary>
    public static List<string> History(World w)
    {
        var violations = new List<string>();
        foreach (var (id, events) in Reports.EventsByPerson(w))
        {
            string? team = null; var retired = false; var known = false;
            foreach (var e in events)
            {
                string? error = null;
                if (retired && e.Type != EventType.RealRegistrationObserved) error = "event after retirement";
                switch (e.Type)
                {
                    case EventType.RealRegistrationObserved: break;
                    case EventType.GeneratedProspect: if (team is not null) error = "prospect while employed"; known = true; break;
                    case EventType.SnapshotEmployment:
                        if (known && team != e.To) error = $"snapshot team {e.To} but history says {team}";
                        team = e.To; known = true; break;
                    case EventType.ContractExtension: if (team != e.From || e.From != e.To) error = "extension elsewhere"; break;
                    case EventType.ContractExpired or EventType.Released or EventType.Dismissed or EventType.VoluntaryDeparture or EventType.TeamDissolved or EventType.AcademyNotRetained:
                        if (team != e.From || e.From is null) error = $"exit from {e.From} while at {team}";
                        team = null; known = true; break;
                    case EventType.Retired:
                        if (known && team != e.From) error = $"retired from {e.From} while at {team}";
                        team = null; retired = true; break;
                    default:   // a signing (from the market) or a move (from a team)
                        if (e.From is null ? team is not null : known && team != e.From) error = $"{e.Type} from {e.From} while at {team}";
                        team = e.To; known = true; break;
                }
                if (error is not null && violations.Count < 50) violations.Add($"{id}: {error} ({e.Id})");
            }
            if (!w.People.TryGetValue(id, out var p)) { if (violations.Count < 50) violations.Add($"{id}: history for a person absent from the state"); continue; }
            if (known && team != p.TeamId && violations.Count < 50) violations.Add($"{id}: history ends at {team}, state says {p.TeamId}");
            if (retired != (p.Status == Status.Retired) && violations.Count < 50) violations.Add($"{id}: retirement mismatch");
        }
        return violations;
    }

    public static string EventsHash(IEnumerable<CareerEvent> events)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var text = string.Join("\n", events.Select(e => $"{e.Id}|{e.PersonId}|{e.Type}|{e.From}|{e.To}|{e.Season}|{e.Reason}|{e.ContractId}|{e.Era}"));
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
