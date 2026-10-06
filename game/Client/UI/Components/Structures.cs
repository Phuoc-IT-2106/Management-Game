using Godot;

namespace ManagementGame.UiKit;

public static class NavigationContext
{
    /// <summary>Optional emblem is already resolved by the presentation host, never a filesystem load here.</summary>
    public static PanelContainer Create(UiContext ui, OrganizationIdentityView organization, IReadOnlyList<EntityView> path,
        Action<UiIntent>? navigate, Texture2D? emblem = null)
    {
        if (path.Count > UiTokens.MaxNavigationDepth) throw new ArgumentException("Navigation path exceeds the bounded presentation depth.", nameof(path));
        var brand = BrandResolver.Resolve(organization, ui.Tokens);
        var stack = ui.Stack();
        stack.AddChild(SemanticText.Create(ui, string.IsNullOrWhiteSpace(organization.DisplayName) ? "Player company · identity unavailable" : organization.DisplayName, TypographyRole.CompanyIdentity));
        var identity = ui.Flow(); stack.AddChild(identity);
        if (emblem is not null)
            identity.AddChild(new TextureRect { Texture = emblem, CustomMinimumSize = new Vector2(UiTokens.IconWidth, UiTokens.IconWidth),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered });
        var shortLabel = SemanticText.Create(ui, brand.EmblemFallback, TypographyRole.Label);
        shortLabel.AddThemeColorOverride("font_color", UiTheme.ToGodot(brand.TextOnOrganizationAccent));
        var swatch = ui.Panel(shortLabel, ColorRole.SurfaceInset);
        swatch.CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth, 0);
        var style = UiTheme.Box(ui.Tokens, ColorRole.SurfaceInset);
        style.BgColor = UiTheme.ToGodot(brand.AccentOrganization);
        style.BorderColor = UiTheme.ToGodot(brand.AccentOrganizationSecondary);
        swatch.AddThemeStyleboxOverride("panel", style); identity.AddChild(swatch);
        // Missing emblem falls back to the text swatch alone; fixture status is not a warning state.
        if (organization.IsDevelopmentFixture) stack.AddChild(SemanticText.Create(ui, PresentationText.FixtureNotice, TypographyRole.Annotation, ColorRole.TextMuted));
        var route = ui.Flow(); stack.AddChild(route);
        foreach (var entity in path)
        {
            var button = new EntityLabel(); button.Bind(ui, entity, navigate);
            button.CustomMinimumSize = new Vector2(ui.Tokens.ActionMinimumWidth, ui.Tokens.ControlHeight); route.AddChild(button);
        }
        if (path.Count == 0) stack.AddChild(SemanticText.Create(ui, "Company context", TypographyRole.Label));
        var panel = ui.Panel(stack, ColorRole.SurfaceInset);
        var organizationStyle = UiTheme.Box(ui.Tokens, ColorRole.SurfaceInset);
        organizationStyle.BgColor = UiTheme.ToGodot(brand.OrganizationSurface);
        panel.AddThemeStyleboxOverride("panel", organizationStyle); return panel;
    }
}

public partial class EntityRow : VBoxContainer
{
    public EntityLabel Identity { get; } = new();
    private readonly Label status = new(), information = new();
    public EntityRow()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; AddChild(Identity); AddChild(status); AddChild(information);
        foreach (var label in new[] { status, information }) { label.AutowrapMode = TextServer.AutowrapMode.WordSmart; label.ThemeTypeVariation = TypographyRole.Label.ToString(); }
    }
    public void Bind(UiContext ui, EntityView entity, Action<UiIntent>? inspect)
    {
        Identity.Bind(ui, entity, inspect);
        var availability = PresentationText.AvailabilityText(entity.Availability, entity.Reason);
        status.Text = entity.Status + (availability.Length == 0 ? "" : " · " + availability);
        ui.Tone(status, StatusIndicator.Tone(entity.Status));
        information.Text = $"{PresentationText.InformationMarker(entity.Information)} {PresentationText.Information(entity.Information)}";
        ui.Tone(information, entity.Information == InformationState.Estimated ? ColorRole.InformationEstimated : entity.Information == InformationState.Unknown ? ColorRole.InformationUnknown : ColorRole.InformationKnown);
    }
}
public static class AlertItem
{
    public static PanelContainer Create(UiContext ui, MatterView matter, Action<UiIntent>? open)
    {
        var stack = ui.Stack(); var entity = new EntityLabel(); entity.Bind(ui, matter.Entity, open); stack.AddChild(entity);
        stack.AddChild(StatusIndicator.Create(ui, matter.Entity.Status, matter.Class.ToString()));
        stack.AddChild(SemanticText.Create(ui, matter.Why)); stack.AddChild(TimeMarker.Create(ui, matter.DueDay, "Deadline"));
        stack.AddChild(SemanticText.Create(ui, "If ignored: " + matter.Consequence, TypographyRole.Label, ColorRole.TextSecondary));
        return ui.Panel(stack);
    }
}
public static class TimelineEvent
{
    public static PanelContainer Create(UiContext ui, MatterView matter, Action<UiIntent>? open)
    {
        var stack = ui.Stack(); stack.AddChild(TimeMarker.Create(ui, matter.DueDay, matter.Class.ToString()));
        var entity = new EntityLabel(); entity.Bind(ui, matter.Entity, open); stack.AddChild(entity);
        stack.AddChild(SemanticText.Create(ui, matter.Why)); stack.AddChild(SemanticText.Create(ui, matter.Consequence, TypographyRole.Label, ColorRole.TextSecondary));
        return ui.Panel(stack, ColorRole.SurfaceInset);
    }
}
public static class ComparisonView
{
    public static VBoxContainer Create(UiContext ui, ComparisonData data)
    {
        if (data.Lines.Length > UiTokens.MaxSpecimenRows) throw new ArgumentException("Comparison must be bounded; paginate in the host.", nameof(data));
        var stack = ui.Stack(); stack.AddChild(SectionHeader.Create(ui, "Comparison"));
        if (data.Availability != Availability.Ready || data.Lines.IsEmpty)
        { stack.AddChild(SemanticText.Create(ui, PresentationText.AvailabilityText(data.Lines.IsEmpty && data.Availability == Availability.Ready ? Availability.Empty : data.Availability, "Comparison records"))); return stack; }
        foreach (var line in data.Lines)
        {
            var section = ui.Stack(); section.AddChild(SemanticText.Create(ui, line.Dimension, TypographyRole.Label));
            section.AddChild(SemanticText.Create(ui, $"{data.LeftTitle}: {line.Left}", TypographyRole.Body));
            section.AddChild(SemanticText.Create(ui, $"{data.RightTitle}: {line.Right}", TypographyRole.Body));
            stack.AddChild(ui.Panel(section, ColorRole.SurfaceInset));
        }
        return stack;
    }
}
public static class DocumentView
{
    public static PanelContainer Create(UiContext ui, DocumentData data)
    {
        if (data.Sections.Length > UiTokens.MaxDocumentSections) throw new ArgumentException("Document must be bounded; section in the host.", nameof(data));
        var stack = ui.Stack(); stack.AddChild(SectionHeader.Create(ui, data.Title, data.Provenance));
        if (data.Availability != Availability.Ready || data.Sections.IsEmpty)
            stack.AddChild(SemanticText.Create(ui, PresentationText.AvailabilityText(data.Sections.IsEmpty && data.Availability == Availability.Ready ? Availability.Empty : data.Availability, "Document content")));
        else foreach (var section in data.Sections)
            stack.AddChild(SemanticText.Create(ui, $"{section.Label}: {section.Value}"));
        var panel = ui.Panel(stack); panel.SetMeta("document_id", data.Id); panel.SetMeta("revision", data.Revision); return panel;
    }
}

public partial class CommitmentReview : VBoxContainer
{
    public EntityLabel ActionButton { get; } = new();
    private readonly Label summary = new(), tradeOff = new(), result = new();
    private ActionPhase phase;
    public CommitmentReview()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        ActionButton.Variation = "PrimaryAction"; ActionButton.Alignment = HorizontalAlignment.Center;
        foreach (var label in new[] { summary, tradeOff, result }) { label.AutowrapMode = TextServer.AutowrapMode.WordSmart; label.ThemeTypeVariation = TypographyRole.Body.ToString(); AddChild(label); }
        AddChild(ActionButton);
    }
    public void Bind(UiContext ui, CommitmentData data, Action<UiIntent>? commit)
    {
        if (ActionButton.Binding.Current is { } current && current.Id == data.Id && data.Revision < current.Revision)
            throw new ArgumentException("Cannot bind an older commitment revision.", nameof(data));
        // Rebind after refresh is the explicit fresh-review path; rejection never auto-retries.
        phase = data.Phase; summary.Text = data.Summary; tradeOff.Text = "Trade-off: " + data.TradeOff;
        result.Text = $"{data.Phase}: {data.Result}";
        ui.Tone(summary, ColorRole.TextPrimary); ui.Tone(tradeOff, ColorRole.TextSecondary);
        ui.Tone(result, data.Phase == ActionPhase.Rejected ? ColorRole.SystemError : ColorRole.TextSecondary);
        var available = data.Phase == ActionPhase.Ready ? Availability.Ready : data.Phase == ActionPhase.Pending ? Availability.Loading : Availability.Unavailable;
        ActionButton.Bind(ui, new EntityView(data.Id, "Confirm commitment", EntityKind.Decision, "", data.Revision, Availability: available, Reason: data.Result),
            commit is null ? null : intent =>
            {
                if (phase != ActionPhase.Ready) return;
                phase = ActionPhase.Pending; ActionButton.Disabled = true;
                result.Text = "Pending: awaiting presenter response.";
                commit(intent with { Kind = "commit" });
            });
    }
}
