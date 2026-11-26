# ADR 0013 — Xoay khoá ký JWT (D7) trong identity-service

Trạng thái: Chấp nhận · Ngày: 2026-11-26
Nguồn: TB-035, `document/plans/TB-035-identity-signing-key-rotation.md`, design D7
Nhãn: `[Chọn để học]`

## Bối cảnh
ADR 0010 chốt ký RS256 từ file JSON với một khoá `active`. ADR 0012 công bố JWKS một khoá `active`. FR-008 yêu cầu xoay khoá mà không làm token access còn hạn hỏng: resource server phải thấy nhiều `kid` trên JWKS trong cửa sổ xoay. Design D7 mô tả chu trình operator-driven qua CLI; production Secrets Manager hoãn (TB-032/D3).

## Quyết định
- **Trạng thái khoá** trong file `Identity:SigningKeys:JsonFile`: `pending` → `active` → `retiring` (xoá khỏi file khi `retire` xong). Đúng một khoá `active` tại mọi thời điểm; phát access token chỉ dùng khoá `active`.
- **JWKS** (`ADR 0012`, cập nhật): xuất **mọi khoá được công bố** — `pending`, `active`, `retiring` — chỉ phần công khai RS256; metric `jwks_served_total` giữ nguyên.
- **Reload:** `FileSigningKeyStore` đọc lại file trước mỗi lần ký và trước khi phục vụ JWKS (Confirm plan TB-035 #2).
- **Ba lệnh CLI** (gọi trực tiếp từ test/ops, chưa bắt buộc wire `Program`):
  - `keys rotate start` — tạo RSA 2048 mới `pending`, ghi `publishedAtUtc` (ISO-8601 `O`).
  - `keys rotate activate` — sau ≥ **10 phút** kể từ `publishedAtUtc` của `pending`: khoá `active` cũ → `retiring` + `retiringSinceUtc`; `pending` → `active` (đổi `kid` theo policy triển khai).
  - `keys rotate retire` — sau ≥ **24 giờ + 60 giây** ở `retiring`: xoá các entry `retiring` khỏi file; JWKS không còn `kid` đó.
- **Audit:** log cấu trúc `key_rotation event=…` qua `ILogger` (không bảng DB trong lát này).
- **Ghi file:** write temp + move atomic; validate máy trạng thái trước khi ghi.
- **Nguồn khoá:** chỉ file JSON dev/CI (giống ADR 0010); không Secrets Manager trong TB-035.
- **Không** trong lát: xoay theo lịch tự động, route gateway JWKS, đổi hành vi refresh/login ngoài hồi quy FR-008.

## Phương án đã cân nhắc

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| A. CLI operator + file JSON (chọn) | Khớp D7 và lab; test được với file tạm | Thao tác thủ công production | Chọn (lát 7) |
| B. Xoay tự động theo lịch | Ít phụ thuộc người | Phức tạp, cần SM/KMS | Hoãn |
| C. Secrets Manager PutSecretValue ngay | Gần production | Ngoài phạm vi TB-035 | Hoãn |

## Hệ quả
- Consumer `Teamboard.Auth` với cache JWKS 10 phút: operator phải chờ cửa sổ publish trước `activate` (guard CLI = TTL cache).
- Token phát trước `activate` vẫn verify khi `retiring` còn trên JWKS; sau `retire` token cũ fail nếu còn hạn (FR-008).
- ADR 0012 bổ sung JWKS đa khoá khi xoay; một khoá `active` duy nhất khi không xoay (hồi quy @S05).

## Liên kết
FR-008, ADR 0010, ADR 0011, ADR 0012, `document/design/identity-auth.md` D7, TB-035 plan.