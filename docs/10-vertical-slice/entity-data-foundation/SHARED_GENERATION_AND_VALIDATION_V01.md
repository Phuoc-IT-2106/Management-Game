# Shared Generation and Validation v0.1

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 (Asia/Saigon) · PLAN → SPEC → VERIFY  
Điểm vào: [README](README.md). Domain contracts: [Team](TEAM_DATA_STANDARD_V01.md), [Coach](COACH_DATA_STANDARD_V01.md), [Organization](RIVAL_ORGANIZATION_DATA_STANDARD_V01.md).

## 1. Authority, acquisition và fidelity

**DECISION — inherited:** [ownership](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [content](../../09-technical-foundation/DATA_DRIVEN_CONTENT.md) và [persistence](../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md) giữ Company/World authority, deterministic execution, outcome boundaries và strict version identity. Entity-data không là root gameplay thứ ba.

**PROPOSAL:** public provider → raw record có quyền sử dụng → schema/identity/metric validation → normalized source facts → separately reviewed simulation definition → campaign initialization → runtime-owned state. Generated path: versioned world config + rng-v1 + authored tables → definition → relational/budget validation → cùng initialization contract. Origin chỉ quyết định acquisition/provenance, không chọn match rules, salary exemption hoặc information advantage.

Phân biệt năm tầng: public evidence; estimated simulation attributes; generated latent attributes; campaign facts; actor-visible observations. Source identity/roster/history không có quyền ký employment hoặc cập nhật campaign đang chạy. Coach “unknown” không thành50; financial “unknown” không thành0. Real-source example có thể hợp lệ ở staging mà chưa đủ điều kiện initialize. Verified public finance khác scenario budget và generated budget.

**Fidelity boundary:** chỉ player organization có explicit roster/contracts/payroll/coach. Compressed rivals có bounded profile và một competitive-unit reference, không full roster, cash ledger hoặc staff tree. Không gán fictional player IDs vào rival chỉ để UI trông đầy đủ. Nếu một person thật sự đi vào explicit market, World sở hữu đúng person đó; transition từ compressed representation cần mapping/reservation và approval riêng, không nhân đôi một biography.

## 2. Shared schema primitives

Mỗi domain có format/schema riêng, version1. Đây là **documentation envelopes**, không `Content`, `PersonDto`, `CoachDto` hoặc `SaveEnvelope` hiện tại. UTF-8, exact snake_case, unknown/duplicate keys fail; finite JSON numbers; bool không là integer. Không extension bag. Tất cả listed keys required; `N` cho phép null với reason/status đã quy định, không ngụ ý giá trị zero. Display strings NFC1..120; short_name1..12. IDs lowercase ASCII `[a-z0-9:_-]`,1..128; không dùng display name làm key. External IDs opaque case-sensitive1..256, không normalize như internal ID. UTC RFC3339 timestamps; public dates YYYY-MM-DD; campaign days integer≥1; ngày source không cùng field với ngày campaign.

Common envelope có đúng: `format` (domain-specific string), `schema_version` (1), `status` (`PROPOSED — NOT APPROVED`), `example_only` (true), `origin_path` (`public_source_synthetic` hoặc `generated_synthetic` trong examples), `definition` (domain-specific), `source_facts` (domain-specific N), `initialization` (domain-specific N), `evidence` (object). Evidence đúng keys `kind` và `note`: kind=`synthetic_public_path_unknown` hoặc `synthetic_hand_authored_not_seed_output`. Production admission cần envelope/schema khác được duyệt; example_only luôn chặn.

Definition dùng common `definition_id`, `definition_version` và `provenance`. Provenance đúng keys:

| Field | Type / requirement | Semantics |
|---|---|---|
| basis | enum | `synthetic_public_path` / `generated_scenario` trong examples; không `verified` giả |
| source_ref | URI N | `urn:example:*` chỉ synthetic evidence; production public cần provider/source thật và retained lineage |
| as_of | timestamp | Explicit cutoff, không system clock default |
| use_status | enum | examples=`example_only`; tương lai pending/approved_for_named_use/prohibited cần contract/version riêng |
| license_ref | URI N | null ở examples; một string “approved” không thay referenced rights record |
| external_identities | array | entry đúng keys provider, namespace, value, source_ref, verified_at; all non-null khi thực sự verified; examples empty |
| confidence_status | enum | `unknown` cho public-path capability inference; `not_applicable_generated` cho authored latent; không probability1 |

Mọi unknown source value có `unknown_reason` trong source_facts. Source rows đã xác minh từng field mới có evidence riêng; production import phải giữ per-field source IDs, provider/revision/hash, retrieval timestamp, disagreement records và metric definition version, không chỉ provenance summary của examples.

### Identity/reference semantics

| ID / reference | Meaning and owner |
|---|---|
| DefinitionId | Static catalog biography/organization/unit identity; version thay đổi không thay identity |
| EntityId | Campaign identity allocated một lần, stable khi chuyển owner |
| OrganizationId | EntityId của Company hoặc World compressed organization; không TeamId |
| TeamId | EntityId của competitive unit thuộc một organization; ownership link dưới Company/World |
| CoachId / PersonId | Cùng một runtime person identity, không tạo coach ID mới khi đổi employer; player→coach career conversion deferred |
| RosterMembershipId | Một assignment episode dưới People owner, references person/team, start/end; không copy ratings/payroll |
| StaffAssignmentId | Coach-to-team episode dưới People owner; một Head Coach per explicit team |
| EmploymentId | Contract identity dưới Contracts owner; person/employer refs, terms và dates; assignment không là employment |
| ExternalIdentity | Provider+namespace+opaque value; account/brand/team/person namespaces khác nhau; verified crosswalk mới merge |

Roster membership interval `[start_day,end_day)`; null end=open. Employment trong fixture giữ `end_day` **inclusive** đúng current contract; validation không trộn hai convention. One primary team trong scope, một current employer/person và một current team assignment/person. Khi transfer: close assignment, move owner atomically, replace/create employment theo luật, retain person/definition IDs. Rename không rewrite key; historical source memberships chỉ dated evidence. Organization ownership transfer/M&A chưa có mutation rule: reject command, không gán employer mới cho toàn roster ngầm.

## 3. Candidate Numerical Profile — common rules

Các bounds là candidate technical/trial bounds, không final balance. Domain tables bổ sung mọi quantity của domain.

| Field ID | Type | Min | Reference/default | Max | Unit | Source/rationale | Status/confidence | Dependency |
|---|---|---|---|---|---|---|---|---|
| schema_version / definition_version | integer | 1 | 1 | 2147483647 | revision | explicit version | proposed technical | schema review |
| campaign day / period endpoints | integer or null end | 1 | explicit | 2147483647 | day | current tick | inherited boundary | calendar bounds |
| currency amount | integer | 0 | explicit, never infer0 | 1000000000 | minor units | checked integers;100minor/display unit for trial | proposed technical | economy/currency policy |
| distribution weight | integer | 0 | table | 10000 | basis points | sum10000 unless discrete ratio table says otherwise | synthetic | population calibration |
| record count | integer | 0 | actual | 10000 | records | bounded offline contract | proposed cap | scale DV-07 |
| statistical fraction | finite decimal | 0 | null if unavailable | 1 | fraction | no percent-as-integer | diagnostic | measurement evidence |
| sample n | integer | 0 | observed | 10000 | complete units | no duplicate evidence credit | diagnostic | eligibility |

Current arithmetic giữ từng operator của code: `Numbers.Divide(n,d)` ties-to-even với d>0; integer `/` trên nonnegative values=floor, không đổi sang round-even. Offline proposed diagnostics dùng Decimal precision50, final decimals6, round-half-even; lưu unrounded intermediate ở evidence. No division-by-zero epsilon. Overflow/negative physical amount invalid; null required input→unknown hoặc initialization failure. Saturation chỉ ở công thức có clamp được nêu; không clamp malformed input để cứu schema.

## 4. Deterministic generation specification

**FACT:** [rng-v1](../../../src/ManagementGame.Domain/Determinism.cs) SHA-256 length-prefixed NFC UTF-8 fields, keyed domains/purposes và rejection sampling. Reuse algorithm, không triển khai RNG/generator mới trong task này.

**PROPOSAL:** generator_version=`entity-foundation-trial-v01`; each input bundle pins seed uint64 decimal string, config/pack/reference hashes, explicit as_of, namespace registry, sorted pools và typed policy tables. Components `world_namespace`, `cohort`, `region` chỉ `[a-z0-9_-]{1,24}`; slot1..999999 encoded6digits. Reuse Player Data v0.2 delimiter repair. Key=`cohort:{cohort}:region:{region}:slot:{slot6}`; no colon trong components. Organization/team/coach domains lần lượt `entity-org-v01`, `entity-team-v01`, `entity-coach-v01`. Purposes: profile-order, grade, tradeoff, identity-name, age, experience, role-reserve, allocation-tie. Team/coach/employer dùng independent purposes; không derive hidden bonus từ display name hoặc organization fame.

Organization DefinitionId=`org:{world_namespace}:{cohort}:{slot6}`; Team DefinitionId=`team:{world_namespace}:{cohort}:{slot6}`; coach DefinitionId=`coach:{world_namespace}:{cohort}:{slot6}`. Team slot registry riêng với org slot, giữ foreign key rõ; runtime PersonId vẫn một entity namespace. Generated biography ID không chứa employer. Model/config version thuộc definition revision/manifest, không tái dùng slot cho biography khác. External imports được cấp internal registry IDs, không ép provider ID vào generator grammar.

### Order lựa chọn

Org-first rồi mỗi team tự sinh đủ người dễ nhân đôi talent hoặc làm ngân sách không thể trả. **Chọn hybrid hai-pass:**

1. Validate scope/rights/version/names/ID reservations; stage imported identities và khóa các fixed source bindings, chưa khởi tạo người unknown.
2. Allocate organization and team IDs/competition refs; choose initial profile và **initial endowment envelope** từ world config, không tạo money ở nhiều bước.
3. Reserve abstract talent/staff counts cho compressed teams; exclude chúng khỏi explicit pool. Chưa tạo Person/Coach cho các slots này.
4. Build one shared explicit talent/coach definition pool từ admitted definitions; public source unknown blocks applicable initialization, không reroll thành NPC cùng tên. Generated people vẫn dùng separately approved player generator; Player v0.2 chưa approved, nên trial JSON chỉ hand-authored legacy attributes.
5. Allocate one explicit primary roster+coach dưới budget/eligibility. Imported valid bindings cố định; không regenerate real team để “đạt điểm”. Reserve employer/person IDs atomically, không reset constraints bằng retry.
6. Create employment/assignment refs, then unassigned admitted people become World candidates; define quote costs một lần. Compressed rivals giữ profile, không nhận explicit roster ngầm.
7. Register teams, generate schedule bằng existing versioned scheduling convention sau approval; validate graph/budgets/ownership/content identities rồi commit initialization một lần.

Team allocator trial tối đa12explicit players,≤3 eligible people mỗi role,≤4coaches, roster đúng6 (5roles+1reserve), no secondary-role assignment. Enumerate legal role choices, one reserve và one coach; sort tuples ordinal IDs, evaluate ≤10000tuples, reject configuration khi vượt cap. Với12players upper bound3^5×7×4=6804. Chọn feasible tuple theo named team objective rồi tie theo ordinal tuple; đây là deterministic finite search specification, không implementation. Không silently greedy-resample hoặc chạy đến khi ra roster đẹp. Nếu không feasible: report missing role/coach/rights/budget, no partial world, no borrowed person.

Generation tables có finite supports; validate bounds trước draw, reject invalid table thay rejection loop. Rival cohort dùng largest remainder quotas: floor(N*w/sumw), phần còn lại theo descending remainder rồi explicit table order; seed dùng gán slot labels, không thay quota. Coach template dùng categorical weights như domain spec, không áp dụng quota Rival ngầm; Team objective được config chọn rõ. Small N cần exact count config cho archetype demo, không claim theoretical marginal phải bằng realized sample. Same-seed/golden/save-resume/different-seed tests chờ authorized implementation; hand-authored numbers không là seed output.

## 5. Synthetic relational world

[synthetic_world.example.json](examples/synthetic_world.example.json) là seventh, supporting fixture cho sáu examples bắt buộc. Một Company, một explicit primary Team,3compressed rivals/Teams,12explicit players (6Company+6World),4explicit coaches (1Company+3World). Chỉ role IDs LoL candidate trong fixture; không là approved A–E adapter hoặc loadable campaign. Mỗi compressed team reserve6anonymous player slots+1staff slot trong input talent-capacity manifest: total explicit12+reserved18=30player slots, explicit4+reserved3=7coach slots. Đây là **offline scope accounting**, không currency/state root hay actual roster simulation. Strength phải ở range của `legacy-tier70-trial` reference; chưa chứng minh supply realism.

World fixture dictionaries (all keys required):

| Object | Exact fields |
|---|---|
| top-level | format=`entity-world-example-v01`, schema_version=1, status, example_only=true, basis=`synthetic_hand_authored_not_seed_output`, day, reference_catalog, organizations, teams, players, coaches, employments, memberships, staff_assignments, registrations, fixtures, player_market, coach_market, company_plan, talent_capacity |
| reference_catalog | organization_definitions, team_definitions, player_definitions, coach_definitions, competition_ids (unique ID arrays) |
| organization | id, definition_id, fidelity (`player_full`/`compressed`), cash_minor N, operating_cost_minor N, compressed_profile N |
| team | id, definition_id, organization_id, representation (`explicit`/`compressed`) |
| player | id, definition_id, owner_organization_id N (null=World market), role, execution, adaptability, consistency, readiness; legacy synthetic points only |
| coach | id, definition_id, owner_organization_id N, preparation, analysis, adaptability, capacity |
| employment | id, person_id, employer_organization_id, salary_minor_per_day, release_cost_minor, start_day, end_day inclusive |
| membership | id, person_id, team_id, start_day, end_day exclusive N |
| staff_assignment | id, person_id, team_id, staff_role=`head_coach`, start_day, end_day exclusive N |
| registration | id, competition_id, team_id, season_id, status=`registered` |
| fixture | id, competition_id, home_team_id, away_team_id, day |
| player_market / coach_market entry | person_id, fee_minor, salary_minor_per_day, release_cost_minor, deadline_day |
| company_plan | organization_id, team_id, lineup_person_ids (exactly5), reserve_person_ids (derived complement), allocation_execution, allocation_opponent, allocation_meta (sum100), posture |
| talent_capacity | total_player_slots, explicit_player_slots, reserved_compressed_player_slots, total_coach_slots, explicit_coach_slots, reserved_compressed_coach_slots, compressed_reference_id, strength_min, strength_max |

Fixture arrays là serialization của hypothetical owners: Company owns its persons/employments/assignments/plan/cash; World owns candidates, compressed orgs, schedule/registration. `reserve_person_ids` là kiểm tra read projection, không proposed second roster store. Reference catalog chứa IDs, không copy stats; owned Team list derive từ team.organization_id. Foreign keys và definitions có cả external illustrative stubs trong world manifest; public-path examples không được instantiate vì profiles unknown.

## 6. Validation matrix — PLANNED trừ phần executed bên dưới

| Group | Cases / acceptance |
|---|---|
| A Schema | Exact keys/types/nullability, bool-as-number, negative/out-of-range, duplicate IDs/external keys, invalid refs; unknown public capability/finance stays unknown. Unsupported provider field quarantined, không đổi tên đại để lọt schema. |
| B Formula | Empty/one/two samples, zero denominator, extreme/clamp, ties-to-even versus floor, complete reference panels, context mismatch, missing player estimates, no form/preparation double bonus; coach wins không causal quality. |
| C Generation | Same-seed/config/version stable IDs/bytes; different seeds vary permitted dimensions; finite quota/support/covariance checks; order shuffle/purpose isolation; roster/coach/cash feasibility; fail before partial commit. Seeded parts NOT RUN here. |
| D Gameplay | Stronger lineup can lose/chọn khác khi readiness/cost/overload đổi; prep specialist không thắng capacity coach mọi load; rival threat varies repetition/opportunity constraints; no bonus từ origin/chemistry/diagnostic. Toy reversals không là balance gate. |
| E Compatibility | Company/World sole owners; stable transfer IDs; current E/A/K/rules preserved; recurring market unchanged; exact content/save support; actor-safe staff; no salary or outcome duplication. Runtime regressions NOT RUN here. |

Import negative cases: inconsistent public roster snapshots not automatically resolved; overlapping exclusive employment fails admission; unverified name match cannot merge identity; series aggregate cannot be imported as game-level player evidence; source financial statement phải giữ currency/date/consolidation basis, không convert sang Rival.Budget.

Acceptance cho spec package: schemas/examples agree, exact numerical witnesses and graph invariants pass; status labels/links/source preservation checked. Implementation acceptance riêng: deterministic replay/golden on supported versions, transaction/save/actor tests, workload cap và meaningful D/E scenarios. Measurement/long-campaign balance yêu cầu data/protocol được duyệt, không chặn giao spec.

## 7. Verification thực hiện

Local helpers trong `artifacts/entity-data-v01-checks/` là isolated review artifacts, không production validators hoặc generators. Lệnh/kết quả actual được ghi trong [README](README.md):101checks passed,7JSON và26negative cases, cùng finite-support/graph/budget audit. Planned seed/generator/integration/calibration cases vẫn NOT RUN. Mọi examples synthetic, không API/import thật, không real entity ratings/finances, không sửa Player Data, C# hoặc accepted decisions.
