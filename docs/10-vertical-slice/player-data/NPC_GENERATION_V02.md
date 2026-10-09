# NPC Generation v0.2

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 · Không có generator implementation  
Liên quan: [catalog/G1](PLAYER_ATTRIBUTE_CATALOG_V02.md), [scale](PLAYER_RATING_FORMULAS_V02.md), [protocol](MEASUREMENT_CALIBRATION_PROTOCOL_V02.md), [runtime](RUNTIME_COMPATIBILITY_V02.md).

## 1. Mục tiêu trước khi chọn phân phối

**PROPOSAL — targets của trial, không là thống kê người thật:**

1. Top/Mid/Bot phải có lane và contribution specialists ở cùng mean budget; |L−C|≥12 cho hai archetype này, không còn chỉ {-2,0,2}.
2. Cùng B/readiness/cost: lane specialist thắng ở G1 h=.8, contribution specialist thắng ở h=.2; tại h=.5 hòa. Không có unconditional dominance trên cả ba policy thử.
3. Population L/C correlation trial dưới .8; báo cả conditional covariance. Đây là design check chống hai cột gần trùng, không phải mục tiêu fit người thật.
4. Bounds1..99 và quota elite kiểm tra trước draw; không chi điểm vào Vision/Consistency diagnostic để “trả giá” cho mean strength.
5. Jungle/Support không có Laning; không được quảng cáo archetype pressure/protection khi chưa có consumer. Sự đa dạng phong cách hai role này vẫn là R-M01 open.

## 2. Scale và acquisition

NPC L/C là authored latent mean trong cùng `scale_id/reference_id/role` như [formulas](PLAYER_RATING_FORMULAS_V02.md), đại diện theta=(R−50)/15. `game_consistency` latent k biểu diễn variance ratio v=4^((50−k)/49). Không phải authored `Person.Consistency` hiện tại; không automatic adapter.

Generated origin không có external ID, provider statistics hay measurement_confidence=1. Public data tạo measurement estimate; NPC tạo latent value. Chung struct không cho phép ghép hai tầng này thành cùng runtime truth. Trước nhập mixed pack, gate C phải duyệt acquisition rule/uncertainty: một observation-error model đã kiểm chứng áp dụng nhất quán, hoặc chỉ nghiên cứu riêng từng nguồn. v0.2 chọn phương án thứ hai cho tới khi có evidence. Không thêm sức mạnh cho NPC vì biết truth chính xác.

Vision chỉ có diagnostic khi có observations được khai báo; không generate “latent ward score” rồi tiêu budget. Deferred attributes, potential, personality, champion specialization và measured Adaptability vẫn null. Không tạo history giả hoặc bonus theo quốc tịch.

## 3. Phân phối trial và covariance

**ASSUMPTION:** trial N200, một region, 40 người mỗi role. Tất cả hệ số dưới đây là synthetic thiết kế để test targets, chưa empirical fit. Identity/name/age không tham gia utility. Không tiếp tục stage shift năng lực của v0.1; tuổi/kinh nghiệm chỉ metadata có provenance fictional.

| B — mean ability budget | Share / quota N200 | Lý do trial |
|---|---|---|
| 35 | 25% / 50 | replacement |
| 50 | 50% / 100 | middle anchor |
| 65 | 23% / 46 | stronger candidate |
| 85 | tối đa2% / 4 | exceptional quota |

Cho N khác, exceptional=floor(.02N), còn lại largest remainder theo25:50:23; tie theo thứ tự bảng. Role allocation equal với tie top/jungle/mid/bot/support; independent keyed slot ordering để gán marginal labels, không đảm bảo mỗi role có elite. N200 có4elite<5roles nên một số role không có elite là hợp lệ. Chỉ cho trial50≤N≤2000, mỗi role≥9 để chịu max roster8 trong giả thuyết pool bên dưới; cap không phải production world size.

Laners: chọn d=+8 (lane specialist) với35%, d=−8 (contribution specialist)35%, d=0 (balanced)30%. Chọn x∈{−2,0,2} với25/50/25%, purpose khác; độc lập có điều kiện B/role trong trial model:

```text
L = B + d + x
C = B - d - x
L+C = 2*B
```

Jungle/Support: L=null/not_applicable, C=B; chưa có stylistic archetype được release. Không dùng L+C để so tổng sức mạnh họ với laner. Diagnostic K có support {1,25,50,75,99}, weights {1,9,80,9,1}% độc lập B,d,x trong trial; đây là hypothesis covariance0 với mean ability, không quy luật thực tế. K không tiêu budget và không tác động G1. Khi muốn giữ nó làm gameplay, phải thiết kế utility/cost mới trước phân phối mới.

Không clip/projection trong trial: B35..85, |d+x|≤10 → L,C25..95. Configuration làm vượt bounds phải fail trước sinh, không resample hoặc dồn phần dư sang K. Với N200 marginal shares:

```text
E[B]=50.4; Var(B)=132.34
E[d+x]=0; Var(d+x)=46.8
Var(L)=Var(C)=179.14
Cov(L,C)=85.54; Corr(L,C)=85.54/179.14 ≈ .4775
L-C in {-20,-16,-12,-4,0,4,12,16,20}
```

Tại B cố định, correlation L/C=−1 là chủ ý của đường trade-off hai thuộc tính; không được gọi các residual độc lập. Có thể quá cứng để mô tả đời thật: gate C/D phải đánh giá thêm independent budget variation có chi phí trước sửa. Population correlation dương do B chung; đây không phải lỗi. Finite support có36 combinations B×d×x, không phải output của36 players hay seed. Quota assignment finite cohort có thể làm realized covariance khác theoretical marginal; sau BUILD mới có multi-seed checks.

Điểm utility trial U_h=hL+(1−h)C=B+(2h−1)(d+x). Ở B50,x0: (58,42) và(42,58) cho U_.8=54.8/45.2, U_.2 đảo lại. Các quantization/base probability clamps có thể che khác biệt; trial đại số chưa là gameplay D PASS. Giá/lương chưa có consumer map, vì vậy chưa claim hai NPC có cùng tổng chi phí thực tế.

## 4. Identity và deterministic key

**DECISION — inherited:** giữ rng-v1 trong [Determinism.cs](../../../src/ManagementGame.Domain/Determinism.cs). Không viết RNG mới hoặc generator ở nhiệm vụ này.

**PROPOSAL:** sửa collision bằng grammar chặt, không thay RNG: `world_namespace`, `cohort_id`, `region_id` chỉ `[a-z0-9_-]{1,24}`; không colon, Unicode hoặc whitespace. Slot global trong cohort, integer1..999999, encode đúng6 chữ số; leading zero chỉ là output encoding. Cùng logical component không có hai spelling. Definition ID:

```text
player:npc:{world_namespace}:{cohort_id}:{slot6}
event_id = cohort:{cohort_id}:region:{region_id}:slot:{slot6}
domain = player-data-npc-v02
```

World namespace được reserve trong pack/campaign registry, không chỉ dựa vào world seed; collision registry fail, không random retry. ID dài dưới128; region là frozen assignment trong cohort manifest, không thay đổi theo employer. DefinitionId không chứa generator version: đổi công thức là definition revision/new cohort, không làm biography đổi theo update. EntityId dùng persisted campaign allocator, không dùng DefinitionId như entity và không dùng wall clock.

Counterexample cũ: `(cohort=a:region:b, region=c)` và `(cohort=a, region=b:region:c)` cùng concat. v0.2 reject cả input có colon; accepted grammar có parse duy nhất. rng-v1 length-prefix toàn event ID không tự giải quyết lỗi delimiter bên trong; validation này phải chạy trước draw. NFC vẫn áp dụng name pools và outer RNG như hiện tại; không đổi opaque external IDs theo grammar NPC.

Inputs bắt buộc: seed uint64, generator version, config/hash, explicit as_of, occupied namespace/ID registry, sorted NFC unique name pools và fixed slot allocations. Keyed ordering theo Draw rồi ordinal slot để gán quotas. Purpose tách `role-order`, `band-order`, `archetype`, `lane-tradeoff`, `game-variance`, `age`, `experience`, `name-given`, `name-family`. Random integer intervals đúng bảng, không Gaussian hay modulo shortcut. Name/employer change không đổi capabilities/identity; same config/seed/version phải cùng output; yêu cầu này **chưa được test generator**.

Tuổi trial18..32, experience0..min(age−16,14), giả lập birth_year với precision year; không tự đặt sinh nhật January1 để dùng vào future aging. Không shift ability theo age, không promise growth. Name trùng được phép, ID trùng fail.

## 5. Recurring market và active rarity

**FACT:** [Market.Refresh](../../../src/ManagementGame.Domain/Market.cs) đang bổ sung candidates, bảo đảm role còn thiếu khi refresh; [Simulation](../../../src/ManagementGame.Domain/Simulation.cs) gọi refresh định kỳ28ngày theo fixture. v0.2 không thay generator này; không tắt market khi cohort research kết thúc. Không claim missing role được bổ sung tức thì giữa hai kỳ.

**PROPOSAL cho decision contract sau này:** nếu Director chọn fixed stock trial, có N200 durable people, mỗi người đúng một owner Company hoặc World. World giữ pool và listing là reference tới person, không bản sao Person. Listing hết hạn trả người về pool; tuyển dụng chuyển ownership; release/contract expiry trả đúng người về World, không reroll. Mỗi kỳ vẫn fill candidate target và tất cả missing roles trước các slot còn lại. Actor chỉ thấy listings được phép, không xem toàn latent pool.

Tại mỗi kỳ: lọc eligible World people; ưu tiên missing roles theo role order rồi fill target bằng keyed order theo `(market-cycle, person-id)` đã version; loại active listing và employed IDs. Deadline/terms theo contract được duyệt, không tự reset salary khi relist. N200, mỗi role40, Company max8, không có full rival rosters tiêu stock trong trial ⇒ còn ít nhất32 ngoài Company mỗi role. Đây là proof availability trong phạm vi giả định, không proof affordability, max listing cap hay tương lai rival ownership. Nếu rules mới phá điều kiện này, fail admission hoặc revise pool policy; không sinh emergency elite.

Elite budget tính trên **toàn active pool + Company**, không riêng mỗi batch listing: tối đa4 B85 cho N200. Tuyển/release/relist không tạo slot mới nên không có2% draw vô hạn tích lũy. New entrants, retirement, career growth và active full rival rosters deferred; trước thêm phải duyệt inflow/outflow và replace-slot rules bảo toàn scarcity. Fixed pool có thể lặp tên và thiếu cảm giác thế hệ mới — trade-off phải được Director chấp nhận. Existing recurring generator tiếp tục cho tới khi một chuyển đổi có đầy đủ World DTO, listing policy, persistence, economic tests và authority được duyệt.

## 6. Evidence và admission

Ví dụ tupleB50,d8,x0 là hand-authored arithmetic, không seed output. [JSON v0.2](examples/shared_scale_v02.example.json) không tạo person, IDs allocator, public match records hoặc save. Finite PMF/collision algebra checks nằm trong [revision](PLAYER_DATA_REVISION_V02.md). Distribution realism, seeded determinism, realized quotas, long-campaign affordability và observation fairness đều chưa xác minh. Chỉ khi gates C/D/E đạt mới được đề nghị đưa population vào cùng game simulation.
