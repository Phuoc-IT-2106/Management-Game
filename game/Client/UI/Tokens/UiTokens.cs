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
    ActionPrimary, ActionPrimaryHover, TextOnAction, Divider,
    // Material strokes (graphite / steel / brass surface system): the dark outer frame, the cool steel inner stroke, the
    // top-edge highlight, the deep shade of a gradient's lower edge and a muted brass for structural accents.
    FrameOuter, FrameStroke, FrameHighlight, SurfaceShade, AccentMuted, HeaderSurface
}
// Sizes follow one 7-step scale: 12, 14, 16, 20, 24, 32, 48 (no one-off sizes). Caption and Display extend the scale ends.
// The metric roles type a status-bar fact: a small caps label, a prominent value (StatusValue when the value is a word),
// and subordinate context after the value.
public enum TypographyRole { CompanyIdentity, WorkspaceTitle, SectionTitle, Body, Data, Label, Annotation, Caption, Display, Strong,
    MetricLabel, MetricValue, StatusValue, MetricContext }
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
    public const string Version = "graphite-brass-v4";
    public const string Notice = "PROVISIONAL DEVELOPMENT THEME";
    public const double TextContrast = 4.5;
    public const double GraphicContrast = 3.0;
    /// <summary>Flat boxes (selection bars, square focus rings, plain surfaces) are square: framed elements take 45° cuts
    /// (polish standard §3), so a radius would be a second corner language.</summary>
    public const int BorderWidth = 1, FocusWidth = 2, SelectionWidth = 4, CornerRadius = 0;
    /// <summary>Smallest click target for compact controls (filters, header routes): 24 × 24 px (WCAG 2.2 target size).</summary>
    public const int MinimumTarget = 24;
    /// <summary>Frame geometry (structural chrome). Chamfer legs in pixels, all cut at 45°: Control for buttons, filters and
    /// rows; Small for nested plates, icon housings and the rail selection; Medium for standard regions; Large for premium
    /// regions. Edge thicknesses: a hairline, a control rim and a region's structural wall.</summary>
    public const int ChamferControl = 4, ChamferSmall = 6, ChamferMedium = 11, ChamferLarge = 16;
    public const int EdgeHairline = 1, EdgeRim = 2, EdgeWall = 3;
    /// <summary>A region's outer chamfer by importance: premium regions carry the larger cut.</summary>
    public static int RegionChamfer(FrameTier tier) => tier == FrameTier.Premium ? ChamferLarge : ChamferMedium;
    /// <summary>Brass corner inserts on a region's chamfer facets: premium two (top-left, bottom-right), standard one
    /// (top-left), utility none. Brass marks structural junctions; graphite and steel carry the frame.</summary>
    public static int BrassInserts(FrameTier tier) => tier switch { FrameTier.Premium => 2, FrameTier.Standard => 1, _ => 0 };
    public const int IconWidth = 44, MinimumColumnWidth = 330, ActionWidth = 160;
    public const int MaxSpecimenRows = 12, MaxDocumentSections = 12, MaxNavigationDepth = 6, MaxTrackDays = 62;
    public const int CaptureSettleFrames = 12, LayoutTolerance = 2;
    /// <summary>One bundled family for every role (Inter 4.1, SIL OFL 1.1, Assets/Fonts/Inter): a screen face with a tall
    /// x-height and tabular figures. Three weights carry the whole hierarchy; nothing picks a weight of its own.</summary>
    public const string FontFamily = "Inter";
    public const int WeightRegular = 400, WeightMedium = 500, WeightSemibold = 600;
    public static readonly ImmutableArray<int> FontWeights = [WeightRegular, WeightMedium, WeightSemibold];
    public static string FontPath(int weight) => "res://Assets/Fonts/Inter/Inter-" + weight switch
    {
        WeightRegular => "Regular", WeightMedium => "Medium", WeightSemibold => "SemiBold",
        _ => throw new ArgumentOutOfRangeException(nameof(weight), "Only the three system weights are bundled.")
    } + ".ttf";
    /// <summary>System faces behind the bundled family: glyphs it lacks (◐, ⌂ …) and a missing import fall back to these.</summary>
    public static readonly string[] UiFontNames = ["Segoe UI", "Noto Sans", "Arial"];
    public static readonly string[] SymbolFontNames = ["Segoe UI Symbol", "Segoe UI", "Noto Sans Symbols 2", "Noto Sans Symbols", "Noto Sans"];
    public const int DisplayWeight = WeightSemibold;
    /// <summary>Weight by role: regular for reading text and context, medium for labels and data figures, semibold for
    /// values, titles, emphasis and actions.</summary>
    public static int Weight(TypographyRole role) => role switch
    {
        TypographyRole.Body or TypographyRole.Label or TypographyRole.Annotation or TypographyRole.Caption or TypographyRole.MetricContext => WeightRegular,
        TypographyRole.Data or TypographyRole.MetricLabel => WeightMedium,
        _ => WeightSemibold
    };
    /// <summary>Roles that show changing figures use tabular numerals, so values that update do not shift sideways.</summary>
    public static bool Tabular(TypographyRole role) => role is not (TypographyRole.Body or TypographyRole.Label or TypographyRole.Annotation or TypographyRole.Caption);
    /// <summary>Metric labels are caps with one pixel of tracking; all other roles keep their natural spacing.</summary>
    public static int Tracking(TypographyRole role) => role == TypographyRole.MetricLabel ? 1 : 0;
    /// <summary>Process-wide player preference; tokens built without an explicit choice follow it.</summary>
    public static bool PreferReducedMotion { get; set; }
    public static readonly ImmutableDictionary<ColorRole, Rgb> Colors = new Dictionary<ColorRole, string>
    {
        // Graphite / steel / brass (approved Portal reference 11_portal_world_transfer_graphite_mockup): charcoal surface tiers
        // with a cool steel cast, off-white text, muted brass reserved for the primary action and selection, teal and blue
        // as semantic accents. Secondary and muted text are two distinct steps, both above 4.5:1 on every surface.
        [ColorRole.SurfaceBase] = "0C0E11", [ColorRole.SurfaceRaised] = "1C2025",
        [ColorRole.SurfaceInset] = "121417", [ColorRole.SurfaceOverlay] = "252A31",
        [ColorRole.TextPrimary] = "EEF0F2", [ColorRole.TextSecondary] = "B6BCC4",
        [ColorRole.TextMuted] = "9CA3AD", [ColorRole.TextOnAccent] = "101419",
        [ColorRole.StatePositive] = "4FD1B5", [ColorRole.StateWarning] = "F2C46D",
        [ColorRole.StateCritical] = "F07A72", [ColorRole.StateNeutral] = "C5CCD5",
        [ColorRole.SystemError] = "FFB0D1", [ColorRole.FocusRing] = "C2D9FF",
        [ColorRole.InformationKnown] = "C5CCD5", [ColorRole.InformationEstimated] = "E2CC9A",
        [ColorRole.InformationUnknown] = "BFC4D0", [ColorRole.AccentOrganization] = "BAC4CF",
        [ColorRole.AccentOrganizationSecondary] = "AAB6C4", [ColorRole.OrganizationSurface] = "121417",
        [ColorRole.Border] = "7A838F", [ColorRole.HoverSurface] = "2E343B",
        [ColorRole.SelectedSurface] = "2B3037", [ColorRole.BrandTextLight] = "FFFFFF", [ColorRole.BrandTextDark] = "000000",
        [ColorRole.ActionPrimary] = "C9A45A", [ColorRole.ActionPrimaryHover] = "D8B56E",
        [ColorRole.TextOnAction] = "1A1405", [ColorRole.Divider] = "2C3138",
        [ColorRole.FrameOuter] = "030405", [ColorRole.FrameStroke] = "3E4550", [ColorRole.FrameHighlight] = "76808C",
        [ColorRole.SurfaceShade] = "15181C", [ColorRole.AccentMuted] = "8C7444", [ColorRole.HeaderSurface] = "282D34"
    }.ToImmutableDictionary(k => k.Key, v => Rgb.Hex(v.Value));

    private static readonly ImmutableDictionary<TypographyRole, int> Fonts = new Dictionary<TypographyRole, int>
    {
        [TypographyRole.CompanyIdentity] = 24, [TypographyRole.WorkspaceTitle] = 32,
        [TypographyRole.SectionTitle] = 20, [TypographyRole.Body] = 16, [TypographyRole.Data] = 20,
        [TypographyRole.Label] = 16, [TypographyRole.Annotation] = 14, [TypographyRole.Caption] = 12, [TypographyRole.Display] = 48, [TypographyRole.Strong] = 16,
        [TypographyRole.MetricLabel] = 12, [TypographyRole.MetricValue] = 20, [TypographyRole.StatusValue] = 16, [TypographyRole.MetricContext] = 14
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
