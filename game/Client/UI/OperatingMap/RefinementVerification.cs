using Godot;
using ManagementGame.OperatingMap;
using ManagementGame.UiKit;

public partial class OperatingMap
{
    private readonly List<object> taskProxies = [];
    private async Task VerifyRefinement()
    {
        taskProxies.Clear();
        foreach (var listMode in new[] { false, true })
        {
            var region = listMode ? "list" : "map";
            foreach (var task in new[] { "1-orientation", "2-sponsor", "3-preparation", "4-quiet", "5-stress" })
            {
                navigation = new(MapFixtures.Create(task == "4-quiet" ? "quiet" : task == "5-stress" ? "long-name" : "normal")) { ListMode = listMode };
                Build(); await Settle();
                Check(header.Size.Y <= 3 * ui.Tokens.ControlHeight, "compact identity header " + region + " " + task);
                var source = Data; var activations = 0; var scrolls = 0; var transitions = 0;
                async Task Activate(string key)
                {
                    var target = targets[key];
                    if (mapScroll.IsAncestorOf(target) && !mapScroll.GetGlobalRect().Encloses(target.GetGlobalRect()))
                    { mapScroll.EnsureControlVisible(target); scrolls++; await Settle(); }
                    Check(!mapScroll.IsAncestorOf(target) || mapScroll.GetGlobalRect().Encloses(target.GetGlobalRect()), "task target visible " + region + " " + key);
                    target.EmitSignal(BaseButton.SignalName.Pressed); activations++; transitions++; await Settle();
                }
                async Task Handoff()
                {
                    Check(inspectorScroll.GetGlobalRect().Encloses(openEntry.GetGlobalRect()), "entry visible without inspector scroll " + region + " " + task);
                    var selected = navigation.SituationId; var scope = navigation.ScopeId; var focus = originFocus; var scroll = mapScroll.ScrollVertical;
                    openEntry.EmitSignal(BaseButton.SignalName.Pressed); activations++; transitions++; await Settle();
                    Check(identity.Text == CompanyShortName && path.Text.Contains(Data.Scope(scope).Name), "identity and path retained during handoff");
                    var back = Descendants(map.GetParent()).OfType<Button>().Single(x => x.Text == "Return to originating context");
                    back.EmitSignal(BaseButton.SignalName.Pressed); activations++; transitions++; await Settle();
                    Check(navigation.SituationId == selected && navigation.ScopeId == scope && navigation.ListMode == listMode &&
                        originFocus == focus && mapScroll.ScrollVertical == scroll && targets[focus].HasFocus(), $"refined exact return {region} {task}: scroll={mapScroll.ScrollVertical}/{scroll} focus={targets[focus].HasFocus()}");
                }
                if (task == "1-orientation")
                {
                    await Activate(region + ":" + MapFixtures.Team);
                    Check(Data.CompactDevelopmentPath && Data.Path(MapFixtures.Team).Skip(1).All(x => path.Text.Contains(x.Name)), "full compressed semantic ancestry visible");
                    Check(targets[region + ":" + MapFixtures.Esports].IsVisibleInTree() && targets[region + ":" + MapFixtures.Discipline].IsVisibleInTree(), "both compressed scopes individually available");
                    Check(Descendants(inspector).OfType<Label>().Any(x => x.Text.Contains("Supports / constrains", StringComparison.OrdinalIgnoreCase)) &&
                        Descendants(inspector).OfType<Label>().Any(x => x.Text.Contains("serves", StringComparison.OrdinalIgnoreCase)), "team inspection explains both supporting functions");
                }
                if (task == "2-sponsor") { await Activate(region + ":matter:sponsor"); await Handoff(); }
                if (task == "3-preparation")
                {
                    await Activate(region + ":matter:preparation");
                    Check(navigation.ScopeId == MapFixtures.Team && navigation.Situation?.Information == InformationState.Unknown, "preparation scope and uncertainty");
                    // Reset is fixture setup, excluded from task counts.
                    Select(MapFixtures.Company); await Settle();
                    affairsToggle.EmitSignal(BaseButton.SignalName.Pressed); activations++; await Settle();
                    await Activate("affairs:matter:preparation");
                    Check(navigation.ScopeId == MapFixtures.Team && navigation.SituationId == "matter:preparation", "compact Affairs resolves same preparation");
                    Check(affairs.Size.Y <= 4 * ui.Tokens.ControlHeight, "bounded compact Affairs height");
                }
                if (task == "4-quiet")
                    Check(!affairs.Visible && Data.Situations.All(x => x.Priority == Attention.Normal) &&
                        Descendants(layout).OfType<Label>().Any(x => x.Text.Contains("Next checkpoint")), "quiet checkpoint and ongoing structure available");
                if (task == "5-stress")
                {
                    // Full identity is readable by keyboard/click, never hover-only.
                    var inspectCompany = Descendants(header).OfType<Button>().Single();
                    inspectCompany.EmitSignal(BaseButton.SignalName.Pressed); activations++; await Settle();
                    Check(Descendants(inspector).OfType<Label>().Any(x => x.Text == Data.Organization.DisplayName), "full long identity available on inspection");
                    await Activate(region + ":matter:sponsor"); await Handoff();
                    await Activate(region + ":matter:preparation"); await Handoff();
                }
                Check(ReferenceEquals(source, Data), "shared immutable snapshot " + region + " " + task);
                VerifyLayout();
                taskProxies.Add(new { Task = task, Representation = region, Activations = activations, ScrollOperations = scrolls,
                    ContextTransitions = transitions, ReadingHeight = mapScroll.Size.Y, ContentHeight = (listMode ? list : map).Size.Y,
                    RepresentationControls = Descendants(listMode ? list : map).OfType<Control>().Count() });
            }
        }
    }
}
