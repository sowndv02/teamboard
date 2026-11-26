# Part 2 — Core Engineering Skills (Testing)

> [!NOTE]
> **Mục tiêu:** làm được TDD hằng ngày trên C#: tham số hoá test, đặt tên, dựng dữ liệu test, thay đồng hồ bằng test double, viết Gherkin tốt và nối vào code bằng Reqnroll.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md); `dotnet test` chạy được.
> **Thời lượng:** ~5 đến 7 giờ nếu làm đủ lab · **Đầu ra kiểm được:** một bộ test xUnit và một feature Reqnroll cho cùng một luật, cả hai xanh, kèm một lần refactor mà test giữ nguyên.
> **Part trước / sau:** [←](part-1-fundamentals.md) · [→](part-3-multi-component.md)

## L1 What — Bộ công cụ kỹ năng

#### xUnit: `[Fact]` và `[Theory]`
**Là gì:** `[Fact]` là test không tham số; `[Theory]` là một test chạy nhiều lần với bộ dữ liệu khác nhau (`[InlineData]`). Microsoft Learn dùng đúng cách này: Theory thay cho nhiều lời gọi Act trong một test [3].
**Ví dụ đời thường:** một phiếu kiểm tra, điền nhiều dòng vào cùng bảng.
**Use case:**
- Bảng chuyển trạng thái Task (6 cặp từ-đến) → một Theory, mỗi cặp là một dòng `[InlineData]`.
- Một quy tắc đơn lẻ ("task Done thì không quá hạn") → một `[Fact]`.

**Lưu ý:** mỗi dòng dữ liệu hiện thành một test riêng có tham số trong tên, nên đỏ ở dòng nào thấy ngay.

#### Đặt tên test: `Method_Scenario_Expected`
**Là gì:** tên có ba phần: hàm được kiểm, tình huống, kết quả mong đợi. Ví dụ `MoveTo_ForbiddenTransition_ThrowsAndKeepsStatus` [3].
**Ví dụ đời thường:** nhãn hộp hồ sơ: tên hồ sơ, hoàn cảnh, kết luận.
**Use case:**
- Test đỏ trên CI → đọc tên là biết hành vi nào hỏng, chưa cần mở code.
- Đồng bộ với quy tắc dự án `Method_Condition_ExpectedResult` (`.agents/rules/testing.md`).

#### Assertion: `Assert` thuần và thư viện
**Là gì:** câu lệnh kiểm kết quả. xUnit có sẵn `Assert`; các thư viện cho cú pháp dễ đọc hơn.
**Use case:** so sánh nhiều thuộc tính hoặc collection phức tạp → thư viện assertion cho thông báo lỗi rõ hơn.

| Thư viện | Giấy phép (theo trang đã đọc) | Lưu ý |
|---|---|---|
| FluentAssertions | v7 mã nguồn mở; từ v8 dùng thương mại cần trả phí | v7 vẫn nhận sửa lỗi |
| AwesomeAssertions | Apache 2.0 | fork cộng đồng của FluentAssertions |
| Shouldly | BSD 3-Clause | cú pháp `.ShouldBe()` |

⚠ Điều khoản FluentAssertions chỉ đọc qua tóm tắt trang chủ, chưa đọc văn bản giấy phép; kiểm lại trước khi dùng cho dự án thương mại [12]. Dự án hiện dùng `Assert` thuần; thêm thư viện mới đi qua `dependency-policy.md`.

#### Test data builder
**Là gì:** lớp nhỏ dựng đối tượng test với giá trị mặc định hợp lý, mỗi test chỉ chỉnh phần mình quan tâm. ⚠ Chưa kiểm: bài gốc của Nat Pryce không mở được; mô tả dưới đây là kỹ thuật phổ biến, không trích nguồn.
**Ví dụ đời thường:** đặt món có combo mặc định, chỉ đổi đồ uống.
**Use case:**
- Test "quá hạn" chỉ quan tâm `DueAt` và trạng thái → builder lo phần còn lại.
- Thêm một thuộc tính bắt buộc vào `BoardTask` → sửa builder một chỗ thay vì mọi test.

**Lưu ý:** Microsoft Learn khuyên dùng đầu vào tối thiểu, không đặt thuộc tính thừa [3].

#### Test double trong C#: stub và fake
**Là gì:** theo Microsoft Learn, stub là vật thay thế điều khiển được và **không** assert vào nó; mock là fake bạn assert vào. Gọi nhầm stub là mock làm người đọc hiểu sai [3]. ⚠ Trang ghi chú cách dùng này hẹp hơn định nghĩa Meszaros mà [Part 1](part-1-fundamentals.md) dùng.
**Use case:**
- `DateTime.UtcNow` làm test lúc xanh lúc đỏ → bọc sau interface `IClock`, test dùng `FixedClock` (một "seam", điểm tách để thay thế) [3].
- Thành phần cần `IMeterFactory` → một fake trong chính file test, như `TestMeterFactory` trong `src/identity-service.Tests/PasswordHasherTests.cs`.

#### Gherkin và step definition (Reqnroll)
**Là gì:** `.feature` mô tả hành vi bằng `Feature`, `Rule`, `Background`, `Scenario`, `Scenario Outline` + `Examples`; mỗi câu `Given/When/Then` nối với một **step definition** (hàm C# gắn `[Given]`, `[When]`, `[Then]`) trong lớp `[Binding]` [4][7].
**Use case:**
- Cùng một bước dùng ở nhiều scenario → một step definition có tham số `{string}`, không viết lại mỗi lần [4].
- Nhiều dòng dữ liệu cho cùng một kịch bản → `Scenario Outline` với bảng `Examples` [7].

**Lưu ý:** bảng chi tiết cú pháp xem tài liệu Reqnroll; Part này chỉ giữ phần cần để viết và gỡ lỗi.

## L2 Why — Vì sao test dễ đọc quan trọng

- **Test là tài liệu sống.** Khi test đỏ, tên và nội dung test là thứ đầu tiên người ta đọc. Test khó đọc nghĩa là sửa chậm.
- **Test giòn bám cài đặt.** Microsoft Learn nêu nguyên nhân phổ biến: assert quá chặt vào kết quả và dùng mock sai chỗ; cách chữa là coi test như code production [3].
- **Logic trong test sinh bug trong test.** Test có `if`, `for`, nối chuỗi thì tự nó cần được kiểm; thay bằng Theory với dữ liệu cố định [3].
- **Gherkin tệ thành gánh nặng.** Scenario viết theo từng cú nhấp chuột phải sửa mỗi khi giao diện đổi; người nghiệp vụ không đọc, chỉ dev gánh phí bảo trì [9].

## L3 How — Cách một test tốt được dựng

#### Cô lập test: một instance mỗi test
**Ai làm gì:** xUnit tạo một **instance mới của lớp test cho mỗi test**, nên trường (field) thường không rò rỉ giữa các test; code trong constructor chạy trước mỗi test, `Dispose` dọn sau [1]. Muốn chia sẻ có kiểm soát: class fixture (một đối tượng cho cả lớp), collection fixture (cho nhiều lớp) [1].
- **Mặc định song song:** mỗi lớp test là một collection; các lớp khác nhau có thể chạy song song, test trong một lớp chạy tuần tự [2]. Hai lớp dùng chung tài nguyên (file, cổng) thì cho cùng `[Collection]`.
- **Ưu:** test độc lập mặc định. **Nhược:** trường `static` thoát khỏi cơ chế này và gây phụ thuộc thứ tự (xem L5).
- **Dùng khi:** luôn. Nguồn: xUnit docs (Chắc cho [1]; Vừa cho [2]).

#### Một Act mỗi test, tham số hoá bằng Theory
**Ai làm gì:** mỗi test có một lời gọi hành vi; nhiều tình huống thì dùng Theory thay vì nhiều Act, vì assert fail làm các assert sau không chạy [3].
- **Ưu:** lỗi chỉ đúng chỗ; thêm tình huống bằng một dòng dữ liệu. **Nhược:** bộ dữ liệu lớn khó đọc.
- **Dùng khi:** cùng hành vi, nhiều đầu vào. **Không dùng khi:** mỗi dòng cần setup khác hẳn nhau.
- Nguồn: Microsoft Learn (Vừa).

#### Dựng dữ liệu bằng builder
**Ai làm gì:** builder có sẵn giá trị mặc định; test gọi `.InStatus(...)`, `.DueAt(...)` rồi `.Build()`. ⚠ Chưa kiểm nguồn (xem L1).
- **Ưu:** test chỉ nêu điều liên quan, đổi cấu trúc đối tượng chỉ sửa một chỗ. **Nhược:** thêm một lớp phải bảo trì; builder phức tạp che mất ý test.
- **Dùng khi:** đối tượng có nhiều trường, nhiều test dựng gần giống nhau. **Không dùng khi:** đối tượng 1–2 trường, `new` thẳng là đủ.

#### Seam đồng hồ
**Ai làm gì:** bọc phụ thuộc không xác định (giờ, ngẫu nhiên, ID) sau interface; production truyền bản thật, test truyền bản cố định [3].
- **Ưu:** test lặp lại được (chữ R trong F.I.R.S.T). **Nhược:** thêm interface và tham số.
- **Dùng khi:** hành vi phụ thuộc thời điểm hiện tại.

#### Gherkin: khai báo thay vì mệnh lệnh
**Ai làm gì:** scenario nói **điều gì xảy ra** ("When I move the task to Done"), chi tiết cách làm đặt trong step definition. Phép thử của Cucumber: nếu cài đặt đổi mà câu chữ phải đổi, nâng mức trừu tượng lên [9]. Cucumber cũng nêu hai anti-pattern: step definition gắn riêng từng feature (không dùng lại được) và **câu ghép** gộp nhiều hành động trong một step [10].
- **Ưu:** scenario sống lâu, ít sửa. **Nhược:** step definition phải làm nhiều việc hơn.
- **Dùng khi:** luôn. Nguồn: Cucumber Better Gherkin và anti-patterns (Vừa). Cucumber không nêu con số bước tối đa mỗi scenario [9].

#### Reqnroll: nối kịch bản với code
**Ai làm gì:** Reqnroll đọc `.feature`, sinh test xUnit và gọi step definition khớp theo câu chữ [4][8]. Điểm cần nhớ:
- Bindings (step, hook) **toàn cục** trong project; khớp bằng Cucumber expression (`{string}`, `{int}`) hoặc regex [4].
- Dữ liệu dùng chung giữa các binding đi qua **context injection**: lớp thường nhận qua constructor, mỗi scenario một instance, tự `Dispose` sau scenario [5].
- Hook (`[BeforeScenario]`...) cùng loại chạy theo thứ tự không xác định trừ khi đặt `Order`; Before thất bại thì các bước bị bỏ qua và After vẫn chạy, nên kiểm tài nguyên đã khởi tạo trước khi dọn [6].
- Reqnroll là bản fork và khởi động lại của SpecFlow từ đầu 2024; Tricentis thông báo ngừng SpecFlow cuối 2024 [11].
- **Ưu:** spec và test là một file. **Nhược:** thêm tầng glue code; câu chữ lệch một dấu là step không khớp.
- **Nguồn:** step definitions (Chắc); các trang còn lại (Vừa). Trang tích hợp xUnit không nói một scenario thành đúng một test hay không, nên điều này chỉ rút từ kết quả chạy ở Lab 2.3 [8].

#### Refactor dưới lưới test
**Ai làm gì:** khi test xanh, đổi cấu trúc code (không đổi hành vi) rồi chạy lại; test vẫn xanh nghĩa là hành vi giữ nguyên. Bước này hay bị bỏ qua nhất trong TDD [Part 1](part-1-fundamentals.md#red-green-refactor-quy-trình-tdd-của-beck).
- **Ưu:** sửa mạnh tay mà vẫn chắc. **Nhược:** lưới thủng thì xanh vẫn không bảo đảm (xem mutation testing, Part 4).

## L4 Hands-on — Kata và lab

Môi trường: Windows 11, .NET SDK 10.0.402, `xunit` 2.9.3, `Reqnroll.xUnit` 3.3.4, `Microsoft.NET.Test.Sdk` 18.11.0, `xunit.runner.visualstudio` 3.1.5 (cùng phiên bản với `src/board-service.Tests`); chạy ngày 2026-11-26. Cả ba lab dùng một project `lh-skills`.

#### Lab 2.0 — Dựng project
```bash
mkdir lh-skills && cd lh-skills
dotnet new xunit -n LhSkills -o . && rm UnitTest1.cs
dotnet add package Reqnroll.xUnit --version 3.3.4
```
⚠ Lệnh `dotnet add package` chưa chạy nguyên văn; project mẫu được ghim phiên bản bằng cách sửa trực tiếp `LhSkills.csproj`.
Tạo `BoardTask.cs`: `enum TaskStatus { Todo, InProgress, Done }`, `interface IClock { DateTimeOffset UtcNow { get; } }`, và lớp `BoardTask` với `Title`, `DueAt`, `Status`, `MoveTo(TaskStatus)` ném `InvalidOperationException` khi chuyển không hợp lệ, `IsOverdue(IClock)` (quá hạn khi chưa Done và đã qua `DueAt`). Luật cho phép: `Todo→InProgress`, `InProgress→Done`, `InProgress→Todo`.

#### Lab 2.1 — Theory và đặt tên
**Mục tiêu:** một test bao nhiều cặp chuyển trạng thái, tên nói rõ hành vi.
```csharp
[Theory]
[InlineData(TaskStatus.Todo, TaskStatus.InProgress)]
[InlineData(TaskStatus.InProgress, TaskStatus.Done)]
[InlineData(TaskStatus.InProgress, TaskStatus.Todo)]
public void MoveTo_AllowedTransition_ChangesStatus(TaskStatus from, TaskStatus to)
{
    var task = new BoardTaskBuilder().InStatus(from).Build();

    task.MoveTo(to);

    Assert.Equal(to, task.Status);
}
```
Thêm Theory thứ hai `MoveTo_ForbiddenTransition_ThrowsAndKeepsStatus` cho ba cặp bị cấm (`Todo→Done`, `Done→Todo`, `Done→InProgress`). `BoardTaskBuilder` ở Lab 2.2.
```bash
dotnet test --logger "console;verbosity=normal"
```
**Đầu ra thật** (một phần):
```text
  Passed LhSkills.BoardTaskTests.MoveTo_AllowedTransition_ChangesStatus(from: InProgress, to: Done) [7 ms]
  Passed LhSkills.BoardTaskTests.MoveTo_ForbiddenTransition_ThrowsAndKeepsStatus(from: Todo, to: Done) [1 ms]
```
**Giải thích:** mỗi dòng dữ liệu hiện thành một test, tham số nằm trong tên.
**Refactor:** thay `switch` trong `MoveTo` bằng bảng `HashSet<(TaskStatus From, TaskStatus To)>` các chuyển được phép, rồi chạy lại:
```text
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 77 ms - LhSkills.dll (net10.0)
```
(11 gồm 6 dòng Theory, 2 test quá hạn của Lab 2.2 và 3 scenario của Lab 2.3.)
**Dọn dẹp:** không có.

#### Lab 2.2 — Builder và đồng hồ cố định
**Mục tiêu:** test "quá hạn" không phụ thuộc giờ thật và không dựng thừa dữ liệu.
```csharp
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}

public sealed class BoardTaskBuilder
{
    private string _title = "Write report";
    private DateTimeOffset? _dueAt;
    private TaskStatus _status = TaskStatus.Todo;

    public BoardTaskBuilder Titled(string title) { _title = title; return this; }
    public BoardTaskBuilder DueAt(DateTimeOffset dueAt) { _dueAt = dueAt; return this; }
    public BoardTaskBuilder InStatus(TaskStatus status) { _status = status; return this; }

    public BoardTask Build()
    {
        var task = new BoardTask(_title, _dueAt);
        if (_status is TaskStatus.InProgress or TaskStatus.Done) task.MoveTo(TaskStatus.InProgress);
        if (_status == TaskStatus.Done) task.MoveTo(TaskStatus.Done);
        return task;
    }
}
```
```csharp
[Fact]
public void IsOverdue_PastDueButDone_ReturnsFalse()
{
    var task = new BoardTaskBuilder().DueAt(Now.AddHours(-1)).InStatus(TaskStatus.Done).Build();

    Assert.False(task.IsOverdue(new FixedClock(Now)));
}
```
(cùng test `IsOverdue_PastDueAndNotDone_ReturnsTrue`; `Now` là `static readonly` 2026-11-26 09:00 UTC.)
**Đầu ra thật:** `Passed: 8, Total: 8` trước khi thêm Reqnroll (6 dòng Theory và 2 test quá hạn).
**Giải thích:** `FixedClock` thay `DateTime.UtcNow`, nên kết quả không đổi theo giờ chạy.
**Dọn dẹp:** không có.

#### Lab 2.3 — Cùng luật bằng Reqnroll
**Mục tiêu:** viết feature khai báo, nối bằng step definition.
`Features/TaskLifecycle.feature`:
```gherkin
Feature: Task lifecycle
  Background:
    Given a task titled "Write report"

  Rule: A task can only move along allowed transitions

    Scenario Outline: Allowed transitions
      Given the task is in status "<from>"
      When I move the task to "<to>"
      Then the task is in status "<to>"

      Examples:
        | from       | to         |
        | Todo       | InProgress |
        | InProgress | Done       |

    Scenario: Completing a task that was never started is rejected
      Given the task is in status "Todo"
      When I move the task to "Done"
      Then the move is rejected
      And the task is in status "Todo"
```
`TaskLifecycleSteps.cs`: lớp `[Binding]` giữ `_builder`, `_task`, `_error`; `[Given("a task titled {string}")]` đặt builder; `[Given("the task is in status {string}")]` dựng task qua `InStatus(Enum.Parse<TaskStatus>(status))`; `[When("I move the task to {string}")]` bọc `MoveTo` trong `try/catch (InvalidOperationException)` lưu lỗi; `[Then("the task is in status {string}")]` assert trạng thái; `[Then("the move is rejected")]` assert `_error` khác null.
```bash
dotnet test --logger "console;verbosity=normal"
```
**Đầu ra thật** (một phần):
```text
  Passed Allowed transitions(from: "Todo", to: "InProgress", __pickleIndex: "0", exampleTags: []) [1 ms]
  Passed Allowed transitions(from: "InProgress", to: "Done", __pickleIndex: "1", exampleTags: []) [48 ms]
  Passed Completing a task that was never started is rejected [2 ms]
```
**Giải thích:** hai dòng `Examples` thành hai test, scenario thường là một test; bản mẫu lặp một step `{string}` cho mọi trạng thái.
**Dọn dẹp:** `cd .. && rm -rf lh-skills` (chỉ thư mục bạn vừa tạo).

#### Lab 2.4 — Đọc binding thật của dự án
**Mục tiêu:** nhận ra các kỹ thuật trên trong code thật.
```bash
sed -n 1,30p src/board-service.Tests/HealthSteps.cs
sed -n 1,25p src/identity-service.Tests/PasswordHasherTests.cs
```
**Tiêu chí đạt:** chỉ ra được (a) lớp `[Binding]` giữ state ở field và `Dispose` tài nguyên (`WebApplicationFactory`), (b) tên test theo `Method_Condition_ExpectedResult`, (c) fake `TestMeterFactory` đặt ngay trong file test. ⚠ chưa chạy, là bài đọc code.

## L5 Troubleshooting — Test đỏ vì sai chỗ

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| Test đạt khi chạy riêng, đỏ khi chạy cả lớp | chạy với `--filter "FullyQualifiedName~Tên"` rồi bỏ filter | state `static` dùng chung | bỏ `static`, dựng mới trong constructor |
| Scenario báo pending "No matching step definition" | `dotnet test --logger "console;verbosity=detailed"` | câu chữ step không khớp binding nào | thêm binding hoặc sửa câu cho khớp |
| Đổi cài đặt nhỏ, hàng loạt test đỏ | đọc assert của vài test đỏ | assert vào cách làm (lời gọi, chuỗi lỗi) | assert hành vi quan sát được |
| Test lúc xanh lúc đỏ theo giờ | tìm `DateTime.Now`, `UtcNow`, `Random` trong code được test | phụ thuộc đồng hồ, ngẫu nhiên thật | seam `IClock` + `FixedClock` |
| Hai lớp test tranh cổng hoặc file | đọc log, thử chạy từng lớp | xUnit chạy song song các lớp mặc định | cùng `[Collection]` hoặc tắt song song [2] |

**Thẻ 1: phụ thuộc thứ tự do `static`.** Hai test cùng thêm vào một `static readonly List<string>` rồi `Assert.Single`. Chạy cả lớp:
```text
  Failed LhSkills.SharedStateTests.AddingAnotherTask_ListHasOneItem [2 ms]
   Assert.Single() Failure: The collection contained 2 items
Failed!  - Failed:     1, Passed:     1, Skipped:     0, Total:     2
```
Chạy riêng `AddingAnotherTask`: `Passed: 1`. Nguyên nhân: xUnit tạo instance mới mỗi test [1], nhưng `static` thuộc về kiểu, không thuộc instance, nên test thứ hai thấy dữ liệu của test thứ nhất. Cách sửa: đổi thành field thường.

**Thẻ 2: step chưa có binding.** Thêm scenario có `When I archive the task` mà không có step definition. Kết quả thật:
```text
Reqnroll.xUnit.ReqnrollPlugin.XUnitPendingStepException : Test pending: No matching step definition found for one or more steps.
```
Reqnroll in luôn mẫu binding gợi ý (`[When("I archive the task")]`). Các step trước đó vẫn chạy (`-> done: ...GivenTitled`).
Đây là kết quả chạy thật; các cách sửa còn lại trong bảng dựa trên nguồn đã liệt kê, chưa tái hiện từng cái.

## Tự kiểm tra

1. Vì sao `static` list trong lớp test gây lỗi dù xUnit tạo instance mới cho mỗi test? <details><summary>Đáp án</summary>`static` thuộc kiểu chứ không thuộc instance; mọi instance dùng chung một bản, nên dữ liệu rò rỉ giữa các test.</details>
2. Khi nào dùng `[Theory]` thay vì nhiều `[Fact]`? <details><summary>Đáp án</summary>Cùng một hành vi với nhiều đầu vào; mỗi dòng dữ liệu là một test riêng và assert fail ở dòng này không che dòng khác.</details>
3. Sửa câu "When I click the Complete button and wait 2 seconds" thành khai báo. <details><summary>Đáp án</summary>"When I complete the task": bỏ chi tiết giao diện và thời gian, đặt trong step definition.</details>
4. Vì sao bọc `DateTime.UtcNow` trong `IClock`? <details><summary>Đáp án</summary>Để test điều khiển được thời điểm; không thì kết quả phụ thuộc giờ chạy, vi phạm tính lặp lại.</details>
5. Việc chạy được: thêm trạng thái `Blocked` (cho phép `InProgress→Blocked` và `Blocked→InProgress`). Tiêu chí: viết dòng Theory đỏ trước, xanh sau, các test cũ vẫn xanh, thêm một scenario khai báo trong feature.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [1] [xUnit.net, Sharing Context between Tests](https://xunit.net/docs/shared-context): mỗi test một instance, constructor/`Dispose`, hạn chế `IAsyncLifetime`.
- [4] [Reqnroll, Step Definitions](https://docs.reqnroll.net/latest/automation/step-definitions.html): `[Binding]`, Cucumber expression, regex, bindings toàn cục.
- Repo: `src/board-service.Tests/HealthSteps.cs`, `AuthMeSteps.cs`, `src/identity-service.Tests/PasswordHasherTests.cs`, `src/board-service.Tests/board-service.Tests.csproj`, và kết quả các lab ở trên.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- [2] [xUnit.net, Running Tests in Parallel](https://xunit.net/docs/running-tests-in-parallel)
- [3] [Microsoft Learn, Unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) (ms.date 2026-11-05 theo agent)
- [5] [Reqnroll, Context Injection](https://docs.reqnroll.net/latest/automation/context-injection.html); [6] [Hooks](https://docs.reqnroll.net/latest/automation/hooks.html); [7] [Gherkin reference](https://docs.reqnroll.net/latest/gherkin/gherkin-reference.html); [8] [tích hợp xUnit](https://docs.reqnroll.net/latest/integrations/xunit.html)
- [9] [Cucumber, Better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/); [10] [Cucumber, Anti-patterns](https://cucumber.io/docs/guides/anti-patterns/) (agent nói tóm tắt chỉ liệt kê hai anti-pattern, có thể sót)
- [11] [Reqnroll, SpecFlow end of life](https://reqnroll.net/news/2025/01/specflow-end-of-life-has-been-announced/) (2025)
- [12] giấy phép: [AwesomeAssertions](https://github.com/AwesomeAssertions/AwesomeAssertions), [FluentAssertions](https://fluentassertions.com/), [Shouldly LICENSE](https://raw.githubusercontent.com/shouldly/shouldly/master/LICENSE.txt)

**Chưa kiểm hoặc chưa tìm được:**
- Test data builder: bài gốc của Nat Pryce không mở được; phần này không có nguồn.
- `[MemberData]` và bảng so sánh của xUnit: trang `comparisons` trả về trống.
- Step argument transformations (`[StepArgumentTransformation]`): chưa đọc.
- Tài liệu Reqnroll không xác nhận một scenario thành đúng một test; chỉ quan sát từ Lab 2.3.
- Hướng dẫn số bước mỗi scenario từ Dan North, Gojko Adzic, Seb Rose, Liz Keogh: chưa mở được bài gốc nào.
- Cuộc tranh luận mocking library (Moq, NSubstitute) không đưa vào vì chưa có nguồn mở được.
- Khả năng chạy của Reqnroll 3.3.4 trên .NET 10: đã chạy được ở các lab trên, chưa kiểm tài liệu tương thích chính thức.
- Case real-world cho Part này: chưa có; real-world thuộc Part 1 và Part 7.
