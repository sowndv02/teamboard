# Terraform / AWS (vibe code hạ tầng)

Lý do tồn tại: hạ tầng sai tốn tiền thật, lộ dữ liệu thật và khó đảo ngược. Agent được phép **đọc, kiểm tra, lập plan**; **không** tự thay đổi tài nguyên AWS. Áp dụng cho `infra/**` và mọi việc liên quan AWS/CI-CD.

Bổ sung cho: `architecture.md`, `quality-gate.md`, `code-change-gate.md`, `dependency-pinning.md`, `agent-conduct.md`, ADR 0003/0004. Kiến thức tấn công/phòng thủ để học: `document/security/infra-attack-scenarios.md`.

## 1. An toàn và phê duyệt

| Loại | Lệnh/hành động | Quy tắc |
|---|---|---|
| **Được chạy tự do** | `terraform fmt`, `validate`, `init` (không đổi state), `plan`, `show`, `output` (không `-raw` secret), `providers`, `graph`; `tflint`, `trivy config`, `checkov`; `aws ... describe/get/list` chỉ đọc | Cần hồ sơ đọc-only (mục 1.1) |
| **Chỉ khi người dùng đồng ý tại thời điểm đó** | `apply`, `destroy`, `import`, `state mv/rm/push`, `taint/untaint`, `force-unlock`, `workspace delete`, `-replace`, `-target`, `-auto-approve`, mọi lệnh `aws` ghi/xoá, `terraform apply` từ CI | Nêu rõ **tài nguyên thay đổi, chi phí, rủi ro**, kèm kết quả `plan`; một lần đồng ý **không** áp dụng cho lần sau |
| **Cấm** | Tự `apply`/`destroy` dù "chỉ môi trường dev"; `-auto-approve`; tạo/sửa tài nguyên bằng console/CLI ngoài Terraform; dùng credential của người khác/prod để thử; vô hiệu hoá guardrail (SCP, deletion protection) để lách | Gặp thì dừng và báo |

- `-target` làm lệch state so với code; chỉ dùng khi sự cố, có người dùng đồng ý và ghi vào issue.
- **Không bao giờ** đưa secret/credential vào prompt, file commit hay output báo cáo.

### 1.1 Credential cho agent
- Agent dùng **hồ sơ chỉ đọc** (read-only + quyền đọc state/plan), **không** giữ credential có quyền ghi.
- `apply` chạy bằng vai trò riêng do **người dùng** hoặc **CI có bước duyệt thủ công** nắm (xác thực qua SSO/OIDC, không access key tĩnh).
- Không có hồ sơ chỉ đọc → chỉ làm `fmt`/`validate`/scan tĩnh, nói rõ không thể `plan` thật.

## 2. Kiến trúc và ranh giới tài nguyên

- **Chỉ tạo tài nguyên đã được phê duyệt** trong design/plan (danh sách tài nguyên + lý do). Nguồn danh sách: `document/infra/<service>.md` mục "Danh sách tài nguyên đề xuất duyệt" (tạo bằng skill `aws-service-selection`, chi phí đi đầu); chưa có tài liệu đó thì chọn dịch vụ trước, đừng tự quyết dịch vụ khi viết Terraform. Tài nguyên không có trong plan = vi phạm; muốn thêm thì quay lại design/plan, không tự thêm.
- Cấu trúc (ADR 0004): `infra/modules/*` (tái sử dụng), `infra/envs/{dev,stg}` (root module + **state riêng mỗi env**), `infra/bootstrap` (S3 state). Môi trường **không** tham chiếu state/tài nguyên của môi trường khác.
- **Module:** một trách nhiệm; `variables.tf` (có `description`, `type`, `validation` khi hợp lý), `outputs.tf` (chỉ xuất cái cần), README ngắn; không logic ngầm theo tên môi trường bên trong module (dùng biến).
- **Dependency rõ ràng:** ưu tiên tham chiếu thuộc tính (`aws_x.y.id`) để Terraform tự suy ra thứ tự; `depends_on` chỉ khi bắt buộc và có comment lý do. Không vòng phụ thuộc giữa module.
- **Đặt tên và tag** (bắt buộc cho mọi tài nguyên hỗ trợ): tên `teamboard-<env>-<service>-<loại>`; tag `project=teamboard`, `env`, `service`, `managed_by=terraform`, `owner`, `cost_center`/`purpose`.
- Region/account lấy từ `config/environments.yaml` (đang `TODO` → hỏi người dùng, không đoán).
- Không dùng `provisioner "local-exec"`/`remote-exec`, `null_resource` chạy lệnh ra ngoài, hay `external` data source nếu không được duyệt (rủi ro thực thi mã tuỳ ý, khó tái lập).
- `count`/`for_each` dựa trên giá trị chưa biết lúc plan → tránh; ưu tiên `for_each` với khoá ổn định (không dùng chỉ số).
- Tài nguyên giữ dữ liệu (RDS, S3, DynamoDB, KMS): `lifecycle { prevent_destroy = true }` ở môi trường quan trọng (stg/prod), cùng deletion protection và sao lưu.

## 3. Bảo mật và IAM

- **Least privilege:** vai trò riêng cho từng service (task role) và từng pipeline; không `Action: "*"` / `Resource: "*"` trừ khi bất khả kháng và có comment lý do cùng ghi vào issue; dùng điều kiện (`aws:SourceArn`, `aws:SourceAccount`, tag) và permission boundary khi hợp lý. Chú ý hành động leo thang đặc quyền (`iam:PassRole`, `iam:CreatePolicyVersion`, `iam:AttachRolePolicy`, `sts:AssumeRole` rộng).
- **Không hardcode secret** trong `.tf`, `*.tfvars`, biến mặc định, `user_data`, task definition. Lấy từ Secrets Manager / SSM Parameter Store (SecureString); service lấy lúc chạy bằng task role. Biến nhạy cảm `sensitive = true`. Lưu ý **state có thể chứa secret** (xem mục 6).
- **Không mở public tuỳ tiện:**
  - S3: bật Block Public Access, mã hoá (SSE-KMS/SSE-S3), versioning khi cần, chặn truyền không TLS.
  - RDS/cơ sở dữ liệu và hàng đợi: private subnet, `publicly_accessible = false`, không `0.0.0.0/0`.
  - Security group: không `0.0.0.0/0` cho cổng quản trị (22/3389) hay DB; chỉ mở cổng cần thiết từ nguồn cụ thể (SG khác/ALB). Quản trị qua SSM Session Manager thay vì SSH mở.
  - Policy tài nguyên (SQS/SNS/S3/KMS/ECR): giới hạn principal, không `Principal: "*"` không điều kiện.
- **Mã hoá:** dữ liệu lưu (KMS) và truyền (TLS); EBS/RDS/S3/SQS/SNS bật mã hoá.
- **EC2/ECS:** IMDSv2 bắt buộc (`http_tokens = required`, hop limit phù hợp); container chạy không root; image từ registry tin cậy, bật quét ECR.
- **Nhật ký và phát hiện:** CloudTrail, VPC Flow Logs/ALB logs khi cần, GuardDuty (theo chi phí); log group có **retention**.
- **CI/CD:** OIDC GitHub → AWS; trust policy ràng buộc `sub`/`aud` (đúng repo, nhánh/environment), **không** `repo:*`; quyền của vai trò CI tối thiểu, `apply` cần duyệt.
- Mọi ngoại lệ bảo mật phải có **lý do ghi trong issue/ADR** và được người dùng chấp nhận (Confirm log), không im lặng bỏ qua cảnh báo scanner.

## 4. Kiểm soát chi phí

Nhận diện tài nguyên **tính phí theo giờ hoặc theo lưu lượng** trước khi đề xuất; ghi vào plan kèm ước tính (kèm giả định và nguồn giá, xem trang giá AWS hiện hành; không bịa số):

| Tài nguyên | Vì sao dễ phát sinh | Cách kiểm soát |
|---|---|---|
| **NAT Gateway** | Tính giờ + theo dung lượng xử lý; dễ bị quên | Tránh nếu không cần (VPC endpoint cho S3/DynamoDB/ECR…, hoặc 1 NAT cho dev); ghi rõ trong plan; tắt sau lab |
| **Load balancer (ALB/NLB)** | Tính giờ liên tục | Một ALB dùng chung (rule theo host/path) thay vì mỗi service một ALB; `destroy` sau lab |
| **RDS (nhất là SQL Server)** | Tính giờ 24/7, giấy phép; Multi-AZ nhân đôi | Instance nhỏ nhất cho dev, Single-AZ; dừng/xoá sau lab; cân nhắc PostgreSQL thay SQL Server nếu được |
| **ECS Fargate / EC2** | Tính theo vCPU/RAM chạy | Kích thước nhỏ; `desired_count` thấp; **đặt `max_capacity` cho autoscaling** (tránh "denial of wallet") |
| **EKS/cluster được quản lý** | Phí control plane cố định | Không dùng nếu ADR 0003 chọn ECS |
| **CloudWatch Logs, X-Ray, GuardDuty, Config** | Theo dung lượng/sự kiện | Retention rõ, lọc log; bật theo nhu cầu |
| **Elastic IP, EBS, snapshot, ECR image** | Tài nguyên "mồ côi" vẫn tính phí | Dọn khi không dùng; lifecycle policy cho ECR/S3 |
| **Truyền dữ liệu (cross-AZ/Internet)** | Tính theo GB | Đặt tài nguyên cùng AZ khi hợp lý; dùng endpoint |
| **Lambda/API Gateway/SQS khối lượng lớn** | Theo số lần gọi | Giới hạn đồng thời, throttling |

Quy tắc:
- **Tài nguyên không có trong ước tính được duyệt = không tạo.** Phát sinh ngoài dự kiến → dừng và hỏi.
- Mọi môi trường có **AWS Budgets** (qua Terraform) với cảnh báo theo `quality.monthlyBudgetUsd` (đang `TODO` → hỏi người dùng).
- Môi trường học/lab có **cách tắt** (`destroy`) và nhắc dọn sau buổi lab; kích thước dev/stg tối thiểu.
- Ước tính chi phí thay đổi bằng công cụ (Infracost 🚧) hoặc bảng thủ công trong plan; thay đổi làm **tăng** chi phí phải nêu rõ trong báo cáo.

## 5. Kiểm chứng và test

Chạy theo thứ tự, mọi bước phải xanh (hoặc nêu rõ không chạy được và vì sao):

```bash
terraform fmt -check -recursive
terraform init -backend=false          # kiểm cú pháp/provider không chạm state; init đầy đủ khi cần plan
terraform validate
tflint --recursive                      # 🚧
trivy config infra/                     # 🚧 quét misconfig/bảo mật (tfsec nay nằm trong Trivy)
checkov -d infra/                       # 🚧 tuỳ chọn, bổ sung kiểm tra chính sách
terraform plan -out=tfplan -detailed-exitcode
terraform show -no-color tfplan         # đọc diff
terraform show -json tfplan             # kiểm tra tự động (jq/OPA/conftest 🚧): có "delete", "replace" không?
```

- **Đọc diff thật kỹ**: đếm tài nguyên `create/update/delete/replace`; mọi `delete` hoặc `replace` (`-/+`, "must be replaced") phải được giải thích và người dùng chấp nhận.
- Cảnh báo scanner **không** được tắt (`#tfsec:ignore`, `skip_check`) trừ khi có lý do ghi trong issue, kèm chấp nhận của người dùng; ngoại lệ hẹp nhất có thể.
- Test hạ tầng: `terraform test` (mô-đun, `command = plan` ưu tiên; `apply` trong test chỉ ở tài khoản sandbox được duyệt) hoặc Terratest 🚧; kiểm tra `precondition`/`postcondition` và `validation` của biến.
- Không dùng dữ liệu/secret thật trong fixture; không chạy test tạo tài nguyên trên môi trường dev/stg/prod dùng chung.
- `terraform plan` **không** đảm bảo apply thành công (quyền, giới hạn dịch vụ, tên trùng); báo rõ mức chắc chắn.

## 6. State và quản lý thay đổi

- **Remote state** (S3, mã hoá, versioning bật, chặn public, quyền tối thiểu); **khoá state** (S3 native locking với `use_lockfile` — cần phiên bản Terraform hỗ trợ, kiểm tra `config/tool-versions.yaml`; hoặc DynamoDB lock). Mỗi môi trường một state/khoá riêng. **Không commit** `*.tfstate`, `.terraform/`, `tfplan`.
- **State chứa dữ liệu nhạy cảm** (mật khẩu, khoá): hạn chế ai đọc được; không `terraform output` secret ra log/chat.
- **Drift:** phát hiện bằng `terraform plan -refresh-only`; không sửa tay trên console. Có drift → báo người dùng, quyết định (đưa code về thực tế hay áp lại code), không tự `apply`.
- **Refactor không phá tài nguyên:** đổi tên/di chuyển tài nguyên dùng khối `moved`, nhập tài nguyên có sẵn dùng `import` block; **kiểm tra plan không còn delete/replace ngoài ý muốn** trước khi báo xong.
- **Thay đổi huỷ/thay thế (destructive/replacement):** đổi thuộc tính "forces replacement" (tên, AZ, engine…) sẽ xoá rồi tạo lại; với tài nguyên có dữ liệu → bắt buộc có kế hoạch sao lưu/di chuyển và người dùng đồng ý. Ưu tiên `create_before_destroy` khi cần liên tục.
- `state mv/rm/import`, `taint`, `force-unlock` chỉ khi người dùng đồng ý và có sao lưu state (versioning S3 hoặc `terraform state pull` lưu cục bộ ngoài repo).
- **Phiên bản:** `required_version` và provider pin (`dependency-pinning.md`); commit `.terraform.lock.hcl`; nâng provider/Terraform là thay đổi riêng, có plan sạch trước/sau.
- Thay đổi hạ tầng đi theo quy trình `pipeline-lifecycle.md` (làn hạ tầng): issue → plan → review → người dùng duyệt → apply (do người dùng/CI) → kiểm chứng → ghi vào issue.

## 7. Review và bằng chứng (trước khi báo hoàn thành)

Báo cáo phải có đủ, ngắn gọn:

```
Thay đổi: <tóm tắt, tài nguyên create/update/delete/replace: N/N/N/N>
Vì sao: <nối về spec/design/plan>
Rủi ro: <bảo mật, chi phí, dữ liệu, phụ thuộc>; mức độ; cách giảm
Chi phí: <ước tính trước → sau, giả định>; tài nguyên tính giờ mới: <...>; cách tắt: <...>
Kiểm chứng: fmt ✅ | validate ✅ | tflint ✅ | trivy ✅ (n cảnh báo, đã xử lý/ngoại lệ có lý do) | plan ✅ (đã đọc diff)
Chưa kiểm chứng: <phần nào, vì sao> (ví dụ chưa có hồ sơ chỉ đọc nên chưa plan thật)
Cần người dùng: <quyết định/duyệt apply>
```

- Không nói "xong/an toàn" khi chưa chạy kiểm chứng; không giấu cảnh báo scanner hay `delete/replace` trong plan.
- Review code hạ tầng dùng thêm probe chi phí/bảo mật của `code-review.md` và lăng kính trong `review-philosophy.md`.

## 8. Mô phỏng tấn công hạ tầng (học tập)

Mục đích: hiểu **cách hạ tầng bị tấn công và cách phát hiện/phòng ngừa**, từ đó viết Terraform an toàn hơn. Danh mục kịch bản, dấu hiệu phát hiện và biện pháp phòng ngừa: `document/security/infra-attack-scenarios.md`.

Điều kiện bắt buộc (vi phạm = dừng):
1. **Chỉ trên tài nguyên của chính bạn, trong tài khoản AWS sandbox riêng** (không phải dev/stg/prod dùng chung, không phải hệ thống của người khác). Không dữ liệu thật.
2. **Có người dùng phê duyệt từng bài thực hành** (vì cần `apply`); ghi vào issue: mục tiêu, phạm vi, thời gian, cách dọn.
3. Có **ngân sách và Budgets** cho tài khoản sandbox; **`destroy` ngay sau khi xong**.
4. Tuân **chính sách kiểm thử xâm nhập của AWS hiện hành** (kiểm tra trang chính sách; không tấn công từ chối dịch vụ, không kiểm thử vào tài nguyên không thuộc về bạn).
5. Mã dễ bị tấn công cố ý đặt riêng ở `infra/labs/` (không nằm trong `modules/` hay `envs/`), có **khoá an toàn**: `precondition` kiểm account ID nằm trong danh sách sandbox được phép và biến `allow_vulnerable_lab = true`; không bao giờ trộn vào môi trường thật.
6. Không dùng kỹ thuật phá hoại thật (xoá dữ liệu thật, tấn công bên thứ ba, vượt giới hạn dịch vụ) và không viết công cụ khai thác tự động nhằm mục đích tấn công thực tế.

Cách học theo mức an toàn tăng dần (ưu tiên từ trên xuống):
- **Phân tích tĩnh (không triển khai):** cho scanner (Trivy/Checkov/tflint) quét bản Terraform cố ý sai cấu hình và đọc xem nó bắt gì; so sánh bản sai và bản đúng.
- **Tabletop/mô hình đe doạ:** đi qua kịch bản trên giấy theo STRIDE (kẻ tấn công làm gì → cấu hình nào cho phép → phát hiện bằng gì → chặn bằng gì).
- **Đánh giá tư thế (posture) tài khoản sandbox bằng công cụ chỉ đọc:** ví dụ Prowler, ScoutSuite (kiểm tra bản hiện hành của công cụ).
- **Mô phỏng kỹ thuật tấn công có kiểm soát trong sandbox:** ví dụ Stratus Red Team (kích hoạt từng kỹ thuật rồi dọn), CloudGoat hoặc flAWS.cloud (bài tập cố ý có lỗ hổng) — chỉ trong tài khoản sandbox, theo hướng dẫn của công cụ; tên công cụ cần xác minh phiên bản và tình trạng bảo trì hiện tại.
- Bật **CloudTrail/GuardDuty** trong sandbox để xem **dấu hiệu phát hiện** mà mô phỏng sinh ra (đây là giá trị học chính).

Mỗi bài thực hành ghi vào `document/security/` hoặc runbook: kịch bản, cấu hình sai, điều quan sát được, cách phát hiện, cách phòng ngừa bằng Terraform, và bản vá. Phát hiện lỗ hổng trong hạ tầng thật khi làm việc → báo người dùng ngay, không khai thác thêm.

## 9. Checklist trước khi báo xong thay đổi hạ tầng

- [ ] Chỉ tài nguyên đã duyệt; module/env/dependency rõ; tag và tên đúng quy ước
- [ ] Không secret trong code/tfvars/state commit; IAM least privilege; không public tuỳ tiện; mã hoá bật
- [ ] Đã nhận diện tài nguyên tính phí (NAT, LB, RDS, Fargate…), ước tính chi phí, có Budgets và cách tắt
- [ ] `fmt`, `validate`, `tflint`, `trivy config`, `plan` đã chạy; đã đọc kỹ diff; mọi `delete/replace` được giải thích và người dùng chấp nhận
- [ ] State/khoá/drift đã xem xét; không `state`/`-target`/`-auto-approve` chưa được phép
- [ ] Không tự `apply`/`destroy`; báo cáo có đủ thay đổi, rủi ro, chi phí, kiểm chứng, phần chưa kiểm chứng
