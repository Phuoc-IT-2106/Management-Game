# UI Technical Foundation

Status: PROPOSED. Date: 2026-10-04. Owner: UI engineer / Technical Lead.

**DECISION:** Windows-first, management-heavy UI and 2D presentation; executive/management choices with meaningful checkpoints (DEC-002/009). UI reads observations and submits commands; it never mutates authoritative state.

**FACT:** Godot supplies Control nodes, layout containers, themes and keyboard focus facilities ([official UI documentation](https://docs.godotengine.org/en/stable/tutorials/ui/index.html)). Tree supports multiple columns ([Tree reference](https://docs.godotengine.org/en/stable/classes/class_tree.html)). These capabilities do not establish performance for this project's dense tables. Official capabilities and candidate trade-offs are in the [engine evaluation](ENGINE_EVALUATION.md).

## Composition and flow

**PROPOSAL:** Use a Godot scene shell containing navigation, checkpoint/review area and replaceable screen panels. Reuse table shell, filter/search bar, entity summary, financial commitment list, comparison panel, validation message and confirmation component. Screen scenes own layout and signals; plain C# presentation models own formatting, selection, query parameters and command construction. Subscribe/unsubscribe explicitly when panels enter/leave; dispose stale subscriptions so navigation cannot duplicate commands.

Application publishes immutable actor-visible view data at a committed revision. UI signals update an edit draft, then submit a typed command with stable IDs, checkpoint/revision and a unique request ID. Acknowledgement refreshes views; rejection preserves the draft and explains changed eligibility/authority. Disable duplicate activation while pending, but rely on application idempotency. Do not optimistically edit Cash/roster/schedule values. Data refreshes do not reroll observations or reveal hidden attributes.

UI-only state includes active tab, scroll offset, sort/filter/search, selected IDs, expanded sections and unsubmitted forms. Store preferences separately if useful; none is gameplay authority. Committed preparation, lineup, authority envelopes and financial terms belong to simulation. Closing a dialog cancels an unsubmitted draft; closing a panel does not undo a committed decision.

## Dense data

**PROPOSAL:** Begin with pagination (100 visible rows as a qualification default) and Tree or composed Controls for bounded rows. Keep all records as plain data; create engine objects only for the current view. No Node per player, contract, historical event or entire simulation dataset. Sort/filter/query on immutable observation records before binding a page. Use stable ID tie-breaks, typed numeric/date comparers, explicit unknown-value placement and case-insensitive display search. Locale formatting is presentation-only.

Debounce search (provisional 150 ms), cancel obsolete query work and discard responses from an older query/state revision. Sort/filter changes reset or deliberately preserve page/selection by ID, never row index. Commands always reference selected entity IDs; verify selected items remain legal after a refresh. Show count, empty result and pending-query states. Filter only fields the actor may observe; hidden truth must not become discoverable through sorting.

If pagination cannot meet review-approved workflows, qualify a recycled visible-row window with a small overscan buffer. Recycling must reset bindings, callbacks, tooltips and focus ownership. Do not assume a ScrollContainer or Tree provides arbitrary dataset virtualization automatically. Measure interop calls, allocation and retained Controls. If a substantial custom table framework is required, revisit Unity before expanding that framework.

## Navigation, input and layout

**PROPOSAL:** Stable routes identify screens/entities; back navigation preserves UI context when still valid. A mandatory checkpoint remains visible and prevents further advancement until legally resolved. Mouse and keyboard share command handlers. Define predictable Tab/Shift+Tab order, arrow navigation for tables, Enter activation, Escape/back behavior, visible focus and tooltips accessible by focus. Editing a text field must suppress global advance shortcuts. Confirmation appears for material irreversible commitments with costs/obligations, not every daily step.

Use containers, anchors, shared theme spacing and minimum sizes. At smaller windows stack summary/detail panels and allow controlled table scrolling or optional-column hiding; keep identity and action columns usable. Avoid fixed pixel layouts and cropped dialogs. Test 1280×720, 1920×1080 and 2560×1440 at 100/125/150/200% Windows scaling; combinations are test cases, not an approved minimum configuration. Support window resize and mixed-DPI monitor moves without losing focus or selection. Text expansion, long names, uncertain values and zero/negative financial displays require explicit cases. Final visual art direction and new gameplay screens remain outside this assignment.

## Future validations A and B

**PROPOSAL — A / QG-01:** Export the exact proposed Godot .NET/C# combination and run the complete artifact on a clean Windows machine without the development SDK. Also verify no separately installed managed runtime or network download is needed. Start, interact, advance, save and reload. Editor launch is insufficient evidence. See [qualification gates](TESTING_DETERMINISM_DEBUGGING.md#future-qualification-gates).

**PROPOSAL — B / QG-02:** On the hardware approved through Q-04, test 1k/10k/100k synthetic rows and 12 columns with 100-row pages or bounded recycled windows. Run at least 30 measured sort/filter cycles after warm-up, 5 minutes of scrolling/resizing and 100 navigation/query-refresh cycles. Preserve raw data, workload seed, build and machine identity.

Provisional acceptance budgets for Director review: p95 steady scrolling frame time at most 16.7 ms; p95 input acknowledgement at most 100 ms; p95 completed 10k-row sort/filter at most 250 ms and 100k-row query at most 1 s, without blocking input. Report cold-start separately. Control count stays bounded by visible rows/columns plus fixed shell; no monotonic live-control/subscription growth after repeated navigation, and retained memory after collection returns within 10% of the comparable warmed baseline. These are proposed engineering budgets, not measured capability or a production world-size promise.

Correctness must pass at every dataset size: stable sort with ties/unknowns, search/filter composition, selection after reorder/page change, latest-query wins, correct entity command, rejected stale revision, keyboard focus, scaling without inaccessible controls, and no hidden-data leakage. Timing success cannot compensate for wrong-entity actions. Failure triggers a bounded implementation revision or engine ADR reconsideration; neither validation is performed in PLAN / SPEC.
