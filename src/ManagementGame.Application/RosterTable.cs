using System.Collections.Immutable;

namespace ManagementGame.Application;

public sealed record RosterQuery(string Search = "", string Role = "", string Sort = "Name", bool Descending = false, int Page = 0, int PageSize = 25);
public sealed record RosterPage(ImmutableArray<PersonRow> Rows, int Total, int Page, string? SelectedId);
public static class RosterTable
{
    public static RosterPage Query(ImmutableArray<PersonRow> source, RosterQuery query, string? selectedId)
    {
        var size = Math.Clamp(query.PageSize, 1, 100);
        var filtered = source.Where(p => (query.Role.Length == 0 || p.Role == query.Role) &&
            (p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) || p.Id.Contains(query.Search, StringComparison.OrdinalIgnoreCase)));
        IOrderedEnumerable<PersonRow> ordered = query.Sort switch
        {
            "Salary" => query.Descending ? filtered.OrderByDescending(p => p.Salary) : filtered.OrderBy(p => p.Salary),
            "Readiness" => query.Descending ? filtered.OrderByDescending(p => p.Readiness) : filtered.OrderBy(p => p.Readiness),
            _ => query.Descending ? filtered.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase) : filtered.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
        };
        var rows = ordered.ThenBy(p => p.Id, StringComparer.Ordinal).ToArray();
        var page = Math.Clamp(query.Page, 0, Math.Max(0, (rows.Length - 1) / size));
        return new RosterPage(rows.Skip(page * size).Take(size).ToImmutableArray(), rows.Length, page,
            selectedId is not null && source.Any(p => p.Id == selectedId) ? selectedId : null);
    }
}
