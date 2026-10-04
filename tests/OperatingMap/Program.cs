using ManagementGame.OperatingMap;
using ManagementGame.UiKit;

var count = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
foreach (var scenario in MapFixtures.Cases)
{
    var fixture = MapFixtures.Create(scenario); fixture.Validate();
    Check(fixture.Organization.IsDevelopmentFixture && fixture.Scopes.Length == 8, "bounded noncanonical fixture " + scenario);
}
var source = MapFixtures.Create(); var nav = new MapNavigation(source);
Check(source.Path(MapFixtures.Team).Select(x => x.Id).SequenceEqual(new[] { MapFixtures.Company, MapFixtures.Portfolio, MapFixtures.Esports, MapFixtures.Discipline, MapFixtures.Team }), "ownership hierarchy");
Check(source.Situations.Select(x => x.Category).Distinct().Count() == 4, "four independent situation types");
foreach (var matter in source.Situations)
{
    Check(nav.Select(matter.Id, 7) && nav.ScopeId == matter.ScopeId, "situation attaches to affected scope " + matter.Id);
    nav.ListMode = true;
    Check(nav.Enter(matter.TargetId, 7, "affairs:" + matter.Id, 37), "typed entry " + matter.WorkspaceKind);
    Check(!nav.Enter(matter.TargetId, 7, "bad", 0) && !nav.Select(MapFixtures.Company, 7), "entry cannot overwrite origin");
    var returned = nav.Back(); Check(returned?.Scroll == 37 && returned.FocusKey == "affairs:" + matter.Id && nav.SituationId == matter.Id && nav.ListMode, "exact return tuple");
}
Check(!nav.Select("absent", 7) && !nav.Select(MapFixtures.Company, 6) && !nav.Enter("bad", 7, "", 0), "invalid and stale intents rejected");
nav.Select("matter:preparation", 7); nav.Enter("entry:preparation", 7, "map:matter:preparation", 0);
nav.Replace(source with { Revision = 8, Scopes = [.. source.Scopes.Where(x => x.Id != MapFixtures.Team)], Situations = [.. source.Situations.Where(x => x.ScopeId != MapFixtures.Team)], Links = [.. source.Links.Where(x => x.To != MapFixtures.Team)] });
Check(nav.ScopeId == MapFixtures.Discipline && nav.SituationId is null && nav.Return is null && nav.Notice.Contains("no longer exists"), "removed scope returns nearest valid ancestor and clears entry");
nav = new(source); nav.Select("matter:sponsor", 7); nav.Replace(source with { Revision = 8, Situations = [] });
Check(nav.ScopeId == MapFixtures.Business && nav.SituationId is null, "removed matter preserves valid scope");
try { nav.Replace(source); Check(false, "old snapshot rejected"); } catch (ArgumentException) { Check(nav.Snapshot.Revision == 8, "stale replacement atomic"); }
nav.Replace(source with { SessionKey = "loaded-session" });
Check(nav.ScopeId == source.Organization.Id && nav.SituationId is null && !nav.ListMode, "new/load session resets identical technical IDs");
nav.Select("matter:sponsor", 7); nav.Replace(source with { SessionKey = "loaded-session", Revision = 8, Organization = source.Organization with { DisplayName = "FIXTURE_ORGANIZATION" } });
Check(nav.ScopeId == MapFixtures.Business && nav.SituationId == "matter:sponsor", "rename preserves stable navigation");
nav.Back(); Check(nav.ScopeId == MapFixtures.Business && nav.SituationId is null, "back closes situation first");
nav.Back(); Check(nav.ScopeId == MapFixtures.Company, "back reaches company parent");
Check(MapFixtures.Create("empty").Situations.IsEmpty && MapFixtures.Create("quiet").Situations.All(x => x.Priority == Attention.Normal), "quiet and empty do not synthesize urgent alerts");
Check(source.Situations.Single(x => x.Id == "matter:preparation").Information == InformationState.Unknown, "unknown remains explicit without hidden numeric data");
Check(typeof(MapNavigation).Assembly.GetReferencedAssemblies().All(x => !x.Name!.StartsWith("ManagementGame.Domain") && !x.Name.StartsWith("ManagementGame.Application") && !x.Name.StartsWith("Godot")), "projection and navigation have no gameplay or engine dependency");
foreach (var invalid in new[] {
    source with { Scopes = source.Scopes.SetItem(1, source.Scopes[1] with { Id = MapFixtures.Company }) },
    source with { Scopes = source.Scopes.SetItem(1, source.Scopes[1] with { ParentId = MapFixtures.Team }) },
    source with { Situations = source.Situations.SetItem(0, source.Situations[0] with { ScopeId = "missing" }) } })
{
    try { invalid.Validate(); Check(false, "invalid projection must fail"); }
    catch (ArgumentException) { Check(true, "malformed projection rejected"); }
}
nav = new(source); nav.Select("matter:sponsor", 7); nav.Enter("entry:sponsor", 7, "map:matter:sponsor", 0);
nav.Replace(source with { Revision = 8, Situations = source.Situations.SetItem(0, source.Situations[0] with { ScopeId = MapFixtures.Team }) });
Check(nav.Return is null && nav.ScopeId == MapFixtures.Team && nav.SituationId == "matter:sponsor", "refresh closes entry and follows relocated matter scope");
Console.WriteLine("OPERATING_MAP_PURE_PASS " + count);
