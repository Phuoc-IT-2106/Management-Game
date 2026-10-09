using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>World news: the world's messages to the company (results, market, league and commercial notices) as an
/// editorial feed, not a log. Personnel notices live in transfer news. Each story shows its subject's mark (resolved by
/// stable ID through the asset catalog, with a deterministic category fallback; no event photography exists and none is
/// generated), a category, a headline, a summary and its age. The newest story leads.</summary>
public static partial class PortalScreen
{
    private static string KindName(string kind) => kind switch
    {
        "Match" => "Match result", "Meta" => "Meta shift", "Negotiation" => "Commercial", "Offer" => "Commercial",
        "Market" => "Market", "Season" => "League", _ => kind
    };

    private static Control News(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        var panel = Region(ui, "Portal_News", "world", "World news", roomy, out var body, "",
            Link(ui, "PortalNewsAll", "View all", () => routes.Open(ShellSections.Inbox, "")));
        if (m.News.IsEmpty)
        {
            body.AddChild(Empty(ui, "world", "No world news yet.", "Results, offers and league notices arrive here as the season moves."));
            return panel;
        }
        var items = m.News.Take(ShellLayout.NewsItems(roomy)).ToArray();
        // The lead story is an elevated card; secondary stories are quieter row plates.
        var list = Stack(roomy ? 6 : 4); list.Name = "NewsRows"; body.AddChild(list);
        for (var i = 0; i < items.Length; i++)
            list.AddChild(i == 0 ? LeadStory(ui, assets, items[i], m.Time.Day, roomy, routes) : Story(ui, assets, items[i], i, m.Time.Day, roomy, routes));
        return panel;
    }
    /// <summary>Category, unread state and age share one metadata row.</summary>
    private static Control StoryMeta(UiContext ui, PortalNews n, int today, Routes routes, bool category = true)
    {
        var meta = CardButton.Passive(Row(8));
        if (category) meta.AddChild(Caps(ui, KindName(n.Category)));
        if (routes.Unread(n.Id)) meta.AddChild(Caps(ui, "New", ColorRole.StatePositive));
        meta.AddChild(Spacer());
        meta.AddChild(Text(ui, Ago(n.Day, today), TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        return meta;
    }
    private static CardButton StoryButton(PortalNews n, int index, Routes routes)
    {
        var item = new CardButton(8, 6) { Name = "News_" + index, ThemeTypeVariation = index == 0 ? "Card" : index % 2 == 1 ? "Row" : "RowAlt",
            TooltipText = n.Detail.Length > 0 ? n.Headline + ". " + n.Detail : n.Headline };
        item.SetMeta("section", n.Section); item.SetMeta("target_id", n.Id);
        item.Pressed += () => routes.Open(n.Section, n.Id);
        return item;
    }
    /// <summary>The story's art: its subject's mark on a plate, with the category chip laid over the corner as in the reference.</summary>
    private static Control StoryArt(UiContext ui, IdentityAssets assets, PortalNews n, int index, Vector2 size, bool chip)
    {
        var frame = PanelFrame.Mask(ui, new Panel { Name = "NewsArt_" + index, CustomMinimumSize = size, MouseFilter = Control.MouseFilterEnum.Ignore, ClipContents = true }, Surfaces.ControlCut);
        var art = assets.Create(n.ImageAssetId, size, plate: true); art.Name = "NewsImage_" + index;
        frame.AddChild(art); art.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var edge = PanelFrame.Overlay(ui); frame.AddChild(edge); edge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        if (chip)
        {
            var label = Chip(ui, KindName(n.Category).ToUpper(Invariant), ColorRole.TextPrimary);
            frame.AddChild(label);
            label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft, Control.LayoutPresetMode.KeepSize, 8);
            label.GrowVertical = Control.GrowDirection.Begin;
        }
        return frame;
    }
    private static Control LeadStory(UiContext ui, IdentityAssets assets, PortalNews n, int today, bool roomy, Routes routes)
    {
        var item = StoryButton(n, 0, routes); item.Name = "News_0";
        var art = ShellLayout.LeadArt(roomy);
        if (roomy)
        {
            // Editorial lead: a wash in the subject's colour, its mark on the right, the story over the lower left. The
            // mark is the subject's identity by stable ID; no event image exists, so none is shown or generated. The frame
            // is at least the art height and grows with the story, so long text is never cut by the frame.
            var frame = PanelFrame.Mask(ui, new PanelContainer { Name = "NewsArt_0", CustomMinimumSize = art, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore }, Surfaces.CardCut);
            var surface = Tone(ui, ColorRole.SurfaceInset); var tint = UiTheme.ToGodot(assets.TeamColor(n.ImageAssetId));
            var wash = Backdrop.Create(surface.Lerp(tint, .15f), surface.Lerp(tint, .55f), Tone(ui, ColorRole.SurfaceBase) with { A = .75f },
                new Backdrop.Glow(new(.8f, .42f), 1.1f, tint.Lightened(.3f) with { A = .4f }));
            frame.AddChild(wash); wash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var height = art.Y * .74f;
            var markSize = n.ImageAssetId.StartsWith("crest:", StringComparison.Ordinal) ? new Vector2(height * 100f / 120, height) : new Vector2(height, height);
            var mark = assets.Create(n.ImageAssetId, markSize); mark.Name = "NewsImage_0";
            var markLayer = new Control { Name = "NewsMarkLayer", MouseFilter = Control.MouseFilterEnum.Ignore };
            frame.AddChild(markLayer); markLayer.AddChild(mark);
            mark.AnchorLeft = mark.AnchorRight = .8f; mark.AnchorTop = mark.AnchorBottom = .46f;
            mark.OffsetLeft = -markSize.X / 2; mark.OffsetRight = markSize.X / 2; mark.OffsetTop = -markSize.Y / 2; mark.OffsetBottom = markSize.Y / 2;
            var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            foreach (var (edge, value) in new[] { ("left", 18), ("right", 18), ("top", 12), ("bottom", 12) }) margin.AddThemeConstantOverride("margin_" + edge, value);
            frame.AddChild(margin); margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var split = CardButton.Passive(Row(0)); margin.AddChild(split);
            var words = CardButton.Passive(Stack(6)); words.Alignment = BoxContainer.AlignmentMode.End; words.SizeFlagsStretchRatio = .62f;
            words.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            words.AddChild(Chip(ui, KindName(n.Category).ToUpper(Invariant), ColorRole.TextPrimary));
            words.AddChild(Clamp(Text(ui, n.Headline, TypographyRole.Strong, size: TypographyRole.CompanyIdentity), 2));
            if (n.Detail.Length > 0) words.AddChild(Clamp(Text(ui, n.Detail, TypographyRole.Body, ColorRole.TextSecondary, size: TypographyRole.Annotation), 2));
            words.AddChild(StoryMeta(ui, n, today, routes, category: false));
            split.AddChild(words);
            var room = Spacer(); room.SizeFlagsStretchRatio = .38f; split.AddChild(room);
            var rim = PanelFrame.Overlay(ui, Surfaces.CardCut); frame.AddChild(rim); rim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            item.Body.AddChild(frame);
            return item;
        }
        var row = CardButton.Passive(Row(12));
        var thumb = StoryArt(ui, assets, n, 0, art, chip: false); thumb.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        row.AddChild(thumb);
        var text = CardButton.Passive(Stack(2)); text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        text.AddChild(StoryMeta(ui, n, today, routes));
        text.AddChild(Clamp(Text(ui, n.Headline, TypographyRole.Strong, size: TypographyRole.Label), 2));
        if (n.Detail.Length > 0) text.AddChild(Clamp(Text(ui, n.Detail, TypographyRole.Annotation, ColorRole.TextSecondary), 1));
        row.AddChild(text);
        item.Body.AddChild(row);
        return item;
    }
    private static Control Story(UiContext ui, IdentityAssets assets, PortalNews n, int index, int today, bool roomy, Routes routes)
    {
        var item = StoryButton(n, index, routes);
        var row = CardButton.Passive(Row(12));
        var thumb = StoryArt(ui, assets, n, index, ShellLayout.NewsThumb(roomy), chip: false); thumb.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        row.AddChild(thumb);
        var text = CardButton.Passive(Stack(2)); text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        text.AddChild(StoryMeta(ui, n, today, routes));
        text.AddChild(Clamp(Text(ui, n.Headline, TypographyRole.Strong, size: roomy ? TypographyRole.Label : TypographyRole.Annotation), roomy ? 2 : 1));
        if (roomy && n.Detail.Length > 0) text.AddChild(Clamp(Text(ui, n.Detail, TypographyRole.Annotation, ColorRole.TextSecondary), 1));
        row.AddChild(text);
        item.Body.AddChild(row);
        return item;
    }
}
