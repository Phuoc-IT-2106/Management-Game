using System.Collections.Immutable;
using System.Globalization;
using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;
using ManagementGame.Formatting;

namespace ManagementGame.Shell;

/// <summary>Portal (genre shell S4) composed after the approved reference
/// (generated-reference-set/11_portal_world_transfer_graphite_mockup): key decisions, then world news beside transfer
/// news, then next match, season record and money. Built only from <see cref="PortalView"/>: it never reads Domain
/// state, never invents content, and routes by section plus stable target ID, never by display text. Regions live in
/// the PortalScreen.*.cs partial files.</summary>
public static partial class PortalScreen
{
    /// <summary>Navigation and Portal-local view state.</summary>
    public sealed record Routes(Action<string, string> Open, Func<string, bool> Unread, Action ToggleDecisions, bool DecisionsExpanded);

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    public static string Until(int day, int today) => (day - today) switch { < 0 => $"{today - day} days ago", 0 => "today", 1 => "tomorrow", var n => $"in {n} days" };
    /// <summary>The one wording for the pending-decision total, shared by the top bar and the Portal.</summary>
    public static string PendingText(int count) => count == 1 ? "1 pending decision" : $"{count} pending decisions";
    private static string Ago(int day, int today) => (today - day) switch { <= 0 => "Today", 1 => "Yesterday", var n => $"{n} days ago" };
    private static string Phase(SeasonPhase phase) => phase switch { SeasonPhase.FirstHalf => "First half", SeasonPhase.SecondHalf => "Second half", _ => "Season complete" };

    public static Control Create(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        var scroll = new ScrollContainer { Name = "PortalScroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        var margin = new MarginContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (var edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, ShellLayout.ContentPadding);
        scroll.AddChild(margin);
        var page = Stack(ShellLayout.SectionGap); page.SizeFlagsVertical = Control.SizeFlags.ExpandFill; margin.AddChild(page);
        page.AddChild(Decisions(ui, assets, m, roomy, routes));
        var middle = Row(ShellLayout.SectionGap); middle.Name = "Portal_Middle"; middle.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        middle.AddChild(Share(News(ui, assets, m, roomy, routes), ShellLayout.WorldNewsShare));
        middle.AddChild(Share(Transfers(ui, assets, m, roomy, routes), ShellLayout.TransferShare));
        page.AddChild(middle);
        var bottom = Row(ShellLayout.SectionGap); bottom.Name = "Portal_Bottom";
        bottom.CustomMinimumSize = new(0, ShellLayout.BottomBandHeight(roomy)); bottom.SizeFlagsVertical = Control.SizeFlags.Fill;
        bottom.AddChild(Share(Match(ui, assets, m, roomy, routes), ShellLayout.MatchShare));
        bottom.AddChild(Share(Record(ui, m, roomy), ShellLayout.RecordShare));
        bottom.AddChild(Share(Money(ui, m, roomy, routes), ShellLayout.MoneyShare));
        page.AddChild(bottom);
        return scroll;
    }

    // ---------- shared pieces ----------
    private static VBoxContainer Stack(int gap)
    {
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; box.AddThemeConstantOverride("separation", gap); return box;
    }
    private static HBoxContainer Row(int gap)
    {
        var box = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; box.AddThemeConstantOverride("separation", gap); return box;
    }
    private static Control Spacer() => new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
    /// <summary>Text on the shared scale. <paramref name="size"/> picks another step of the same scale, never a one-off size.</summary>
    private static Label Text(UiContext ui, string text, TypographyRole role, ColorRole tone = ColorRole.TextPrimary, bool wrap = true, TypographyRole? size = null)
    {
        var label = SemanticText.Create(ui, text, role, tone);
        if (!wrap) { label.AutowrapMode = TextServer.AutowrapMode.Off; label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin; }
        if (size is { } step) label.AddThemeFontSizeOverride("font_size", ui.Tokens.FontSize(step));
        return label;
    }
    /// <summary>Bounded text: at most <paramref name="lines"/> lines, the rest in the tooltip and at the destination.</summary>
    private static Label Clamp(Label label, int lines)
    {
        if (lines == 1) label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.MaxLinesVisible = lines; label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return label;
    }
    /// <summary>Category labels follow the reference: an uppercase semibold caption in a semantic or secondary tone.</summary>
    private static Label Caps(UiContext ui, string text, ColorRole tone = ColorRole.TextSecondary) =>
        Text(ui, text.ToUpper(Invariant), TypographyRole.Strong, tone, wrap: false, size: TypographyRole.Caption);
    private static Control Share(Control c, float share) { c.SizeFlagsStretchRatio = share; return c; }
    private static Color Tone(UiContext ui, ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));
    private static Control Rule(UiContext ui, bool vertical = false) => EngravedRule.Create(ui, vertical);
    /// <summary>A small status chip (a utility tag): the words carry the meaning, the tone supports it (never colour alone).</summary>
    private static PanelContainer Chip(UiContext ui, string text, ColorRole tone, bool filled = true)
    {
        var chip = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        var color = Tone(ui, tone);
        chip.AddThemeStyleboxOverride("panel", Surfaces.Tag(filled ? Tone(ui, ColorRole.SurfaceInset).Lerp(color, .18f) : Colors.Transparent,
            color.Lerp(Tone(ui, ColorRole.SurfaceRaised), .35f), 7, 1));
        chip.AddChild(Text(ui, text, TypographyRole.Strong, tone, wrap: false, size: TypographyRole.Caption));
        return chip;
    }
    /// <summary>One region per gameplay question: a level-1 major frame (PanelFrame.Major) whose header band is the shared
    /// SectionHeader.Region; content sits on level-2 plates inside, so regions are never flat boxes of text.</summary>
    private static PanelContainer Region(UiContext ui, string name, string icon, string title, bool roomy, out VBoxContainer body, string context = "",
        Control? action = null, ColorRole iconTone = ColorRole.ActionPrimary) =>
        PanelFrame.Major(ui, name, SectionHeader.Region(ui, icon, title, roomy, context, action, iconTone, ShellLayout.RegionTier(name)), ShellLayout.RegionPadding(roomy),
            out body, roomy ? 10 : 6, ShellLayout.RegionTier(name));
    /// <summary>Route with a trailing arrow ("View all →"): a small graphite-and-steel secondary button, keyboard focusable.</summary>
    private static CardButton Link(UiContext ui, string name, string text, Action act, ColorRole tone = ColorRole.TextPrimary)
    {
        var link = new CardButton(9, 2) { Name = name, ThemeTypeVariation = "", SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }.Compact();
        var row = CardButton.Passive(Row(6));
        row.AddChild(Text(ui, text, TypographyRole.Annotation, tone, wrap: false));
        row.AddChild(UiIcon.Create(ui, "arrow", 14, tone));
        link.Body.AddChild(row);
        link.Pressed += act;
        return link;
    }
    /// <summary>Intentional empty state on a level-2 plate: a status indicator in a recessed well (its tone says whether the
    /// absence is good news), the primary message, then the supporting line. Compact; never filler content or art.</summary>
    private static Control Empty(UiContext ui, string icon, string message, string detail = "", ColorRole status = ColorRole.TextMuted)
    {
        var plate = PanelFrame.Card(ui, 12, 8); plate.Name = "EmptyState"; plate.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        var row = Row(12); plate.AddChild(row);
        var badge = PanelFrame.Well(ui); badge.CustomMinimumSize = new(34, 34); badge.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var glyph = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore }; glyph.AddChild(UiIcon.Create(ui, icon, 18, status)); badge.AddChild(glyph);
        row.AddChild(badge);
        var words = Stack(1); words.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        words.AddChild(Text(ui, message, TypographyRole.Strong, ColorRole.TextPrimary));
        if (detail.Length > 0) words.AddChild(Text(ui, detail, TypographyRole.Annotation, ColorRole.TextMuted));
        row.AddChild(words);
        return plate;
    }

    // ---------- key decisions ----------
    /// <summary>The total always matches the top bar; when only the most urgent cards fit, the header says so and offers
    /// the rest in place.</summary>
    private static Control Decisions(UiContext ui, IdentityAssets assets, PortalView m, bool roomy, Routes routes)
    {
        var slots = ShellLayout.DecisionCards(roomy);
        var blocking = m.Decisions.Any(d => d.Urgency == TaskUrgency.Blocking);
        var summary = Row(12); summary.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        if (!m.Decisions.IsEmpty)
        {
            var total = Text(ui, (blocking ? PresentationText.WarningMarker + " " : "") + PendingText(m.Decisions.Length), TypographyRole.Strong,
                blocking ? ColorRole.StateCritical : ColorRole.TextSecondary, wrap: false, size: TypographyRole.Annotation);
            total.Name = "PortalDecisionCount"; total.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            summary.AddChild(total);
        }
        if (m.Decisions.Length > slots)
        {
            if (!routes.DecisionsExpanded) summary.AddChild(Text(ui, $"showing {slots}", TypographyRole.Annotation, ColorRole.TextMuted, wrap: false));
            summary.AddChild(Link(ui, "PortalDecisionsMore", routes.DecisionsExpanded ? "Show fewer" : "Show all", routes.ToggleDecisions, ColorRole.TextPrimary));
        }
        foreach (var child in summary.GetChildren().OfType<Label>()) child.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var panel = Region(ui, "Portal_Decisions", "alert", "Key decisions", roomy, out var body, "", summary, ColorRole.ActionPrimary);
        panel.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        if (m.Decisions.IsEmpty)
        {
            body.AddChild(Empty(ui, "decisions", "Nothing currently requires executive attention.", "Continue when ready; new matters will appear here.", ColorRole.StatePositive));
            return panel;
        }
        var shown = routes.DecisionsExpanded ? m.Decisions : m.Decisions.Take(slots).ToImmutableArray();
        var columns = routes.DecisionsExpanded ? slots : Math.Clamp(shown.Length, 2, slots);
        var grid = new GridContainer { Columns = columns, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", ShellLayout.SectionGap); grid.AddThemeConstantOverride("v_separation", ShellLayout.SectionGap);
        for (var i = 0; i < shown.Length; i++) grid.AddChild(DecisionCard(ui, assets, shown[i], i, m.Time.Day, routes.Open, roomy));
        for (var i = shown.Length; i < columns; i++) grid.AddChild(Spacer());
        body.AddChild(grid);
        return panel;
    }
    private static string CategoryName(DecisionKind kind) => kind switch
    {
        DecisionKind.Distress => "Finance", DecisionKind.MissingRole or DecisionKind.RosterShortfall => "Roster move", DecisionKind.MatchPreparation => "Match preparation",
        DecisionKind.SponsorOffer => "Sponsor offer", DecisionKind.PlayerRenewal => "Player contract", DecisionKind.CoachRenewal => "Staff contract",
        DecisionKind.ProposalPending => "Sponsor proposal", _ => "Commercial"
    };
    /// <summary>The verb on the card says what opening it lets the player do; the commitment itself is confirmed at the destination.</summary>
    private static string Verb(DecisionKind kind) => kind switch
    {
        DecisionKind.Distress => "Review finances", DecisionKind.MissingRole or DecisionKind.RosterShortfall => "Sign a player", DecisionKind.MatchPreparation => "Prepare",
        DecisionKind.SponsorOffer => "Review offer", DecisionKind.PlayerRenewal => "Renew or release", DecisionKind.CoachRenewal => "Renew or replace",
        DecisionKind.ProposalPending => "View proposal", _ => "Approach a brand"
    };
    /// <summary>Approved semantic art when the decision is one of its concepts; otherwise the subject's generated mark
    /// (a brand), otherwise the vector icon of its category. Never a stand-in from another concept.</summary>
    public static SemanticConcept? DecisionArt(DecisionKind kind) => kind switch
    {
        DecisionKind.MatchPreparation => SemanticConcept.MatchPreparationDecision,
        DecisionKind.PlayerRenewal or DecisionKind.CoachRenewal => SemanticConcept.ContractDecision,
        DecisionKind.MissingRole or DecisionKind.RosterShortfall => SemanticConcept.RosterMoveDecision,
        _ => null
    };
    /// <summary>When the matter is due, in one consistent place and wording; the tone follows urgency, the words carry it.</summary>
    private static (string Text, ColorRole Tone) Due(PortalDecision d, int today)
    {
        if (d.DueDay is not { } day) return ("", ColorRole.TextSecondary);
        var days = day - today;
        var text = days < 0 ? "Overdue" : d.Urgency == TaskUrgency.Open ? "Expected " + Until(day, today) : "Due " + Until(day, today);
        var tone = d.Urgency == TaskUrgency.Blocking || days <= 0 ? ColorRole.StateCritical : days == 1 ? ColorRole.StateWarning : ColorRole.TextSecondary;
        return (text, tone);
    }
    /// <summary>Whether the card shows the decision's consequence line. Sponsor offers never do: their commercial terms
    /// belong to the offer review, not to a Portal summary. Compact cards carry the category, subject, due date and action
    /// only; the consequence stays in the card's tooltip and at the destination.</summary>
    public static bool ShowsReason(DecisionKind kind, bool roomy) => roomy && kind != DecisionKind.SponsorOffer;

    /// <summary>A decision card in three functional areas: the context row (what kind of matter, when it is due) across
    /// the top; then the identity (approved art, the subject's mark or the category icon) on the left, the decision in the
    /// centre, and the action at the bottom right, beside the decision so the Portal keeps its height. The decision column
    /// takes the remaining width, so a long name wraps instead of meeting the action.</summary>
    private static Control DecisionCard(UiContext ui, IdentityAssets assets, PortalDecision d, int index, int today, Action<string, string> open, bool roomy)
    {
        var card = new CardButton(roomy ? 14 : 10, roomy ? 10 : 9) { Name = "Task_" + index, ThemeTypeVariation = "Card" };
        card.SetMeta("section", d.Section); card.SetMeta("target_id", d.TargetId); card.SetMeta("decision_id", d.Id); card.SetMeta("kind", d.Kind.ToString());
        card.Body.AddThemeConstantOverride("separation", roomy ? 6 : 5);
        // Context row: category on the left, when it is due on the right.
        var head = CardButton.Passive(Row(8));
        var blocking = d.Urgency == TaskUrgency.Blocking;
        head.AddChild(Clamp(Caps(ui, (blocking ? PresentationText.WarningMarker + " " : "") + CategoryName(d.Kind), blocking ? ColorRole.StateWarning : ColorRole.TextSecondary), 1));
        var (due, dueTone) = Due(d, today);
        if (due.Length > 0)
        {
            var when = Text(ui, due, TypographyRole.Strong, dueTone, wrap: false, size: TypographyRole.Caption); when.Name = "Due";
            head.AddChild(when);
        }
        card.Body.AddChild(head);
        // Identity on the left; the most urgent decision's frame carries the warm edge.
        // The content row takes the card's spare height (cards in a grid row share the tallest height), so the action
        // always lands at the bottom right.
        var row = CardButton.Passive(Row(roomy ? 14 : 10)); row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        var size = ShellLayout.DecisionArt(roomy);
        var tile = PanelFrame.Well(ui, accent: index == 0); tile.Name = "DecisionArt";
        tile.CustomMinimumSize = new(size, size); tile.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        Control art = DecisionArt(d.Kind) is { } concept ? SemanticArt.Create(concept, new(size - 6, size - 6))
            : d.SubjectAssetId.Length > 0 ? assets.Create(d.SubjectAssetId, new(size * .7f, size * .7f))
            : UiIcon.Create(ui, d.Kind == DecisionKind.Distress ? "finance" : "commercial", size / 2, ColorRole.TextSecondary);
        art.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; art.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        tile.AddChild(art);
        row.AddChild(tile);
        // The decision: the subject leads (names wrap; an extreme name is bounded, the full text in the tooltip and at the
        // destination), then the consequence where the kind shows one.
        var text = CardButton.Passive(Stack(roomy ? 3 : 1)); text.Name = "DecisionText"; text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var subject = Clamp(Text(ui, d.Subject, TypographyRole.Strong, size: roomy ? TypographyRole.SectionTitle : TypographyRole.Label), 2); subject.Name = "Subject";
        text.AddChild(subject);
        var reason = ShowsReason(d.Kind, roomy) && d.Why.Length > 0;
        if (reason) text.AddChild(Clamp(Text(ui, d.Why, TypographyRole.Annotation, ColorRole.TextSecondary, size: TypographyRole.Annotation), 1));
        row.AddChild(text);
        // The card is the one focusable action; its verb reads like a button without being a second target. Brass action
        // material (the primary-button family, smaller); a blocking matter uses the critical-tinted secondary.
        var verb = new PanelContainer { Name = "Verb", SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd, SizeFlagsVertical = Control.SizeFlags.ShrinkEnd, MouseFilter = Control.MouseFilterEnum.Ignore };
        verb.AddThemeStyleboxOverride("panel", blocking ? Surfaces.Secondary(ui.Tokens, SurfaceState.Normal, roomy ? 10 : 8, roomy ? 3 : 2, ColorRole.StateCritical)
            : Surfaces.Primary(ui.Tokens, SurfaceState.Normal, roomy ? 10 : 8, roomy ? 3 : 2, local: true));
        var tone = blocking ? ColorRole.TextPrimary : ColorRole.TextOnAction;
        var verbRow = Row(roomy ? 6 : 4); verbRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        verbRow.AddChild(Text(ui, Verb(d.Kind), TypographyRole.Strong, tone, wrap: false, size: TypographyRole.Annotation));
        verbRow.AddChild(UiIcon.Create(ui, "arrow", 14, tone));
        verb.AddChild(verbRow);
        row.AddChild(verb);
        card.Body.AddChild(row);
        card.Body.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        card.TooltipText = d.Why.Length > 0 && d.Kind != DecisionKind.SponsorOffer ? $"{d.Subject}: {d.Why}" : d.Subject;
        card.Pressed += () => open(d.Section, d.TargetId);
        return card;
    }
}
