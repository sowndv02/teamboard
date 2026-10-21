# Cập nhật tài liệu: chỉ sửa spec; issue và ADR cũ bất biến

Lý do tồn tại: giữ lịch sử quyết định và delivery đáng tin (audit trail), tránh "sửa quá khứ cho khớp code" và tài liệu mô tả hành vi cũ.

## Phạm vi

Mọi agent khi cập nhật tài liệu trong `document/`: ad-hoc, khi code theo issue, khi sửa finding review, khi đồng bộ contract, hoặc khi người dùng yêu cầu đổi hành vi/phạm vi.

## Quy tắc

### 1. Tài liệu lịch sử: chỉ đọc

**Không sửa nội dung** (mô tả, tiêu chí, kế hoạch, kết luận) của:

| Loại | Điều kiện là "cũ" |
|---|---|
| **ADR** | Đã `Chấp nhận` (hoặc `Thay thế bởi`). Đổi quyết định → viết ADR **mới**, ở ADR cũ chỉ đổi dòng trạng thái thành `Thay thế bởi ADR-NNNN` và thêm link. |
| **Issue** | Issue **không** khớp issue key của branch hiện tại, hoặc issue đã `done`/`dropped`. |
| **Plan** (`document/plans/TB-NNN-*.md`) | Plan của issue không khớp key branch hiện tại, hoặc issue đã `done`/`dropped`. Plan của branch hiện tại được sửa qua skill `plan-init`/`plan-review`. |

Chúng là snapshot lịch sử để đối chiếu; không "backport" hành vi mới vào đó.

Được phép với tài liệu cũ: sửa lỗi chính tả/link chết không đổi nghĩa, cập nhật mục lục, thêm link tham chiếu mới.

### 2. Branch binding: chỉ sửa issue của branch đang làm

Trước khi sửa nội dung bất kỳ issue nào, chạy:

```bash
git branch --show-current
```

| Bước | Quy tắc |
|---|---|
| Issue key của branch | Regex `TB-\d+` trong tên branch (`feature/TB-001-foo` → `TB-001`) |
| Issue key của file | Từ tên file (`TB-001-foo.md` → `TB-001`) |
| Cho phép sửa nội dung | **Chỉ khi hai key trùng nhau** |

| Tình huống | Hành vi |
|---|---|
| Branch `feature/TB-002-*`, sửa `TB-002-*.md` | **Được** (tick tiêu chí, nhật ký, trạng thái, Review rounds) |
| Branch `feature/TB-002-*`, sửa `TB-001-*.md` | **Cấm**: sửa spec, hoặc tạo issue mới |
| Branch `main` hoặc không parse được key | **Cấm** sửa nội dung issue; chỉ spec/docs khác |

Ngoại lệ: bảng danh sách trong `document/issues/README.md` được cập nhật khi đổi trạng thái/tạo issue.

State file workflow (`document/workflow/<id>.yaml`) là sổ theo dõi quy trình, không phải nội dung tài liệu: được cập nhật khi chạy workflow theo `state-protocol.md` (state theo issue chỉ khi branch khớp issue). Nhật ký sự kiện chỉ thêm, không sửa lịch sử.

### 3. Hành vi đổi → cập nhật spec

Khi hành vi, contract hoặc kiến trúc thay đổi và cần đồng bộ tài liệu, sửa **nguồn hiện hành**:

| Được cập nhật | Ví dụ |
|---|---|
| Spec/FSD | `document/spec/**` |
| Contract (API, event schema) | `document/contracts/**`, `src/shared` contract và test tương ứng |
| Kiến trúc, config (khi task yêu cầu) | `document/architecture/README.md`, `config/*.yaml`, `PROJECT.md` |
| Issue của branch hiện tại | tiêu chí hoàn thành, nhật ký |
| Runbook | khi sự cố mới hoặc cách xử lý đổi |

Phạm vi/tiêu chí **mới** → tạo **issue mới** (hoặc ADR mới nếu là quyết định kiến trúc), không nhồi vào issue/ADR cũ.

### 4. Đồng bộ khi người dùng yêu cầu đổi hành vi

Khi người dùng yêu cầu đổi behavior hoặc phạm vi, cập nhật **cùng thay đổi** mọi tài liệu mô tả hành vi đó: spec, contract, test mapping, tiêu chí issue hiện tại. Không để tài liệu mô tả hành vi cũ.

Nếu thay đổi đó **hoàn tác** một yêu cầu người dùng đã đưa trước đó → hỏi lại người dùng trước khi sửa (xem `code-review.md`, mục sửa finding).

### 5. Ngoại lệ

| Tình huống | Được phép |
|---|---|
| Tạo issue/ADR mới | Luôn được; thêm vào mục lục |
| Người dùng yêu cầu **rõ** sửa một issue/ADR cũ cụ thể | Nêu hậu quả (mất audit trail) và chờ xác nhận tường minh, hoặc checkout đúng branch của issue đó |
| Tham chiếu chéo | Thêm link từ spec sang issue/ADR (chỉ thêm link, không sửa nội dung đích) |

## Anti-patterns

- Sửa `TB-001-*.md` (đã `done`) trên branch `feature/TB-005-*` để "cho khớp code".
- Sửa nội dung ADR đã chấp nhận thay vì viết ADR mới.
- Ghi hành vi mới vào issue cũ thay vì spec.
- Tin trạng thái ghi trong chat mà không đối chiếu `git branch --show-current` và key của file.
- Trộn sửa tài liệu lịch sử vào commit tính năng không liên quan.
