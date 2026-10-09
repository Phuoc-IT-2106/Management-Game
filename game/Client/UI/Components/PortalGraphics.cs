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

/// <summary>Procedural atmosphere for a staged region (the match stage): a horizontal wash, soft light pools and
/// a floor shade. It carries no facts, names or values; identity colours come from the asset catalog by stable ID.
/// It is not art and stands in for no particular place.</summary>
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

/// <summary>The direction of a move between its two sides (origin → destination): a steel hairline ending in an open
/// arrowhead, drawn to the width it is given. It always points from the first side to the second and carries no colour
/// meaning; the sides' names say what moved where.</summary>
public partial class JourneyArrow : Control
{
    private Color tone;
    public static JourneyArrow Create(UiContext ui, int length, int height) => new()
    {
        tone = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.TextMuted)), CustomMinimumSize = new Vector2(length, height),
        MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter
    };
    public override void _Draw()
    {
        var y = Mathf.Floor(Size.Y / 2) + .5f; var tip = Size.X - 1.5f; var head = Math.Min(5f, Size.Y / 2 - 1);
        DrawLine(new Vector2(1, y), new Vector2(tip, y), tone, 1.5f, true);
        DrawPolyline([new Vector2(tip - head, y - head), new Vector2(tip, y), new Vector2(tip - head, y + head)], tone, 1.5f, true);
    }
}
