using Godot;

namespace ManagementGame.UiKit;

/// <summary>One consistent outline icon family (24-unit grid, uniform stroke) drawn as vectors, so no external art is
/// loaded. Icons always accompany text; meaning never depends on the icon alone.</summary>
public partial class UiIcon : Control
{
    public static readonly string[] Keys =
    [
        "portal", "inbox", "squad", "competition", "commercial", "finance", "staff", "company", "calendar", "cash", "chart",
        "decisions", "save", "load", "alert", "contract", "arrow", "chevron", "record", "stakes", "plan", "news"
    ];
    private string key = "";
    private Color color;
    public string Key => key;

    public static UiIcon Create(UiContext ui, string key, int size, ColorRole tone = ColorRole.TextPrimary) =>
        Create(key, size, UiTheme.ToGodot(ui.Tokens.Color(tone)));
    public static UiIcon Create(string key, int size, Color color)
    {
        var icon = new UiIcon { key = key, color = color, CustomMinimumSize = new Vector2(size, size), MouseFilter = MouseFilterEnum.Ignore,
            FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        icon.SetMeta("icon", key);
        return icon;
    }
    public void SetColor(Color value) { color = value; QueueRedraw(); }

    public override void _Draw()
    {
        var s = Math.Min(Size.X, Size.Y) / 24f;
        var o = (Size - new Vector2(24, 24) * s) / 2;
        var w = Math.Max(1.5f, 1.75f * s);
        Vector2 P(float x, float y) => o + new Vector2(x, y) * s;
        void L(params (float X, float Y)[] points) => DrawPolyline(points.Select(p => P(p.X, p.Y)).ToArray(), color, w, true);
        void Box(float x0, float y0, float x1, float y1) => L((x0, y0), (x1, y0), (x1, y1), (x0, y1), (x0, y0));
        void Ring(float x, float y, float r) => DrawArc(P(x, y), r * s, 0, Mathf.Tau, 28, color, w, true);
        void Arc(float x, float y, float r, float from, float to) => DrawArc(P(x, y), r * s, Mathf.DegToRad(from), Mathf.DegToRad(to), 20, color, w, true);
        void Coins(params float[] ys) { foreach (var y in ys) { Arc(12, y, 7, 0, 180); L((5, y), (5, y - 2)); L((19, y), (19, y - 2)); } Arc(12, ys[^1] - 2, 7, 180, 360); Arc(12, ys[^1] - 2, 7, 0, 180); }
        switch (key)
        {
            case "portal": L((3, 11), (12, 3.5f), (21, 11)); L((5.5f, 9.5f), (5.5f, 20.5f), (18.5f, 20.5f), (18.5f, 9.5f)); L((10, 20.5f), (10, 15), (14, 15), (14, 20.5f)); break;
            case "inbox": case "news": Box(5, 3.5f, 19, 20.5f); L((8, 8), (16, 8)); L((8, 12), (16, 12)); L((8, 16), (13, 16)); break;
            case "squad": Ring(9, 8, 3); Arc(9, 20, 6, 180, 360); Ring(16.5f, 9, 2.4f); Arc(17, 20, 4.5f, 210, 360); break;
            case "staff": Ring(12, 7.5f, 3.5f); Arc(12, 21, 7.5f, 180, 360); L((10.5f, 14.5f), (12, 17), (13.5f, 14.5f)); break;
            case "competition": case "stakes":
                L((7, 4), (17, 4), (17, 9), (15.5f, 12.5f), (12, 14), (8.5f, 12.5f), (7, 9), (7, 4)); Arc(7, 7, 3, 90, 270); Arc(17, 7, 3, 270, 450);
                L((12, 14), (12, 18)); L((8, 20.5f), (16, 20.5f)); L((9.5f, 18), (14.5f, 18)); break;
            case "commercial": L((4, 10), (4, 14), (8, 14), (16, 19), (16, 5), (8, 10), (4, 10)); L((8, 14), (9.5f, 19.5f)); Arc(17, 12, 3, 300, 420); break;
            case "finance": case "cash": Coins(18, 13, 8); break;
            case "company": Box(5, 4, 14, 20.5f); L((14, 10), (19, 10), (19, 20.5f), (14, 20.5f)); foreach (var y in new[] { 8f, 12f, 16f }) { L((7.5f, y), (8.5f, y)); L((10.5f, y), (11.5f, y)); } break;
            case "calendar": Box(4, 6, 20, 20); L((4, 10), (20, 10)); L((8, 3.5f), (8, 7.5f)); L((16, 3.5f), (16, 7.5f)); break;
            case "chart": L((4, 17), (9, 12), (13, 15), (20, 7.5f)); L((16, 7.5f), (20, 7.5f), (20, 11.5f)); break;
            case "decisions": case "contract": Box(5, 3.5f, 19, 20.5f); L((8, 8), (16, 8)); L((8, 11.5f), (16, 11.5f)); L((8, 15), (12, 15)); L((12.5f, 17.5f), (14, 19), (17, 15.5f)); break;
            case "plan": Box(5, 5, 19, 21); L((9, 3.5f), (15, 3.5f), (15, 6.5f), (9, 6.5f), (9, 3.5f)); L((8, 11), (16, 11)); L((8, 15), (14, 15)); break;
            case "save": Box(4, 4, 20, 20); L((8, 4), (8, 9), (16, 9), (16, 4)); L((7.5f, 20), (7.5f, 14), (16.5f, 14), (16.5f, 20)); break;
            case "load": L((3.5f, 7), (9, 7), (11, 9), (20.5f, 9), (20.5f, 19), (3.5f, 19), (3.5f, 7)); break;
            case "alert": DrawCircle(P(12, 12), 10 * s, color); var ink = new Color(0.08f, 0.06f, 0.02f);
                DrawLine(P(12, 6.5f), P(12, 13.5f), ink, w * 1.3f, true); DrawCircle(P(12, 17), 1.4f * s, ink); break;
            case "arrow": L((5, 12), (19, 12)); L((14, 7), (19, 12), (14, 17)); break;
            case "chevron": L((9, 5.5f), (15.5f, 12), (9, 18.5f)); break;
            case "record": foreach (var (x, top) in new[] { (6f, 13f), (12f, 8f), (18f, 4f) }) DrawLine(P(x, 20.5f), P(x, top), color, w * 1.6f, true); break;
            default: Ring(12, 12, 8); break;
        }
    }
}
