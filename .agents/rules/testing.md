# Testing rules

- Làm theo **TDD (đỏ → xanh → refactor)** và **BDD** cho hành vi: kịch bản `.feature` trước, test đỏ, rồi code. Review kịch bản và test theo `test-review.md`.
- Code mới đi kèm test trong cùng thay đổi. Sửa bug: viết test fail trước, rồi sửa.
- Bắt buộc có test: idempotency, outbox, consumer, xử lý lỗi, auth/phân quyền, logic nghiệp vụ.
- Tầng test:
  - **Unit**: domain/handler, không I/O, nhanh.
  - **Integration**: DB thật trong container (Testcontainers) hoặc LocalStack cho SNS/SQS/DynamoDB. Không mock DB để "cho qua".
  - **Contract**: API và event schema; đổi contract phải đổi test.
- Đặt tên test mô tả hành vi: `Method_Condition_ExpectedResult`. Mỗi test một ý, Arrange/Act/Assert.
- Test độc lập, không phụ thuộc thứ tự, không phụ thuộc thời gian thực/mạng ngoài; dùng clock/ID giả khi cần.
- Không skip, xoá, hoặc nới assertion để test pass. Test fail → tìm nguyên nhân gốc.
- Khi đang code chạy test **liên quan**; test đầy đủ ở các mốc theo `quality-gate.md`. Nêu rõ phần không chạy được.
