using System.Collections.Immutable;
using System.Globalization;
using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Portal (genre shell S4) composed after the approved reference (generated-reference-set/01_portal_mockup).
/// Built only from <see cref="PortalView"/>: it never reads Domain state, never invents content, and routes by section
/// plus stable target ID, never by display text. Regions live in the PortalScreen.*.cs partial files.</summary>
public static partial class PortalScreen
{
    public sealed record Routes(Action<string, string> Open, Func<string, bool> Unread, Action ToggleDecisions, bool DecisionsExpanded);

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    public static string Cu(long minor) => (minor < 0 ? "−" : "") + Math.Abs(minor / 100m).ToString("N0", Invariant) + " CU";
    public static string Signed(long minor) => (minor < 0 ? "−" : "+") + Math.Abs(minor / 100m).ToString("N0", Invariant) + " CU";
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
        page.AddChild(Hero(ui, assets, m, roomy));
        page.AddChild(Decisions(ui, m, roomy, routes));
        var lower = new HBoxContainer { Name = "Portal_Lower", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        lower.AddThemeConstantOverride("separation", ShellLayout.SectionGap);
        page.AddChild(lower);
        lower.AddChild(Column(ShellLayout.NewsShare, News(ui, assets, m, roomy, routes)));
        lower.AddChild(Column(ShellLayout.MatchShare, Share(Match(ui, assets, m, roomy, routes), ShellLayout.MatchUpperShare), Share(Record(ui, assets, m, roomy), 1 - ShellLayout.MatchUpperShare)));
        lower.AddChild(Column(ShellLayout.OperationsShare, Share(Horizon(ui, m, roomy, routes), ShellLayout.OperationsUpperShare), Share(Money(ui, m, roomy, routes), 1 - ShellLayout.OperationsUpperShare)));
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
    private static VBoxContainer Column(float share, params Control[] children)
    {
        var column = Stack(ShellLayout.SectionGap); column.SizeFlagsStretchRatio = share; column.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        foreach (var child in children) column.AddChild(child);
        return column;
    }
    private static Control Share(Control c, float share) { c.SizeFlagsStretchRatio = share; return c; }
    private static StyleBoxFlat Fill(Color color, int margin, Color? border = null)
    {
        var box = new StyleBoxFlat { BgColor = color };
        box.SetCornerRadiusAll(UiTokens.CornerRadius); box.SetContentMarginAll(margin);
        if (border is { } edge) { box.BorderColor = edge; box.SetBorderWidthAll(1); }
        return box;
    }
    private static Color Tone(UiContext ui, ColorRole role) => UiTheme.ToGodot(ui.Tokens.Color(role));
    private static ColorRect Rule(UiContext ui, bool vertical = false) => new()
    {
        Color = Tone(ui, ColorRole.Divider), CustomMinimumSize = vertical ? new(1, 0) : new(0, 1), MouseFilter = Control.MouseFilterEnum.Ignore,
        SizeFlagsVertical = vertical ? Control.SizeFlags.Fill : Control.SizeFlags.ShrinkBegin
    };
    /// <summary>One region per gameplay question on the raised workspace tier: icon, title, optional context and one route
    /// above a divider. Rows inside use the inset tier or no surface at all, so regions are never nested cards.</summary>
    private static PanelContainer Region(UiContext ui, string name, string icon, string title, bool roomy, out VBoxContainer body, string context = "",
        Control? action = null, ColorRole iconTone = ColorRole.TextPrimary)
    {
        var stack = Stack(roomy ? 10 : 5);
        var head = Row(10);
        head.AddChild(UiIcon.Create(ui, icon, ShellLayout.RegionIcon(roomy), iconTone));
        head.AddChild(Text(ui, title, TypographyRole.SectionTitle, wrap: false));
        head.AddChild(Spacer());
        if (context.Length > 0) { var note = Text(ui, context, TypographyRole.Caption, ColorRole.TextMuted, wrap: false); note.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; head.AddChild(note); }
        if (action is not null) head.AddChild(action);
        stack.AddChild(head);
        stack.AddChild(Rule(ui));
        body = Stack(roomy ? 10 : 8); body.SizeFlagsVertical = Control.SizeFlags.ExpandFill; stack.AddChild(body);
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", Fill(Tone(ui, ColorRole.SurfaceRaised), ShellLayout.RegionPadding(roomy), Tone(ui, ColorRole.Divider)));
        panel.AddChild(stack);
        return panel;
    }
    /// <summary>Text route with a trailing arrow ("View all →"), keyboard focusable.</summary>
    private static CardButton Link(UiContext ui, string name, string text, Action act, ColorRole tone = ColorRole.TextSecondary)
    {
        var link = new CardButton(6, 2) { Name = name, ThemeTypeVariation = "Entity", SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        var row = CardButton.Passive(Row(6));
        row.AddChild(Text(ui, text, TypographyRole.Annotation, tone, wrap: false));
        row.AddChild(UiIcon.Create(ui, "arrow", 14, tone));
        link.Body.AddChild(row);
        link.Pressed += act;
        return link;
    }
    /// <summary>Intentional empty state: what is absent and when it will appear. Never filler content.</summary>
    private static Control Empty(UiContext ui, string icon, string message, string detail = "")
    {
        var row = Row(12); row.Name = "EmptyState"; row.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        row.AddChild(UiIcon.Create(ui, icon, 24, ColorRole.TextMuted));
        var words = Stack(2); words.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        words.AddChild(Text(ui, message, TypographyRole.Label, ColorRole.TextSecondary));
        if (detail.Length > 0) words.AddChild(Text(ui, detail, TypographyRole.Annotation, ColorRole.TextMuted));
        row.AddChild(words);
        return row;
    }

    // ---------- hero ----------
    /// <summary>The Portal title over the company's atmosphere. The environment slot resolves through the asset catalog;
    /// until approved art exists it is a procedural wash and light pool in the company's own colour, so it never carries
    /// a fixed name, logo or value. The crest is the company's generated arms, by stable ID.</summary>
    private static Control Hero(UiContext ui, IdentityAssets assets, PortalView m, bool roomy)
    {
        var panel = new PanelContainer { Name = "Portal_Header", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new(0, ShellLayout.HeaderHeight(roomy)), ClipContents = true };
        var surface = Tone(ui, ColorRole.SurfaceRaised);
        panel.AddThemeStyleboxOverride("panel", Fill(surface, 0, Tone(ui, ColorRole.Divider)));
        var club = UiTheme.ToGodot(assets.TeamColor(m.Company.CrestAssetId));
        var environment = IdentityAssets.Environment("portal");
        var backdrop = Backdrop.Create(surface, surface.Lerp(club, .5f), Tone(ui, ColorRole.SurfaceBase).Lerp(surface, .3f) with { A = .55f },
            new Backdrop.Glow(new(.93f, .5f), 1.15f, club.Lightened(.35f) with { A = .45f }),
            new Backdrop.Glow(new(.7f, 1.05f), 1f, club with { A = .25f }));
        backdrop.Name = "PortalEnvironment"; backdrop.SetMeta("asset_id", environment); backdrop.SetMeta("asset_resolution", IdentityAssets.Resolve(environment));
        panel.AddChild(backdrop);
        // The company's own arms, oversized and faint, as the room's emblem: identity by stable ID, never a fixed logo.
        var layer = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        var echoHeight = ShellLayout.HeaderHeight(roomy) * 2.4f;
        var echo = assets.Create(m.Company.CrestAssetId, new(echoHeight * 100f / 120, echoHeight)); echo.Name = "PortalEmblem";
        echo.Modulate = new Color(1, 1, 1, .07f);
        layer.AddChild(echo);
        echo.AnchorLeft = echo.AnchorRight = .76f; echo.AnchorTop = echo.AnchorBottom = .5f;
        echo.OffsetLeft = -echoHeight * 50f / 120; echo.OffsetRight = echoHeight * 50f / 120; echo.OffsetTop = -echoHeight / 2; echo.OffsetBottom = echoHeight / 2;
        panel.AddChild(layer);
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (var (edge, value) in new[] { ("left", 24), ("right", 40), ("top", 6), ("bottom", 6) }) margin.AddThemeConstantOverride("margin_" + edge, value);
        var row = Row(24); margin.AddChild(row);
        var title = Stack(0); title.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        title.AddChild(Text(ui, "Portal", TypographyRole.Display, wrap: false));
        title.AddChild(Text(ui, "Your central hub for managing the organization. An overview of what matters most right now.", TypographyRole.Label,
            ColorRole.TextSecondary, size: roomy ? TypographyRole.Label : TypographyRole.Annotation));
        row.AddChild(title);
        var size = ShellLayout.CrestHeader(roomy);
        var crest = assets.Create(m.Company.CrestAssetId, new(size * 100f / 120, size)); crest.Name = "PortalCrest";
        crest.TooltipText = m.Company.Name;
        row.AddChild(crest);
        panel.AddChild(margin);
        return panel;
    }

    // ---------- key decisions ----------
    /// <summary>The total always matches the top bar; when only the most urgent cards fit, the header says so and offers
    /// the rest in place.</summary>
    private static Control Decisions(UiContext ui, PortalView m, bool roomy, Routes routes)
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
            body.AddChild(Empty(ui, "decisions", "Nothing currently requires executive attention.", "Continue when ready; new matters will appear here."));
            return panel;
        }
        var shown = routes.DecisionsExpanded ? m.Decisions : m.Decisions.Take(slots).ToImmutableArray();
        var columns = routes.DecisionsExpanded ? slots : Math.Clamp(shown.Length, 2, slots);
        var grid = new GridContainer { Columns = columns, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", ShellLayout.SectionGap); grid.AddThemeConstantOverride("v_separation", ShellLayout.SectionGap);
        for (var i = 0; i < shown.Length; i++) grid.AddChild(DecisionCard(ui, shown[i], i, m.Time.Day, routes.Open, roomy));
        for (var i = shown.Length; i < columns; i++) grid.AddChild(Spacer());
        body.AddChild(grid);
        return panel;
    }
    private static string CategoryName(DecisionCategory category) => category switch
    {
        DecisionCategory.Competition => "Match preparation", DecisionCategory.Squad => "Squad", DecisionCategory.Contract => "Contract",
        DecisionCategory.Staff => "Staff", DecisionCategory.Commercial => "Commercial", _ => "Finance"
    };
    private static string CategoryIcon(DecisionCategory category) => category switch
    {
        DecisionCategory.Competition => "competition", DecisionCategory.Squad => "squad", DecisionCategory.Contract => "contract",
        DecisionCategory.Staff => "staff", DecisionCategory.Commercial => "commercial", _ => "finance"
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
    private static Control DecisionCard(UiContext ui, PortalDecision d, int index, int today, Action<string, string> open, bool roomy)
    {
        var card = new CardButton(12, roomy ? 10 : 7) { Name = "Task_" + index, ThemeTypeVariation = "Card" };
        card.SetMeta("section", d.Section); card.SetMeta("target_id", d.TargetId); card.SetMeta("decision_id", d.Id);
        var row = CardButton.Passive(Row(12));
        // Icon tile; the first (most urgent) decision's tile carries the gold edge, as in the reference.
        var size = ShellLayout.DecisionIcon(roomy);
        var tile = new PanelContainer { CustomMinimumSize = new(size, size), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        tile.AddThemeStyleboxOverride("panel", Fill(Tone(ui, ColorRole.SelectedSurface), 0, index == 0 ? Tone(ui, ColorRole.ActionPrimary) : null));
        tile.AddChild(UiIcon.Create(ui, CategoryIcon(d.Category), roomy ? 26 : 24, index == 0 ? ColorRole.ActionPrimary : ColorRole.TextPrimary));
        row.AddChild(tile);
        var text = CardButton.Passive(Stack(2)); text.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        // Metadata row: category on the left, when it is due on the right; subject leads below, the consequence follows.
        var head = CardButton.Passive(Row(8));
        var blocking = d.Urgency == TaskUrgency.Blocking;
        head.AddChild(Clamp(Caps(ui, (blocking ? PresentationText.WarningMarker + " " : "") + CategoryName(d.Category), blocking ? ColorRole.StateWarning : ColorRole.TextSecondary), 1));
        var (due, dueTone) = Due(d, today);
        if (due.Length > 0)
        {
            var when = Text(ui, due, TypographyRole.Strong, dueTone, wrap: false, size: TypographyRole.Caption); when.Name = "Due";
            head.AddChild(when);
        }
        text.AddChild(head);
        // Names wrap; an extreme name is bounded to two lines with the full text in the tooltip and the destination.
        text.AddChild(Clamp(Text(ui, d.Subject, TypographyRole.Strong), roomy ? 2 : 1));
        if (d.Why.Length > 0) text.AddChild(Clamp(Text(ui, d.Why, TypographyRole.Annotation, ColorRole.TextSecondary), roomy ? 2 : 1));
        card.TooltipText = d.Why.Length > 0 ? $"{d.Subject}: {d.Why}" : d.Subject;
        row.AddChild(text);
        row.AddChild(UiIcon.Create(ui, "chevron", 18, ColorRole.TextSecondary));
        card.Body.AddChild(row);
        card.Pressed += () => open(d.Section, d.TargetId);
        return card;
    }
}
