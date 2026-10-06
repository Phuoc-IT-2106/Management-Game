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
        $"{PresentationText.StatusMarker(status)} {status}{(reason.Length > 0 ? ": " + reason : "")}", TypographyRole.Label, Tone(status));
}

/// <summary>Focusable entity identity. Rebind replaces one intent callback; labels never act as keys.</summary>
public partial class EntityLabel : Button
{
    public IntentBinding Binding { get; } = new();
    /// <summary>Unselected theme variation: a flat "Entity" row by default, "PrimaryAction" for a decision.</summary>
    public string Variation { get; set; } = "Entity";
    public EntityLabel()
    {
        Pressed += OnActivate; FocusMode = FocusModeEnum.All; AutowrapMode = TextServer.AutowrapMode.WordSmart;
        SizeFlagsHorizontal = SizeFlags.ExpandFill; Alignment = HorizontalAlignment.Left;
    }
    private static string Caption(EntityView entity)
    {
        // Kind is carried by role wording and placement; no code prefix before every label (#20).
        var text = entity.Role.Length == 0 ? entity.Name : $"{entity.Name} · {entity.Role}";
        return entity.Availability == Availability.Ready ? text : text + " · " + PresentationText.AvailabilityText(entity.Availability, entity.Reason);
    }
    public void Bind(UiContext ui, EntityView entity, Action<UiIntent>? inspect)
    {
        Binding.Bind(entity, inspect);
        SetPressedNoSignal(false); ToggleMode = false; ThemeTypeVariation = Variation;
        Text = Caption(entity);
        TooltipText = ""; SetMeta("entity_id", entity.Id); SetMeta("revision", entity.Revision);
        CustomMinimumSize = new Vector2(0, ui.Tokens.ControlHeight);
        Disabled = !Binding.CanActivate;
    }
    public void SetSelected(bool selected)
    {
        Binding.Selected = selected; ThemeTypeVariation = selected ? "SelectedEntity" : Variation;
        // Selection is carried by the SelectedEntity edge bar (shape + surface), not repeated label text.
        if (Binding.Current is { } entity) Text = Caption(entity);
    }
    private void OnActivate() => Binding.Activate("inspect");
    public override void _ExitTree() => Binding.Clear();
}
public static class ResourceValue
{
    /// <summary>Numeric face only for the amount; information state and context read as prose.</summary>
    public static VBoxContainer Create(UiContext ui, ResourceView value)
    {
        var stack = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        stack.AddThemeConstantOverride("separation", ui.Tokens.Space(SpaceRole.SpaceInline));
        stack.AddChild(SemanticText.Create(ui, PresentationText.ResourceAmount(value), TypographyRole.Data,
            value.Information == InformationState.Estimated ? ColorRole.InformationEstimated : value.Information == InformationState.Unknown ? ColorRole.InformationUnknown : ColorRole.TextPrimary));
        stack.AddChild(SemanticText.Create(ui, PresentationText.ResourceEvidence(value), TypographyRole.Label,
            value.Information == InformationState.Estimated ? ColorRole.InformationEstimated : value.Information == InformationState.Unknown ? ColorRole.InformationUnknown : ColorRole.TextSecondary));
        return stack;
    }
}
public static class TimeMarker
{
    public static Label Create(UiContext ui, int? day, string meaning) => SemanticText.Create(ui, PresentationText.Time(day, meaning), TypographyRole.Label, ColorRole.TextSecondary);
}
public static class ConfidenceIndicator
{
    public static Label Create(UiContext ui, InformationState state, string evidence) => SemanticText.Create(ui,
        $"{PresentationText.InformationMarker(state)} {PresentationText.Information(state)} · {evidence}", TypographyRole.Label,
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
        message.Length == 0 ? "No validation issues." : $"{(error ? PresentationText.CriticalMarker : PresentationText.NeutralMarker)} {message}", TypographyRole.Body,
        error && message.Length > 0 ? ColorRole.SystemError : ColorRole.TextSecondary);
}
