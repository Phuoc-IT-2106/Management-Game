using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Formatting;
using ManagementGame.Infrastructure;
using System.Collections.Immutable;
var content = ContentLoader.Load(Path.GetFullPath("content/fixture.json"));
var b = content.Definition.Balance;
Session New(ulong seed = 123456789, Content? definition = null, string? company = null) =>
    new(definition ?? content.Definition, CampaignFactory.Create(definition ?? content.Definition, content.Hash, seed, "campaign:fixture", company));
var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
long serial = 0;
Response Submit(Session s, Decision d) => s.Submit(new Request("t:" + ++serial, s.Observe().Revision, d));
void AdvanceUntilResults(Session s, int results) { for (var i = 0; i < 20 && s.Observe().Results.Length < results; i++) Submit(s, new AdvanceDecision()); }

// Command atomicity and idempotency.
var session = New();
var before = Canonical.Hash(session.Capture());
Check(!session.Submit(new Request("bad", 99, new AdvanceDecision())).Accepted && Canonical.Hash(session.Capture()) == before, "stale rejection atomicity");
Check(!session.Submit(new Request("bad", 0, new SigningDecision("missing"))).Accepted && Canonical.Hash(session.Capture()) == before, "invalid ID atomicity");
var coach = new Request("authority", 0, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
Check(session.Submit(coach).Accepted, "coach authority commit");
var after = Canonical.Hash(session.Capture());
Check(session.Submit(coach).Accepted && Canonical.Hash(session.Capture()) == after, "exact retry idempotency");
Check(!session.Submit(coach with { Decision = new CoachDecision(Delegation.Manual, Risk.Balanced) }).Accepted, "conflicting reuse rejected");

// Two consecutive management cycles on the generated season schedule.
var schedule = session.Capture().World.Fixtures;
Check(schedule.Length == 2 * b.RivalCount && schedule[0].Day == b.FirstMatchDay && schedule[1].Day == b.FirstMatchDay + b.MatchInterval
    && schedule.GroupBy(f => f.RivalId).All(g => g.Count() == 2), "season schedule meets every rival twice");
string[] cycleIds = ["advance:1", "advance:2", "advance:3", "advance:4", "advance:5", "advance:6"];
var cursor = 0;
void Cycle(Session s, int results) { while (s.Observe().Results.Length < results && cursor < cycleIds.Length) s.Submit(new Request(cycleIds[cursor++], s.Observe().Revision, new AdvanceDecision())); }
Cycle(session, 1);
Check(session.Observe().Results.Length == 1 && session.Observe().Day == schedule[0].Day, "first result checkpoint");
Cycle(session, 2);
Check(session.Observe().Results.Length == 2 && session.Observe().Day == schedule[1].Day, "second result checkpoint");
var usedCycles = cursor;
var repeat = New(); repeat.Submit(coach); foreach (var advanceId in cycleIds.Take(usedCycles)) repeat.Submit(new Request(advanceId, repeat.Observe().Revision, new AdvanceDecision()));
Check(Canonical.Hash(repeat.Capture()) == Canonical.Hash(session.Capture()), "same seed and commands equal authoritative hash");
var result = session.Capture().World.Results[0]; var fixture = schedule.Single(f => f.Id == result.FixtureId);
var company = session.Capture().Company;
Check(Finance.Consume(company, result, fixture, b) == company && Commercial.Consume(company, result, b) == company, "consumer idempotency");
var observed = Canonical.Json(session.Observe());
Check(!observed.Contains("Probability") && !observed.Contains("Strength") && !observed.Contains("Variance"), "observations exclude developer truth");
for (var i = 0; i < 50; i++) session.Observe();
Check(Canonical.Hash(repeat.Capture()) == Canonical.Hash(session.Capture()), "observation does not mutate or consume RNG");

// Saves.
var testDirectory = Path.GetFullPath("artifacts/save-tests-" + Guid.NewGuid().ToString("N"));
var store = new SnapshotStore(testDirectory, content);
store.Save("roundtrip", session.Capture());
var loaded = store.Load("roundtrip");
Check(Canonical.Hash(loaded) == Canonical.Hash(session.Capture()), "explicit DTO disk round trip");
var continued = new Session(content.Definition, loaded, store);
var nextCommand = new Request("advance:next", session.Observe().Revision, new AdvanceDecision());
Check(continued.Submit(nextCommand).Accepted && session.Submit(nextCommand).Accepted && Canonical.Hash(continued.Capture()) == Canonical.Hash(session.Capture()), "loaded continuation equals uninterrupted");
store.Save("roundtrip", continued.Capture());
Check(Canonical.Hash(store.Load("roundtrip", true)) == Canonical.Hash(loaded), "explicit backup preserves previous revision");
var liveHash = Canonical.Hash(continued.Capture());
File.WriteAllText(store.PrimaryPath("roundtrip"), "{corrupt");
Check(!continued.Load("roundtrip").Accepted && Canonical.Hash(continued.Capture()) == liveHash, "corrupt load preserves valid session");
var corruptBytes = File.ReadAllBytes(store.PrimaryPath("roundtrip"));
Check(!continued.Save("roundtrip").Accepted && File.ReadAllBytes(store.PrimaryPath("roundtrip")).SequenceEqual(corruptBytes), "corrupt primary cannot replace backup evidence");
Check(continued.Load("roundtrip", true).Accepted && Canonical.Hash(continued.Capture()) == Canonical.Hash(loaded), "explicit recovery loads valid backup");

// Save every executable phase boundary of a whole match, including its immediate consumers.
var phaseState = New().Capture();
var plan = Observations.Build(phaseState, content.Definition).Recommendation!;
phaseState = Simulation.Apply(phaseState, new CommitPlan(new Plan(plan.FixtureId, plan.Execution, plan.Opponent, plan.Meta, (Posture)plan.Posture, plan.Lineup, Posture.Balanced, "phase-plan")), content.Definition, "phase-plan");
var boundaries = 0;
while (phaseState.World.Results.Length == 0)
{
    store.Save("phase", phaseState);
    var resume = store.Load("phase");
    var uninterrupted = Simulation.Step(phaseState, content.Definition);
    Check(Canonical.Hash(Simulation.Step(resume, content.Definition)) == Canonical.Hash(uninterrupted), "phase save continuation " + boundaries);
    phaseState = uninterrupted; boundaries++;
}
store.Save("phase", phaseState); Check(Canonical.Hash(store.Load("phase")) == Canonical.Hash(phaseState), "result plus immediate consumers saved atomically");

// Commercial and talent decisions on generated identities.
var market = New(); var opening = market.Observe(); var cashBefore = opening.Cash;
var offer = opening.Offers.First(o => o.Availability == "Available");
Check(opening.Offers.Length >= 2 && opening.Offers.All(o => o.Origin == "Inbound"), "opening market offers a real choice of inbound sponsors");
Check(Submit(market, new SponsorDecision(offer.Id)).Accepted && market.Capture().Company.Cash == cashBefore, "signed sponsorship creates receivables, no immediate cash");
Check(market.Capture().Company.FinancialItems.Any(x => x.CauseId == "agreement:" + offer.Id && x.DueDay == opening.Day + 3), "sponsor timing explicit");
Check(market.Observe().Offers.Where(o => o.Id != offer.Id).All(o => o.Availability != "Available"), "filled sponsor slots disable competing offers");
var candidate = market.Observe().Candidates[0];
Check(Submit(market, new SigningDecision(candidate.Id, 2)).Accepted && !market.Capture().World.Candidates.Any(x => x.Person.Id == candidate.Id)
    && market.Capture().Company.Employment.Single(e => e.PersonId == candidate.Id).EndDay == Simulation.ContractEnd(opening.Day, 2, b), "recruitment transfers ownership with a chosen contract length");
SnapshotValidation.Validate(market.Capture(), content);
var duplicate = market.Observe().People.First(p => p.CanRelease);
Check(Submit(market, new ReleaseDecision(duplicate.Id)).Accepted && market.Capture().World.Candidates.Any(x => x.Person.Id == duplicate.Id), "release transfers ownership and sacrifices roster depth");
SnapshotValidation.Validate(market.Capture(), content);

// Negotiation: the player authors terms; the brand answers after a delay.
var deal = New(); var brand = deal.Observe().Brands.First(x => x.Availability == "Open to proposals");
var definitionBrand = content.Definition.Brands.Single(x => x.Id == brand.Id);
var fair = Market.FairPayment(definitionBrand, deal.Observe().Reputation, deal.Observe().Audience);
Check(Submit(deal, new ProposalDecision(brand.Id, fair, brand.DurationDays[0])).Accepted && deal.Observe().Negotiations.Length == 1, "proposal is queued, not instantly granted");
Check(!Submit(deal, new ProposalDecision(brand.Id, fair, brand.DurationDays[0])).Accepted, "one live proposal per brand");
Check(!Submit(deal, new ProposalDecision(brand.Id, fair, 3)).Accepted, "brands only sign their own durations");
Submit(deal, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
for (var i = 0; i < 4 && deal.Observe().Negotiations.Length > 0; i++) Submit(deal, new AdvanceDecision());
var answered = deal.Observe().Offers.SingleOrDefault(o => o.Name == brand.Name && o.Origin != "Inbound");
Check(answered is { Origin: "Negotiated" } && answered.Payment == fair && answered.DurationDays == brand.DurationDays[0], "fair proposal is accepted on the proposed terms");
var greedy = New(); var target = greedy.Observe().Brands.First(x => x.Availability == "Open to proposals");
var targetFair = Market.FairPayment(content.Definition.Brands.Single(x => x.Id == target.Id), greedy.Observe().Reputation, greedy.Observe().Audience);
Submit(greedy, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
Check(Submit(greedy, new ProposalDecision(target.Id, targetFair * 2, target.DurationDays[0])).Accepted, "overreaching proposal can be sent");
for (var i = 0; i < 4 && greedy.Observe().Negotiations.Length > 0; i++) Submit(greedy, new AdvanceDecision());
var greedyBrand = greedy.Observe().Brands.Single(x => x.Id == target.Id);
Check(greedyBrand.Availability.StartsWith("Unavailable until day", StringComparison.Ordinal) && !greedy.Observe().Offers.Any(o => o.Name == target.Name && o.Origin != "Inbound"), "overreach is rejected and the brand cools off");
Check(!Submit(greedy, new ProposalDecision(target.Id, targetFair, target.DurationDays[0])).Accepted, "cooling-off brand cannot be re-approached immediately");
var premium = New().Observe().Brands.Where(x => x.Availability.StartsWith("Out of reach", StringComparison.Ordinal)).ToArray();
Check(premium.Length > 0 && New().Observe().Offers.All(o => content.Definition.Brands.Single(x => x.Name == o.Name).MinimumReputation <= b.Reputation), "premium brands neither approach nor accept a small company");

// Generated identities: nothing about people, rivals or brands is fixed in code.
var a = New(1).Observe(); var other = New(2).Observe();
Check(a.People.Select(p => p.Name).SequenceEqual(New(1).Observe().People.Select(p => p.Name)), "same seed regenerates the same people");
Check(!a.People.Select(p => p.Name).SequenceEqual(other.People.Select(p => p.Name)) || a.Coach != other.Coach, "different seeds generate different rosters or coaches");
var rivalsBySeed = Enumerable.Range(1, 6).Select(seed => string.Join("|", New((ulong)seed).Capture().World.Rivals.Select(r => r.Name))).Distinct().Count();
var coachesBySeed = Enumerable.Range(1, 6).Select(seed => New((ulong)seed).Observe().Coach).Distinct().Count();
Check(rivalsBySeed > 1 && coachesBySeed > 1, "rival organizations and head coaches vary across campaigns");
Check(New(company: "Hanoi Signal").Observe().Company == "Hanoi Signal" && New().Observe().Company == content.Definition.DefaultCompanyName, "player names the company");
Check(a.People.All(p => content.Definition.Names.Given.Any(g => p.Name.StartsWith(g + " ", StringComparison.Ordinal))), "names come from the content pool");

// Contracts: renewal window, renewal terms, expiry and the coach market.
var contracts = New(); var renewable = contracts.Capture().Company.Employment.Where(e => e.PersonId != contracts.Capture().Company.Coach.Id).OrderBy(e => e.EndDay).First();
Check(!Submit(contracts, new RenewalDecision(renewable.PersonId, 1)).Accepted, "renewal talks wait for the renewal window");
var windowed = contracts.Capture() with { World = contracts.Capture().World with { Calendar = new Calendar(renewable.EndDay - b.RenewalWindow, Phase.Finance, (renewable.EndDay - b.RenewalWindow - 1L) * 11) } };
var renewal = new Session(content.Definition, windowed);
var asked = renewal.Observe().People.Single(p => p.Id == renewable.PersonId);
Check(asked.CanRenew && asked.RenewalSalary >= renewable.Salary && Submit(renewal, new RenewalDecision(renewable.PersonId, 2)).Accepted, "renewal opens in the window at the asking wage");
var renewed = renewal.Capture().Company.Employment.Single(e => e.PersonId == renewable.PersonId);
Check(renewed.EndDay == renewable.EndDay + 2 * b.SeasonLength && renewed.Salary == asked.RenewalSalary, "renewal extends by whole seasons");
var hiring = New(); var hire = hiring.Observe().CoachCandidates[0]; var oldCoach = hiring.Observe().Coach;
Check(Submit(hiring, new HireCoachDecision(hire.Id)).Accepted && hiring.Observe().Coach == hire.Name && hiring.Observe().CoachCandidates.Any(x => x.Name == oldCoach), "a coach can be replaced from the market");
SnapshotValidation.Validate(hiring.Capture(), content);

// Continuous campaign: several seasons, schedules regenerate, contracts lapse, history stays bounded.
var career = New(7); Submit(career, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
var seasonsSeen = new HashSet<int>(); var departures = 0; var maxItems = 0;
Situation? departureView = null, expiryView = null;
for (var guard = 0; guard < 600 && career.Observe().Season <= 3; guard++)
{
    var v = career.Observe();
    foreach (var free in v.Offers.Where(o => o.Availability == "Available").Take(1)) Submit(career, new SponsorDecision(free.Id));
    foreach (var role in "ABCDE".Select(r => r.ToString()).Where(r => !v.People.Any(p => p.Role == r)))
        if (v.Candidates.FirstOrDefault(c => c.Role == role) is { } cover) Submit(career, new SigningDecision(cover.Id));
    var response = Submit(career, new AdvanceDecision());
    if (!response.Accepted) throw new Exception("Career blocked: " + response.Message);
    departures += career.Capture().Company.Reviews.Count(r => r.Id.StartsWith("notice:contract:", StringComparison.Ordinal) && r.Day == career.Observe().Day);
    if (departureView is null && departures > 0) departureView = career.Observe();
    if (expiryView is null && career.Observe() is var renewing && renewing.People.Any(p => p.CanRenew)) expiryView = renewing;
    seasonsSeen.Add(career.Observe().Season); maxItems = Math.Max(maxItems, career.Capture().Company.FinancialItems.Length);
    if (guard % 25 == 0) SnapshotValidation.Validate(career.Capture(), content);
}
var careerState = career.Capture();
SnapshotValidation.Validate(careerState, content);
Check(departures > 0, "contracts lapse over seasons and people leave");
// Transfer history is recorded when moves happen: a contract-end departure is a move from the company to the open market.
var departed = departureView!.Inbox.Where(r => r.Kind == "Contract" && r.Day == departureView.Day).ToArray();
Check(departed.Length > 0 && departed.All(r => r.SubjectId.Length > 0), "a contract-end notice names the departed person by stable ID");
var endings = departureView.Moves.Where(m => m.Kind == MoveKind.ContractEnd && m.Day == departureView.Day).ToArray();
Check(departed.All(r => endings.Any(m => m.PersonId == r.SubjectId && m.From == departureView.CompanyId && m.To.Length == 0 && m.CauseId == r.CauseId)),
    "every contract-end departure is a recorded move from the company to the open market, caused by the contract that ended");
var departurePortal = PortalProjection.Build(departureView);
var departureStories = PortalProjection.TransferStories(departureView);
var usSide = new JourneySide(Standing.Organization, departureView.CompanyId, departureView.Company, AssetIds.Crest(departureView.CompanyId));
Check(endings.All(m => departureStories.Count(t => t.Records.Contains(m.Id)) == 1)
    && departureStories.Where(t => t.Kind == MoveKind.ContractEnd).All(t => t.Status == StoryStatus.Confirmed && t.Source == StorySource.Simulation
        && t.From == usSide && t.To == JourneySide.FreeAgent && t.Headline.Contains(departureView.Company, StringComparison.Ordinal)
        && (t.Records.Length == 1 ? t.PortraitAssetId == AssetIds.Portrait(t.PersonId) && t.Headline.StartsWith(t.PersonName, StringComparison.Ordinal)
            : t.PersonName.Length == 0 && t.Id.StartsWith("roundup:", StringComparison.Ordinal) && t.Headline.StartsWith($"{t.Records.Length} players ", StringComparison.Ordinal))),
    "each departure is told once as the company → Free agent: players leaving together are one roundup, a single person is headlined by name");
Check(departurePortal.Transfers.SequenceEqual(departureStories.Take(PortalProjection.TransferStoryLimit)), "the Portal tells the first few of the ranked stories");
Check(departurePortal.Transfers.All(t => !departurePortal.News.Any(n => n.Id == t.Id)) && departurePortal.News.All(n => !PortalProjection.Personnel(n.Category)),
    "world news never repeats a transfer story, and personnel notices are not world news");
Check(departureStories.All(t => !t.Headline.Contains('$') && !t.Summary.Contains('$') && !(t.Headline + t.Summary).Contains("wage", StringComparison.OrdinalIgnoreCase)
        && !(t.Headline + t.Summary).Contains("fee", StringComparison.OrdinalIgnoreCase) && !(t.Headline + t.Summary).Contains("ability", StringComparison.OrdinalIgnoreCase)),
    "transfer stories carry no fee, wage or ability");
// A player who left and re-signs: the return is a new story from the open market, with the departure only as context,
// and every story told before is still told the same way.
var leaver = endings.First(m => !m.Coach);
var returned = departureView with { Moves = [new MoveRow("move:test-return", MoveKind.Signing, departureView.Day, leaver.PersonId, leaver.Name, false, leaver.Role, "",
    departureView.CompanyId, "test-return"), .. departureView.Moves] };
var returnStories = PortalProjection.TransferStories(returned);
Check(returnStories.Single(t => t.Records.Contains("move:test-return")) is { Kind: MoveKind.Signing } comeback && comeback.From == JourneySide.FreeAgent && comeback.To == usSide
    && comeback.Headline == $"{leaver.Name} re-signs with {departureView.Company}" && comeback.Summary.EndsWith("back the same day after the contract ended.", StringComparison.Ordinal)
    && departureStories.All(returnStories.Contains), "a return comes from the free-agent market with the departure as context, and the departure is still told as it was");
var expiryPortal = PortalProjection.Build(expiryView!);
Check(expiryView!.People.Where(p => p.CanRenew).All(p => !expiryPortal.Transfers.Any(t => t.PersonId == p.Id && t.Day > expiryView.Day - PortalProjection.StoryDays(expiryView.SeasonLength) && t.Kind == MoveKind.ContractEnd))
    && expiryPortal.Decisions.Any(d => d.Kind == DecisionKind.PlayerRenewal), "a contract in its renewal window is a key decision, not transfer news");
Check(expiryPortal.Decisions.Where(d => d.Kind == DecisionKind.PlayerRenewal).All(d => expiryView.People.Any(p => p.Id == d.TargetId && p.CanRenew)
        && d.SubjectAssetId == AssetIds.Portrait(d.TargetId)), "renewal decisions are typed and carry the person's portrait ID");
Check(careerState.World.Moves.All(m => m.Day >= Seasons.Start(Seasons.Of(careerState.World.Calendar.Day, b) - 1, b))
    && careerState.World.Moves.Any(m => m.Reason == MoveReason.Interim) == careerState.World.Moves.Any(m => m.Reason == MoveReason.ContractEnded && m.Kind == Personnel.HeadCoach),
    "transfer history keeps the current and the previous season, and every head coach leaving at contract end has an interim successor");
Check(seasonsSeen.Contains(4) && careerState.World.Fixtures.All(f => f.Id.StartsWith(Seasons.FixturePrefix(4), StringComparison.Ordinal)), "the campaign continues into later seasons with a regenerated schedule");
Check(careerState.World.Results.Length >= 3 * 2 * b.RivalCount, "every season's matches are played or forfeited");
Check(maxItems < 4 * b.SeasonLength * (b.MaxRoster + 3) && careerState.Company.Reviews.Length <= 200, "financial and review history stay bounded across seasons");
Check(careerState.Company.Cash == careerState.Company.LedgerBase + careerState.Company.Settlements.Sum(x => x.Delta), "archived ledger still reconciles cash");
store.Save("career", careerState); Check(Canonical.Hash(store.Load("career")) == Canonical.Hash(careerState), "late-season campaign saves and loads");

// A roster that cannot field all roles forfeits rather than blocking time.
var thin = New().Capture(); var keep = thin.Company.People.Where(p => p.Role != "A").ToImmutableArray();
thin = thin with { Company = thin.Company with { People = keep, Employment = thin.Company.Employment.Where(e => e.PersonId == thin.Company.Coach.Id || keep.Any(p => p.Id == e.PersonId)).ToImmutableArray() },
    World = thin.World with { Candidates = [] } };
var forfeiting = new Session(content.Definition, thin);
AdvanceUntilResults(forfeiting, 1);
Check(forfeiting.Observe().Results.Length == 1 && forfeiting.Observe().Results[0].Result == "Forfeit", "missing role forfeits the match");
for (var i = 0; i < 3; i++) Submit(forfeiting, new AdvanceDecision());
Check(forfeiting.Observe().Reputation <= b.Reputation - 6, "forfeit carries a visible reputation penalty");

// Information affects estimates, never the competitive resolver's true inputs or result draw.
var observedState = New().Capture(); var upgraded = observedState with { Company = observedState.Company with { Information = 95 } };
Check(Observations.Build(observedState, content.Definition).OpponentEstimate != Observations.Build(upgraded, content.Definition).OpponentEstimate && upgraded.Company.People.SequenceEqual(observedState.Company.People) && upgraded.World.Rivals.SequenceEqual(observedState.World.Rivals), "information narrows estimates without changing true assets");
// Manual and delegated identical plans have identical competitive/economic facts; authority/receipt hashes deliberately differ.
var manual = New(); var automatic = New(); automatic.Submit(new Request("a", 0, new CoachDecision(Delegation.Autonomous, Risk.Aggressive)));
var recommended = manual.Observe().Recommendation!;
manual.Submit(new Request("p", 0, new PreparationDecision(recommended.FixtureId, recommended.Execution, recommended.Opponent, recommended.Meta, recommended.Posture, recommended.Lineup)));
AdvanceUntilResults(manual, 1); AdvanceUntilResults(automatic, 1);
Check(manual.Capture().World.Results[0] with { PlanCause = "" } == automatic.Capture().World.Results[0] with { PlanCause = "" } && manual.Capture().Company.Cash == automatic.Capture().Company.Cash, "manual/delegated competitive and finance parity");
foreach (var culture in new[] { "en-US", "tr-TR", "vi-VN" })
{
    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
    var alternate = New(); alternate.Submit(coach); foreach (var advanceId in cycleIds.Take(usedCycles)) alternate.Submit(new Request(advanceId, alternate.Observe().Revision, new AdvanceDecision()));
    Check(Canonical.Hash(alternate.Capture()) == Canonical.Hash(repeat.Capture()), "culture-independent replay " + culture);
}
var permutedContent = content.Definition with { Brands = content.Definition.Brands.Reverse().ToImmutableArray(), OrganizationNames = content.Definition.OrganizationNames.Reverse().ToImmutableArray(),
    Names = new NamePool(content.Definition.Names.Given.Reverse().ToImmutableArray(), content.Definition.Names.Family.Reverse().ToImmutableArray()) };
Check(Canonical.Hash(CampaignFactory.Create(permutedContent, content.Hash, 123456789, "campaign:fixture")) == Canonical.Hash(New().Capture()), "shuffled content pools produce the same campaign");
var lowInfoState = New().Capture(); var lowInfoSession = new Session(content.Definition, lowInfoState with { Company = lowInfoState.Company with { Information = 30 } });
lowInfoSession.Submit(new Request("a", 0, new CoachDecision(Delegation.Autonomous, Risk.Conservative))); var escalationHash = Canonical.Hash(lowInfoSession.Capture());
Check(!lowInfoSession.Submit(new Request("go", 1, new AdvanceDecision())).Accepted && Canonical.Hash(lowInfoSession.Capture()) == escalationHash, "uncertain coach escalates without partial mutation");
var unplanned = New();
AdvanceUntilResults(unplanned, 1);
Check(unplanned.Observe().Results.Length == 0 && unplanned.Observe().Day == schedule[0].Day, "manual company stops on match day for a plan");
Check(!Submit(unplanned, new AdvanceDecision()).Accepted, "match day cannot be skipped while a lineup is possible");

// Genre-shell projections: inbox, tasks, fixtures and head-to-head come from authoritative state only.
var shell = New(); var start = shell.Observe();
Check(start.Inbox.Count(r => r.Kind == "Offer") == start.Offers.Count(o => o.Origin == "Inbound") && start.Inbox.All(r => !r.Id.StartsWith("decision:", StringComparison.Ordinal)), "inbox carries world messages, never the player's own decisions");
Check(start.Fixtures.Length == 2 * b.RivalCount && start.Rivals.Length == b.RivalCount && start.Rivals.All(r => r.Remaining == 2 && r.Wins + r.Losses == 0), "season fixtures and head-to-head rows");
var startTasks = PortalTasks.Build(start);
Check(startTasks.Any(t => t.Section == ShellSections.Competition && t.Urgency == TaskUrgency.Due) && startTasks.Count(t => t.Section == ShellSections.Commercial) == start.Offers.Count(o => o.Availability == "Available"), "portal tasks list match preparation and every open offer");
Check(startTasks.Select(t => t.Urgency).SequenceEqual(startTasks.Select(t => t.Urgency).Order()) && PortalTasks.Blocker(start) is null, "tasks order by urgency; nothing blocks on day one");
Submit(shell, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
Check(!PortalTasks.Build(shell.Observe()).Any(t => t.Section == ShellSections.Competition), "an autonomous coach removes the preparation task");
var matchDay = New(); AdvanceUntilResults(matchDay, 1);
Check(PortalTasks.Blocker(matchDay.Observe()) is { Urgency: TaskUrgency.Blocking }, "an unplanned match day blocks Continue and routes to Competition");
var played = New(); Submit(played, new CoachDecision(Delegation.Autonomous, Risk.Balanced)); AdvanceUntilResults(played, 1);
var afterMatch = played.Observe();
Check(afterMatch.Inbox[0].Kind == "Match" || afterMatch.Inbox.Any(r => r.Kind == "Match"), "match results reach the inbox");
Check(afterMatch.Rivals.Sum(r => r.Wins + r.Losses) == 1 && afterMatch.Fixtures.Count(f => f.Result.Length > 0) == 1, "head-to-head and fixtures record the result");
Check(ShellSections.ForInbox("Offer") == ShellSections.Commercial && ShellSections.ForInbox("Renewal") == ShellSections.Squad && ShellSections.ForInbox("Match") == ShellSections.Competition, "inbox kinds route to their owning section");

// Portal read model: everything comes from the observation, identities and routes use stable IDs.
var portal = PortalProjection.Build(start);
Check(portal.Company.Id == start.CompanyId && portal.Company.Name == start.Company && portal.Company.CrestAssetId == AssetIds.Crest(start.CompanyId), "portal company identity and crest come from the stable company ID");
Check(portal.Decisions.Select(d => d.Id).SequenceEqual(startTasks.Select(t => t.Id)) && portal.Decisions.All(d => d.Section.Length > 0 && d.Subject.Length > 0),
    "portal decisions are the typed task list with a section and subject");
Check(portal.Decisions.Where(d => d.Category == DecisionCategory.Commercial).All(d => start.Offers.Any(o => o.Id == d.TargetId)) &&
    portal.Decisions.Where(d => d.Category == DecisionCategory.Competition).All(d => d.TargetId == start.NextFixtureId), "decision targets are stable record IDs, not display text");
Check(portal.NextMatch is { } next && next.FixtureId == start.NextFixtureId && next.Opponent.Id == start.OpponentId && next.Opponent.CrestAssetId == AssetIds.Crest(start.OpponentId)
    && next.Us.Id == start.CompanyId && next.MatchNumber == 1 && next.SeasonMatches == start.Fixtures.Length, "next match binds fixture, opponent and crests by ID");
Check(portal.News.Length == Math.Min(PortalProjection.NewsLimit, start.Inbox.Count(r => !PortalProjection.Personnel(r.Kind))) && portal.News.All(n => start.Inbox.Any(r => r.Id == n.Id))
    && portal.News.Where(n => n.Category == "Offer").All(n => n.SubjectId.StartsWith("brand:", StringComparison.Ordinal) && n.ImageAssetId == AssetIds.BrandLogo(n.SubjectId)),
    "news preview is the inbox with subjects and image IDs resolved from records");
Check(portal.Decisions.Single(d => d.Category == DecisionCategory.Competition).Kind == DecisionKind.MatchPreparation
    && portal.Decisions.Where(d => d.Category == DecisionCategory.Commercial).All(d => d.Kind == DecisionKind.SponsorOffer && start.Offers.Any(o => o.Id == d.TargetId && d.SubjectAssetId == AssetIds.BrandLogo(o.BrandId))),
    "decisions carry their typed kind and the subject's asset ID from records");
// Day one: nobody has moved, so transfer history and news are empty; free agents on the market are not stories.
Check(start.Moves.IsEmpty && shell.Capture().World.Moves.IsEmpty && portal.Transfers.IsEmpty && !start.Candidates.IsEmpty, "day one has a free-agent market and no transfer news");
// A signing and a head-coach appointment, made as decisions, are recorded with their direction when they happen; the
// appointment also records the replaced coach leaving.
var mover = New(); var moverStart = mover.Observe();
var signed = moverStart.Candidates[0]; var appointed = moverStart.CoachCandidates[0]; var us = moverStart.CompanyId;
Check(mover.Submit(new Request("sign-one", moverStart.Revision, new SigningDecision(signed.Id, 2))).Accepted
    && mover.Submit(new Request("hire-one", mover.Observe().Revision, new HireCoachDecision(appointed.Id))).Accepted, "a signing and an appointment are committed");
var moved = mover.Observe(); var movedPortal = PortalProjection.Build(moved);
Check(moved.Moves.Select(m => (m.Kind, m.PersonId, m.From, m.To, m.Coach)).SequenceEqual([(MoveKind.Appointment, appointed.Id, "", us, true),
        (MoveKind.Replacement, moverStart.CoachContract.Id, us, "", true), (MoveKind.Signing, signed.Id, "", us, false)])
    && moved.Moves[2].Id == "move:sign-one:" + signed.Id && moved.Moves[2].CauseId == "sign-one" && moved.Moves[2].Name == signed.Name && moved.Moves[2].Role == signed.Role
    && moved.Moves[1].Name == moverStart.Coach && moved.Moves[0].Name == appointed.Name && moved.Moves[0].Role.Length == 0 && moved.Moves.Take(2).All(m => m.CauseId == "hire-one"),
    "moves are recorded newest first with who, from where and to where: free agent → company for the signing and the appointment, company → free agent for the replaced coach");
var usView = new JourneySide(Standing.Organization, us, moved.Company, AssetIds.Crest(us));
Check(movedPortal.Transfers.Select(t => t.Kind).SequenceEqual([MoveKind.Appointment, MoveKind.Replacement, MoveKind.Signing])
    && movedPortal.Transfers[0] is { Significance: StorySignificance.HeadCoach, Section: ShellSections.Staff, Coach: true } leadStory && leadStory.TargetId == appointed.Id
    && leadStory.Headline == $"{appointed.Name} named head coach of {moved.Company}" && leadStory.From == JourneySide.FreeAgent && leadStory.To == usView
    && movedPortal.Transfers[1] is { Significance: StorySignificance.HeadCoach } replacedStory && replacedStory.Headline == $"{moverStart.Coach} leaves {moved.Company}"
    && replacedStory.Summary == $"Head coach; replaced by {appointed.Name}." && replacedStory.From == usView && replacedStory.To == JourneySide.FreeAgent
    && movedPortal.Transfers[2] is { Significance: StorySignificance.Player, Section: ShellSections.Squad, Coach: false } secondStory && secondStory.TargetId == signed.Id
    && secondStory.Headline == $"{signed.Name} signs with {moved.Company}" && secondStory.Summary.StartsWith($"Role {signed.Role} player", StringComparison.Ordinal)
    && secondStory.From == JourneySide.FreeAgent && secondStory.To == usView,
    "head-coach stories lead the player story; each tells its journey and opens its person in the owning section");
Check(movedPortal.Transfers.All(t => !t.Headline.Contains('$') && !t.Summary.Contains('$') && !t.Summary.Contains(Currency.Format(signed.Fee), StringComparison.Ordinal))
    && movedPortal.Transfers.All(t => !movedPortal.News.Any(n => n.Id == t.Id)), "own moves state no terms and are not world news");
// Reloading tells the same stories: transfer history is saved (schema 4), so nothing is repeated or lost.
store.Save("moves", mover.Capture());
var savedMoves = System.Text.Json.JsonDocument.Parse(File.ReadAllText(store.PrimaryPath("moves"))).RootElement;
Check(savedMoves.GetProperty("Schema").GetInt32() == SnapshotStore.Schema && SnapshotStore.Schema == 4
    && savedMoves.GetProperty("Snapshot").GetProperty("World").GetProperty("Moves").GetArrayLength() == 3, "transfer history is part of the save (schema 4)");
var reloaded = new Session(content.Definition, store.Load("moves"));
Check(PortalProjection.Build(reloaded.Observe()).Transfers.SequenceEqual(movedPortal.Transfers) && PortalProjection.Build(mover.Observe()).Transfers.SequenceEqual(movedPortal.Transfers),
    "transfer news survives save and load unchanged, and re-observing does not regenerate it");
File.WriteAllText(store.PrimaryPath("moves"), File.ReadAllText(store.PrimaryPath("moves")).Replace("\"Schema\":4", "\"Schema\":3", StringComparison.Ordinal));
try { store.Load("moves"); Check(false, "a schema-3 save is rejected"); } catch (InvalidDataException) { Check(true, "a schema-3 save is rejected"); }
// Transfer history is validated: a move in a direction its reason does not allow, or from the future, is rejected.
var recorded = mover.Capture();
// World.Moves is in recorded order: the signing, the replaced coach, the appointment.
foreach (var (label, index, broken) in new (string, int, Move)[] { ("a signing from the company itself", 0, recorded.World.Moves[0] with { Origin = recorded.Company.Id }),
    ("a move dated in the future", 0, recorded.World.Moves[0] with { Day = recorded.World.Calendar.Day + 1 }), ("a coach recorded as a player", 1, recorded.World.Moves[1] with { Role = "A" }) })
{
    try { SnapshotValidation.Validate(recorded with { World = recorded.World with { Moves = recorded.World.Moves.SetItem(index, broken) } }, content); Check(false, "history validation: " + label); }
    catch (InvalidDataException) { Check(true, "history validation: " + label); }
}
// History is never rewritten by later moves or by the current rosters and markets: a player signed, released and
// re-signed has three stories, each as it was when it happened, and keeps them after their listing has gone.
var career2 = New(); Submit(career2, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
var first = career2.Observe(); var journeyman = first.Candidates.First(c => first.People.Any(p => p.Role == c.Role));
Check(Submit(career2, new SigningDecision(journeyman.Id)).Accepted, "a player whose role is covered is signed");
var afterSigning = PortalProjection.TransferStories(career2.Observe());
Submit(career2, new AdvanceDecision());
var releaseDay = career2.Observe().Day;
Check(releaseDay > first.Day && Submit(career2, new ReleaseDecision(journeyman.Id)).Accepted, "the player is released on a later day");
var afterRelease = PortalProjection.TransferStories(career2.Observe());
var listed = career2.Capture();
var unlisted = listed with { World = listed.World with { Candidates = listed.World.Candidates.RemoveAll(x => x.Person.Id == journeyman.Id) } };
Check(PortalProjection.TransferStories(Observations.Build(unlisted, content.Definition)).SequenceEqual(afterRelease),
    "a person who has left every current record is still named, and the stories are unchanged");
Submit(career2, new AdvanceDecision());
var returnDay = career2.Observe().Day;
Check(returnDay > releaseDay && Submit(career2, new SigningDecision(journeyman.Id)).Accepted, "the released player re-signs on a later day");
var afterReturn = PortalProjection.TransferStories(career2.Observe());
var journey = afterReturn.Where(t => t.PersonId == journeyman.Id).ToArray();
Check(afterSigning.All(afterRelease.Contains) && afterRelease.All(afterReturn.Contains), "every story told before a later move is still told exactly the same way");
Check(journey.Select(t => (t.Kind, t.Day, t.From.Standing, t.To.Standing)).SequenceEqual([(MoveKind.Signing, returnDay, Standing.FreeAgent, Standing.Organization),
        (MoveKind.Release, releaseDay, Standing.Organization, Standing.FreeAgent), (MoveKind.Signing, first.Day, Standing.FreeAgent, Standing.Organization)])
    && journey[0].Headline == $"{journeyman.Name} re-signs with {first.Company}" && journey[0].Summary.Contains("; back ", StringComparison.Ordinal) && journey[0].Summary.EndsWith(" after being released.", StringComparison.Ordinal)
    && journey[1].Headline == $"{journeyman.Name} released by {first.Company}" && journey[2].Headline == $"{journeyman.Name} signs with {first.Company}",
    "signed, released and re-signed: three stories, each with the journey of its own day");
SnapshotValidation.Validate(career2.Capture(), content);
// Between organizations (read model only: rivals have no rosters in the current rules): A → B, then B → the company. The
// first story keeps A → B after the second move.
var (rivalA, rivalB) = (moved.Rivals[0], moved.Rivals[1]);
var aToB = new MoveRow("move:test-a-b", MoveKind.Signing, moved.Day - 3, signed.Id, signed.Name, false, signed.Role, rivalA.Id, rivalB.Id, "test-a-b");
var bToUs = new MoveRow("move:test-b-us", MoveKind.Signing, moved.Day, signed.Id, signed.Name, false, signed.Role, rivalB.Id, us, "test-b-us");
var early = PortalProjection.TransferStories(moved with { Moves = [aToB] });
var later = PortalProjection.TransferStories(moved with { Moves = [bToUs, aToB] });
Check(early.Single() is { From.Standing: Standing.Organization, To.Standing: Standing.Organization } earlyStory && earlyStory.From.OrganizationId == rivalA.Id
    && earlyStory.To.OrganizationId == rivalB.Id && earlyStory.Headline == $"{signed.Name} joins {rivalB.Name}" && earlyStory.Summary.EndsWith($"moved directly from {rivalA.Name}.", StringComparison.Ordinal) && later.Contains(earlyStory)
    && later.Single(t => t.Id == bToUs.Id) is { } secondMove && secondMove.From.OrganizationId == rivalB.Id && secondMove.To == usView,
    "a move between organizations keeps its direction, and a later move never rewrites it");
// Unknown organizations are shown as unknown, never guessed; a move with no organization the company can name is not told.
Check(PortalProjection.TransferStories(moved with { Moves = [moved.Moves[2] with { From = "rival:unknown" }] }).Single() is { From.Standing: Standing.Unknown } unknownOrigin
    && unknownOrigin.From.OrganizationId == "rival:unknown" && unknownOrigin.To == usView && unknownOrigin.Headline == $"{signed.Name} joins {moved.Company}"
    && PortalProjection.TransferStories(moved with { Moves = [moved.Moves[2] with { From = "rival:unknown", To = "" }] }).IsEmpty,
    "an organization the company cannot name stays unknown; a move with no named organization is not told");
// Editorial window and cap.
var stale = moved with { Day = moved.Day + PortalProjection.StoryDays(moved.SeasonLength) };
Check(PortalProjection.Build(stale).Transfers.IsEmpty && PortalProjection.Build(stale with { Day = stale.Day - 1 }).Transfers.Length == 3, "a story stays news for half a season");
var crowded = moved with { Moves = [.. Enumerable.Range(0, 9).Select(i => moved.Moves[i % 3] with { Id = "test:" + i, Day = moved.Day - i % 3 })] };
Check(PortalProjection.Build(crowded).Transfers is var topStories && topStories.Length == PortalProjection.TransferStoryLimit && topStories.Select(t => t.Id).Distinct().Count() == topStories.Length
    && topStories.Zip(topStories.Skip(1)).All(p => p.First.Significance > p.Second.Significance || p.First.Significance == p.Second.Significance && p.First.Day >= p.Second.Day),
    "many moves become a few distinct stories, ranked by significance then recency");
// The operating horizon left the Portal but stays a read model over the same observation.
var horizon = HorizonProjection.Build(start);
Check(horizon.Length == HorizonProjection.Weeks && horizon[0].Current && horizon.Skip(1).All(w => !w.Current)
    && horizon.Zip(horizon.Skip(1)).All(p => p.Second.FirstDay == p.First.LastDay + 1), "four consecutive season weeks, the first is current");
var horizonEvents = horizon.SelectMany(w => w.Events).ToArray();
Check(horizonEvents.Where(e => e.Kind == HorizonKind.Match).All(e => start.Fixtures.Any(f => f.Id == e.TargetId && f.Day == e.Day))
    && horizonEvents.Where(e => e.Kind == HorizonKind.OfferDeadline).All(e => e.Count > 1
        ? e.TargetId.Length == 0 && start.Offers.Count(o => o.Availability == "Available" && o.Deadline == e.Day) == e.Count
        : start.Offers.Any(o => o.Id == e.TargetId && o.Deadline == e.Day && o.Name == e.Subject))
    && horizonEvents.Where(e => e.Kind == HorizonKind.Receipt).All(e => e.Amount == start.Bills.Where(x => x.Incoming && x.DueDay == e.Day).Sum(x => x.Remaining)),
    "horizon events come only from fixtures, offers and scheduled receipts, with typed subject and amount");
Check(horizonEvents.Where(e => e.Kind == HorizonKind.Match).All(e => start.Fixtures.Any(f => f.Id == e.TargetId && f.Opponent == e.Subject))
    && horizonEvents.GroupBy(e => (e.Day, e.Kind, e.Section)).All(g => g.Count() == 1), "one event per day, kind and section; grouped deadlines keep the count");
var offerNews = portal.News.Where(n => n.Category == "Offer").ToArray();
bool OfferHeadline(PortalNews n)
{
    var cause = start.Inbox.Single(r => r.Id == n.Id).CauseId;
    if (start.Offers.FirstOrDefault(o => o.Id == cause) is not { } offer) return true;
    return n.Headline.StartsWith(offer.Name, StringComparison.Ordinal) && !n.Headline.Contains('$')
        && n.Detail.StartsWith(Currency.Format(offer.Payment) + " a week", StringComparison.Ordinal);
}
Check(offerNews.Length > 0 && offerNews.All(OfferHeadline), "an open offer reads as a headline from its typed record; terms move to the summary");
var distressPortal = PortalProjection.Build(start with { Status = "Distress" });
Check(distressPortal.Decisions[0].Category == DecisionCategory.Finance && distressPortal.Decisions[0].Urgency == TaskUrgency.Blocking
    && distressPortal.Decisions.Length == portal.Decisions.Length + 1, "distress is the first, blocking decision and counts toward the total");
Check(portal.Season.Won == 0 && portal.Season.Lost == 0 && portal.Season.ToPlay == start.Fixtures.Length && portal.Season.Recent.IsEmpty, "season summary on day one");
Check(portal.Finance.Cash == start.Cash && portal.Finance.Forecast == start.Forecast && portal.Finance.ReceiptsAhead >= 0 && portal.Finance.PaymentsAhead > 0 && portal.Finance.Arrears == 0,
    "money snapshot: known cash, estimated forecast and payments");
var portalAfter = PortalProjection.Build(afterMatch);
Check(portalAfter.Season.Won + portalAfter.Season.Lost == 1 && portalAfter.Season.Recent.Length == 1 && portalAfter.News.Any(n => n.Category == "Match" && n.SubjectId.StartsWith("rival:", StringComparison.Ordinal)
    && n.ImageAssetId == AssetIds.Crest(n.SubjectId)), "after a match the record and a crest-backed result story appear");
var quiet = start with { Inbox = [], Offers = [], NextFixtureId = "", OpponentId = "", Fixtures = [], CommittedPlan = null, Candidates = [], CoachCandidates = [] };
var quietPortal = PortalProjection.Build(quiet);
Check(quietPortal.News.IsEmpty && quietPortal.NextMatch is null && quietPortal.Time.Phase == SeasonPhase.Complete,
    "empty states stay empty: no invented news, opponent or matches");
Check(quietPortal.Transfers.IsEmpty, "no completed move gives empty transfer news, nothing invented");
Check(HorizonProjection.Build(quiet).All(w => w.Events.All(e => e.Kind != HorizonKind.Match)), "an empty schedule gives no horizon matches");
Check(AssetIds.ForSubject("", "Season") == AssetIds.NewsFallback("Season") && AssetIds.ForSubject("person:g00001", "Contract") == AssetIds.Portrait("person:g00001"),
    "asset IDs derive from the subject's stable ID kind, with a category fallback");
var longName = new string('N', 60);
var named = New(company: longName).Observe();
Check(PortalProjection.Build(named).Company.Name == longName && PortalProjection.Build(named).Company.CrestAssetId == AssetIds.Crest(named.CompanyId), "a long player company name passes through; the crest key never uses the name");

// Recomputed checksums are not a substitute for semantic validation.
void RejectSave(string name, Func<SaveEnvelope, SaveEnvelope> change)
{
    store.Save(name, loaded);
    var envelope = StrictJson.Read<SaveEnvelope>(File.ReadAllBytes(store.PrimaryPath(name)));
    envelope = change(envelope); envelope = envelope with { Checksum = "" };
    envelope = envelope with { Checksum = Canonical.Digest(Canonical.Json(envelope)) };
    File.WriteAllText(store.PrimaryPath(name), Canonical.Json(envelope));
    var currentHash = Canonical.Hash(continued.Capture());
    Check(!continued.Load(name).Accepted && Canonical.Hash(continued.Capture()) == currentHash, "invalid save leaves session intact: " + name);
}
RejectSave("schema", e => e with { Schema = 1 });
RejectSave("schema-v2", e => e with { Schema = 2 });
RejectSave("content", e => e with { ContentHash = new string('0', 64) });
RejectSave("cursor", e => e with { Snapshot = e.Snapshot with { World = e.Snapshot.World with { Calendar = e.Snapshot.World.Calendar with { Phase = (Phase)99 } } } });
RejectSave("cash", e => e with { Snapshot = e.Snapshot with { Company = e.Snapshot.Company with { Cash = e.Snapshot.Company.Cash + 1 } } });
RejectSave("owner", e => e with { Snapshot = e.Snapshot with { Company = e.Snapshot.Company with { People = e.Snapshot.Company.People.Add(e.Snapshot.Company.People[0]) } } });
RejectSave("receipts", e => e with { Snapshot = e.Snapshot with { Company = e.Snapshot.Company with { FinanceReceipts = [] } } });
RejectSave("schedule", e => e with { Snapshot = e.Snapshot with { World = e.Snapshot.World with { Fixtures = e.Snapshot.World.Fixtures.RemoveAt(0) } } });
RejectSave("brand", e => e with { Snapshot = e.Snapshot with { Company = e.Snapshot.Company with { Sponsors = e.Snapshot.Company.Sponsors.Select(x => x with { BrandId = "brand:unknown" }).ToImmutableArray() } } });
store.Save("checksum", loaded); File.AppendAllText(store.PrimaryPath("checksum"), "!");
Check(!continued.Load("checksum").Accepted, "trailing-corruption rejection");
store.Save("locked", loaded); var lockBytes = File.ReadAllBytes(store.PrimaryPath("locked"));
using (var held = new FileStream(store.PrimaryPath("locked") + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    Check(!continued.Save("locked").Accepted, "exclusive slot lock rejects concurrent writer");
Check(File.ReadAllBytes(store.PrimaryPath("locked")).SequenceEqual(lockBytes), "failed write preserves primary bytes");
Check(!continued.Save("../escape").Accepted, "save slot traversal rejected");
try { StrictJson.Read<Content>(System.Text.Encoding.UTF8.GetBytes("{\"Schema\":1,\"Schema\":2}")); Check(false, "duplicate key rejection"); } catch (InvalidDataException) { Check(true, "duplicate key rejection"); }
try { ContentLoader.Validate(content.Definition with { Brands = content.Definition.Brands.Add(content.Definition.Brands[0]) }); Check(false, "duplicate content IDs"); } catch (InvalidDataException) { Check(true, "duplicate content IDs"); }
try { ContentLoader.Validate(content.Definition with { Balance = b with { SeasonLength = b.FirstMatchDay + 2 } }); Check(false, "schedule must fit the season"); } catch (InvalidDataException) { Check(true, "schedule must fit the season"); }

// Distress and recovery.
var distressContent = content.Definition with { Balance = b with { StartingCash = 15000 } };
var distressed = new Session(distressContent, CampaignFactory.Create(distressContent, "development-distress-fixture", 42, "campaign:distress"));
distressed.Submit(new Request("coach", 0, new CoachDecision(Delegation.Autonomous, Risk.Balanced)));
for (var i = 0; i < 6 && distressed.Observe().Status != "Distress"; i++) Submit(distressed, new AdvanceDecision());
Check(distressed.Observe().Status == "Distress", "low liquidity reaches a material distress checkpoint");
var missed = distressed.Capture().Company.FinancialItems.Where(i => i.MissedDay is not null).Select(i => i.Id).ToArray();
Check(missed.Length > 0, "unpaid items retain original missed date");
var rescue = distressed.Observe().Offers.FirstOrDefault(o => o.Availability == "Available");
Check(rescue is not null && Submit(distressed, new SponsorDecision(rescue.Id)).Accepted, "recovery commits a sponsor opportunity and its load");
for (var tries = 0; tries < 8 && distressed.Observe().Results.Length < 1; tries++)
{ var v = distressed.Observe(); Check(distressed.Submit(new Request("resume:" + tries, v.Revision, new AdvanceDecision())).Accepted, "distress resumes without repeated completed phases"); }
Check(distressed.Capture().Company.FinancialItems.Where(i => missed.Contains(i.Id)).All(i => i.MissedDay is not null), "recovery retains late-payment history after settlement");

// Rules v3 wave 1 (docs/10-vertical-slice/RULES_V3_INCENTIVES.md): a fixed running cost, the registered-roster minimum,
// the sponsor termination clause, the season-end reputation pull and a bounded predictability window.
var running = New(); Submit(running, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
var runningStart = running.Capture().Company;
Check(b.OperatingCost > 0 && running.Observe().OperatingCost == b.OperatingCost && running.Observe().MinimumRoster == b.MinimumRoster,
    "the observation states the running cost and the roster minimum");
Check(Finance.Forecast(runningStart, 1, b.OperatingCost) == Finance.Forecast(runningStart, 1, 0) - 8 * b.OperatingCost,
    "the 7-day forecast carries the running cost for every day of the window");
Submit(running, new AdvanceDecision());
var runningDay = running.Observe().Day; var runningItems = running.Capture().Company.FinancialItems;
Check(runningDay > 1 && Enumerable.Range(1, runningDay - 1).All(d => runningItems.Any(x => x.Id == $"{Finance.OperationsCause}:day:{d}" && x.Amount == b.OperatingCost && !x.Incoming && x.Remaining == 0))
    && !runningItems.Any(x => x.CauseId == Finance.OperationsCause && x.DueDay > runningDay), "the running cost falls due and is paid every day, whatever the headcount");
// Five registered players, one per role: one below the league minimum.
var fiveState = New().Capture();
var fivePeople = fiveState.Company.People.GroupBy(p => p.Role).Select(g => g.First()).ToImmutableArray();
fiveState = fiveState with { Company = fiveState.Company with { People = fivePeople,
    Employment = fiveState.Company.Employment.Where(e => e.PersonId == fiveState.Company.Coach.Id || fivePeople.Any(p => p.Id == e.PersonId)).ToImmutableArray() } };
var five = new Session(content.Definition, fiveState);
var fiveTask = PortalTasks.Build(five.Observe()).FirstOrDefault(t => t.Kind == DecisionKind.RosterShortfall);
Check(fivePeople.Length == b.MinimumRoster - 1 && fiveTask is { Urgency: TaskUrgency.Blocking, Section: ShellSections.Squad, Category: DecisionCategory.Squad } && PortalTasks.Blocker(five.Observe()) is null,
    "a roster below the league minimum is a blocking Squad decision that does not stop time");
Check(!PortalTasks.Build(start).Any(t => t.Kind == DecisionKind.RosterShortfall), "a new company meets the roster minimum");
Submit(five, new CoachDecision(Delegation.Autonomous, Risk.Balanced)); AdvanceUntilResults(five, 1);
var fiveResult = five.Capture().World.Results[0]; var fiveFixture = five.Capture().World.Fixtures.Single(f => f.Id == fiveResult.FixtureId);
var fine = five.Capture().Company.FinancialItems.SingleOrDefault(x => x.Id == "fine:" + fiveResult.Id);
Check(fine is { Incoming: false } && fine.Amount == Finance.ShortfallFine(fiveFixture, b) && fine.Amount > 0 && fine.DueDay == fiveResult.Day + 1
    && !five.Capture().Company.FinancialItems.Any(x => x.Id == "prize:" + fiveResult.Id), "a match below the minimum withholds the prize and owes the league its fine");
var fineNotice = five.Observe().Inbox.SingleOrDefault(r => r.Id.StartsWith("notice:roster:", StringComparison.Ordinal));
Check(fineNotice is { Kind: "League" } && ShellSections.ForInbox(fineNotice.Kind) == ShellSections.Squad && fineNotice.Text.Contains(Currency.Format(fine!.Amount), StringComparison.Ordinal),
    "the fine reaches the inbox with its amount and routes to Squad");
SnapshotValidation.Validate(five.Capture(), content);
// USD relabel (nominal 1:1): a save written before it stores notice amounts as "1,234.50 CU". It loads with the same state
// hash and the same amounts, its stored text is never rewritten, and the inbox reads it in dollars.
var legacyState = five.Capture() with { Company = five.Capture().Company with { Reviews = five.Capture().Company.Reviews.Add(new ReviewEntry(
    "notice:roster:legacy", five.Observe().Day, "legacy", "League fine vs Legacy Five: 5 registered players of the required 6. A 1,234.50 CU fine is due tomorrow.")) } };
store.Save("legacy-currency", legacyState);
var legacy = new Session(content.Definition, five.Capture(), store);
Check(legacy.Load("legacy-currency").Accepted && Canonical.Hash(legacy.Capture()) == Canonical.Hash(legacyState)
    && File.ReadAllText(store.PrimaryPath("legacy-currency")).Contains("1,234.50 CU", StringComparison.Ordinal),
    "a pre-relabel save loads unchanged: same state hash, stored text kept");
var legacyView = legacy.Observe(); var liveFive = five.Observe();
Check(legacyView.Cash == liveFive.Cash && legacyView.Forecast == liveFive.Forecast && legacyView.Bills.SequenceEqual(liveFive.Bills)
    && legacyView.People.Select(p => (p.Salary, p.RenewalSalary, p.ExitCost)).SequenceEqual(liveFive.People.Select(p => (p.Salary, p.RenewalSalary, p.ExitCost))),
    "money in a loaded save is numerically unchanged by the relabel");
Check(legacyView.Inbox.Single(r => r.Id == "notice:roster:legacy").Text.Contains("A $1,234.50 fine", StringComparison.Ordinal)
    && legacyView.Inbox.All(r => !System.Text.RegularExpressions.Regex.IsMatch(r.Text, @"\bCU\b")) && legacyView.Review.All(r => !r.Contains(" CU", StringComparison.Ordinal)),
    "stored pre-relabel amounts reach the inbox in dollars");
var fullRoster = played.Capture();
Check(!fullRoster.Company.FinancialItems.Any(x => x.Id.StartsWith("fine:", StringComparison.Ordinal))
    && fullRoster.World.Results[0].Won == fullRoster.Company.FinancialItems.Any(x => x.Id == "prize:" + fullRoster.World.Results[0].Id), "a full roster owes no fine and keeps a win's prize");
// The termination clause: two forfeits in a season let the sponsor end the agreement.
var clause = new Session(content.Definition, thin);
var starter = clause.Capture().Company.Sponsors.Single();
Check(starter.ForfeitLimit == b.SponsorForfeitLimit && b.SponsorForfeitLimit > 1, "agreements carry the termination clause");
AdvanceUntilResults(clause, b.SponsorForfeitLimit - 1);
Check(clause.Capture().Company.Sponsors.Single().EndDay == starter.EndDay, "fewer forfeits than the clause allows leave the agreement in force");
AdvanceUntilResults(clause, b.SponsorForfeitLimit);
var ended = clause.Capture().Company.Sponsors.Single(); var lastForfeit = clause.Capture().World.Results[^1];
Check(clause.Capture().World.Results.All(r => r.PlanCause == "forfeit") && ended.EndDay == lastForfeit.Day
    && !clause.Capture().Company.FinancialItems.Any(x => x.CauseId == starter.Id && x.Incoming && x.DueDay > lastForfeit.Day)
    && clause.Capture().World.Brands.Any(x => x.BrandId == starter.BrandId && x.AvailableFromDay == lastForfeit.Day + b.RejectCooldown),
    "the forfeit that reaches the clause ends the agreement: future payments are cancelled and the brand cools off");
var termination = clause.Observe().Inbox.SingleOrDefault(r => r.Id.StartsWith("notice:sponsor:", StringComparison.Ordinal));
Check(termination is { Kind: "Commercial" } && termination.SubjectId == starter.BrandId, "the termination reaches the inbox, about its brand");
SnapshotValidation.Validate(clause.Capture(), content);
// Season end: reputation moves part of the way toward the level the season's record earns.
Check(Simulation.ReputationTarget(0, 8, b) == b.ReputationTargetLow && Simulation.ReputationTarget(8, 8, b) == b.ReputationTargetHigh
    && Enumerable.Range(0, 8).All(wins => Simulation.ReputationTarget(wins, 8, b) <= Simulation.ReputationTarget(wins + 1, 8, b)), "the reputation target rises with the season's wins");
var closing = New().Capture(); var seasonEnd = Seasons.End(1, b);
var seasonRecord = closing.World.Fixtures.Select((f, i) => new CompetitiveOutcome("result:" + f.Id, f.Id, f.Day, f.RivalId, i < 2, Posture.Balanced, 500_000, 0, 0, 0, 0, 0, f.Importance, "test")).ToImmutableArray();
closing = closing with { World = closing.World with { Results = seasonRecord, Calendar = new Calendar(seasonEnd, Phase.Review, (seasonEnd - 1L) * 11 + (int)Phase.Review - 1) },
    Company = closing.Company with { Reputation = 60, FinanceReceipts = [.. seasonRecord.Select(r => r.Id)], CommercialReceipts = [.. seasonRecord.Select(r => r.Id)] } };
var rolled = Simulation.Step(closing, content.Definition);
var seasonTarget = Simulation.ReputationTarget(2, seasonRecord.Length, b);
Check(seasonTarget < 60 && rolled.Company.Reputation == 60 + (int)Numbers.Divide((long)(seasonTarget - 60) * b.ReputationRegression, 100)
    && rolled.Company.Reviews.Any(r => r.Id.StartsWith("notice:season:2:", StringComparison.Ordinal) && r.Text.Contains("settles toward " + seasonTarget, StringComparison.Ordinal)),
    "a losing season pulls reputation toward the level its record earns, and the season notice says so");
// Predictability: a rival reads only the company's latest results for a repeated posture.
int AdaptationAfter(int prior)
{
    var s0 = New().Capture();
    var old = Enumerable.Range(0, prior).Select(i => new CompetitiveOutcome($"result:old:{i}", $"fixture:s0:{i:D2}", 0, s0.World.Rivals[0].Id, false, Posture.Balanced,
        500_000, 0, 0, 0, 0, 0, 1, "test")).ToImmutableArray();
    var repeated = new Session(content.Definition, s0 with { World = s0.World with { Results = old } });
    Submit(repeated, new CoachDecision(Delegation.Autonomous, Risk.Balanced)); AdvanceUntilResults(repeated, prior + 1);
    return repeated.Capture().World.Results[^1].Adaptation;
}
Check(AdaptationAfter(b.PredictabilityWindow) == AdaptationAfter(b.PredictabilityWindow + 9) && AdaptationAfter(0) > AdaptationAfter(b.PredictabilityWindow),
    "repeating a posture is read from the latest results only: older history no longer compounds");
try { ContentLoader.Validate(content.Definition with { Balance = b with { MinimumRoster = b.RosterSize + 1 } }); Check(false, "roster minimum above the starting roster"); }
catch (InvalidDataException) { Check(true, "a new company must meet the roster minimum"); }
try { ContentLoader.Validate(content.Definition with { Balance = b with { OperatingCost = -1 } }); Check(false, "negative running cost"); }
catch (InvalidDataException) { Check(true, "the running cost cannot be negative"); }

for (ulong seed = 1; seed <= 16; seed++)
{
    Session Seeded() => New(seed);
    var left = Seeded(); var right = Seeded();
    foreach (var request in new[] { new Request("a", 0, new CoachDecision(Delegation.Autonomous, Risk.Aggressive)), new Request("b", 1, new AdvanceDecision()), new Request("c", 2, new AdvanceDecision()) })
    { left.Submit(request); right.Observe(); right.Submit(request); Check(Canonical.Hash(left.Capture()) == Canonical.Hash(right.Capture()), $"seed {seed} transition {request.Id}"); }
}
Console.WriteLine($"INTEGRATION PASS {count} phaseBoundaries={boundaries} hash={Canonical.Hash(session.Capture())}");
