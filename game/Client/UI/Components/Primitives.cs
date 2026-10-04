using Godot;

namespace ManagementGame.UiKit;

public static class SemanticText
{
    public static Label Create(UiContext ui, string text, TypographyRole role = TypographyRole.Body, ColorRole tone = ColorRole.TextPrimary)
    {
        var label = new Label { Text = text, ThemeTypeVariation = role.ToString(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        ui.Tone(label, tone); return label;
    }
}
public static class IconPresentation
{
    public static Label Create(UiContext ui, string key)
    {
        var label = SemanticText.Create(ui, $"{PresentationText.Icon(key)} {key}", TypographyRole.Label, ColorRole.TextSecondary);
        label.CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth, 0); return label;
    }
}
public static class StatusIndicator
{
    public static ColorRole Tone(DomainStatus status) => status switch
    {
        DomainStatus.Positive => ColorRole.StatePositive, DomainStatus.Warning => ColorRole.StateWarning,
        DomainStatus.Critical => ColorRole.StateCritical, _ => ColorRole.StateNeutral
    };
    public static Label Create(UiContext ui, DomainStatus status, string reason = "") => SemanticText.Create(ui,
        $"{(status is DomainStatus.Warning or DomainStatus.Critical ? "[!]" : "[·]")} {status}{(reason.Length > 0 ? ": " + reason : "")}", TypographyRole.Label, Tone(status));
}

/// <summary>Focusable entity identity. Rebind replaces one intent callback; labels never act as keys.</summary>
public partial class EntityLabel : Button
{
    public IntentBinding Binding { get; } = new();
    public EntityLabel() { Pressed += OnActivate; FocusMode = FocusModeEnum.All; AutowrapMode = TextServer.AutowrapMode.WordSmart; SizeFlagsHorizontal = SizeFlags.ExpandFill; }
    public void Bind(UiContext ui, EntityView entity, Action<UiIntent>? inspect)
    {
        Binding.Bind(entity, inspect);
        SetPressedNoSignal(false); ToggleMode = false; ThemeTypeVariation = "";
        Text = $"{PresentationText.Icon(entity.Kind.ToString())} {entity.Name} · {entity.Role}";
        if (entity.Availability != Availability.Ready) Text += " · " + PresentationText.AvailabilityText(entity.Availability, entity.Reason);
        TooltipText = ""; SetMeta("entity_id", entity.Id); SetMeta("revision", entity.Revision);
        CustomMinimumSize = new Vector2(0, ui.Tokens.ControlHeight);
        Disabled = !Binding.CanActivate;
    }
    public void SetSelected(bool selected)
    {
        Binding.Selected = selected; ThemeTypeVariation = selected ? "SelectedEntity" : "";
        if (Binding.Current is not { } entity) return;
        Text = $"{(selected ? "[Selected] " : "")}{PresentationText.Icon(entity.Kind.ToString())} {entity.Name} · {entity.Role}";
        if (entity.Availability != Availability.Ready) Text += " · " + PresentationText.AvailabilityText(entity.Availability, entity.Reason);
    }
    private void OnActivate() => Binding.Activate("inspect");
    public override void _ExitTree() => Binding.Clear();
}
public static class ResourceValue
{
    public static Label Create(UiContext ui, ResourceView value) => SemanticText.Create(ui, PresentationText.Resource(value), TypographyRole.Data,
        value.Information == InformationState.Estimated ? ColorRole.InformationEstimated : value.Information == InformationState.Unknown ? ColorRole.InformationUnknown : ColorRole.TextPrimary);
}
public static class TimeMarker
{
    public static Label Create(UiContext ui, int? day, string meaning) => SemanticText.Create(ui, PresentationText.Time(day, meaning), TypographyRole.Label, ColorRole.TextSecondary);
}
public static class ConfidenceIndicator
{
    public static Label Create(UiContext ui, InformationState state, string evidence) => SemanticText.Create(ui,
        $"{(state == InformationState.Estimated ? "[~]" : state == InformationState.Unknown ? "[?]" : "[=]")} {PresentationText.Information(state)} · {evidence}", TypographyRole.Label,
        state == InformationState.Estimated ? ColorRole.InformationEstimated : state == InformationState.Unknown ? ColorRole.InformationUnknown : ColorRole.InformationKnown);
}
public static class StatusBadge
{
    public static PanelContainer Create(UiContext ui, DomainStatus status, string reason = "")
    {
        var panel = ui.Panel(StatusIndicator.Create(ui, status, reason), ColorRole.SurfaceInset);
        panel.CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth, 0); return panel;
    }
}
public static class SectionHeader
{
    public static VBoxContainer Create(UiContext ui, string title, string description = "")
    {
        var stack = ui.Stack(); stack.AddChild(SemanticText.Create(ui, title, TypographyRole.SectionTitle));
        if (description.Length > 0) stack.AddChild(SemanticText.Create(ui, description, TypographyRole.Annotation, ColorRole.TextSecondary));
        return stack;
    }
}
public static class ValidationMessage
{
    public static Label Create(UiContext ui, string message, bool error = true) => SemanticText.Create(ui,
        message.Length == 0 ? "No validation issues." : $"{(error ? "[!]" : "[i]")} {message}", TypographyRole.Body,
        error && message.Length > 0 ? ColorRole.SystemError : ColorRole.TextSecondary);
}
