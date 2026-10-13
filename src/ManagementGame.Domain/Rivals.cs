using System.Collections.Immutable;

namespace ManagementGame.Domain;

/// <summary>Rival organizations with real squads (rules v4 phase 5). A rival's strength is derived from its players; at each
/// season end it develops, renews or lets go of players whose contracts end, may replace its head coach, then recruits:
/// it fills its squad, and a contender or balanced rival seeks a bounded number of upgrades from the open market or from
/// another rival. Every choice is keyed by the rival, the season and the person, so the same seed tells the same world.</summary>
public static class Rivals
{
    public static Rival Get(World w, string id) => w.Rivals.Single(r => r.Id == id);

    /// <summary>The rival's best lineup: one player per role (primary or secondary role), the assignment with the highest
    /// total rating in role; a role nobody can play stays empty. Players are tried in ID order, so ties are stable.</summary>
    public static ImmutableArray<(Person Person, string Role, int Rating)> Lineup(Rival r, Content content)
    {
        var people = r.People.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
        var ratings = people.Select(p => Roles.All.Select(role => Ratings.InRole(p, role, content)).ToArray()).ToArray();
        var best = -1; var choice = new int[Roles.All.Length]; var current = new int[Roles.All.Length]; var used = new bool[people.Length];
        void Search(int slot, int total)
        {
            if (slot == Roles.All.Length) { if (total > best) { best = total; current.CopyTo(choice, 0); } return; }
            var any = false;
            for (var i = 0; i < people.Length; i++)
            {
                if (used[i] || ratings[i][slot] == 0) continue;
                any = true; used[i] = true; current[slot] = i;
                Search(slot + 1, total + ratings[i][slot]);
                used[i] = false;
            }
            if (!any) { current[slot] = -1; Search(slot + 1, total); }
        }
        Search(0, 0);
        return [.. Enumerable.Range(0, Roles.All.Length).Where(k => choice[k] >= 0).Select(k => (people[choice[k]], Roles.All[k], ratings[choice[k]][k]))];
    }

    /// <summary>A rival's match strength: the mean rating in role of its best lineup (an empty role counts zero) plus Edge.</summary>
    public static int Strength(Rival r, Content content) =>
        (int)Numbers.Divide(Lineup(r, content).Sum(x => (long)x.Rating), Roles.All.Length) + content.Rivals.Edge;
    public static long Wages(Rival r) => r.Contracts.Sum(x => x.Salary);
    public static long WageCap(Rival r, RivalRules rules) => rules.WageCapBase + rules.WageCapPerReputation * r.Reputation;
    private static int ContractEnd(ulong seed, string key, int season, Balance b) => Seasons.End(season + KeyedRandom.Range(seed, "rival", key, "seasons", 2), b);

    // ---------- the opening league ----------
    /// <summary>The opening rivals. Each draws a target strength from the generation range (the league's calibrated spread);
    /// its squad comes from the shared player distribution, shifted so the lineup lands near the target less Edge (up to two
    /// corrected redraws when it misses by more than two points). Contracts run one or two seasons. Reputation follows the target;
    /// the strongest contend and the weakest rebuild; posture and tendencies are keyed.</summary>
    public static ImmutableArray<Rival> Create(ulong seed, Content content, ref long next)
    {
        var rules = content.Rivals; var g = content.Generation; var b = content.Balance;
        var names = Generator.Shuffle(seed, "rivals", content.OrganizationNames.Order(StringComparer.Ordinal).ToArray());
        var span = Math.Max(1, g.Rival.Max - g.Rival.Min);
        var rivals = ImmutableArray.CreateBuilder<Rival>(b.RivalCount);
        for (var i = 1; i <= b.RivalCount; i++)
        {
            var id = $"rival:r{i:D2}";
            var target = Generator.Roll(seed, "rival", id, "strength", g.Rival);
            var size = Generator.Weighted(seed, "rival", id, "roster-size", rules.RosterSizes.OrderBy(x => x.Value).ToArray(), x => x.Weight).Value;
            var roles = Roles.All.Concat(Enumerable.Range(0, size - Roles.All.Length)
                .Select(k => Roles.All[KeyedRandom.Range(seed, "rival", id, "extra-role:" + k, Roles.All.Length)])).ToArray();
            var coach = Generator.Coach(seed, content, $"coach:g{++next:D5}");
            Rival Build(int shift, ref long n)
            {
                var start = n;
                var people = roles.Select((role, k) => Generator.Player(seed, content, $"person:g{start + k + 1:D5}", role, 1, null, shift)).ToImmutableArray();
                n = start + roles.Length;
                return new Rival(id, names[i - 1], people, [.. people.Select(p => new RivalContract(p.Id, Pay.Player(p, 1, content), ContractEnd(seed, p.Id, 1, b)))],
                    coach, rules.FundsBase + rules.FundsPerPlace * (b.RivalCount / 2), Math.Clamp(30 + (target - g.Rival.Min) * 40 / span, 5, 95),
                    (Posture)KeyedRandom.Range(seed, "rival", id, "posture", 3),
                    target >= g.Rival.Max - span / 4 ? Strategy.Contend : target <= g.Rival.Min + span / 4 ? Strategy.Rebuild : Strategy.Balanced,
                    Generator.Roll(seed, "rival", id, "aggression", new AttributeRange(20, 90)), Generator.Roll(seed, "rival", id, "youth-focus", new AttributeRange(10, 90)), 0);
            }
            // Up to three draws, each shifted by the last one's miss; the closest to the target stands.
            var shift = target - rules.Edge - 71;
            var rival = Build(shift, ref next); var last = Strength(rival, content);
            for (var attempt = 0; attempt < 2 && Math.Abs(target - Strength(rival, content)) > 2; attempt++)
            {
                shift += target - last;
                var retry = Build(shift, ref next); last = Strength(retry, content);
                if (Math.Abs(target - last) < Math.Abs(target - Strength(rival, content))) rival = retry;
            }
            rivals.Add(rival);
        }
        return rivals.MoveToImmutable();
    }

    // ---------- season end, part one: departures, development, standing, coach ----------
    /// <summary>For each rival at season end, after the open market's own season end: its players develop with the head coach
    /// and playing time (starters as regulars); players whose contracts end retire, are kept (a starter, someone near the
    /// weakest starter, or a young player with room to grow, within the wage cap; a rebuilding rival lets veterans go) or
    /// join the open market; reputation moves with the final place; funds reset by place; strategy follows results and
    /// funds; and a rival that finished poorly for too long replaces its head coach from the coach market.</summary>
    public static World Departures(World w, ulong seed, Content content, int season, int day, ImmutableArray<string> order)
    {
        var rules = content.Rivals; var b = content.Balance; var next = season + 1; var teams = order.Length;
        foreach (var id in w.Rivals.Select(r => r.Id).ToArray())
        {
            var r = Get(w, id);
            var regulars = Lineup(r, content).Select(x => x.Person.Id).ToHashSet(StringComparer.Ordinal);
            r = r with { People = [.. r.People.Select(p => Careers.Develop(p, Careers.Change(seed, p, season, content.Lifecycle, r.Coach.Development, regulars.Contains(p.Id) ? 100 : 30), content))] };
            var lineup = Lineup(r, content);
            var weakest = lineup.IsEmpty ? 0 : lineup.Min(x => x.Rating);
            foreach (var contract in r.Contracts.Where(x => x.EndDay <= Seasons.End(season, b)).OrderBy(x => x.PersonId, StringComparer.Ordinal).ToArray())
            {
                var p = r.People.Single(x => x.Id == contract.PersonId);
                var age = Ratings.Age(p, next); var rating = Ratings.Rating(p, content);
                var cause = $"rival-contract:{id}:{season}";
                if (Careers.Retires(seed, p, age, content, $"{cause}:{p.Id}"))
                {
                    r = r with { People = r.People.Remove(p), Contracts = r.Contracts.Remove(contract) };
                    w = Market.Moved(w, day, cause, p, MoveReason.Retired, id, null);
                    continue;
                }
                var salary = Pay.Player(p, next, content);
                var keep = (lineup.Any(x => x.Person.Id == p.Id) || rating >= weakest - 2 || age <= 21 && p.Potential - rating >= 6)
                    && (r.Strategy != Strategy.Rebuild || age < rules.SellAge) && Wages(r) - contract.Salary + salary <= WageCap(r, rules);
                if (keep) { r = r with { Contracts = r.Contracts.Replace(contract, contract with { Salary = salary, EndDay = ContractEnd(seed, $"{cause}:{p.Id}", next, b) }) }; continue; }
                r = r with { People = r.People.Remove(p), Contracts = r.Contracts.Remove(contract) };
                w = w with { Candidates = w.Candidates.Add(Careers.Listing(p, next, content)) };
                w = Market.Moved(w, day, cause, p, MoveReason.ContractEnded, id, null);
            }
            var place = order.IndexOf(id) + 1;
            var poor = place >= rules.PoorPlace ? r.PoorSeasons + 1 : 0;
            var funds = rules.FundsBase + rules.FundsPerPlace * (teams - place);
            r = r with { Reputation = Math.Clamp(r.Reputation + teams + 1 - 2 * place, 5, 95), Funds = funds, PoorSeasons = poor,
                Posture = (Posture)KeyedRandom.Range(seed, "rival", $"{id}:{season}", "posture", 3),
                Strategy = place <= 2 ? Strategy.Contend : poor >= 2 ? Strategy.Rebuild : funds < rules.FundsBase ? Strategy.Conserve : Strategy.Balanced };
            if (poor >= rules.CoachPatience && w.CoachCandidates.Where(x => x.Fee <= r.Funds).OrderByDescending(x => x.Coach.Preparation + x.Coach.Analysis + x.Coach.Adaptability + x.Coach.Development)
                .ThenBy(x => x.Coach.Id, StringComparer.Ordinal).FirstOrDefault() is { } hire)
            {
                var cause = $"rival-coach:{id}:{season}"; var departed = r.Coach; var salary = Pay.Coach(departed);
                w = w with { CoachCandidates = w.CoachCandidates.Remove(hire).Add(new CoachCandidate(departed, salary * 15, salary, day + b.TalentRefreshInterval - 1)) };
                w = Market.Moved(Market.Moved(w, day, cause, departed, MoveReason.Replaced, id, null), day, cause, hire.Coach, MoveReason.Appointed, null, id);
                r = r with { Coach = hire.Coach, Funds = r.Funds - hire.Fee, PoorSeasons = 0 };
            }
            w = w with { Rivals = w.Rivals.Replace(Get(w, id), r) };
        }
        return w;
    }

    // ---------- season end, part two: recruitment ----------
    /// <summary>Recruitment, after the open market's season end, rivals in order of reputation:
    /// <list type="bullet">
    /// <item>fill: every role covered and the league's registered minimum, from the open market (the best affordable fit, a
    /// young player valued by the rival's youth focus; the cheapest when nothing is affordable);</item>
    /// <item>upgrade: a contender or balanced rival seeks a player at least UpgradeMargin better than its weakest starter, up
    /// to MaxMoves times; BuyPercent of the time (scaled by its aggression) from another rival that can spare the player
    /// (not a starter, or the seller is rebuilding or conserving) for a fee, otherwise from the open market;</item>
    /// <item>a rebuilding rival lets one veteran go who is not needed to cover a role; any squad above the roster limit
    /// releases its weakest player who is not a starter.</item>
    /// </list></summary>
    public static World Recruit(World w, ulong seed, Content content, int season, int day)
    {
        var rules = content.Rivals; var b = content.Balance; var next = season + 1;
        foreach (var id in w.Rivals.OrderByDescending(r => r.Reputation).ThenBy(r => r.Id, StringComparer.Ordinal).Select(r => r.Id).ToArray())
        {
            for (var guard = 0; guard < 2 * b.MaxRoster; guard++)
            {
                var r = Get(w, id);
                // A role the best lineup leaves empty (one player cannot cover two roles).
                var covered = Lineup(r, content).Select(x => x.Role).ToHashSet(StringComparer.Ordinal);
                var missing = Roles.All.FirstOrDefault(role => !covered.Contains(role));
                if (missing is null && r.People.Length >= b.MinimumRoster) break;
                var pool = w.Candidates.Where(x => missing is null || x.Person.Role == missing).ToArray();
                if (pool.Length == 0) break;
                var pick = pool.Where(x => Affordable(r, x.Fee, x.Salary, rules)).OrderByDescending(x => Value(r, x.Person, x.Person.Role, next, content)).ThenBy(x => x.Person.Id, StringComparer.Ordinal).FirstOrDefault()
                    ?? pool.OrderBy(x => x.Fee).ThenBy(x => x.Person.Id, StringComparer.Ordinal).First();
                w = Sign(w, id, pick, seed, content, season, day);
            }
            for (var move = 0; move < rules.MaxMoves; move++)
            {
                var r = Get(w, id);
                if (r.Strategy is not (Strategy.Contend or Strategy.Balanced)) break;
                var lineup = Lineup(r, content);
                if (lineup.Length < Roles.All.Length) break;
                var weak = lineup.OrderBy(x => x.Rating).ThenBy(x => Roles.All.IndexOf(x.Role)).First();
                var need = weak.Rating + rules.UpgradeMargin;
                var market = w.Candidates.Where(x => Ratings.InRole(x.Person, weak.Role, content) >= need && Affordable(r, x.Fee, x.Salary, rules))
                    .OrderByDescending(x => Ratings.InRole(x.Person, weak.Role, content)).ThenBy(x => x.Person.Id, StringComparer.Ordinal).FirstOrDefault();
                var trade = w.Rivals.Where(s => s.Id != id).SelectMany(s => s.People.Select(p => (Seller: s, Person: p)))
                    .Where(x => Ratings.InRole(x.Person, weak.Role, content) >= need && Spare(x.Seller, x.Person, b.MinimumRoster, content)
                        && Affordable(r, Fee(x.Person, next, content), Pay.Player(x.Person, next, content), rules))
                    .OrderByDescending(x => Ratings.InRole(x.Person, weak.Role, content)).ThenBy(x => x.Person.Id, StringComparer.Ordinal).FirstOrDefault();
                var buy = KeyedRandom.Range(seed, "rival", $"{id}:{season}:{move}", "buy", 100) < Math.Min(100, rules.BuyPercent * r.Aggression / 50);
                if (buy && trade.Person is not null) w = Transfer(w, trade.Seller.Id, id, trade.Person, seed, content, season, day);
                else if (market is not null) w = Sign(w, id, market, seed, content, season, day);
                else if (trade.Person is not null) w = Transfer(w, trade.Seller.Id, id, trade.Person, seed, content, season, day);
                else break;
            }
            {
                var r = Get(w, id);
                var starters = Lineup(r, content).Select(x => x.Person.Id).ToHashSet(StringComparer.Ordinal);
                bool Dispensable(Person p) => !starters.Contains(p.Id) && r.People.Length > b.MinimumRoster;
                if (r.Strategy == Strategy.Rebuild && r.People.Where(p => Dispensable(p) && Ratings.Age(p, next) >= rules.SellAge)
                    .OrderByDescending(p => Ratings.Age(p, next)).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault() is { } veteran)
                    w = Release(w, id, veteran, content, season, day);
                while (Get(w, id) is var current && current.People.Length > b.MaxRoster)
                {
                    var keep = Lineup(current, content).Select(x => x.Person.Id).ToHashSet(StringComparer.Ordinal);
                    w = Release(w, id, current.People.Where(p => !keep.Contains(p.Id)).OrderBy(p => Ratings.Rating(p, content)).ThenBy(p => p.Id, StringComparer.Ordinal).First(), content, season, day);
                }
            }
        }
        return w;
    }

    private static bool Affordable(Rival r, long fee, long salary, RivalRules rules) => fee <= r.Funds && Wages(r) + salary <= WageCap(r, rules);
    /// <summary>What a player is worth to a rival filling a role: the rating there, plus a young player's room to grow as far
    /// as the rival favours youth.</summary>
    private static int Value(Rival r, Person p, string role, int season, Content content)
    {
        var rating = Ratings.InRole(p, role, content);
        return rating + (Ratings.Age(p, season) <= 21 && p.Potential > rating ? (p.Potential - rating) * r.YouthFocus / 100 : 0);
    }
    /// <summary>A seller can spare a player who is not in its lineup, or anyone when it is rebuilding or conserving, as long
    /// as it keeps the registered minimum.</summary>
    private static bool Spare(Rival seller, Person p, int minimumRoster, Content content) => seller.People.Length > minimumRoster
        && (seller.Strategy is Strategy.Rebuild or Strategy.Conserve || !Lineup(seller, content).Any(x => x.Person.Id == p.Id));
    /// <summary>The fee between organizations: the market's fee for the player (<see cref="Pay.Fee"/>).</summary>
    public static long Fee(Person p, int season, Content content) => Pay.Fee(p, season, content);

    private static World Sign(World w, string id, Candidate x, ulong seed, Content content, int season, int day)
    {
        var r = Get(w, id); var cause = $"rival-signing:{id}:{season}";
        var signed = r with { People = r.People.Add(x.Person), Funds = Math.Max(0, r.Funds - x.Fee),
            Contracts = r.Contracts.Add(new RivalContract(x.Person.Id, x.Salary, ContractEnd(seed, $"{cause}:{x.Person.Id}", season + 1, content.Balance))) };
        w = w with { Candidates = w.Candidates.Remove(x), Rivals = w.Rivals.Replace(r, signed) };
        return Market.Moved(w, day, cause, x.Person, MoveReason.Signed, null, id);
    }
    private static World Transfer(World w, string from, string to, Person p, ulong seed, Content content, int season, int day)
    {
        var seller = Get(w, from); var fee = Fee(p, season + 1, content); var cause = $"rival-transfer:{to}:{season}";
        w = w with { Rivals = w.Rivals.Replace(seller, seller with { People = seller.People.Remove(p), Contracts = seller.Contracts.RemoveAll(c => c.PersonId == p.Id), Funds = seller.Funds + fee }) };
        var buyer = Get(w, to);
        w = w with { Rivals = w.Rivals.Replace(buyer, buyer with { People = buyer.People.Add(p), Funds = Math.Max(0, buyer.Funds - fee),
            Contracts = buyer.Contracts.Add(new RivalContract(p.Id, Pay.Player(p, season + 1, content), ContractEnd(seed, $"{cause}:{p.Id}", season + 1, content.Balance))) }) };
        return Market.Moved(w, day, cause, p, MoveReason.Transferred, from, to);
    }
    private static World Release(World w, string id, Person p, Content content, int season, int day)
    {
        var r = Get(w, id); var cause = $"rival-release:{id}:{season}";
        w = w with { Rivals = w.Rivals.Replace(r, r with { People = r.People.Remove(p), Contracts = r.Contracts.RemoveAll(c => c.PersonId == p.Id) }),
            Candidates = w.Candidates.Add(Careers.Listing(p, season + 1, content)) };
        return Market.Moved(w, day, cause, p, MoveReason.Released, id, null);
    }
}
