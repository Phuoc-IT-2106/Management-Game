using Godot;
using ManagementGame.Application;
using ManagementGame.Formatting;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Transfer news: a compact editorial feed of recorded personnel moves (<see cref="TransferStory"/>), not a market.
/// Every story is the same article on the region's own surface, divided by engraved rules: who moved (the person's
/// generic art, or a roundup's count), the certainty, kind of move and age, the headline, then the journey (origin →
/// destination, each an organization's crest and name, the free-agent status, or an explicit unknown) and, where there is
/// room, the summary. No story is enlarged for being first, and nothing draws a box behind a person. Never a fee, wage,
/// ability or market availability, which belong to the Squad and Staff markets.</summary>
public static partial class PortalScreen
{
    /// <summary>A story's certainty label. Only Confirmed exists: the rules have no public reporting of interest or talks.</summary>
    public static (string Text, ColorRole Tone) StatusLabel(StoryStatus status) => status switch
    {
        StoryStatus.Confirmed => (PresentationText.InformationMarker(InformationState.Known) + " CONFIRMED", ColorRole.InformationKnown),
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
    /// <summary>The kind of move, naming whether it concerns a player or staff: a move between two organizations is a
    /// transfer; joining from the open market a signing or an appointment; leaving to it a release or a departure.</summary>
    public static string KindLabel(TransferStory s)
    {
        var roundup = s.PersonName.Length == 0;
        var kind = s.From.Standing == Standing.Organization && s.To.Standing == Standing.Organization ? "transfer" : s.Kind switch
        {
            MoveKind.Signing => "signing", MoveKind.Appointment => "appointment", MoveKind.Interim => "appointment",
            MoveKind.Release => "release", _ => "departure"
        };
        return (s.Kind == MoveKind.Interim ? "Interim" : s.Coach ? "Staff" : "Player") + " " + kind + (roundup ? "s" : "");
    }
    /// <summary>Plain-text journey for tooltips: "Origin → Destination", unknown and free-agent sides in words.</summary>
    public static string JourneyText(TransferStory s) => $"{SideText(s.From, true, full: true)} → {SideText(s.To, false, full: true)}";
    /// <summary>A side in words. An unknown organization carries the Unknown marker; on screen its place before or after
    /// the arrow says which team it was, and the tooltip says it in full.</summary>
    public static string SideText(JourneySide side, bool origin, bool full = false) => side.Standing switch
    {
        Standing.Organization => side.Name, Standing.FreeAgent => FreeAgentText,
        _ => PresentationText.InformationMarker(InformationState.Unknown) + (!full ? " Team unknown" : origin ? " Previous team unknown" : " New team unknown")
    };
    public const string FreeAgentText = "Free agent";

    private static Control Transfers(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        // No header route: no screen holds every transfer story yet, and the player market is reached from Squad and Staff.
        var panel = Region(ui, "Portal_Transfers", "transfer", "Transfer news", roomy, out var body);
        if (m.Transfers.IsEmpty)
        {
            body.AddChild(Empty(ui, "transfer", "No major transfer developments to report.", "Confirmed signings, appointments and departures appear here."));
            return panel;
        }
        var list = Stack(2); list.Name = "TransferStories"; body.AddChild(list);
        var stories = m.Transfers.Take(ShellLayout.TransferItems(roomy)).ToArray();
        for (var i = 0; i < stories.Length; i++)
        {
            if (i > 0) list.AddChild(Rule(ui));
            list.AddChild(TransferArticle(ui, assets, stories[i], i, m.Time.Day, roomy, routes));
        }
        return panel;
    }

    /// <summary>One story, the same for every position in the feed: the identity column, then the meta row, headline,
    /// journey and (roomy) summary. A flat row on the region's surface that lifts on hover.</summary>
    private static Control TransferArticle(UiContext ui, IdentityAssets assets, TransferStory s, int index, int today, bool roomy, Routes routes)
    {
        var item = new CardButton(roomy ? 8 : 6, 4) { Name = "Transfer_" + index, ThemeTypeVariation = "Entity",
            TooltipText = $"{s.Headline}. {JourneyText(s)}. {s.Summary}" };
        item.SetMeta("story_id", s.Id); item.SetMeta("section", s.Section); item.SetMeta("target_id", s.TargetId);
        item.SetMeta("kind", s.Kind.ToString()); item.SetMeta("status", s.Status.ToString());
        item.Pressed += () => routes.Open(s.Section, s.TargetId);
        var row = CardButton.Passive(Row(roomy ? 14 : 10));
        row.AddChild(StoryIdentity(ui, assets, s, index, roomy));
        var text = CardButton.Passive(Stack(2)); text.Name = "TransferText"; text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        text.AddChild(TransferMeta(ui, s, today));
        // The headline leads with the person's whole name, so its bound only ever shortens an organization's name (shown whole
        // in the journey below); the full text is in the tooltip.
        var headline = Clamp(Text(ui, s.Headline, TypographyRole.Strong, size: roomy ? TypographyRole.Label : TypographyRole.Annotation), 2);
        headline.Name = "StoryHeadline"; headline.TooltipText = s.Headline;
        headline.Resized += () => FitHeadline(headline, s.PersonName);
        text.AddChild(headline);
        text.AddChild(Journey(ui, assets, s, roomy));
        if (roomy)
        {
            var summary = Clamp(Text(ui, s.Summary, TypographyRole.Annotation, ColorRole.TextSecondary, size: TypographyRole.Annotation), 1); summary.Name = "StorySummary";
            text.AddChild(summary);
        }
        row.AddChild(text);
        item.Body.AddChild(row);
        return item;
    }

    /// <summary>A headline takes one line, so every story keeps the same height; two only when the person's whole name
    /// needs them, because a name is never cut.</summary>
    private static void FitHeadline(Label headline, string name)
    {
        if (headline.Size.X <= 0) return;
        var font = headline.GetThemeFont("font"); var size = headline.GetThemeFontSize("font_size");
        var lines = name.Length == 0 ? 1 : (int)Math.Ceiling(font.GetMultilineStringSize(name, HorizontalAlignment.Left, headline.Size.X, size).Y / font.GetHeight(size) - .05f);
        headline.MaxLinesVisible = Math.Clamp(lines, 1, 2);
    }

    /// <summary>Certainty, kind of move and age share one metadata row.</summary>
    private static Control TransferMeta(UiContext ui, TransferStory s, int today)
    {
        var meta = CardButton.Passive(Row(8));
        var (status, tone) = StatusLabel(s.Status);
        var chip = Chip(ui, status, tone); chip.Name = "StoryStatus"; meta.AddChild(chip);
        var kind = Caps(ui, KindLabel(s), ColorRole.TextMuted); kind.Name = "StoryKind"; kind.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; meta.AddChild(kind);
        meta.AddChild(Spacer());
        var age = Text(ui, Ago(s.Day, today), TypographyRole.Caption, ColorRole.TextMuted, wrap: false); age.Name = "StoryAge"; age.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        meta.AddChild(age);
        return meta;
    }

    /// <summary>Who the story is about, in a fixed column: the person's approved generic art (player or staff by their ID),
    /// drawn bare with no plate or frame; or, for a roundup, the number of players who moved.</summary>
    private static Control StoryIdentity(UiContext ui, IdentityAssets assets, TransferStory s, int index, bool roomy)
    {
        var size = ShellLayout.StoryPortrait(roomy);
        if (s.PortraitAssetId.Length > 0)
        {
            var mark = assets.Create(s.PortraitAssetId, new(size, size)); mark.Name = "TransferPortrait_" + index;
            mark.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            return mark;
        }
        // The count fills the same square column as a portrait, so a roundup lines up with the stories around it.
        var count = CardButton.Passive(Stack(0)); count.Name = "TransferCount_" + index; count.Alignment = BoxContainer.AlignmentMode.Center;
        count.CustomMinimumSize = new(size, size); count.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin; count.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var figure = Text(ui, Figures.Count(s.Records.Length), TypographyRole.Data, ColorRole.TextPrimary, wrap: false); figure.Name = "TransferCountValue";
        var unit = Text(ui, "players", TypographyRole.Caption, ColorRole.TextMuted, wrap: false);
        foreach (var label in new[] { figure, unit }) { label.HorizontalAlignment = HorizontalAlignment.Center; label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; count.AddChild(label); }
        return count;
    }

    /// <summary>The journey: origin, an arrow, destination, always in that order. The two sides take equal shares of the
    /// row (on a roomy Portal, a third of the spare width each, so the arrow stays near the names), so arrows line up from
    /// story to story and a long name is shortened with its full text in the tooltip.</summary>
    private static Control Journey(UiContext ui, IdentityAssets assets, TransferStory s, bool roomy)
    {
        var journey = CardButton.Passive(Row(roomy ? 10 : 8)); journey.Name = "TransferJourney";
        journey.AddChild(JourneySideView(ui, assets, s.From, origin: true, roomy));
        var arrow = JourneyArrow.Create(ui, ShellLayout.JourneyArrow(roomy), 12); arrow.Name = "JourneyArrow"; journey.AddChild(arrow);
        journey.AddChild(JourneySideView(ui, assets, s.To, origin: false, roomy));
        if (roomy) journey.AddChild(Spacer());
        return journey;
    }
    /// <summary>One side of the journey. An organization is its crest and name (the destination in the stronger tone); the
    /// open market is the words "Free agent", a status with no crest; an organization the company cannot name is marked
    /// unknown, never replaced by another.</summary>
    private static Control JourneySideView(UiContext ui, IdentityAssets assets, JourneySide side, bool origin, bool roomy)
    {
        var crestHeight = ShellLayout.JourneyCrest(roomy);
        var box = CardButton.Passive(Row(roomy ? 8 : 6)); box.Name = origin ? "JourneyFrom" : "JourneyTo";
        box.CustomMinimumSize = new(0, crestHeight); box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        box.SetMeta("standing", side.Standing.ToString()); box.SetMeta("organization_id", side.OrganizationId);
        if (side.Standing == Standing.Organization)
        {
            var crest = assets.Create(side.CrestAssetId, new(MathF.Round(crestHeight * 100f / 120), crestHeight)); crest.Name = "JourneyCrest";
            box.AddChild(crest);
        }
        var (tone, role) = side.Standing switch
        {
            Standing.Organization => origin ? (ColorRole.TextSecondary, TypographyRole.Annotation) : (ColorRole.TextPrimary, TypographyRole.Strong),
            Standing.FreeAgent => (ColorRole.TextSecondary, TypographyRole.Annotation),
            _ => (ColorRole.InformationUnknown, TypographyRole.Annotation)
        };
        var name = Clamp(Text(ui, SideText(side, origin), role, tone, size: TypographyRole.Annotation), 1); name.Name = "JourneyName";
        name.TooltipText = SideText(side, origin, full: true); name.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        box.AddChild(name);
        return box;
    }
}
