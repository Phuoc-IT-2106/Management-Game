using Godot;

namespace ManagementGame.UiKit;

/// <summary>Draws one approved semantic asset (<see cref="SemanticAssets"/>) at its own aspect ratio, never stretched,
/// recoloured or cropped into its visible content. Textures load once through Godot's import pipeline.</summary>
public partial class SemanticArt : TextureRect
{
    private static readonly Dictionary<SemanticConcept, Texture2D> Cache = [];
    public SemanticConcept Concept { get; private set; }

    /// <summary>The approved texture, or null when it is not in the project (checked first, so nothing is logged).</summary>
    public static Texture2D? Load(SemanticConcept concept)
    {
        if (Cache.TryGetValue(concept, out var cached)) return cached;
        var path = SemanticAssets.RuntimePath(concept);
        if (!ResourceLoader.Exists(path) || ResourceLoader.Load<Texture2D>(path) is not { } texture) return null;
        Cache[concept] = texture;
        return texture;
    }

    private static readonly Dictionary<SemanticConcept, Rect2?> Content = [];

    /// <summary>The part of the art that is not fully transparent, or null when it cannot be read (headless runs). Assets
    /// carry different transparent margins; drawing only this part gives them the same perceived size in one box. The
    /// art itself is untouched: nothing visible is cropped, recoloured or rescaled out of proportion.</summary>
    public static Rect2? ContentBounds(SemanticConcept concept, Texture2D texture)
    {
        if (Content.TryGetValue(concept, out var known)) return known;
        Rect2? bounds = null;
        if (DisplayServer.GetName() != "headless" && texture.GetImage() is { } image && !image.IsEmpty())
        {
            if (image.IsCompressed()) image.Decompress();
            var used = image.GetUsedRect();
            if (used.Size.X > 0 && used.Size.Y > 0) bounds = new Rect2(used.Position, used.Size);
        }
        Content[concept] = bounds;
        return bounds;
    }

    /// <summary>The approved art for <paramref name="concept"/>; when it cannot be loaded, the concept's own vector icon
    /// (<see cref="Fallback"/>). One policy for every caller. <paramref name="fitContent"/> draws only the visible part
    /// of the art (<see cref="ContentBounds"/>) so the art fills its box, as navigation icons need. A positive
    /// <paramref name="optical"/> sizes that visible part to one perceived size within <paramref name="size"/>
    /// (<see cref="SemanticAssets.OpticalSize"/>): a tall trophy and a wide building then read alike in the same slot.</summary>
    public static Control Create(SemanticConcept concept, Vector2 size, bool fitContent = false, float optical = 0)
    {
        if (Load(concept) is not { } source) return Fallback(concept, size);
        var region = fitContent ? ContentBounds(concept, source) : null;
        Texture2D texture = region is { } visible ? new AtlasTexture { Atlas = source, Region = visible } : source;
        if (optical > 0)
        {
            var (width, height) = SemanticAssets.OpticalSize(region?.Size.X ?? 0, region?.Size.Y ?? 0, optical, size.X, size.Y);
            size = new Vector2(Mathf.Round(width), Mathf.Round(height));
        }
        var art = new SemanticArt
        {
            Concept = concept, Texture = texture, CustomMinimumSize = size, ExpandMode = ExpandModeEnum.IgnoreSize,
            StretchMode = StretchModeEnum.KeepAspectCentered, TextureFilter = TextureFilterEnum.LinearWithMipmaps,
            MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        art.SetMeta("semantic_concept", concept.ToString());
        return art;
    }

    /// <summary>Missing approved art degrades to the concept's vector icon in the secondary text tone.</summary>
    public static UiIcon Fallback(SemanticConcept concept, Vector2 size)
    {
        var icon = UiIcon.Create(SemanticAssets.Get(concept).FallbackIcon, (int)Math.Max(16, Math.Min(size.X, size.Y) * .6f),
            UiTheme.ToGodot(UiTokens.Colors[ColorRole.TextSecondary]));
        icon.SetMeta("semantic_fallback", concept.ToString());
        return icon;
    }
}
