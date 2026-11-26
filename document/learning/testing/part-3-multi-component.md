# Part 3 — Multi-component Applications (Testing)

> [!NOTE]
> **Mục tiêu:** test đúng ranh giới giữa các thành phần (HTTP, DB, dịch vụ khác); chọn mức test phù hợp thay vì test mọi thứ end-to-end.
> **Cần biết trước:** [Part 2](part-2-core-skills.md); Docker chạy được cho Lab 3.2 và 3.4.
> **Thời lượng:** ~6 đến 8 giờ nếu làm đủ lab · **Đầu ra kiểm được:** một integration test chạy trên DB thật trong container và một contract test bắt được thay đổi phá vỡ của provider.
> **Part trước / sau:** [←](part-2-core-skills.md) · [→](part-4-advanced.md)

## L1 What — Các mức test nhiều thành phần

#### Integration test (hẹp và rộng)
**Là gì:** test kiểm các phần phát triển riêng có chạy đúng khi nối với nhau. Fowler chia **hẹp** (chỉ kiểm code của mình nói chuyện với dịch vụ ngoài, dịch vụ ngoài thay bằng double) và **rộng** (cần bản chạy thật của mọi dịch vụ; ông thích gọi là system hay end-to-end test) [3].
**Ví dụ đời thường:** thử khớp ổ khoá với chìa (hẹp) so với thử cả quy trình ra vào toà nhà (rộng).
**Use case:**
- Endpoint `/tasks` với pipeline ASP.NET Core thật (routing, JSON) → integration test hẹp, chạy trong tiến trình.
- Câu SQL và ràng buộc DB → integration test với DB thật.

**Lưu ý:** thuật ngữ này bị dùng lẫn lộn; chốt định nghĩa trong nhóm (xem [Part 1](part-1-fundamentals.md#test-pyramid-trophy-honeycomb)).

#### Component test
**Là gì:** kiểm **một phần xác định** của hệ thống (khác test rộng bao phủ gần hết ứng dụng); phần ngoài thường thay bằng double [4]. ⚠ Định nghĩa trong bài của Toby Clemson (microservice testing) chưa đọc được thân bài.
**Use case:** kiểm riêng `board-service` qua API công khai, DB và bus thay bằng double hoặc container.

#### Contract test
**Là gì:** hai nghĩa gần nhau.
- Theo Fowler: kiểm rằng **double của bạn vẫn khớp** dịch vụ thật; chạy theo lịch riêng (ví dụ hằng ngày), lỗi là tín hiệu chứ không nhất thiết chặn build [9].
- Consumer-driven contract (Pact): consumer ghi lại những gì nó dùng; provider chạy lại để chứng minh vẫn đáp ứng [7][8].

**Use case:**
- Frontend/gateway dựa vào `title`, `status` của `/tasks` → contract ghi hai trường đó; đổi tên `title` ở provider là test đỏ trước khi triển khai.
- Double của dịch vụ ngoài trong integration test hẹp → contract test giữ double trung thực [3].

#### End-to-end test
**Là gì:** chạy qua toàn bộ hệ thống thật như người dùng. Chậm, khó gỡ, dễ flaky; chỉ giữ ít luồng giá trị cao ([Part 1](part-1-fundamentals.md), Vocke).
**Use case:** đăng nhập → tạo board → tạo task, một luồng chính, không phủ mọi nhánh.

#### WebApplicationFactory và Testcontainers
**Là gì:** `WebApplicationFactory<Program>` (gói `Microsoft.AspNetCore.Mvc.Testing`) khởi chạy ứng dụng trong bộ nhớ (TestServer, không mở cổng) và cho `HttpClient` gọi vào [1]. Testcontainers cho .NET dựng container Docker dùng một lần cho test (ví dụ PostgreSQL), đọc cổng host ngẫu nhiên [2].
**Use case:**
- Gọi endpoint thật, thay một dịch vụ (`ITaskStore`) bằng stub cho một test → `WithWebHostBuilder` [1].
- Kiểm repository với PostgreSQL thật → `Testcontainers.PostgreSql`.

**Lưu ý:** Microsoft Learn khuyên giữ integration test cho các tình huống hạ tầng chính, ưu tiên unit test khi được [1].

## L2 Why — Vì sao unit test chưa đủ

- **Fake nói "ổn", thật thì hỏng.** Fake lưu trong bộ nhớ chấp nhận hai task trùng tên; PostgreSQL có ràng buộc `UNIQUE` thì từ chối. Lab 3.2 chạy đúng trường hợp này.
- **Chỗ lỗi hay nằm ở ranh giới.** Serialize/deserialize, SQL, header, cấu hình DI chỉ lộ khi các phần chạy cùng nhau (Vocke, [Part 1](part-1-fundamentals.md)).
- **Provider đổi, consumer không biết.** Provider sửa một trường, consumer hỏng khi chạy thật. Contract đưa lỗi này lên giai đoạn build [8].
- **Nhưng test tích hợp đại trà cũng sai.** Rainsberger lập luận: số đường đi qua nhiều tầng nhân lên theo từng nhánh, nên test tích hợp chỉ phủ được vài đường; ông ưu tiên test tập trung, nhỏ, chạy kỹ từng phần cộng với contract giữa chúng [5].
- **DB thay thế khác DB thật.** Tài liệu EF Core nói provider in-memory khác DB thật ở những điểm quan trọng (giao dịch, SQL thô, so sánh chuỗi) và khuyên thử trên hệ DB thật; SQLite in-memory giống hơn nhưng vẫn lệch [6].

## L3 How — Cơ chế

#### WebApplicationFactory: ứng dụng trong bộ nhớ
**Ai làm gì:** test nhận `WebApplicationFactory<Program>` qua `IClassFixture`; `CreateClient()` trả `HttpClient` nối thẳng vào TestServer; `WithWebHostBuilder(... ConfigureServices ...)` tạo factory riêng cho một test để thay service bằng double [1]. Lớp `Program` phải nhìn thấy được: cuối `Program.cs` thêm `public partial class Program;` [1].
```text
test ──HttpClient──> TestServer (trong tiến trình) ──> pipeline thật ──> ITaskStore (thật hoặc double)
```
- **Ưu:** nhanh (không mở cổng), phủ routing, binding, DI, middleware. **Nhược:** vẫn chưa chạy hạ tầng ngoài (DB thật, mạng).
- **Dùng khi:** kiểm hành vi HTTP của một service. **Không dùng khi:** cần kiểm hành vi riêng của DB hoặc nhiều tiến trình.
- **Content root:** factory cần tìm thư mục project của ứng dụng; xem hai lỗi thật ở L5.
- **Nguồn:** Microsoft Learn (Vừa); Lab 3.1 đã chạy.

#### Testcontainers: hạ tầng thật dùng một lần
**Ai làm gì:** code test khai báo image, gọi `StartAsync()`, chờ kiểm tra sẵn sàng, đọc chuỗi kết nối với cổng host ngẫu nhiên; cuối chạy, Ryuk (resource reaper) dọn các tài nguyên Docker của phiên, kể cả khi test fail; chỉ hỗ trợ Linux container [2].
```text
fixture.InitializeAsync ──> docker create/start postgres ──> pg_isready (lặp) ──> sẵn sàng
test chạy ──> fixture.DisposeAsync ──> xoá container
```
- **Ưu:** DB thật, không dùng chung trạng thái giữa lần chạy. **Nhược:** cần Docker, mỗi lần khởi động tốn vài giây (Lab 3.2 thấy cỡ 4 giây); Windows/macOS chỉ chạy Linux container.
- **Dùng khi:** repository, migration, ràng buộc DB. **Không dùng khi:** logic thuần (xUnit là đủ).
- **Nguồn:** Testcontainers docs (Vừa); Lab 3.2 và Lab 3.4 đã chạy. ⚠ Tài liệu không nói chi tiết vòng đời theo test hay theo fixture; giấy phép của gói chưa kiểm.

#### Integration test hẹp với double ở biên
**Ai làm gì:** kiểm code của service gọi dịch vụ ngoài, dịch vụ ngoài thay bằng double chạy trong tiến trình hoặc qua mạng; contract test giữ double đúng với thật [3].
- **Ưu:** nhanh, ổn định, trông như unit test. **Nhược:** double lệch thật nếu không có contract.
- **Dùng khi:** service có nhiều phụ thuộc. Nguồn: Fowler (Chắc).

#### Consumer-driven contract (Pact)
**Ai làm gì:** [7]
1. Consumer mô tả mỗi **interaction**: request mong đợi và response **tối thiểu** nó cần; test consumer chạy với mock server của Pact.
2. Test consumer đạt thì sinh **pact file** (hợp đồng).
3. Provider chạy lại từng request của pact trên service thật; đạt khi response chứa **ít nhất** dữ liệu đã mô tả, thêm trường mới không làm hỏng.
4. **Provider state** dựng điều kiện trước ("user tồn tại").
5. Pact Broker lưu pact và kết quả; `can-i-deploy` hỏi bảng tương thích trước khi triển khai, trả 0 (an toàn) hoặc 1 [7].

```text
consumer test ──> pact file ──> (Broker) ──> provider verification ──> can-i-deploy
```
- **Ưu:** bắt thay đổi phá vỡ trước khi triển khai, không cần dựng cả hệ thống. **Nhược:** thêm hạ tầng (Broker), quy trình phối hợp; Pact nói rõ nó **không** hợp với API công khai không biết consumer, và không thay test chức năng hay tải [7].
- **Dùng khi:** hai phía trong tầm kiểm soát, đều đang đổi, ít consumer [7]. Pact.NET: bản 5.x, MIT, lõi là thư viện native (Rust), một số nền tảng không hỗ trợ [13]; ⚠ chưa kiểm hỗ trợ .NET 10, chưa chạy trong Part này.
- **Nguồn:** Pact docs (Chắc cho cách hoạt động, Vừa cho phần còn lại); Robinson 2006 (Vừa) [8].

#### Tolerant reader
**Ai làm gì:** consumer chỉ đọc phần nó cần, bỏ qua trường lạ, ít giả định cấu trúc, gom việc parse vào một chỗ (DTO); chia sẻ test của reader cho provider [9].
- **Ưu:** provider thêm trường không làm consumer hỏng. **Nhược:** lỗi thật có thể bị nuốt nếu đọc quá lỏng.
- **Dùng khi:** luôn với contract nội bộ.

#### Kiểm hợp đồng từ OpenAPI
**Ai làm gì:** hai hướng bổ sung, không thay Pact [10]:
- `oasdiff`: so hai bản OpenAPI, báo thay đổi phá vỡ; chạy được trong CI; chỉ so **đặc tả với đặc tả**, không kiểm server đang chạy.
- Schemathesis: sinh test từ OpenAPI và tìm lỗi server, vi phạm schema.
- **Dùng khi:** có OpenAPI làm nguồn sự thật. Nguồn: README các công cụ (Vừa); chưa chạy.

#### Nhóm test theo tài nguyên
**Ai làm gì:** gắn trait (`Category=docker`) cho test cần Docker để tách nhóm nhanh và nhóm chậm; chạy `--filter "Category!=docker"` ở vòng lặp nhanh. Cẩn thận class fixture (L5).
- **Dùng khi:** luôn. Dự án làm đúng như vậy (xem Trong TeamBoard).

#### Test tính idempotent của consumer
**Ai làm gì:** gửi **cùng một message hai lần** (cùng `eventId`) và kiểm hiệu ứng chỉ xảy ra một lần. ⚠ Kiến thức chung, chưa có nguồn mở được trong Part này, chưa có lab; dự án bắt buộc có test này (`.agents/rules/testing.md`), xem thêm `.agents/rules/messaging.md`.

## L4 Hands-on — Lab

Môi trường: Windows 11, Docker Desktop (Docker 29.5.3), .NET SDK 10.0.402; `Microsoft.AspNetCore.Mvc.Testing` 10.0.13, `Testcontainers.PostgreSql` 4.16.0, `Npgsql` 10.0.3, `xunit` 2.9.3, image `postgres:17-alpine` (cùng phiên bản với `src/identity-service.Tests`); chạy 2026-11-26. Cả ba lab đầu dùng một project `LhInteg`.

#### Lab 3.0 — Dựng project (đúng bố cục)
```bash
mkdir lh-integ && cd lh-integ && mkdir LhInteg
# trong LhInteg/: LhInteg.csproj (Sdk "Microsoft.NET.Sdk.Web", IsTestProject=true,
#   gói Mvc.Testing, Npgsql, Testcontainers.PostgreSql, xunit, runner), Program.cs, Usings.cs
dotnet new sln -n LhInteg && dotnet sln add LhInteg/LhInteg.csproj
```
`Program.cs`: minimal API với `GET /health/live`, `GET /tasks`, `POST /tasks`, interface `ITaskStore` (bản `InMemoryTaskStore` mặc định), record `NewTask(Title)` và `TaskDto(Id, Title, Status)`, cuối file `public partial class Program;`.
**Bố cục quan trọng:** file solution ở gốc, project ở **thư mục con trùng tên**; sai bố cục sẽ gặp hai lỗi ở L5.
**Dọn dẹp:** không có.

#### Lab 3.1 — Integration test hẹp với WebApplicationFactory
**Mục tiêu:** gọi endpoint thật trong tiến trình và thay một service bằng stub cho một test.
```csharp
public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateTask_ValidTitle_ReturnsCreatedAndAppearsInList()
    {
        var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync("/tasks", new NewTask("Write report"));
        var list = await client.GetFromJsonAsync<List<TaskDto>>("/tasks");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Contains(list!, t => t.Title == "Write report" && t.Status == "Todo");
    }

    [Fact]
    public async Task CreateTask_WithFakeStoreSwappedIn_UsesTheFake()
    {
        var swapped = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<ITaskStore>();
            s.AddSingleton<ITaskStore, StubStore>();
        }));
        var list = await swapped.CreateClient().GetFromJsonAsync<List<TaskDto>>("/tasks");

        Assert.Equal("From stub", Assert.Single(list!).Title);
    }
}
```
(thêm test `Live_Request_Returns200` và lớp `StubStore : ITaskStore` trả một `TaskDto("1", "From stub", "Todo")`.)
```bash
dotnet test --logger "console;verbosity=normal"
```
**Đầu ra thật:**
```text
  Passed ApiTests.Live_Request_Returns200 [293 ms]
  Passed ApiTests.CreateTask_ValidTitle_ReturnsCreatedAndAppearsInList [31 ms]
  Passed ApiTests.CreateTask_WithFakeStoreSwappedIn_UsesTheFake [38 ms]
```
**Giải thích:** ứng dụng chạy trong bộ nhớ, không mở cổng; test thứ ba chỉ đổi một service.
**Dọn dẹp:** không có.

#### Lab 3.2 — Cùng ràng buộc, fake và DB thật cho kết quả khác
**Mục tiêu:** thấy chỗ test bằng fake nói "ổn" nhưng DB thật từ chối.
`PgTaskStore : ITaskStore` dùng Npgsql, bảng `tasks (id uuid PRIMARY KEY, title text NOT NULL UNIQUE, status text NOT NULL)`.
```csharp
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();
    public string ConnectionString => _container.GetConnectionString();
    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[Fact]
[Trait("Category", "docker")]
public async Task PostgresStore_DuplicateTitle_IsRejectedByUniqueConstraint()
{
    var store = new PgTaskStore(pg.ConnectionString);
    await store.EnsureSchemaAsync();
    await store.AddAsync("Write report");

    var ex = await Assert.ThrowsAsync<PostgresException>(() => store.AddAsync("Write report"));

    Assert.Equal("23505", ex.SqlState);
}
```
Đặt kèm `FakeStoreTests.InMemoryFake_DuplicateTitle_IsAccepted` (fake nhận hai task cùng tên).
```bash
dotnet test --filter "Category=docker" --logger "console;verbosity=normal"
```
**Đầu ra thật** (một phần):
```text
[testcontainers.org 00:00:04.25] Docker container afad972d8cc8 ready
[testcontainers.org 00:00:04.50] Delete Docker container afad972d8cc8
  Passed StoreTests.PostgresStore_DuplicateTitle_IsRejectedByUniqueConstraint [207 ms]
```
**Giải thích:** `23505` là mã lỗi vi phạm unique của PostgreSQL; fake không có ràng buộc nên cùng thao tác thành công. Container được tạo, kiểm `pg_isready` vài lần, rồi bị xoá.
**Dọn dẹp:** Testcontainers tự xoá container của phiên; kiểm bằng `docker ps -a`, không dùng lệnh dọn rộng.

#### Lab 3.3 — Contract test viết tay, phía provider
**Mục tiêu:** thấy contract bắt thay đổi phá vỡ và cho phép thay đổi bổ sung.
`contracts/web-tasks.contract.json` (consumer `web`, provider `board`):
```json
{
  "consumer": "web", "provider": "board",
  "request": { "method": "GET", "path": "/tasks" },
  "response": {
    "status": 200,
    "itemFields": { "id": "string", "title": "string", "status": "string" },
    "statusValues": ["Todo", "InProgress", "Done"]
  }
}
```
Test provider đọc file, gọi `/tasks` qua `WebApplicationFactory`, kiểm status, rằng body là mảng, mỗi phần tử có đủ trường kiểu string và `status` thuộc tập cho phép; **không** kiểm trường thừa. (File được copy ra output bằng `<Content Include="contracts\*.json" CopyToOutputDirectory="PreserveNewest" />`.)
```bash
dotnet test --filter "FullyQualifiedName~ContractTests"
```
**Đầu ra thật:**
```text
(provider đúng)            Passed!  - Failed: 0, Passed: 1
(đổi tên title → name)     Failed ContractTests.Provider_Satisfies_WebTasksContract
                           Missing field 'title'.
(thêm trường priority)     Passed!  - Failed: 0, Passed: 1
```
**Giải thích:** đổi tên trường bằng `[JsonPropertyName("name")]` làm contract đỏ; thêm trường `Priority` thì vẫn đạt vì contract chỉ nêu cái consumer dùng. Đây là mô phỏng nguyên lý của Pact, **không** thay Pact: không có consumer test, mock server, Broker hay `can-i-deploy`.
**Dọn dẹp:** khôi phục `Program.cs` sau hai lần thử; `cd .. && rm -rf lh-integ` khi xong.

#### Lab 3.4 — Chạy nhóm Docker của dự án
**Mục tiêu:** thấy test tích hợp thật của TeamBoard chạy trên PostgreSQL trong container.
```bash
dotnet test src/identity-service.Tests --filter "Category=docker"
```
**Đầu ra thật** (gốc repo, 2026-11-26):
```text
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 3 m 35 s - identity-service.Tests.dll (net10.0)
```
**Giải thích:** cần Docker Desktop; chạy mỗi lần **một** assembly vì các nhóm docker dùng chung cổng host (`COMMANDS.md`). Container tạo bởi `Testcontainers` trong `src/identity-service.Tests/Support/PostgresDatabase.cs`.
**Dọn dẹp:** không có (Testcontainers tự dọn).

## L5 Troubleshooting — Test tích hợp hỏng

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|
| `Solution root could not be located` | đọc thông báo lỗi | không có file solution trong cây thư mục | thêm `.sln`/`.slnx` ở gốc |
| `DirectoryNotFoundException ...\LhInteg\` | xem đường dẫn trong lỗi | solution cùng thư mục với csproj | project vào thư mục con trùng tên |
| Lọc `Category!=docker` vẫn khởi container | log `testcontainers.org` | class fixture chạy cho cả lớp | tách test nhanh sang lớp không fixture |
| Nhóm docker lỗi cổng bận khi chạy song song | `COMMANDS.md` mục 2 | các assembly dùng chung cổng host | chạy mỗi lần một assembly |
| Contract đỏ sau khi provider đổi | thông báo kiểu `Missing field` | thay đổi phá vỡ của provider | đổi consumer và contract cùng lúc |

**Thẻ 1: hai lỗi content root.** Chạy test `WebApplicationFactory` khi project đứng một mình, lần lượt thấy:
```text
System.InvalidOperationException : Solution root could not be located using application root ...\bin\Debug\net10.0\.
```
Thêm `.slnx` cùng thư mục với csproj thì thấy tiếp:
```text
System.IO.DirectoryNotFoundException : ...\lh-integ\LhInteg\
```
Nguyên nhân: factory tìm thư mục project theo file solution và tên project (`<solution>/<tên project>`); chỉ hết lỗi khi project nằm trong thư mục con đúng tên. Đây là suy ra từ hai lỗi quan sát được, ⚠ chưa đối chiếu với tài liệu về cơ chế này.

**Thẻ 2: fixture phá bộ lọc.** Test nhanh đặt chung lớp với `IClassFixture<PostgresFixture>`; chạy `--filter "Category!=docker"`, log vẫn có `Docker container ce2025a14da9 created` rồi `Delete`, bộ test mất khoảng 4 giây. Nguyên nhân: filter chọn test, còn fixture của lớp vẫn được dựng khi có test nào của lớp chạy. Sau khi chuyển test nhanh sang `FakeStoreTests` (không fixture), cùng lệnh không còn tạo container và `Passed: 5` trong chưa tới 1 giây.

## L7 Architecture — Chọn mức test

| Mức | Kiểm gì | Tốc độ | Cái giá |
|---|---|---|---|
| Unit | logic thuần, không I/O | mili-giây | không bắt lỗi ở ranh giới |
| Integration hẹp (WebApplicationFactory) | pipeline HTTP, DI, binding | trăm mili-giây | không chạm hạ tầng ngoài |
| Integration với container (Testcontainers) | SQL, ràng buộc, migration | vài giây mỗi lần khởi động | cần Docker, chậm hơn |
| Contract | tương thích consumer–provider | nhanh | phải phối hợp và bảo trì hợp đồng |
| End-to-end | luồng người dùng qua cả hệ thống | chậm | flaky, khó gỡ, tốn môi trường |

**Cách chọn:** logic nghiệp vụ → unit; điểm chạm DB → container thật, không mock DB để cho qua; ranh giới giữa service → contract; chỉ giữ vài luồng chính ở end-to-end. **Khi nào không cần e2e:** khi contract và integration hẹp đã phủ các ranh giới; GOV.UK Pay bỏ phần lớn e2e theo hướng này [11].

### Trong thực tế (real-world)

#### GOV.UK Pay (2019): thay end-to-end bằng contract testing
- **Vấn đề:** dựng mọi microservice bằng Docker Compose để chạy e2e tốn thời gian, tốn tài nguyên, không bền, và kiểm hệ thống như một khối monolith.
- **Cách làm:** consumer-driven contract với Pact Broker; consumer publish pact (gắn commit SHA và tên nhánh), provider chạy lại pact đó; kiểm tương thích với Broker trước khi triển khai.
- **Bài học và cái giá:** vẫn giữ một phần e2e trong giai đoạn chuyển; phát sinh phức tạp ở pipeline (xem mục thất bại bên dưới).
- Nguồn: [Lessons learned using contract testing in GOV.UK Pay](https://technology.blog.gov.uk/2019/01/29/lessons-learnt-using-contract-testing-in-gov-uk-pay/) (2019; Chắc).

#### Spotify (2018): integration test cô lập cho microservice
- Xem [Part 1](part-1-fundamentals.md#spotify-2018-honeycomb-thay-pyramid-cho-microservice): DB thật, API thật, tránh "integrated test" phụ thuộc dịch vụ khác; cái giá là chậm hơn.
- Nguồn: [Testing of Microservices](https://engineering.atspotify.com/2018/01/testing-of-microservices) (2018; Chắc).

#### Slack (2022): test flaky là chi phí thật của CI
- **Vấn đề:** test flaky làm hỏng build; nhóm đo độ ổn định nhánh chính khoảng 20% và triage thủ công khoảng 28 phút mỗi lần.
- **Cách làm:** thử ẩn kết quả test flaky theo ngưỡng lỗi, rồi bỏ vì để lọt lỗi thật và phụ thuộc backend; sau đó phát hiện flaky, tắt test, mở ticket cho nhóm sở hữu và mở PR tự động.
- **Kết quả:** độ ổn định nhánh chính lên khoảng 96%; lưu ý đây là CI di động, không phải integration test ASP.NET.
- Nguồn: [Handling flaky tests at scale](https://slack.engineering/handling-flaky-tests-at-scale-auto-detection-suppression/) (2022; Vừa).

**Mô hình phổ biến:** integration hẹp + contract để bảo vệ double [3]; DB thật cho phần chạm DB [6]; ít e2e.
**Khi thất bại hoặc bị bỏ:**
- GOV.UK Pay gặp phụ thuộc vòng: consumer đổi contract mà provider không đổi API, nên bên nào cũng không triển khai trước được; cách xử lý là xác minh provider với pact gắn nhiều tag môi trường, kèm tăng số lần chạy; pact đang làm dở cần xử lý riêng để không bị publish sớm [11].
- Pact không hợp với API công khai không biết consumer hay khi hai nhóm ít phối hợp [7].

### Trong TeamBoard
- **Quy tắc:** `.agents/rules/testing.md` yêu cầu integration trên DB thật trong container, "không mock DB để cho qua"; contract cho API và event schema, đổi contract phải đổi test.
- **WebApplicationFactory:** `src/board-service.Tests/HealthSteps.cs` dùng `WebApplicationFactory<Program>` trong binding Reqnroll.
- **Testcontainers:** `src/identity-service.Tests/Support/PostgresDatabase.cs` dựng `postgres:17-alpine` qua `Testcontainers.PostgreSql` 4.16.0 (phê duyệt trong plan TB-021 theo comment trong file); nhóm này gắn tag `@docker` trong `document/spec/features/identity-service/database.feature`.
- **Chưa có:** công cụ contract test cho cặp service (chưa có ADR chọn Pact hay hướng khác); Lab 3.3 chỉ là mô phỏng.

## Tự kiểm tra

1. Vì sao test repository trên DB thật vẫn cần dù đã có fake? <details><summary>Đáp án</summary>Fake không có ràng buộc, kiểu dữ liệu, hành vi SQL của DB thật; Lab 3.2: unique constraint bị bỏ qua ở fake.</details>
2. Thay đổi nào của provider contract test bắt được, thay đổi nào cho qua? <details><summary>Đáp án</summary>Đổi tên hoặc bỏ trường consumer dùng thì đỏ; thêm trường mới thì qua vì contract chỉ nêu phần consumer cần.</details>
3. Vì sao filter `Category!=docker` vẫn khởi động container? <details><summary>Đáp án</summary>Filter chọn test, nhưng class fixture của lớp vẫn được dựng khi có test nào của lớp chạy; tách test nhanh ra lớp khác.</details>
4. Khi nào không nên dùng Pact? <details><summary>Đáp án</summary>API công khai không biết consumer, provider ổn định không do consumer định hình, hoặc hai nhóm ít phối hợp.</details>
5. Việc chạy được: thêm trường `Priority` bắt buộc vào contract (`"priority": "string"`) rồi chạy lại Lab 3.3. Tiêu chí: contract đỏ với thông báo thiếu `priority`; sửa provider (đã có trường) để xanh; liên hệ vì sao đây là cách consumer "kéo" provider thay đổi.

## Nguồn và độ tin cậy

**Đã tự mở và đối chiếu (Chắc):**
- [3] [Fowler, Integration Test](https://martinfowler.com/bliki/IntegrationTest.html) (2018): hai ý "kiểm các phần nối với nhau" và "kiểm hệ thống nhiều module", thuật ngữ bị lẫn lộn; phần hẹp/rộng đọc qua agent.
- [7] [Pact, How Pact works](https://docs.pact.io/getting_started/how_pact_works): consumer, provider, pact, interaction, response tối thiểu.
- [11] [GOV.UK Pay](https://technology.blog.gov.uk/2019/01/29/lessons-learnt-using-contract-testing-in-gov-uk-pay/) (2019): lý do bỏ e2e, quy trình Broker.
- [14] [Spotify](https://engineering.atspotify.com/2018/01/testing-of-microservices) (từ Part 1).
- Repo và lab: `HealthSteps.cs`, `PostgresDatabase.cs`, `database.feature`, `COMMANDS.md`, kết quả Lab 3.1 đến 3.4.

**Chỉ qua nguồn thứ cấp hoặc agent (⚠, mức Vừa):**
- [1] [Microsoft Learn, Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [2] [Testcontainers for .NET](https://dotnet.testcontainers.org/), [module PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/), [Resource Reaper](https://dotnet.testcontainers.org/api/resource_reaper/)
- [4] [Fowler, BroadStackTest](https://martinfowler.com/bliki/BroadStackTest.html), [SubcutaneousTest](https://martinfowler.com/bliki/SubcutaneousTest.html)
- [5] [Rainsberger, Integrated Tests Are A Scam](https://blog.thecodewhisperer.com/permalink/integrated-tests-are-a-scam) (2009, cập nhật 2023)
- [6] [Microsoft Learn, Testing EF Core applications](https://learn.microsoft.com/en-us/ef/core/testing/)
- [7] [Pact docs](https://docs.pact.io/), [what Pact is good for](https://docs.pact.io/getting_started/what_is_pact_good_for), [can-i-deploy](https://docs.pact.io/pact_broker/can_i_deploy)
- [8] [Robinson, Consumer-Driven Contracts](https://martinfowler.com/articles/consumerDrivenContracts.html) (2006)
- [9] [Fowler, ContractTest](https://martinfowler.com/bliki/ContractTest.html), [TolerantReader](https://martinfowler.com/bliki/TolerantReader.html)
- [10] [oasdiff](https://github.com/Tufin/oasdiff), [Schemathesis](https://schemathesis.readthedocs.io/en/stable/)
- [12] [Slack, flaky tests](https://slack.engineering/handling-flaky-tests-at-scale-auto-detection-suppression/) (2022)
- [13] [Pact.NET](https://github.com/pact-foundation/pact-net)

**Chưa kiểm hoặc chưa tìm được:**
- Bài Toby Clemson về chiến lược test microservice: chỉ đọc được phần mở đầu; định nghĩa component/contract trong bài chưa kiểm.
- Vòng đời container theo test hay theo fixture trong Testcontainers, và giấy phép gói: chưa kiểm.
- Pact.NET trên .NET 10: chưa kiểm; chưa chạy Pact trong Part này. Dredd, Kiota, Spring Cloud Contract: chưa mở.
- Cơ chế content root của `WebApplicationFactory` (liên hệ với file solution): chỉ suy ra từ hai lỗi quan sát, chưa đối chiếu tài liệu.
- Test idempotent consumer: kiến thức chung, chưa nguồn, chưa lab.
- Các case contract testing khác (lastminute.com, Teachable, Microsoft ISE...): chỉ thấy trong kết quả tìm kiếm, chưa mở.
