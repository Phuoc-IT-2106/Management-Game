using System.Collections.Immutable;

namespace ManagementGame.UiKit;

/// <summary>Deterministic presentation-only specimens. No content pack, campaign, clock, RNG or simulation.</summary>
public static class LabFixtures
{
    public const string Version = "ui-lab-fixtures-v1";
    public static readonly ImmutableArray<string> BrandKeys = ["neutral", "bright", "dark", "warning", "low-contrast", "identical", "missing-emblem"];
    public static OrganizationIdentityView Organization(string brand, bool longName = false)
    {
        var pair = brand switch
        {
            "neutral" => (Primary: (Rgb?)null, Secondary: (Rgb?)null),
            "bright" => (Rgb.Hex("FFFFDE"), Rgb.Hex("E5FEFF")),
            "dark" => (Rgb.Hex("080A0C"), Rgb.Hex("101215")),
            "warning" => (Rgb.Hex("E44220"), Rgb.Hex("F49326")),
            "low-contrast" => (Rgb.Hex("22262B"), Rgb.Hex("25292D")),
            "identical" => (Rgb.Hex("A3B5CA"), Rgb.Hex("A3B5CA")),
            "missing-emblem" => (Rgb.Hex("ACBFC9"), Rgb.Hex("C6BAB2")),
            _ => throw new ArgumentException("Unknown UI Lab brand variant.", nameof(brand))
        };
        return new("DEV_ORG_001", longName ? "FIXTURE_ORGANIZATION — KIỂM THỬ TÊN CÔNG TY DÀI — Unicode identity and layout specimen" : "TEST_COMPANY",
            "TEST", brand == "missing-emblem" ? "missing:fixture-emblem" : null, pair.Primary, pair.Secondary, 7, true);
    }
    public static readonly EntityView Person = new("person:fixture:001", "TEST_PERSON_001", EntityKind.Person, "Staff · symbolic identity", 7, InformationState.Estimated, DomainStatus.Warning);
    public static readonly MatterView Alert = new(new("matter:fixture:001", "Review the pending commitment", EntityKind.Decision, "Company scope", 7,
        InformationState.Estimated, DomainStatus.Warning), MatterClass.Actionable, "A dated opportunity needs a decision.", 12, "Leaving it unanswered may lose the opportunity.");
    public static readonly MatterView Event = new(new("event:fixture:001", "Commitment review recorded", EntityKind.Event, "Company scope", 7),
        MatterClass.Historical, "Demonstration event; no simulation occurred.", 8, "No gameplay outcome is created by this Lab.");
    public static readonly ComparisonData Comparison = new("Option A", "Option B",
        [new("Commitment", "Higher scheduled cost", "Preserve flexibility"), new("Timing", "Earlier availability", "Later review"), new("Evidence", "Estimated · moderate confidence", "Unknown · evidence unavailable")]);
    public static readonly DocumentData Document = new("document:fixture:001", 7, "FIXTURE DOCUMENT · decision terms", "Presentation specimen · revision 7 · day 8",
        [new("Scope", "Company-wide illustrative commitment"), new("Terms", "Known dates and commitments remain readable without an image."), new("Trade-off", "Expected benefit competes with future flexibility; estimates are not guarantees.")]);
    public static CommitmentData Commitment(ActionPhase phase = ActionPhase.Ready) => new("decision:fixture:001", 7,
        "Review a demonstration commitment", "A benefit can require resources and reduce future flexibility.", phase,
        phase switch { ActionPhase.Rejected => "Rejected: observed revision changed. Review fresh terms before retrying.", ActionPhase.Accepted => "Accepted specimen: acknowledgement only; no gameplay executed.", ActionPhase.Pending => "Pending specimen: awaiting a result.", ActionPhase.Unavailable => "Unavailable: fixture has no current authority.", _ => "No action submitted." });
}
