# Testing rules

- Làm theo **TDD (đỏ → xanh → refactor)** và **BDD** cho hành vi: kịch bản `.feature` trước, test đỏ, rồi code. Review kịch bản và test theo `test-review.md`.
- Code mới đi kèm test trong cùng thay đổi. Sửa bug: viết test fail trước, rồi sửa.
- Bắt buộc có test: idempotency, outbox, consumer, xử lý lỗi, auth/phân quyền, logic nghiệp vụ.
- Tầng test:
  - **Unit**: domain/handler, không I/O, nhanh.
  - **Integration**: DB thật trong container (docker compose; Testcontainers là phụ thuộc T2, cần phê duyệt) và giả lập AWS hoặc tài khoản sandbox cho SNS/SQS/DynamoDB (công cụ chưa chốt, xem mục "Giả lập AWS cục bộ"). Không mock DB để "cho qua".
  - **Contract**: API và event schema; đổi contract phải đổi test.
- Đặt tên test mô tả hành vi: `Method_Condition_ExpectedResult`. Mỗi test một ý, Arrange/Act/Assert.
- Test độc lập, không phụ thuộc thứ tự, không phụ thuộc thời gian thực/mạng ngoài; dùng clock/ID giả khi cần.
- Không skip, xoá, hoặc nới assertion để test pass. Test fail → tìm nguyên nhân gốc.
- Khi đang code chạy test **liên quan**; test đầy đủ ở các mốc theo `quality-gate.md`. Nêu rõ phần không chạy được.

## Giả lập AWS cục bộ

LocalStack hiện cần auth token (xem `dependency-policy.md` mục 7, ADR 0007); công cụ giả lập chưa chốt. Ưu tiên fake trong bộ nhớ qua interface cho unit test, ít test tích hợp dùng giả lập hoặc tài khoản AWS sandbox.
