using System.Globalization;
using System.Text.Json;
using Qualification.Application;
using Qualification.Domain;

string locale=args.Contains("--tr")?"tr-TR":"en-US";
CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(locale);
CultureInfo.CurrentUICulture=CultureInfo.CurrentCulture;
if(args.Contains("--tests"))
{
    int checks=0;
    void Check(bool condition,string name) { if(!condition) throw new Exception(name); checks++; Console.WriteLine($"PASS {name}"); }
    void Reject(Action action,string name) { try { action(); } catch(InvalidOperationException) { Check(true,name); return; } throw new Exception(name); }
    var original=Replay.Run();
    Check(KeyedRng.Draw(123456789,"fixture","cmd-000","adjust",0)==15711650815429184693UL,"RNG independent SHA256 golden vector");
    Check(original.Hashes.SequenceEqual(Replay.Run(true,true).Hashes),"logging and insertion ordering");
    CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("tr-TR");
    Check(original.Hashes.SequenceEqual(Replay.Run().Hashes),"culture invariance");
    Check(!original.Hashes.SequenceEqual(Replay.Run(seed:123456788).Hashes),"seed changes outcome");
    var session=new Session(Fixture.Initial()); var first=Fixture.Journal()[0];
    string hash=session.Submit(first);
    Check(session.Submit(first)==hash,"exact retry idempotent");
    Reject(()=>session.Submit(first with {Delta=999}),"conflicting receipt rejected");
    Reject(()=>session.Submit(first with {Id="stale"}),"stale command rejected");
    Check(Fixture.Hash(session.Current)==hash,"rejections preserve committed state");
    var overflow=new Session(Fixture.Initial() with {Company=new(long.MaxValue)});
    string before=Fixture.Hash(overflow.Current);
    try { overflow.Submit(first with {Delta=100}); throw new Exception("expected overflow"); } catch(OverflowException) {}
    Check(Fixture.Hash(overflow.Current)==before,"overflow aborts whole transaction");
    foreach(int cut in Enumerable.Range(0,33))
    {
        var a=new Session(Fixture.Initial()); foreach(var c in Fixture.Journal().Take(cut)) a.Submit(c);
        var b=new Session(a.Current); foreach(var c in Fixture.Journal().Skip(cut)) b.Submit(c);
        Check(Fixture.Hash(b.Current)==original.FinalHash,$"immutable boundary resume {cut}");
    }
    var references=typeof(Fixture).Assembly.GetReferencedAssemblies().Select(r=>r.Name!).Order().ToArray();
    Check(references.All(r=>r.StartsWith("System",StringComparison.Ordinal)),"Domain assembly reference allowlist");
    Console.WriteLine("DOMAIN_REFERENCES "+string.Join(",",references));
    Console.WriteLine($"RNG_VECTOR {KeyedRng.Draw(123456789,"fixture","cmd-000","adjust",0)}");
    Console.WriteLine($"QG03_TESTS_PASS {checks}");
}
else
{
    var result=Replay.Run(args.Contains("--log"),args.Contains("--reverse"));
    string output=args.Single(a=>a.StartsWith("--output=",StringComparison.Ordinal))[9..];
    File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
    Console.WriteLine($"QG03_RUN {locale} {result.FinalHash}");
}
