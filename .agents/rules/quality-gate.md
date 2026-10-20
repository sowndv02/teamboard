# Quality gate local (không cần server)

Lý do tồn tại: có một cổng chất lượng chạy hoàn toàn trên máy, không phụ thuộc SonarQube/SonarCloud hay dịch vụ ngoài, để agent và người dùng cùng một chuẩn. `code-change-gate.md` quy định **khi nào và theo thứ tự nào** phải qua gate; file này quy định **công cụ, lệnh, ngưỡng, phạm vi test** và cách xử lý rule xung đột.

Công cụ đánh dấu 🚧 là cấu hình đề xuất, chưa có trong repo; khi thêm thì bỏ 🚧 và cập nhật `COMMANDS.md`.

## Các lớp kiểm tra

| Lớp | Công cụ | Ai dùng |
|---|---|---|
| IDE thời gian thực | SonarQube for IDE (SonarLint) + `.editorconfig` | Người phát triển |
| .NET phân tích tĩnh | Roslyn analyzers (`AnalysisMode`) + `SonarAnalyzer.CSharp` 🚧, `dotnet build -warnaserror` | Agent, CI local, pre-push |
| .NET định dạng | `dotnet format --verify-no-changes` | Agent, pre-commit |
| Angular/TS phân tích tĩnh | ESLint (`angular-eslint`, `typescript-eslint` strict, `eslint-plugin-sonarjs`) 🚧 | Agent, CI local, pre-push |
| Angular/TS định dạng | Prettier `--check` 🚧 | Agent, pre-commit |
| Kiểu | `tsc --noEmit` (qua `ng build`) và build .NET | Agent, CI |
| Secret | `gitleaks` (staged + toàn repo) 🚧 | Agent, pre-commit, trước PR |
| Phụ thuộc có lỗ hổng | `dotnet list package --vulnerable --include-transitive`, `npm audit --omit=dev` | Trước PR, khi đổi dependency |
| Infra | `terraform fmt -check`, `validate`, `tflint` 🚧, `trivy config` 🚧 | Khi đổi `infra/**` |
| Compose/Docker | `docker compose config -q`, `hadolint` 🚧 | Khi đổi Dockerfile/compose |
| Gate đầy đủ | Tất cả lớp liên quan + full test | Trước merge |

**Parity:** SonarLint trong IDE và analyzer chạy CLI không giống nhau 100%. Chuẩn cho agent là **kết quả CLI** (lệnh dưới đây); IDE chỉ để phát hiện sớm.

## Lệnh gate

```bash
# .NET
dotnet build src/teamboard.slnx -warnaserror
dotnet format src/teamboard.slnx --verify-no-changes

# Angular (src/web)  🚧
npm --prefix src/web run lint -- --max-warnings=0
npm --prefix src/web run format:check
npm --prefix src/web run build

# Secret + phụ thuộc
gitleaks protect --staged --redact                # 🚧 trước commit
gitleaks detect --redact                          # 🚧 quét toàn repo trước PR
dotnet list src/teamboard.slnx package --vulnerable --include-transitive
npm --prefix src/web audit --omit=dev             # 🚧

# Infra  🚧
terraform -chdir=infra/envs/dev fmt -check && terraform -chdir=infra/envs/dev validate
tflint --chdir infra/envs/dev
trivy config infra/

# Test: xem mục "Phạm vi test"
```

## Quy ước severity (quan trọng)

Vì gate chạy với `-warnaserror` và `--max-warnings=0`, mọi rule trong cấu hình phải là **`error`** hoặc **`none`/`off`**. Không dùng `warning`/`warn`: warning cũng làm fail gate và thành BLOCKER ngay.

- .NET: đặt trong `.editorconfig` (`dotnet_diagnostic.<ID>.severity = error | none`), `Directory.Build.props` bật `TreatWarningsAsErrors`. Rule nào chưa muốn thi hành thì `none` kèm lý do trong bảng bên dưới, không để `warning`.
- Angular/TS: trong `eslint.config.*` chỉ `"error"` hoặc `"off"`.

| Mức | Ý nghĩa |
|---|---|
| BLOCKER | Build, lint, format, secret scan, hoặc test liên quan fail; lỗ hổng mức cao trong dependency |
| MAJOR | Smell Sonar chưa có rule thi hành; vi phạm hardcode (URL, cổng, connection string, tên môi trường) |
| MINOR | Cải thiện không ảnh hưởng gate |

## Ngưỡng chung

- Độ phức tạp nhận thức (cognitive complexity) mỗi hàm ≤ **15** (.NET: `S3776` mặc định; TS: `sonarjs/cognitive-complexity` = 15). Vượt thì tách hàm, không nới ngưỡng.
- Không `#pragma warning disable`, `[SuppressMessage]`, `eslint-disable`, `@ts-ignore`, `@ts-nocheck`, `any` mới. Nếu thật sự cần: ghi issue + comment lý do, phạm vi hẹp nhất có thể.
- Phụ thuộc có lỗ hổng mức High/Critical: BLOCKER, nâng bản vá hoặc ghi lý do chấp nhận trong issue.
- Coverage: không đặt ngưỡng cứng toàn dự án (dự án học tập); logic quan trọng (idempotency, outbox, auth, domain) phải có test, và PR không được **giảm** coverage vùng đã có.

## Phạm vi test (targeted vs full)

### Mặc định khi đang code (giữa lát, sửa finding, ad-hoc)

| Việc | Lệnh |
|---|---|
| Phân tích, format, kiểu, secret | Chạy đầy đủ các lệnh gate của vùng đã đổi |
| Test | **Chỉ test liên quan**, không chạy toàn solution/toàn workspace |

Chọn test liên quan (gộp, bỏ trùng):
1. File test sửa/tạo trong lượt này.
2. Test cùng cặp với file đổi (`Foo.cs` → `FooTests.cs`; `foo.ts` → `foo.spec.ts`).
3. **Blast radius dùng chung:** đổi trong `src/shared/` hoặc package/lib dùng chung → chạy test của mọi project tham chiếu nó.
4. **Module/slice:** đổi trong một service → chạy toàn bộ test của project service đó (unit + integration cục bộ).
5. Đụng route/schema/event contract → thêm contract test của provider **và** consumer.

```bash
dotnet test src/<Service>.Tests --filter "FullyQualifiedName~<Namespace.Class>"
npm --prefix src/web test -- --watch=false --include='<đường-dẫn-spec>'
```

**Cấm khi đang code:** chạy `dotnet test` toàn solution hay toàn bộ test web, trừ khi có trigger "full" bên dưới.

### Full test (mốc)

| Mốc | Phạm vi |
|---|---|
| Giữa lát, sửa finding nhỏ | Targeted |
| Hoàn thành issue (trước báo xong) | Full test của **mỗi project đã đụng** trong branch |
| Mở PR / tick checklist PR | Full test mỗi project đã đổi + gate đầy đủ |
| Trước merge | Toàn bộ gate + toàn bộ test |

**Trigger bắt buộc chạy full ngay** (kể cả khi đang code): người dùng yêu cầu rõ; đổi cấu hình dùng chung (`.editorconfig`, `Directory.Build.props`, `eslint.config.*`, `tsconfig*.json`, test harness chung); diff chạm từ 2 vùng trở lên; refactor xuyên service (ghi lý do vào báo cáo).

## Xung đột giữa các analyzer

Roslyn, SonarAnalyzer, `typescript-eslint` và `sonarjs` có thể trùng chức năng. Khi xung đột:
1. Ưu tiên rule thuộc chuẩn dự án (kiến trúc, no-hardcode, import boundary).
2. Tắt rule trùng ở **một** công cụ, ghi lý do vào bảng dưới.
3. Không tắt cả hai.

### Rule đã tắt/điều chỉnh (cập nhật khi thay đổi cấu hình)

Chưa có cấu hình trong repo nên bảng trống. Mỗi dòng gồm: `Rule | Phạm vi | Quyết định | Lý do`. Quy ước đề xuất khi thêm:

| Rule | Phạm vi | Quyết định | Lý do |
|---|---|---|---|
| (ví dụ) `sonarjs/no-hardcoded-passwords` | toàn bộ | off | Tên field `password`/key env; secret đã có quy tắc no-hardcode và `gitleaks` |
| (ví dụ) `CA1707` (dấu `_` trong tên) | `*Tests*` | none | Tên test dạng `Method_Condition_Expected` |
| (ví dụ) `sonarjs/no-duplicate-string` | `**/*.spec.ts` | off | Fixture lặp trong test |

Phạm vi test (`*Tests*`, `*.spec.ts`) được nới các rule về trùng lặp/độ dài tên; code production **không** được nới.

## Vi phạm analyzer

- Sửa tại source. Không thêm suppress/disable trừ khi có issue và comment lý do (xem Ngưỡng chung).
- Không thêm script/lệnh riêng cho Sonar: analyzer chạy trong `build`/`lint`, là tập con của gate.

## Cấu hình cần tạo (🚧 khi bắt đầu giai đoạn 0)

- `.editorconfig` (style + `dotnet_diagnostic` severity), `Directory.Build.props` (`TreatWarningsAsErrors`, `AnalysisMode`, `SonarAnalyzer.CSharp` pin theo `dependency-pinning.md`).
- `src/web/eslint.config.*`, `.prettierrc`, scripts `lint`/`format:check` trong `package.json`.
- `.gitleaks.toml` (allowlist cho file `*.example`).
- Git hook (pre-commit: format + gitleaks staged; pre-push: build + lint + test liên quan). Hook không thay thế gate, chỉ phát hiện sớm. Không `--no-verify`.

## Tham chiếu

- `code-change-gate.md`: khi nào phải chạy gate, hard stop, báo cáo kết quả.
- `testing.md`: loại test và cách viết.
- `dependency-pinning.md`: pin phiên bản công cụ và analyzer.
- `code-review.md`: gate là điều kiện trước khi review.

## Giấy phép phụ thuộc

Gate cần có quét giấy phép và lỗ hổng cho phụ thuộc (đề xuất, xem `dependency-policy.md` mục 8). Gói thuộc hạng T3 hoặc giấy phép ngoài danh sách cho phép là BLOCKER.
