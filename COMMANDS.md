# Lệnh cho agent

Chạy từ gốc repo. Thực hiện mục 1 đầu mỗi phiên; mục 2 trước khi báo "xong"; các mục sau khi cần.
Lệnh đánh dấu 🚧 chưa dùng được (thư mục/file chưa tạo); cập nhật file này khi thành phần đó xuất hiện.

## 1. Đầu phiên (chỉ đọc, an toàn)

```bash
git status --short && git branch --show-current   # biết đang ở đâu, có thay đổi dở không
git log --oneline -5
dotnet --version                                  # cần .NET 10
docker --version && docker compose version
node --version && npm --version                   # 🚧 cần khi làm src/web (Angular)
terraform version                                 # 🚧 chỉ cần từ giai đoạn 4
```

Sau đó đọc "Trạng thái hiện tại" trong `PROJECT.md` để biết giai đoạn đang làm.

## 2. Gate trước khi báo xong

Quy tắc đầy đủ: `.agents/rules/code-change-gate.md`. Chạy gate của từng vùng đã đổi.

```bash
# .NET
dotnet build src/teamboard.slnx -warnaserror
dotnet test src/teamboard.slnx                    # 🚧 chưa có project test
dotnet format src/teamboard.slnx --verify-no-changes

# Angular (src/web) — 🚧 chưa scaffold; script cụ thể chốt khi tạo project
npm --prefix src/web run lint
npm --prefix src/web run build
npm --prefix src/web test -- --watch=false
npx --prefix src/web prettier --check "src/web/src/**/*.{ts,html,scss}"

# Secret + phụ thuộc có lỗ hổng (quy tắc: .agents/rules/quality-gate.md)
gitleaks protect --staged --redact                # 🚧 chưa cài
dotnet list src/teamboard.slnx package --vulnerable --include-transitive
npm --prefix src/web audit --omit=dev             # 🚧

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

Cổng hiện tại: `teamboard-api` ở http://localhost:6060 (container 7070).
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
