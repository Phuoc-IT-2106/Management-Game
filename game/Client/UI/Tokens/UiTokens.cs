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
    Border, HoverSurface, SelectedSurface, BrandTextLight, BrandTextDark
}
public enum TypographyRole { CompanyIdentity, WorkspaceTitle, SectionTitle, Body, Data, Label, Annotation }
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
    public const string Version = "engineering-neutral-v1";
    public const string Notice = "PROVISIONAL DEVELOPMENT THEME";
    public const double TextContrast = 4.5;
    public const double GraphicContrast = 3.0;
    public const int BorderWidth = 1, FocusWidth = 2, CornerRadius = 4;
    public const int IconWidth = 44, MinimumColumnWidth = 330, ActionWidth = 160;
    public const int MaxSpecimenRows = 12, MaxDocumentSections = 12, MaxNavigationDepth = 6;
    public const int CaptureSettleFrames = 12, LayoutTolerance = 2;
    public static readonly string[] UiFontNames = ["Segoe UI", "Noto Sans", "Arial"];
    public static readonly string[] NumericFontNames = ["Consolas", "Noto Sans Mono", "Courier New"];
    public static readonly ImmutableDictionary<ColorRole, Rgb> Colors = new Dictionary<ColorRole, string>
    {
        [ColorRole.SurfaceBase] = "171A1F", [ColorRole.SurfaceRaised] = "242930",
        [ColorRole.SurfaceInset] = "1C2026", [ColorRole.SurfaceOverlay] = "303640",
        [ColorRole.TextPrimary] = "F2F4F7", [ColorRole.TextSecondary] = "CCD2DA",
        [ColorRole.TextMuted] = "ACB5C1", [ColorRole.TextOnAccent] = "101419",
        [ColorRole.StatePositive] = "A8DBB7", [ColorRole.StateWarning] = "F3CE83",
        [ColorRole.StateCritical] = "FFB0AA", [ColorRole.StateNeutral] = "CCD2DA",
        [ColorRole.SystemError] = "FFB0D1", [ColorRole.FocusRing] = "C2D9FF",
        [ColorRole.InformationKnown] = "CCD2DA", [ColorRole.InformationEstimated] = "E2CC9A",
        [ColorRole.InformationUnknown] = "BFC4D0", [ColorRole.AccentOrganization] = "BAC4CF",
        [ColorRole.AccentOrganizationSecondary] = "AAB6C4", [ColorRole.OrganizationSurface] = "1C2026",
        [ColorRole.Border] = "919DAD", [ColorRole.HoverSurface] = "343C47",
        [ColorRole.SelectedSurface] = "364352", [ColorRole.BrandTextLight] = "FFFFFF", [ColorRole.BrandTextDark] = "000000"
    }.ToImmutableDictionary(k => k.Key, v => Rgb.Hex(v.Value));

    private static readonly ImmutableDictionary<TypographyRole, int> Fonts = new Dictionary<TypographyRole, int>
    {
        [TypographyRole.CompanyIdentity] = 24, [TypographyRole.WorkspaceTitle] = 28,
        [TypographyRole.SectionTitle] = 20, [TypographyRole.Body] = 17, [TypographyRole.Data] = 18,
        [TypographyRole.Label] = 16, [TypographyRole.Annotation] = 14
    }.ToImmutableDictionary();
    public UiDensity Density { get; }
    public double TextScale { get; }
    public bool ReducedMotion { get; }
    public UiTokens(UiDensity density = UiDensity.Default, double textScale = 1, bool reducedMotion = true)
    {
        if (!Enum.IsDefined(density) || !double.IsFinite(textScale) || textScale < 1 || textScale > 1.5)
            throw new ArgumentOutOfRangeException(nameof(textScale), "Supported lab text scale is 1–1.5 and density must be defined.");
        Density = density; TextScale = textScale; ReducedMotion = reducedMotion;
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
        MotionRole.Acknowledge => .08, MotionRole.ContextTransition => .12, MotionRole.ChangeHighlight => .18,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
