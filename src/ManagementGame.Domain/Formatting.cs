using System.Globalization;
using System.Text.RegularExpressions;

// Player-facing number text. It lives in the Domain assembly because notices stored in campaign state carry amounts, and
// every layer above (Application read models, the Godot client) formats through the same two classes. Its own namespace
// keeps UI code from importing Domain types just to format a number.
namespace ManagementGame.Formatting;

/// <summary>The one formatter for player-facing money. The simulation and saves keep amounts as integer minor units
/// (cents); only this class turns them into text. The currency is the US dollar: a nominal 1:1 relabel of the earlier
/// abstract currency unit ("CU"), so no amount is converted or rebalanced. Integer arithmetic and the invariant culture
/// only, so text never depends on the host and never carries floating-point error.</summary>
public static class Currency
{
    public const string Code = "USD", Symbol = "$";
    /// <summary>A true minus sign (U+2212): as wide as the plus, so signed amounts line up in tabular figures.</summary>
    public const string Minus = "−", Plus = "+";
    public const long MinorPerUnit = 100;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Exact amount: whole dollars without decimals, otherwise dollars and cents. "$2,476", "$9.80", "−$920".</summary>
    public static string Format(long minor) => Compose(minor, Units(minor), Remainder(minor) == 0 ? null : Remainder(minor), signed: false);
    /// <summary>Dollars and cents always, for columns of exact amounts that must line up. "$2,476.00", "$9.80".</summary>
    public static string Cents(long minor) => Compose(minor, Units(minor), Remainder(minor), signed: false);
    /// <summary>Rounded to whole dollars (half away from zero) for glanceable summaries; the exact amount stays available
    /// where the decision is taken. "$2,476".</summary>
    public static string Whole(long minor) => Compose(Rounded(minor) == 0 ? 0 : minor, Rounded(minor), null, signed: false);
    /// <summary>A change in money, always with its sign: "+$765", "−$920", "$0". Whole dollars unless <paramref name="exact"/>.</summary>
    public static string Signed(long minor, bool exact = false) => exact
        ? Compose(minor, Units(minor), Remainder(minor) == 0 ? null : Remainder(minor), signed: true)
        : Compose(Rounded(minor) == 0 ? 0 : minor, Rounded(minor), null, signed: true);
    /// <summary>Large amounts in millions or billions where space is short ("$1.25M", "$12.5M", "$3B"); below a million
    /// the whole-dollar amount. Never used where the exact figure is being decided.</summary>
    public static string Compact(long minor)
    {
        var dollars = Rounded(minor);
        if (dollars < 1_000_000) return Whole(minor);
        var (scale, suffix) = dollars >= 1_000_000_000 ? (1_000_000_000UL, "B") : (1_000_000UL, "M");
        // Hundredths of the scale, rounded half away from zero, then trailing zeros dropped: 1.25M, 12.5M, 3M.
        var hundredths = (dollars * 100 + scale / 2) / scale;
        var text = (hundredths / 100).ToString("N0", Invariant);
        var fraction = (hundredths % 100).ToString("D2", Invariant).TrimEnd('0');
        return (minor < 0 ? Minus : "") + Symbol + text + (fraction.Length > 0 ? "." + fraction : "") + suffix;
    }
    /// <summary>A daily rate, exact: "$9.80/day". A word joiner after the slash keeps the rate on one line.</summary>
    public static string PerDay(long minor) => Format(minor) + Per + "day";
    /// <summary>A weekly rate, exact: "$300/week".</summary>
    public static string PerWeek(long minor) => Format(minor) + Per + "week";
    /// <summary>The rate slash followed by U+2060 WORD JOINER: line breaking may not split "$9.80/" from "day".</summary>
    public const string Per = "/\u2060";

    // Notices written before the USD relabel are stored in saves as "1,234.50 CU" (or "-1,234.50 CU"). They are shown
    // through the current formatter; the stored text, and with it the save and its hash, is never rewritten.
    private static readonly Regex Legacy = new(@"(?<![\w.,])(-?)(\d{1,3}(?:,\d{3})*|\d+)\.(\d{2}) CU\b", RegexOptions.CultureInvariant);
    /// <summary>Stored campaign text in the current currency format. Text without legacy amounts is returned unchanged.</summary>
    public static string Relabel(string stored) => stored.Contains(" CU", StringComparison.Ordinal)
        ? Legacy.Replace(stored, m =>
        {
            var minor = long.Parse(m.Groups[2].Value.Replace(",", "", StringComparison.Ordinal), Invariant) * MinorPerUnit
                + long.Parse(m.Groups[3].Value, Invariant);
            return Format(m.Groups[1].Length > 0 ? -minor : minor);
        })
        : stored;

    private static ulong Magnitude(long minor) => minor < 0 ? (ulong)(-(minor + 1)) + 1 : (ulong)minor;
    private static ulong Units(long minor) => Magnitude(minor) / MinorPerUnit;
    private static ulong Remainder(long minor) => Magnitude(minor) % MinorPerUnit;
    private static ulong Rounded(long minor) => (Magnitude(minor) + MinorPerUnit / 2) / MinorPerUnit;
    private static string Compose(long sign, ulong units, ulong? cents, bool signed) =>
        (sign < 0 ? Minus : signed && sign > 0 ? Plus : "") + Symbol + units.ToString("N0", Invariant)
        + (cents is { } c ? "." + c.ToString("D2", Invariant) : "");
}

/// <summary>Player-facing counts, records, percentages and day positions: one style everywhere (invariant culture,
/// thousands separators, an en dash in records, no arbitrary decimals).</summary>
public static class Figures
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    /// <summary>"1,250".</summary>
    public static string Count(long value) => value.ToString("N0", Invariant);
    /// <summary>A count in a small fixed space: "99+" above the cap.</summary>
    public static string Capped(long value, int cap = 99) => value > cap ? Count(cap) + "+" : Count(value);
    /// <summary>Won–lost record with an en dash: "5–3".</summary>
    public static string Record(int won, int lost) => Count(won) + "–" + Count(lost);
    /// <summary>Whole percent of a part, half away from zero: "63%"; "—" when there is nothing to divide.</summary>
    public static string Percent(long part, long whole) => whole <= 0 ? "—"
        : Count((long)Math.Round(part * 100m / whole, MidpointRounding.AwayFromZero)) + "%";
    /// <summary>A position within a known length: "6 / 56".</summary>
    public static string OfTotal(int value, int total) => Count(value) + " / " + Count(total);
}
