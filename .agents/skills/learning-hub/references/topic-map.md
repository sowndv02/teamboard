# Bản đồ chủ đề → khái niệm gốc → nơi tìm

Điểm xuất phát cho `pack`. Luôn mở file thật; bảng này có thể lỗi thời. Thêm dòng khi gặp chủ đề mới.

**Cột "Khái niệm gốc":** chủ đề là công cụ thì ghi khái niệm nó triển khai (viết ghi chú khái niệm trước, công cụ trỏ về). Chủ đề tự nó là khái niệm thì ghi `(là khái niệm)`.

**Công cụ không đại diện khái niệm:** hỏi một công cụ thì luôn ra cả ghi chú khái niệm có "Bản đồ giải pháp" (các họ giải pháp và lựa chọn khác công cụ đó). Ví dụ lựa chọn khác để bản đồ không thiên vị: container → Docker, Podman, containerd/nerdctl; API gateway → YARP, Ocelot, Envoy, NGINX, Kong, Traefik, AWS API Gateway, Azure API Management; caching → cache trong tiến trình, Redis/Valkey, Memcached, CDN/HTTP cache, cache của DB.

| Chủ đề | Khái niệm gốc | Nội bộ (đọc theo thứ tự) | Code/config | Chính thức |
|---|---|---|---|---|
| Container (namespace/cgroup, image, OCI, runtime, điều phối) | (là khái niệm) → ghi chú `document/learning/container.md` | `.agents/rules/docker.md`, ADR 0003 | – | opencontainers.org, kubernetes.io |
| Docker (Dockerfile, image, layer) | Container | `.agents/rules/docker.md`, `COMMANDS.md` mục 3 | `src/teamboard-api/Dockerfile` | docs.docker.com |
| Docker Compose | Container | `.agents/rules/docker.md`, `COMMANDS.md` mục 3 | `src/docker-compose*.yml`, `src/*.example` | docs.docker.com/compose |
| API Gateway (cửa vào, routing, aggregation, offloading) | (là khái niệm) → `document/learning/api-gateway.md` | `document/spec/api-gateway.md`, `document/design/api-gateway.md`, `document/architecture/adr/0001-*.md`, `0002-*.md` | – | microservices.io, learn.microsoft.com (Azure Architecture Center) |
| YARP, routing, proxy | API Gateway | `document/spec/api-gateway.md`, `document/design/api-gateway.md`, `document/plans/api-gateway-slices.md`, `document/issues/TB-007-*.md`, `TB-010-*.md` | `src/` (gateway khi có) | learn.microsoft.com (YARP) |
| Kiến trúc microservice, pattern (Clean, Vertical Slice, CQRS, Hexagonal, Outbox) | (là khái niệm) | `document/architecture/README.md`, `patterns-catalog.md`, `.agents/rules/architecture.md`, `adr/0001-*.md` | `src/` | microservices.io, martinfowler.com |
| Caching (cache-aside, TTL, invalidation, stampede; HTTP/CDN, trong tiến trình, phân tán, tầng dữ liệu) | (là khái niệm) → `document/learning/caching.md` | `document/architecture/patterns-catalog.md` mục 2.5, `.agents/skills/system-design/references/decision-framework.md` mục 2.7 | – | docs.aws.amazon.com (ElastiCache caching strategies) |
| Redis / Valkey, ElastiCache | Caching | `.agents/rules/dependency-policy.md`, `patterns-catalog.md` mục 2.5, `.agents/skills/aws-service-selection/references/aws-service-catalog.md`, `cost-model.md` | `config/dependencies.yaml` (chưa có code) | valkey.io, docs.aws.amazon.com (ElastiCache) |
| Giao tiếp, event, SNS/SQS, DLQ, idempotent | (là khái niệm: messaging) | `.agents/rules/messaging.md`, `adr/0002-*.md`, `patterns-catalog.md` | `src/shared/` | docs.aws.amazon.com (SNS, SQS) |
| Observability (log, trace, metric, health) | (là khái niệm) | `document/architecture/observability.md`, `.agents/rules/observability.md` | ServiceDefaults (TB-001), `Program.cs` | opentelemetry.io |
| Resilience (retry, circuit breaker, timeout) | (là khái niệm) | `document/architecture/resilience.md` | – | learn.microsoft.com (Polly/resilience) |
| Identity, JWT, OAuth2, OIDC, PKCE | (là khái niệm) | `document/architecture/identity-learning.md`, `adr/0006-*.md`, `references.md` mục 2 | – | RFC 6749/7519/7636, OpenID Connect Core |
| ASP.NET Core / C# / EF Core | Web framework, ORM | `.agents/rules/dotnet.md`, `backend-engineering.md`, `api-endpoints.md`, `adr/0005-*.md` | `src/teamboard-api/` | learn.microsoft.com |
| Testing (TDD/BDD) | (là khái niệm) | `.agents/rules/testing.md`, `test-review.md` | `src/**/*Tests*` khi có | – |
| Terraform, AWS, ECS Fargate | Infrastructure as Code, container orchestration | `.agents/rules/terraform.md`, `adr/0003-*.md`, `0004-*.md`, `document/security/infra-attack-scenarios.md`, skill `aws-service-selection` | `infra/` (giai đoạn 4) | developer.hashicorp.com, docs.aws.amazon.com |
| Angular / frontend | Single-page application | skill `angular-learning`, `.agents/rules/angular.md`, `adr/0008-*.md`, `0009-*.md` | `src/web/` (chưa scaffold) | angular.dev |
| Quy trình dự án (issue, branch, commit, PR, gate) | – | `AGENTS.md`, `.agents/rules/task-workflow.md`, `git-workflow.md`, `git-commit.md`, `code-change-gate.md`, skill `workflow` | `config/project.yaml` | – |
| Quản lý dự án | – | skill `project-manager`, `document/pm/` | – | – |
| Bài học kinh nghiệm | – | `document/lessons/README.md` | – | – |
| Phụ thuộc/thư viện | – | `.agents/rules/dependency-policy.md`, `adr/0007-*.md`, `config/dependencies.yaml` | `*.csproj` | – |
