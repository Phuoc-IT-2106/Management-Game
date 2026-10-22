using ManagementGame.Domain;

namespace WorldExperiment;

/// <summary>Initial world creation (EXPERIMENT): Population generation -> career profiles -> organizations and teams ->
/// free-agent cohort -> employment allocation -> squad registration -> starting five -> backstories -> validation.</summary>
public static class WorldBuilder
{
    public const string NpcPrefix = "player:npc:w1:";
    public static readonly Layer[] Fictional = [Layer.FictionalPro, Layer.Academy, Layer.SemiPro, Layer.Amateur];

    public static int LayerOrder(WTeam t) => t.Key switch
    {
        "Real:a" => 0, "Real:b" => 1, "FictionalPro" => 2, "Real:c" => 3, "SemiPro" => 4, "Real:d" => 5, "Academy" => 6, _ => 7
    };
    public static bool ProLevel(WTeam t) => t.Key is "Real:a" or "Real:b" or "FictionalPro";
    public static long WageCap(World w, WTeam t) => (long)(t.Revenue * w.Config.Economy.WageShare / w.Config.SeasonDays);
    public static long Salary(World w, WPerson p, WTeam t, int season) =>
        Math.Max(10, (long)Math.Round(Pay.Player(p.Core, season, w.Content) * w.Config.Economy.WageFactor[t.Key]));
    public static long Fee(World w, WPerson p, WTeam seller, int season) =>
        (long)Math.Round(Pay.Fee(p.Core, season, w.Content) * w.Config.Economy.FeeFactor[seller.Key]);
    public static long Wages(World w, WTeam t) => t.Squad.Sum(id => w.Contracts[w.People[id].ContractId!].Salary);
    public static int RegisteredCount(WTeam t) => t.Squad.Count + t.SeniorRegistered.Count;

    private static string KeyOf(Layer layer) => layer.ToString();

    public static WTeam NewTeam(World w, string id, string org, string name, Layer layer, string league, int season, string? parent = null)
    {
        var cfg = w.Config; var seed = w.Seed; var key = KeyOf(layer);
        var strategies = cfg.World.Strategies[key];
        var strategy = (TeamStrategy)Draw.Weighted(seed, "xteam", id, "strategy", Enumerable.Range(0, 4).ToArray(), i => strategies[i]);
        var coach = cfg.World.CoachDevelopment[key];
        var revenue = (long)(cfg.Economy.Revenue[key] * Draw.Between(seed, "xteam", id, "revenue", 0.85, 1.15));
        var (repMin, repMax) = layer switch { Layer.FictionalPro => (40, 70), Layer.SemiPro => (20, 45), Layer.Academy => (25, 50), _ => (5, 25) };
        var t = new WTeam
        {
            Id = id, OrgId = org, Name = name, Layer = layer, LeagueId = league, Key = key, ParentTeamId = parent, FoundedSeason = season,
            Strategy = strategy, YouthFocus = layer == Layer.Academy ? Draw.Int(seed, "xteam", id, "youth", 70, 95) : Draw.Int(seed, "xteam", id, "youth", 10, 90),
            CoachDevelopment = (int)Math.Round(Draw.Bell(seed, "xteam", id, "coach", coach[0], coach[1])),
            Intensity = IntensityFor(seed, id, strategy), Reputation = Draw.Int(seed, "xteam", id, "reputation", repMin, repMax),
            Revenue = revenue, Funds = (long)(revenue * cfg.Economy.StartingFunds),
            SquadTarget = layer switch
            {
                Layer.FictionalPro => Draw.Int(seed, "xteam", id, "squad", cfg.World.FictionalProSquad[0], cfg.World.FictionalProSquad[1]),
                Layer.Academy => Draw.Int(seed, "xteam", id, "squad", cfg.World.AcademySquad[0], cfg.World.AcademySquad[1]),
                Layer.SemiPro => Draw.Int(seed, "xteam", id, "squad", cfg.World.SemiProSquad[0], cfg.World.SemiProSquad[1]),
                _ => cfg.World.AmateurSquad[0]
            }
        };
        w.Teams[id] = t;
        return t;
    }

    private static Intensity IntensityFor(ulong seed, string id, TeamStrategy s)
    {
        var u = Draw.Unit(seed, "xteam", id, "intensity");
        return s switch
        {
            TeamStrategy.Contend => u < 0.55 ? Intensity.High : Intensity.Normal,
            TeamStrategy.Develop => u < 0.35 ? Intensity.High : Intensity.Normal,
            TeamStrategy.Conserve => u < 0.5 ? Intensity.Low : Intensity.Normal,
            _ => u < 0.2 ? Intensity.High : u < 0.85 ? Intensity.Normal : Intensity.Low
        };
    }

    /// <summary>Real teams' simulation assumptions (coach, strategy, budget: no source) and NPC reserve seats.</summary>
    private static void PrepareRealTeams(World w)
    {
        var cfg = w.Config; var seed = w.Seed;
        foreach (var t in w.Teams.Values.Where(t => t.Real))
        {
            var strategies = cfg.World.Strategies["RealPro"];
            t.Strategy = (TeamStrategy)Draw.Weighted(seed, "xteam", t.Id, "strategy", Enumerable.Range(0, 4).ToArray(), i => strategies[i]);
            var coach = cfg.World.CoachDevelopment[t.Key];
            t.CoachDevelopment = (int)Math.Round(Draw.Bell(seed, "xteam", t.Id, "coach", coach[0], coach[1]));
            t.Intensity = IntensityFor(seed, t.Id, t.Strategy);
            t.YouthFocus = Draw.Int(seed, "xteam", t.Id, "youth", 10, 90);
            var (repMin, repMax) = t.Key switch { "Real:a" => (65, 90), "Real:b" => (50, 70), "Real:c" => (35, 55), _ => (20, 40) };
            t.Reputation = Draw.Int(seed, "xteam", t.Id, "reputation", repMin, repMax);
            t.Revenue = (long)(cfg.Economy.Revenue[t.Key] * Draw.Between(seed, "xteam", t.Id, "revenue", 0.85, 1.15));
            t.Funds = (long)(t.Revenue * cfg.Economy.StartingFunds);
            var room = cfg.World.RealTeamNpcTargetSquad - t.Squad.Count;
            t.RealNpcSeats = room > 0 && Draw.Chance(seed, "xteam", t.Id, "npc-reserves", cfg.World.RealTeamNpcChancePercent / 100.0)
                ? Math.Min(cfg.World.RealTeamNpcMax, room) : 0;
            t.SquadTarget = t.Squad.Count + t.RealNpcSeats;
            // Contracts and wages of real players are unknown: assumed terms, marked as such.
            foreach (var id in t.Squad)
            {
                var p = w.People[id];
                var range = cfg.Market.ContractSeasons["RealPro"];
                var c = new WContract { Id = $"ct:{++w.NextContract:D7}", PersonId = id, TeamId = t.Id, StartSeason = 0,
                    EndSeason = Draw.Int(seed, "xcontract", id, "end", range[0], range[1]), Salary = Salary(w, p, t, 0), Assumed = true };
                w.Contracts[c.Id] = c; p.ContractId = c.Id;
                if (ProLevel(t)) p.EverProfessional = true;
            }
        }
    }

    /// <summary>Largest-remainder quotas of <paramref name="n"/> by weight, in the given order.</summary>
    public static int[] Quotas(int n, IReadOnlyList<int> weights)
    {
        var total = weights.Sum();
        var exact = weights.Select(x => (double)n * x / total).ToArray();
        var q = exact.Select(x => (int)Math.Floor(x)).ToArray();
        foreach (var i in Enumerable.Range(0, q.Length).OrderByDescending(i => exact[i] - q[i]).ThenBy(i => i).Take(n - q.Sum())) q[i]++;
        return q;
    }

    public sealed record BuildResult(int FreeAgentTarget, int EmployedTarget, int AmateurSeats, int AmateurTeams, int FixedSeats,
        int RealNpcSeats, List<string> Notes);

    public static BuildResult Build(World w, Calibration cal)
    {
        var cfg = w.Config; var pop = cfg.Population; var wc = cfg.World; var seed = w.Seed; var notes = new List<string>();
        PrepareRealTeams(w);
        // ---- organizations and teams (fictional)
        for (var l = 1; l <= wc.FictionalProLeagues; l++)
            for (var k = 1; k <= wc.FictionalProTeamsPerLeague; k++)
            {
                var id = $"team:fp:{l:D2}:{k:D2}";
                var name = NpcGenerator.UniqueTeamName(w, id);
                var senior = NewTeam(w, id, $"org:fp:{l:D2}:{k:D2}", name, Layer.FictionalPro, $"league:fp:{l:D2}", 0);
                var academy = NewTeam(w, $"team:ac:{l:D2}:{k:D2}", senior.OrgId, name + " Academy", Layer.Academy, $"league:ac:{l:D2}", 0, senior.Id);
                senior.AcademyTeamId = academy.Id;
            }
        for (var l = 1; l <= wc.SemiProLeagues; l++)
            for (var k = 1; k <= wc.SemiProTeamsPerLeague; k++)
            {
                var id = $"team:sp:{l:D2}:{k:D2}";
                NewTeam(w, id, $"org:sp:{l:D2}:{k:D2}", NpcGenerator.UniqueTeamName(w, id), Layer.SemiPro, $"league:sp:{l:D2}", 0);
            }
        var faTarget = pop.NpcCount * pop.FreeAgentTargetPercent / 100;
        var hardLimit = pop.NpcCount * pop.FreeAgentHardLimitPercent / 100;   // strictly below
        if (faTarget >= hardLimit) throw new InvalidDataException($"Free-agent target {faTarget} is not below {hardLimit}.");
        var employed = pop.NpcCount - faTarget;
        var realSeats = w.Teams.Values.Where(t => t.Real).Sum(t => t.RealNpcSeats);
        var fixedSeats = w.Teams.Values.Where(t => !t.Real).Sum(t => t.SquadTarget) + realSeats;
        var amateurSeats = employed - fixedSeats;
        if (amateurSeats < wc.AmateurSquad[0]) throw new InvalidDataException("World structure leaves no amateur seats.");
        var n = (int)Math.Round(amateurSeats / ((wc.AmateurSquad[0] + wc.AmateurSquad[1]) / 2.0));
        if (wc.AmateurSquad[0] * n > amateurSeats) n = amateurSeats / wc.AmateurSquad[0];
        if (wc.AmateurSquad[1] * n < amateurSeats) n = (amateurSeats + wc.AmateurSquad[1] - 1) / wc.AmateurSquad[1];
        var amateurs = new List<WTeam>();
        for (var i = 0; i < n; i++)
        {
            var id = $"team:am:{i / wc.AmateurCircuitSize + 1:D3}:{i % wc.AmateurCircuitSize + 1:D2}";
            amateurs.Add(NewTeam(w, id, "org:am:" + id[8..], NpcGenerator.UniqueTeamName(w, id), Layer.Amateur, $"league:am:{i / wc.AmateurCircuitSize + 1:D3}", 0));
        }
        var extra = amateurSeats - wc.AmateurSquad[0] * n;
        var order = Generator.Shuffle(seed, "amateur-sizes", amateurs.Select(t => t.Id).ToArray());
        for (var i = 0; extra > 0; i = (i + 1) % order.Length)
        {
            var t = w.Teams[order[i]];
            if (t.SquadTarget < wc.AmateurSquad[1]) { t.SquadTarget++; extra--; }
        }

        // ---- population: exact role and profile quotas, assigned by keyed shuffles
        var roles = Generator.Shuffle(seed, "npc-roles", Enumerable.Range(0, pop.NpcCount).Select(i => Roles.All[i % Roles.All.Length]).ToArray());
        var quotas = Quotas(pop.NpcCount, pop.Profiles.Select(x => x.Weight).ToArray());
        var profiles = Generator.Shuffle(seed, "npc-profiles", pop.Profiles.SelectMany((p, i) => Enumerable.Repeat(p, quotas[i])).ToArray());
        var npcs = new List<WPerson>(pop.NpcCount);
        for (var slot = 1; slot <= pop.NpcCount; slot++)
        {
            var person = NpcGenerator.Create(w, cal, $"{NpcPrefix}c0000:{slot:D6}", roles[slot - 1], profiles[slot - 1], 0);
            w.People[person.Id] = person; npcs.Add(person);
        }
        w.NextPerson = pop.NpcCount;

        // ---- the free-agent cohort: exactly the target, by weighted sampling without replacement on person propensity
        double Propensity(WPerson p)
        {
            var age = w.Age(p); var o = w.Overall(p);
            var a = age <= 17 ? 2.2 : age == 18 ? 1.8 : age == 19 ? 1.3 : age <= 22 ? 0.8 : age <= 26 ? 0.6 : age <= 28 ? 0.9 : 1.6;
            var pr = p.Profile switch { "PROSPECT" => 1.2, "AMATEUR" => 1.0, "SEMI" => 0.9, _ => 0.8 };
            return a * pr * (o >= 78 ? 0.6 : 1.0);
        }
        var fa = npcs.Select(p => (Person: p, Key: -Math.Log(Draw.Unit(seed, "xfa", p.Id, "cohort")) / Propensity(p)))
            .OrderBy(x => x.Key).ThenBy(x => x.Person.Id, StringComparer.Ordinal).Take(faTarget).Select(x => x.Person).ToHashSet();
        foreach (var p in fa) { p.Status = Status.FreeAgent; p.FreeAgentSince = 0; }

        // ---- employment allocation
        var pool = npcs.Where(p => !fa.Contains(p)).ToList();
        Allocate(w, pool);
        if (pool.Count != 0) throw new InvalidOperationException($"{pool.Count} NPCs left without a seat.");

        // ---- backstories, snapshot employment, lineups, academy senior registrations
        foreach (var p in npcs) Backstory(w, p);
        foreach (var p in w.People.Values.Where(p => p.Status == Status.Employed))
            w.Record(p.Id, EventType.SnapshotEmployment, null, p.TeamId, 0, p.Origin == Origin.Real ? "verified real roster at the as-of day" : "employment at world start",
                p.ContractId, Era.Snapshot);
        foreach (var t in w.Teams.Values.Where(t => !t.Real)) t.Lineup = Lineups.Best(w, t);
        foreach (var senior in w.Teams.Values.Where(t => t.Layer == Layer.FictionalPro))
        {
            var academy = w.Teams[senior.AcademyTeamId!];
            foreach (var id in academy.Squad.OrderByDescending(id => w.Overall(w.People[id])).ThenBy(id => id, StringComparer.Ordinal).ToArray())
            {
                if (senior.SeniorRegistered.Count >= wc.AcademySeniorRegistrationMax || RegisteredCount(senior) >= wc.MaxSquad) break;
                if (!Draw.Chance(seed, "xteam", senior.Id + ":" + id, "senior-registration", wc.AcademySeniorRegistrationPercent / 100.0)) continue;
                senior.SeniorRegistered.Add(id); w.People[id].SeniorRegistration = true;
            }
        }
        return new BuildResult(faTarget, employed, amateurSeats, n, fixedSeats, realSeats, notes);
    }

    // ---------- allocation ----------
    public static double Observed(World w, Layer scout, WPerson p, int season, string what, double value)
    {
        var sigma = w.Config.Market.ScoutingNoise[scout.ToString()];
        var z = w.Noise(scout, p, season);
        return what == "potential" ? value + 2 * sigma * z.Potential : value + sigma * z.Overall;
    }

    private static double InitialValue(World w, WTeam t, WPerson p, string role, bool starter)
    {
        var age = w.Age(p);
        var o = Observed(w, t.Layer, p, 0, "overall", w.InRole(p, role));
        var pot = Observed(w, t.Layer, p, 0, "potential", p.Core.Potential);
        if (t.Layer == Layer.Academy) return pot + 1.5 * (21 - age);
        if (t.Real) return pot + (21 - age);
        var youth = age <= 21 ? 1 : age <= 23 ? 0.5 : 0;
        return o + (starter ? w.Config.World.StarterYouthWeight : 1.0) * t.YouthFocus / 100.0 * Math.Max(0, pot - o) * youth;
    }

    private static bool Eligible(WTeam t, WPerson p, int age) => t.Layer switch
    {
        Layer.Academy => age <= 20,
        Layer.RealPro => age <= 22,
        _ => true
    };

    /// <summary>Layer by layer (fictional pro, real-team NPC reserve seats, academy, semi-pro, amateur): first the starters
    /// (one per role, snake order, the strongest budgets first), then reserves to each squad's target. Every team picks its
    /// best-valued affordable NPC (scouted with the layer's noise; a starter is valued mostly on current ability, a reserve
    /// more on potential); the amateur seats absorb exactly the rest.</summary>
    private static void Allocate(World w, List<WPerson> pool)
    {
        var byRole = Roles.All.ToDictionary(r => r, r => pool.Where(p => p.Core.Role == r).ToList(), StringComparer.Ordinal);
        void Take(WTeam t, WPerson p)
        {
            pool.Remove(p); byRole[p.Core.Role].Remove(p);
            Hire(w, t, p, 0, Draw.Int(w.Seed, "xcontract", p.Id, "end", w.Config.Market.ContractSeasons[t.Real ? "RealPro" : t.Key][0],
                w.Config.Market.ContractSeasons[t.Real ? "RealPro" : t.Key][1]), Era.PreWorld);
        }
        WPerson? Best(WTeam t, IEnumerable<WPerson> candidates, string? role)
        {
            var cap = WageCap(w, t); var wages = Wages(w, t);
            WPerson? best = null; double bestValue = double.MinValue; WPerson? cheapest = null; long cheapestSalary = long.MaxValue;
            foreach (var p in candidates)
            {
                var age = w.Age(p);
                if (!Eligible(t, p, age)) continue;
                var salary = Salary(w, p, t, 0);
                if (salary < cheapestSalary || salary == cheapestSalary && string.CompareOrdinal(p.Id, cheapest!.Id) < 0) { cheapest = p; cheapestSalary = salary; }
                if (wages + salary > cap) continue;
                var v = InitialValue(w, t, p, role ?? p.Core.Role, role is not null);
                if (v > bestValue || v == bestValue && string.CompareOrdinal(p.Id, best!.Id) < 0) { best = p; bestValue = v; }
            }
            return best ?? cheapest;
        }
        IEnumerable<WTeam> Ordered(Layer layer) => w.Teams.Values.Where(t => t.Layer == layer && !t.Real)
            .OrderByDescending(t => t.Revenue).ThenBy(t => t.Id, StringComparer.Ordinal);
        void Starters(Layer layer)
        {
            var teams = Ordered(layer).ToArray();
            for (var round = 0; round < Roles.All.Length; round++)
            {
                var role = Roles.All[round];
                foreach (var t in round % 2 == 0 ? teams : teams.Reverse())
                {
                    var pick = Best(t, byRole[role], role);
                    if (pick is null && t.Layer == Layer.Academy) pick = byRole[role].Where(p => w.Age(p) <= 22).OrderBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
                    pick ??= byRole[role].OrderBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (pick is null) throw new InvalidOperationException($"No {role} left for {t.Id}.");
                    Take(t, pick);
                }
            }
        }
        void Reserves(WTeam[] teams)
        {
            for (var round = 0; pool.Count > 0 && teams.Any(t => t.Squad.Count < t.SquadTarget); round++)
                foreach (var t in round % 2 == 0 ? teams : teams.Reverse())
                {
                    if (t.Squad.Count >= t.SquadTarget || pool.Count == 0) continue;
                    var depth = Roles.All.ToDictionary(r => r, r => t.Squad.Count(id => w.People[id].Core.Role == r), StringComparer.Ordinal);
                    var thin = Roles.All.Where(r => depth[r] < 2).ToHashSet(StringComparer.Ordinal);
                    var candidates = thin.Count > 0 && t.Layer != Layer.Amateur ? pool.Where(p => thin.Contains(p.Core.Role)).ToList() : pool;
                    var pick = Best(t, candidates.Count > 0 ? candidates : pool, null) ?? Best(t, pool, null)
                        ?? pool.OrderBy(p => p.Id, StringComparer.Ordinal).First();
                    Take(t, pick);
                }
        }
        Starters(Layer.FictionalPro); Reserves([.. Ordered(Layer.FictionalPro)]);
        Reserves([.. w.Teams.Values.Where(t => t.Real && t.RealNpcSeats > 0).OrderBy(t => t.Id, StringComparer.Ordinal)]);
        foreach (var layer in new[] { Layer.Academy, Layer.SemiPro, Layer.Amateur }) { Starters(layer); Reserves([.. Ordered(layer)]); }
    }

    /// <summary>Signs a person to a team: a new active contract, the squad place and the employment state.</summary>
    public static WContract Hire(World w, WTeam t, WPerson p, int season, int endSeason, Era era)
    {
        var c = new WContract { Id = $"ct:{++w.NextContract:D7}", PersonId = p.Id, TeamId = t.Id, StartSeason = season, EndSeason = endSeason,
            Salary = Salary(w, p, t, season), Assumed = p.Origin == Origin.Real || era == Era.PreWorld };
        w.Contracts[c.Id] = c;
        p.Status = Status.Employed; p.TeamId = t.Id; p.ContractId = c.Id; p.FreeAgentSince = null;
        if (ProLevel(t)) p.EverProfessional = true;
        t.Squad.Add(p.Id);
        return c;
    }

    /// <summary>Ends a person's employment (contract inactive, squad and registrations left); the caller records why.</summary>
    public static void Leave(World w, WPerson p, Status next, int season)
    {
        if (p.TeamId is { } teamId)
        {
            var t = w.Teams[teamId];
            t.Squad.Remove(p.Id);
            foreach (var role in t.Lineup.Where(x => x.Value == p.Id).Select(x => x.Key).ToArray()) t.Lineup[role] = null;
            if (t.Layer == Layer.Academy && t.ParentTeamId is { } parent)
            {
                var senior = w.Teams[parent];
                senior.SeniorRegistered.Remove(p.Id);
                foreach (var role in senior.Lineup.Where(x => x.Value == p.Id).Select(x => x.Key).ToArray()) senior.Lineup[role] = null;
            }
        }
        if (p.ContractId is { } cid) { w.Contracts[cid].Active = false; p.LastSalary = w.Contracts[cid].Salary; }
        p.TeamId = null; p.ContractId = null; p.SeniorRegistration = false; p.Status = next;
        if (next == Status.FreeAgent) p.FreeAgentSince = season;
        if (next == Status.Retired) { p.RetiredSeason = season; p.FreeAgentSince = null; }
    }

    // ---------- backstories (fictional, PreWorld) ----------
    private static WTeam PickTeam(World w, WPerson p, string purpose, Func<WTeam, bool> filter)
    {
        var options = w.Teams.Values.Where(t => !t.Real && filter(t)).OrderBy(t => t.Id, StringComparer.Ordinal).ToArray();
        return options[KeyedRandom.Range(w.Seed, "xstory", p.Id, purpose, options.Length)];
    }

    /// <summary>A generated NPC's fictional history before the world starts, consistent with its age and its state at the
    /// start: a debut age, the seasons with contracts, earlier teams (existing fictional teams), and for a free agent the
    /// exit that left them unattached. The exit is drawn from the person's own plausibility (age, level, ambition,
    /// professionalism), never to fill a category quota; someone who never signed has no exit at all.</summary>
    private static void Backstory(World w, WPerson p)
    {
        var seed = w.Seed; var age = w.Age(p); var o = w.Overall(p);
        var debut = Draw.Int(seed, "xstory", p.Id, "debut-age", 16, 19);
        var experience = Math.Max(0, age - debut);
        if (p.Status == Status.Employed)
        {
            var team = w.Teams[p.TeamId!];
            var contract = w.Contracts[p.ContractId!];
            var years = Math.Min(experience, Draw.Int(seed, "xstory", p.Id, "years-with-team", 0, 3));
            var start = -years;
            if (team.Layer == Layer.Academy)
            {
                if (experience > years)
                {
                    var earlier = PickTeam(w, p, "earlier", t => t.Layer == Layer.Amateur);
                    w.Record(p.Id, EventType.FirstAmateurContract, null, earlier.Id, -experience, "first contract (backstory)", null, Era.PreWorld);
                    w.Record(p.Id, EventType.ContractExpired, earlier.Id, null, start - 1, "contract ended (backstory)", null, Era.PreWorld);
                }
                w.Record(p.Id, EventType.AcademyRecruitment, null, team.Id, start, "recruited to the academy (backstory)", contract.Id, Era.PreWorld);
            }
            else if (experience == years)
                w.Record(p.Id, ProLevel(team) ? EventType.FirstProfessionalContract : EventType.FirstAmateurContract, null, team.Id, start,
                    "first contract (backstory)", contract.Id, Era.PreWorld);
            else
            {
                var earlier = PickTeam(w, p, "earlier", t => LayerOrder(t) >= LayerOrder(team) && t.Layer != Layer.Academy && t.Id != team.Id);
                w.Record(p.Id, ProLevel(earlier) ? EventType.FirstProfessionalContract : EventType.FirstAmateurContract, null, earlier.Id, -experience,
                    "first contract (backstory)", null, Era.PreWorld);
                var firstPro = ProLevel(team) && !ProLevel(earlier);
                w.Record(p.Id, firstPro ? EventType.FirstProfessionalContract : EventType.Transfer, earlier.Id, team.Id, start,
                    firstPro ? "first professional contract (backstory)" : "transfer (backstory)", contract.Id, Era.PreWorld);
            }
            p.ContractSeasons = experience;
            return;
        }
        // A free agent.
        var never = age <= 17 ? 0.9 : age == 18 ? 0.75 : age == 19 ? 0.55 : age == 20 ? 0.35 : age == 21 ? 0.2 : age <= 23 ? 0.1 : 0.04;
        if (experience == 0 || Draw.Chance(seed, "xstory", p.Id, "never-contracted", never))
        {
            w.Record(p.Id, EventType.GeneratedProspect, null, null, 0, "unattached prospect, never contracted", null, Era.PreWorld);
            p.FreeAgentSince = 0; p.ContractSeasons = 0;
            return;
        }
        var u = Draw.Unit(seed, "xstory", p.Id, "last-layer");
        Layer last = o >= 70 ? (u < 0.7 ? Layer.FictionalPro : Layer.SemiPro)
            : o >= 58 ? (u < 0.6 ? Layer.SemiPro : u < 0.9 ? Layer.Amateur : Layer.Academy)
            : (u < 0.75 ? Layer.Amateur : u < 0.9 ? Layer.Academy : Layer.SemiPro);
        if (last == Layer.Academy && age > 23) last = Layer.Amateur;
        var lastTeam = PickTeam(w, p, "last-team", t => t.Layer == last);
        if (last == Layer.FictionalPro) p.EverProfessional = true;
        var idle = Math.Min(experience - 1, Draw.Weighted(seed, "xstory", p.Id, "idle", new[] { (0, 70), (1, 20), (2, 10) }, x => x.Item2).Item1);
        var exitSeason = -idle;
        var tenure = Math.Max(1, Math.Min(experience - idle, Draw.Int(seed, "xstory", p.Id, "tenure", 1, 3)));
        var joined = exitSeason - tenure;
        if (experience - idle > tenure)
        {
            var earlier = PickTeam(w, p, "earlier", t => t.Layer == Layer.Amateur && t.Id != lastTeam.Id);
            w.Record(p.Id, EventType.FirstAmateurContract, null, earlier.Id, -experience, "first contract (backstory)", null, Era.PreWorld);
            w.Record(p.Id, last == Layer.FictionalPro ? EventType.FirstProfessionalContract : last == Layer.Academy ? EventType.AcademyRecruitment : EventType.Transfer,
                earlier.Id, lastTeam.Id, joined, "moved (backstory)", null, Era.PreWorld);
        }
        else
            w.Record(p.Id, last == Layer.FictionalPro ? EventType.FirstProfessionalContract : last == Layer.Academy ? EventType.AcademyRecruitment : EventType.FirstAmateurContract,
                null, lastTeam.Id, joined, "first contract (backstory)", null, Era.PreWorld);
        var weak = o < LowerQuartile(last);
        var reasons = new List<(EventType Type, int Weight)>
        {
            (last == Layer.Academy ? EventType.AcademyNotRetained : EventType.ContractExpired, 50),
            (EventType.Released, weak ? 40 : 25),
            (EventType.VoluntaryDeparture, p.Core.Mind.Ambition >= 70 ? 25 : 10)
        };
        if (p.Core.Mind.Professionalism < 35) reasons.Add((EventType.Dismissed, 15));
        var exit = Draw.Weighted(seed, "xstory", p.Id, "exit", reasons, r => r.Weight).Type;
        w.Record(p.Id, exit, lastTeam.Id, null, exitSeason, exit switch
        {
            EventType.Released => "released (backstory)", EventType.Dismissed => "dismissed for conduct (backstory)",
            EventType.VoluntaryDeparture => "left the team (backstory)", EventType.AcademyNotRetained => "academy did not retain (backstory)",
            _ => "contract expired (backstory)"
        }, null, Era.PreWorld);
        p.FreeAgentSince = exitSeason; p.ContractSeasons = experience - idle;
        static int LowerQuartile(Layer layer) => layer switch { Layer.FictionalPro => 70, Layer.SemiPro => 58, Layer.Academy => 50, _ => 45 };
    }
}
