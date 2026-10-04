using System.Globalization;

// Synthetic observation records only; no simulation authority or engine types.
public sealed record FakeRow(int Id, string Name, string Group, int Score, long Amount,
    bool Active, DateOnly Date, int? Optional, string Region, int Rank, int Load, string Note)
{
    public string[] Cells() => [Id.ToString(), Name, Group, Score.ToString(),
        Amount.ToString("N0", CultureInfo.InvariantCulture), Active ? "yes" : "no",
        Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), Optional?.ToString() ?? "unknown",
        Region, Rank.ToString(), Load.ToString(), Note];
}

public static class SyntheticTable
{
    public static FakeRow[] Generate(int count) => Enumerable.Range(1, count).Select(i =>
        new FakeRow(i, $"Fixture {i:D6}" + (i % 37 == 0 ? " — deliberately long synthetic name" : ""),
            $"G{i % 7}", i * 31 % 101, (long)(i % 99 - 49) * 100, i % 3 != 0,
            new DateOnly(2020, 1, 1).AddDays(i % 365), i % 11 == 0 ? null : i % 17,
            $"R{i % 4}", i % 9, i % 100, i % 23 == 0 ? "Expanded text / synthetic only" : "—")).ToArray();

    public static FakeRow[] Query(FakeRow[] rows, int column, bool descending, string search, bool active)
    {
        var result = rows.Where(r => (!active || r.Active) &&
            r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
        Array.Sort(result, (a, b) => Compare(a, b, column, descending));
        return result;
    }

    public static int Compare(FakeRow a, FakeRow b, int column, bool descending)
    {
        // Unknown values remain last in both directions; ties use stable ID.
        if (column == 7 && a.Optional.HasValue != b.Optional.HasValue)
            return a.Optional.HasValue ? -1 : 1;
        int value = column switch
        {
            0 => a.Id.CompareTo(b.Id), 1 => StringComparer.OrdinalIgnoreCase.Compare(a.Name,b.Name),
            2 => StringComparer.Ordinal.Compare(a.Group,b.Group), 3 => a.Score.CompareTo(b.Score),
            4 => a.Amount.CompareTo(b.Amount), 5 => a.Active.CompareTo(b.Active),
            6 => a.Date.CompareTo(b.Date), 7 => Nullable.Compare(a.Optional,b.Optional),
            8 => StringComparer.Ordinal.Compare(a.Region,b.Region), 9 => a.Rank.CompareTo(b.Rank),
            10 => a.Load.CompareTo(b.Load), 11 => StringComparer.Ordinal.Compare(a.Note,b.Note),
            _ => throw new ArgumentOutOfRangeException(nameof(column))
        };
        return value == 0 ? a.Id.CompareTo(b.Id) : descending ? -value : value;
    }
}
