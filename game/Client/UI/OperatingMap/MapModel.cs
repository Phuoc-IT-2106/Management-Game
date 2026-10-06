using System.Collections.Immutable;
using ManagementGame.UiKit;

namespace ManagementGame.OperatingMap;

public enum ScopeKind { Company, Portfolio, Division, Discipline, Team, Function }
public enum Attention { Critical, High, Normal, Background }
public sealed record MapScope(string Id, string? ParentId, string Name, ScopeKind Kind, string Description);
public sealed record ScopeLink(string From, string To, string Meaning);
public sealed record MapSituation(string Id, string ScopeId, string Name, string Category, Attention Priority,
    string Status, int? DueDay, string Reason, string Consequence, string TargetId, string WorkspaceKind,
    InformationState Information, string Evidence);
public sealed record MapSnapshot(string SessionKey, long Revision, OrganizationIdentityView Organization,
    int Day, int? NextCheckpoint, ImmutableArray<MapScope> Scopes, ImmutableArray<ScopeLink> Links,
    ImmutableArray<MapSituation> Situations)
{
    public MapScope Scope(string id) => Scopes.Single(x => x.Id == id);
    // Only this approved, bounded pair can share a row. Branches retain separate rows.
    public bool CompactDevelopmentPath => Scopes.Any(x => x.Id == MapFixtures.Esports) &&
        Scopes.Any(x => x.Id == MapFixtures.Discipline && x.ParentId == MapFixtures.Esports) &&
        Scopes.Count(x => x.ParentId == MapFixtures.Esports) == 1 &&
        Scopes.Count(x => x.ParentId == MapFixtures.Discipline) == 1 &&
        !Situations.Any(x => x.ScopeId is MapFixtures.Esports or MapFixtures.Discipline);
    public ImmutableArray<MapScope> Path(string id)
    {
        var result = new List<MapScope>();
        for (var scope = Scope(id); ; scope = Scope(scope.ParentId!))
        { result.Add(scope); if (scope.ParentId is null) break; }
        result.Reverse(); return [.. result];
    }
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SessionKey) || Revision < 0 || Scopes.Length is < 1 or > 8 || Situations.Length > 4)
            throw new ArgumentException("Snapshot exceeds the bounded prototype contract.");
        var ids = Scopes.Select(x => x.Id).Concat(Situations.Select(x => x.Id)).ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length ||
            Scopes.Count(x => x.ParentId is null) != 1 || Scope(Organization.Id).Kind != ScopeKind.Company)
            throw new ArgumentException("Invalid stable IDs or company root.");
        foreach (var scope in Scopes)
        {
            var visited = new HashSet<string>(); var current = scope;
            while (current.ParentId is { } parent)
            {
                if (!visited.Add(current.Id) || Scopes.All(x => x.Id != parent)) throw new ArgumentException("Invalid ancestry.");
                current = Scope(parent);
            }
            if (current.Id != Organization.Id) throw new ArgumentException("Disconnected company context.");
        }
        if (Situations.Any(x => Scopes.All(s => s.Id != x.ScopeId) || string.IsNullOrWhiteSpace(x.TargetId) || string.IsNullOrWhiteSpace(x.WorkspaceKind)) ||
            Links.Any(x => Scopes.All(s => s.Id != x.From) || Scopes.All(s => s.Id != x.To)))
            throw new ArgumentException("Unresolved scope reference.");
    }
}

public sealed record MapReturn(string CompanyId, string ScopeId, string? SituationId, bool ListMode, string FocusKey, int Scroll);

/// <summary>Local navigation only; snapshot is an immutable observation, never gameplay authority.</summary>
public sealed class MapNavigation
{
    /// <summary>Player-facing verb for entering a situation's decision workspace.</summary>
    public const string EntryLabel = "Review decision";
    public MapSnapshot Snapshot { get; private set; }
    public string ScopeId { get; private set; }
    public string? SituationId { get; private set; }
    public bool ListMode { get; set; }
    public MapReturn? Return { get; private set; }
    public string Notice { get; private set; } = "";
    public MapSituation? Situation => Snapshot.Situations.FirstOrDefault(x => x.Id == SituationId);
    public MapNavigation(MapSnapshot snapshot) { snapshot.Validate(); Snapshot = snapshot; ScopeId = snapshot.Organization.Id; }
    public bool Select(string id, long revision)
    {
        if (revision != Snapshot.Revision) { Notice = "Observed context has changed. Review the current projection."; return false; }
        if (Return is not null) return false;
        if (Snapshot.Situations.FirstOrDefault(x => x.Id == id) is { } matter)
        { ScopeId = matter.ScopeId; SituationId = matter.Id; Notice = ""; return true; }
        if (Snapshot.Scopes.Any(x => x.Id == id)) { ScopeId = id; SituationId = null; Notice = ""; return true; }
        Notice = "This context no longer exists. Select an available company scope."; return false;
    }
    public bool Enter(string targetId, long revision, string focusKey, int scroll)
    {
        if (Return is not null || revision != Snapshot.Revision || Situation is not { } matter || matter.TargetId != targetId) return false;
        Return = new(Snapshot.Organization.Id, ScopeId, SituationId, ListMode, focusKey, scroll); return true;
    }
    public MapReturn? Back()
    {
        if (Return is { } origin)
        { ScopeId = origin.ScopeId; SituationId = origin.SituationId; ListMode = origin.ListMode; Return = null; return origin; }
        if (SituationId is not null) SituationId = null;
        else ScopeId = Snapshot.Scope(ScopeId).ParentId ?? Snapshot.Organization.Id;
        Notice = ""; return null;
    }
    public void Replace(MapSnapshot next)
    {
        next.Validate();
        if (next.SessionKey == Snapshot.SessionKey && next.Revision < Snapshot.Revision) throw new ArgumentException("Stale projection rejected.");
        var oldPath = Snapshot.Path(ScopeId);
        var changedSession = next.SessionKey != Snapshot.SessionKey || next.Organization.Id != Snapshot.Organization.Id;
        Snapshot = next;
        // Any replacement invalidates workspace evidence, even at a surviving target.
        Return = null;
        if (changedSession)
        { ScopeId = next.Organization.Id; SituationId = null; ListMode = false; Notice = "Company session changed. Navigation was reset."; return; }
        if (next.Scopes.All(x => x.Id != ScopeId))
        { ScopeId = oldPath.Reverse().First(x => next.Scopes.Any(s => s.Id == x.Id)).Id; SituationId = null; Notice = "This scope no longer exists. Returned to its nearest available parent."; }
        else if (SituationId is not null && next.Situations.All(x => x.Id != SituationId))
        { SituationId = null; Notice = "This situation no longer exists. Its decision entry was cleared."; }
        else
        { if (Situation is { } matter) ScopeId = matter.ScopeId; Notice = "Context refreshed. Review current evidence before opening a decision entry."; }
    }
}
