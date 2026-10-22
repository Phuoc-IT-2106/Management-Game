using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>Census and metrics. The NPC census is mutually exclusive by authoritative employment and lineup; overlapping
/// affiliations (an academy player also registered with the senior squad) are counted separately.</summary>
public static class Reports
{
    public static readonly string[] CensusCategories =
        ["fictional_pro_starters", "fictional_pro_reserves", "semi_pro", "amateur", "academy", "real_team_npc_reserves", "free_agents"];
    public static readonly string[] FreeAgentOrigins =
        ["never_contracted", "contract_expired", "released_dismissed", "voluntary_departure", "former_academy_amateur", "other_team_dissolved"];

    public static string Category(World w, WPerson p)
    {
        if (p.Status == Status.FreeAgent) return "free_agents";
        if (p.Status == Status.Retired) return "retired";
        var t = w.Teams[p.TeamId!];
        return t.Layer switch
        {
            Layer.FictionalPro => t.Lineup.Values.Contains(p.Id) ? "fictional_pro_starters" : "fictional_pro_reserves",
            Layer.SemiPro => "semi_pro", Layer.Amateur => "amateur", Layer.Academy => "academy", _ => "real_team_npc_reserves"
        };
    }

    public static Dictionary<string, List<CareerEvent>> EventsByPerson(World w)
    {
        var map = new Dictionary<string, List<CareerEvent>>(StringComparer.Ordinal);
        foreach (var e in w.Events) { if (!map.TryGetValue(e.PersonId, out var list)) map[e.PersonId] = list = []; list.Add(e); }
        return map;
    }

    private static readonly HashSet<EventType> Exits = [EventType.ContractExpired, EventType.Released, EventType.Dismissed, EventType.VoluntaryDeparture,
        EventType.TeamDissolved, EventType.AcademyNotRetained];

    /// <summary>A free agent's origin, from the last exit in their own history (precedence documented in
    /// FREE_AGENT_MARKET_VALIDATION.md): none -> never contracted; released or dismissed; voluntary departure; academy not
    /// retained or an academy/amateur contract that expired -> former academy/amateur; another contract expired; team dissolved.</summary>
    public static string FreeAgentOrigin(World w, WPerson p, Dictionary<string, List<CareerEvent>> events)
    {
        var exit = events.GetValueOrDefault(p.Id)?.LastOrDefault(e => Exits.Contains(e.Type));
        if (exit is null) return "never_contracted";
        return exit.Type switch
        {
            EventType.Released or EventType.Dismissed => "released_dismissed",
            EventType.VoluntaryDeparture => "voluntary_departure",
            EventType.AcademyNotRetained => "former_academy_amateur",
            EventType.TeamDissolved => "other_team_dissolved",
            _ => exit.From is { } from && w.Teams[from].Layer is Layer.Academy or Layer.Amateur ? "former_academy_amateur" : "contract_expired"
        };
    }

    public static double Pct(IReadOnlyList<int> sorted, double p) => sorted.Count == 0 ? double.NaN : sorted[Math.Clamp((int)Math.Floor(p / 100 * (sorted.Count - 1) + 0.5), 0, sorted.Count - 1)];
    public static Dictionary<string, object> Summary(IEnumerable<int> values)
    {
        var x = values.OrderBy(v => v).ToList();
        if (x.Count == 0) return new() { ["n"] = 0 };
        return new()
        {
            ["n"] = x.Count, ["mean"] = Math.Round(x.Average(), 2), ["min"] = x[0], ["p10"] = Pct(x, 10), ["p25"] = Pct(x, 25), ["p50"] = Pct(x, 50),
            ["p75"] = Pct(x, 75), ["p90"] = Pct(x, 90), ["p99"] = Pct(x, 99), ["max"] = x[^1]
        };
    }
    public static Dictionary<string, int> Histogram(IEnumerable<int> values, int bin) =>
        values.GroupBy(v => v / bin * bin).OrderBy(g => g.Key).ToDictionary(g => $"{g.Key}-{g.Key + bin - 1}", g => g.Count());

    /// <summary>The state of the world after a season (or at the start): census, free agents, flows, squads, ratings, market
    /// depth and economy. Aggregates only.</summary>
    public static Dictionary<string, object> State(World w, Dictionary<string, int>? flows = null)
    {
        var events = EventsByPerson(w);
        var npcs = w.People.Values.Where(p => p.Origin == Origin.Generated).ToList();
        var real = w.People.Values.Where(p => p.Origin == Origin.Real).ToList();
        var activeNpc = npcs.Where(p => p.Status != Status.Retired).ToList();
        var census = CensusCategories.ToDictionary(c => c, _ => 0);
        foreach (var p in activeNpc) census[Category(w, p)]++;
        var fa = activeNpc.Where(p => p.Status == Status.FreeAgent).ToList();
        var origins = FreeAgentOrigins.ToDictionary(c => c, _ => 0);
        foreach (var p in fa) origins[FreeAgentOrigin(w, p, events)]++;
        var realActive = real.Where(p => p.Status != Status.Retired).ToList();
        var realFa = realActive.Count(p => p.Status == Status.FreeAgent);
        var teams = w.Teams.Values.Where(t => !t.Dissolved).ToList();
        var fictionalVacant = teams.Count(t => !t.Real && t.Lineup.Values.Any(v => v is null));
        var realVacant = teams.Count(t => t.Real && Roles.All.Any(r => t.Lineup.GetValueOrDefault(r) is null));
        var realNpcStarting = teams.Where(t => t.Real).Sum(t => t.Lineup.Values.Count(v => v is not null && w.People[v].Origin != Origin.Real));
        var squads = teams.GroupBy(t => t.Real ? "RealPro" : t.Key).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
        {
            ["teams"] = g.Count(), ["mean_squad"] = Math.Round(g.Average(t => t.Squad.Count), 2), ["max_registered"] = g.Max(WorldBuilder.RegisteredCount),
            ["mean_strength"] = Math.Round(g.Average(t => Lineups.Strength(w, t)), 2), ["mean_funds"] = (long)g.Average(t => t.Funds),
            ["negative_funds"] = g.Count(t => t.Funds < 0), ["with_vacant_starter"] = g.Count(t => Roles.All.Any(r => t.Lineup.GetValueOrDefault(r) is null))
        });
        var durations = fa.GroupBy(p => Math.Min(3, w.Season - (p.FreeAgentSince ?? w.Season))).OrderBy(g => g.Key).ToDictionary(g => g.Key == 3 ? "3+" : g.Key.ToString(), g => g.Count());
        var depth = Roles.All.ToDictionary(r => r, r => (object)new Dictionary<string, int>
        {
            ["free_agents"] = fa.Count(p => p.Core.Role == r), ["free_agents_70_plus"] = fa.Count(p => p.Core.Role == r && w.Overall(p) >= 70),
            ["free_agents_potential_85_plus"] = fa.Count(p => p.Core.Role == r && p.Core.Potential >= 85)
        });
        var npcOverall = activeNpc.Select(w.Overall).ToList();
        var allActive = w.People.Values.Where(p => p.Status != Status.Retired).ToList();
        var topByRole = Roles.All.ToDictionary(r => r, r => Math.Round(allActive.Where(p => p.Core.Role == r).Select(w.Overall).OrderByDescending(x => x).Take(50).DefaultIfEmpty(0).Average(), 2));
        var npcTopByRole = Roles.All.ToDictionary(r => r, r => Math.Round(activeNpc.Where(p => p.Core.Role == r).Select(w.Overall).OrderByDescending(x => x).Take(50).DefaultIfEmpty(0).Average(), 2));
        var result = new Dictionary<string, object>
        {
            ["season"] = w.Season,
            ["npc_total_created"] = npcs.Count, ["npc_active"] = activeNpc.Count, ["npc_retired"] = npcs.Count - activeNpc.Count,
            ["census"] = census, ["npc_free_agents"] = fa.Count, ["npc_free_agent_percent"] = Math.Round(100.0 * fa.Count / Math.Max(1, activeNpc.Count), 2),
            ["free_agent_origins"] = origins, ["free_agent_seasons_unemployed"] = durations,
            ["real_active"] = realActive.Count, ["real_free_agents"] = realFa, ["real_retired"] = real.Count - realActive.Count,
            ["combined_free_agent_percent"] = Math.Round(100.0 * (fa.Count + realFa) / Math.Max(1, activeNpc.Count + realActive.Count), 2),
            ["academy_senior_registered"] = activeNpc.Count(p => p.SeniorRegistration),
            ["teams_active"] = teams.Count, ["teams_dissolved_total"] = w.Teams.Values.Count(t => t.Dissolved),
            ["fictional_teams_with_vacant_starter"] = fictionalVacant, ["real_teams_with_vacant_starter"] = realVacant,
            ["real_team_npc_in_lineup"] = realNpcStarting,
            ["squads"] = squads, ["market_depth"] = depth,
            ["npc_overall"] = Summary(npcOverall), ["npc_overall_above_82"] = npcOverall.Count(x => x > 82),
            ["npc_potential"] = Summary(activeNpc.Select(p => p.Core.Potential)),
            ["npc_potential_83_plus"] = activeNpc.Count(p => p.Core.Potential >= 83), ["npc_potential_90_plus"] = activeNpc.Count(p => p.Core.Potential >= 90),
            ["npc_potential_95_plus"] = activeNpc.Count(p => p.Core.Potential >= 95),
            ["real_overall"] = Summary(realActive.Select(w.Overall)), ["all_overall_94_plus"] = allActive.Count(p => w.Overall(p) >= 94),
            ["top50_by_role"] = topByRole, ["npc_top50_by_role"] = npcTopByRole,
            ["fa_overall"] = Summary(fa.Select(w.Overall)), ["employed_npc_overall"] = Summary(activeNpc.Where(p => p.Status == Status.Employed).Select(w.Overall)),
            ["events_total"] = w.Events.Count
        };
        if (flows is not null) result["flows"] = flows.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value);
        var percent = (double)result["npc_free_agent_percent"];
        if (percent > w.Config.Market.UnemploymentWarningPercent)
        {
            var warning = $"season {w.Season}: NPC unemployment {percent}% above the {w.Config.Market.UnemploymentWarningPercent}% warning level";
            result["warning"] = warning;
            if (!w.Warnings.Contains(warning)) w.Warnings.Add(warning);
        }
        return result;
    }

    /// <summary>The open market at one moment: NPC and real free agents, origins, quality and mix.</summary>
    public static Dictionary<string, object> Market(World w)
    {
        var events = EventsByPerson(w);
        var fa = w.People.Values.Where(p => p.Status == Status.FreeAgent && p.Origin == Origin.Generated).ToList();
        var active = w.People.Values.Count(p => p.Status != Status.Retired && p.Origin == Origin.Generated);
        string AgeGroup(int age) => age <= 19 ? "16-19" : age <= 22 ? "20-22" : age <= 26 ? "23-26" : age <= 29 ? "27-29" : "30+";
        var origins = FreeAgentOrigins.ToDictionary(c => c, _ => 0);
        foreach (var p in fa) origins[FreeAgentOrigin(w, p, events)]++;
        return new Dictionary<string, object>
        {
            ["season"] = w.Season, ["npc_free_agents"] = fa.Count, ["npc_percent"] = Math.Round(100.0 * fa.Count / Math.Max(1, active), 2),
            ["real_free_agents"] = w.People.Values.Count(p => p.Status == Status.FreeAgent && p.Origin == Origin.Real),
            ["origins"] = origins, ["overall"] = Summary(fa.Select(w.Overall)),
            ["overall_70_plus"] = fa.Count(p => w.Overall(p) >= 70), ["potential_85_plus"] = fa.Count(p => p.Core.Potential >= 85),
            ["ever_professional"] = fa.Count(p => p.EverProfessional),
            ["ages"] = fa.GroupBy(p => AgeGroup(w.Age(p))).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>How the initial NPC cohort developed: by age and potential band at creation, the mean initial and best
    /// Overall reached, the share that reached the initial potential (within 1 point) and the share retired.</summary>
    public static List<Dictionary<string, object>> Cohorts(World w)
    {
        string AgeBand(int a) => a <= 18 ? "16-18" : a <= 21 ? "19-21" : a <= 24 ? "22-24" : "25+";
        string PotBand(int p) => p >= 90 ? "90+" : p >= 83 ? "83-89" : p >= 75 ? "75-82" : "<75";
        return w.People.Values.Where(p => p.Origin == Origin.Generated && p.CreatedSeason == 0)
            .GroupBy(p => (Age: AgeBand(p.CreatedSeason - p.Core.BornSeason), Pot: PotBand(p.InitialPotential)))
            .OrderBy(g => g.Key.Age, StringComparer.Ordinal).ThenBy(g => g.Key.Pot, StringComparer.Ordinal)
            .Select(g => new Dictionary<string, object>
            {
                ["age_at_start"] = g.Key.Age, ["initial_potential"] = g.Key.Pot, ["n"] = g.Count(),
                ["initial_overall_mean"] = Math.Round(g.Average(p => p.InitialOverall), 1), ["initial_potential_mean"] = Math.Round(g.Average(p => p.InitialPotential), 1),
                ["best_overall_mean"] = Math.Round(g.Average(p => p.MaxOverall), 1), ["best_overall_max"] = g.Max(p => p.MaxOverall),
                ["reached_initial_potential_pct"] = Math.Round(100.0 * g.Count(p => p.MaxOverall >= p.InitialPotential - 1) / g.Count(), 1),
                ["retired_pct"] = Math.Round(100.0 * g.Count(p => p.Status == Status.Retired) / g.Count(), 1),
                ["breakthrough_pct"] = Math.Round(100.0 * g.Count(p => p.Breakthroughs > 0) / g.Count(), 1)
            }).ToList();
    }

    /// <summary>The initial population report (NPC_4000_GENERATION_REPORT): distributions by category, profile, age, role
    /// and nationality, identity checks and the free-agent mix.</summary>
    public static Dictionary<string, object> Generation(World w)
    {
        var npcs = w.People.Values.Where(p => p.Origin == Origin.Generated && p.CreatedSeason == 0).ToList();
        var events = EventsByPerson(w);
        var byCategory = npcs.GroupBy(p => Category(w, p)).OrderBy(g => Array.IndexOf(CensusCategories, g.Key)).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
        {
            ["count"] = g.Count(), ["overall"] = Summary(g.Select(w.Overall)), ["potential"] = Summary(g.Select(p => p.Core.Potential)),
            ["age"] = Summary(g.Select(w.Age)), ["potential_83_plus"] = g.Count(p => p.Core.Potential >= 83)
        });
        var fa = npcs.Where(p => p.Status == Status.FreeAgent).ToList();
        string AgeGroup(int age) => age <= 19 ? "16-19" : age <= 22 ? "20-22" : age <= 26 ? "23-26" : age <= 29 ? "27-29" : "30+";
        var mix = fa.GroupBy(p => AgeGroup(w.Age(p))).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
        {
            ["count"] = g.Count(), ["overall"] = Summary(g.Select(w.Overall)), ["potential_85_plus"] = g.Count(p => p.Core.Potential >= 85),
            ["origins"] = g.GroupBy(p => FreeAgentOrigin(w, p, events)).OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count())
        });
        var handles = npcs.Select(p => NpcGenerator.Normalize(p.Core.Handle)).ToList();
        var names = npcs.GroupBy(p => p.Core.Nationality + "|" + p.Core.Name).Count(g => g.Count() > 1);
        var headroomByAge = npcs.GroupBy(w.Age).OrderBy(g => g.Key).ToDictionary(g => g.Key.ToString(), g => (object)new Dictionary<string, object>
        {
            ["n"] = g.Count(), ["overall_p50"] = Pct(g.Select(w.Overall).OrderBy(x => x).ToList(), 50),
            ["headroom"] = Summary(g.Select(p => p.Core.Potential - w.Overall(p))), ["potential_83_plus"] = g.Count(p => p.Core.Potential >= 83)
        });
        return new Dictionary<string, object>
        {
            ["count"] = npcs.Count, ["unique_ids"] = npcs.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count(),
            ["unique_handles"] = handles.Distinct(StringComparer.Ordinal).Count(), ["handles_matching_real"] = handles.Count(h => w.RealHandles.Contains(h)),
            ["duplicate_name_nationality_groups"] = names,
            ["ids_with_real_prefix"] = npcs.Count(p => !p.Id.StartsWith(WorldBuilder.NpcPrefix, StringComparison.Ordinal)),
            ["overall"] = Summary(npcs.Select(w.Overall)), ["overall_histogram"] = Histogram(npcs.Select(w.Overall), 5),
            ["overall_above_cap"] = npcs.Count(p => w.Overall(p) > w.Config.Population.InitialOverallCap),
            ["potential"] = Summary(npcs.Select(p => p.Core.Potential)), ["potential_histogram"] = Histogram(npcs.Select(p => p.Core.Potential), 5),
            ["potential_83_plus"] = npcs.Count(p => p.Core.Potential >= 83), ["potential_90_plus"] = npcs.Count(p => p.Core.Potential >= 90),
            ["potential_95_plus"] = npcs.Count(p => p.Core.Potential >= 95), ["potential_99"] = npcs.Count(p => p.Core.Potential == 99),
            ["potential_below_overall"] = npcs.Count(p => p.Core.Potential < w.Overall(p)),
            ["age"] = Summary(npcs.Select(w.Age)), ["age_histogram"] = npcs.GroupBy(w.Age).OrderBy(g => g.Key).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            ["roles"] = npcs.GroupBy(p => p.Core.Role).OrderBy(g => Roles.All.IndexOf(g.Key)).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
                { ["count"] = g.Count(), ["overall_mean"] = Math.Round(g.Average(w.Overall), 2), ["potential_mean"] = Math.Round(g.Average(p => p.Core.Potential), 2) }),
            ["secondary_role_share"] = Math.Round(100.0 * npcs.Count(p => p.Core.SecondaryRole.Length > 0) / npcs.Count, 1),
            ["nationalities"] = npcs.GroupBy(p => p.Core.Nationality).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
                { ["count"] = g.Count(), ["overall_mean"] = Math.Round(g.Average(w.Overall), 2), ["potential_mean"] = Math.Round(g.Average(p => p.Core.Potential), 2) }),
            ["profiles"] = npcs.GroupBy(p => p.Profile).OrderBy(g => g.Key, StringComparer.Ordinal).ToDictionary(g => g.Key, g => (object)new Dictionary<string, object>
                { ["count"] = g.Count(), ["overall"] = Summary(g.Select(w.Overall)), ["age"] = Summary(g.Select(w.Age)) }),
            ["by_category"] = byCategory, ["free_agent_mix_by_age"] = mix, ["headroom_by_age"] = headroomByAge,
            ["late_bloomers"] = npcs.Count(p => p.Dev.LateBloomer),
            ["focus"] = npcs.GroupBy(p => p.Dev.Focus.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            ["mind_means"] = new Dictionary<string, double>
            {
                ["consistency"] = Math.Round(npcs.Average(p => p.Core.Mind.Consistency), 1), ["composure"] = Math.Round(npcs.Average(p => p.Core.Mind.Composure), 1),
                ["communication"] = Math.Round(npcs.Average(p => p.Core.Mind.Communication), 1), ["professionalism"] = Math.Round(npcs.Average(p => p.Core.Mind.Professionalism), 1),
                ["ambition"] = Math.Round(npcs.Average(p => p.Core.Mind.Ambition), 1), ["loyalty"] = Math.Round(npcs.Average(p => p.Core.Mind.Loyalty), 1)
            },
            ["history_events_per_npc"] = Math.Round(npcs.Average(p => events.GetValueOrDefault(p.Id)?.Count ?? 0), 2),
            ["npcs_without_history"] = npcs.Count(p => !events.ContainsKey(p.Id))
        };
    }
}
