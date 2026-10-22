using ManagementGame.Domain;

namespace WorldExperiment;

public static class Lineups
{
    public const int VacancyRating = 25;

    /// <summary>Real-starter policies (the D-H1 alternatives; STRICT is the rule of the v1 experiment):
    /// STRICT (A) real teams start real players only; EXIT (C) the same, and a real team that cannot field five after the
    /// window withdraws; NPC_FALLBACK (B) an NPC recruited through the market may start in a role no real player in the
    /// squad can fill; NPC_OPEN (B, sensitivity) real teams pick and recruit on merit regardless of origin.</summary>
    public static bool NpcMayPlay(World w) => w.Config.World.RealStarterPolicy is "NPC_FALLBACK" or "NPC_OPEN";
    public static bool Open(World w) => w.Config.World.RealStarterPolicy == "NPC_OPEN";

    /// <summary>A team's starting five: the best total rating in role over the three strongest candidates of each role
    /// (3^5 assignments), distinct people; a role nobody can play stays empty. Under STRICT and EXIT a real team only
    /// fields real players (the rule that NPCs never take a real team's starting slot).</summary>
    public static Dictionary<string, string?> Best(World w, WTeam t)
    {
        var open = t.Real && Open(w);
        var pool = t.Squad.Concat(t.SeniorRegistered).Distinct(StringComparer.Ordinal).Select(id => w.People[id])
            .Where(p => p.Status == Status.Employed && (!t.Real || open || p.Origin == Origin.Real)).ToList();
        var candidates = Roles.All.Select(role => pool.Select(p => (Person: p, Rating: w.InRole(p, role))).Where(x => x.Rating > 0)
            .OrderByDescending(x => x.Rating).ThenBy(x => x.Person.Id, StringComparer.Ordinal).Take(3).ToArray()).ToArray();
        var best = -1; var choice = new WPerson?[5]; var current = new WPerson?[5];
        void Search(int slot, int total)
        {
            if (slot == 5) { if (total > best) { best = total; current.CopyTo(choice, 0); } return; }
            var any = false;
            foreach (var (person, rating) in candidates[slot])
            {
                if (current.Take(slot).Contains(person)) continue;
                any = true; current[slot] = person; Search(slot + 1, total + rating); current[slot] = null;
            }
            if (!any) { current[slot] = null; Search(slot + 1, total); }
        }
        Search(0, 0);
        var lineup = Enumerable.Range(0, 5).ToDictionary(i => Roles.All[i], i => choice[i]?.Id, StringComparer.Ordinal);
        // Sensitivity variant only (not the rule): a real team may start an NPC in a role no real player can fill.
        if (t.Real && w.Config.World.RealStarterPolicy == "NPC_FALLBACK")
            foreach (var role in Roles.All.Where(r => lineup[r] is null))
                lineup[role] = t.Squad.Select(id => w.People[id]).Where(p => p.Status == Status.Employed && p.Origin == Origin.Generated && !lineup.Values.Contains(p.Id))
                    .OrderByDescending(p => w.InRole(p, role)).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault(p => w.InRole(p, role) > 0)?.Id;
        return lineup;
    }

    public static double Strength(World w, WTeam t) =>
        Roles.All.Average(role => t.Lineup.GetValueOrDefault(role) is { } id ? w.InRole(w.People[id], role) : VacancyRating);
}

/// <summary>The off-screen season (EXPERIMENT, bounded fidelity): league series from lineup strength with the production
/// series resolver, playing time, a season budget, development (dev2), then the season end: retirement, contract ends,
/// dismissals, academy graduation, team dissolution and replacement, youth intake, the transfer window, releases and new
/// lineups. Every choice is keyed by season and entity; iteration is in ID order.</summary>
public static class SeasonSim
{
    private static void Count(Dictionary<string, int> stats, string key, int by = 1) => stats[key] = stats.GetValueOrDefault(key) + by;

    public static Dictionary<string, int> Play(World w, Calibration cal)
    {
        var s = ++w.Season;
        var stats = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var p in w.People.Values) { p.SeasonMatches = 0; p.SeasonShare = 0; p.MovedThisWindow = false; }
        Compete(w, s, stats);
        Economy(w);
        Develop(w, s);
        Retire(w, s, stats);
        ContractsEnd(w, s, stats);
        Dismiss(w, s, stats);
        Graduate(w, s, stats);
        Dissolve(w, s, stats);
        Intake(w, cal, s, stats);
        w.PreWindow.Add(Reports.Market(w));
        Recruit(w, s, stats);
        Release(w, s, stats);
        if (w.Config.World.RealStarterPolicy == "EXIT") Withdraw(w, s, stats);
        foreach (var t in w.Teams.Values.Where(t => !t.Dissolved)) t.Lineup = Lineups.Best(w, t);
        return stats;
    }

    /// <summary>EXIT (D-H1 C): a real organization that still cannot field five eligible players after the window withdraws
    /// from competition. It keeps its identity (the team record stays real and is marked withdrawn, never renamed), its
    /// people leave as free agents with a recorded exit, and its league slot stays empty (no replacement mechanism is
    /// defined).</summary>
    private static void Withdraw(World w, int s, Dictionary<string, int> stats)
    {
        foreach (var t in w.Teams.Values.Where(t => t.Real && !t.Dissolved).ToArray())
        {
            if (Lineups.Best(w, t).Values.All(x => x is not null)) continue;
            foreach (var id in t.Squad.ToArray())
            {
                var p = w.People[id]; var contract = p.ContractId;
                WorldBuilder.Leave(w, p, Status.FreeAgent, s);
                w.Record(p.Id, EventType.TeamDissolved, t.Id, null, s, "organization withdrew from competition: cannot field five eligible players", contract, Era.Simulation);
                Count(stats, "real_withdrawal_players");
            }
            t.Dissolved = true; t.WithdrawnSeason = s;
            Count(stats, "real_teams_withdrawn");
        }
    }

    // ---------- competition and playing time ----------
    private static void Compete(World w, int s, Dictionary<string, int> stats)
    {
        var b = w.Content.Balance; var spread = Competition.Spread(b, Posture.Balanced, 50);
        foreach (var league in w.Teams.Values.Where(t => !t.Dissolved).GroupBy(t => t.LeagueId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var teams = league.OrderBy(t => t.Id, StringComparer.Ordinal).ToArray();
            var light = league.Key.StartsWith("league:am:", StringComparison.Ordinal) || league.Key.StartsWith("league:ac:", StringComparison.Ordinal);
            var games = light ? 1 : b.RegularGames; var legs = light ? 1 : 2;
            var strength = teams.ToDictionary(t => t.Id, t => (int)Math.Round(Lineups.Strength(w, t)), StringComparer.Ordinal);
            var wins = teams.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);
            var played = teams.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);
            var diff = teams.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);
            if (teams.Length < 2)
            {
                Count(stats, "underfilled_leagues");
                foreach (var t in teams) { t.SeasonGames = light ? 9 : 45; t.SeasonWinShare = 0.5; t.Place = 1; }
            }
            else
            {
                for (var i = 0; i < teams.Length; i++)
                    for (var j = i + 1; j < teams.Length; j++)
                        for (var leg = 0; leg < legs; leg++)
                        {
                            var (home, away) = leg == 0 ? (teams[i], teams[j]) : (teams[j], teams[i]);
                            var (won, lost, _) = Competition.Play(w.Seed, "xworld", $"{s}:{league.Key}:{home.Id}:{away.Id}:{leg}",
                                strength[home.Id] - strength[away.Id], spread, games, b.LogitScale, b.SeriesForm);
                            played[home.Id] += won + lost; played[away.Id] += won + lost;
                            diff[home.Id] += won - lost; diff[away.Id] += lost - won;
                            if (won > lost) wins[home.Id]++; else wins[away.Id]++;
                            Count(stats, "series");
                            if (home.Real && strength[home.Id] != strength[away.Id])
                            {
                                Count(stats, "real_series_unequal");
                                if (won > lost == strength[home.Id] > strength[away.Id]) Count(stats, "real_series_stronger_won");
                            }
                        }
                var series = (teams.Length - 1) * legs;
                var ranked = teams.OrderByDescending(t => wins[t.Id]).ThenByDescending(t => diff[t.Id]).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();
                for (var k = 0; k < ranked.Length; k++)
                {
                    var t = ranked[k];
                    t.Place = k + 1; t.SeasonGames = played[t.Id]; t.SeasonWinShare = wins[t.Id] / (double)series;
                }
            }
            foreach (var t in teams) PlayingTime(w, t);
        }
    }

    private static void PlayingTime(World w, WTeam t)
    {
        var rotation = w.Config.Market.Rotation[t.Strategy.ToString()] / 100.0;
        var starters = t.Lineup.Values.Where(x => x is not null).Select(x => x!).ToHashSet(StringComparer.Ordinal);
        var bench = t.Squad.Concat(t.SeniorRegistered).Distinct(StringComparer.Ordinal).Where(id => !starters.Contains(id)).Select(id => w.People[id])
            .Where(p => p.Status == Status.Employed && (!t.Real || p.Origin == Origin.Real || Lineups.NpcMayPlay(w)) && Roles.All.Any(r => w.InRole(p, r) > 0))
            .OrderByDescending(p => w.Overall(p)).ThenBy(p => p.Id, StringComparer.Ordinal).Take(2).ToArray();
        if (bench.Length == 0) rotation = 0;
        void Give(WPerson p, double share)
        {
            p.SeasonShare = Math.Max(p.SeasonShare, share);
            p.SeasonMatches += (int)Math.Round(share * t.SeasonGames);
        }
        foreach (var id in starters) Give(w.People[id], 1 - rotation);
        foreach (var p in bench) Give(p, rotation * starters.Count / bench.Length / Roles.All.Length);
    }

    // ---------- the season budget ----------
    private static void Economy(World w)
    {
        var e = w.Config.Economy;
        foreach (var league in w.Teams.Values.Where(t => !t.Dissolved).GroupBy(t => t.LeagueId))
        {
            var n = league.Count();
            foreach (var t in league)
            {
                var prize = n >= 2 ? (long)(e.ChampionBonus * t.Revenue * (n - t.Place) / (n - 1)) : 0;
                t.Funds += t.Revenue + prize - WorldBuilder.Wages(w, t) * w.Config.SeasonDays;
                if (n >= 2) t.Reputation = Math.Clamp(t.Reputation + (int)Math.Round(8 * ((n - t.Place) / (double)(n - 1) - 0.5)), 1, 99);
            }
        }
    }

    // ---------- development ----------
    private static void Develop(World w, int s)
    {
        var d = w.Config.Development;
        foreach (var p in w.People.Values.Where(p => p.Status != Status.Retired))
        {
            DevEnvironment env;
            if (p.Status == Status.Employed)
            {
                var t = w.Teams[p.TeamId!];
                var morale = Math.Clamp(55 + (int)Math.Round(20 * (t.SeasonWinShare - 0.5)) + (p.SeasonShare >= 0.5 ? 5 : p.SeasonShare > 0 ? 0 : -8), 20, 90);
                env = new DevEnvironment(t.CoachDevelopment, t.Intensity, p.SeasonShare, p.SeasonMatches, morale);
                p.ContractSeasons++;
            }
            else
                env = new DevEnvironment(d.FreeAgentCoach, Intensity.Normal, 0, 0, Math.Clamp(42 - 5 * (s - (p.FreeAgentSince ?? s)), 15, 60));
            p.SeasonMorale = env.Morale;
            DevelopmentV2.Season(d, w.Content, w.Seed, p, w.Age(p), s, env);
            p.MatchesTotal += p.SeasonMatches;
        }
    }

    // ---------- season end ----------
    private static void Retire(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        foreach (var p in w.People.Values.Where(p => p.Status != Status.Retired).ToArray())
        {
            var ageNext = s + 1 - p.Core.BornSeason;
            var layer = p.Status == Status.Employed ? (w.Teams[p.TeamId!].Real ? "RealPro" : w.Teams[p.TeamId!].Key) : "FreeAgent";
            var mult = m.RetirementLayer[layer] + (p.Status == Status.FreeAgent ? m.RetirementPerFreeAgentSeason * (s - (p.FreeAgentSince ?? s)) : 0);
            var rating = Math.Clamp(1 + (m.RetirementRatingPivot - w.Overall(p)) * m.RetirementRatingPercent / 100, m.RetirementRatingRange[0], m.RetirementRatingRange[1]);
            var chance = Math.Min(1, Curve.Step(m.RetirementByAge, ageNext) / 10_000 * mult * rating);
            if (!Draw.Chance(w.Seed, "xretire", $"{s}:{p.Id}", "retire", chance)) continue;
            var from = p.TeamId;
            WorldBuilder.Leave(w, p, Status.Retired, s);
            w.Record(p.Id, EventType.Retired, from, null, s, from is null ? "retired from the open market" : "retired", null, Era.Simulation);
            Count(stats, "retired"); Count(stats, p.Origin == Origin.Real ? "retired_real" : "retired_npc");
        }
    }

    private static WContract Extend(World w, WPerson p, WTeam t, int s)
    {
        var old = w.Contracts[p.ContractId!];
        old.Active = false;
        var range = w.Config.Market.ContractSeasons[t.Real ? "RealPro" : t.Key];
        var c = new WContract { Id = $"ct:{++w.NextContract:D7}", PersonId = p.Id, TeamId = t.Id, StartSeason = s + 1,
            EndSeason = s + Draw.Int(w.Seed, "xcontract", $"{s}:{p.Id}", "extend", range[0], range[1]), Salary = WorldBuilder.Salary(w, p, t, s + 1) };
        w.Contracts[c.Id] = c; p.ContractId = c.Id;
        return c;
    }

    private static void ContractsEnd(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        foreach (var c in w.Contracts.Values.Where(c => c.Active && c.EndSeason == s).ToArray())
        {
            var p = w.People[c.PersonId];
            if (p.Status != Status.Employed || p.ContractId != c.Id) continue;
            var t = w.Teams[c.TeamId];
            var ageNext = s + 1 - p.Core.BornSeason; var o = w.Overall(p);
            if (t.Layer == Layer.Academy && ageNext >= m.AcademyGraduationAge) continue;   // the academy graduation decides
            var inLineup = t.Lineup.Values.Contains(p.Id);
            var lineupRatings = t.Lineup.Where(x => x.Value is not null).Select(x => w.InRole(w.People[x.Value!], x.Key)).ToArray();
            var weakest = lineupRatings.Length == 0 ? 0 : lineupRatings.Min();
            var keep = inLineup || o >= weakest - 2 || ageNext <= 22 && p.Core.Potential - o >= 6 || t.Squad.Count <= 5;
            if (t.Strategy is TeamStrategy.Develop or TeamStrategy.Conserve && ageNext >= 28 && !inLineup) keep = false;
            var salary = WorldBuilder.Salary(w, p, t, s + 1);
            if (WorldBuilder.Wages(w, t) - c.Salary + salary > WorldBuilder.WageCap(w, t) && !(inLineup && t.Funds > 0)) keep = false;
            var ceiling = m.LayerCeiling[t.Real ? "RealPro" : t.Key];
            var seeksLevel = p.Core.Mind.Ambition >= m.AmbitionDeparture && o >= ceiling;
            var wantsTime = p.SeasonShare < m.BenchShare && p.Core.Mind.Ambition >= m.BenchDepartureAmbition && !inLineup;
            if (keep && !seeksLevel && !wantsTime)
            {
                var next = Extend(w, p, t, s);
                w.Record(p.Id, EventType.ContractExtension, t.Id, t.Id, s, "contract extended", next.Id, Era.Simulation);
                Count(stats, "extended");
                continue;
            }
            WorldBuilder.Leave(w, p, Status.FreeAgent, s);
            if (keep)
            {
                w.Record(p.Id, EventType.VoluntaryDeparture, t.Id, null, s, seeksLevel ? "declined to renew: seeks a higher level" : "declined to renew: wants playing time", c.Id, Era.Simulation);
                Count(stats, "voluntary_departure");
            }
            else
            {
                w.Record(p.Id, EventType.ContractExpired, t.Id, null, s, "contract expired, not renewed", c.Id, Era.Simulation);
                Count(stats, "contract_expired");
            }
        }
    }

    private static void Dismiss(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        foreach (var p in w.People.Values.Where(p => p.Status == Status.Employed && p.Core.Mind.Professionalism < m.DismissalProfessionalism).ToArray())
        {
            var chance = (m.DismissalProfessionalism - p.Core.Mind.Professionalism) * m.DismissalBpPerPoint / 10_000.0;
            if (!Draw.Chance(w.Seed, "xconduct", $"{s}:{p.Id}", "dismissal", chance)) continue;
            var from = p.TeamId; var contract = p.ContractId;
            WorldBuilder.Leave(w, p, Status.FreeAgent, s);
            w.Record(p.Id, EventType.Dismissed, from, null, s, "dismissed for repeated conduct breaches", contract, Era.Simulation);
            Count(stats, "dismissed");
        }
    }

    private static void Graduate(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market; var max = w.Config.World.MaxSquad;
        foreach (var academy in w.Teams.Values.Where(t => t.Layer == Layer.Academy && !t.Dissolved).ToArray())
        {
            var senior = w.Teams[academy.ParentTeamId!];
            foreach (var id in academy.Squad.Where(id => s + 1 - w.People[id].Core.BornSeason >= m.AcademyGraduationAge).OrderBy(id => id, StringComparer.Ordinal).ToArray())
            {
                var p = w.People[id]; var o = w.Overall(p);
                var reserves = senior.Squad.Where(x => !senior.Lineup.Values.Contains(x)).Select(x => w.Overall(w.People[x])).ToArray();
                var room = WorldBuilder.RegisteredCount(senior) - (p.SeniorRegistration ? 1 : 0) < max;
                var affordable = WorldBuilder.Wages(w, senior) + WorldBuilder.Salary(w, p, senior, s + 1) <= WorldBuilder.WageCap(w, senior);
                var better = senior.Squad.Count < senior.SquadTarget || reserves.Length > 0 && o > reserves.Min();
                var contract = p.ContractId;
                WorldBuilder.Leave(w, p, Status.FreeAgent, s);
                if (o >= m.AcademyPromotionMinimum && room && affordable && better)
                {
                    var c = WorldBuilder.Hire(w, senior, p, s + 1, s + Draw.Int(w.Seed, "xcontract", $"{s}:{p.Id}", "promotion", 1, 3), Era.Simulation);
                    w.Record(p.Id, EventType.SeniorPromotion, academy.Id, senior.Id, s, "promoted from the academy", c.Id, Era.Simulation);
                    Count(stats, "senior_promotion");
                }
                else
                {
                    w.Record(p.Id, EventType.AcademyNotRetained, academy.Id, null, s, "graduated from the academy without a senior place", contract, Era.Simulation);
                    Count(stats, "academy_not_retained");
                }
            }
        }
    }

    private static void Dissolve(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        foreach (var t in w.Teams.Values.Where(t => !t.Dissolved && t.Layer is Layer.Amateur or Layer.SemiPro).ToArray())
        {
            var bp = t.Funds < -t.Revenue / 5 ? m.DebtDissolutionBp : m.DissolutionBp[t.Key];
            if (!Draw.Chance(w.Seed, "xdissolve", $"{s}:{t.Id}", "dissolve", bp / 10_000.0)) continue;
            foreach (var id in t.Squad.ToArray())
            {
                var p = w.People[id]; var contract = p.ContractId;
                WorldBuilder.Leave(w, p, Status.FreeAgent, s);
                w.Record(p.Id, EventType.TeamDissolved, t.Id, null, s, "team dissolved", contract, Era.Simulation);
                Count(stats, "team_dissolved_players");
            }
            t.Dissolved = true;
            Count(stats, "teams_dissolved");
            var prefix = t.Layer == Layer.Amateur ? "am" : "sp";
            var id2 = $"team:{prefix}:n{++w.NextTeam:D4}";
            var founded = WorldBuilder.NewTeam(w, id2, "org:" + id2[5..], NpcGenerator.UniqueTeamName(w, id2), t.Layer, t.LeagueId, s + 1);
            founded.SquadTarget = t.SquadTarget;
            Count(stats, "teams_founded");
        }
    }

    private static void Intake(World w, Calibration cal, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        var profile = w.Config.Population.Profiles.Single(x => x.Id == "PROSPECT");
        var first = KeyedRandom.Range(w.Seed, "xintake", $"s{s}", "first-role", Roles.All.Length);
        // Replacement: one entrant per NPC retirement, bounded (v1). OneToOne: exactly one per NPC retirement, unbounded
        // (E2 candidate). Pathways: one-to-one, each entrant drawn from a configured entry pathway. Fixed: a fixed number.
        // Real retirements never trigger an entrant: no real person is ever created.
        var retired = stats.GetValueOrDefault("retired_npc");
        var count = m.IntakeMode switch
        {
            "Replacement" => Math.Clamp(retired, m.IntakeRange[0], m.IntakeRange[1]),
            "OneToOne" or "Pathways" => retired,
            // Alternative (E2, measured, not the rule): one NPC entrant per retirement of anyone, real players included, so
            // the seats real retirees leave are matched by entrants. Still no real person is created.
            "OneToOneAll" => retired + stats.GetValueOrDefault("retired_real"),
            _ => m.Intake
        };
        for (var i = 0; i < count; i++)
        {
            var id = $"{WorldBuilder.NpcPrefix}c{s:D4}:{i + 1:D6}";
            var role = Roles.All[(first + i) % Roles.All.Length];
            WPerson p; var reason = "new prospect entered the open market, never contracted";
            if (m.IntakeMode == "Pathways" && w.Pathways is { Length: > 0 } pathways)
            {
                var path = Draw.Weighted(w.Seed, "xintake", id, "pathway", pathways, x => x.Weight);
                var source = w.Config.Population.Profiles.Single(x => x.Id == path.Profile);
                var shaped = source with { Id = path.Id, AgeMin = path.AgeMin, AgeMax = path.AgeMax };
                int? age = path.Profile == "PROSPECT" ? Draw.Weighted(w.Seed, "xintake", id, "age", m.IntakeAges, a => a.Weight).Value : null;
                p = NpcGenerator.Create(w, cal, id, role, shaped, s, age, path.Profile == "PROSPECT" ? m.IntakeTiers : null);
                reason = $"new player entered the open market ({path.Id}), never contracted";
                Count(stats, "intake_" + path.Id);
            }
            else
            {
                var age = Draw.Weighted(w.Seed, "xintake", id, "age", m.IntakeAges, a => a.Weight).Value;
                p = NpcGenerator.Create(w, cal, id, role, profile, s, age, m.IntakeTiers);
            }
            p.Status = Status.FreeAgent; p.FreeAgentSince = s;
            w.People[id] = p; w.NextPerson++;
            w.Record(id, EventType.GeneratedProspect, null, null, s, reason, null, Era.Simulation);
            Count(stats, "intake");
        }
    }

    // ---------- the transfer window ----------
    private sealed record Offer(WPerson Person, WTeam? Seller, double Value, long Fee, bool Promotion);

    private static bool Willing(World w, WPerson p, WTeam buyer, int s, bool npcFallback = false)
    {
        var m = w.Config.Market; var age = s + 1 - p.Core.BornSeason; var o = w.Overall(p);
        if (buyer.Layer == Layer.Academy && age > 20) return false;
        if (buyer.Real && p.Origin != Origin.Real && !npcFallback && buyer.Squad.Count(id => w.People[id].Origin == Origin.Generated) >= buyer.RealNpcSeats) return false;
        if (!buyer.Real && p.Origin == Origin.Real && !w.Config.World.RealPlayersOnFictionalTeams) return false;
        if (p.Status != Status.FreeAgent) return true;
        var unemployed = s - (p.FreeAgentSince ?? s);
        if (unemployed >= m.AspirationPatienceSeasons) return true;
        // The reservation wage: a player who just lost a contract does not take much less than it paid, less so the longer
        // the search lasts.
        if (p.LastSalary > 0 && WorldBuilder.Salary(w, p, buyer, s + 1) < p.LastSalary * m.ReservationShare * Math.Max(0, 1 - m.ReservationDecayPerSeason * unemployed)) return false;
        if (o >= m.AspirationPro) return WorldBuilder.ProLevel(buyer);
        if (o >= m.AspirationSemi) return buyer.Layer is not (Layer.Amateur or Layer.Academy);
        return true;
    }

    private static Offer? Find(World w, WTeam t, string? role, double minimum, int s, Dictionary<string, List<WPerson>> index, bool reserveOnly,
        bool npcFallback = false)
    {
        var cap = WorldBuilder.WageCap(w, t); var wages = WorldBuilder.Wages(w, t);
        if (WorldBuilder.RegisteredCount(t) >= w.Config.World.MaxSquad) return null;
        Offer? best = null;
        foreach (var p in role is null ? index["*"] : index[role])
        {
            if (p.Status == Status.Retired || p.MovedThisWindow || p.TeamId == t.Id) continue;
            if (t.Real && p.Origin != Origin.Real && !reserveOnly && !npcFallback) continue;
            if (reserveOnly && p.Status != Status.FreeAgent) continue;
            var rating = w.InRole(p, role ?? p.Core.Role);
            if (rating <= 0) continue;
            var z = w.Noise(t.Layer, p, s);
            var sigma = w.Config.Market.ScoutingNoise[t.Layer.ToString()];
            var observed = rating + sigma * z.Overall;
            if (observed < minimum) continue;
            WTeam? seller = null; long fee = 0; var promotion = false;
            if (p.Status == Status.Employed)
            {
                seller = w.Teams[p.TeamId!];
                if (seller.Id == t.AcademyTeamId) promotion = true;
                else if (WorldBuilder.LayerOrder(seller) > WorldBuilder.LayerOrder(t)) { }
                else if (WorldBuilder.LayerOrder(seller) == WorldBuilder.LayerOrder(t) && t.Strategy == TeamStrategy.Contend
                         && !seller.Lineup.Values.Contains(p.Id) && t.Reputation > seller.Reputation) { }
                else continue;
                if (!promotion)
                {
                    var contract = w.Contracts[p.ContractId!];
                    fee = (long)(WorldBuilder.Fee(w, p, seller, s + 1) * Math.Clamp(0.5 + 0.25 * (contract.EndSeason - s), 0.5, 1.0));
                    if (fee > t.Funds) continue;
                }
            }
            if (!Willing(w, p, t, s, npcFallback)) continue;
            if (wages + WorldBuilder.Salary(w, p, t, s + 1) > cap) continue;
            var age = s + 1 - p.Core.BornSeason;
            var youth = age <= 21 ? 1 : age <= 23 ? 0.5 : 0;
            var potential = p.Core.Potential + 2 * sigma * z.Potential;
            var weight = reserveOnly ? 1.0 : w.Config.World.StarterYouthWeight;
            var value = observed + weight * t.YouthFocus / 100.0 * Math.Max(0, potential - observed) * youth;
            if (best is null || value > best.Value || value == best.Value && string.CompareOrdinal(p.Id, best.Person.Id) < 0)
                best = new Offer(p, seller, value, fee, promotion);
        }
        return best;
    }

    private static void Acquire(World w, WTeam t, Offer o, int s, Dictionary<string, int> stats)
    {
        var p = o.Person; var range = w.Config.Market.ContractSeasons[t.Real ? "RealPro" : t.Key];
        var end = s + Draw.Int(w.Seed, "xcontract", $"{s}:{t.Id}:{p.Id}", "signing", range[0], range[1]);
        // Never contracted: no season under contract and no contract that ended (a released prospect is not a first signing).
        var wasPro = p.EverProfessional; var neverContracted = p.ContractSeasons == 0 && p.LastSalary == 0 && p.Status == Status.FreeAgent;
        if (o.Seller is null)
        {
            var c = WorldBuilder.Hire(w, t, p, s + 1, end, Era.Simulation);
            var type = WorldBuilder.ProLevel(t) && !wasPro ? EventType.FirstProfessionalContract : t.Layer == Layer.Academy ? EventType.AcademyRecruitment
                : neverContracted ? EventType.FirstAmateurContract : EventType.Signed;
            w.Record(p.Id, type, null, t.Id, s, neverContracted ? "first contract, signed from the open market" : "signed from the open market", c.Id, Era.Simulation);
            Count(stats, "signed_from_market"); Count(stats, p.Origin == Origin.Real ? "signed_real" : "signed_npc");
        }
        else
        {
            var seller = o.Seller;
            WorldBuilder.Leave(w, p, Status.FreeAgent, s);
            var c = WorldBuilder.Hire(w, t, p, s + 1, end, Era.Simulation);
            if (o.Promotion)
            {
                w.Record(p.Id, EventType.SeniorPromotion, seller.Id, t.Id, s, "promoted from the academy to fill a starting role", c.Id, Era.Simulation);
                Count(stats, "senior_promotion");
            }
            else
            {
                seller.Funds += o.Fee; t.Funds -= o.Fee;
                var up = WorldBuilder.LayerOrder(seller) > WorldBuilder.LayerOrder(t);
                var reason = up && seller.Layer is Layer.Amateur or Layer.SemiPro && WorldBuilder.ProLevel(t) ? "recruited from a lower tier to a professional team"
                    : up ? "recruited from a lower tier" : "transfer between teams";
                w.Record(p.Id, WorldBuilder.ProLevel(t) && !wasPro ? EventType.FirstProfessionalContract : EventType.Transfer, seller.Id, t.Id, s,
                    reason + $" (fee {o.Fee})", c.Id, Era.Simulation);
                Count(stats, up ? "transfer_up" : "transfer_same"); Count(stats, "transfer_fees", (int)Math.Min(int.MaxValue, o.Fee / 100));
                if (w.Overall(p) >= 88) { Count(stats, "transfer_88_plus"); Count(stats, "transfer_88_plus_fees", (int)Math.Min(int.MaxValue, o.Fee / 100)); }
                if (t.Real) Count(stats, "real_buyer_fees", (int)Math.Min(int.MaxValue, o.Fee / 100));
                if (up && seller.Layer is Layer.Amateur or Layer.SemiPro && WorldBuilder.ProLevel(t)) Count(stats, "amateur_semi_to_pro");
                if (p.Origin == Origin.Real) Count(stats, "transfer_real");
            }
        }
        p.MovedThisWindow = true;
    }

    private static void Recruit(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market;
        var active = w.People.Values.Where(p => p.Status != Status.Retired).ToList();
        var index = Roles.All.ToDictionary(r => r, r => active.Where(p => p.Core.Role == r || p.Core.SecondaryRole == r).ToList(), StringComparer.Ordinal);
        index["*"] = active.Where(p => p.Status == Status.FreeAgent).ToList();
        w.ScoutNoise.Clear();
        var order = w.Teams.Values.Where(t => !t.Dissolved).OrderBy(WorldBuilder.LayerOrder).ThenByDescending(t => t.Reputation).ThenBy(t => t.Id, StringComparer.Ordinal).ToArray();
        foreach (var t in order)
        {
            t.Lineup = Lineups.Best(w, t);
            var open = t.Real && Lineups.Open(w);
            foreach (var role in Roles.All.Where(r => t.Lineup[r] is null).ToArray())
            {
                var offer = Find(w, t, role, double.MinValue, s, index, false, npcFallback: open);
                if (offer is null && t.Real && w.Config.World.RealStarterPolicy == "NPC_FALLBACK")
                {
                    offer = Find(w, t, role, double.MinValue, s, index, false, npcFallback: true);
                    if (offer is not null) Count(stats, "real_npc_fallback_signed");
                }
                if (offer is null)
                {
                    Count(stats, t.Real ? "real_starter_unfilled" : "starter_unfilled");
                    if (t.Real) Count(stats, $"real_unfilled_{role}_{Shortage(w, t, role, s, index, open || w.Config.World.RealStarterPolicy == "NPC_FALLBACK")}");
                    continue;
                }
                if (t.Real) Count(stats, offer.Person.Origin == Origin.Real ? "real_starter_filled_real" : "real_starter_filled_npc");
                Acquire(w, t, offer, s, stats);
                Count(stats, "starter_filled");
                t.Lineup = Lineups.Best(w, t);
            }
            if (t.Strategy is TeamStrategy.Contend or TeamStrategy.Balanced)
                for (var k = 0; k < m.MaxUpgrades; k++)
                {
                    var weakest = t.Lineup.Where(x => x.Value is not null).Select(x => (Role: x.Key, Rating: w.InRole(w.People[x.Value!], x.Key)))
                        .OrderBy(x => x.Rating).ThenBy(x => Roles.All.IndexOf(x.Role)).FirstOrDefault();
                    if (weakest.Role is null) break;
                    var offer = Find(w, t, weakest.Role, weakest.Rating + m.UpgradeMargin, s, index, false, npcFallback: open);
                    if (offer is null) break;
                    Acquire(w, t, offer, s, stats);
                    Count(stats, "upgrades");
                    if (t.Real) Count(stats, offer.Person.Origin == Origin.Real ? "real_upgrades_real" : "real_upgrades_npc");
                    t.Lineup = Lineups.Best(w, t);
                }
            while (t.Squad.Count < t.SquadTarget)
            {
                var offer = Find(w, t, null, double.MinValue, s, index, true, npcFallback: open);
                if (offer is null) { Count(stats, "reserve_unfilled"); break; }
                Acquire(w, t, offer, s, stats);
                Count(stats, "reserve_filled");
            }
        }
    }

    /// <summary>Why a real team's starting vacancy stayed open (diagnostics only; changes nothing): squad_full; none (no
    /// candidate of an allowed origin can play the role); protected (every candidate is held by a team the buyer may not
    /// poach); fee (no affordable fee); unwilling (willingness or reservation wage); wage (over the wage cap).</summary>
    private static string Shortage(World w, WTeam t, string role, int s, Dictionary<string, List<WPerson>> index, bool npcAllowed)
    {
        if (WorldBuilder.RegisteredCount(t) >= w.Config.World.MaxSquad) return "squad_full";
        var cap = WorldBuilder.WageCap(w, t); var wages = WorldBuilder.Wages(w, t);
        bool protectedOnly = false, fee = false, unwilling = false, wage = false;
        foreach (var p in index[role])
        {
            if (p.Status == Status.Retired || p.MovedThisWindow || p.TeamId == t.Id || w.InRole(p, role) <= 0) continue;
            if (p.Origin != Origin.Real && !npcAllowed) continue;
            if (p.Status == Status.Employed)
            {
                var seller = w.Teams[p.TeamId!];
                var poachable = WorldBuilder.LayerOrder(seller) > WorldBuilder.LayerOrder(t) || WorldBuilder.LayerOrder(seller) == WorldBuilder.LayerOrder(t)
                    && t.Strategy == TeamStrategy.Contend && !seller.Lineup.Values.Contains(p.Id) && t.Reputation > seller.Reputation;
                if (!poachable) { protectedOnly = true; continue; }
                var contract = w.Contracts[p.ContractId!];
                if ((long)(WorldBuilder.Fee(w, p, seller, s + 1) * Math.Clamp(0.5 + 0.25 * (contract.EndSeason - s), 0.5, 1.0)) > t.Funds) { fee = true; continue; }
            }
            if (!Willing(w, p, t, s, npcAllowed)) { unwilling = true; continue; }
            if (wages + WorldBuilder.Salary(w, p, t, s + 1) > cap) { wage = true; continue; }
        }
        return fee ? "fee" : wage ? "wage" : unwilling ? "unwilling" : protectedOnly ? "protected" : "none";
    }

    private static void Release(World w, int s, Dictionary<string, int> stats)
    {
        var m = w.Config.Market; var e = w.Config.Economy;
        foreach (var t in w.Teams.Values.Where(t => !t.Dissolved))
        {
            var lineup = Lineups.Best(w, t).Values.Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
            while (t.Squad.Count > t.SquadTarget + m.ReleaseOverTarget)
            {
                var id = t.Squad.Where(x => !lineup.Contains(x)).OrderBy(x => w.Overall(w.People[x])).ThenBy(x => x, StringComparer.Ordinal).FirstOrDefault();
                if (id is null) break;
                var p = w.People[id]; var c = w.Contracts[p.ContractId!];
                t.Funds -= (long)(e.ReleaseShare * c.Salary * w.Config.SeasonDays * Math.Max(0, c.EndSeason - s));
                WorldBuilder.Leave(w, p, Status.FreeAgent, s);
                w.Record(p.Id, EventType.Released, t.Id, null, s, "released: surplus to the squad", c.Id, Era.Simulation);
                Count(stats, "released");
            }
        }
    }
}
