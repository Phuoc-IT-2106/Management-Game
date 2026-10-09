using Godot;

namespace ManagementGame.UiKit;

/// <summary>An engraved divider: a dark groove over a faint steel highlight, optionally led by a short brass mark (the
/// section-header accent). One control, drawn; never nested panels.</summary>
public partial class EngravedRule : Control
{
    private Color groove, highlight, accent;
    private float accentLength;
    private bool vertical;

    public static EngravedRule Create(UiContext ui, bool vertical = false, float accentLength = 0)
    {
        Color C(ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));
        return new EngravedRule
        {
            groove = C(ColorRole.FrameOuter), highlight = C(ColorRole.FrameStroke) with { A = .7f }, accent = C(ColorRole.ActionPrimary) with { A = .9f },
            accentLength = accentLength, vertical = vertical, CustomMinimumSize = vertical ? new Vector2(2, 0) : new Vector2(0, 2),
            MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = vertical ? SizeFlags.Fill : SizeFlags.ShrinkBegin
        };
    }

    public override void _Draw()
    {
        if (vertical)
        {
            DrawLine(new Vector2(.5f, 0), new Vector2(.5f, Size.Y), groove, 1);
            DrawLine(new Vector2(1.5f, 0), new Vector2(1.5f, Size.Y), highlight, 1);
            return;
        }
        DrawLine(new Vector2(0, .5f), new Vector2(Size.X, .5f), groove, 1);
        DrawLine(new Vector2(0, 1.5f), new Vector2(Size.X, 1.5f), highlight, 1);
        if (accentLength > 0) DrawRect(new Rect2(0, 0, Math.Min(accentLength, Size.X), 2), accent);
    }
}

/// <summary>A region's header plate (<see cref="Surfaces.Header"/>) with its title tab: from the plate's leading edge to
/// just past the title the face steps up one tone and ends in a 45° slant, followed by a narrow inset seam and a short
/// brass stripe parallel to it, the header's one brass marker. The tab follows the title's laid-out rectangle, so it never
/// depends on screen coordinates; children (icon housing, title, context, route) draw over it.</summary>
public partial class HeaderPlate : PanelContainer
{
    /// <summary>Seam offset from the slant, stripe width and the minimum gap kept to text (pixels).</summary>
    public const float SeamOffset = 6, StripeWidth = 2, Clearance = 2;
    private Control? title;
    private Color face, faceTop, edge, seam, stripe;
    private float topCut, bottomCut, bottomInset, reach;

    /// <summary>How far the tab runs past the title at the title's centre line: half a cap height plus clearance, so the
    /// slant passes the title's last glyph at its baseline.</summary>
    public static float Reach(int titleSize) => MathF.Ceiling(titleSize * .36f + Clearance + 1);
    /// <summary>Width of the empty slot after the title (one extra separation plus this) that keeps the next text's line
    /// box clear of the slant and stripe.</summary>
    public static int Slot(int titleSize, int textSize, int separation) =>
        Math.Max(0, (int)MathF.Ceiling(Reach(titleSize) + SeamOffset + StripeWidth + textSize * .62f + Clearance - 2 * separation));

    public static HeaderPlate Create(UiContext ui, SurfaceStyle style)
    {
        Color C(ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));
        var plate = new HeaderPlate
        {
            Name = "SectionHeader", SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Pass,
            face = C(ColorRole.HeaderSurface).Lerp(C(ColorRole.FrameHighlight), .13f), faceTop = C(ColorRole.HeaderSurface).Lerp(C(ColorRole.FrameHighlight), .24f),
            edge = C(ColorRole.FrameHighlight) with { A = .75f }, seam = C(ColorRole.FrameOuter) with { A = .7f }, stripe = C(ColorRole.ActionPrimary),
            topCut = style.Spec.Cuts.TopLeft, bottomCut = style.Spec.Cuts.BottomLeft, bottomInset = (float)style.ContentMarginBottom - 1
        };
        plate.AddThemeStyleboxOverride("panel", style);
        return plate;
    }

    /// <summary>The title the tab follows, and its font size. Redraws when the title's rectangle changes.</summary>
    public void Track(Control target, int titleSize)
    {
        title = target; reach = Reach(titleSize);
        target.ItemRectChanged += QueueRedraw;
    }

    /// <summary>Tab height: the plate minus its lower inset line and lip.</summary>
    private float TabHeight => Math.Max(4, Size.Y - Math.Min(bottomInset, 4));
    /// <summary>X of the slant at the top of the plate (local): the slant crosses the title's centre line
    /// <see cref="Reach"/> pixels after the title and falls one pixel per pixel (45°).</summary>
    private float SlantTop
    {
        get
        {
            if (title is null) return 0;
            var rect = title.GetGlobalRect(); var origin = GetGlobalRect().Position;
            return rect.End.X - origin.X + reach + (rect.GetCenter().Y - origin.Y);
        }
    }
    /// <summary>The slant's x at local height <paramref name="y"/>.</summary>
    public float SlantAt(float y) => SlantTop - y;
    /// <summary>The brass stripe's right edge at local height <paramref name="y"/>.</summary>
    public float StripeRight(float y) => SlantTop + SeamOffset + StripeWidth - y;

    public override void _Draw()
    {
        if (title is null || !IsInstanceValid(title) || Size.X < 8) return;
        var height = TabHeight; var top = SlantTop;
        // Tab face: the plate's top-left cut, a 45° slant at its right end, the plate's lower-left cut.
        var cuts = new Cuts(topCut, 0, height, Math.Max(0, bottomCut - (Size.Y - height)));
        var outline = FrameGeometry.Outline(0, 0, top, height, cuts).Select(p => new Vector2(p.X, p.Y)).ToArray();
        DrawPolygon(outline, [.. outline.Select(p => faceTop.Lerp(face, p.Y / height))]);
        DrawLine(new Vector2(topCut, .5f), new Vector2(top, .5f), edge with { A = edge.A * .8f }, 1, true);
        DrawLine(new Vector2(top, 0), new Vector2(top - height, height), edge, 1, true);
        // Narrow inset seam, then the brass stripe, both parallel to the slant.
        DrawLine(new Vector2(top + SeamOffset - 3, 0), new Vector2(top + SeamOffset - 3 - height, height), seam, 1, true);
        DrawPolygon([new(top + SeamOffset, 0), new(top + SeamOffset + StripeWidth, 0), new(top + SeamOffset + StripeWidth - height, height), new(top + SeamOffset - height, height)], [stripe]);
    }
}

/// <summary>Holds optional text (a header's context): the child is shown only when it fits whole, otherwise omitted, so
/// supplementary words are never cut mid-word. Its own minimum width is zero, so it never pushes a row wider.</summary>
public partial class FitOrHide : Container
{
    public FitOrHide() { MouseFilter = MouseFilterEnum.Ignore; SizeFlagsHorizontal = SizeFlags.ExpandFill; }
    public override Vector2 _GetMinimumSize() =>
        new(0, GetChildren().OfType<Control>().Select(c => c.GetCombinedMinimumSize().Y).DefaultIfEmpty(0).Max());
    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        foreach (var child in GetChildren().OfType<Control>())
        {
            var size = child.GetCombinedMinimumSize();
            child.Visible = size.X <= Size.X;
            FitChildInRect(child, new Rect2(0, (Size.Y - size.Y) / 2, Math.Min(size.X, Size.X), size.Y));
        }
    }
}

/// <summary>A column that spreads its items through its height: each item keeps its own minimum height at full width, and
/// one gap, clamped to [<see cref="MinGap"/>, <see cref="MaxGap"/>], separates them, with half a gap above the first and
/// below the last so the group sits balanced in its space. Height the gaps cannot take goes to <see cref="Tail"/> (laid
/// out under the group, never below its own minimum) or stays empty below the group. Layout follows the container's own
/// rectangle, so it holds at any window size.</summary>
public partial class SpreadColumn : Container
{
    public float MinGap { get; set; }
    public float MaxGap { get; set; } = float.MaxValue;
    /// <summary>Optional child that takes the height left under the group (it is not one of the spread items).</summary>
    public Control? Tail { get; set; }
    /// <summary>The gap of the last layout.</summary>
    public float Gap { get; private set; }

    public SpreadColumn() { MouseFilter = MouseFilterEnum.Ignore; }
    private Control[] Items => [.. GetChildren().OfType<Control>().Where(c => c.Visible && c != Tail && !c.IsSetAsTopLevel())];
    private float TailMinimum => Tail is { Visible: true } tail ? tail.GetCombinedMinimumSize().Y : 0;

    public override Vector2 _GetMinimumSize()
    {
        var items = Items;
        var width = GetChildren().OfType<Control>().Where(c => c.Visible).Select(c => c.GetCombinedMinimumSize().X).DefaultIfEmpty(0).Max();
        return new(width, items.Sum(c => c.GetCombinedMinimumSize().Y) + items.Length * MinGap + TailMinimum);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        var items = Items;
        var heights = items.Select(c => c.GetCombinedMinimumSize().Y).ToArray();
        Gap = items.Length == 0 ? 0 : Math.Clamp((Size.Y - heights.Sum() - TailMinimum) / items.Length, MinGap, Math.Max(MinGap, MaxGap));
        var y = Gap / 2;
        for (var i = 0; i < items.Length; i++) { FitChildInRect(items[i], new Rect2(0, y, Size.X, heights[i])); y += heights[i] + Gap; }
        y -= Gap / 2;
        if (Tail is { Visible: true } tail) FitChildInRect(tail, new Rect2(0, y, Size.X, Math.Max(TailMinimum, Size.Y - y)));
    }
}

/// <summary>Material panels from <see cref="Surfaces"/>. Padding varies by viewport, so the style is applied per panel
/// from the shared factory rather than written inline.</summary>
public static class PanelFrame
{
    /// <summary>Thickness of a region's frame wall: content (the header plate, then the body) starts inside it.</summary>
    public const int RegionFrame = UiTokens.EdgeWall;

    /// <summary>Level-1 major region of a tier: <see cref="SectionHeader.Region"/> plate on top (built for the same tier),
    /// content below with its own padding. Returns the panel and the content stack to fill.</summary>
    public static PanelContainer Major(UiContext ui, string name, Control header, int padding, out VBoxContainer body, int gap, FrameTier tier = FrameTier.Standard)
    {
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        panel.SetMeta("frame_tier", tier.ToString());
        panel.AddThemeStyleboxOverride("panel", Surfaces.Region(ui.Tokens, RegionFrame, tier));
        var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        stack.AddThemeConstantOverride("separation", 0);
        stack.AddChild(header);
        var inner = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (var (edge, value) in new[] { ("left", padding), ("right", padding), ("top", padding - 1), ("bottom", padding) })
            inner.AddThemeConstantOverride("margin_" + edge, value);
        body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", gap);
        inner.AddChild(body); stack.AddChild(inner);
        panel.AddChild(stack);
        return panel;
    }
    /// <summary>Level-1 region without a header band (boards, overlays).</summary>
    public static PanelContainer Region(UiContext ui, string name, int padding)
    {
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", Surfaces.Region(ui.Tokens, padding));
        return panel;
    }
    /// <summary>Level-2 plate set into a region (data blocks: cash, record).</summary>
    public static PanelContainer Card(UiContext ui, int horizontal, int vertical)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", Surfaces.Card(ui.Tokens, SurfaceState.Normal, horizontal, vertical));
        return panel;
    }
    /// <summary>Recessed well; <paramref name="accent"/> warms its lower rim with muted brass (the most urgent item).</summary>
    public static PanelContainer Well(UiContext ui, int padding = 0, bool accent = false, int cut = Surfaces.ControlCut)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var well = Surfaces.Well(ui.Tokens, padding, cut);
        panel.AddThemeStyleboxOverride("panel", accent
            ? SurfaceStyle.Create(well.Spec with { EdgeBottom = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.AccentMuted)) with { A = .6f } }, padding, padding)
            : well);
        return panel;
    }
    /// <summary>The one icon housing (section headers): a recessed well with small diagonal cuts.</summary>
    public static PanelContainer Housing(UiContext ui)
    {
        var panel = new PanelContainer { Name = "IconHousing", MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", Surfaces.Housing(ui.Tokens));
        return panel;
    }
    /// <summary>A frame drawn over art (no fill): a well's rim and inner shade with the given cut, so art reads as set into
    /// the panel. Pair it with <see cref="Masked"/> so the art itself follows the same cuts.</summary>
    public static Panel Overlay(UiContext ui, int cut = Surfaces.ControlCut)
    {
        var overlay = new Panel { Name = "Frame", MouseFilter = Control.MouseFilterEnum.Ignore };
        overlay.AddThemeStyleboxOverride("panel", SurfaceStyle.Create(Surfaces.Well(ui.Tokens, 0, cut).Spec with { Top = Colors.Transparent, Bottom = Colors.Transparent, InnerShade = Colors.Transparent }, 0, 0));
        return overlay;
    }
    /// <summary>Clips a panel's children (art, washes) to a chamfered outline: the panel draws an opaque cut plate and masks
    /// its children by it, so square art never pokes past the frame's cut corners. Works for a Panel (children placed by
    /// anchors) and a PanelContainer (children fitted) alike.</summary>
    public static T Mask<T>(UiContext ui, T panel, int cut) where T : Control
    {
        var plate = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.SurfaceInset));
        panel.ClipChildren = CanvasItem.ClipChildrenMode.AndDraw;
        panel.AddThemeStyleboxOverride("panel", SurfaceStyle.Create(new(plate, plate, Colors.Transparent, Colors.Transparent, Cuts.Diagonal(cut), Rim: RimMode.None), 0, 0));
        return panel;
    }
}
