---
title: <tên lát>
issue: TB-NNN
status: draft            # draft | in-review | approved | done
owner: <người chịu trách nhiệm>
updated: YYYY-MM-DD
spec: document/spec/<ten>.md
design: document/design/<ten>.md
services: [<service liên quan>]
---

# Plan: TB-NNN — <tên lát>

## 1. Phạm vi của lát
**Làm:**
-

**Không làm (trong lát này):**
-

## 2. Bảng truy vết

| Yêu cầu (FR/BR/NFR) | Quyết định design | Bước plan | Kịch bản | Test dự kiến |
|---|---|---|---|---|
| FR-001 | D1 | B1 | @S01 | |

## 3. Các bước (mỗi bước một chu kỳ Đỏ → Xanh → Refactor)

### B1. <mục tiêu>
- **Kịch bản/test sẽ viết:**
- **File/module dự kiến chạm:** (đã đọc code để xác định)
- **Hợp đồng thay đổi (API/sự kiện):**
- **Migration / cấu hình / biến môi trường:**
- **Tài liệu cần cập nhật:**
- **Kiểm chứng:** (lệnh + kết quả quan sát được)
- **Phụ thuộc:**

(Lặp B2…Bn)

## 4. Chiến lược test
| Tầng | Phạm vi | Dữ liệu/môi trường | Ghi chú (mock/không mock) |
|---|---|---|---|

Công cụ test/BDD (từ `config/tool-versions.yaml`):

## 5. Rủi ro và rollback
| Rủi ro | Tác động | Giảm thiểu | Rollback |
|---|---|---|---|

Ảnh hưởng consumer (Angular, service khác):

Bảo mật (bước kiểm quyền/validate/secret và test tương ứng):

Chi phí (môi trường dev/test, tài nguyên mới, bước dọn/`destroy`, tag):

## 6. Gate và Definition of Done
- Gate: `code-change-gate.md`, `quality-gate.md` (vùng bị ảnh hưởng)
- Tiêu chí hoàn thành của issue:
- Tài liệu/ADR/config cập nhật:

## 7. Câu hỏi mở và giả định
| # | Điều chưa rõ | Ảnh hưởng tới | Người trả lời | Ghi chú (không tự điền) |
|---|---|---|---|---|

## Confirm log
| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|

## Review rounds
| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|

## Lịch sử thay đổi
| Ngày | Thay đổi | Lý do |
|---|---|---|
| YYYY-MM-DD | Tạo bản nháp | |
