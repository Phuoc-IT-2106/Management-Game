using ManagementGame.Domain;
var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
Check(Numbers.Divide(5,2) == 2 && Numbers.Divide(7,2) == 4 && Numbers.Divide(-5,2) == -2, "ties-to-even signed rounding");
Check(KeyedRandom.Draw(123456789, "fixture", "cmd-000", "adjust") == 15711650815429184693UL, "independent SHA-256 rng-v1 golden vector");
Check(Enumerable.Range(0,1000).Select(i => KeyedRandom.Range(123,"test",i.ToString(),"range",7)).All(x => x >= 0 && x < 7), "bounded rejection sampling");
Check(typeof(Campaign).Assembly.GetReferencedAssemblies().All(a => a.Name!.StartsWith("System", StringComparison.Ordinal)), "Domain System-only assembly dependencies");
Console.WriteLine($"DOMAIN PASS {count}");
