# Player Data Foundation v0.2 — Design Repair

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 (Asia/Saigon) · PLAN → SPEC → VERIFY  
Owner: Simulation Systems / Data Model Design

## Kết quả và phạm vi

**PROPOSAL:** v0.2 sửa measurement contract và làm rõ integration decisions; không biến bốn candidate thành bốn Core. Laning/Combat là conditional consumer candidates cho G1 có giới hạn; Vision diagnostic-only; game_consistency có common variance-scale research contract, không thay runtime Consistency. Các luật gameplay hiện tại tiếp tục nguyên trạng.

**Recommendation: NEEDS REVISION trước approval cho mô hình gameplay đầy đủ năm role.** Bộ tài liệu này đủ cụ thể để Director review lựa chọn phạm vi tiếp theo. Các lỗi đại số, mask policy, horizon, key grammar và readiness wording đã có phương án sửa và focused evidence; utility/pressure/resource coverage, empirical measurement, acquisition fairness và economy vẫn chưa được chứng minh. Không gọi document PASS là design acceptance.

### Tài liệu v0.2

| File | Nội dung |
|---|---|
| [Catalog](PLAYER_ATTRIBUTE_CATALOG_V02.md) | Disposition8candidates, role×capability×consumer, G1 exact effect,5paired scenarios và exclusions |
| [Formulas](PLAYER_RATING_FORMULAS_V02.md) | Covariance scale, full-panel missingness, game-level variance, common scale, arithmetic và lineage |
| [NPC](NPC_GENERATION_V02.md) | Targets trước parameters, trade-off/covariance, keys, recurring finite-stock alternative |
| [Protocol](MEASUREMENT_CALIBRATION_PROTOCOL_V02.md) | Context/feasibility, split/holdout/unknown, thresholds và5gates A–E |
| [Runtime](RUNTIME_COMPATIBILITY_V02.md) | Current-source assessment và integration decision contract |
| [Synthetic math JSON](examples/shared_scale_v02.example.json) | Minh họa các tầng value/unknown, không PlayerDefinition hoặc save |

## Source baseline và preservation

**FACT:** initial branch `stage5-sponsor-instruments`, HEAD `14b9f48134264d8c04f3dfb268b93ee295b825f8`; rules=`slice-rules-v3`; content/save schema3 được xác nhận lại từ fixture/loader/store. Worktree có nhiều thay đổi chưa commit của công việc khác. [Runtime](RUNTIME_COMPATIBILITY_V02.md) liên kết source được đọc; initial inventory giữ951file hashes cùng git status trong `artifacts/player-data-v02-checks/initial-snapshot.json`. Không dùng HEAD một mình để mô tả dirty source.

Năm spec Markdown v0.1, hai JSON cũ và [independent review](PLAYER_DATA_REVIEW_V1.md) được giữ nguyên. README là ngoại lệ do yêu cầu cập nhật index: lưu bản byte-identical ở [README_V01](README_V01.md) trước thay index; cùng thư mục nên các relative links lịch sử còn hợp lệ. Index mới giữ anchor `required-director-decisions` để link từ review không gãy. Các kiểm tra historical của v0.1/review vẫn là kết quả ở thời điểm cũ, không rerun rồi sửa báo cáo cũ để ép inventory mới PASS.

**DECISION — inherited:** [DEC-010/012/014/017/018/019/022/023](../../01-governance/DECISION_LOG.md) giữ ownership, outcomes, actor-safe decisions và architecture; không approve các proposal này. **ASSUMPTION:** examples/toy PMFs hoàn toàn synthetic. **OPEN QUESTION:** Director có chấp nhận phạm vi measurement/consumer hẹp hay yêu cầu thiết kế pressure/utility/resource trước? Không edit accepted decisions hoặc tự chọn production LoL.

## Disposition của mọi finding

“Sửa ở spec” nghĩa là có contract mới và bằng chứng đúng phạm vi, không phải đóng finding bằng quyền Director hoặc đã pass empirical gates.

| Finding | Thay đổi v0.2 / evidence | Disposition và phần còn mở |
|---|---|---|
| R-M01 five-role coverage | Catalog bỏ4Core, thêm G1 contract và5scenario pairs/negative controls | **PARTIAL / OPEN scope:** thiếu pressure, utility, resource response. Director phải chấp nhận exclusions hoặc yêu cầu design tiếp. |
| R-M02 shared Consistency scale | Log variance map1..99 và inverse chung cho public diagnostic/NPC latent; không đánh đồng layers | **SPEC REPAIRED**, A support checks; empirical C và initialization policy vẫn open. |
| R-M03 horizon/clipping/masks | Game moments trước clipping, fixed full panel; no series-mean variance; no common-mask intersection | **SPEC REPAIRED**, synthetic counterexamples; finite-sample/serial/context confounding B vẫn open. |
| R-M04 composite/missing | Covariance normalization; missing unfavorable feature→unknown, không renormalize release | **SPEC REPAIRED**, unit-variance/mask checks; strict coverage/selection trade-off B open. |
| R-M05 feasibility/context | Explicit registry, single-context default, bridge requirements, completeness reports/gates | **PROTOCOL SPECIFIED**, feasibility chưa biết vì không có dataset. Không đánh dấu resolved empirically. |
| R-M06 E/A/K integration | Giữ current consumers, G1 replace-base-only candidate, no salary/legacy adapter; probability-noise counterfactual | **BOUNDARY REPAIRED**, consumer/pay decisions IC01–03 và D/E open. |
| R-M07 readiness wording | Phân biệt stable identity/no reroll với min(readiness,70), daily/match transitions | **SPEC REPAIRED**, arithmetic90→70; runtime code không đổi, integration regression chưa chạy. |
| R-M08 NPC diversity | ±lane/contribution trade-off; target absolute(L−C)≥12, finite support covariance, utility reversals | **SPEC REPAIRED for laners**, theoretical evidence; realized seeds, empirical realism và Jungle/Support styles open. |
| R-M09 recurring lifecycle | Existing market tiếp tục; bounded fixed-stock relisting option với active quota/role proof | **CONTRACT SPECIFIED**, Director chưa chọn pool/lifecycle; DTO/economy E open. |
| R-M10 acceptance target | Separate A/B/C/D/E, primary/baselines/holdouts/thresholds và stopping rules | **PROTOCOL SPECIFIED**, thresholds proposed, không có calibration PASS. |
| R-m01 confidence | Bỏ uncalibrated bands; raw adequacy dimensions và not_estimated intervals | SPEC REPAIRED; coverage of uncertainty cần B. |
| R-m02 keys | Delimiter-free component grammar; namespace registry; reject old collision inputs | SPEC REPAIRED, collision checks; rng-v1/generator golden chưa chạy. |
| R-m03 lineage | Per-feature/context/row panel/model numerical profile và required hash inventory | CONTRACT SPECIFIED; actual benchmark/source/rights artifacts chưa có nên publication blocked. |
| R-m04 stale compatibility | Current versions xác nhận từ source ở task này; v0.1 không bị rewrite | SPEC REPAIRED trong v0.2; future source drift phải recheck. |

## Lựa chọn thiết kế và trade-offs

1. **Strict missingness thay adaptive renormalization:** ngăn mất evidence biến thành tăng rating, đổi lại nhiều unknown; không tuyên bố unbiased. Mask-model chỉ trở lại sau bridge/coverage validation riêng.
2. **Covariance-normalized composite:** sửa đơn vị trong reference, không sửa causal attribution hoặc disconnected tiers. Mean display vẫn50+15theta nhưng model version khác; v0.1 values không tự migrate.
3. **Game-level latent variance:** k1/50/99 đại diện v4/1/.25; measured estimate/scouting knowledge riêng. Không dùng hậu nghiệm point estimate như NPC truth, không đổi probability-noise consumer.
4. **L/C specialist geometry:** budget của hai mean attributes, không bù bằng diagnostic Vision/K; cost-aware utility và future pay vẫn cần thiết. Fixed-B covariance−1 minh bạch, không giả lập realism.
5. **Fixed-stock option:** recurring listings không đồng nghĩa recurring new people; active rarity hữu hạn nhưng có thể lặp tên. Không chọn thay current generator trước lifecycle/DTO approval.

## JSON example contract

`shared_scale_v02.example.json` chỉ là math example. Required top keys: format=`player-data-v02-math-example`, spec_version=`v0.2`, status=`PROPOSED — NOT APPROVED`, example_only=true, basis=`synthetic_hand_authored_not_seed_output`, scale_id, consistency_pairs(array), missing_feature_case(object), npc_profiles(array). Unknown/duplicate keys hoặc NaN/Infinity fail focused checker.

Mỗi consistency pair có variance_ratio≥0, display_k1..99 và acquisition records cho `public_synthetic`/`npc_synthetic`: layer tách latent/measurement/scouting, runtime_admissible=false. Public latent null; NPC measurement null; scouting null cho cả hai. No real identity, provider claim, source hash bịa hoặc generated person. `missing_feature_case` giữ old62/70 và v02_missing=null/status_unknown. `npc_profiles` lưu B,d,x,L,C, hand-authored, không world seed. Đây là ví dụ nhỏ đủ minh họa layer change; không tạo một production schema nữa.

## Verification thực sự thực hiện

Lệnh chạy từ repository root:

```text
python artifacts/player-data-v02-checks/math_checks.py
python artifacts/player-data-v02-checks/verify_docs.py
```

**FACT — kết quả đã chạy:** mathematical checks **PASS, 337 assertions**; document/JSON/preservation checks **PASS, 435 checks**. Kiểm tra 191 local links, cả3JSON parse được,8historical files giữ nguyên hash, README cũ archive byte-identical,22directly reviewed source hashes không đổi sau đối chiếu. Reports nằm ở `math-results.json`, `verification-results.json` cùng thư mục artifacts. Helpers chỉ kiểm tra toán học/structure; không tạo NPC, không gọi RNG seed để xuất người, không fit real data hoặc chạm runtime.

Concurrent changes được ghi riêng ở7Domain/Application paths, chủ yếu currency/display formatting trong các consumer đã đối chiếu; không gán các chỉnh sửa đó cho task này hoặc revert chúng. Current rules/content/save vẫn v3/schema3. Preservation claim áp dụng cho các file lịch sử và phần không do task này sửa, không phải tuyên bố worktree sạch.

Coverage bắt buộc: endpoints/inverse common scale; reproduce62→70 rồi unknown; covariance role/mask; games-vs-series; clipping; finite NPC PMF/diversity/counterfactual; extreme/zero; key collisions; strict JSON và links; preserved historical hashes và reviewed source drift. Numerical checks có tolerance công khai; boundary Decimal round-even kiểm tra riêng.

Không chạy production C# tests/build, API, data calibration, NPC generator, save migration hoặc long campaign. Không sửa Person/content/save/Decision Log; không commit/push. Gate A focused PASS không đóng B–E hoặc Director requests.

## Director requests — giữ đúng ý nghĩa PD-D01–D05

Các wording gốc được giữ nguyên trong [index](README.md#required-director-decisions) và [README lịch sử](README_V01.md#required-director-decisions).

| Request | Recommendation / trade-off | Chặn bước nào? |
|---|---|---|
| PD-D01 | Tiếp tục LoL isolated design candidate; chưa chọn production. Production choice cần data/consumer feasibility; reject thì giữ architecture lessons | Production discipline/content, không chặn math research |
| PD-D02 | Giữ diagnostics trước; chọn có chấp nhận G1 scope hẹp hay cần bounded pressure/utility/resource design. Vision/K không Core; legacy E/A/K giữ nguyên. Mapping A–E phải explicit | Full gameplay approval, D và BUILD; đây là unresolved design lớn nhất |
| PD-D03 | Duyệt một supported/context/rights scope và protocol pilot trước data work; strict full-mask hoặc commission mask-model study. Common scale không là absolute skill | Empirical B/C, data use và calibration-success claims |
| PD-D04 | Chọn trial N200 và L/C diversity hypothesis; cân nhắc fixed-stock recurring option thay new-entry lifecycle. Defer potential/career | Population integration/E; không tự tắt recurring market |
| PD-D05 | New campaigns only cho adoption đầu; exact-version support matrix, layer/actor DTO và no migration mặc định | Runtime/pack/save BUILD và information exposure |

Next action đề xuất: Director review PD-D02 scope trước; sau đó freeze study population/acceptance protocol và thực hiện coverage audit được phép. Nếu yêu cầu đủ cả năm strategic pairs, cần một design revision nữa trước gameplay calibration. Không mở full tactical simulator để lấp khoảng trống trong dữ liệu.
