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
Check(a.People.All(p => content.Definition.Nationalities.Single(n => n.Id == p.Nationality) is var n
    && n.Given.Any(g => n.Family.Any(f => p.FullName == (n.FamilyFirst ? f + " " + g : g + " " + f)))
    && content.Definition.Handles.Prefixes.Any(x => p.Name.StartsWith(x, StringComparison.Ordinal))), "names and handles come from the content pools of each person's nationality");
// Rules v4 player model: generation tables, role ratings, wages, lineups and what the company may observe.
static bool Throws(Action action) { try { action(); return false; } catch (RuleViolation) { return true; } }
var def = content.Definition;
var sample = Enumerable.Range(0, 500).Select(i => Generator.Player(99, def, $"person:t{i:D5}", Roles.All[i % 5], 1)).ToArray();
var ages = sample.Select(p => Ratings.Age(p, 1)).ToArray();
double TableMean(ImmutableArray<WeightedValue> table) => table.Sum(x => (double)x.Value * x.Weight) / table.Sum(x => x.Weight);
Check(ages.All(a => def.Generation.Ages.Any(x => x.Value == a)) && Math.Abs(ages.Average() - TableMean(def.Generation.Ages)) < 1,
    "generated ages come from the calibrated age table, with its mean");
Check(sample.All(p => new[] { p.Skills.Mechanics, p.Skills.EarlyGame, p.Skills.Teamfight, p.Skills.Macro, p.Skills.ChampionPool }.All(v => v is >= 1 and <= 99)
    && p.Potential >= Ratings.Rating(p, def) && p.Potential <= 99 && p.Popularity is >= 1 and <= 99 && p.SecondaryRole != p.Role),
    "skills stay within 1-99, potential is never below the rating, a secondary role differs from the primary");
Check(Roles.All.All(r => sample.Where(p => p.Role == r).Select(p => p.Skills).Distinct().Count() > 50)
    && sample.Count(p => p.SecondaryRole.Length > 0) is > 75 and < 175, "every role has varied players; about a quarter cover a secondary role");
var young = sample.Where(p => Ratings.Age(p, 1) <= 19).ToArray(); var prime = sample.Where(p => Ratings.Age(p, 1) is >= 22 and <= 26).ToArray();
Check(young.Average(p => p.Potential - Ratings.Rating(p, def)) > prime.Average(p => p.Potential - Ratings.Rating(p, def)) + 4
    && young.Average(p => Ratings.Rating(p, def)) < prime.Average(p => Ratings.Rating(p, def)),
    "young players trade current rating for headroom to grow");
var nationalityShare = sample.GroupBy(p => p.Nationality).ToDictionary(g => g.Key, g => g.Count() / 500.0);
Check(def.Nationalities.All(n => Math.Abs(nationalityShare.GetValueOrDefault(n.Id) - n.Weight / (double)def.Nationalities.Sum(x => x.Weight)) < .06),
    "nationalities follow the calibrated mix");
// Market and starting roster share one ability distribution (no unexplained gap).
var starts = Enumerable.Range(1, 8).Select(seed => New((ulong)seed).Capture()).ToArray();
var rosterMean = starts.SelectMany(s => s.Company.People).Average(p => Ratings.Rating(p, def));
var marketMean = starts.SelectMany(s => s.World.Candidates).Average(x => Ratings.Rating(x.Person, def));
Check(Math.Abs(rosterMean - marketMean) <= 3, $"market and starting roster ratings share one mean ({rosterMean:0.0} vs {marketMean:0.0})");
// Rating: role weights times skills (basis points), scaled by proficiency; a secondary role at 80%, others not at all.
var probe = sample.First(p => p.SecondaryRole.Length > 0);
var probeWeights = def.RoleWeights.Single(w => w.Role == probe.Role).Weights;
var expectedRating = (int)Numbers.Divide(Enumerable.Range(0, 5).Sum(i => (long)probeWeights[i] * Ratings.Skill(probe.Skills, i)) * 100, 1_000_000);
var otherRole = Roles.All.First(r => r != probe.Role && r != probe.SecondaryRole);
Check(Ratings.Rating(probe, def) == expectedRating && Ratings.InRole(probe, otherRole, def) == 0
    && Ratings.InRole(probe, probe.SecondaryRole, def) < Ratings.InRole(probe with { Role = probe.SecondaryRole, SecondaryRole = "" }, probe.SecondaryRole, def),
    "a rating is role-weighted skills; a secondary role plays at reduced proficiency and other roles not at all");
Check(Pay.Player(probe with { Popularity = 90 }, 1, def) > Pay.Player(probe with { Popularity = 20 }, 1, def)
    && Pay.Player(probe with { Skills = probe.Skills with { Mechanics = 99, Teamfight = 99 } }, 1, def) > Pay.Player(probe, 1, def),
    "wages rise with rating and fan following");
// Lineups list one player per role in role order; a secondary role can cover; a player cannot play an unrelated role.
var lineupState = New(5).Capture(); var lineupView = Observations.Build(lineupState, def);
var best = CoachPolicy.BestLineup(lineupView.People);
Check(!best.IsDefault && best.Select((id, i) => Ratings.Proficiency(lineupState.Company.People.Single(x => x.Id == id), Roles.All[i]) > 0).All(x => x),
    "the coach's lineup puts each player in a role they can play, in role order");
var wrongOrder = best.Reverse().ToImmutableArray();
var lineupFixture = Simulation.NextFixture(lineupState)!;
Check(wrongOrder.Select((id, i) => Ratings.Proficiency(lineupState.Company.People.Single(x => x.Id == id), Roles.All[i])).Any(x => x == 0)
    && Throws(() => Simulation.Apply(lineupState, new CommitPlan(new Plan(lineupFixture.Id, 50, 30, 20, Posture.Balanced, wrongOrder, Posture.Balanced, "x")), def, "wrong-roles")),
    "a lineup that puts a player in a role they cannot play is rejected");
var helper = sample.First(x => x.Role == "mid") with { Id = "person:helper", SecondaryRole = "top" };
var noTops = lineupState.Company with { People = [.. lineupState.Company.People.Where(x => x.Role != "top" && x.SecondaryRole != "top"), helper] };
Check(Simulation.LineupPossible(noTops) && !Simulation.LineupPossible(noTops with { People = [.. noTops.People.Where(x => x.Id != helper.Id), helper with { SecondaryRole = "" }] }),
    "a secondary-role player can cover a role nobody plays as primary");
// What the company observes: its own players exactly, others as bands that contain the truth; personality never.
var observedJson = Canonical.Json(lineupView);
Check(new[] { "Professionalism", "Ambition", "Loyalty", "\"Potential\":" }.All(key => !observedJson.Contains(key, StringComparison.Ordinal)),
    "observations never carry personality or exact potential");
static (int Lo, int Hi) Bounds(string band) { var parts = band.Replace(" (estimate)", "", StringComparison.Ordinal).Split('–'); return (int.Parse(parts[0]), int.Parse(parts[1])); }
Check(lineupView.Candidates.All(c => lineupState.World.Candidates.Single(x => x.Person.Id == c.Id).Person is var truth
        && Bounds(c.RatingEstimate) is var rb && rb.Lo <= Ratings.Rating(truth, def) && Ratings.Rating(truth, def) <= rb.Hi
        && Bounds(c.PotentialEstimate) is var pb && pb.Lo <= truth.Potential && truth.Potential <= pb.Hi),
    "every rating and potential estimate contains the true value");
var sharp = Observations.Build(lineupState with { Company = lineupState.Company with { Information = 95 } }, def);
Check(sharp.Candidates.Zip(lineupView.Candidates).All(x => Bounds(x.First.RatingEstimate) is var narrow && Bounds(x.Second.RatingEstimate) is var wide && narrow.Hi - narrow.Lo <= wide.Hi - wide.Lo)
    && sharp.Candidates.Any(c => Bounds(c.RatingEstimate) is var narrow && narrow.Hi - narrow.Lo < 18), "better information narrows the estimates");


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
    foreach (var role in Roles.All.Where(r => !v.People.Any(p => p.Role == r)))
        if (v.Candidates.FirstOrDefault(c => c.Role == role) is { } cover) Submit(career, new SigningDecision(cover.Id));
    // The owner never renews but keeps the registered roster at the league minimum (rules v3), as any owner must.
    for (var depth = career.Observe(); depth.People.Length < b.MinimumRoster && depth.Candidates.FirstOrDefault(c => c.Fee <= depth.Cash) is { } extra; depth = career.Observe())
        if (!Submit(career, new SigningDecision(extra.Id)).Accepted) break;
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
var endings = departureView.Moves.Where(m => m.Kind is MoveKind.ContractEnd or MoveKind.Retirement && m.Day == departureView.Day).ToArray();
Check(departed.All(r => endings.Any(m => m.PersonId == r.SubjectId && m.From == departureView.CompanyId && m.To.Length == 0 && m.CauseId == r.CauseId)),
    "every contract-end departure is a recorded move from the company to the open market (or into retirement), caused by the contract that ended");
var departurePortal = PortalProjection.Build(departureView);
var departureStories = PortalProjection.TransferStories(departureView);
var usSide = new JourneySide(Standing.Organization, departureView.CompanyId, departureView.Company, AssetIds.Crest(departureView.CompanyId));
Check(endings.All(m => departureStories.Count(t => t.Records.Contains(m.Id)) == 1)
    && departureStories.Where(t => t.Kind == MoveKind.ContractEnd && t.From.OrganizationId == departureView.CompanyId).All(t => t.Status == StoryStatus.Confirmed && t.Source == StorySource.Simulation
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
var leaver = endings.First(m => !m.Coach && m.Kind == MoveKind.ContractEnd);
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
var thin = New().Capture(); var keep = thin.Company.People.Where(p => p.Role != "top" && p.SecondaryRole != "top").ToImmutableArray();
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
var d = content.Definition;
var permutedContent = d with { Brands = d.Brands.Reverse().ToImmutableArray(), OrganizationNames = d.OrganizationNames.Reverse().ToImmutableArray(),
    Nationalities = [.. d.Nationalities.Reverse().Select(n => n with { Given = n.Given.Reverse().ToImmutableArray(), Family = n.Family.Reverse().ToImmutableArray() })],
    Handles = new HandleParts(d.Handles.Prefixes.Reverse().ToImmutableArray(), d.Handles.Suffixes.Reverse().ToImmutableArray()),
    Generation = d.Generation with { Archetypes = d.Generation.Archetypes.Reverse().ToImmutableArray(), Ages = d.Generation.Ages.Reverse().ToImmutableArray(),
        AbilityBands = d.Generation.AbilityBands.Reverse().ToImmutableArray() } };
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
    && secondStory.Headline == $"{signed.Name} signs with {moved.Company}" && secondStory.Summary.StartsWith(PortalProjection.RoleNoun(signed.Role), StringComparison.Ordinal)
    && secondStory.From == JourneySide.FreeAgent && secondStory.To == usView,
    "head-coach stories lead the player story; each tells its journey and opens its person in the owning section");
Check(movedPortal.Transfers.All(t => !t.Headline.Contains('$') && !t.Summary.Contains('$') && !t.Summary.Contains(Currency.Format(signed.Fee), StringComparison.Ordinal))
    && movedPortal.Transfers.All(t => !movedPortal.News.Any(n => n.Id == t.Id)), "own moves state no terms and are not world news");
// Reloading tells the same stories: transfer history is saved (schema 4), so nothing is repeated or lost.
store.Save("moves", mover.Capture());
var savedMoves = System.Text.Json.JsonDocument.Parse(File.ReadAllText(store.PrimaryPath("moves"))).RootElement;
Check(savedMoves.GetProperty("Schema").GetInt32() == SnapshotStore.Schema && SnapshotStore.Schema == 5
    && savedMoves.GetProperty("Snapshot").GetProperty("World").GetProperty("Moves").GetArrayLength() == 3, "transfer history is part of the save (schema 5)");
var reloaded = new Session(content.Definition, store.Load("moves"));
Check(PortalProjection.Build(reloaded.Observe()).Transfers.SequenceEqual(movedPortal.Transfers) && PortalProjection.Build(mover.Observe()).Transfers.SequenceEqual(movedPortal.Transfers),
    "transfer news survives save and load unchanged, and re-observing does not regenerate it");
File.WriteAllText(store.PrimaryPath("moves"), File.ReadAllText(store.PrimaryPath("moves")).Replace("\"Schema\":5", "\"Schema\":4", StringComparison.Ordinal));
try { store.Load("moves"); Check(false, "a schema-4 save is rejected"); } catch (InvalidDataException) { Check(true, "a schema-4 save is rejected"); }
// Transfer history is validated: a move in a direction its reason does not allow, or from the future, is rejected.
var recorded = mover.Capture();
// World.Moves is in recorded order: the signing, the replaced coach, the appointment.
foreach (var (label, index, broken) in new (string, int, Move)[] { ("a signing from the company itself", 0, recorded.World.Moves[0] with { Origin = recorded.Company.Id }),
    ("a move dated in the future", 0, recorded.World.Moves[0] with { Day = recorded.World.Calendar.Day + 1 }), ("a coach recorded as a player", 1, recorded.World.Moves[1] with { Role = "top" }) })
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
var leagueTeams = b.RivalCount + 1;
Check(Simulation.ReputationTarget(0, 8, leagueTeams, leagueTeams, b) == b.ReputationTargetLow && Simulation.ReputationTarget(8, 8, 1, leagueTeams, b) == b.ReputationTargetHigh
    && Enumerable.Range(0, 8).All(wins => Simulation.ReputationTarget(wins, 8, 4, leagueTeams, b) <= Simulation.ReputationTarget(wins + 1, 8, 4, leagueTeams, b))
    && Enumerable.Range(1, leagueTeams - 1).All(place => Simulation.ReputationTarget(4, 8, place, leagueTeams, b) >= Simulation.ReputationTarget(4, 8, place + 1, leagueTeams, b)),
    "the reputation target rises with the season's wins and with its final place");
var closing = New().Capture(); var seasonEnd = Seasons.End(1, b);
var seasonRecord = closing.World.Fixtures.Select((f, i) => new CompetitiveOutcome("result:" + f.Id, f.Id, f.Day, f.RivalId, i < 2, Posture.Balanced, 500_000, 0, 0, 0, 0, 0, f.Importance, "test", 0, 0, 0, 0, i < 2 ? 2 : 0, i < 2 ? 0 : 2)).ToImmutableArray();
closing = closing with { World = closing.World with { Results = seasonRecord, Calendar = new Calendar(seasonEnd, Phase.Review, (seasonEnd - 1L) * 11 + (int)Phase.Review - 1) },
    Company = closing.Company with { Reputation = 60, FinanceReceipts = [.. seasonRecord.Select(r => r.Id)], CommercialReceipts = [.. seasonRecord.Select(r => r.Id)] } };
var rolled = Simulation.Step(closing, content.Definition);
var closingPlace = rolled.World.Finishes.Single(f => f.Season == 1).Order.IndexOf(closing.Company.Id) + 1;
var seasonTarget = Simulation.ReputationTarget(2, seasonRecord.Length, closingPlace, leagueTeams, b);
Check(seasonTarget < 60 && rolled.Company.Reputation == 60 + (int)Numbers.Divide((long)(seasonTarget - 60) * b.ReputationRegression, 100)
    && rolled.Company.Reviews.Any(r => r.Id.StartsWith("notice:season:2:", StringComparison.Ordinal) && r.Text.Contains("settles toward " + seasonTarget, StringComparison.Ordinal)),
    "a losing season pulls reputation toward the level its record earns, and the season notice says so");
// Predictability: a rival reads only the company's latest results for a repeated posture.
int AdaptationAfter(int prior)
{
    var s0 = New().Capture();
    var old = Enumerable.Range(0, prior).Select(i => new CompetitiveOutcome($"result:old:{i}", $"fixture:s0:{i:D2}", 0, s0.World.Rivals[0].Id, false, Posture.Balanced,
        500_000, 0, 0, 0, 0, 0, 1, "test", 0, 0, 0, 0, 0, 2)).ToImmutableArray();
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

// Rules v4 phase 3: logistic games, best-of series, the eight-team league, its playoffs and forced restructuring.
Check(Enumerable.Range(0, Logistic.MaxLogit + 1).All(z => Math.Abs(Logistic.Probability(z) - 1_000_000 / (1 + Math.Exp(-z / 100.0))) <= 1)
    && Enumerable.Range(1, Logistic.MaxLogit).All(z => Logistic.Probability(-z) == 1_000_000 - Logistic.Probability(z)) && Logistic.Probability(0) == 500_000
    && Logistic.Probability(Logistic.MaxLogit + 50) == Logistic.Probability(Logistic.MaxLogit),
    "the integer logistic table matches the logistic function within one millionth, is symmetric and clamps beyond its range");
Check(new[] { 3, 5 }.All(n => Logistic.Series(500_000, n) == 500_000
        && Enumerable.Range(1, 99).All(k => Math.Abs(Logistic.Series(k * 10_000, n) + Logistic.Series(1_000_000 - k * 10_000, n) - 1_000_000) <= 1)
        && Enumerable.Range(1, 98).All(k => Logistic.Series(k * 10_000, n) < Logistic.Series((k + 1) * 10_000, n)))
    && Logistic.Series(600_000, 1) == 600_000 && Logistic.Series(600_000, 3) == 648_000 && Logistic.Series(600_000, 5) == 682_560,
    "a best-of series is exact integer arithmetic: even at even odds, symmetric, monotonic, and it amplifies the favourite");
{
    var even = Enumerable.Range(0, 4000).Select(i => Competition.Play(99, "test", "series:" + i, 0, 1, 3, b.LogitScale)).ToArray();
    var favourite = Enumerable.Range(0, 2000).Select(i => Competition.Play(99, "test", "favourite:" + i, 14, 1, 3, b.LogitScale)).ToArray();
    var bestOfFive = Enumerable.Range(0, 2000).Select(i => Competition.Play(99, "test", "five:" + i, 0, 1, 5, b.LogitScale)).ToArray();
    Check(even.All(x => Math.Max(x.Won, x.Lost) == 2 && Math.Min(x.Won, x.Lost) < 2) && bestOfFive.All(x => Math.Max(x.Won, x.Lost) == 3 && Math.Min(x.Won, x.Lost) < 3)
        && Math.Abs(even.Count(x => x.Won > x.Lost) / 4000.0 - 0.5) < 0.03 && Math.Abs(even.Count(x => x.Won + x.Lost == 2) / 4000.0 - 0.5) < 0.03
        && favourite.Count(x => x.Won > x.Lost) / 2000.0 > 0.85,
        "series stop at the majority: even sides split wins and sweeps about half the time; a 14-point favourite wins most best-of-threes");
}
// The regular season: a seeded double round robin of the company and its rivals.
{
    var openingSeason = New(11).Capture(); var lw = openingSeason.World; var companyId = openingSeason.Company.Id;
    var teams = lw.Rivals.Select(r => r.Id).Append(companyId).ToArray();
    var games = lw.Fixtures.Select(f => (f.Day, Home: companyId, Away: f.RivalId)).Concat(lw.LeagueFixtures.Select(f => (f.Day, Home: f.HomeId, Away: f.AwayId))).ToArray();
    var pairs = teams.SelectMany(x => teams.Where(y => string.CompareOrdinal(x, y) < 0).Select(y => (x, y))).ToArray();
    Check(Seasons.Rounds(b) == 2 * b.RivalCount && games.Length == teams.Length * (teams.Length - 1)
        && pairs.All(p => games.Count(g => g.Home == p.x && g.Away == p.y || g.Home == p.y && g.Away == p.x) == 2)
        && games.GroupBy(g => g.Day).Count() == Seasons.Rounds(b)
        && games.GroupBy(g => g.Day).All(g => g.SelectMany(m => new[] { m.Home, m.Away }).Distinct().Count() == teams.Length)
        && lw.Fixtures.Select((f, k) => f.Day == Seasons.RoundDay(1, k, b) && f.Importance == (k >= b.RivalCount ? 2 : 1) && f.Games == b.RegularGames && f.Stage == Seasons.Regular).All(x => x)
        && lw.LeagueFixtures.All(f => f.Games == b.RegularGames && f.Stage == Seasons.Regular),
        "the regular season is a double round robin: every pair meets twice, every team plays once each round, the company every round");
    var again = Seasons.Schedule(openingSeason.Execution.Seed, 1, companyId, lw.Rivals, b);
    var secondSeason = Seasons.Schedule(openingSeason.Execution.Seed, 2, companyId, lw.Rivals, b);
    Check(again.Company.SequenceEqual(lw.Fixtures) && again.League.SequenceEqual(lw.LeagueFixtures)
        && !secondSeason.Company.Select(f => f.RivalId).SequenceEqual(lw.Fixtures.Select(f => f.RivalId)),
        "the schedule is a pure function of seed and season, and a new season draws a new order");
    Check(Seasons.SemifinalDay(1, b) - Seasons.LastRegularDay(1, b) == b.PlayoffInterval && Seasons.FinalDay(1, b) < Seasons.End(1, b),
        "the playoffs follow the regular season and the final is played before the season ends");

    // Table order: points, game difference, head-to-head among the teams still level, games won.
    var r = lw.Rivals.Select(x => x.Id).ToArray();
    ImmutableArray<Seasons.Standing> TableOf(params (string Winner, string Loser, int Won, int Lost)[] series) => Seasons.Table(lw with
    {
        Results = [], LeagueResults = [.. series.Select((x, i) => new LeagueResult($"result:league:s1:t{i}", $"league:s1:t{i}", 10, x.Winner, x.Loser, x.Won, x.Lost))]
    }, companyId, 1, openingSeason.Execution.Seed);
    int At(ImmutableArray<Seasons.Standing> table, string id) => table.Select((x, i) => (x, i)).First(p => p.x.TeamId == id).i;
    var byDifference = TableOf((r[0], r[1], 2, 0), (r[2], r[3], 2, 1));
    var byHeadToHead = TableOf((r[0], r[1], 2, 1), (r[1], r[2], 2, 0));
    var reversed = TableOf((r[1], r[0], 2, 1), (r[0], r[2], 2, 0));
    var byGames = TableOf((r[0], r[1], 2, 1), (r[2], r[0], 2, 1), (r[3], r[4], 2, 0), (r[5], r[3], 2, 0));
    var swapped = TableOf((r[3], r[1], 2, 1), (r[2], r[3], 2, 1), (r[0], r[4], 2, 0), (r[5], r[0], 2, 0));
    Check(At(byDifference, r[0]) < At(byDifference, r[2]) && byDifference[0].Points == 1 && At(byDifference, r[2]) < At(byDifference, companyId),
        "the table ranks by points (one per series won), then game difference");
    Check(At(byHeadToHead, r[0]) < At(byHeadToHead, r[1]) && At(reversed, r[1]) < At(reversed, r[0]),
        "teams level on points and game difference are separated by their head-to-head series");
    Check(At(byGames, r[0]) < At(byGames, r[3]) && At(swapped, r[3]) < At(swapped, r[0]), "then by games won");
}
// A season played through: playoffs from the table, a save in the middle of them, the champion, the final order and prizes.
{
    var league = New(21); Submit(league, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
    var leagueStore = new SnapshotStore(Path.GetFullPath("artifacts/save-tests-" + Guid.NewGuid().ToString("N")), content);
    void Play(Func<Situation, bool> until)
    {
        for (var guard = 0; guard < 400 && !until(league.Observe()); guard++)
        {
            var v = league.Observe();
            foreach (var role in Roles.All.Where(role => !v.People.Any(p => p.Role == role)))
                if (v.Candidates.FirstOrDefault(c => c.Role == role && c.Fee <= v.Cash) is { } cover) Submit(league, new SigningDecision(cover.Id));
            foreach (var p in v.People.Where(p => p.CanRenew)) Submit(league, new RenewalDecision(p.Id, 1));
            if (v.CoachContract.CanRenew) Submit(league, new RenewalDecision(v.CoachContract.Id, 1));
            if (!Submit(league, new AdvanceDecision()).Accepted) throw new Exception("League season blocked on day " + v.Day);
        }
    }
    Play(v => v.Day > Seasons.LastRegularDay(1, b));
    var midway = league.Capture(); var seed = midway.Execution.Seed; var cid = midway.Company.Id;
    var table = Seasons.Table(midway.World, cid, 1, seed).Select(s => s.TeamId).ToArray();
    var semis = midway.World.Fixtures.Where(f => f.Stage == Seasons.Semifinal).Select(f => (cid, f.RivalId))
        .Concat(midway.World.LeagueFixtures.Where(f => f.Stage == Seasons.Semifinal).Select(f => (f.HomeId, f.AwayId))).ToArray();
    var expected = new[] { (table[0], table[3]), (table[1], table[2]) };
    Check(semis.Length == 2 && expected.All(e => semis.Any(s => s.Item1 == e.Item1 && s.Item2 == e.Item2 || s.Item1 == e.Item2 && s.Item2 == e.Item1))
        && midway.World.Fixtures.Concat<object>(midway.World.LeagueFixtures).Count() == 2 * b.RivalCount + b.RivalCount * (b.RivalCount - 1) + 2
        && midway.World.Fixtures.Where(f => f.Stage == Seasons.Semifinal).All(f => f.Games == b.PlayoffGames && f.Importance == 3),
        "after the regular season the top four play semifinals, first against fourth and second against third, as best-of-fives");
    var standings = league.Observe().Standings;
    Check(standings.Select(s => s.TeamId).SequenceEqual(table) && standings.Select(s => s.Place).SequenceEqual(Enumerable.Range(1, table.Length))
        && standings.All(s => s.Played == 2 * b.RivalCount && s.Points == s.Won && s.Won + s.Lost == s.Played) && standings.Count(s => s.Company) == 1,
        "the observed standings are the derived table: every team played its fourteen series");
    Check(league.Observe().Inbox.Any(row => row.Id.StartsWith("notice:league:regular:", StringComparison.Ordinal) && row.Kind == "Season"
        && ShellSections.ForInbox(row.Kind) == ShellSections.Competition), "the end of the regular season is a season notice that opens Competition");
    var leagueNews = league.Observe().LeagueNews;
    Check(leagueNews.Where(n => n.Kind == "Leader").FirstOrDefault() is not { } leader || leader.SubjectId == standings[0].TeamId,
        "the latest leader story names the team that leads the table");
    Check(leagueNews.All(n => n.Kind is "Leader" or "Upset") && leagueNews.Select(n => n.Day).SequenceEqual(leagueNews.Select(n => n.Day).OrderDescending())
        && PortalProjection.WorldNews(league.Observe()).Length <= PortalProjection.NewsLimit
        && PortalProjection.WorldNews(league.Observe()).Select(n => n.Day).SequenceEqual(PortalProjection.WorldNews(league.Observe()).Select(n => n.Day).OrderDescending()),
        "league stories are read from results, newest first, and join the Portal's world news by day");
    leagueStore.Save("playoffs", midway);
    var resumed = new Session(content.Definition, leagueStore.Load("playoffs"), leagueStore);
    Check(Canonical.Hash(resumed.Capture()) == Canonical.Hash(midway), "a save in the middle of the playoffs loads unchanged");
    Play(v => v.Day > Seasons.FinalDay(1, b));
    var crowned = league.Capture();
    var order = Seasons.FinalOrder(crowned.World, cid, 1, seed, b);
    Check(order is { } o && o.Length == b.RivalCount + 1 && o.Distinct().Count() == o.Length
        && crowned.Company.Reviews.Any(row => row.Id.StartsWith("notice:league:champion:", StringComparison.Ordinal))
        && o.Take(b.PlayoffTeams).Order(StringComparer.Ordinal).SequenceEqual(table.Take(b.PlayoffTeams).Order(StringComparer.Ordinal))
        && o.Skip(b.PlayoffTeams).SequenceEqual(table.Skip(b.PlayoffTeams)),
        "the final order puts the champion and runner-up first, then the other semifinalists, then the table");
    Play(v => v.Season > 1);
    var nextSeason = league.Capture();
    var place = order!.Value.IndexOf(cid) + 1;
    Check(nextSeason.World.Finishes.Single().Season == 1 && nextSeason.World.Finishes.Single().Order.SequenceEqual(order.Value) && nextSeason.World.LeagueResults.All(x => x.Day > Seasons.End(1, b))
        && (b.PlacePrizes[place - 1] == 0 || nextSeason.Company.FinancialItems.Any(x => x.Id == "place:s1" && x.Amount == b.PlacePrizes[place - 1])
            || nextSeason.Company.Settlements.Any(x => x.CauseId == "place:s1" && x.Delta == b.PlacePrizes[place - 1])),
        "the season closes with its final order recorded and the company's place prize paid");
    Check(league.Observe().LastSeasonPlace == place, "the next season shows last season's place");
    SnapshotValidation.Validate(nextSeason, content);
}
// Familiarity: the starting roster is integrated, a new signing is not, and it grows with days and matches.
{
    var fresh = New();
    var startCompany = fresh.Capture().Company;
    Check(startCompany.Employment.Where(e => e.PersonId != startCompany.Coach.Id).All(e => e.Familiarity == b.StartingFamiliarity), "the starting roster begins integrated");
    var signing = fresh.Observe().Candidates[0];
    Submit(fresh, new SigningDecision(signing.Id));
    var newContract = fresh.Capture().Company.Employment.Single(e => e.PersonId == signing.Id);
    Check(newContract.Familiarity == 0, "a new signing arrives unfamiliar with the team");
    var day = Simulation.Step(Simulation.Step(fresh.Capture(), content.Definition), content.Definition);
    Check(day.Company.Employment.Single(e => e.PersonId == signing.Id).Familiarity == b.FamiliarityPerDay, "familiarity grows each day");
}
// Forced restructuring (DEC-016): stuck in distress, the board sells one contract a day, before settlement.
{
    var broke = New(5); Submit(broke, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
    for (var i = 0; i < 6; i++) Submit(broke, new AdvanceDecision());
    var stuck = broke.Capture();
    while (stuck.World.Calendar.Phase != Phase.Finance) stuck = Simulation.Step(stuck, content.Definition);
    var day = stuck.World.Calendar.Day;
    var debt = new FinancialItem($"{Finance.OperationsCause}:day:{day - 1}:debt", Finance.OperationsCause, day - 1, 50_000_000, false, 50_000_000, day - 1);
    stuck = stuck with { Company = stuck.Company with { Cash = 0, LedgerBase = stuck.Company.LedgerBase - stuck.Company.Cash, Recovery = RecoveryStage.Distress,
        FinancialItems = [.. stuck.Company.FinancialItems.Where(x => !x.Incoming || x.Remaining == 0).Append(debt)] } };
    Check(Simulation.Stuck(stuck.Company, day), "a company with overdue debt, no cash and no inflow within three days is stuck");
    var roster = stuck.Company.People.Length;
    var afterSales = stuck;
    for (var phase = 0; phase < 11; phase++) afterSales = Simulation.Step(afterSales, content.Definition);
    afterSales = Simulation.Step(afterSales, content.Definition);
    for (var phase = 0; phase < 10; phase++) afterSales = Simulation.Step(afterSales, content.Definition);
    var sales = afterSales.Company.Settlements.Where(x => x.CauseId.StartsWith("restructuring:", StringComparison.Ordinal)).ToArray();
    Check(sales.Length == 2 && sales.Select(x => x.Day).Distinct().Count() == 2 && afterSales.Company.People.Length == roster - 2
        && afterSales.World.Moves.Count(m => m.CauseId.StartsWith("restructuring:", StringComparison.Ordinal) && m.Reason == MoveReason.Released) == 2
        && afterSales.Company.Reviews.Count(r => r.Id.StartsWith("notice:restructuring:", StringComparison.Ordinal)) == 2,
        "the board sells one contract a day while the company is stuck, records the move and says what it cost");
    SnapshotValidation.Validate(afterSales, content);
    var empty = stuck with { Company = stuck.Company with { People = [], Employment = [.. stuck.Company.Employment.Where(e => e.PersonId == stuck.Company.Coach.Id)] } };
    Check(ReferenceEquals(Simulation.Step(empty, content.Definition), empty), "with nothing left to sell, time stops");
}

// Rules v4 phase 4: careers. Development, retirement, youth intake and a lasting pool; form, morale, popularity, conduct.
{
    var life = content.Definition.Lifecycle; var definition = content.Definition;
    var youngster = Generator.Player(42, definition, "person:test-youngster", "mid", 1, 18);
    var veteran = Generator.Player(42, definition, "person:test-veteran", "mid", 1, 30);
    var grown = Careers.Develop(youngster, Careers.Change(42, youngster, 1, life, 70, 100), definition);
    Check(Ratings.Rating(grown, definition) >= Ratings.Rating(youngster, definition) && Ratings.Rating(grown, definition) <= youngster.Potential
        && Careers.Develop(youngster, 50, definition) is var capped && Ratings.Rating(capped, definition) <= youngster.Potential && Ratings.Rating(capped, definition) >= Ratings.Rating(youngster, definition),
        "a young player grows, and never past the potential ceiling");
    Check(Careers.Change(42, veteran, 1, life, 70, 100) < 0 && Ratings.Rating(Careers.Develop(veteran, Careers.Change(42, veteran, 1, life, 70, 100), definition), definition) < Ratings.Rating(veteran, definition),
        "a veteran declines with age");
    Person With(Person p, int professionalism) => p with { Mind = p.Mind with { Professionalism = professionalism } };
    var draws = Enumerable.Range(0, 40).Select(i => Generator.Player(7, definition, $"person:test-growth:{i}", Roles.All[i % 5], 1, 18)).ToArray();
    Check(draws.Sum(p => Careers.Change(7, With(p, 90), 1, life, 50, 50)) > draws.Sum(p => Careers.Change(7, With(p, 10), 1, life, 50, 50))
        && draws.Sum(p => Careers.Change(7, p, 1, life, 90, 50)) > draws.Sum(p => Careers.Change(7, p, 1, life, 10, 50))
        && draws.Sum(p => Careers.Change(7, p, 1, life, 50, 100)) > draws.Sum(p => Careers.Change(7, p, 1, life, 50, 0)),
        "growth scales with professionalism, the head coach's development and playing time");
    var olds = Enumerable.Range(0, 40).Select(i => Generator.Player(7, definition, $"person:test-decline:{i}", Roles.All[i % 5], 1, 30)).ToArray();
    Check(olds.Sum(p => Careers.Change(7, With(p, 90), 1, life, 50, 50)) > olds.Sum(p => Careers.Change(7, With(p, 10), 1, life, 50, 50)),
        "professionalism slows decline");
    var mid = Generator.Player(42, definition, "person:test-retire", "top", 1, 26);
    Check(Careers.RetirementChance(mid, 24, definition) == 0 && Careers.RetirementChance(mid, life.RetirementByAge.Max(x => x.Value), definition) == 10_000
        && Enumerable.Range(25, 9).Select(a => Careers.RetirementChance(mid, a, definition)).Zip(Enumerable.Range(26, 9).Select(a => Careers.RetirementChance(mid, a, definition))).All(p => p.First <= p.Second)
        && Careers.RetirementChance(Careers.Develop(mid, -12, definition), 29, definition) > Careers.RetirementChance(mid, 29, definition),
        "retirement follows the calibrated age table: none before 25, certain at its last age, rising with age, likelier for weaker players");
    Check(Careers.Trend(youngster, 1, life) == "Improving" && Careers.Trend(veteran, 1, life) == "Declining", "the public trend follows age");

    // Season end on the open openMarket.
    var openMarket = New(9).Capture(); var closeDay = Seasons.End(1, b);
    Check(openMarket.World.Candidates.Length == life.PoolSize && Roles.All.All(r => openMarket.World.Candidates.Count(x => x.Person.Role == r) >= life.PoolSize / Roles.All.Length),
        "the campaign opens a free-agent pool of its full size, every role represented");
    var end = openMarket with { World = openMarket.World with { Calendar = new Calendar(closeDay, Phase.Review, (closeDay - 1L) * 11 + (int)Phase.Review - 1) } };
    // The open market's own season end (rivals recruit from it afterwards, in the full rollover below).
    var rolledOver = end with { World = Careers.SeasonMarket(end.World, end.Execution.Seed, definition, 1, seasonEnd) };
    var fullRollover = Simulation.Step(end, definition);
    var poolBefore = end.World.Candidates.Select(x => x.Person.Id).ToHashSet();
    var arrivals = rolledOver.World.Candidates.Where(x => !poolBefore.Contains(x.Person.Id)).ToArray();
    var retired = rolledOver.World.Moves.Where(m => m.Reason == MoveReason.Retired).ToArray();
    Check(arrivals.Length == life.YouthIntake && arrivals.All(x => life.IntakeAges.Any(a => a.Value == Ratings.Age(x.Person, 2))) && arrivals.Select(x => x.Person.Role).Distinct().Count() == Math.Min(5, life.YouthIntake),
        "the season's youth intake arrives at the calibrated entry ages, one role after another");
    Check(rolledOver.World.Candidates.Length <= life.PoolCap && retired.All(m => m.Origin is null && m.Destination is null && poolBefore.Contains(m.PersonId) && !rolledOver.World.Candidates.Any(x => x.Person.Id == m.PersonId))
        && poolBefore.Count + arrivals.Length - retired.Length == rolledOver.World.Candidates.Length,
        "free agents retire from the open market as recorded moves, and the pool keeps within its cap");
    Check(rolledOver.World.Candidates.All(x => x.Salary == Pay.Player(x.Person, 2, definition) && x.Fee == x.Salary * 20), "every listing follows its player into the new season");
    Check(fullRollover.Company.Employment.All(e => e.Played == 0), "playing time restarts with the season");
    SnapshotValidation.Validate(fullRollover, content);

    // A player whose contract ends may retire instead of becoming a free agent (certain at the table's last age).
    var lastAge = life.RetirementByAge.Max(x => x.Value);
    var expiring = New().Capture(); var oldest = expiring.Company.People[0];
    var aged = oldest with { BornSeason = 1 - lastAge };
    expiring = expiring with { Company = expiring.Company with { People = expiring.Company.People.Replace(oldest, aged),
        Employment = expiring.Company.Employment.Select(e => e.PersonId == oldest.Id ? e with { EndDay = 0 } : e).ToImmutableArray() } };
    var afterEnd = Simulation.Step(Simulation.Step(expiring, definition), definition);
    Check(afterEnd.World.Moves.Any(m => m.PersonId == oldest.Id && m.Reason == MoveReason.Retired && m.Origin == expiring.Company.Id && m.Destination is null)
        && !afterEnd.World.Candidates.Any(x => x.Person.Id == oldest.Id) && afterEnd.Company.Reviews.Any(r => r.Text.Contains($"retired at {lastAge}", StringComparison.Ordinal)),
        "a player whose contract ends at the last age retires: a recorded move and a notice, not a free agent");
    var afterView = Observations.Build(afterEnd, definition);
    var told = PortalProjection.TransferStories(afterView).Single(t => t.Records.Any(id => afterView.Moves.Any(m => m.Id == id && m.PersonId == oldest.Id)));
    Check(told.Kind == MoveKind.Retirement && told.To == JourneySide.Retired && told.Headline == $"{aged.Handle} retires",
        "transfer news tells a retirement: from the company into retirement");
    Check(afterView.Inbox.Any(r => r.Kind == "Contract" && r.SubjectId == oldest.Id), "the retirement notice names the player");

    // The pool lasts: no free agent vanishes during a season; a role nobody can fill brings a free agent forward.
    var lasting = New(9); Submit(lasting, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
    var openingPool = lasting.Capture().World.Candidates.Select(x => x.Person.Id).ToHashSet();
    for (var i = 0; i < 8; i++) Submit(lasting, new AdvanceDecision());
    Check(lasting.Observe().Season == 1 && openingPool.All(id => lasting.Capture().World.Candidates.Any(x => x.Person.Id == id)), "free agents stay on the market until signed or retired");
    // Mid-season (day 1 opens the whole pool instead): take a role away from both the roster and the market for a day.
    var gap = lasting.Capture(); var gapRole = gap.Company.People[0].Role;
    gap = gap with { Company = gap.Company with { People = [.. gap.Company.People.Where(p => p.Role != gapRole)], Plan = null,
        Employment = [.. gap.Company.Employment.Where(e => gap.Company.People.All(p => p.Id != e.PersonId || p.Role != gapRole))] },
        World = gap.World with { Candidates = [.. gap.World.Candidates.Where(x => x.Person.Role != gapRole)] } };
    var filled = Enumerable.Range(0, 11).Aggregate(gap, (state, _) => Simulation.Step(state, definition));
    Check(filled.World.Candidates.Count(x => x.Person.Role == gapRole) == 1 && filled.Company.Reviews.Any(r => r.Id.StartsWith("notice:talent:", StringComparison.Ordinal)),
        "a role neither the roster nor the market can fill brings one free agent forward, and the company is told");

    // Condition from matches.
    var squad = New().Capture().Company; var lineup = squad.People.Take(5).Select(p => p.Id).ToImmutableArray(); var bench = squad.People[5];
    CompetitiveOutcome Outcome(bool won, int probability, int importance) => new("result:test", "fixture:s1:01", 6, "rival:r01", won, Posture.Balanced, probability,
        0, 0, 0, 0, 0, importance, "test", 0, 0, 0, 0, won ? 2 : 0, won ? 0 : 2);
    var upset = Careers.AfterMatch(squad, Outcome(true, 200_000, 2), lineup, life);
    var expectedForm = life.FormStep + life.FormSurprise * 800_000 / 1_000_000;
    Check(lineup.All(id => upset.People.Single(p => p.Id == id) is var p && squad.People.Single(x => x.Id == id) is var q
            && p.Form == Math.Clamp(q.Form + expectedForm, -10, 10) && p.Morale == Math.Min(100, q.Morale + life.MoraleWin) && p.Popularity == Math.Min(100, q.Popularity + 2 * life.PopularityWin))
        && upset.People.Single(p => p.Id == bench.Id) is var benched && benched.Morale == Math.Max(0, bench.Morale - life.MoraleBench) && benched.Popularity == Math.Max(0, bench.Popularity - life.PopularityBench),
        "an unexpected win lifts the lineup's form, morale and popularity; the bench loses morale and a little popularity");
    var expected = Careers.AfterMatch(squad, Outcome(true, 900_000, 1), lineup, life);
    Check(lineup.All(id => expected.People.Single(p => p.Id == id).Form < upset.People.Single(p => p.Id == id).Form || upset.People.Single(p => p.Id == id).Form == 10),
        "an expected win moves form less than an upset");
    var settled = Careers.Settle(upset, life);
    Check(settled.People.All(p => Math.Abs(p.Form) <= Math.Abs(upset.People.Single(x => x.Id == p.Id).Form)
        && Math.Abs(p.Morale - life.MoraleRest) <= Math.Abs(upset.People.Single(x => x.Id == p.Id).Morale - life.MoraleRest)), "form and morale settle back over time");

    // Low morale: slower recovery and, with ambition, no renewal.
    var sulking = New().Capture(); var unhappy = sulking.Company.People[0];
    var unhappyContract = sulking.Company.Employment.Single(e => e.PersonId == unhappy.Id);
    sulking = sulking with { Company = sulking.Company with {
        People = sulking.Company.People.Replace(unhappy, unhappy with { Morale = life.MoraleLow - 5, Mind = unhappy.Mind with { Ambition = life.RefusalAmbition + 5 } }),
        Employment = sulking.Company.Employment.Replace(unhappyContract, unhappyContract with { EndDay = b.RenewalWindow - 1 }) } };
    var sulkView = Observations.Build(sulking, definition).People.Single(p => p.Id == unhappy.Id);
    var refusal = new Session(definition, sulking);
    Check(!sulkView.CanRenew && sulkView.RenewalNote.Length > 0 && !Submit(refusal, new RenewalDecision(unhappy.Id, 1)).Accepted,
        "an ambitious player with low morale will not renew, and the profile says so");
    Check(!Careers.Recovers(sulking.Company.People.Single(p => p.Id == unhappy.Id), life, 3) && Careers.Recovers(sulking.Company.People.Single(p => p.Id == unhappy.Id), life, 4)
        && Careers.Recovers(squad.People[1] with { Morale = 80 }, life, 3), "low morale halves readiness recovery");

    // Conduct follows professionalism.
    var careless = squad with { People = [.. squad.People.Select(p => p with { Mind = p.Mind with { Professionalism = 1 } })] };
    var careful = squad with { People = [.. squad.People.Select(p => p with { Mind = p.Mind with { Professionalism = 95 } })] };
    var carelessAfter = Enumerable.Range(1, 24).Aggregate(careless, (company, i) => Careers.Conduct(company, 11, life, i * life.ConductInterval));
    var carefulAfter = Enumerable.Range(1, 24).Aggregate(careful, (company, i) => Careers.Conduct(company, 11, life, i * life.ConductInterval));
    Check(carelessAfter.Reviews.Count(r => r.Id.StartsWith(Careers.ConductPrefix, StringComparison.Ordinal)) > 0 && carelessAfter.Reputation < careless.Reputation
        && carefulAfter.Reviews.All(r => !r.Id.StartsWith(Careers.ConductPrefix, StringComparison.Ordinal)) && carefulAfter.Reputation == careful.Reputation,
        "unprofessional players break team rules and cost reputation; professional ones do not");

    // Popularity reaches the company: sponsors' star bonus and a match's audience.
    var stars = squad with { People = [.. squad.People.Select((p, i) => i < 3 ? p with { Popularity = 95 } : p with { Popularity = 40 })] };
    var plain = squad with { People = [.. squad.People.Select(p => p with { Popularity = 40 })] };
    var sponsorBrand = definition.Brands[0];
    Check(Careers.StarBonus(stars, life) == life.StarBonusCap * life.StarBonusPercent && Careers.StarBonus(plain, life) == 0
        && Market.FairPayment(sponsorBrand, 50, 5000, Careers.StarBonus(stars, life)) > Market.FairPayment(sponsorBrand, 50, 5000, 0),
        "popular players raise what sponsors pay, for a capped number of stars");
    var fans = Commercial.Consume(squad, Outcome(true, 300_000, 1), b, 90).PendingEffects.Single().Audience;
    var few = Commercial.Consume(squad, Outcome(true, 300_000, 1), b, 10).PendingEffects.Single().Audience;
    Check(fans > few, "a popular lineup moves the audience more");

    // The head coach's adaptability multiplies the value of meta work.
    var prepared = New().Capture(); var firstFixture = prepared.World.Fixtures[0]; var rival = prepared.World.Rivals.Single(r => r.Id == firstFixture.RivalId);
    var lineupPeople = prepared.Company.People.Take(5).ToImmutableArray();
    int MetaPrep(int adaptability) => Competition.Resolve(new CompetitionInput(1, firstFixture, new Plan(firstFixture.Id, 0, 0, 100, Posture.Balanced, [.. lineupPeople.Select(p => p.Id)], Posture.Balanced, "test"),
        new Work(0, 0, 3000), lineupPeople, [.. lineupPeople.Select(p => 70)], prepared.Company.Coach with { Adaptability = adaptability }, rival, Posture.Aggressive, 0, b, 50, 75)).Preparation;
    Check(MetaPrep(90) > MetaPrep(10), "a head coach who reads the meta gets more from meta preparation");

    // A long campaign stays a living pool: steady size, retirements and intake, and ages that stay near the calibrated mix.
    var long_ = New(3); Submit(long_, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
    var ageMean = definition.Generation.Ages.Sum(a => (double)a.Value * a.Weight) / definition.Generation.Ages.Sum(a => a.Weight);
    var sizes = new List<int>(); var means = new List<double>(); var retirements = 0; var seen = new HashSet<string>();
    for (var guard = 0; guard < 2400 && long_.Observe().Season <= 8; guard++)
    {
        var v = long_.Observe();
        // A sound owner: takes a sponsor, covers every role, keeps the registered minimum and renews.
        foreach (var free in v.Offers.Where(o => o.Availability == "Available").Take(1)) Submit(long_, new SponsorDecision(free.Id));
        foreach (var role in Roles.All.Where(role => !v.People.Any(p => p.Role == role)))
            if (v.Candidates.FirstOrDefault(c => c.Role == role && c.Fee <= v.Cash) is { } cover) Submit(long_, new SigningDecision(cover.Id));
        for (var depth = long_.Observe(); depth.People.Length < b.MinimumRoster && depth.Candidates.FirstOrDefault(c => c.Fee <= depth.Cash) is { } extra; depth = long_.Observe())
            if (!Submit(long_, new SigningDecision(extra.Id)).Accepted) break;
        foreach (var p in v.People.Where(p => p.CanRenew)) Submit(long_, new RenewalDecision(p.Id, 1));
        if (v.CoachContract.CanRenew) Submit(long_, new RenewalDecision(v.CoachContract.Id, 1));
        var season = v.Season;
        if (!Submit(long_, new AdvanceDecision()).Accepted) throw new Exception("Long campaign blocked on day " + v.Day);
        var state = long_.Capture();
        retirements += state.World.Moves.Count(m => m.Reason == MoveReason.Retired && seen.Add(m.Id));
        if (long_.Observe().Season != season)
        {
            var everyone = state.Company.People.Concat(state.World.Candidates.Select(x => x.Person)).Concat(state.World.Rivals.SelectMany(r => r.People)).ToArray();
            sizes.Add(state.World.Candidates.Length); means.Add(everyone.Average(p => Ratings.Age(p, long_.Observe().Season)));
        }
    }
    Check(sizes.Count >= 7 && sizes.All(n => n >= Roles.All.Length && n <= life.PoolCap) && retirements > 0,
        $"over eight seasons the pool stays between one per role and its cap, and players retire ({string.Join(",", sizes)}; {retirements} retirements)");
    Check(means.All(m => Math.Abs(m - ageMean) <= 2.5), $"the whole population's mean age (company, rivals, market) stays near the calibrated mix ({ageMean:0.0}: {string.Join(", ", means.Select(m => m.ToString("0.0")))})");
    SnapshotValidation.Validate(long_.Capture(), content);
}

// Rules v4 phase 5: rival organizations with real squads, budgets and recruitment.
{
    var rules = content.Definition.Rivals; var gen = content.Definition.Generation; var world = content.Definition;
    var openingLeague = New(13).Capture();
    var strengths = openingLeague.World.Rivals.Select(r => Rivals.Strength(r, world)).ToArray();
    Check(openingLeague.World.Rivals.All(r => r.People.Length >= b.MinimumRoster && r.People.Length <= b.MaxRoster && Rivals.Lineup(r, world).Length == Roles.All.Length
            && r.Contracts.Length == r.People.Length && r.Contracts.All(x => x.EndDay == Seasons.End(1, b) || x.EndDay == Seasons.End(2, b)) && r.Funds > 0),
        "every rival opens with a full squad that covers all five roles, one- or two-season contracts and transfer funds");
    Check(strengths.All(x => x >= gen.Rival.Min - 4 && x <= gen.Rival.Max + 4) && strengths.Max() - strengths.Min() >= 8,
        $"rival strength comes from the squads and spans the league's calibrated spread ({string.Join(", ", strengths)})");
    var strongest = openingLeague.World.Rivals.OrderByDescending(r => Rivals.Strength(r, world)).First();
    var star = Rivals.Lineup(strongest, world).OrderByDescending(x => x.Rating).First().Person;
    var weakened = strongest with { People = strongest.People.Remove(star), Contracts = strongest.Contracts.RemoveAll(x => x.PersonId == star.Id) };
    Check(Rivals.Strength(weakened, world) < Rivals.Strength(strongest, world), "a rival that loses its best player is weaker");
    Check(openingLeague.World.Rivals.Select(r => r.Strategy).Distinct().Count() >= 2, "rivals open with different strategies");

    // Season end: departures, development and recruitment keep every rival legal and every person with one owner.
    var close = Seasons.End(1, b);
    var atEnd = openingLeague with { World = openingLeague.World with { Calendar = new Calendar(close, Phase.Review, (close - 1L) * 11 + (int)Phase.Review - 1) } };
    var afterEnd5 = Simulation.Step(atEnd, world);
    var again = Simulation.Step(atEnd, world);
    SnapshotValidation.Validate(afterEnd5, content);
    Check(Canonical.Hash(afterEnd5) == Canonical.Hash(again), "the rivals' season end is deterministic");
    var rivalIds = openingLeague.World.Rivals.Select(r => r.Id).ToHashSet();
    var moves = afterEnd5.World.Moves.Where(m => m.Day == close).ToArray();
    Check(afterEnd5.World.Rivals.All(r => Rivals.Lineup(r, world).Length == Roles.All.Length && r.People.Length >= b.MinimumRoster && r.Funds >= 0),
        "after the season end every rival still covers all five roles, with funds that never go negative");
    Check(moves.Where(m => m.Reason == MoveReason.Signed && rivalIds.Contains(m.Destination!)).All(m => m.Origin is null
            && afterEnd5.World.Rivals.Single(r => r.Id == m.Destination).People.Any(p => p.Id == m.PersonId) && !afterEnd5.World.Candidates.Any(x => x.Person.Id == m.PersonId))
        && moves.Where(m => m.Reason == MoveReason.Transferred).All(m => rivalIds.Contains(m.Origin!) && rivalIds.Contains(m.Destination!)
            && afterEnd5.World.Rivals.Single(r => r.Id == m.Destination).People.Any(p => p.Id == m.PersonId) && afterEnd5.World.Rivals.Single(r => r.Id == m.Origin).People.All(p => p.Id != m.PersonId))
        && moves.Where(m => m.Reason is MoveReason.ContractEnded or MoveReason.Released && rivalIds.Contains(m.Origin!)).All(m => m.Destination is null
            && afterEnd5.World.Candidates.Any(x => x.Person.Id == m.PersonId))
        && moves.Where(m => m.Reason == MoveReason.Retired && m.Origin is not null && rivalIds.Contains(m.Origin)).All(m =>
            afterEnd5.World.Rivals.All(r => r.People.All(p => p.Id != m.PersonId)) && afterEnd5.World.Candidates.All(x => x.Person.Id != m.PersonId)),
        "rival moves go where they say: signings from the market, transfers between rivals, departures to the market, retirements out of the game");
    Check(moves.Any(m => m.Origin is not null && rivalIds.Contains(m.Origin) || m.Destination is not null && rivalIds.Contains(m.Destination)),
        "rivals reshape their squads at season end");

    // Several seasons: rivals keep recruiting, trade with each other, and their turnover stays near the calibrated churn.
    var league = New(17); Submit(league, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
    var transfers = 0; var rivalSignings = 0; var counted = new HashSet<string>(); var rosterSeasons = 0;
    for (var guard = 0; guard < 1600 && league.Observe().Season <= 4; guard++)
    {
        var v = league.Observe();
        foreach (var free in v.Offers.Where(o => o.Availability == "Available").Take(1)) Submit(league, new SponsorDecision(free.Id));
        foreach (var role in Roles.All.Where(role => !v.People.Any(p => p.Role == role)))
            if (v.Candidates.FirstOrDefault(c => c.Role == role && c.Fee <= v.Cash) is { } cover) Submit(league, new SigningDecision(cover.Id));
        for (var depth = league.Observe(); depth.People.Length < b.MinimumRoster && depth.Candidates.FirstOrDefault(c => c.Fee <= depth.Cash) is { } extra; depth = league.Observe())
            if (!Submit(league, new SigningDecision(extra.Id)).Accepted) break;
        foreach (var p in v.People.Where(p => p.CanRenew)) Submit(league, new RenewalDecision(p.Id, 1));
        if (v.CoachContract.CanRenew) Submit(league, new RenewalDecision(v.CoachContract.Id, 1));
        var season = v.Season;
        if (!Submit(league, new AdvanceDecision()).Accepted) throw new Exception("Rival league blocked on day " + v.Day);
        var state = league.Capture();
        foreach (var m in state.World.Moves.Where(m => counted.Add(m.Id)))
        {
            if (m.Reason == MoveReason.Transferred) transfers++;
            if (m.Reason == MoveReason.Signed && m.Destination is not null && m.Destination.StartsWith("rival:", StringComparison.Ordinal)) rivalSignings++;
        }
        if (league.Observe().Season != season) rosterSeasons += state.World.Rivals.Sum(r => r.People.Length);
    }
    var churn = (transfers + rivalSignings) / (double)Math.Max(1, rosterSeasons);
    Check(transfers > 0 && rivalSignings > 0 && churn is > 0.08 and < 0.6,
        $"over several seasons rivals sign free agents and trade with each other; newcomers are {churn:P0} of rival squads a season (calibrated: about 25%)");
    SnapshotValidation.Validate(league.Capture(), content);
    var store5 = new SnapshotStore(Path.GetFullPath("artifacts/save-tests-" + Guid.NewGuid().ToString("N")), content);
    store5.Save("rivals", league.Capture());
    Check(Canonical.Hash(store5.Load("rivals")) == Canonical.Hash(league.Capture()), "rival squads, contracts and coaches survive a save and load");
    var view5 = league.Observe();
    Check(view5.RivalProfiles.Length == b.RivalCount && view5.RivalProfiles.All(p => p.Players.Length >= b.MinimumRoster && p.Players.All(x => x.RatingEstimate.EndsWith("(estimate)", StringComparison.Ordinal))
            && p.LevelEstimate.EndsWith("(estimate)", StringComparison.Ordinal) && p.Coach.Length > 0),
        "every rival has a public profile: its squad as scouted, its head coach and a strength estimate");
    var story = PortalProjection.TransferStories(view5 with { Moves = [.. view5.Moves.Where(m => m.Kind == MoveKind.Transfer)] }).FirstOrDefault();
    Check(story is null || story.From.Standing == Standing.Organization && story.To.Standing == Standing.Organization && story.Headline.Contains(" joins ", StringComparison.Ordinal),
        "a transfer between rivals is told as a team-to-team story");

    // A rival that keeps finishing poorly replaces its head coach.
    var struggling = atEnd with { World = atEnd.World with { Rivals = atEnd.World.Rivals.Select(r => r with { PoorSeasons = rules.CoachPatience }).ToImmutableArray() } };
    var order = Seasons.Table(struggling.World, struggling.Company.Id, 1, struggling.Execution.Seed).Select(x => x.TeamId).ToImmutableArray();
    var replaced = Rivals.Departures(struggling.World, struggling.Execution.Seed, world, 1, close, order);
    var poor = struggling.World.Rivals.Where(r => order.IndexOf(r.Id) + 1 >= rules.PoorPlace).ToArray();
    Check(poor.Length > 0 && poor.Any(r => replaced.Moves.Any(m => m.Reason == MoveReason.Replaced && m.Origin == r.Id) && Rivals.Get(replaced, r.Id).Coach.Id != r.Coach.Id && Rivals.Get(replaced, r.Id).PoorSeasons == 0),
        "a rival that keeps finishing poorly replaces its head coach from the coach market");
}

for (ulong seed = 1; seed <= 16; seed++)
{
    Session Seeded() => New(seed);
    var left = Seeded(); var right = Seeded();
    foreach (var request in new[] { new Request("a", 0, new CoachDecision(Delegation.Autonomous, Risk.Aggressive)), new Request("b", 1, new AdvanceDecision()), new Request("c", 2, new AdvanceDecision()) })
    { left.Submit(request); right.Observe(); right.Submit(request); Check(Canonical.Hash(left.Capture()) == Canonical.Hash(right.Capture()), $"seed {seed} transition {request.Id}"); }
}
Console.WriteLine($"INTEGRATION PASS {count} phaseBoundaries={boundaries} hash={Canonical.Hash(session.Capture())}");
