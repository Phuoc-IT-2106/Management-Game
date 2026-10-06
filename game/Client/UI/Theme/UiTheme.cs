using Godot;

namespace ManagementGame.UiKit;

/// <summary>Native shared Theme resource. All appearance values come from UiTokens.</summary>
public static class UiTheme
{
    public static Color ToGodot(Rgb color) => Color.Color8(color.R, color.G, color.B);
    public static Theme Build(UiTokens tokens)
    {
        var font = new SystemFont { FontNames = UiTokens.UiFontNames, AllowSystemFallback = true };
        var numeric = new SystemFont { FontNames = UiTokens.NumericFontNames, AllowSystemFallback = true };
        var theme = new Theme { DefaultFont = font, DefaultFontSize = tokens.FontSize(TypographyRole.Body) };
        foreach (var role in Enum.GetValues<ColorRole>()) theme.SetColor(role.ToString(), "Semantic", ToGodot(tokens.Color(role)));
        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            if (role != TypographyRole.Label) theme.SetTypeVariation(role.ToString(), "Label");
            theme.SetFontSize("font_size", role.ToString(), tokens.FontSize(role));
            theme.SetFont("font", role.ToString(), role == TypographyRole.Data ? numeric : font);
        }
        theme.SetColor("font_color", "Label", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        foreach (var kind in new[] { "Button", "OptionButton", "CheckButton", "CheckBox" })
        {
            foreach (var item in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
                theme.SetColor(item, kind, ToGodot(tokens.Color(ColorRole.TextPrimary)));
            theme.SetColor("font_disabled_color", kind, ToGodot(tokens.Color(ColorRole.TextMuted)));
            theme.SetFontSize("font_size", kind, tokens.FontSize(TypographyRole.Label));
            theme.SetStylebox("normal", kind, Box(tokens, ColorRole.SurfaceRaised));
            theme.SetStylebox("hover", kind, Box(tokens, ColorRole.HoverSurface));
            theme.SetStylebox("pressed", kind, Box(tokens, ColorRole.SelectedSurface));
            theme.SetStylebox("hover_pressed", kind, Box(tokens, ColorRole.SelectedSurface));
            theme.SetStylebox("disabled", kind, Box(tokens, ColorRole.SurfaceInset));
            theme.SetStylebox("focus", kind, Focus(tokens));
        }
        theme.SetTypeVariation("SelectedEntity", "Button");
        // Leading edge bar keeps selection perceivable without relying on surface color alone.
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed" })
            theme.SetStylebox(state, "SelectedEntity", Selected(tokens, state.StartsWith("hover") ? ColorRole.HoverSurface : ColorRole.SelectedSurface));
        foreach (var surface in new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset, ColorRole.SurfaceOverlay })
        {
            theme.SetTypeVariation(surface.ToString(), "PanelContainer");
            theme.SetStylebox("panel", surface.ToString(), Box(tokens, surface, surface != ColorRole.SurfaceBase));
        }
        foreach (var kind in new[] { "VBoxContainer", "HBoxContainer" })
            theme.SetConstant("separation", kind, tokens.Space(SpaceRole.SpaceRelated));
        foreach (var kind in new[] { "HFlowContainer", "VFlowContainer", "GridContainer" })
        {
            theme.SetConstant("h_separation", kind, tokens.Space(SpaceRole.SpaceRelated));
            theme.SetConstant("v_separation", kind, tokens.Space(SpaceRole.SpaceRelated));
        }
        theme.SetStylebox("scroll", "VScrollBar", Box(tokens, ColorRole.SurfaceInset, false));
        theme.SetStylebox("focus", "ScrollContainer", Focus(tokens));
        theme.SetStylebox("grabber", "VScrollBar", Box(tokens, ColorRole.Border, false));
        theme.SetStylebox("grabber_highlight", "VScrollBar", Box(tokens, ColorRole.TextMuted, false));
        theme.SetStylebox("grabber_pressed", "VScrollBar", Box(tokens, ColorRole.TextSecondary, false));
        theme.SetStylebox("panel", "TooltipPanel", Box(tokens, ColorRole.SurfaceOverlay));
        theme.SetColor("font_color", "TooltipLabel", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        return theme;
    }
    public static StyleBoxFlat Box(UiTokens tokens, ColorRole surface, bool border = true) => new()
    {
        BgColor = ToGodot(tokens.Color(surface)), BorderColor = ToGodot(tokens.Color(ColorRole.Border)),
        BorderWidthLeft = border ? UiTokens.BorderWidth : 0, BorderWidthRight = border ? UiTokens.BorderWidth : 0,
        BorderWidthTop = border ? UiTokens.BorderWidth : 0, BorderWidthBottom = border ? UiTokens.BorderWidth : 0,
        CornerRadiusTopLeft = UiTokens.CornerRadius, CornerRadiusTopRight = UiTokens.CornerRadius,
        CornerRadiusBottomLeft = UiTokens.CornerRadius, CornerRadiusBottomRight = UiTokens.CornerRadius,
        ContentMarginLeft = tokens.Space(SpaceRole.SpaceGroup), ContentMarginRight = tokens.Space(SpaceRole.SpaceGroup),
        ContentMarginTop = tokens.Space(SpaceRole.SpaceRelated), ContentMarginBottom = tokens.Space(SpaceRole.SpaceRelated)
    };
    private static StyleBoxFlat Selected(UiTokens tokens, ColorRole surface)
    {
        var box = Box(tokens, surface);
        box.BorderColor = ToGodot(tokens.Color(ColorRole.TextPrimary));
        box.BorderWidthLeft = UiTokens.SelectionWidth;
        return box;
    }
    private static StyleBoxFlat Focus(UiTokens tokens) => new()
    {
        DrawCenter = false, BorderColor = ToGodot(tokens.Color(ColorRole.FocusRing)),
        BorderWidthLeft = UiTokens.FocusWidth, BorderWidthRight = UiTokens.FocusWidth,
        BorderWidthTop = UiTokens.FocusWidth, BorderWidthBottom = UiTokens.FocusWidth,
        CornerRadiusTopLeft = UiTokens.CornerRadius, CornerRadiusTopRight = UiTokens.CornerRadius,
        CornerRadiusBottomLeft = UiTokens.CornerRadius, CornerRadiusBottomRight = UiTokens.CornerRadius
    };
}

/// <summary>Small composition helpers, not a screen framework.</summary>
public sealed class UiContext(UiTokens tokens)
{
    public UiTokens Tokens { get; } = tokens;
    public Theme Theme { get; } = UiTheme.Build(tokens);
    public VBoxContainer Stack() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    public HFlowContainer Flow() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    public PanelContainer Panel(Control child, ColorRole surface = ColorRole.SurfaceRaised)
    {
        var panel = new PanelContainer { ThemeTypeVariation = surface.ToString(), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddChild(child); return panel;
    }
    public Button Button(string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All,
            CustomMinimumSize = new Vector2(Tokens.ActionMinimumWidth, Tokens.ControlHeight), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        button.Pressed += action;
        return button;
    }
    public void Tone(Label label, ColorRole role) => label.AddThemeColorOverride("font_color", UiTheme.ToGodot(Tokens.Color(role)));
}
