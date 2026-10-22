# Rulebook: anti-pattern UI/UX thường gặp trong giao diện do AI tạo

Trạng thái: **PROPOSAL — chờ Director review** (2026-10-22). Tài liệu này là khung đánh giá chất lượng dùng lại được;
nó **không** tạo quyết định thiết kế mới và **không** đứng trên governance của dự án.

- Thứ bậc thẩm quyền trong repo này giữ nguyên: DEC → [Stage 5 governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md)
  và [brief 50 luật](../../01-governance/STAGE5_VISUAL_UX_BRIEF.txt) → [genre-shell amendment](../../01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md)
  → [game-identity thesis](../../01-governance/STAGE5_GAME_IDENTITY_THESIS.md) → [UI polish standard](UI_POLISH_STANDARD.md)
  → mockup đã duyệt (`../ui-research/mockups/generated-reference-set/`) → rulebook này.
- Khi rulebook mâu thuẫn với một quyết định đã duyệt, **quyết định đã duyệt thắng**; ghi mâu thuẫn vào §6 thay vì
  "sửa" màn hình.
- Skill dùng rulebook này: [`.claude/skills/ui-authenticity/`](../../../.claude/skills/ui-authenticity/SKILL.md).
  Danh mục rút gọn cho skill: [`references/rule-index.md`](../../../.claude/skills/ui-authenticity/references/rule-index.md)
  (được kiểm tra đồng bộ bằng `scripts/validate_skill.py`).
- Kết quả áp dụng lên dự án: [UI/UX audit 2026-10-22](UI_UX_AUDIT_2026-10-22.md) và [kế hoạch khắc phục](UI_REMEDIATION_PLAN.md).

Thuật ngữ kỹ thuật, ID quy tắc và tên định danh giữ bằng tiếng Anh.

---

## 1. Nguyên tắc nền

1. **Nhận ra một pattern không chứng minh AI đã tạo ra nó.** Rulebook đánh giá *chất lượng theo ngữ cảnh*, không đoán
   tác giả. Không bao giờ ghi "màn hình này do AI làm" dựa trên thẩm mỹ (xem WFL-006). Ngay cả nghiên cứu định lượng
   tốt nhất tìm được cũng tự nhận 5–10% dương tính giả và nói một pattern đơn lẻ không đủ để kết luận [C1].
2. **Không cấm một yếu tố chỉ vì AI hay dùng nó.** Bo góc, Inter, Roboto, Tailwind, Lucide, card, gradient, shadow,
   glassmorphism, sidebar trái, dark theme đều có thể đúng. Vi phạm chỉ xảy ra khi lựa chọn đó **không cần thiết, thừa,
   thiếu nhất quán, sai chức năng, hoặc trái hướng thiết kế đã duyệt**.
3. **Không thay template này bằng template khác.** Thoát khỏi "SaaS tím" để rơi vào "editorial be/terracotta",
   "đen + xanh axit", "tactical neon" hay "brutalist" vẫn là thất bại cùng loại (IDN-005). Chính Anthropic ghi nhận model
   vẫn hội tụ về lựa chọn mặc định mới (ví dụ Space Grotesk) [A1].
4. **Ngữ cảnh trước luật.** Một quy tắc đúng cho landing page có thể sai cho màn hình quản lý dùng 100 lần/ngày
   (ví dụ hiệu ứng stagger khi tải trang được khuyến khích cho web marketing [A1] nhưng bị governance của game này
   loại vì lặp lại [P1 §29, §45]).
5. **Bằng chứng trước khẩu vị.** Mỗi finding phải chỉ ra hệ quả (khả dụng, phân cấp, nhất quán, độ trung thực, bản sắc
   đã duyệt). Khẩu vị cá nhân thuộc loại C và không được cưỡng chế.
6. **Thay đổi nhỏ nhất đủ hiệu quả.** Khuyến nghị ưu tiên sửa hệ thống (token, component, formatter) hơn vẽ lại màn hình.

## 2. Phương pháp nghiên cứu và giới hạn

Nghiên cứu web thực hiện ngày 2026-10-22, ưu tiên nguồn chính thức (Anthropic, OpenAI, Vercel, W3C), rồi nghiên cứu UX
(NN/g, arXiv), rồi phản hồi cộng đồng. Mỗi nguồn ở §7 có URL, ngày (nếu trang ghi) và mức tin cậy.

Giới hạn cần biết:
- **Reddit:** đã tìm nhưng không xác minh được thread cụ thể nào; rulebook **không** trích Reddit.
- **Hacker News:** chỉ có gián tiếp qua phân tích 1.590 trang Show HN của Adrian Krebs [C1]; không trích bình luận HN
  riêng lẻ.
- **Bài đăng của Adam Wathan về `bg-indigo-500`** (8/2025) chỉ được xác minh qua nguồn thứ cấp [C3]; không truy cập được
  bài gốc trên X. Câu chuyện "Tailwind indigo làm UI AI có màu tím" là **giả thuyết được nhiều người lặp lại**, không phải
  kết quả nghiên cứu.
- [A3] được công cụ tải về dưới dạng tóm tắt, không phải nguyên văn; các cụm được dẫn là diễn giải sát nghĩa.
- Một số tiêu chí WCAG (1.4.1, 1.4.3, 1.4.11, 2.4.7, 2.3.3) dẫn từ kiến thức sẵn có về tiêu chuẩn W3C, không tải lại
  trong phiên này; riêng 2.5.8 đã xác minh trực tiếp [W1].
- Không có benchmark nào đo trực tiếp "UI quản lý/game do AI tạo"; bằng chứng định lượng hiện có đều về web/app
  [C1][R1][N1]. Phần game dựa vào governance của dự án, Game Accessibility Guidelines [G1] và kinh nghiệm chuyên môn.

Bằng chứng định lượng chính:
- **Design Theater** [R1]: 120 giao diện từ 5 công cụ generative UI; >25% lý do thiết kế được tuyên bố *không* có trong
  giao diện (34% với yêu cầu chức năng); trung bình chỉ nhận ra 0,54 nguyên tắc UX được nhúng trong prompt; 4/5 công cụ
  hiện thực ≤6% nguyên tắc chức năng; giao diện hội tụ về hình thức và bố cục, khác nhau nhiều hơn ở màu.
- **NN/g** [N1]: công cụ AI prototyping "lack the sophistication to weigh design tradeoffs"; đầu ra có "a similar, generic
  look", thường mặc định Shadcn/Tailwind; lỗi heuristic gặp: thiếu phân cấp/nhóm, màu dùng quá mức, tương phản kém, lề
  không nhất quán, hiểu sai pattern (profile thành profile mạng xã hội), thêm nội dung không được yêu cầu.
- **Krebs** [C1]: 1.590 trang Show HN chấm bằng script DOM/CSS; 22% có ≥4 pattern, 32% có 2–3, 46% có 0–1.

## 3. Phân loại, mức độ và độ tin cậy

### 3.1. Loại quy tắc

| Loại | Ý nghĩa | Cách cưỡng chế |
|---|---|---|
| **A — Anti-pattern mạnh** | Hầu như luôn gây hại khả dụng, độ rõ ràng hoặc độ trung thực | Báo cáo khi gặp; cần lý do mạnh mới giữ |
| **B — Phụ thuộc ngữ cảnh** | Pattern hợp lệ, thành vấn đề khi thừa, sai chỗ hoặc thiếu nhất quán | Chỉ báo cáo khi chứng minh được tác hại trong ngữ cảnh |
| **C — Sở thích thẩm mỹ** | Lựa chọn phong cách; không có tác hại phổ quát | **Không** cưỡng chế như luật; chỉ nêu khi trái hướng đã duyệt hoặc khi chưa có quyết định và người dùng hỏi |

### 3.2. Mức độ (severity) — đo **tác hại khi vi phạm thực sự**

| Mức | Định nghĩa |
|---|---|
| **Critical** | Chặn nhiệm vụ chính; che giấu hoặc bóp méo thông tin then chốt; dữ liệu bịa trình bày như thật; không đọc được/không thao tác được trên đường đi chính |
| **Major** | Làm hỏng đáng kể phân cấp, hiểu biết hoặc nhất quán trên nhiều màn; vi phạm quyết định thiết kế đã duyệt; thiếu thông tin cho một cam kết quan trọng |
| **Moderate** | Suy giảm rõ ràng trên một màn hoặc một pattern lặp có chi phí UX |
| **Minor** | Đánh bóng cục bộ; tác động nhỏ |

### 3.3. Độ tin cậy (confidence) — đo **mức chắc chắn của finding**, độc lập với severity

| Mức | Điều kiện |
|---|---|
| **High** | Đã thấy trên giao diện render thật (ảnh chụp) **và/hoặc** mã nguồn rõ ràng không phụ thuộc diễn giải |
| **Medium** | Mã rõ nhưng chưa xác nhận trực quan, hoặc thấy trực quan nhưng việc vi phạm phụ thuộc diễn giải quy tắc |
| **Low** | Suy luận; cần render/kiểm thử thêm |

### 3.4. Trạng thái finding

- **Confirmed** — đã xác minh (render, đo, hoặc mã không mơ hồ) và vi phạm rõ.
- **Potential** — cần kiểm chứng trực quan, dữ liệu khác, hoặc quyết định diễn giải của Director.
- **Acceptable / justified** — pattern mà quy tắc chung sẽ gắn cờ nhưng được hướng đã duyệt hoặc chức năng biện minh;
  liệt kê để **bảo vệ**, không để sửa.

## 4. Bảng tra nhanh

| ID | Tên | Loại | Mức mặc định khi vi phạm |
|---|---|---|---|
| LAY-001 | Card hoá mặc định | B | Moderate |
| LAY-002 | Hộp lồng hộp | B | Moderate |
| LAY-003 | Dashboard KPI không có lý do quyết định | A | Major |
| LAY-004 | Đối xứng và căn giữa theo mặc định | B | Minor |
| LAY-005 | Khoảng trắng và mật độ không phục vụ nhiệm vụ | B | Moderate |
| LAY-006 | Phân cấp thông tin yếu | A | Major |
| LAY-007 | Vùng trang trí không có chức năng | A | Moderate |
| LAY-008 | Một khuôn cho mọi màn | B | Moderate |
| LAY-009 | Gần thẳng hàng và khoảng cách tuỳ tiện | A | Minor |
| LAY-010 | Không đứng vững ở độ phân giải và nội dung mục tiêu | A | Major |
| TYP-001 | Font chọn theo mặc định, không theo quyết định | C | Minor |
| TYP-002 | Chữ quá khổ kiểu marketing | B | Moderate |
| TYP-003 | Thang chữ không kiểm soát | A | Moderate |
| TYP-004 | Chữ hoa, tracking và nhãn phụ trang trí | B | Minor |
| TYP-005 | Monospace để trông "kỹ thuật" | B | Minor |
| TYP-006 | Cắt chữ làm mất nghĩa | A | Moderate |
| TYP-007 | Chữ quá nhỏ hoặc quá mờ | A | Major |
| COL-001 | Gradient tím/xanh mặc định | B | Moderate |
| COL-002 | Glow và neon trang trí | B | Moderate |
| COL-003 | Bóng đổ không biểu đạt lớp | B | Minor |
| COL-004 | Glassmorphism không có lý do | B | Moderate |
| COL-005 | Bảng màu/chủ đề mặc định không gắn sản phẩm | C | Minor |
| COL-006 | Màu nhấn tràn lan | A | Moderate |
| COL-007 | Màu ngữ nghĩa dùng sai hoặc không nhất quán | A | Moderate |
| COL-008 | Màu là kênh duy nhất mang nghĩa | A | Major |
| COL-009 | Tương phản thấp | A | Major |
| CMP-001 | Bo góc quá mức và bo lồng bo | B | Minor |
| CMP-002 | Badge/pill tràn lan | B | Moderate |
| CMP-003 | Giao diện thư viện mặc định chưa tuỳ biến | B | Moderate |
| CMP-004 | Hộp chứa icon thừa | B | Minor |
| CMP-005 | Thành phần thiếu nhất quán ngữ nghĩa | A | Moderate |
| CMP-006 | Viền và đường phân cách thừa | B | Minor |
| CMP-007 | Nhiều CTA ngang hàng | A | Moderate |
| CMP-008 | Thiếu trạng thái tương tác | A | Major |
| CMP-009 | Vùng bấm quá nhỏ | A | Major |
| CMP-010 | Tự chế thành phần/kiểu cục bộ | A | Moderate |
| ICO-001 | Icon trước mọi nhãn | B | Minor |
| ICO-002 | Icon và ký hiệu chỉ để trang trí | A | Minor |
| ICO-003 | Emoji làm icon hệ thống | A | Moderate |
| ICO-004 | Biểu đồ, vòng, thanh tiến trình vô nghĩa | A | Moderate |
| ICO-005 | Hoạ tiết và lớp trang trí lặp | B | Minor |
| ICO-006 | Nghĩa icon/ký hiệu không nhất quán | A | Moderate |
| CNT-001 | Nội dung placeholder hoặc mẫu lọt vào sản phẩm | A | Major |
| CNT-002 | Giọng marketing trong giao diện vận hành | A | Moderate |
| CNT-003 | KPI, xu hướng hoặc độ chính xác bịa | A | Critical |
| CNT-004 | Thiếu thông tin đặc thù miền | A | Moderate |
| CNT-005 | Ưu tiên thông tin sai | A | Major |
| CNT-006 | Thiếu trạng thái empty/error/loading/disabled/selected | A | Moderate |
| CNT-007 | Văn bản quy trình nội bộ lọt vào không gian người dùng | A | Moderate |
| CNT-008 | Một sự thật, nhiều cách diễn đạt | A | Moderate |
| CNT-009 | Điều khiển hoặc điều hướng không dẫn tới workflow thật | A | Major |
| CNT-010 | Quyết định thiếu nhân quả, chi phí hoặc thời điểm | A | Major |
| CNT-011 | Vẻ ngoài bóng bẩy che logic tương tác kém | A | Major |
| IDN-001 | Giao diện có thể hoán đổi | A | Major |
| IDN-002 | Phong cách không hợp miền sản phẩm | A | Major |
| IDN-003 | Art direction không nhất quán giữa các màn | A | Major |
| IDN-004 | Trang trí thay cho bản sắc | A | Moderate |
| IDN-005 | Thay template này bằng template khác | A | Major |
| MCK-001 | Chữ trong mockup sai hoặc không đọc được | B | Minor |
| MCK-002 | Mockup không nhất quán với nhau | A | Moderate |
| MCK-003 | Tính năng và điều khiển ảo giác trong mockup | A | Major |
| MCK-004 | Nội dung mẫu của mockup bị coi là dữ liệu | A | Major |
| MCK-005 | Bố cục/tỷ lệ mockup không hiện thực được | A | Moderate |
| MCK-006 | Mô hình tương tác phi thực tế trong mockup | A | Major |
| MCK-007 | Bắt chước cả ảnh thay vì trích thuộc tính | A | Moderate |
| MOT-001 | Chuyển động trang trí | B | Minor |
| MOT-002 | Không có đường reduced motion | A | Moderate |
| WFL-001 | Thiết kế hình trước, nhồi dữ liệu sau | A | Major |
| WFL-002 | "Design theater": lý do thiết kế không có trong sản phẩm | A | Moderate |
| WFL-003 | Tuyên bố đã kiểm chứng mà không render | A | Major |
| WFL-004 | "Đánh bóng" bằng cách thêm trang trí | A | Moderate |
| WFL-005 | Thay đổi thị giác ngoài phạm vi | A | Moderate |
| WFL-006 | Kết luận "do AI làm" từ thẩm mỹ | A | Moderate |

## 5. Quy tắc

Mỗi quy tắc gồm: loại, mức độ khi vi phạm, mô tả, dấu hiệu nhận biết, vì sao quan trọng, ngoại lệ, thay thế khuyến
nghị, ví dụ (✗ có vấn đề · ✓ chấp nhận được) và bằng chứng (mã nguồn ở §7). "Dự án" chỉ nguồn nội bộ [P1]–[P6].

### A. Bố cục và bố trí (LAY)

### LAY-001 — Card hoá mặc định
- **Loại:** B · **Mức độ khi vi phạm:** Moderate; Major nếu lặp trên nhiều màn và che mất phân cấp.
- **Mô tả:** Mọi khối thông tin đều nằm trong một card nổi có viền/bóng/bo góc, kể cả khi nội dung là danh sách đồng
  nhất, một con số hay một đoạn văn.
- **Dấu hiệu:** card chứa đúng một giá trị; danh sách đồng nhất hiển thị thành lưới card; card không dẫn tới chi tiết hay
  hành động; khoảng cách giữa card lớn hơn khoảng cách trong card khiến nhóm bị vỡ.
- **Vì sao:** card làm giảm khả năng quét và so sánh vì vị trí phần tử thay đổi giữa các card, chiếm nhiều diện tích và
  làm mờ thứ hạng [N2].
- **Ngoại lệ:** nội dung không đồng nhất cần tóm tắt và dẫn tới chi tiết [N2]; card là vùng chứa của một tương tác
  [O1]; thẻ quyết định độc lập (mỗi thẻ là một việc cần làm).
- **Thay thế:** vùng cấu trúc + hàng/bảng; dùng tone nền để phân nhóm thay vì khung riêng cho từng mục.
- **Ví dụ:** ✗ sáu card KPI một-số trên trang tổng quan · ✓ thẻ quyết định mỗi thẻ một việc, có hạn và hành động.
- **Bằng chứng:** [N2], [O1] ("Default: no cards"), [C2] ("Cards for every block of info"), [C1]; dự án [P1 §4, mục A].

### LAY-002 — Hộp lồng hộp
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Khung trong khung trong khung (panel → card → inset → chip) mà mỗi lớp không mang nghĩa riêng.
- **Dấu hiệu:** ≥3 lớp có viền hoặc nền riêng trên cùng một đường đọc; padding cộng dồn đẩy nội dung vào giữa; viền
  của lớp ngoài và lớp trong song song nhau.
- **Vì sao:** mỗi lớp tốn padding, tạo nhiễu cạnh và khiến người dùng không biết cấp nào là "nhóm" thật.
- **Ngoại lệ:** hệ vật liệu có tầng rõ ràng và được duyệt (ví dụ region → plate → well) khi mỗi tầng có vai trò ổn định.
- **Thay thế:** gộp lớp; dùng khoảng cách và tone để nhóm; chỉ giữ khung cho đơn vị độc lập.
- **Ví dụ:** ✗ card bo góc chứa card bo góc chứa nút pill · ✓ một vùng, bên trong là hàng phân biệt bằng tone.
- **Bằng chứng:** dự án [P1 §4, §6]; [O1] ("thick borders on every region"); [N1] (thiếu nhóm).

### LAY-003 — Dashboard KPI không có lý do quyết định
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Màn hình chính là tường KPI, biểu đồ, mũi tên xu hướng và "activity feed" mà không chỉ ra việc cần quyết định.
- **Dấu hiệu:** hàng "icon + tiêu đề + số + mũi tên %" lặp; số liệu không gắn hành động hay đích đến; có biểu đồ vì
  "module này nên có biểu đồ".
- **Vì sao:** người dùng tiêu thụ số liệu thụ động thay vì định hướng, ưu tiên và vào quyết định; AI tạo bố cục này
  rất thường xuyên [O1][C1].
- **Ngoại lệ:** sản phẩm thực sự là dashboard giám sát; ô tổng quan có câu hỏi rõ và dẫn tới đích thật (genre shell S6
  của dự án: tối đa 8 ô, mỗi ô một câu hỏi [P2]).
- **Thay thế:** bắt đầu từ "điều gì cần chú ý / quyết định gì"; mỗi chỉ số phải trả lời "quyết định nào tệ hơn nếu bỏ nó".
- **Ví dụ:** ✗ 4 ô "Revenue ▲12%" giả · ✓ "3 quyết định chờ, hạn gần nhất ngày mai" dẫn tới danh sách quyết định.
- **Bằng chứng:** [O1] (tránh "dashboard-card mosaics", "stat strips"), [C1] (stat banner), dự án [P1 §3, §4, §44; mục F],
  [P2].

### LAY-004 — Đối xứng và căn giữa theo mặc định
- **Loại:** B · **Mức độ khi vi phạm:** Minor; Moderate nếu làm khó quét dữ liệu.
- **Mô tả:** Tiêu đề hero căn giữa, mọi cột bằng nhau, nội dung dữ liệu căn giữa "cho cân".
- **Dấu hiệu:** văn bản nhiều dòng căn giữa; số liệu căn giữa trong bảng; cột chia đều dù nội dung không đều.
- **Vì sao:** mắt quét theo mép trái và cột số theo hàng đơn vị; căn giữa phá các mốc đó. Hero căn giữa với font sans
  mặc định là một trong các pattern được đếm nhiều nhất [C1].
- **Ngoại lệ:** màn bắt đầu/đăng nhập có một hành động; thẻ đối đầu "A vs B"; hộp thoại xác nhận.
- **Thay thế:** căn trái văn bản, căn phải số; tỷ lệ cột theo nội dung (cột tên rộng nhất).
- **Ví dụ:** ✗ bảng tài chính căn giữa · ✓ màn "New campaign" căn giữa một form duy nhất.
- **Bằng chứng:** [C1]; dự án [P4 §6 Tables].

### LAY-005 — Khoảng trắng và mật độ không phục vụ nhiệm vụ
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Padding kiểu web sang trọng làm loãng màn hình chuyên gia; hoặc ngược lại nhồi nhét không phân nhóm. Ở màn
  hình lớn, vùng giãn ra thành khoảng trống thay vì hiện thêm thông tin hữu ích.
- **Dấu hiệu:** khoảng trống lớn tách nhãn khỏi giá trị hay hành động; ở 1080p xuất hiện dải trống trong vùng; người dùng
  phải cuộn để thấy điều có thể nằm trong màn đầu.
- **Vì sao:** mật độ có cấu trúc là chuẩn của phần mềm/game quản lý [P1 §12]; khoảng trống tách phần tử liên quan làm
  hỏng nhóm [N1].
- **Ngoại lệ:** màn đọc dài; màn bắt đầu; khoảng trắng phân tách ranh giới quyết định.
- **Thay thế:** khoảng cách theo thang token; ở màn lớn dùng thêm chỗ cho so sánh, bằng chứng, lịch sử [P1 §47].
- **Ví dụ:** ✗ vùng tin rỗng chiếm 40% màn 1080p · ✓ hiển thị thêm hàng lịch sử khi có chỗ.
- **Bằng chứng:** [N1] (khoảng cách lớn tách mật khẩu khỏi liên kết), dự án [P1 §12, §13, §47; mục E].

### LAY-006 — Phân cấp thông tin yếu
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Mọi vùng có trọng số thị giác như nhau; không có vùng chủ đạo; thông tin chính và phụ cùng cỡ, cùng màu.
- **Dấu hiệu:** lưới đều cho nội dung có tầm quan trọng khác nhau; nhiều vùng cùng "hét"; không trả lời được trong
  5 giây "màn này để làm gì".
- **Vì sao:** NN/g và OpenAI đều ghi nhận đầu ra AI có "weak visual hierarchy" [N1][O1]; khi mọi thứ thu hút chú ý thì
  không gì được ưu tiên [P1 §49].
- **Ngoại lệ:** danh sách đồng nhất (mọi hàng ngang cấp là đúng).
- **Thay thế:** xếp loại primary/secondary/tertiary; một vùng chủ đạo; dùng cỡ, vị trí, vật liệu theo tầng quan trọng.
- **Ví dụ:** ✗ ba thẻ quyết định giống hệt dù một thẻ đang chặn tiến trình · ✓ thẻ chặn có dấu và màu cảnh báo + chữ.
- **Bằng chứng:** [N1], [O1], [N2] (card làm mờ thứ hạng), dự án [P1 §14, §49].

### LAY-007 — Vùng trang trí không có chức năng
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Hero lớn, banner chào mừng, ảnh minh hoạ, panel "tổng quan" không trả lời câu hỏi nào.
- **Dấu hiệu:** bỏ vùng đi không làm quyết định nào tệ hơn; vùng chiếm màn đầu nhưng không có dữ liệu trạng thái.
- **Vì sao:** chiếm chỗ của thông tin cần thiết; trong giao diện ứng dụng, OpenAI khuyên không thêm hero trừ khi được yêu
  cầu [O1].
- **Ngoại lệ:** sự kiện được dàn dựng có nội dung thật (ngày thi đấu, kết quả); màn bắt đầu.
- **Thay thế:** trạng thái thật làm điểm mở đầu (thời gian, việc cần làm, hệ quả).
- **Ví dụ:** ✗ ảnh đấu trường sinh tạo làm nền trang · ✓ thẻ trận kế tiếp với hai đội, ngày và mức chuẩn bị.
- **Bằng chứng:** [O1], dự án [P1 §1, §32, §44], [P5 art policy].

### LAY-008 — Một khuôn cho mọi màn
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Mọi màn dùng cùng sườn "tiêu đề + 3 card + bảng" bất kể nhiệm vụ khác nhau (xem xét, so sánh, cam kết).
- **Dấu hiệu:** màn hồ sơ, màn thị trường và màn xác nhận có cùng bố cục; vùng có mặt vì khuôn chứ không vì câu hỏi.
- **Vì sao:** giao diện AI hội tụ về hình thức và bố cục [R1]; nhiệm vụ khác nhau cần cấu trúc khác nhau.
- **Ngoại lệ:** **nhất quán là tính năng** [P1 §50]: dùng chung vật liệu, header, bảng, nút là đúng. Vi phạm là dùng chung
  *bố cục nhiệm vụ* cho nhiệm vụ khác loại.
- **Thay thế:** chọn pattern theo taxonomy màn hình (danh sách, hồ sơ, không gian quyết định, xác nhận) rồi mới tạo hình.
- **Ví dụ:** ✗ màn xác nhận ký hợp đồng trông như trang tổng quan · ✓ không gian quyết định: điều khoản ↔ hệ quả ↔ hành động.
- **Bằng chứng:** [R1], [N1] ("flat and interchangeable"), dự án [P1 §39, §50].

### LAY-009 — Gần thẳng hàng và khoảng cách tuỳ tiện
- **Loại:** A · **Mức độ khi vi phạm:** Minor; Moderate nếu lặp trên toàn hệ thống.
- **Mô tả:** Lề/khoảng cách là số tuỳ ý (13, 21, 27 px) "trông ổn"; phần tử tưởng thẳng hàng nhưng lệch vài px.
- **Dấu hiệu:** giá trị khoảng cách không thuộc thang token; cùng một cấu trúc lặp có khoảng cách khác nhau; mép trái
  các vùng không chung neo.
- **Vì sao:** NN/g ghi nhận "inconsistent margin spacing" trong đầu ra AI [N1]; lệch nhỏ tích luỹ thành cảm giác cẩu thả.
- **Ngoại lệ:** giá trị quang học có lý do được ghi lại (bù hình học, baseline).
- **Thay thế:** thang khoảng cách token; giá trị bố cục tập trung một chỗ.
- **Ví dụ:** ✗ `separation 5`, `7`, `9` rải rác · ✓ tất cả lấy từ `SpaceRole`/bảng layout.
- **Bằng chứng:** [N1], dự án [P1 §36].

### LAY-010 — Không đứng vững ở độ phân giải và nội dung mục tiêu
- **Loại:** A · **Mức độ khi vi phạm:** Major (Critical nếu hành động chính không với tới được).
- **Mô tả:** Thiết kế chỉ đúng với chuỗi mẫu ngắn và một kích thước màn hình; ở kích thước mục tiêu hoặc nội dung dài
  thì tràn, cắt, đẩy hành động chính khỏi màn đầu.
- **Dấu hiệu:** hành động chính dưới nếp gấp ở độ phân giải nhỏ nhất được hỗ trợ; tên dài phá bố cục; giá trị tiền lớn
  tràn cột; màn lớn chỉ là bản kéo giãn.
- **Vì sao:** mockup AI thường dựng ở kích thước tuỳ ý với dữ liệu mẫu ngắn (MCK-005); sản phẩm thật phải chịu dữ liệu thật.
- **Ngoại lệ:** màn cho phép cuộn theo quyết định đã duyệt, khi hành động chính vẫn có đường tắt cố định.
- **Thay thế:** kiểm ở kích thước stress với tên dài, số lớn, trạng thái rỗng/nhiều; ở màn nhỏ hiện ít mục hơn, không
  thu nhỏ chữ.
- **Ví dụ:** ✗ nút cam kết nằm dưới nếp gấp ở 1280×720 · ✓ màn nhỏ hiện 3 thẻ, màn lớn 4 thẻ, cùng cỡ chữ.
- **Bằng chứng:** dự án [P1 §46–§48], [P4 §9]; [G1] (giao diện thay đổi kích thước được).

### B. Typography (TYP)

### TYP-001 — Font chọn theo mặc định, không theo quyết định
- **Loại:** C · **Mức độ khi vi phạm:** Minor (chỉ khi chưa có quyết định typography hoặc trái quyết định đã có).
- **Mô tả:** Dùng font mặc định của framework/model (Inter, Roboto, Arial, system) chỉ vì đó là mặc định, hoặc chọn một
  font "lạ" chỉ để trông khác.
- **Dấu hiệu:** không có lý do được ghi (khả đọc, chữ số tabular, giấy phép, bản địa hoá); kiểu chữ mâu thuẫn tính chất
  sản phẩm.
- **Vì sao:** Anthropic và OpenAI đều ghi nhận model mặc định về Inter/Roboto/Arial [A1][O1]; nhưng **bản thân Inter
  không xấu** — nó là mặc định *vì* khả đọc tốt.
- **Ngoại lệ:** đã có quyết định typography có lý do. Trong dự án này Inter là lựa chọn đã ghi nhận (một family, ba
  weight, chữ số tabular, OFL) [P4 §7] → **không gắn cờ**.
- **Thay thế:** ghi lý do chọn font; nếu cần cá tính, đặt nó vào một chỗ (display) thay vì đổi toàn bộ.
- **Ví dụ:** ✗ đổi Inter sang Space Grotesk "cho bớt AI" · ✓ giữ Inter, dùng tabular figures cho số thay đổi.
- **Bằng chứng:** [A1], [A2], [O1], [O2] (Inter nằm trong danh sách font gợi ý), [C1].

### TYP-002 — Chữ quá khổ kiểu marketing
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Tiêu đề khổng lồ, "số to nhãn nhỏ" làm điểm nhấn mặc định, cỡ chữ của landing page trong màn công cụ.
- **Dấu hiệu:** tiêu đề trang chiếm nhiều chỗ hơn nội dung chính; nhiều con số cỡ display trên cùng màn; chữ to dùng để
  bù cho phân cấp yếu.
- **Vì sao:** skill frontend-design của Anthropic cảnh báo mẫu "big number, small label, gradient accent" [A3]; màn quản
  lý cần chữ cỡ trung dễ đọc [P1 §10].
- **Ngoại lệ:** một con số là trung tâm của quyết định (ví dụ rating tổng của cầu thủ trong hồ sơ — pattern của thể loại
  và có trong mockup đã duyệt).
- **Thay thế:** thang chữ có kiểm soát; chỉ nâng một bậc ở nơi mang quyết định [P4 §7].
- **Ví dụ:** ✗ ba số 48 px trên một màn · ✓ một số 48 px trong hồ sơ, phần còn lại 14–20 px.
- **Bằng chứng:** [A3], dự án [P1 §10].

### TYP-003 — Thang chữ không kiểm soát
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Cỡ và weight tự do theo từng màn; nhiều family; bold khắp nơi.
- **Dấu hiệu:** cỡ chữ ngoài thang; một màn dùng >3 weight; override font size cục bộ.
- **Vì sao:** phá nhất quán và phân cấp; khó bảo trì.
- **Ngoại lệ:** glyph fallback cho ký hiệu mà family thiếu.
- **Thay thế:** vai trò typography (role) → cỡ/weight từ token.
- **Ví dụ:** ✗ 15 px và 17 px xen kẽ · ✓ 12/14/16/20/24/32/48.
- **Bằng chứng:** dự án [P1 §10], [P4 §7]; [A3] (thang chữ rõ ràng).

### TYP-004 — Chữ hoa, tracking và nhãn phụ trang trí
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Nhãn "eyebrow" viết hoa giãn chữ phía trên mọi tiêu đề; nhãn lặp lại điều tiêu đề đã nói; một từ trong
  tiêu đề được tô màu nhấn.
- **Dấu hiệu:** mỗi khối có một dòng chữ hoa nhỏ thừa; tiêu đề toàn chữ hoa cho văn bản đọc; tracking trên chữ thường.
- **Vì sao:** [A3] liệt kê chữ hoa trang trí, nhãn thừa phía trên nội dung và tô một từ tiêu đề là dấu hiệu chung chung;
  [C1] đếm tiêu đề toàn chữ hoa.
- **Ngoại lệ:** chữ hoa cho nhãn danh mục/tiêu đề vùng khi đó là quy ước đã duyệt (dự án này: tiêu đề vùng và nhãn
  metric viết hoa, tracking 1 px [P4 §7]) → **không gắn cờ** trong game screen có mockup.
- **Thay thế:** chỉ dùng nhãn phụ khi nó thêm thông tin (danh mục, hạn); câu văn để chữ thường.
- **Ví dụ:** ✗ "OVERVIEW" phía trên tiêu đề "Overview" · ✓ "SPONSOR OFFER" + "Due tomorrow" trên thẻ quyết định.
- **Bằng chứng:** [A3], [C1], dự án [P4 §7].

### TYP-005 — Monospace để trông "kỹ thuật"
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Metadata, số, nhãn dùng font monospace/code để tạo cảm giác "dữ liệu", "terminal".
- **Dấu hiệu:** ngày, ID, số tiền trong giao diện người dùng cuối dùng font code; mã nội bộ hiển thị như dữ liệu.
- **Vì sao:** tạo cảm giác công cụ debug; chữ số tabular của font UI đã giải quyết căn hàng.
- **Ngoại lệ:** mã thật người dùng cần sao chép (seed, key); sản phẩm cho lập trình viên.
- **Thay thế:** chữ số tabular (`tnum`) của font UI.
- **Ví dụ:** ✗ `DAY_024` monospace trong thanh trên · ✓ "Day 24" với chữ số tabular.
- **Bằng chứng:** dự án [P3] ("Proportional tabular numerals… not a code/monospace face"); kinh nghiệm chuyên môn.

### TYP-006 — Cắt chữ làm mất nghĩa
- **Loại:** A · **Mức độ khi vi phạm:** Moderate; Major nếu mất tên người, số tiền hoặc ngày.
- **Mô tả:** Nhãn, tên hoặc số bị cắt giữa từ hay bằng dấu "…" đến mức không còn hiểu hoặc không phân biệt được.
- **Dấu hiệu:** "League p…", "Payments, estim…"; hai mục khác nhau hiển thị cùng một chuỗi bị cắt; số tiền bị cắt trông
  như số khác.
- **Vì sao:** người dùng phải đoán; mất phân biệt giữa các mục. Văn bản phụ nên được bỏ trọn vẹn thay vì cắt [P4 §7].
- **Ngoại lệ:** văn bản dài có giới hạn rõ, đầy đủ trong tooltip **và** tại đích đến (tiêu đề tin, tên tổ chức dài).
- **Thay thế:** nhãn ngắn hơn ở chế độ compact; ẩn trọn phần phụ; cho cột nhãn ưu tiên hơn cột trang trí.
- **Ví dụ:** ✗ nhãn "League p…" ở 1280×720 · ✓ "Place" hoặc ẩn dòng "Playoff places" khi không đủ chỗ.
- **Bằng chứng:** dự án [P4 §7, §13], [P1 §46, §48].

### TYP-007 — Chữ quá nhỏ hoặc quá mờ
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Chữ dưới ngưỡng đọc; chữ xám nhạt trên nền tối dưới 4,5:1; chú thích 10–11 px.
- **Dấu hiệu:** đo tương phản < 4,5:1 (chữ thường) hoặc < 3:1 (chữ lớn); cỡ < 12 px trong game PC.
- **Vì sao:** WCAG 1.4.3 [W2]; Game Accessibility Guidelines yêu cầu cỡ chữ mặc định dễ đọc và tương phản cao [G1];
  chữ xám tương phản thấp trên dark theme là pattern được đếm trong [C1].
- **Ngoại lệ:** trạng thái disabled (vẫn nên đọc được); chữ trang trí thuần tuý không mang nghĩa.
- **Thay thế:** token chữ có tương phản đã đo trên mọi nền; sàn cỡ chữ.
- **Ví dụ:** ✗ chữ #6B7280 trên #111 · ✓ mọi vai trò chữ ≥4,5:1 trên mọi bề mặt (đo bằng script).
- **Bằng chứng:** [W2], [G1], [C1], [N1] ("Poor color contrast").

### C. Màu sắc và hiệu ứng (COL)

### COL-001 — Gradient tím/xanh mặc định
- **Loại:** B · **Mức độ khi vi phạm:** Moderate (khi không có lý do thương hiệu).
- **Mô tả:** Gradient tím–xanh/indigo trên nền trắng hoặc tối làm "chất" mặc định; nút chính tím.
- **Dấu hiệu:** màu chủ đạo không đến từ thương hiệu/hướng đã duyệt; gradient trên nút, tiêu đề, nền không mang nghĩa.
- **Vì sao:** Anthropic gọi thẳng "purple gradients on white backgrounds" là mặc định khi không có hướng dẫn [A1][A2];
  gradient và nút chính màu tím nằm trong các pattern được đếm [C1][C2]. Giả thuyết phổ biến gắn hiện tượng này với
  `bg-indigo-500` của Tailwind UI [C3] (chưa được kiểm chứng như quan hệ nhân quả).
- **Ngoại lệ:** thương hiệu thật dùng tím/gradient; gradient mang thông tin (thang giá trị, ánh sáng mô phỏng vật liệu).
- **Thay thế:** màu từ hướng đã duyệt; nền phẳng hoặc vật liệu có lý do; một màu nhấn chủ đạo.
- **Ví dụ:** ✗ nút CTA gradient tím trong game quản lý graphite · ✓ wash màu đội trong thẻ đối đầu (màu là danh tính).
- **Bằng chứng:** [A1], [A2], [C1], [C2], [C3]; dự án [P1 §5, §33].

### COL-002 — Glow và neon trang trí
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Viền phát sáng, quầng màu lớn, neon trên dark theme để tạo cảm giác "premium" hay "gaming".
- **Dấu hiệu:** glow không chỉ ra tương tác, trạng thái hay phân cấp; mọi panel có quầng sáng.
- **Vì sao:** glow trang trí cạnh tranh với nội dung mà không mang nghĩa [C2]; "large colored glows" là pattern được đếm
  [C1]; governance dự án loại neon/cyberpunk esports [P1 §33].
- **Ngoại lệ:** focus ring; một điểm nhấn thông báo thay đổi trạng thái có thời hạn; ánh sáng mô phỏng vật liệu một hướng.
- **Thay thế:** dùng tương phản bề mặt và một hướng sáng nhất quán.
- **Ví dụ:** ✗ viền cyan phát sáng quanh mọi card · ✓ nháy sáng 0,7 s trên giá trị vừa đổi, tắt khi reduced motion.
- **Bằng chứng:** [C1], [C2], dự án [P1 §5, §33; mục D, I].

### COL-003 — Bóng đổ không biểu đạt lớp
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Shadow đậm hoặc có màu trên mọi khối, không phản ánh độ cao thật.
- **Dấu hiệu:** panel nền cũng nổi; bóng màu ("vibrant box shadow"); hướng sáng không nhất quán.
- **Vì sao:** bóng nên biểu đạt elevation/overlay/modal/drag [P1 §7]; bóng màu là pattern được đếm [C1].
- **Ngoại lệ:** hệ vật liệu có hướng sáng thống nhất được duyệt.
- **Thay thế:** tầng bề mặt + tương phản; bóng chỉ cho lớp nổi thật.
- **Ví dụ:** ✗ bóng tím dưới nút · ✓ overlay xác nhận có bóng và nền mờ phía sau.
- **Bằng chứng:** [C1], dự án [P1 §7].

### COL-004 — Glassmorphism không có lý do
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Panel trong mờ/blur trên nền phức tạp làm chất liệu mặc định.
- **Dấu hiệu:** chữ trên nền trong mờ có tương phản thay đổi theo vị trí; nhiều lớp kính chồng nhau.
- **Vì sao:** NN/g: có thể gây vấn đề đọc vì nền xuyên qua làm tương phản thay đổi; dùng tiết chế để tạo chiều sâu [N3].
- **Ngoại lệ:** nền được kiểm soát, blur đủ mạnh, tương phản đạt WCAG ở trường hợp xấu nhất [N3].
- **Thay thế:** bề mặt đặc; nếu dùng kính, kiểm tương phản trên nền xấu nhất.
- **Ví dụ:** ✗ HUD kính trên ảnh đấu trường · ✓ overlay tối 55% sau hộp thoại.
- **Bằng chứng:** [N3], [C1], dự án [P1 §5].

### COL-005 — Bảng màu/chủ đề mặc định không gắn sản phẩm
- **Loại:** C · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Chọn dark mode "vì trông cao cấp", hay bảng màu thời thượng không liên quan sản phẩm.
- **Dấu hiệu:** không thể giải thích màu chủ đạo bằng sản phẩm, người dùng hay môi trường sử dụng.
- **Vì sao:** "always dark mode" là pattern được đếm [C1]; nhưng dark theme **không** sai tự thân.
- **Ngoại lệ:** hướng đã duyệt. Dự án này: graphite/steel/brass theo mockup đã duyệt [P4 §1–§2] → **không gắn cờ**.
- **Thay thế:** bảng màu từ chất liệu, ngữ cảnh và người dùng của sản phẩm; ghi lý do.
- **Ví dụ:** ✗ chuyển sang nền kem + terracotta "cho khác" · ✓ giữ graphite đã duyệt.
- **Bằng chứng:** [C1], [A3] (danh sách sáo mòn), [A1] ("Commit to a cohesive aesthetic").

### COL-006 — Màu nhấn tràn lan
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Nhiều màu nhấn bão hoà ngang hàng; mỗi mục điều hướng một màu; dải màu cạnh mỗi khối.
- **Dấu hiệu:** >1 màu nhấn không mang ngữ nghĩa trên một màn; "rainbow navigation"; nhiều nút màu khác nhau ngang cấp.
- **Vì sao:** "Dominant colors with sharp accents outperform timid, evenly-distributed palettes" [A1]; bảng neon không ưu
  tiên gì và dải màu cạnh khối triệt tiêu nhau [C2].
- **Ngoại lệ:** màu danh tính dữ liệu (huy hiệu đội), màu ngữ nghĩa trạng thái.
- **Thay thế:** nền trung tính + một màu nhấn + màu ngữ nghĩa khi cần [P1 §9].
- **Ví dụ:** ✗ mỗi section rail một màu · ✓ chỉ mục đang chọn có màu nhấn.
- **Bằng chứng:** [A1], [C2], dự án [P1 §9].

### COL-007 — Màu ngữ nghĩa dùng sai hoặc không nhất quán
- **Loại:** A · **Mức độ khi vi phạm:** Moderate; Major nếu che cảnh báo thật.
- **Mô tả:** Màu positive/warning/critical dùng để trang trí, theo giá trị cố định không phụ thuộc trạng thái, hoặc cùng
  một trạng thái mang màu khác nhau ở các nơi.
- **Dấu hiệu:** số luôn xanh "thành công" bất kể giá trị; chi phí bình thường tô màu cảnh báo; cùng trạng thái "chặn" vừa
  vàng vừa đỏ; token hành động mượn token trạng thái.
- **Vì sao:** người dùng học màu như ngôn ngữ; dùng sai làm loãng cảnh báo thật [P1 §8]; "status dots that don't mean
  anything" là dấu hiệu được nêu [C2].
- **Ngoại lệ:** quy ước thể loại rõ (thắng xanh/thua đỏ) khi luôn đi kèm chữ.
- **Thay thế:** bảng ánh xạ trạng thái → màu duy nhất; độ lớn dùng tone trung tính; token hành động riêng.
- **Ví dụ:** ✗ rating 45 vẫn màu "positive" · ✓ rating màu chữ chính, ngưỡng thể hiện bằng nhãn.
- **Bằng chứng:** [C2], dự án [P1 §8], [P4 §6 "amber only… red never"].

### COL-008 — Màu là kênh duy nhất mang nghĩa
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Trạng thái, đúng/sai, biết/ước tính/không biết chỉ phân biệt bằng màu.
- **Dấu hiệu:** bỏ màu (ảnh xám) thì mất nghĩa; chấm màu không chữ; dòng tô đỏ không nhãn.
- **Vì sao:** WCAG 1.4.1 [W2]; Game Accessibility Guidelines: không truyền thông tin thiết yếu bằng màu cố định [G1].
- **Ngoại lệ:** màu chỉ củng cố một tín hiệu khác (chữ, hình, vị trí).
- **Thay thế:** marker hình học + chữ (● ◐ ○ ▲ ■) + màu.
- **Ví dụ:** ✗ chấm xanh/đỏ cạnh tên · ✓ "▲ 1 blocking" màu cảnh báo.
- **Bằng chứng:** [W2], [G1], dự án [P1 §15], [P3].

### COL-009 — Tương phản thấp
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Chữ hoặc thành phần điều khiển không đủ tương phản, đặc biệt trên ảnh, gradient, wash màu động.
- **Dấu hiệu:** chữ < 4,5:1; đường viền điều khiển thiết yếu < 3:1 (WCAG 1.4.11); chữ trên nền màu lấy từ dữ liệu
  (màu đội) không được kiểm trường hợp xấu nhất.
- **Vì sao:** [N1] ghi nhận tương phản kém trong đầu ra AI; [N3] chỉ ra tương phản thay đổi theo nền.
- **Ngoại lệ:** trang trí không mang nghĩa.
- **Thay thế:** đo token; kiểm nền động bằng bảng màu xấu nhất; thêm lớp đệm (scrim).
- **Ví dụ:** ✗ chữ vàng trên wash đỏ chưa đo · ✓ script đo mọi cặp token chữ/bề mặt.
- **Bằng chứng:** [W2], [N1], [N3], [G1].

### D. Thành phần (CMP)

### CMP-001 — Bo góc quá mức và bo lồng bo
- **Loại:** B · **Mức độ khi vi phạm:** Minor; Moderate nếu trái hướng đã duyệt.
- **Mô tả:** Panel bo lớn chứa card bo lớn chứa nút bo chứa pill; bo góc để bù phân cấp yếu.
- **Dấu hiệu:** nhiều bán kính khác nhau trên một màn; bán kính trong lớn hơn hoặc bằng bán kính ngoài.
- **Vì sao:** governance gọi đích danh pattern này [P1 §6]; [O1] tránh "rounded media cards".
- **Ngoại lệ:** bo góc nhỏ, một họ bán kính nhất quán; pill cho tag ngữ nghĩa. Dự án này dùng 45° chamfer, bán kính 0
  [P4 §3]: mọi bo góc trong game screen là trái chuẩn.
- **Thay thế:** một họ hình học (bán kính hoặc góc vát) theo token.
- **Ví dụ:** ✗ `rounded-2xl` trong `rounded-2xl` · ✓ chamfer 4/6/11/16 px theo tầng.
- **Bằng chứng:** [O1], dự án [P1 §6; mục B], [P4 §3].

### CMP-002 — Badge/pill tràn lan
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Nhiều pill trên mỗi thực thể; badge thay cho phân cấp; badge không bao giờ thay đổi giá trị.
- **Dấu hiệu:** ≥2 badge trên một hàng; metadata thường ("Updated", "New") được đóng thành chip; badge trên mọi mục có
  cùng một giá trị.
- **Vì sao:** [O1] tránh "pill clusters"; badge phía trên tiêu đề là pattern được đếm [C1]; governance: badge chỉ cho
  trạng thái phân loại ảnh hưởng tới cách hiểu [P1 §22].
- **Ngoại lệ:** trạng thái phân loại thật (Active, Expired, Pending, Unknown); badge hằng số có lý do trung thực đã được
  duyệt (ví dụ nhãn độ chắc chắn của tin).
- **Thay thế:** metadata để dạng chữ; một badge trạng thái khi cần.
- **Ví dụ:** ✗ "NEW · HOT · PRO · 2025" trên mỗi hàng · ✓ một chip "● CONFIRMED" theo chuẩn tin tức.
- **Bằng chứng:** [O1], [C1], [C2], dự án [P1 §22; mục C].

### CMP-003 — Giao diện thư viện mặc định chưa tuỳ biến
- **Loại:** B · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Component Tailwind/shadcn/Material ở trạng thái mặc định làm nên toàn bộ ngôn ngữ thị giác.
- **Dấu hiệu:** token mặc định của thư viện (màu, bán kính, shadow, font) không bị chạm tới; giao diện giống demo của
  thư viện.
- **Vì sao:** công cụ AI mặc định dùng Shadcn/Tailwind [N1]; v0 sinh UI bằng shadcn/ui + Tailwind [V1]; stack gợi ý của
  OpenAI cũng vậy [O2]; "shadcn/ui" chưa đổi là một pattern được đếm [C1].
- **Ngoại lệ:** công cụ nội bộ chấp nhận vẻ ngoài thư viện; thư viện là design system đã duyệt.
- **Thay thế:** giữ thư viện, đặt token của sản phẩm (không cần thêm dependency mới).
- **Ví dụ:** ✗ game dùng card shadcn trắng mặc định · ✓ cùng component nhưng theo token sản phẩm.
- **Bằng chứng:** [N1], [V1], [O2], [C1].

### CMP-004 — Hộp chứa icon thừa
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Mỗi icon nằm trong ô vuông/tròn có nền màu; "feature card" với icon trong ô ở trên cùng.
- **Dấu hiệu:** ô chứa icon lặp ở mọi card; nền tối sau art trong suốt tạo "ô inventory".
- **Vì sao:** "identical icon-topped feature cards" là pattern được đếm [C1]; dự án ghi nhận nền tối sau art trong suốt
  đọc như hộp đen [P4 §6, §8].
- **Ngoại lệ:** housing có vai trò cấu trúc nhất quán (ví dụ icon trong header vùng của dự án).
- **Thay thế:** icon đặt trực tiếp trên bề mặt; art vẽ trần.
- **Ví dụ:** ✗ 3 ô icon tím trên 3 card tính năng · ✓ art rail đặt thẳng trên bề mặt mục.
- **Bằng chứng:** [C1], dự án [P4 §6, §8].

### CMP-005 — Thành phần thiếu nhất quán ngữ nghĩa
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Cùng một khái niệm/hành động có hai cách trình bày; hoặc hai khái niệm khác nhau dùng chung một cách trình bày.
- **Dấu hiệu:** cùng hành động "Review offer" là nút nhỏ ở màn này, nút rộng ở màn khác; nút "Back" thứ hai bên trong
  một vùng đã có điều hướng; nhãn nút không khớp đích đến.
- **Vì sao:** người dùng học component như từ vựng; NN/g ghi nhận thiếu nhất quán trong đầu ra AI [N1].
- **Ngoại lệ:** biến thể theo ngữ cảnh được định nghĩa trong catalog (compact/roomy).
- **Thay thế:** catalog component + biến thể có tên; review J "component inconsistency" [P1 §42].
- **Ví dụ:** ✗ "Return to company" mà đưa về Commercial · ✓ nhãn nói đúng nơi sẽ tới.
- **Bằng chứng:** [N1], dự án [P1 §37; mục J].

### CMP-006 — Viền và đường phân cách thừa
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Viền quanh mọi vùng, mọi ô bảng, mọi hàng; đường kẻ dọc trong bảng.
- **Dấu hiệu:** lưới ô có viền; hai viền song song sát nhau; viền đen đặc.
- **Vì sao:** [O1] tránh "thick borders on every region"; đường kẻ phải phản ánh cấu trúc thật [A3].
- **Ngoại lệ:** ranh giới điều khiển thiết yếu (≥3:1); bảng tính cần lưới.
- **Thay thế:** tone hàng xen kẽ; khoảng cách; một đường phân cách khi cấu trúc cần.
- **Ví dụ:** ✗ viền mỗi ô bảng · ✓ hàng phân biệt bằng tone, không kẻ dọc.
- **Bằng chứng:** [O1], [A3], dự án [P4 §2, §6].

### CMP-007 — Nhiều CTA ngang hàng
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Nhiều nút cùng màu nhấn mạnh trên một màn; hành động phá huỷ trông như điều hướng.
- **Dấu hiệu:** ≥2 nút primary trong một workspace; nút "Delete" cùng kiểu "Next".
- **Vì sao:** nhấn mạnh phải phản ánh tầm quan trọng của quyết định [P1 §23].
- **Ngoại lệ:** danh sách thẻ, mỗi thẻ có hành động cục bộ được định nghĩa là biến thể nhỏ hơn (dự án: verb quyết định
  dùng vật liệu brass nhỏ [P4 §6]).
- **Thay thế:** một primary/workspace; secondary trung tính; phá huỷ có kiểu riêng + xác nhận.
- **Ví dụ:** ✗ năm nút vàng cùng cỡ · ✓ Continue là điều khiển mạnh nhất, verb thẻ nhỏ hơn.
- **Bằng chứng:** dự án [P1 §23], [P3 P6], [P4 §6].

### CMP-008 — Thiếu trạng thái tương tác
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Thiếu hover, pressed, focus nhìn thấy được, disabled, selected; hoặc các trạng thái trông như nhau.
- **Dấu hiệu:** điều hướng bàn phím không thấy focus; disabled không giải thích lý do; mục đang chọn không phân biệt.
- **Vì sao:** WCAG 2.4.7 focus visible [W2]; tín hiệu bấm yếu làm tăng ~22% thời gian tìm và ~25% số lần nhìn [N5];
  skill frontend-design đặt focus nhìn thấy được vào "quality floor" [A3].
- **Ngoại lệ:** không có.
- **Thay thế:** một hình học cho mọi trạng thái; bảng trạng thái được render và kiểm.
- **Ví dụ:** ✗ nút disabled không nói vì sao · ✓ sheet trạng thái normal/hover/pressed/disabled/focused cho mọi biến thể.
- **Bằng chứng:** [W2], [N5], [A3], dự án [P4 §6].

### CMP-009 — Vùng bấm quá nhỏ
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Mục tiêu bấm < 24×24 px không có khoảng đệm; link chữ nhỏ là đường duy nhất.
- **Dấu hiệu:** icon button 16 px; chip có hành động; segment quá thấp.
- **Vì sao:** WCAG 2.2 SC 2.5.8 (AA) yêu cầu ≥24×24 CSS px trừ ngoại lệ [W1]; [G1] yêu cầu điều khiển lớn và cách xa.
- **Ngoại lệ:** theo [W1] (khoảng cách, inline, tương đương).
- **Thay thế:** token kích thước tối thiểu; kiểm tự động.
- **Ví dụ:** ✗ nút "×" 12 px · ✓ `MinimumTarget = 24`.
- **Bằng chứng:** [W1], [G1].

### CMP-010 — Tự chế thành phần/kiểu cục bộ
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Màn hình định nghĩa style, màu, khoảng cách, component riêng thay vì dùng hệ thống.
- **Dấu hiệu:** style inline; màu hex trong màn hình; component trùng chức năng component có sẵn.
- **Vì sao:** governance: màn hình compose, không redefine; AI không được lặng lẽ phát minh component [P1 §37, §38, §41].
- **Ngoại lệ:** đề xuất bổ sung nhỏ nhất, có lý do, đưa vào hệ thống [P1 §38].
- **Thay thế:** thử compose bằng cái có sẵn → nêu vì sao thất bại → thêm phần nhỏ nhất dùng lại được.
- **Ví dụ:** ✗ `new StyleBoxFlat` trong màn Squad · ✓ mở rộng `Surfaces` và ghi vào chuẩn.
- **Bằng chứng:** dự án [P1 §37, §38, §41], [P4 §10].

### E. Biểu tượng và trang trí (ICO)

### ICO-001 — Icon trước mọi nhãn
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Mỗi nhãn, mỗi dòng, mỗi tiêu đề đều có icon (thường Lucide-style).
- **Dấu hiệu:** icon không giúp nhận diện, quét hay điều hướng; nhiều icon cùng hình cho khái niệm khác nhau.
- **Vì sao:** NN/g: rất ít icon mang nghĩa phổ quát, icon cần nhãn chữ; icon không rõ nghĩa thành "eye candy" [N4].
- **Ngoại lệ:** điều hướng, trạng thái, tài nguyên lặp lại, header vùng theo mockup đã duyệt.
- **Thay thế:** icon chỉ nơi cải thiện nhận diện; luôn kèm chữ.
- **Ví dụ:** ✗ icon trước từng metric thanh trên · ✓ art module trên rail + nhãn.
- **Bằng chứng:** [N4], [O2] (Lucide trong stack gợi ý), dự án [P1 §20; mục G].

### ICO-002 — Icon và ký hiệu chỉ để trang trí
- **Loại:** A · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Sparkles ✨, mũi tên, chấm, ngôi sao, icon "AI" rắc vào khoảng trống.
- **Dấu hiệu:** bỏ icon không mất thông tin; icon trong chỗ trống của component.
- **Vì sao:** [N4] icon trang trí làm chậm nhiệm vụ; governance loại sparkles và huy hiệu gamification [P1 §20, §35].
- **Ngoại lệ:** mũi tên chỉ hướng điều hướng ("View all →"), hướng di chuyển (A → B).
- **Thay thế:** bỏ.
- **Ví dụ:** ✗ ✨ cạnh "Insights" · ✓ "origin → destination" trong tin chuyển nhượng.
- **Bằng chứng:** [N4], dự án [P1 §20, §35].

### ICO-003 — Emoji làm icon hệ thống
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Emoji dùng làm icon điều hướng, trạng thái, tài nguyên, bullet.
- **Dấu hiệu:** 🏆 💰 📈 trong menu hoặc tiêu đề; emoji khác nhau giữa nền tảng.
- **Vì sao:** thiếu nhất quán, khác nhau theo OS; "emoji in sidebar/nav" và "emojis used as icons" là dấu hiệu được nêu
  [C1][C2]; governance cấm [P1 §21].
- **Ngoại lệ:** marker phát triển nội bộ, rõ ràng không phải UI người dùng.
- **Thay thế:** một họ icon vector nhất quán.
- **Ví dụ:** ✗ "💰 Finance" · ✓ icon vector "finance" + chữ.
- **Bằng chứng:** [C1], [C2], dự án [P1 §21].

### ICO-004 — Biểu đồ, vòng, thanh tiến trình vô nghĩa
- **Loại:** A · **Mức độ khi vi phạm:** Moderate; Major nếu gây hiểu sai.
- **Mô tả:** Biểu đồ không có câu hỏi; ring/donut/thanh cho mọi chỉ số; sparkline giả.
- **Dấu hiệu:** không nói được biểu đồ trả lời câu hỏi gì; thanh tiến trình cho đại lượng không bị chặn; trục/đơn vị thiếu.
- **Vì sao:** governance: chart cần câu hỏi, progress bar chỉ cho đại lượng bị chặn [P1 §17, §18]; "stat banner" là
  pattern được đếm [C1].
- **Ngoại lệ:** đại lượng bị chặn thật (capacity, thắng/số trận); công cụ trực quan có text tương đương [P3 P4].
- **Thay thế:** số + chữ; bảng; một instrument có câu hỏi rõ.
- **Ví dụ:** ✗ ring "Team Form 80%" không định nghĩa · ✓ ring thắng/trận đã đấu với "0–1 W–L" cạnh bên.
- **Bằng chứng:** [C1], dự án [P1 §17, §18], [P2 S7], [P3].

### ICO-005 — Hoạ tiết và lớp trang trí lặp
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Góc kim loại, đinh tán, mạch phát sáng, đường kẻ góc, lưới hex, lớp viền chồng; "đánh bóng" bằng lớp thừa.
- **Dấu hiệu:** trang trí bằng nhau ở mọi vùng; trang trí không theo tầng quan trọng.
- **Vì sao:** "Equal decoration everywhere" và "gold lines on corners" đã bị loại trong review của dự án [P4 §13].
- **Ngoại lệ:** trang trí theo tầng quan trọng đã duyệt (premium/standard/utility [P4 §4]).
- **Thay thế:** hình học thật (silhouette), ít lớp hơn.
- **Ví dụ:** ✗ đường vàng ở cả bốn góc mọi panel · ✓ insert brass chỉ ở vùng premium.
- **Bằng chứng:** dự án [P4 §1, §13], [P1 §33].

### ICO-006 — Nghĩa icon/ký hiệu không nhất quán
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Một icon/marker mang nhiều nghĩa ở các nơi; marker cảnh báo dùng cho "có thể gia hạn"; marker lỗi dùng cho
  "thua trận".
- **Dấu hiệu:** bảng định nghĩa marker không khớp cách dùng; icon giống nhau cho hai khái niệm.
- **Vì sao:** "Icon meaning must remain consistent globally" [P1 §20].
- **Ngoại lệ:** không có.
- **Thay thế:** bảng ngữ nghĩa marker/icon duy nhất, kiểm khi review.
- **Ví dụ:** ✗ "■" (critical) đánh dấu trận thua · ✓ "L"/"Defeat" bằng chữ.
- **Bằng chứng:** dự án [P1 §20], [P3 (bảng marker)].

### F. Nội dung và kiến trúc thông tin (CNT)

### CNT-001 — Nội dung placeholder hoặc mẫu lọt vào sản phẩm
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Lorem ipsum, "John Doe", "Acme", tên/giá trị/ngày lấy từ mockup, ID nội bộ hiển thị cho người dùng.
- **Dấu hiệu:** chuỗi cứng trong mã UI; giá trị trùng với mockup; tên placeholder của nội dung (`DEV_ORG_001`) ở màn chính.
- **Vì sao:** sai dữ liệu, mất niềm tin; dự án yêu cầu mọi thứ đến từ read model [P4 §8].
- **Ngoại lệ:** fixture kiểm thử có tiền tố rõ (`DEV …`) trong ảnh chụp kiểm thử.
- **Thay thế:** dữ liệu từ read model; fallback trung tính.
- **Ví dụ:** ✗ "Raven Arena" từ mockup · ✓ "○ Team unknown" khi không biết.
- **Bằng chứng:** dự án [P4 §8], [P6].

### CNT-002 — Giọng marketing trong giao diện vận hành
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Lời chào, khẩu hiệu, phụ đề quảng cáo trong màn công việc ("Welcome back", "Build the best roster for success").
- **Dấu hiệu:** câu không chứa trạng thái hay hành động; phụ đề giống tagline.
- **Vì sao:** OpenAI: bề mặt sản phẩm nên ưu tiên "utility copy over marketing copy", định hướng/trạng thái/hành động
  [O1]; governance cấm "Welcome back, CEO" [P1 §11].
- **Ngoại lệ:** màn bắt đầu/giới thiệu sản phẩm.
- **Thay thế:** trạng thái thật ("6 players · every role is covered").
- **Ví dụ:** ✗ "Manage your players and build the best roster for success" · ✓ "▲ No player for Support: matches will be
  forfeited".
- **Bằng chứng:** [O1], dự án [P1 §11].

### CNT-003 — KPI, xu hướng hoặc độ chính xác bịa
- **Loại:** A · **Mức độ khi vi phạm:** Critical khi trình bày như dữ liệu thật; Major trong mockup được dùng làm spec.
- **Mô tả:** "▲12% vs last month", "67,4% expected success", sân đấu, thể thức, phong độ mà hệ thống không sở hữu.
- **Dấu hiệu:** giá trị không truy ra được nguồn; độ chính xác vượt mô hình; Unknown hiển thị như 0.
- **Vì sao:** phá độ trung thực; người dùng ra quyết định trên dữ liệu giả [P1 §15], [P4 §8].
- **Ngoại lệ:** không có trong sản phẩm; trong mockup phải được đánh dấu là minh hoạ.
- **Thay thế:** chỉ hiện điều mô phỏng sở hữu; Known/Estimated/Unknown phân biệt bằng marker + chữ.
- **Ví dụ:** ✗ "Win probability 67.4%" · ✓ "◐ Strength 63–79 (estimate)".
- **Bằng chứng:** dự án [P1 §15], [P4 §8]; [R1] (lý do được tuyên bố nhưng không hiện thực).

### CNT-004 — Thiếu thông tin đặc thù miền
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Màn có cấu trúc chung chung nhưng thiếu dữ liệu chuyên môn cần cho quyết định; số không có nhãn/đơn vị.
- **Dấu hiệu:** so sánh không có mốc đối chiếu (ứng viên có chỉ số, người hiện tại không); "74/84" không nói là gì.
- **Vì sao:** NN/g ghi nhận AI hiểu sai pattern và đặt thông tin phụ trên nhiệm vụ chính [N1].
- **Ngoại lệ:** thông tin thật sự chưa tồn tại trong mô phỏng → nêu là khoảng trống, không bịa.
- **Thay thế:** xuất phát từ câu hỏi của người chơi; mỗi số có nhãn và đơn vị.
- **Ví dụ:** ✗ "Slyvka · 74/84" · ✓ "Slyvka · rating 74 · ready 84".
- **Bằng chứng:** [N1], dự án [P1 §1, §44].

### CNT-005 — Ưu tiên thông tin sai
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Thông tin chính nằm trong tooltip hoặc dưới nếp gấp; thông tin phụ chiếm vị trí nổi bật; hai thông tin cùng
  nhóm dùng mốc khác nhau mà không nói rõ.
- **Dấu hiệu:** hạn chót chỉ có trong tooltip; một nhóm "14 ngày tới" chứa con số 7 ngày.
- **Vì sao:** governance cấm giấu PRIMARY trong tooltip [P1 §14, §28]; [N1] secondary info đặt trên nhiệm vụ chính.
- **Ngoại lệ:** định nghĩa, cơ sở tính toán, viết tắt có thể ở tooltip [P1 §28].
- **Thay thế:** phân loại primary/secondary/tertiary cho từng vùng; một mốc thời gian mỗi nhóm.
- **Ví dụ:** ✗ "NEXT 14 DAYS" + "7-day estimate" chung một khối · ✓ mỗi khối một mốc, ghi rõ.
- **Bằng chứng:** [N1], dự án [P1 §14, §28].

### CNT-006 — Thiếu trạng thái empty/error/loading/disabled/selected
- **Loại:** A · **Mức độ khi vi phạm:** Moderate; Major nếu lỗi bị nuốt.
- **Mô tả:** Chỉ thiết kế "happy path"; rỗng hiện vùng trống; lỗi biến mất; disabled không có lý do.
- **Dấu hiệu:** không có ảnh chụp/kiểm thử cho rỗng, một mục, nhiều mục, tên dài, thiếu art, lỗi; thông báo lỗi tự ẩn
  không để lại dấu vết.
- **Vì sao:** skill frontend-design yêu cầu lỗi nói điều gì xảy ra và cách sửa, empty state mời hành động [A3]; bài
  phân tích [C3] nêu đầu ra chung chung thường thiếu validation/error state.
- **Ngoại lệ:** không có.
- **Thay thế:** ma trận trạng thái cho mỗi màn; empty state trung thực, gọn.
- **Ví dụ:** ✗ nút disabled im lặng · ✓ "No major transfer developments to report." + dòng giải thích.
- **Bằng chứng:** [A3], [C3], dự án [P6 portal v2 README: empty/single/many/long-name/missing-art/unknown/distress].

### CNT-007 — Văn bản quy trình nội bộ lọt vào không gian người dùng
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Câu hỏi thiết kế, ghi chú hợp đồng, legend bàn phím, enum thô, log mô phỏng, nhãn kiểu dữ liệu hiển thị
  cho người dùng.
- **Dấu hiệu:** "Who plays, how good are they…?" làm phụ đề; "Competitive Portfolio · Portfolio"; "contributed 0";
  "prize/bonus"; "· Esc" trong nhãn nút.
- **Vì sao:** thesis của dự án xác định developer/contract/keyboard-legend text là một trong năm nguyên nhân giao diện
  trông như công cụ admin [P3]; amendment giữ nó là không thương lượng [P2].
- **Ngoại lệ:** tooltip phím tắt; chế độ debug ngoài không gian người chơi.
- **Thay thế:** câu hỏi thiết kế ở blueprint/review record; người chơi thấy trạng thái và hành động.
- **Ví dụ:** ✗ "Business & Finance · Function" · ✓ "Finance".
- **Bằng chứng:** dự án [P3 (bảng nguyên nhân, P5)], [P2], [P4 §7].

### CNT-008 — Một sự thật, nhiều cách diễn đạt
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Cùng một giá trị hiển thị với đơn vị, định dạng hoặc cách nói khác nhau giữa các màn.
- **Dấu hiệu:** "$370 a week for 12 weeks" / "$370 per week for 84 days" / "$370 weekly"; số ngày ở chỗ này, số tuần chỗ kia.
- **Vì sao:** người dùng phải quy đổi và nghi ngờ hai con số khác nhau; governance: một sự thật có nhiều view nhưng
  không tự tính khác [P1 §25].
- **Ngoại lệ:** độ chính xác khác nhau theo quy ước đã định (tóm tắt làm tròn, chi tiết chính xác).
- **Thay thế:** một formatter cho mỗi loại giá trị; văn bản sinh từ read model dùng cùng formatter.
- **Ví dụ:** ✗ ba cách viết cùng một mức giá · ✓ "$370/week" ở mọi nơi.
- **Bằng chứng:** dự án [P1 §25], [P4 §7 Numbers and money].

### CNT-009 — Điều khiển hoặc điều hướng không dẫn tới workflow thật
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Tab, filter, nút, section cho tính năng chưa có; breadcrumb gọi tên nút không tồn tại; nút back trùng lặp.
- **Dấu hiệu:** điều khiển không làm gì hoặc làm khác nhãn; section khoá "coming soon"; đường dẫn dùng taxonomy khác
  điều hướng chính.
- **Vì sao:** [N1] ghi nhận AI thêm nội dung không được yêu cầu; [R1] 4/5 công cụ hiện thực ≤6% nguyên tắc chức năng;
  amendment cấm section cho cơ chế chưa có [P2].
- **Ngoại lệ:** không có trong sản phẩm.
- **Thay thế:** chỉ hiện điều khiển có hành vi; nhãn = đích đến.
- **Ví dụ:** ✗ "Business & Finance / Sponsorship" khi rail chỉ có "Commercial" · ✓ "Commercial / Offer review".
- **Bằng chứng:** [N1], [R1], dự án [P1 §24], [P2].

### CNT-010 — Quyết định thiếu nhân quả, chi phí hoặc thời điểm
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Màn cam kết không nêu đủ chi phí, hệ quả và khi nào xảy ra; "+5 / −3" tách rời nguyên nhân.
- **Dấu hiệu:** hộp xác nhận có khoản tiền "chưa nêu số"; hệ quả chỉ ở màn khác.
- **Vì sao:** governance yêu cầu cause → commitment → consequence → time [P1 §16]; thesis: cam kết có trọng lượng [P3].
- **Ngoại lệ:** hệ quả thật sự không biết → hiện "○ Unknown" kèm lý do.
- **Thay thế:** tổng chi phí trước khi xác nhận; before → after với thời điểm.
- **Ví dụ:** ✗ "Pay $174 plus the old coach's exit cost now" · ✓ "Pay $174 + $X exit cost = $Y now".
- **Bằng chứng:** dự án [P1 §16], [P3 pillar 3].

### CNT-011 — Vẻ ngoài bóng bẩy che logic tương tác kém
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Giao diện "đẹp từ xa" nhưng luồng sai: không xác nhận cam kết, ràng buộc không hiển thị, giá trị nhập không
  được kiểm, trạng thái không phản hồi.
- **Dấu hiệu:** review chỉ dựa vào ảnh tĩnh; không thử luồng; các ô nhập phần trăm không cho biết tổng.
- **Vì sao:** NN/g: "Good from afar, but far from good" [N1]; [R1]: lý do được tuyên bố tự tin nhưng không có trong sản phẩm.
- **Ngoại lệ:** không có.
- **Thay thế:** kiểm luồng (accept/reject/pending, quay lại, lỗi) trước khi đánh giá thẩm mỹ.
- **Ví dụ:** ✗ nút "Accept" trực tiếp trên thẻ Portal · ✓ mở workspace → review → xác nhận.
- **Bằng chứng:** [N1], [R1], dự án [P5 AI rules: implementation handoff checklist].

### G. Bản sắc sản phẩm và art direction (IDN)

### IDN-001 — Giao diện có thể hoán đổi
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Bỏ tên/logo thì không nhận ra sản phẩm; màn có thể đặt vào bất kỳ SaaS/template nào.
- **Dấu hiệu:** câu hỏi nhận dạng "nếu bỏ branding, còn đọc ra là game quản lý không?" trả lời Không [P1 §43].
- **Vì sao:** NN/g: màn AI "flat and interchangeable" [N1]; [R1] đo được độ đồng nhất; Anthropic: "cookie-cutter design
  that lacks context-specific character" [A1].
- **Ngoại lệ:** công cụ tiện ích nội bộ.
- **Thay thế:** bản sắc từ trạng thái, thời gian, thế giới, hệ quả của chính sản phẩm [P3].
- **Ví dụ:** ✗ bảng admin trắng · ✓ thanh trên có thời gian, tiền, việc chờ và Continue.
- **Bằng chứng:** [N1], [R1], [A1], dự án [P1 §43; mục H].

### IDN-002 — Phong cách không hợp miền sản phẩm
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Game quản lý mặc SaaS; sản phẩm doanh nghiệp mặc neon gaming; "game feel" từ huy hiệu, XP bar, sparkles.
- **Dấu hiệu:** governance mục H (SaaS) hoặc I (neon esports) trả lời Có [P1 §42].
- **Vì sao:** phong cách phải truyền tải bản chất sản phẩm (mô phỏng và hệ quả) [P1 §33–§35].
- **Ngoại lệ:** hướng art được phê duyệt rõ ràng.
- **Thay thế:** game feel từ trạng thái thay đổi, rủi ro, thời gian [P1 §35].
- **Ví dụ:** ✗ HUD cyan lục giác · ✓ graphite/steel/brass theo mockup đã duyệt.
- **Bằng chứng:** dự án [P1 §33–§35, §42].

### IDN-003 — Art direction không nhất quán giữa các màn
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Một màn (thường là prototype cũ được nhúng) dùng ngôn ngữ khác: định danh khác, nút khác, cây danh sách
  kiểu admin, khoảng cách khác.
- **Dấu hiệu:** chuyển màn thấy đổi hệ (monogram thay vì crest; nút phẳng thay vì vật liệu; cấu trúc cây có hậu tố kiểu).
- **Vì sao:** sản phẩm đọc như ghép từ nhiều công cụ; governance: nhất quán hơn mới lạ [P1 §50].
- **Ngoại lệ:** công cụ debug ngoài không gian người dùng.
- **Thay thế:** đưa màn về vật liệu/thành phần dùng chung hoặc tạm ẩn khỏi điều hướng người chơi.
- **Ví dụ:** ✗ section Company là bản đồ vận hành Stage 3 trong shell Stage 5 · ✓ cùng region/header/bảng.
- **Bằng chứng:** dự án [P1 §50; mục J], [P3].

### IDN-004 — Trang trí thay cho bản sắc
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Dùng ảnh sinh tạo, minh hoạ, nền 3D, chân dung để "làm giàu" khi UX yếu.
- **Dấu hiệu:** art không có giá trị nhận diện/quyết định; chân dung người thật bịa; ảnh nền văn phòng/đấu trường.
- **Vì sao:** governance cấm art sinh tạo để lấp chỗ [P1 §31, §32]; chính sách art yêu cầu nêu giá trị, ngữ cảnh dùng lại,
  fallback [P5].
- **Ngoại lệ:** asset ngữ nghĩa đã duyệt cho đúng khái niệm của nó.
- **Thay thế:** đánh dấu danh tính sinh từ dữ liệu; fallback trung thực.
- **Ví dụ:** ✗ ảnh khán đài sinh tạo trong tin · ✓ huy hiệu của đội liên quan + màu đội.
- **Bằng chứng:** dự án [P1 §31, §32], [P5], [P4 §8].

### IDN-005 — Thay template này bằng template khác
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** "Chữa" vẻ AI bằng một thời trang khác: editorial be/terracotta, đen + xanh axit, tactical neon, brutalist,
  serif nghiêng tạo điểm nhấn, đổi font sang Space Grotesk/Instrument Serif/Geist.
- **Dấu hiệu:** thay đổi không xuất phát từ sản phẩm/người dùng; hướng mới không có lý do ngoài "bớt giống AI".
- **Vì sao:** Anthropic ghi nhận model hội tụ về lựa chọn mặc định mới (Space Grotesk) [A1]; skill frontend-design liệt
  kê cả kem/terracotta và gần-đen/xanh-axit là sáo mòn [A3]; Krebs đếm cụ thể các cặp font "thay thế" thời thượng [C1].
- **Ngoại lệ:** hướng mới được Director duyệt có lý do sản phẩm.
- **Thay thế:** cải thiện trong hướng đã duyệt; nếu cần hướng mới, đề xuất có lý do và chờ duyệt.
- **Ví dụ:** ✗ đổi shell graphite sang nền kem serif · ✓ sửa phân cấp và nội dung trong vật liệu hiện có.
- **Bằng chứng:** [A1], [A3], [C1].

### H. Mockup do AI sinh ảnh (MCK)

### MCK-001 — Chữ trong mockup sai hoặc không đọc được
- **Loại:** B · **Mức độ khi vi phạm:** Minor (Major nếu chữ sai bị chép vào sản phẩm).
- **Mô tả:** Lỗi chính tả, chữ méo, chữ dày đặc nhỏ không đọc được trong ảnh mockup.
- **Dấu hiệu:** từ sai ("seconnd half" trong mockup Portal của dự án); số và nhãn không khớp nhau.
- **Vì sao:** tài liệu OpenAI nêu giới hạn về đặt chữ chính xác và độ rõ của chữ [O3].
- **Ngoại lệ:** không áp dụng cho bản dựng UI thật.
- **Thay thế:** không bao giờ chép chữ từ mockup; nội dung đến từ read model.
- **Ví dụ:** ✗ copy tiêu đề tin từ mockup · ✓ dùng mockup chỉ cho bố cục.
- **Bằng chứng:** [O3], dự án [P6].

### MCK-002 — Mockup không nhất quán với nhau
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Các ảnh trong cùng bộ có điều hướng, thanh trên, định dạng thời gian, component khác nhau.
- **Dấu hiệu:** bộ mục rail khác nhau giữa các mockup; thanh trên lúc "Day 24/365" lúc "Season 1 · Week 3".
- **Vì sao:** OpenAI nêu giới hạn duy trì nhất quán của yếu tố lặp lại giữa các lần sinh [O3].
- **Ngoại lệ:** biến thể khám phá có chủ đích.
- **Thay thế:** chọn một nguồn chuẩn cho thành phần dùng chung (code + polish standard); mockup chỉ quyết định bố cục màn.
- **Ví dụ:** ✗ thêm section "Training" vì một mockup có · ✓ giữ 8 section thật.
- **Bằng chứng:** [O3], dự án [P6], [P2 S2].

### MCK-003 — Tính năng và điều khiển ảo giác trong mockup
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Mockup vẽ tính năng không tồn tại: tin đồn, đàm phán, mục tiêu công ty, đơn vị phát triển, lịch, cài đặt,
  nút Accept/Reject trực tiếp.
- **Dấu hiệu:** điều khiển không ánh xạ được tới command nào trong read model/command list.
- **Vì sao:** [N1] ghi nhận đầu ra thêm nội dung không yêu cầu; [R1] khoảng cách giữa tuyên bố và hiện thực.
- **Ngoại lệ:** mockup khám phá được đánh dấu "concept".
- **Thay thế:** đối chiếu từng điều khiển với command/read field; ghi khoảng trống thay vì hiện thực giả.
- **Ví dụ:** ✗ hiện chip "Rumour" khi không có cơ chế thông tin công khai · ✓ chỉ "● CONFIRMED".
- **Bằng chứng:** [N1], [R1], dự án [P4 §8], [P5 AI rules].

### MCK-004 — Nội dung mẫu của mockup bị coi là dữ liệu
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Tên, giá trị, ngày, sân đấu, chân dung, ảnh trong mockup bị chép vào mã hoặc dùng làm yêu cầu.
- **Dấu hiệu:** chuỗi trong mã trùng mockup; asset sinh tạo xuất hiện như nội dung.
- **Vì sao:** README mockup của dự án ghi rõ mọi tên/giá trị/ảnh là minh hoạ [P6]; UiKit fail khi gặp mẫu mockup trong mã [P4 §8].
- **Ngoại lệ:** không có.
- **Thay thế:** dùng asset ngữ nghĩa đã duyệt; thiếu art thì fallback trung thực.
- **Ví dụ:** ✗ "Raven Arena", "+3,200" · ✓ chỉ ngày trận từ lịch thi đấu.
- **Bằng chứng:** dự án [P6], [P4 §8].

### MCK-005 — Bố cục/tỷ lệ mockup không hiện thực được
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Mockup dựng ở kích thước tuỳ ý, chữ quá nhỏ, mật độ không đạt ở độ phân giải thật, tỷ lệ thành phần không
  nhất quán, chữ đè lên ảnh bận.
- **Dấu hiệu:** ảnh mockup không phải 1280×720/1920×1080; nhiều cỡ chữ < 12 px khi quy đổi.
- **Vì sao:** OpenAI nêu giới hạn điều khiển bố cục trong các composition nhạy layout [O3].
- **Ngoại lệ:** không có.
- **Thay thế:** dựng lại ở độ phân giải thật; ít mục hơn thay vì chữ nhỏ hơn.
- **Ví dụ:** ✗ ép 4 thẻ quyết định có 2 nút vào 1280 px · ✓ 3 thẻ ở 1280, 4 thẻ ở 1920.
- **Bằng chứng:** [O3], dự án [P4 §9].

### MCK-006 — Mô hình tương tác phi thực tế trong mockup
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Cam kết không có xác nhận, thông tin chỉ khi hover, chỉnh sửa trực tiếp trên thẻ tổng quan, hành động phá huỷ
  một chạm.
- **Dấu hiệu:** nút Accept/Release/Hire trên thẻ tóm tắt; không có đường quay lại.
- **Vì sao:** governance: cam kết không đảo ngược phải được xác nhận; không dùng modal cho xem thông thường [P1 §26, §27], [P2].
- **Ngoại lệ:** hành động có thể hoàn tác ngay.
- **Thay thế:** thẻ → workspace/review → xác nhận → quay lại đúng ngữ cảnh.
- **Ví dụ:** ✗ "Accept" trên thẻ Portal · ✓ "Review offer →".
- **Bằng chứng:** dự án [P1 §26, §27], [P2].

### MCK-007 — Bắt chước cả ảnh thay vì trích thuộc tính
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Sao chép mọi đặc điểm bề mặt của ảnh tham chiếu (kể cả màu mẫu tình cờ) thay vì xác định thuộc tính nào cần
  theo (bố cục, tỷ lệ, vật liệu, phân cấp).
- **Dấu hiệu:** giá trị luôn tô xanh vì mockup tô xanh một giá trị cao; giữ phụ đề quảng cáo của mockup.
- **Vì sao:** ảnh tham chiếu giúp suy ra nhịp bố cục, thang chữ, hệ khoảng cách [O1] — tức là *thuộc tính*, không phải
  từng pixel; governance: screenshot là tham chiếu thẩm quyền thấp [P1 §40].
- **Ngoại lệ:** không có.
- **Thay thế:** bảng trích thuộc tính: theo / điều chỉnh / bỏ qua, mỗi dòng có lý do.
- **Ví dụ:** ✗ rating luôn xanh "positive" · ✓ theo vị trí và cỡ của rating, màu theo ngữ nghĩa.
- **Bằng chứng:** [O1], dự án [P1 §40], [P6].

### I. Chuyển động (MOT)

### MOT-001 — Chuyển động trang trí
- **Loại:** B · **Mức độ khi vi phạm:** Minor.
- **Mô tả:** Nhấp nháy liên tục, nền trôi, shimmer, card bay vào mỗi lần mở màn, spring/bounce lớn.
- **Dấu hiệu:** chuyển động không giải thích điều hướng, thay đổi trạng thái hay kết quả mới.
- **Vì sao:** governance: chuyển động phải giải thích thay đổi [P1 §29, §30]. Lưu ý ngữ cảnh: [A1] khuyến khích một lần
  tải trang có stagger cho trang web — hợp với landing page, không hợp màn lặp lại [P1 §45].
- **Ngoại lệ:** xác nhận thay đổi, chuyển ngữ cảnh, tiết lộ kết quả.
- **Thay thế:** chuyển động ngắn, có lý do, tắt được.
- **Ví dụ:** ✗ shimmer trên mọi số · ✓ highlight 0,7 s trên giá trị vừa đổi.
- **Bằng chứng:** [A1], dự án [P1 §29, §30, §45].

### MOT-002 — Không có đường reduced motion
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Không có cách tắt chuyển động, hoặc tắt chuyển động làm mất thông tin.
- **Dấu hiệu:** không có cài đặt; thông tin chỉ truyền qua hoạt ảnh.
- **Vì sao:** WCAG 2.3.3 [W2]; [G1] tuỳ chọn tắt chuyển động nền, tránh chuyển động đột ngột; [A3] reduced motion là
  "quality floor".
- **Ngoại lệ:** không có.
- **Thay thế:** một tuỳ chọn đặt thời lượng về 0; mọi thay đổi còn dấu hiệu tĩnh.
- **Ví dụ:** ✗ kết quả chỉ hiện qua hoạt ảnh · ✓ `ReducedMotion` → thời lượng 0, bố cục không đổi.
- **Bằng chứng:** [W2], [G1], [A3], dự án [P1 §29].

### J. Quy trình làm việc với AI (WFL)

### WFL-001 — Thiết kế hình trước, nhồi dữ liệu sau
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Bắt đầu từ một bố cục đẹp rồi ép thông tin gameplay vào.
- **Dấu hiệu:** không có blueprint (mục tiêu, câu hỏi chính, primary/secondary, hành động, trạng thái lỗi) trước khi vẽ.
- **Vì sao:** thứ tự thẩm quyền: gameplay → ngữ cảnh → ưu tiên → tương tác → bố cục → phong cách → trang trí [P1 §1, §39].
- **Ngoại lệ:** khám phá concept được đánh dấu rõ.
- **Thay thế:** blueprint trước; mockup sau.
- **Ví dụ:** ✗ vẽ dashboard rồi tìm số liệu điền · ✓ liệt kê câu hỏi của người chơi rồi chọn vùng.
- **Bằng chứng:** dự án [P1 §1, §39], [P5].

### WFL-002 — "Design theater": lý do thiết kế không có trong sản phẩm
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Mô tả/giải thích thiết kế tự tin ("đã tối ưu phân cấp", "đã thêm empty state") nhưng giao diện không có.
- **Dấu hiệu:** báo cáo nêu thứ không tìm thấy trong mã hoặc ảnh chụp.
- **Vì sao:** >25% lý do không được hiện thực, 34% với yêu cầu chức năng [R1].
- **Ngoại lệ:** không có.
- **Thay thế:** mỗi tuyên bố trỏ tới file:dòng hoặc ảnh chụp.
- **Ví dụ:** ✗ "đã kiểm 1280×720" mà không có ảnh · ✓ đường dẫn ảnh chụp kèm kết quả.
- **Bằng chứng:** [R1].

### WFL-003 — Tuyên bố đã kiểm chứng mà không render
- **Loại:** A · **Mức độ khi vi phạm:** Major.
- **Mô tả:** Kết luận về hình thức thị giác chỉ từ tên class/mã, hoặc báo xong UI mà chưa xem kết quả render.
- **Dấu hiệu:** không có ảnh chụp; "trông sẽ ổn".
- **Vì sao:** OpenAI: công cụ trình duyệt/ảnh chụp giúp model "verify its work visually" và tăng khả năng giao diện hoàn
  chỉnh [O1]; chiến lược kiểm chứng của dự án tách kiểm kỹ thuật khỏi duyệt thị giác [P1 governance].
- **Ngoại lệ:** khi không render được: ghi rõ "chưa kiểm chứng trực quan", hạ confidence.
- **Thay thế:** render ở độ phân giải mục tiêu, đọc ảnh, rồi mới kết luận.
- **Ví dụ:** ✗ "không còn cắt chữ" mà chưa chụp · ✓ ảnh 1280×720 và 1920×1080 kèm nhận xét.
- **Bằng chứng:** [O1], dự án [P4 §12].

### WFL-004 — "Đánh bóng" bằng cách thêm trang trí
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Mỗi vòng sửa thêm viền, glow, vàng, lớp; vòng lặp thiết kế lại không có bằng chứng cải thiện.
- **Dấu hiệu:** số lớp/viền/màu nhấn tăng qua các phiên bản trong khi phân cấp không đổi.
- **Vì sao:** chuẩn của dự án: "More borders or more gold is not progress" [P4 §1]; [A3]: "Spend your boldness in one
  place" và tự phê bình/đơn giản hoá sau khi dựng.
- **Ngoại lệ:** không có.
- **Thay thế:** mỗi vòng sửa phải gắn với một finding cụ thể; dừng khi finding đã đóng.
- **Ví dụ:** ✗ vòng 3 thêm viền vàng thứ hai · ✓ vòng 3 bỏ một lớp và sửa nhãn cắt chữ.
- **Bằng chứng:** dự án [P4 §1, §13], [A3].

### WFL-005 — Thay đổi thị giác ngoài phạm vi
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Sửa một màn nhưng "tiện tay" đổi token, component dùng chung, hay màn khác.
- **Dấu hiệu:** diff chạm file không liên quan yêu cầu; golden/evidence cập nhật để "im" diff.
- **Vì sao:** governance: không restyle cục bộ, không cập nhật golden chỉ để im diff [P1 §37], [P5 AI rules].
- **Ngoại lệ:** sửa hệ thống được yêu cầu rõ.
- **Thay thế:** phạm vi nhỏ nhất; nêu mọi file thay đổi và lý do.
- **Ví dụ:** ✗ đổi màu brass toàn cục khi sửa một nút · ✓ chỉ sửa nhãn và ghi lại.
- **Bằng chứng:** dự án [P1 §37], [P5].

### WFL-006 — Kết luận "do AI làm" từ thẩm mỹ
- **Loại:** A · **Mức độ khi vi phạm:** Moderate.
- **Mô tả:** Gắn nhãn "AI slop"/"do AI tạo" cho một màn hoặc người làm dựa trên pattern thị giác.
- **Dấu hiệu:** finding nói về tác giả thay vì tác hại.
- **Vì sao:** chính phân tích định lượng có 5–10% dương tính giả và nói một pattern không đủ để kết luận [C1]; nhãn tác
  giả không giúp sửa gì.
- **Ngoại lệ:** không có.
- **Thay thế:** mô tả pattern, tác hại và cách sửa.
- **Ví dụ:** ✗ "màn này trông như AI làm" · ✓ "LAY-001: 6 card một-số làm mất thứ hạng; đề xuất bảng".
- **Bằng chứng:** [C1].

## 6. Áp dụng trong dự án này

### 6.1. Ánh xạ sang governance hiện có

Rulebook **không thay thế** các luật của dự án; nó đặt tên ổn định và bổ sung bằng chứng ngoài.

| Nguồn của dự án | Quy tắc tương ứng |
|---|---|
| Brief §1 (thẩm quyền, câu hỏi vùng), §39 | WFL-001, LAY-007 |
| Brief §3–§4, mục A, F | LAY-001, LAY-002, LAY-003 |
| Brief §5, §33, mục D, I | COL-001, COL-002, COL-004, IDN-002 |
| Brief §6, mục B | CMP-001 |
| Brief §7 | COL-003 |
| Brief §8–§9 | COL-006, COL-007 |
| Brief §10–§11 | TYP-002, TYP-003, CNT-002 |
| Brief §12–§13, §47, mục E | LAY-005 |
| Brief §14, §28 | CNT-005 |
| Brief §15 | COL-008, CNT-003 |
| Brief §16 | CNT-010 |
| Brief §17–§18 | ICO-004 |
| Brief §20–§22, mục C, G | ICO-001, ICO-002, ICO-003, CMP-002, ICO-006 |
| Brief §23 | CMP-007 |
| Brief §24, amendment "no sections for unbuilt mechanics" | CNT-009 |
| Brief §25 | CNT-008 |
| Brief §29–§30 | MOT-001, MOT-002 |
| Brief §31–§32, art policy | IDN-004, MCK-004 |
| Brief §34, §43, mục H | IDN-001, IDN-002 |
| Brief §36 | LAY-009 |
| Brief §37–§38, §41, mục J | CMP-005, CMP-010, WFL-005 |
| Brief §40, mockup README | MCK-001 … MCK-007 |
| Brief §46–§48 | LAY-010, TYP-006 |
| Brief §50 | IDN-003, LAY-008 |
| Thesis P5, amendment (developer/keyboard-legend text) | CNT-007 |
| Polish standard §1, §13 | WFL-004, ICO-005 |

### 6.2. Lựa chọn đã duyệt mà quy tắc chung có thể gắn cờ — **không** báo là vi phạm

| Lựa chọn đã duyệt | Quy tắc chung | Lý do giữ | Nguồn |
|---|---|---|---|
| Một family Inter, 3 weight | TYP-001 | Quyết định có lý do: khả đọc, chữ số tabular, OFL | [P4 §7] |
| Tiêu đề vùng và nhãn metric viết hoa, tracking 1 px | TYP-004 | Quy ước của mockup và chuẩn | [P4 §7] |
| Dark graphite/steel/brass | COL-005 | Hướng của mockup đã duyệt | [P4 §1–§2], [P6] |
| Góc vát 45°, bán kính 0 | CMP-001 | Ngôn ngữ hình học được duyệt | [P4 §3] |
| Verb brass nhỏ trên thẻ quyết định Portal | CMP-007 | Biến thể "local decision verb" được định nghĩa | [P4 §6] |
| Ring thắng/trận và thanh segment ở Portal | ICO-004 | Đại lượng bị chặn, có text tương đương; thesis P4 | [P3], mockup 11 |
| Chip "● CONFIRMED" trên mọi tin chuyển nhượng | CMP-002 | Trung thực về độ chắc chắn; chuẩn bị cho trạng thái khác | [P4 §8] |
| Art module 3D trên rail | ICO-001, CMP-004 | Asset ngữ nghĩa đã duyệt; không có housing | [P4 §6] |
| Wash màu đội trong thẻ trận | COL-001, COL-002 | Màu là danh tính dữ liệu; thể loại "broadcast card" | [P4], mockup 11 |
| Rating 48 px trong hồ sơ cầu thủ | TYP-002 | Quy ước thể loại, có trong mockup 03 | [P6] |

Lưu ý: các luật nghiêm hơn từ lần phê bình đề xuất HTML ngày 2026-10-07 (không icon, chữ hoa chỉ cho mã/nút, vàng chỉ
cho Continue/Commit, không eyebrow, không nối " · ", huy hiệu không chữ) chỉ áp dụng cho **trang đề xuất/nghiên cứu của
AI không có mockup đã duyệt**, không áp dụng cho game screen có mockup.

## 7. Nguồn

Ngày truy cập web: 2026-10-22. Mức tin cậy: **Cao** (chính thức/tiêu chuẩn/nghiên cứu có phương pháp), **Trung bình**
(phân tích cộng đồng có phương pháp nhưng một tác giả), **Thấp** (ý kiến chuyên gia, nguồn thứ cấp).

| ID | Nguồn | Loại · Tin cậy |
|---|---|---|
| A1 | Anthropic, "Prompting for frontend aesthetics", Claude Cookbook, 2025-10-21. https://platform.claude.com/cookbook/coding-prompting-for-frontend-aesthetics | Chính thức · Cao (về xu hướng mặc định của Claude) |
| A2 | Anthropic (Rajasekaran, Wei, Bricken), "Improving frontend design through Skills", 2025-11-12. https://claude.com/blog/improving-frontend-design-through-skills | Chính thức · Cao |
| A3 | Anthropic, `anthropics/skills` — `skills/frontend-design/SKILL.md` (nhánh main). https://github.com/anthropics/skills/blob/main/skills/frontend-design/SKILL.md | Chính thức · Cao; nội dung đọc qua bản tóm tắt của công cụ |
| A4 | Anthropic, Claude Code docs — Skills. https://code.claude.com/docs/en/skills | Đặc tả chính thức · Cao |
| O1 | OpenAI, "Designing delightful frontends with GPT-5.4" (trang không ghi ngày). https://developers.openai.com/blog/designing-delightful-frontends-with-gpt-5-4 | Chính thức · Cao |
| O2 | OpenAI Cookbook, "Frontend with GPT-5" (đã lưu trữ). https://developers.openai.com/cookbook/examples/gpt-5/gpt-5_frontend | Chính thức · Cao (chỉ dùng cho stack mặc định) |
| O3 | OpenAI API docs, Image generation — Limitations. https://developers.openai.com/api/docs/guides/image-generation | Chính thức · Cao |
| V1 | Vercel Academy, "UI with v0". https://vercel.com/academy/ai-sdk/ui-with-v0 | Chính thức (nhà cung cấp) · Cao |
| N1 | Wang & Brown, "Good from Afar, But Far from Good: AI Prototyping in Real Design Contexts", NN/g, 2025-10-24. https://www.nngroup.com/articles/ai-prototyping/ | Nghiên cứu UX · Cao (một dự án, định hướng) |
| N2 | Laubheimer, "Cards: UI-Component Definition", NN/g, 2016-11-06. https://www.nngroup.com/articles/cards-component/ | Nghiên cứu UX · Cao |
| N3 | Brown, "Glassmorphism", NN/g, 2024-06-07. https://www.nngroup.com/articles/glassmorphism/ | Nghiên cứu UX · Cao |
| N4 | Harley, "Icon Usability", NN/g, 2014-07-27. https://www.nngroup.com/articles/icon-usability/ | Nghiên cứu UX · Cao |
| N5 | Moran, "Flat UI Elements Attract Less Attention and Cause Uncertainty", NN/g, 2017-09-03. https://www.nngroup.com/articles/flat-ui-less-attention-cause-uncertainty/ | Nghiên cứu eyetracking (71 người) · Cao |
| R1 | Imteyaz et al., "Design Theater: Evaluating the Gap Between User-Facing Design Reasoning and Implementation in Generative UI Tools", arXiv:2607.22928 (2026; AAAI/ACM AIES). https://arxiv.org/abs/2607.22928 | Nghiên cứu · Cao |
| R2 | Jung, Garcinuno, Mateega, "UI-Bench", arXiv:2508.20410 (2025). https://arxiv.org/abs/2508.20410 | Benchmark chất lượng thị giác · Trung bình (không đo độ đồng nhất; chỉ tham khảo) |
| C1 | Adrian Krebs, "Design slop", 2026-04-20. https://www.adriankrebs.ch/blog/design-slop/ | Phân tích cộng đồng (script DOM/CSS, 1.590 trang Show HN) · Trung bình |
| C2 | Jeff Humble, "7 Signs a UI Has Been Vibe Coded", The Fountain Institute. https://www.thefountaininstitute.com/blog/signs-vibe-coded-ui | Ý kiến chuyên gia · Thấp–Trung bình |
| C3 | "Why Your AI Keeps Building the Same Purple Gradient Website", prg.sh, 2025-10-26. https://prg.sh/ramblings/Why-Your-AI-Keeps-Building-the-Same-Purple-Gradient-Website — dẫn bài đăng của Adam Wathan (8/2025) https://x.com/adamwathan/status/1953510802159219096 (bài gốc chưa xác minh) | Blog · Thấp |
| W1 | W3C, "Understanding SC 2.5.8 Target Size (Minimum)", WCAG 2.2. https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html | Tiêu chuẩn · Cao (đã xác minh) |
| W2 | W3C, WCAG 2.2 — SC 1.4.1, 1.4.3, 1.4.11, 2.4.7, 2.3.3. https://www.w3.org/TR/WCAG22/ | Tiêu chuẩn · Cao (dẫn từ kiến thức sẵn có, không tải lại) |
| G1 | Game Accessibility Guidelines — full list. https://gameaccessibilityguidelines.com/full-list/ | Hướng dẫn ngành · Cao |
| P1 | Dự án: [`STAGE5_VISUAL_UX_BRIEF.txt`](../../01-governance/STAGE5_VISUAL_UX_BRIEF.txt) và [governance](../../01-governance/STAGE5_VISUAL_UX_GOVERNANCE.md) (50 luật, review A–J, gate 14 tiêu chí) | ACTIVE |
| P2 | Dự án: [genre-shell amendment](../../01-governance/STAGE5_GENRE_SHELL_AMENDMENT.md) | ACTIVE, thẩm quyền mới hơn brief |
| P3 | Dự án: [game-identity thesis](../../01-governance/STAGE5_GAME_IDENTITY_THESIS.md) | ACTIVE |
| P4 | Dự án: [UI polish standard](UI_POLISH_STANDARD.md) | ACTIVE |
| P5 | Dự án: [AI UI implementation rules](AI_UI_IMPLEMENTATION_RULES.md), [art production policy](ART_PRODUCTION_POLICY.md) | Proposal được DEC-023 chấp nhận làm kiến trúc |
| P6 | Dự án: [UI research README](../ui-research/README.md), mockup `generated-reference-set/`, [portal v2 README](../ui-research/mockups/portal-world-transfer-v2/README.md) | Tham chiếu bố cục đã duyệt; nội dung mẫu không phải dữ liệu |
