using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;

namespace ManagementGame.Shell;

/// <summary>Four-week operations and the money snapshot. Horizon events are presented from their typed kind (category,
/// icon, title) plus subject, amount and day; nothing is parsed from text. Money keeps the known / estimated /
/// scheduled / overdue distinctions of the read model.</summary>
public static partial class PortalScreen
{
    /// <summary>Presentation class of a horizon event: how it scans (category label, icon, tone), its title and a short
    /// title for the compact week column.</summary>
    private static (string Category, string Icon, ColorRole Tone, string Title, string Short) EventClass(HorizonEvent e)
    {
        var times = e.Count > 1 ? $" ×{e.Count}" : "";
        return e.Kind switch
        {
            HorizonKind.Match => ("Match", "competition", ColorRole.TextPrimary, "vs " + e.Subject, "Match"),
            HorizonKind.OfferDeadline => ("Commercial", "commercial", ColorRole.StateWarning, e.Count > 1 ? $"{e.Count} offers expire" : "Offer expires", "Offer" + times),
            HorizonKind.ProposalAnswer => ("Commercial", "commercial", ColorRole.TextSecondary, e.Count > 1 ? $"{e.Count} answers due" : "Answer due", "Answer" + times),
            HorizonKind.ContractEnd => e.Section == ShellSections.Staff
                ? ("Staff", "staff", ColorRole.StateWarning, "Contract ends", "Coach")
                : ("Contract", "contract", ColorRole.StateWarning, e.Count > 1 ? $"{e.Count} contracts end" : "Contract ends", "Contract" + times),
            HorizonKind.Receipt => ("Finance", "cash", ColorRole.StatePositive, "Receipt", Signed(e.Amount)),
            _ => ("Season", "calendar", ColorRole.TextSecondary, "Season ends", "Season end")
        };
    }
    /// <summary>The line under the title: what is at stake for a match, the amount of a receipt, who a deadline concerns.</summary>
    private static string EventDetail(HorizonEvent e) => e.Kind switch
    {
        HorizonKind.Receipt => Signed(e.Amount),
        HorizonKind.Match => e.Note.Length > 0 ? "Double stakes" : "",
        HorizonKind.SeasonEnd => "",
        _ => e.Subject
    };

    private static Control Horizon(UiContext ui, PortalView m, bool roomy, Routes routes)
    {
        var panel = Region(ui, "Portal_Horizon", "calendar", "Four-week operations", roomy, out var body, "",
            Link(ui, "PortalHorizonOpen", "Next 4 weeks", () => routes.Open(ShellSections.Competition, "")));
        var row = Row(6); row.SizeFlagsVertical = Control.SizeFlags.ExpandFill; body.AddChild(row);
        var limit = ShellLayout.HorizonEventsPerWeek(roomy);
        foreach (var week in m.Horizon)
        {
            var column = Stack(roomy ? 8 : 4);
            var title = Text(ui, $"Week {week.Week}", TypographyRole.Strong, wrap: false, size: roomy ? TypographyRole.Label : TypographyRole.Annotation);
            title.HorizontalAlignment = HorizontalAlignment.Center; title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            var days = Text(ui, $"Days {week.FirstDayOfSeason}–{week.LastDayOfSeason}", TypographyRole.Caption, ColorRole.TextMuted, wrap: false);
            days.HorizontalAlignment = HorizontalAlignment.Center; days.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            var head = Stack(0); head.AddChild(title); head.AddChild(days); column.AddChild(head);
            // Timeline row: a line through every week; the current week's node carries a "Current" tag (text, not colour alone).
            var track = new Control { CustomMinimumSize = new(0, 20), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
            var node = TimelineNode.Create(ui, week.Current); track.AddChild(node); node.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var tagText = week.Current ? "Current" : week.Season != m.Time.Season ? $"Season {week.Season}" : "";
            if (tagText.Length > 0)
            {
                var tag = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
                var pill = Fill(week.Current ? Tone(ui, ColorRole.StatePositive) : Tone(ui, ColorRole.SelectedSurface), 0);
                pill.ContentMarginLeft = pill.ContentMarginRight = 8;
                tag.AddThemeStyleboxOverride("panel", pill);
                tag.AddChild(Text(ui, tagText, TypographyRole.Strong, week.Current ? ColorRole.TextOnAction : ColorRole.TextSecondary, wrap: false, size: TypographyRole.Caption));
                track.AddChild(tag); tag.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center, Control.LayoutPresetMode.KeepSize);
                tag.GrowHorizontal = Control.GrowDirection.Both; tag.GrowVertical = Control.GrowDirection.Both;
            }
            column.AddChild(track);
            if (!week.ScheduleDrawn)
                column.AddChild(Clamp(Text(ui, $"{PresentationText.InformationMarker(InformationState.Unknown)} Fixtures not drawn yet", TypographyRole.Caption, ColorRole.InformationUnknown), 2));
            // A compact column that cannot show every event keeps one slot for the "more" line.
            var shown = week.Events.Length > limit ? limit - 1 : week.Events.Length;
            for (var i = 0; i < shown; i++) column.AddChild(Event(ui, week, i, roomy, routes.Open));
            if (week.Events.Length > shown) column.AddChild(Text(ui, $"+{week.Events.Length - shown} more", TypographyRole.Caption, ColorRole.TextSecondary));
            if (week.Events.IsEmpty && week.ScheduleDrawn) column.AddChild(Clamp(Text(ui, "Nothing scheduled", TypographyRole.Caption, ColorRole.TextMuted), 2));
            var cell = new PanelContainer { Name = $"Week_{week.Season}_{week.Week}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            cell.AddThemeStyleboxOverride("panel", Fill(Tone(ui, ColorRole.SurfaceInset), roomy ? 8 : 5, week.Current ? Tone(ui, ColorRole.StatePositive) : null));
            cell.AddChild(column);
            row.AddChild(cell);
        }
        return panel;
    }
    /// <summary>One structured event: category (icon and label), title, subject or amount, and the season day.</summary>
    private static Control Event(UiContext ui, HorizonWeek week, int index, bool roomy, Action<string, string> open)
    {
        var e = week.Events[index];
        var (category, icon, tone, title, shortTitle) = EventClass(e);
        var detail = EventDetail(e);
        var day = $"Day {e.Day - week.FirstDay + week.FirstDayOfSeason}";
        var button = new CardButton(2, roomy ? 3 : 1) { Name = $"Event_{week.Season}_{week.Week}_{index}", ThemeTypeVariation = "Entity" };
        button.SetMeta("section", e.Section); button.SetMeta("target_id", e.TargetId); button.SetMeta("kind", e.Kind.ToString());
        button.TooltipText = $"{category}: {e.Text}{(e.Note.Length > 0 ? ", " + e.Note : "")}. {day}.";
        var stack = CardButton.Passive(Stack(roomy ? 1 : 0));
        var head = CardButton.Passive(Row(5));
        head.AddChild(UiIcon.Create(ui, icon, roomy ? 14 : 12, tone));
        if (roomy) head.AddChild(Clamp(Caps(ui, category, tone), 1));
        else head.AddChild(Clamp(Text(ui, shortTitle, TypographyRole.Strong, e.Kind == HorizonKind.Receipt ? ColorRole.StatePositive : e.Material ? ColorRole.TextPrimary : ColorRole.TextSecondary,
            size: TypographyRole.Caption), 1));
        stack.AddChild(head);
        if (roomy)
        {
            stack.AddChild(Clamp(Text(ui, title, TypographyRole.Strong, e.Material ? ColorRole.TextPrimary : ColorRole.TextSecondary, size: TypographyRole.Annotation), 2));
            if (detail.Length > 0) stack.AddChild(Clamp(Text(ui, detail, TypographyRole.Caption, e.Kind switch
            {
                HorizonKind.Receipt => ColorRole.StatePositive, HorizonKind.Match => ColorRole.StateWarning, _ => ColorRole.TextSecondary
            }), 1));
            stack.AddChild(Text(ui, day, TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        }
        else stack.AddChild(Clamp(Text(ui, day, TypographyRole.Caption, ColorRole.TextMuted), 1));
        button.Body.AddChild(stack);
        button.Pressed += () => open(e.Section, e.TargetId);
        return button;
    }

    // ---------- money ----------
    /// <summary>Current position (known cash, condition, arrears) beside the short horizon (scheduled receipts, estimated
    /// payments, the 7-day estimate). Each bar sits directly under the value it scales.</summary>
    private static Control Money(UiContext ui, PortalView m, bool roomy, Routes routes)
    {
        var f = m.Finance;
        var panel = Region(ui, "Portal_Money", "finance", "Money snapshot", roomy, out var body, roomy ? "" : $"Now · next {f.AheadDays} days",
            Link(ui, "PortalMoneyOpen", "Finance", () => routes.Open(ShellSections.Finance, "")));
        var row = Row(roomy ? 20 : 14); row.SizeFlagsVertical = Control.SizeFlags.ExpandFill; body.AddChild(row);
        var now = Stack(roomy ? 4 : 2); now.Name = "MoneyNow"; now.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; now.SizeFlagsStretchRatio = .9f;
        if (roomy) now.AddChild(Caps(ui, "Now", ColorRole.TextMuted));
        var cash = Text(ui, Cu(f.Cash), TypographyRole.CompanyIdentity, wrap: false, size: roomy ? TypographyRole.WorkspaceTitle : TypographyRole.CompanyIdentity);
        cash.Name = "MoneyCash"; now.AddChild(cash);
        now.AddChild(Text(ui, $"Total cash, {PresentationText.InformationMarker(InformationState.Known)} known", TypographyRole.Caption, ColorRole.TextSecondary, wrap: false));
        var (condition, conditionTone) = Condition(f.Condition);
        if (roomy) now.AddChild(Text(ui, condition, TypographyRole.Strong, conditionTone, wrap: false, size: TypographyRole.Annotation));
        now.AddChild(f.Arrears > 0
            ? Text(ui, $"{PresentationText.CriticalMarker} Arrears {Cu(f.Arrears)} overdue", TypographyRole.Strong, ColorRole.StateCritical, wrap: false, size: TypographyRole.Caption)
            : Text(ui, "No arrears", TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        row.AddChild(now);
        row.AddChild(Rule(ui, vertical: true));
        var ahead = Stack(roomy ? 8 : 3); ahead.Name = "MoneyAhead"; ahead.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        if (roomy) ahead.AddChild(Caps(ui, $"Next {f.AheadDays} days", ColorRole.TextMuted));
        var reference = Math.Max(f.ReceiptsAhead, f.PaymentsAhead);
        foreach (var (label, value, tone, text) in new[]
        {
            ("Receipts, scheduled", f.ReceiptsAhead, ColorRole.StatePositive, Signed(f.ReceiptsAhead)),
            ("Payments, estimated", f.PaymentsAhead, ColorRole.StateWarning, Signed(-f.PaymentsAhead))
        })
        {
            var flow = Stack(3);
            var line = Row(8);
            line.AddChild(Clamp(Text(ui, label, TypographyRole.Caption, ColorRole.TextSecondary), 1));
            line.AddChild(Text(ui, text, TypographyRole.Strong, wrap: false, size: TypographyRole.Annotation));
            flow.AddChild(line);
            var bar = SegmentBar.Create(ui, value, reference, roomy ? 10 : 8, tone); bar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            flow.AddChild(bar);
            ahead.AddChild(flow);
        }
        var up = f.Forecast >= f.Cash;
        var forecast = Row(8);
        forecast.AddChild(Clamp(Text(ui, $"{f.ForecastDays}-day estimate", TypographyRole.Caption, ColorRole.TextSecondary), 1));
        forecast.AddChild(Text(ui, $"{(up ? "▲" : "▼")} {PresentationText.InformationMarker(InformationState.Estimated)} {Cu(f.Forecast)}", TypographyRole.Strong,
            up ? ColorRole.InformationEstimated : ColorRole.StateWarning, wrap: false, size: TypographyRole.Caption));
        ahead.AddChild(forecast);
        row.AddChild(ahead);
        return panel;
    }
    /// <summary>Financial condition wording and tone, shared with the top bar; the marker carries meaning without colour.</summary>
    public static (string Text, ColorRole Tone) Condition(string status) => status switch
    {
        "Stable" or "Stabilized" => ($"{PresentationText.PositiveMarker} {status}", ColorRole.StatePositive),
        "Distress" => ($"{PresentationText.CriticalMarker} {status}", ColorRole.StateCritical),
        _ => ($"{PresentationText.WarningMarker} {status}", ColorRole.StateWarning)
    };
}
