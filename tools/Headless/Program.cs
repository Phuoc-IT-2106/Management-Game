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
void Send(Decision decision)
{
    var response = session.Submit(new Request("ui:" + ++id, session.Observe().Revision, decision));
    if (!response.Accepted) throw new Exception(response.Message);
    hashes.Add(Canonical.Hash(session.Capture()));
}
Send(new SponsorDecision("offer:atlas")); Send(new SigningDecision("person:star"));
Send(new CoachDecision(Delegation.Recommend, Risk.Balanced));
Send(new PreparationDecision("fixture:01",70,20,10,Risk.Balanced,["person:a","person:star","person:c","person:d","person:e"]));
Send(new AdvanceDecision()); Send(new CoachDecision(Delegation.Autonomous,Risk.Aggressive)); Send(new AdvanceDecision());
if (session.Observe().Results.Length != 2) throw new Exception("Two-cycle scenario failed.");
Console.WriteLine("HEADLESS_TWO_CYCLE_PASS hash=" + hashes[^1]);
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
