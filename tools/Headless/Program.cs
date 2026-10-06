using ManagementGame.Application;
using ManagementGame.Domain;
using ManagementGame.Infrastructure;
using System.Collections.Immutable;
using System.Text.Json;
using System.Diagnostics;

var content = ContentLoader.Load("content/fixture.json");
var session = new Session(content.Definition, CampaignFactory.Create(content.Definition, content.Hash, 20261004, "campaign:development"));
var hashes = new List<string> { Canonical.Hash(session.Capture()) };
long id = 0;
Response Send(Decision decision, bool required = true)
{
    var response = session.Submit(new Request("ui:" + ++id, session.Observe().Revision, decision));
    if (required && !response.Accepted) throw new Exception(response.Message);
    hashes.Add(Canonical.Hash(session.Capture()));
    return response;
}
// Identities are generated per seed, so every choice is read from the observation, never hard-coded.
void AdvanceUntil(int results) { for (var i = 0; i < 20 && session.Observe().Results.Length < results; i++) Send(new AdvanceDecision()); }
var opening = session.Observe();
Send(new SponsorDecision(opening.Offers.First(o => o.Availability == "Available").Id));
Send(new SigningDecision(opening.Candidates[0].Id));
Send(new CoachDecision(Delegation.Recommend, Risk.Balanced));
var draft = session.Observe().Recommendation!;
Send(new PreparationDecision(draft.FixtureId,70,20,10,Risk.Balanced,draft.Lineup));
AdvanceUntil(1); Send(new CoachDecision(Delegation.Autonomous,Risk.Aggressive)); AdvanceUntil(2);
if (session.Observe().Results.Length != 2) throw new Exception("Two-cycle scenario failed.");
Console.WriteLine("HEADLESS_TWO_CYCLE_PASS hash=" + hashes[^1]);
if (args.FirstOrDefault(a => a.StartsWith("--seasons=", StringComparison.Ordinal)) is { } seasonArg)
{
    // Exploration: a simple owner policy plays several seasons to expose economy and loop pressure.
    var target = int.Parse(seasonArg[10..]);
    var lastSeason = session.Observe().Season;
    for (var guard = 0; guard < 4000 && session.Observe().Season <= target; guard++)
    {
        var v = session.Observe();
        foreach (var offer in v.Offers.Where(o => o.Availability == "Available").OrderByDescending(o => o.Payment).Take(1)) Send(new SponsorDecision(offer.Id), false);
        foreach (var person in v.People.Where(p => p.CanRenew && p.Execution >= 66)) Send(new RenewalDecision(person.Id, 1), false);
        if (v.CoachContract.CanRenew) Send(new RenewalDecision(v.CoachContract.Id, 1), false);
        foreach (var role in "ABCDE".Select(r => r.ToString()).Where(r => !v.People.Any(p => p.Role == r)))
            if (v.Candidates.FirstOrDefault(c => c.Role == role) is { } cover) Send(new SigningDecision(cover.Id), false);
        var advanced = Send(new AdvanceDecision(), false);
        if (!advanced.Accepted) { Console.WriteLine("BLOCKED day " + v.Day + ": " + advanced.Message); break; }
        var now = session.Observe();
        if (now.Season != lastSeason)
        {
            var record = now.Results.Where(r => r.Day > (lastSeason - 1) * now.SeasonLength && r.Day <= lastSeason * now.SeasonLength).ToArray();
            Console.WriteLine($"SEASON {lastSeason}: {record.Count(r => r.Result == "Victory")}-{record.Count(r => r.Result != "Victory")} cash={now.Cash / 100m:N0} rep={now.Reputation} aud={now.Audience} sponsors={now.ActiveSponsors} roster={now.People.Length} coach={now.Coach} status={now.Status} items={session.Capture().Company.FinancialItems.Length} reviews={session.Capture().Company.Reviews.Length}");
            lastSeason = now.Season;
        }
    }
}
var output = args.FirstOrDefault(a => a.StartsWith("--output=", StringComparison.Ordinal))?[9..];
if (output is not null) File.WriteAllText(output, JsonSerializer.Serialize(new { Hashes = hashes, FinalHash = hashes[^1], Results = session.Observe().Results.Length }, new JsonSerializerOptions { WriteIndented = true }));
if (args.Contains("--scale"))
{
    var reports = new List<object>();
    foreach (var size in new[] { 1000,10000,100000 })
    {
        var rows = Enumerable.Range(0,size).Select(i => new PersonRow($"person:{i:D6}","Player " + (i%1000), "ABCDE"[i%5].ToString(),70,i%100,i*10L,0,true)).ToImmutableArray();
        var samples = new List<double>(); var retained = GC.GetTotalMemory(true);
        for(var i=0;i<35;i++)
        {
            var timer = Stopwatch.StartNew();
            var result = RosterTable.Query(rows, new RosterQuery(i%2==0?"Player":"7",i%3==0?"B":"",i%2==0?"Salary":"Name",i%2==0,i%10),"person:000007");
            if(result.Rows.Length>25 || result.SelectedId != "person:000007") throw new Exception("Scale query correctness failed.");
            if(i>=5) samples.Add(timer.Elapsed.TotalMilliseconds);
        }
        reports.Add(new { Rows=size, Samples=samples, P95Ms=samples.Order().ElementAt(28), RetainedBefore=retained, RetainedAfter=GC.GetTotalMemory(true), BoundRows=25 });
    }
    var scaleOutput = args.FirstOrDefault(a => a.StartsWith("--scale-output=",StringComparison.Ordinal))?[15..] ?? "artifacts/query-scale.json";
    File.WriteAllText(scaleOutput,JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("QUERY_SCALE_PASS " + scaleOutput);
}
