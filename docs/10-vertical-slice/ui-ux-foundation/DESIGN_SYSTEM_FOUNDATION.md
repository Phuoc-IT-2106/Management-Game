# Semantic design-system foundation

Status: PROPOSAL. No final fonts, palette, pixel scale or aesthetic is selected.
The subsequent UI Kit task must approve concrete values against these semantics.

## Token contract

| Family | Semantic roles | Rule |
| --- | --- | --- |
| Surfaces | SurfaceBase, SurfaceRaised, SurfaceInset, SurfaceOverlay | Indicate structural depth; not a different card style per screen |
| Text | TextPrimary, TextSecondary, TextMuted, TextOnAccent | Readability independent of company color; muted does not mean unreadable |
| System color | StatePositive, StateWarning, StateCritical, StateNeutral, SystemError, FocusRing | Globally stable semantics; text/icon redundancy |
| Information | InformationKnown, InformationEstimated, InformationUnknown | Distinguish epistemic status from success/failure |
| Branding | AccentOrganization, AccentOrganizationSecondary, OrganizationSurface | Derived safe presentation values from campaign branding; never aliases for danger/focus |
| Typography | CompanyIdentity, WorkspaceTitle, SectionTitle, Body, Data, Label, Annotation | Hierarchy by role; tabular numeric treatment where supported; font family/size values later |
| Spacing | SpaceInline, SpaceRelated, SpaceGroup, SpaceSection, SpaceWorkspace | Ordered shared scale; values selected once in UI Kit; no local magic-number system |
| Sizing | ControlCompact, ControlDefault, ControlComfortable, InspectorWidth, ReadingMeasure | Density variants preserve readability and keyboard focus; no fixed full-screen canvas |
| Separators | DividerGroup, BoundaryScope, FocusOutline | Relationships before ornamental borders |
| Motion | Acknowledge, ContextTransition, ChangeHighlight | Purposeful brief feedback; durations later; reduced-motion equivalent |
| Icons | EntityKind, Action, Status, Relationship | Common stroke/weight/size family; labels for unfamiliar meaning |

Use shared Godot Theme resources and reviewed type variations. Native theme
editing is documented in the [official theme guide](https://docs.godotengine.org/en/stable/tutorials/ui/gui_using_theme_editor.html).
This supports reuse; it does not establish this project's final styling or
performance. Components consume tokens; screens arrange components. Brand
adaptation occurs in one presentation resolver, never ad hoc color math per screen.

## State composition

Interactive state: default, hover, focus, selected, pressed, disabled, loading.
Domain status: neutral, positive, warning, critical. Information status:
known, estimated, unknown. These are independent: a selected row can be estimated
and warning at once. Loading is not unknown; disabled is not inaccessible detail.

Focus remains visible above selection/hover; status is labelled without replacing
focus. Pressed confirms input, pending communicates work, accepted/rejected comes
from the command response. Disabled actions explain why without requiring hover.
Do not use the same symbol for loading and unavailable. State combinations need
UI Lab coverage before a component is shared.

## Composition and visualization

Use container-driven layout, readable measures and explicit primary/supporting/
on-demand regions. Smaller windows stack regions and scroll bounded content;
larger windows add evidence space, not more unrelated primary actions. Optional
table columns may collapse while identity and action remain accessible.

Charts label units, dates, scope and baseline. Known history and estimated futures
use distinct line/pattern conventions and a legend; missing data is a gap, never
zero. Show denominators for rates, comparable periods and accessible text/table
equivalents. Resource changes carry sign and cause; positive cash is not proof a
choice is strategically correct. Maps label relationship kinds and provide a
keyboard list equivalent. Motion cannot be required to understand a consequence.

## Accessibility targets for later validation

Propose at least 4.5:1 contrast for ordinary text, 3:1 for large text and 3:1 for
essential control/state graphics against adjacent colors, using W3C guidance as
measurable design targets. See [text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)
and [non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html).
This is a desktop design target, not a claim of WCAG conformance or native
assistive-technology support.

Provide visible keyboard focus, labelled controls, no color-only/hover-only
meaning, readable text scaling and reduced motion. Test long names, Unicode,
large/negative values and unknown fields. Avoid text baked into images. Godot's
[focus guidance](https://docs.godotengine.org/en/stable/tutorials/ui/gui_navigation.html)
informs implementation; physical keyboard and actual DPI/mixed-monitor checks
remain DV-03/04 obligations. Native screen-reader behavior needs its own evidence
before any support claim.

Token additions/changes require UI lead review, representative state screenshots
and regression rationale. Director approval is required for final art direction;
the UI lead cannot silently choose a canonical organization palette.
