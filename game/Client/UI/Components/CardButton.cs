using Godot;

namespace ManagementGame.UiKit;

/// <summary>A focusable button whose content is laid out by ordinary containers. Its minimum size follows the content,
/// so wrapped names grow the row instead of being clipped. Children never take the pointer or focus.</summary>
public partial class CardButton : Button
{
    private readonly MarginContainer inner = new() { MouseFilter = MouseFilterEnum.Ignore };
    public VBoxContainer Body { get; } = new() { MouseFilter = MouseFilterEnum.Ignore };

    public CardButton() : this(12, 8) { }
    public CardButton(int horizontal, int vertical)
    {
        FocusMode = FocusModeEnum.All; ClipText = true; SizeFlagsHorizontal = SizeFlags.ExpandFill;
        foreach (var (edge, value) in new[] { ("left", horizontal), ("right", horizontal), ("top", vertical), ("bottom", vertical) })
            inner.AddThemeConstantOverride("margin_" + edge, value);
        Body.AddThemeConstantOverride("separation", 2);
        inner.AddChild(Body); AddChild(inner);
        inner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Button computes its own minimum from text, so the content's minimum is mirrored into the custom minimum.
        // Wrapped labels report a height for the current width; resizing re-measures them.
        inner.MinimumSizeChanged += Fit;
        Resized += Fit;
    }
    private void Fit()
    {
        var wanted = inner.GetCombinedMinimumSize();
        var next = new Vector2(Math.Max(wanted.X, MinimumWidth), Math.Max(wanted.Y, MinimumHeight));
        if (!next.IsEqualApprox(CustomMinimumSize)) CustomMinimumSize = next;
    }
    /// <summary>Lower bounds; content larger than these still grows the card.</summary>
    public float MinimumWidth { get; set; }
    public float MinimumHeight { get; set; }
    /// <summary>Containers placed inside a card must not swallow clicks meant for the card.</summary>
    public static T Passive<T>(T control) where T : Control { control.MouseFilter = MouseFilterEnum.Ignore; return control; }
}
