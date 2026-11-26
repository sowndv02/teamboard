# Part 7 — Troubleshooting, Interview & Mastery (Testing)

> [!NOTE]
> **Mục tiêu:** gỡ các bệnh phổ biến của bộ test, so sánh cách tiếp cận bằng tiêu chí, và tự đánh giá mức thành thạo bằng bài mở và câu hỏi phỏng vấn.
> **Cần biết trước:** [Part 1](part-1-fundamentals.md) đến [Part 6](part-6-projects.md).
> **Thời lượng:** ~3 đến 4 giờ · **Đầu ra kiểm được:** chẩn đoán đúng nguyên nhân gốc cho các sự cố trong bảng L5, và bảo vệ bằng văn bản một lựa chọn (ví dụ giữ hay bỏ Gherkin) trước phản biện, theo tiêu chí ở L8.
> **Part trước / sau:** [←](part-6-projects.md) · [→](README.md)

## L5 Troubleshooting — Kịch bản sự cố

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| Test đỏ ngẫu nhiên, chạy lại thì xanh | chạy 20 lần, đếm số lần đỏ | trạng thái dùng chung, thứ tự, thời gian | bỏ trạng thái chung, đồng hồ giả ([Part 5](part-5-production-security-ops.md)) |
| Chạy test chậm dần, người ta ngừng chạy | `dotnet test` có `--filter`, so thời lượng | test chậm lẫn trong nhóm nhanh | phân nhóm `Category`, gate theo nhóm ([Part 5](part-5-production-security-ops.md)) |
| Sửa code nội bộ là test đỏ dù hành vi không đổi ⚠ chưa tái hiện | đọc test, đếm mock và `Received` | mock mô tả cách cài đặt thay vì kết quả [1] | dùng fake hoặc đối tượng thật, kiểm kết quả |
| Test xanh nhưng code hỏng | thử cố ý phá code, xem test có đỏ không | mock được cấu hình giống đúng điều sai | fake có hành vi thật, thêm test tích hợp |
| Kịch bản Gherkin dài, đầy HTTP và JSON | đọc `.feature`, đếm step kỹ thuật | feature viết như test kỹ thuật, không ai đọc [3] | viết theo hành vi, đẩy chi tiết xuống test đơn vị |
| Coverage cao nhưng bug lọt | mutation score ([Part 4](part-4-advanced.md)) | coverage đo chạy, không đo kiểm | bổ sung test cho mutant sống sót |
| Test đỏ nên người ta nới assert cho xanh | `git log -p` trên file test, xem assert đổi gì | sửa test thay vì tìm lỗi gốc | khôi phục assert, tìm nguyên nhân; rule `testing.md` cấm |

**Thẻ 1: mock quá đà, test xanh trong khi code hỏng.** Lab dùng NSubstitute 5.3.0. Cùng một hành vi ("xử lý thanh toán thì thẻ bị trừ 500") được viết hai kiểu:
```csharp
// Mock style: tell the double how to behave, then verify the call.
var server = Substitute.For<ICardServer>();
server.IsAvailable().Returns(true);
server.BeginTransaction().Returns("tx1");
server.Pay("tx1", "4111", 500).Returns(true);
Assert.True(new PaymentProcessor(server).Process("4111", 500));
server.Received(1).Pay("tx1", "4111", 500);

// Fake style: in-memory implementation with real behaviour, assert the outcome.
var server = new FakeCardServer();   // Pay rejects a transaction that was never begun
Assert.True(new PaymentProcessor(server).Process("4111", 500));
Assert.Equal(500, server.ChargedTotal("4111"));
```
Cả hai xanh: `Passed: 2`. Rồi cố ý đưa vào lỗi một dòng: `var tx = "tx1"; // leftover from debugging` thay cho `server.BeginTransaction()`:
```text
Failed LhMock.FakeStyleTests.Process_ChargesCard_FakeStyle
Assert.True() Failure   Expected: True   Actual: False
Failed!  - Failed: 1, Passed: 1, Total: 2
```
Test kiểu mock vẫn **xanh** vì tôi đã cấu hình mock trả `true` cho đúng `"tx1"`, chính là giá trị sai; chỉ fake (từ chối giao dịch chưa được mở) phát hiện lỗi. Đây là điều Google gọi là "ít đảm bảo hơn": test chỉ chắc nếu mock cư xử đúng như bản thật [1].
**Không tái hiện được:** tôi cũng thử một refactor nội bộ vô hại (bỏ lời gọi `IsAvailable()` thừa) để làm test mock đỏ; cả hai test vẫn xanh (`Passed: 2`), vì NSubstitute không báo stub không dùng và test không kiểm lời gọi đó. Nên luận điểm "sửa nội bộ làm mock đỏ" của Google [1] **chưa được lab này xác nhận**, chỉ có luận điểm "ít đảm bảo hơn".
**Giới hạn:** lỗi này do tôi dựng để minh hoạ; không chứng minh mock luôn kém hơn fake. Fake cũng phải được duy trì cho giống bản thật [1]; và mock vẫn hợp để kiểm một tương tác (Google nêu mock phù hợp khi kiểm tương tác) ⚠ chi tiết này chỉ từ đoạn tìm kiếm, bài gốc đã mở không nhắc đủ.
**Dấu hiệu theo Google [1]:** mock hơn một hai lớp, một mock quy định hành vi hơn một hai phương thức, hoặc khi đọc test bạn phải "chạy" code trong đầu để hiểu.

**Thẻ 2: kịch bản BDD thành "JUnit có thêm nghi lễ".** Một người dùng Cucumber lâu năm mô tả: sau sprint đầu, chủ sản phẩm thôi đọc feature; kịch bản thành `Given the user entity with id 42 exists in schema "core"...`, "đây không phải BDD" mà là JUnit thêm nghi lễ; với test API hộp đen, lớp Gherkin→step→HTTP client "không thêm giá trị" [3]. Cách kiểm tra trong repo của bạn: đếm step mang từ kỹ thuật (xem "Trong TeamBoard" ở L7). ⚠ Một nguồn, tác giả có công cụ thay thế riêng; xem L7.

**Thẻ 3: test bị nới cho xanh.** ⚠ Chưa tái hiện bằng lab. Cách chẩn đoán: đọc lịch sử file test (`git log -p`), xem `Assert` bị yếu đi (đổi `Equal(63, x)` thành `True(x > 0)`), và chạy mutation (Part 4) trên module đó: assert yếu thường để mutant sống sót. Quy tắc dự án: "Không skip, xoá, hoặc nới assertion để test pass" ([`testing.md`](../../../.agents/rules/testing.md)).

## L7 Architecture — Đánh đổi và case thực tế

| Cách tiếp cận | Mạnh | Cái giá | Khi hợp |
|---|---|---|---|
| Không test tự động | không tốn công viết | lỗi lọt, sợ sửa code | mã dùng một lần, thử nghiệm bỏ đi |
| Test-after | dễ bắt đầu, hợp code đã có | test thiên về cài đặt, bỏ sót biên | code cũ, kèm characterization ([Part 4](part-4-advanced.md)) |
| TDD | phản hồi nhanh, thiết kế dễ test | cần kỷ luật, học lâu | logic domain, hàm thuần |
| BDD | ví dụ nghiệp vụ thành tài liệu sống | glue code, người ngoài thôi đọc [3] | hành vi có người ra quyết định đọc spec |

**Cách chọn nhanh:** mặc định TDD cho logic, BDD cho hành vi mà người ngoài nhóm thật sự đọc; nếu không ai đọc `.feature`, hãy cân lại phần Gherkin (xem case dưới), không phải bỏ ý tưởng "đặc tả bằng ví dụ" ([Part 1](part-1-fundamentals.md)).

### Trong thực tế (real-world)

#### Dan North (2006): BDD sinh ra để dạy TDD, không phải để thêm công cụ
- **Vấn đề:** North dạy TDD và thấy mọi người hỏi cùng một câu: bắt đầu từ đâu, test gì, gọi test là gì, vì sao test đỏ [2].
- **Cách làm:** đặt tên test thành câu ("should..."), dùng từ "hành vi" thay "test"; ông nêu khi test đỏ có ba khả năng: bạn gây lỗi, hành vi chuyển chỗ, hoặc hành vi không còn đúng và test nên **xoá** [2].
- **Điều rút ra:** bộ test không mô tả đủ hành vi "ru ngủ" bằng cảm giác an toàn giả [2].
- Nguồn: [Introducing BDD](https://dannorth.net/blog/introducing-bdd/) (Dan North, 2006-09-20; Chắc, nửa đầu bài; phần JBehave và story chưa đọc).

#### Google (2013): đừng lạm dụng mock
- **Vấn đề:** mock dễ viết nhưng tạo test khó hiểu, khó bảo trì, ít đảm bảo [1].
- **Cách làm:** ví dụ cùng một thanh toán viết bằng nhiều mock và bằng một đối tượng thật; với phụ thuộc không dùng thật được (chậm, qua mạng) ưu tiên máy chủ cục bộ kín hoặc bản fake trong bộ nhớ [1].
- **Dấu hiệu lạm dụng:** mock quá một hai lớp, hoặc một mock quy định quá một hai phương thức [1].
- Nguồn: [Testing on the Toilet: Don't Overuse Mocks](https://testing.googleblog.com/2013/05/testing-on-toilet-dont-overuse-mocks.html) (Google Testing Blog, 2013-05-28; Chắc, phần chính; đoạn cuối bị cắt khi đọc).

#### Một nhóm bỏ Cucumber cho test API hộp đen (kinh nghiệm cá nhân)
- **Bối cảnh:** hệ thống backend lớn dùng Cucumber làm công cụ test tích hợp tiêu chuẩn.
- **Vấn đề:** hàng trăm step definition khó tìm và trùng lặp; trạng thái dùng chung ("World") gây flaky; sau sprint đầu chủ sản phẩm thôi đọc feature; người mới mất vài ngày hiểu glue code; với test API hộp đen, lớp Gherkin thêm dịch mà không thêm giá trị [3].
- **Điểm dừng:** tác giả kết luận trả đủ chi phí glue code mà gần như không nhận lợi ích BDD, và nêu các lựa chọn: Karate (không step definition), REST-assured (viết test thành code) [3].
- ⚠ **Độ tin cậy thấp hơn hai case trên:** một người, bài đăng cá nhân, gắn thẻ công cụ riêng của tác giả (`azertio`), nên có lợi ích liên quan khi đề xuất lựa chọn thay thế; chỉ là **một** kinh nghiệm, không phải thống kê. Họ bỏ **công cụ** Cucumber cho trường hợp API hộp đen, không nhất thiết bỏ tư duy BDD. Kết quả tìm kiếm cho thấy cả phía ủng hộ lẫn phản đối, phần lớn là ý kiến cá nhân hoặc bài của hãng bán công cụ khác; chưa mở bài nào ngoài bài này.
- Nguồn: [When Cucumber Grows Too Big](https://dev.to/luiinge/when-cucumber-grows-too-big-pain-points-lessons-learned-and-alternatives-21pm) (DEV Community; Chắc đã mở, nửa đầu; đoạn so sánh các lựa chọn bị cắt khi đọc).

**Mô hình phổ biến:** chuyển từ "kiểm tra" sang "mô tả hành vi" [2]; ưu tiên đối tượng thật hoặc fake khi mock phình [1]; chọn công cụ theo người đọc spec [3]. Ở các Part trước còn có các case liên quan: bỏ TDD (DHH, [Part 1](part-1-fundamentals.md)), test flaky ở quy mô lớn ([Part 5](part-5-production-security-ops.md)), CI xanh vẫn sập (Cloudflare, [Part 5](part-5-production-security-ops.md)).
**Khi thất bại hoặc bị bỏ:** case thứ ba là trường hợp bỏ công cụ BDD; DHH ở Part 1 là trường hợp bỏ TDD thuần.

### Trong TeamBoard
- Kịch bản BDD là tài liệu sống theo [`test-review.md`](../../../.agents/rules/test-review.md) (BDD cho hành vi, TDD cho code); toàn bộ viết tiếng Anh.
- Đo ngày 2026-11-26 trên `document/spec/features/`: **132 kịch bản, 713 step**; 59 step nhắc `status code`, 104 step nhắc header, body, JSON hay payload; step lặp nhiều nhất là `Then the status code is N` (51 lần), `Given a fake upstream is running` (43), `Given an empty PostgreSQL database is running` (34). Chỉ **3 file** dùng `Background`, trong khi rule review viết "Dùng `Background` cho tiền điều kiện chung".
- **Chỉ là quan sát, không phải lỗi đã xác nhận:** các step lặp có thể cố ý (ví dụ `register.feature` có kịch bản chen thêm step ở giữa nên không gom thẳng vào `Background` được); nếu muốn đổi cần issue riêng và `test-review.md`. Ngoài phạm vi issue này nên chỉ báo lại.
- Đây là kịch bản mức API (đọc bởi nhà phát triển); đúng vai trò hay chưa là câu hỏi mở ở bài L8.1.

## L8 Mastery — Bài mở và phỏng vấn

#### Bài mở 8.1: giữ hay bỏ Gherkin?
**Đề bài:** một nhóm nhỏ đang cân nhắc bỏ Gherkin cho các test API, vì glue code tốn công và không chủ sản phẩm nào đọc `.feature`. Viết một quyết định (không quá một trang) giữ, bỏ, hay chia đôi, kèm cách đo sau ba tháng.
**Tiêu chí chấm** (không có đáp án duy nhất):
- Nêu được **ai** đọc spec và có bằng chứng (hay giả định) không.
- Phân biệt "bỏ công cụ" với "bỏ đặc tả bằng ví dụ" [2][3].
- Có điều kiện để quay lại quyết định.
- Có số đo kiểm được (thời gian thêm một kịch bản, số step trùng, số người đọc spec).
**Hướng hợp lệ:** giữ Gherkin cho hành vi nghiệp vụ và chuyển phần kỹ thuật sang test viết bằng code; hoặc bỏ hoàn toàn nếu không ai đọc; hoặc giữ nhưng gom step bằng `Background`.

#### Bài mở 8.2: cứu bộ test chậm và flaky
**Đề bài:** bộ test của một service chạy 40 phút, khoảng một phần ba lượt chạy có ít nhất một test đỏ giả. Đưa kế hoạch bốn tuần.
**Tiêu chí chấm:**
- Bắt đầu bằng **đo** (phân nhóm, đếm flaky, thời lượng từng nhóm), không bằng đoán.
- Có quarantine **giới hạn** và người nhận ([Part 5](part-5-production-security-ops.md)).
- Giữ tín hiệu của test khoẻ trong lúc sửa.
- Nêu rủi ro của cách làm (quarantine che lỗi thật).
**Hướng hợp lệ:** chia nhóm nhanh/chậm trước; sửa nguyên nhân phổ biến (trạng thái chung, đồng hồ); chạy mutation chọn lọc trên module quan trọng.

#### Bài mở 8.3: chiến lược test cho một service mới
**Đề bài:** `task-service` (CQRS, Event Sourcing, outbox) sắp được xây. Đề xuất các lớp test, thứ tự làm, và ngưỡng nào bạn **không** đặt.
**Tiêu chí chấm:**
- Mỗi lớp (unit, contract, integration, synthetic) có lý do và cái giá ([Part 5](part-5-production-security-ops.md)).
- Nói rõ cái gì chỉ test tích hợp mới bắt được (ví dụ idempotency, outbox).
- Phù hợp quy tắc dự án ([`testing.md`](../../../.agents/rules/testing.md)).
- Nêu rủi ro còn lại.

#### Câu hỏi phỏng vấn
Mỗi câu có hướng trả lời hợp lệ, không có đáp án duy nhất.
1. TDD khác viết test sau ở điểm nào ngoài thứ tự? <details><summary>Hướng trả lời</summary>Thiết kế được kéo bởi test, phản hồi sau mỗi bước nhỏ, và test đỏ trước chứng minh test có khả năng phát hiện lỗi.</details>
2. Khi nào mock là lựa chọn đúng, khi nào nên thay bằng fake? <details><summary>Hướng trả lời</summary>Mock khi cần kiểm một tương tác cụ thể; fake hoặc đối tượng thật khi cần kiểm kết quả; dấu hiệu lạm dụng ở [1].</details>
3. Coverage 90% và coverage 60% cái nào tốt hơn? <details><summary>Hướng trả lời</summary>Không đủ dữ liệu; coverage đo chạy chứ không đo kiểm; cần mutation hoặc xem test kiểm gì ([Part 4](part-4-advanced.md)).</details>
4. Bạn làm gì khi một test flaky chặn cả nhóm? <details><summary>Hướng trả lời</summary>Quarantine có hạn và có người nhận, tìm nguyên nhân (trạng thái chung, thời gian, bất đồng bộ), tránh chạy lại cho đến khi xanh ([Part 5](part-5-production-security-ops.md)).</details>
5. BDD và kiểm thử chấp nhận khác nhau thế nào? <details><summary>Hướng trả lời</summary>BDD là cách làm chung tên gọi hành vi và ví dụ; kiểm thử chấp nhận là loại test; BDD có thể sinh ra test chấp nhận ([Part 1](part-1-fundamentals.md)).</details>
6. Cách bạn bảo vệ một quyết định không viết test cho một đoạn code? <details><summary>Hướng trả lời</summary>Nêu rủi ro, tuổi thọ code, cách phát hiện thay thế (giám sát, review), và điều kiện để viết test sau.</details>
7. Test này xanh nhưng bạn nghi vô dụng, làm sao chứng minh? <details><summary>Hướng trả lời</summary>Cố ý phá code và xem test có đỏ không, hoặc chạy mutation testing ([Part 4](part-4-advanced.md), Lab 5.2 ở [Part 5](part-5-production-security-ops.md)).</details>

## Tự kiểm tra

1. Vì sao test kiểu mock ở Thẻ 1 vẫn xanh khi code đã sai? <details><summary>Đáp án</summary>Mock được cấu hình trả `true` cho đúng giá trị sai `"tx1"`; test chỉ đảm bảo code đúng nếu mock giống bản thật.</details>
2. Ba khả năng khi một test đỏ sau khi bạn đổi code, theo North? <details><summary>Đáp án</summary>Bạn gây lỗi; hành vi chuyển sang chỗ khác; hoặc hành vi không còn đúng và nên xoá test [2].</details>
3. Bỏ Cucumber có đồng nghĩa với bỏ BDD không? <details><summary>Đáp án</summary>Không; có thể giữ tư duy hành vi và ví dụ nhưng bỏ lớp công cụ, như ở case 3 (một kinh nghiệm cá nhân) [3].</details>
4. Số liệu step của repo cho thấy lỗi gì? <details><summary>Đáp án</summary>Không cho thấy lỗi xác nhận; chỉ là quan sát (step kỹ thuật, step lặp, ít `Background`) cần xét từng file trước khi kết luận.</details>
5. Việc chạy được: lấy một test dùng nhiều mock trong dự án của bạn, thử viết lại bằng fake hoặc đối tượng thật, rồi cố ý phá code một dòng. Tiêu chí: ghi lại test nào đỏ, test nào xanh, và giải thích sự khác biệt.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [1] [Google Testing Blog, Don't Overuse Mocks](https://testing.googleblog.com/2013/05/testing-on-toilet-dont-overuse-mocks.html) (2013): phần chính, các vấn đề và dấu hiệu lạm dụng.
- [2] [Dan North, Introducing BDD](https://dannorth.net/blog/introducing-bdd/) (2006): nửa đầu bài (tên test thành câu, ba khả năng khi test đỏ, từ "behaviour").
- [3] [DEV Community, When Cucumber Grows Too Big](https://dev.to/luiinge/when-cucumber-grows-too-big-pain-points-lessons-learned-and-alternatives-21pm): nửa đầu bài; có lợi ích liên quan của tác giả.
- Kết quả lab mock/fake trong bài này, chạy 2026-11-26; số liệu đếm step trong `document/spec/features/`, cùng ngày; quy tắc `testing.md`, `test-review.md`.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- Kết quả tìm kiếm về các bài khác chống/ủng hộ Cucumber (chưa mở): [Agileway](https://agileway.substack.com/p/why-gherkin-cucumber-specflow-always), [Code with Jason](https://www.codewithjason.com/?p=278), [Sauce Labs](https://saucelabs.com/blog/is-bdd-automation-actually-killing-your-project); tôi chỉ biết tiêu đề và đoạn trích, một số từ hãng bán công cụ nên có lợi ích liên quan.
- Chi tiết "mock phù hợp để kiểm tương tác" của Google: chỉ từ đoạn tìm kiếm.

**Chưa kiểm hoặc chưa tìm được:**
- Chưa có case một **công ty** bỏ BDD hay TDD có báo cáo công khai kiểm chứng được; case 3 là một cá nhân.
- Chưa đọc bài gốc của Aslak Hellesøy (câu "Please don't — or at least do it very sparingly" trích qua nguồn bán công cụ) nên không dùng.
- Sách *Software Engineering at Google* chương Test Doubles (abseil.io) và các bài Testing on the Toilet về fake: chưa mở.
- Chưa tái hiện bằng lab hai thẻ: test bị nới cho xanh, và kịch bản Gherkin phình; không có đối chứng độc lập cho các bài mở và câu hỏi phỏng vấn (viết bởi một tác giả).
- Chưa đọc phần JBehave và story của bài North, chưa đọc Fowler "Mocks Aren't Stubs" ngoài Part 1 (Vừa).
