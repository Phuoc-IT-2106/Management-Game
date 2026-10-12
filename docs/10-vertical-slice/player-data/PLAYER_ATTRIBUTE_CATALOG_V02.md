# Player Attribute Catalog v0.2

Status: **PROPOSED — NOT APPROVED**  
Ngày: 2026-10-08 · Chế độ: SPEC ONLY  
Nguồn: [revision](PLAYER_DATA_REVISION_V02.md), [review lịch sử](../../90-archive/10-vertical-slice/player-data/PLAYER_DATA_REVIEW_V1.md), [công thức](PLAYER_RATING_FORMULAS_V02.md), [runtime](RUNTIME_COMPATIBILITY_V02.md).

## Phạm vi được đề xuất

**PROPOSAL:** bỏ nhãn “bốn Core”. Không có capability mới nào được phép đi vào runtime trong v0.2. Giữ **Laning và Combat làm hai ứng viên cho thí nghiệm consumer có giới hạn**; trước khi qua các gate B–D chúng chỉ là diagnostics. Vision trở thành diagnostic-only. Consistency mới là nghiên cứu biến thiên ở cấp game, không thay `Person.Consistency`. Không tự bổ sung Map Impact để lấp chỗ trống bằng một tên hấp dẫn.

Hai ứng viên không được tuyên bố đủ để mô phỏng mọi phong cách của năm role. Đây là giới hạn có chủ ý để Director quyết định: chấp nhận phạm vi hẹp, hoặc yêu cầu một thiết kế đóng góp pressure/utility/resource có giới hạn trước khi triển khai. R-M01 còn mở ở phạm vi gameplay đầy đủ.

| Candidate | Trạng thái v0.2 | Ý nghĩa và giới hạn |
|---|---|---|
| Laning (`laning`) | Conditional consumer candidate | Trung bình lợi thế tài nguyên sớm trong reference đã khai báo; chỉ Top/Mid/Bot. Không phải solo mechanics. |
| Combat (`combat`) | Conditional consumer candidate | Trung bình chỉ số performance damage/participation/survival theo role. Không đồng nhất với tổng giá trị giao tranh, đặc biệt tank/engage/protection. |
| Vision (`vision_activity`) | Diagnostic-only | Hoạt động vision theo phút; không buff Company.Information, preparation hoặc mọi quyết định scouting. |
| Consistency (`game_consistency`) | Diagnostic/latent-scale research only | Biến thiên game-level của Combat proxy quanh trung bình người chơi. Không là tính cách, độ tin cậy lao động hay biến thiên thắng/thua. |
| Resource Efficiency | Deferred capability; ratio diagnostic | Damage/gold không đo tác động khi ngân sách tài nguyên thay đổi; không dùng làm “giá rẻ hơn”. |
| Map Impact | Deferred | Thiếu attribution/opportunity model; Jungle pressure và Mid roaming chưa được phân biệt. |
| Champion Pool | Deferred; entropy diagnostic | Không có draft consumer. Không thưởng đa dạng pick như thể đó là mastery. |
| Adaptability đo từ public data | Deferred | Không thay authored Adaptability hiện tại bằng null, 0, Consistency hoặc chênh lệch patch. |

Role là slot và điều kiện reference; không phải một bonus. Secondary role cần reference riêng; không sao chép rating primary role. Readiness, form, potential, reputation, kinh nghiệm, tuổi và knowledge tách biệt. Potential/form/development mới vẫn null; tuổi không tự tăng giảm năng lực.

## Role × capability × consumer

**PROPOSAL — consumer G1 chỉ là một thiết kế thí nghiệm**, không phải luật đang chạy. `L`/`C` dưới đây là capability snapshot đã qua admission, không phải một raw diagnostic. Không có snapshot đủ điều kiện thì thí nghiệm tích hợp không được bắt đầu.

| Role | Laning | Combat | Vision | Game Consistency | Consumer và phần không hỗ trợ |
|---|---|---|---|---|---|
| Top | Có reference riêng | Có; cần audit damage/utility | Diagnostic | Diagnostic | G1 đổi ưu tiên early/later; không suy ra frontline/utility từ damage. |
| Jungle | not_applicable | Có proxy theo role | Diagnostic | Diagnostic | G1 chỉ C; không phân biệt pressure/farm bằng cách gán L giả. |
| Mid | Có reference riêng | Có proxy theo role | Diagnostic | Diagnostic | G1 lane/contribution split; không coi C là roaming. |
| Bot | Có reference riêng | Có proxy theo role | Diagnostic | Diagnostic | G1 lane/contribution split; không suy ra hiệu quả low-resource. |
| Support | not_applicable | KP/death proxy hẹp | Diagnostic | Diagnostic | G1 chỉ C; không phân biệt engage/protection hoặc đánh giá utility đầy đủ. |

## G1 — hợp đồng consumer thí nghiệm

**PROPOSAL:** chỉ thay một input mean capability trong một ruleset thí nghiệm tương lai. Không cộng vector mới bên cạnh Execution cũ. Một team policy `early_weight = h`, trong tập thử hữu hạn `{0.2, 0.5, 0.8}`, áp dụng chung cho ba laner:

```text
q_i(h) = h*L_i + (1-h)*C_i       Top/Mid/Bot
q_i(h) = C_i                     Jungle/Support
base_G1 = round_even(sum(q_i(h)*Readiness_i)/(5*100))
```

`base_G1` thay đúng base term readiness-weighted Execution trong thí nghiệm, không ghi ngược vào `Person.Execution`. Năm role phải hợp lệ trước khi tính. Đây là một phép tổng hợp roster, không có minion, event, map path, champion draft hoặc phase simulation. Các hệ số h là điểm thử thiết kế, không balance đã fit. Khi `L=C=Execution` cho laner và `C=Execution` cho hai role còn lại, base không đổi; kiểm tra này chỉ chứng minh đồng nhất đại số của base.

Owner: Company/People giữ snapshot capability; Company/Competitive Management giữ lựa chọn h; Domain Competition chỉ đọc snapshot và phát outcome; Application kiểm tra role, authority và observation trước quyết định. H chưa có trong Plan/DTO hiện tại; không được tự tái sử dụng Posture/Execution preparation để chứa nó. Preparation, adaptation, rivalry, noise và consequence giữ nguyên trong thí nghiệm cô lập; mọi mở rộng cần rules identity riêng.

| Thuộc tính retained candidate | Quyết định quản lý | Tác dụng chính xác / aggregation | Opportunity cost | Thông tin được xem | Ablation/counterfactual |
|---|---|---|---|---|---|
| Laning | Chọn lane specialist, roster phù hợp ưu tiên early | Với một laner: +ΔL đổi base trước rounding bằng h×ΔL×Readiness/500; ba laner có weight như nhau | C bị hy sinh ở cùng ability budget; roster slot, wage/fee và readiness vẫn có chi phí; không cấp free points | Estimate/range và reference/date nếu được phép; không xem latent NPC hay trace | Giữ B=50: (L,C)=(58,42) so với (42,58). h=.8 ưu tiên người đầu; h=.2 đảo lựa chọn. Bỏ L phải mất đảo chiều này. |
| Combat | Chọn contribution specialist, so khả năng với phí tuyển/lương | Laner: (1-h)×ΔC×Readiness/500; Jungle/Support: ΔC×Readiness/500 | Trade-off L cho laner; chi phí hợp đồng và slot cho mọi role. Không thưởng thêm KDA/damage-gold | Estimate/range role-specific; ghi rõ proxy không đo utility đầy đủ | C thay đổi phải đổi base khi chưa bị rounding/clamp che; đặt hai C bằng nhau loại lợi thế contribution. Với Jungle/Support, chỉ chứng minh sensitivity, chưa chứng minh depth của phong cách. |

Tổng raw điểm bằng nhau không phải cùng utility. Với profile trên, utility cá nhân là 54.8/45.2 tại h=.8 và đảo ngược tại h=.2; h=.5 hòa 50. Không profile nào trội ở cả ba h tại cùng B/readiness/cost. Thí nghiệm đại số này không chứng minh người chơi muốn dùng h, staff chọn đúng h, giá hợp lý hay đối thủ tạo strategic value. Đặc biệt h có nguy cơ chỉ là chọn số cao hơn của đội mình: gate D phải chứng minh có quyết định có giá trị trong phân bố đối thủ/chi phí đã duyệt, hoặc loại G1.

## Năm cặp chiến lược bắt buộc

**ASSUMPTION:** mỗi cặp cùng role, ngân sách tiền, tuổi, readiness và thông tin. Các nhãn mô tả giả thuyết kiểm tra, không là đánh giá người thật. Với giới hạn hiện tại, kết quả đúng có thể là “không phân biệt được”; không tạo rating bịa để buộc mô hình thắng bài kiểm tra.

| Cặp | Phản thực cần thử | v0.2 biểu diễn được gì? | Chưa biểu diễn được / quyết định cần thiết |
|---|---|---|---|
| S1 Damage-oriented vs utility-oriented Top | Đổi lineup còn lại từ thiếu damage sang cần bảo vệ carry; giữ tổng ngân sách | G1 phân biệt L/C nếu các capability đã được xác lập độc lập | Public Combat có thể hạ utility Top sai. Không được mã hóa utility bằng ít death. Cần scope chấp nhận trừu tượng hoặc contribution-label study. |
| S2 Pressure-oriented vs farming Jungle | Đổi nhu cầu đội từ early pressure sang scaling resources | Cost/readiness và C khác nhau vẫn ảnh hưởng roster | Nếu C bằng nhau thì G1 hòa; KP không làm chứng pressure, CS không là farming skill. Map/resource model còn thiếu. |
| S3 Lane specialist vs roaming Mid | Thay đội từ tự thắng lane sang cần phối hợp các lane khác | Lane-vs-C specialist (58,42)/(42,58) đổi thứ tự khi h đổi | Roaming không đồng nhất C. Phải gọi đúng “lane/contribution”, không tuyên bố đã hỗ trợ roam. |
| S4 Resource-heavy vs economical Bot | Giảm phần tài nguyên dành cho Bot trong đội giả định | So wage/fee được qua economy hiện tại | In-game gold và tiền công ty là hai tài nguyên khác nhau. Không có allocation curve nên C bằng nhau vẫn hòa; damage/gold chỉ diagnostic. |
| S5 Engage vs protection Support | Đổi đội từ thiếu mở giao tranh sang cần giữ carry sống | C proxy và readiness có thể khác nhau | KP/ít death không xác định engage/protection; cùng vector phải cho cùng base. Không dùng Vision làm utility thay thế. |

Các phép “cùng vector → cùng kết quả” là negative controls có chủ ý. Chúng chứng minh giới hạn, không là PASS năm cặp. Director phải chấp nhận giản lược này bằng phạm vi cụ thể hoặc yêu cầu thêm thiết kế bounded; chưa thể nhận “đủ cả năm role”.

## Hợp đồng đo và thông tin

L dùng GD15/XPD15/CSD15 và weight .50/.30/.20; C dùng weight của v0.1 theo role, chỉ giữ làm hypothesis: laner .45/.35/.20; Jungle .25/.55/.20; Support KP/death .75/.25. Không claim weight xác định causal value. Chuẩn hóa covariance, full-mask admission, game-level Consistency, sample gates và uncertainty tuân [formulas](PLAYER_RATING_FORMULAS_V02.md).

Output L/C có reference ID, số thực trước rounding và display integer 1–99, trạng thái `diagnostic_estimate` cho public research. Mean dài hạn khác recent form; offline refresh chỉ tạo artifact mới, không đổi campaign. Vision giữ đơn vị score/min và standardized diagnostic, không tạo gameplay integer bắt buộc. Consistency phải mang tên mới `game_consistency`, variance horizon và saturation flag. Không xuất `confidence=1` cho NPC. Staff nhận đúng projection như người chơi trong cùng authority; diagnostics chưa được đưa vào UI hiện tại.

**OPEN QUESTION:** chỉ khi B (measurement), C (real/NPC) và D (consumer) đạt theo [protocol](MEASUREMENT_CALIBRATION_PROTOCOL_V02.md), Director mới có thể chọn L/C làm gameplay attributes. Vision và game_consistency không tiêu raw point budget của G1. Các deferred candidates không có tác dụng ẩn.
