# Measurement and Calibration Protocol v0.2

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 · Không thu thập hoặc fit dataset trong nhiệm vụ này  
Liên quan: [formulas](PLAYER_RATING_FORMULAS_V02.md), [catalog](PLAYER_ATTRIBUTE_CATALOG_V02.md), [NPC](NPC_GENERATION_V02.md), [revision](PLAYER_DATA_REVISION_V02.md).

## 1. Gate độc lập

**PROPOSAL:** Director phải duyệt mục tiêu, population, thresholds và analysis plan trước nhìn final holdout. Các con số dưới đây là **ngưỡng pilot đề xuất**, không suy ra từ dữ liệu chưa có. Không tự điều chỉnh cho vừa kết quả.

| Gate | Câu hỏi / evidence | Acceptance proposal | Trạng thái hiện tại |
|---|---|---|---|
| A Mathematical correctness | Đơn vị, bounds, full-mask rule, game horizon, covariance, keys | Mọi exact invariant/counterexample bắt buộc pass; numerical tolerance công bố | Có focused offline evidence; chưa production validator |
| B Measurement validity | Proxy dự báo gì, coverage, bias, uncertainty? | Protocol bên dưới trên dữ liệu được phép và held-out thật | NOT RUN |
| C Real/NPC comparability | Cùng estimand, observation-error và population selection? | Cùng scale support; synthetic latent-recovery và matched-observation checks; không bypass missingness/uncertainty theo origin | Chỉ algebraic support PASS; empirical NOT RUN |
| D Gameplay decision value | Attribute thay đổi lựa chọn có ích không? | Ablation và năm cặp chiến lược trong scope được duyệt; trade-offs ở chi phí bằng nhau | Chỉ G1 toy utility; complete five-role coverage NOT ESTABLISHED |
| E Economy / long campaign | Tuyển dụng, rarity, lương, roster và incentives có bền không? | Multi-policy/seed campaign plan sau integration riêng | NOT RUN |

A không suy ra B; dự đoán proxy ở B không suy ra causal skill; C không suy ra strategic value D; D không suy ra affordability/rarity E. Rules-v3 incentive gate trong repo là evidence của chính rules đó, không được tái dùng làm player-data E PASS.

## 2. Population, contexts và coverage audit trước fit

Study manifest chọn rõ provider/rights record, một competition_group có scope, season(s), patch_eras, role definitions, metric-definition version và as_of/cutoffs. `competition_group` là danh sách competition IDs được review, không suy từ tên tier. `patch_era` là map patch→era và lý do semantic compatibility được ký trước fit; không dùng substring hoặc một patch cũ làm fallback.

Benchmark mỗi role/context cần≥200 completed player-game rows,≥20 distinct people,≥4 teams; Consistency reference cần≥20 people, mỗi người đạt≥24games/12effective series full panel. Các gates có thể bất khả thi trong một circuit: phải audit trước, không mặc định đạt. Chỉ nghiên cứu context đủ hỗ trợ. Sparse cell→unknown. Pool adjacent eras chỉ bằng manifest mới có kiểm tra drift ở validation, không trộn roles hoặc disconnected tiers. Team/champion/side/duration dùng audit strata, chưa causal adjustment; outcome/result không được làm pregame feature.

Eligibility list dựng từ metadata trước tính performance; ghi mọi excluded/remake/invalid/missing-context/no-opportunity game theo role/team/format và source. Full-mask policy có khả năng coverage thấp, đặc biệt Support KP=unknown khi team không có kill. Pilot yêu cầu ít nhất60% player-windows hợp lệ **trong từng role đang tuyên bố hỗ trợ**, denominator gồm mọi người/window eligible metadata chứ không chỉ survivors. Report 95% interval và counts; role không đủ precision là INCONCLUSIVE, không âm thầm drop khỏi headline. Coverage threshold chỉ là feasibility gate, không đảm bảo missing-at-random.

So sánh eligible/rejected windows theo pregame team strength, format, patch, duration, champion class nếu nguồn hợp lệ, resource allocation diagnostics và realized result (chỉ audit hậu nghiệm). Complete-case performance không đại diện population bị loại. Không claim bias đã hết sau inverse weighting hoặc covariance normalization.

## 3. Multi-context players

Một người có nhiều role/era/tier nhận **nhiều context-specific measurement records**, không trung bình mù thành overall rating. Default release chỉ target context đã đăng ký; lưu bằng chứng khác nhưng không làm evidence count của context hiện tại tăng lên.

Nếu muốn combine, cần map từng context `theta_target = a_c + b_c*theta_c` với b_c>0, anchor connectivity, uncertainty và held-out bridge tests. Map variance bằng b_c² trước kết hợp; không dùng một tier_offset sau khi trộn. Không có bridge→`unknown:unlinked_context` cho yêu cầu target chung. Một series có role/context chuyển giữa các game không đáp ứng panel của release này; report lý do, không cherry-pick games. New patch unsupported→unknown cho new release; campaign cũ giữ frozen values.

Đối với Consistency, **không combine cross-context variance trong v0.2**, kể cả đã có mean bridge; khác context có thể đổi within-player noise và schedule. Nghiên cứu rộng hơn phải spec within/between-context decomposition trước khi fit. Không gọi chênh lệch means giữa context là tính thiếu ổn định.

## 4. Train / validation / final holdout

Manifest ghi ngày cụ thể trước chạy. Proposal chia theo complete series thời gian60% train,20% validation,20% final test; series cùng end-date nằm cùng partition để tránh split tuỳ ý. Không chọn ngày sau khi biết error. Primary là dự báo người đã thấy sang thời gian tương lai; secondary là player-disjoint và team-era-disjoint folds, được báo riêng vì population khác. Mọi reference, mu/Sigma/v_ref, prior K, bridge và missingness model fit train; chọn variant trên validation; fit lại train+validation chỉ theo procedure preregistered rồi khóa test. Không refit benchmark trên test.

Primary target B cho mean: full-panel game/series composites của30ngày kế tiếp trong **cùng context** theo reference frozen, aggregate đúng estimand của formulas. Nếu không đủ evidence tương lai, target unavailable được report, không zero. Target này kiểm tra dự báo chính proxy, chưa xác định utility. Baselines: role/context mean0, no-shrink historical mean, last supported30-day mean; compare trên cùng eligible windows. Metric chính MAE trong normalized units, phụ RMSE/rank stability và saturation rate.

Pilot acceptance mean: MAE giảm≥5% so role/context baseline, paired95% cluster-bootstrap CI của cải thiện tuyệt đối phải trên0. So baseline mạnh nhất còn lại: upper95% CI của MAE excess≤.05 normalized units. Mỗi supported role và nhóm style đã preregistered: upper95% CI excess≤.10 so baseline cùng nhóm; nếu thiếu data thì INCONCLUSIVE và không claim support nhóm đó. Ít nhất50 test player-windows và20 distinct people mỗi supported role cho pilot; minimum count không thay power/precision analysis. Không sử dụng fan rankings làm ground truth.

Independent construct review: người đánh giá blinded với rating xem các case damage/utility, pressure/farm, roam/lane, resources và engage/protection trên evidence được phép, theo rubric đóng góp đã đăng ký. Không có rubric hoặc inter-rater agreement thì chưa claim full Combat contribution. Numeric proxy forecasting có thể pass trong khi construct review fail; giữ diagnostic ở trường hợp đó.

## 5. Uncertainty, recency và sensitivity

Report raw games/series, n_eff_series, mean age weight, latest age, completeness, unknown reason, fit/holdout coverage; **không còn heuristic high-confidence band**. Estimation intervals proposal: 2000 resamples theo whole series, giữ games bên trong, seed và fitting procedure pinned; stress test larger team-era/opponent blocks. Chưa chạy→interval=null/not_estimated. Bootstrap không chữa selection bias, confounding hoặc identifiability.

For predictive90% intervals (khác confidence interval của latent parameter), holdout empirical coverage phải85–95% overall và mỗi supported role, kèm width và interval-score so baseline. Low sample hoặc không đủ interval-coverage precision→INCONCLUSIVE. Không trình bày point estimate là known truth. Consistency inference còn yêu cầu simulation-based recovery cho finite samples, varying series length, serial correlation, zero variance và outliers; đơn thuần map variance không hoàn thành inference.

Predeclare sensitivity: K_mean0/10/20/40; half-life90/180/270; complete fixed panel versus mask-specific model **chỉ nếu variant đó được thiết kế và duyệt trước**; raw moments versus robust candidate estimator ở research branch riêng. Report ranking/MAE/coverage và selection effects, không chọn best sau test. New model/artifact ID cho mọi thay đổi arithmetic, reference hoặc eligibility.

## 6. Gate C — operational shared scale

First: verify all L/C/K endpoints and inverse/saturation meaning, no origin field enters resolver. Synthetic observation-error study sau authorization dùng abstract u observations, không fake provider/player-game records. Biết theta/v ẩn để kiểm tra bias/interval recovery theo n, mask, format và origin-label negative controls. Same observation panel đổi origin label phải cho exact same estimates/unknown status; signed bias của theta recovery≤.10 units và90% interval coverage85–95% ở từng declared cell là pilot targets.

Second: trên dữ liệu thật có quyền dùng, chỉ so NPC với public cohorts sau role/context/sample/selection matching và cùng acquisition operator. Median mean-estimate gap≤.20 standardized units và relative variance gap≤20% là diagnostic pilot thresholds; failure phải giải thích bằng explicit population hypothesis hoặc revise distribution, không ép mọi recruit khớp elite pros. Equal observed distributions không chứng minh true latent equality. Không có validated observation model thì C INCONCLUSIVE và mixed pack blocked.

## 7. Gates D/E và stopping rules

D cần scenario fixture registry cho năm cặp trong catalog, fixed permitted knowledge, equal costs/readiness, attribute ablations và counterfactuals. Claimed strategy pair phải có ít nhất hai context đảo preferred choice với expected-utility difference lớn hơn simulation uncertainty; point thresholds preregister theo utility unit của consumer trước chạy. G1 chỉ có algebraic reversal L/C; S1/S2/S4/S5 và roaming-specific S3 chưa có consumer hoàn chỉnh. Nếu Director chọn narrower scope, ghi exclusions trước thí nghiệm; không gọi đó five-role strategic coverage. Nếu G1 chỉ thêm thao tác chọn số cao hơn, reject consumer và giữ diagnostics.

E pilot proposal sau BUILD riêng:≥32seeds×4policies×10seasons, matched current-rules baseline; chính sách balanced, delegated, thrift và rarity-seeking; report distributions/paired uncertainty, không chỉ mean. Zero duplicate owners/IDs, receipts errors, unsupported save loads, expired-person rerolls hoặc illegal listings. Every refresh covers missing roles under approved finite-pool assumptions; active elite cap never exceeded. Test six-person roster affordability, eight-person cap, contract churn, wage convexity, operating cost/fines, sponsor loss và reputation. Report distress/dead-end rates separately from balance; gameplay-intended success/solvency thresholds phải do Director chọn trước chạy vì phụ thuộc scope/cost model chưa quyết. Gate E chưa runnable, không lấy toy budget PASS thay thế.

Stop khi data rights/coverage/reference fail; không gọi API để “thử lấy đủ”. Khi B/C fail: revise measurement hoặc giảm supported scope, không sửa person ratings thủ công. Khi D fail: loại consumer hoặc mở design task rõ ràng. Khi E fail: review lifecycle/economy với phiên bản riêng. Final holdout đã xem không được dùng lại làm holdout chưa thấy. Lưu protocol+dataset/model hashes, results, deviations và negative findings; không xóa counterexamples cũ.
