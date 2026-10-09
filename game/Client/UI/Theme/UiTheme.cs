using Godot;

namespace ManagementGame.UiKit;

/// <summary>Native shared Theme resource. All appearance values come from UiTokens.</summary>
public static class UiTheme
{
    public static Color ToGodot(Rgb color) => Color.Color8(color.R, color.G, color.B);
    /// <summary>The bundled face of one system weight (<see cref="UiTokens.FontFamily"/>), shared by every theme. Glyphs the
    /// family lacks (◐ and other symbols) fall back to system symbol faces. Without the imported font the system UI face
    /// stands in at the same weight, so a missing import degrades the look, never the layout logic.</summary>
    public static Font Face(int weight)
    {
        if (Faces.TryGetValue(weight, out var cached)) return cached;
        var path = UiTokens.FontPath(weight);
        Font face = ResourceLoader.Exists(path) && ResourceLoader.Load<FontFile>(path) is { } file
            ? file
            : new SystemFont { FontNames = [UiTokens.FontFamily, .. UiTokens.UiFontNames], FontWeight = weight, AllowSystemFallback = true };
        if (face is FontFile bundled) bundled.Fallbacks = [new SystemFont { FontNames = UiTokens.SymbolFontNames, AllowSystemFallback = true }];
        return Faces[weight] = face;
    }
    private static readonly Dictionary<int, Font> Faces = [];
    /// <summary>True when the bundled family is loaded (not the system stand-in).</summary>
    public static bool BundledFace(Font font) => (font is FontVariation v ? v.BaseFont : font) is FontFile file
        && file.FontName.StartsWith(UiTokens.FontFamily, StringComparison.Ordinal);
    public static Theme Build(UiTokens tokens)
    {
        // One family, three weights (UiTokens.Weight). Tabular figures keep changing values (cash, days, records) from
        // shifting sideways; case-sensitive forms seat punctuation on caps labels. Letters are unaffected.
        var server = TextServerManager.GetPrimaryInterface();
        FontVariation Variant(int weight, bool tabular, int tracking = 0, bool caps = false)
        {
            var features = new Godot.Collections.Dictionary();
            if (tabular) features[server.NameToTag("tnum")] = 1;
            if (caps) features[server.NameToTag("case")] = 1;
            return new FontVariation { BaseFont = Face(weight), OpentypeFeatures = features, SpacingGlyph = tracking };
        }
        var font = Variant(UiTokens.WeightRegular, false);
        var strong = Variant(UiTokens.WeightSemibold, true);
        // Section titles: semibold caps with one pixel of tracking, the broadcast header of the reference.
        var caps = Variant(UiTokens.WeightSemibold, true, 1, caps: true);
        var theme = new Theme { DefaultFont = font, DefaultFontSize = tokens.FontSize(TypographyRole.Body) };
        foreach (var role in Enum.GetValues<ColorRole>()) theme.SetColor(role.ToString(), "Semantic", ToGodot(tokens.Color(role)));
        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            if (role != TypographyRole.Label) theme.SetTypeVariation(role.ToString(), "Label");
            theme.SetFontSize("font_size", role.ToString(), tokens.FontSize(role));
            theme.SetFont("font", role.ToString(), role == TypographyRole.Strong ? strong
                : Variant(UiTokens.Weight(role), UiTokens.Tabular(role), UiTokens.Tracking(role), caps: role == TypographyRole.MetricLabel));
        }
        theme.SetColor("font_color", nameof(TypographyRole.MetricLabel), ToGodot(tokens.Color(ColorRole.TextMuted)));
        theme.SetColor("font_color", nameof(TypographyRole.MetricContext), ToGodot(tokens.Color(ColorRole.TextMuted)));
        theme.SetColor("font_color", "Label", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetTypeVariation("SectionCaps", "Label");
        theme.SetFont("font", "SectionCaps", caps);
        theme.SetFontSize("font_size", "SectionCaps", tokens.FontSize(TypographyRole.SectionTitle));
        // Material surface system (Surfaces): every button and panel below shares one layered material and keeps the
        // previous content margins, so layouts do not move.
        var padX = tokens.Space(SpaceRole.SpaceGroup); var padY = tokens.Space(SpaceRole.SpaceRelated);
        // Every state of a control keeps one frame geometry, and its focus ring follows the same cuts.
        void States(string kind, Func<SurfaceState, StyleBox> style)
        {
            theme.SetStylebox("normal", kind, style(SurfaceState.Normal));
            theme.SetStylebox("hover", kind, style(SurfaceState.Hover));
            theme.SetStylebox("pressed", kind, style(SurfaceState.Pressed));
            theme.SetStylebox("hover_pressed", kind, style(SurfaceState.Pressed));
            theme.SetStylebox("disabled", kind, style(SurfaceState.Disabled));
            if ((style(SurfaceState.Normal) as SurfaceStyle ?? style(SurfaceState.Hover) as SurfaceStyle) is { } shape)
                theme.SetStylebox("focus", kind, Surfaces.Focus(tokens, shape.Spec.Cuts));
        }
        foreach (var kind in new[] { "Button", "OptionButton", "CheckButton", "CheckBox" })
        {
            foreach (var item in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
                theme.SetColor(item, kind, ToGodot(tokens.Color(ColorRole.TextPrimary)));
            theme.SetColor("font_disabled_color", kind, ToGodot(tokens.Color(ColorRole.TextMuted)));
            theme.SetFontSize("font_size", kind, tokens.FontSize(TypographyRole.Label));
            States(kind, state => Surfaces.Secondary(tokens, state, padX, padY));
        }
        // Entities are rows, not buttons: flat until hovered, with surfaces instead of per-item borders.
        theme.SetTypeVariation("Entity", "Button");
        States("Entity", state => state switch
        {
            SurfaceState.Hover => Surfaces.Lift(tokens, padX, padY),
            SurfaceState.Pressed => Surfaces.Sunk(tokens, padX, padY),
            _ => Flat(tokens, null)
        });
        // List rows (news, transfers): level-2 plates; alternate rows a step darker; hover lifts to level 3.
        theme.SetTypeVariation("Row", "Button");
        States("Row", state => Surfaces.Row(tokens, state, false, padX, padY));
        theme.SetTypeVariation("RowAlt", "Button");
        States("RowAlt", state => Surfaces.Row(tokens, state, true, padX, padY));
        // Segmented filters (e.g. transfer news): flat options on a recessed track; the active one is a small brass-edged lift.
        theme.SetTypeVariation("Segment", "Button");
        // Filters are utility controls: the smallest cut, the same in every state and in the selection.
        const int segmentCut = Surfaces.ControlCut - 1;
        States("Segment", state => state == SurfaceState.Hover ? Surfaces.Lift(tokens, 8, 2, segmentCut) : state == SurfaceState.Pressed ? Surfaces.Sunk(tokens, 8, 2, segmentCut) : Flat(tokens, null));
        theme.SetTypeVariation("SegmentSelected", "Button");
        States("SegmentSelected", state => Surfaces.Selected(tokens, state == SurfaceState.Hover, 8, 2, compact: true));
        // Bounded actionable rows inside a region (Portal decisions): the inset tier with a divider edge, so a region reads as
        // raised workspace over inset rows rather than a card inside a card.
        theme.SetTypeVariation("Card", "Button");
        States("Card", state => Surfaces.Card(tokens, state, padX, padY));
        // Navigation rail (approved Portal reference): flat items; the current section carries a restrained gold tint and edge.
        theme.SetTypeVariation("Nav", "Button");
        // Navigation: quiet at rest; hover is an intermediate plate (weaker than a card lift); the selection is level 3.
        // Rail items share the selection's angled frame (all four corners cut) in every state.
        var navCuts = Cuts.All(Surfaces.CardCut);
        States("Nav", state => state switch
        {
            SurfaceState.Hover => SurfaceStyle.Create(Surfaces.Row(tokens, SurfaceState.Normal, false, padX, padY).Spec with { Cuts = navCuts }, padX, padY),
            SurfaceState.Pressed => SurfaceStyle.Create(Surfaces.Sunk(tokens, padX, padY).Spec with { Cuts = navCuts }, padX, padY, Surfaces.PressShift),
            _ => Flat(tokens, null)
        });
        // Current section: a dark surface with a restrained warm tint, a solid leading edge and a faint outline; text stays
        // high-contrast primary. The edge, not the fill, carries the selection.
        theme.SetTypeVariation("NavSelected", "Button");
        States("NavSelected", state => Surfaces.Selected(tokens, state == SurfaceState.Hover, padX + UiTokens.SelectionWidth, padY));
        // Strong secondary action (e.g. "Prepare match" while no plan exists): a dark teal fill with a bright teal edge. It
        // is discoverable next to content without competing with the gold global Continue.
        theme.SetTypeVariation("SecondaryAction", "Button");
        States("SecondaryAction", state => Surfaces.Secondary(tokens, state, padX, padY, ColorRole.StatePositive, cut: Surfaces.CardCut));
        // Secondary workspace route (e.g. "Review match plan"): an outline in the positive accent, never a filled primary.
        theme.SetTypeVariation("AccentOutline", "Button");
        States("AccentOutline", state => Surfaces.Secondary(tokens, state, padX, padY, ColorRole.StatePositive, outline: true, cut: Surfaces.CardCut));
        theme.SetTypeVariation("SelectedEntity", "Button");
        // Leading edge bar keeps selection perceivable without relying on surface color alone.
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
            theme.SetStylebox(state, "SelectedEntity", Selected(tokens, state.StartsWith("hover") ? ColorRole.HoverSurface : ColorRole.SelectedSurface));
        // Its frame is a square selection bar, so its focus ring stays square too (one geometry per control).
        theme.SetStylebox("focus", "SelectedEntity", Focus(tokens));
        // One inverted-neutral treatment marks the workspace decision; it is not a semantic state color.
        theme.SetTypeVariation("PrimaryAction", "Button");
        States("PrimaryAction", state => Surfaces.Primary(tokens, state, padX, padY));
        foreach (var item in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_hover_pressed_color" })
            theme.SetColor(item, "PrimaryAction", ToGodot(tokens.Color(ColorRole.TextOnAction)));
        theme.SetFont("font", "PrimaryAction", strong);
        // Surface tiers as materials (UI polish standard §2): any raised panel is a standard region frame, an inset panel a
        // nested plate, an overlay a premium frame; the base tier stays flat. Content margins are unchanged, so layouts
        // built on these variations (kit structures, the sponsor workspace, the company view) keep their positions.
        foreach (var surface in new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset, ColorRole.SurfaceOverlay })
        {
            theme.SetTypeVariation(surface.ToString(), "PanelContainer");
            theme.SetStylebox("panel", surface.ToString(), surface switch
            {
                ColorRole.SurfaceRaised => Surfaces.Region(tokens, padX, FrameTier.Standard, padY),
                ColorRole.SurfaceInset => Surfaces.Card(tokens, SurfaceState.Normal, padX, padY),
                ColorRole.SurfaceOverlay => Surfaces.Region(tokens, padX, FrameTier.Premium, padY),
                _ => Box(tokens, surface, false)
            });
        }
        // Material panels: level-1 regions, level-2 cards and recessed wells, for any PanelContainer.
        theme.SetTypeVariation("PanelRegion", "PanelContainer"); theme.SetStylebox("panel", "PanelRegion", Surfaces.Region(tokens, padX));
        theme.SetTypeVariation("PanelCard", "PanelContainer"); theme.SetStylebox("panel", "PanelCard", Surfaces.Card(tokens, SurfaceState.Normal, padX, padY));
        theme.SetTypeVariation("PanelWell", "PanelContainer"); theme.SetStylebox("panel", "PanelWell", Surfaces.Well(tokens));
        theme.SetTypeVariation("PanelHeader", "PanelContainer"); theme.SetStylebox("panel", "PanelHeader", Surfaces.Header(tokens, padX, padY));
        foreach (var kind in new[] { "VBoxContainer", "HBoxContainer" })
            theme.SetConstant("separation", kind, tokens.Space(SpaceRole.SpaceRelated));
        foreach (var kind in new[] { "HFlowContainer", "VFlowContainer", "GridContainer" })
        {
            theme.SetConstant("h_separation", kind, tokens.Space(SpaceRole.SpaceRelated));
            theme.SetConstant("v_separation", kind, tokens.Space(SpaceRole.SpaceRelated));
        }
        // Scrollbars: a recessed track and a steel grabber with the tag cut.
        theme.SetStylebox("scroll", "VScrollBar", Surfaces.Tag(ToGodot(tokens.Color(ColorRole.SurfaceInset)).Darkened(.2f), Colors.Transparent, 0, 0));
        theme.SetStylebox("focus", "ScrollContainer", Focus(tokens));
        theme.SetStylebox("grabber", "VScrollBar", Surfaces.Tag(ToGodot(tokens.Color(ColorRole.FrameStroke)), ToGodot(tokens.Color(ColorRole.FrameHighlight)) with { A = .5f }, 0, 0));
        theme.SetStylebox("grabber_highlight", "VScrollBar", Surfaces.Tag(ToGodot(tokens.Color(ColorRole.FrameHighlight)), ToGodot(tokens.Color(ColorRole.TextMuted)), 0, 0));
        theme.SetStylebox("grabber_pressed", "VScrollBar", Surfaces.Tag(ToGodot(tokens.Color(ColorRole.TextMuted)), ToGodot(tokens.Color(ColorRole.TextSecondary)), 0, 0));
        // Dense tables (genre shell S5, polish standard §6): no panel of their own (they sit in a region), a header strip of
        // caption caps, rows defined by tone (the screen paints alternate rows), a brass-edged selected row, no guides.
        theme.SetStylebox("panel", "Tree", new StyleBoxEmpty());
        theme.SetStylebox("focus", "Tree", Focus(tokens));
        theme.SetStylebox("selected", "Tree", Surfaces.RowSelected(tokens, false));
        theme.SetStylebox("selected_focus", "Tree", Surfaces.RowSelected(tokens, true));
        theme.SetStylebox("hovered", "Tree", SurfaceStyle.Create(Surfaces.Row(tokens, SurfaceState.Normal, false, 0, 0).Spec with
            { Top = ToGodot(tokens.Color(ColorRole.HoverSurface)) with { A = .6f }, Bottom = ToGodot(tokens.Color(ColorRole.HoverSurface)) with { A = .45f }, Contact = 0 }, 0, 0));
        theme.SetStylebox("cursor", "Tree", Flat(tokens, null));
        theme.SetStylebox("cursor_unfocused", "Tree", Flat(tokens, null));
        theme.SetStylebox("title_button_normal", "Tree", Surfaces.TableHeader(tokens, 6, 4));
        theme.SetStylebox("title_button_hover", "Tree", Surfaces.TableHeader(tokens, 6, 4, hover: true));
        theme.SetStylebox("title_button_pressed", "Tree", Surfaces.TableHeader(tokens, 6, 4, hover: true));
        foreach (var item in new[] { "font_color", "font_selected_color" })
            theme.SetColor(item, "Tree", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetColor("title_button_color", "Tree", ToGodot(tokens.Color(ColorRole.TextSecondary)));
        theme.SetColor("guide_color", "Tree", ToGodot(tokens.Color(ColorRole.Divider)));
        theme.SetConstant("draw_guides", "Tree", 0);
        theme.SetConstant("v_separation", "Tree", tokens.Space(SpaceRole.SpaceRelated));
        // Cell margins: a right-aligned amount keeps a gap before the next column's text.
        theme.SetConstant("inner_item_margin_left", "Tree", tokens.Space(SpaceRole.SpaceInline));
        theme.SetConstant("inner_item_margin_right", "Tree", tokens.Space(SpaceRole.SpaceGroup));
        // Declared rather than inherited from the engine theme, so table code can measure cells exactly (Table, ChromeCheck).
        theme.SetConstant("h_separation", "Tree", tokens.Space(SpaceRole.SpaceInline));
        theme.SetConstant("item_margin", "Tree", 16);
        theme.SetFont("title_button_font", "Tree", strong);
        // Table cells use tabular figures, so amounts in a column line up by place value.
        theme.SetFont("font", "Tree", Variant(UiTokens.WeightRegular, true));
        theme.SetFontSize("font_size", "Tree", tokens.FontSize(TypographyRole.Label));
        theme.SetFontSize("title_button_font_size", "Tree", tokens.FontSize(TypographyRole.Caption));
        foreach (var kind in new[] { "LineEdit" })
        {
            // Inputs are recessed wells with control cuts; the focus ring follows the same cuts.
            var input = Surfaces.Input(tokens, padX, padY);
            theme.SetStylebox("normal", kind, input);
            theme.SetStylebox("focus", kind, Surfaces.Focus(tokens, input.Spec.Cuts));
            theme.SetStylebox("read_only", kind, Surfaces.Input(tokens, padX, padY, readOnly: true));
            theme.SetColor("font_color", kind, ToGodot(tokens.Color(ColorRole.TextPrimary)));
            theme.SetColor("font_placeholder_color", kind, ToGodot(tokens.Color(ColorRole.TextMuted)));
            theme.SetColor("caret_color", kind, ToGodot(tokens.Color(ColorRole.FocusRing)));
            theme.SetFontSize("font_size", kind, tokens.FontSize(TypographyRole.Label));
        }
        theme.SetStylebox("panel", "PopupMenu", Surfaces.Popup(tokens, 4, 4));
        theme.SetStylebox("hover", "PopupMenu", SurfaceStyle.Create(Surfaces.Lift(tokens, 0, 0).Spec with { Soft = 0, Contact = 0, Frame = default }, 0, 0));
        theme.SetColor("font_color", "PopupMenu", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetColor("font_hover_color", "PopupMenu", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        theme.SetFontSize("font_size", "PopupMenu", tokens.FontSize(TypographyRole.Label));
        theme.SetStylebox("panel", "TooltipPanel", Surfaces.Tag(ToGodot(tokens.Color(ColorRole.SurfaceOverlay)), ToGodot(tokens.Color(ColorRole.Divider)), padX, padY / 2));
        theme.SetColor("font_color", "TooltipLabel", ToGodot(tokens.Color(ColorRole.TextPrimary)));
        return theme;
    }
    /// <summary>True when a focus style draws a visible ring at least <see cref="UiTokens.FocusWidth"/> thick: the square
    /// ring of flat controls or the cut ring that follows a layered frame.</summary>
    public static bool VisibleFocus(StyleBox box) => box is StyleBoxFlat { BorderWidthLeft: >= UiTokens.FocusWidth }
        || box is SurfaceStyle { Spec.RingWidth: >= UiTokens.FocusWidth, Spec.Ring.A: > 0f };
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
    /// <summary>An action button: at least the action width, growing with its label. A label never wraps mid-phrase
    /// (polish standard §7): "Return to company · Esc" reads as one action, not two stacked fragments.</summary>
    public Button Button(string text, Action action)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.All,
            CustomMinimumSize = new Vector2(Tokens.ActionMinimumWidth, Tokens.ControlHeight), AutowrapMode = TextServer.AutowrapMode.Off };
        button.Pressed += action;
        return button;
    }
    public void Tone(Label label, ColorRole role) => label.AddThemeColorOverride("font_color", UiTheme.ToGodot(Tokens.Color(role)));
}
