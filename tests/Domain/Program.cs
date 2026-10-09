using ManagementGame.Domain;
using ManagementGame.Formatting;
var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
Check(Numbers.Divide(5,2) == 2 && Numbers.Divide(7,2) == 4 && Numbers.Divide(-5,2) == -2, "ties-to-even signed rounding");
Check(KeyedRandom.Draw(123456789, "fixture", "cmd-000", "adjust") == 15711650815429184693UL, "independent SHA-256 rng-v1 golden vector");
Check(Enumerable.Range(0,1000).Select(i => KeyedRandom.Range(123,"test",i.ToString(),"range",7)).All(x => x >= 0 && x < 7), "bounded rejection sampling");
Check(typeof(Campaign).Assembly.GetReferencedAssemblies().All(a => a.Name!.StartsWith("System", StringComparison.Ordinal)), "Domain System-only assembly dependencies");
// Player-facing numbers (ManagementGame.Formatting): integer minor units in, one style out. The USD relabel is nominal 1:1.
Check(Currency.Format(247_600) == "$2,476" && Currency.Format(980) == "$9.80" && Currency.Format(-92_000) == "−$920" && Currency.Format(0) == "$0"
    && Currency.Format(5) == "$0.05", "exact money: whole dollars without decimals, cents only when present, a true minus before the sign");
Check(Currency.Format(1_250_00) == "$1,250" && Currency.Format(12_500_00) == "$12,500" && Currency.Format(125_000_00) == "$125,000"
    && Currency.Format(1_250_000_00) == "$1,250,000", "money uses comma thousands separators");
Check(Currency.Cents(247_600) == "$2,476.00" && Currency.Cents(-4_537) == "−$45.37" && Currency.Cents(0) == "$0.00", "column money always carries cents");
Check(Currency.Whole(247_650) == "$2,477" && Currency.Whole(247_649) == "$2,476" && Currency.Whole(-247_650) == "−$2,477" && Currency.Whole(-49) == "$0",
    "summary money rounds half away from zero; a rounded zero has no sign");
Check(Currency.Signed(76_500) == "+$765" && Currency.Signed(-92_000) == "−$920" && Currency.Signed(0) == "$0" && Currency.Signed(-4_537, exact: true) == "−$45.37"
    && Currency.Signed(76_500, exact: true) == "+$765", "money changes always carry their sign");
Check(Currency.Compact(999_999_49) == "$999,999" && Currency.Compact(999_999_50) == "$1M" && Currency.Compact(1_250_000_00) == "$1.25M"
    && Currency.Compact(12_500_000_00) == "$12.5M" && Currency.Compact(-3_000_000_000_00) == "−$3B", "large money is compact only from a million");
Check(Currency.PerDay(980) == "$9.80" + Currency.Per + "day" && Currency.PerWeek(30_000) == "$300" + Currency.Per + "week" && Currency.Per == "/" + (char)0x2060,
    "rates name their period, joined so a line never breaks inside the rate");
Check(Currency.Format(long.MinValue) == "−$92,233,720,368,547,758.08" && Currency.Format(long.MaxValue) == "$92,233,720,368,547,758.07",
    "the extremes of the minor-unit range format exactly, without overflow");
Check(Currency.Relabel("A 140.00 CU fine is due tomorrow.") == "A $140 fine is due tomorrow."
    && Currency.Relabel("Proposed 1,234.50 CU per week; was -12.05 CU.") == "Proposed $1,234.50 per week; was −$12.05."
    && Currency.Relabel("CUP 3 CUSTOM 4.00 CUT") == "CUP 3 CUSTOM 4.00 CUT" && Currency.Relabel("Day 3") == "Day 3",
    "stored pre-relabel amounts read in dollars; other text is untouched");
Check(Figures.Count(125_000) == "125,000" && Figures.Capped(0) == "0" && Figures.Capped(12) == "12" && Figures.Capped(99) == "99" && Figures.Capped(100) == "99+"
    && Figures.Record(5, 3) == "5–3" && Figures.Percent(5, 8) == "63%" && Figures.Percent(0, 0) == "—" && Figures.OfTotal(6, 56) == "6 / 56",
    "counts, capped counts, records, percentages and positions share one style");
var hostCulture = System.Globalization.CultureInfo.CurrentCulture;
try
{
    System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("vi-VN");
    Check(Currency.Format(247_650) == "$2,476.50" && Figures.Count(1_250) == "1,250" && Figures.Percent(1, 3) == "33%", "number text never depends on the host culture");
}
finally { System.Globalization.CultureInfo.CurrentCulture = hostCulture; }
Console.WriteLine($"DOMAIN PASS {count}");
