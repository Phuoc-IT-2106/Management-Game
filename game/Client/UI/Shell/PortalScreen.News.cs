using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Latest news: the world's messages to the company as a feed, not a log. Each story has its subject's mark
/// (resolved by stable ID through the asset catalog, with a deterministic category fallback), a category, a headline,
/// a short summary and its age. On a roomy viewport the newest story leads with a wide art panel.</summary>
public static partial class PortalScreen
{
    private static string KindName(string kind) => kind switch
    {
        "Match" => "Match result", "Meta" => "Meta shift", "Renewal" => "Contract", "Negotiation" => "Commercial", "Offer" => "Commercial",
        "Market" => "Market", _ => kind
    };

    private static Control News(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        var panel = Region(ui, "Portal_News", "news", "Latest news", roomy, out var body, "", Link(ui, "PortalNewsAll", "View all", () => routes.Open(ShellSections.Inbox, "")));
        if (m.News.IsEmpty)
        {
            body.AddChild(Empty(ui, "news", "No recent organization news.", "Results, offers and contract notices arrive here as the season moves."));
            return panel;
        }
        var items = m.News.Take(ShellLayout.NewsItems(roomy)).ToArray();
        for (var i = 0; i < items.Length; i++)
        {
            body.AddChild(roomy && i == 0 ? LeadStory(ui, assets, items[i], m.Time.Day, routes) : Story(ui, assets, items[i], i, m.Time.Day, roomy, routes));
            if (i < items.Length - 1) body.AddChild(Rule(ui));
        }
        return panel;
    }
    /// <summary>Category, unread state and age share one metadata row.</summary>
    private static Control StoryMeta(UiContext ui, PortalNews n, int today, Routes routes)
    {
        var meta = CardButton.Passive(Row(8));
        meta.AddChild(Caps(ui, KindName(n.Category)));
        if (routes.Unread(n.Id)) meta.AddChild(Caps(ui, "New", ColorRole.StatePositive));
        meta.AddChild(Spacer());
        meta.AddChild(Text(ui, Ago(n.Day, today), TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        return meta;
    }
    private static CardButton StoryButton(PortalNews n, int index, Routes routes)
    {
        var item = new CardButton(6, 6) { Name = "News_" + index, ThemeTypeVariation = "Entity", TooltipText = n.Detail.Length > 0 ? n.Headline + ". " + n.Detail : n.Headline };
        item.SetMeta("section", n.Section); item.SetMeta("target_id", n.Id);
        item.Pressed += () => routes.Open(n.Section, n.Id);
        return item;
    }
    private static Control LeadStory(UiContext ui, IdentityAssets assets, PortalNews n, int today, Routes routes)
    {
        var item = StoryButton(n, 0, routes);
        var stack = CardButton.Passive(Stack(8));
        var art = assets.Create(n.ImageAssetId, new(0, ShellLayout.NewsLeadArtHeight), plate: true); art.Name = "NewsImage_0";
        art.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        stack.AddChild(art);
        stack.AddChild(StoryMeta(ui, n, today, routes));
        stack.AddChild(Clamp(Text(ui, n.Headline, TypographyRole.Strong, size: TypographyRole.SectionTitle), 2));
        if (n.Detail.Length > 0) stack.AddChild(Clamp(Text(ui, n.Detail, TypographyRole.Annotation, ColorRole.TextSecondary), 2));
        item.Body.AddChild(stack);
        return item;
    }
    private static Control Story(UiContext ui, IdentityAssets assets, PortalNews n, int index, int today, bool roomy, Routes routes)
    {
        var item = StoryButton(n, index, routes);
        var row = CardButton.Passive(Row(12));
        var thumb = assets.Create(n.ImageAssetId, ShellLayout.NewsThumb(roomy), plate: true); thumb.Name = "NewsImage_" + index;
        thumb.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        row.AddChild(thumb);
        var text = CardButton.Passive(Stack(2)); text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        text.AddChild(StoryMeta(ui, n, today, routes));
        text.AddChild(Clamp(Text(ui, n.Headline, TypographyRole.Strong, size: roomy ? TypographyRole.Label : TypographyRole.Annotation), 2));
        if (roomy && n.Detail.Length > 0) text.AddChild(Clamp(Text(ui, n.Detail, TypographyRole.Annotation, ColorRole.TextSecondary), 1));
        row.AddChild(text);
        row.AddChild(UiIcon.Create(ui, "chevron", 18, ColorRole.TextSecondary));
        item.Body.AddChild(row);
        return item;
    }
}
