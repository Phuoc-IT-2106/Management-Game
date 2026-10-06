using ManagementGame.UiKit;
using System.Globalization;

var count = 0;
void Check(bool ok, string name) { if (!ok) { Console.Error.WriteLine("FAIL " + name); Environment.Exit(1); } count++; Console.WriteLine("PASS " + name); }
var tokens = new UiTokens();
Check(Enum.GetValues<ColorRole>().All(x => UiTokens.Colors.ContainsKey(x)), "complete semantic color catalog");
foreach (var surface in new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset, ColorRole.SurfaceOverlay, ColorRole.HoverSurface, ColorRole.SelectedSurface })
{
    foreach (var text in new[] { ColorRole.TextPrimary, ColorRole.TextSecondary, ColorRole.TextMuted, ColorRole.StatePositive, ColorRole.StateWarning, ColorRole.StateCritical, ColorRole.SystemError, ColorRole.InformationKnown, ColorRole.InformationEstimated, ColorRole.InformationUnknown })
        Check(Rgb.Contrast(tokens.Color(text), tokens.Color(surface)) >= UiTokens.TextContrast, $"text contrast {text}/{surface}");
    Check(Rgb.Contrast(tokens.Color(ColorRole.FocusRing), tokens.Color(surface)) >= UiTokens.GraphicContrast, "focus contrast " + surface);
    Check(Rgb.Contrast(tokens.Color(ColorRole.Border), tokens.Color(surface)) >= UiTokens.GraphicContrast, "essential border contrast " + surface);
}
foreach (var brand in LabFixtures.BrandKeys)
{
    var identity = LabFixtures.Organization(brand); var original = identity with { }; var roles = BrandResolver.Resolve(identity, tokens);
    Check(Rgb.Contrast(roles.TextOnOrganizationAccent, roles.AccentOrganization) >= UiTokens.TextContrast, brand + " brand text contrast");
    Check(Rgb.Contrast(roles.AccentOrganization, roles.OrganizationSurface) >= UiTokens.GraphicContrast && Rgb.Contrast(roles.AccentOrganizationSecondary, roles.OrganizationSurface) >= UiTokens.GraphicContrast, brand + " accent contrast");
    Check(identity == original && BrandResolver.Resolve(identity with { DisplayName = "FIXTURE_RENAMED" }, tokens) == roles, brand + " immutable and name-independent branding");
    Check(roles.EmblemFallback == "TEST", brand + " text emblem fallback");
}
Check(BrandResolver.Resolve(LabFixtures.Organization("dark"), tokens).PrimaryAdjusted, "dark raw color resolves safe accent");
Check(BrandResolver.Resolve(LabFixtures.Organization("low-contrast"), tokens).SecondaryAdjusted, "low-contrast secondary fallback");
Check(BrandResolver.Resolve(LabFixtures.Organization("identical"), tokens).AccentOrganization == BrandResolver.Resolve(LabFixtures.Organization("identical"), tokens).AccentOrganizationSecondary, "identical brand colors allowed without invented identity");
Check(Rgb.Contrast(Rgb.Hex("FFFFFF"), Rgb.Hex("000000")) == 21, "independent black-white contrast vector");
Check(Math.Abs(Rgb.Contrast(Rgb.Hex("777777"), Rgb.Hex("FFFFFF")) - 4.478) < .002, "mid-gray contrast reference vector");
var binding = new IntentBinding(); var oldCalls = 0; var newCalls = 0; UiIntent? received = null;
binding.Bind(LabFixtures.Person, _ => oldCalls++); binding.Selected = true;
binding.Bind(LabFixtures.Person with { Id = "person:fixture:002", Name = LabFixtures.Person.Name, Revision = 9 }, x => { newCalls++; received = x; });
binding.Activate("inspect");
Check(oldCalls == 0 && newCalls == 1 && received?.TargetId == "person:fixture:002" && received.Revision == 9 && !binding.Selected, "rebind callback reset with identical display names");
try { binding.Bind(binding.Current! with { Revision = 8 }, _ => { }); Check(false, "older projection must be rejected"); }
catch (ArgumentException) { Check(binding.Current!.Revision == 9, "stale bind preserves current target and callback"); }
foreach (var state in new[] { Availability.Empty, Availability.Loading, Availability.Unavailable, Availability.Error })
{ binding.Bind(LabFixtures.Person with { Availability = state }, _ => newCalls++); Check(!binding.Activate("inspect"), "non-ready activation suppressed: " + state); }
binding.Bind(LabFixtures.Person with { Id = "" }, _ => { }); Check(!binding.CanActivate, "missing stable ID is not actionable");
binding.Clear(); Check(!binding.CanActivate && binding.Current is null, "unbound lifetime releases target");
Check(PresentationText.Resource(new("Value", 999m, "TEST", InformationState.Unknown, "no evidence")).Contains("Unknown") && !PresentationText.Resource(new("Value", 999m, "TEST", InformationState.Unknown, "no evidence")).Contains("999"), "unknown never displays a backing number");
Check(PresentationText.Resource(new("Value", -1000m, "TEST", InformationState.Estimated, "forecast")).Contains("-1,000.00") && PresentationText.Information(InformationState.Estimated) != PresentationText.Information(InformationState.Unknown), "negative estimated value distinct from unknown");
Check(PresentationText.Time(null, "Due").Contains("unknown") && PresentationText.Icon("undefined") == "[?]", "unknown time/icon safe fallbacks");
var icons = Enum.GetValues<EntityKind>().Select(k => PresentationText.Icon(k.ToString())).ToArray();
Check(icons.Distinct().Count() == icons.Length && !icons.Contains(PresentationText.InformationMarker(InformationState.Unknown)), "entity icons distinct and never the Unknown marker");
var amount = new ResourceView("Value", 1000m, "TEST", InformationState.Estimated, "forecast");
Check(PresentationText.ResourceAmount(amount) == "Value: 1,000.00 TEST" && PresentationText.ResourceEvidence(amount).StartsWith("[~] Estimated"), "resource amount separated from information evidence");
Check(PresentationText.DayRanges([6, 1, 2, 3, 9, 10]) == "1–3, 6, 9–10" && PresentationText.DayRanges([]) == "", "day ranges compress consecutive runs");
var track = new DayTrackData("Q?", 1, 5, 10, "limit", "Now", [5, 5, 5, 5, 5], "If", [8, 12, 12, 8, 5],
    [new("Marks", [2, 4, 9], DayMarkShape.Filled)], "assumption");
var trackLines = PresentationText.DayTrackSummary(track);
Check(trackLines.SequenceEqual(["If: over limit on day 2–3 (peak 12 / 10).", "Now: within limit every day through day 5 (peak 5 / 10).", "Marks: day 2, 4."]), "day track text states every drawn fact in range");
Check(PresentationText.DayTrackSummary(track with { Proposed = [] }).Length == 2 && !PresentationText.DayTrackLegend(track with { Proposed = [] }).Contains("If"), "absent alternative omitted from summary and legend");
foreach (var invalid in new[] { track with { Current = [1, 2] }, track with { Proposed = [1] }, track with { LastDay = 0 }, track with { LastDay = UiTokens.MaxTrackDays + 1 } })
{
    try { invalid.Validate(); Check(false, "invalid day track must be rejected"); } catch (ArgumentException) { }
}
Check(true, "day track rejects mismatched or unbounded series");
foreach (var d in Enum.GetValues<UiDensity>())
{
    var t = new UiTokens(d, 1.5); Check(t.FontSize(TypographyRole.Body) > tokens.FontSize(TypographyRole.Body) && t.ControlHeight >= t.Size(SizeRole.ControlCompact), "text and density scale: " + d);
    Check(Enum.GetValues<MotionRole>().All(x => t.Duration(x) == 0), "reduced motion: " + d);
}
Check(Enum.GetValues<MotionRole>().All(x => new UiTokens(reducedMotion: false).Duration(x) > 0), "semantic optional motion durations exist");
var priorCulture = CultureInfo.CurrentCulture;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN"); var vi = PresentationText.Resource(new("Value", 1000.5m, "TEST", InformationState.Known, "fixture"));
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); var en = PresentationText.Resource(new("Value", 1000.5m, "TEST", InformationState.Known, "fixture"));
CultureInfo.CurrentCulture = priorCulture;
Check(vi == en, "capture fixture numeric formatting reproducible across host cultures");
Check(typeof(LabFixtures).Assembly.GetReferencedAssemblies().All(x => !x.Name!.StartsWith("ManagementGame.Domain", StringComparison.Ordinal) && !x.Name.StartsWith("ManagementGame.Application", StringComparison.Ordinal) && !x.Name.StartsWith("Godot", StringComparison.Ordinal)), "presentation fixture/test assembly has no gameplay or engine dependency");
Console.WriteLine($"UI_KIT_PURE_PASS {count}");
