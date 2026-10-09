# Runtime Compatibility v0.2

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 · Integration decision contract, không implementation  
Liên quan: [revision](PLAYER_DATA_REVISION_V02.md), [catalog](PLAYER_ATTRIBUTE_CATALOG_V02.md), [NPC lifecycle](NPC_GENERATION_V02.md), [v0.1 identity contract](PLAYER_DATA_CONTRACT_V1.md).

## 1. Current source of truth

**FACT:** branch `stage5-sponsor-instruments`; HEAD `14b9f48134264d8c04f3dfb268b93ee295b825f8`; working tree dirty, gồm Domain/Application/Infrastructure, fixture và UI changes đã có. Inspected content=`slice-rules-v3`, content schema3, save schema3. Đây không phải commit chứa toàn bộ source đang đọc. Initial/final hash inventories trong local verification artifacts ghi ranh giới kiểm tra và concurrent changes; không reset worktree.

Trong task có thêm concurrent currency/display-formatting changes ở các Domain/Application paths. Đã đọc lại các consumer liên quan: salary arithmetic, signing/recovery/fatigue, market refresh, match base/variance và schema identities mô tả dưới đây vẫn đúng. Không coi display text là authoritative money hoặc player state; không sửa các thay đổi đó. Final evidence snapshot pin các source paths được trích dẫn sau đối chiếu, không claim cả repo đứng yên.

Nguồn: [Project State](../../00-project/PROJECT_STATE.md), [Decision Log](../../01-governance/DECISION_LOG.md), [ownership](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [content](../../09-technical-foundation/DATA_DRIVEN_CONTENT.md), [persistence](../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md), [continuous rules v2](../RULES_V2_CONTINUOUS_CAMPAIGN.md), [rules v3 incentives](../RULES_V3_INCENTIVES.md). DEC-001–023 không chọn LoL hoặc capability model này. Rules v3 wave1 đang active theo explicit user instruction được repo ghi; yêu cầu Director DEC confirmation vẫn tồn tại. Không xem các later waves chưa implement là current behavior.

## 2. Bảng preservation và decision gates

| Area / source | FACT hiện tại | PROPOSAL nghĩa vụ trước integration |
|---|---|---|
| [Person/Company/World](../../../src/ManagementGame.Domain/Model.cs) | Person có8fields; Company.People sở hữu employed players, World.Candidates sở hữu free candidates | Không sửa record trong nhiệm vụ này. Definition/source/measurement tách offline. Không thêm authoritative Team roster hoặc gameplay owner thứ ba. |
| [Competition](../../../src/ManagementGame.Domain/Competition.cs): Execution | base=round_even(sum(Execution×Readiness)/(lineup_count×100)); legal lineup là5 | Default giữ nguyên. G1 là candidate thay base riêng sau approval, không tự map L/C vào Execution và không cộng cả hai base. |
| Competition: Adaptability | round_even((integer mean player Adaptability+coach)/30), trừ repetition×rival analysis/30 theo integer arithmetic | Giữ authored input, preparation/meta và predictability window3 như current rules. Deferred measured Adaptability không zero/overwrite legacy. |
| Competition: Consistency | integer team mean; spread=max(1,Variance+4×Posture−floor(mean/20)); uniform keyed probability noise; probability clamp5–95% | Giữ legacy field/rng/domain/purpose. `game_consistency` không alias field này. Không claim giảm spread tự tăng upset/favorite decision value. |
| [Simulation](../../../src/ManagementGame.Domain/Simulation.cs): Readiness | SignPlayer=min(current,70); daily Company recovery+1 capped100; selected match players−8 floor1 | Bảo toàn đúng transition; “không reroll” không nghĩa “readiness bất biến”. 90→70 khi ký, 60→60; release/renew không tự phục hồi90. External refresh không cập nhật condition. |
| [Pay](../../../src/ManagementGame.Domain/Seasons.cs), [Market](../../../src/ManagementGame.Domain/Market.cs) | salary=floor(Execution²/5); fresh candidate fee20×salary, release cost3×salary; renewal có demand/reputation | Execution70→50 làm salary980→500 minor units/day. Không dùng G1 q hay mean vector cho salary tự động. Separate value/pay proposal bắt buộc trước gameplay/economy comparison. |
| Market lifecycle | Expired listings được lọc; recurring Refresh fill CandidateCount4 và missing A–E roles; refresh interval28; employment expiry trả đúng người về market | Giữ recurring flow cho tới khi finite-stock alternative có đủ World ownership/DTO/terms/availability tests. No initial-only replacement; no emergency reroll. |
| [Finance](../../../src/ManagementGame.Domain/Finance.cs), v3 incentives | OperatingCost2500/day; minimum roster6; shortfall fine50% fixture prize và không nhận prize; sponsor forfeit clause2; reputation regression25% tới target15..85 | Candidate supply và salary phải chịu constraint sáu người, không chỉ legal lineup5; max roster8. Không đổi financial receipts hoặc trực tiếp cộng cash từ attribute. All fixture coefficients NONCANONICAL. |
| [ContentLoader](../../../src/ManagementGame.Infrastructure/ContentLoader.cs) | Schema3/rules-v3, exact hash, unknown/duplicate fields rejected | New artifact/pack cần contract/version/manifest riêng; không append snake_case example vào fixture. |
| [SnapshotDtos](../../../src/ManagementGame.Infrastructure/SnapshotDtos.cs), [SnapshotStore](../../../src/ManagementGame.Infrastructure/SnapshotStore.cs), [validation](../../../src/ManagementGame.Infrastructure/SnapshotValidation.cs) | Save schema3, exact rules/content/RNG/canonical identity,8-field PersonDto; schema2 rejected | New-campaign-only recommended cho implementation đầu; old build/pack hoặc reject theo support matrix. Không hứa current loader support nhiều versions, không retroactive rewrite. |
| [Observations/CoachPolicy](../../../src/ManagementGame.Application/Observations.cs), [contracts](../../../src/ManagementGame.Application/Contracts.cs) | Own roster Execution/readiness; candidate ability band; coach chọn theo Execution×Readiness bằng observed data | Không leak latent K, d/x, source raw rows, private World pool hoặc future potential. G1 cần read model/authority riêng được duyệt trước; UI/staff cùng permitted projection. |

Technical `Execution` record là campaign identity/receipts; `Plan.Execution` là allocation và `Work.Execution` là completed preparation; không field nào tự trở thành player skill. Domain/Application engine-independent; external data không gọi trong simulation loop.

## 3. Probability-noise counterfactual

**FACT + ASSUMPTION uniform enumeration:** current Balanced/Variance8 cho legacy K40→spread10, K80→spread8. Trung bình win probability với center.20 hoặc.50 giống nhau cho hai spreads khi không clamp. Center.06 gần lower bound cho mean≈.086667 và≈.080706. Cùng seed có thể khác kết quả; downstream commercial surprise dùng realized probability cũng có thể khác. Vì vậy không gọi K vô dụng, cũng không gọi nó một game-level variance parameter.

**PROPOSAL:** giữ legacy K cho current rules. Nếu muốn game_consistency tác động future score variance phải spec một resolver distribution/outcome mapping mới và chứng minh decision value trước đổi. Không cần đưa luật ấy vào v0.2 chỉ để tạo consumer cho diagnostic.

## 4. Boundary contract cho artifact v0.2

Identity, provenance, profile/observation ownership và deduplication của [contract v0.1](PLAYER_DATA_CONTRACT_V1.md) được tái dùng như proposal; **RatingValue semantics và example envelopes v0.1 không phải v0.2**. Không dùng method `performance_proxy_v01` với công thức mới. v0.2 measurement record proposal có các field tách biệt:

| Field | Type / rule |
|---|---|
| attribute_id | laning, combat, vision_activity, game_consistency; không tự map bằng tên legacy |
| scale_id, reference_id, model_version | Required stable versioned refs; reference includes role/context/mask |
| layer | latent_capability / measurement_estimate / scouting_estimate; không tự convert bằng cast |
| status | authored_latent / diagnostic_estimate / unknown / not_applicable / deferred |
| value | Nullable finite number; L/C/K display1..99; vision diagnostic giữ units riêng; null luôn reason |
| uncertainty | Nullable typed interval/method/reference; null reason not_estimated hoặc not_applicable; không confidence1 vì NPC |
| evidence_ref | Measurement/generation/observer evidence có quyền riêng; không đưa raw trace vào actor projection |
| runtime_admissible | false trong mọi v0.2 example/research artifact |

Với public record, latent value **unknown**, ngay cả khi measurement point rất rõ. NPC authored latent không tự có measurement estimate. Scouting estimate unknown tới khi observer acquisition được spec. Shared gameplay input sau này chỉ nhận approved capability snapshot theo cùng policy cho mọi origin; source origin không selector của match rules.

Không viết đầy đủ production JSON Schema/DTO trong task này: source rights registry, benchmark coefficients, calibrated observation model và consumer chưa có. [JSON example](examples/shared_scale_v02.example.json) là envelope test toán học riêng, không pretended PlayerDefinition/save. Keys của envelope được mô tả trong revision; v0.1 JSON giữ nguyên làm historical evidence.

## 5. Integration decision contract — chưa ký

| ID | Deliverable phải review | Acceptance obligation / owner |
|---|---|---|
| IC01 | D01/D02: scope, five-role omissions, A–E mapping | Director chọn explicit mapping hoặc reject LoL adoption; không gán theo thứ tự tự động. |
| IC02 | Mean/variance acquisition policy, required fields và unknown admission | Data/Simulation reviewer: cùng scale/reference/uncertainty cho real/NPC; unknown required capability blocks initialization. Không prior50 ẩn. |
| IC03 | G1 hoặc consumer thay thế, pay/valuation và coach decisions | Domain/Application owners: before/after scenarios, base isolation, outcome compatibility; legacy E/A/K vẫn valid cho baseline. Salary không đổi trừ quyết định riêng. |
| IC04 | Recurring market/pool design | World/People: idempotent relisting, stable IDs, bounded rarity, đủ roles/roster, no duplicate owners; initial-only cohort không thay market. |
| IC05 | Typed definitions/runtime/observation DTOs và version support matrix | Infrastructure/Application: current3 không tự là future4; choose version at implementation review; pin hashes; old saves immutable. |
| IC06 | Readiness/financial/authority regression contract | Sign90→70, recovery/fatigue đúng, hire/release/expiry giữ identity và transitions; manual/delegated policy parity, receipt idempotency, no leaks. |

Recommended sequence: Director scope → permitted measurement → gates B/C → consumer D → approved isolated implementation/new campaign → replay/save/actor tests → economy E. Không thực hiện sequence BUILD này trong nhiệm vụ. Design spec không discharge DV-06 cross-machine, DV-07 scale hoặc DV-08 migration obligations.
