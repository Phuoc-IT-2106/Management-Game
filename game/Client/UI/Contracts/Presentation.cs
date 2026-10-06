using System.Collections.Immutable;
using System.Globalization;

namespace ManagementGame.UiKit;

public enum EntityKind { Company, Division, Team, Department, Person, Contract, Sponsor, Competition, Market, Opportunity, Risk, Obligation, Event, Decision, Portfolio, Discipline }
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
public enum DayMarkShape { Filled, Outlined, Diamond }
public sealed record DayRow(string Label, ImmutableArray<int> Days, DayMarkShape Shape);
/// <summary>Bounded day-by-day quantity against one limit. Proposed may be empty when no alternative applies.</summary>
public sealed record DayTrackData(string Question, int FirstDay, int LastDay, int Limit, string LimitLabel,
    string CurrentLabel, ImmutableArray<int> Current, string ProposedLabel, ImmutableArray<int> Proposed,
    ImmutableArray<DayRow> Rows, string Assumption)
{
    public int Length => LastDay - FirstDay + 1;
    public void Validate()
    {
        if (Length < 1 || Length > UiTokens.MaxTrackDays) throw new ArgumentException("Day track must cover 1 to MaxTrackDays days.");
        if (Current.IsDefault || Current.Length != Length) throw new ArgumentException("Current series must supply one value per day.");
        if (Proposed.IsDefault || (!Proposed.IsEmpty && Proposed.Length != Length)) throw new ArgumentException("Proposed series must be empty or one value per day.");
        if (Rows.IsDefault || Rows.Any(r => r.Days.IsDefault)) throw new ArgumentException("Day rows must be initialized.");
    }
}

public static class PresentationText
{
    public const string FixtureNotice = "DEVELOPMENT FIXTURE — NONCANONICAL";
    /// <summary>Single compact development marker; keyboard legends and contract notes never enter player space.</summary>
    public const string DevelopmentWatermark = FixtureNotice + " · provisional theme";
    // Geometric markers always accompany words; "○" is reserved for Unknown information.
    public const string WarningMarker = "▲", CriticalMarker = "■", NeutralMarker = "•", PositiveMarker = "✓";
    /// <summary>Text emblem fallback from the display name ("DEV_ORG_001" → "DO"), never a type code.</summary>
    public static string Monogram(string name)
    {
        var words = name.Split([' ', '_', '-', '.', '·'], StringSplitOptions.RemoveEmptyEntries).Where(w => char.IsLetter(w[0])).ToArray();
        if (words.Length == 0) return "—";
        var letters = words.Length == 1 ? words[0].Where(char.IsLetter).Take(2) : words.Take(2).Select(w => w[0]);
        return new string(letters.ToArray()).ToUpperInvariant();
    }
    public static string StatusMarker(DomainStatus status) => status switch
    {
        DomainStatus.Critical => CriticalMarker, DomainStatus.Warning => WarningMarker,
        DomainStatus.Positive => PositiveMarker, _ => NeutralMarker
    };
    public static string Information(InformationState value) => value switch
    {
        InformationState.Known => "Known", InformationState.Estimated => "Estimated", _ => "Unknown"
    };
    /// <summary>Non-color information marker: filled known, half estimated, open unknown.</summary>
    public static string InformationMarker(InformationState value) => value switch
    {
        InformationState.Known => "●", InformationState.Estimated => "◐", _ => "○"
    };
    public static string ResourceAmount(ResourceView value) => value.Information == InformationState.Unknown || value.Value is null
        ? $"{value.Label}: Unknown"
        : $"{value.Label}: {value.Value.Value.ToString("N2", CultureInfo.InvariantCulture)} {value.Unit}";
    public static string ResourceEvidence(ResourceView value) => $"{InformationMarker(value.Information)} {Information(value.Information)} · {value.Context}";
    /// <summary>Compact ascending day list with consecutive runs, e.g. "1–3, 6".</summary>
    public static string DayRanges(IEnumerable<int> days)
    {
        var ordered = days.Distinct().Order().ToArray(); var parts = new List<string>();
        for (var i = 0; i < ordered.Length; i++)
        {
            var start = ordered[i];
            while (i + 1 < ordered.Length && ordered[i + 1] == ordered[i] + 1) i++;
            parts.Add(start == ordered[i] ? start.ToString(CultureInfo.InvariantCulture) : $"{start}–{ordered[i]}");
        }
        return string.Join(", ", parts);
    }
    public static string DayTrackLegend(DayTrackData data) =>
        $"Bars: {data.CurrentLabel} filled" + (data.Proposed.IsEmpty ? "" : $", {data.ProposedLabel} outlined on top") + $"; amber above {data.LimitLabel}. " +
        string.Join("; ", data.Rows.Select(r => (r.Shape switch { DayMarkShape.Filled => "Filled square", DayMarkShape.Outlined => "Outlined square", _ => "Diamond" }) + ": " + r.Label)) + ".";
    /// <summary>Text equivalent of every fact the day track draws; the drawing never carries information alone.</summary>
    public static ImmutableArray<string> DayTrackSummary(DayTrackData data)
    {
        data.Validate();
        var lines = ImmutableArray.CreateBuilder<string>();
        void Series(string label, ImmutableArray<int> values)
        {
            if (values.IsEmpty) return;
            var over = values.Select((v, i) => (v, day: data.FirstDay + i)).Where(x => x.v > data.Limit).Select(x => x.day).ToArray();
            var peak = values.Max();
            lines.Add(over.Length == 0
                ? $"{label}: within {data.LimitLabel} every day through day {data.LastDay} (peak {peak} / {data.Limit})."
                : $"{label}: over {data.LimitLabel} on day {DayRanges(over)} (peak {peak} / {data.Limit}).");
        }
        Series(data.ProposedLabel, data.Proposed); Series(data.CurrentLabel, data.Current);
        foreach (var row in data.Rows)
        {
            var days = row.Days.Where(d => d >= data.FirstDay && d <= data.LastDay).Distinct().Order().ToArray();
            lines.Add(days.Length == 0 ? $"{row.Label}: none through day {data.LastDay}."
                : $"{row.Label}: day {string.Join(", ", days.Select(d => d.ToString(CultureInfo.InvariantCulture)))}.");
        }
        return lines.ToImmutable();
    }
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
        EntityKind.Event => "[EV]", EntityKind.Decision => "[DC]", EntityKind.Portfolio => "[PF]", EntityKind.Discipline => "[DS]", _ => "[?]"
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
