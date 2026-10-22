# Báo cáo kiểm tra UI/UX — số liệu và nhật ký kiểm chứng (2026-10-22)

Trạng thái: **ENGINEERING EVIDENCE** — ghi lại mọi lần kiểm tra đã chạy trong đợt đánh giá UI/UX ngày 2026-10-22, kèm
đầu ra nguyên văn và cách chạy lại. Đây không phải duyệt thị giác của Director.

## 0. Bộ báo cáo UI/UX trong dự án

| Tài liệu | Nội dung |
|---|---|
| [UI_AI_ANTIPATTERNS.md](UI_AI_ANTIPATTERNS.md) | Rulebook 73 quy tắc, nghiên cứu và nguồn |
| [UI_UX_AUDIT_2026-10-22.md](UI_UX_AUDIT_2026-10-22.md) | Inventory UI, 29 finding (21 Confirmed, 8 Potential), tóm tắt theo màn, mâu thuẫn tài liệu |
| [UI_REMEDIATION_PLAN.md](UI_REMEDIATION_PLAN.md) | Quyết định D1–D6, ưu tiên P0–P3, đợt A–H, rủi ro hồi quy |
| **Tài liệu này** | Kết quả kiểm tra tự động, đo tương phản, kiểm tra tĩnh, danh mục ảnh, kiểm chứng skill |
| [audit-evidence-2026-10-22/VERIFY_SHELL_CHECKS.md](audit-evidence-2026-10-22/VERIFY_SHELL_CHECKS.md) | Danh sách đầy đủ 3.861 + 3.901 check Verify-Shell (nguyên văn) |
| [audit-evidence-2026-10-22/](audit-evidence-2026-10-22/) | 17 ảnh chụp được trích làm bằng chứng |
| [Skill ui-authenticity](../../../.claude/skills/ui-authenticity/SKILL.md) và [kịch bản kiểm thử](../../../.claude/skills/ui-authenticity/tests/scenarios.md) | Skill và kết quả desk-check 14 kịch bản |

Tóm tắt kết quả:

| Kiểm tra | Kết quả |
|---|---|
| Build client (Debug) | Thành công, 0 warning, 0 error |
| Chụp màn hình | 70 ảnh (35 cảnh × 2 kích thước), cả hai lượt PASS |
| Verify-Shell 1280×720 | 3.861 PASS, 0 FAIL |
| Verify-Shell 1920×1080 | 3.901 PASS, 0 FAIL |
| Tương phản chữ (13 vai trò × 7 bề mặt) | Mọi vai trò chữ ≥ 4,5:1; thấp nhất 4,63:1 |
| Kiểm tra tĩnh mã UI | Không `StyleBoxFlat` inline trong màn; bán kính góc 0; không chuỗi mẫu của mockup |
| Validator của skill | 38 PASS, 0 FAIL; 42 PASS, 0 FAIL sau khi thêm tài liệu này và README vào phạm vi kiểm |
| Đối chiếu audit ↔ kế hoạch | 29/29 finding có trong kế hoạch; ưu tiên P0–P3 khớp |
| Skill được Claude Code phát hiện | Có — xuất hiện trong danh sách skill của phiên ngay sau khi tạo |
| Audit thị giác | 29 finding; xem [audit](UI_UX_AUDIT_2026-10-22.md) |

## 1. Môi trường

| Mục | Giá trị |
|---|---|
| Commit | `587b910`, nhánh `stage5-sponsor-instruments` |
| Working tree | Có thay đổi chưa commit ở Domain/Application/tools (rating, generator); `game/Client` không đổi |
| Công cụ | .NET SDK 10.0.401, Godot 4.7.2 mono (pin trong `build/Environment.ps1`), Python 3.11 |
| Hệ điều hành | Windows 10 Pro 10.0.19044 |
| Dữ liệu chơi | Seed 20261004, công ty "Hanoi Signal", 1 trận đã đấu (ngày 6/76, mùa 1), content `content/fixture.json` |

## 2. Build và chụp màn hình

Lệnh (PowerShell, ngoài sandbox vì chụp cửa sổ cần desktop tương tác):

```powershell
build/Capture-Shell.ps1 -Results 1 -Cases -Output artifacts/ui-audit-2026-10-22
```

Đầu ra (trích):

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:18.86
PASS shell captures 1280x720
PASS shell captures 1920x1080
```

Harness chụp bật reduced motion (`GameShell.cs:63`), nên chuyển động không có trong ảnh. Ảnh nằm ở
`artifacts/ui-audit-2026-10-22/` (git-ignored); 17 ảnh được sao vào `audit-evidence-2026-10-22/`.

### 2.1. Danh mục ảnh

"Đã đọc" = ảnh đã được mở và xem trong đợt audit. "Evidence" = có bản sao trong thư mục evidence của dự án.

| Cảnh | Nội dung | 1280×720 | 1920×1080 | Evidence |
|---|---|---|---|---|
| `start` | Màn New campaign | Đã đọc | — | 1280 |
| `portal` | Portal mặc định | Đã đọc | Đã đọc | 1280, 1920 |
| `portal-case-distress` | Khủng hoảng tài chính, quyết định blocking | Đã đọc | — | 1280 |
| `portal-case-long-names` | Tên công ty/tài trợ/người rất dài | Đã đọc | — | 1280 |
| `portal-case-many-transfers` | Nhiều tin chuyển nhượng | Đã đọc | — | — |
| `portal-case-empty-news` | Không có World news | — | — | — |
| `portal-case-empty-transfers` | Không có tin chuyển nhượng | — | — | — |
| `portal-case-journeys` | Các kiểu hành trình chuyển nhượng | — | — | — |
| `portal-case-large-values` | Giá trị tiền lớn | — | — | — |
| `portal-case-many-decisions` | Nhiều quyết định | — | — | — |
| `portal-case-many-news` | Nhiều tin | — | — | — |
| `portal-case-missing-art` | Thiếu art | — | — | — |
| `portal-case-no-decisions` | Không có quyết định | — | — | — |
| `portal-case-no-match` | Không có trận kế tiếp | — | — | — |
| `portal-case-one-decision` | Một quyết định | — | — | — |
| `portal-case-one-story` | Một tin | — | — | — |
| `portal-case-return` | Quay lại Portal sau điều hướng | — | — | — |
| `portal-case-roster-shortfall` | Roster dưới mức tối thiểu | — | — | — |
| `portal-case-roundup` | Tin tổng hợp nhiều cầu thủ | — | — | — |
| `portal-case-warning` | Trạng thái cảnh báo tài chính | — | — | — |
| `rail-states` | Các trạng thái mục rail | — | — | — |
| `rail-collapsed` | Rail thu gọn | Đã đọc | — | 1280 |
| `inbox` | Inbox | Đã đọc | — | 1280 |
| `squad` | Squad, hồ sơ cầu thủ của mình | Đã đọc | Đã đọc | 1280, 1920 |
| `squad-under-contract` | Cầu thủ đang có hợp đồng ở đội khác | Đã đọc | — | — |
| `competition` | Competition — Match view | Đã đọc | — | 1280 |
| `competition-league` | Competition — League view | Đã đọc | — | 1280 |
| `commercial` | Commercial | Đã đọc | — | 1280 |
| `finance` | Finance | Đã đọc | — | 1280 |
| `staff` | Staff | Đã đọc | — | 1280 |
| `staff-under-contract` | HLV đang có hợp đồng ở đội khác | — | — | — |
| `company` | Company (operating map nhúng) | Đã đọc | — | 1280 |
| `offer` | Offer workspace (pha review) | Đã đọc | — | 1280 |
| `confirm` | Overlay xác nhận "Hire head coach" | Đã đọc | — | 1280 |
| `surface-states` | Sheet trạng thái control (dev) | Đã đọc | — | — |

Tổng: 20/70 ảnh đã được đọc trực tiếp; các ảnh còn lại được Verify-Shell kiểm tự động nhưng chưa được xem bằng mắt.

## 3. Verify-Shell (kiểm tra tự động của shell)

Lệnh (headless, chạy được trong sandbox):

```powershell
build/Verify-Shell.ps1 -NoBuild
```

Kết quả: `SHELL_PASS 3861` (1280×720) và `SHELL_PASS 3901` (1920×1080); 0 FAIL; stderr rỗng. Danh sách đầy đủ, nguyên
văn: [VERIFY_SHELL_CHECKS.md](audit-evidence-2026-10-22/VERIFY_SHELL_CHECKS.md).

Phân bố check theo nhóm (phân loại tự động theo từ khoá đầu tiên khớp; mỗi check vào đúng một nhóm, nên con số chỉ mang
tính định hướng):

| Nhóm | 1280×720 | 1920×1080 |
|---|---:|---:|
| Rail | 1619 | 1619 |
| Thanh trên | 274 | 274 |
| Header vùng | 638 | 665 |
| Bảng | 52 | 52 |
| Thẻ quyết định | 296 | 302 |
| Tin tức & chuyển nhượng | 436 | 437 |
| Tiền | 58 | 58 |
| Trận đấu | 68 | 68 |
| Khung & tầng vật liệu | 133 | 133 |
| Vừa chữ & cắt chữ | 52 | 52 |
| Điều khiển & vùng bấm | 4 | 4 |
| Overlay & xác nhận | 0 | 0 |
| Tài trợ | 22 | 24 |
| Màn bắt đầu | 2 | 2 |
| Khác | 207 | 211 |
| **Tổng** | **3861** | **3901** |

Nhận xét: bộ check rất mạnh về hình học, rail, header và nhãn hành động, nhưng **không** bắt được các finding thị giác
của audit — ví dụ nhãn "League p…" (UXA-03) được coi là văn bản có giới hạn hợp lệ, Portal tràn ở case tên dài
(UXA-14) và notice che nội dung khi rail thu gọn (UXA-15) không có check. Kiểm tra tự động đạt không thay cho duyệt thị giác.

## 4. Đo tương phản token (WCAG)

Phương pháp: công thức độ chói tương đối và tỷ lệ tương phản của WCAG 2.x trên giá trị hex trong `UiTokens.Colors`
(`game/Client/UI/Tokens/UiTokens.cs:105-126`), nền đặc. Ngưỡng: chữ thường 4,5:1; thành phần đồ hoạ 3:1. Không đo chữ
trên nền wash/gradient động (xem UXA-27).

| Màu (vai trò) | Base | Raised | Inset | Overlay | Header | Hover | Selected | Thấp nhất |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPrimary | 16.92 | 14.33 | 16.15 | 12.64 | 12.13 | 11.00 | 11.63 | 11.00 |
| TextSecondary | 10.10 | 8.56 | 9.65 | 7.55 | 7.25 | 6.57 | 6.95 | 6.57 |
| TextMuted | 7.60 | 6.44 | 7.25 | 5.68 | 5.45 | 4.94 | 5.22 | 4.94 |
| StatePositive | 10.23 | 8.67 | 9.77 | 7.65 | 7.34 | 6.66 | 7.03 | 6.66 |
| StateWarning | 11.87 | 10.05 | 11.33 | 8.87 | 8.51 | 7.72 | 8.16 | 7.72 |
| StateCritical | 7.12 | 6.03 | 6.79 | 5.32 | 5.10 | 4.63 | 4.89 | 4.63 |
| InformationEstimated | 12.28 | 10.40 | 11.72 | 9.17 | 8.80 | 7.98 | 8.44 | 7.98 |
| InformationUnknown | 11.06 | 9.37 | 10.56 | 8.27 | 7.93 | 7.20 | 7.61 | 7.20 |
| FocusRing | 13.50 | 11.43 | 12.88 | 10.08 | 9.68 | 8.78 | 9.28 | 8.78 |
| Border | 5.03 | 4.26 | 4.81 | 3.76 | 3.61 | 3.27 | 3.46 | 3.27 |
| FrameHighlight | 4.82 | 4.08 | 4.60 | 3.60 | 3.46 | 3.13 | 3.31 | 3.13 |
| AccentMuted | 4.32 | 3.66 | 4.13 | 3.23 | 3.10 | 2.81 | 2.97 | 2.81 |
| ActionPrimary | 8.23 | 6.97 | 7.86 | 6.15 | 5.90 | 5.35 | 5.66 | 5.35 |

Chữ trên nút brass: `TextOnAction` trên `ActionPrimary` 7,80:1; trên `ActionPrimaryHover` 9,38:1.

Kết luận:
- Mọi vai trò chữ đạt 4,5:1 trên mọi bề mặt đặc.
- `Border` và `FrameHighlight` đạt 3:1 (ngưỡng đồ hoạ) trên mọi bề mặt.
- `AccentMuted` dưới 3:1 trên Hover/Selected/Header (2,81–3,10). Nó chỉ dùng cho điểm nhấn cấu trúc, không mang
  thông tin hay ranh giới điều khiển, nên không vi phạm; ghi lại để không dùng nó cho chữ hoặc viền điều khiển.

## 5. Kiểm tra tĩnh mã UI

| Kiểm tra | Lệnh | Kết quả |
|---|---|---|
| `StyleBoxFlat` tạo trong màn hình (ngoài Theme/Tokens) | `grep -rln "new StyleBoxFlat" game/Client/UI` trừ `Theme/`, `Tokens/` | Không có |
| Bán kính góc | `UiTokens.cs:61` | `CornerRadius = 0`; các tham chiếu bán kính chỉ ở `UiTheme.cs` (gán 0) và một check |
| Chuỗi mẫu của mockup trong mã | grep "Raven Arena", "Nexus", "+3,200", "PLAYER 01" trong `game/Client/UI` và `src` | 0 file |
| Giá trị khoảng cách literal trong Shell | grep `Row(n)`, `Stack(n)`, `separation` | ≈20 giá trị khác nhau (0–20 px) ngoài thang `SpaceRole` → UXA-26 |
| Formatter tiền theo tuần | `Formatting.cs:48` | `Currency.PerWeek` có sẵn nhưng các chuỗi giá trong UXA-11 không dùng |
| Read model HLV | `Contracts.cs:48` | `StaffRow` không có exit cost và kỹ năng → UXA-02, UXA-20 |
| Luật "blocking" | `PortalTasks.cs:73`, `tests/Integration/Program.cs:277`, `:550-551` | Chỉ trận chưa có kế hoạch chặn Continue; việc khác chủ ý không chặn → UXA-01 |
| File `*.cs.uid` sinh ra | `find game -name "*.cs.uid"` | Không có (không chạy `--import`) |

## 6. Kích thước mockup đã duyệt

Đọc từ header ảnh (PNG IHDR, WebP VP8/VP8L/VP8X) trong
`docs/10-vertical-slice/ui-research/mockups/generated-reference-set/`:

| File | Kích thước |
|---|---|
| `01_portal_mockup.webp` … `10_company_mockup.webp` (10 file) | 1672 × 941 |
| `11_portal_world_transfer_graphite_mockup.png` | 1672 × 941 |

Không mockup nào ở 1280×720 hay 1920×1080 → MCK-005 trong audit §10.

## 7. Kiểm chứng skill `ui-authenticity`

### 7.1. Validator

Lệnh: `py -3.11 -I .claude/skills/ui-authenticity/scripts/validate_skill.py` — kết quả nguyên văn:

```text
PASS SKILL.md has YAML frontmatter
PASS name 'ui-authenticity' is lowercase-hyphenated and at most 64 characters
PASS name matches directory 'ui-authenticity'
PASS description present and at most 1024 characters (620)
PASS description has no angle brackets
PASS description + when_to_use within the 1,536-character listing cap (980)
PASS no unknown frontmatter keys
PASS SKILL.md body under 500 lines (111)
PASS SKILL.md body has no unescaped $<digit> (argument substitution)
PASS SKILL.md body has no !`command` injection
PASS links resolve in .claude\skills\ui-authenticity\references\audit-protocol.md
PASS links resolve in .claude\skills\ui-authenticity\references\mockup-protocol.md
PASS links resolve in .claude\skills\ui-authenticity\references\project-context.md
PASS links resolve in .claude\skills\ui-authenticity\references\rule-index.md
PASS links resolve in .claude\skills\ui-authenticity\references\self-review.md
PASS links resolve in .claude\skills\ui-authenticity\SKILL.md
PASS links resolve in .claude\skills\ui-authenticity\tests\scenarios.md
PASS links resolve in docs\10-vertical-slice\ui-ux-foundation\UI_AI_ANTIPATTERNS.md
PASS links resolve in docs\10-vertical-slice\ui-ux-foundation\UI_UX_AUDIT_2026-10-22.md
PASS links resolve in docs\10-vertical-slice\ui-ux-foundation\UI_REMEDIATION_PLAN.md
PASS rulebook headings and quick table list the same 73 rules
PASS rule names match between headings and quick table
PASS every rule has all required fields
PASS type and default severity match between each rule and the quick table
PASS every cited source ID exists in §7
PASS default severities use the four defined levels
PASS all ten rule categories are present
PASS references/rule-index.md matches the rulebook (73 rows)
PASS rule IDs cited in .claude\skills\ui-authenticity\references\audit-protocol.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\references\mockup-protocol.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\references\project-context.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\references\rule-index.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\references\self-review.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\SKILL.md exist
PASS rule IDs cited in .claude\skills\ui-authenticity\tests\scenarios.md exist
PASS rule IDs cited in docs\10-vertical-slice\ui-ux-foundation\UI_AI_ANTIPATTERNS.md exist
PASS rule IDs cited in docs\10-vertical-slice\ui-ux-foundation\UI_UX_AUDIT_2026-10-22.md exist
PASS rule IDs cited in docs\10-vertical-slice\ui-ux-foundation\UI_REMEDIATION_PLAN.md exist
38 passed, 0 failed
```

Sau khi tài liệu này và `README.md` của ui-ux-foundation được thêm vào phạm vi kiểm (liên kết và rule ID), validator
chạy 42 check: 42 PASS, 0 FAIL.

Lịch sử: lần chạy đầu có 4 FAIL — 3 liên kết tới file chưa viết lúc đó (audit, kế hoạch, kịch bản) và 1 lệch tên
thật (LAY-003 "không lý do" ở bảng tra ↔ "không có lý do" ở tiêu đề), đã sửa.

Frontmatter được kiểm bằng parser tối giản của validator; PyYAML không được cài nên chưa kiểm bằng parser YAML đầy đủ.

### 7.2. Phát hiện bởi Claude Code

Ngay sau khi tạo `.claude/skills/ui-authenticity/SKILL.md`, skill xuất hiện trong danh sách skill khả dụng của phiên
Claude Code đang chạy, với đúng `description` và `when_to_use`. Điều này xác nhận cấu trúc thư mục và frontmatter được
nhận. **Chưa đo** tỷ lệ tự kích hoạt đúng/sai theo yêu cầu thực tế.

### 7.3. Desk-check kịch bản

14 kịch bản đại diện (thiết kế, mockup, triển khai, tự đánh giá, audit, việc backend không liên quan, yêu cầu trái luật,
counter-template) — đối chiếu thủ công với văn bản skill: 10 đạt ngay, 4 đạt sau khi bổ sung "Bước 0.5" (xử lý yêu cầu
trái luật đã duyệt). Thêm một khoảng trống được vá: mini-blueprint không thay thế blueprint/review record bắt buộc.
Chi tiết: [tests/scenarios.md](../../../.claude/skills/ui-authenticity/tests/scenarios.md). Đây không phải eval tự động.

## 8. Đối chiếu audit ↔ kế hoạch khắc phục

Script đọc bảng tổng hợp §4.1 của audit, các mục chi tiết §4.2 và các phần P0–P3 của kế hoạch. Kết quả nguyên văn:

```text
rows 29 unique 29
status Counter({'Confirmed': 21, 'Potential': 8})
severity Counter({'Moderate': 16, 'Minor': 10, 'Major': 3})
priority Counter({'P2': 12, 'P3': 8, 'P1': 6, 'P0': 3})
details 29 True
missing from plan []
P0 plan==audit
P1 plan==audit
P2 plan==audit
P3 plan==audit
```

## 9. Điều chưa kiểm

- Điều hướng bàn phím, thứ tự focus, phím tắt (Alt+1…8, Ctrl+Enter, Esc).
- Chuyển động khi tắt reduced motion.
- 50/70 ảnh chưa được xem bằng mắt (chỉ Verify-Shell); các thời điểm khác trong mùa giải.
- Pha xác nhận của offer workspace, chế độ "Map view" của Company, console nội bộ, UI Lab, Company Operating Field,
  Operating Map độc lập.
- Text scale 1–1,5, bản địa hoá, DPI vật lý.
- Tương phản chữ trên wash màu đội.
- Kiểm thử với người dùng thật (thesis P1 ten-second test).

## 10. Phụ lục — script để chạy lại

Các script dưới đây chạy bằng `py -3.11 -I <file>` từ thư mục bất kỳ; đường dẫn repo được ghi cứng là
`C:\Management Game`.

### 10.1. Tương phản token

```python
def lin(c):
    c = c / 255
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
def L(h):
    r, g, b = int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)
    return .2126 * lin(r) + .7152 * lin(g) + .0722 * lin(b)
def cr(a, b):
    la, lb = L(a), L(b); return (max(la, lb) + .05) / (min(la, lb) + .05)
surf = {"SurfaceBase": "0C0E11", "SurfaceRaised": "1C2025", "SurfaceInset": "121417", "SurfaceOverlay": "252A31",
        "HeaderSurface": "282D34", "HoverSurface": "2E343B", "SelectedSurface": "2B3037"}
text = {"TextPrimary": "EEF0F2", "TextSecondary": "B6BCC4", "TextMuted": "9CA3AD", "StatePositive": "4FD1B5",
        "StateWarning": "F2C46D", "StateCritical": "F07A72", "InformationEstimated": "E2CC9A",
        "InformationUnknown": "BFC4D0", "FocusRing": "C2D9FF", "Border": "7A838F", "FrameHighlight": "76808C",
        "AccentMuted": "8C7444", "ActionPrimary": "C9A45A"}
for t, tv in text.items():
    vals = [(round(cr(tv, sv), 2), s) for s, sv in surf.items()]
    print(f"{t:22s}", " ".join(f"{s[:8]}={v}" for v, s in vals), " min=", min(vals)[0])
print("TextOnAction on ActionPrimary", round(cr("1A1405", "C9A45A"), 2))
print("TextOnAction on ActionPrimaryHover", round(cr("1A1405", "D8B56E"), 2))
```

Giá trị hex lấy từ `UiTokens.cs`; khi token đổi, cập nhật hai từ điển.

### 10.2. Đối chiếu audit ↔ kế hoạch

```python
import re, collections, sys, pathlib
sys.stdout.reconfigure(encoding="utf-8")
docs = pathlib.Path(r"C:\Management Game\docs\10-vertical-slice\ui-ux-foundation")
t = (docs / "UI_UX_AUDIT_2026-10-22.md").read_text(encoding="utf-8")
summary = t.split("### 4.1.")[1].split("### 4.2.")[0]
rows = [[c.strip() for c in l.strip().strip('|').split('|')] for l in summary.splitlines() if l.startswith('| UXA-')]
print("rows", len(rows), "unique", len({r[0] for r in rows}))
print("status", collections.Counter(r[6] for r in rows))
print("severity", collections.Counter(r[4] for r in rows))
print("priority", collections.Counter(r[7] for r in rows))
details = re.findall(r"^#### (UXA-\d+)", t, re.M)
print("details", len(details), details == [r[0] for r in rows])
plan = (docs / "UI_REMEDIATION_PLAN.md").read_text(encoding="utf-8")
print("missing from plan", sorted({r[0] for r in rows} - set(re.findall(r"UXA-\d+", plan))))
for sec, p in (("## P0", "P0"), ("## P1", "P1"), ("## P2", "P2"), ("## P3", "P3")):
    ids = set(re.findall(r"UXA-\d+", plan.split(sec)[1].split("\n## ")[0]))
    want = {r[0] for r in rows if r[7] == p}
    print(p, "plan==audit" if ids == want else f"DIFF plan-only {sorted(ids - want)} audit-only {sorted(want - ids)}")
```

### 10.3. Danh sách check Verify-Shell

`VERIFY_SHELL_CHECKS.md` được sinh từ `artifacts/shell/verify-1280x720.log` và `verify-1920x1080.log` sau khi chạy
`build/Verify-Shell.ps1 -NoBuild`: giữ nguyên mọi dòng không rỗng của log, mỗi kích thước một khối.
