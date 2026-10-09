using System.Numerics;

namespace ManagementGame.UiKit;

/// <summary>Importance of a framed element; richer geometry and brass only where the gameplay weight justifies it.
/// Premium: Continue, key decisions, next match. Standard: the other Portal regions. Utility: rows, filters, small controls.</summary>
public enum FrameTier { Premium, Standard, Utility }

/// <summary>Corner cuts of a frame. Every cut is a 45° chamfer (equal legs, in pixels); 0 keeps a square corner. One angle
/// family everywhere: only the leg length changes with importance.</summary>
public readonly record struct Cuts(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public static Cuts All(float c) => new(c, c, c, c);
    /// <summary>Top-left and bottom-right: the restrained cut of controls and nested plates.</summary>
    public static Cuts Diagonal(float c) => new(c, 0, c, 0);
    public float Max => Math.Max(Math.Max(TopLeft, TopRight), Math.Max(BottomRight, BottomLeft));
    public float this[int corner] => corner switch { 0 => TopLeft, 1 => TopRight, 2 => BottomRight, _ => BottomLeft };

    /// <summary>The cuts of the same outline moved inward by <paramref name="distance"/> (negative: outward). A 45° leg
    /// shrinks by d(2 − √2) so the inner chamfer stays parallel to the outer one; square corners stay square.</summary>
    public Cuts Inset(float distance)
    {
        static float Move(float c, float d) => c <= 0 ? 0 : Math.Max(0, c - d * (2 - MathF.Sqrt(2)));
        return new(Move(TopLeft, distance), Move(TopRight, distance), Move(BottomRight, distance), Move(BottomLeft, distance));
    }

    /// <summary>Cuts reduced so they fit a <paramref name="width"/> × <paramref name="height"/> rectangle: adjacent legs
    /// never exceed the edge between them, so small controls keep a valid outline instead of folding over.</summary>
    public Cuts Fit(float width, float height)
    {
        var scale = 1f;
        foreach (var (a, b, edge) in new[] { (TopLeft, TopRight, width), (BottomLeft, BottomRight, width), (TopLeft, BottomLeft, height), (TopRight, BottomRight, height) })
            if (a + b > edge && a + b > 0) scale = Math.Min(scale, Math.Max(0, edge) / (a + b));
        return scale >= 1 ? this : new(TopLeft * scale, TopRight * scale, BottomRight * scale, BottomLeft * scale);
    }
}

/// <summary>Engine-independent outline math for the frame system (tested by the UiKit suite). Coordinates are pixels with y
/// growing downward, as in Godot.</summary>
public static class FrameGeometry
{
    /// <summary>The eight outline vertices, clockwise from the top-left chamfer's upper end. A square corner repeats its
    /// point, so outer and inset outlines always correspond vertex by vertex (facet i joins vertex i and i + 1).</summary>
    public static Vector2[] Corners(float x, float y, float width, float height, Cuts cuts)
    {
        var c = cuts.Fit(width, height);
        var (e, b) = (x + width, y + height);
        return
        [
            new(x + c.TopLeft, y), new(e - c.TopRight, y),
            new(e, y + c.TopRight), new(e, b - c.BottomRight),
            new(e - c.BottomRight, b), new(x + c.BottomLeft, b),
            new(x, b - c.BottomLeft), new(x, y + c.TopLeft)
        ];
    }

    /// <summary>The outline without repeated points (for polygon fills, which reject degenerate edges).</summary>
    public static Vector2[] Outline(float x, float y, float width, float height, Cuts cuts)
    {
        var points = Corners(x, y, width, height, cuts);
        var result = new List<Vector2>(8);
        foreach (var p in points)
            if (result.Count == 0 || Vector2.DistanceSquared(result[^1], p) > 1e-4f) result.Add(p);
        if (result.Count > 1 && Vector2.DistanceSquared(result[0], result[^1]) <= 1e-4f) result.RemoveAt(result.Count - 1);
        return [.. result];
    }

    /// <summary>Outward normal of facet <paramref name="index"/> (0 top, 1 top-right chamfer, 2 right, … 7 top-left chamfer).</summary>
    public static Vector2 Normal(int index)
    {
        const float d = 0.70710678f;
        return index switch
        {
            0 => new(0, -1), 1 => new(d, -d), 2 => new(1, 0), 3 => new(d, d),
            4 => new(0, 1), 5 => new(-d, d), 6 => new(-1, 0), _ => new(-d, -d)
        };
    }

    /// <summary>The corner a chamfer facet belongs to (facets 1, 3, 5, 7), or −1 for straight edges.</summary>
    public static int ChamferCorner(int facet) => facet switch { 7 => 0, 1 => 1, 3 => 2, 5 => 3, _ => -1 };

    /// <summary>How much of the light from above, slightly from the left, a facet facing <paramref name="normal"/> catches:
    /// 1 for the top edge, 0 for the bottom. The one light direction shared by every frame.</summary>
    public static float Light(Vector2 normal)
    {
        var light = Vector2.Normalize(new Vector2(-.35f, -1));
        return Math.Clamp((Vector2.Dot(normal, light) + 1) / 2, 0, 1);
    }

    /// <summary>True when the rectangle lies inside the cut outline, within <paramref name="tolerance"/> pixels: content
    /// placed there is never clipped or crossed by a chamfer.</summary>
    public static bool Contains(float x, float y, float width, float height, Cuts cuts, float left, float top, float right, float bottom, float tolerance = 1)
    {
        var c = cuts.Fit(width, height);
        var (e, b) = (x + width, y + height);
        if (left < x - tolerance || top < y - tolerance || right > e + tolerance || bottom > b + tolerance) return false;
        return (left - x) + (top - y) >= c.TopLeft - tolerance && (e - right) + (top - y) >= c.TopRight - tolerance
            && (e - right) + (b - bottom) >= c.BottomRight - tolerance && (left - x) + (b - bottom) >= c.BottomLeft - tolerance;
    }
}
