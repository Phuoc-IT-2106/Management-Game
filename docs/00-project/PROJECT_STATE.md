# PROJECT STATE

Version: 0.8
Phase: Phase 5 — Vertical Slice
Status: Phase 4 Technical Foundation Closed — Vertical Slice Authorized

## Current Objective

Build the smallest production Vertical Slice proving the real management loop through production architecture.

DEC-022 closes Phase 4 with **PASS WITH DEFERRED VALIDATION OBLIGATIONS** and then authorizes Phase 5 BUILD. The [closure package](../09-technical-foundation/PHASE4_CLOSURE.md) controls its conditions. Production implementation is authorized but was not started by the closure task.

## Strategic Center

The project is a **company-first PC management / business strategy simulation that begins with professional esports as its first production domain**.

The player primarily acts as a CEO / executive while remaining directly involved in high-value management decisions. Competitive success matters because it creates strategic and commercial leverage, while long-term company success remains the broader objective.

## Completed Phase Gates

### Phase 0 — Discovery
CLOSED.

### Phase 1 — Product Foundation
CLOSED for current production planning.

### Phase 2 — Core Loop & Simulation
CLOSED.

### Phase 3 — Headless / Minimal Prototype
CLOSED — **PASS PROTOTYPE GATE**.

The approved deterministic prototype implemented the bounded simulation and verified:
- scenarios A–M: PASS;
- acceptance criteria AC-01 through AC-13: PASS;
- 32/32 automated regression tests: PASS;
- 1,664/1,664 repeated seeded scenario pairs with matching final-state hashes;
- 13/13 representative full-trace replays matched;
- 128/128 manual/delegated paired runs preserved gameplay parity while delegation reduced approval burden.

The evidence is stored under `prototypes/reports/`.

Phase 3 validates the bounded management-simulation thesis. It does **not** validate final balance, final content scope, production architecture, or a production esports discipline.

### Phase 4 — Technical Foundation

CLOSED — **PASS WITH DEFERRED VALIDATION OBLIGATIONS** (DEC-022).

Godot .NET + C#, ADR-TF-002 modular monolith, engine-independent Domain/Application, Company/World authority, deterministic/headless testing, bounded save compatibility and data-driven content are accepted. Godot remains revisable through later evidence and a Director decision; Unity is the fallback if major technical failure or disproportionate UI infrastructure cost is demonstrated.

Historical results remain QG-01 **INCONCLUSIVE**, QG-02 **FAIL** against its original 16.7 ms budget, QG-03 **PASS** for the bounded local determinism fixture, and QG-04 **PASS** for tested local NTFS durability scenarios. Closure accepts explicit deferrals; it does not rewrite those results.

## Accepted Decisions

Canonical accepted decisions are recorded in `docs/01-governance/DECISION_LOG.md`.

Current accepted range:
- DEC-001 through DEC-023.

DEC-021 closes Phase 3 and authorizes Phase 4.
DEC-022 accepts the production technical foundation, closes Phase 4 and authorizes Phase 5 BUILD.
DEC-023 is accepted on 2026-10-05: Company-first UX Foundation governs subsequent
Phase 5 presentation. UX Stage 2 authorizes a provisional design system, minimal
UI Kit and internal UI Lab. The [Stage 2 package](../10-vertical-slice/ui-kit/README.md)
is ready for Director review, with provisional tokens/theme, 17 components and
golden candidates; it does not constitute final visual acceptance. Phase 5 remains open; final art direction, future
modules, production discipline and identity/save migration remain separately gated.

## Prototype Findings Carried Forward

Treat these as validated design evidence:
- context can change the preferred management choice;
- competition and business consequences can interact through explicit outcome boundaries;
- Commercial Value should create opportunities, not direct Cash;
- Information Quality can improve estimates without buffing true asset strength;
- delegation can reduce management burden without hidden performance bonuses;
- rival adaptation and meta state can change preparation choices;
- Organizational Capacity / Load can affect process conversion without becoming generic action points;
- financial commitments and debt can constrain future flexibility;
- distress can support costly recovery rather than only instant failure;
- deterministic seeded simulation and causal traces materially improve verification.

## Prototype Elements That Are NOT Production Canon

Do not silently carry the following forward as final design:
- Python prototype implementation;
- 84-day horizon;
- exact four-rival count;
- fictional 5v5 Role Arena structure;
- exact player / coach ratings;
- Currency Unit values;
- coefficients, thresholds, probability clamps, overload curves and debt costs;
- exact number of matches;
- current sponsor / talent-market calibration;
- current terminal-state thresholds;
- current policy scripts.

These remain disposable validation artifacts unless separately approved.

## Known Prototype Limitations Carried Into Phase 5

- end-date viability is not the same as long-term solvency;
- strong-start / snowball balance is not proven beyond the bounded horizon;
- talent-market pressure is only lightly validated;
- only opponent-information value is strongly demonstrated;
- several coach / information fields remain inactive or weakly justified;
- K/L validate one principal restructuring path more strongly than the full recovery space;
- rival world simulation is intentionally lower fidelity;
- prototype traces are verification instrumentation, not a final production event-sourcing design.

These limitations remain inputs to Vertical Slice design and later validation. The Python prototype is a behavioral reference/test oracle only; do not port it wholesale.

## Phase 5 Scope

### Required
- build one complete playable management cycle within the roadmap's one-company, one-discipline, one-primary-team scope;
- prove staff/roster decisions, competition, finance, sponsors, progression, save/load and usable management UI through the accepted production architecture;
- preserve Company/World gameplay authority and outcome boundaries; technical execution bookkeeping is not a third gameplay authority;
- keep Domain/Application engine-independent and verify deterministic/headless behavior;
- use data-driven content and bounded save compatibility without inventing migration guarantees;
- carry the closure register into Phase 5 reviews and discharge obligations at their assigned gates.

### Not Authorized Yet
- final UI art direction;
- large content production;
- multiple esports disciplines;
- multiple player-controlled teams;
- additional industries;
- deep facilities / relationships / merchandising / M&A;
- final balance;
- broad production save migration support beyond a separately approved compatibility policy;
- broad optimization work.

## Ongoing Technical Decision Rules

For each major technical choice, record:
- problem;
- requirements;
- alternatives;
- trade-offs;
- decision;
- consequences;
- migration / reversal cost;
- affected modules.

Do not select technology because it is fashionable or familiar alone.

Technical choices must be evaluated against:
- PC deployment;
- management-heavy UI;
- simulation and deterministic testing;
- data volume;
- iteration speed;
- AI-assisted coding workflow;
- debugging / observability;
- save/load;
- maintainability;
- performance;
- future content growth;
- possible modding without implementing modding now.

## Development Pin and Performance Policy

Initial Phase 5 development pin: Godot 4.7.2 stable mono, matching 4.7.2 mono export templates, .NET SDK 10.0.401, runtime 10.0.12, target `net10.0`. This is for reproducible development, not a permanent release-support promise. Engine/runtime upgrades require targeted requalification.

Phase 5 budgets: target approximately 60 Hz / 16.7 ms for continuous scrolling/direct navigation where practical; provisional p95 frame work around 33.3 ms for discrete rebind/page/sort/filter/layout transitions; <=100 ms p95 ordinary-action acknowledgement without deliberate debounce; query p95 <=250 ms at 10k and <=1 s at 100k. Measure debounce separately. These are engineering targets, not proof that historical QG-02 passed or that every current workflow meets them.

## Deferred Validation Obligations

The [closure register DV-01–DV-10](../09-technical-foundation/PHASE4_CLOSURE.md#risks-carried-forward-and-deferred-validation-gates) is the current owner/gate/evidence register. All entries remain OPEN / DEFERRED:

- DV-01: clean supported Windows distribution before external testers, public demo, release candidate or self-contained clean-Windows claims.
- DV-02: supported external debugger workflow during Phase 5 developer-tooling validation; resolve earlier if development becomes impractical.
- DV-03/DV-04: physical keyboard/mouse usability and actual Windows DPI/mixed-monitor validation before the UI quality gate.
- DV-05: Director-approved target/minimum hardware and workflow performance acceptance during Vertical Slice.
- DV-06: second-machine replay evidence before determinism acceptance or cross-machine claims.
- DV-07: approved campaign scale and measured performance/history growth before campaign-scale acceptance or broader expansion.
- DV-08: production save compatibility policy before first distribution; test every promised migration edge before support claims.
- DV-09: UI performance/correctness regression monitoring at affected changes and milestone reviews.
- DV-10: targeted upgrade requalification and release OS/runtime support review before release candidate.

These obligations do not reopen Phase 4 by default. They cannot be silently waived or treated as solved. No clean-machine support, universal 60 FPS or production save migration guarantee is established.

## Phase 5 UX Stage 4 review

The Director's 2026-10-05 brief accepts **LIST-FIRST / MAP-OPTIONAL** under DEC-023.
The [live sponsor workspace](../10-vertical-slice/sponsor-workspace/README.md) uses
current Application state and fixed-offer acceptance. Following the 2026-10-06
[Stage 4.1 acceptance pass](../10-vertical-slice/sponsor-workspace/acceptance/README.md),
**Stage 4 engineering acceptance is complete**; recommendation:
**ACCEPT FIRST CONTEXTUAL WORKSPACE**. Local managed Release rebind/confirmation
p95 are 3.6017/10.9039 ms; initial construction/company navigation limits are
explicitly carried forward. Required regression and bounded visual checks pass.
Human Director review remains **pending**, with a blank five-task review record;
golden candidates are not human-approved. The internal client remains available.

Published gameplay baseline is `0e9bbee0`, Stage 4 implementation is `7ad0501`,
and Stage 4.1 tested runtime/fixture source is `043ad9b`. Original verification
retains its original working-tree provenance. The sole content change is technical
company name `DEV_ORG_001`; canonical/content hashes change and old exact-content
development saves correctly reject. Schema 1 and simulation rules are unchanged.
Phase 5 remains open; no new DEC, gameplay scope, full identity system, save
migration or next UX stage is introduced. Historical QG-02 and wider DV gates remain.

## Phase 5 UX Stage 5 design governance

The user's 2026-10-06 [strict visual / UX instruction](../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md)
is active for Stage 5 presentation work. Its full 50 rules and 14-part review gate
are preserved, with a reusable screen review record linked from the blueprint and
implementation workflow. Gameplay decision, context and information priority govern
visual style; company-first structure and semantic components remain authoritative.

This is a governance integration, not a screen implementation or acceptance pass.
Stage 4 human review remains pending; provisional design tokens and candidate
captures retain their status. DEC-001–023, gameplay scope, final-art restrictions
and deferred validation obligations remain unchanged. Phase 5 stays open.

### Stage 5 visual composition clarification — 2026-10-06

The [Director clarification](../01-governance/STAGE5_VISUAL_COMPOSITION_DIRECTION.md)
is **ACTIVE** under DEC-023 and Stage 5 governance. LIST-FIRST remains structured
navigation/reference authority with keyboard parity and shared IDs/context; it
does not require list-dominant company composition. The visual target is a Company
Operating / Command Space. Both generic SaaS dashboards and engineering/admin/debug
interfaces fail the intended direction. Panels/instruments and semantic operating
visualization are allowed when tied to current state, questions and real navigation.

The [Company Operating Space blueprint](../10-vertical-slice/company-ui-demo/COMPANY_OPERATING_SPACE_BLUEPRINT.md)
is **DRAFT FOR DIRECTOR VISUAL REVIEW**, defining eight semantic regions and 720p /
1080p behavior without final pixels. Latest local/remote main inspected for this
correction: `1241bc1d4972481fd5c91c621147da4d6d922a18`. Stage 3/3.1 findings and
LIST-FIRST / MAP-OPTIONAL recommendation remain historical valid evidence; they
did not test the final visual direction. Stage 4 engineering acceptance, pending
human review and exact-return exemplar remain unchanged.

This correction is documentation-only and stops before Stage 5 implementation.
The original brief and DEC text are preserved; tokens remain provisional. Broader
company matter routing and identity integration remain explicit gaps, not invented
playable modules. Next composition milestone: Director visual exploration/review
against the draft and expanded screen-review record; no visual/usability pass claimed.

### Stage 5 game-identity foundation — 2026-10-06

The user's instruction to remove the admin/debug appearance is recorded in the
[game-identity thesis](../01-governance/STAGE5_GAME_IDENTITY_THESIS.md) (**ACTIVE**):
a positive visual target, six pillars and positive checks P1–P8 added to the
screen review record. Provisional material `operations-room-v2` replaces
`engineering-neutral-v1`: flat entity rows, one primary-action treatment, surface
tiers with subtle dividers, display/tabular numeric faces instead of a code face,
geometric status markers, outer focus rings, explanatory motion on by default with
a live reduced-motion preference, and one development watermark in place of
keyboard legends and contract notes. Sponsor workspace and company map adopt it.

No gameplay, Application contract, content or save schema changed. Final fonts,
palette, icon family and art remain Director decisions. The company space still
lacks time control and world presence (roster, rivals, fixtures); that is the next
enabled step, followed by graphical primitives and retiring the internal console
as a player path. DEC-001–023 and DV obligations are unchanged; Phase 5 stays open.

### Genre shell — 2026-10-06

The user approved the [genre-shell amendment](../01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md),
which pre-approves the management-genre skeleton (top bar with Continue, collapsible
navigation rail, Portal home, dense tables, bounded tiles) and replaces the
Operating-Space-as-home and dashboard hard-rejection readings while keeping every
uncertainty, accessibility, data-honesty and anti-SaaS-styling rule. The
[genre shell](../10-vertical-slice/genre-shell/README.md) is implemented and is now the
main scene: start screen, Portal, Inbox, Squad, Competition, Commercial, Finance,
Staff and the embedded Company view; the internal console remains for development.
Native verification passes at 1280×720 and 1920×1080; human review is pending.
Gameplay mechanics are unchanged by this design pass.

### Rules v2 — continuous campaign — 2026-10-06

By explicit user instruction the bounded 28-day fixture is replaced by
[rules v2](../10-vertical-slice/RULES_V2_CONTINUOUS_CAMPAIGN.md): consecutive seasons
with regenerated schedules, a qualification-gated sponsor market plus player
negotiation, staggered contracts with renewal, a coach market, and people, rivals
and brands generated per seed from content pools. Content and save schema move to
2; v1 saves are rejected by design (no migration promised, DV-08). All values stay
noncanonical. A Director DEC entry should confirm this scope expansion.

## Current Priority

Proceed with **Phase 5 — Vertical Slice BUILD** within the accepted foundation and bounded roadmap scope. Phase 4 is closed; production development is authorized. The closure task ends with governance/documentation only and starts no gameplay or Vertical Slice implementation.
