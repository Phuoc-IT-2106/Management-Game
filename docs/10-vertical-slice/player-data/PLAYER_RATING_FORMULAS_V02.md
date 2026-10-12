# Player Rating Formulas v0.2

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 · Offline specification; không sửa Competition  
Liên quan: [catalog](PLAYER_ATTRIBUTE_CATALOG_V02.md), [protocol](MEASUREMENT_CALIBRATION_PROTOCOL_V02.md), [NPC](NPC_GENERATION_V02.md), [v0.1](../../90-archive/10-vertical-slice/player-data/PLAYER_RATING_FORMULAS_V1.md).

## 1. Ba tầng không được đánh đồng

**PROPOSAL:** `latent_capability` là tham số ẩn của một reference/model đã khai báo; `measurement_estimate` là ước lượng từ observations kèm uncertainty và selection limits; `scouting_estimate` là tri thức có ngày của một actor, do Company.Information sở hữu. Không coi posterior mean/public proxy là chân lý rồi so với NPC truth như hai vật đo giống nhau.

Các định nghĩa feature, units, physical validity, source/identity và denominator trong v0.1 được giữ, trừ pipeline aggregation/normalization/Consistency bên dưới. KDA vẫn diagnostic với max(1,deaths); KP 0/0 là unknown/no_opportunity; numerator>0 trên team_kills=0 là invalid. duration≤0 invalid; damage/gold với gold=0 unknown; sigma=0, covariance denominator≤0, v_ref≤0, empty sample hoặc trọng số tổng 0 đều unknown/error rõ lý do. Không epsilon, infinity hoặc tự thay bằng 50. Division hợp lệ không chứng minh construct hợp lệ.

## 2. Common scale có điều kiện reference

Mỗi artifact khai báo `scale_id`, `role`, `competition_group`, `season`, `patch_era`, `metric_definition_id`, `feature_mask`, `reference_id`, `as_of` và arithmetic/model version. “Cùng thang” nghĩa là cùng estimand, transform, target context và admission policy; hai con số giống nhau ở disconnected tiers không là sức mạnh tuyệt đối giống nhau.

**PROPOSAL:** mean latent L/C là kỳ vọng composite performance trong target reference, không là mechanics hay toàn bộ đóng góp chiến thuật. NPC latent mean và ước lượng public dùng cùng map:

```text
R(theta) = clamp(round_even(50 + 15*theta), 1, 99)
theta(R) = (R-50)/15       # representative value của authored integer
```

R ở biên có saturation flag; inverse không khôi phục giá trị đã clipped. Chỉ round để display/pack integer tại boundary xác định, không fit trên integer. Reference là role-relative; chưa có cách so utility một điểm Top với một điểm Support chỉ nhờ normalize.

## 3. Composite normalization và mask

So sánh: (A) giữ weighted index nhưng relabel — trung thực nhưng spread vẫn khác giữa roles; (B) normalize full composite bằng covariance — sửa đơn vị rõ, không giải quyết causal bias; (C) fit riêng từng mask rồi link bằng anchor chung — giữ coverage nhưng cần overlapping data và held-out equivalence; (D) impute model-based — cần model missingness và uncertainty, không có dữ liệu để chọn. **Khuyến nghị B với full-mask admission; C/D deferred.**

Fit mu, sigma và covariance Sigma của các oriented feature z trên **cùng complete reference rows**, cùng population weighting, không lấy mỗi cột từ một population khác. Không clip z trước fit/variance. Với w theo role trong catalog:

Reference fitting weights cụ thể: mỗi training player có mass1/P; trong người đó mỗi series nhận W_j/sum(W); mỗi game trong series nhận1/m_j. Mu_f=sum(b_i*x_if), sigma_f²=sum(b_i*(x_if−mu_f)²), Sigma_fg=sum(b_i*z_if*z_ig), với b_i là tích ba weights trên và tổng bằng1. Dùng training cutoff cho age weights, cùng full feature panel và actual role/context. Như vậy prolific player hoặc series dài không tự có nhiều reference mass hơn. Chỉ fit trên train partition theo protocol; thay weighting tạo reference/model version mới.

```text
z_f = direction_f * (x_f-mu_f)/sigma_f
d2 = transpose(w)*Sigma*w
u_i = (sum(w_f*z_if) - mu_composite)/sqrt(d2)
```

Population-centered reference z cho mu_composite=0 theo đại số; artifact vẫn lưu giá trị fit/rounding thực. Sigma phải symmetric positive semidefinite; d2>0; không đòi inverse nên singular Sigma vẫn được nếu d2>0. Gần suy biến hoặc numerical discrepancy phải bị báo, không tự thêm ridge. Trong reference fit, population Var(u)=1 (sai số arithmetic đã khai báo). Không đảm bảo holdout Var(u)=1 hoặc u có phân phối chuẩn. Correlation giữa attributes vẫn phải audit; không cộng KDA hay damage/gold như thêm bằng chứng độc lập.

**Fixed evidence panel:** freeze danh sách games/series hợp lệ theo metadata trước khi nhìn giá trị; mọi game trong window của một attribute phải có toàn bộ positive-weight features, context và reference được hỗ trợ. Có một required feature thiếu → attribute release `unknown:incomplete_panel`; lưu coverage và diagnostic complete-case riêng nhưng không xuất rating dùng được. Structural invalid/missing-context games không được lặng lẽ xóa khỏi denominator; nếu thuộc scope window thì block release. Game thực sự ngoài scope theo protocol đã đăng ký có reason/exclusion count. KP no_opportunity có thể khiến C/Consistency unknown: đây là chi phí feasibility được công khai, không “sửa” bằng zero.

Mask khác nhau được tính riêng **chỉ như diagnostic với mask ID**, không gom hoặc xếp hạng như cùng construct. Không chọn mask theo người chơi hay giao của các rows còn đẹp. Missingness có thể làm release trở thành unknown, tuyệt đối không biến 62 thành 70. Thêm dữ liệu không đầy đủ có thể làm mất release; UI phải nói thiếu evidence, không báo người chơi yếu đi. Nếu coverage quá thấp, thu hẹp scope hoặc revise estimator trước data collection; không giảm gate sau khi thấy holdout.

### F02 — tái hiện lỗi và sửa

**ASSUMPTION — synthetic:** n_eff=40, r=2/3, z=(2,2,-2), w=(.45,.35,.20). v0.1 full score=1.2 →62; bỏ death rồi renormalize .8 cho score=2 →70. v0.2 trả unknown khi death thiếu. Với Sigma=I đầy đủ, d2=.365, u=1.2/sqrt(.365)≈1.986254; diagnostic R(r*u)=70. Hai số 70 khác nguyên nhân: số full-panel v0.2 là thay scale có version, số missing v0.1 là đổi estimand không được phép. Không so trực tiếp số v0.1/v0.2 như progression.

### F03 — covariance và role/mask

Independent unit inputs cho d2: L=.38, laner C=.365, Jungle C=.405, Support C=.625. Chia đúng sqrt(d2) cho Var(u)=1 ở từng reference. Với laner và mọi off-diagonal correlation=.5, d2=.6825, không phải .365. Mask bỏ death/renormalize có d2=.5078125 khi Sigma=I; normalize lại cũng không xác lập cùng estimand. Hệ số role cố định không loại covariance, team/champion confounding hoặc selection bias.

## 4. Window, aggregation, effective evidence

**PROPOSAL:** một target context/role cho mỗi release; age 0..364 ngày UTC với cutoff explicit, half-life180 cho persistent diagnostic. Future row invalid. Group games theo verified series ID. Với m_j games trong series j và age_j là tuổi game mới nhất của series, W_j=2^(-age_j/180); mỗi game có a_ij=W_j/m_j. Series khác patch/role/context không được cắt để lấy phần thuận lợi: release tách context hoặc unknown theo protocol.

```text
mu_hat = sum_j sum_i a_ij*u_ij / sum_j W_j
n_eff_series = (sum W_j)^2 / sum(W_j^2)
n_eff_games = (sum a_ij)^2 / sum(a_ij^2)  # mô tả, không thay n_eff_series
r = n_eff_series/(n_eff_series + K_mean)
theta_point = r*mu_hat                  # regularized diagnostic, không claim posterior
```

Estimand là game được chọn đều trong một series, rồi series được chọn theo W; không là game độc lập đều trong toàn giải. Một Bo5 không thành năm independent series. n_eff là Kish count dưới trọng số, không chứng minh series độc lập; uncertainty phải cluster theo series và stress test team/era blocks. K_mean=20 chỉ là giá trị synthetic để tái hiện phản ví dụ; release model phải chọn trên validation hoặc ghi chưa fit. Shrink mean một lần; không shrink feature rồi shrink composite lần nữa.

Mean gate proposal: ≥12 distinct series, n_eff_series≥10; full panel; reference support; newest game≤90 ngày và mean freshness=sum(W)/n_series≥.5. Consistency thêm ≥24 games và n_eff_series≥12. Đây là predeclared study gates, không assurance accuracy. Report counts/freshness/coverage riêng; bỏ tích heuristic confidence và low/moderate/high chưa calibrated. Estimate interval null với `not_estimated` cho tới khi bootstrap/model uncertainty được thực hiện, không xuất interval bịa.

## 5. Consistency: game-level variation, common latent scale

So sánh giải pháp:

| Giải pháp | Ưu / nhược | Disposition |
|---|---|---|
| Giới hạn NPC K≤80 | Xóa khác biệt biên nhưng finite-sample shrink vẫn làm public hẹp hơn; giữ horizon/clipping lỗi | Không chọn |
| Rescale công thức cũ lên 99 | Sửa cosmetic range, không sửa series mean và clipped variation | Không chọn |
| Common latent variance ratio + estimate/knowledge riêng | Định nghĩa chung, cho phép uncertainty; cần measurement model trước runtime | **Chọn làm research contract**, không tự nối legacy K |
| Bỏ hoàn toàn nghiên cứu Consistency | Scope nhỏ nhất nhưng mất công cụ phát hiện variability bias | Giữ diagnostic, bỏ Core/consumer mới |

Latent `v` là game-level within-player variance của u tương đối reference v_ref, cho role/context cố định và estimand chọn game như §4. Nó vẫn chứa các yếu tố không quan sát (champion, teammate, schedule), không tự gọi residual causal variance. Mean được trừ trước tính, không dùng sự ổn định của thắng/thua.

```text
V_game = sum(a_ij*(u_ij-mu_hat)^2) / sum(W_j)
v_hat = V_game / v_ref
K(v) = clamp(round_even(50 - 49*ln(v)/ln(4)), 1, 99), v>0
K(0) = 99 with saturated_low_variance=true
v_latent(k) = 4^((50-k)/49), k in [1,99]
```

V_game là weighted **population central moment estimator**, không gọi unbiased sample variance. v_ref: mean của V_game từng người trong complete training panel, mỗi người weight bằng nhau, cùng sample gates/context/mask; tối thiểu20 người/four teams. Inference về latent v phải tính finite-sample bias, serial correlation và uncertainty ở gate B/C. `v_hat`/K(v_hat) chỉ diagnostic plug-in, không phải latent truth. v<0 invalid; v_ref=0 unknown; v<.25 hoặc >4 saturation chứ không bảo đã đo được extreme chính xác. Không áp shrink-to-1 của v0.1 trước map K. Prior/likelihood chưa fit → latent posterior unknown; không giả định không cần regularization cho variance.

Endpoints: k=1↔v=4; k=50↔v=1; k=99↔v=.25. NPC dùng cùng map này, không gán K99 vào một định nghĩa public có trần80. Tất cả integer1..99 round-trip qua map, được kiểm tra offline. **Attainable-scale compatibility không chứng minh measurement comparability:** NPC authored latent K và public point estimate vẫn không được trộn trực tiếp trong simulation. Trước nhập pack phải chọn một acquisition/initialization rule chung đã qua gate C; current v0.2 không nhập bất kỳ bên nào.

### F04 — game và series

Với độc lập và variance game=1, mean năm games có variance .2; inverse map K(.2) saturates99 so với K(1)=50. Vì vậy không bao giờ feed variance series means vào K game-level. Synthetic panel gồm hai series `[0,2]`, `[0,2]`: mu=1, V_game=1; hai series means đều1 có variance0. v0.2 lấy V_game=1 →K50 nếu v_ref=1. Mở rộng mỗi series lặp cùng multiset năm lần giữ V_game nhưng không tăng số independent series. Đây là invariance arithmetic, không giả định Bo5 thực tế dài năm games hoặc độc lập.

### F05 — clipping

Panel40 games luân phiên3/5 và0/2 đều population variance1; map cho cùng K50 khi reference1. Clip feature±3 trước variance biến nhóm đầu thành constant3, variance0 →K99 giả tạo. v0.2 không clip u hoặc feature trước variance. Chỉ saturate final display, giữ raw mean/variance và flags. Extreme hợp lệ phải được giữ trong fit/audit; sensitivity report so robust alternatives, không âm thầm loại điểm xấu. Nếu outlier làm model không ổn định, gate B fail và revise riêng, không gọi transform này robust đã kiểm chứng.

## 6. Reproducibility và lineage

Offline arithmetic proposal: Decimal precision50, round-half-even, thứ tự `(played_at, match_id, game_id, observation_id)` ordinal; finite validated decimal inputs; sqrt/ln/exp tại precision50, chỉ quantize stored outputs 12 decimals và integer display ở cuối. Khác profile sáu số lẻ v0.1: phải đổi model ID. Runtime tương lai chỉ nhận frozen approved capability, không chạy fit/transcendental. Một implementation khác phải khớp published golden boundaries; Python float trong finite-moment assertions chỉ chứng minh identity với tolerance, không là chứng nhận cross-machine serialization.

Artifact bắt buộc giữ row inventory/hash, tất cả excluded/missing reasons, metric/context registry, exact train/validation/test cutoffs, mu/sigma/Sigma/w/d2, reference-player panels/v_ref, per-series membership/W và per-game a, unrounded/rounded output, saturation, fitted/prior version, uncertainty method, source-use record. Thiếu lookup/numerical/model artifact → `not_reproducible_for_release`. Không làm giả hash hoặc license để điền đủ schema. Ví dụ và protocol ở [revision](PLAYER_DATA_REVISION_V02.md) ghi rõ bằng chứng nào thực sự có.
