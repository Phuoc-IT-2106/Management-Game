using System.Collections.Immutable;
using ManagementGame.UiKit;

namespace ManagementGame.OperatingMap;

public static class MapFixtures
{
    public const string Version = "company-map-fixtures-v1";
    public const string Company = "DEV_ORG_001", Portfolio = "scope:competitive", Esports = "scope:esports",
        Discipline = "scope:development", Team = "scope:primary-team", Business = "function:business",
        Talent = "function:talent", Affairs = "function:affairs";
    public static readonly string[] Cases = ["normal", "sponsor", "competitive", "quiet", "empty", "long-name", "branding", "missing-identity", "list", "decision", "return", "affairs"];
    public static MapSnapshot Create(string scenario = "normal")
    {
        if (!Cases.Contains(scenario)) throw new ArgumentException("Unknown map fixture.");
        var organization = LabFixtures.Organization(scenario == "branding" ? "low-contrast" : "missing-emblem", scenario == "long-name");
        if (scenario == "missing-identity") organization = organization with { DisplayName = "", ShortName = "" };
        ImmutableArray<MapScope> scopes = [
            new(Company, null, string.IsNullOrWhiteSpace(organization.DisplayName) ? "Player company" : organization.DisplayName, ScopeKind.Company, "Your company owns the competitive portfolio. Shared functions support its operating units."),
            new(Portfolio, Company, "Competitive Portfolio", ScopeKind.Portfolio, "The company's currently supported competitive business."),
            new(Esports, Portfolio, "Esports", ScopeKind.Division, "The first operating domain within the company portfolio."),
            new(Discipline, Esports, "Development Discipline", ScopeKind.Discipline, PresentationText.FixtureNotice + ". No production discipline is selected."),
            new(Team, Discipline, "Primary Team", ScopeKind.Team, "The current operating team. Preparation and talent decisions share company resources."),
            new(Business, Company, "Business & Finance", ScopeKind.Function, "Shared commercial commitments and financial obligations; supports company scopes."),
            new(Talent, Company, "Talent & Performance", ScopeKind.Function, "Shared roster and employment context serving the primary team."),
            new(Affairs, Company, "Affairs & Time", ScopeKind.Function, "A dated view of the same situations across company scopes; no separate scheduler.")];
        ImmutableArray<MapSituation> matters = [
            new("matter:sponsor", Business, "Sponsor commitment", "Commercial", Attention.High, "Awaiting review", 12,
                "An available commitment needs review before its fixture deadline.", "Scheduled income adds delivery load and can constrain preparation; opening this view signs nothing.",
                "entry:sponsor", "Commercial commitment review", InformationState.Estimated, "Direction of the income/load trade-off is known; its competitive effect is estimated. Exact outcome is unknown."),
            new("matter:preparation", Team, "Competition preparation", "Competitive", Attention.High, "Needs inspection", 14,
                "The next competitive checkpoint needs a preparation context.", "Execution, opponent preparation and adaptation compete for the same preparation effort.",
                "entry:preparation", "Preparation review", InformationState.Unknown, "Hidden opponent preparation and match-day variance are unknown. No win probability is asserted."),
            new("matter:talent", Team, "Roster contract review", "Talent", Attention.Normal, "Available to inspect", null,
                "Review roster depth together with employment commitments.", "Changing personnel affects payroll and competitive depth; no signing or release is executed here.",
                "entry:talent", "Talent contract review", InformationState.Estimated, "External candidate ability is an estimate. Contract-review deadline is not supplied."),
            new("matter:obligation", Business, "Financial obligation", "Finance", Attention.Normal, "Scheduled", 11,
                "A scheduled obligation precedes the next competition checkpoint.", "Scheduled payments constrain future flexibility. No amount or solvency threshold is invented.",
                "entry:finance", "Obligation inspection", InformationState.Known, "The due day is a presentation fixture; amount is unknown.")];
        if (scenario == "quiet") matters = [.. matters.Select(x => x with { Priority = Attention.Normal, Status = "Ongoing review" })];
        if (scenario == "empty") matters = [];
        return new("fixture-session:001", 7, organization, 8, 14, scopes,
            [new(Business, Team, "Supports / constrains"), new(Talent, Team, "Serves"), new(Affairs, Company, "References situations across")], matters);
    }
}
