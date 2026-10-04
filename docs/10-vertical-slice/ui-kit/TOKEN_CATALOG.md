# Semantic tokens

Version: `engineering-neutral-v1`. **PROVISIONAL DEVELOPMENT THEME**.
Canonical values: `game/Client/UI/Tokens/UiTokens.cs`. Opaque sRGB; sizes in Godot
logical pixels. These values are replaceable engineering choices, not art canon.

| Role | Hex | Meaning |
|---|---|---|
| SurfaceBase | 171A1F | Workspace background |
| SurfaceRaised | 242930 | Bounded grouped content/control |
| SurfaceInset | 1C2026 | Nested context |
| SurfaceOverlay | 303640 | Transient supporting content |
| TextPrimary | F2F4F7 | Main reading |
| TextSecondary | CCD2DA | Supporting reading |
| TextMuted | ACB5C1 | Annotation/disabled wording, still readable |
| TextOnAccent | 101419 | Default neutral accent only; dynamic brands use resolver |
| StatePositive | A8DBB7 | Positive + explicit label |
| StateWarning | F3CE83 | Warning + explicit label |
| StateCritical | FFB0AA | Critical + explicit label |
| StateNeutral | CCD2DA | Neutral + explicit label |
| SystemError | FFB0D1 | Validation/rejection |
| FocusRing | C2D9FF | Independent focus outline |
| InformationKnown | CCD2DA | Observed/known data |
| InformationEstimated | E2CC9A | Explicit estimate, never implied certainty |
| InformationUnknown | BFC4D0 | Explicit absence of knowledge |
| AccentOrganization | BAC4CF | Fallback primary, replaced by safe brand resolution |
| AccentOrganizationSecondary | AAB6C4 | Fallback secondary |
| OrganizationSurface | 1C2026 | Neutral identity context under variable accents |
| Border | 919DAD | Essential outline; >=3:1 against tested surfaces |
| HoverSurface | 343C47 | Native hover |
| SelectedSurface | 364352 | Selected/pressed surface; selected also has text marker |
| BrandTextLight / BrandTextDark | FFFFFF / 000000 | Resolver endpoints, not raw player colors |

Ordinary semantic text targets >=4.5:1 on the four surfaces plus hover/selected.
Focus and essential borders target >=3:1. Brand primary/secondary target >=3:1
against OrganizationSurface; text over primary targets >=4.5:1. Secondary swatch
borders are decorative, never the only indicator of state. TextOnAccent must not
be reused blindly over a player-selected color.

| Typography | Base font size | Provisional family |
|---|---:|---|
| CompanyIdentity | 24 | UI |
| WorkspaceTitle | 28 | UI |
| SectionTitle | 20 | UI |
| Body | 17 | UI |
| Data | 18 | Numeric |
| Label | 16 | UI |
| Annotation | 14 | UI |

UI: Segoe UI, Noto Sans, Arial, then system fallback. Numeric: Consolas, Noto Sans
Mono, Courier New, then system fallback. Prefer monospaced digits for values;
do not promise identical font metrics across operating systems. Fonts are not
redistributed. All text remains native, Unicode-capable text.

| Spacing | Default | Meaning |
|---|---:|---|
| SpaceInline | 4 | Within a small semantic unit |
| SpaceRelated | 8 | Related controls/vertical panel padding |
| SpaceGroup | 12 | Horizontal panel padding |
| SpaceSection | 20 | Section boundary available to hosts |
| SpaceWorkspace | 24 | Lab/workspace outer margin |

Compact multiplies spacing by .75, Comfortable by 1.25; rounded to integer using
.NET Math.Round. Default is 1. Typography scales independently from 1 through 1.5.
Control heights are minimums, not clipping bounds: Compact 32, Default 40,
Comfortable 48, multiplied by text scale and rounded up. Native wrap may grow them.
InspectorWidth=380 and ReadingMeasure=760 are shared future host hints; this kit
does not build an inspector. No alternate palettes or component forks per density.

Other centralized values: border=1, focus=2, radius=4, icon=44, minimum flow text
column=330, action/marker minimum width=160 multiplied by text scale. Bounds: 12 comparison rows, 12 document
sections, 6 navigation ancestors; host paginates larger sets. Capture settles 12
frames and layout checks tolerate 2 pixels. Bounds are engineering limits rather
than artistic values.

Motion: Acknowledge=.08s, ContextTransition=.12s, ChangeHighlight=.18s when enabled;
all resolve to 0 in reduced mode. The current controls use immediate changes and
the Lab enables reduced mode. No animation is necessary to read state.

Ownership: UI technical lead changes tokens/theme centrally, records version and
reruns contrast/layout/capture checks. Screen authors select semantic roles and
actor-safe data; they must not fork colors, fonts, padding or statuses locally.
Per-organization resolved styles are the intentional shared presentation exception.
Changing provisional art values never authorizes changes to gameplay or identity.
