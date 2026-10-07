using Godot;

namespace ManagementGame.UiKit;

/// <summary>Generated letterless arms (genre shell S8): a shield silhouette, a division and one geometric charge.</summary>
public sealed record CrestArms(string AssetId, Rgb Primary, Rgb Secondary, int Shape, int Division, int Charge);
/// <summary>Generated letterless symbol (S8) for a brand (round badge with a geometric glyph) or a person (symbolic bust).</summary>
public sealed record SymbolMark(string AssetId, Rgb Primary, Rgb Secondary, int Glyph, bool Person);

/// <summary>Asset catalog: resolves presentation asset IDs (built from stable entity IDs, never display names) to marks.
/// Crest IDs resolve to generated arms, brand logo and portrait IDs to generated letterless symbols. No bitmap art is
/// registered, so news and environment IDs resolve to neutral, deterministic fallbacks. Nothing here is random and
/// nothing is fetched.</summary>
public sealed class IdentityAssets
{
    public const string DefaultCrest = "fallback:crest", DefaultLogo = "fallback:logo", DefaultPortrait = "fallback:portrait", DefaultNews = "fallback:news",
        DefaultEnvironment = "fallback:environment";
    // Club colours carry a hue family so a rival never shares the player's family. No violet (estimates), gold (action),
    // teal (positive state) or grey (interface) families.
    private static readonly (string Primary, string Secondary, string Family)[] Palettes =
    [
        ("14506B", "E9D9B8", "blue"), ("9A4A1C", "F1E4CF", "orange"), ("2F5D3A", "E8E2CF", "green"), ("7A1F2B", "EADFCB", "red"),
        ("1F3C78", "D7E1F0", "blue"), ("A11D33", "F3E3D3", "red"), ("6B4A2B", "EFE3D0", "brown"), ("5B6B20", "EEF0DA", "green")
    ];
    private readonly string playerCrest;
    private readonly string playerFamily;
    public UiTokens Tokens { get; }

    public IdentityAssets(UiTokens tokens, string playerCrestAssetId)
    {
        Tokens = tokens; playerCrest = playerCrestAssetId;
        playerFamily = IsKind(playerCrestAssetId, "crest") ? Palettes[Index(playerCrestAssetId)].Family : "";
    }
    /// <summary>Scene atmosphere ID (e.g. the Portal hero). Resolves to the procedural fallback until approved art exists.</summary>
    public static string Environment(string scene) => "environment:" + scene;
    private static bool IsKind(string assetId, string kind) =>
        assetId.Length > kind.Length + 1 && assetId.StartsWith(kind, StringComparison.Ordinal) && assetId[kind.Length] == ':';
    private static uint Hash(string text)
    {
        var h = 2166136261u;
        foreach (var c in text) { h ^= c; h *= 16777619u; }
        return h;
    }
    private static int Index(string assetId) => (int)(Hash(assetId) % (uint)Palettes.Length);
    /// <summary>Avalanche finalizer: similar IDs (e.g. brand slugs) still spread across palettes and glyphs.</summary>
    private static uint Mixed(string text)
    {
        var h = Hash(text);
        h ^= h >> 16; h *= 0x85EBCA6Bu; h ^= h >> 13; h *= 0xC2B2AE35u; h ^= h >> 16;
        return h;
    }

    /// <summary>The fallback an unregistered asset ID resolves to, or the ID itself when it is generated or registered.</summary>
    public static string Resolve(string assetId) =>
        IsKind(assetId, "crest") || IsKind(assetId, "logo") || IsKind(assetId, "portrait") ? assetId
        : assetId.StartsWith("crest:", StringComparison.Ordinal) ? DefaultCrest
        : assetId.StartsWith("logo:", StringComparison.Ordinal) ? DefaultLogo
        : assetId.StartsWith("portrait:", StringComparison.Ordinal) ? DefaultPortrait
        : assetId.StartsWith("environment:", StringComparison.Ordinal) ? DefaultEnvironment
        : DefaultNews;

    public CrestArms? Arms(string assetId)
    {
        if (!IsKind(assetId, "crest")) return null;
        var h = Hash(assetId);
        var index = Index(assetId);
        if (assetId != playerCrest && playerFamily.Length > 0)
            for (var i = 0; i < Palettes.Length && Palettes[index].Family == playerFamily; i++) index = (index + 1) % Palettes.Length;
        var (primary, secondary, _) = Palettes[index];
        return new CrestArms(assetId, Rgb.Hex(primary), Rgb.Hex(secondary), (int)(h >> 4) % 3, (int)(h >> 8) % 6, (int)(h >> 12) % 5);
    }
    public SymbolMark? Symbol(string assetId)
    {
        var person = IsKind(assetId, "portrait");
        if (!person && !IsKind(assetId, "logo")) return null;
        var h = Mixed(assetId);
        var (primary, secondary, _) = Palettes[h % (uint)Palettes.Length];
        return new SymbolMark(assetId, Rgb.Hex(primary), Rgb.Hex(secondary), (int)((h >> 8) % 5), person);
    }
    /// <summary>Identity colour for tinted regions; neutral when the asset has no generated identity.</summary>
    public Rgb TeamColor(string assetId) => Arms(assetId)?.Primary ?? Symbol(assetId)?.Primary ?? Tokens.Color(ColorRole.SurfaceInset);

    public AssetMark Create(string assetId, Vector2 size, bool plate = false)
    {
        var mark = new AssetMark(); mark.Bind(this, assetId, size, plate); return mark;
    }
}

/// <summary>Draws one catalog asset. A plate gives thumbnails a ground washed diagonally in the subject's own colour (or
/// neutral); the mark sits centred on it.</summary>
public partial class AssetMark : Control
{
    private IdentityAssets catalog = null!;
    private string assetId = "";
    private bool plate;
    public string AssetId => assetId;
    public string Resolution { get; private set; } = "";

    public void Bind(IdentityAssets assets, string id, Vector2 size, bool withPlate)
    {
        catalog = assets; assetId = id; plate = withPlate; Resolution = IdentityAssets.Resolve(id);
        CustomMinimumSize = size; MouseFilter = MouseFilterEnum.Ignore; FocusMode = FocusModeEnum.None;
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin; SizeFlagsVertical = SizeFlags.ShrinkCenter;
        SetMeta("asset_id", id); SetMeta("asset_resolution", Resolution);
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        // A neutral fallback plate names its kind with the shared icon family, never with invented imagery.
        if (catalog.Arms(id) is null && catalog.Symbol(id) is null && !id.StartsWith("crest:", StringComparison.Ordinal))
        {
            var glyph = UiIcon.Create(FallbackIcon(id), (int)Math.Min(32, size.Y * .5f), UiTheme.ToGodot(catalog.Tokens.Color(ColorRole.TextMuted)));
            AddChild(glyph); glyph.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.KeepSize);
        }
        QueueRedraw();
    }
    private static string FallbackIcon(string id)
    {
        var kind = id.StartsWith("fallback:news:", StringComparison.Ordinal) ? id["fallback:news:".Length..] : id.Split(':')[0];
        return kind switch
        {
            "logo" or "offer" or "negotiation" or "market" or "commercial" => "commercial",
            "portrait" => "squad", "contract" or "renewal" => "contract", "match" => "competition", "season" or "meta" => "calendar", _ => "news"
        };
    }
    private Color Tone(ColorRole role) => UiTheme.ToGodot(catalog.Tokens.Color(role));

    public override void _Draw()
    {
        if (catalog is null) return;
        var arms = catalog.Arms(assetId);
        var symbol = catalog.Symbol(assetId);
        var neutral = arms is null && symbol is null;
        if (plate || neutral && !assetId.StartsWith("crest:", StringComparison.Ordinal))
        {
            var ground = Tone(ColorRole.SurfaceInset);
            var tint = arms?.Primary ?? symbol?.Primary;
            var far = tint is { } t ? ground.Lerp(UiTheme.ToGodot(t), .5f) : Tone(ColorRole.SelectedSurface);
            DrawPolygon([Vector2.Zero, new(Size.X, 0), Size, new(0, Size.Y)], [ground.Lerp(far, .35f), ground.Lerp(far, .7f), far, ground.Lerp(far, .55f)]);
            var edge = new StyleBoxFlat { DrawCenter = false, BorderColor = Tone(ColorRole.Divider) };
            edge.SetBorderWidthAll(1); edge.SetCornerRadiusAll(UiTokens.CornerRadius);
            DrawStyleBox(edge, new Rect2(Vector2.Zero, Size));
        }
        if (symbol is not null) { DrawSymbol(symbol, plate ? .74f : 1f); return; }
        if (neutral && !assetId.StartsWith("crest:", StringComparison.Ordinal)) return;
        var height = plate ? Size.Y * .8f : Size.Y;
        var scale = Math.Min(Size.X / 100f, height / 120f);
        var offset = new Vector2((Size.X - 100 * scale) / 2, (Size.Y - 120 * scale) / 2);
        Vector2[] Map(IEnumerable<Vector2> points) => points.Select(p => offset + p * scale).ToArray();
        var shield = Map(Shield(arms?.Shape ?? 0));
        if (arms is null)
        {
            // Default crest: a neutral outline only, so a missing identity never looks like a real club.
            DrawColoredPolygon(shield, Tone(ColorRole.SurfaceInset));
            DrawPolyline([.. shield, shield[0]], Tone(ColorRole.Border), Math.Max(1, 2 * scale));
            return;
        }
        var primary = UiTheme.ToGodot(arms.Primary); var secondary = UiTheme.ToGodot(arms.Secondary);
        DrawColoredPolygon(shield, primary);
        if (Division(arms.Division) is { } division)
            foreach (var part in Geometry2D.IntersectPolygons(shield, Map(division))) DrawColoredPolygon(part, secondary);
        var split = arms.Division is 1 or 3;
        var charge = split ? new Color("F4F1EA") : arms.Division == 5 ? primary : secondary;
        DrawCharge(arms.Charge, offset + new Vector2(50, arms.Division == 4 ? 36 : 50) * scale, scale, charge, split ? new Color("15171A") : (Color?)null);
        DrawPolyline([.. shield, shield[0]], secondary, Math.Max(1, 2.5f * scale));
    }

    /// <summary>Brand: a round badge with one geometric glyph. Person: a symbolic bust. Both letterless and generated.</summary>
    private void DrawSymbol(SymbolMark mark, float share)
    {
        var r = Math.Min(Size.X, Size.Y) * share / 2; var c = Size / 2;
        var primary = UiTheme.ToGodot(mark.Primary); var secondary = UiTheme.ToGodot(mark.Secondary);
        var w = Math.Max(1.5f, r * .1f);
        DrawCircle(c, r, primary);
        DrawArc(c, r - w / 2, 0, Mathf.Tau, 48, secondary, w, true);
        if (mark.Person)
        {
            DrawCircle(c + new Vector2(0, -r * .22f), r * .3f, secondary);
            var body = Enumerable.Range(0, 17).Select(i => c + new Vector2(-r * .62f + r * 1.24f * i / 16, r * .78f - r * .62f * Mathf.Sin(Mathf.Pi * i / 16))).ToArray();
            var disc = Enumerable.Range(0, 32).Select(i => c + new Vector2(Mathf.Cos(Mathf.Tau * i / 32), Mathf.Sin(Mathf.Tau * i / 32)) * (r - w)).ToArray();
            foreach (var part in Geometry2D.IntersectPolygons(body, disc)) DrawColoredPolygon(part, secondary);
            return;
        }
        Vector2 P(float x, float y) => c + new Vector2(x, y) * r;
        switch (mark.Glyph)
        {
            case 0: DrawColoredPolygon([P(-.5f, -.1f), P(0, -.5f), P(.5f, -.1f), P(.5f, .2f), P(0, -.2f), P(-.5f, .2f)], secondary);
                DrawColoredPolygon([P(-.5f, .3f), P(0, -.1f), P(.5f, .3f), P(.5f, .55f), P(0, .15f), P(-.5f, .55f)], secondary); break;
            case 1: foreach (var x in new[] { -.36f, 0f, .36f }) DrawRect(new Rect2(P(x - .11f, -.42f + Math.Abs(x) * .5f), new Vector2(.22f * r, (.84f - Math.Abs(x)) * r)), secondary); break;
            case 2: DrawColoredPolygon([P(0, -.52f), P(.46f, 0), P(0, .52f), P(-.46f, 0)], secondary); DrawCircle(c, r * .16f, primary); break;
            case 3: DrawArc(c, r * .38f, 0, Mathf.Tau, 40, secondary, r * .16f, true); DrawCircle(c, r * .12f, secondary); break;
            default: DrawColoredPolygon([P(-.5f, .35f), P(-.15f, -.4f), P(.05f, .05f), P(.2f, -.25f), P(.5f, .35f)], secondary); break;
        }
    }

    private static IEnumerable<Vector2> Shield(int shape)
    {
        if (shape == 1)
        {
            yield return new(6, 6); yield return new(94, 6); yield return new(94, 70);
            for (var i = 1; i < 16; i++) { var t = Mathf.Pi * i / 16; yield return new(50 + 44 * Mathf.Cos(t), 70 + 44 * Mathf.Sin(t)); }
            yield return new(6, 70); yield break;
        }
        if (shape == 2) { yield return new(6, 6); yield return new(94, 6); yield return new(94, 72); yield return new(50, 116); yield return new(6, 72); yield break; }
        yield return new(6, 6); yield return new(94, 6); yield return new(94, 52);
        foreach (var p in Bezier(new(94, 52), new(94, 86), new(72, 106), new(50, 116))) yield return p;
        foreach (var p in Bezier(new(50, 116), new(28, 106), new(6, 86), new(6, 52))) yield return p;
    }
    private static IEnumerable<Vector2> Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        for (var i = 1; i <= 12; i++)
        {
            var t = i / 12f; var u = 1 - t;
            yield return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
        }
    }
    private static Vector2[]? Division(int division) => division switch
    {
        1 => [new(50, 0), new(100, 0), new(100, 120), new(50, 120)],
        2 => [new(0, 62), new(100, 62), new(100, 120), new(0, 120)],
        3 => [new(0, 0), new(100, 120), new(0, 120)],
        4 => [new(0, 88), new(50, 58), new(100, 88), new(100, 110), new(50, 80), new(0, 110)],
        5 => [new(36, 0), new(64, 0), new(64, 120), new(36, 120)],
        _ => null
    };
    private void DrawCharge(int charge, Vector2 c, float s, Color fill, Color? edge)
    {
        Vector2[] Poly(params (float X, float Y)[] points) => points.Select(p => c + new Vector2(p.X, p.Y) * s).ToArray();
        Vector2[] shape;
        switch (charge)
        {
            case 0:
                DrawCircle(c, 14 * s, fill);
                if (edge is { } e0) DrawArc(c, 14 * s, 0, Mathf.Tau, 32, e0, Math.Max(1, 2 * s));
                return;
            case 3:
                DrawArc(c, 12 * s, 0, Mathf.Tau, 32, edge ?? fill, Math.Max(2, 9 * s));
                DrawArc(c, 12 * s, 0, Mathf.Tau, 32, fill, Math.Max(1, 6 * s));
                return;
            case 1: shape = Poly((0, -19), (14, 0), (0, 19), (-14, 0)); break;
            case 2: shape = Enumerable.Range(0, 10).Select(i => { var r = i % 2 == 1 ? 7f : 17f; var a = -Mathf.Pi / 2 + i * Mathf.Pi / 5; return c + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a)) * s; }).ToArray(); break;
            default: shape = Poly((-16, -13), (16, -13), (0, 16)); break;
        }
        DrawColoredPolygon(shape, fill);
        if (edge is { } e) DrawPolyline([.. shape, shape[0]], e, Math.Max(1, 2 * s));
    }
}
