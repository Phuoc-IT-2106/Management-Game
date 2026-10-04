namespace ManagementGame.UiKit;

public sealed record BrandRoles(Rgb AccentOrganization, Rgb AccentOrganizationSecondary, Rgb OrganizationSurface,
    Rgb TextOnOrganizationAccent, bool PrimaryAdjusted, bool SecondaryAdjusted, string EmblemFallback);

/// <summary>Only presentation roles returned; raw choices and system tokens cannot be overwritten.</summary>
public static class BrandResolver
{
    public static BrandRoles Resolve(OrganizationIdentityView identity, UiTokens tokens)
    {
        var surface = tokens.Color(ColorRole.OrganizationSurface);
        var rawPrimary = identity.PrimaryColor ?? tokens.Color(ColorRole.AccentOrganization);
        var rawSecondary = identity.SecondaryColor ?? tokens.Color(ColorRole.AccentOrganizationSecondary);
        var primary = EnsureGraphic(rawPrimary, surface, tokens.Color(ColorRole.BrandTextLight));
        var secondary = EnsureGraphic(rawSecondary, surface, tokens.Color(ColorRole.BrandTextLight));
        var light = tokens.Color(ColorRole.BrandTextLight); var dark = tokens.Color(ColorRole.BrandTextDark);
        var text = Rgb.Contrast(light, primary) >= Rgb.Contrast(dark, primary) ? light : dark;
        return new(primary, secondary, surface, text, primary != rawPrimary, secondary != rawSecondary,
            string.IsNullOrWhiteSpace(identity.ShortName) ? "[CO]" : identity.ShortName);
    }
    private static Rgb EnsureGraphic(Rgb raw, Rgb surface, Rgb light)
    {
        if (Rgb.Contrast(raw, surface) >= UiTokens.GraphicContrast) return raw;
        const int steps = 64;
        for (var i = 1; i <= steps; i++)
        {
            var candidate = raw.Mix(light, i / (double)steps);
            if (Rgb.Contrast(candidate, surface) >= UiTokens.GraphicContrast) return candidate;
        }
        return light;
    }
}
