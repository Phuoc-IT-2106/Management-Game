# Player Data Foundation — document index

Status: **PROPOSED — NOT APPROVED**; superseded in part by [rules v4](../RULES_V4_PLAYER_WORLD.md), the player model as
implemented (2026-10-13)  
Current proposal: **v0.2** · Ngày: 2026-10-08 (Asia/Saigon)

v0.2 là design repair để Director review, không phải approval, BUILD hoặc calibrated player pack. **LoL vẫn là working design candidate.** Current inspected runtime: `slice-rules-v3`, content schema3, save schema3; [compatibility contract](RUNTIME_COMPATIBILITY_V02.md) ghi branch/HEAD và dirty-worktree scope.

## v0.2 — đọc trước

| Tài liệu | Trách nhiệm |
|---|---|
| [PLAYER_DATA_REVISION_V02](PLAYER_DATA_REVISION_V02.md) | Thay đổi, disposition10Major/4Minor, evidence và quyết định còn mở |
| [PLAYER_ATTRIBUTE_CATALOG_V02](PLAYER_ATTRIBUTE_CATALOG_V02.md) | L/C conditional candidates, diagnostic/deferred fields,5roles và5scenario pairs |
| [PLAYER_RATING_FORMULAS_V02](PLAYER_RATING_FORMULAS_V02.md) | Covariance/full-mask policy và common game-level variance scale |
| [NPC_GENERATION_V02](NPC_GENERATION_V02.md) | Specialist trade-offs, deterministic keys và bounded recurring-stock proposal |
| [MEASUREMENT_CALIBRATION_PROTOCOL_V02](MEASUREMENT_CALIBRATION_PROTOCOL_V02.md) | Gates A/B/C/D/E, eligibility, holdouts và acceptance proposals |
| [RUNTIME_COMPATIBILITY_V02](RUNTIME_COMPATIBILITY_V02.md) | Preservation obligations, actor layers và integration decision contract |
| [Synthetic math example](examples/shared_scale_v02.example.json) | Hand-authored, không người thật, generator output hoặc save |

Không còn tuyên bố bốn Core attributes. Laning/Combat chỉ có future consumer hypothesis; Vision và game_consistency diagnostic/research; gameplay hiện tại giữ E/A/K/Readiness. **Recommendation: NEEDS REVISION cho full five-role gameplay approval**, nhất là pressure/utility/resource coverage. Xem revision để phân biệt lỗi spec đã sửa với empirical/design dependencies chưa giải quyết.

## v0.1 và independent review — lịch sử giữ nguyên

Từ 2026-10-12 các file v0.1 nằm nguyên văn trong [docs/90-archive/10-vertical-slice/player-data](../../90-archive/10-vertical-slice/player-data/).

| Tài liệu | Vai trò lịch sử |
|---|---|
| [README_V01](../../90-archive/10-vertical-slice/player-data/README_V01.md) | Bản byte-identical của index trước sửa v0.2; claims về versions giữ nguyên như evidence |
| [PLAYER_DATA_CONTRACT_V1](../../90-archive/10-vertical-slice/player-data/PLAYER_DATA_CONTRACT_V1.md) | Identity/provenance/ownership được tái dùng; RatingValue cũ không phải v0.2 |
| [PLAYER_ATTRIBUTE_CATALOG_V1](../../90-archive/10-vertical-slice/player-data/PLAYER_ATTRIBUTE_CATALOG_V1.md) | Four-core proposal cũ |
| [PLAYER_RATING_FORMULAS_V1](../../90-archive/10-vertical-slice/player-data/PLAYER_RATING_FORMULAS_V1.md) | Công thức trước repair |
| [NPC_GENERATION_V1](../../90-archive/10-vertical-slice/player-data/NPC_GENERATION_V1.md) | Phân phối trước repair |
| [VALIDATION_CALIBRATION_V1](../../90-archive/10-vertical-slice/player-data/VALIDATION_CALIBRATION_V1.md) | Kế hoạch và historical document-check results |
| [Real-path example v0.1](../../90-archive/10-vertical-slice/player-data/examples/real_player.example.json) | Synthetic public-data path, không rating người thật |
| [NPC example v0.1](../../90-archive/10-vertical-slice/player-data/examples/npc_player.example.json) | Hand-worked, không seeded generator output |
| [PLAYER_DATA_REVIEW_V1](../../90-archive/10-vertical-slice/player-data/PLAYER_DATA_REVIEW_V1.md) | Independent review NEEDS REVISION; findings không bị sửa để hợp v0.2 |

v0.2 chỉ supersede các proposal measurement/consumer/NPC tương ứng; không sửa accepted decisions hoặc architecture. Cross-links từ historical documents về README này vẫn hợp lệ nhưng không biến historical arithmetic thành current result. Tài liệu lịch sử dùng version scope tại thời điểm viết.

## Authority and labels

[Project State](../../00-project/PROJECT_STATE.md), [Decision Log](../../01-governance/DECISION_LOG.md), [content](../../09-technical-foundation/DATA_DRIVEN_CONTENT.md), [ownership](../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [persistence](../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md) và current source là baseline. [Rules v3 wave1](../RULES_V3_INCENTIVES.md) active theo instruction được repo ghi; các later waves chưa phải runtime.

**FACT:** source/evidence đã quan sát. **DECISION:** accepted authority có nguồn, không phải lựa chọn tác giả. **ASSUMPTION:** điều kiện của synthetic example. **PROPOSAL:** phương án chưa duyệt. **OPEN QUESTION:** cần Director/evidence. Kiểm tra tài liệu PASS không là design acceptance; không có API/data calibration, gameplay edit, commit hoặc push trong design-repair task.

## Required Director Decisions

Giữ nguyên wording PD-D01–D05 từ v0.1. [Revision](PLAYER_DATA_REVISION_V02.md) nêu recommendation và gate cho từng request; đây không phải DEC entries.

| Request | Concrete decision needed before dependent BUILD |
|---|---|
| PD-D01 | Accept/reject LoL as production discipline, or keep this an isolated design exercise. |
| PD-D02 | Select capability set and bounded competitive/management consumers; decide legacy Execution/Adaptability treatment and role mapping. |
| PD-D03 | Approve data source/use rights, supported coverage, benchmark population, uncertainty display and calibration acceptance criteria. |
| PD-D04 | Approve NPC cohort size/role/rarity budgets and lifecycle scope; approve or defer potential and career mechanics. |
| PD-D05 | Approve new definition/runtime DTOs, version boundaries, old-save disposition and actor-safe projections. |
