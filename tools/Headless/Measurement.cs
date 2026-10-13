using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Formatting;
using ManagementGame.Infrastructure;

/// <summary>Rules v3 measurement ("Cách đo đỡ chán"): owner policies play several seeds for several seasons through the
/// same Session API a player uses (observations in, typed decisions out), and the report states the anti-boredom metrics
/// and the gates each wave must pass. It reads authoritative state only to label seasons; every decision comes from the
/// observation. Values are development measurements, not balance claims.</summary>
public static class Measurement
{
    public const string Coach = "coach", Greedy = "sponsor-greedy", Thrift = "thrift", Balanced = "balanced";
    public static readonly string[] Policies = [Coach, Greedy, Thrift, Balanced];

    /// <summary>One closed season of one run. The match columns are diagnostics from the recorded outcome components
    /// (forfeits excluded): mean series win probability, mean base strength and completed preparation of the fielded lineup,
    /// and the mean strength of the rivals faced (as they stand at the season's end).</summary>
    /// <remarks>Rules v4 adds the league: the company's final <c>Place</c>; the open market's thinnest role at season end
    /// (<c>PoolMinPerRole</c>); the rivals' newcomers at season end (signings and transfers) against their squad sizes; and
    /// the spread of rival strength (strongest minus weakest) at season end.</remarks>
    public sealed record SeasonRow(int Season, int Wins, int Losses, int Forfeits, long CashEnd, int ReputationStart, int ReputationEnd,
        int AudienceEnd, int RosterEnd, int ActiveSponsorsEnd, int NegativeForecastWeeks, int DecisionsMade, int Prompts, int LongestQuiet,
        double MeanWinProbability, double MeanBase, double MeanPreparation, double MeanRivalStrength,
        int Place, int PoolMinPerRole, int RivalNewcomers, int RivalPlayers, int RivalSpread);
    /// <summary>A run that cannot advance (distress with no cash and no inflow) is insolvent: its final cash stands in for the
    /// seasons it did not reach.</summary>
    public sealed record Run(string Policy, ulong Seed, bool Blocked, string BlockedReason, int Days, long FinalCash, ImmutableArray<SeasonRow> Seasons);
    /// <summary>A metric of the proposal's measurement table: the roadmap gate it opens (1–3), or 0 for a
    /// reference metric no gate uses (<c>OpensGate</c>); <c>Wave</c> is the wave whose mechanisms make it measurable.</summary>
    public sealed record Gate(string Metric, string Measure, string Threshold, double? Value, string Detail, bool? Pass, int OpensGate, int Wave, string Problems);
    public sealed record Report(string Rules, string ContentHash, ImmutableArray<ulong> Seeds, int Seasons, ImmutableArray<Run> Runs,
        ImmutableArray<Gate> Gates, ImmutableArray<string> Notes);

    public static Report Measure(LoadedContent content, IReadOnlyList<ulong> seeds, int seasons)
    {
        var runs = new List<Run>();
        foreach (var seed in seeds)
            foreach (var policy in Policies)
                runs.Add(Play(content, seed, policy, seasons));
        return new Report(content.Definition.RulesVersion, content.Hash, [.. seeds], seasons, [.. runs], Gates(runs, seeds, seasons, content.Definition.Balance.SeasonLength),
            ["Development measurement: four scripted owner policies, not player data. Thresholds are the proposal's starting values.",
             "Gates for later waves are reported for reference; a gate whose mechanism does not exist yet is not measurable."]);
    }

    /// <summary>One campaign under one policy until the given number of seasons has closed (or the campaign blocks).</summary>
    public static Run Play(LoadedContent content, ulong seed, string policy, int seasons)
    {
        var session = new Session(content.Definition, CampaignFactory.Create(content.Definition, content.Hash, seed, "campaign:measure"));
        long id = 0;
        var made = 0;
        bool Send(Decision decision)
        {
            var response = session.Submit(new Request("m:" + ++id, session.Observe().Revision, decision));
            if (response.Accepted && decision is not AdvanceDecision) made++;
            return response.Accepted;
        }
        var rows = new List<SeasonRow>();
        var length = content.Definition.Balance.SeasonLength;
        var start = session.Observe();
        int season = 1, repStart = start.Reputation, madeAtStart = 0;
        var negativeWeeks = new HashSet<int>(); var eventDays = new SortedSet<int>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        var prompts = 0;
        void Watch(Situation v)
        {
            if (v.Forecast < 0) negativeWeeks.Add((v.Day - 1) / 7);
            foreach (var row in v.Inbox.Where(r => seen.Add(r.Id)))
            {
                eventDays.Add(row.Day);
                if (row.Kind is "Offer" or "Negotiation" or "Renewal" or "Contract" or "Roster" or "Commercial" && !row.Id.StartsWith("commercial:", StringComparison.Ordinal)) prompts++;
            }
            foreach (var f in v.Fixtures) eventDays.Add(f.Day);
        }
        if (policy == Coach) Send(new CoachDecision(Delegation.Autonomous, Risk.Balanced));
        else Send(new CoachDecision(Delegation.Recommend, policy == Balanced ? Risk.Aggressive : Risk.Balanced));
        Watch(start);
        string blocked = "";
        for (var guard = 0; guard < 400 * seasons && blocked.Length == 0; guard++)
        {
            var v = session.Observe();
            Act(policy, v, Send);
            v = session.Observe();
            // A manual owner commits the next match's plan as soon as the fixture is known: preparation only accumulates
            // while a plan is committed, so a plan made on match day carries no completed work.
            if (v.NextFixtureId.Length > 0 && v.CommittedPlan is null && v.Delegation != Delegation.Autonomous && v.Recommendation is { } plan)
                Send(Plan(policy, v, plan));
            if (!Send(new AdvanceDecision()))
            {
                // Distress: take any credible inflow, then retry once; a campaign that still cannot move is reported, not hidden.
                var stuck = session.Observe();
                foreach (var offer in stuck.Offers.Where(o => o.Availability == "Available")) Send(new SponsorDecision(offer.Id));
                if (stuck.NextMatchDay == stuck.Day && stuck.CommittedPlan is null && stuck.Recommendation is { } rescue) Send(Plan(policy, stuck, rescue));
                if (!Send(new AdvanceDecision())) blocked = session.Submit(new Request("m:probe:" + id, session.Observe().Revision, new AdvanceDecision())).Message;
            }
            var now = session.Observe();
            Watch(now);
            if (now.Season == season) continue;
            var state = session.Capture();
            var results = state.World.Results.Where(r => r.FixtureId.StartsWith(Seasons.FixturePrefix(season), StringComparison.Ordinal)).ToArray();
            var first = (season - 1) * length + 1; var last = season * length;
            var days = eventDays.Where(d => d >= first && d <= last).ToArray();
            var quiet = days.Length == 0 ? length : new[] { days[0] - first }.Concat(days.Zip(days.Skip(1), (a, b) => b - a - 1)).Append(last - days[^1]).Max();
            var fielded = results.Where(r => r.PlanCause != "forfeit").ToArray();
            double Mean(Func<CompetitiveOutcome, double> pick) => fielded.Length == 0 ? 0 : Math.Round(fielded.Average(pick), 1);
            rows.Add(new SeasonRow(season, results.Count(r => r.Won), results.Count(r => !r.Won), results.Count(r => r.PlanCause == "forfeit"), now.Cash,
                repStart, now.Reputation, now.Audience, now.People.Length, now.ActiveSponsors,
                negativeWeeks.Count(w => w * 7 + 1 >= first && w * 7 + 1 <= last), made - madeAtStart, prompts, quiet,
                Mean(r => r.Probability / 10_000.0), Mean(r => r.Base), Mean(r => r.Preparation),
                Mean(r => Rivals.Strength(state.World.Rivals.Single(x => x.Id == r.RivalId), content.Definition)),
                state.World.Finishes.FirstOrDefault(f => f.Season == season)?.Order.IndexOf(state.Company.Id) + 1 ?? 0,
                Roles.All.Min(role => state.World.Candidates.Count(x => x.Person.Role == role)),
                state.World.Moves.Count(m => m.Day == last && m.Reason is MoveReason.Signed or MoveReason.Transferred && m.Destination is not null && m.Destination != state.Company.Id),
                state.World.Rivals.Sum(r => r.People.Length),
                state.World.Rivals.Max(r => Rivals.Strength(r, content.Definition)) - state.World.Rivals.Min(r => Rivals.Strength(r, content.Definition))));
            prompts = 0; madeAtStart = made; repStart = now.Reputation; season = now.Season;
            if (season > seasons) break;
        }
        return new Run(policy, seed, blocked.Length > 0, blocked, session.Observe().Day, session.Observe().Cash, [.. rows]);
    }

    /// <summary>Owner choices between matches, from the observation only.</summary>
    private static void Act(string policy, Situation v, Func<Decision, bool> send)
    {
        var available = v.Offers.Where(o => o.Availability == "Available").OrderByDescending(o => o.Payment).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        switch (policy)
        {
            case Greedy:
                // Every sponsor, whatever the delivery load; an open slot is chased at the top of the brand's guide.
                foreach (var offer in available) send(new SponsorDecision(offer.Id));
                if (v.ActiveSponsors < v.SponsorSlots && v.Negotiations.IsEmpty && OpenBrand(v) is { } greedyBrand)
                    send(new ProposalDecision(greedyBrand.Id, greedyBrand.GuideHigh, greedyBrand.DurationDays[^1]));
                break;
            case Balanced:
                // A sponsor only while the delivery load stays near what the coach can carry; an open slot is filled at a fair price.
                foreach (var offer in available.Where(o => v.Load + o.Load <= v.Capacity + 20 || v.ActiveSponsors == 0).Take(1)) send(new SponsorDecision(offer.Id));
                if (v.ActiveSponsors < v.SponsorSlots && available.Length == 0 && v.Negotiations.IsEmpty && OpenBrand(v) is { } brand)
                    send(new ProposalDecision(brand.Id, (brand.GuideLow + brand.GuideHigh) / 2, brand.DurationDays[0]));
                break;
            default:
                foreach (var offer in available.Take(1)) send(new SponsorDecision(offer.Id));
                break;
        }
        var people = v.People;
        // Every policy covers a role the roster lacks (a forfeit helps no owner); the thrifty owner picks the cheapest.
        foreach (var role in Roles.All.Where(r => !people.Any(p => p.Role == r)))
        {
            var options = v.Candidates.Where(c => c.Role == role && c.Fee <= v.Cash);
            var pick = policy == Thrift ? options.OrderBy(c => c.Fee + c.Salary * 28).FirstOrDefault() : options.OrderByDescending(c => Midpoint(c.RatingEstimate)).FirstOrDefault();
            if (pick is not null) send(new SigningDecision(pick.Id));
        }
        switch (policy)
        {
            case Thrift:
                // Never renews; releases a second player in a role when the exit cost is below the wages it saves.
                foreach (var spare in people.Where(p => p.CanRelease && (long)(p.ContractEnd - v.Day) * p.Salary > p.ExitCost && p.ExitCost <= v.Cash)
                    .GroupBy(p => p.Role).Select(g => g.OrderBy(p => p.Rating).First()).Take(1))
                    send(new ReleaseDecision(spare.Id));
                break;
            case Balanced:
                // Rules v4: renew by value (a good player not yet in decline, or one the squad cannot do without).
                foreach (var person in people.Where(p => p.CanRenew && (p.Rating >= 66 && p.Age <= 28 || people.Count(x => x.Role == p.Role) == 1 || people.Length <= v.MinimumRosterOrDefault())))
                    send(new RenewalDecision(person.Id, 1));
                if (v.CoachContract.CanRenew) send(new RenewalDecision(v.CoachContract.Id, 1));
                // Keep the registered roster at the league minimum (six when no rule says otherwise) with the best affordable player.
                if (people.Length < v.MinimumRosterOrDefault() && v.Candidates.Where(c => c.Fee <= v.Forecast).OrderByDescending(c => Midpoint(c.RatingEstimate)).FirstOrDefault() is { } depth)
                    send(new SigningDecision(depth.Id));
                // Upgrade by estimated rating: a free agent at least four points better (estimate midpoint) than the weakest player
                // in their role, affordable from under half the cash, while the roster has room; then let the displaced player go
                // when the exit is affordable and the squad stays above the minimum.
                if (people.Length < v.MaxRosterOrDefault() && people.Length > 0)
                {
                    var upgrade = v.Candidates.Where(c => c.Fee * 2 <= v.Cash && people.Any(p => p.Role == c.Role && Midpoint(c.RatingEstimate) >= p.Rating + 4))
                        .OrderByDescending(c => Midpoint(c.RatingEstimate) - people.Where(p => p.Role == c.Role).Min(p => p.Rating)).ThenBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (upgrade is not null && send(new SigningDecision(upgrade.Id)))
                    {
                        var displaced = people.Where(p => p.Role == upgrade.Role && p.CanRelease).OrderBy(p => p.Rating).ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault();
                        if (displaced is not null && people.Length + 1 > v.MinimumRosterOrDefault() && displaced.ExitCost * 4 <= v.Cash) send(new ReleaseDecision(displaced.Id));
                    }
                }
                break;
            default:
                foreach (var person in people.Where(p => p.CanRenew && (policy == Coach || p.Rating >= 66))) send(new RenewalDecision(person.Id, 1));
                if (v.CoachContract.CanRenew) send(new RenewalDecision(v.CoachContract.Id, 1));
                break;
        }
    }

    /// <summary>The coach's recommended plan; the balanced owner also matches its posture to the public meta.</summary>
    private static PreparationDecision Plan(string policy, Situation v, PlanView plan)
    {
        var posture = policy == Balanced ? v.Meta switch { "Aggressive" => Risk.Aggressive, "Conservative" => Risk.Conservative, _ => Risk.Balanced } : plan.Posture;
        return new PreparationDecision(plan.FixtureId, plan.Execution, plan.Opponent, plan.Meta, posture, plan.Lineup);
    }

    private static BrandRow? OpenBrand(Situation v) => v.Brands.Where(b => b.Availability == "Open to proposals")
        .OrderByDescending(b => b.GuideHigh).ThenBy(b => b.Id, StringComparer.Ordinal).FirstOrDefault();

    private static int MinimumRosterOrDefault(this Situation v) => v.MinimumRoster > 0 ? v.MinimumRoster : 6;
    private static int MaxRosterOrDefault(this Situation v) => 8;

    private static double Midpoint(string band)
    {
        var digits = band.Split(['–', ' '], StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0).ToArray();
        return digits.Length == 2 ? (digits[0] + digits[1]) / 2.0 : 0;
    }

    private static ImmutableArray<Gate> Gates(List<Run> runs, IReadOnlyList<ulong> seeds, int seasons, int seasonLength)
    {
        Run Of(string policy, ulong seed) => runs.Single(r => r.Policy == policy && r.Seed == seed);
        // Season-N cash; an insolvent run keeps the cash it stopped with.
        long Final(Run r) => r.Seasons.FirstOrDefault(s => s.Season == seasons)?.CashEnd ?? r.FinalCash;
        string Label(Run r) => r.Blocked ? $"{Currency.Whole(r.FinalCash)} insolvent day {r.Days}" : Currency.Whole(Final(r));
        var gates = new List<Gate>();
        // 1. Losing is not profitable: the balanced owner ends season N with at least the thrifty owner's cash.
        var ahead = seeds.Count(s => !Of(Balanced, s).Blocked && Final(Of(Balanced, s)) >= Final(Of(Thrift, s)));
        var required = (int)Math.Ceiling(seeds.Count * 6 / 8.0);
        gates.Add(new("Losing is not profitable", $"Season-{seasons} cash, balanced vs thrift, per seed (an insolvent run keeps its last cash)",
            $"balanced >= thrift on >= {required}/{seeds.Count} seeds", ahead,
            $"{ahead}/{seeds.Count} seeds ({string.Join("; ", seeds.Select(s => $"{s}: {Label(Of(Balanced, s))} vs {Label(Of(Thrift, s))}"))})",
            ahead >= required, 1, 1, "1"));
        // 7. Reputation follows results: correlation of season win rate with the season's reputation change.
        var samples = runs.SelectMany(r => r.Seasons).Where(s => s.Wins + s.Losses > 0).Select(s => ((double)s.Wins / (s.Wins + s.Losses), (double)(s.ReputationEnd - s.ReputationStart))).ToArray();
        var correlation = Pearson(samples);
        gates.Add(new("Reputation follows results (raw)", "Pearson r of season win rate vs season reputation change, all policies and seeds", ">= 0.6 (reference)",
            Math.Round(correlation, 3), $"{samples.Length} seasons; diluted by convergence (see the partial measure)", correlation >= .6, 0, 1, "7"));
        // The same, holding the season's starting reputation fixed (partial correlation): reputation converges on what a
        // season earns, so a strong team's change is small even in a strong season, which dilutes the raw figure above.
        var withStart = runs.SelectMany(r => r.Seasons).Where(s => s.Wins + s.Losses > 0).ToArray();
        double R(Func<SeasonRow, double> x, Func<SeasonRow, double> y) => Pearson([.. withStart.Select(s => (x(s), y(s)))]);
        double Win(SeasonRow s) => (double)s.Wins / (s.Wins + s.Losses);
        double Change(SeasonRow s) => s.ReputationEnd - s.ReputationStart;
        double Start(SeasonRow s) => s.ReputationStart;
        var (rxy, rxz, ryz) = (R(Win, Change), R(Win, Start), R(Change, Start));
        var partial = (rxy - rxz * ryz) / Math.Sqrt(Math.Max(1e-9, (1 - rxz * rxz) * (1 - ryz * ryz)));
        // Gate 1 reads this measure since the user's decision (2026-10-13); the Director should confirm the change of gate.
        gates.Add(new("Reputation follows results", "Partial correlation of season win rate with the season's reputation change, holding the starting reputation fixed",
            ">= 0.6", Math.Round(partial, 3), $"level: r(win rate, season-end reputation) {Math.Round(R(Win, s => s.ReputationEnd), 3)}", partial >= .6, 1, 1, "7"));
        // Reference metrics for later waves.
        double WinRate(Run r) => r.Seasons.Sum(s => s.Wins) / (double)Math.Max(1, r.Seasons.Sum(s => s.Wins + s.Losses));
        var playerValue = seeds.Average(s => WinRate(Of(Balanced, s)) - WinRate(Of(Coach, s))) * 100;
        gates.Add(new("Player value", "Win rate, balanced minus full coach delegation", "+5 to +15 percentage points", Math.Round(playerValue, 1),
            "percentage points, mean over seeds", playerValue is >= 5 and <= 15, 0, 3, "4"));
        var allSeasons = runs.SelectMany(r => r.Seasons).ToArray();
        var density = allSeasons.Length == 0 ? 0 : allSeasons.Average(s => s.DecisionsMade * 7.0 / seasonLength);
        gates.Add(new("Decision density", "Consequential decisions made per 7 days (offers, renewals, signings, releases, plans, proposals)", "mean >= 2; no off-season week at 0",
            Math.Round(density, 2), "per 7 days, mean over seasons; off-season weeks are not separated yet", density >= 2, 3, 3, "8"));
        var quiet = allSeasons.Length == 0 ? 0 : allSeasons.Max(s => s.LongestQuiet);
        gates.Add(new("Empty stretches", "Longest run of days without any message or match", "<= 10 days", quiet,
            $"worst season; mean {(allSeasons.Length == 0 ? 0 : allSeasons.Average(s => s.LongestQuiet)):0.0}", quiet <= 10, 2, 2, "8"));
        var pressured = allSeasons.Length == 0 ? 0 : allSeasons.Count(s => s.NegativeForecastWeeks > 0) * 100.0 / allSeasons.Length;
        gates.Add(new("Financial pressure", "Share of seasons with at least one week of negative 7-day forecast", "20-60%", Math.Round(pressured, 1),
            $"percent of {allSeasons.Length} seasons played; {runs.Count(r => r.Blocked)} of {runs.Count} runs insolvent", pressured is >= 20 and <= 60, 0, 1, "1, 5"));
        // 2 and 3. Multi-year progress and outcome diversity read the final league places (rules v4 league).
        double MeanPlace(int season) => seeds.Select(s => Of(Balanced, s).Seasons.FirstOrDefault(x => x.Season == season)?.Place ?? 0).Where(p => p > 0).DefaultIfEmpty(0).Average();
        var places = Enumerable.Range(1, seasons).Select(MeanPlace).ToArray();
        var progress = places[0] - places[^1];
        var ceiling = places.Take(Math.Min(3, seasons)).Average();
        gates.Add(new("Multi-year progress", $"Mean final league place of the balanced owner, season 1 to {seasons}", "improves >= 1 place, not at the ceiling (mean of seasons 1-3 above 1.5)",
            Math.Round(progress, 2), $"mean places by season: {string.Join(", ", places.Select(p => p.ToString("0.0", CultureInfo.InvariantCulture)))}", progress >= 1 && ceiling > 1.5, 2, 2, "2, 3"));
        var finals = seeds.Select(s => (double)(Of(Balanced, s).Seasons.FirstOrDefault(x => x.Season == seasons)?.Place ?? 0)).Where(p => p > 0).ToArray();
        var spread = finals.Length < 2 ? 0 : Math.Sqrt(finals.Average(p => (p - finals.Average()) * (p - finals.Average())));
        gates.Add(new("Outcome diversity", $"Standard deviation of the balanced owner's season-{seasons} league place across seeds", ">= 1 place", Math.Round(spread, 2),
            $"places: {string.Join(", ", finals)}", spread >= 1, 0, 2, "6, 9"));
        // Rules v4 world references: the open market stays stocked, rivals turn over like real squads, the league keeps its spread.
        var stocked = allSeasons.Length == 0 ? 0 : allSeasons.Count(s => s.PoolMinPerRole >= 3) * 100.0 / allSeasons.Length;
        gates.Add(new("Market depth", "Share of season ends with at least three free agents in every role", ">= 90%", Math.Round(stocked, 1),
            $"thinnest role at season end: min {(allSeasons.Length == 0 ? 0 : allSeasons.Min(s => s.PoolMinPerRole))}, mean {(allSeasons.Length == 0 ? 0 : allSeasons.Average(s => s.PoolMinPerRole)):0.0}", stocked >= 90, 0, 4, "rules v4"));
        var churn = allSeasons.Where(s => s.RivalPlayers > 0).Select(s => s.RivalNewcomers * 100.0 / s.RivalPlayers).DefaultIfEmpty(0).Average();
        gates.Add(new("Rival churn", "Rival newcomers (signings and transfers) per season, percent of rival squads", "15-35% (calibrated: two splits of 12.6%)", Math.Round(churn, 1),
            "mean over seasons", churn is >= 15 and <= 35, 0, 4, "rules v4"));
        var rivalSpread = allSeasons.Where(s => s.RivalSpread > 0).Select(s => (double)s.RivalSpread).DefaultIfEmpty(0).Average();
        gates.Add(new("Rival spread", "Strongest minus weakest rival strength at season end", "10-20 points (calibrated: about 14)", Math.Round(rivalSpread, 1),
            "mean over seasons", rivalSpread is >= 10 and <= 20, 0, 4, "rules v4"));
        gates.Add(new("No solved preparation", "Win rate gap between the best focus pair and the mean pair", "<= 5 percentage points", null,
            "not measurable: weekly focus pairs arrive in wave 3", null, 3, 3, "4"));
        gates.Add(new("No solved negotiation", "Profit of the best fixed ask vs estimate-based asks", "fixed ask wins on <= 50% of brands", null,
            "not measured: multi-term negotiation arrives in wave 4", null, 0, 4, "5"));
        return [.. gates];
    }

    private static double Pearson((double X, double Y)[] samples)
    {
        if (samples.Length < 3) return 0;
        var mx = samples.Average(s => s.X); var my = samples.Average(s => s.Y);
        var cov = samples.Sum(s => (s.X - mx) * (s.Y - my));
        var vx = Math.Sqrt(samples.Sum(s => (s.X - mx) * (s.X - mx))); var vy = Math.Sqrt(samples.Sum(s => (s.Y - my) * (s.Y - my)));
        return vx == 0 || vy == 0 ? 0 : cov / (vx * vy);
    }

    public static void Print(Report report)
    {
        Console.WriteLine($"MEASURE rules={report.Rules} seeds={report.Seeds.Length} seasons={report.Seasons}");
        foreach (var policy in Policies)
        {
            var runs = report.Runs.Where(r => r.Policy == policy).ToArray();
            var last = runs.Select(r => r.Seasons.FirstOrDefault(s => s.Season == report.Seasons)).Where(s => s is not null).ToArray();
            var games = runs.SelectMany(r => r.Seasons).ToArray();
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"POLICY {policy,-14} winRate={games.Sum(s => s.Wins) * 100.0 / Math.Max(1, games.Sum(s => s.Wins + s.Losses)),5:0.0}% forfeits={games.Sum(s => s.Forfeits),3} " +
                $"cashS{report.Seasons}={Currency.Whole((long)(last.Length == 0 ? 0 : last.Average(s => s!.CashEnd))),9} ({last.Length} reached) repS{report.Seasons}={(last.Length == 0 ? 0 : last.Average(s => s!.ReputationEnd)),5:0.0} " +
                $"roster={(last.Length == 0 ? 0 : last.Average(s => s!.RosterEnd)),4:0.0} insolvent={runs.Count(r => r.Blocked)} " +
                $"p(win)={(games.Length == 0 ? 0 : games.Average(s => s.MeanWinProbability)),5:0.0}% base={(games.Length == 0 ? 0 : games.Average(s => s.MeanBase)),5:0.0} " +
                $"prep={(games.Length == 0 ? 0 : games.Average(s => s.MeanPreparation)),5:0.0} rival={(games.Length == 0 ? 0 : games.Where(s => s.MeanRivalStrength > 0).DefaultIfEmpty().Average(s => s?.MeanRivalStrength ?? 0)),5:0.0}"));
        }
        foreach (var gate in report.Gates)
            Console.WriteLine($"MEASURE_{(gate.OpensGate == 0 ? "REFERENCE" : "GATE" + gate.OpensGate)} {(gate.Pass is null ? "N/A " : gate.Pass.Value ? "PASS" : "FAIL")} {gate.Metric}: {gate.Value?.ToString(CultureInfo.InvariantCulture) ?? "-"} ({gate.Threshold}) {gate.Detail}");
    }

    public static void Write(Report report, string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
}
