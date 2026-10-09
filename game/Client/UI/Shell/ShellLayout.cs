using Godot;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Shell and Portal composition targets (approved reference 11_portal_world_transfer_graphite_mockup) in one place.
/// 1280×720 is the stress target; viewports at least <see cref="RoomyHeight"/> tall get the detailed variant (more rows,
/// more columns) instead of a stretched copy. Compact viewports show fewer items, never smaller type.</summary>
public static class ShellLayout
{
    public const int TopBarHeight = 60;
    public const int ContentPadding = 14, SectionGap = 10, BarGap = 12;
    // Top-bar metrics (label → value → context). Each keeps its content width; the free width is shared as equal gaps of
    // at least MetricMinGap. A text value (the next opponent) is bounded at MetricTextMax and trimmed with its tooltip.
    public const int MetricMinGap = 16, MetricLabelGap = 1, MetricContextGap = 6, MetricTextMax = 240;
    public const int RoomyHeight = 900;
    // Middle band: world news beside transfer news (about 57% / 43%).
    public const float WorldNewsShare = .57f, TransferShare = .43f;
    // Bottom band: next match, season record, money snapshot.
    public const float MatchShare = .37f, RecordShare = .26f, MoneyShare = .37f;

    public static bool Roomy(Vector2 viewport) => viewport.Y >= RoomyHeight;
    /// <summary>Functional hierarchy of the Portal's frames: key decisions and the next match carry the premium chrome;
    /// world news, transfer news, season record and money are standard. Rows, filters and small controls are utility.</summary>
    public static FrameTier RegionTier(string region) => region is "Portal_Decisions" or "Portal_NextMatch" ? FrameTier.Premium : FrameTier.Standard;
    // Rail (sidebar redesign, 2026-10-08). Each navigation item is one component: the module art in a fixed slot, the
    // label in the column after it, and the optional unread badge at the right. The rail's inner edge, the items' inset
    // and the identity plate's inset line up, so the crest, the art and the labels share columns.
    /// <summary>Rail width: the longest label (Competition, semibold) fits beside the art slot with room to spare.</summary>
    public static int RailWidth(bool roomy) => roomy ? 208 : 184;
    /// <summary>Collapsed rail: the art slot, the items' collapsed inset and the rail's inset on both sides.</summary>
    public static int RailCollapsedWidth(bool roomy) => NavArt(roomy) + 2 * (NavInsetCollapsed + RailInset);
    /// <summary>Horizontal and vertical inner margins of the rail panel, and the gap between its identity, navigation and
    /// Collapse.</summary>
    public const int RailInset = 6, RailInsetVertical = 6, RailGap = 6;
    /// <summary>Content inset of a rail item (and of the identity plate), expanded and collapsed.</summary>
    public const int NavInset = 10, NavInsetCollapsed = 8;
    /// <summary>The company's crest at the head of the rail: the largest identity mark in the shell. Roomy rails stack it
    /// above the name; compact rails set it in the art column beside the name, which keeps the identity short.</summary>
    public static int CrestRail(bool roomy) => roomy ? 64 : 42;
    public const int CrestCollapsed = 36;
    /// <summary>Navigation item height: one click target, comfortably larger than the art it holds.</summary>
    public static int NavItemHeight(bool roomy) => roomy ? 56 : 50;
    /// <summary>The art slot of a rail item. Approved art is drawn by its visible content at <see cref="NavOptical"/> (one
    /// perceived size for wide and tall art), never larger than the slot; a missing asset's vector icon sits in the same slot.</summary>
    public static int NavArt(bool roomy) => roomy ? 46 : 42;
    public static int NavOptical(bool roomy) => roomy ? 42 : 38;
    /// <summary>Space between the art slot and the label.</summary>
    public static int NavGap(bool roomy) => roomy ? 14 : 12;
    /// <summary>Gap between rail items: spread evenly through the navigation region, never tighter than the minimum; above
    /// the maximum, the rest of the height is left to the notice dock.</summary>
    public const int NavMinGap = 8;
    public static int NavMaxGap(bool roomy) => roomy ? 40 : 20;
    /// <summary>Height kept for a player notice (two lines) between the last item and Collapse.</summary>
    public const int NoticeDock = 52;
    public static int NavToggleHeight(bool roomy) => roomy ? 36 : 32;
    public static int RegionPadding(bool roomy) => roomy ? 14 : 7;
    public static int RegionIcon(bool roomy) => roomy ? 22 : 20;
    public static int DecisionCards(bool roomy) => roomy ? 4 : 3;
    public static int DecisionArt(bool roomy) => roomy ? 56 : 48;
    /// <summary>World news rows shown, the first being the lead story.</summary>
    public static int NewsItems(bool roomy) => roomy ? 4 : 3;
    public static Vector2 NewsThumb(bool roomy) => roomy ? new(112, 63) : new(80, 45);
    public static Vector2 LeadArt(bool roomy) => roomy ? new(0, 150) : new(150, 84);
    /// <summary>Transfer stories shown: a few important stories, never a list of every move. Every story has the same
    /// treatment and size (no enlarged lead) and keeps room for a two-line headline, so a long name never pushes the
    /// Portal past the first viewport.</summary>
    public static int TransferItems(bool roomy) => roomy ? 4 : 3;
    /// <summary>The identity column of a transfer story: the person's generic art drawn bare by its visible content, with
    /// no plate or frame; a roundup shows its count there instead.</summary>
    public static int StoryPortrait(bool roomy) => roomy ? 52 : 44;
    /// <summary>A story's journey row: organization crests (height; crests are 100:120) and the arrow between the sides.</summary>
    public static int JourneyCrest(bool roomy) => 20;
    public static int JourneyArrow(bool roomy) => roomy ? 40 : 28;
    public static int BottomBandHeight(bool roomy) => roomy ? 268 : 176;
    public static int MatchCrest(bool roomy) => roomy ? 60 : 40;
    public static int RecordRing(bool roomy) => roomy ? 104 : 76;
}
