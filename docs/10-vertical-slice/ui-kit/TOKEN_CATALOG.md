# Semantic tokens

Version: `operations-room-v2` (2026-10-06, [game-identity thesis](../../01-governance/STAGE5_GAME_IDENTITY_THESIS.md)).
**PROVISIONAL DEVELOPMENT THEME**; v1 `engineering-neutral-v1` values are superseded.
Canonical values: `game/Client/UI/Tokens/UiTokens.cs`. Opaque sRGB; sizes in Godot
logical pixels. These values are replaceable engineering choices, not art canon.

| Role | Hex | Meaning |
|---|---|---|
| SurfaceBase | 12151A | Workspace background |
| SurfaceRaised | 1B2028 | Bounded grouped content/control; Divider edge |
| SurfaceInset | 161A20 | Nested context; Divider edge |
| SurfaceOverlay | 252B34 | Transient supporting content; essential Border |
| TextPrimary | F2F4F7 | Main reading |
| TextSecondary | C5CCD5 | Supporting reading |
| TextMuted | A7B0BC | Annotation/disabled wording, still readable |
| TextOnAccent | 101419 | Default neutral accent only; dynamic brands use resolver |
| StatePositive | A8DBB7 | Positive + explicit label |
| StateWarning | F3CE83 | Warning + explicit label |
| StateCritical | FFB0AA | Critical + explicit label |
| StateNeutral | C5CCD5 | Neutral + explicit label |
| SystemError | FFB0D1 | Validation/rejection |
| FocusRing | C2D9FF | Independent focus outline |
| InformationKnown | C5CCD5 | Observed/known data |
| InformationEstimated | E2CC9A | Explicit estimate, never implied certainty |
| InformationUnknown | BFC4D0 | Explicit absence of knowledge |
| AccentOrganization | BAC4CF | Fallback primary, replaced by safe brand resolution |
| AccentOrganizationSecondary | AAB6C4 | Fallback secondary |
| OrganizationSurface | 161A20 | Neutral identity context under variable accents |
| Border | 8592A3 | Essential control outline; >=3:1 against tested surfaces |
| HoverSurface | 2A313B | Native hover; entity row hover |
| SelectedSurface | 2F3A48 | Selected/pressed surface; selected rows add a 4px leading bar |
| ActionPrimary / ActionPrimaryHover | E3E8EF / FFFFFF | The one decision action per workspace; inverted neutral, never a state color |
| TextOnAction | 0F1318 | Text on primary action, >=4.5:1 on both fills |
| Divider | 2C333D | Decorative structural edge; never the only indicator of anything |
| BrandTextLight / BrandTextDark | FFFFFF / 000000 | Resolver endpoints, not raw player colors |

Ordinary semantic text targets >=4.5:1 on the four surfaces plus hover/selected.
Focus and essential borders target >=3:1. Brand primary/secondary target >=3:1
against OrganizationSurface; text over primary targets >=4.5:1. Secondary swatch
borders are decorative, never the only indicator of state. TextOnAccent must not
be reused blindly over a player-selected color.

| Typography | Base font size | Provisional family |
|---|---:|---|
| CompanyIdentity | 24 | Display (600) |
| WorkspaceTitle | 28 | Display (600) |
| SectionTitle | 20 | Display (600) |
| Body | 17 | UI |
| Data | 18 | Numeric (tabular figures) |
| Label | 16 | UI |
| Annotation | 14 | UI |

UI: Segoe UI, Noto Sans, Arial, then system fallback. Display: Bahnschrift at
weight 600, then Segoe UI Semibold/Segoe UI/Noto Sans/Arial. Numeric: Bahnschrift,
Segoe UI, Noto Sans, Arial with the OpenType `tnum` feature, never a code face.
Do not promise identical font metrics across operating systems. Fonts are not
redistributed; bundling/licensing a final family remains a Director decision.
All text remains native, Unicode-capable text.

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

Other centralized values: border=1, focus=2 (drawn outside the control), selection bar=4, radius=4, icon=44, minimum flow text
column=330, action/marker minimum width=160 multiplied by text scale. Bounds: 12 comparison rows, 12 document
sections, 6 navigation ancestors, 62 day-track days; host paginates larger sets. Capture settles 12
frames and layout checks tolerate 2 pixels. Bounds are engineering limits rather
than artistic values.

Motion: Acknowledge=.08s, ContextTransition=.16s, ChangeHighlight=.7s. Motion is
**on by default**; `UiTokens.PreferReducedMotion` is the single live player
preference, and an explicit constructor choice overrides it. Reduced mode resolves
every duration to 0. Capture harnesses set the preference so no frame is taken
mid-transition. No animation is necessary to read state.

Ownership: UI technical lead changes tokens/theme centrally, records version and
reruns contrast/layout/capture checks. Screen authors select semantic roles and
actor-safe data; they must not fork colors, fonts, padding or statuses locally.
Per-organization resolved styles are the intentional shared presentation exception.
Changing provisional art values never authorizes changes to gameplay or identity.
