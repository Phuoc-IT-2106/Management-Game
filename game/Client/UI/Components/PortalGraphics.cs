using Godot;

namespace ManagementGame.UiKit;

/// <summary>Share ring (e.g. wins of matches played). An empty track is drawn when nothing has been played; the
/// numbers always sit beside it as text.</summary>
public partial class RingGauge : Control
{
    private float share; private bool empty; private Color fill, track;
    public static RingGauge Create(UiContext ui, int size, int part, int whole, ColorRole tone)
    {
        var ring = new RingGauge { CustomMinimumSize = new Vector2(size, size), MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        ring.empty = whole <= 0; ring.share = whole <= 0 ? 0 : Math.Clamp(part / (float)whole, 0, 1);
        ring.fill = UiTheme.ToGodot(ui.Tokens.Color(tone)); ring.track = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SelectedSurface));
        return ring;
    }
    public override void _Draw()
    {
        var r = Math.Min(Size.X, Size.Y) / 2f; var width = Math.Max(4, r * .16f); var c = Size / 2;
        DrawArc(c, r - width / 2, 0, Mathf.Tau, 64, track, width, true);
        if (!empty && share > 0) DrawArc(c, r - width / 2, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * share, 64, fill, width, true);
    }
}

/// <summary>Segmented magnitude bar: filled segments show a value relative to a stated reference, never alone.</summary>
public partial class SegmentBar : Control
{
    private int filled, segments; private Color on, off;
    public static SegmentBar Create(UiContext ui, long value, long reference, int segments, ColorRole tone)
    {
        var bar = new SegmentBar { CustomMinimumSize = new Vector2(segments * 14, 6), MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter, segments = segments };
        bar.filled = reference <= 0 ? 0 : (int)Math.Clamp(Math.Round(segments * Math.Abs(value) / (double)reference), value == 0 ? 0 : 1, segments);
        bar.on = UiTheme.ToGodot(ui.Tokens.Color(tone)); bar.off = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SelectedSurface));
        return bar;
    }
    public override void _Draw()
    {
        var gap = 2f; var w = (Size.X - gap * (segments - 1)) / segments;
        for (var i = 0; i < segments; i++) DrawRect(new Rect2(i * (w + gap), 0, w, Size.Y), i < filled ? on : off);
    }
}

/// <summary>One stretch of a week timeline: a line through the column and a node; adjacent columns join into one line.</summary>
public partial class TimelineNode : Control
{
    private bool current; private Color line, node;
    public static TimelineNode Create(UiContext ui, bool current) => new()
    {
        current = current, CustomMinimumSize = new Vector2(0, 12), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill,
        line = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SelectedSurface)), node = UiTheme.ToGodot(ui.Tokens.Color(current ? ColorRole.StatePositive : ColorRole.TextMuted))
    };
    public override void _Draw()
    {
        var y = Size.Y / 2;
        DrawLine(new Vector2(0, y), new Vector2(Size.X, y), line, 2);
        DrawCircle(new Vector2(Size.X / 2, y), current ? 5 : 4, node);
    }
}

/// <summary>Event marker: filled for a material commitment, hollow for a routine one. Shape carries the meaning.</summary>
public partial class EventMarker : Control
{
    private bool material; private Color color;
    public static EventMarker Create(UiContext ui, bool material, ColorRole tone) => new()
    {
        material = material, color = UiTheme.ToGodot(ui.Tokens.Color(tone)), CustomMinimumSize = new Vector2(10, 10),
        MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin
    };
    public override void _Draw()
    {
        var c = Size / 2; var r = Math.Min(Size.X, Size.Y) / 2 - 1;
        if (material) DrawCircle(c, r, color); else DrawArc(c, r - .5f, 0, Mathf.Tau, 20, color, 1.5f, true);
    }
}

/// <summary>Procedural atmosphere for a staged region (Portal hero, match stage): a horizontal wash, soft light pools and
/// a floor shade. It carries no facts, names or values; identity colours come from the asset catalog by stable ID.
/// It stands in for approved environment art until such art exists (see IdentityAssets.Environment).</summary>
public partial class Backdrop : Control
{
    public readonly record struct Glow(Vector2 At, float Radius, Color Color);
    private static GradientTexture2D? pool;
    private Color left, right, shade;
    private Glow[] glows = [];

    public static Backdrop Create(Color left, Color right, Color shade, params Glow[] glows) => new()
    {
        left = left, right = right, shade = shade, glows = glows, MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None
    };
    private static GradientTexture2D Pool()
    {
        if (pool is not null) return pool;
        var gradient = new Gradient(); gradient.SetColor(0, Colors.White); gradient.SetColor(1, new Color(1, 1, 1, 0));
        gradient.AddPoint(.45f, new Color(1, 1, 1, .45f));
        return pool = new GradientTexture2D { Gradient = gradient, Width = 128, Height = 128, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(.5f, .5f), FillTo = new Vector2(1f, .5f) };
    }
    public override void _Draw()
    {
        DrawPolygon([Vector2.Zero, new(Size.X, 0), Size, new(0, Size.Y)], [left, right, right, left]);
        foreach (var glow in glows)
        {
            var r = glow.Radius * Size.Y; var c = new Vector2(glow.At.X * Size.X, glow.At.Y * Size.Y);
            DrawTextureRect(Pool(), new Rect2(c - new Vector2(r, r), new Vector2(2 * r, 2 * r)), false, glow.Color);
        }
        var clear = new Color(shade, 0);
        DrawPolygon([new(0, Size.Y * .55f), new(Size.X, Size.Y * .55f), Size, new(0, Size.Y)], [clear, clear, shade, shade]);
    }
}
