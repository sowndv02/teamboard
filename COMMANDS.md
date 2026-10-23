# Lệnh cho agent

Chạy từ gốc repo. Thực hiện mục 1 đầu mỗi phiên; mục 2 trước khi báo "xong"; các mục sau khi cần.
Lệnh đánh dấu 🚧 chưa dùng được (thư mục/file chưa tạo); cập nhật file này khi thành phần đó xuất hiện.

## 1. Đầu phiên (chỉ đọc, an toàn)

```bash
git status --short && git branch --show-current   # biết đang ở đâu, có thay đổi dở không
git log --oneline -5
dotnet --version                                  # cần .NET 10
node --version && npm --version                   # 🚧 cần khi làm src/web (Angular, ADR 0009); phiên bản theo config/tool-versions.yaml
docker --version && docker compose version
terraform version                                 # 🚧 chỉ cần từ giai đoạn 4
```

Sau đó đọc "Trạng thái hiện tại" trong `PROJECT.md` để biết giai đoạn đang làm.

## 2. Gate trước khi báo xong

Quy tắc đầy đủ: `.agents/rules/code-change-gate.md`. Chạy gate của từng vùng đã đổi.

```bash
# .NET
dotnet build src/teamboard.slnx -warnaserror
dotnet test src/teamboard.slnx --filter "Category!=docker"   # nhanh; thêm test Docker bằng `--filter "Category=docker"` (cần Docker Desktop, cổng 6060 trống)
dotnet format src/teamboard.slnx --verify-no-changes

# Angular (src/web), 🚧 chưa scaffold; dùng `npm ci` (không `npm install`) trong gate
npm --prefix src/web ci
npm --prefix src/web run lint -- --max-warnings=0
npm --prefix src/web run format:check
npm --prefix src/web run typecheck
npm --prefix src/web test -- --watch=false         # gồm policy test, axe
npm --prefix src/web run build                    # production, budgets
npm --prefix src/web run openapi:check            # kiểu sinh từ OpenAPI khớp backend
npm --prefix src/web run e2e                      # Playwright smoke, trước PR
npm --prefix src/web audit --omit=dev --audit-level=high

# Secret + phụ thuộc có lỗ hổng (quy tắc: .agents/rules/quality-gate.md)
gitleaks protect --staged --redact                # 🚧 chưa cài
dotnet list src/teamboard.slnx package --vulnerable --include-transitive

# Docker
docker compose -f src/docker-compose.yml config -q

# Terraform
terraform -chdir=infra/envs/dev fmt -check        # 🚧
terraform -chdir=infra/envs/dev validate          # 🚧

# Chung: xem diff và quét secret thô
git status --short && git diff --stat
git diff | grep -n -i -E "password|secret|connectionstring|api[_-]?key" || true
```

Chỉ chạy phần liên quan tới thay đổi (đổi .NET thì không cần chạy terraform).

## 3. Chạy local

```bash
docker compose -f src/docker-compose.yml up --build -d
docker compose -f src/docker-compose.yml ps
docker compose -f src/docker-compose.yml logs -f <service>
docker compose -f src/docker-compose.yml down
```

Cổng hiện tại: `board-service` ở http://localhost:6060 (container 7070); health ở `/health/live`, `/health/ready`.
Quan sát cục bộ (Jaeger, chỉ bind 127.0.0.1): `docker compose -f src/docker-compose.yml -f src/docker-compose.observability.yml up --build -d`, giao diện http://127.0.0.1:16686 (API truy vấn chỉ có v3).
Compose theo môi trường: `src/docker-compose.develop.yml`, `src/docker-compose.stg.yml` (cần file `.env.*` tương ứng, xem mục Vấn đề đã biết trong `PROJECT.md`).

## 4. Infra (Terraform)

```bash
terraform -chdir=infra/envs/dev init              # 🚧 cần hồ sơ chỉ đọc
terraform -chdir=infra/envs/dev plan -out=tfplan -detailed-exitcode   # được chạy tự do
terraform -chdir=infra/envs/dev show -no-color tfplan                 # đọc diff, đếm delete/replace
trivy config infra/                               # 🚧
```

`apply` và `destroy` **không** tự chạy: hỏi người dùng và nêu rõ tài nguyên sẽ thay đổi, chi phí. Quy tắc đầy đủ: `.agents/rules/terraform.md`.

## 5. Không chạy khi chưa được đồng ý

- `terraform apply`, `terraform destroy`, mọi lệnh `aws ...` có ghi/xoá
- `git push`, `git commit`, `git reset --hard`, `docker system prune`, `docker volume rm`
- `dotnet ef database update` trên DB không phải local
