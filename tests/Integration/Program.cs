using ManagementGame.Application;
using ManagementGame.Domain;
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
Check(Finance.Consume(company, result, fixture) == company && Commercial.Consume(company, result, b) == company, "consumer idempotency");
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
for (var guard = 0; guard < 600 && career.Observe().Season <= 3; guard++)
{
    var v = career.Observe();
    foreach (var free in v.Offers.Where(o => o.Availability == "Available").Take(1)) Submit(career, new SponsorDecision(free.Id));
    foreach (var role in "ABCDE".Select(r => r.ToString()).Where(r => !v.People.Any(p => p.Role == r)))
        if (v.Candidates.FirstOrDefault(c => c.Role == role) is { } cover) Submit(career, new SigningDecision(cover.Id));
    var response = Submit(career, new AdvanceDecision());
    if (!response.Accepted) throw new Exception("Career blocked: " + response.Message);
    departures += career.Capture().Company.Reviews.Count(r => r.Id.StartsWith("notice:contract:", StringComparison.Ordinal) && r.Day == career.Observe().Day);
    seasonsSeen.Add(career.Observe().Season); maxItems = Math.Max(maxItems, career.Capture().Company.FinancialItems.Length);
    if (guard % 25 == 0) SnapshotValidation.Validate(career.Capture(), content);
}
var careerState = career.Capture();
SnapshotValidation.Validate(careerState, content);
Check(departures > 0, "contracts lapse over seasons and people leave");
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

for (ulong seed = 1; seed <= 16; seed++)
{
    Session Seeded() => New(seed);
    var left = Seeded(); var right = Seeded();
    foreach (var request in new[] { new Request("a", 0, new CoachDecision(Delegation.Autonomous, Risk.Aggressive)), new Request("b", 1, new AdvanceDecision()), new Request("c", 2, new AdvanceDecision()) })
    { left.Submit(request); right.Observe(); right.Submit(request); Check(Canonical.Hash(left.Capture()) == Canonical.Hash(right.Capture()), $"seed {seed} transition {request.Id}"); }
}
Console.WriteLine($"INTEGRATION PASS {count} phaseBoundaries={boundaries} hash={Canonical.Hash(session.Capture())}");
