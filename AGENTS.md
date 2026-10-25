# Quy tắc chung cho AI agent

Các rule dưới đây áp dụng cho mọi dự án. Thông tin riêng của từng dự án (mục tiêu, kiến trúc, lệnh, trạng thái) nằm trong `PROJECT.md` ở gốc repo, và cấu hình workflow (issue, branch, commit, PR, danh sách tài liệu) nằm trong `config/project.yaml`. Khi hai bên mâu thuẫn, tài liệu của dự án thắng.

## Bắt đầu phiên

1. Đọc `PROJECT.md` và `config/project.yaml`.
2. Đọc các tài liệu tier R0 trong `docs.required` của `config/project.yaml`; tier R1 khi vào phạm vi liên quan; tier R2 chỉ khi điều kiện `when` đúng. Không quét toàn bộ tài liệu.
3. Đọc `.agents/rules/INDEX.md` và chỉ các rule khớp với việc đang làm (bảng "hoạt động → rule"); luôn đọc `agent-conduct.md` và (khi nhận task) `task-workflow.md`. Trước khi báo xong, commit hoặc mở PR cho bất kỳ thay đổi nào, bắt buộc qua `code-change-gate.md`. Rule trong `.agents/rules/` thắng convention mặc định của agent.
4. Tra bài học liên quan: đọc mục lục `document/lessons/README.md`, chỉ mở lesson có tag khớp vùng đang làm (rule `lessons-learned.md`).
5. Chạy các lệnh kiểm tra đầu phiên trong file lệnh của dự án (`COMMANDS.md`).
6. Giá trị `TODO` trong config nghĩa là chưa quyết định: hỏi người dùng, không tự điền.

## Quy trình làm việc

- Mọi thay đổi code gắn với một issue theo cách dự án quy định. Chưa có issue thì tạo trước, rồi mới làm.
- Tài liệu mâu thuẫn nhau hoặc yêu cầu mơ hồ: hỏi người dùng chốt phương án trước khi sửa code hay tài liệu. Không suy diễn hành vi khi spec, contract hoặc ADR đã mô tả cụ thể.
- Đọc code và tài liệu liên quan trước khi sửa; không đoán cấu trúc.
- Việc lớn hoặc nhiều bước: đưa kế hoạch ngắn và chờ duyệt trước khi code.
- Chia thành các lát nhỏ chạy được end-to-end; không làm nhiều phần lớn cùng lúc.
- Mỗi lát có tiêu chí kiểm chứng cụ thể. Tự chạy build, test hoặc lệnh kiểm chứng trước khi báo xong; nếu không chạy được thì nói rõ.
- Chỉ làm đúng phạm vi được yêu cầu. Thấy vấn đề ngoài phạm vi thì báo lại, không tự sửa.
- Khi mắc lỗi, bị người dùng sửa, hoặc review tìm ra lỗi có nguyên nhân hệ thống: ghi bài học và biện pháp phòng ngừa (skill `lesson-learned`), đừng chỉ sửa rồi bỏ qua.

## Chất lượng code

- Theo style và quy ước sẵn có của repo; không thêm dependency hoặc abstraction khi chưa cần.
- Code mới đi kèm test; logic quan trọng (xử lý lỗi, idempotency, bảo mật, tiền) bắt buộc có test.
- Không nuốt lỗi âm thầm; không bỏ qua test hoặc hook đang fail, hãy tìm nguyên nhân.
- Tên code/identifier và comment trong source code viết tiếng Anh; tài liệu viết tiếng Việt (theo `PROJECT.md`). Comment chỉ giải thích "vì sao", không mô tả "cái gì".

## Kiến trúc và quyết định

- Tuân thủ kiến trúc và ranh giới mà `PROJECT.md` quy định; không tự "đơn giản hoá" làm lệch mục đích.
- Quyết định kiến trúc mới hoặc thay đổi quyết định cũ → viết ADR, không đổi ngầm.
- Sự cố đã gặp và cách xử lý → ghi runbook ngắn.

## Git

- Mỗi thay đổi một branch riêng; tên branch, format commit và PR theo `config/project.yaml`. Commit nhỏ, message nêu rõ lý do.
- Đổi behavior thì cập nhật spec, contract, ADR hoặc test tương ứng trong cùng thay đổi.
- Chỉ commit/push khi được yêu cầu. Không force-push, không bỏ qua hook.
- Không commit file sinh ra (bin, obj, build output) hoặc file local.

## Bảo mật

- Không commit, in ra hoặc ghi log secret, token, mật khẩu, access key. Dùng biến môi trường, secret manager hoặc profile/SSO.
- Cung cấp file `*.example` cho cấu hình cần secret; file thật phải nằm trong `.gitignore`.
- Phát hiện secret đã bị commit → báo ngay và đề xuất xoay (rotate), không chỉ xoá khỏi file.

## Hạ tầng và thao tác rủi ro

- Thao tác tốn tiền, phá huỷ hoặc khó đảo ngược (apply/destroy hạ tầng, xoá dữ liệu, ghi lên môi trường thật) cần người dùng đồng ý rõ ràng ngay lúc đó; một lần đồng ý không áp dụng cho lần sau.
- Hạ tầng quản lý bằng code (IaC); không tạo tài nguyên bằng tay hay script ngoài IaC.
- Lệnh chỉ đọc hoặc kiểm tra (plan, validate, lint, test) được chạy tự do.
