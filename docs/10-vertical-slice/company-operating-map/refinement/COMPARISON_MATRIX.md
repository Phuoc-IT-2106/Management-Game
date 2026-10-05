# Direct comparison — ENGINEERING PROXIES

These are scripted navigation/geometry observations, **not USER EXPERIENCE PROOF**.
No human completion time, comprehension score, satisfaction or preference exists.
Source: `map-normal-1280x720.json` and `map-normal-1920x1080.json`, under
[candidates](evidence/candidates/), `EngineeringProxies.Tasks`; executed by
`RefinementVerification.cs`. Both views use exactly the same source records.

## Counting protocol

Each task starts at the company overview, scroll 0, Affairs collapsed, in its
assigned mode. Reset/setup and choosing the assigned mode are excluded. Activation
is one button invocation (mouse click or Enter/Space equivalent), not a count of
Tab presses. Context transition is a scope/situation selection or handoff/return;
opening Affairs or re-inspecting the already selected company is not a context transition. A scroll operation is an
explicit reveal operation needed to reach a task target. Native focus-follow and
post-layout restoration are automatic, not claimed as human wheel gestures.
Pointer/keyboard correctness is checked separately by the preserved native tests.

1. Inspect Primary Team; read full ancestry and both support links in inspector.
2. Select attached sponsor; read scope, why, consequence and estimated/unknown
   evidence; enter and return to the same selection/mode/focus/scroll.
3. Select attached preparation; read scope/unknown evidence. Reset company as
   setup, open Affairs and select preparation there. Both methods are counted.
4. Observe quiet company, next checkpoint and ongoing structure without input.
5. Long Unicode name + alternate low-contrast brand + normal four-matter density:
   inspect full company identity, then sponsor → entry → return and preparation
   → entry → return. Stress script is distinct from the long-name root screenshot.

## Identical task results at 1280×720

All rows expose five semantic ownership levels in four visual rows (Esports and
Discipline share a row). Full selected ancestry remains in the persistent path.
Map complexity = Medium relative to tree: 37 representation Controls, two lanes,
three support panels, explicit relationship labels. List complexity = Low: 29
representation Controls, indented sequence and two group headings. These labels
describe this bounded composition, not a cost estimate for future gameplay.

| Task | View | Activations | Scroll operations | Context transitions | Visible hierarchy / pressure | Observed strength | Observed weakness |
| --- | --- | ---: | ---: | ---: | --- | --- | --- |
| 1 Orientation | Map | 1 | 0 | 1 | Five levels; Low: 414px content in 468px region | Support relations visible before inspection | Two lanes to scan; Medium complexity |
| 1 Orientation | List | 1 | 0 | 1 | Five levels; Medium: 454/468px | One ownership sequence; Low complexity | Specific support relations require inspector |
| 2 Sponsor | Map | 3 | 0 | 3 | Five levels; Low: 437/468px | Sponsor near shared support relation | Extra panels/labels; Medium complexity |
| 2 Sponsor | List | 3 | 0 | 3 | Five levels; Medium: 477/468px | Direct owner-adjacent matter; Low complexity | Last function partly below fold; task target/action still visible |
| 3 Preparation + Affairs | Map | 3 | 0 | 2 | Five levels; Medium: drawer reduces region to 386px | Preparation and dated route agree | Some lower structure below fold with drawer; Medium complexity |
| 3 Preparation + Affairs | List | 3 | 0 | 2 | Five levels; Medium: 477/386px with drawer | Same target/scope/unknown evidence; Low complexity | More lower rows below fold with drawer |
| 4 Quiet | Map | 0 | 0 | 0 | Five levels; Low: 414/468px | Normal structure and no urgent message | Relationship scaffolding persists in quiet state; Medium complexity |
| 4 Quiet | List | 0 | 0 | 0 | Five levels; Low: 431/468px | Ongoing structure visible; Low complexity | Less immediate cross-function explanation |
| 5 720p stress | Map | 7 | 0 | 6 | Five levels; Low for core route, Medium for full identity inspection | Stable identity/return through both handoffs | Full name needs inspection; Medium complexity |
| 5 720p stress | List | 7 | 0 | 6 | Five levels; Medium: selected content 477/468px | Same actions and exact return; Low complexity | Last function partly below fold; full name needs inspection |

At 1920×1080 all task counts remain identical, with zero explicit task scrolls.
The reading region is 828px high; normal map/list content is 414/431px. The 720p
list overflow is 9px for selected sponsor/preparation, not hidden horizontal
overflow. Reading every lower row while the drawer is open may require scrolling;
zero above describes the defined tasks, not every possible inspection.

## Evaluation framework

| Dimension | Map | List | Evidence / limit |
| --- | --- | --- | --- |
| Orientation | Company root + two lanes | Company root + one sequence | Shared persistent identity/path; human explanation untested |
| Hierarchy | Contains arrows + compressed pair | Indentation + compressed pair | Full path and individual IDs verified |
| Function vs portfolio | Parallel shared support lane | Separate shared-function group | Both explicitly distinguish capabilities from owned scopes |
| Attention | Two emphasized attached matters | Same two emphasized matters | Priority/status labels, no invented score |
| Discoverability | Matter at owner, support links adjacent | Matter beside owner, support links in inspector | No memorized tab prerequisite; search time unknown |
| Context | Same reason, status, scope, date, consequence, uncertainty | Same | Inspector is a single implementation |
| Navigation | Counts above | Same counts | No measured task-path advantage |
| Return | Same ID/mode/focus/scroll | Same | Layout-delayed scroll restoration verified |
| Density | Less height at 720p | Fewer Controls, more height | Map 37 vs list 29 Controls (about 22% fewer in list) |
| Scannability | Simultaneous relations | Sequential reading order | Heuristic trade-off, not measured comprehension |
| Company feel | Explicit ownership/support overview | Identity + owned hierarchy + contextual consequences | Map may help; no human superiority evidence |
| Web-app risk | Relation labels distinguish from module dashboard | Tree can resemble admin navigation | Keep company context/decision consequences in either model |
| Implementation cost | Medium, native bounded composition | Low, native bounded composition | Zero new reusable components; no graph framework in either |
| Scalability | Additional lanes may crowd | Additional rows may scroll | Conceptual only; current eight-scope bound unchanged |

The process retains **both** representations: total Controls are not separate
deployment costs. Removing the optional map would require a later product choice.
Shared 50-cycle retention and work timings are in VERIFICATION.md. These do not
prove production performance, growth capacity or human usability.

Disposition: list-first/map-optional on lower structural complexity and equivalent
verified task access. Explicitly credit the map's better vertical density and
visible relations; there is no blanket list-density win.
