using System.Collections.Immutable;

namespace ManagementGame.UiKit;

/// <summary>The concepts that have approved, user-supplied semantic art. A concept is requested by meaning, never by
/// file path; anything not listed here has no approved art and uses the vector icon family or the generated identity
/// marks instead.</summary>
public enum SemanticConcept
{
    Portal, Squad, Company, Staff, Player, MatchPreparationDecision, ContractDecision, RosterMoveDecision,
    // Approved 2026-10-08 (portal_assets_v2). Inbox, Competition, Commercial and Finance head their rail items; Calendar is
    // catalogued only (the slice has no Calendar section).
    Inbox, Competition, Commercial, Finance, Calendar
}

/// <summary>One approved asset: its stable file stem, the canonical supplied PNG (source of record, never loaded), the
/// runtime WebP derivative (same image, resized without cropping to at most 512 px) and the vector icon that stands in
/// when the art cannot be loaded (the single fallback policy: never another concept's art, never generated imagery).</summary>
public sealed record SemanticAsset(SemanticConcept Concept, string Stem, string Role, string FallbackIcon)
{
    public string SourceFile => Stem + ".png";
    public string RuntimeFile => Stem + ".webp";
}

/// <summary>Single source of truth for approved semantic art (see game/Client/Assets/UI/Semantic/ASSET_MANIFEST.md).
/// Engine-free so the mapping is testable; the client loads <see cref="RuntimePath"/> through Godot.</summary>
public static class SemanticAssets
{
    public const string Directory = "res://Assets/UI/Semantic/";

    public static readonly ImmutableArray<SemanticAsset> All =
    [
        new(SemanticConcept.Portal, "icon_module_portal_v1", "Portal / central management module", "portal"),
        new(SemanticConcept.Squad, "icon_module_squad_v1", "Squad / roster module", "squad"),
        new(SemanticConcept.Company, "icon_module_company_v1", "Company / organization module", "company"),
        new(SemanticConcept.Staff, "icon_module_staff_v1", "Staff / coach / personnel module; generic staff fallback, never a named portrait", "staff"),
        new(SemanticConcept.Player, "icon_person_player_v1", "Generic player entity; fallback when no person-specific portrait exists", "squad"),
        new(SemanticConcept.MatchPreparationDecision, "icon_decision_match_preparation_v1", "Match preparation decision", "plan"),
        new(SemanticConcept.ContractDecision, "icon_decision_contract_v1", "Contract / renewal / formal agreement decision", "contract"),
        new(SemanticConcept.RosterMoveDecision, "icon_decision_roster_move_v1", "Roster move: signing, release or transfer decision", "transfer"),
        new(SemanticConcept.Inbox, "icon_module_inbox_v1", "Inbox / messages module", "inbox"),
        new(SemanticConcept.Competition, "icon_module_competition_v1", "Competition module (matches, fixtures, standings)", "competition"),
        new(SemanticConcept.Commercial, "icon_module_commercial_v1", "Commercial module (sponsors, brands, audience)", "commercial"),
        new(SemanticConcept.Finance, "icon_module_finance_v1", "Finance module (cash, budgets, reports)", "finance"),
        new(SemanticConcept.Calendar, "icon_module_calendar_v1", "Calendar / schedule and time; no Calendar section exists in the slice", "calendar")
    ];

    public static SemanticAsset Get(SemanticConcept concept) => All.Single(a => a.Concept == concept);
    public static string RuntimePath(SemanticConcept concept) => Directory + Get(concept).RuntimeFile;

    /// <summary>Module art for a shell section ID: each of the eight rail sections has its own approved module art
    /// (sidebar redesign, 2026-10-08). An unknown ID has none and keeps the vector icon family.</summary>
    public static SemanticConcept? ForSection(string sectionId) => sectionId switch
    {
        "portal" => SemanticConcept.Portal, "inbox" => SemanticConcept.Inbox, "squad" => SemanticConcept.Squad,
        "competition" => SemanticConcept.Competition, "commercial" => SemanticConcept.Commercial, "finance" => SemanticConcept.Finance,
        "staff" => SemanticConcept.Staff, "company" => SemanticConcept.Company, _ => null
    };

    /// <summary>Drawn size for art whose visible content measures <paramref name="contentWidth"/> × <paramref name="contentHeight"/>:
    /// scaled so the content covers the area of an <paramref name="optical"/>-pixel square, so wide, tall and square art read
    /// at one perceived size, then reduced uniformly where it would exceed the <paramref name="boxWidth"/> × <paramref name="boxHeight"/>
    /// slot. The aspect ratio is always kept; nothing is stretched or cropped.</summary>
    public static (float Width, float Height) OpticalSize(float contentWidth, float contentHeight, float optical, float boxWidth, float boxHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0) return (Math.Min(optical, boxWidth), Math.Min(optical, boxHeight));
        var scale = optical / MathF.Sqrt(contentWidth * contentHeight);
        scale = Math.Min(scale, Math.Min(boxWidth / contentWidth, boxHeight / contentHeight));
        return (contentWidth * scale, contentHeight * scale);
    }

    /// <summary>Generic art for a portrait asset ID (<c>portrait:&lt;stable person ID&gt;</c>) when no person-specific
    /// portrait exists: players get the generic player, coaches the staff-domain art. The ID kind decides, never a name.</summary>
    public static SemanticConcept? ForPortrait(string assetId) =>
        assetId.StartsWith("portrait:person:", StringComparison.Ordinal) ? SemanticConcept.Player
        : assetId.StartsWith("portrait:coach:", StringComparison.Ordinal) ? SemanticConcept.Staff
        : null;
}
