using Godot;
using Num = System.Numerics;

namespace ManagementGame.UiKit;

public enum AccentEdge { None, Top, Left }

/// <summary>How a frame without a structural wall draws its edge: a full lit loop, or only a top highlight and the lower
/// contour (the edge is then implied by tone, not outlined), or nothing.</summary>
public enum RimMode { Loop, Edges, None }

/// <summary>One physical material for a frame, drawn in a single style box (no nested controls). The silhouette is a
/// rectangle whose corners are cut at 45° (<see cref="Cuts"/>, <see cref="FrameGeometry"/>); content margins stay
/// rectangular and the control keeps its rectangular hit area. Layers, back to front:
/// <list type="number">
/// <item>soft shadow (three widening layers) and a contact shadow that peeks below the frame;</item>
/// <item>a structural <see cref="Wall"/> of eight flat-shaded facets lit from above (top bright, bottom dark), so the cut
/// corners read as machined faces; <see cref="BrassCorners"/> turns chosen chamfer facets into brass inserts;</item>
/// <item>fill, a vertical gradient <see cref="Top"/> → <see cref="Bottom"/>, inside the wall;</item>
/// <item>without a wall, a hairline rim lit from above (<see cref="Rim"/>);</item>
/// <item>inner edge: a graphite groove at the wall's inner boundary and a faint highlight under its top;</item>
/// <item>outer shadow edge: a graphite hairline just outside the silhouette (never an opaque black outline);</item>
/// <item>recessed inner shade, lower lip, accent bar and, for focus, a ring that follows the same cuts.</item>
/// </list></summary>
public sealed record SurfaceSpec(Color Top, Color Bottom, Color EdgeTop, Color EdgeBottom, Cuts Cuts = default,
    int Wall = 0, RimMode Rim = RimMode.Loop, Color Frame = default, Color Groove = default, Color InnerLight = default,
    Color Brass = default, int BrassCorners = 0, Color InnerShade = default, int InnerDepth = 0, Color Lip = default,
    float Contact = 0, int ContactDrop = 0, float Soft = 0, int SoftSize = 0, int SoftDrop = 0,
    Color Accent = default, AccentEdge AccentEdge = AccentEdge.None, float AccentLength = 0,
    Color Ring = default, float RingGrow = 0, float RingWidth = 0)
{
    /// <summary>Corner bits for <see cref="BrassCorners"/>.</summary>
    public const int TopLeft = 1, TopRight = 2, BottomRight = 4, BottomLeft = 8;
}

/// <summary>Scriptable style box for <see cref="SurfaceSpec"/>. Content margins are the normal StyleBox margins; a pressed
/// state moves content down by <c>pressShift</c> pixels so a press reads as physical depth, not a recolour.</summary>
public partial class SurfaceStyle : StyleBox
{
    private SurfaceSpec spec = null!;
    public SurfaceSpec Spec => spec;

    public static SurfaceStyle Create(SurfaceSpec spec, int horizontal, int vertical, int pressShift = 0) => new()
    {
        spec = spec, ContentMarginLeft = horizontal, ContentMarginRight = horizontal,
        ContentMarginTop = vertical + pressShift, ContentMarginBottom = vertical - pressShift
    };

    public override Rect2 _GetDrawRect(Rect2 rect) =>
        rect.Grow(Math.Max(spec.SoftSize + spec.SoftDrop, Math.Max(spec.ContactDrop, spec.RingGrow + spec.RingWidth)) + 2);

    public override void _Draw(Rid ci, Rect2 rect)
    {
        if (rect.Size.X < 4 || rect.Size.Y < 4) return;
        var s = spec;
        var cuts = s.Cuts.Fit(rect.Size.X, rect.Size.Y);
        if (s.Soft > 0)
            for (var k = 3; k >= 1; k--)
            {
                var grow = s.SoftSize * k / 3f;
                Fill(ci, new Rect2(rect.Position + new Vector2(0, s.SoftDrop * k / 3f), rect.Size).Grow(grow), cuts.Inset(-grow), new Color(0, 0, 0, s.Soft / 3));
            }
        if (s.Contact > 0) Fill(ci, new Rect2(rect.Position + new Vector2(0, s.ContactDrop), rect.Size), cuts, new Color(0, 0, 0, s.Contact));
        var wall = Math.Min(s.Wall, (int)Math.Min(rect.Size.X, rect.Size.Y) / 4);
        var inner = rect.Grow(-wall); var innerCuts = cuts.Inset(wall);
        if (wall > 0) Facets(ci, rect, cuts, inner, innerCuts, s);
        if (s.Top.A > 0 || s.Bottom.A > 0)
        {
            var fill = Points(inner, innerCuts);
            RenderingServer.CanvasItemAddPolygon(ci, fill, [.. fill.Select(p => s.Top.Lerp(s.Bottom, (p.Y - inner.Position.Y) / inner.Size.Y))]);
        }
        if (s.InnerShade.A > 0 && s.InnerDepth > 0)
        {
            // A dark band fading downward from the top edge: light falls into the recess from above.
            var y0 = inner.Position.Y + 1; var y1 = Math.Min(inner.End.Y - 1, y0 + s.InnerDepth);
            var clear = s.InnerShade with { A = 0 };
            RenderingServer.CanvasItemAddPolygon(ci, [new(inner.Position.X + 1 + innerCuts.TopLeft * .6f, y0), new(inner.End.X - 1 - innerCuts.TopRight * .6f, y0),
                new(inner.End.X - 1, y1), new(inner.Position.X + 1, y1)], [s.InnerShade, s.InnerShade, clear, clear]);
        }
        if (wall == 0 && (s.EdgeTop.A > 0 || s.EdgeBottom.A > 0))
        {
            if (s.Rim == RimMode.Loop) Loop(ci, rect.Grow(-.5f), cuts.Inset(.5f), p => Lit(s, rect, p));
            else if (s.Rim == RimMode.Edges) Edges(ci, rect, cuts, s.EdgeTop, s.EdgeBottom);
        }
        if (wall > 0)
        {
            if (s.Groove.A > 0) Loop(ci, inner.Grow(-.5f), innerCuts.Inset(.5f), _ => s.Groove);
            if (s.InnerLight.A > 0)
                Line(ci, new(inner.Position.X + innerCuts.TopLeft + 1, inner.Position.Y + 1.5f), new(inner.End.X - innerCuts.TopRight - 1, inner.Position.Y + 1.5f), s.InnerLight);
        }
        if (s.Frame.A > 0) Loop(ci, rect.Grow(.5f), cuts.Inset(-.5f), _ => s.Frame);
        if (s.Lip.A > 0)
            Line(ci, new(inner.Position.X + innerCuts.BottomLeft + 2, inner.End.Y - 1.5f), new(inner.End.X - innerCuts.BottomRight - 2, inner.End.Y - 1.5f), s.Lip);
        if (s.AccentEdge == AccentEdge.Left && s.Accent.A > 0)
            RenderingServer.CanvasItemAddRect(ci, new Rect2(inner.Position + new Vector2(1, 2 + innerCuts.TopLeft), new Vector2(3, Math.Max(0, inner.Size.Y - 4 - innerCuts.TopLeft - innerCuts.BottomLeft))), s.Accent);
        if (s.AccentEdge == AccentEdge.Top && s.Accent.A > 0)
        {
            var start = innerCuts.TopLeft + 2;
            var length = s.AccentLength > 0 ? Math.Min(s.AccentLength, inner.Size.X - start - innerCuts.TopRight - 2) : inner.Size.X - start - innerCuts.TopRight - 2;
            RenderingServer.CanvasItemAddRect(ci, new Rect2(inner.Position + new Vector2(start, 1), new Vector2(Math.Max(0, length), 2)), s.Accent);
        }
        if (s.Ring.A > 0 && s.RingWidth > 0) Loop(ci, rect.Grow(s.RingGrow - s.RingWidth / 2), cuts.Inset(-(s.RingGrow - s.RingWidth / 2)), _ => s.Ring, s.RingWidth);
    }

    /// <summary>Colour of the rim at a point: lit from above, from <c>EdgeTop</c> at the top edge to <c>EdgeBottom</c>.</summary>
    private static Color Lit(SurfaceSpec s, Rect2 r, Vector2 p) => s.EdgeTop.Lerp(s.EdgeBottom, Math.Clamp((p.Y - r.Position.Y) / Math.Max(1, r.Size.Y), 0, 1));

    /// <summary>The structural wall: one flat-shaded quad per facet between the outer and inner outlines, drawn as a single
    /// triangle array so adjoining facets share exact vertices (no seams). Brass inserts replace chosen chamfer facets.</summary>
    private static void Facets(Rid ci, Rect2 outer, Cuts cuts, Rect2 inner, Cuts innerCuts, SurfaceSpec s)
    {
        var o = FrameGeometry.Corners(outer.Position.X, outer.Position.Y, outer.Size.X, outer.Size.Y, cuts);
        var n = FrameGeometry.Corners(inner.Position.X, inner.Position.Y, inner.Size.X, inner.Size.Y, innerCuts);
        var points = new List<Vector2>(32); var colors = new List<Color>(32); var indices = new List<int>(48);
        for (var i = 0; i < 8; i++)
        {
            var j = (i + 1) % 8;
            if (Num.Vector2.DistanceSquared(o[i], o[j]) < 1e-4f) continue;
            var light = FrameGeometry.Light(FrameGeometry.Normal(i));
            var corner = FrameGeometry.ChamferCorner(i);
            var color = corner >= 0 && (s.BrassCorners & (1 << corner)) != 0 && s.Brass.A > 0
                ? s.Brass.Darkened(.5f).Lerp(s.Brass.Lightened(.22f), light)
                : s.EdgeBottom.Lerp(s.EdgeTop, light);
            var at = points.Count;
            foreach (var p in new[] { o[i], o[j], n[j], n[i] }) { points.Add(new Vector2(p.X, p.Y)); colors.Add(color); }
            indices.AddRange([at, at + 1, at + 2, at, at + 2, at + 3]);
        }
        if (indices.Count > 0) RenderingServer.CanvasItemAddTriangleArray(ci, [.. indices], [.. points], [.. colors]);
    }

    /// <summary>Top highlight along the straight top edge, and the lower contour (both lower chamfers and the bottom edge) in
    /// the shade colour: the frame reads by tone, without a full outline.</summary>
    private static void Edges(Rid ci, Rect2 r, Cuts c, Color top, Color bottom)
    {
        var p = FrameGeometry.Corners(r.Position.X + .5f, r.Position.Y + .5f, r.Size.X - 1, r.Size.Y - 1, c.Inset(.5f));
        if (top.A > 0) Line(ci, new(p[0].X, p[0].Y), new(p[1].X, p[1].Y), top);
        if (bottom.A > 0)
            RenderingServer.CanvasItemAddPolyline(ci, [new(p[3].X, p[3].Y), new(p[4].X, p[4].Y), new(p[5].X, p[5].Y), new(p[6].X, p[6].Y)], [bottom], 1, true);
    }

    private static void Line(Rid ci, Vector2 a, Vector2 b, Color color, float width = 1)
    {
        if (b.X > a.X || b.Y > a.Y) RenderingServer.CanvasItemAddLine(ci, a, b, color, width, true);
    }

    private static Vector2[] Points(Rect2 r, Cuts cuts) =>
        [.. FrameGeometry.Outline(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y, cuts).Select(p => new Vector2(p.X, p.Y))];

    private static void Fill(Rid ci, Rect2 r, Cuts cuts, Color color)
    {
        if (r.Size.X > 0 && r.Size.Y > 0) RenderingServer.CanvasItemAddPolygon(ci, Points(r, cuts), [color]);
    }

    /// <summary>A closed antialiased outline. It starts and ends mid-way along the top edge, so the polyline's open joint
    /// sits on a straight run instead of on a corner.</summary>
    private static void Loop(Rid ci, Rect2 r, Cuts cuts, Func<Vector2, Color> color, float width = 1)
    {
        var outline = Points(r, cuts);
        if (outline.Length < 3) return;
        var start = (outline[0] + outline[1]) / 2;
        var points = new[] { start }.Concat(outline.Skip(1)).Append(outline[0]).Append(start).ToArray();
        RenderingServer.CanvasItemAddPolyline(ci, points, points.Select(color).ToArray(), width, true);
    }
}

public enum SurfaceState { Normal, Hover, Pressed, Disabled }

/// <summary>The surface system: every frame in the shell and Portal comes from here, so elevation and geometry read the
/// same everywhere. Graphite first, steel second, brass third.
/// <list type="bullet">
/// <item>Level 0: the room (SurfaceBase, with the procedural room backdrop).</item>
/// <item>Level 1: <see cref="Region"/>, a raised graphite slab inside a faceted steel wall, all four corners cut by tier;
/// its <see cref="Header"/> plate sits flush in the wall's top and narrows at its lower ends.</item>
/// <item>Level 2: <see cref="Card"/> and <see cref="Row"/>, darker plates set into the region; rows are defined by tone.</item>
/// <item>Level 3: <see cref="Lift"/> (hover) and <see cref="Selected"/> (current selection, brass).</item>
/// </list>
/// <see cref="Well"/> is recessed (art, filter tracks, forecasts, data wells, icon housings).</summary>
public static class Surfaces
{
    public const int PressShift = 1;
    /// <summary>Corner cuts of nested plates and of controls.</summary>
    public const int CardCut = UiTokens.ChamferSmall, ControlCut = UiTokens.ChamferControl;
    private static Color C(UiTokens t, ColorRole role) => UiTheme.ToGodot(t.Color(role));
    /// <summary>The graphite shadow edge just outside a frame: dark, never opaque black.</summary>
    private static Color ShadowEdge(UiTokens t, float alpha = .5f) => C(t, ColorRole.FrameOuter) with { A = alpha };

    /// <summary>Outer cuts of a region of this tier (all four corners).</summary>
    public static Cuts RegionCuts(FrameTier tier) => Cuts.All(UiTokens.RegionChamfer(tier));
    /// <summary>Cuts of the face inside a region's wall: the header plate's top corners sit exactly on them.</summary>
    public static Cuts RegionInnerCuts(FrameTier tier) => RegionCuts(tier).Inset(UiTokens.EdgeWall);
    /// <summary>Brass corner bits of a region by tier: premium top-left and bottom-right, standard top-left, utility none.</summary>
    public static int RegionBrass(FrameTier tier) => UiTokens.BrassInserts(tier) switch
    {
        >= 2 => SurfaceSpec.TopLeft | SurfaceSpec.BottomRight, 1 => SurfaceSpec.TopLeft, _ => 0
    };

    /// <summary>Level 1: a major region. Graphite slab inside a three-pixel faceted steel wall lit from above, all corners
    /// cut by tier, brass inserts on the tier's chamfer facets, a graphite groove and faint highlight inside the wall, a
    /// graphite shadow edge outside it, and a contact shadow over a broader soft one.</summary>
    public static SurfaceStyle Region(UiTokens t, int padding, FrameTier tier = FrameTier.Standard, int? vertical = null) => SurfaceStyle.Create(new(
        C(t, ColorRole.SurfaceRaised).Lerp(C(t, ColorRole.FrameHighlight), .05f), C(t, ColorRole.SurfaceShade),
        C(t, ColorRole.FrameHighlight).Lightened(.06f), C(t, ColorRole.FrameStroke).Darkened(.35f), RegionCuts(tier), UiTokens.EdgeWall,
        Frame: ShadowEdge(t), Groove: C(t, ColorRole.FrameOuter) with { A = .55f }, InnerLight: C(t, ColorRole.FrameHighlight) with { A = .12f },
        Brass: C(t, ColorRole.ActionPrimary), BrassCorners: RegionBrass(tier),
        Contact: .55f, ContactDrop: 3, Soft: .7f, SoftSize: 12, SoftDrop: 6), padding, vertical ?? padding);

    /// <summary>The header plate of a region: one step brighter than the slab, its top corners on the wall's inner chamfer
    /// and its lower ends cut (a trapezoid seated in the frame), a brighter upper steel edge and a dark lower inset line
    /// over a steel lip. The title tab and its brass seam are drawn by <see cref="HeaderPlate"/>.</summary>
    public static SurfaceStyle Header(UiTokens t, int horizontal, int vertical, float topCut = 0) => SurfaceStyle.Create(new(
        C(t, ColorRole.HeaderSurface).Lerp(C(t, ColorRole.FrameHighlight), .06f), C(t, ColorRole.HeaderSurface).Lerp(C(t, ColorRole.SurfaceRaised), .55f),
        C(t, ColorRole.FrameHighlight) with { A = .5f }, C(t, ColorRole.FrameOuter) with { A = .8f },
        new Cuts(topCut, topCut, UiTokens.ChamferSmall, UiTokens.ChamferSmall), Rim: RimMode.Edges,
        Lip: C(t, ColorRole.FrameStroke).Lightened(.12f)), horizontal, vertical);

    /// <summary>Level 2: a plate set into a region (decision, lead story, data block): darker than the slab, small diagonal
    /// cuts, a hairline rim lit from above and a short contact shadow. Hover lifts it to level 3; a press sinks it.</summary>
    public static SurfaceStyle Card(UiTokens t, SurfaceState state, int horizontal = 12, int vertical = 8)
    {
        var plate = C(t, ColorRole.SurfaceInset); var steel = C(t, ColorRole.FrameHighlight); var dim = C(t, ColorRole.FrameStroke);
        var cuts = Cuts.Diagonal(CardCut);
        return state switch
        {
            SurfaceState.Hover => Lift(t, horizontal, vertical, CardCut),
            SurfaceState.Pressed => Sunk(t, horizontal, vertical, CardCut),
            SurfaceState.Disabled => SurfaceStyle.Create(new(plate, plate, dim with { A = .6f }, dim with { A = .35f }, cuts), horizontal, vertical),
            _ => SurfaceStyle.Create(new(plate.Lerp(steel, .05f), plate.Darkened(.1f), steel.Lerp(dim, .35f), dim.Darkened(.3f), cuts,
                Frame: ShadowEdge(t, .4f), Contact: .5f, ContactDrop: 2), horizontal, vertical)
        };
    }

    /// <summary>Level 2 list row (news, transfers): a plate defined by tone, a faint top highlight and a lower shade, no
    /// outline; alternate rows sit a step darker so a list reads as separate items. Hover lifts to level 3.</summary>
    public static SurfaceStyle Row(UiTokens t, SurfaceState state, bool alternate, int horizontal, int vertical)
    {
        var plate = C(t, ColorRole.SurfaceInset).Lerp(C(t, ColorRole.SurfaceRaised), alternate ? .15f : .45f);
        return state switch
        {
            SurfaceState.Hover => Lift(t, horizontal, vertical, ControlCut),
            SurfaceState.Pressed => Sunk(t, horizontal, vertical, ControlCut),
            _ => SurfaceStyle.Create(new(plate, plate.Darkened(.08f), C(t, ColorRole.FrameHighlight) with { A = .16f }, new Color(0, 0, 0, .35f),
                Cuts.Diagonal(ControlCut), Rim: RimMode.Edges, Contact: .25f, ContactDrop: 1), horizontal, vertical)
        };
    }

    /// <summary>Level 3 hover: a brighter cool plate with a bright steel rim lit from above and a firmer shadow.</summary>
    public static SurfaceStyle Lift(UiTokens t, int horizontal, int vertical, int cut = ControlCut) => SurfaceStyle.Create(new(
        C(t, ColorRole.HoverSurface).Lerp(C(t, ColorRole.FrameHighlight), .12f), C(t, ColorRole.HoverSurface).Darkened(.12f),
        C(t, ColorRole.FrameHighlight).Lightened(.25f), C(t, ColorRole.FrameStroke), Cuts.Diagonal(cut), Frame: ShadowEdge(t),
        Contact: .75f, ContactDrop: 2, Soft: .45f, SoftSize: 5, SoftDrop: 3), horizontal, vertical);

    /// <summary>Pressed: the plate drops into the surface. Dark at the top (inner shade), rim lit from below, no lift.</summary>
    public static SurfaceStyle Sunk(UiTokens t, int horizontal, int vertical, int cut = ControlCut) => SurfaceStyle.Create(new(
        C(t, ColorRole.SurfaceBase), C(t, ColorRole.SurfaceInset), C(t, ColorRole.FrameOuter) with { A = .7f }, C(t, ColorRole.FrameStroke).Lightened(.1f), Cuts.Diagonal(cut),
        Frame: ShadowEdge(t, .35f), InnerShade: new Color(0, 0, 0, .6f), InnerDepth: 6), horizontal, vertical, PressShift);

    /// <summary>Recessed well: dark, shaded from the top edge, its rim dark above and lit below (light falls in from above).</summary>
    public static SurfaceStyle Well(UiTokens t, int padding = 0, int cut = ControlCut) => SurfaceStyle.Create(new(
        C(t, ColorRole.SurfaceBase).Darkened(.3f), C(t, ColorRole.SurfaceInset).Darkened(.1f), new Color(0, 0, 0, .55f), C(t, ColorRole.FrameHighlight) with { A = .3f },
        Cuts.Diagonal(cut), InnerShade: new Color(0, 0, 0, .6f), InnerDepth: 8), padding, padding);

    /// <summary>The icon housing of section headers: one recessed well, one cut. Rail art sits on its item's own surface instead.</summary>
    public static SurfaceStyle Housing(UiTokens t) => Well(t, 0, CardCut);

    /// <summary>Level 3 selection: warm graphite lifted inside a two-pixel brass wall cut at every corner (the angled
    /// selection frame), bright brass chamfer facets and a brass leading bar. <paramref name="compact"/> selections (filter
    /// tabs) keep a hairline rim and diagonal control cuts so small labels stay legible.</summary>
    public static SurfaceStyle Selected(UiTokens t, bool hover, int horizontal = 12, int vertical = 0, bool compact = false)
    {
        var fill = C(t, ColorRole.SelectedSurface).Lerp(C(t, ColorRole.ActionPrimary), hover ? .2f : .14f);
        var bottom = C(t, ColorRole.SurfaceRaised).Lerp(C(t, ColorRole.ActionPrimary), .05f);
        var brass = C(t, ColorRole.ActionPrimary);
        return compact
            ? SurfaceStyle.Create(new(fill, bottom, C(t, ColorRole.ActionPrimaryHover), C(t, ColorRole.AccentMuted).Darkened(.3f), Cuts.Diagonal(ControlCut - 1),
                Frame: ShadowEdge(t), Contact: .7f, ContactDrop: 1, Accent: brass, AccentEdge: AccentEdge.Top), horizontal, vertical)
            : SurfaceStyle.Create(new(fill, bottom, C(t, ColorRole.ActionPrimaryHover).Lerp(C(t, ColorRole.FrameHighlight), .35f), C(t, ColorRole.AccentMuted).Darkened(.45f),
                Cuts.All(CardCut), UiTokens.EdgeRim, Frame: ShadowEdge(t), Brass: brass,
                BrassCorners: SurfaceSpec.TopLeft | SurfaceSpec.TopRight | SurfaceSpec.BottomRight | SurfaceSpec.BottomLeft,
                Contact: .75f, ContactDrop: 2, Soft: .5f, SoftSize: 6, SoftDrop: 3, Accent: brass, AccentEdge: AccentEdge.Left), horizontal, vertical);
    }

    /// <summary>The global bars (top bar, rail): graphite with a steel rim lit from above and a shadow onto the room.</summary>
    public static SurfaceStyle Bar(UiTokens t, int horizontal, int vertical) => SurfaceStyle.Create(new(
        C(t, ColorRole.SurfaceRaised).Lerp(C(t, ColorRole.FrameHighlight), .08f), C(t, ColorRole.SurfaceInset), C(t, ColorRole.FrameHighlight).Darkened(.1f),
        C(t, ColorRole.FrameStroke), Frame: ShadowEdge(t, .6f), Contact: .8f, ContactDrop: 3, Soft: .6f, SoftSize: 9, SoftDrop: 4), horizontal, vertical);

    /// <summary>Primary action. The global action (Continue, a workspace's one decision) is the premium control: a brass
    /// bevel inside a two-pixel brass wall, cut at two opposite corners, with contact and soft shadows. <paramref name="local"/>
    /// actions (decision verbs) keep the brass material with a hairline rim, smaller cuts and no soft lift, so Continue stays
    /// the strongest action. Hover brightens; a press inverts the bevel, shades the top and drops the lift.</summary>
    public static SurfaceStyle Primary(UiTokens t, SurfaceState state, int horizontal, int vertical, bool local = false)
    {
        var brass = C(t, ColorRole.ActionPrimary); var bright = C(t, ColorRole.ActionPrimaryHover);
        var frame = brass.Darkened(.82f) with { A = .55f }; var soft = local ? 0 : .55f;
        var cuts = Cuts.Diagonal(local ? ControlCut : UiTokens.ChamferSmall + 2); var wall = local ? 0 : UiTokens.EdgeRim;
        return state switch
        {
            SurfaceState.Hover => SurfaceStyle.Create(new(bright.Lightened(.14f), brass.Darkened(.06f), new Color(1, .96f, .84f), brass.Darkened(.45f), cuts, wall, Frame: frame,
                Contact: .85f, ContactDrop: 2, Soft: soft, SoftSize: 8, SoftDrop: 4), horizontal, vertical),
            SurfaceState.Pressed => SurfaceStyle.Create(new(brass.Darkened(.22f), brass, brass.Darkened(.5f), bright, cuts, wall, Frame: frame,
                InnerShade: new Color(0, 0, 0, .4f), InnerDepth: 5), horizontal, vertical, PressShift),
            SurfaceState.Disabled => SurfaceStyle.Create(Card(t, SurfaceState.Disabled).Spec with { Cuts = cuts }, horizontal, vertical),
            _ => SurfaceStyle.Create(new(bright.Lightened(.05f), brass.Darkened(.18f), new Color(1, .93f, .76f), brass.Darkened(.5f), cuts, wall, Frame: frame,
                Contact: .8f, ContactDrop: 2, Soft: soft, SoftSize: 6, SoftDrop: 3), horizontal, vertical)
        };
    }

    /// <summary>Secondary action: graphite with a steel bevel (bright rim on top), lower priority than brass. An optional
    /// semantic tint (the positive teal of match preparation) colours fill and rim, never the shape. Every state keeps the
    /// same cuts.</summary>
    public static SurfaceStyle Secondary(UiTokens t, SurfaceState state, int horizontal, int vertical, ColorRole? tint = null, bool outline = false, int cut = ControlCut)
    {
        var top = C(t, ColorRole.HeaderSurface).Lightened(.04f); var bottom = C(t, ColorRole.SurfaceRaised).Darkened(.1f);
        var rimTop = C(t, ColorRole.FrameHighlight); var rimBottom = C(t, ColorRole.FrameStroke).Darkened(.3f);
        if (tint is { } role)
        {
            var accent = C(t, role);
            if (!outline) { top = C(t, ColorRole.SurfaceInset).Lerp(accent, .3f); bottom = C(t, ColorRole.SurfaceInset).Lerp(accent, .12f); }
            rimTop = accent.Lightened(.15f); rimBottom = accent.Darkened(.55f);
        }
        var cuts = Cuts.Diagonal(cut);
        return state switch
        {
            SurfaceState.Hover => SurfaceStyle.Create(new(top.Lightened(.1f), bottom.Lightened(.08f), rimTop.Lightened(.25f), rimBottom, cuts, Frame: ShadowEdge(t),
                Contact: .75f, ContactDrop: 2, Soft: .4f, SoftSize: 5, SoftDrop: 3), horizontal, vertical),
            SurfaceState.Pressed => SurfaceStyle.Create(new(bottom.Darkened(.35f), top.Darkened(.1f), rimBottom, rimTop, cuts, Frame: ShadowEdge(t, .35f),
                InnerShade: new Color(0, 0, 0, .5f), InnerDepth: 5), horizontal, vertical, PressShift),
            SurfaceState.Disabled => SurfaceStyle.Create(Card(t, SurfaceState.Disabled).Spec with { Cuts = cuts }, horizontal, vertical),
            _ => SurfaceStyle.Create(new(top, bottom, rimTop, rimBottom, cuts, Frame: ShadowEdge(t, .45f), Contact: .6f, ContactDrop: 2), horizontal, vertical)
        };
    }

    /// <summary>Text input (line edits, spin boxes): a recessed well with control cuts; read-only inputs sit flatter.</summary>
    public static SurfaceStyle Input(UiTokens t, int horizontal, int vertical, bool readOnly = false) => SurfaceStyle.Create(Well(t, 0).Spec with
    {
        Top = C(t, ColorRole.SurfaceInset).Darkened(readOnly ? .1f : .25f), Bottom = C(t, ColorRole.SurfaceInset).Darkened(readOnly ? 0 : .05f),
        InnerShade = new Color(0, 0, 0, readOnly ? .2f : .45f), InnerDepth = 6, EdgeBottom = C(t, ColorRole.FrameHighlight) with { A = readOnly ? .15f : .35f }
    }, horizontal, vertical);

    /// <summary>Table column titles: one header strip (cells adjoin, so no cuts), a lit top edge and a dark lower line.</summary>
    public static SurfaceStyle TableHeader(UiTokens t, int horizontal, int vertical, bool hover = false) => SurfaceStyle.Create(new(
        C(t, ColorRole.HeaderSurface).Lerp(C(t, ColorRole.FrameHighlight), hover ? .12f : .04f), C(t, ColorRole.HeaderSurface).Lerp(C(t, ColorRole.SurfaceRaised), .5f),
        C(t, ColorRole.FrameHighlight) with { A = .3f }, C(t, ColorRole.FrameOuter) with { A = .8f }, Rim: RimMode.Edges), horizontal, vertical);

    /// <summary>The selected table row: warm graphite with a brass hairline and leading bar, control cuts, no lift (rows are
    /// utility; the brass marks the selection, not importance).</summary>
    public static SurfaceStyle RowSelected(UiTokens t, bool focused) => SurfaceStyle.Create(new(
        C(t, ColorRole.SelectedSurface).Lerp(C(t, ColorRole.ActionPrimary), focused ? .16f : .1f), C(t, ColorRole.SurfaceRaised).Lerp(C(t, ColorRole.ActionPrimary), .04f),
        C(t, ColorRole.ActionPrimaryHover) with { A = focused ? .9f : .6f }, C(t, ColorRole.AccentMuted).Darkened(.3f), Cuts.Diagonal(ControlCut),
        Accent: C(t, ColorRole.ActionPrimary), AccentEdge: AccentEdge.Left), 0, 0);

    /// <summary>Popup lists (choice drop-downs): a plate with control cuts and a steel rim; no outer shadow, which a popup
    /// window would clip.</summary>
    public static SurfaceStyle Popup(UiTokens t, int horizontal, int vertical) => SurfaceStyle.Create(Card(t, SurfaceState.Normal).Spec with
    {
        Top = C(t, ColorRole.SurfaceOverlay), Bottom = C(t, ColorRole.SurfaceRaised), Cuts = Cuts.Diagonal(ControlCut), Contact = 0, Frame = default
    }, horizontal, vertical);

    /// <summary>Utility tag (status chips, counts, badges, small notes): a flat plate with a hairline edge in its own tone and
    /// the smallest diagonal cut, so metadata shares the frame's angle family without competing with it.</summary>
    public static SurfaceStyle Tag(Color fill, Color edge, int horizontal, int vertical) =>
        SurfaceStyle.Create(new(fill, fill, edge, edge, Cuts.Diagonal(ControlCut - 1), Rim: edge.A > 0 ? RimMode.Loop : RimMode.None), horizontal, vertical);

    /// <summary>Keyboard focus: a ring outside the control that follows the same cuts as its frame, so focus never changes
    /// the shape.</summary>
    public static SurfaceStyle Focus(UiTokens t, Cuts cuts) => SurfaceStyle.Create(new(Colors.Transparent, Colors.Transparent, Colors.Transparent, Colors.Transparent, cuts,
        Rim: RimMode.None, Ring: C(t, ColorRole.FocusRing), RingGrow: UiTokens.FocusWidth + UiTokens.BorderWidth, RingWidth: UiTokens.FocusWidth), 0, 0);
}
