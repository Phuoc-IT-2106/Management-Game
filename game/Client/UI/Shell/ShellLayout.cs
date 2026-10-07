using Godot;

namespace ManagementGame.Shell;

/// <summary>Shell and Portal composition targets (approved Portal reference) in one place. 1280×720 is the stress target;
/// viewports at least <see cref="RoomyHeight"/> tall get the detailed variant instead of a stretched copy.</summary>
public static class ShellLayout
{
    public const int TopBarHeight = 60, RailWidth = 168, RailCollapsedWidth = 64;
    public const int ContentPadding = 14, SectionGap = 10, BarGap = 12, CrestTopBar = 38;
    public const int RoomyHeight = 900;
    // Lower Portal grid: news, match/record, operations/money.
    public const float NewsShare = .34f, MatchShare = .31f, OperationsShare = .35f;
    // Within the middle and right columns: the upper module (next match, four-week operations) takes this share of the height.
    public const float MatchUpperShare = .63f, OperationsUpperShare = .66f;

    public static bool Roomy(Vector2 viewport) => viewport.Y >= RoomyHeight;
    public static int NavItemHeight(bool roomy) => roomy ? 52 : 44;
    public static int RegionPadding(bool roomy) => roomy ? 14 : 8;
    public static int RegionIcon(bool roomy) => roomy ? 24 : 22;
    public static int HeaderHeight(bool roomy) => roomy ? 128 : 90;
    public static int CrestHeader(bool roomy) => roomy ? 108 : 76;
    public static int DecisionCards(bool roomy) => roomy ? 4 : 3;
    public static int DecisionIcon(bool roomy) => roomy ? 48 : 44;
    /// <summary>News rows shown; on a roomy viewport the first is the lead story with a wide art panel.</summary>
    public static int NewsItems(bool roomy) => roomy ? 4 : 3;
    public static Vector2 NewsThumb(bool roomy) => roomy ? new(128, 72) : new(96, 54);
    public const int NewsLeadArtHeight = 132;
    public static int HorizonEventsPerWeek(bool roomy) => roomy ? 4 : 3;
    public static int MatchCrest(bool roomy) => roomy ? 92 : 48;
    public static int RecordRing(bool roomy) => roomy ? 104 : 72;
}
