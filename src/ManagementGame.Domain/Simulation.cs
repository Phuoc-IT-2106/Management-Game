using System.Collections.Immutable;

namespace ManagementGame.Domain;

public static class Simulation
{
    public static Fixture? NextFixture(Campaign s) => s.World.Fixtures.FirstOrDefault(f => !s.World.Results.Any(r => r.FixtureId == f.Id));
    public static int Load(Company c, Balance b, int day) => b.BaseLoad + c.People.Length * 3 + c.Sponsors.Where(x => x.EndDay >= day).Sum(x => x.Load);

    public static Campaign Apply(Campaign state, Intent intent, Content content, string cause)
    {
        if (state.World.Finished) throw new RuleViolation("Development campaign complete. Start another campaign to explore different decisions.");
        var c = state.Company; var w = state.World; var day = w.Calendar.Day;
        switch (intent)
        {
            case CommitPlan command:
                var plan = command.Plan;
                var next = NextFixture(state) ?? throw new RuleViolation("No remaining competition.");
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
                if (candidate.Deadline < day || c.People.Length >= 7) throw new RuleViolation("Signing window expired or seven-player fixture limit reached.");
                c = Finance.PayNow(c, candidate.Fee, cause, day);
                var contract = new Employment("employment:" + candidate.Person.Id + ":" + cause, candidate.Person.Id, candidate.Salary, candidate.ReleaseCost, content.Balance.Horizon);
                var items = c.FinancialItems;
                for (var due = day + 1; due <= contract.EndDay; due++)
                    items = items.Add(new FinancialItem($"{contract.Id}:day:{due}", contract.Id, due, contract.Salary, false, contract.Salary, null));
                c = c with { People = c.People.Add(candidate.Person with { Readiness = Math.Min(candidate.Person.Readiness, 70) }), Employment = c.Employment.Add(contract), FinancialItems = items };
                w = w with { Candidates = w.Candidates.Remove(candidate) };
                break;
            case ReleasePlayer command:
                var person = c.People.FirstOrDefault(x => x.Id == command.PersonId) ?? throw new RuleViolation("Player no longer belongs to this company.");
                if (!c.People.Any(p => p.Id != person.Id && p.Role == person.Role)) throw new RuleViolation("Release would leave a required role uncovered.");
                var employment = c.Employment.Single(x => x.PersonId == person.Id);
                c = Finance.PayNow(c, employment.ReleaseCost, cause, day);
                // Future wages are cancelled under fixture terms; due/overdue amounts remain payable.
                c = c with { People = c.People.Remove(person), Employment = c.Employment.Remove(employment),
                    FinancialItems = c.FinancialItems.Where(x => x.CauseId != employment.Id || x.DueDay <= day).ToImmutableArray(), Plan = null,
                    Recovery = c.Recovery == RecoveryStage.Distress ? RecoveryStage.Restructuring : c.Recovery };
                w = w with { Candidates = w.Candidates.Add(new Candidate(person, employment.ReleaseCost, employment.Salary, employment.ReleaseCost, content.Balance.Horizon)) };
                break;
            case AcceptSponsor command:
                var offer = w.Offers.FirstOrDefault(x => x.Id == command.OfferId) ?? throw new RuleViolation("Unknown sponsor offer.");
                if (offer.ClaimedBy is not null || offer.Deadline < day || offer.EndDay < day + 3 || c.Reputation < offer.MinimumReputation)
                    throw new RuleViolation("Sponsor offer is unavailable or reputation requirement is unmet.");
                if (c.Sponsors.Count(x => x.EndDay >= day) >= 2) throw new RuleViolation("The fixture permits one additional active sponsor.");
                c = Finance.AddSponsor(c, offer, day);
                w = w with { Offers = w.Offers.Replace(offer, offer with { ClaimedBy = c.Id }) };
                break;
            case Advance:
                return AdvanceToCheckpoint(state, content);
            default: throw new RuleViolation("Unsupported action.");
        }
        c = c with { Reviews = c.Reviews.Add(new ReviewEntry("decision:" + cause, day, cause, Describe(intent))) };
        return state with { Company = c, World = w };
    }

    private static string Describe(Intent intent) => intent switch
    {
        CommitPlan p => $"Committed {p.Plan.Execution}/{p.Plan.Opponent}/{p.Plan.Meta}% preparation and {p.Plan.Posture} posture; only future work changes.",
        SetAuthority a => $"Coach authority: {a.Authority.Mode}, risk ceiling {a.Authority.RiskCeiling}.",
        SignPlayer => "Signed player: fee paid, future wages committed; initial integration reduces readiness.",
        ReleasePlayer => "Released player: exit cost paid and future wages removed; existing arrears retained. Roster depth sacrificed.",
        AcceptSponsor => "Sponsor signed: first payment in three days, then every seven days; delivery load applies now.",
        _ => "Decision committed."
    };

    public static Campaign AdvanceToCheckpoint(Campaign s, Content content)
    {
        if (NextFixture(s) is not null && s.Company.Plan is null) throw new RuleViolation("Commit a preparation/lineup plan or authorize the coach before advancing.");
        for (var i = 0; i < 400; i++)
        {
            var previous = s;
            s = Step(s, content);
            if (ReferenceEquals(previous, s)) throw new RuleViolation("Financial distress requires recovery. Review arrears, sponsor timing and releasable contracts.");
            if (s.World.Finished || s.World.Results.Length > previous.World.Results.Length ||
                s.Company.Recovery == RecoveryStage.Distress && previous.Company.Recovery != RecoveryStage.Distress) return s;
        }
        throw new RuleViolation("Advancement exceeded bounded fixture work.");
    }

    public static Campaign Step(Campaign s, Content content)
    {
        if (s.World.Finished) return s;
        var c = s.Company; var w = s.World; var b = content.Balance; var cursor = w.Calendar; var day = cursor.Day;
        // Do not repeatedly settle or repeat prior phases while blocked. A new sponsor may offer a credible inflow.
        if (c.Recovery == RecoveryStage.Distress && c.FinancialItems.Any(x => !x.Incoming && x.Remaining > 0 && x.DueDay < day)
            && !c.FinancialItems.Any(x => x.Incoming && x.Remaining > 0 && x.DueDay <= day + 3) && c.Cash == 0) return s;
        switch (cursor.Phase)
        {
            case Phase.Finance: c = Finance.Settle(c, day); break;
            case Phase.Contracts:
                c = c with { People = c.People.Select(p => p with { Readiness = Math.Min(100, p.Readiness + 1) }).ToImmutableArray() }; break;
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
                    var history = w.Results;
                    w = w with { Rivals = w.Rivals.Select(r => r with { Strength = Math.Clamp(r.Strength + (r.Need > 0 ? 1 : -1), 1, 100),
                        Posture = history.Length > 0 && r.Analysis >= 60 ? (Posture)(((int)history[^1].Posture + 1) % 3) : r.Posture }).ToImmutableArray() };
                    // Finite sponsor scarcity from a rival's own budget/need; never player rubber-banding.
                    foreach (var rival in w.Rivals.Where(r => r.Need > 0 && r.Budget >= 10).OrderBy(r => r.Id, StringComparer.Ordinal))
                    {
                        var offer = w.Offers.FirstOrDefault(o => o.ClaimedBy is null && o.Deadline <= day && o.MinimumReputation <= 50);
                        if (offer is null) continue;
                        w = w with { Offers = w.Offers.Replace(offer, offer with { ClaimedBy = rival.Id }),
                            Rivals = w.Rivals.Replace(rival, rival with { Budget = rival.Budget - 10, Need = 0 }) };
                        c = c with { Reviews = c.Reviews.Add(new ReviewEntry($"claim:{offer.Id}", day, offer.Id, $"{rival.Name} claimed {offer.Name}; the shared opportunity is no longer available.")) };
                    }
                }
                break;
            case Phase.Meta:
                if (day == b.MetaDay) { w = w with { Meta = Posture.Aggressive }; c = c with { Reviews = c.Reviews.Add(new ReviewEntry("meta:shift", day, "world:meta", "The public meta shifted toward aggressive play; adaptation preparation becomes more valuable.")) }; }
                break;
            case Phase.Checkpoint:
                if (w.Fixtures.Any(f => f.Day == day) && c.Plan is null) return s;
                break;
            case Phase.Competition:
                var fixture = w.Fixtures.FirstOrDefault(f => f.Day == day && !w.Results.Any(r => r.FixtureId == f.Id));
                if (fixture is not null)
                {
                    var plan = c.Plan ?? throw new RuleViolation("Competition requires a committed plan.");
                    var rival = w.Rivals.Single(r => r.Id == fixture.RivalId);
                    var outcome = Competition.Resolve(new CompetitionInput(s.Execution.Seed, fixture, plan, c.Work,
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
                if (day >= b.Horizon) w = w with { Finished = true };
                break;
        }
        var nextCursor = cursor.Phase == Phase.Review ? new Calendar(day + 1, Phase.Finance, cursor.Sequence + 1)
            : cursor with { Phase = cursor.Phase + 1, Sequence = cursor.Sequence + 1 };
        return s with { Company = c, World = w with { Calendar = nextCursor } };
    }
}
