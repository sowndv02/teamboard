# Part 1 — Fundamentals & Mental Models (Testing)

> [!NOTE]
> **Mục tiêu:** nắm mô hình tư duy về kiểm thử tự động; phân biệt TDD, BDD, ATDD; biết các lựa chọn thay thế và cái giá của từng cái.
> **Cần biết trước:** [Part 0](README.md); đọc được C# cơ bản.
> **Thời lượng:** ~4 đến 6 giờ nếu làm đủ lab · **Đầu ra kiểm được:** giải thích Red-Green-Refactor và Given-When-Then bằng ví dụ Board, chọn được kỹ thuật cho một tình huống cho trước.
> **Part trước / sau:** [←](README.md) · [→](part-2-core-skills.md)

## L1 What — Test, test double, TDD, BDD, ATDD

#### Test tự động
**Là gì:** đoạn code chạy code khác, tự báo đạt hoặc không đạt, không cần người đọc kết quả.
**Ví dụ đời thường:** công tắc báo khói: không cần ai ngửi mới biết có cháy.
**Use case:**
- Sửa logic chuyển trạng thái Task → chạy lại vài giây để biết có làm hỏng chỗ khác không.
- Đổi thư viện hoặc nâng .NET → test là bằng chứng hành vi không đổi.
```bash
dotnet test src/board-service.Tests --filter "Category!=docker"
```
**Lưu ý:** test tự động chỉ kiểm điều đã được viết ra; nó không thay thế khám phá thủ công.

#### Arrange-Act-Assert và Given-When-Then
**Là gì:** hai tên của cùng một khung ba phần: dựng bối cảnh (Arrange/Given), thực hiện hành vi (Act/When), kiểm kết quả (Assert/Then).
**Ví dụ đời thường:** công thức nấu ăn: nguyên liệu, nấu, nếm thử.
**Use case:**
- Viết unit test C# → Arrange/Act/Assert, ba khối cách nhau một dòng trống.
- Viết kịch bản cho người không lập trình đọc → Given/When/Then trong file `.feature`.

**Lưu ý:** Fowler mô tả Given-When-Then giống Four-Phase Test và Arrange-Act-Assert, thường bỏ bước dọn dẹp [1].

#### F.I.R.S.T
**Là gì:** năm đặc tính của test tốt: **F**ast (nhanh), **I**ndependent (độc lập), **R**epeatable (lặp lại cùng kết quả ở mọi môi trường), **S**elf-validating (tự báo đạt/không), **T**imely (viết kịp thời, sát code). ⚠ Nguồn chỉ thứ cấp; chữ T cũng được nhiều bài gọi là Thorough.
**Ví dụ đời thường:** bài kiểm tra nhanh, không chép của nhau, ai chấm cũng ra cùng điểm.
**Use case:**
- Test chậm vì gọi mạng ngoài → vi phạm F và R, thay bằng fake.
- Test chạy đúng khi chạy riêng, sai khi chạy cả bộ → vi phạm I.

#### Test pyramid, trophy, honeycomb
**Là gì:** ba hình dung về tỷ lệ các mức test. Pyramid: nhiều unit, ít UI/end-to-end. Trophy: nhấn mạnh test tích hợp. Honeycomb (Spotify): nhiều integration, ít test chi tiết cài đặt, càng ít test "integrated" càng tốt.
**Ví dụ đời thường:** mua bảo hiểm: nhiều gói rẻ ở dưới, ít gói đắt ở trên.
**Use case:**
- Monolith với logic nghiệp vụ dày → pyramid là điểm xuất phát hợp lý.
- Microservice nhỏ, logic mỏng, phần khó nằm ở tương tác → honeycomb [4].

**Lưu ý:** các tài liệu dùng "unit", "integration", "end-to-end" không thống nhất; chốt định nghĩa trong nhóm [2].

#### Test double
**Là gì:** vật thay thế một thành phần thật trong test. Năm loại theo Meszaros, Fowler dùng lại [3].

| Loại | Làm gì | Use case (Board) |
|---|---|---|
| Dummy | điền chỗ tham số, không bao giờ được dùng | truyền `ILogger` vào hàm không log |
| Stub | trả đáp án dựng sẵn | `IClock` trả giờ cố định |
| Fake | cài đặt chạy được nhưng rút gọn | repository lưu trong bộ nhớ |
| Spy | stub, thêm ghi lại cách bị gọi | đếm số lần gửi event |
| Mock | dựng sẵn kỳ vọng về lời gọi, tự kiểm | kiểm đúng một lần gọi `Publish` |

**Lưu ý:** mock gắn test với cách cài đặt; dự án cấm mock DB để "cho qua" (`.agents/rules/testing.md`).

#### TDD (Test-Driven Development)
**Là gì:** viết một test fail trước, rồi viết code vừa đủ cho nó pass, rồi dọn code; lặp lại từng nhịp nhỏ.
**Ví dụ đời thường:** vạch dấu cắt trước khi cưa.
**Use case:**
- Thêm luật "không Complete task đang Todo" → test từ chối trước, code sau.
- Sửa bug → test tái hiện bug (đỏ) trước, rồi sửa.

**Lưu ý:** TDD là kỹ thuật phát triển, chưa bảo đảm test hay thiết kế tốt; bước refactor hay bị bỏ qua [5].

#### BDD (Behaviour-Driven Development)
**Là gì:** cách làm để cả nhóm hiểu chung hành vi mong muốn bằng ví dụ cụ thể, rồi biến ví dụ thành kịch bản chạy được. North đặt ra BDD khi thấy người học TDD vướng ở "bắt đầu từ đâu, test gì, đặt tên thế nào" [6].
**Ví dụ đời thường:** thống nhất bằng một tình huống mẫu trước khi xây.
**Use case:**
- Yêu cầu "khoá tài khoản sau nhiều lần sai mật khẩu" mơ hồ → họ ví dụ cụ thể bằng Given/When/Then.
- Cần tài liệu sống khớp với code → `.feature` chạy được trong CI.

**Lưu ý:** BDD không phải công cụ; dùng như thuần kỹ thuật test thì mất ý nghĩa chính, là cuộc trò chuyện chung [7].

#### ATDD và Specification by Example
**Là gì:** ATDD (Acceptance TDD): khách, dev, tester thống nhất test chấp nhận trước khi xây. Specification by Example (cụm từ Fowler cho là ông đặt ra sau một workshop năm 2002): dùng ví dụ làm đặc tả [8][9].
**Ví dụ đời thường:** hợp đồng đính kèm bảng ví dụ tính tiền.
**Use case:**
- Quy tắc nghiệp vụ khó nói bằng lời → bảng ví dụ vào/ra.
- Nghiệm thu một lát (issue) → kịch bản `.feature` trong `document/spec/features/`.

**Lưu ý:** theo Fowler, test một mình chưa đủ; có thể bị qua mặt bằng hard-code, cần thêm biện pháp khác [9].

## L2 Why — Vì sao viết test trước và đặc tả bằng ví dụ

Ba tình huống đau khi chưa có:
- **Không dám sửa.** Lát cắt Task có luật chuyển trạng thái; ai đó thêm trạng thái `Blocked`, không ai biết luật cũ có còn đúng. Có test là có lưới an toàn.
- **Hiểu mỗi người một kiểu.** Spec ghi "khoá sau nhiều lần sai". Dev A hiểu 3 lần, dev B hiểu 5 lần, khách muốn 10. Một ví dụ cụ thể buộc cả ba nói ra con số.
- **Test viết sau dễ chỉ phản chiếu code.** Viết sau thì dễ viết đúng cái code đã có, kể cả khi code sai. ⚠ Kinh nghiệm chung, chưa gắn nguồn.

**Test viết trước ép nghĩ về cách dùng.** Viết test trước khiến người viết nghĩ về cách code được gọi, và giúp tách giao diện khỏi cài đặt [5]. Beck và các tác giả khác cũng coi lỗi mất thiết kế (hard-to-test) là tín hiệu để xem lại thiết kế [11].

**Bằng chứng công nghiệp:** nghiên cứu trên 3 nhóm Microsoft và 1 nhóm IBM cho thấy mật độ lỗi trước phát hành giảm 40% đến 90% so với dự án tương tự không dùng TDD, đổi lại nhóm tự ghi nhận thời gian phát triển ban đầu tăng 15% đến 35% [10]. Đây là case study, không phải thí nghiệm ngẫu nhiên: các nhóm tự chọn dùng TDD, so sánh với dự án "tương tự", nên không kết luận được TDD gây ra mức giảm đó.

## L3 How — Cơ chế từng kỹ thuật

#### Red-Green-Refactor (quy trình TDD của Beck)
**Ai làm gì, thứ tự nào** ("Canon TDD" của Kent Beck, 2023 [12]):
1. Lập **danh sách test** các tình huống hành vi cần có, gồm cả điều không được hỏng.
2. Chọn **đúng một** mục, viết thành test chạy được. Test **đỏ** (fail) vì đúng lý do.
3. Viết code để test này và các test trước pass (**xanh**). Phát hiện tình huống mới thì ghi vào danh sách.
4. **Refactor** nếu cần (đổi cấu trúc, không đổi hành vi).
5. Lặp tới khi danh sách rỗng.

```text
danh sách ──> chọn 1 ──> viết test ──> ĐỎ ──> code vừa đủ ──> XANH ──> refactor ──┐
     ^                                                                           │
     └───────────────────────── thêm tình huống mới ─────────────────────────────┘
```
- **Ưu:** vòng ngắn, lỗi lộ ngay; tránh làm quá yêu cầu.
- **Nhược:** cần kỷ luật; dễ bỏ refactor; dễ viết test bám cài đặt.
- **Dùng khi:** logic nghiệp vụ rõ, nhiều nhánh (trạng thái, tính tiền, validate). **Không dùng khi:** khám phá giao diện chưa rõ hình dạng, spike thử công nghệ (viết bỏ đi).
- **Nguồn:** Beck 2023 (Vừa); Fowler TDD bliki (Vừa).

Beck liệt kê các hiểu lầm: bỏ danh sách test, đổi hết danh sách thành test ngay từ đầu, viết test không có assert chỉ để lấy coverage, xoá assert để "xanh", refactor trong lúc đang cố làm xanh, trừu tượng hoá ngay khi thấy trùng lặp đầu tiên [12].

#### Ba luật của TDD (nano-cycle)
**Ai làm gì:** (1) không viết code production khi chưa có test fail; (2) không viết test nhiều hơn mức đủ để fail (lỗi biên dịch cũng tính là fail); (3) không viết code production nhiều hơn mức đủ để pass test đang fail. Chu kỳ chỉ kéo dài vài chục giây [13].
- **Ưu:** nhịp rất nhỏ, ít khi mất thời gian gỡ lỗi dài. **Nhược:** cảm giác máy móc; khó với thiết kế chưa rõ.
- **Dùng khi:** học TDD, kata. **Mức nguồn:** Vừa (bản kể lại 2014 của chính tác giả; trang gốc không mở được).

#### TDD kiểu Chicago (classical) và London (mockist)
**Ai làm gì:** hai phong cách khác nhau ở cách dùng test double [3].
- **Classical:** dùng đối tượng thật khi được; chỉ thay khi thật quá bất tiện (chậm, ngẫu nhiên). Kiểm bằng **trạng thái** cuối.
- **Mockist:** thay mọi cộng tác viên có hành vi đáng kể bằng mock; kiểm bằng **lời gọi** đã xảy ra (hành vi). Hợp lối thiết kế từ ngoài vào trong, theo nhu cầu.
- **Ưu/nhược:** classical gần với mini-integration test, ít gắn cài đặt; mockist cho phản hồi thiết kế sớm nhưng test gắn chặt cách cài đặt hơn [3].
- **Dùng khi:** classical cho domain model, logic thuần; mockist khi thiết kế từ API xuống tầng dưới. Fowler tự nhận là classicist [3].
- **Lưu ý:** tên "Chicago" và "London" là cách gọi phổ biến ⚠, trang Fowler không dùng hai tên này.

#### Given-When-Then và vòng Discovery, Formulation, Automation
**Ai làm gì:** Cucumber mô tả ba pha lặp lại [7]:
1. **Discovery** (hệ thống *có thể* làm gì): cả nhóm trao đổi bằng ví dụ cụ thể, lộ ra luật, chỗ hổng, phần có thể hoãn.
2. **Formulation** (*nên* làm gì): ví dụ đã thống nhất được viết theo dạng người và máy đều đọc được (Gherkin).
3. **Automation** (*thực sự* làm gì): mỗi ví dụ thành test, đỏ cho tới khi hành vi được xây xong.

```gherkin
Feature: Task lifecycle
  Scenario: Completing a task that was never started is rejected
    Given a task in status "Todo"
    When I complete the task
    Then the request is rejected
    And the task stays in status "Todo"
```
- **Ưu:** hiểu chung trước khi code; kịch bản là tài liệu sống. **Nhược:** tốn công phối hợp; "glue code" (step definition) phải bảo trì; sẽ vô nghĩa nếu chỉ dev đọc.
- **Dùng khi:** luật nghiệp vụ cần chốt với người không lập trình. **Không dùng khi:** chỉ có dev và logic thuần kỹ thuật (xUnit đủ).
- **Nguồn:** Cucumber docs (Vừa); North 2006 (Chắc). Gherkin có `Rule` (từ Gherkin 6), `Background`, `Scenario Outline` + `Examples`. Viết **khai báo** (nói hành vi) thay vì **mệnh lệnh** (bấm nút nào); phép thử: nếu cài đặt đổi, câu chữ có phải đổi không [7].

#### Example Mapping
**Ai làm gì:** buổi họp ngắn (khoảng 25 phút cho story đã rõ) dùng bốn màu thẻ: **vàng** story, **xanh dương** luật, **xanh lá** ví dụ dưới mỗi luật, **đỏ** câu hỏi chưa ai trả lời được [14].
- **Tín hiệu:** nhiều thẻ đỏ → còn quá nhiều ẩn số; nhiều thẻ xanh dương → nên tách story.
- **Ưu:** rẻ, lộ rủi ro sớm. **Nhược:** cần đủ vai (nghiệp vụ, dev, tester).
- **Mục tiêu** là hiểu chung, không phải viết Gherkin ngay trong buổi họp. **Nguồn:** Wynne 2015 (Vừa).

#### ATDD
**Ai làm gì:** khách, dev, tester thống nhất test chấp nhận **trước** khi xây (nhóm "three amigos"); test mô tả hành vi theo góc nhìn người dùng và đóng vai yêu cầu [8].
- **Ưu:** yêu cầu kiểm chứng được. **Nhược:** công cụ (Fit, Cucumber) dễ thế chỗ cuộc trò chuyện, vốn là mục đích thật [8].
- **Nguồn:** Agile Alliance (Vừa).

#### So sánh các cơ chế

| Cơ chế | Ai viết | Viết khi nào | Phản hồi chính |
|---|---|---|---|
| Red-Green-Refactor | dev | từng nhịp, ngay trước code | test đỏ → xanh |
| Ba luật TDD | dev | mỗi vài chục giây | lỗi lộ trong một nhịp |
| Chicago / London | dev | khi chọn test double | kiểm trạng thái / kiểm lời gọi |
| BDD (Given-When-Then) | cả nhóm | trước khi xây tính năng | hiểu chung, kịch bản chạy được |
| Example Mapping | cả nhóm | họp trước khi viết Gherkin | luật, ví dụ, câu hỏi mở |
| ATDD | khách + dev + tester | trước khi xây story | test chấp nhận |

**Cách kết hợp thường gặp:** ATDD/BDD ở ngoài (một kịch bản chấp nhận đỏ), TDD ở trong (nhiều vòng đỏ-xanh nhỏ để làm kịch bản đó xanh). Đây là mô hình dự án theo `.agents/rules/testing.md`: kịch bản `.feature` trước, test đỏ, rồi code.

## L4 Hands-on — Lab nền tảng

Môi trường: Windows 11, .NET SDK 10.0.402, xunit 2.9.3, chạy ngày 2026-11-26.

#### Lab 1.1 — Một vòng Đỏ-Xanh-Refactor tối thiểu
**Mục tiêu:** làm được bốn nhịp TDD trên luật chuyển trạng thái Task.
```bash
mkdir lh-tdd && cd lh-tdd
dotnet new xunit -n LhTdd -o . && rm UnitTest1.cs
```
**Nhịp 1, đỏ.** Tạo `BoardTaskTests.cs` với test `Start_FromTodo_MovesToInProgress` và `BoardTask.cs` có `Start()` ném `NotImplementedException`.
```bash
dotnet test
```
**Đầu ra thật:**
```text
  Failed LhTdd.BoardTaskTests.Start_FromTodo_MovesToInProgress [157 ms]
   System.NotImplementedException : The method or operation is not implemented.
Failed!  - Failed:     1, Passed:     0, Skipped:     0, Total:     1, Duration: 172 ms - LhTdd.dll (net10.0)
```

**Nhịp 2, xanh.** Sửa `Start()` thành `Status = TaskStatus.InProgress;`. Kết quả: `Passed: 1`.

**Nhịp 3, đỏ lần hai.** Thêm hai test: `Complete_FromTodo_IsRejected` (mong `InvalidOperationException`, trạng thái vẫn `Todo`) và `Complete_FromInProgress_MovesToDone`. Chưa có `Complete()` nên **không biên dịch** (lỗi `CS1061`); theo luật 2 của TDD, lỗi biên dịch cũng là đỏ. Thêm bản ngây thơ `Complete() => Status = TaskStatus.Done;`:
```text
  Failed LhTdd.BoardTaskTests.Complete_FromTodo_IsRejected [5 ms]
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3
```
**Nhịp 4, xanh và refactor.** Thêm kiểm tra trạng thái trong `Complete()`:
```csharp
public void Complete()
{
    if (Status != TaskStatus.InProgress)
    {
        throw new InvalidOperationException($"Cannot complete a task in status {Status}.");
    }

    Status = TaskStatus.Done;
}
```
```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3
```
**Giải thích:** bản ngây thơ làm hai test còn lại xanh, chỉ test "từ chối" đỏ. Chính test đỏ đó buộc thêm luật. Kata này nhỏ nên chưa có bước refactor đáng kể; với code lớn hơn, bước này chạy lại bộ test để chắc hành vi không đổi.
**Dọn dẹp:** `cd .. && rm -rf lh-tdd` (chỉ thư mục bạn vừa tạo).

#### Lab 1.2 — Đọc một kịch bản thật và chạy nó
**Mục tiêu:** thấy một `.feature` của dự án chạy như test; viết thêm một scenario.
```bash
cat document/spec/features/board-service/health.feature
dotnet test src/board-service.Tests --filter "Category!=docker"
```
**Đầu ra thật** (chạy từ gốc repo, 2026-11-26):
```text
Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 5 s - board-service.Tests.dll (net10.0)
```
**Giải thích:** file `.feature` nằm ở `document/spec/features/board-service/` và được Reqnroll đưa vào dự án test qua `ReqnrollFeatureFiles` trong `src/board-service.Tests/board-service.Tests.csproj`, nên spec và test là một.

**Bài tập (⚠ chưa chạy):** viết scenario Given-When-Then cho luật "task đã Done không Start lại được", dùng cùng văn phong như `health.feature`. Tiêu chí đúng: có Given bối cảnh, một When, Then kiểm kết quả quan sát được, không nhắc cài đặt.
**Dọn dẹp:** không có.

## L7 Architecture — Bản đồ giải pháp

### Bản đồ giải pháp

Câu hỏi: nên kiểm tra phần mềm bằng cách nào, và TDD/BDD đứng ở đâu?

| Cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn |
|---|---|---|---|---|
| Không test tự động (kiểm tra thủ công) | tester, exploratory testing | spike, prototype bỏ đi | hồi quy chậm, dễ sót, mệt | Spotify [4] (Chắc) |
| Test sau khi code (test-after) | xUnit, NUnit | đã có code, thêm lưới an toàn | dễ viết đúng cái code đang có | DHH [15] (Chắc) |
| TDD | xUnit + quy trình Đỏ-Xanh-Refactor | logic nghiệp vụ rõ, nhiều nhánh | thời gian ban đầu tăng; dễ gắn cài đặt | Nagappan [10] (Chắc) |
| BDD / Specification by Example | Reqnroll, Cucumber | luật nghiệp vụ cần chốt với người ngoài code | tốn phối hợp, step definition phải bảo trì | North [6] (Chắc), Cucumber [7] (Vừa) |
| ATDD | Fit/FitNesse, Cucumber | nghiệm thu từng story | công cụ dễ thế chỗ cuộc trò chuyện | Agile Alliance [8] (Vừa) |
| Property-based testing | FsCheck, Hypothesis | nhiều input, quy luật tổng quát | khó nghĩ ra property; chưa kiểm nguồn trong Part này | Chưa kiểm (sẽ ở Part 4) |

**Cách chọn nhanh:** logic thuần có nhiều nhánh → TDD; luật cần thống nhất với người không code → BDD/ATDD; code cũ chưa có test → test-after (xem characterization test ở Part 4); ý tưởng chưa chắc → spike, bỏ đi rồi viết lại bằng TDD.

### Trong thực tế (real-world)

#### Microsoft và IBM (2008): TDD đo trên bốn nhóm
- **Vấn đề:** TDD phổ biến nhưng ít bằng chứng công nghiệp.
- **Cách làm:** case study ba nhóm Microsoft, một nhóm IBM đã áp dụng TDD, so với dự án tương tự không dùng.
- **Kết quả:** mật độ lỗi trước phát hành giảm 40% đến 90%; thời gian phát triển ban đầu tăng 15% đến 35% (nhóm tự ghi nhận). Hạn chế: không ngẫu nhiên, nhóm tự chọn dùng TDD.
- Nguồn: [Nagappan và cộng sự, Empirical Software Engineering 13, 2008](https://link.springer.com/article/10.1007/s10664-008-9062-z) (Chắc: abstract).

#### Spotify (2018): honeycomb thay pyramid cho microservice
- **Vấn đề:** microservice nhỏ, logic mỏng; nhiều unit test khiến mỗi lần refactor phải sửa test.
- **Cách làm:** ưu tiên integration test chạy dịch vụ cô lập (DB thật, gọi API thật), rất ít test chi tiết cài đặt, tránh test "integrated" phụ thuộc dịch vụ khác.
- **Bài học:** với service chỉ dùng DB và REST, đổi PostgreSQL sang NoSQL chỉ cần sửa phần setup chứ không sửa test (Spotify nói "có thể"); cái giá là bộ test chậm hơn, từ mili-giây lên vài giây.
- Nguồn: [Testing of Microservices, Spotify Engineering](https://engineering.atspotify.com/2018/01/testing-of-microservices) (2018; Chắc).

#### DHH và Rails (2014): bỏ test-first làm phương pháp thiết kế
- **Vấn đề:** test-first như kỷ luật bắt buộc dẫn tới nhiều lớp trung gian chỉ để tránh DB, file, trình duyệt.
- **Cách làm:** ít unit test mock hết phụ thuộc; test model chạm DB thật; chuyển trọng tâm sang test hệ thống.
- **Bài học:** DHH vẫn coi test hồi quy tự động là giá trị lớn; ông phản đối coi test-first là chuẩn đạo đức. Chuỗi thảo luận "Is TDD Dead?" với Fowler và Beck cho thấy nhiều bất đồng đến từ định nghĩa lẫn lộn giữa TDD, unit test và code tự kiểm.
- Nguồn: [TDD is dead. Long live testing.](https://dhh.dk/2014/tdd-is-dead-long-live-testing.html) (2014; Chắc), [Is TDD Dead?](https://martinfowler.com/articles/is-tdd-dead/) (2014; Vừa).

#### Google (2020): phân loại test theo tài nguyên
- **Vấn đề:** gọi "unit" và "integration" mỗi nơi một nghĩa.
- **Cách làm:** phân test thành small (một tiến trình, không I/O chặn), medium (nhiều tiến trình, localhost), large (nhiều máy); cảnh báo mô hình "ice cream cone" và "hourglass". ⚠ Tỷ lệ khoảng 80/15/5 theo số lượng, tuỳ nhóm.
- Nguồn: [Software Engineering at Google, chương Testing Overview](https://abseil.io/resources/swe-book/html/ch11.html) (2020; Vừa).

**Mô hình phổ biến:** nhiều test nhỏ, nhanh ở dưới, ít test lớn ở trên [2]; chốt rõ định nghĩa "unit/integration" trong nhóm; tái hiện bug bằng test nhỏ trước khi sửa [16].
**Khi thất bại hoặc bị bỏ:** DHH (ở trên). Chưa tìm được post-mortem công khai của một công ty bỏ BDD/Cucumber; chỉ có ý kiến người thực hành qua nguồn thứ cấp chưa mở, nên không đưa vào làm case.

### Trong TeamBoard
- Quy tắc: [`.agents/rules/testing.md`](../../../.agents/rules/testing.md) yêu cầu TDD (đỏ → xanh → refactor) và BDD (`.feature` trước), tên test mô tả hành vi, Arrange/Act/Assert.
- Công cụ: xUnit 2.9.3 và Reqnroll.xUnit 3.3.4 ([`config/tool-versions.yaml`](../../../config/tool-versions.yaml):11-12); kịch bản ở `document/spec/features/<service>/`.
- Điểm cần chú ý: Reqnroll chạy kịch bản thành test xUnit (Lab 1.2); test Docker tách riêng bằng `Category=docker`.

## Tự kiểm tra

1. Vì sao test phải đỏ **trước** khi viết code? <details><summary>Đáp án</summary>Test chưa từng đỏ không chứng minh nó phát hiện được lỗi; đỏ đúng lý do là bằng chứng test kiểm đúng điều cần kiểm.</details>
2. Phân biệt stub và mock bằng một ví dụ Board. <details><summary>Đáp án</summary>Stub chỉ trả giá trị dựng sẵn (`IClock` giờ cố định); mock được dựng kỳ vọng về lời gọi và tự kiểm (đúng một lần gọi `Publish`).</details>
3. Một scenario viết "bấm nút Complete rồi chờ 2 giây" sai ở đâu? <details><summary>Đáp án</summary>Viết theo kiểu mệnh lệnh, gắn giao diện và thời gian; nên khai báo hành vi: "When I complete the task".</details>
4. Nghiên cứu Microsoft/IBM có chứng minh TDD giảm lỗi không? <details><summary>Đáp án</summary>Không: là case study không ngẫu nhiên, nhóm tự chọn áp dụng; chỉ cho thấy tương quan trong các nhóm đó.</details>
5. Việc chạy được: chạy lại Lab 1.1, rồi thêm test "Start từ Done bị từ chối" và làm nó xanh. Tiêu chí: thấy đỏ trước, xanh sau, các test cũ vẫn xanh.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [Springer, Nagappan và cộng sự 2008](https://link.springer.com/article/10.1007/s10664-008-9062-z): abstract, số liệu 40% đến 90% và 15% đến 35% (chỉ abstract, bài đầy đủ cần thuê bao).
- [Spotify, Testing of Microservices 2018](https://engineering.atspotify.com/2018/01/testing-of-microservices)
- [DHH, TDD is dead 2014](https://dhh.dk/2014/tdd-is-dead-long-live-testing.html)
- [Dan North, Introducing BDD 2006](https://dannorth.net/blog/introducing-bdd/): đọc phần đầu (nguồn gốc, đặt tên test như câu, mẫu "should").
- [Fowler, Mocks Aren't Stubs](https://martinfowler.com/articles/mocksArentStubs.html): xác nhận có mục "Classical and Mockist Testing"; nội dung chi tiết qua agent.
- Repo: `config/tool-versions.yaml`, `src/board-service.Tests/board-service.Tests.csproj`, kết quả hai lab ở trên.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- [1] [Fowler, GivenWhenThen](https://martinfowler.com/bliki/GivenWhenThen.html); [9] [SpecificationByExample](https://martinfowler.com/bliki/SpecificationByExample.html)
- [2] [Vocke, Practical Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html) và [Fowler, TestPyramid](https://martinfowler.com/bliki/TestPyramid.html) [16]
- [3] [Fowler, Mocks Aren't Stubs](https://martinfowler.com/articles/mocksArentStubs.html) và [TestDouble](https://martinfowler.com/bliki/TestDouble.html)
- [4] Spotify (Chắc) và [Kent C. Dodds, Testing Trophy](https://kentcdodds.com/blog/the-testing-trophy-and-testing-classifications) (2021, chỉ agent)
- [5] [Fowler, TestDrivenDevelopment](https://martinfowler.com/bliki/TestDrivenDevelopment.html)
- [7] [Cucumber BDD](https://cucumber.io/docs/bdd/), [Gherkin reference](https://cucumber.io/docs/gherkin/reference/), [Better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- [8] [Agile Alliance, ATDD](https://agilealliance.org/glossary/atdd/), [BDD](https://agilealliance.org/glossary/bdd/), [TDD](https://agilealliance.org/glossary/tdd/)
- [11] [Fowler, Is TDD Dead?](https://martinfowler.com/articles/is-tdd-dead/) (quan điểm của Beck)
- [12] [Beck, Canon TDD](https://newsletter.kentbeck.com/p/canon-tdd) (2023)
- [13] [Martin, The Cycles of TDD](https://blog.cleancoder.com/uncle-bob/2014/12/17/TheCyclesOfTDD.html) (2014; bản gốc ba luật không mở được)
- [14] [Wynne, Introducing Example Mapping](https://cucumber.io/blog/bdd/example-mapping-introduction/) (2015)
- Google SWE book ch11 (số 80/15/5 và ngưỡng flaky chỉ qua agent).
- [10] [15] đã liệt kê ở trên (Chắc).

**Chưa kiểm hoặc chưa tìm được:**
- F.I.R.S.T: chưa mở nguồn gốc (Clean Code); chỉ tìm kiếm thứ cấp, các nguồn không thống nhất ở chữ T.
- Tên "Chicago" và "London" cho hai trường phái TDD: chưa có nguồn mở được.
- Danh sách "process patterns" của Specification by Example (Adzic): trang sách không liệt kê, chưa kiểm.
- Property-based testing: chưa mở nguồn nào (sẽ ở Part 4).
- ThoughtWorks Radar về BDD/SpecFlow/Cucumber: không tìm thấy mục tương ứng.
- Case công ty bỏ BDD/Cucumber, Ian Cooper "TDD, Where Did It All Go Wrong", Google E2E blog (2015): chưa mở được thân bài.
- Phiên bản mới nhất của Reqnroll hỗ trợ .NET 10 chưa kiểm ngoài `Reqnroll.xUnit 3.3.4` đang dùng trong repo (đã build và chạy được).
