# Kế hoạch khắc phục UI/UX

Trạng thái: **PROPOSAL — chưa được phép thực hiện** (2026-10-22). Tài liệu này sắp xếp các finding của
[audit 2026-10-22](UI_UX_AUDIT_2026-10-22.md) thành các đợt sửa; không có thay đổi mã nào đi kèm. Mỗi đợt cần người
dùng cho phép riêng.

Nguyên tắc:
- Sửa nhỏ nhất có tác động lớn; sửa ở token, component, formatter trước khi sửa từng màn.
- Không thiết kế lại toàn bộ giao diện; không đổi hướng thị giác đã duyệt (graphite/steel/brass, góc vát 45°, Inter).
- Mỗi thay đổi kèm ảnh trước/sau ở 1280×720 và 1920×1080 (`build/Capture-Shell.ps1 … -Output artifacts/<thư-mục>`),
  `build/Verify-Shell.ps1`, và các test liên quan; không cập nhật evidence/golden chỉ để làm im khác biệt.
- Công sức tương đối: **S** ≤ nửa ngày, **M** 1–2 ngày, **L** > 2 ngày (ước lượng cho một người quen mã).

## 0. Quyết định cần có trước (Director / người dùng)

| # | Câu hỏi | Phương án | Khuyến nghị | Mở khoá |
|---|---|---|---|---|
| D1 | "Blocking" nghĩa là gì? (T5) | (a) giữ luật, đổi từ ngữ UI · (b) cho các việc đó chặn Continue thật | **(a)** — luật đã được test khoá có chủ ý | UXA-01, UXA-08 |
| D2 | Section Company đi hướng nào? (T4) | (a) chỉnh tối thiểu trong vật liệu shell · (b) blueprint mới theo mockup 10 bỏ phần ảo giác · (c) tạm ẩn khỏi rail | **(a)** ngay, (b) khi có blueprint | UXA-04, UXA-05 |
| D3 | Legend phím trong nhãn nút? (T1) | (a) bỏ, đưa vào tooltip · (b) giữ và sửa thesis/amendment | **(a)** — rail và Continue đã dùng tooltip | UXA-22 |
| D4 | Tin đầu World news được phóng to? (T2) | (a) giới hạn quy tắc §8 cho Transfer news · (b) bỏ lead, mọi tin cùng cỡ · (c) thêm luật quan trọng biên tập | **(a)** — khớp mockup 11 đã duyệt; sửa câu trong chuẩn | UXA-23 |
| D5 | Hành động chính của section ở 1280×720? (T3) | (a) chấp nhận cuộn (chuẩn §9) · (b) hành động luôn thấy (brief §46) | **(b)** cho Competition và hồ sơ Squad | UXA-17 |
| D6 | Read model có thêm exit cost và kỹ năng ước tính của HLV hiện tại? | có / không | **có** — cần cho UXA-02, hữu ích cho UXA-20 | UXA-02, UXA-20 |

## P0 — Lỗi chức năng và khả dụng

| Finding | Thay đổi | File chính | Công sức | Phụ thuộc | Lợi ích | Rủi ro hồi quy / kiểm tra |
|---|---|---|---|---|---|---|
| UXA-01 | Theo D1(a): metric thanh trên và tổng Portal nói "urgent" cho việc không chặn; "blocking … before Continue" chỉ khi `PortalTasks.Blocker` có giá trị; tooltip theo đúng điều kiện | `GameShell.cs` (`PendingMetric`), `PortalScreen.cs` (`Decisions`) | S | D1 | Trạng thái hiển thị đúng luật | `LayoutVerification.cs:60` kiểm chuỗi "blocking" → cập nhật theo luật mới; `PortalVerification.cs:340`; Integration test không đổi |
| UXA-02 | Hiện exit cost và tổng chi phí trong xác nhận thuê HLV; thêm "wage" trước mức lương ngày | `Contracts.cs` (`StaffRow`), nơi dựng `StaffRow` trong Application, `ShellScreens.cs:642` | M | D6 | Cam kết có đủ chi phí | Đổi record → mọi nơi khởi tạo `StaffRow`; Integration tests; save schema **không** đổi (chỉ read model) |
| UXA-03 | Nhãn ngắn ở compact ("Place", "Payments, est.") hoặc cột nhãn ưu tiên; thêm check "nhãn dòng Portal không bị ellipsis" | `PortalScreen.Competition.cs:141`, `PortalScreen.Money.cs:48`, `ChromeVerification.cs` | S | — | Hết lỗi đọc ở màn chính 1280×720 | Check mới có thể lộ thêm nhãn bị cắt ở case khác — đó là mục đích |

## P1 — Vấn đề hệ thống

| Finding | Thay đổi | File chính | Công sức | Phụ thuộc | Lợi ích | Rủi ro hồi quy / kiểm tra |
|---|---|---|---|---|---|---|
| UXA-06 | `Card()` không hiển thị câu hỏi thiết kế; context header chỉ dùng trạng thái sống (hoặc để trống); câu hỏi giữ trong tooltip/review record. Phụ đề Finance/Staff đổi sang trạng thái | `ShellScreens.cs:53-58`, `:565`, `:609`; `SponsorWorkspaceView.cs:67` | S–M | — | Một sửa đổi làm sạch header của 7 màn | Check "header text clears the slant and stripe" và `FitOrHide` trong `ChromeVerification` vẫn phải đạt; ảnh trước/sau mọi section |
| UXA-07 | Rating và số đo dùng `TextPrimary`; thanh thuộc tính và Payments dùng tone trung tính; "NEW" trung tính hoặc brass nhạt; thêm `ColorRole.ActionSecondary` (cùng giá trị teal) cho `SecondaryAction`/`AccentOutline`; highlight dùng token riêng | `ShellScreens.cs:271`, `:288`, `:212`; `PortalScreen.Money.cs:43`; `PortalScreen.News.cs:40`; `UiTokens.cs`; `UiTheme.cs:121-124`; `UiMotion.cs:14` | S | — | Màu ngữ nghĩa có nghĩa trở lại | UiKit kiểm token/contrast; theme check "cut size and brass follow the hierarchy"; ảnh Squad, Portal |
| UXA-08 | Một ánh xạ: blocking → `StateWarning` ở mọi nơi; `StateCritical` chỉ cho Distress/lỗi | `PortalScreen.cs:133-134`, `:188`, `:208`, `:240` | S | D1 | Học được "vàng = chặn" | TopBarCheck ("amber only when a decision blocks"); case distress |
| UXA-09 | Nhãn "Back to Commercial · …" theo origin; breadcrumb từ origin của shell | `SponsorWorkspaceView.cs:56`, `:146`; `GameShell.cs:568` | S | D3 (phần "· Esc") | Định hướng đúng | `Verify-SponsorWorkspace.ps1`, `Verify-SponsorAcceptance.ps1` (có thể tìm nút theo chữ) |
| UXA-05 | Ẩn "Back · Esc" khi `Embedded`; hoặc sửa `genre-shell/README.md:23` nếu nút có vai trò trong map view | `OperatingMap.cs:103` | S | — | Một cơ chế điều hướng | `Verify-OperatingMap.ps1` (map độc lập vẫn cần Back) |
| UXA-04 | Theo D2(a): tên scope từ read model (tên đội, môn), bỏ hậu tố `· Kind`, crest thay monogram, bỏ nút "Company overview" trùng, tình huống viết thành câu thay vì enum nối " · " | `SponsorPresenter.cs:97-103`, `OperatingMap.cs:91-110`, `:151-154` | M | D2 | Section cuối cùng vào cùng hệ với shell | `Verify-OperatingMap.ps1`, `SponsorVerification.cs:70` (tìm nút "Map view"); lab `CompanyOperatingField` dùng chung fixture → kiểm riêng |

## P2 — Cải thiện theo màn

| Finding | Thay đổi | Công sức | Phụ thuộc | Rủi ro / kiểm tra |
|---|---|---|---|---|
| UXA-10 | Văn bản kết quả trận cho người chơi tách khỏi trace mô phỏng (`Simulation.cs:273`) | S | Domain | Integration test có thể so chuỗi tin; headless hash có thể đổi nếu chuỗi vào save → kiểm `Verify.ps1` |
| UXA-11 | Dùng `Currency.PerWeek` đã có (`Formatting.cs:48`) và một cách nói thời hạn; áp ở `Market.cs`, `Simulation.cs:141`, `PortalView.cs:250`, `PortalTasks.cs:56`, `Observations.cs:191`, `ShellScreens.cs:520`, `:557` | S–M | Domain, Application | Như trên; UiKit "no CU" check |
| UXA-12 | Một mốc thời gian cho khối Money "Next N days" | S | — | PortalVerification (Money) |
| UXA-13 | Dải giữa Portal co theo nội dung; empty state Transfer news gọn; ở 1080p dùng chỗ cho tin cũ | M | — | Check "Portal fits the first viewport", case empty/many |
| UXA-14 | Giới hạn chiều cao dải giữa ở compact; check fit cho case `long-names` | S–M | — | Check mới |
| UXA-15 | Notice khi rail thu gọn đặt ở vùng không chứa nội dung | S | — | RailCheck ("notices dock in the rail") |
| UXA-16 | "Slyvka · rating 74 · ready 84" | S | — | ShellVerification tìm item theo metadata, không theo chữ |
| UXA-17 | Theo D5(b): hành động Commit plan và Renew/Release luôn thấy ở 1280×720 | M | D5 | ChromeVerification "decision actions sit at the bottom right" |
| UXA-18 | Một chỗ cho điều khoản tài trợ (theo quyết định) | S | quyết định nhỏ | PortalVerification "sponsor offers show the brand's mark and name without terms" |
| UXA-19 | Lời từ chối giữ đến khi đóng hoặc vào lịch sử | S–M | — | Toast/RailCheck |
| UXA-20 | Kỹ năng ước tính của HLV hiện tại trong vùng Head coach | S | D6 | — |
| UXA-21 | Instrument load/capacity before → after (bị chặn, có text tương đương); cân nhắc giữ thanh trên | M | — | Sponsor acceptance; reading-slack check |

## P3 — Đánh bóng

| Finding | Thay đổi | Công sức |
|---|---|---|
| UXA-22 | Theo D3: bỏ "· Esc" khỏi nhãn, phím tắt vào tooltip | S |
| UXA-23 | Theo D4: sửa câu trong polish standard §8 hoặc bố cục lead | S |
| UXA-24 | Nút hành động cục bộ theo bề rộng nhãn, căn phải | S |
| UXA-25 | Thay ■/▲ bằng chữ hoặc marker có định nghĩa | S |
| UXA-26 | Gom khoảng cách literal vào bảng có tên trong `ShellLayout`, ánh xạ về thang; không đổi pixel ở bước đầu | M |
| UXA-27 | Check tương phản chữ trên wash màu đội với mọi màu crest | S |
| UXA-28 | Cắt tên tài trợ ở giữa hoặc giữ phần phân biệt | S |
| UXA-29 | Tooltip/dòng "No saved campaign yet" | S |

## Đợt thực hiện đề xuất

| Đợt | Nội dung | Finding | Vì sao gộp |
|---|---|---|---|
| A — Từ ngữ và nhãn (chỉ UI) | Sau D1, D3 | 01, 03, 05, 09, 16, 22, 29 | Rủi ro thấp, sửa chuỗi và điều kiện hiển thị |
| B — Ngữ nghĩa màu | | 07, 08, 25 | Cùng một ánh xạ trạng thái → màu/marker |
| C — Header và phụ đề | | 06 | Một điểm sửa trong `Card()`; ảnh chụp mọi section |
| D — Read model | Sau D6 | 02, 20 | Cùng thay đổi `StaffRow` |
| E — Văn bản sinh từ Domain | | 10, 11, 18 | Cùng formatter, cùng test |
| F — Độ bền bố cục | Sau D5 | 12, 13, 14, 15, 17, 28 | Cùng nhóm check fit/viewport |
| G — Company | Sau D2 | 04 | Phạm vi riêng, có thể cần blueprint |
| H — Vệ sinh hệ thống | | 24, 26, 27 | Không đổi hình thức đáng kể |

Thứ tự khuyến nghị: A → B → C → D → E → F → G → H. A–C đóng phần lớn vấn đề nhìn thấy với công sức nhỏ.

## Lợi ích mong đợi

- Trạng thái và cam kết trung thực (UXA-01, 02) — phù hợp gate 14 tiêu chí mục 4 và 14.
- Màn chính đọc được ở kích thước stress (UXA-03, 14, 15).
- Bớt văn bản nội bộ — trả lời "không" cho identity question 8 (editor/debug) ở mọi section sau đợt C và G.
- Ngữ nghĩa màu và định dạng số nhất quán giữa các màn (UXA-07, 08, 11).

## Rủi ro chung

- Các check tự động tìm điều khiển theo chuỗi hiển thị (ví dụ `LayoutVerification.cs:60`, `SponsorVerification.cs:70`)
  sẽ cần cập nhật cùng lúc với chuỗi — cập nhật theo luật mới, không nới lỏng check.
- Thay đổi văn bản sinh trong Domain có thể đổi hash headless nếu chuỗi đi vào trạng thái lưu; chạy
  `build/Verify.ps1 -Configuration Debug -Evidence artifacts/<thư-mục>`.
- Working tree hiện có thay đổi Domain chưa commit (rating/generator); tách commit của đợt UI khỏi các thay đổi đó.
