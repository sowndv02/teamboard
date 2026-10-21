# Kịch bản tấn công hạ tầng AWS (để học và phòng thủ)

Mục đích: hiểu **cấu hình sai nào cho kẻ tấn công cơ hội**, **phát hiện bằng gì**, **chặn bằng Terraform thế nào**. Nội dung ở mức khái niệm và phòng thủ, không có hướng dẫn khai thác từng bước.

**Điều kiện thực hành (xem `.agents/rules/terraform.md` mục 8):** chỉ trong tài khoản AWS sandbox riêng của bạn, không dữ liệu thật, có người dùng phê duyệt từng bài, có Budgets, `destroy` ngay sau khi xong, tuân chính sách kiểm thử xâm nhập của AWS hiện hành. Mã dễ bị tấn công cố ý đặt ở `infra/labs/` kèm khoá an toàn.

**Độ tin cậy:** đây là kiến thức tổng quát được tổng hợp; tên dịch vụ và công cụ cần kiểm tra lại theo tài liệu hiện hành (AWS, OWASP, MITRE ATT&CK for Cloud) trước khi dựa vào.

## Cách đọc một kịch bản

Mỗi kịch bản: **mục tiêu kẻ tấn công** → **cấu hình sai cho phép** → **dấu hiệu phát hiện** → **phòng ngừa (Terraform/chính sách)** → **cách kiểm chứng an toàn** → **liên quan TeamBoard**.

---

## Nhóm A: lộ dữ liệu

### A1. Bucket S3 công khai
- **Mục tiêu:** đọc/ghi dữ liệu không cần xác thực.
- **Cấu hình sai:** ACL/policy cho `Principal: "*"`, tắt Block Public Access.
- **Phát hiện:** IAM Access Analyzer (finding "public"), AWS Config rule, Macie (dữ liệu nhạy cảm), S3 server access/CloudTrail data events.
- **Phòng ngừa:** bật Block Public Access ở cấp tài khoản và bucket; policy chặn truyền không TLS; SCP cấm tắt Block Public Access.
- **Kiểm chứng an toàn:** cho Trivy/Checkov quét Terraform bucket cố ý sai; so với bản đúng.
- **TeamBoard:** bucket state Terraform (mã hoá, chặn public, quyền tối thiểu).

### A2. Cơ sở dữ liệu/snapshot lộ ra Internet
- **Mục tiêu:** truy cập DB trực tiếp hoặc chép snapshot.
- **Cấu hình sai:** `publicly_accessible = true`, security group `0.0.0.0/0` cổng DB, snapshot chia sẻ công khai.
- **Phát hiện:** Config rule, Security Hub, quét SG; GuardDuty (đăng nhập bất thường).
- **Phòng ngừa:** DB ở private subnet; SG chỉ nhận từ SG ứng dụng; mã hoá KMS; chặn chia sẻ snapshot công khai; mật khẩu từ Secrets Manager.
- **TeamBoard:** RDS SQL Server/PostgreSQL của board/task/identity.

### A3. Secret nằm sai chỗ (env, task definition, log, state)
- **Mục tiêu:** đọc secret rồi dùng để vào hệ thống khác.
- **Cấu hình sai:** secret trong biến môi trường plain, `user_data`, tfvars commit, log, hoặc state bucket quyền rộng.
- **Phát hiện:** secret scanning (gitleaks), Macie, kiểm tra định kỳ task definition; CloudTrail `GetSecretValue` bất thường.
- **Phòng ngừa:** Secrets Manager/SSM SecureString; task role lấy lúc chạy; `sensitive = true`; hạn chế quyền đọc state; không log secret.
- **TeamBoard:** `SA_PASSWORD`/connection string đã từng bị commit (xem `PROJECT.md`) — ví dụ thật của nhóm này.

### A4. State Terraform bị đọc
- **Mục tiêu:** lấy secret và bản đồ hạ tầng từ state.
- **Cấu hình sai:** bucket state không mã hoá, public, hoặc nhiều người/vai trò đọc được.
- **Phát hiện:** CloudTrail/S3 data events trên bucket state; Access Analyzer.
- **Phòng ngừa:** mã hoá, versioning, Block Public Access, policy chỉ cho vai trò Terraform; không dùng state chung nhiều môi trường.

---

## Nhóm B: danh tính và đặc quyền

### B1. Lộ khoá truy cập dài hạn
- **Mục tiêu:** chiếm quyền tài khoản, thường để đào tiền ảo hoặc lấy dữ liệu.
- **Cấu hình sai:** access key tĩnh trong repo/CI/máy.
- **Phát hiện:** GuardDuty (hoạt động từ vị trí/IP lạ), CloudTrail (API bất thường, tạo tài nguyên ở vùng lạ), AWS cảnh báo key bị lộ trên GitHub.
- **Phòng ngừa:** không dùng key tĩnh; SSO cho người, OIDC cho CI; phiên ngắn hạn; secret scanning và push protection; quay vòng key nếu buộc phải có.

### B2. Vai trò quá rộng và leo thang đặc quyền
- **Mục tiêu:** từ quyền nhỏ nâng lên quản trị.
- **Cấu hình sai:** `Action: "*"`/`Resource: "*"`; cho phép `iam:PassRole`, `iam:CreatePolicyVersion`, `iam:AttachRolePolicy`, `sts:AssumeRole` rộng.
- **Phát hiện:** IAM Access Analyzer (policy validation, unused access), công cụ phân tích đặc quyền (kiểm tra bản hiện hành: ví dụ Cloudsplaining, PMapper), CloudTrail `iam:*`.
- **Phòng ngừa:** least privilege theo từng service; permission boundary; SCP; điều kiện `aws:SourceArn`; chỉ định rõ role được `PassRole`.
- **TeamBoard:** task role mỗi service (SQS, DynamoDB, SNS riêng).

### B3. Tin cậy OIDC/CI quá rộng
- **Mục tiêu:** bất kỳ repo/nhánh nào cũng assume được vai trò triển khai.
- **Cấu hình sai:** trust policy GitHub OIDC không ràng buộc `sub`/`aud` (ví dụ `repo:*`).
- **Phát hiện:** CloudTrail `AssumeRoleWithWebIdentity` từ repo lạ; Access Analyzer.
- **Phòng ngừa:** điều kiện chặt `repo:owner/name:ref:refs/heads/main` hoặc environment; vai trò `plan` (đọc) tách `apply`; `apply` cần duyệt.

### B4. Lấy credential qua metadata (SSRF → IMDS)
- **Mục tiêu:** ứng dụng bị lừa gọi `169.254.169.254` để lấy credential của instance/task.
- **Cấu hình sai:** IMDSv1 bật; ứng dụng gọi URL do người dùng cung cấp; egress không giới hạn.
- **Phát hiện:** GuardDuty (credential dùng từ ngoài), log truy cập.
- **Phòng ngừa:** IMDSv2 bắt buộc, hop limit phù hợp; validate/allowlist URL phía ứng dụng; egress tối thiểu; quyền task role nhỏ.

---

## Nhóm C: mạng và bề mặt tấn công

### C1. Cổng quản trị mở ra Internet
- **Cấu hình sai:** SSH/RDP/DB `0.0.0.0/0`.
- **Phát hiện:** Config/Security Hub, quét SG; GuardDuty (brute force).
- **Phòng ngừa:** SSM Session Manager thay SSH; private subnet; SG tham chiếu SG khác; không public IP cho dịch vụ nội bộ.

### C2. Endpoint quản trị/tài liệu lộ ở môi trường thật
- **Mục tiêu:** thu thập thông tin hoặc gọi API quản trị.
- **Cấu hình sai:** Swagger UI, actuator/health chi tiết, trang debug bật ở stg/prod.
- **Phát hiện:** quét bề mặt công khai, log ALB.
- **Phòng ngừa:** chỉ bật ở môi trường không phải prod (`api-endpoints.md` mục 8); WAF/allowlist; xác thực cho endpoint quản trị.
- **TeamBoard:** dự án đã có Swagger UI (`chore: Add swagger UI`): ví dụ thật.

### C3. Policy tài nguyên mở (SQS/SNS/ECR/KMS)
- **Mục tiêu:** chèn thông điệp giả, đọc hàng đợi, kéo image.
- **Cấu hình sai:** `Principal: "*"` không điều kiện.
- **Phòng ngừa:** giới hạn principal và `aws:SourceArn`/`aws:SourceAccount`; đây cũng là chỗ confused deputy.
- **TeamBoard:** `teamboard-events` (SNS) → SQS mỗi consumer.

### C4. Di chuyển ngang (lateral movement)
- **Cấu hình sai:** SG cho phép mọi thứ trong VPC; vai trò dùng chung giữa service.
- **Phòng ngừa:** phân đoạn theo service; SG theo cặp; vai trò riêng; chính sách mạng tối thiểu.

---

## Nhóm D: chi phí và sẵn sàng

### D1. "Denial of wallet": cạn tiền thay vì cạn dịch vụ
- **Mục tiêu:** làm tốn tiền bằng cách kích hoạt tự động mở rộng hoặc gọi hàng loạt.
- **Cấu hình sai:** autoscaling không có `max_capacity`; API công khai không giới hạn tốc độ; Lambda/SQS không giới hạn đồng thời.
- **Phát hiện:** AWS Budgets/Cost Anomaly Detection, CloudWatch.
- **Phòng ngừa:** `max_capacity`, throttling/WAF rate-based rule, giới hạn đồng thời, Budgets có hành động cảnh báo.

### D2. Đào tiền ảo bằng credential bị chiếm
- **Dấu hiệu:** instance GPU/lớn xuất hiện ở vùng không dùng.
- **Phòng ngừa:** SCP giới hạn vùng và loại instance; Budgets; GuardDuty; không key tĩnh (B1).

### D3. Từ chối dịch vụ lớp ứng dụng
- **Phòng ngừa:** CloudFront/WAF, rate limit ở gateway (`api-endpoints.md`), timeout và circuit breaker. **Không** thực hành DoS thật; chỉ tabletop và kiểm tra cấu hình (chính sách AWS cấm DoS trái phép).

---

## Nhóm E: toàn vẹn và phục hồi

### E1. Tắt hoặc xoá nhật ký
- **Mục tiêu:** che dấu vết.
- **Cấu hình sai:** có thể `StopLogging`, xoá log; không có log tập trung.
- **Phát hiện:** CloudTrail event `StopLogging`/`DeleteTrail`; cảnh báo.
- **Phòng ngừa:** trail toàn tổ chức/đa vùng; validate file log; bucket log khoá (Object Lock); SCP cấm tắt.

### E2. Xoá dữ liệu / ransomware
- **Cấu hình sai:** không versioning, không deletion protection, sao lưu cùng tài khoản.
- **Phòng ngừa:** versioning + MFA delete (khi phù hợp), `deletion_protection`, `prevent_destroy`, sao lưu tách tài khoản/Object Lock, kiểm thử khôi phục.

### E3. Chuỗi cung ứng (module, provider, image)
- **Mục tiêu:** mã độc đi vào qua Terraform module, provider, image container.
- **Cấu hình sai:** nguồn module không pin (nhánh/`latest`), không `.terraform.lock.hcl`, image không quét.
- **Phòng ngừa:** pin version/checksum, commit lock file, nguồn tin cậy, quét ECR/Trivy, review thay đổi phụ thuộc (`dependency-pinning.md`).

### E4. Chiếm quyền bằng DNS treo (dangling DNS)
- **Cấu hình sai:** bản ghi Route 53 trỏ tới tài nguyên đã xoá (bucket, CloudFront, ELB).
- **Phòng ngừa:** dọn bản ghi khi xoá tài nguyên (cùng Terraform), giám sát DNS.

---

## Cách biến kịch bản thành bài học (quy trình đề xuất)

1. **Chọn** một kịch bản; viết giả thuyết ("nếu bucket này public thì...").
2. **Đọc tĩnh:** viết bản Terraform sai và bản đúng; cho scanner quét; ghi cảnh báo nào xuất hiện.
3. **Tabletop STRIDE:** vẽ ranh giới tin cậy, liệt kê mối đe doạ, biện pháp, rủi ro còn lại.
4. **(Tuỳ chọn, cần phê duyệt)** triển khai bản sai trong **tài khoản sandbox** (`infra/labs/`), bật CloudTrail/GuardDuty, kích hoạt đúng một kỹ thuật bằng công cụ có kiểm soát, **quan sát dấu hiệu phát hiện**, rồi `destroy`.
5. **Ghi lại:** cấu hình sai, điều quan sát, cách phát hiện, cách phòng ngừa, bản vá Terraform; thêm quy tắc vào `terraform.md` nếu thấy thiếu.

## Công cụ và tài nguyên học (kiểm tra bản hiện hành trước khi dùng)

| Mục đích | Ví dụ | Ghi chú |
|---|---|---|
| Quét IaC tĩnh | Trivy (`trivy config`), Checkov, tflint | Chạy trong gate (`quality-gate.md`) |
| Đánh giá tư thế tài khoản (chỉ đọc) | Prowler, ScoutSuite | Chạy trên sandbox với vai trò đọc |
| Phân tích IAM/đặc quyền | IAM Access Analyzer, Cloudsplaining, PMapper | Dịch vụ AWS + công cụ mã nguồn mở |
| Mô phỏng kỹ thuật tấn công có kiểm soát | Stratus Red Team | Kích hoạt từng kỹ thuật rồi dọn; chỉ sandbox |
| Bài tập cố ý có lỗ hổng | CloudGoat, flAWS.cloud | Chỉ sandbox hoặc dịch vụ công khai do tác giả cung cấp |
| Khung kiến thức | MITRE ATT&CK for Cloud, OWASP Cloud-Native / Top 10, AWS Well-Architected (Security) | Tra tài liệu hiện hành |
| Phát hiện | GuardDuty, CloudTrail, Security Hub, AWS Config | Bật trong sandbox để thấy dấu hiệu |
