using System.Collections.Immutable;
using ManagementGame.Domain;
using ManagementGame.Formatting;

namespace ManagementGame.Application;

public static class Observations
{
    private static readonly (string Prefix, string Kind)[] Kinds =
    [
        ("notice:offer:", "Offer"), ("notice:negotiation:", "Negotiation"), ("notice:contract:", "Contract"),
        ("notice:renewal:", "Renewal"), ("notice:season:", "Season"), ("notice:roster:", "League"), ("notice:sponsor:", "Commercial"),
        ("notice:league:", "Season"), ("notice:restructuring:", "Finance"),
        ("notice:development:", "Development"), ("notice:conduct:", "Conduct"), ("notice:talent:", "Talent"),
        ("result:", "Match"), ("claim:", "Market"),
        ("lapse:", "Market"), ("meta:", "Meta"), ("commercial:", "Commercial")
    ];
    /// <summary>World-originated messages, newest first; the player's own decisions are not inbox mail.</summary>
    public static ImmutableArray<InboxRow> Inbox(Company c) => Rows(c, _ => "");
    /// <summary>Inbox rows with the stable ID of the entity each message concerns, resolved from records, never from text.</summary>
    public static ImmutableArray<InboxRow> Inbox(Campaign s) => Rows(s.Company, r => Subject(s, r));
    private static ImmutableArray<InboxRow> Rows(Company c, Func<ReviewEntry, string> subject) => c.Reviews
        .Select(r => (Review: r, Kind: Kinds.FirstOrDefault(k => r.Id.StartsWith(k.Prefix, StringComparison.Ordinal)).Kind))
        .Where(x => x.Kind is not null).Reverse().Take(40)
        .Select(x => new InboxRow(x.Review.Id, x.Review.Day, x.Kind!, Currency.Relabel(x.Review.Text), x.Review.CauseId, subject(x.Review))).ToImmutableArray();
    private static string Subject(Campaign s, ReviewEntry r)
    {
        var w = s.World;
        if (w.Results.FirstOrDefault(x => x.Id == r.Id || x.Id == r.CauseId) is { } result) return result.RivalId;
        if (w.Offers.FirstOrDefault(o => o.Id == r.CauseId) is { } offer) return offer.BrandId;
        if (w.Negotiations.FirstOrDefault(n => n.Id == r.CauseId) is { } negotiation) return negotiation.BrandId;
        if (s.Company.Sponsors.FirstOrDefault(x => x.Id == r.CauseId) is { } agreement) return agreement.BrandId;
        if (s.Company.Employment.FirstOrDefault(e => e.Id == r.CauseId) is { } contract) return contract.PersonId;
        // Conduct and talent notices are caused by the person themselves.
        if (s.Company.People.Any(p => p.Id == r.CauseId) || w.Candidates.Any(x => x.Person.Id == r.CauseId)) return r.CauseId;
        // A contract that has ended no longer exists: transfer history names who left with it, and before that history
        // begins, the person it named is on a market under the same stable ID.
        if (w.Moves.FirstOrDefault(m => m.CauseId == r.CauseId && m.Reason is MoveReason.ContractEnded or MoveReason.Retired) is { } ended) return ended.PersonId;
        if (w.Candidates.FirstOrDefault(x => EmployedAs(r.CauseId, x.Person.Id)) is { } player) return player.Person.Id;
        if (w.CoachCandidates.FirstOrDefault(x => EmployedAs(r.CauseId, x.Coach.Id)) is { } coach) return coach.Coach.Id;
        return "";
    }
    /// <summary>Employment IDs are <c>employment:&lt;person ID&gt;</c>, optionally followed by the signing command's cause.</summary>
    private static bool EmployedAs(string employmentId, string personId) =>
        employmentId == "employment:" + personId || employmentId.StartsWith("employment:" + personId + ":", StringComparison.Ordinal);
    /// <summary>Transfer history, newest first: every move as the simulation recorded it when it happened (who, from where,
    /// to where, when and why). Nothing is reconstructed from current rosters or markets, so a later move never rewrites an
    /// earlier one, and a person who has since left every record keeps their name. Moves involve the company, the rivals
    /// (rules v4 phase 5) and the open market; a retirement from the market involves no organization.</summary>
    public static ImmutableArray<MoveRow> Moves(Campaign s) => [.. s.World.Moves.Reverse().Select(m => new MoveRow(m.Id, (MoveKind)m.Reason, m.Day,
        m.PersonId, m.Name, m.Kind == Personnel.HeadCoach, m.Role, m.Origin ?? "", m.Destination ?? "", m.CauseId))];
    private static string Outcome(CompetitiveOutcome r) => r.PlanCause == "forfeit" ? "Forfeit" : r.Won ? "Victory" : "Defeat";
    public static ImmutableArray<FixtureRow> FixtureRows(Campaign s) => s.World.Fixtures.Select(f =>
    {
        var result = s.World.Results.FirstOrDefault(r => r.FixtureId == f.Id);
        return new FixtureRow(f.Id, f.Day, s.World.Rivals.Single(r => r.Id == f.RivalId).Name, f.Importance, result is null ? "" : Outcome(result), f.RivalId,
            f.Stage, f.Games, result is null ? "" : $"{result.GamesWon}–{result.GamesLost}");
    }).ToImmutableArray();
    /// <summary>The league table of the current season (derived from results; ties broken by game difference, games won and
    /// a seeded draw).</summary>
    public static ImmutableArray<StandingRow> StandingRows(Campaign s, Content content)
    {
        var c = s.Company; var b = content.Balance; var season = Seasons.Of(s.World.Calendar.Day, b);
        string Name(string id) => id == c.Id ? c.Name : s.World.Rivals.Single(r => r.Id == id).Name;
        return [.. Seasons.Table(s.World, c.Id, season, s.Execution.Seed).Select((t, i) =>
            new StandingRow(i + 1, t.TeamId, Name(t.TeamId), t.Played, t.Won, t.Lost, t.GamesWon, t.GamesLost, t.Points, t.TeamId == c.Id))];
    }
    /// <summary>A rival-against-rival win is an upset when the winner sat at least this many places below the loser.</summary>
    public const int UpsetGap = 3;
    /// <summary>League stories of the current regular season, newest first, read from results only: after each round day, a
    /// new leader at the top of the table, and each upset between two rivals. Tables use the domain's own ordering, so a
    /// story never contradicts the standings.</summary>
    public static ImmutableArray<LeagueNewsRow> LeagueNews(Campaign s, Content content)
    {
        var c = s.Company; var w = s.World; var b = content.Balance; var season = Seasons.Of(w.Calendar.Day, b); var seed = s.Execution.Seed;
        string Name(string id) => id == c.Id ? c.Name : w.Rivals.Single(r => r.Id == id).Name;
        bool Regular(string fixtureId) => Seasons.StageOf(fixtureId) == Seasons.Regular;
        var own = w.Results.Where(r => r.FixtureId.StartsWith(Seasons.FixturePrefix(season), StringComparison.Ordinal) && Regular(r.FixtureId)).ToArray();
        var league = w.LeagueResults.Where(r => Regular(r.FixtureId)).ToArray();
        var days = own.Select(r => r.Day).Concat(league.Select(r => r.Day)).Distinct().Order().ToArray();
        ImmutableArray<Seasons.Standing> TableBefore(int day) =>
            Seasons.Table(w with { Results = [.. own.Where(r => r.Day < day)], LeagueResults = [.. league.Where(r => r.Day < day)] }, c.Id, season, seed);
        static int Place(ImmutableArray<Seasons.Standing> table, string id) => table.Select((x, i) => (x, i)).First(p => p.x.TeamId == id).i + 1;
        var news = new List<LeagueNewsRow>();
        foreach (var day in days)
        {
            var before = TableBefore(day); var after = TableBefore(day + 1);
            if (before[0].Played > 0 && after[0].TeamId != before[0].TeamId)
                news.Add(new LeagueNewsRow($"league:s{season}:leader:{day}", day, "Leader", $"{Name(after[0].TeamId)} go top of the league",
                    $"After day {day} they lead on {after[0].Points} points; {Name(before[0].TeamId)} drop to {Figures.Ordinal(Place(after, before[0].TeamId))}.", after[0].TeamId));
            foreach (var r in league.Where(r => r.Day == day).OrderBy(r => r.FixtureId, StringComparer.Ordinal))
            {
                var (winner, loser, won, lost) = r.HomeGames > r.AwayGames ? (r.HomeId, r.AwayId, r.HomeGames, r.AwayGames) : (r.AwayId, r.HomeId, r.AwayGames, r.HomeGames);
                var (above, below) = (Place(before, loser), Place(before, winner));
                if (before.Min(x => x.Played) > 0 && below - above >= UpsetGap)
                    news.Add(new LeagueNewsRow($"league:s{season}:upset:{r.FixtureId}", day, "Upset", $"{Name(winner)} upset {Name(loser)} {won}–{lost}",
                        $"{Name(winner)} were {Figures.Ordinal(below)} and {Name(loser)} {Figures.Ordinal(above)} before the match.", winner));
            }
        }
        return [.. news.OrderByDescending(n => n.Day).ThenBy(n => n.Id, StringComparer.Ordinal)];
    }
    /// <summary>Every match of the current season in day order, the company's and the rest of the league's.</summary>
    public static ImmutableArray<LeagueMatchRow> LeagueMatchRows(Campaign s)
    {
        var c = s.Company; var w = s.World;
        string Name(string id) => id == c.Id ? c.Name : w.Rivals.Single(r => r.Id == id).Name;
        var own = w.Fixtures.Select(f => w.Results.FirstOrDefault(r => r.FixtureId == f.Id) is { } r
            ? new LeagueMatchRow(f.Id, f.Day, f.Stage, f.Games, c.Id, c.Name, f.RivalId, Name(f.RivalId), $"{r.GamesWon}–{r.GamesLost}", r.Won ? c.Id : f.RivalId, true)
            : new LeagueMatchRow(f.Id, f.Day, f.Stage, f.Games, c.Id, c.Name, f.RivalId, Name(f.RivalId), "", "", true));
        var others = w.LeagueFixtures.Select(f => w.LeagueResults.FirstOrDefault(r => r.FixtureId == f.Id) is { } r
            ? new LeagueMatchRow(f.Id, f.Day, f.Stage, f.Games, f.HomeId, Name(f.HomeId), f.AwayId, Name(f.AwayId), $"{r.HomeGames}–{r.AwayGames}", r.HomeGames > r.AwayGames ? f.HomeId : f.AwayId, false)
            : new LeagueMatchRow(f.Id, f.Day, f.Stage, f.Games, f.HomeId, Name(f.HomeId), f.AwayId, Name(f.AwayId), "", "", false));
        return [.. own.Concat(others).OrderBy(m => m.Day).ThenByDescending(m => m.Company).ThenBy(m => m.Id, StringComparer.Ordinal)];
    }
    public static ImmutableArray<RivalRow> RivalRows(Campaign s) => s.World.Rivals.OrderBy(r => r.Name, StringComparer.Ordinal).Select(r =>
    {
        var played = s.World.Results.Where(x => x.RivalId == r.Id && s.World.Fixtures.Any(f => f.Id == x.FixtureId)).ToArray();
        return new RivalRow(r.Id, r.Name, played.Count(x => x.Won), played.Count(x => !x.Won),
            s.World.Fixtures.Count(f => f.RivalId == r.Id && !s.World.Results.Any(x => x.FixtureId == f.Id)));
    }).ToImmutableArray();
    public static Situation Build(Campaign s, Content content)
    {
        var c = s.Company; var w = s.World; var b = content.Balance; var day = w.Calendar.Day;
        var fixture = Simulation.NextFixture(s);
        var rival = fixture is null ? null : w.Rivals.Single(r => r.Id == fixture.RivalId);
        // The opponent's strength is an estimate that narrows with the company's information and the head coach's analysis.
        var width = Math.Max(3, (100 - c.Information) / 5 + (100 - c.Coach.Analysis) / 10);
        var estimate = rival is null ? 0 : Rivals.Strength(rival, content) + KeyedRandom.Range(s.Execution.Seed, "information",
            $"player:{rival.Id}:{w.Results.Length}", "strength-estimate", width * 2 + 1) - width;
        var estimatedPosture = rival is null ? Posture.Balanced : (Posture)(((int)rival.Posture +
            (KeyedRandom.Range(s.Execution.Seed, "information", $"player:{rival.Id}:{w.Results.Length}", "tendency", 100) < c.Information ? 0 : 1)) % 3);
        bool Renewable(Employment contract) => contract.EndDay - day <= b.RenewalWindow;
        var season = Seasons.Of(day, b);
        // Estimates: a band around a keyed, stable centre; the true value always lies inside it. For people the company does
        // not employ the band narrows with its information and the head coach's analysis; its own players' potential is
        // read by the head coach alone, more closely.
        var scoutWidth = Math.Max(3, (100 - c.Information) / 5 + (100 - c.Coach.Analysis) / 10);
        var ownWidth = Math.Max(2, (100 - c.Coach.Analysis) / 10);
        string Band(string id, string purpose, int value, int width)
        {
            var centre = value + KeyedRandom.Range(s.Execution.Seed, "scouting", id, purpose, width + 1) - width / 2;
            return $"{Math.Clamp(centre - width, 1, 99)}–{Math.Clamp(centre + width, 1, 99)} (estimate)";
        }
        var people = c.People.OrderBy(p => Roles.All.IndexOf(p.Role)).ThenBy(p => p.Id, StringComparer.Ordinal).Select(p =>
        {
            var contract = c.Employment.Single(x => x.PersonId == p.Id);
            var refuses = Careers.RefusesRenewal(p, content.Lifecycle);
            return new PersonRow(p.Id, p.Handle, p.Role, Ratings.Rating(p, content), p.Readiness, contract.Salary, contract.ReleaseCost,
                c.People.Any(x => x.Id != p.Id && x.Role == p.Role), contract.EndDay, Simulation.RenewalDemand(c, contract, season, content), Renewable(contract) && !refuses,
                p.Name, p.Nationality, Ratings.Age(p, season), p.SecondaryRole, p.SecondaryRole.Length == 0 ? 0 : Ratings.InRole(p, p.SecondaryRole, content),
                new PlayerSkills(p.Skills.Mechanics, p.Skills.EarlyGame, p.Skills.Teamfight, p.Skills.Macro, p.Skills.ChampionPool,
                    p.Mind.Consistency, p.Mind.Composure, p.Mind.Communication), p.Form, p.Morale, p.Popularity, Pay.Fee(p, season, content), contract.Familiarity,
                Band(p.Id, "potential", p.Potential, ownWidth), Careers.Trend(p, season, content.Lifecycle),
                Renewable(contract) && refuses ? "Not open to renewal while morale is this low." : "");
        }).ToImmutableArray();
        var coachContract = c.Employment.Single(x => x.PersonId == c.Coach.Id);
        // Rival profiles: each squad scouted like the free agents, the posture read with the company's information, and the
        // strength as the same band the next opponent shows.
        var profiles = w.Rivals.OrderBy(r => r.Name, StringComparer.Ordinal).Select(r =>
        {
            var centre = Rivals.Strength(r, content) + KeyedRandom.Range(s.Execution.Seed, "information", $"profile:{r.Id}:{w.Results.Length}", "strength-estimate", width * 2 + 1) - width;
            var posture = (Posture)(((int)r.Posture + (KeyedRandom.Range(s.Execution.Seed, "information", $"profile:{r.Id}:{w.Results.Length}", "tendency", 100) < c.Information ? 0 : 1)) % 3);
            return new RivalProfile(r.Id, r.Name, r.Coach.Name, posture.ToString(), $"{Math.Max(0, centre - width)}–{Math.Min(100, centre + width)} (estimate)",
                [.. r.People.OrderBy(p => Roles.All.IndexOf(p.Role)).ThenBy(p => p.Id, StringComparer.Ordinal).Select(p => new RivalPlayerRow(p.Id, p.Handle, p.Role,
                    Ratings.Age(p, season), Band(p.Id, "rating", Ratings.Rating(p, content), scoutWidth), p.Popularity))], r.Coach.Id);
        }).ToImmutableArray();
        var active = Market.ActiveSponsors(c, day);
        string Availability(Offer o) => o.ClaimedBy is not null ? o.ClaimedBy == c.Id ? "Signed" : "Claimed by rival"
            : o.Deadline < day ? "Expired" : c.Reputation < o.MinimumReputation ? "Reputation too low"
            : c.Sponsors.Any(x => x.BrandId == o.BrandId && x.EndDay >= day) ? "Brand already a partner"
            : active >= b.SponsorSlots ? "No free sponsor slot" : "Available";
        var brands = content.Brands.OrderBy(x => x.MinimumReputation).ThenBy(x => x.Id, StringComparer.Ordinal).Select(x =>
        {
            var fair = Market.FairPayment(x, c.Reputation, c.Audience, Careers.StarBonus(c, content.Lifecycle));
            var status = !Market.Approachable(x, c) ? $"Out of reach: needs reputation {x.MinimumReputation}, audience {x.MinimumAudience:N0}"
                : Market.Blocked(x, c, w, day) ?? (Market.Qualifies(x, c) ? "Open to proposals" : "Open, but values you below its tier");
            return new BrandRow(x.Id, x.Name, x.Sector, x.MinimumReputation, x.MinimumAudience, status,
                Numbers.Divide(fair * 90, 100), Numbers.Divide(fair * 110, 100), x.DurationDays.Order().ToImmutableArray());
        }).ToImmutableArray();
        var view = new Situation(s.Execution.Revision, day, Seasons.Of(day, b), Seasons.DayOf(day, b), b.SeasonLength, c.Name, c.Recovery.ToString(),
            c.Cash, Finance.Forecast(c, day, b.OperatingCost), c.Reputation, c.Audience, Simulation.Load(c, b, day), c.Coach.Capacity,
            c.Coach.Name, (Delegation)c.Authority.Mode, (Risk)c.Authority.RiskCeiling, fixture?.Id ?? "", fixture?.Day ?? 0,
            rival?.Name ?? "No match until next season", rival is null ? "—" : $"{Math.Max(0, estimate - width)}–{Math.Min(100, estimate + width)} (estimate)",
            estimatedPosture.ToString(), c.Information >= 65 ? "Moderate confidence" : "Low confidence", w.Meta.ToString(),
            c.Plan is null ? null : new PlanView(c.Plan.FixtureId, c.Plan.Execution, c.Plan.Opponent, c.Plan.Meta, (Risk)c.Plan.Posture, c.Plan.Lineup),
            null, "", people,
            w.Candidates.Select(x => new CandidateRow(x.Person.Id, x.Person.Handle, x.Person.Role,
                Band(x.Person.Id, "rating", Ratings.Rating(x.Person, content), scoutWidth), x.Fee, x.Salary, x.Person.Name, x.Person.Nationality,
                Ratings.Age(x.Person, season), Band(x.Person.Id, "potential", x.Person.Potential, scoutWidth * 3 / 2), x.Person.Popularity, x.Person.SecondaryRole)).ToImmutableArray(),
            w.Offers.Select(o => new SponsorRow(o.Id, o.Name, o.Payment, o.WinBonus, o.Load, o.Deadline, o.MinimumReputation, Availability(o), o.Origin.ToString(), o.DurationDays, o.BrandId)).ToImmutableArray(),
            c.Sponsors.Where(x => x.EndDay >= day).Select(x => $"{x.Name}: {Currency.Format(x.Payment)} weekly, delivery load {x.Load}, win bonus {Currency.Format(x.WinBonus)}, ends day {x.EndDay}").ToImmutableArray(),
            c.FinancialItems.Where(x => x.Remaining > 0).OrderBy(x => x.DueDay).ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => new BillRow(x.Id, x.CauseId, x.DueDay, x.Remaining, x.Incoming, x.MissedDay)).ToImmutableArray(),
            w.Results.Select(r => new ResultRow(r.Id, r.Day, w.Rivals.Single(x => x.Id == r.RivalId).Name, r.PlanCause == "forfeit" ? "Forfeit" : r.Won ? "Victory" : "Defeat", r.Posture.ToString())).ToImmutableArray(),
            c.Reviews.TakeLast(24).Reverse().Select(r => $"Day {r.Day} · {Currency.Relabel(r.Text)} [cause: {r.CauseId}]").ToImmutableArray(),
            string.Join("; ", c.PendingEffects.Select(e => $"Day {e.DueDay}: reputation {e.Reputation:+#;-#;0}, audience {e.Audience:+#;-#;0}")), content.Label,
            new StaffRow(c.Coach.Id, c.Coach.Name, coachContract.Salary, coachContract.EndDay, Simulation.RenewalDemand(c, coachContract, season, content), Renewable(coachContract)),
            w.CoachCandidates.Where(x => x.Deadline >= day).Select(x => new CoachCandidateRow(x.Coach.Id, x.Coach.Name,
                Band(x.Coach.Id, "skill", (x.Coach.Preparation + x.Coach.Analysis + x.Coach.Adaptability + x.Coach.Development) / 4, scoutWidth), x.Fee, x.Salary, x.Deadline)).ToImmutableArray(),
            brands, w.Negotiations.Select(n => new NegotiationRow(n.Id, Market.Brand(content, n.BrandId).Name, n.Payment, n.DurationDays, n.ResponseDay, n.BrandId)).ToImmutableArray(),
            b.SponsorSlots, active, Inbox(s), FixtureRows(s), RivalRows(s), c.Id, rival?.Id ?? "", b.OperatingCost, b.MinimumRoster, Moves(s),
            StandingRows(s, content), LeagueMatchRows(s), b.PlayoffTeams, w.Finishes.LastOrDefault() is { } last ? last.Order.IndexOf(c.Id) + 1 : 0,
            LeagueNews(s, content), new LeagueInfo(b.RivalCount + 1, Seasons.Rounds(b), b.RegularGames, b.PlayoffTeams, b.PlayoffGames,
                Seasons.RoundDay(season, 0, b), Seasons.LastRegularDay(season, b), Seasons.SemifinalDay(season, b), Seasons.FinalDay(season, b), b.PlacePrizes), profiles,
            c.Authority.RecruitFee, c.Authority.RecruitWage, b.MaxRoster);
        var recommendation = CoachPolicy.Recommend(view);
        return view with { Recommendation = recommendation, RecommendationReason = recommendation is null
            ? view.NextFixtureId.Length == 0 ? "No match to prepare until next season." : "No legal lineup: sign players to cover all five roles."
            : $"Coach uses observed readiness, opponent estimates and the public meta. Suggested {recommendation.Execution}/{recommendation.Opponent}/{recommendation.Meta}% allocation; {view.Confidence.ToLowerInvariant()}. Hidden opponent preparation and variance are unknown." };
    }
}

// The policy can only receive the same actor-safe observations presented to the player.
public static class CoachPolicy
{
    public static PlanView? Recommend(Situation view)
    {
        if (view.NextFixtureId.Length == 0) return null;
        var lineup = BestLineup(view.People);
        if (lineup.IsDefault) return null;
        var meta = view.Meta == "Aggressive";
        var posture = meta && view.Ceiling == Risk.Aggressive ? Risk.Aggressive : view.Ceiling == Risk.Conservative ? Risk.Conservative : Risk.Balanced;
        return new PlanView(view.NextFixtureId, meta ? 25 : 50, meta ? 25 : 30, meta ? 50 : 20, posture, lineup);
    }
    /// <summary>The five available players, one per role in role order, with the highest total of rating in role times
    /// readiness (a secondary role counts at its own rating). Default when no legal lineup exists. Ties go to the earlier
    /// candidate in roster order, so the choice is deterministic.</summary>
    public static ImmutableArray<string> BestLineup(ImmutableArray<PersonRow> people)
    {
        var available = people.Where(p => p.Readiness > 0).ToArray();
        static int Value(PersonRow p, string role) => role == p.Role ? p.Rating * p.Readiness : role == p.SecondaryRole && role.Length > 0 ? p.SecondaryRating * p.Readiness : -1;
        var best = -1L; string[]? chosen = null; var current = new string[Roles.All.Length];
        void Search(int role, long total, HashSet<string> used)
        {
            if (role == Roles.All.Length) { if (total > best) { best = total; chosen = [.. current]; } return; }
            foreach (var p in available)
            {
                var value = Value(p, Roles.All[role]);
                if (value < 0 || used.Contains(p.Id)) continue;
                used.Add(p.Id); current[role] = p.Id;
                Search(role + 1, total + value, used);
                used.Remove(p.Id);
            }
        }
        Search(0, 0, []);
        return chosen is null ? default : [.. chosen];
    }
}
