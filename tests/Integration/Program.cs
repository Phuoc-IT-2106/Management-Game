using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;
using System.Collections.Immutable;
var content = ContentLoader.Load(Path.GetFullPath("content/fixture.json"));
Session New() => new(content.Definition, CampaignFactory.Create(content.Definition, content.Hash, 123456789, "campaign:fixture"));
var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
var session = New();
var before = Canonical.Hash(session.Capture());
Check(!session.Submit(new Request("bad", 99, new AdvanceDecision())).Accepted && Canonical.Hash(session.Capture()) == before, "stale rejection atomicity");
Check(!session.Submit(new Request("bad", 0, new SigningDecision("missing"))).Accepted && Canonical.Hash(session.Capture()) == before, "invalid ID atomicity");
var coach = new Request("authority", 0, new CoachDecision(Delegation.Autonomous, Risk.Balanced));
Check(session.Submit(coach).Accepted, "coach authority commit");
var after = Canonical.Hash(session.Capture());
Check(session.Submit(coach).Accepted && Canonical.Hash(session.Capture()) == after, "exact retry idempotency");
Check(!session.Submit(coach with { Decision = new CoachDecision(Delegation.Manual, Risk.Balanced) }).Accepted, "conflicting reuse rejected");
Check(session.Submit(new Request("advance:1", 1, new AdvanceDecision())).Accepted, "first full management cycle");
Check(session.Observe().Results.Length == 1 && session.Observe().Day == 6, "first result checkpoint");
Check(session.Submit(new Request("advance:2", 2, new AdvanceDecision())).Accepted, "second consecutive management cycle");
Check(session.Observe().Results.Length == 2 && session.Observe().Day == 12, "second result checkpoint");
var repeat = New(); repeat.Submit(coach); repeat.Submit(new Request("advance:1", 1, new AdvanceDecision())); repeat.Submit(new Request("advance:2", 2, new AdvanceDecision()));
Check(Canonical.Hash(repeat.Capture()) == Canonical.Hash(session.Capture()), "same seed and commands equal authoritative hash");
var result = session.Capture().World.Results[0]; var fixture = content.Definition.Fixtures[0];
var company = session.Capture().Company;
Check(Finance.Consume(company, result, fixture) == company && Commercial.Consume(company,result,content.Definition.Balance) == company, "consumer idempotency");
var observed = Canonical.Json(session.Observe());
Check(!observed.Contains("Probability") && !observed.Contains("Strength") && !observed.Contains("Variance"), "observations exclude developer truth");
for(var i=0;i<50;i++) session.Observe();
Check(Canonical.Hash(repeat.Capture()) == Canonical.Hash(session.Capture()), "observation does not mutate or consume RNG");
var testDirectory = Path.GetFullPath("artifacts/save-tests-" + Guid.NewGuid().ToString("N"));
var store = new SnapshotStore(testDirectory, content);
store.Save("roundtrip", session.Capture());
var loaded = store.Load("roundtrip");
Check(Canonical.Hash(loaded) == Canonical.Hash(session.Capture()), "explicit DTO disk round trip");
var continued = new Session(content.Definition, loaded, store);
var nextCommand = new Request("advance:3",3,new AdvanceDecision());
Check(continued.Submit(nextCommand).Accepted && session.Submit(nextCommand).Accepted && Canonical.Hash(continued.Capture()) == Canonical.Hash(session.Capture()), "loaded continuation equals uninterrupted");
store.Save("roundtrip",continued.Capture());
Check(Canonical.Hash(store.Load("roundtrip",true)) == Canonical.Hash(loaded), "explicit backup preserves previous revision");
var liveHash = Canonical.Hash(continued.Capture());
File.WriteAllText(store.PrimaryPath("roundtrip"),"{corrupt");
Check(!continued.Load("roundtrip").Accepted && Canonical.Hash(continued.Capture()) == liveHash, "corrupt load preserves valid session");
var corruptBytes = File.ReadAllBytes(store.PrimaryPath("roundtrip"));
Check(!continued.Save("roundtrip").Accepted && File.ReadAllBytes(store.PrimaryPath("roundtrip")).SequenceEqual(corruptBytes), "corrupt primary cannot replace backup evidence");
Check(continued.Load("roundtrip",true).Accepted && Canonical.Hash(continued.Capture()) == Canonical.Hash(loaded), "explicit recovery loads valid backup");

// Save every executable phase boundary of a whole match, including its immediate consumers.
var phaseState = New().Capture();
var plan = Observations.Build(phaseState,content.Definition).Recommendation!;
phaseState = Simulation.Apply(phaseState,new CommitPlan(new Plan(plan.FixtureId,plan.Execution,plan.Opponent,plan.Meta,(Posture)plan.Posture,plan.Lineup,Posture.Balanced,"phase-plan")),content.Definition,"phase-plan");
var boundaries = 0;
while(phaseState.World.Results.Length == 0)
{
    store.Save("phase",phaseState);
    var resume = store.Load("phase");
    var uninterrupted = Simulation.Step(phaseState,content.Definition);
    Check(Canonical.Hash(Simulation.Step(resume,content.Definition)) == Canonical.Hash(uninterrupted), "phase save continuation " + boundaries);
    phaseState = uninterrupted; boundaries++;
}
store.Save("phase",phaseState); Check(Canonical.Hash(store.Load("phase")) == Canonical.Hash(phaseState), "result plus immediate consumers saved atomically");

var unsigned = New(); var cashBefore = unsigned.Capture().Company.Cash;
Check(unsigned.Submit(new Request("sponsor",0,new SponsorDecision("offer:atlas"))).Accepted && unsigned.Capture().Company.Cash == cashBefore, "signed sponsorship creates receivables, no immediate cash");
Check(unsigned.Capture().Company.FinancialItems.Any(x=>x.CauseId=="agreement:offer:atlas"&&x.DueDay==4), "sponsor timing explicit");
Check(unsigned.Submit(new Request("sign",1,new SigningDecision("person:star"))).Accepted && !unsigned.Capture().World.Candidates.Any(x=>x.Person.Id=="person:star"), "recruitment transfers sole person ownership");
SnapshotValidation.Validate(unsigned.Capture(),content);
Check(unsigned.Submit(new Request("release",2,new ReleaseDecision("person:sub"))).Accepted && unsigned.Capture().World.Candidates.Any(x=>x.Person.Id=="person:sub"), "release transfers ownership and sacrifices roster depth");
SnapshotValidation.Validate(unsigned.Capture(),content);

// Information affects estimates, never the competitive resolver's true inputs or result draw.
var observedState = New().Capture(); var upgraded = observedState with { Company=observedState.Company with { Information=95 } };
Check(Observations.Build(observedState,content.Definition).OpponentEstimate != Observations.Build(upgraded,content.Definition).OpponentEstimate && upgraded.Company.People.SequenceEqual(observedState.Company.People) && upgraded.World.Rivals.SequenceEqual(observedState.World.Rivals), "information narrows estimates without changing true assets");
// Manual and delegated identical plans have identical competitive/economic facts; authority/receipt hashes deliberately differ.
var manual=New(); var automatic=New(); automatic.Submit(new Request("a",0,new CoachDecision(Delegation.Autonomous,Risk.Aggressive)));
var recommended=manual.Observe().Recommendation!;
manual.Submit(new Request("p",0,new PreparationDecision(recommended.FixtureId,recommended.Execution,recommended.Opponent,recommended.Meta,recommended.Posture,recommended.Lineup)));
manual.Submit(new Request("go",1,new AdvanceDecision())); automatic.Submit(new Request("go",1,new AdvanceDecision()));
Check(manual.Capture().World.Results[0] with { PlanCause="" } == automatic.Capture().World.Results[0] with { PlanCause="" } && manual.Capture().Company.Cash==automatic.Capture().Company.Cash, "manual/delegated competitive and finance parity");
foreach(var culture in new[]{"en-US","tr-TR","vi-VN"})
{
    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
    var alternate=New(); alternate.Submit(coach); alternate.Submit(new Request("advance:1",1,new AdvanceDecision())); alternate.Submit(new Request("advance:2",2,new AdvanceDecision()));
    Check(Canonical.Hash(alternate.Capture())==Canonical.Hash(repeat.Capture()),"culture-independent replay " + culture);
}
var permutedContent=content.Definition with { Players=content.Definition.Players.Reverse().ToImmutableArray(), Rivals=content.Definition.Rivals.Reverse().ToImmutableArray(), Fixtures=content.Definition.Fixtures.Reverse().ToImmutableArray(), Offers=content.Definition.Offers.Reverse().ToImmutableArray() };
Check(Canonical.Hash(CampaignFactory.Create(permutedContent,content.Hash,123456789,"campaign:fixture"))==Canonical.Hash(New().Capture()),"shuffled definition order has canonical initial state");
var lowInfoState=New().Capture(); var lowInfoSession=new Session(content.Definition,lowInfoState with { Company=lowInfoState.Company with { Information=30 } });
lowInfoSession.Submit(new Request("a",0,new CoachDecision(Delegation.Autonomous,Risk.Conservative))); var escalationHash=Canonical.Hash(lowInfoSession.Capture());
Check(!lowInfoSession.Submit(new Request("go",1,new AdvanceDecision())).Accepted && Canonical.Hash(lowInfoSession.Capture())==escalationHash,"uncertain coach escalates without partial mutation");
while(!session.Observe().Finished) { var v=session.Observe(); Check(session.Submit(new Request("finish:"+v.Revision,v.Revision,new AdvanceDecision())).Accepted,"continue bounded campaign"); }
Check(session.Observe().Results.Length==4 && session.Observe().Day==29,"all four cycles and final business consequences resolved");
SnapshotValidation.Validate(session.Capture(),content);
// Recomputed checksums are not a substitute for semantic validation.
void RejectSave(string name, Func<SaveEnvelope,SaveEnvelope> change)
{
    store.Save(name, loaded);
    var envelope=StrictJson.Read<SaveEnvelope>(File.ReadAllBytes(store.PrimaryPath(name)));
    envelope=change(envelope); envelope=envelope with { Checksum="" };
    envelope=envelope with { Checksum=Canonical.Digest(Canonical.Json(envelope)) };
    File.WriteAllText(store.PrimaryPath(name),Canonical.Json(envelope));
    var currentHash=Canonical.Hash(continued.Capture());
    Check(!continued.Load(name).Accepted && Canonical.Hash(continued.Capture())==currentHash,"invalid save leaves session intact: " + name);
}
RejectSave("schema", e=>e with { Schema=2 });
RejectSave("content", e=>e with { ContentHash=new string('0',64) });
RejectSave("cursor", e=>e with { Snapshot=e.Snapshot with { World=e.Snapshot.World with { Calendar=e.Snapshot.World.Calendar with { Phase=(Phase)99 } } } });
RejectSave("cash", e=>e with { Snapshot=e.Snapshot with { Company=e.Snapshot.Company with { Cash=e.Snapshot.Company.Cash+1 } } });
RejectSave("owner", e=>e with { Snapshot=e.Snapshot with { Company=e.Snapshot.Company with { People=e.Snapshot.Company.People.Add(e.Snapshot.Company.People[0]) } } });
RejectSave("receipts", e=>e with { Snapshot=e.Snapshot with { Company=e.Snapshot.Company with { FinanceReceipts=[] } } });
store.Save("checksum",loaded); File.AppendAllText(store.PrimaryPath("checksum"),"!");
Check(!continued.Load("checksum").Accepted,"trailing-corruption rejection");
store.Save("locked",loaded); var lockBytes=File.ReadAllBytes(store.PrimaryPath("locked"));
using(var held=new FileStream(store.PrimaryPath("locked")+".lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None))
    Check(!continued.Save("locked").Accepted,"exclusive slot lock rejects concurrent writer");
Check(File.ReadAllBytes(store.PrimaryPath("locked")).SequenceEqual(lockBytes),"failed write preserves primary bytes");
Check(!continued.Save("../escape").Accepted,"save slot traversal rejected");
try { StrictJson.Read<Content>(System.Text.Encoding.UTF8.GetBytes("{\"Schema\":1,\"Schema\":2}")); Check(false,"duplicate key rejection"); } catch(InvalidDataException) { Check(true,"duplicate key rejection"); }
try { ContentLoader.Validate(content.Definition with { Fixtures=content.Definition.Fixtures.Add(content.Definition.Fixtures[0]) }); Check(false,"duplicate content IDs"); } catch(InvalidDataException) { Check(true,"duplicate content IDs"); }

var distressContent=content.Definition with { Balance=content.Definition.Balance with { StartingCash=15000 } };
var distressed=new Session(distressContent,CampaignFactory.Create(distressContent,"development-distress-fixture",42,"campaign:distress"));
distressed.Submit(new Request("coach",0,new CoachDecision(Delegation.Autonomous,Risk.Balanced)));
Check(distressed.Submit(new Request("go",1,new AdvanceDecision())).Accepted && distressed.Observe().Status=="Distress","low liquidity reaches a material distress checkpoint");
var missed=distressed.Capture().Company.FinancialItems.Where(i=>i.MissedDay is not null).Select(i=>i.Id).ToArray();
Check(missed.Length>0,"unpaid items retain original missed date");
var recoverView=distressed.Observe();
Check(distressed.Submit(new Request("recover",recoverView.Revision,new SponsorDecision("offer:local"))).Accepted,"recovery commits scarce sponsor opportunity and load");
for(var tries=0;tries<6 && distressed.Observe().Results.Length<1;tries++)
{ var v=distressed.Observe(); Check(distressed.Submit(new Request("resume:"+tries,v.Revision,new AdvanceDecision())).Accepted,"distress resumes without repeated completed phases"); }
Check(distressed.Observe().Results.Length==1 && distressed.Capture().Company.FinancialItems.Where(i=>missed.Contains(i.Id)).All(i=>i.MissedDay is not null),"recovery retains late-payment history after settlement");

for(ulong seed=1;seed<=16;seed++)
{
    Session Seeded()=>new(content.Definition,CampaignFactory.Create(content.Definition,content.Hash,seed,"seeded"));
    var left=Seeded(); var right=Seeded();
    foreach(var request in new[]{new Request("a",0,new CoachDecision(Delegation.Autonomous,Risk.Aggressive)),new Request("b",1,new AdvanceDecision()),new Request("c",2,new AdvanceDecision())})
    { left.Submit(request); right.Observe(); right.Submit(request); Check(Canonical.Hash(left.Capture())==Canonical.Hash(right.Capture()),$"seed {seed} transition {request.Id}"); }
}
Console.WriteLine($"INTEGRATION PASS {count} phaseBoundaries={boundaries} hash={Canonical.Hash(session.Capture())}");
