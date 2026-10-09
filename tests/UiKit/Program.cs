using ManagementGame.UiKit;
using System.Globalization;

var count = 0;
void Check(bool ok, string name) { if (!ok) { Console.Error.WriteLine("FAIL " + name); Environment.Exit(1); } count++; Console.WriteLine("PASS " + name); }
var tokens = new UiTokens();
Check(Enum.GetValues<ColorRole>().All(x => UiTokens.Colors.ContainsKey(x)), "complete semantic color catalog");
foreach (var surface in new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset, ColorRole.SurfaceOverlay, ColorRole.HoverSurface, ColorRole.SelectedSurface, ColorRole.HeaderSurface })
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
Check(PresentationText.ResourceAmount(amount) == "Value: 1,000.00 TEST" && PresentationText.ResourceEvidence(amount).StartsWith("◐ Estimated"), "resource amount separated from information evidence");
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
Check(PresentationText.Monogram("DEV_ORG_001") == "DO" && PresentationText.Monogram("TEST") == "TE" && PresentationText.Monogram("Tổ chức phát triển") == "TC" && PresentationText.Monogram("___") == "—", "text emblem fallback is a monogram, not a type code");
var markers = Enum.GetValues<InformationState>().Select(PresentationText.InformationMarker).Concat(Enum.GetValues<DomainStatus>().Select(PresentationText.StatusMarker)).ToArray();
Check(markers.Distinct().Count() == markers.Length && markers.All(m => !m.Contains('[')), "information and status markers are distinct geometric glyphs, not bracket codes");
Check(!PresentationText.DevelopmentWatermark.Contains("Tab") && PresentationText.DevelopmentWatermark.Contains(PresentationText.FixtureNotice), "single watermark keeps fixture marker without keyboard legend");
foreach (var d in Enum.GetValues<UiDensity>())
{
    var t = new UiTokens(d, 1.5, reducedMotion: true); Check(t.FontSize(TypographyRole.Body) > tokens.FontSize(TypographyRole.Body) && t.ControlHeight >= t.Size(SizeRole.ControlCompact), "text and density scale: " + d);
    Check(Enum.GetValues<MotionRole>().All(x => t.Duration(x) == 0), "reduced motion: " + d);
}
Check(Enum.GetValues<MotionRole>().All(x => new UiTokens(reducedMotion: false).Duration(x) > 0), "semantic optional motion durations exist");
UiTokens.PreferReducedMotion = false; var live = new UiTokens();
Check(!live.ReducedMotion && live.Duration(MotionRole.ChangeHighlight) > 0, "explanatory motion is on by default");
UiTokens.PreferReducedMotion = true; Check(live.ReducedMotion && live.Duration(MotionRole.ChangeHighlight) == 0 && !new UiTokens(reducedMotion: false).ReducedMotion, "reduced-motion preference applies live; explicit choice wins");
UiTokens.PreferReducedMotion = false;
Check(Rgb.Contrast(tokens.Color(ColorRole.TextOnAction), tokens.Color(ColorRole.ActionPrimary)) >= UiTokens.TextContrast &&
    Rgb.Contrast(tokens.Color(ColorRole.TextOnAction), tokens.Color(ColorRole.ActionPrimaryHover)) >= UiTokens.TextContrast, "primary action text contrast");
Check(new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.SurfaceInset }.All(x => Rgb.Contrast(tokens.Color(ColorRole.ActionPrimary), tokens.Color(x)) >= UiTokens.GraphicContrast), "primary action boundary contrast against surfaces");
Check(new[] { ColorRole.StateWarning, ColorRole.StateCritical, ColorRole.SystemError, ColorRole.FocusRing }.All(x => tokens.Color(x) != tokens.Color(ColorRole.ActionPrimary)), "primary action never reuses a semantic state color");
var priorCulture = CultureInfo.CurrentCulture;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN"); var vi = PresentationText.Resource(new("Value", 1000.5m, "TEST", InformationState.Known, "fixture"));
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); var en = PresentationText.Resource(new("Value", 1000.5m, "TEST", InformationState.Known, "fixture"));
CultureInfo.CurrentCulture = priorCulture;
Check(vi == en, "capture fixture numeric formatting reproducible across host cultures");
Check(typeof(LabFixtures).Assembly.GetReferencedAssemblies().All(x => !x.Name!.StartsWith("ManagementGame.Domain", StringComparison.Ordinal) && !x.Name.StartsWith("ManagementGame.Application", StringComparison.Ordinal) && !x.Name.StartsWith("Godot", StringComparison.Ordinal)), "presentation fixture/test assembly has no gameplay or engine dependency");
// Material surfaces: the brass bevel darkens toward its lower edge (and further when pressed); the label stays readable on every tone.
foreach (var depth in new[] { .18, .22 })
    Check(Rgb.Contrast(tokens.Color(ColorRole.TextOnAction), tokens.Color(ColorRole.ActionPrimary).Mix(Rgb.Hex("000000"), depth)) >= UiTokens.TextContrast,
        $"primary action label contrast on the brass bevel darkened {depth:P0}");
Check(new[] { ColorRole.FrameOuter, ColorRole.FrameStroke, ColorRole.FrameHighlight, ColorRole.SurfaceShade, ColorRole.AccentMuted }.All(r => UiTokens.Colors.ContainsKey(r))
    && tokens.Color(ColorRole.FrameOuter).Luminance < tokens.Color(ColorRole.SurfaceBase).Luminance
    && tokens.Color(ColorRole.FrameStroke).Luminance > tokens.Color(ColorRole.SurfaceRaised).Luminance
    && tokens.Color(ColorRole.FrameHighlight).Luminance > tokens.Color(ColorRole.FrameStroke).Luminance, "material strokes order dark frame < surfaces < steel stroke < highlight");
// Elevation tiers must stay visibly apart: room < raised region < header band < hover lift, each a real step, and the
// recessed plates set into a region sit clearly below it (they never touch the room, so only that step matters).
var tiers = new[] { ColorRole.SurfaceBase, ColorRole.SurfaceRaised, ColorRole.HeaderSurface, ColorRole.HoverSurface };
Check(tiers.Zip(tiers.Skip(1)).All(p => tokens.Color(p.Second).Luminance > tokens.Color(p.First).Luminance && Rgb.Contrast(tokens.Color(p.First), tokens.Color(p.Second)) >= 1.1),
    "surface tiers are ordered and each step is distinct (contrast >= 1.1)");
Check(Rgb.Contrast(tokens.Color(ColorRole.SurfaceInset), tokens.Color(ColorRole.SurfaceRaised)) >= 1.1
    && tokens.Color(ColorRole.SurfaceInset).Luminance > tokens.Color(ColorRole.SurfaceBase).Luminance, "nested plates sit clearly below the region and above the room");
Check(Rgb.Contrast(tokens.Color(ColorRole.FrameHighlight), tokens.Color(ColorRole.SurfaceRaised)) >= UiTokens.GraphicContrast, "the lit steel rim stands out against the raised region");
// Structural chrome geometry (FrameGeometry): one 45° angle family, cuts by importance, outlines that stay valid at any size.
static bool Within(int value, int low, int high) => value >= low && value <= high;
Check(Within(UiTokens.ChamferSmall, 5, 7) && Within(UiTokens.ChamferMedium, 9, 12) && Within(UiTokens.ChamferLarge, 14, 18)
    && new[] { UiTokens.EdgeHairline, UiTokens.EdgeRim, UiTokens.EdgeWall }.All(e => Within(e, 1, 3)), "chamfer and edge tokens sit in the approved ranges");
Check(UiTokens.ChamferControl < UiTokens.ChamferSmall && UiTokens.ChamferSmall < UiTokens.ChamferMedium && UiTokens.ChamferMedium < UiTokens.ChamferLarge
    && UiTokens.RegionChamfer(FrameTier.Premium) > UiTokens.RegionChamfer(FrameTier.Standard), "cut size grows with importance");
Check(UiTokens.BrassInserts(FrameTier.Premium) == 2 && UiTokens.BrassInserts(FrameTier.Standard) == 1 && UiTokens.BrassInserts(FrameTier.Utility) == 0, "brass inserts by tier: 2, 1, 0");
static bool AngleFamily(System.Numerics.Vector2[] p)
{
    for (var i = 0; i < p.Length; i++)
    {
        var d = p[(i + 1) % p.Length] - p[i];
        if (d.LengthSquared() < 1e-6f) continue;
        if (!(Math.Abs(d.X) < 1e-3f || Math.Abs(d.Y) < 1e-3f || Math.Abs(Math.Abs(d.X) - Math.Abs(d.Y)) < 1e-3f)) return false;
    }
    return true;
}
var sizes = new[] { (24f, 24f), (40f, 18f), (132f, 36f), (275f, 176f), (900f, 300f), (8f, 60f) };
var cutSets = new[] { Cuts.All(UiTokens.ChamferLarge), Cuts.All(UiTokens.ChamferMedium), Cuts.Diagonal(UiTokens.ChamferSmall), new Cuts(14.2f, 14.2f, 6, 6), new Cuts(10, 0, 30, 2) };
Check(sizes.All(z => cutSets.All(c => AngleFamily(FrameGeometry.Corners(3, 5, z.Item1, z.Item2, c)) && AngleFamily(FrameGeometry.Outline(3, 5, z.Item1, z.Item2, c)))),
    "every outline edge is horizontal, vertical or at exactly 45° (any size, any cuts)");
Check(sizes.All(z => cutSets.All(c => c.Fit(z.Item1, z.Item2) is var f && f.TopLeft + f.TopRight <= z.Item1 + 1e-3f && f.BottomLeft + f.BottomRight <= z.Item1 + 1e-3f
    && f.TopLeft + f.BottomLeft <= z.Item2 + 1e-3f && f.TopRight + f.BottomRight <= z.Item2 + 1e-3f)), "cuts shrink to fit small controls instead of folding the outline");
// An inset outline stays parallel to the outer one at the inset distance: a chamfer facet of a wall has constant thickness.
var outer = FrameGeometry.Corners(0, 0, 300, 200, Cuts.All(16)); var inset = FrameGeometry.Corners(3, 3, 294, 194, Cuts.All(16).Inset(3));
static float Distance(System.Numerics.Vector2 p, System.Numerics.Vector2 a, System.Numerics.Vector2 b)
{
    var d = System.Numerics.Vector2.Normalize(b - a); var v = p - a; return Math.Abs(v.X * d.Y - v.Y * d.X);
}
Check(Enumerable.Range(0, 8).All(i => Math.Abs(Distance(inset[i], outer[i], outer[(i + 1) % 8]) - 3) < 1e-3f), "the inset outline is parallel at the wall thickness on every facet, chamfers included");
Check(Cuts.Diagonal(6).Inset(3).TopRight == 0 && Cuts.All(1).Inset(3).TopLeft == 0 && Cuts.All(16).Inset(-2).TopLeft > 16, "square corners stay square; insets never go negative; outsets grow the cut");
// Content that respects the margins clears the cut; content pushed into a corner does not.
Check(FrameGeometry.Contains(0, 0, 200, 100, Cuts.All(16), 12, 4, 188, 96) && !FrameGeometry.Contains(0, 0, 200, 100, Cuts.All(16), 4, 4, 188, 96)
    && !FrameGeometry.Contains(0, 0, 200, 100, Cuts.All(16), 12, 4, 199, 99) && !FrameGeometry.Contains(0, 0, 200, 100, default(Cuts), -3, 0, 50, 50),
    "containment: margins that clear a chamfer pass, content in a cut corner or outside fails");
// One light from above, slightly from the left: the top facet is brightest, the bottom darkest, the upper-left chamfer catches more than the lower-right.
var light = Enumerable.Range(0, 8).Select(i => FrameGeometry.Light(FrameGeometry.Normal(i))).ToArray();
Check(light[0] == light.Max() && light[4] == light.Min() && light[7] > light[3] && light[1] > light[5] && light[6] > light[2], "facet lighting follows one light from the upper left");
Check(Enumerable.Range(0, 8).Count(i => FrameGeometry.ChamferCorner(i) >= 0) == 4 && FrameGeometry.ChamferCorner(7) == 0 && FrameGeometry.ChamferCorner(3) == 2, "chamfer facets map to their corners");
// Approved semantic assets: one concept per file, exact supplied bytes, one obvious source of truth.
var repo = new DirectoryInfo(AppContext.BaseDirectory);
while (repo is not null && !File.Exists(Path.Combine(repo.FullName, "ManagementGame.slnx"))) repo = repo.Parent;
Check(repo is not null, "repository root located for asset checks");
var semanticDirectory = Path.Combine(repo!.FullName, "game", "Client", "Assets", "UI", "Semantic");
// SHA-256 of the user-supplied source PNGs (portal_assets_and_mockup_v1); a changed byte fails here.
var suppliedHashes = new Dictionary<SemanticConcept, string>
{
    [SemanticConcept.Portal] = "ce8cb938e3967aae3a140aa8e480ea2127cc135d032ed37349c03552ec497597",
    [SemanticConcept.Squad] = "743197ad40821a633da422f3a876be0d0c9a072f643783c74de39619186a4df6",
    [SemanticConcept.Company] = "98b88003a3cd5bdbcd432df31f9150b085cb8b6c5a560e31518873cfafbc010e",
    [SemanticConcept.Staff] = "89d628cd8ff8e6ba22eeb2cff7cf6f2b9478ef755ed2f60f7900229310c9757b",
    [SemanticConcept.Player] = "e390cc92fa20e79fc442c2235939e506ce7543f124bc3fb8eb81826a00185d04",
    [SemanticConcept.MatchPreparationDecision] = "e31d89142887b2fe8846fb3f8fcd5124e35db6430cbe1c2fcc314ee44fbb279d",
    [SemanticConcept.ContractDecision] = "5fb562b87e36c6f45226866fffaa00b27241e6722a6c887b8935fa07eabbe219",
    [SemanticConcept.RosterMoveDecision] = "c5cc557dd3d9dfb0a7ef45fc90ed53780ca149641f9283f61789bce5a8fe8902",
    // portal_assets_v2 (2026-10-08): Inbox, Competition, Commercial and Finance in the rail; Calendar catalogued only.
    [SemanticConcept.Inbox] = "6bc17285fbaa315717283a3c032b3659ff6c359ab9e65862ec68aca99311cf4e",
    [SemanticConcept.Competition] = "365433f72f28a9fd43f278556d030281adef03fd75edd6f18cc4f8b849124de6",
    [SemanticConcept.Commercial] = "627e9d42f3d4b2dec5eb0e44103b0b2335a893823619749476388d8ab9edae49",
    [SemanticConcept.Finance] = "877a9185b324597beaaa46177d29ec16f0f5109ce4c0d0da1045b0b5b447608f",
    [SemanticConcept.Calendar] = "78b056cfe55a68cd46c1a9f48d0b27e8fd56da513854ee8230209c7c90df15d3"
};
var conceptCount = Enum.GetValues<SemanticConcept>().Length;
Check(conceptCount == suppliedHashes.Count && SemanticAssets.All.Length == conceptCount && Enum.GetValues<SemanticConcept>().All(c => SemanticAssets.All.Count(a => a.Concept == c) == 1)
    && SemanticAssets.All.Select(a => a.Stem).Distinct().Count() == conceptCount, $"{conceptCount} concepts, each mapped to exactly one distinct asset with a recorded source hash");
foreach (var asset in SemanticAssets.All)
{
    var source = Path.Combine(semanticDirectory, asset.SourceFile);
    Check(File.Exists(source) && File.Exists(Path.Combine(semanticDirectory, asset.RuntimeFile)), $"{asset.Concept}: source and runtime files exist ({asset.Stem})");
    Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant() == suppliedHashes[asset.Concept],
        $"{asset.Concept}: canonical PNG is byte-identical to the supplied source");
    Check(SemanticAssets.RuntimePath(asset.Concept) == "res://Assets/UI/Semantic/" + asset.Stem + ".webp", $"{asset.Concept}: one runtime path from the catalog");
}
Check(SemanticAssets.All.All(a => a.FallbackIcon.Length > 0), "every approved concept declares its vector fallback");
Check(Directory.GetFiles(semanticDirectory, "*.png").Length == conceptCount && Directory.GetFiles(semanticDirectory, "*.webp").Length == conceptCount, "no stray or duplicate semantic images");
// Sidebar redesign (2026-10-08): every rail section maps to its own module art, by stable section ID.
var railSections = new (string Id, SemanticConcept Concept)[]
{
    ("portal", SemanticConcept.Portal), ("inbox", SemanticConcept.Inbox), ("squad", SemanticConcept.Squad), ("competition", SemanticConcept.Competition),
    ("commercial", SemanticConcept.Commercial), ("finance", SemanticConcept.Finance), ("staff", SemanticConcept.Staff), ("company", SemanticConcept.Company)
};
Check(railSections.All(s => SemanticAssets.ForSection(s.Id) == s.Concept && SemanticAssets.Get(s.Concept).Stem == "icon_module_" + s.Id + "_v1")
    && new[] { "calendar", "Portal", "portal ", "" }.All(x => SemanticAssets.ForSection(x) is null),
    "each of the eight rail sections maps to its own icon_module_<section>_v1 art; unknown IDs map to none");
// Calendar has no section in the slice: it stays catalogued, and no presentation code names it.
var screenSources = Directory.GetFiles(Path.Combine(repo!.FullName, "game", "Client", "UI"), "*.cs", SearchOption.AllDirectories).Where(f => !f.EndsWith("SemanticAssets.cs", StringComparison.Ordinal));
Check(screenSources.All(f => !File.ReadAllText(f).Contains("SemanticConcept." + SemanticConcept.Calendar, StringComparison.Ordinal)), "Calendar art is catalogued but not used by any screen");
// One perceived size: content is scaled to the area of the optical square, never past its slot, with its aspect kept.
static bool Near(float a, float b) => Math.Abs(a - b) < .01f;
var square = SemanticAssets.OpticalSize(100, 100, 38, 42, 42); var wide = SemanticAssets.OpticalSize(300, 250, 38, 42, 42);
var tall = SemanticAssets.OpticalSize(255, 368, 38, 42, 42); var unknown = SemanticAssets.OpticalSize(0, 0, 38, 42, 42);
Check(Near(square.Width, 38) && Near(square.Height, 38) && Near(MathF.Sqrt(wide.Width * wide.Height), 38) && Near(wide.Width / wide.Height, 300f / 250)
    && Near(tall.Height, 42) && Near(tall.Width / tall.Height, 255f / 368) && Near(unknown.Width, 38) && Near(unknown.Height, 38)
    && SemanticAssets.OpticalSize(500, 50, 38, 42, 42).Width <= 42.001f, "optical sizing: equal area, aspect kept, never past the slot");
Check(SemanticAssets.ForPortrait("portrait:person:g00001") == SemanticConcept.Player && SemanticAssets.ForPortrait("portrait:coach:g00007") == SemanticConcept.Staff
    && new[] { "portrait:", "crest:rival:r01", "logo:brand:x", "person:g00001", "" }.All(x => SemanticAssets.ForPortrait(x) is null), "generic person art only for portrait IDs of a known person kind");
// Mockup example content is a visual reference, never data: none of its sample names, places or values live in the code.
// Typography system: one bundled family in three weights, metric roles on the shared scale, labels subordinate to values.
var fonts = Path.Combine(repo.FullName, "game", "Client", "Assets", "Fonts", "Inter");
Check(UiTokens.FontWeights.SequenceEqual([UiTokens.WeightRegular, UiTokens.WeightMedium, UiTokens.WeightSemibold])
    && Enum.GetValues<TypographyRole>().All(r => UiTokens.FontWeights.Contains(UiTokens.Weight(r))), "every type role uses one of three weights");
Check(UiTokens.FontWeights.All(w => File.Exists(Path.Combine(fonts, Path.GetFileName(UiTokens.FontPath(w)))) && UiTokens.FontPath(w).StartsWith("res://Assets/Fonts/Inter/", StringComparison.Ordinal))
    && File.ReadAllText(Path.Combine(fonts, "OFL.txt")).Contains("SIL Open Font License, Version 1.1", StringComparison.Ordinal),
    "the bundled family ships each weight beside its SIL OFL 1.1 licence");
var scale = new[] { 12, 14, 16, 20, 24, 32, 48 };
Check(Enum.GetValues<TypographyRole>().All(r => scale.Contains(tokens.FontSize(r))), "every type role sits on the 7-step scale");
Check(tokens.FontSize(TypographyRole.MetricLabel) < tokens.FontSize(TypographyRole.MetricContext) && tokens.FontSize(TypographyRole.MetricContext) < tokens.FontSize(TypographyRole.StatusValue)
    && tokens.FontSize(TypographyRole.StatusValue) < tokens.FontSize(TypographyRole.MetricValue) && UiTokens.Weight(TypographyRole.MetricValue) == UiTokens.WeightSemibold
    && UiTokens.Weight(TypographyRole.MetricLabel) == UiTokens.WeightMedium && UiTokens.Weight(TypographyRole.MetricContext) == UiTokens.WeightRegular,
    "metric hierarchy: small medium label, regular context, semibold status and larger semibold value");
Check(UiTokens.Tabular(TypographyRole.MetricValue) && UiTokens.Tabular(TypographyRole.Data) && !UiTokens.Tabular(TypographyRole.Body)
    && Enum.GetValues<TypographyRole>().Count(r => UiTokens.Tracking(r) > 0) == 1 && UiTokens.Tracking(TypographyRole.MetricLabel) == 1,
    "changing figures use tabular numerals; only the caps metric label is tracked");
Check(PresentationText.ResourceAmount(new("Cash", 2476m, "USD", InformationState.Known, "settled", "$2,476")) == "Cash: $2,476"
    && PresentationText.Resource(new("Cash", 2476m, "USD", InformationState.Known, "settled", "$2,476")).StartsWith("Cash: $2,476 · Known", StringComparison.Ordinal)
    && PresentationText.ResourceAmount(new("Cash", null, "USD", InformationState.Unknown, "none", "$0")) == "Cash: Unknown",
    "a pre-formatted amount shows as given, and unknown still hides any backing value");
// Role nouns ("Top laner", "Jungler") left this list on 2026-10-09: the five roles are a rule of v4, not mockup samples.
var mockupSamples = new[] { "Nexus", "Raven Arena", "Titan Forge", "Phoenix Gaming", "Spring Cup", "Kael", "Sorin", "Mako", "AD Carry", "182,450", "Apr 14", "Apr 19",
    "2,476", "Week 1 of 8", "6 / 56", "3 pending" };
var presentationSources = Directory.GetFiles(Path.Combine(repo.FullName, "game", "Client", "UI"), "*.cs", SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(repo.FullName, "src", "ManagementGame.Application"), "*.cs")).ToArray();
var leaked = presentationSources.SelectMany(f => mockupSamples.Where(x => File.ReadAllText(f).Contains(x, StringComparison.Ordinal)).Select(x => Path.GetFileName(f) + ":" + x)).ToArray();
Check(leaked.Length == 0, "no mockup sample data is hardcoded in presentation or read-model code" + (leaked.Length == 0 ? "" : " (" + string.Join(", ", leaked) + ")"));
Console.WriteLine($"UI_KIT_PURE_PASS {count}");
