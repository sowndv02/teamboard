# Bài học kinh nghiệm (lessons learned)

Sổ ghi lại **lỗi/sự cố/sai sót đã xảy ra** và **cách ngăn nó lặp lại**. Mục tiêu: không mắc lại cùng một lỗi. Một bài học chỉ "xong" khi có **biện pháp phòng ngừa đã được mã hoá** (rule, test tự động, gate, mẫu, ADR), không chỉ là ghi chú.

- Quy tắc ngắn: `.agents/rules/lessons-learned.md`. Quy trình ghi/tra/rà: skill `lesson-learned` (`.agents/skills/lesson-learned/`).
- Mẫu: [`_template.md`](_template.md). Tên file: `LL-NNN-<slug>.md` (số tăng dần, không tái sử dụng).
- Văn hoá **không đổ lỗi** (blameless): mô tả hệ thống và quy trình đã để lỗi lọt qua, không phải ai sai. Không ghi secret/dữ liệu cá nhân.
- Tra theo **tag** trước khi làm việc trong một vùng (đọc bảng dưới, chỉ mở lesson khớp tag).

## Từ vựng tag
`security` `cost` `infra` `aws` `api` `messaging` `database` `testing` `workflow` `docs` `dependency` `git` `observability` `frontend` `backend` `agent-behavior` `process` + tên service (`board-service`, `task-service`...).

## Trạng thái phòng ngừa
- `prevented`: đã có biện pháp phòng ngừa và đã kiểm chứng.
- `partial`: có biện pháp nhưng còn việc dở (ghi trong lesson).
- `open`: chưa có biện pháp (cần làm).
- `recurred`: lỗi lặp lại sau khi đã ghi → nâng cấp biện pháp (ưu tiên tự động hoá).

## Danh sách

| ID | Bài học (một dòng) | Tag | Phòng ngừa | Biện pháp chính | Issue |
|---|---|---|---|---|---|
| [LL-001](LL-001-secrets-committed-in-env-files.md) | Không commit file `.env` chứa mật khẩu; ignore và dùng `*.example` ngay từ đầu | `security` `git` | partial | `.gitignore`, `*.example`; chờ gate `gitleaks` và xoay mật khẩu | TB-002 |
| [LL-002](LL-002-tool-license-assumed-before-checking.md) | Kiểm tra giấy phép/điều khoản của công cụ trước khi đưa vào thiết kế và tài liệu | `dependency` `docs` `process` | prevented | `dependency-policy.md`, `config/dependencies.yaml`, ADR 0007 | TB-002 |
