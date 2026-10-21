# Quy ước tài liệu

Áp dụng khi tạo/sửa file trong `document/`, `config/`, các file Markdown của dự án, hoặc vẽ Mermaid. Quy tắc **cập nhật** tài liệu (cái nào được sửa, cái nào bất biến) nằm ở `docs-update-specs.md`.

## Cấu trúc và đặt tên file

| Loại | Nơi | Tên file |
|---|---|---|
| Kiến trúc tổng thể | `document/architecture/README.md` | – |
| ADR | `document/architecture/adr/` | `NNNN-ten-ngan-gon.md` (4 chữ số, tăng dần, không tái sử dụng) |
| Lựa chọn dịch vụ AWS theo service | `document/infra/` | `<service>.md` (khớp tên trong `config/services.yaml`); mẫu `_template.md` |
| Kiến thức bảo mật (kịch bản tấn công/phòng thủ) | `document/security/` | `<chu-de>.md` (kebab-case) |
| Hồ sơ quản lý dự án (roadmap, backlog, RAID, báo cáo, sprint, retro) | `document/pm/` | `roadmap.md`, `backlog.md`, `raid.md`; `status/YYYY-MM-DD.md`, `sprints/SNN.md`, `retro/YYYY-MM-DD.md` |
| Bài học kinh nghiệm | `document/lessons/` | `LL-NNN-<slug>.md`; mẫu `_template.md`; mục lục `README.md` |
| Trạng thái workflow | `document/workflow/` | `<id>.yaml` (`TB-NNN` hoặc `feature-<ten>`); mẫu `_template.yaml` |
| Plan triển khai | `document/plans/` | `TB-NNN-<slug>.md` (khớp tên issue); mẫu `_template.md` |
| Thiết kế hệ thống (design doc) | `document/design/` | `<tinh-nang>.md` (cùng tên spec tương ứng); mẫu `_template.md` |
| Spec/FSD từng service hoặc feature | `document/spec/` | `<service-hoac-feature>.md` (kebab-case) |
| Kịch bản BDD (living doc) | `document/spec/features/<service>/` | `<ten>.feature` (kebab-case) |
| Contract (API, event schema) | `document/contracts/` | `NN-ten-ngan-gon.md` (2 chữ số, kebab-case) |
| Issue | `document/issues/` | `TB-NNN-slug.md`; mẫu `_template.md` |
| Runbook | `document/runbooks/` | `ten-su-co.md` |
| Slide/tài liệu học | `document/slide/` | `dayN-weekN.md` |
| Index thư mục | mỗi thư mục | `README.md` (không đánh số) |

- Chữ thường, ngăn cách bằng `-`, không dấu tiếng Việt, không khoảng trắng, không `snake_case`. Ngoại lệ: `README.md`, `_template.md`, key issue `TB-NNN`.
- Mỗi thư mục tài liệu có `README.md` làm mục lục; thêm/đổi tên/xoá file thì cập nhật mục lục trong cùng thay đổi.
- Tên file ổn định, dễ sort và nhất quán trong cùng thư mục; không đổi convention đang có khi chưa có yêu cầu refactor.

### Khi đổi tên hoặc xoá file tài liệu

- Tìm (`Grep`) và sửa **mọi** tham chiếu: link trong `document/`, `README.md`, `PROJECT.md`, `config/*.yaml`, `.agents/**`, mục lục.
- Không để bản trùng tên cũ chạy song song với tên mới.
- Không đổi tên ADR đã chấp nhận hoặc issue đã `done` trừ khi người dùng yêu cầu (link lịch sử sẽ hỏng).

## Nội dung

- Viết tiếng Việt, câu ngắn; thuật ngữ kỹ thuật giữ nguyên tiếng Anh. Tên field, API key, enum giữ nguyên theo contract.
- Thuật ngữ nâng cao hoặc ít gặp: giải thích ngắn ở lần đầu trong mỗi tài liệu (inline hoặc mục "Thuật ngữ"), không chỉ dán link (`explain-terms.md`).
- Tài liệu quyết định ghi trạng thái và ngày ở đầu file (ADR: `Đề xuất | Chấp nhận | Thay thế bởi ADR-NNNN`).
- Link tương đối giữa các tài liệu; không để link chết.
- Không chép lại nội dung đã có nơi khác; link tới nguồn. Số liệu (cổng, tên service, phiên bản) lấy từ `config/`.
- Spec mô tả hành vi cụ thể (đầu vào, đầu ra, lỗi, ví dụ), không mô tả chung chung. Dùng "mục" khi tham chiếu phần trong tài liệu (không dùng ký hiệu `§`).
- Giá trị chưa quyết định ghi `TODO`, không đoán.

## Mermaid

- Khối ```` ```mermaid ````; mỗi sơ đồ một ý, tối đa khoảng 15 node, có chú thích một câu ngay phía trên nói sơ đồ thể hiện điều gì.
- Dùng cấu hình mặc định của Mermaid; không tắt HTML labels để xử lý lỗi hiển thị chung. Nếu renderer cụ thể cần cấu hình thì chỉ chỉnh ở cấp root theo version Mermaid đang dùng.
- **Màu**: khi tô màu node bằng `classDef`, luôn đặt rõ màu chữ (ví dụ `color:#000000`) để đọc được ở cả giao diện sáng và tối.
- **Nhóm**: sơ đồ có nhiều nhóm thì dùng màu nhất quán theo nhóm qua `classDef`/class (cùng trách nhiệm = cùng class), kèm chú giải màu khi ý nghĩa không tự rõ. Không tô màu ngẫu nhiên từng node.
- **Nhãn**: chỉ bọc nhãn node/cạnh bằng ngoặc kép `["..."]` khi có ký tự dễ gây lỗi parser (ngoặc đơn, `/`, `&`). Nhãn đơn giản không cần ngoặc kép.
- **Nhãn dài**: ưu tiên Markdown Strings để Mermaid tự xuống dòng; cần kiểm soát bố cục thì dùng `<br>` hoặc xuống dòng chủ động. Chỉ đặt `markdownAutoWrap: false` khi có yêu cầu bố cục cụ thể.
- Tên service trong sơ đồ dùng đúng tên trong `config/services.yaml`.
