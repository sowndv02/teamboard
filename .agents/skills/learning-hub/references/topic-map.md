# Bản đồ chủ đề → nơi tìm

Điểm xuất phát cho `pack`. Luôn mở file thật; bảng này có thể lỗi thời. Thêm dòng khi gặp chủ đề mới.

| Chủ đề | Nội bộ (đọc theo thứ tự) | Code/config | Chính thức |
|---|---|---|---|
| Docker (Dockerfile, image, layer) | `.agents/rules/docker.md`, `COMMANDS.md` mục 3 | `src/teamboard-api/Dockerfile` | docs.docker.com |
| Docker Compose | `.agents/rules/docker.md`, `COMMANDS.md` mục 3 | `src/docker-compose*.yml`, `src/*.example` | docs.docker.com/compose |
| API Gateway, YARP, routing, proxy | `document/spec/api-gateway.md`, `document/design/api-gateway.md`, `document/plans/api-gateway-slices.md`, `document/issues/TB-007-*.md`, `TB-010-*.md`, `document/architecture/adr/0001-*.md`, `0002-*.md` | `src/` (gateway khi có) | learn.microsoft.com (YARP) |
| Kiến trúc microservice, pattern (Clean, Vertical Slice, CQRS, Hexagonal, Outbox) | `document/architecture/README.md`, `patterns-catalog.md`, `.agents/rules/architecture.md`, `adr/0001-*.md` | `src/` | microservices.io |
| Giao tiếp, event, SNS/SQS, DLQ, idempotent | `.agents/rules/messaging.md`, `adr/0002-*.md`, `patterns-catalog.md` | `src/shared/` | docs.aws.amazon.com (SNS, SQS) |
| Observability (log, trace, metric, health) | `document/architecture/observability.md`, `.agents/rules/observability.md` | ServiceDefaults (TB-001), `Program.cs` | opentelemetry.io |
| Resilience (retry, circuit breaker, timeout) | `document/architecture/resilience.md` | – | learn.microsoft.com (Polly/resilience) |
| Identity, JWT, OAuth2, OIDC, PKCE | `document/architecture/identity-learning.md`, `adr/0006-*.md`, `references.md` mục 2 | – | RFC 6749/7519/7636, OpenID Connect Core |
| ASP.NET Core / C# / EF Core | `.agents/rules/dotnet.md`, `backend-engineering.md`, `api-endpoints.md`, `adr/0005-*.md` | `src/teamboard-api/` | learn.microsoft.com |
| Testing (TDD/BDD) | `.agents/rules/testing.md`, `test-review.md` | `src/**/*Tests*` khi có | – |
| Terraform, AWS, ECS Fargate | `.agents/rules/terraform.md`, `adr/0003-*.md`, `0004-*.md`, `document/security/infra-attack-scenarios.md`, skill `aws-service-selection` | `infra/` (giai đoạn 4) | developer.hashicorp.com, docs.aws.amazon.com |
| Angular / frontend | skill `angular-learning`, `.agents/rules/angular.md`, `adr/0008-*.md`, `0009-*.md` | `src/web/` (chưa scaffold) | angular.dev |
| Quy trình dự án (issue, branch, commit, PR, gate) | `AGENTS.md`, `.agents/rules/task-workflow.md`, `git-workflow.md`, `git-commit.md`, `code-change-gate.md`, skill `workflow` | `config/project.yaml` | – |
| Quản lý dự án | skill `project-manager`, `document/pm/` | – | – |
| Bài học kinh nghiệm | `document/lessons/README.md` | – | – |
| Phụ thuộc/thư viện | `.agents/rules/dependency-policy.md`, `adr/0007-*.md`, `config/dependencies.yaml` | `*.csproj` | – |
