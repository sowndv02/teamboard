# Part 6 — Real-world Projects (Testing)

> [!NOTE]
> **Mục tiêu:** áp dụng TDD, BDD và mutation testing lên code và spec thật của TeamBoard, đo kết quả bằng lệnh, không chỉ đọc lý thuyết.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md) đến [Part 5](part-5-production-security-ops.md); Docker Desktop chạy được (dự án 6.2).
> **Thời lượng:** ~3 đến 5 giờ nếu làm đủ ba dự án · **Đầu ra kiểm được:** (1) chuỗi test đỏ→xanh theo từng yêu cầu của spec; (2) một kịch bản BDD mới lấp khoảng trống spec, chạy xanh và đỏ khi cố ý phá code; (3) mutation score trước và sau khi bổ sung test.
> **Part trước / sau:** [←](part-5-production-security-ops.md) · [→](part-7-troubleshooting-mastery.md)

## L1 What — Mô tả các dự án

Cả ba dự án dùng chung một yêu cầu thật của [`document/spec/identity-auth.md`](../../spec/identity-auth.md): **BR-001**, email là định danh duy nhất và được chuẩn hoá (bỏ khoảng trắng đầu/cuối, so sánh không phân biệt hoa thường). Code thật là [`EmailAddress.cs`](../../../src/identity-service/Features/Register/EmailAddress.cs) của `identity-service`.

**Quy tắc làm dự án:** chỉ làm trong **bản sao ngoài repo**; không sửa `src/` hay `document/spec/` của repo (xem "Phạm vi" của [Part 0](README.md)). Lấy bản sao sạch từ commit hiện tại:
```bash
mkdir lh-copy && git archive HEAD | tar -x -C lh-copy
```

#### Dự án 6.1 — TDD một yêu cầu của spec (kata trên code thật)
**Là gì:** viết lại `EmailAddress.TryNormalize` bằng vòng Đỏ-Xanh-Refactor, mỗi chu kỳ một yêu cầu của spec (BR-001, kịch bản S11 "email sai định dạng"), rồi chạy **cùng bộ test** lên code thật để so sánh.
**Use case:** tập TDD trên logic thuần nhỏ, test chạy trong mili-giây, không cần Docker.
**Lệch so với khung ban đầu:** khung ghi "một lát board-service". `board-service` hiện chỉ có endpoint `/api/v1/auth/me` ([`Program.cs`](../../../src/board-service/Program.cs)) và chưa có spec cho domain Board, nên chưa có yêu cầu nào để TDD mà không bịa hành vi; dự án chuyển sang `identity-service`, nơi có spec.

#### Dự án 6.2 — Tìm khoảng trống giữa spec và kịch bản BDD
**Là gì:** đối chiếu yêu cầu trong spec với tag trong `.feature`, tìm yêu cầu chưa có kịch bản, viết kịch bản mới **tái dùng step có sẵn**, chạy thật.
**Use case:** BDD là đặc tả sống; mỗi quy tắc nghiệp vụ nên có ít nhất một ví dụ thực thi được.

#### Dự án 6.3 — Đo mutation score và bổ sung test
**Là gì:** chạy Stryker.NET lên `EmailAddress` thật, đọc mutant còn sống hoặc không được phủ, bổ sung test, đo lại.
**Use case:** biết bộ test của dự án 6.1 thật sự kiểm hay chỉ chạy qua code ([Part 4](part-4-advanced.md)).

## L4 Hands-on — Làm dự án

Môi trường chạy: Windows 11, .NET SDK 10.0.402, xunit 2.9.3, dotnet-stryker 5.1.0, Docker 29.5.3, Git Bash. Thư mục làm việc do bạn tự tạo, ngoài repo.

#### Dự án 6.1 — TDD `EmailAddress` từ spec
**Chuẩn bị:** solution `LhProj` với `LhProj.Domain` (code của bạn) và `LhProj.Tests`. Dùng **cùng namespace và tên** với code thật để chạy lại test lên code thật sau này:
```csharp
namespace IdentityService.Features.Register;
public static class EmailAddress
{
    public static bool TryNormalize(string email, out string normalized)
        => throw new NotImplementedException();
}
```
**Chu kỳ 1 (email hợp lệ giữ nguyên).**
```csharp
[Fact] public void TryNormalize_ValidEmail_ReturnsTrueAndSameValue()
{
    Assert.True(EmailAddress.TryNormalize("user@example.com", out var n));
    Assert.Equal("user@example.com", n);
}
```
Đỏ: `Failed: 1, Passed: 0`. Xanh bằng code tối thiểu `normalized = email; return true;`: `Passed: 1`.
**Chu kỳ 2 (S11: không có `@` là sai).** Đỏ: `Failed: 1, Passed: 1, Total: 2`. Xanh: thêm `if (!email.Contains('@')) return false;` và `normalized = string.Empty` ở đầu: `Passed: 2`.
**Chu kỳ 3 (BR-001: hoa thường).** `"Case-S07@Example.COM"` mong đợi `case-s07@example.com`. Đỏ: `Failed: 1, Passed: 2`. Xanh: `ToLowerInvariant()`: `Passed: 3`.
**Chu kỳ 4 (BR-001: bỏ khoảng trắng đầu/cuối).** `"  user@example.com \t"` mong đợi `user@example.com`. Đỏ: `Failed: 1, Passed: 3`. Xanh: `Trim()`: `Passed: 4`.
**Chu kỳ 5 (các hình dạng sai).** Một `[Theory]` với tám đầu vào: `""`, `"   "`, `"a@b@c.com"`, `"user@example"`, `"us er@example.com"`, `"@example.com"`, `"user@.com"`, `"user@example."`. Đỏ:
```text
Failed ...InvalidShape(email: "user@.com")
Failed ...InvalidShape(email: "@example.com")
Failed ...InvalidShape(email: "a@b@c.com")
Failed ...InvalidShape(email: "user@example.")
Failed ...InvalidShape(email: "user@example")
Failed ...InvalidShape(email: "us er@example.com")
Failed!  - Failed: 6, Passed: 6, Total: 12
```
Hai đầu vào `""` và `"   "` **xanh ngay** (không đỏ), vì kiểm `@` đã loại chúng; xem L5. Xanh bằng regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` sau `Trim()`: `Passed: 12`.
**So với code thật.** Sao chép `EmailAddress.cs` của repo vào project `LhProj.Real` (có `<InternalsVisibleTo Include="LhProj.RealTests" />` vì lớp thật là `internal`), dùng **đúng file test** ở `LhProj.RealTests`:
```text
Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```
**Giải thích:** bộ test sinh từ spec chạy xanh trên code thật. ⚠ **Thiên lệch:** tôi đã đọc code thật (có regex) trước khi viết chu kỳ 5, nên việc hội tụ về cùng regex không phải bằng chứng TDD tự dẫn tới thiết kế đó.
**Tiêu chí đạt:** mỗi chu kỳ có một lần đỏ ghi lại đúng lý do rồi xanh; 12/12 trên code thật.
**Dọn dẹp:** không có.

#### Dự án 6.2 — Lấp khoảng trống BR-001 bằng kịch bản BDD
**Bước 1: tìm khoảng trống.** Tag yêu cầu trong `document/spec/features/identity-service/`:
```bash
grep -rn -B1 -A1 "@BR-001" document/spec/features/identity-service
grep -rn -i "whitespace\|trim\|spaces\|khoảng trắng" document/spec/features
```
Kết quả: hai kịch bản gắn `@BR-001` (`register.feature` S07 và `login.feature` S04), **cả hai về chữ hoa/thường**; lệnh tìm "trim/whitespace" **không có kết quả**. Phần "bỏ khoảng trắng đầu/cuối" của BR-001 chưa có ví dụ thực thi.
**Bước 2: viết kịch bản, tái dùng step có sẵn** (không viết code step mới), thêm vào cuối `register.feature` trong bản sao:
```gherkin
@S12 @FR-001 @BR-001
Scenario: Email with surrounding spaces is treated as the same account
  Given an empty PostgreSQL database is running
  And identity-service is configured to use that database and applies migrations on startup
  And a user is registered with email "trim-s12@example.com" and password "ValidPass123456"
  When I send a registration request with email "  trim-s12@example.com  " and password "ValidPass123456"
  Then the status code is 409
  And the error follows the standard format with code "EMAIL_ALREADY_EXISTS" and a trace id
```
**Bước 3: chạy** (cần Docker; test dùng Testcontainers):
```bash
dotnet test src/identity-service.Tests --filter "Category=S12&Category=FR-001"
```
```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 8 s
```
Kịch bản **xanh ngay** vì code đã cắt khoảng trắng; khoảng trống nằm ở **đặc tả**, không ở code.
**Bước 4: chứng minh kịch bản canh gác được.** Trong bản sao, đổi `var trimmed = email.Trim();` thành `var trimmed = email;` rồi chạy lại:
```text
Failed Email with surrounding spaces is treated as the same account
Expected: 409
Actual:   400
Failed!  - Failed: 1, Passed: 0, Total: 1
```
Khôi phục dòng cũ: `Passed: 1`.
**Giải thích:** bỏ `Trim()` thì chuỗi có khoảng trắng bị regex từ chối (400) thay vì nhận ra tài khoản trùng (409); kịch bản bắt đúng lỗi đó. So chi phí: một kịch bản chạy qua Docker mất khoảng 6 đến 8 giây, trong khi 12 test đơn vị của dự án 6.1 mất khoảng 70 ms; nên kịch bản BDD giữ ví dụ nghiệp vụ, còn các hình dạng sai nằm ở test đơn vị.
**Tiêu chí đạt:** kịch bản xanh với code thật, đỏ với code đã phá, và chỉ dùng step có sẵn.
**Lưu ý:** kịch bản chưa được thêm vào repo (ngoài phạm vi issue này); đưa vào repo phải qua quy trình thường (issue, review kịch bản `test-review.md`).
**Dọn dẹp:** `rm -rf lh-copy`; Testcontainers tự xoá container khi test kết thúc.

#### Dự án 6.3 — Mutation score trước và sau
**Chuẩn bị:** trong thư mục của dự án 6.1:
```bash
dotnet new tool-manifest
dotnet tool install dotnet-stryker --version 5.1.0
cd LhProj.RealTests
```
**Bước 1: chỉ bốn test của chu kỳ 1 đến 4 (không có `[Theory]`).**
```bash
dotnet stryker --project LhProj.Real.csproj --test-case-filter "FullyQualifiedName!~InvalidShape"
```
```text
Number of tests found: 4
12 mutants created
1 mutants got status NoCoverage.   Reason: Not covered by any test.
3 mutants got status Ignored.      Reason: Removed by block already covered filter
The final mutation score is 88.89 %
```
Mutant không được phủ: `Boolean mutation` đổi `return false` thành `true` trong nhánh `string.IsNullOrWhiteSpace(email)`; bốn test đầu **không có test nào đưa chuỗi rỗng hoặc toàn khoảng trắng**.
**Bước 2: thêm `[Theory]` chu kỳ 5 rồi chạy lại.**
```bash
dotnet stryker --project LhProj.Real.csproj
```
```text
Number of tests found: 12
12 mutants created
3 mutants got status Ignored.      Reason: Removed by block already covered filter
The final mutation score is 100.00 %
```
Chín mutant được kiểm đều bị diệt (Killed).
**Giải thích:** điểm tăng từ 88,89% lên 100% nhờ một nhóm test, và mutant báo đúng chỗ thiếu. ⚠ **100% không có nghĩa bộ test hoàn hảo:** danh sách mutant không có mutant nào nhắm vào **chuỗi regex** trong thuộc tính `[GeneratedRegex(...)]`, nên mẫu regex không bị đột biến; cái giữ mẫu regex đúng là `[Theory]` các hình dạng sai, không phải điểm số. Ý nghĩa chính xác của lý do "Removed by block already covered filter" ⚠ chưa đọc trong tài liệu Stryker, chỉ hiểu theo tên.
**Tiêu chí đạt:** điểm trước và sau được ghi lại, và bạn chỉ ra được mutant nào dẫn tới test nào.
**Dọn dẹp:** `rm -rf lh-proj` (gồm thư mục `StrykerOutput`).

## L5 Troubleshooting — Vướng khi làm dự án

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| Không tìm được yêu cầu để TDD cho board-service | đọc `Program.cs` và `document/spec/` | board-service chưa có domain, chưa có spec | chọn lát có spec (đã chuyển sang identity-service) |
| Test mới xanh ngay, không thấy đỏ | chạy test trước khi viết code | code đang có đã đủ để test xanh | ghi lại, giữ như đặc tả; đỏ giả bằng cách phá code (6.2 bước 4) |
| Điểm mutation 100% nhưng nghi chưa đủ | đọc danh sách mutant trong JSON | thuộc tính (regex) không bị đột biến | giữ test hình dạng sai; không tin riêng con số |
| Có mutant `NoCoverage` | đọc mutant và tìm test thiếu | không test nào chạy tới nhánh đó | thêm test cho đầu vào chạm nhánh |
| Test dùng lớp `internal` không biên dịch | đọc lỗi biên dịch | lớp không public | `InternalsVisibleTo` tới project test ⚠ chưa tái hiện lỗi |
| Kịch bản BDD cần Docker không chạy | `docker info` | Docker Desktop chưa chạy | bật Docker; chạy nhóm nhanh bằng `Category!=docker` trước |

**Thẻ 1: chu kỳ 5 chỉ đỏ sáu trên tám.** Hai đầu vào `""` và `"   "` xanh ngay vì code ở chu kỳ 2 kiểm `Contains('@')` và trả `false` cho chúng. Đó là hiện tượng bình thường của TDD: test mới có thể đã đúng; việc cần làm là xác nhận nó **sẽ đỏ nếu code sai** (xem mutation ở dự án 6.3: mutant ở nhánh chuỗi rỗng chính là chỗ bốn test đầu không phủ).
**Thẻ 2: kịch bản xanh ngay ở dự án 6.2.** Kịch bản mới xanh ngay không có nghĩa là vô ích: nó biến một quy tắc ngầm của code thành đặc tả thực thi. Bước 4 (phá `Trim()`) chứng minh nó đỏ khi hành vi mất: `Expected: 409 / Actual: 400`.

## Tự kiểm tra

1. Vì sao dự án 6.1 không làm trên board-service? <details><summary>Đáp án</summary>Chưa có domain và spec cho Board; TDD cần yêu cầu có nguồn, không bịa hành vi.</details>
2. Test chu kỳ 5 có hai đầu vào xanh ngay. Có phải lỗi quy trình TDD không? <details><summary>Đáp án</summary>Không; đó là test đã đúng với code hiện có. Cần chắc nó sẽ đỏ nếu code sai, ví dụ qua mutation.</details>
3. Vì sao điểm mutation 88,89% thành 100% khi thêm một `[Theory]`? <details><summary>Đáp án</summary>Mutant ở nhánh chuỗi rỗng trước đó không được test nào phủ; Theory có đầu vào `""` và `"   "` diệt nó.</details>
4. Vì sao kịch bản S12 tốn 6 đến 8 giây trong khi 12 test đơn vị chỉ khoảng 70 ms, và nên đặt gì ở đâu? <details><summary>Đáp án</summary>Kịch bản chạy qua Docker và ứng dụng thật; để ví dụ nghiệp vụ ở BDD, các hình dạng sai ở test đơn vị.</details>
5. Điểm 100% có chứng minh regex đúng không? <details><summary>Đáp án</summary>Không; mẫu regex trong thuộc tính không bị đột biến; test hình dạng sai mới giữ nó.</details>
6. Việc chạy được: thêm test `"user@example.com."` (dấu chấm cuối). Dự đoán kết quả trên code thật trước khi chạy, rồi chạy và ghi lại. Tiêu chí: dự đoán và kết quả được viết ra, và nếu lệch thì giải thích vì sao.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- Code và tài liệu trong repo: `src/identity-service/Features/Register/EmailAddress.cs`, `src/board-service/Program.cs`, `document/spec/identity-auth.md` (BR-001, FR-001), `document/spec/features/identity-service/register.feature`, `login.feature`.
- Kết quả ba dự án trong bài này, chạy 2026-11-26 (dự án 6.2 chạy với Docker 29.5.3 trên bản sao của commit hiện tại).

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- Cách dùng Stryker.NET (`--test-case-filter`, trạng thái mutant): đã dùng ở [Part 4](part-4-advanced.md), nguồn đã liệt kê ở đó; tài liệu về bộ lọc "block already covered" chưa mở.

**Chưa kiểm hoặc chưa tìm được:**
- Part này không có mục real-world riêng (Part dự án, không dạy khái niệm mới); các case thực tế nằm ở Part 1, 3, 4, 5.
- Dự án 6.1 chỉ chạy một chuỗi chu kỳ do tôi viết; chưa có đối chứng độc lập (người khác làm TDD cùng yêu cầu).
- Không chạy mutation lên `LoginLockoutService` (cần PostgreSQL thật, đường đi khác) và không có dự án nào cho `board-service` vì chưa có spec domain.
- Kịch bản S12 chưa được đưa vào repo và chưa qua `test-review.md`.
- Lỗi biên dịch khi test dùng lớp `internal` không có `InternalsVisibleTo`: chưa tái hiện, chỉ là kiến thức chung.
