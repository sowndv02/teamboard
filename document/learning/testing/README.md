# Testing (TDD, BDD): lộ trình học (Part 0)

> [!NOTE]
> **Mục tiêu:** hiểu TDD, BDD và các chủ đề kề cận như những kỹ thuật độc lập với công cụ, rồi tự áp dụng lên một lát của TeamBoard: viết test trước, đặc tả bằng ví dụ, giữ lưới an toàn khi refactor, biết khi nào **không** nên làm theo.
> **Cần biết trước:** đọc được C# cơ bản, đã chạy `dotnet test` một lần. Chưa cần biết Gherkin hay mock.
> **Thời lượng ước tính:** TODO (chốt sau khi viết xong nội dung).
> **Loại:** khái niệm. Trạng thái: **khung**, nội dung các Part đang `TODO`.

## Cách học lộ trình này

Mỗi Part là một nhóm kiến thức (chiều ngang). Trong mỗi Part, nội dung đi qua tối đa 8 tầng (chiều sâu), đánh dấu `L1` đến `L8`:

| Tầng | Bạn trả lời được câu hỏi |
|---|---|
| L1 What | Khái niệm này là gì? |
| L2 Why | Tại sao cần nó, nó giải quyết vấn đề gì? |
| L3 How | Cơ chế bên trong hoạt động thế nào? |
| L4 Hands-on | Tự làm và đo bằng code hoặc lệnh thế nào? |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? |
| L7 Architecture | Khi nào chọn cách này thay vì cách khác (hoặc không làm)? |
| L8 Mastery | Tự thiết kế, giải thích đánh đổi, xử lý tình huống chưa gặp? |

Mô hình đầy đủ: [lesson-model.md](../../../.agents/skills/learning-hub/references/lesson-model.md).

## Các Part

| Part | Nội dung | Làm xong thì |
|---|---|---|
| [1 Fundamentals](part-1-fundamentals.md) | test là gì, pyramid, test double, TDD, BDD, ATDD, Bản đồ giải pháp | Giải thích và chọn được kỹ thuật phù hợp |
| [2 Core skills](part-2-core-skills.md) | TDD kata, xUnit, đặt tên, test data builder, viết Gherkin tốt | Làm được một vòng Đỏ-Xanh-Refactor |
| [3 Multi-component](part-3-multi-component.md) | integration, contract, test với DB và message bus | Test đúng ranh giới giữa các thành phần |
| [4 Advanced](part-4-advanced.md) | property-based, mutation, characterization, outside-in | Đo chất lượng test và test được code cũ |
| [5 Production](part-5-production-security-ops.md) | test trong CI, coverage đúng nghĩa, test hồi quy sự cố | Vận hành bộ test bền và rẻ |
| [6 Projects](part-6-projects.md) | áp dụng lên TeamBoard | Làm được một lát bằng TDD/BDD trên bài toán thật |
| [7 Mastery](part-7-troubleshooting-mastery.md) | test flaky, mock quá đà, case thực tế, phỏng vấn, bài mở | Tự đánh giá mức thành thạo |

## Bản đồ chủ đề → Part

| Chủ đề | Part chính |
|---|---|
| Test pyramid/trophy, F.I.R.S.T, Arrange-Act-Assert | 1 |
| Test double (dummy, stub, spy, mock, fake) | 1, 2 |
| TDD (Red-Green-Refactor, Chicago vs London) | 1, 2 |
| BDD (Given-When-Then, ví dụ minh hoạ spec), Gherkin | 1, 2 |
| ATDD, Specification by Example | 1, 6 |
| Refactoring dưới lưới test | 2, 4 |
| Integration test, contract test | 3 |
| Property-based, mutation testing | 4 |
| Characterization test, TDD cho code cũ | 4 |
| Outside-in vs inside-out | 4 |
| Test trong CI, coverage, test hồi quy | 5 |
| Test flaky, test chậm, mock quá đà | 7 |

## Ma trận phủ

`✓` là Part có mục tương ứng, `–` là không áp dụng (lý do ở dưới). Mỗi ô `✓` có đúng một mục `## Lx` trong file Part.

| Part | L1 | L2 | L3 | L4 | L5 | L6 | L7 | L8 |
|---|---|---|---|---|---|---|---|---|
| 1 | ✓ | ✓ | ✓ | ✓ | – | – | ✓ | – |
| 2 | ✓ | ✓ | ✓ | ✓ | ✓ | – | – | – |
| 3 | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | – |
| 4 | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | – |
| 5 | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – |
| 6 | ✓ | – | – | ✓ | ✓ | – | – | – |
| 7 | – | – | – | – | ✓ | – | ✓ | ✓ |

Lý do các ô `–`:
- **Part 1 L5, L6, L8:** Part nền tảng, chưa có hệ thống để gỡ lỗi hay vận hành; mastery dồn vào Part 7.
- **Part 2 L6, L7, L8:** kỹ năng lõi ở mức đơn vị; đánh đổi kiến trúc nằm ở Part 1 và 4, vận hành ở Part 5.
- **Part 3 L6, L8:** vận hành test ở Part 5; mastery ở Part 7.
- **Part 4 L6, L8:** vận hành ở Part 5; mastery ở Part 7.
- **Part 5 L8:** mastery tổng hợp ở Part 7.
- **Part 6 L2, L3, L6, L7, L8:** Part dự án chỉ mô tả bài toán (L1) và làm lab (L4, L5); lý thuyết đã có ở Part trước.
- **Part 7 L1 đến L4, L6:** Part tổng hợp, không dạy khái niệm mới; chỉ gỡ sự cố, so sánh và bài mở.

## Phạm vi

- Làm: kỹ thuật TDD, BDD và chủ đề kề cận, ví dụ C#/.NET trong domain Board/Task.
- Chưa làm (đợt sau): ghi chú **công cụ** riêng (xUnit, Reqnroll, Testcontainers, Stryker.NET), nằm ở thư mục `tools/` khi được tạo; quy tắc kiểm thử của dự án vẫn lấy từ `.agents/rules/testing.md` và `test-review.md`, lộ trình này không thay thế.

## Tiến độ nội dung

| Part | Trạng thái |
|---|---|
| 1 | Đã viết (2026-11-26): Bản đồ giải pháp, 4 case real-world, 2 lab đã chạy. Còn ⚠ ở các nguồn chỉ qua agent |
| 2 | Đã viết (2026-11-26): xUnit, builder, seam đồng hồ, Gherkin và Reqnroll, 4 lab đã chạy, 2 sự cố tái hiện thật. Còn ⚠ ở nguồn chỉ qua agent; không có case real-world riêng |
| 3 | Đã viết (2026-11-26): WebApplicationFactory, Testcontainers, contract test (Pact và bản viết tay), 4 lab đã chạy, 3 case real-world. Còn ⚠ ở nguồn chỉ qua agent; chưa chạy Pact thật |
| 4 | Đã viết (2026-11-26): property-based, mutation (Stryker.NET), snapshot (Verify), characterization, outside-in; 4 lab đã chạy, 2 sự cố tái hiện thật. Real-world chưa đạt (một case tự mở, một Vừa, một chưa mở được); bài QuickCheck gốc chưa đọc được |
| 5 | Đã viết (2026-11-26): phân nhóm test, coverage (coverlet) và gate, test hồi quy sự cố, flaky; 3 lab đã chạy, 2 sự cố tái hiện thật, 3 case real-world tự mở (Google, Cloudflare, Fowler). Chưa có CI thật trong repo nên chưa chạy pipeline; test bảo mật và dữ liệu test chưa có nguồn ngoài |
| 6 | Đã viết (2026-11-26): ba dự án trên `EmailAddress`/BR-001 của identity-service (TDD từ spec, kịch bản BDD lấp khoảng trống chạy với Docker, mutation 88,89% lên 100%); đã chạy thật trong bản sao ngoài repo. Dự án board-service thay bằng identity-service vì chưa có spec Board; chưa có đối chứng độc lập; không có real-world riêng |
| 7 | TODO: khung, chưa nghiên cứu nguồn, chưa chạy lab |

## Tiêu chí hoàn thành lộ trình

TODO: chốt khi có nội dung (mỗi Part có "Tự kiểm tra" đạt, lab đã chạy thật, Bản đồ giải pháp ≥ 3 lựa chọn kể cả "không test tự động").

## Nguồn và độ tin cậy

- **Đã tự mở và đối chiếu:** `.agents/skills/learning-hub/references/lesson-model.md`, `part-template.md`.
- **Chỉ qua nguồn thứ cấp hoặc agent (⚠):** chưa có.
- **Chưa kiểm hoặc chưa tìm được:** toàn bộ nguồn bên ngoài về TDD/BDD (Beck, North, Fowler...), sẽ tìm ở đợt `pack`.
