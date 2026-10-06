using Godot;

namespace ManagementGame.UiKit;

/// <summary>Native shared Theme resource. All appearance values come from UiTokens.</summary>
public static class UiTheme
{
    public static Color ToGodot(Rgb color) => Color.Color8(color.R, color.G, color.B);
    public static Theme Build(UiTokens tokens)
    {
        var font = new SystemFont { FontNames = UiTokens.UiFontNames, AllowSystemFallback = true };
        var display = new SystemFont { FontNames = UiTokens.DisplayFontNames, FontWeight = UiTokens.DisplayWeight, AllowSystemFallback = true };
        // Tabular figures keep columns of values aligned without a code/monospace face.
        var numeric = new FontVariation { BaseFont = new SystemFont { FontNames = UiTokens.NumericFontNames, AllowSystemFallback = true },
            OpentypeFeatures = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("tnum"), 1 } } };
        var theme = new Theme { DefaultFont = font, DefaultFontSize = tokens.FontSize(TypographyRole.Body) };
        foreach (var role in Enum.GetValues<ColorRole>()) theme.SetColor(role.ToString(), "Semantic", ToGodot(tokens.Color(role)));
        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            if (role != TypographyRole.Label) theme.SetTypeVariation(role.ToString(), "Label");
            theme.SetFontSize("font_size", role.ToString(), tokens.FontSize(role));
            theme.SetFont("font", role.ToString(), role switch
            {
                TypographyRole.Data => numeric,
                TypographyRole.CompanyIdentity or TypographyRole.WorkspaceTitle or TypographyRole.SectionTitle => display,
                _ => font
            });
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
        // Entities are rows, not buttons: flat until hovered, with surfaces instead of per-item borders.
        theme.SetTypeVariation("Entity", "Button");
        theme.SetStylebox("normal", "Entity", Flat(tokens, null));
        theme.SetStylebox("disabled", "Entity", Flat(tokens, null));
        theme.SetStylebox("hover", "Entity", Flat(tokens, ColorRole.HoverSurface));
        theme.SetStylebox("pressed", "Entity", Flat(tokens, ColorRole.SelectedSurface));
        theme.SetStylebox("hover_pressed", "Entity", Flat(tokens, ColorRole.SelectedSurface));
        theme.SetTypeVariation("SelectedEntity", "Button");
        // Leading edge bar keeps selection perceivable without relying on surface color alone.
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
            theme.SetStylebox(state, "SelectedEntity", Selected(tokens, state.StartsWith("hover") ? ColorRole.HoverSurface : ColorRole.SelectedSurface));
        // One inverted-neutral treatment marks the workspace decision; it is not a semantic state color.
        theme.SetTypeVariation("PrimaryAction", "Button");
        theme.SetStylebox("normal", "PrimaryAction", Box(tokens, ColorRole.ActionPrimary, false));
        theme.SetStylebox("hover", "PrimaryAction", Box(tokens, ColorRole.ActionPrimaryHover, false));
        theme.SetStylebox("pressed", "PrimaryAction", Box(tokens, ColorRole.ActionPrimaryHover, false));
        theme.SetStylebox("hover_pressed", "PrimaryAction", Box(tokens, ColorRole.ActionPrimaryHover, false));
        theme.SetStylebox("disabled", "PrimaryAction", Box(tokens, ColorRole.SurfaceInset));
        foreach (var item in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
            theme.SetColor(item, "PrimaryAction", ToGodot(tokens.Color(ColorRole.TextOnAction)));
        theme.SetFont("font", "PrimaryAction", display);
        foreach (var surface in new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset, ColorRole.SurfaceOverlay })
        {
            theme.SetTypeVariation(surface.ToString(), "PanelContainer");
            // Surface tiers carry structure; raised/inset edges are subtle dividers, overlays keep an essential border.
            var box = Box(tokens, surface, surface != ColorRole.SurfaceBase);
            if (surface is ColorRole.SurfaceRaised or ColorRole.SurfaceInset) box.BorderColor = ToGodot(tokens.Color(ColorRole.Divider));
            theme.SetStylebox("panel", surface.ToString(), box);
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
        // Dense tables (genre shell S5): structural rows, no per-cell borders, the shared selection bar.
        theme.SetStylebox("panel", "Tree", Box(tokens, ColorRole.SurfaceInset, false));
        theme.SetStylebox("focus", "Tree", Focus(tokens));
        theme.SetStylebox("selected", "Tree", Selected(tokens, ColorRole.SelectedSurface));
        theme.SetStylebox("selected_focus", "Tree", Selected(tokens, ColorRole.SelectedSurface));
        theme.SetStylebox("cursor", "Tree", Flat(tokens, null));
        theme.SetStylebox("cursor_unfocused", "Tree", Flat(tokens, null));
        theme.SetStylebox("title_button_normal", "Tree", Box(tokens, ColorRole.SurfaceRaised, false));
        theme.SetStylebox("title_button_hover", "Tree", Box(tokens, ColorRole.HoverSurface, false));
        theme.SetStylebox("title_button_pressed", "Tree", Box(tokens, ColorRole.SelectedSurface, false));
        foreach (var item in new[] { "font_color", "font_selected_color", "title_button_color" })
            theme.SetColor(item, "Tree", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetColor("guide_color", "Tree", ToGodot(tokens.Color(ColorRole.Divider)));
        theme.SetConstant("draw_guides", "Tree", 1);
        theme.SetConstant("v_separation", "Tree", tokens.Space(SpaceRole.SpaceRelated));
        theme.SetFont("title_button_font", "Tree", display);
        theme.SetFontSize("font_size", "Tree", tokens.FontSize(TypographyRole.Label));
        theme.SetFontSize("title_button_font_size", "Tree", tokens.FontSize(TypographyRole.Annotation));
        foreach (var kind in new[] { "LineEdit" })
        {
            theme.SetStylebox("normal", kind, Box(tokens, ColorRole.SurfaceInset));
            theme.SetStylebox("focus", kind, Focus(tokens));
            theme.SetStylebox("read_only", kind, Box(tokens, ColorRole.SurfaceBase));
            theme.SetColor("font_color", kind, ToGodot(tokens.Color(ColorRole.TextPrimary)));
            theme.SetColor("font_placeholder_color", kind, ToGodot(tokens.Color(ColorRole.TextMuted)));
            theme.SetColor("caret_color", kind, ToGodot(tokens.Color(ColorRole.FocusRing)));
            theme.SetFontSize("font_size", kind, tokens.FontSize(TypographyRole.Label));
        }
        theme.SetStylebox("panel", "PopupMenu", Box(tokens, ColorRole.SurfaceOverlay));
        theme.SetStylebox("hover", "PopupMenu", Box(tokens, ColorRole.HoverSurface, false));
        theme.SetColor("font_color", "PopupMenu", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetColor("font_hover_color", "PopupMenu", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetFontSize("font_size", "PopupMenu", tokens.FontSize(TypographyRole.Label));
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
    private static StyleBoxFlat Flat(UiTokens tokens, ColorRole? surface)
    {
        var box = Box(tokens, surface ?? ColorRole.SurfaceBase, false);
        box.DrawCenter = surface is not null;
        return box;
    }
    private static StyleBoxFlat Selected(UiTokens tokens, ColorRole surface)
    {
        var box = Box(tokens, surface, false);
        box.BorderColor = ToGodot(tokens.Color(ColorRole.TextPrimary));
        box.BorderWidthLeft = UiTokens.SelectionWidth;
        box.ContentMarginLeft += UiTokens.SelectionWidth;
        return box;
    }
    // Drawn outside the control so the ring keeps contrast on light primary fills and dark rows alike.
    private static StyleBoxFlat Focus(UiTokens tokens) => new()
    {
        DrawCenter = false, BorderColor = ToGodot(tokens.Color(ColorRole.FocusRing)),
        ExpandMarginLeft = UiTokens.FocusWidth + UiTokens.BorderWidth, ExpandMarginRight = UiTokens.FocusWidth + UiTokens.BorderWidth,
        ExpandMarginTop = UiTokens.FocusWidth + UiTokens.BorderWidth, ExpandMarginBottom = UiTokens.FocusWidth + UiTokens.BorderWidth,
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
    /// <summary>The single decision action of a workspace.</summary>
    public Button PrimaryButton(string text, Action action)
    {
        var button = Button(text, action); button.ThemeTypeVariation = "PrimaryAction"; return button;
    }
    /// <summary>The one development marker allowed in player space.</summary>
    public Label Watermark() => SemanticText.Create(this, PresentationText.DevelopmentWatermark, TypographyRole.Annotation, ColorRole.TextMuted);
    public Button Button(string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All,
            CustomMinimumSize = new Vector2(Tokens.ActionMinimumWidth, Tokens.ControlHeight), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        button.Pressed += action;
        return button;
    }
    public void Tone(Label label, ColorRole role) => label.AddThemeColorOverride("font_color", UiTheme.ToGodot(Tokens.Color(role)));
}
