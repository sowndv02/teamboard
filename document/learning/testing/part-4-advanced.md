# Part 4 — Advanced Engineering (Testing)

> [!NOTE]
> **Mục tiêu:** đo chất lượng bộ test (không chỉ coverage), tìm lỗi mà test theo ví dụ bỏ sót, đưa code cũ vào lưới test, và chọn outside-in hay inside-out.
> **Cần biết trước:** [Part 3](part-3-multi-component.md); `dotnet tool` chạy được.
> **Thời lượng:** ~6 đến 8 giờ nếu làm đủ lab · **Đầu ra kiểm được:** một property bắt được lỗi mà test ví dụ bỏ sót, và một báo cáo mutation có điểm trước/sau khi bổ sung test.
> **Part trước / sau:** [←](part-3-multi-component.md) · [→](part-5-production-security-ops.md)

## L1 What — Kỹ thuật nâng cao

#### Property-based testing (PBT)
**Là gì:** thay vì viết từng ví dụ, bạn phát biểu một **tính chất luôn đúng** ("tổng các phần chia bằng tổng ban đầu") và công cụ sinh nhiều đầu vào ngẫu nhiên để tìm phản ví dụ. FsCheck (.NET) mặc định chạy 100 lần mỗi property và khi thất bại **thu nhỏ** (shrink) đầu vào về phản ví dụ nhỏ nhất [1].
**Ví dụ đời thường:** thay vì thử vài chiếc chìa, bạn phát biểu "mọi chìa cắt đúng mã đều mở được" rồi cho máy thử hàng trăm chiếc.
**Use case:**
- Chia tiền thành n phần → property "tổng các phần bằng tổng ban đầu" bắt lỗi mất phần dư.
- Hàm mã hoá/giải mã, serialize/parse → property "đi rồi về giống ban đầu" [2].

**Lưu ý:** bổ sung cho test theo ví dụ, không thay thế [3].

#### Mutation testing
**Là gì:** công cụ chèn lỗi nhỏ (mutant) vào code, chạy lại bộ test; test fail nghĩa là mutant bị **diệt** (killed), test vẫn đạt nghĩa là mutant **sống sót** (survived) và bạn thiếu một test [4].
**Ví dụ đời thường:** cố tình cài lỗi vào bài làm để kiểm tra người chấm có bắt được không.
**Use case:**
- Coverage 95% nhưng không biết test có thật sự kiểm → mutation score cho con số trung thực hơn.
- Bổ sung test nhắm đúng mutant sống sót (ví dụ biên `>` thành `>=`).

**Lưu ý:** chậm hơn nhiều so với chạy test thường (xem L3, L5).

#### Equivalent mutant
**Là gì:** mutant đổi code nhưng **không đổi hành vi**, nên không test nào diệt được; Stryker không tự phát hiện và không có cách đánh dấu bỏ qua, nên cách xử lý là tìm thủ công, viết lại code, hoặc chấp nhận điểm dưới 100% [5].
**Use case:** thay `x + 0` hoặc `x * 1` là các mẫu điển hình [5].

#### Snapshot / approval test
**Là gì:** lưu kết quả đã được người duyệt vào file `.verified`; mỗi lần chạy so với kết quả mới (`.received`); khác thì fail và hiện diff [6].
**Ví dụ đời thường:** ảnh chụp bàn làm việc "đúng chuẩn", so với ảnh chụp hôm nay.
**Use case:**
- Bảng giá, JSON trả về, báo cáo nhiều trường → một snapshot thay cho hàng chục `Assert`.
- Code cũ chưa biết đúng sai → "golden master" làm lưới an toàn ban đầu.

**Lưu ý:** commit file `.verified`, không commit `.received` [6].

#### Characterization test
**Là gì:** test **mô tả điều code đang làm thật**, không phải điều nó nên làm; dùng để phát hiện khi một thay đổi làm đổi hành vi. Kết quả lạ chưa chắc là bug [7].
**Ví dụ đời thường:** ghi lại nhịp tim của bệnh nhân trước khi chữa, để biết sau khi chữa có đổi không.
**Use case:** hàm tính giá "không ai nhớ vì sao" cần dọn dẹp → test mô tả trước, dọn sau.

#### Seam
**Là gì:** chỗ có thể **đổi hành vi chương trình mà không sửa tại chỗ đó** (Feathers); ví dụ lớp con trong test ghi đè một phương thức bọc lời gọi toàn cục [8].
**Use case:** đưa code cũ vào test: tìm seam để chặn tác dụng phụ rồi mới viết test.

#### Outside-in và inside-out
**Là gì:** hai hướng đi khi TDD. Outside-in bắt đầu từ test chấp nhận ở ngoài cùng, mock các cộng tác viên để khám phá thiết kế từng lớp vào trong. Inside-out (classicist) bắt đầu từ domain, dùng đối tượng thật, kiểm trạng thái ([Part 1](part-1-fundamentals.md#tdd-kiểu-chicago-classical-và-london-mockist)) [9][10].
**Use case:** tính năng mới theo story có kịch bản BDD → outside-in; logic domain thuần → inside-out.

## L2 Why — Vì sao coverage cao vẫn lọt bug

- **Coverage chỉ cho biết dòng nào được chạy, không cho biết test có kiểm.** Fowler: coverage tốt để tìm chỗ chưa được test, kém để đánh giá chất lượng test và dễ bị biến thành chỉ tiêu làm test kém đi [11].
- **Số liệu thật ở Lab 4.2:** bộ test yếu của `BoardTask` có line coverage 95,45% (21/22 dòng) nhưng mutation score chỉ 71,43%.
- **Test theo ví dụ chỉ thấy những gì bạn nghĩ ra.** Lab 4.1: ví dụ `Split(10, 2) = [5, 5]` vẫn đạt với bản cài sai; property tổng bằng tổng ban đầu bắt ngay lỗi.
- **Code cũ không có đặc tả.** Không ai biết đúng/sai, chỉ biết hiện đang làm gì; characterization test ghi lại điều đó trước khi sửa [7].

## L3 How — Cơ chế

#### Property-based testing: sinh, chạy, thu nhỏ, phát lại
**Ai làm gì:**
1. Bạn viết property: hàm nhận đầu vào (kiểu hoặc wrapper như `NonNegativeInt`, `PositiveInt`) và trả `bool`.
2. FsCheck sinh đầu vào theo kiểu, chạy property (mặc định 100 lần) [1].
3. Gặp phản ví dụ thì **shrink** về đầu vào nhỏ nhất vẫn fail và in kèm seed; `Replay` chạy lại đúng chuỗi đó [1].

```text
generator ──> 100 đầu vào ──> property ──[fail]──> shrink ──> phản ví dụ nhỏ + seed ──> Replay
```
- **Ưu:** thấy tình huống bạn không nghĩ tới. **Nhược:** viết property khó hơn viết ví dụ; kết quả có thể khác nhau mỗi lần nếu không cố định seed.
- **Dùng khi:** hàm thuần với quy luật rõ. **Không dùng khi:** chưa nói được "luôn đúng" là gì; hành vi gắn chặt hạ tầng.
- **Nguồn:** FsCheck docs (Vừa); Lab 4.1 đã chạy.

**Bảy mẫu property thường gặp** (Wlaschin) [12]:

| Mẫu | Ý nghĩa | Ví dụ Board |
|---|---|---|
| Đường khác, cùng đích | hai cách làm cho cùng kết quả | sắp xếp rồi lọc = lọc rồi sắp xếp |
| Đi rồi về | thao tác rồi đảo ngược thì trở lại ban đầu | serialize rồi deserialize task |
| Có thứ không đổi | bất biến sau biến đổi | tổng các phần chia không đổi |
| Làm hai lần như một | idempotent | `Archive` hai lần = một lần |
| Giải bài nhỏ trước | phần đúng thì tổng thể đúng | cây task con |
| Khó tìm, dễ kiểm | kiểm một lời giải rẻ | kiểm lịch đã xếp có hợp lệ |
| Đối chứng (oracle) | so với bản chậm nhưng đúng | so với cài đặt ngây thơ |

⚠ Bảng này tóm từ bài gốc qua agent.

#### Mutation testing: chèn lỗi, chạy test, đếm
**Ai làm gì:** Stryker.NET tạo các mutant (đổi `>` thành `>=`, đổi `!=` thành `==`, xoá câu lệnh, đổi chuỗi...), chỉ chạy các test phủ mutant đó (chế độ coverage-based), và gán trạng thái [4]:
- **Killed:** có test fail. **Survived:** mọi test đạt, thiếu test. **No coverage:** không test nào chạm tới. **Timeout:** tính là bị phát hiện. **Compile error, runtime error:** không tính vào điểm. **Ignored:** không tính.
- **Mutation score = detected / valid × 100**, với detected = killed + timeout, valid = detected + survived + no coverage [5].

Cấu hình đáng nhớ [5]: ngưỡng mặc định high 80, low 60, break 0 (điểm dưới `break` thì lệnh thoát mã 1, hữu ích cho CI nhưng mặc định không bao giờ fail); mức đột biến (Basic, Standard mặc định, Advanced, Complete) tích luỹ dần.
- **Ưu:** đo test có kiểm thật không; chỉ ra test thiếu cụ thể. **Nhược:** chậm; có equivalent mutant; điểm 100% không phải mục tiêu.
- **Dùng khi:** module logic quan trọng đã có test. **Không dùng khi:** code hạ tầng ít logic, hoặc chưa có test nào.
- **Cách Google làm:** quét toàn bộ quá đắt, nên Google chỉ đột biến trên **phần diff**, bỏ qua dòng không có coverage và dòng "arid" (ít ý nghĩa), rồi hiện kết quả ngay trong code review [13].
- **Nguồn:** Stryker docs (Chắc cho trạng thái và metric; Vừa cho cấu hình); Google ICSE-SEIP 2018 (Chắc, abstract).

#### Snapshot / approval: received và verified
**Ai làm gì:** lần đầu chưa có file `.verified` nên test fail và ghi `.received`; bạn xem rồi duyệt (đổi tên/promote thành `.verified`). Lần sau khớp thì đạt; khác thì fail kèm diff, bạn sửa code hoặc duyệt snapshot mới [6].
- **Ưu:** một snapshot thay nhiều assert; diff dễ đọc. **Nhược:** dễ duyệt bừa; giá trị biến động (GUID, thời gian) làm snapshot đổi liên tục nếu không scrub.
- **Dùng khi:** kết quả lớn, có cấu trúc. **Không dùng khi:** chỉ cần kiểm một giá trị.
- **Nguồn:** Verify README (Vừa); Lab 4.4 đã chạy. ⚠ Chi tiết scrubbing GUID/ngày chưa kiểm.

#### Characterization test: năm bước của Feathers
**Ai làm gì** [7]:
1. Phá các phụ thuộc để code chạy được trong test (thường là phần khó nhất).
2. Viết test với giá trị mong đợi **giữ chỗ**, vì chưa biết hành vi.
3. Chạy và đọc **giá trị thật** từ thông báo lỗi.
4. Dán giá trị thật làm giá trị mong đợi và **đặt lại tên** cho mô tả hành vi.
5. Thêm test dò tiếp; không rõ là bug thì để nguyên test.

- **Ưu:** có lưới an toàn khi chưa có đặc tả. **Nhược:** đóng băng cả hành vi sai; phải ghi chú rõ chỗ nghi bug.
- **Nguồn:** Feathers 2016 (Chắc); Lab 4.3 đã chạy. Seam theo trích đoạn sách 2004 (Vừa).

#### Outside-in và inside-out
**Ai làm gì:**
- **Outside-in:** viết test chấp nhận ở ngoài cùng (đỏ), rồi xen kẽ các vòng TDD ở mức đơn vị bên trong; mock giúp khám phá cộng tác viên cần có; mỗi kỳ vọng của mock trở thành test kế tiếp, đi dần vào trong [9][10].
- **Inside-out:** bắt đầu từ domain, đối tượng thật, kiểm trạng thái, ghép dần ra ngoài [Part 1].

```text
outside-in:  [chấp nhận] ─đỏ─> [unit lớp ngoài, mock lớp trong] ─> [unit lớp trong] ─> chấp nhận xanh
inside-out:  [unit domain] ─> [ghép service] ─> [API] ─> chấp nhận xanh
```
- **Ưu/nhược:** outside-in cho phản hồi thiết kế sớm nhưng test gắn chặt cách cài đặt hơn; inside-out ít gắn cài đặt nhưng dễ làm thừa thứ ngoài chưa cần [Part 1].
- **Dùng khi:** outside-in cho story có kịch bản BDD; inside-out cho domain thuần.
- **Nguồn:** Fowler (Vừa), một bản sách TDD (Vừa). ⚠ Chưa đọc được bản gốc GOOS; khái niệm "walking skeleton" và "hai vòng" chỉ thấy trong kết quả tìm kiếm.

## L4 Hands-on — Lab

Môi trường: Windows 11, .NET SDK 10.0.402, `xunit` 2.9.3, `FsCheck.Xunit` 3.6.0, `dotnet-stryker` 5.1.0 (công cụ local), `Verify.Xunit` 31.12.5; chạy 2026-11-26. Lab 4.1, 4.3, 4.4 dùng project `lh-adv`; Lab 4.2 dùng `lh-mut`.

#### Lab 4.1 — Property bắt lỗi mà ví dụ bỏ sót
**Mục tiêu:** viết property cho hàm chia tiền và thấy FsCheck tìm ra lỗi mất phần dư.
```bash
mkdir lh-adv && cd lh-adv
dotnet new xunit -n LhAdv -o . && rm UnitTest1.cs
dotnet add package FsCheck.Xunit --version 3.6.0
```
Bản cài sai (mất phần dư):
```csharp
public static int[] SplitCents(int totalCents, int parts) =>
    Enumerable.Repeat(totalCents / parts, parts).ToArray();
```
Ba property:
```csharp
[Property]
public bool Split_SumsBackToTotal(NonNegativeInt total, PositiveInt parts) =>
    Splitter.SplitCents(total.Get, parts.Get).Sum() == total.Get;

[Property]
public bool Split_ReturnsRequestedNumberOfParts(NonNegativeInt total, PositiveInt parts) =>
    Splitter.SplitCents(total.Get, parts.Get).Length == parts.Get;

[Property]
public bool Split_PartsDifferByAtMostOneCent(NonNegativeInt total, PositiveInt parts)
{
    var result = Splitter.SplitCents(total.Get, parts.Get);
    return result.Max() - result.Min() <= 1;
}
```
(với `using FsCheck; using FsCheck.Fluent; using FsCheck.Xunit;`.)
```bash
dotnet test
```
**Đầu ra thật:**
```text
Falsifiable, after 5 tests (0 shrinks) (5697189719869338745,11463249940167270741).
Replay directly at failing step with (15260529599040950328,16360893745037229397,6).
Original:
(NonNegativeInt 1, PositiveInt 2)
Failed!  - Failed:     1, Passed:     2, Skipped:     0, Total:     3
```
**Giải thích:** phản ví dụ `(1, 2)`: 1 xu chia hai phần cho `[0, 0]`, tổng 0. Chỉ property "tổng" đỏ; hai property còn lại **đạt** với bản sai (các phần bằng nhau, đúng số phần). Mỗi lần chạy có thể ra phản ví dụ khác vì seed khác (lần chạy đầu của bài này ra seed và đầu vào khác).
**So với test ví dụ:** `Assert.Equal([5, 5], Splitter.SplitCents(10, 2))` **đạt** với bản sai (đã chạy).
**Sửa:** phân phối phần dư:
```csharp
var each = totalCents / parts;
var remainder = totalCents % parts;
return Enumerable.Range(0, parts).Select(i => i < remainder ? each + 1 : each).ToArray();
```
Kết quả: `Passed: 3` (4 khi cộng test ví dụ).
**Dọn dẹp:** không có.

#### Lab 4.2 — Mutation testing với Stryker.NET
**Mục tiêu:** thấy mutation score thấp hơn coverage, rồi bổ sung test nhắm đúng mutant sống sót.
**Bố cục bắt buộc:** project cần kiểm (`Domain/LhMut.Domain`, classlib chứa `BoardTask` của Part 2) và project test (`Tests/LhMut.Tests`) **tách riêng**, test tham chiếu domain (`dotnet add reference`). Stryker chạy từ thư mục project test.
```bash
dotnet new tool-manifest
dotnet tool install dotnet-stryker
cd Tests && dotnet stryker --reporter json --reporter cleartext
```
Bộ test yếu (4 test): `MoveTo` từ Todo sang InProgress, InProgress sang Done, chuyển cấm có ném lỗi, và `IsOverdue` khi đã quá hạn.
**Đầu ra thật:**
```text
[INF] 12 mutants created
[INF] 3     mutants got status CompileError.
[INF] 2     mutants got status Ignored.      Reason: Removed by block already covered filter
[INF] 7     total mutants will be tested
[INF] The final mutation score is 71.43 %
```
Báo cáo JSON liệt kê trạng thái từng mutant; các mutant đáng chú ý:
```text
Survived  L38 Equality mutation -> clock.UtcNow >= due
Survived  L31 String mutation   -> $""
Killed    L38 Equality mutation -> clock.UtcNow < due
Killed    L38 Equality mutation -> Status == TaskStatus.Done
```
**Coverage cùng bộ test yếu** (coverlet): `line-rate="0.9545"` (21/22 dòng).
**Bổ sung test biên** `IsOverdue_ExactlyAtDueTime_ReturnsFalse` (hạn đúng bằng giờ hiện tại thì chưa quá hạn), chạy lại:
```text
Passed!  - Failed: 0, Passed: 5 ... 
[INF] The final mutation score is 85.71 %
Survived L31 String mutation -> $""
```
**Giải thích:** mutant `>` → `>=` sống sót vì không test nào thử biên; test mới diệt nó. Mutant còn lại đổi thông điệp lỗi thành chuỗi rỗng; không test nào kiểm thông điệp, và có nên kiểm hay không là quyết định (kiểm thông điệp sẽ gắn test vào cách cài đặt).
**Dọn dẹp:** `cd .. && rm -rf lh-mut` (chỉ thư mục bạn tạo; Stryker sinh thư mục `StrykerOutput`).

#### Lab 4.3 — Characterization test cho code cũ
**Mục tiêu:** có lưới an toàn cho hàm "không ai nhớ" trước khi sửa.
```csharp
public static decimal Quote(int units, string tier, bool member)
{
    decimal price = units * 12.5m;
    if (tier == "gold" && units > 10) price *= 0.9m;
    if (tier == "platinum") price *= 0.85m;
    if (member) price -= 3;
    if (tier == "platinum" && member) price -= 3;
    return Math.Round(price, 0);
}
```
**Bước 2 và 3.** Viết test với giá trị giữ chỗ `-1m`, chạy:
```text
Expected: -1
Actual:   166        (gold, 15 đơn vị, thành viên)
Expected: -1
Actual:   100        (platinum, 10 đơn vị, thành viên)
```
**Bước 4.** Dán giá trị thật, đặt lại tên (`Quote_GoldFifteenUnitsMember_Returns166`, `Quote_PlatinumTenUnitsMember_Returns100` kèm ghi chú "platinum + thành viên bị trừ hai lần, giữ nguyên tới khi nghiệp vụ xác nhận"): `Passed: 2`.
**"Dọn dẹp" làm đổi hành vi.** Xoá dòng trừ lần hai cho platinum:
```text
  Failed ...Quote_PlatinumTenUnitsMember_Returns100
Expected: 100
Actual:   103
```
**Giải thích:** test bắt được việc dọn dẹp làm đổi giá; sau đó khôi phục code. Bài học: tên test ghi **điều code làm**, không khẳng định đó là đúng.
**Dọn dẹp:** không có.

#### Lab 4.4 — Snapshot bảng giá bằng Verify
**Mục tiêu:** một snapshot thay nhiều assert cho cùng hàm.
```bash
dotnet add package Verify.Xunit --version 31.12.5
```
```csharp
public class SnapshotTests
{
    [Fact]
    public Task Quote_PriceTable_MatchesApprovedSnapshot()
    {
        var table = new[] { "standard", "gold", "platinum" }
            .SelectMany(tier => new[] { 5, 15 }.Select(units => new
            {
                tier, units,
                guest = LegacyPricing.Quote(units, tier, false),
                member = LegacyPricing.Quote(units, tier, true),
            }));
        return Verifier.Verify(table);
    }
}
```
(với `using VerifyXunit;`; **không** có `[UsesVerify]` ở bản 31.x, xem L5; đặt `DiffEngine_Disabled=true` để không mở công cụ diff.)
```bash
dotnet test --filter "FullyQualifiedName~Snapshot"
```
**Lần chạy đầu** (chưa có `.verified`): `Failed!` và ghi file `SnapshotTests...received.txt`; nội dung bắt đầu bằng:
```text
[ { tier: standard, units: 5, guest: 62.0, member: 60.0 }, ...
```
Đổi tên `.received.txt` thành `.verified.txt` (duyệt) rồi chạy lại: `Passed: 1`. Đổi hệ số `0.85m` thành `0.80m` trong hàm: `Failed!` kèm cặp file Received/Verified; khôi phục code, xoá `.received`: `Passed`.
**Giải thích:** snapshot cũng lộ một chi tiết lạ của hàm: `standard`, 5 đơn vị, khách lẻ cho `62` chứ không phải `62,5` làm tròn lên; nguyên nhân là `Math.Round` mặc định làm tròn về số chẵn ở mức .5; đã xác minh ở [Part 5 Lab 5.2](part-5-production-security-ops.md) (test hồi quy đỏ, sửa bằng `MidpointRounding.AwayFromZero`, xanh).
**Dọn dẹp:** xoá file `*.received.*` còn lại; `cd .. && rm -rf lh-adv` khi xong.

## L5 Troubleshooting — Test kỹ thuật cao hỏng

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| Stryker: `No project found` | đọc log `dotnet stryker` | project test và project nguồn cùng một csproj | tách project nguồn, test tham chiếu nó |
| `CS0104 'TaskStatus' is ambiguous` | đọc lỗi biên dịch | `ImplicitUsings` kéo `System.Threading.Tasks.TaskStatus` | alias `using TaskStatus = ...` |
| Property fail mỗi lần một phản ví dụ | xem seed trong thông báo | đầu vào ngẫu nhiên, mỗi lần seed khác | dùng `Replay` với seed đã in |
| Mutant còn sống nhưng không cứu được | đọc mutant và thử nghĩ test kiểm hành vi | equivalent mutant hoặc chỉ khác thông điệp | viết lại code hoặc chấp nhận điểm dưới 100% [5] |
| `UsesVerifyAttribute` không tìm thấy | đọc lỗi biên dịch | bản `Verify.Xunit` 31.x không còn thuộc tính này | bỏ `[UsesVerify]`, dùng `Verifier.Verify` |
| Snapshot đổi liên tục | xem diff `.received` | có GUID, thời gian, thứ tự không ổn định | scrub hoặc sắp xếp trước khi verify ⚠ chưa tái hiện |

**Thẻ 1: Stryker không tìm thấy project.** Chạy `dotnet stryker` trong project test duy nhất chứa cả `BoardTask`:
```text
Could not find an assembly reference to a mutable assembly for project ...
No project found, check settings and ensure project file is not corrupted.
```
Stryker cần một project test **tham chiếu** project cần đột biến. Sau khi tách `LhMut.Domain` và `LhMut.Tests`, Stryker báo `Found project ... LhMut.Domain.csproj to mutate`.
**Thẻ 2: ambiguous `TaskStatus`.** Khi chuyển enum `TaskStatus` sang assembly khác, test báo `error CS0104: 'TaskStatus' is an ambiguous reference between 'LhMut.Domain.TaskStatus' and 'System.Threading.Tasks.TaskStatus'`. Thêm `using TaskStatus = LhMut.Domain.TaskStatus;` vào file test là hết lỗi; cách khác là đặt tên enum không trùng.

## L7 Architecture — Khi nào dùng kỹ thuật nào

| Kỹ thuật | Hợp khi | Cái giá | Không dùng khi |
|---|---|---|---|
| Test theo ví dụ | mọi trường hợp cụ thể, dễ đọc | chỉ thấy điều đã nghĩ ra | (luôn cần, nhưng chưa đủ) |
| Property-based | hàm thuần có quy luật rõ | khó viết property, kết quả ngẫu nhiên | chưa nói được "luôn đúng" là gì |
| Mutation testing | module logic quan trọng đã có test | chậm, equivalent mutant | code hạ tầng, chưa có test |
| Snapshot / approval | kết quả lớn có cấu trúc, code cũ | dễ duyệt bừa, cần scrub | chỉ kiểm một giá trị |
| Characterization | code cũ chưa có đặc tả | đóng băng cả hành vi sai | code mới (dùng TDD) |

**Cách chọn nhanh:** code mới → TDD (Part 1); code cũ cần sửa → characterization hoặc snapshot, rồi refactor; hàm thuần có quy luật → thêm property; muốn biết bộ test có tốt không → mutation trên module quan trọng; coverage là chỉ báo phụ, không phải mục tiêu [11].

### Trong thực tế (real-world)

#### Google (2018): mutation testing trong code review
- **Vấn đề:** phân tích đột biến truyền thống quá đắt ở quy mô công nghiệp.
- **Cách làm:** chỉ đột biến trên phần thay đổi (diff), bỏ dòng không có coverage và dòng "arid", đưa kết quả vào code review.
- **Kết quả:** hệ thống được 6.000 kỹ sư dùng cho mọi thay đổi họ viết hoặc review, ảnh hưởng hơn 14.000 tác giả; xử lý khoảng 30% diff.
- Nguồn: [State of Mutation Testing at Google](https://research.google/pubs/state-of-mutation-testing-at-google/) (ICSE-SEIP 2018; Chắc, abstract).

#### Dropbox và các dịch vụ đồng bộ (2016): property-based cho hệ phân tán
- **Cách làm:** mô hình hình thức có thể kiểm thử của hành vi lõi một bộ đồng bộ file, dùng PBT cho hệ không tất định; tìm thấy hành vi bất ngờ ở hai bộ đồng bộ đang triển khai.
- Nguồn: [Mysteries of Dropbox, ICST 2016](https://research.chalmers.se/en/publication/232551) (bản ghi xuất bản; Vừa). ⚠ Chi tiết cụ thể (Dropbox, Google Drive, ownCloud; xoá file mới tạo) chỉ từ đoạn tìm kiếm, chưa kiểm.

#### Amazon S3 ShardStore (2021): PBT để kiểm tra mô hình tham chiếu
- ⚠ Chưa mở được bài gốc (SOSP 2021); chỉ biết từ đoạn tìm kiếm rằng nhóm dùng PBT so cài đặt với mô hình tham chiếu thực thi. Số "16 vấn đề được ngăn" chưa kiểm, **không** dùng làm số liệu.

**Mô hình phổ biến:** đột biến theo diff thay vì toàn bộ [13]; characterization trước, refactor sau [7]; coverage như chỉ báo phụ [11].
**Khi thất bại hoặc bị bỏ:** chưa tìm được case công ty bỏ mutation testing hay PBT, nên Part này **chưa đạt** chuẩn real-world (chỉ một case tự mở, một case Vừa, một case chưa mở được). Điều đã biết về cái giá: Google phải giảm mạnh số mutant vì chi phí [13]; Stryker thừa nhận không xử lý được equivalent mutant [5].

### Trong TeamBoard
- Không có FsCheck, Stryker hay Verify trong `src/` hoặc `config/` (đã tìm); thêm công cụ mới đi qua `.agents/rules/dependency-policy.md`.
- Chưa có ADR cho mutation testing hay PBT; các lab ở đây chạy ngoài repo.
- Quy tắc liên quan: `.agents/rules/testing.md` ("Không skip, xoá, hoặc nới assertion để test pass"), áp dụng cho cả snapshot: không duyệt `.received` chỉ để xanh.
- Giấy phép cần kiểm trước khi dùng thật: README của Verify nêu "Open Source Maintenance Fee" cho tổ chức có doanh thu từ v33 trở đi (các bản phát hành sau 2026-09-01); bản dùng ở lab là 31.12.5 ⚠ chưa đọc văn bản giấy phép.

## Tự kiểm tra

1. Vì sao property "tổng các phần bằng tổng ban đầu" bắt được lỗi mà ví dụ `10 chia 2` bỏ sót? <details><summary>Đáp án</summary>Ví dụ chọn đúng trường hợp chia hết; property thử cả trường hợp không chia hết, nơi phần dư bị mất.</details>
2. Coverage 95% và mutation score 71%: hai con số nói điều gì? <details><summary>Đáp án</summary>Hầu hết dòng được chạy nhưng nhiều thay đổi hành vi không bị test nào phát hiện; coverage đo chạy, mutation đo kiểm.</details>
3. Vì sao mutant `>` thành `>=` sống sót, và cách diệt? <details><summary>Đáp án</summary>Không test nào thử đúng biên (hạn bằng giờ hiện tại); thêm test biên.</details>
4. Characterization test có chứng minh code đúng không? <details><summary>Đáp án</summary>Không; chỉ ghi lại hành vi hiện tại để phát hiện thay đổi; hành vi lạ nên ghi chú và hỏi nghiệp vụ.</details>
5. Việc chạy được: trong Lab 4.1, thêm property "mỗi phần ≥ 0" và property "sắp thứ tự không ảnh hưởng tổng", rồi cố ý phá bản đã sửa (ví dụ bỏ `+ 1`). Tiêu chí: ít nhất một property đỏ với phản ví dụ nhỏ, và bạn giải thích được vì sao.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [4][5] [Stryker, Mutant states and metrics](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/): các trạng thái, công thức detected/valid.
- [7] [Feathers, Characterization Testing](https://michaelfeathers.silvrback.com/characterization-testing) (2016): ý tưởng và ví dụ; phần năm bước đọc qua agent đối chiếu.
- [13] [Google, State of Mutation Testing](https://research.google/pubs/state-of-mutation-testing-at-google/) (2018): abstract, số 6.000 và 14.000.
- Kết quả bốn lab trong bài này, chạy 2026-11-26.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- [1] [FsCheck](https://fscheck.github.io/FsCheck/), [Running tests](https://fscheck.github.io/FsCheck/RunningTests.html); [3] [Hypothesis tutorial](https://hypothesis.readthedocs.io/en/latest/tutorial/introduction.html); [2] cũng từ Hypothesis (ví dụ round-trip)
- [4] [Stryker.NET getting started](https://stryker-mutator.io/docs/stryker-net/getting-started/), [configuration](https://stryker-mutator.io/docs/stryker-net/configuration/)
- [5] [Equivalent mutants](https://stryker-mutator.io/docs/mutation-testing-elements/equivalent-mutants/)
- [6] [Verify](https://github.com/VerifyTests/Verify)
- [8] [Feathers, Working Effectively with Legacy Code (trích đoạn)](https://www.informit.com/articles/article.aspx?p=359417&seqNum=2)
- [9][10] [Fowler, Mocks Aren't Stubs](https://martinfowler.com/articles/mocksArentStubs.html), [TDD bliki](https://martinfowler.com/bliki/TestDrivenDevelopment.html), [bản sách TDD](https://leanpub.com/read/tddbook-en/leanpub-auto-outside-in-tdd)
- [11] [Fowler, TestCoverage](https://martinfowler.com/bliki/TestCoverage.html) (2012)
- [12] [Wlaschin, Choosing properties](https://fsharpforfunandprofit.com/posts/property-based-testing-2/)
- [Dropbox] [Chalmers, Mysteries of Dropbox](https://research.chalmers.se/en/publication/232551)

**Chưa kiểm hoặc chưa tìm được:**
- Bài QuickCheck gốc (Claessen & Hughes, ICFP 2000) không đọc được (PDF nhị phân, trang ACM trả 403); mọi mô tả về nguồn gốc QuickCheck chỉ là kiến thức chung, không dùng làm nguồn.
- Bài Amazon S3 ShardStore và bản PDF Dropbox: không mở được; số liệu và chi tiết chỉ từ đoạn tìm kiếm.
- Giấy phép của Stryker.NET riêng (chỉ biết Stryker nói chung dùng Apache 2.0); giấy phép Verify chưa đọc văn bản.
- Chi tiết scrubbing GUID và ngày trong Verify; bài Google Testing Blog 2021 và ICSE 2021 về mutation chưa mở; khảo sát Papadakis 2019 chưa đọc.
- Sách GOOS (Freeman, Pryce) chưa đọc; "walking skeleton" và "hai vòng" chỉ từ tìm kiếm.
- "Legacy code là code không có test" (Feathers): chưa tìm thấy trong nguồn nào đã mở.
- Chưa chạy Stryker trên code thật của TeamBoard; chưa có lab riêng cho outside-in (chỉ đọc nguồn).
