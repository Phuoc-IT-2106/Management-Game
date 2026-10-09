# Hướng dẫn thiết lập — LoL Esports Data Starter

Công cụ nghiên cứu/staging **độc lập** với game. Nó không đọc hay ghi vào gameplay, UI, save/load hay dữ liệu `content/` của game. Chi tiết kỹ thuật và giới hạn pháp lý: xem [README.md](README.md).

## Trạng thái hiện tại

| Hạng mục | Trạng thái |
|---|---|
| Mã nguồn | `C:\Management Game\tools\lol-data-ingestion\` (giải nén phẳng từ `lol_esports_data_starter.zip`) |
| Python | `.venv\` dùng **Python 3.11.6**; `requests 2.34.2`, `python-dotenv 1.2.4` |
| `.env` | Có sẵn, **vẫn là placeholder** `PASTE_YOUR_TOKEN_HERE`. Đây là file duy nhất bạn cần sửa |
| Git | `.env`, `.venv/`, `data/`, `logs/`, `*.sqlite3`, `*.log` đều bị `.gitignore` loại trừ |
| Dữ liệu thật | **Chưa có.** Chưa có request nào gửi tới PandaScore |

Mọi lệnh bên dưới chạy trong **PowerShell** (hoặc terminal của VS Code), bắt đầu bằng:

```powershell
cd "C:\Management Game\tools\lol-data-ingestion"
```

## Quy tắc bảo mật cho API key

- Chỉ ghi key vào **một chỗ duy nhất**: dòng `PANDASCORE_TOKEN=` trong file `.env`.
- **Không dùng Find/Replace cho cả thư mục** để thay `PASTE_YOUR_TOKEN_HERE`. VS Code mặc định bỏ qua file bị git-ignore như `.env`, nên Replace All sẽ sửa nhầm file mã nguồn và `.env.example` (hai file này *có thể* bị commit), còn `.env` lại không được sửa.
- Không gửi key vào chat với AI, không đưa vào README, ảnh chụp màn hình hay game.
- Nếu key từng nằm ở file khác `.env`, hoặc đã bị lộ ở bất kỳ đâu: vào Dashboard và **Regenerate** token, rồi chỉ dán token mới vào `.env`.

## Bước 1 — Lấy PandaScore API key miễn phí

1. Mở <https://app.pandascore.co/> rồi đăng ký (Sign up) hoặc đăng nhập.
2. Tài khoản mới mặc định dùng **Free plan**, không cần thẻ tín dụng và không cần chọn gói trả phí.
3. Xác nhận email nếu được yêu cầu.
4. Mở Dashboard <https://app.pandascore.co/dashboard> và copy **access token**.

Gói Free:
- Giới hạn **1000 request/giờ**. Script tự chờ ít nhất 4 giây giữa các request.
- Gồm lịch thi đấu, kết quả và thông tin giải/đội/tuyển thủ. Endpoint ngoài gói sẽ trả `HTTP 401/403`.
- Theo PandaScore, các gói này chỉ dành cho mục đích không liên quan đến cá cược.

## Bước 2 — Nhập key vào `.env` bằng VS Code

```powershell
code .env
```

(Hoặc mở trong Explorer của VS Code: `tools/lol-data-ingestion/.env`, **không phải** `.env.example`.)

Sửa dòng thứ hai thành:

```
PANDASCORE_TOKEN=token_cua_ban
```

Không cần dấu nháy, không có khoảng trắng thừa. Lưu bằng **Ctrl+S**.

## Bước 3 — Kiểm tra cấu hình (offline, không gọi API)

```powershell
.\.venv\Scripts\python.exe check_config.py
```

| Kết quả | Ý nghĩa |
|---|---|
| `STATUS: NOT_CONFIGURED` (exit 2) | `.env` thiếu, rỗng hoặc vẫn là placeholder. Làm lại Bước 2 |
| `STATUS: CONFIGURED` (exit 0) | Python đã đọc được key (chỉ in độ dài, không in giá trị). **Chưa** xác minh với API |

## Bước 4 — Kiểm tra kết nối (tốn đúng 1 request)

```powershell
.\.venv\Scripts\python.exe check_config.py --connect
```

| Kết quả | Ý nghĩa |
|---|---|
| `STATUS: CONNECTION_VERIFIED` | 1 request thật tới `/lol/leagues` thành công; có in số request còn lại trong giờ. Không lưu dữ liệu |
| `CONFIGURED (connection NOT verified)` + `HTTP 401`/`403` | Token sai hoặc đã bị regenerate. Copy lại từ Dashboard |
| `CONFIGURED (connection NOT verified)` + `HTTP 429` | Hết quota giờ này. Chờ sang giờ sau |
| `... ConnectionError` / `ProxyError` | Lỗi mạng hoặc proxy |

Token không bao giờ được in ra, kể cả trong thông báo lỗi.

## Bước 5 — Đồng bộ thử, giới hạn 1 trang

```powershell
.\.venv\Scripts\python.exe sync_pandascore.py --collections leagues teams players --max-pages 1
```

Lệnh gửi tối đa **3 request** (mỗi collection 1 trang × 100 bản ghi) và mất khoảng 15 giây. `--max-pages 1` **cố tình không đầy đủ**. Không được coi kết quả là "toàn bộ dữ liệu 2026".

Các tùy chọn CLI thực tế: `--collections {leagues,series,tournaments,teams,players,matches}`, `--max-pages` (≥1), `--per-page` (1–100), `--delay` (≥4 giây), `--db`, `--snapshot-root`.

## Bước 6 — Kiểm tra raw data và database

Manifest của lần chạy mới nhất (`"status": "completed"` là thành công):

```powershell
Get-ChildItem data\raw\pandascore -Directory | Sort-Object Name | Select-Object -Last 1 | ForEach-Object { Get-Content (Join-Path $_.FullName 'manifest.json') }
```

Số bản ghi theo collection:

```powershell
.\.venv\Scripts\python.exe -c "import sqlite3; db=sqlite3.connect('data/staging.sqlite3'); [print(r) for r in db.execute('SELECT collection, count(*) FROM provider_records GROUP BY collection')]"
```

## Dữ liệu được lưu ở đâu

Mặc định mọi output nằm trong `tools\lol-data-ingestion\data\`, **dù bạn chạy lệnh từ thư mục nào**. Toàn bộ thư mục này bị git loại trừ.

| Đường dẫn | Nội dung |
|---|---|
| `data\raw\pandascore\<UTC-run-id>\*.json` | Snapshot gốc từng trang, không sửa đổi |
| `data\raw\pandascore\<UTC-run-id>\manifest.json` | Trạng thái run, số dòng, SHA256 từng trang |
| `data\staging.sqlite3` → `provider_records` | Bản ghi PandaScore, khóa `(provider, collection, external_id)`, có `first_seen_at`/`last_seen_at` |
| `data\staging.sqlite3` → `oracle_player_game_raw` | Dòng player-game từ Oracle's Elixir CSV, kèm SHA256 của file CSV |
| `data\reports\participation_2026.csv` | Báo cáo tham gia thi đấu năm 2026 |

Đây là vùng **raw/staging**. Không có gì được nhập vào `content/`, save, hay model Domain/Application của game.

## Oracle's Elixir (dữ liệu trận đấu)

CSV **không** đi kèm và phải tải thủ công theo điều kiện sử dụng của trang. Trang tải: <https://master.d36liwrx5rvjnc.amplifyapp.com/tools/downloads> (cùng site với <https://oracleselixir.com/tools/downloads>). Tải file match data năm 2026, rồi:

```powershell
.\.venv\Scripts\python.exe import_oracles_elixir.py "C:\duong\dan\toi\2026_LoL_esports_match_data_from_OraclesElixir.csv"
.\.venv\Scripts\python.exe report_2026.py --year 2026
```

Importer cần các cột `gameid`, `participantid`, `date`, `playername`, `teamname`. Nó chỉ giữ `participantid` 1–10 (bỏ dòng tổng đội) và import lại nhiều lần không bị trùng.

### Phân loại tuyển thủ: công cụ cho biết gì, và không cho biết gì

| Nhóm | Công cụ hiện có trả lời được không? |
|---|---|
| **Có game trong năm 2026** | Có, qua `report_2026.py`. Nhưng mỗi dòng là một tổ hợp *handle–team–league*, chưa phải một người đã khử trùng lặp. Cột `interpretation` luôn là `played_in_year_only_not_current_roster` |
| **Đang có tên trong roster hiện tại** | **Không.** Từng thi đấu năm 2026 ≠ vẫn đang thi đấu. Cần bằng chứng roster có mốc thời gian. Bản ghi `teams`/`players` của PandaScore chỉ phản ánh dữ liệu nhà cung cấp tại `last_seen_at`, chưa phải roster đã xác minh |
| **Academy / reserve / substitute** | **Không suy luận.** Người đã ký hợp đồng nhưng chưa ra sân có thể không xuất hiện trong CSV |
| **Không đủ dữ liệu xác minh** | Phải giữ trạng thái `unknown`, không được đoán |

## Chạy lại kiểm tra offline bất kỳ lúc nào

```powershell
.\.venv\Scripts\python.exe -B -m unittest discover -s tests -v
.\.venv\Scripts\python.exe -m pip check
.\.venv\Scripts\python.exe check_config.py
```

## Xử lý lỗi thường gặp

| Thông báo | Cách xử lý |
|---|---|
| `PANDASCORE_TOKEN missing...` | `.env` vẫn là placeholder hoặc chưa lưu. Làm lại Bước 2–3 |
| `ERROR: RuntimeError: HTTP 401/403 ...` (kèm traceback) | Token sai, hoặc endpoint (ví dụ `matches`) ngoài gói Free. Dòng `ERROR:` là thông tin chính; traceback không chứa token |
| `HTTP 429: hourly quota exhausted` | Hết quota 1000 request/giờ. Chờ, rồi giảm `--max-pages` |
| `FileExistsError` ở thư mục snapshot | Hai lần chạy trùng cùng một giây UTC. Chạy lại sau vài giây |
| `no such table: oracle_player_game_raw` | Chạy `import_oracles_elixir.py` trước `report_2026.py` |
| `Python venv missing` (từ `run_sync.ps1`) | `py -3.11 -m venv .venv` rồi `.\.venv\Scripts\python.exe -m pip install -r requirements.txt` |

Quét rộng hơn chỉ nên làm sau khi Bước 5 ổn: `.\run_sync.ps1 -MaxPages 2 -Collections leagues,teams,players`. Lên lịch tự động (Task Scheduler) **chưa được thiết lập**; xem README nếu sau này cần.

## Thay đổi so với starter gốc

| File | Thay đổi | Lý do |
|---|---|---|
| `sync_pandascore.py`, `import_oracles_elixir.py`, `report_2026.py` | Đường dẫn mặc định `data/...` neo vào thư mục script (`DATA_DIR`) | Bản gốc tính theo thư mục hiện tại. Chạy từ `C:\Management Game` sẽ tạo `C:\Management Game\data\`, thư mục **không** bị git loại trừ |
| `sync_pandascore.py` | Cổng chặn token coi mọi giá trị bắt đầu bằng `PASTE_YOUR_` là "chưa có key" | Bản gốc chỉ khớp đúng một chuỗi. Một placeholder khác, hoặc một lần Find/Replace, làm cổng mất tác dụng và script gửi request với token giả |
| `.gitignore` | Thêm `.env.*` (trừ `.env.example`), `*.sqlite3*`, `*.sqlite`, `*.db`, `*.log` | Phòng khi `--db` hoặc log được ghi ra ngoài `data/`/`logs/` |
| `check_config.py` (mới) | Báo `NOT_CONFIGURED` / `CONFIGURED` / `CONNECTION_VERIFIED`; dùng lại `request_page` của starter | Starter không có lệnh kiểm tra kết nối riêng |
| `tests/test_check_config.py` (mới) | Test offline (session giả, không mở socket) cho 200/401/403/429/500, việc che token và cổng chặn placeholder | Chứng minh xử lý lỗi mà không cần key |
| `discover_api.py` (mới) | Khảo sát API có giới hạn (≤25 request, dừng khi gặp 429); raw lưu ở `data/raw/discovery/`, chạy `--analyze` thì offline | Kiểm chứng endpoint, phân trang, field và bộ lọc 2026. Kết quả nằm ở `reports/API_DISCOVERY_2026.md` |
| `SETUP_VI.md` (mới) | Tài liệu này | |

## Lưu ý phạm vi

- Đây là dữ liệu nghiên cứu thô. Có mặt trong PandaScore hay Oracle's Elixir **không** có nghĩa là tuyển thủ còn thi đấu.
- Không đưa tên, ảnh hay logo thật vào game khi chưa rà soát giấy phép. Riot có hạn chế riêng về việc game dùng IP của Riot; thuê bao dữ liệu không đồng nghĩa với quyền dùng IP đó.
