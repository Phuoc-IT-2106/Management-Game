using Godot;

namespace ManagementGame.UiKit;

/// <summary>Explanatory motion only (#29): acknowledge a change or a context transition.
/// Durations come from tokens; reduced motion makes every call a no-op, never a different layout.</summary>
public static class UiMotion
{
    /// <summary>Warm flash fading to normal on content whose authoritative value just changed.</summary>
    public static void Highlight(UiContext ui, CanvasItem item)
    {
        var duration = ui.Tokens.Duration(MotionRole.ChangeHighlight);
        if (duration <= 0 || !item.IsInsideTree()) return;
        var tint = UiTheme.ToGodot(ui.Tokens.Color(ColorRole.InformationEstimated));
        item.Modulate = Colors.White.Lerp(tint, .6f);
        item.CreateTween().TweenProperty(item, "modulate", Colors.White, duration).SetEase(Tween.EaseType.Out);
    }
    /// <summary>Fade-in for content that replaced the previous context.</summary>
    public static void Reveal(UiContext ui, CanvasItem item)
    {
        var duration = ui.Tokens.Duration(MotionRole.ContextTransition);
        if (duration <= 0 || !item.IsInsideTree()) return;
        item.Modulate = new Color(1, 1, 1, 0);
        item.CreateTween().TweenProperty(item, "modulate:a", 1f, duration).SetEase(Tween.EaseType.Out);
    }
}
