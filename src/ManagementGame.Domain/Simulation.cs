using System.Collections.Immutable;

namespace ManagementGame.Domain;

public static class Simulation
{
    public static Fixture? NextFixture(Campaign s) => s.World.Fixtures.FirstOrDefault(f => !s.World.Results.Any(r => r.FixtureId == f.Id));
    public static int Load(Company c, Balance b, int day) => b.BaseLoad + c.People.Length * 3 + c.Sponsors.Where(x => x.EndDay >= day).Sum(x => x.Load);
    /// <summary>Every role A–E has at least one available player.</summary>
    public static bool LineupPossible(Company c) => Generator.Roles.All(r => c.People.Any(p => p.Role == r.ToString() && p.Readiness > 0));
    public static int ContractEnd(int day, int seasons, Balance b) => day + seasons * b.SeasonLength - 1;

    public static Campaign Apply(Campaign state, Intent intent, Content content, string cause)
    {
        var c = state.Company; var w = state.World; var b = content.Balance; var day = w.Calendar.Day;
        switch (intent)
        {
            case CommitPlan command:
                var plan = command.Plan;
                var next = NextFixture(state) ?? throw new RuleViolation("No scheduled match this season.");
                if (plan.FixtureId != next.Id) throw new RuleViolation("The selected competition is no longer current.");
                if (!Enum.IsDefined(plan.Posture) || !Enum.IsDefined(plan.ExpectedOpponent) || plan.Execution < 0 || plan.Opponent < 0 || plan.Meta < 0
                    || (long)plan.Execution + plan.Opponent + plan.Meta != 100) throw new RuleViolation("Preparation allocations must total 100%.");
                if (plan.Lineup.IsDefault || plan.Lineup.Length != 5 || plan.Lineup.Distinct().Count() != 5) throw new RuleViolation("Select five distinct eligible players.");
                var people = plan.Lineup.Select(id => c.People.FirstOrDefault(p => p.Id == id) ?? throw new RuleViolation("A selected player is no longer on the roster.")).ToArray();
                if (people.Select(p => p.Role).Distinct().Count() != 5) throw new RuleViolation("Lineup must cover roles A, B, C, D and E.");
                if (people.Any(p => p.Readiness <= 0)) throw new RuleViolation("A selected player is unavailable.");
                c = c with { Plan = plan with { CauseId = cause } };
                break;
            case SetAuthority command:
                if (!Enum.IsDefined(command.Authority.Mode) || !Enum.IsDefined(command.Authority.RiskCeiling)) throw new RuleViolation("Unknown authority setting.");
                c = c with { Authority = command.Authority };
                break;
            case SignPlayer command:
                var candidate = w.Candidates.FirstOrDefault(x => x.Person.Id == command.PersonId) ?? throw new RuleViolation("Candidate is no longer available.");
                if (candidate.Deadline < day) throw new RuleViolation("This free agent has moved on.");
                if (c.People.Length >= b.MaxRoster) throw new RuleViolation($"Roster limit of {b.MaxRoster} reached; release a player first.");
                if (command.Seasons is < 1 or > 3) throw new RuleViolation("Contracts run for one to three seasons.");
                c = Finance.PayNow(c, candidate.Fee, cause, day);
                var contract = new Employment("employment:" + candidate.Person.Id + ":" + cause, candidate.Person.Id, candidate.Salary, candidate.ReleaseCost, ContractEnd(day, command.Seasons, b));
                c = c with { People = c.People.Add(candidate.Person with { Readiness = Math.Min(candidate.Person.Readiness, 70) }), Employment = c.Employment.Add(contract) };
                w = w with { Candidates = w.Candidates.Remove(candidate) };
                break;
            case ReleasePlayer command:
                var person = c.People.FirstOrDefault(x => x.Id == command.PersonId) ?? throw new RuleViolation("Player no longer belongs to this company.");
                if (!c.People.Any(p => p.Id != person.Id && p.Role == person.Role)) throw new RuleViolation("Release would leave a required role uncovered.");
                var employment = c.Employment.Single(x => x.PersonId == person.Id);
                c = Finance.PayNow(c, employment.ReleaseCost, cause, day);
                // Wages accrue daily; due/overdue amounts remain payable after release.
                c = c with { People = c.People.Remove(person), Employment = c.Employment.Remove(employment), Plan = null,
                    Recovery = c.Recovery == RecoveryStage.Distress ? RecoveryStage.Restructuring : c.Recovery };
                w = w with { Candidates = w.Candidates.Add(new Candidate(person, employment.ReleaseCost, employment.Salary, employment.ReleaseCost, day + b.TalentRefreshInterval - 1)) };
                break;
            case RenewContract command:
                var current = c.Employment.FirstOrDefault(x => x.PersonId == command.PersonId) ?? throw new RuleViolation("No contract to renew.");
                if (command.Seasons is < 1 or > 3) throw new RuleViolation("Contracts run for one to three seasons.");
                if (current.EndDay - day > b.RenewalWindow) throw new RuleViolation($"Renewal talks open {b.RenewalWindow} days before the contract ends (day {current.EndDay}).");
                var demand = RenewalDemand(c, current);
                c = c with { Employment = c.Employment.Replace(current, current with { Salary = demand, ReleaseCost = demand * 3, EndDay = current.EndDay + command.Seasons * b.SeasonLength }) };
                break;
            case HireCoach command:
                var hire = w.CoachCandidates.FirstOrDefault(x => x.Coach.Id == command.CoachId) ?? throw new RuleViolation("Coach is no longer available.");
                if (hire.Deadline < day) throw new RuleViolation("This coach has taken another position.");
                var previous = c.Employment.Single(x => x.PersonId == c.Coach.Id);
                c = Finance.PayNow(c, checked(hire.Fee + previous.ReleaseCost), cause, day);
                w = w with { CoachCandidates = w.CoachCandidates.Remove(hire)
                    .Add(new CoachCandidate(c.Coach, previous.Salary * 15, previous.Salary, day + b.TalentRefreshInterval - 1)) };
                c = c with { Coach = hire.Coach, Employment = c.Employment.Remove(previous)
                    .Add(new Employment("employment:" + hire.Coach.Id + ":" + cause, hire.Coach.Id, hire.Salary, hire.Salary * 5, ContractEnd(day, 2, b))) };
                break;
            case AcceptSponsor command:
                var offer = w.Offers.FirstOrDefault(x => x.Id == command.OfferId) ?? throw new RuleViolation("Unknown sponsor offer.");
                if (offer.ClaimedBy is not null || offer.Deadline < day || c.Reputation < offer.MinimumReputation)
                    throw new RuleViolation("Sponsor offer is unavailable or reputation requirement is unmet.");
                if (Market.ActiveSponsors(c, day) >= b.SponsorSlots) throw new RuleViolation($"All {b.SponsorSlots} sponsor slots are in use.");
                if (c.Sponsors.Any(s => s.BrandId == offer.BrandId && s.EndDay >= day)) throw new RuleViolation("This brand already sponsors the company.");
                c = Finance.AddSponsor(c, offer, day, "agreement:" + offer.Id);
                w = w with { Offers = w.Offers.Replace(offer, offer with { ClaimedBy = c.Id }) };
                break;
            case ProposeSponsorship command:
                var brand = Market.Brand(content, command.BrandId);
                if (!Market.Approachable(brand, c)) throw new RuleViolation($"{brand.Name} is out of reach: it expects reputation {brand.MinimumReputation} and audience {Pay.Count(brand.MinimumAudience)}.");
                if (Market.Blocked(brand, c, w, day) is { } reason) throw new RuleViolation($"{brand.Name}: {reason}.");
                if (Market.ActiveSponsors(c, day) >= b.SponsorSlots) throw new RuleViolation($"All {b.SponsorSlots} sponsor slots are in use.");
                if (w.Negotiations.Length >= 3) throw new RuleViolation("Three proposals are already under review.");
                if (!brand.DurationDays.Contains(command.DurationDays)) throw new RuleViolation($"{brand.Name} signs for {string.Join(", ", brand.DurationDays.Order())} days.");
                if (command.Payment <= 0 || command.Payment > brand.BasePayment * 3) throw new RuleViolation("Proposed payment is outside any plausible range.");
                w = w with { Negotiations = w.Negotiations.Add(new Negotiation($"negotiation:{brand.Id}:{day}", brand.Id, command.Payment, command.DurationDays, day, day + b.NegotiationDelay)) };
                break;
            case Advance:
                return AdvanceToCheckpoint(state, content);
            default: throw new RuleViolation("Unsupported action.");
        }
        c = c with { Reviews = c.Reviews.Add(new ReviewEntry("decision:" + cause, day, cause, Describe(intent, content))) };
        return state with { Company = c, World = w };
    }

    public static long RenewalDemand(Company c, Employment contract) => contract.PersonId == c.Coach.Id
        ? Pay.Demand(contract.Salary, Pay.Coach(c.Coach), c.Reputation)
        : Pay.Demand(contract.Salary, Pay.Player(c.People.Single(p => p.Id == contract.PersonId).Execution), c.Reputation);

    private static string Describe(Intent intent, Content content) => intent switch
    {
        CommitPlan p => $"Committed {p.Plan.Execution}/{p.Plan.Opponent}/{p.Plan.Meta}% preparation and {p.Plan.Posture} posture; only future work changes.",
        SetAuthority a => $"Coach authority: {a.Authority.Mode}, risk ceiling {a.Authority.RiskCeiling}.",
        SignPlayer s => $"Signed player for {s.Seasons} season(s): fee paid, daily wages committed; initial integration reduces readiness.",
        ReleasePlayer => "Released player: exit cost paid and future wages stop; existing arrears retained. Roster depth sacrificed.",
        RenewContract r => $"Renewed contract for {r.Seasons} more season(s) at the new asking wage.",
        HireCoach => "Hired a new head coach: signing fee and the previous coach's exit cost paid.",
        AcceptSponsor => "Sponsor signed: first payment in three days, then weekly; delivery load applies now.",
        ProposeSponsorship p => $"Proposed {Pay.Money(p.Payment)} per week for {p.DurationDays} days to {Market.Brand(content, p.BrandId).Name}; reply in {content.Balance.NegotiationDelay} days.",
        _ => "Decision committed."
    };

    private static Fixture? MatchToday(Campaign s) => s.World.Fixtures.FirstOrDefault(f => f.Day == s.World.Calendar.Day && !s.World.Results.Any(r => r.FixtureId == f.Id));

    /// <summary>Runs days until something needs the player: a result, a notice, distress, a match without a plan, or a quiet week.</summary>
    public static Campaign AdvanceToCheckpoint(Campaign s, Content content)
    {
        var start = s; var b = content.Balance;
        for (var i = 0; i < 11 * (b.MarketInterval + 2); i++)
        {
            if (s.World.Calendar.Phase == Phase.Checkpoint && MatchToday(s) is not null && s.Company.Plan is null && LineupPossible(s.Company))
            {
                if (ReferenceEquals(s, start)) throw new RuleViolation("Match day: commit a preparation/lineup plan or authorize the coach before advancing.");
                return s;
            }
            var previous = s;
            s = Step(s, content);
            if (ReferenceEquals(previous, s)) throw new RuleViolation("Financial distress requires recovery. Review arrears, sponsor timing and releasable contracts.");
            if (StopReason(previous, s) is not null) return s;
            if (s.World.Calendar.Phase == Phase.Finance && s.World.Calendar.Day >= start.World.Calendar.Day + b.MarketInterval) return s;
        }
        throw new RuleViolation("Advancement exceeded bounded work.");
    }

    /// <summary>Why progression between two states stopped, from authoritative state differences only.</summary>
    public static string? StopReason(Campaign before, Campaign after)
    {
        if (after.World.Results.Length > before.World.Results.Length)
        {
            var result = after.World.Results[^1];
            return $"{(result.Won ? "Victory" : "Defeat")} vs {after.World.Rivals.Single(r => r.Id == result.RivalId).Name}.";
        }
        if (after.Company.Recovery == RecoveryStage.Distress && before.Company.Recovery != RecoveryStage.Distress) return "Financial distress: obligations are overdue.";
        var notices = after.Company.Reviews.Skip(before.Company.Reviews.Length).Where(r => r.Id.StartsWith(Market.NoticePrefix, StringComparison.Ordinal)).ToArray();
        if (notices.Length > 0) return string.Join(" ", notices.Select(n => n.Text));
        if (after.World.Calendar.Phase == Phase.Checkpoint && MatchToday(after) is not null && after.Company.Plan is null && LineupPossible(after.Company))
            return $"Match day vs {after.World.Rivals.Single(r => r.Id == MatchToday(after)!.RivalId).Name}: commit a preparation plan.";
        return null;
    }

    public static Campaign Step(Campaign s, Content content)
    {
        var c = s.Company; var w = s.World; var b = content.Balance; var cursor = w.Calendar; var day = cursor.Day; var seed = s.Execution.Seed;
        // Do not repeatedly settle or repeat prior phases while blocked. A new sponsor may offer a credible inflow.
        if (c.Recovery == RecoveryStage.Distress && c.FinancialItems.Any(x => !x.Incoming && x.Remaining > 0 && x.DueDay < day)
            && !c.FinancialItems.Any(x => x.Incoming && x.Remaining > 0 && x.DueDay <= day + 3) && c.Cash == 0) return s;
        switch (cursor.Phase)
        {
            case Phase.Finance: c = Finance.Settle(Finance.Payroll(c, day), day); break;
            case Phase.Contracts:
                c = c with { People = c.People.Select(p => p with { Readiness = Math.Min(100, p.Readiness + 1) }).ToImmutableArray() };
                (c, w) = Market.Contracts(c, w, seed, content, day);
                (c, w) = Market.Resolve(c, w, seed, content, day);
                (c, w) = Market.Expire(c, w, content, day);
                if (day > 1 && (day - 1) % b.MarketInterval == 0) (c, w) = Market.Approach(c, w, seed, content, day);
                if (day > 1 && (day - 1) % b.TalentRefreshInterval == 0) w = Market.Refresh(c, w, seed, content, day);
                break;
            case Phase.Preparation:
                if (c.Plan is not null && NextFixture(s) is not null)
                {
                    var converted = (int)Numbers.Divide((long)c.Coach.Preparation * c.Coach.Capacity, Math.Max(c.Coach.Capacity, Load(c, b, day)));
                    c = c with { Work = new Work(checked(c.Work.Execution + converted * c.Plan.Execution / 100),
                        checked(c.Work.Opponent + converted * c.Plan.Opponent / 100), checked(c.Work.Meta + converted * c.Plan.Meta / 100)) };
                }
                break;
            case Phase.Organization: break; // No second aggregate strength penalty for preparation overload.
            case Phase.Rivals:
                if (day % b.RivalCadence == 0)
                {
                    var history = w.Results; var range = content.Generation.Rival;
                    w = w with { Rivals = w.Rivals.Select(r => r with { Strength = Math.Clamp(r.Strength + (r.Need > 0 ? 1 : -1), range.Min - 5, range.Max + 5),
                        Posture = history.Length > 0 && r.Analysis >= 60 ? (Posture)(((int)history[^1].Posture + 1) % 3) : r.Posture }).ToImmutableArray() };
                }
                break;
            case Phase.Meta:
                if (Seasons.DayOf(day, b) == b.MetaDayOfSeason)
                {
                    var meta = KeyedRandom.Range(seed, "meta", "season:" + Seasons.Of(day, b), "shift", 2) == 0 ? Posture.Aggressive : Posture.Conservative;
                    w = w with { Meta = meta };
                    c = c with { Reviews = c.Reviews.Add(new ReviewEntry($"meta:shift:{day}", day, "world:meta", $"The public meta shifted toward {meta.ToString().ToLowerInvariant()} play; adaptation preparation becomes more valuable.")) };
                }
                break;
            case Phase.Checkpoint:
                if (MatchToday(s) is not null && c.Plan is null && LineupPossible(c)) return s;
                break;
            case Phase.Competition:
                var fixture = MatchToday(s);
                if (fixture is not null)
                {
                    var rival = w.Rivals.Single(r => r.Id == fixture.RivalId);
                    if (c.Plan is not { } plan)
                    {
                        // No legal lineup: the match is forfeited rather than blocking time.
                        var forfeit = new CompetitiveOutcome("result:" + fixture.Id, fixture.Id, day, rival.Id, false, Posture.Balanced, 50_000, 0, 0, 0, 0, 0, fixture.Importance, "forfeit");
                        w = w with { Results = w.Results.Add(forfeit) };
                        // A forfeit is a public failure to field a team, not an unlucky loss: a fixed, visible commercial penalty.
                        c = Finance.Consume(c, forfeit, fixture);
                        c = c with { CommercialReceipts = c.CommercialReceipts.Add(forfeit.Id),
                            PendingEffects = c.PendingEffects.Add(new CommercialEffect("commercial:" + forfeit.Id, forfeit.Id, day + 1, -6 * fixture.Importance, -300 * fixture.Importance)) };
                        c = c with { Reviews = c.Reviews.Add(new ReviewEntry(forfeit.Id, day, "forfeit", $"Forfeit vs {rival.Name}: the roster could not field all five roles. Sign players to cover missing roles.")) };
                        break;
                    }
                    var outcome = Competition.Resolve(new CompetitionInput(seed, fixture, plan, c.Work,
                        plan.Lineup.Select(id => c.People.Single(p => p.Id == id)).ToImmutableArray(), c.Coach, rival, w.Meta,
                        w.Results.Count(r => r.Posture == plan.Posture), b));
                    w = w with { Results = w.Results.Add(outcome) };
                    c = Finance.Consume(c, outcome, fixture);
                    c = Commercial.Consume(c, outcome, b);
                    c = c with { Plan = null, Work = new Work(c.Work.Execution / 2, 0, c.Work.Meta / 2),
                        People = c.People.Select(p => plan.Lineup.Contains(p.Id) ? p with { Readiness = Math.Max(1, p.Readiness - 8) } : p).ToImmutableArray(),
                        Reviews = c.Reviews.Add(new ReviewEntry(outcome.Id, day, plan.CauseId,
                        $"{(outcome.Won ? "Victory" : "Defeat")} vs {rival.Name}. {plan.Posture} posture; completed preparation contributed {outcome.Preparation}; public meta {(outcome.Matchup > 0 ? "suited" : "challenged")} the plan. Rival response and match-day variance remain uncertain. Any prize/bonus and commercial changes are due tomorrow.")) };
                }
                break;
            case Phase.Consequences: c = Commercial.ApplyDue(c, day); break;
            case Phase.Recovery:
                var overdue = c.FinancialItems.Any(x => !x.Incoming && x.Remaining > 0 && x.DueDay <= day);
                c = c with { Recovery = overdue ? RecoveryStage.Distress : Finance.Forecast(c, day) < 0 ? RecoveryStage.Warning :
                    c.Recovery is RecoveryStage.Restructuring or RecoveryStage.Distress or RecoveryStage.Stabilized ? RecoveryStage.Stabilized : RecoveryStage.Stable };
                break;
            case Phase.Review:
                if (day == Seasons.End(Seasons.Of(day, b), b)) (c, w) = Rollover(c, w, seed, content, day);
                break;
        }
        var nextCursor = cursor.Phase == Phase.Review ? new Calendar(day + 1, Phase.Finance, cursor.Sequence + 1)
            : cursor with { Phase = cursor.Phase + 1, Sequence = cursor.Sequence + 1 };
        return s with { Company = c, World = w with { Calendar = nextCursor } };
    }

    /// <summary>Closes a season: record, rival evolution, next schedule, and bounded history.</summary>
    private static (Company, World) Rollover(Company c, World w, ulong seed, Content content, int day)
    {
        var b = content.Balance; var season = Seasons.Of(day, b); var range = content.Generation.Rival;
        var played = w.Results.Where(r => r.FixtureId.StartsWith(Seasons.FixturePrefix(season), StringComparison.Ordinal)).ToArray();
        var rivals = w.Rivals.Select(r => r with
        {
            Strength = Math.Clamp(r.Strength + KeyedRandom.Range(seed, "rival", $"{r.Id}:{season}", "development", 9) - 4, range.Min - 5, range.Max + 5),
            Posture = (Posture)KeyedRandom.Range(seed, "rival", $"{r.Id}:{season}", "posture", 3),
            Budget = 40, Need = KeyedRandom.Range(seed, "rival", $"{r.Id}:{season}", "need", 2)
        }).ToImmutableArray();
        c = Finance.Archive(c, Seasons.Start(season, b));
        c = c with { Reviews = c.Reviews.Skip(Math.Max(0, c.Reviews.Length - 150)).ToImmutableArray() };
        c = c with { Reviews = c.Reviews.Add(new ReviewEntry($"{Market.NoticePrefix}season:{season + 1}:{day}", day, $"season:{season}",
            $"Season {season} closed {played.Count(r => r.Won)}–{played.Count(r => !r.Won)}. Season {season + 1} begins with a new schedule; rivals have reshaped their squads.")) };
        return (c, w with { Rivals = rivals, Fixtures = Seasons.Schedule(seed, season + 1, rivals, b), Meta = Posture.Balanced });
    }
}
