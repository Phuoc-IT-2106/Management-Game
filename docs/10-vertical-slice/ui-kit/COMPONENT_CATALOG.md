# Implemented component contracts

Scope: the 17 Stage 2 components below plus Stage 5 addition DayTrack. Code:
`UI/Components/Primitives.cs`, `Structures.cs` and `DayTrack.cs`; data:
`UI/Contracts/Presentation.cs`. Factories return native
Controls; only reusable stateful binders are EntityLabel, EntityRow and
CommitmentReview. Factories are intentionally small compositions, not independent
feature controllers. See the [accepted component architecture](../ui-ux-foundation/COMPONENT_ARCHITECTURE.md)
for future candidates, not additional implemented functionality.

## Rules applying to every entry

- Inputs are actor-safe immutable presentation records or already-filtered text.
  A component cannot enforce secrecy on arbitrary supplied strings; the presenter
  owns authorization/observation filtering. No Domain, simulation, save or command
  service is called. Unknown ResourceValue hides any supplied backing number.
- IDs are supplied stable keys, never generated from names. Intents carry kind,
  TargetId and Revision. Noninteractive text carries no entity identity. Factory
  views are immutable snapshots: their host replaces/frees the subtree on revision
  change. Stateful Bind replaces one callback; a lower revision for the same
  target throws before changing state. Different IDs may have different revisions.
- Default/hover/pressed/disabled/focus are native Button states where interactive.
  Selection is independent edge bar+surface; status uses labelled system roles;
  information uses known/estimated/unknown wording. No critical meaning needs color
  or hover. Noninteractive labels/panels do not enter the focus order.
- All typography, panel styles, minimum widths, spacing and control heights come
  from UiTokens/UiTheme. Organization branding is neutral unless explicitly noted.
  No component can override system warning, error or focus with a player color.
- Static atomic labels have no asynchronous lifecycle. For them loading/error/
  unavailable are presenter text or surrounding ValidationMessage, not hidden
  component states. This is an explicit support limit, not an invented data fetch.
- Labels wrap; native layout grows vertically; hosts supply a scrollable reading
  region. Name+role+text symbol works without portraits. This baseline makes no
  screen-reader or WCAG claim. No tooltip contains exclusive critical information.
- Caller owns returned Controls and frees replaced subtrees. Factories create a
  finite tree once. EntityLabel subscribes once in its constructor, replaces its
  managed callback on Bind, and clears target/callback on ExitTree. Reinserting an
  exited control requires Bind again. No per-frame query, process loop or animation.

## Data, identity, revision and output

| Component | Purpose / inputs | Output; identity and revision |
|---|---|---|
| SemanticText | Native Label with text, typography role and color role | No event/key; replace text snapshot |
| IconPresentation | Representation key for Company, Division, Team, Department, Person, Contract, Sponsor, Competition, Market, Opportunity, Risk, Obligation, Event, Decision, Portfolio, Discipline | No action/key; every kind has a distinct marker; `[?]` is reserved for unknown kinds and Unknown information |
| StatusIndicator | DomainStatus and explanatory reason | No event/key; labelled Neutral/Positive/Warning/Critical snapshot |
| EntityLabel | EntityView plus optional inspect handler | `inspect` intent with supplied ID/revision; Bind resets selection, pressed/toggle state, tooltip, availability and callback; missing ID is nonactionable |
| ResourceValue | Label, nullable decimal, unit, InformationState and context | No action/key; invariant grouped signed amount or Unknown, then marker/state/context line; actor-safe scalar snapshot |
| TimeMarker | Nullable day and meaning | No event/key; fixed day or date unknown; never advances a clock |
| ConfidenceIndicator | InformationState and evidence text | No event/key; `[=]`, `[~]`, `[?]` plus Known/Estimated/Unknown; never computes hidden confidence |
| StatusBadge | DomainStatus and reason | StatusIndicator in a panel; no event/key |
| SectionHeader | Title and optional supporting description | No event/key; reading hierarchy |
| ValidationMessage | Message and error/information choice | No event/key; supplies explanation, not automatic retry |
| NavigationContext | OrganizationIdentityView, <=6 EntityView path entries, optional navigate callback, optional resolved Texture2D | Path buttons emit `inspect` intents for the entry IDs/revisions; identity name never becomes key; organization snapshot carries ID/revision and provenance |
| EntityRow | EntityView and optional inspect handler | EntityLabel intent; persistent identity/status/information controls rebound together; same stale-revision protection |
| AlertItem | MatterView: entity, classification, why, due day, consequence; open handler | Inspect intent through supplied entity ID/revision; immutable composition |
| TimelineEvent | MatterView and open handler | Inspect intent through supplied event ID/revision; historical/future meaning supplied, no scheduling system |
| ComparisonView | Titles and <=12 labelled comparison lines plus Availability | No action/selection/key; equal emphasis; presenter supplies comparable evidence labels; no automatic score/rank |
| DocumentView | ID, revision, title, provenance, <=12 sections, Availability | No command; ID/revision metadata on panel; never treats title as key |
| CommitmentReview | CommitmentData ID/revision, summary, trade-off, phase/result and commit handler | One `commit` intent with supplied ID/revision; immediately Pending locally, disables resubmission; presenter must bind authoritative result; stale same-ID revision rejected atomically |
| DayTrack | DayTrackData: question, first/last day (<=MaxTrackDays), one limit, current series, optional proposed series, marker rows (filled/outlined/diamond), assumption | No action/key; drawn track plus legend and `DayTrackSummary` text stating every drawn fact; rejects mismatched or unbounded series |

## State, keyboard, branding and lifetime support

E/U/X/L below mean empty/unavailable/error/loading. “Host” means the explicit
static-label contract above; it does not imply that the component loads data.

| Component | Tokens / interaction / keyboard | E / U / X / L; branding; accessibility and lifetime |
|---|---|---|
| SemanticText | Selected typography and text roles; no focus | E blank optional text; U/X/L host. Unbranded. Wrap and system glyph fallback; one Label |
| IconPresentation | Label/TextSecondary, ActionWidth; no focus | E/unknown key `[?]`; U/X/L host. Unbranded. Written key accompanies symbol; one Label |
| StatusIndicator | Label + StateNeutral/Positive/Warning/Critical; no focus | E reason omitted but status remains; U/X/L host. Unbranded. Written status plus marker; one Label |
| EntityLabel | Shared Button, ControlHeight, SelectedSurface + SelectionWidth leading edge, FocusRing; Tab/Shift+Tab, Enter/Space; default/hover/press/disabled/selected | E/U/X/L Availability text and reason; all non-ready activations suppressed even on direct signal path. Unbranded identity text. Wrap/name/role/symbol; stable finite control, clears callback on exit |
| ResourceValue | Data amount, Label evidence; TextPrimary/TextSecondary or InformationEstimated/Unknown; no focus | E/null/unknown value shows Unknown; U/X/L host. Unbranded. Minus sign, unit, `[=]`/`[~]`/`[?]` marker and evidence wording, never color-only; stack of two Labels |
| TimeMarker | Label/TextSecondary; no focus | E/null shows date unknown; U/X/L host. Unbranded. Written meaning/day; one Label |
| ConfidenceIndicator | Label + InformationKnown/Estimated/Unknown; no focus | E evidence may be empty; state remains; U/X/L host. Unbranded. Symbol plus word; one Label |
| StatusBadge | SurfaceInset/Border/Label + status, ActionWidth; no focus | Same states as StatusIndicator. Unbranded. Text preserved inside bounded panel; two Controls |
| SectionHeader | SectionTitle, Annotation/TextSecondary, SpaceRelated; no focus | E optional description omitted; title required by host; U/X/L host. Unbranded. Hierarchy stays text; <=3 Controls |
| ValidationMessage | Body/SystemError or TextSecondary; no focus | E “No validation issues”; U/L host explanations, X labelled error message. Unbranded. `[!]`/`[i]` plus message; one Label |
| NavigationContext | CompanyIdentity, Label, Annotation, SurfaceInset, resolved OrganizationSurface/accents/text, StateWarning fixture notice; native path focus | E display name “identity unavailable”; absent short name `[CO]`; no path “Company context”; U/X/L delegated to path EntityView, no emblem shows text fallback. Only component applying organization brand; does not color status. Bounded <=6 buttons; caller owns optional texture and replaced snapshot |
| EntityRow | EntityLabel + Label/system status/information colors; identity button alone focuses | E/U/X/L explicit availability/unknown/status labels; identity activation suppressed. Unbranded. Independent selected+focused+estimated+warning visible; 3 persistent children; 100 rebinds checked without node growth |
| AlertItem | Raised panel, EntityLabel, StatusIndicator, Body, TimeMarker, secondary Label; focus on identity | E/U/X/L delegated entity state; supplied explanatory snapshot remains visible, never fabricated; missing due day unknown. Unbranded. Why/deadline/if-ignored always text; bounded composition |
| TimelineEvent | Inset panel, time, identity, Body/secondary Label; focus on identity | E/U/X/L delegated entity state; missing date unknown. Unbranded. Date/classification and consequence in text; bounded composition |
| ComparisonView | SectionTitle/Annotation, SurfaceInset, Label/Body; no focus | E no lines -> Empty; U/X/L explicit state label and hides rows. Unbranded. Equal sequential labelled alternatives, words carry uncertainty; rejects >12 lines; host owns scroll/pagination |
| DocumentView | Raised panel, section/provenance and Body; no focus | E no sections -> Empty; U/X/L explicit state and hides content. Unbranded. Real text/provenance, no embedded images; rejects >12 sections |
| DayTrack | SectionHeader, Annotation font in drawing, ControlHeight-derived bar height, Border/TextPrimary/StateWarning/TextSecondary, FocusWidth limit line; no focus | E empty proposed series omits it from drawing, legend and text; U/X/L host. Unbranded. Limit, overload days, peaks and marker days all stated in text; amber is never the only cue; drawing never carries exclusive information; finite tree, redraw only on bind |
| CommitmentReview | Body/TextPrimary/TextSecondary/SystemError and native EntityLabel focus | E host supplies meaningful terms; U explicit disabled reason; X Rejected labelled and disabled; L Pending labelled and disabled; Accepted acknowledgement disabled. Only Ready + callback allows action. Unbranded. Summary/trade-off/result explicit; 4 persistent children; no automatic retry or direct gameplay mutation |

Host responsibility for static reading surfaces: preserve a keyboard-reachable
scroll region. UI Lab implements and tests this. Hosts must validate untrusted
input lengths at their presentation boundary and paginate larger datasets; these
bounded specimens are not virtualized tables.

The six expressly deferred structural candidates are listed in
[OPEN_QUESTIONS](OPEN_QUESTIONS.md). The existing roster table is unchanged.
