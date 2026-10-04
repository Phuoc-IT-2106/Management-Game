# Internal UI Lab blueprint

Scope authorized by the Stage 2 Director brief. Technical blueprint: UI Technical
Lead. Final visual acceptance remains Director/UI review. Not a game screen.

Role/type: internal component inspection; taxonomy dense inspection + document
specimens. Hierarchy: development tools, outside campaign navigation.
Question: Do shared controls preserve identity, meaning, focus and readability
across branding, density, information and availability states?
Primary decision: developer evaluates/revises components; no player command.
Entry: launch the Lab scene. Exit: close the Lab; no campaign/save is created.

Primary information: component name/state, fixture identity and theme notice.
Secondary: sample values/terms, event output and selected brand/density.
On demand: specimen pages and capture manifest. Raw/internal: revision/IDs and
automated verification results are appropriate here.

Entities: technical presentation references only; constant revision, no Domain
object. Commands: none. Outputs: inspect/back/review intents and local feedback.
Uncertainty: explicitly known/estimated/unknown with no hidden backing value.
Authority: labels demonstrate supplied context; no simulated delegation.
Timing: fixed day markers, no clock advancement or RNG.

Regions: header and provisional/fixture notices; wrapping specimen-page, brand,
density and text-scale controls; scrollable specimen area; intent feedback.
Pages: identity/primitives, interaction/rebinding, document/comparison/commitment,
branding specimens. Native containers reflow; no fixed fullscreen design.
Keyboard: Tab/Shift+Tab, Enter/Space activation; initial visible focus; scroll
follows focus; Escape closes only the standalone Lab. Native hover/press/focus
styles plus selected/disabled specimens. Input never maps to gameplay actions.

Empty/unavailable/error/loading specimens use explicit text and suppress unsafe
activation. Rebind clears previous selected/pressed/callback state; stale revision
within one target is rejected. Brand variants include neutral, bright, dark,
warning-like, low-contrast, identical colors and missing emblem. Long Unicode
name, short name, large/negative values and missing/unknown evidence are included.

Accessibility: >=4.5 text / >=3 essential graphic contrast checks, labels in
addition to color, visible focus, larger text and no-motion behavior. No portrait
or font file distribution. Finite specimens and bounded repeated bind checks;
no graph/table framework. Long records scroll instead of requiring tiny text.

Acceptance: fixed capture cases at 1280x720/1920x1080 plus 2560x1440 if practical;
native theme/state checks; synthetic keyboard traversal; renamed/rebound targets
emit correct IDs once; no Domain dependency in fixtures; old client smoke still
works. These checks do not pass physical-input/DPI or human golden approval gates.

UX review: purpose/decision value is component verification; information matches
that task; costs/trade-offs appear in document samples; company identity is shown
with technical fixture marking; feedback reports intents; canonical controls
provide consistency; art cost is Tier 0; native Controls bound technical cost;
repeatability comes from fixed fixtures. The eventual game-map blueprint remains
a separate task. No local draft creates a commitment or outcome.

Launch with `build/Run-UiLab.ps1`; capture with `build/Capture-UiKit.ps1` (see
[capture procedure](VISUAL_VERIFICATION.md)). Page buttons choose identity,
interaction, documents or branding. Brand, density and text buttons cycle fixed
options. Focus the scroll region and use PageUp/PageDown/Home/End to read static
content without a mouse. Every visible development identity is under the persistent
NONCANONICAL header; company context also repeats the marker. Escape only exits
this standalone development tool.
