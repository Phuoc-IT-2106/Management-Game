# Stage 2 reuse

Unchanged shared components: DocumentView, ComparisonView, ResourceValue,
ConfidenceIndicator, SectionHeader, ValidationMessage, CommitmentReview and
EntityLabel. Existing OperatingMap uses StatusIndicator, TimeMarker/TimelineEvent
and the shared semantic theme. All sizes/spacing/colors/fonts/focus use UiTokens
and UiTheme (`engineering-neutral-v1`, Compact, text scale 1).

NavigationContext's large Stage 2 composition is deliberately not reinstated.
The accepted Stage 3.1 compact company header remains the navigation host; the
decision workspace composes a compact text company/day/path header. Long company
names use an ellipsis in that compact line and remain fully readable in supporting
evidence, not just a hover tooltip. Sponsor document names wrap normally.

Added reusable components: **zero**. SponsorWorkspaceView is a bounded screen
composition; SponsorPresenter owns only local review/feedback lifecycle. A small
CompanyHost composes existing map/list and the one workspace. Existing map gets
an optional live projection/entry/return seam, preserving its fixture scene.

Rejected: copied kit controls, custom graph, generic form generator, universal
contract/decision engine, negotiation framework and universal finance panel.
No portraits, sponsor artwork, new color system or identity persistence.
