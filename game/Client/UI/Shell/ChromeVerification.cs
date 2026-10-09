using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Native checks of the structural chrome (UI polish standard §12), for the Portal and every section screen:
/// silhouettes are cut by tier, header plates sit flush in the wall, no text or art crosses a chamfer, header text clears
/// the tab's slant and brass stripe, every control keeps one geometry across its states and focus ring, tables keep rows
/// by tone, and click targets stay usable.</summary>
public partial class GameShell
{
    /// <summary>The frame a control draws itself with, when it is a layered surface (panels and buttons only).</summary>
    private static SurfaceStyle? FrameOf(Control control) => control switch
    {
        BaseButton button => button.GetThemeStylebox("normal") as SurfaceStyle,
        PanelContainer or Panel => control.GetThemeStylebox("panel") as SurfaceStyle,
        _ => null
    };

    /// <summary>Where a label's glyphs actually are: single-line text spans its string width by alignment, not its rectangle.</summary>
    private static Rect2 Ink(Control leaf)
    {
        var rect = leaf.GetGlobalRect();
        if (leaf is not Label label || label.AutowrapMode != TextServer.AutowrapMode.Off || label.Text.Contains('\n')) return rect;
        var size = label.GetThemeFontSize("font_size");
        var width = Math.Min(rect.Size.X, label.GetThemeFont("font").GetStringSize(label.Text, HorizontalAlignment.Left, -1, size).X);
        var x = label.HorizontalAlignment switch
        {
            HorizontalAlignment.Right => rect.End.X - width, HorizontalAlignment.Center => rect.Position.X + (rect.Size.X - width) / 2, _ => rect.Position.X
        };
        return new Rect2(x, rect.Position.Y, width, rect.Size.Y);
    }

    private static bool Inside(Rect2 frame, Cuts cuts, Rect2 content) => FrameGeometry.Contains(frame.Position.X, frame.Position.Y, frame.Size.X, frame.Size.Y, cuts,
        content.Position.X, content.Position.Y, content.End.X, content.End.Y);

    /// <summary>Portal chrome: the Portal's regions carry the tiers ShellLayout assigns, the shared rules hold, and the rail
    /// shows its identity plate and angled selection.</summary>
    private void ChromeCheck(string label)
    {
        foreach (var name in PortalRegions)
            Check(Descendants(content).OfType<PanelContainer>().First(c => c.Name == name).GetMeta("frame_tier").AsString() == ShellLayout.RegionTier(name).ToString(),
                $"{name} carries its Portal tier, {ShellLayout.RegionTier(name)} ({label})");
        RegionChrome(content, label);
        // Rail: the identity plate leads into navigation; the current item is the angled brass frame, its art set straight on
        // the selection (RailCheck: no housing or backdrop behind rail art).
        var current = navButtons[section];
        Check(FrameOf(Find<PanelContainer>("RailIdentityPlate")) is { } identity && identity.Spec.Cuts.Max > 0, $"the rail's identity sits on a cut header plate ({label})");
        Check(FrameOf(current) is { } selected && selected.Spec.Cuts == Cuts.All(Surfaces.CardCut) && selected.Spec.Wall > 0 && selected.Spec.AccentEdge == AccentEdge.Left,
            $"the current rail item is an angled brass frame with a leading bar ({label})");
    }

    /// <summary>Section screens follow the same standard: framed regions, the shared rules, and tables with rows by tone
    /// inside a region.</summary>
    private void SectionChromeCheck(string id)
    {
        if (id == ShellSections.Portal) return;
        var regions = Descendants(content).OfType<PanelContainer>().Count(p => p.HasMeta("frame_tier") && p.IsVisibleInTree());
        Check(regions > 0, $"{id}: content sits in framed regions with header plates");
        RegionChrome(content, id);
        foreach (var table in Descendants(content).OfType<Tree>().Where(t => t.IsVisibleInTree()))
        {
            var inRegion = false;
            for (var node = table.GetParent(); node is not null && node != content; node = node.GetParent()) inRegion |= node is PanelContainer p && p.HasMeta("frame_tier");
            Check(inRegion && table.GetThemeStylebox("selected") is SurfaceStyle { Spec.AccentEdge: AccentEdge.Left } && table.GetThemeConstant("draw_guides") == 0,
                $"{id}: table {table.Name} sits in a region with rows by tone and the brass row selection");
            // A cell the table trims keeps its full text as a tooltip.
            var cells = (table.GetRoot()?.GetChildren() ?? []).SelectMany(item => Enumerable.Range(0, table.Columns).Select(c => (item, c))).ToArray();
            Check(cells.All(x => x.item.GetTooltipText(x.c) == x.item.GetText(x.c)), $"{id}: every cell of table {table.Name} keeps its full text as a tooltip");
            // Amount and day columns are never trimmed: a cut figure ("$158." for $158.60, "Day 1" for Day 12) reads as another value.
            if (table.HasMeta("whole_columns"))
            {
                var font = table.GetThemeFont("font"); var size = table.GetThemeFontSize("font_size");
                Check(new[] { "inner_item_margin_left", "inner_item_margin_right", "h_separation", "item_margin" }.All(k => table.GetThemeConstant(k) == ui.Theme.GetConstant(k, "Tree")),
                    $"{id}: table {table.Name} measures cells with the spacing it draws them with");
                float Needed(TreeItem item, int c) => font.GetStringSize(item.GetText(c), HorizontalAlignment.Left, -1, size).X + CellPadding(c);
                var wholeColumns = table.GetMeta("whole_columns").AsGodotArray<int>().ToArray();
                var trimmed = cells.Where(x => wholeColumns.Contains(x.c))
                    .FirstOrDefault(x => x.item.GetTextOverrunBehavior(x.c) != TextServer.OverrunBehavior.NoTrimming || Needed(x.item, x.c) > table.GetColumnWidth(x.c) + .5f);
                Check(trimmed.item is null, $"{id}: every amount and day in table {table.Name} shows whole{(trimmed.item is null ? "" : $": {trimmed.item.GetText(trimmed.c)} needs {Needed(trimmed.item, trimmed.c):0.0} px, column {table.GetColumnWidth(trimmed.c)} px")}");
            }
            // S5: every row is visible (no inner scroll), and the table ends with its last row, not a band of empty space.
            var rows = table.GetRoot()?.GetChildren() ?? [];
            if (rows.Count > 0)
            {
                var last = table.GetItemAreaRect(rows[^1]);
                Check(last.End.Y <= table.Size.Y + 1 && table.Size.Y - last.End.Y <= 12,
                    $"{id}: table {table.Name} shows every row and ends with its last row ({table.Size.Y - last.End.Y:0} px spare)");
            }
        }
    }

    /// <summary>The rules every framed screen shares (UI polish standard §12).</summary>
    private void RegionChrome(Control root, string label)
    {
        var regions = Descendants(root).OfType<PanelContainer>().Where(p => p.HasMeta("frame_tier") && p.IsVisibleInTree()).ToArray();
        Check(regions.Count(r => r.GetMeta("frame_tier").AsString() == nameof(FrameTier.Premium)) <= 2, $"at most two premium regions per screen ({label})");
        foreach (var region in regions)
        {
            var name = region.Name.ToString();
            var tier = Enum.Parse<FrameTier>(region.GetMeta("frame_tier").AsString());
            var frame = FrameOf(region);
            Check(frame is { Spec.Wall: UiTokens.EdgeWall } && frame.Spec.Cuts == Surfaces.RegionCuts(tier) && frame.Spec.BrassCorners == Surfaces.RegionBrass(tier)
                && new[] { 0, 1, 2, 3 }.All(i => frame.Spec.Cuts[i] > 0), $"{name} is a {tier} frame cut at all four corners with its tier's wall and brass ({label})");
            var plate = Descendants(region).OfType<HeaderPlate>().First();
            var plateStyle = FrameOf(plate)!;
            var inner = region.GetGlobalRect().Grow(-UiTokens.EdgeWall); var face = plate.GetGlobalRect();
            Check(Math.Abs(face.Position.X - inner.Position.X) <= .5f && Math.Abs(face.End.X - inner.End.X) <= .5f && Math.Abs(face.Position.Y - inner.Position.Y) <= .5f,
                $"{name}: the header plate sits flush inside the frame wall, no gap or overlap ({label})");
            Check(Math.Abs(plateStyle.Spec.Cuts.TopLeft - Surfaces.RegionInnerCuts(tier).TopLeft) < .01f && Math.Abs(plateStyle.Spec.Cuts.TopRight - Surfaces.RegionInnerCuts(tier).TopRight) < .01f
                && plateStyle.Spec.Cuts.BottomLeft > 0 && plateStyle.Spec.Cuts.BottomRight > 0, $"{name}: header plate follows the wall's inner chamfer and narrows at its lower ends ({label})");
            // The title's glyphs end before the tab's slant at the baseline; the next text starts after the brass stripe.
            var title = Descendants(plate).OfType<Label>().First(l => l.ThemeTypeVariation == "SectionCaps");
            var font = title.GetThemeFont("font"); var size = title.GetThemeFontSize("font_size");
            var titleRect = title.GetGlobalRect();
            var baseline = titleRect.Position.Y + (titleRect.Size.Y - font.GetHeight(size)) / 2 + font.GetAscent(size) - face.Position.Y;
            var inkEnd = Ink(title).End.X - face.Position.X;
            Check(plate.SlantAt(baseline) >= inkEnd + 1, $"{name}: the tab's slant clears the title ({label}, {plate.SlantAt(baseline) - inkEnd:0.0} px)");
            var row = title.GetParent<HBoxContainer>();
            var next = row.GetChildren().OfType<Control>().SkipWhile(c => c.Name != "HeaderSeam").Skip(1)
                .FirstOrDefault(c => c.IsVisibleInTree() && c is Label or BaseButton or Container);
            // The first text or control shown after the title (context omitted for lack of room shows nothing to check).
            var shown = next is null ? [] : Descendants(next).OfType<Control>().Append(next).Where(c => c is Label or BaseButton && c.IsVisibleInTree()).Select(Ink).ToArray();
            if (shown.Length > 0)
            {
                var nextRect = shown.MinBy(r => r.Position.X);
                Check(nextRect.Position.X - face.Position.X >= plate.StripeRight(nextRect.Position.Y - face.Position.Y) + 1,
                    $"{name}: header text after the title clears the slant and brass stripe ({label})");
            }
            foreach (var action in Descendants(plate).OfType<BaseButton>().Where(b => b.IsVisibleInTree()))
                Check(Math.Abs(action.GetGlobalRect().GetCenter().Y - titleRect.GetCenter().Y) <= 2 && Inside(face, plateStyle.Spec.Cuts, action.GetGlobalRect()),
                    $"{name}: the header route is aligned with the title and inside the plate ({label})");
        }
        // No text or art crosses a chamfer of any frame around it. Art inside a masking frame is clipped to the cuts by design.
        var crossing = new List<string>();
        foreach (var leaf in Descendants(root).OfType<Control>().Where(c => c.IsVisibleInTree() && c.Size.X > 0 && c is Label or TextureRect or AssetMark or UiIcon))
        {
            // Only the visible part counts: scrolls and clipping containers cut what lies outside them.
            var ink = Ink(leaf);
            for (var node = leaf.GetParent(); node is not null && node != root; node = node.GetParent())
            {
                if (node is Control clip && (clip is ScrollContainer || clip.ClipContents))
                {
                    ink = ink.Intersection(clip.GetGlobalRect());
                    if (ink.Size.X <= 0 || ink.Size.Y <= 0) break;
                }
                if (node is not Control ancestor || FrameOf(ancestor) is not { } style || style.Spec.Cuts.Max <= 0) continue;
                if (leaf is not Label && ancestor.ClipChildren != CanvasItem.ClipChildrenMode.Disabled) continue;
                if (!Inside(ancestor.GetGlobalRect(), style.Spec.Cuts, ink)) crossing.Add($"{leaf.Name} in {ancestor.Name}");
            }
        }
        Check(crossing.Count == 0, $"no text or art crosses a corner cut ({label}{(crossing.Count == 0 ? "" : ": " + string.Join(", ", crossing.Take(6)))})");
        // One corner language: no rounded flat box on any panel, label or button of the screen.
        var rounded = Descendants(root).OfType<Control>().Where(c => c.IsVisibleInTree())
            .FirstOrDefault(c => (c is PanelContainer or Panel ? c.GetThemeStylebox("panel") : c is Button or Label ? c.GetThemeStylebox("normal") : null)
                is StyleBoxFlat { CornerRadiusTopLeft: > 0 } or StyleBoxFlat { CornerRadiusBottomRight: > 0 });
        Check(rounded is null, $"no rounded boxes: frames use 45° cuts, flat boxes stay square ({label}{(rounded is null ? "" : ": " + rounded.Name)})");
        // Action labels stay on one line and fit their button (rows, choice lists and card buttons lay out their own text).
        var wrapped = Descendants(root).OfType<Button>().Where(b => b.IsVisibleInTree() && b.Text.Length > 0 && b is not EntityLabel and not OptionButton and not CardButton)
            .FirstOrDefault(b => b.AutowrapMode != TextServer.AutowrapMode.Off
                || b.GetThemeFont("font").GetStringSize(b.Text, HorizontalAlignment.Left, -1, b.GetThemeFontSize("font_size")).X > b.Size.X);
        Check(wrapped is null, $"action labels stay on one line and fit their button ({label}{(wrapped is null ? "" : ": " + wrapped.Text)})");
        // Click targets stay at least 24 × 24 px (WCAG 2.2 target size), whatever their frame's cuts.
        var small = Descendants(root).Concat(Descendants(topBar)).OfType<BaseButton>().Where(b => b.IsVisibleInTree())
            .FirstOrDefault(b => b.Size.X < UiTokens.MinimumTarget || b.Size.Y < UiTokens.MinimumTarget);
        Check(small is null, $"every control on screen and in the top bar keeps a 24 × 24 px click target ({label}{(small is null ? "" : ": " + small.Name + " " + small.Size)})");
        CurrencyTextCheck(root, label);
    }

    private static readonly System.Text.RegularExpressions.Regex RetiredUnit = new(@"\bCU\b", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    /// <summary>Money reads in US dollars everywhere: no visible label, button, table cell or title, input affix or tooltip
    /// under <paramref name="root"/> names the retired abstract unit.</summary>
    private void CurrencyTextCheck(Node root, string label)
    {
        static IEnumerable<string> Texts(Node node) => node switch
        {
            Tree t => Enumerable.Range(0, t.Columns).Select(t.GetColumnTitle)
                .Concat((t.GetRoot()?.GetChildren() ?? []).SelectMany(i => Enumerable.Range(0, t.Columns).SelectMany(c => new[] { i.GetText(c), i.GetTooltipText(c) }))),
            SpinBox s => [s.Prefix, s.Suffix, s.TooltipText],
            Label l => [l.Text, l.TooltipText],
            Button b => [b.Text, b.TooltipText],
            Control c => [c.TooltipText],
            _ => []
        };
        var leak = Descendants(root).Prepend(root).Where(n => n is not CanvasItem item || item.IsVisibleInTree()).SelectMany(Texts).FirstOrDefault(t => RetiredUnit.IsMatch(t));
        Check(leak is null, $"money reads in dollars: no player-facing text names the retired CU unit ({label}{(leak is null ? "" : ": " + leak)})");
    }

    /// <summary>Theme-level chrome rules, checked once per viewport: one geometry per control across states and focus, the
    /// functional hierarchy of cuts, walls and brass, graphite shadow edges instead of opaque black outlines, and the shared
    /// materials of panels, inputs and tables.</summary>
    private void ChromeStateChecks()
    {
        var states = new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" };
        foreach (var kind in new[] { "PrimaryAction", "Button", "OptionButton", "SecondaryAction", "AccentOutline", "Card", "Row", "RowAlt", "Nav", "NavSelected", "Segment", "SegmentSelected", "Entity" })
        {
            var shapes = states.Select(s => ui.Theme.GetStylebox(s, kind)).OfType<SurfaceStyle>().Select(b => b.Spec.Cuts).Distinct().ToArray();
            Check(shapes.Length == 1 && ui.Theme.GetStylebox("focus", kind) is SurfaceStyle { Spec.Ring.A: > 0f },
                $"{kind}: every state and the focus ring keep one frame geometry");
        }
        Check(ui.Theme.GetStylebox("normal", "LineEdit") is SurfaceStyle input && ui.Theme.GetStylebox("focus", "LineEdit") is SurfaceStyle { Spec.Ring.A: > 0f } ring
            && ring.Spec.Cuts == input.Spec.Cuts && input.Spec.Cuts.Max > 0, "inputs are cut wells whose focus ring follows the same cuts");
        Check(ui.Theme.GetStylebox("panel", nameof(ColorRole.SurfaceRaised)) is SurfaceStyle { Spec.Wall: UiTokens.EdgeWall } raised && raised.Spec.Cuts == Surfaces.RegionCuts(FrameTier.Standard)
            && ui.Theme.GetStylebox("panel", nameof(ColorRole.SurfaceInset)) is SurfaceStyle inset && inset.Spec.Cuts == Cuts.Diagonal(Surfaces.CardCut)
            && ui.Theme.GetStylebox("panel", nameof(ColorRole.SurfaceOverlay)) is SurfaceStyle overlay && overlay.Spec.Cuts == Surfaces.RegionCuts(FrameTier.Premium),
            "panel tiers are materials: raised a standard region, inset a nested plate, overlay a premium region");
        Check(ui.Theme.GetStylebox("selected", "Tree") is SurfaceStyle { Spec.AccentEdge: AccentEdge.Left } && ui.Theme.GetStylebox("title_button_normal", "Tree") is SurfaceStyle
            && ui.Theme.GetConstant("draw_guides", "Tree") == 0, "tables: a header strip, rows by tone and a brass-edged selected row, no guides");
        var t = ui.Tokens;
        var premium = Surfaces.RegionCuts(FrameTier.Premium).Max; var standard = Surfaces.RegionCuts(FrameTier.Standard).Max;
        Check(premium > standard && standard > Surfaces.CardCut && Surfaces.CardCut > Surfaces.ControlCut,
            "cut size follows importance: premium region > standard region > nested plate > control");
        Check(Surfaces.RegionBrass(FrameTier.Premium) == (SurfaceSpec.TopLeft | SurfaceSpec.BottomRight) && Surfaces.RegionBrass(FrameTier.Standard) == SurfaceSpec.TopLeft
            && Surfaces.RegionBrass(FrameTier.Utility) == 0 && Surfaces.Card(t, SurfaceState.Normal).Spec.BrassCorners == 0 && Surfaces.Row(t, SurfaceState.Normal, false, 8, 4).Spec.BrassCorners == 0,
            "brass marks junctions by tier: premium two inserts, standard one, nested plates and rows none");
        var go = (SurfaceStyle)ui.Theme.GetStylebox("normal", "PrimaryAction"); var verb = Surfaces.Primary(t, SurfaceState.Normal, 10, 1, local: true);
        var secondary = (SurfaceStyle)ui.Theme.GetStylebox("normal", "Button");
        Check(go.Spec.Wall > verb.Spec.Wall && go.Spec.Cuts.Max > verb.Spec.Cuts.Max && go.Spec.Soft > 0 && verb.Spec.Soft == 0 && go.Spec.Cuts.Max > secondary.Spec.Cuts.Max,
            "Continue carries the richest control geometry; decision verbs and secondary buttons stay restrained");
        var frames = new[] { Surfaces.Region(t, 3, FrameTier.Premium), Surfaces.Region(t, 3), Surfaces.Header(t, 8, 4), Surfaces.Card(t, SurfaceState.Normal),
            Surfaces.Row(t, SurfaceState.Normal, false, 8, 4), Surfaces.Well(t), Surfaces.Lift(t, 8, 4), Surfaces.Secondary(t, SurfaceState.Normal, 8, 4), go,
            Surfaces.Input(t, 8, 4), Surfaces.Popup(t, 4, 4), Surfaces.RowSelected(t, true) };
        Check(frames.All(f => f.Spec.Frame.A <= .6f && f.Spec.Groove.A <= .6f), "frames use graphite shadow edges (alpha ≤ 0.6), never an opaque black outline");
        Check(Surfaces.Row(t, SurfaceState.Normal, true, 8, 4).Spec is { Rim: RimMode.Edges, Frame.A: 0f }, "list rows are defined by tone and edge light, not a full outline");
    }
}
