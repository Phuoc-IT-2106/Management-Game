# Kiểm toán UI/UX — 2026-10-22

Trạng thái: **ENGINEERING / UX AUDIT — không phải duyệt thị giác của Director.** Không màn nào được chấp nhận hay bị
từ chối bởi tài liệu này; nó cung cấp bằng chứng cho review. Không có thay đổi mã UI nào trong đợt audit.

Khung đánh giá: [rulebook UI_AI_ANTIPATTERNS.md](UI_AI_ANTIPATTERNS.md) (rule ID), áp dụng qua skill
[`ui-authenticity`](../../../.claude/skills/ui-authenticity/SKILL.md). Kế hoạch khắc phục: [UI_REMEDIATION_PLAN.md](UI_REMEDIATION_PLAN.md).
Số liệu kiểm tra nguyên văn (Verify-Shell, tương phản, kiểm tra tĩnh, danh mục ảnh, validator):
[UI_UX_VERIFICATION_REPORT_2026-10-22.md](UI_UX_VERIFICATION_REPORT_2026-10-22.md).

## 1. Phạm vi, phương pháp, giới hạn

| Mục | Giá trị |
|---|---|
| Commit | `587b910` trên nhánh `stage5-sponsor-instruments`; working tree có thay đổi chưa commit ở Domain/Application/tools (rating, generator) nhưng **không** có thay đổi trong `game/Client` |
| Build | Debug, SDK .NET 10.0.401 và Godot 4.7.2 theo pin của `build/Environment.ps1` |
| Ảnh chụp | `build/Capture-Shell.ps1 -Results 1 -Cases -Output artifacts/ui-audit-2026-10-22` (chạy ngoài sandbox), 70 ảnh ở 1280×720 và 1920×1080: start, Portal + 21 case, 7 section, League, under-contract, offer workspace, xác nhận, rail, sheet trạng thái |
| Ngữ cảnh chụp | Seed 20261004, công ty "Hanoi Signal", đã chơi 1 trận (ngày 6/76, mùa 1); chế độ chụp bật reduced motion (`GameShell.cs:63`) |
| Kiểm tra tự động | `build/Verify-Shell.ps1 -NoBuild`: **3.861/3.861** check đạt ở 1280×720, **3.901/3.901** ở 1920×1080 |
| Đo đạc | Tương phản WCAG của mọi cặp token chữ × bề mặt (script Python trên giá trị `UiTokens.Colors`) |
| Đọc mã | `game/Client/UI/**` (≈7.800 dòng), các read model liên quan trong `src/ManagementGame.Application`, chuỗi hiển thị sinh ở `src/ManagementGame.Domain` |
| Đối chiếu | Governance Stage 5, genre-shell amendment, thesis, UI polish standard, 11 mockup đã duyệt |

Ảnh được trích làm bằng chứng được sao vào [`audit-evidence-2026-10-22/`](audit-evidence-2026-10-22/); bộ đầy đủ nằm ở
`artifacts/ui-audit-2026-10-22/` (git-ignored, tái tạo bằng lệnh trên).

Giới hạn (chi tiết ở §9): chưa thử điều hướng bàn phím và thứ tự focus; chưa xem chuyển động (chụp ở reduced motion);
chỉ một thời điểm đầu mùa; các màn lab/prototype và console nội bộ không được render; không có người dùng thật tham gia.

## 2. Inventory bề mặt UI

Điểm vào: `project.godot` → `res://UI/Shell/GameShell.tscn`. Chuyển section bằng `GameShell.ShowSection` (`GameShell.cs:520`);
overlay bằng `Confirm`/`OpenOffer`; console nội bộ qua `ChangeSceneToFile("res://Main.tscn")` chỉ trong bản debug.
Các scene khác được mở bằng script `build/Run-*.ps1` hoặc verify.

| Bề mặt | Loại | Mã | Mockup | Đã render |
|---|---|---|---|---|
| Màn bắt đầu (New campaign) | Player-facing | `GameShell.cs:78-109` | — | Có |
| Thanh trên (S1) | Player-facing | `GameShell.cs:300-441` | 11 | Có |
| Rail điều hướng (S2), mở/thu gọn, notice dock | Player-facing | `GameShell.cs:188-299` | 11 | Có |
| Portal (S4) + 21 case dữ liệu | Player-facing | `PortalScreen*.cs` | 11 (thay 01) | Có |
| Inbox | Player-facing | `ShellScreens.cs:194-225` | 02 (news) | Có |
| Squad (cầu thủ, free agents, under contract, hồ sơ) | Player-facing | `ShellScreens.cs:228-344` | 03 | Có |
| Competition — Match view | Player-facing | `ShellScreens.cs:361-431` | 04 | Có |
| Competition — League view | Player-facing | `ShellScreens.cs:438-503` | 05 | Có |
| Commercial | Player-facing | `ShellScreens.cs:508-560` | 07 | Có |
| Finance | Player-facing | `ShellScreens.cs:563-604` | 08 | Có |
| Staff (+ under contract) | Player-facing | `ShellScreens.cs:607-647` | 09 | Có |
| Company (operating map nhúng) | Player-facing | `ShellScreens.cs:650-656` → `SponsorCompanyHost.cs`, `OperatingMap.cs` | 10 | Có (chế độ list; chế độ map chưa) |
| Offer workspace (review đề nghị tài trợ) | Overlay | `GameShell.cs:561-574`, `SponsorWorkspaceView.cs` | — | Có (pha review; pha xác nhận chưa) |
| Hộp xác nhận cam kết | Overlay | `GameShell.cs:536-558` | — | Có (Hire head coach) |
| Toast/notice | Overlay | `GameShell.cs:462-472` | — | Có |
| Sheet trạng thái bề mặt "DEV surface states" | Dev tool (chỉ harness chụp) | harness của shell | — | Có |
| Console nội bộ (`Main.tscn`, TabContainer) | Dev tool / legacy, chỉ bản debug | `Main.cs` | — | Không |
| UI Lab | Prototype/lab | `UI/Lab/UiLab.cs` | — | Không |
| Company Operating Field (COS-05) | Prototype/lab | `UI/Lab/CompanyOperatingField.cs` | — | Không |
| Operating Map độc lập | Prototype | `UI/OperatingMap/OperatingMap.cs` qua `Run-OperatingMap.ps1` | — | Không (chỉ bản nhúng) |
| Sponsor verification host | Test host | `SponsorVerification.cs` | — | Không |
| Section Training, League riêng, Calendar, News riêng | **Không tồn tại** (chỉ có trong mockup) | — | 02, 05, 06, 11 | — |

Thành phần dùng chung: `Tokens/UiTokens.cs`, `Tokens/FrameGeometry.cs`, `Theme/Surfaces.cs`, `Theme/UiTheme.cs`,
`Components/Framing.cs`, `Components/Primitives.cs`, `Components/CardButton.cs`, `Components/PortalGraphics.cs`
(RingGauge, SegmentBar, Backdrop, JourneyArrow), `Components/UiIcon.cs`, `Components/UiMotion.cs`,
`Identity/IdentityAssets.cs`, `Identity/SemanticArt.cs`. Không có biểu đồ xu hướng (S7) trong slice.

## 3. Kết quả kiểm tra tự động và đo đạc

- **Verify-Shell đạt toàn bộ** (3.861 và 3.901 check): hình học chamfer, header plate, vùng bấm ≥24 px, không bo góc,
  nhãn hành động không xuống dòng, không chữ < 12 px, thanh trên, rail, bảng. Kiểm tra tự động **không** bắt được các
  lỗi ở §4 (ví dụ UXA-03: nhãn bị cắt bằng ellipsis được coi là "bounded text" hợp lệ).
- **Tương phản token** (WCAG, nền đặc): mọi vai trò chữ ≥ 4,5:1 trên mọi bề mặt — thấp nhất `TextMuted` trên
  `HoverSurface` 4,94:1, `StateCritical` trên `HoverSurface` 4,63:1; chữ trên nút brass 7,8:1. `AccentMuted` (2,81:1 trên
  hover) chỉ dùng cho điểm nhấn cấu trúc, không cho chữ. Chưa đo chữ trên nền wash màu đội (UXA-27).
- **Bo góc:** `UiTokens.CornerRadius = 0`; không có `StyleBoxFlat` inline trong màn hình (grep).
- **Mẫu mockup trong mã:** không tìm thấy chuỗi mẫu ("Raven Arena", "Nexus", "+3,200", "PLAYER 01") trong `game/Client/UI`
  và `src`.

## 4. Findings

### 4.1. Bảng tổng hợp

| ID | Màn | Rule ID | Vấn đề | Severity | Confidence | Trạng thái | Ưu tiên |
|---|---|---|---|---|---|---|---|
| UXA-01 | Thanh trên, Portal | CNT-011, CNT-009 | Quyết định "blocking" không chặn Continue, nhưng giao diện nói phải xử lý trước | Major | High | Confirmed | P0 |
| UXA-02 | Staff → xác nhận | CNT-010 | Xác nhận thuê HLV không nêu số tiền đền bù HLV cũ | Major | High | Confirmed | P0 |
| UXA-03 | Portal | TYP-006 | Nhãn bị cắt giữa từ ở 1280×720 | Moderate | High | Confirmed | P0 |
| UXA-04 | Company | IDN-003, CNT-001, CNT-007, IDN-001 | Section Company là cây admin Stage 3 với nhãn fixture và hậu tố kiểu | Major | High | Confirmed | P1 |
| UXA-05 | Company | CMP-005, CNT-009 | Nút "Back · Esc" thứ hai trong section nhúng, trái mô tả đã ghi | Moderate | High | Confirmed | P1 |
| UXA-06 | Inbox, Squad, Competition, Commercial, Finance, Staff, Offer | CNT-007 | Câu hỏi thiết kế và lời giải thích luật hiển thị làm phụ đề | Moderate | High | Confirmed | P1 |
| UXA-07 | Squad, Portal, theme | COL-007 | Màu trạng thái gắn cứng, không phụ thuộc giá trị | Moderate | High | Confirmed | P1 |
| UXA-08 | Thanh trên, Portal | COL-007, CMP-005 | Cùng trạng thái "blocking": vàng ở thanh trên, đỏ ở Portal | Moderate | High | Confirmed | P1 |
| UXA-09 | Offer workspace | CNT-009, CMP-005 | "Return to company" đưa về Commercial; breadcrumb gọi tên section không có | Moderate | High | Confirmed | P1 |
| UXA-10 | Inbox, Portal | CNT-007 | Văn bản kiểu log mô phỏng trong tin kết quả trận | Moderate | High | Confirmed | P2 |
| UXA-11 | Inbox, Portal, Commercial, Finance | CNT-008 | Một mức giá, bốn cách viết; không theo chuẩn "$300/week" | Moderate | High | Confirmed | P2 |
| UXA-12 | Portal | CNT-005 | Khối "Next 14 days" chứa con số 7 ngày | Moderate | High | Confirmed | P2 |
| UXA-13 | Portal | LAY-005 | Vùng tin giữa trống lớn ở 1920×1080 khi ít tin | Moderate | High | Confirmed | P2 |
| UXA-14 | Portal | LAY-010 | Case tên dài: Portal vượt màn đầu ở 1280×720 | Moderate | High | Confirmed | P2 |
| UXA-15 | Shell (rail thu gọn) | LAY-010 | Notice nổi che nội dung Money snapshot | Moderate | High | Confirmed | P2 |
| UXA-16 | Competition | CNT-004 | Lựa chọn đội hình "74/84" không nhãn | Minor | High | Confirmed | P2 |
| UXA-17 | Competition, Squad | LAY-010, CNT-005 | Hành động chính của section nằm dưới nếp gấp ở 1280×720 | Moderate | Medium | Potential | P2 |
| UXA-18 | Portal | CNT-005 | World news hiện điều khoản tài trợ mà thẻ quyết định cố ý ẩn | Minor | Medium | Potential | P2 |
| UXA-19 | Shell | CNT-006 | Notice từ chối tự ẩn sau 5–8 s, không có lịch sử | Moderate | Medium | Potential | P2 |
| UXA-20 | Staff | CNT-004 | Không thấy kỹ năng HLV hiện tại để so với ứng viên | Moderate | Medium | Potential | P2 |
| UXA-21 | Offer workspace | LAY-006 | Hệ quả trình bày chủ yếu bằng đoạn văn; shell bị che | Moderate | Medium | Potential | P2 |
| UXA-22 | Confirm, Offer, Company | CNT-007 | Legend phím "· Esc" trong nhãn nút — mâu thuẫn giữa tài liệu | Minor | High | Potential | P3 |
| UXA-23 | Portal | LAY-006 | Tin mới nhất của World news được phóng to — mâu thuẫn giữa tài liệu | Minor | Medium | Potential | P3 |
| UXA-24 | Portal, Commercial, Staff | CMP-005 | Cùng hành động "Review offer" hai cách trình bày | Minor | High | Confirmed | P3 |
| UXA-25 | Competition, Squad | ICO-006 | Marker ■ (critical) cho trận thua, ▲ (warning) cho "có thể gia hạn" | Minor | High | Confirmed | P3 |
| UXA-26 | Toàn shell (mã) | LAY-009 | ≈20 giá trị khoảng cách tuỳ ý ngoài thang `SpaceRole` | Minor | High (mã) | Confirmed | P3 |
| UXA-27 | Portal | COL-009 | Chữ trên wash màu đội chưa được kiểm tương phản | Minor | Low | Potential | P3 |
| UXA-28 | Portal (case tên dài) | TYP-006 | Hai thẻ tài trợ bị cắt thành cùng một chuỗi | Minor | High | Confirmed | P3 |
| UXA-29 | Màn bắt đầu | CNT-006 | "Continue saved campaign" disabled không nói lý do | Minor | High | Confirmed | P3 |

**Tổng:** 21 Confirmed, 8 Potential (29 finding). Severity: 3 Major, 16 Moderate, 10 Minor; không có Critical.

### 4.2. Chi tiết

#### UXA-01 — Quyết định "blocking" không chặn Continue
- **Màn:** thanh trên (metric Decisions), Portal Key decisions, case distress.
- **Rule:** CNT-011, CNT-009.
- **Mô tả:** Ba loại việc được đánh `TaskUrgency.Blocking` — khủng hoảng tài chính, thiếu vai trò, roster dưới mức tối
  thiểu (`PortalTasks.cs:40-48`). Thanh trên hiện "▲ 1 blocking" và tooltip "1 must be settled before Continue"
  (`GameShell.cs:414`, `:423`). Nhưng `Continue()` chỉ dừng với blocker của Competition (`GameShell.cs:451` →
  `PortalTasks.Blocker`, `PortalTasks.cs:73`), và `Advance` trong Domain không kiểm các điều kiện này
  (`Simulation.cs:116-117`). Việc không chặn là **chủ ý**: test tích hợp ghi "A roster that cannot field all roles
  forfeits rather than blocking time" và "a roster below the league minimum is a blocking Squad decision that does not
  stop time" (`tests/Integration/Program.cs:277`, `:550-551`). Kết quả: giao diện nói thời gian bị chặn, trong khi
  Continue vẫn tiến thời gian — từ ngữ của UI sai so với luật.
- **Bằng chứng:** mã ở trên; [portal-case-distress-1280x720.png](audit-evidence-2026-10-22/portal-case-distress-1280x720.png)
  (nút vẫn là "Continue", metric "▲ 1 blocking"). Hành vi bấm Continue chưa được thử trong UI; kết luận dựa trên đường mã.
- **Tác động:** độ trung thực của trạng thái; người chơi có thể chờ vô ích hoặc mất niềm tin khi thấy "blocking" không chặn.
- **Khuyến nghị:** (a) giữ luật, đổi từ ngữ UI: "urgent" cho việc không chặn, "blocking … before Continue" chỉ cho trận
  chưa có kế hoạch hôm nay — **khuyến nghị**, khớp với luật mà test đã khoá, chỉ sửa UI; hoặc (b) đổi luật để các việc
  đó chặn Continue thật — quyết định gameplay của người dùng, thay đổi Application và test.
- **Ưu tiên:** P0.

#### UXA-02 — Xác nhận thuê HLV thiếu số tiền đền bù
- **Màn:** Staff → overlay "Hire head coach".
- **Rule:** CNT-010.
- **Mô tả:** Văn bản xác nhận: "Pay $174.30 plus Hoang Khang's exit cost now; $11.62/day." (`ShellScreens.cs:642`).
  Số tiền đền bù không được nêu vì read model `StaffRow` không có trường đó (`Contracts.cs:48`), trong khi xác nhận
  release cầu thủ có nêu `ExitCost` (`ShellScreens.cs:316`). Đuôi "; $11.62/day" thiếu danh từ ("wage").
- **Bằng chứng:** [confirm-1280x720.png](audit-evidence-2026-10-22/confirm-1280x720.png).
- **Tác động:** cam kết tài chính không đảo ngược được mà người chơi không biết tổng chi phí.
- **Khuyến nghị:** thêm exit cost của HLV hiện tại vào read model và hiện tổng: "Pay $174.30 fee + $X exit cost = $Y now;
  wage $11.62/day." Phụ thuộc thay đổi Application (ngoài phạm vi UI thuần).
- **Ưu tiên:** P0.

#### UXA-03 — Nhãn bị cắt giữa từ ở 1280×720
- **Màn:** Portal — Season record ("League p…"), Money snapshot ("Payments, estim…"); lặp lại ở case distress, long names.
- **Rule:** TYP-006.
- **Mô tả:** Nhãn dòng được `Clamp(..., 1)` trong cột co giãn (`PortalScreen.Competition.cs:141`,
  `PortalScreen.Money.cs:48`); khi rail mở ở 1280×720 cột nhãn bị ép. Khi rail thu gọn, cùng nhãn hiện đầy đủ
  ([rail-collapsed-1280x720.png](audit-evidence-2026-10-22/rail-collapsed-1280x720.png)). Chuẩn: "optional text is omitted
  whole" và "Trimming optional words mid-word" là anti-pattern đã bị loại (`UI_POLISH_STANDARD.md` §7, §13).
- **Bằng chứng:** [portal-1280x720.png](audit-evidence-2026-10-22/portal-1280x720.png).
- **Tác động:** đọc được; trông lỗi ngay ở màn hình chính, kích thước stress chính thức.
- **Khuyến nghị:** nhãn ngắn ở compact ("Place", "Payments"); hoặc ưu tiên chiều rộng cột nhãn trước giá trị; thêm một
  check tự động "nhãn dòng không bị ellipsis" cho Portal.
- **Ưu tiên:** P0 (sửa nhỏ).

#### UXA-04 — Section Company dùng ngôn ngữ admin của prototype cũ
- **Màn:** Company.
- **Rule:** IDN-003, CNT-001, CNT-007, IDN-001.
- **Mô tả:** Section nhúng `OperatingMap` (Stage 3) qua `SponsorCompanyHost` (`ShellScreens.cs:650-656`). Màn hiện:
  monogram "HS" thay cho crest dùng ở mọi nơi khác; cây "Ownership / Company functions" với hậu tố kiểu dữ liệu
  ("Hanoi Signal · Company", "Competitive Portfolio · Portfolio", "Esports · Division", "Development Discipline ·
  Discipline", "Primary Team · Team", "Business & Finance · Function") — hậu tố từ `scope.Kind.ToString()`
  (`OperatingMap.cs:151-154`); tên scope là fixture cứng ("Development Discipline", "Primary Team", "Competitive
  Portfolio") trong `SponsorPresenter.cs:97-103`, không phải tên đội của người chơi; tình huống là enum nối bằng " · "
  ("Kestrel Peripherals · High · Available"); hai nút "Company overview" và nhãn "Company overview" trùng nhau.
  Đây đúng là dạng "editor/debug/database tooling" mà governance yêu cầu sửa (identity question 8, review mục H/J).
- **Bằng chứng:** [company-1280x720.png](audit-evidence-2026-10-22/company-1280x720.png); mockup `10_company_mockup.webp`
  (bố cục HQ — bản thân chứa nhiều tính năng ảo giác, xem §10).
- **Tác động:** bản sắc, nhất quán; section duy nhất trông như công cụ nội bộ trong shell đã polish.
- **Khuyến nghị:** cần quyết định của Director (§8, T4). Phương án nhỏ nhất: dùng tên thật từ read model, bỏ hậu tố kiểu,
  dùng crest, bỏ nút trùng, đặt nội dung vào region/table của shell. Phương án lớn: blueprint mới theo mockup 10 nhưng
  bỏ phần ảo giác. Phương án tạm: ẩn section khỏi rail đến khi có blueprint.
- **Ưu tiên:** P1.

#### UXA-05 — Nút Back thứ hai trong Company nhúng
- **Màn:** Company.
- **Rule:** CMP-005, CNT-009.
- **Mô tả:** README của genre shell ghi Company nhúng "no second watermark or back control"
  (`genre-shell/README.md:23`), nhưng `OperatingMap` luôn thêm "Back · Esc" (`OperatingMap.cs:103`); cờ `Embedded` chỉ
  ẩn watermark (`OperatingMap.cs:124`).
- **Bằng chứng:** [company-1280x720.png](audit-evidence-2026-10-22/company-1280x720.png).
- **Tác động:** hai cơ chế điều hướng song song; tài liệu sai so với mã.
- **Khuyến nghị:** ẩn nút khi `Embedded` (hoặc sửa tài liệu nếu nút có chức năng riêng trong map view).
- **Ưu tiên:** P1 (sửa nhỏ).

#### UXA-06 — Câu hỏi thiết kế và lời giải thích luật làm phụ đề
- **Màn:** Inbox, Squad, Competition (Match, League), Commercial, Finance, Staff, Offer workspace.
- **Rule:** CNT-007.
- **Mô tả:** `Card()` truyền "question" của vùng làm context hiển thị trong header (`ShellScreens.cs:53-58`): "What
  happened, and who does it affect?", "Who plays, how good are they, what do they cost?", "Where should this week's work
  go?", "When does money arrive and leave?", "Is the scheduled income worth the ongoing delivery obligation?"
  (`SponsorWorkspaceView.cs:67`)… Câu hỏi là công cụ thiết kế của governance ("every visible region must answer…"),
  không phải nội dung cho người chơi. Hiển thị không nhất quán: hiện ở vùng đủ chỗ, ẩn ở vùng khác (`FitOrHide`).
  Phụ đề trang Finance và Staff là lời giải thích luật tĩnh ("Each day receipts settle first…", "The head coach drives
  preparation throughput and load capacity." — `ShellScreens.cs:565`, `:609`) trong khi các section khác dùng trạng thái
  thật ("6 players · every role is covered").
- **Bằng chứng:** [squad-1280x720.png](audit-evidence-2026-10-22/squad-1280x720.png),
  [inbox-1280x720.png](audit-evidence-2026-10-22/inbox-1280x720.png),
  [competition-1280x720.png](audit-evidence-2026-10-22/competition-1280x720.png),
  [commercial-1280x720.png](audit-evidence-2026-10-22/commercial-1280x720.png),
  [finance-1280x720.png](audit-evidence-2026-10-22/finance-1280x720.png),
  [staff-1280x720.png](audit-evidence-2026-10-22/staff-1280x720.png).
- **Tác động:** nhiễu trong header; đọc như spec; mâu thuẫn tinh thần "no generic descriptive subtitles" của chuẩn
  (§7, đang chỉ áp cho Portal) và thesis P5.
- **Khuyến nghị:** một thay đổi ở `Card()`: câu hỏi chỉ ở tooltip/review record; header context chỉ dùng trạng thái sống
  (ví dụ "1 renewal due", "2 offers · answer by day 7"). Phụ đề Finance/Staff đổi sang trạng thái.
- **Ưu tiên:** P1 (một điểm sửa, nhiều màn).

#### UXA-07 — Màu trạng thái gắn cứng, không phụ thuộc giá trị
- **Màn:** Squad (hồ sơ), Portal (Money, World news), theme.
- **Rule:** COL-007.
- **Mô tả:**
  - Rating hồ sơ luôn `StatePositive` bất kể giá trị (`ShellScreens.cs:288`) — sao chép màu xanh của mockup 03 (nơi mọi
    giá trị mẫu đều cao) thay vì ý nghĩa của nó (MCK-007).
  - Thanh thuộc tính < 55 dùng `StateWarning` (`ShellScreens.cs:271`): kỹ năng thấp không phải cảnh báo.
  - "Payments, estimated" luôn `StateWarning` (`PortalScreen.Money.cs:43`): chi phí bình thường mang màu cảnh báo.
  - Nhãn "NEW" dùng `StatePositive` (`PortalScreen.News.cs:40`, `ShellScreens.cs:212`).
  - Biến thể `SecondaryAction`/`AccentOutline` lấy màu từ token `StatePositive` (`UiTheme.cs:121`, `:124`); highlight
    thay đổi lấy `InformationEstimated` (`UiMotion.cs:14`) — token ngữ nghĩa bị dùng làm màu hành động/chuyển động.
- **Bằng chứng:** [squad-1920x1080.png](audit-evidence-2026-10-22/squad-1920x1080.png),
  [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Tác động:** loãng ngữ nghĩa màu (brief §8); cảnh báo thật kém nổi bật.
- **Khuyến nghị:** rating và số đo dùng chữ chính; thanh dùng steel trung tính (ngưỡng nếu cần thì bằng nhãn); Payments
  trung tính; thêm token `ActionSecondary` (có thể cùng giá trị teal) để tách vai trò.
- **Ưu tiên:** P1.

#### UXA-08 — Cùng trạng thái "blocking" hai màu
- **Màn:** thanh trên, Portal Key decisions.
- **Rule:** COL-007, CMP-005.
- **Mô tả:** Thanh trên: "▲ 1 blocking" màu `StateWarning` (`GameShell.cs:414`) — chuẩn §6 "amber only when a decision
  blocks Continue, and red never". Portal: tổng "▲ 4 pending decisions" màu `StateCritical` (`PortalScreen.cs:133-134`),
  danh mục thẻ màu warning (`:208`), hạn màu critical (`:188`), verb viền critical (`:240`). Marker ▲ (warning) được tô
  màu critical.
- **Bằng chứng:** [portal-case-distress-1280x720.png](audit-evidence-2026-10-22/portal-case-distress-1280x720.png).
- **Tác động:** người chơi không học được "vàng = chặn".
- **Khuyến nghị:** một ánh xạ: blocking → warning ở mọi nơi; critical chỉ cho tình trạng tài chính Distress/lỗi. Làm
  cùng UXA-01.
- **Ưu tiên:** P1.

#### UXA-09 — Nhãn quay lại và breadcrumb của offer workspace không khớp shell
- **Màn:** Offer workspace.
- **Rule:** CNT-009, CMP-005.
- **Mô tả:** Nút "Return to company · Esc" (`SponsorWorkspaceView.cs:146`) thực tế đưa về section Commercial
  (`GameShell.cs:571`). Breadcrumb "Business & Finance / Sponsorship / Commitment review" (`SponsorWorkspaceView.cs:56`)
  dùng taxonomy của operating map, không có trong rail.
- **Bằng chứng:** [offer-1280x720.png](audit-evidence-2026-10-22/offer-1280x720.png).
- **Tác động:** định hướng (người chơi không biết sẽ quay về đâu).
- **Khuyến nghị:** "Back to Commercial"; breadcrumb "Commercial / Kestrel Peripherals / Review" lấy từ origin.
- **Ưu tiên:** P1 (sửa nhỏ).

#### UXA-10 — Văn bản kiểu log trong tin kết quả trận
- **Màn:** Inbox, Portal World news.
- **Rule:** CNT-007.
- **Mô tả:** "Defeat 0–2 vs Blue Heron Squad. Balanced posture; completed preparation contributed 0; public meta suited
  the plan. Rival response and match-day variance remain uncertain. Any prize/bonus and commercial changes are due
  tomorrow." sinh trong Domain (`Simulation.cs:273`). "contributed 0", "prize/bonus" đọc như trace của engine.
- **Bằng chứng:** [inbox-1280x720.png](audit-evidence-2026-10-22/inbox-1280x720.png),
  [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Khuyến nghị:** tách văn bản cho người chơi khỏi trace (ví dụ: "No completed preparation counted. The public meta
  suited our plan. Prize money settles tomorrow."). Phụ thuộc Domain/Application; kiểm test tích hợp nếu có so chuỗi.
- **Ưu tiên:** P2.

#### UXA-11 — Một mức giá, bốn cách viết
- **Màn:** Inbox, Portal, Commercial, Finance.
- **Rule:** CNT-008.
- **Mô tả:** "$370 a week for 12 weeks" (`PortalView.cs:250`, `PortalTasks.cs:56`), "$370 per week for 84 days"
  (`Market.cs:74`, `:105-106`, `Simulation.cs:141`), "$370 weekly for 84 days" (`ShellScreens.cs:520`, `:557`),
  "$300 weekly" (`Observations.cs:191`). Chuẩn: "Rates are `$9.80/day` and `$300/week`" (`UI_POLISH_STANDARD.md` §7).
  Formatter `Currency.PerWeek` đã có sẵn (`src/ManagementGame.Domain/Formatting.cs:48`) nhưng không chuỗi nào ở trên dùng nó.
- **Bằng chứng:** [inbox-1280x720.png](audit-evidence-2026-10-22/inbox-1280x720.png),
  [commercial-1280x720.png](audit-evidence-2026-10-22/commercial-1280x720.png),
  [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Khuyến nghị:** dùng `Currency.PerWeek` có sẵn ở mọi nơi và chọn một cách nói thời hạn (ngày hoặc tuần).
- **Ưu tiên:** P2.

#### UXA-12 — Khối "Next 14 days" chứa con số 7 ngày
- **Màn:** Portal Money snapshot (1920×1080).
- **Rule:** CNT-005.
- **Mô tả:** Tiêu đề khối "NEXT 14 DAYS" (`PortalScreen.Money.cs:38`) chứa "Receipts/Payments" 14 ngày và "7-day
  estimate" (`:57`) — hai mốc trong một nhóm.
- **Bằng chứng:** [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Khuyến nghị:** một mốc cho cả khối, hoặc tách estimate ra khỏi khối "next 14 days" và ghi mốc ngay trên giá trị.
- **Ưu tiên:** P2.

#### UXA-13 — Vùng tin giữa trống ở 1920×1080
- **Màn:** Portal.
- **Rule:** LAY-005.
- **Mô tả:** Dải giữa luôn `ExpandFill` với tỷ lệ cố định 57/43 (`PortalScreen.cs:35-38`, `ShellLayout.cs:18`). Đầu mùa
  (3 tin, 0 chuyển nhượng) vùng Transfer news gần như trống toàn bộ và World news có dải trống dưới cùng.
- **Bằng chứng:** [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Tác động:** brief §47 ("1080p must not become empty"); phụ thuộc dữ liệu — mùa sau có thể đầy.
- **Khuyến nghị:** empty state gọn và nhường chiều cao; hoặc ở 1080p hiện thêm tin cũ/lịch sử thay vì khoảng trống.
- **Ưu tiên:** P2.

#### UXA-14 — Case tên dài: Portal vượt màn đầu ở 1280×720
- **Màn:** Portal (case `long-names`).
- **Rule:** LAY-010.
- **Mô tả:** Tiêu đề chuyển nhượng hai dòng + tên dài đẩy dải dưới (Next match, Season record, Money) ra ngoài màn đầu;
  nút "Prepare match" không thấy. Chuẩn §9: "The Portal fits the first viewport at both sizes."
- **Bằng chứng:** [portal-case-long-names-1280x720.png](audit-evidence-2026-10-22/portal-case-long-names-1280x720.png).
- **Tác động:** case stress chính thức (brief §48); tên thật hiếm khi dài như fixture.
- **Khuyến nghị:** giới hạn chiều cao dải giữa theo màn; ở compact giảm số tin khi tiêu đề hai dòng; thêm check fit
  cho case này trong `ChromeVerification`.
- **Ưu tiên:** P2.

#### UXA-15 — Notice nổi che nội dung khi rail thu gọn
- **Màn:** shell, rail thu gọn.
- **Rule:** LAY-010.
- **Mô tả:** Khi rail thu gọn, toast nổi góc dưới phải (`GameShell.cs:241-243`) đè lên dòng "7-day estimate" của Money
  snapshot. Chuẩn §6: notices "never cover content".
- **Bằng chứng:** [rail-collapsed-1280x720.png](audit-evidence-2026-10-22/rail-collapsed-1280x720.png).
- **Khuyến nghị:** đặt notice vào lề không chứa nội dung (ví dụ dưới thanh trên) hoặc giữ một dock nhỏ trên rail thu gọn.
- **Ưu tiên:** P2.

#### UXA-16 — Lựa chọn đội hình "74/84" không nhãn
- **Màn:** Competition — Preparation plan.
- **Rule:** CNT-004.
- **Mô tả:** Mục chọn "Slyvka · 74/84", "(secondary)" (`ShellScreens.cs:382-383`): không nói số nào là rating, số nào
  là readiness.
- **Bằng chứng:** [competition-1280x720.png](audit-evidence-2026-10-22/competition-1280x720.png).
- **Khuyến nghị:** "Slyvka · rating 74 · ready 84".
- **Ưu tiên:** P2.

#### UXA-17 — Hành động chính của section dưới nếp gấp ở 1280×720 (Potential)
- **Màn:** Competition (Commit plan), Squad (Renew/Release trong hồ sơ).
- **Rule:** LAY-010, CNT-005.
- **Mô tả:** "Commit plan" nằm cuối vùng Preparation plan, dưới các trường; ở 1280×720 phải cuộn trang. Trong hồ sơ
  Squad, nút Release nằm dưới thuộc tính và trạng thái.
- **Bằng chứng:** [competition-1280x720.png](audit-evidence-2026-10-22/competition-1280x720.png),
  [squad-1280x720.png](audit-evidence-2026-10-22/squad-1280x720.png).
- **Vì sao Potential:** chuẩn §9 cho phép section cuộn dọc, nhưng brief §46 và amendment yêu cầu 1280×720 giữ "the
  primary decision" — cần Director diễn giải (§8, T3).
- **Khuyến nghị:** nếu áp §46: đặt hành động cạnh tiêu đề vùng hoặc dải hành động cố định cuối vùng.
- **Ưu tiên:** P2.

#### UXA-18 — Điều khoản tài trợ trong World news (Potential)
- **Màn:** Portal.
- **Rule:** CNT-005.
- **Mô tả:** Thẻ quyết định "Sponsor offer" cố ý chỉ hiện tên và mark ("Commercial terms belong to the offer review",
  chuẩn §6), nhưng tin bên cạnh ghi "$370 a week for 12 weeks, +16 delivery load" (`PortalView.cs:250`). Chuẩn §8 "News is
  not a market… no fees" được viết cho tin chuyển nhượng; áp cho tin thương mại chưa rõ.
- **Bằng chứng:** [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Khuyến nghị:** thống nhất một chỗ cho điều khoản (offer review) hoặc ghi rõ ngoại lệ cho tin thương mại.
- **Ưu tiên:** P2.

#### UXA-19 — Notice tự ẩn, không có lịch sử (Potential)
- **Màn:** shell.
- **Rule:** CNT-006.
- **Mô tả:** Notice ẩn sau 5 s (thường) hoặc 8 s (cảnh báo/từ chối) (`GameShell.cs:471`). Chưa xác minh lời từ chối có
  được ghi vào Inbox hay nơi khác.
- **Khuyến nghị:** giữ lời từ chối đến khi người chơi đóng hoặc ghi vào một lịch sử xem lại được.
- **Ưu tiên:** P2.

#### UXA-20 — Không so được HLV hiện tại với ứng viên (Potential)
- **Màn:** Staff.
- **Rule:** CNT-004.
- **Mô tả:** Coach market hiện "Skill (est.)" của ứng viên; vùng Head coach chỉ có wage, hạn hợp đồng, capacity
  (`ShellScreens.cs:611-614`); `StaffRow` không có kỹ năng (`Contracts.cs:48`). Câu hỏi "Who could lead preparation
  instead?" thiếu mốc so sánh.
- **Bằng chứng:** [staff-1280x720.png](audit-evidence-2026-10-22/staff-1280x720.png).
- **Vì sao Potential:** có thể là chủ ý thiết kế (HLV của chính mình cũng chỉ ước tính?) — cần quyết định.
- **Khuyến nghị:** hiện kỹ năng (hoặc ước tính có marker) của HLV hiện tại trong cùng thang. Phụ thuộc read model.
- **Ưu tiên:** P2.

#### UXA-21 — Offer workspace nặng văn bản, che shell (Potential)
- **Màn:** Offer workspace.
- **Rule:** LAY-006.
- **Mô tả:** Workspace phủ toàn màn bằng nền `SurfaceBase` (`GameShell.cs:565-573`), ẩn thanh trên và rail. Hệ quả chính
  (load 65 → 85 / capacity 88, cash, forecast) trình bày bằng các dòng văn bản dài có tiền tố "Known ·"/"Estimated ·";
  phần "Comparison" bị cắt trong cuộn lồng ở 1280×720. Thesis pillar 5 "Instruments, not paperwork".
- **Bằng chứng:** [offer-1280x720.png](audit-evidence-2026-10-22/offer-1280x720.png).
- **Khuyến nghị:** một instrument bị chặn cho load/capacity (before → after) có text tương đương; giữ thanh trên.
- **Ưu tiên:** P2.

#### UXA-22 — Legend phím trong nhãn nút (Potential — mâu thuẫn tài liệu)
- **Màn:** xác nhận ("Cancel · Esc", `GameShell.cs:541`), offer ("Return to company · Esc", "Keep reviewing · Esc",
  `SponsorWorkspaceView.cs:146`), Company ("Back · Esc", `OperatingMap.cs:103`).
- **Rule:** CNT-007.
- **Mô tả:** Thesis P5 và amendment (non-negotiable) cấm keyboard-legend text trong không gian người chơi
  (`STAGE5_GAME_IDENTITY_THESIS.md:99`, `STAGE5_GENRE_SHELL_AMENDMENT.md:59`); polish standard §6 lại dùng "Return to
  company · Esc" làm ví dụ nhãn hợp lệ (`UI_POLISH_STANDARD.md:159-160`).
- **Khuyến nghị:** Director chọn; khuyến nghị đưa phím tắt vào tooltip như rail và Continue đang làm.
- **Ưu tiên:** P3.

#### UXA-23 — Tin mới nhất được phóng to (Potential — mâu thuẫn tài liệu)
- **Màn:** Portal World news.
- **Rule:** LAY-006.
- **Mô tả:** Tin mới nhất là thẻ lead lớn (wash + crest, `PortalScreen.News.cs:69-108`), đúng như mockup 11 và README
  genre shell (`genre-shell/README.md:287`); nhưng chuẩn §8 ghi "No story is enlarged for being first; a featured layout
  needs an explicit editorial importance rule".
- **Bằng chứng:** [portal-1920x1080.png](audit-evidence-2026-10-22/portal-1920x1080.png).
- **Khuyến nghị:** Director chọn: giới hạn quy tắc §8 cho Transfer news, hoặc thêm luật quan trọng biên tập cho lead.
- **Ưu tiên:** P3.

#### UXA-24 — "Review offer" hai cách trình bày
- **Màn:** Portal (verb brass nhỏ trong thẻ, `PortalScreen.cs:239-247`), Commercial (nút graphite rộng toàn thẻ,
  `ShellScreens.cs:522`); tương tự "Hire selected coach" rộng toàn vùng (`ShellScreens.cs:639-645`).
- **Rule:** CMP-005.
- **Bằng chứng:** [commercial-1280x720.png](audit-evidence-2026-10-22/commercial-1280x720.png),
  [portal-1280x720.png](audit-evidence-2026-10-22/portal-1280x720.png).
- **Khuyến nghị:** nút hành động cục bộ theo bề rộng nhãn, căn phải như thẻ quyết định.
- **Ưu tiên:** P3.

#### UXA-25 — Marker mang nghĩa khác
- **Màn:** Competition (Season fixtures: "■ Defeat 0–2", `ShellScreens.cs:191`), Squad (cột Contract ends: "▲ day 76"
  khi có thể gia hạn, `ShellScreens.cs:242`).
- **Rule:** ICO-006.
- **Mô tả:** Theo thesis, ■ = critical/error, ▲ = warning; ở đây chúng nghĩa là "thua" và "đang mở gia hạn".
- **Bằng chứng:** [competition-1280x720.png](audit-evidence-2026-10-22/competition-1280x720.png).
- **Khuyến nghị:** chữ ("Defeat", "renewal open") hoặc marker riêng đã định nghĩa.
- **Ưu tiên:** P3.

#### UXA-26 — Khoảng cách tuỳ ý ngoài thang
- **Màn:** toàn shell (mã).
- **Rule:** LAY-009.
- **Mô tả:** Shell dùng ≈20 giá trị khoảng cách literal (0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 18, 20) trong
  `Row(n)`, `Stack(n)`, `separation`, margin; `ShellLayout` dùng 14/10/7 (`ShellLayout.cs:12`, `:57`) trong khi thang
  `SpaceRole` là 4/8/12/20/24 (compact ×0,75) (`UiTokens.cs:151`). Brief §36 cấm khoảng cách tuỳ ý.
- **Vì sao chỉ Minor:** ảnh chụp không cho thấy lệch rõ; tác động chủ yếu là bảo trì và nhịp.
- **Khuyến nghị:** gom vào bảng khoảng cách có tên trong `ShellLayout`, ánh xạ về thang; không đổi pixel nhìn thấy trong
  bước đầu.
- **Ưu tiên:** P3.

#### UXA-27 — Chữ trên wash màu đội chưa kiểm tương phản (Potential)
- **Màn:** Portal Next match, lead World news.
- **Rule:** COL-009.
- **Mô tả:** Nền wash/glow pha màu đội (`PortalScreen.Competition.cs:43-47`, `PortalScreen.News.cs:80-82`); chữ caption
  ("◐ Strength 63–79", "▲ No plan yet") đặt lên trên. Ở các ảnh đã xem đọc được; chưa có kiểm với bảng màu đội xấu nhất.
- **Khuyến nghị:** check tự động tương phản chữ trên wash với mọi màu crest có thể sinh.
- **Ưu tiên:** P3.

#### UXA-28 — Thẻ tài trợ không phân biệt được khi tên dài
- **Màn:** Portal (case long-names).
- **Rule:** TYP-006.
- **Mô tả:** Hai thẻ "Sponsor offer" đều hiện "DEV Extraordinarily…" (`PortalScreen.cs:232`, clamp 2 dòng ở compact).
- **Bằng chứng:** [portal-case-long-names-1280x720.png](audit-evidence-2026-10-22/portal-case-long-names-1280x720.png).
- **Khuyến nghị:** cắt ở giữa hoặc ưu tiên phần phân biệt của tên; fixture dài bất thường nên ưu tiên thấp.
- **Ưu tiên:** P3.

#### UXA-29 — Nút disabled không nói lý do
- **Màn:** màn bắt đầu.
- **Rule:** CNT-006.
- **Mô tả:** "Continue saved campaign" disabled khi chưa có save (`GameShell.cs:105`), không có dòng giải thích.
- **Bằng chứng:** [start-1280x720.png](audit-evidence-2026-10-22/start-1280x720.png).
- **Khuyến nghị:** tooltip hoặc dòng "No saved campaign yet".
- **Ưu tiên:** P3.

## 5. Tóm tắt theo màn

| Màn | Confirmed | Potential | Vấn đề chính | Ưu tiên |
|---|---:|---:|---|---|
| Shell chrome (thanh trên, rail, notice) | 3 (UXA-01, 08, 15) | 1 (19) | "Blocking" không chặn; màu blocking lệch; notice che nội dung | P0 |
| Portal | 9 (03, 07, 08, 10, 11, 12, 13, 14, 28) | 3 (18, 23, 27) | Nhãn cắt giữa từ; mốc thời gian lẫn; khoảng trống 1080p; tràn ở tên dài | P0 |
| Staff + xác nhận thuê | 2 (02, 06) | 1 (20) | Xác nhận thiếu số tiền; thiếu mốc so sánh | P0 |
| Company | 2 (04, 05) | 1 (22) | Ngôn ngữ admin Stage 3, nhãn fixture, Back trùng | P1 |
| Offer workspace | 2 (06, 09) | 2 (21, 22) | Nhãn quay lại/breadcrumb sai; nặng văn bản | P1 |
| Squad | 3 (06, 07, 25) | 1 (17) | Câu hỏi thiết kế trong header; màu rating gắn cứng | P1 |
| Competition (Match, League) | 3 (06, 16, 25) | 1 (17) | Số không nhãn; marker sai nghĩa; Commit dưới nếp gấp | P1 |
| Inbox | 3 (06, 10, 11) | 0 | Văn bản log; cách viết giá | P1 |
| Commercial | 3 (06, 11, 24) | 0 | Phụ đề câu hỏi; cách viết giá; nút không nhất quán | P1 |
| Finance | 2 (06, 11) | 0 | Phụ đề giải thích luật; cách viết giá | P1 |
| Hệ thống (theme, khoảng cách) | 2 (07, 26) | 0 | Token ngữ nghĩa bị mượn; khoảng cách literal | P1 |
| Màn bắt đầu | 1 (29) | 0 | Disabled không lý do | P3 |
| Lab/prototype, console nội bộ | — | — | Không audit (không render) | — |

Một finding có thể xuất hiện ở nhiều dòng; tổng duy nhất là 29.

## 6. Pattern lặp, tác động cao, lệch hệ

**Pattern lặp nhiều nhất**
1. **Văn bản nội bộ trong không gian người chơi** (CNT-007): UXA-04, 06, 10, 22 — ở 9 bề mặt. Nguồn: câu hỏi thiết kế,
   enum/kind, trace mô phỏng, legend phím.
2. **Màu/marker ngữ nghĩa không nhất quán** (COL-007, ICO-006): UXA-07, 08, 25.
3. **Nhãn không khớp hành vi/đích** (CNT-009, CNT-011): UXA-01, 05, 09.
4. **Một sự thật nhiều cách trình bày** (CNT-008, CNT-005): UXA-11, 12, 18.
5. **Độ bền ở 1280×720 và nội dung dài** (LAY-010, TYP-006): UXA-03, 14, 15, 17, 28.

**Tác động cao nhất:** UXA-01 (trạng thái sai), UXA-02 (cam kết thiếu chi phí), UXA-04 (section lệch hệ), UXA-06 (nhiễu
toàn hệ thống), UXA-03 (lỗi đọc ở màn chính).

**Lệch hệ toàn cục:** section Company và breadcrumb offer dùng taxonomy operating map (Business & Finance, Affairs &
Time…) khác rail; ngữ nghĩa màu blocking; định dạng rate; legend phím.

**Ảnh hưởng nhiều component:** `ShellScreens.Card()` (header của mọi section, UXA-06); token `StatePositive` cho biến thể
nút (UXA-07); chuỗi sinh trong Domain/Application dùng ở Inbox, Portal, Commercial (UXA-10, 11).

## 7. Pattern chấp nhận được — cần giữ

| Pattern | Quy tắc chung có thể gắn cờ | Vì sao giữ | Bằng chứng |
|---|---|---|---|
| Tương phản chữ ≥ 4,5:1 trên mọi bề mặt | — | Đo được, vượt WCAG AA | §3 |
| Không bo góc; góc vát 45° theo tầng | CMP-001 | Ngôn ngữ đã duyệt; nhất quán trên mọi màn | §3, mọi ảnh |
| Vùng bấm ≥ 24 px, trạng thái hover/pressed/disabled/focus đầy đủ | — | Verify-Shell + sheet trạng thái | `surface-states-*.png` (artifacts) |
| Known/Estimated/Unknown bằng marker + chữ | — | Đúng brief §15 | [squad-1920x1080.png](audit-evidence-2026-10-22/squad-1920x1080.png), [competition-1280x720.png](audit-evidence-2026-10-22/competition-1280x720.png) |
| Bảng dày: số căn phải, chữ số tabular, hàng phân bằng tone, hàng chọn brass | LAY-001, CMP-006 | S5 được duyệt; dễ quét | [competition-league-1280x720.png](audit-evidence-2026-10-22/competition-league-1280x720.png) |
| Thẻ quyết định Portal: danh mục + hạn + chủ thể + verb, dẫn tới đích, xác nhận ở đích | LAY-001, CMP-007 | Mỗi thẻ là một việc độc lập; chuẩn §6 | [portal-1280x720.png](audit-evidence-2026-10-22/portal-1280x720.png) |
| Empty state trung thực, gọn | — | Không bịa nội dung | Transfer news trong [portal-1280x720.png](audit-evidence-2026-10-22/portal-1280x720.png) |
| Thanh trên: metric nhãn caps + giá trị, không icon, khoảng đều; Continue mạnh nhất | TYP-004 | Chuẩn §6; quy ước thể loại | mọi ảnh |
| Ring thắng/trận, thanh segment receipts/payments | ICO-004 | Đại lượng bị chặn, có số bên cạnh | Portal |
| Chip "● CONFIRMED" trên mọi tin chuyển nhượng | CMP-002 | Chuẩn §8; trung thực về độ chắc chắn | case `many-transfers` |
| Art module 3D trên rail, không housing | ICO-001, CMP-004 | Asset ngữ nghĩa đã duyệt | mọi ảnh |
| Rating 48 px trong hồ sơ | TYP-002 | Quy ước thể loại, mockup 03 (chỉ màu là vấn đề, UXA-07) | Squad |
| Overlay xác nhận: nền mờ, focus mặc định ở Cancel, premium region | COL-003 | Bóng/mờ biểu đạt lớp modal | [confirm-1280x720.png](audit-evidence-2026-10-22/confirm-1280x720.png) |
| Bỏ các tính năng ảo giác của mockup (Training, Calendar, Rumour, Accept/Reject trên thẻ, chân dung, ảnh đấu trường) | MCK-003 | Đúng amendment và chuẩn §8 | so sánh mockup 11 với Portal |
| Reduced motion đặt mọi thời lượng về 0 | MOT-002 | `UiTokens.Duration`, `UiMotion` | mã |
| Console nội bộ chỉ trong bản debug | CNT-007 | Chuẩn §7 cho phép | `GameShell.cs:107` |

## 8. Mâu thuẫn cần Director quyết định

| # | Chủ đề | Nguồn A | Nguồn B | Finding |
|---|---|---|---|---|
| T1 | Legend phím trong nhãn nút | Thesis P5, amendment "non-negotiable": không keyboard-legend text | Polish standard §6 dùng "Return to company · Esc" làm ví dụ | UXA-22 |
| T2 | Phóng to tin đầu World news | Mockup 11, genre-shell README:287 | Polish standard §8: "No story is enlarged for being first" | UXA-23 |
| T3 | Hành động chính của section ở 1280×720 | Brief §46, amendment: 720p giữ primary decision | Polish standard §9: section được cuộn dọc | UXA-17 |
| T4 | Hướng của section Company | Amendment: operating space thành section Company tuỳ chọn | Mockup 10 (HQ diorama, nhiều tính năng ảo giác); hiện thực là map Stage 3 | UXA-04 |
| T5 | Ý nghĩa của "blocking" | Thanh trên/tooltip: phải xử lý trước Continue | Luật hiện tại: chỉ trận chưa có kế hoạch chặn | UXA-01 |
| T6 | Mô tả Company nhúng | genre-shell README:23: không có back control | `OperatingMap.cs:103`: luôn có | UXA-05 (sửa mã hoặc tài liệu) |

Brief §6 ("pill form allowed where semantic") và polish standard §3 (không bo góc) khác nhau nhưng đã được giải quyết
trên thực tế theo chuẩn mới hơn; không cần hành động.

## 9. Vùng cần kiểm chứng thêm và điều chưa phủ

- **Bàn phím và focus:** thứ tự tab, phím Alt+1…8, Ctrl+Enter, Esc chưa được thử; chỉ thấy hình focus trên sheet trạng thái.
- **Chuyển động:** ảnh chụp ở reduced motion; highlight thay đổi, reveal, toast chưa được xem khi bật chuyển động.
- **Thời điểm mùa giải:** chỉ ngày 6 mùa 1. Chưa xem playoffs, hết mùa, nhiều tin/chuyển nhượng thật, khủng hoảng kéo dài.
- **Bề mặt chưa render:** pha xác nhận của offer workspace, chế độ "Map view" của Company, console nội bộ, UI Lab,
  Company Operating Field, Operating Map độc lập.
- **Text scale** 1–1,5 (`UiTokens`) và bản địa hoá (giãn chữ) chưa thử; DPI vật lý thuộc nghĩa vụ DV mở.
- **Hành vi Continue trong UXA-01** kết luận từ đường mã, chưa bấm thử trong UI.
- **Không có người dùng thật**: không có kiểm thử 10 giây (thesis P1) hay task test.

## 10. Quan sát về bộ mockup đã duyệt (nhóm MCK)

Mockup là tham chiếu bố cục đã duyệt; các điểm dưới **không** phải finding đối với hiện thực, mà là lưu ý khi dùng chúng:

- **MCK-001:** lỗi chữ trong mockup Portal ("ahead of the seconnd half").
- **MCK-002:** bộ section rail khác nhau giữa các ảnh (03 có News, League, Training; 11 có Calendar); thanh trên lúc
  "Day 24 / 365 · Regular Season", lúc "Season 1 · Week 3 · Mon, Apr 14, 2025".
- **MCK-003:** tính năng không có trong slice: chip Rumour/Negotiation/Transfer Listed/Interest, nút Accept/Reject và
  Renew/Release trực tiếp trên thẻ Portal, "Development Unit", "Company Objectives", "2 Active Teams / 4 Competitions",
  nút Settings, "Compare Player", "Edit Lineup".
- **MCK-004:** dữ liệu mẫu: "Raven Arena", "+3,200 this week", "▲12% vs last month", "2nd in league ▲ +1", chân dung,
  ảnh đấu trường và văn phòng.
- **MCK-005:** mọi mockup là 1672×941 — không phải 1280×720 hay 1920×1080; mật độ của chúng không chứng minh được ở kích
  thước đích.
- **MCK-007:** ví dụ đã xảy ra: màu xanh của "Overall Rating 88" trong mockup 03 được chép thành màu cố định (UXA-07).
- Phụ đề quảng cáo trong mockup ("Critical decisions this week will shape your team's progress", "Manage your players…
  for success") — hiện thực đã bỏ đúng ở Portal; không nên đưa lại.
