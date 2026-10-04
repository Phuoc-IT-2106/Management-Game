using System.Collections.Immutable;
using System.Globalization;

namespace ManagementGame.UiKit;

public enum EntityKind { Company, Division, Team, Department, Person, Contract, Sponsor, Competition, Market, Opportunity, Risk, Obligation, Event, Decision }
public enum DomainStatus { Neutral, Positive, Warning, Critical }
public enum InformationState { Known, Estimated, Unknown }
public enum Availability { Ready, Empty, Loading, Unavailable, Error }
public enum MatterClass { Blocking, Actionable, Informational, Historical }
public enum ActionPhase { Ready, Pending, Accepted, Rejected, Unavailable }

public sealed record OrganizationIdentityView(string Id, string DisplayName, string ShortName, string? EmblemReference,
    Rgb? PrimaryColor, Rgb? SecondaryColor, long Revision, bool IsDevelopmentFixture = false);
public sealed record EntityView(string Id, string Name, EntityKind Kind, string Role, long Revision,
    InformationState Information = InformationState.Known, DomainStatus Status = DomainStatus.Neutral,
    Availability Availability = Availability.Ready, string Reason = "");
public sealed record UiIntent(string Kind, string TargetId, long Revision);
public sealed record ResourceView(string Label, decimal? Value, string Unit, InformationState Information, string Context);
public sealed record MatterView(EntityView Entity, MatterClass Class, string Why, int? DueDay, string Consequence);
public sealed record ComparisonLine(string Dimension, string Left, string Right);
public sealed record ComparisonData(string LeftTitle, string RightTitle, ImmutableArray<ComparisonLine> Lines, Availability Availability = Availability.Ready);
public sealed record DocumentSection(string Label, string Value);
public sealed record DocumentData(string Id, long Revision, string Title, string Provenance, ImmutableArray<DocumentSection> Sections,
    Availability Availability = Availability.Ready);
public sealed record CommitmentData(string Id, long Revision, string Summary, string TradeOff, ActionPhase Phase, string Result);

public static class PresentationText
{
    public const string FixtureNotice = "DEVELOPMENT FIXTURE — NONCANONICAL";
    public static string Information(InformationState value) => value switch
    {
        InformationState.Known => "Known", InformationState.Estimated => "Estimated", _ => "Unknown"
    };
    public static string Resource(ResourceView value) => value.Information == InformationState.Unknown || value.Value is null
        ? $"{value.Label}: Unknown · {value.Context}"
        : $"{value.Label}: {value.Value.Value.ToString("N2", CultureInfo.InvariantCulture)} {value.Unit} · {Information(value.Information)} · {value.Context}";
    public static string Time(int? day, string label) => day is null ? $"{label}: date unknown" : $"{label}: day {day.Value.ToString(CultureInfo.InvariantCulture)}";
    public static string AvailabilityText(Availability state, string reason) => state == Availability.Ready ? reason : $"{state}: {(string.IsNullOrWhiteSpace(reason) ? "No current data" : reason)}";
    public static string Icon(string key) => Enum.TryParse<EntityKind>(key, true, out var kind) && Enum.IsDefined(kind) ? kind switch
    {
        EntityKind.Company => "[CO]", EntityKind.Division => "[DV]", EntityKind.Team => "[TM]", EntityKind.Department => "[DP]",
        EntityKind.Person => "[P]", EntityKind.Contract => "[CT]", EntityKind.Sponsor => "[SP]", EntityKind.Competition => "[CP]",
        EntityKind.Market => "[MK]", EntityKind.Opportunity => "[OP]", EntityKind.Risk => "[!]", EntityKind.Obligation => "[DU]",
        EntityKind.Event => "[EV]", EntityKind.Decision => "[?]", _ => "[?]"
    } : "[?]";
}

/// <summary>UI binding only. One replaceable callback; never a gameplay authority.</summary>
public sealed class IntentBinding
{
    private Action<UiIntent>? callback;
    public EntityView? Current { get; private set; }
    public bool Selected { get; set; }
    public void Bind(EntityView view, Action<UiIntent>? handler)
    {
        if (Current is { } old && old.Id == view.Id && view.Revision < old.Revision)
            throw new ArgumentException("Cannot bind an older revision of the current entity.", nameof(view));
        Current = view; Selected = false; callback = handler;
    }
    public bool CanActivate => Current is { Availability: Availability.Ready } v && !string.IsNullOrWhiteSpace(v.Id) && callback is not null;
    public bool Activate(string kind)
    {
        if (!CanActivate) return false;
        callback!(new UiIntent(kind, Current!.Id, Current.Revision));
        return true;
    }
    public void Clear() { Current = null; callback = null; Selected = false; }
}
