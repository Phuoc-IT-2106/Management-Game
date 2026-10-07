using System.Collections.Immutable;
using System.Globalization;

namespace ManagementGame.UiKit;

public enum ColorRole
{
    SurfaceBase, SurfaceRaised, SurfaceInset, SurfaceOverlay,
    TextPrimary, TextSecondary, TextMuted, TextOnAccent,
    StatePositive, StateWarning, StateCritical, StateNeutral, SystemError, FocusRing,
    InformationKnown, InformationEstimated, InformationUnknown,
    AccentOrganization, AccentOrganizationSecondary, OrganizationSurface,
    Border, HoverSurface, SelectedSurface, BrandTextLight, BrandTextDark,
    ActionPrimary, ActionPrimaryHover, TextOnAction, Divider
}
// Sizes follow one 7-step scale: 12, 14, 16, 20, 24, 32, 48 (no one-off sizes). Caption and Display extend the scale ends.
public enum TypographyRole { CompanyIdentity, WorkspaceTitle, SectionTitle, Body, Data, Label, Annotation, Caption, Display, Strong }
public enum SpaceRole { SpaceInline, SpaceRelated, SpaceGroup, SpaceSection, SpaceWorkspace }
public enum SizeRole { ControlCompact, ControlDefault, ControlComfortable, InspectorWidth, ReadingMeasure }
public enum MotionRole { Acknowledge, ContextTransition, ChangeHighlight }
public enum UiDensity { Compact, Default, Comfortable }

/// <summary>Opaque sRGB presentation color; no engine or campaign dependency.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Hex(string value)
    {
        if (value.Length != 6 || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            throw new ArgumentException("Expected six hexadecimal sRGB digits.", nameof(value));
        return new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
    public string ToHex() => $"{R:X2}{G:X2}{B:X2}";
    public double Luminance
    {
        get
        {
            static double Linear(byte channel) { var c = channel / 255d; return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
            return .2126 * Linear(R) + .7152 * Linear(G) + .0722 * Linear(B);
        }
    }
    public static double Contrast(Rgb a, Rgb b) => (Math.Max(a.Luminance, b.Luminance) + .05) / (Math.Min(a.Luminance, b.Luminance) + .05);
    public Rgb Mix(Rgb other, double amount) => new(
        (byte)Math.Round(R + (other.R - R) * amount), (byte)Math.Round(G + (other.G - G) * amount), (byte)Math.Round(B + (other.B - B) * amount));
}

/// <summary>Single replaceable aesthetic source. Values are development choices, not final art direction.</summary>
public sealed class UiTokens
{
    public const string Version = "operations-room-v3";
    public const string Notice = "PROVISIONAL DEVELOPMENT THEME";
    public const double TextContrast = 4.5;
    public const double GraphicContrast = 3.0;
    public const int BorderWidth = 1, FocusWidth = 2, SelectionWidth = 4, CornerRadius = 4;
    public const int IconWidth = 44, MinimumColumnWidth = 330, ActionWidth = 160;
    public const int MaxSpecimenRows = 12, MaxDocumentSections = 12, MaxNavigationDepth = 6, MaxTrackDays = 62;
    public const int CaptureSettleFrames = 12, LayoutTolerance = 2;
    public static readonly string[] UiFontNames = ["Segoe UI", "Noto Sans", "Arial"];
    // Display and numeric faces share a condensed signage family with tabular figures; never a code face.
    public static readonly string[] DisplayFontNames = ["Bahnschrift", "Segoe UI Semibold", "Segoe UI", "Noto Sans", "Arial"];
    public static readonly string[] NumericFontNames = ["Bahnschrift", "Segoe UI", "Noto Sans", "Arial"];
    public const int DisplayWeight = 600;
    /// <summary>Process-wide player preference; tokens built without an explicit choice follow it.</summary>
    public static bool PreferReducedMotion { get; set; }
    public static readonly ImmutableDictionary<ColorRole, Rgb> Colors = new Dictionary<ColorRole, string>
    {
        // Deep navy operations room (approved Portal reference): low-contrast surface tiers, near-white text. Secondary and
        // muted text are two distinct steps so primary / secondary / tertiary read apart (both stay above 4.5:1 everywhere).
        [ColorRole.SurfaceBase] = "0C121B", [ColorRole.SurfaceRaised] = "121A25",
        [ColorRole.SurfaceInset] = "0F1620", [ColorRole.SurfaceOverlay] = "1A2431",
        [ColorRole.TextPrimary] = "EEF1F5", [ColorRole.TextSecondary] = "B4BFCC",
        [ColorRole.TextMuted] = "8E9BAC", [ColorRole.TextOnAccent] = "101419",
        [ColorRole.StatePositive] = "4FD1B5", [ColorRole.StateWarning] = "F2C46D",
        [ColorRole.StateCritical] = "F07A72", [ColorRole.StateNeutral] = "C5CCD5",
        [ColorRole.SystemError] = "FFB0D1", [ColorRole.FocusRing] = "C2D9FF",
        [ColorRole.InformationKnown] = "C5CCD5", [ColorRole.InformationEstimated] = "E2CC9A",
        [ColorRole.InformationUnknown] = "BFC4D0", [ColorRole.AccentOrganization] = "BAC4CF",
        [ColorRole.AccentOrganizationSecondary] = "AAB6C4", [ColorRole.OrganizationSurface] = "0F1620",
        [ColorRole.Border] = "6B809A", [ColorRole.HoverSurface] = "1C2734",
        [ColorRole.SelectedSurface] = "24303F", [ColorRole.BrandTextLight] = "FFFFFF", [ColorRole.BrandTextDark] = "000000",
        [ColorRole.ActionPrimary] = "E0AE4A", [ColorRole.ActionPrimaryHover] = "EDBF63",
        [ColorRole.TextOnAction] = "1A1405", [ColorRole.Divider] = "223042"
    }.ToImmutableDictionary(k => k.Key, v => Rgb.Hex(v.Value));

    private static readonly ImmutableDictionary<TypographyRole, int> Fonts = new Dictionary<TypographyRole, int>
    {
        [TypographyRole.CompanyIdentity] = 24, [TypographyRole.WorkspaceTitle] = 32,
        [TypographyRole.SectionTitle] = 20, [TypographyRole.Body] = 16, [TypographyRole.Data] = 20,
        [TypographyRole.Label] = 16, [TypographyRole.Annotation] = 14, [TypographyRole.Caption] = 12, [TypographyRole.Display] = 48, [TypographyRole.Strong] = 16
    }.ToImmutableDictionary();
    public UiDensity Density { get; }
    public double TextScale { get; }
    private readonly bool? reducedMotion;
    /// <summary>Explicit choice, else the live process preference (a settings change applies without rebuilding).</summary>
    public bool ReducedMotion => reducedMotion ?? PreferReducedMotion;
    public UiTokens(UiDensity density = UiDensity.Default, double textScale = 1, bool? reducedMotion = null)
    {
        if (!Enum.IsDefined(density) || !double.IsFinite(textScale) || textScale < 1 || textScale > 1.5)
            throw new ArgumentOutOfRangeException(nameof(textScale), "Supported lab text scale is 1–1.5 and density must be defined.");
        Density = density; TextScale = textScale; this.reducedMotion = reducedMotion;
    }
    public Rgb Color(ColorRole role) => Colors[role];
    public int FontSize(TypographyRole role) => (int)Math.Round(Fonts[role] * TextScale);
    public int ActionMinimumWidth => (int)Math.Ceiling(ActionWidth * TextScale);
    public int Space(SpaceRole role)
    {
        var value = role switch { SpaceRole.SpaceInline => 4, SpaceRole.SpaceRelated => 8, SpaceRole.SpaceGroup => 12, SpaceRole.SpaceSection => 20, SpaceRole.SpaceWorkspace => 24, _ => throw new ArgumentOutOfRangeException(nameof(role)) };
        var factor = Density switch { UiDensity.Compact => .75, UiDensity.Comfortable => 1.25, _ => 1 };
        return (int)Math.Round(value * factor);
    }
    public int Size(SizeRole role) => role switch
    {
        SizeRole.ControlCompact => 32, SizeRole.ControlDefault => 40, SizeRole.ControlComfortable => 48,
        SizeRole.InspectorWidth => 380, SizeRole.ReadingMeasure => 760, _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    public int ControlHeight => (int)Math.Ceiling(Size(Density switch { UiDensity.Compact => SizeRole.ControlCompact, UiDensity.Comfortable => SizeRole.ControlComfortable, _ => SizeRole.ControlDefault }) * TextScale);
    public double Duration(MotionRole role) => ReducedMotion ? 0 : role switch
    {
        MotionRole.Acknowledge => .08, MotionRole.ContextTransition => .16, MotionRole.ChangeHighlight => .7,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
