# Part 5 — Production, Security & Operations (Testing)

> [!NOTE]
> **Mục tiêu:** vận hành bộ test như một tài sản: chạy trong CI theo nhóm nhanh/chậm, đo coverage đúng nghĩa, giữ test ổn định, biến sự cố thành test hồi quy, và biết giới hạn của test so với giám sát sau triển khai.
> **Cần biết trước:** [Part 4](part-4-advanced.md); `dotnet test` và `--filter` chạy được.
> **Thời lượng:** ~4 đến 6 giờ nếu làm đủ lab · **Đầu ra kiểm được:** (1) gate test chia nhóm nhanh/chậm với ngưỡng coverage có lý do và đã chạy fail rồi pass; (2) một test hồi quy cho một lỗi, đỏ trước khi sửa; (3) một test flaky tái hiện được rồi sửa tới 20/20 lần xanh.
> **Part trước / sau:** [←](part-4-advanced.md) · [→](part-6-projects.md)

## L1 What — Test như phần của vận hành

#### Test gate trong CI
**Là gì:** bước tự động chạy test (và kiểm tra khác) trên mỗi thay đổi; không đạt thì chặn gộp hoặc chặn phát hành. Google tách hai điểm chặn: **trước khi nộp** (pre-submit, cổng cho việc nộp code) và **sau khi nộp** (post-submit, quyết định dự án sẵn sàng phát hành chưa) [1].
**Ví dụ đời thường:** cổng soát vé: không có vé hợp lệ thì không vào, và cổng này chỉ có ích khi vé thật được soát đúng.
**Use case:**
- Mỗi PR chạy nhóm test nhanh trước, nhóm chậm sau.
- Trước phát hành chạy toàn bộ.

**Lưu ý:** TeamBoard hiện **không có** cấu hình CI trong repo (đã tìm: không có `.github/`, pipeline file nào được theo dõi); gate chạy cục bộ theo [`quality-gate.md`](../../../.agents/rules/quality-gate.md) và [`COMMANDS.md`](../../../COMMANDS.md). Part này dạy khái niệm và lệnh; cấu hình CI cụ thể thuộc việc riêng.

#### Phân nhóm test (fast / slow)
**Là gì:** gắn nhãn cho test (xUnit: `[Trait("Category", "...")]`) để chạy riêng từng nhóm bằng `dotnet test --filter`. Nhóm nhanh (không I/O) chạy liên tục, nhóm chậm (Docker, DB thật) chạy ở mốc riêng.
**Ví dụ đời thường:** kiểm tra nhanh ở cửa nhà (đèn, khoá) mỗi lần ra khỏi nhà; kiểm tra kỹ (ga, điện, nước) chỉ khi đi xa.
**Use case:**
- TeamBoard: `dotnet test src/teamboard.slnx --filter "Category!=docker"` cho vòng nhanh ([`COMMANDS.md`](../../../COMMANDS.md) mục 2).
- Test dùng Testcontainers gắn `docker` để không chạy ở vòng nhanh.

#### Coverage (độ phủ)
**Là gì:** phần trăm dòng (line), nhánh (branch) hoặc phương thức được chạy trong lúc test. Nó đo **chạy**, không đo **kiểm** ([Part 4](part-4-advanced.md#l2-why--vì-sao-coverage-cao-vẫn-lọt-bug)).
**Use case:**
- Tìm vùng chưa có test.
- Ngưỡng tối thiểu để chặn việc **giảm** coverage vùng đã có (quy tắc dự án: không đặt ngưỡng cứng toàn dự án, nhưng PR không được giảm coverage vùng đã có, xem [`quality-gate.md`](../../../.agents/rules/quality-gate.md)).

**Lưu ý:** line 100% vẫn có thể che nhánh chưa chạy (Lab 5.1).

#### Test hồi quy sự cố
**Là gì:** test được viết để **tái hiện đúng một lỗi đã xảy ra** (đỏ trước khi sửa), rồi giữ lại để lỗi không quay lại. Quy tắc dự án: "Sửa bug: viết test fail trước, rồi sửa" ([`testing.md`](../../../.agents/rules/testing.md)).
**Use case:** báo giá lẻ bị làm tròn xuống (Lab 5.2) → test cố định đầu vào, kỳ vọng đúng.

#### Test flaky và quarantine
**Là gì:** test **flaky** vừa pass vừa fail với cùng code; Google định nghĩa như vậy [1]. **Quarantine** là chuyển test ra khỏi nhóm tin cậy để vẫn thấy tín hiệu của test khoẻ, nhưng phải sửa sớm [2].
**Use case:** test chia sẻ biến tĩnh với test khác (Lab 5.3).

#### Test bảo mật và test dữ liệu
**Là gì:** ở mức Part này, hai việc cụ thể: (1) test **phân quyền** như test hành vi thường (đã có kịch bản `auth-me` trong repo); (2) không dùng dữ liệu thật hay secret thật trong test; dùng giá trị giả sinh trong test hoặc file `*.example` ([`AGENTS.md`](../../../AGENTS.md) mục Bảo mật; [LL-001](../../lessons/LL-001-secrets-committed-in-env-files.md)). Quét secret tự động (`gitleaks`) là bước **chưa cài** trong repo.

#### Synthetic check (kiểm tra tổng hợp)
**Là gì:** kịch bản kiểm tra chạy **từ bên ngoài** vào hệ đang chạy, định kỳ, để biết dịch vụ có hoạt động không. Không thay thế test trước phát hành. Trong sự cố 2019, cảnh báo đầu tiên của Cloudflare là một synthetic test của WAF, trước cả cảnh báo traffic [3].

## L2 Why — Vì sao bộ test mục nát

- **Test flaky làm mất tín hiệu.** Google: khoảng 1,5% lượt chạy test báo kết quả flaky; gần 16% test có mức flaky nào đó; khoảng 84% chuyển trạng thái pass→fail trong post-submit liên quan tới test flaky [1].
- **Con người học cách bỏ qua báo động.** Khi lịch sử toàn báo sai, người ta bỏ qua cả lỗi thật; Google gọi đó là bản chất con người và dẫn ví dụ phi công bỏ qua báo động [1]. Fowler nói gần như cùng ý: test không tất định "vô dụng" và còn "lây" sang cả bộ [2].
- **Test chậm bị bỏ chạy.** Lab 5.1: nhóm nhanh 2 test chạy 135 ms, thêm một test "giả lập container" làm cả lần chạy lên 3 s. Một bộ test chỉ cần chậm vài lần là người ta ngừng chạy trước mỗi commit. (Số đo của lab chỉ minh hoạ, không phải so sánh tốc độ container thật.)
- **Pass không có nghĩa an toàn.** Sự cố Cloudflare 2019: pull request được duyệt, build xanh, test pass, rồi vẫn gây sập toàn cầu 27 phút vì bộ test **không có cách phát hiện CPU tăng vọt** [3].
- **Con số coverage dễ bị hiểu sai** (xem [Part 4](part-4-advanced.md#l2-why--vì-sao-coverage-cao-vẫn-lọt-bug)); ngưỡng cứng làm người ta viết test cho đủ số thay vì cho đúng.

## L3 How — Cơ chế

#### Pipeline test: nhanh trước, chậm sau
**Ai làm gì:**
1. Mỗi thay đổi chạy nhóm **nhanh** (unit, không I/O): vài giây.
2. Qua thì chạy nhóm **chậm** (integration, Docker): vài phút.
3. Ở mốc phát hành chạy toàn bộ.
4. Sau triển khai, synthetic check và giám sát kiểm tra hệ đang chạy [3].

```text
commit ──> fast (Category!=docker) ──> slow (Category=docker) ──> deploy ──> synthetic check
              ~giây                        ~phút                              liên tục
```
- **Ưu:** lỗi rẻ bị bắt sớm; phản hồi nhanh. **Nhược:** hai nhóm cần duy trì nhãn đúng; một test gắn nhầm nhóm sẽ làm hỏng cả hai ý nghĩa (L5).
- **Dùng khi:** có test chậm. **Không dùng khi:** cả bộ chạy dưới vài giây (chia nhóm chỉ thêm việc).
- **Nguồn:** Google mô tả pre/post-submit [1] (Chắc); phân nhóm bằng `Trait` và `--filter` đã chạy ở Lab 5.1; mốc "targeted vs full" của dự án nằm ở [`quality-gate.md`](../../../.agents/rules/quality-gate.md).

#### Chạy song song và cô lập
**Cơ chế:** xUnit chạy các **collection** song song; mỗi lớp test mặc định là một collection, test trong một lớp chạy tuần tự ([Part 2](part-2-core-skills.md), nguồn xUnit "Running Tests in Parallel": Vừa). Hai lớp cùng đọc ghi một biến tĩnh, một file hay một cổng sẽ **chen nhau** tuỳ thời điểm; đó là nguồn flaky rất hay gặp. Fowler xếp "thiếu cô lập" là nguyên nhân phổ biến và khuyên mỗi test tự dựng trạng thái đầu từ đầu hơn là dọn sau [2].
- **Cách xử lý:** bỏ trạng thái dùng chung; nếu không bỏ được, đặt các lớp vào **cùng `[Collection]`** để chạy tuần tự (Lab 5.3); tắt song song toàn bộ là phương án thô.

#### Cách đo coverage (coverlet) và ngưỡng
**Cơ chế:** coverlet **chèn mã đếm** vào assembly rồi ghi báo cáo (Cobertura/…); `coverlet.collector` dùng với `--collect:"XPlat Code Coverage"`, `coverlet.msbuild` thêm kiểm **ngưỡng** làm build fail [4][5].
- **Mặc định loại assembly test:** `IncludeTestAssembly` mặc định `false` [5]. Nếu code nguồn nằm trong cùng project với test, coverage báo 0 (Lab 5.1, L5).
- **Ngưỡng:** `Threshold` kết hợp `ThresholdType` (`line`, `branch`, `method`, nhiều giá trị cách nhau dấu phẩy) và `ThresholdStat` (`minimum` mặc định, `total`, `average`) [4]. Mặc định kiểm **từng module**, không phải tổng.
- **Giới hạn:** chỉ báo dòng/nhánh **đã chạy**, không báo test có `Assert` hợp lý ([Part 4](part-4-advanced.md)); Fowler: coverage tốt để tìm chỗ chưa test, kém để đo chất lượng, và dễ thành chỉ tiêu làm hỏng test [6] (Vừa).

#### Vòng đời một test hồi quy
1. Nhận báo cáo lỗi, ghi đầu vào và kết quả sai thật.
2. Viết **một test** với đầu vào đó và kết quả đúng; chạy: phải **đỏ** (nếu xanh là chưa tái hiện đúng).
3. Sửa tối thiểu cho test xanh; chạy cả nhóm.
4. **Thử lại ngược:** hoàn tác bản sửa, test phải đỏ lại (chứng minh test thật sự canh gác).
5. Giữ test, đặt tên theo hành vi; ghi issue/sự cố trong comment ngắn nếu lý do không hiển nhiên.
- **Dùng khi:** mọi bug. **Không dùng khi:** lỗi do hạ tầng (không tái hiện bằng test đơn vị) → cần runbook, giám sát.
- **Nguồn:** quy tắc dự án ([`testing.md`](../../../.agents/rules/testing.md)); Lab 5.2 đã chạy.

#### Quarantine có hạn
**Cơ chế:** chuyển test flaky sang nhóm riêng để không làm đỏ pipeline chính, **và** có giới hạn để không bị quên: Fowler gợi ý giới hạn số lượng (ví dụ 8 test) hoặc thời gian (ví dụ một tuần); đội Mingle đưa nhóm quarantine vào pipeline **một giai đoạn sau** nhóm khoẻ để buộc phải xử lý [2].
- **Rủi ro:** Google lưu ý quarantine tự động có thể **che** lỗi tranh chấp thật trong code; đánh dấu "flaky, 3 lần liền mới báo đỏ" làm chậm việc phát hiện test hỏng thật (ví dụ test 15 phút phải chờ 45 phút) [1].

## L4 Hands-on — Lab

Thư mục làm việc là thư mục tạm bạn tự tạo, không phải repo. Môi trường chạy lab: Windows 11, .NET SDK 10.0.402, xunit 2.9.3, coverlet 6.0.4, Git Bash.

#### Lab 5.1 — Phân nhóm test, đo coverage và gate
**Mục tiêu:** chạy riêng nhóm nhanh, đo line và branch coverage, làm gate ngưỡng fail rồi pass.
**Chuẩn bị:** hai project (domain và test), vì coverlet loại assembly test (L5).
```bash
mkdir lh-ops && cd lh-ops
dotnet new sln -n LhOps --format sln -o .
dotnet new classlib -n LhOps.Domain -o LhOps.Domain      # chứa LegacyPricing.Quote (Part 4, Lab 4.3)
dotnet new xunit -n LhOps -o LhOps
dotnet add LhOps reference LhOps.Domain/LhOps.Domain.csproj
dotnet sln add LhOps.Domain/LhOps.Domain.csproj LhOps/LhOps.csproj
```
Test có nhãn:
```csharp
[Trait("Category", "unit")]
public class PricingUnitTests
{
    [Fact] public void Quote_GoldFifteenUnitsMember_Returns166() =>
        Assert.Equal(166m, LegacyPricing.Quote(15, "gold", true));
    [Fact] public void Quote_StandardTenUnitsGuest_Returns125() =>
        Assert.Equal(125m, LegacyPricing.Quote(10, "standard", false));
}

[Trait("Category", "docker")]
public class SimulatedContainerTests
{
    [Fact] public async Task Repository_WithRealDatabase_RoundTrips()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));   // stand-in for a real container start
        Assert.True(true);
    }
}
```
**Bước 1: chạy riêng nhóm nhanh và toàn bộ.**
```bash
dotnet test --filter "Category!=docker"
dotnet test
```
```text
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 135 ms
Passed!  - Failed: 0, Passed: 3, Skipped: 0, Total: 3, Duration: 3 s
```
**Bước 2: đo coverage.**
```bash
dotnet test --filter "Category!=docker" --collect:"XPlat Code Coverage" --results-directory ./cov
```
Trong `coverage.cobertura.xml`:
```text
<package name="LhOps.Domain" line-rate="1" branch-rate="0.8"
```
**Line 100% nhưng branch chỉ 80%:** dòng `if (tier == "platinum") ...` được chạy qua test "gold" (điều kiện sai), nhánh đúng chưa chạy.
**Bước 3: gate ngưỡng nhánh ≥ 90.**
```bash
dotnet add package coverlet.msbuild --version 6.0.4
dotnet test --filter "Category!=docker" -p:CollectCoverage=true -p:Include="[LhOps.Domain]*" -p:Threshold=90 -p:ThresholdType=branch
```
```text
| Total   | 100% | 80%    | 100%   |
error : The minimum branch coverage is below the specified 90
exit=1
```
**Bước 4:** thêm test `Quote_PlatinumTenUnitsMember_Returns100` rồi chạy lại cùng lệnh:
```text
Passed!  - Failed: 0, Passed: 3 ... | Total   | 100% | 100%   | 100%   |
exit=0
```
**Giải thích:** nhóm nhanh cho phản hồi trong chục mili-giây; ngưỡng **nhánh** bắt thiếu sót mà ngưỡng dòng bỏ qua; gate fail có mã thoát khác 0 nên CI chặn được.
**Dọn dẹp:** `cd .. && rm -rf lh-ops` (chỉ thư mục bạn tạo; thư mục `cov` nằm trong đó).

#### Lab 5.2 — Biến một sự cố thành test hồi quy
**Mục tiêu:** làm đúng vòng đời 5 bước ở L3 trên một lỗi có thật trong lab trước.
**Sự cố (mô phỏng):** INC-1: báo giá khách lẻ 5 đơn vị gói `standard` hiện **62**, hoá đơn kỳ vọng **63** (62,5 làm tròn lên). Đây cũng là chi tiết lạ đã thấy ở [Part 4 Lab 4.4](part-4-advanced.md).
**Bước 2: test đỏ trước.**
```csharp
[Trait("Category", "unit")]
public class RegressionTests
{
    // Incident INC-1: guest price for 5 standard units shown as 62, invoice expects 63.
    [Fact]
    public void Quote_StandardFiveUnitsGuest_RoundsHalfAwayFromZero() =>
        Assert.Equal(63m, LegacyPricing.Quote(5, "standard", false));
}
```
```text
Failed LhOps.RegressionTests.Quote_StandardFiveUnitsGuest_RoundsHalfAwayFromZero
Expected: 63
Actual:   62
Failed!  - Failed: 1, Passed: 0, Total: 1
```
**Bước 3: sửa tối thiểu.** Đổi `Math.Round(price, 0)` thành `Math.Round(price, 0, MidpointRounding.AwayFromZero)`:
```text
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4
```
**Bước 4: hoàn tác bản sửa** (đưa về `Math.Round(price, 0)`), chạy riêng nhóm hồi quy:
```text
Failed!  - Failed: 1, Passed: 0, Total: 1
```
rồi áp lại bản sửa: `Passed: 4`.
**Giải thích:** nguyên nhân là `Math.Round` mặc định làm tròn về số chẵn gần nhất ở mức .5 (62,5 → 62); điều này **xác nhận** giả thuyết còn ⚠ ở Part 4. Hai test đặc tính 166 và 100 không bị ảnh hưởng (không có phần .5 ở các đầu vào đó). Snapshot của Lab 4.4 sẽ đổi ở các dòng có phần .5; ⚠ chưa chạy lại snapshot sau bản sửa.
**Dọn dẹp:** không có.

#### Lab 5.3 — Tái hiện và sửa một test flaky do trạng thái dùng chung
**Mục tiêu:** thấy flaky bằng số đo thật, không bằng cảm giác.
```csharp
public static class Counter { public static int Value; }

public class CounterAddTests
{
    [Fact] public async Task Add_FromZero_ReachesOne()
    {
        Counter.Value = 0;
        await Task.Delay(Random.Shared.Next(1, 20));
        Counter.Value++;
        Assert.Equal(1, Counter.Value);
    }
}

public class CounterResetTests
{
    [Fact] public async Task Reset_AfterWork_IsZero()
    {
        await Task.Delay(Random.Shared.Next(1, 20));
        Counter.Value = 0;
        await Task.Delay(Random.Shared.Next(1, 20));
        Assert.Equal(0, Counter.Value);
    }
}
```
**Bước 1: chạy 20 lần với cùng code đã build.**
```bash
for i in $(seq 1 20); do dotnet test --no-build 2>&1 | grep -q "Passed!" && pass=$((pass+1)) || fail=$((fail+1)); done
```
```text
20 runs: pass=12 fail=8
Failed LhFlaky.CounterResetTests.Reset_AfterWork_IsZero
Expected: 0
Actual:   1
```
**Bước 2: sửa** bằng cách đặt hai lớp vào cùng collection để chạy tuần tự:
```csharp
[Collection("counter")] public class CounterAddTests { ... }
[Collection("counter")] public class CounterResetTests { ... }
```
```text
after fix 20 runs: pass=20 fail=0
```
**Giải thích:** hai lớp là hai collection khác nhau nên chạy song song, `Counter.Value` bị ghi chen nhau; kết quả phụ thuộc thứ tự các `Delay` ngẫu nhiên. 8/20 lần đỏ với cùng code là đúng định nghĩa "flaky" [1]. Cách sửa tốt hơn là **bỏ biến tĩnh dùng chung** [2]; `[Collection]` là cách tối thiểu khi chưa bỏ được. 20/20 xanh là bằng chứng nhỏ, không phải chứng minh.
**Dọn dẹp:** `rm -rf lh-flaky`.

## L5 Troubleshooting — Pipeline test hỏng

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| Coverage báo `line-rate="0"`, không có dòng class | mở `coverage.cobertura.xml` | code nguồn cùng project với test; coverlet loại assembly test | tách project nguồn, test tham chiếu [5] |
| `MSB1008: Only one project can be specified` | in lệnh đầy đủ ở thông báo | Git Bash đổi `/p:...` thành đường dẫn | viết `-p:Threshold=90` thay `/p:` |
| Gate coverage đỏ dù tổng cao | đọc bảng Line/Branch/Method | ngưỡng áp lên **từng module**; hoặc loại ngưỡng khác (branch) | chọn `ThresholdType` và `ThresholdStat` có chủ đích [4] |
| Test đỏ ngẫu nhiên, chạy lại thì xanh | chạy 20 lần, đếm | trạng thái tĩnh dùng chung, thứ tự không ổn định | bỏ trạng thái chung hoặc cùng `[Collection]` [2] |
| `--filter "Category!=docker"` vẫn khởi động container | xem test nào chạy với `--list-tests` | fixture cấp lớp nằm ở lớp không gắn nhãn | tách test nhanh sang lớp không dùng fixture (đã gặp ở [Part 3](part-3-multi-component.md)) |
| Test xanh nhưng sự cố vẫn xảy ra | hỏi "test nào lẽ ra bắt được?" | bộ test không kiểm đại lượng đó (CPU, thời gian) | thêm test/giám sát cho đại lượng đó [3] |

**Thẻ 1: coverage 0%.** Chạy coverage khi `LegacyPricing` còn nằm cùng project `LhOps` với test:
```text
Passed!  - Failed: 0, Passed: 2, Skipped: 0, Total: 2
line-rate="0" branch-rate="0"        (không có dòng <class ...>)
```
Test pass nhưng báo cáo trống vì assembly test bị loại theo mặc định [5]. Sau khi tách `LhOps.Domain`, `line-rate="1" branch-rate="0.8"`.
**Thẻ 2: `/p:` bị đổi trên Git Bash.**
```text
MSBUILD : error MSB1008: Only one project can be specified.
... p:CollectCoverage=true p:Threshold=95 p:ThresholdType=line ...
```
Git Bash (MSYS) chuyển đối số bắt đầu bằng `/` thành đường dẫn Windows nên `/p:Threshold=95` bị cắt. Viết `-p:Threshold=95` chạy được. Tài liệu coverlet dùng cú pháp `/p:` cho shell khác [4]. ⚠ Chi tiết cơ chế chuyển đổi đường dẫn của MSYS chỉ suy ra từ triệu chứng, chưa đọc tài liệu MSYS.

## L6 Production — Quyết định và đánh đổi

| Quyết định | Một phía | Phía kia | Gợi ý |
|---|---|---|---|
| Ngưỡng coverage | cứng, toàn dự án: dễ chặn, dễ bị lách | không ngưỡng: không chặn gì | dự án này: không cứng toàn cục, **không giảm** vùng đã có |
| Tự chạy lại test fail | giảm phiền nhưng che flaky | không chạy lại: lộ flaky | chạy lại chỉ để đo, và ghi nhận số lần [1] |
| Quarantine | giữ pipeline xanh | che lỗi tranh chấp thật | giới hạn số và thời gian, có người nhận [2] |
| Dữ liệu test | thật: sát thực tế, rủi ro bảo mật | giả: an toàn, có thể lệch thực tế | giả/sinh trong test; không dữ liệu hay secret thật |

- **Coverage:** quy tắc dự án ([`quality-gate.md`](../../../.agents/rules/quality-gate.md)): không ngưỡng cứng toàn dự án (dự án học tập); logic quan trọng (idempotency, outbox, auth, domain) bắt buộc có test; PR không giảm coverage vùng đã có. Part này không đặt thêm con số.
- **Chi phí:** nhóm chậm dùng Docker và cổng host chung nên theo [`COMMANDS.md`](../../../COMMANDS.md) chạy **mỗi lần một assembly**; đó là cái giá của việc dùng chung cổng.
- **Bảo mật:** `*.example` cho cấu hình cần secret; file thật nằm ngoài git ([LL-001](../../lessons/LL-001-secrets-committed-in-env-files.md)). Quét secret bằng `gitleaks` là biện pháp **chưa cài** (todo trong bài học); hiện chỉ có quét diff thô theo `COMMANDS.md`.
- ⚠ **Chưa đọc** nguồn bên ngoài về chiến lược dữ liệu test (ẩn danh hoá, dữ liệu tổng hợp) hay về mutation/coverage ở quy mô lớn trong CI; phần "dữ liệu giả" ở trên chỉ dựa vào quy tắc của dự án, không dựa nguồn ngoài.

## L7 Architecture — Chiến lược test cho hệ nhiều service

| Lớp kiểm tra | Bắt được | Không bắt được | Khi nào chạy |
|---|---|---|---|
| Unit (nhanh) | logic domain, biên, công thức | ghép nối thật, cấu hình | mỗi commit |
| Contract | lệch API/event giữa service | logic bên trong | khi đổi contract, ở provider và consumer |
| Integration (DB, Docker) | truy vấn, migration, ghi thật | tải, môi trường thật | PR và trước merge |
| Synthetic / giám sát | hệ đang chạy hỏng, quá tải | lỗi logic trước phát hành | liên tục sau triển khai [3] |

**Cách chọn nhanh:** phản hồi nhanh nhờ nhiều test nhỏ ở đáy; ghép nối kiểm bằng contract và integration ít mà chắc; những gì test không thấy (tải, CPU, hạ tầng) giao cho giám sát và rollout theo tầng. Test **không** thay thế quan sát sau triển khai (Cloudflare: cổng CI xanh, vẫn sập [3]).

### Trong thực tế (real-world)

#### Google (2016): flaky test ở quy mô lớn
- **Vấn đề:** tín hiệu từ test là nền tảng để quyết định nộp và phát hành code, nhưng test flaky làm nó nhiễu.
- **Số liệu:** khoảng 1,5% lượt chạy báo flaky; gần 16% test có mức flaky nào đó; khoảng 84% chuyển pass→fail ở post-submit gắn với test flaky; dự án trung bình có khoảng 1.000 test, nên với 1,5% flaky thì khoảng **15 test** đỏ giả mỗi lần phát hành [1].
- **Cách xử lý:** chạy lại test fail, đánh dấu "flaky" (chỉ báo đỏ khi fail 3 lần liền), công cụ theo dõi độ flaky và tự quarantine, công cụ truy ra thay đổi làm tăng độ flaky; một nhóm riêng cho thông tin về flakiness [1].
- **Điều thừa nhận:** tốc độ chèn flaky mới ngang tốc độ sửa; cách "đánh dấu flaky" làm chậm phát hiện và khuyến khích bỏ qua; quarantine có thể che lỗi thật [1].
- Nguồn: [Flaky Tests at Google and How We Mitigate Them](https://testing.googleblog.com/2016/05/flaky-tests-at-google-and-how-we.html) (Google Testing Blog, 2016-05-27; Chắc).

#### Cloudflare (2019-07-02): CI xanh vẫn sập 27 phút
- **Chuyện gì xảy ra:** một quy tắc WAF có biểu thức chính quy backtrack nặng làm cạn CPU trên mọi máy phục vụ HTTP/HTTPS toàn cầu; khách thấy lỗi 502 trong khoảng 27 phút [3].
- **Vì sao test không bắt được:** bộ test WAF gồm nhiều unit test và một tập lớn request HTTP (cần chặn/cần cho qua), nhưng **không có cách phát hiện CPU tăng vọt**; log các build trước cho thấy thời gian chạy bộ test cũng không tăng với quy tắc gây sự cố [3].
- **Nhiều nguyên nhân hội tụ:** biểu thức dễ backtrack; một lớp bảo vệ CPU bị gỡ nhầm khi refactor vài tuần trước; engine regex không có đảm bảo độ phức tạp; bộ test không đo CPU; quy trình cho phép đẩy quy tắc toàn cầu không theo tầng; rollback chậm; cảnh báo traffic chậm [3].
- **Sửa về phía test:** thêm **profiling hiệu năng cho mọi quy tắc** vào bộ test, đổi engine sang loại có đảm bảo thời gian chạy, và đổi quy trình sang rollout theo tầng [3].
- **Synthetic test hoạt động:** cảnh báo đầu tiên là một synthetic test của WAF, ba phút sau khi triển khai [3].
- Nguồn: [Details of the Cloudflare outage on July 2, 2019](https://blog.cloudflare.com/details-of-the-cloudflare-outage-on-july-2-2019/) (Cloudflare Blog, 2019-07-12; Chắc).

#### Fowler (2011): test không tất định là "nhiễm trùng" của bộ test
- **Quan điểm:** test không tất định vô dụng và làm hỏng cả bộ; khuyên cách ly (quarantine) có hạn, rồi sửa theo nhóm nguyên nhân: thiếu cô lập, bất đồng bộ, dịch vụ ngoài, thời gian, rò rỉ tài nguyên [2].
- **Cái giá của quarantine:** test bị ném vào quarantine rồi quên → khả năng phát hiện lỗi của bộ test **mòn dần**; cần giới hạn số lượng hoặc thời gian [2].
- Nguồn: [Eradicating Non-Determinism in Tests](https://martinfowler.com/articles/nonDeterminism.html) (2011-04-14; Chắc, phần đầu bài; các mục sau Lack of Isolation chưa đọc đủ).

**Mô hình phổ biến:** cách ly test hỏng để giữ tín hiệu [1][2]; đo đại lượng test chưa thấy (CPU) và tách lớp giám sát [3]; mỗi sự cố để lại một test hoặc một giám sát [3].
**Khi thất bại hoặc bị bỏ:** Cloudflare là trường hợp "có test, có CI, vẫn sập"; Google thừa nhận không hết flaky [1]. Chưa tìm được case công ty **bỏ** chiến lược CI nhiều tầng, hay một case post-mortem công khai nơi "test hồi quy sự cố" **bị bỏ sót** và lỗi lặp lại; hai chỗ này ghi vào phần chưa kiểm.

### Trong TeamBoard
- Chưa có CI trong repo; gate chạy cục bộ ([`quality-gate.md`](../../../.agents/rules/quality-gate.md), [`COMMANDS.md`](../../../COMMANDS.md)).
- Phân nhóm bằng `Category`: lệnh nhanh `--filter "Category!=docker"` đã nêu ở `COMMANDS.md` mục 2; kịch bản Reqnroll tự sinh `Trait("Category", ...)` từ tag (đã thấy mã sinh như `TB-033`, `board-service`, `S01`, `FR-006` trong thư mục `obj/` của `board-service.Tests`).
- Chưa có công cụ coverage, `gitleaks`, hay cơ chế quarantine/theo dõi flaky trong repo; thêm công cụ mới đi qua [`dependency-policy.md`](../../../.agents/rules/dependency-policy.md). Các lab ở đây chạy ngoài repo.
- Chưa có ADR về chiến lược test trong CI hay ngưỡng coverage.

## Tự kiểm tra

1. Line coverage 100% mà branch 80% nghĩa là gì? <details><summary>Đáp án</summary>Mọi dòng đã chạy nhưng một nhánh điều kiện (ví dụ nhánh đúng của `if` trên một dòng) chưa chạy; Lab 5.1.</details>
2. Vì sao coverage báo 0 khi test đều xanh? <details><summary>Đáp án</summary>Code nằm cùng project với test, coverlet loại assembly test theo mặc định; tách project nguồn.</details>
3. Test hồi quy đúng phải đi qua mấy bước và bước nào chứng minh test thật sự canh gác? <details><summary>Đáp án</summary>Đỏ trước khi sửa, xanh sau khi sửa, rồi hoàn tác bản sửa và thấy test đỏ lại.</details>
4. Vì sao chạy lại test fail cho đến khi xanh là cách xử lý flaky nguy hiểm? <details><summary>Đáp án</summary>Che tranh chấp thật trong code và làm người ta bỏ qua báo động; Google nêu cả hai [1].</details>
5. Sự cố Cloudflare 2019 cho thấy giới hạn nào của CI xanh? <details><summary>Đáp án</summary>Bộ test không đo CPU nên không thấy lỗi hiệu năng; cần test/giám sát đúng đại lượng và rollout theo tầng [3].</details>
6. Việc chạy được: lặp Lab 5.3 nhưng thay `Counter.Value` bằng biến cục bộ trong từng test (không dùng static). Tiêu chí: 20/20 lần xanh **không** cần `[Collection]`, và bạn giải thích vì sao.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [1] [Google Testing Blog, Flaky Tests at Google and How We Mitigate Them](https://testing.googleblog.com/2016/05/flaky-tests-at-google-and-how-we.html) (2016): toàn bài.
- [2] [Fowler, Eradicating Non-Determinism in Tests](https://martinfowler.com/articles/nonDeterminism.html) (2011): phần giới thiệu, "Why", "Quarantine", "Lack of Isolation" (đến mục giao dịch DB); các mục Asynchronous, Remote Services, Time, Resource Leaks chưa đọc.
- [3] [Cloudflare, Details of the outage on July 2, 2019](https://blog.cloudflare.com/details-of-the-cloudflare-outage-on-july-2-2019/): phần diễn biến, vận hành, nguyên nhân hội tụ, hành động sau sự cố; phần phụ lục regex chưa đọc.
- [4] [coverlet, MSBuild integration](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/MSBuildIntegration.md): mục Threshold, `ThresholdType`, `ThresholdStat`.
- [5] [coverlet, VSTest integration](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md): mục tuỳ chọn, `IncludeTestAssembly` mặc định `false`.
- Kết quả ba lab trong bài này, chạy 2026-11-26; quy tắc trong repo: `testing.md`, `quality-gate.md`, `COMMANDS.md`, `LL-001`.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- [6] [Fowler, TestCoverage](https://martinfowler.com/bliki/TestCoverage.html) (2012): đọc qua agent ở Part 4, chưa mở lại ở Part này.
- [xUnit] [Running Tests in Parallel](https://xunit.net/docs/running-tests-in-parallel): đã dùng ở Part 2, chưa mở lại ở Part này.

**Chưa kiểm hoặc chưa tìm được:**
- Chưa chạy một pipeline CI thật (không có CI trong repo); mọi lệnh gate chỉ chạy cục bộ.
- Lab 5.1 dùng `Task.Delay` giả lập container; chưa đo thời gian một container thật (Part 3 đã chạy Testcontainers nhưng không đo tách).
- Cơ chế Git Bash/MSYS đổi đối số `/p:`: chỉ suy ra từ triệu chứng, chưa đọc tài liệu.
- Chưa có nguồn ngoài cho dữ liệu test và bảo mật test; chưa có nguồn cho synthetic monitoring ngoài ví dụ Cloudflare; chưa đọc Google "Software Engineering at Google" (kích cỡ test), DORA hay sách Continuous Delivery.
- Chưa có case công ty bỏ chiến lược CI nhiều tầng hay case lỗi lặp lại do thiếu test hồi quy.
- Snapshot Lab 4.4 chưa chạy lại sau bản sửa làm tròn.
