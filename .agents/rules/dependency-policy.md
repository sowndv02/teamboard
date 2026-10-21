# Chính sách phụ thuộc bên thứ ba (hạn chế thư viện, tránh bẫy giấy phép)

Lý do tồn tại: nhiều thư viện .NET phổ biến đã chuyển sang giấy phép thương mại hoặc ngừng phát triển, và một số công cụ hạ tầng đổi giấy phép. Phụ thuộc nhiều và không kiểm soát = rủi ro chi phí, pháp lý và chuỗi cung ứng. Quy tắc: **dùng nền tảng có sẵn trước, ít phụ thuộc nhất có thể, mọi phụ thuộc bên thứ ba phải qua kiểm tra và nằm sau ranh giới có thể thay**. Ghi quyết định ở ADR 0007; danh bạ phụ thuộc đã xét: `config/dependencies.yaml`.

Bổ sung cho: `dependency-pinning.md` (ghim phiên bản), `quality-gate.md` (quét), `backend-engineering.md`, `architecture.md`, `code-review.md` (probe thêm phụ thuộc), `terraform.md`.

## 1. Thuật ngữ

- **Thư viện / gói / phụ thuộc bên thứ ba (third-party library, package, dependency):** code do người/tổ chức khác viết, đưa vào qua NuGet/provider/image. Rủi ro đi kèm: *license risk* (đổi giấy phép), *supply-chain risk* (mã độc, bị bỏ rơi), *vendor lock-in*.
- **Nền tảng / first-party:** thành phần đi cùng nền tảng và do nhà cung cấp nền tảng duy trì: **.NET BCL (`System.*`), ASP.NET Core, Entity Framework Core, `Microsoft.Extensions.*`** (Microsoft), **AWS SDK** (AWS). EF Core thuộc nhóm này: được xem là an toàn để dùng (không phải "thư viện bên thứ ba").
- **Giấy phép permissive:** MIT, Apache-2.0, BSD, ISC: dùng, sửa, thương mại hoá thoải mái. **Copyleft mạnh** (GPL/AGPL), **source-available** (BUSL/BSL, SSPL, RSAL), và **thương mại** thì có hạn chế hoặc phải trả phí.

## 2. Bằng chứng: chuyện đã xảy ra

Tra cứu ngày 2026-10-21; nhiều nguồn là thứ cấp, **kiểm tra trang chính thức trước khi dựa vào chi tiết** (mức giá, ngưỡng doanh thu).

| Dự án | Điều đã xảy ra | Nguồn |
|---|---|---|
| **MediatR, AutoMapper** | Chuyển sang mô hình giấy phép kép/thương mại dưới Lucky Penny Software (2025); các bản cũ giữ giấy phép lúc phát hành (nguồn thứ cấp: MediatR 12.5.0 là bản Apache-2.0 cuối cùng) | [Bài của tác giả](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/), [phân tích](https://startdebugging.net/2026/05/mediatr-vs-plain-service-classes-in-2026/) |
| **MassTransit** | Từ v9 là sản phẩm thương mại (công bố 2025); bản trước vẫn permissive; mốc hết hỗ trợ v8 chưa rõ ràng | [Trang hỗ trợ chính thức](https://masstransit.io/support), [thảo luận](https://github.com/elsa-workflows/elsa-core/discussions/6583) |
| **FluentAssertions** | Từ v8 dùng giấy phép Xceed (thương mại cho dùng thương mại); ≤ v7 vẫn Apache-2.0; có nhánh `AwesomeAssertions` giữ Apache-2.0 | [DevClass](https://www.devclass.com/development/2025/01/16/another-open-source-project-shifts-to-restrictive-license-fluent-assertions-following-xceed-partnership/1621343), [NuGet](https://packages.nuget.org/packages/FluentAssertions/8.3.0/License) |
| **SpecFlow** | Hết vòng đời cuối 2024; cộng đồng chuyển sang **Reqnroll** (BSD-3) | [Reqnroll](https://reqnroll.net/news/2025/01/specflow-end-of-life-has-been-announced/), [FAQ](https://docs.reqnroll.net/latest/help/faq.html) |
| **LocalStack** | Gộp thành một image cần **auth token** từ phiên bản 2026.03; có gói miễn phí cho phi thương mại; tình trạng bản mã nguồn mở còn mâu thuẫn giữa các nguồn | [Blog chính thức](https://blog.localstack.cloud/localstack-single-image-next-steps/), [phát hành 2026.03](https://blog.localstack.cloud/localstack-for-aws-release-2026-03-0/) |
| **Redis** | Đổi sang giấy phép source-available (RSALv2/SSPL), sau đó Redis 8 thêm AGPLv3; Linux Foundation tạo nhánh **Valkey** (BSD-3) | [SD Times](https://sdtimes.com/os/open-source-wins-again-redis-adds-gnu-agpl-license-to-its-offering/), [The New Stack](https://thenewstack.io/linux-foundation-forks-the-open-source-redis-as-valkey/) |
| **Terraform** | HashiCorp chuyển sang BSL (không phải mã nguồn mở); Linux Foundation tạo nhánh **OpenTofu** | [Phân tích](https://www.softwareseni.com/migration-and-risk-assessment-playbook-for-ctos-evaluating-redis-to-valkey-and-terraform-to-opentofu-switches/) |
| **Docker Desktop** | Cần gói trả phí khi tổ chức vượt ngưỡng nhân sự/doanh thu; **Docker Engine/CLI/Compose vẫn Apache-2.0** | [Docker FAQ](https://docker.io/pricing/faq/) |
| **Moq** | Từng gây tranh cãi vì tính năng SponsorLink (2023, từ trí nhớ, chưa kiểm chứng ở đây) | Cần kiểm tra |

Bài học chung: **dự án do một công ty/một người duy trì, kể cả đang miễn phí, có thể đổi điều khoản bất cứ lúc nào**. Nền tảng do nhà cung cấp lớn hoặc quỹ trung lập duy trì ít rủi ro hơn.

## 3. Phân hạng phụ thuộc

| Hạng | Định nghĩa | Ví dụ (tham khảo, kiểm tra hiện hành) | Quy tắc |
|---|---|---|---|
| **T0 Nền tảng** | First-party đi cùng nền tảng/nhà cung cấp | `System.*`, ASP.NET Core, **EF Core**, `Microsoft.Extensions.*` (Logging, Options, Http.Resilience, DependencyInjection), AWS SDK for .NET | Dùng tự do; vẫn ghim phiên bản |
| **T1 Quỹ/tổ chức trung lập, permissive, nhiều người đóng góp** | Quản trị mở, khó đổi điều khoản đột ngột | OpenTelemetry (CNCF), Polly (.NET Foundation), xUnit, Npgsql, YARP (Microsoft) | Cho phép sau kiểm tra nhanh; ghi vào `config/dependencies.yaml` |
| **T2 Công ty đơn lẻ hoặc ít người duy trì, hiện permissive** | Có thể đổi điều khoản/bỏ rơi | Serilog, FluentValidation, Mapster, Carter, Marten, Refit, Swashbuckle, Reqnroll, NSubstitute, Testcontainers | **Cần phê duyệt**; đặt **sau interface của mình**, một project; có kế hoạch thay thế; ghi "lối thoát" |
| **T3 Đã/đang thương mại, source-available, hết vòng đời** | Điều khoản không phù hợp hoặc rủi ro cao | MediatR ≥ 13, AutoMapper (bản thương mại), MassTransit ≥ 9, FluentAssertions ≥ 8, SpecFlow, Duende/IdentityServer, Hangfire Pro, Redis (RSAL/SSPL/AGPL) | **Cấm**, trừ khi người dùng duyệt tường minh và ghi ADR (kèm chi phí giấy phép) |

Ghi chú: nhóm công cụ (không phải thư viện) cũng có rủi ro giấy phép: Terraform (BSL), Docker Desktop (trả phí khi vượt ngưỡng), LocalStack (cần token), Grafana/Loki/Tempo (AGPL; dùng nội bộ thường ổn, nhưng đọc điều khoản), Seq (có giới hạn gói miễn phí). Xem `config/dependencies.yaml` mục `tools`.

## 4. Quy trình thêm một phụ thuộc

Trước khi thêm (NuGet, provider Terraform, Docker image, công cụ CLI):

1. **Có thật sự cần?** Nền tảng (T0) làm được không? Viết phần nhỏ tự làm được không (mục 5)? Không có lý do rõ → không thêm.
2. **Giấy phép:** lấy từ nguồn chính thức (trang package/repo, `LICENSE`). Cho phép: MIT, Apache-2.0, BSD-2/3, ISC, Unlicense. **Cần phê duyệt:** MPL-2.0, LGPL, EPL, giấy phép tuỳ chỉnh. **Cấm mặc định:** GPL, AGPL, SSPL, BUSL/BSL, RSAL, "source-available", thương mại, không có giấy phép. Kiểm cả **phụ thuộc bắc cầu** (transitive).
3. **Quản trị và sức khoẻ:** ai duy trì (quỹ, công ty, cá nhân)? bao nhiêu người đóng góp? phát hành gần đây? có cơ chế tài trợ bền vững? lịch sử đổi giấy phép? có nhánh cộng đồng dự phòng?
4. **Bảo mật:** lỗ hổng đã biết (`dotnet list package --vulnerable`), cách báo cáo lỗ hổng, kích thước/độ phức tạp.
5. **Lối thoát (exit plan):** thay bằng gì nếu đổi giấy phép/bỏ rơi? Chi phí gỡ bao nhiêu? Đã đặt sau interface chưa (mục 6)?
6. **Ghi nhận:** thêm vào `config/dependencies.yaml` (hạng, giấy phép, nguồn kiểm, ngày kiểm, lý do, lối thoát); lựa chọn đáng kể thì viết ADR.
7. **Ghim phiên bản** (`dependency-pinning.md`); với phụ thuộc T2 có nguy cơ đổi giấy phép, **ghim ở bản cuối cùng còn permissive** khi biết.

Phụ thuộc T3 hoặc giấy phép "cần phê duyệt" → **hỏi người dùng** (CONFIRM), nêu đánh đổi và chi phí; không tự thêm.

## 5. Tự viết thay vì thêm thư viện (khi nhỏ và an toàn)

Nguyên tắc: tự viết khi phần đó **nhỏ, hiểu rõ, ít rủi ro**, và việc tự viết cũng là cơ hội học. **Không tự viết:** thuật toán mật mã, ORM, trình phân tích giao thức phức tạp, bất cứ thứ gì sai là lỗ hổng bảo mật (dùng nền tảng).

| Thường dùng | Nguy cơ | Thay thế gợi ý |
|---|---|---|
| **MediatR** (mediator/CQRS) | Thương mại từ v13 | Handler là class thường qua DI (`ICommandHandler<T>`), một dispatcher nhỏ tự viết (vài chục dòng), hoặc gọi thẳng handler từ endpoint; cắt ngang (validation/logging) bằng decorator DI hoặc middleware Minimal API |
| **AutoMapper / Mapster** | Thương mại (AutoMapper) | Ánh xạ thủ công (extension method `ToDto()`), hoặc công cụ sinh code lúc biên dịch (ví dụ Mapperly, kiểm tra giấy phép) |
| **MassTransit** | Thương mại từ v9 | Lớp nhắn tin mỏng của mình (`IMessagePublisher`, `IMessageConsumer`) trên **AWS SDK** (SQS/SNS); outbox/inbox tự viết (vừa học vừa hiểu) |
| **FluentAssertions** | Thương mại từ v8 | Assert của xUnit, hoặc `AwesomeAssertions`/`Shouldly` (kiểm tra giấy phép hiện hành), hoặc helper assert tự viết |
| **Moq** | Tranh cãi/rủi ro niềm tin | Fake viết tay (đơn giản, rõ ràng), hoặc `NSubstitute` (kiểm tra giấy phép) |
| **SpecFlow** | Hết vòng đời | **Reqnroll** (BSD-3), hoặc test BDD viết tay theo Given/When/Then bằng xUnit |
| **FluentValidation** | Hiện Apache-2.0 (nguồn thứ cấp) | Validation tích hợp ASP.NET Core (`DataAnnotations`, `IValidatableObject`) hoặc validator tự viết nhỏ |
| **Carter** | Ít người duy trì | **Minimal API** có sẵn (`MapGroup`) |
| **Swashbuckle** | Ít người duy trì | `Microsoft.AspNetCore.OpenApi` (đã dùng trong repo) + giao diện đọc tài liệu nhẹ (kiểm tra lựa chọn hiện hành) |
| **Redis (ElastiCache)** | Giấy phép Redis đổi | **Valkey** (BSD-3; ElastiCache hỗ trợ, kiểm tra hiện hành) hoặc không cache khi chưa cần |
| **IdentityServer/Duende** | Thương mại | Tự xây (ADR 0006) |
| **LocalStack** | Cần token từ 2026.03 | Xem mục 7 |
| **Serilog** | Hiện Apache-2.0, nhưng trùng với OpenTelemetry | `ILogger` + OpenTelemetry (đề xuất ở `observability.md`) |

Khi tự viết: có test, giữ nhỏ, đặt trong project hạ tầng/shared hợp lý, **không** thành "framework nội bộ" phình to.

## 6. Cô lập phụ thuộc (để thay được)

- **Interface của mình ở biên:** domain/application phụ thuộc `IClock`, `IMessagePublisher`, `IEmailSender`, `IObjectStore`...; thư viện bên thứ ba chỉ xuất hiện ở project **Infrastructure/adapter** (hợp với Clean/Hexagonal, `architecture.md`).
- **Không để kiểu của thư viện rò vào hợp đồng công khai** (DTO API, sự kiện, miền). Hợp đồng dùng kiểu của mình.
- **Một chỗ dùng:** mỗi phụ thuộc T2 chỉ được tham chiếu từ một project; thay thế = sửa một project.
- **Không viết attribute/convention của thư viện vào entity miền** (tránh dính chặt).
- Test dùng fake của mình qua interface, không phụ thuộc thư viện mock cho logic cốt lõi.

## 7. Công cụ và môi trường cục bộ

- **Giả lập AWS cục bộ:** LocalStack giờ cần auth token (2026.03; có gói miễn phí phi thương mại, điều khoản cần đọc); các lựa chọn khác (ElasticMQ cho SQS, DynamoDB Local của AWS, Moto, các bản giả lập cộng đồng như Floci) chưa được kiểm chứng ở đây. **Chưa chốt** (ADR 0007): tránh phụ thuộc cứng vào một công cụ bằng cách đặt AWS sau interface, fake trong bộ nhớ cho unit test, và chỉ ít test tích hợp dùng giả lập hoặc **tài khoản AWS sandbox thật** (chi phí thấp với SQS/SNS/DynamoDB, xem `aws-service-selection`).
- **Docker:** dùng Docker Engine/Compose (Apache-2.0); **Docker Desktop có thể yêu cầu gói trả phí** nếu tổ chức vượt ngưỡng (kiểm tra điều khoản hiện hành, đặc biệt khi dùng cho công việc ở công ty); phương án thay: Rancher Desktop, Podman, hoặc Docker Engine trong WSL2.
- **Terraform:** BSL cho phép dùng cho hạ tầng của chính mình, nhưng không phải mã nguồn mở; cân nhắc **OpenTofu** (nhánh cộng đồng) nếu muốn tránh rủi ro; cấu hình tương thích phần lớn (kiểm tra). Ghi quyết định vào ADR 0004 qua ADR mới khi đổi.
- **Quan sát:** Jaeger, Prometheus, OpenTelemetry Collector thuộc quỹ trung lập (permissive); Grafana/Loki/Tempo dùng AGPL (dùng nội bộ thường ổn); Seq có giới hạn gói miễn phí. Chọn nhẹ và rẻ (`observability.md`).
- **Cơ sở dữ liệu/dịch vụ chạy trong container:** kiểm giấy phép image (ví dụ dùng Valkey thay Redis; SQL Server Developer edition chỉ cho phát triển/test, không dùng production).

## 8. Kiểm tra tự động (🚧 chưa cài, đề xuất)

Trong `quality-gate.md`:

```bash
dotnet list src/teamboard.slnx package --include-transitive            # danh sách đầy đủ
dotnet list src/teamboard.slnx package --vulnerable --include-transitive
dotnet list src/teamboard.slnx package --deprecated
# Quét giấy phép (chọn một công cụ, kiểm tra hiện hành): ví dụ nuget-license, dotnet-project-licenses
# SBOM: CycloneDX (dotnet) để có danh sách thành phần
```

- Lệnh quét giấy phép phải **fail** khi gặp giấy phép ngoài danh sách cho phép (mục 4) hoặc gói thuộc hạng T3.
- Bật cập nhật tự động (ví dụ Dependabot) **chỉ để thông báo**; nâng phiên bản là thay đổi riêng và phải kiểm giấy phép của phiên bản mới (một bản nâng có thể đổi giấy phép).
- Cập nhật `config/dependencies.yaml` mỗi khi thêm/đổi phụ thuộc.

## 9. Khi một phụ thuộc đổi giấy phép hoặc bị bỏ rơi

1. **Đóng băng:** ghim ở phiên bản cuối cùng còn permissive; không tự nâng.
2. **Đánh giá:** mức dùng, chi phí giấy phép, rủi ro bảo mật khi dừng cập nhật, có nhánh cộng đồng không.
3. **Quyết định (CONFIRM):** thay thế (nhánh cộng đồng/thư viện khác), tự viết phần dùng, hay chấp nhận giấy phép.
4. **Thực hiện** như một issue riêng, qua workflow; cập nhật `config/dependencies.yaml`, ADR.

## 10. Checklist khi review thay đổi có phụ thuộc mới (cho `code-review.md`)

- [ ] Đã giải thích vì sao T0/tự viết không đủ
- [ ] Giấy phép (cả bắc cầu) nằm trong danh sách cho phép; hạng và nguồn kiểm đã ghi trong `config/dependencies.yaml`
- [ ] Nằm sau interface của mình, một project; không rò kiểu vào hợp đồng công khai
- [ ] Phiên bản ghim; có lối thoát; kết quả quét lỗ hổng và giấy phép sạch
- [ ] Không phải T3; hoặc đã được người dùng duyệt tường minh kèm chi phí
