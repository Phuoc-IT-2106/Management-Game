using Godot;
using ManagementGame.Application;
using ManagementGame.UiKit;
using ManagementGame.Formatting;

namespace ManagementGame.Shell;

/// <summary>Money snapshot. It keeps the known / estimated / scheduled / overdue distinctions of the read model.</summary>
public static partial class PortalScreen
{
    // ---------- money ----------
    /// <summary>Current position (known cash, condition, arrears) beside the short horizon (scheduled receipts, estimated
    /// payments, the 7-day estimate). Each bar sits directly under the value it scales.</summary>
    private static Control Money(UiContext ui, PortalView m, bool roomy, Routes routes)
    {
        var f = m.Finance;
        var panel = Region(ui, "Portal_Money", "finance", "Money snapshot", roomy, out var body, "",
            Link(ui, "PortalMoneyOpen", roomy ? "Go to Finance" : "Finance", () => routes.Open(ShellSections.Finance, "")));
        var row = Row(roomy ? 20 : 14); row.SizeFlagsVertical = Control.SizeFlags.ExpandFill; body.AddChild(row);
        // Main panel → metric block (level-2 plate) → forecast (recessed well): the nesting reads at a glance.
        var block = PanelFrame.Card(ui, roomy ? 12 : 8, roomy ? 10 : 5); block.Name = "MoneyBlock"; block.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        block.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; block.SizeFlagsStretchRatio = .9f;
        var now = Stack(roomy ? 4 : 2); now.Name = "MoneyNow"; now.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; block.AddChild(now);
        if (roomy) now.AddChild(Caps(ui, "Now", ColorRole.TextMuted));
        var cash = Text(ui, Currency.Compact(f.Cash), TypographyRole.CompanyIdentity, wrap: false, size: roomy ? TypographyRole.WorkspaceTitle : TypographyRole.CompanyIdentity);
        cash.Name = "MoneyCash"; now.AddChild(cash);
        now.AddChild(Text(ui, $"Total cash, {PresentationText.InformationMarker(InformationState.Known)} known", TypographyRole.Caption, ColorRole.TextSecondary, wrap: false));
        var (condition, conditionTone) = Condition(f.Condition);
        if (roomy) now.AddChild(Text(ui, condition, TypographyRole.Strong, conditionTone, wrap: false, size: TypographyRole.Annotation));
        now.AddChild(f.Arrears > 0
            ? Text(ui, $"{PresentationText.CriticalMarker} Arrears {Currency.Format(f.Arrears)} overdue", TypographyRole.Strong, ColorRole.StateCritical, wrap: false, size: TypographyRole.Caption)
            : Text(ui, "No arrears", TypographyRole.Caption, ColorRole.TextMuted, wrap: false));
        row.AddChild(block);
        // The forecast sits in its own recessed well: what is known now stays apart from what is scheduled or estimated.
        var well = PanelFrame.Well(ui, roomy ? 12 : 7); well.Name = "MoneyForecast"; well.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        well.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var ahead = Stack(roomy ? 8 : 3); ahead.Name = "MoneyAhead"; ahead.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        if (roomy) ahead.AddChild(Caps(ui, $"Next {f.AheadDays} days", ColorRole.TextMuted));
        var reference = Math.Max(f.ReceiptsAhead, f.PaymentsAhead);
        foreach (var (label, value, tone, text) in new[]
        {
            ("Receipts, scheduled", f.ReceiptsAhead, ColorRole.StatePositive, Currency.Signed(f.ReceiptsAhead)),
            ("Payments, estimated", f.PaymentsAhead, ColorRole.StateWarning, Currency.Signed(-f.PaymentsAhead))
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
        forecast.AddChild(Text(ui, $"{(up ? "▲" : "▼")} {PresentationText.InformationMarker(InformationState.Estimated)} {Currency.Compact(f.Forecast)}", TypographyRole.Strong,
            up ? ColorRole.InformationEstimated : ColorRole.StateWarning, wrap: false, size: TypographyRole.Caption));
        ahead.AddChild(forecast);
        well.AddChild(ahead);
        row.AddChild(well);
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
