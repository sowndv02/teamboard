# Mô hình chi phí (chi phí đi đầu)

Dùng ở bước 1 và 4 của skill `aws-service-selection`. Mục tiêu: biết **bậc độ lớn và cấu trúc** chi phí của từng ứng viên trước khi bàn tính năng.

**Về giá:** giá AWS thay đổi theo vùng, theo thời điểm và theo cách dùng. Lấy từ **trang giá chính thức của dịch vụ** hoặc **AWS Pricing Calculator**; nếu công cụ web có sẵn thì đọc trang giá hiện hành, nếu không thì để tham số và ghi `chưa xác minh`. **Không dùng con số nhớ mơ hồ**; mọi con số ghi: đơn vị, giả định, vùng, ngày tra.

## 1. Trần chi phí

- Nguồn: `config/project.yaml` → `quality.monthlyBudgetUsd` (toàn dự án). `TODO` → hỏi người dùng.
- Chia: phần cho service này (đề xuất theo tỷ lệ độ nặng; người dùng chốt) và theo môi trường (dev nhỏ nhất, stg vừa, prod theo nhu cầu; dự án học thường chỉ dev/stg chạy theo lúc dùng).
- Quy mọi ứng viên về **% trần** để so sánh trực quan.

## 2. Phân loại chi phí (bắt buộc tách)

| Loại | Ví dụ | Vì sao quan trọng |
|---|---|---|
| **Cố định / chạy rỗng (idle)** | Tính giờ khi không có tải: NAT Gateway, load balancer, RDS đang chạy, instance/task chạy liên tục, cluster/control plane, key KMS, interface VPC endpoint theo AZ | Phát sinh 24/7 kể cả khi không ai dùng; là nguyên nhân hoá đơn bất ngờ hay gặp nhất |
| **Theo mức dùng (usage)** | Số request, thời gian chạy, dung lượng lưu, GB xử lý/truyền, số sự kiện | Tăng theo tải; kiểm soát bằng giới hạn |
| **Ẩn / gián tiếp** | Truyền dữ liệu (Internet, xuyên AZ/vùng), xử lý NAT theo GB, ingest log (GB), lưu log lâu, gọi KMS/Secrets/Parameter, backup/snapshot, ECR image, request cho S3, metric tuỳ chỉnh | Thường bị quên trong ước tính |
| **Vận hành con người** | Vá lỗi OS/runtime, nâng cấp, trực sự cố, debug, học dịch vụ mới | Dịch vụ do mình quản (EC2, tự dựng) rẻ hoá đơn nhưng đắt công; dịch vụ managed ngược lại |
| **Lock-in / chuyển đổi** | Công sức đổi dịch vụ sau này | Ảnh hưởng chọn dịch vụ độc quyền |

## 3. Quy trình ước tính

1. **Danh sách thành phần** của ứng viên (không chỉ dịch vụ chính: cả NAT, ALB, log, KMS, endpoint đi kèm).
2. **Đơn vị tính phí** của từng thành phần (giờ, GB-tháng, triệu request, GB truyền, vCPU-giây...) từ trang giá.
3. **Số lượng đơn vị** từ tải trong spec/design: ví dụ `giờ chạy/tháng ≈ 730` nếu chạy liên tục; request/tháng = RPS trung bình × 2,6 triệu giây (xấp xỉ 30 ngày) khi có RPS; GB lưu/tháng từ ước lượng dữ liệu.
4. **Công thức** `chi phí = Σ (số đơn vị × giá đơn vị)`; ghi dạng bảng, mỗi dòng có giả định.
5. **Ba kịch bản tải:** thấp (≈0,1×), kỳ vọng (1×), đỉnh (≈10× hoặc theo spec). Với dịch vụ chạy rỗng, kịch bản thấp cho thấy chi phí nền.
6. **Theo môi trường:** dev/stg/prod có cấu hình và thời gian chạy khác nhau (ví dụ dev chỉ chạy vài giờ/ngày nếu có lịch tắt).
7. **Chi phí ẩn:** rà bảng ở mục 2; nêu rõ phần đã tính và phần chưa tính.
8. **Quy %** theo trần; đánh dấu ứng viên vượt trần hoặc chiếm tỷ lệ lớn.
9. **Độ nhạy:** yếu tố nào thay đổi hoá đơn nhiều nhất (ví dụ lưu lượng NAT, số GB log, số request)? Ghi ngưỡng cần xem lại.

Mẫu bảng:

| Thành phần | Đơn vị tính phí | Số đơn vị/tháng (giả định) | Giá đơn vị (nguồn, vùng, ngày) | Chi phí/tháng | Cố định hay theo mức dùng |
|---|---|---|---|---|---|

## 4. Heuristic phân loại nhanh (để chọn ứng viên, không phải số liệu)

- **Không chạy rỗng, trả theo dùng** (hợp tải thất thường/thấp): Lambda, SQS, SNS, EventBridge, DynamoDB on-demand, S3, API Gateway, Step Functions (theo bước chuyển).
- **Có phần cố định nhưng chia sẻ được:** ALB dùng chung nhiều service (một ALB, nhiều rule), một NAT/VPC endpoint dùng chung, một cluster ECS cho nhiều service (ECS control plane không tính riêng; trả cho tác vụ chạy).
- **Chạy rỗng đáng kể (24/7):** RDS/Aurora, ElastiCache, OpenSearch, MSK, Amazon MQ, NAT Gateway, ALB, EKS control plane, tác vụ Fargate/EC2 luôn chạy.
- **Rẻ hoá đơn, đắt công:** tự quản trên EC2 (database tự cài, tự dựng hàng đợi).

Quy tắc chọn: tải thấp/không đều → ưu tiên **trả theo dùng, không chạy rỗng**; tải đều, dự đoán được → có thể cố định rẻ hơn theo đơn vị; trước khi chọn dịch vụ chạy rỗng, ghi rõ **vì sao** nó đáng chi phí nền.

## 5. Cạm bẫy chi phí AWS hay gặp (kiểm khi chọn)

- **NAT Gateway**: tính giờ + theo GB; tác vụ trong private subnet gọi dịch vụ AWS qua NAT làm tăng chi phí. Dùng **gateway endpoint** (S3, DynamoDB, thường không tính phí endpoint) hoặc interface endpoint khi hợp lý, hoặc dev chỉ một NAT, hoặc đặt tác vụ dev ở public subnet có kiểm soát bảo mật (cân nhắc rủi ro). Mọi lựa chọn là **đánh đổi chi phí ↔ bảo mật** → CONFIRM.
- **Load balancer**: một ALB dùng chung (rule theo host/path) rẻ hơn mỗi service một ALB.
- **RDS**: Multi-AZ gần như nhân đôi chi phí instance; SQL Server có chi phí giấy phép; storage/IOPS/backup vượt hạn mức tính thêm; instance dừng có thể tự khởi động lại sau thời hạn của AWS (kiểm tra quy định hiện hành).
- **Fargate/EC2**: kích thước vCPU/RAM thừa; `desired_count` cao; autoscaling không `max_capacity`.
- **CloudWatch Logs**: ingest theo GB + lưu giữ; không đặt retention = giữ mãi; log DEBUG ở môi trường thật; metric tuỳ chỉnh/độ phân giải cao.
- **Truyền dữ liệu**: ra Internet, xuyên AZ, xuyên vùng; thiết kế service giao tiếp xuyên AZ nhiều.
- **KMS / Secrets Manager**: phí theo khoá/secret và theo số lần gọi; Parameter Store (standard) rẻ hơn cho cấu hình thường; cân nhắc yêu cầu xoay vòng.
- **Tài nguyên mồ côi**: Elastic IP không gắn, EBS/snapshot không dùng, image ECR cũ, bucket log lớn: đặt lifecycle và dọn.
- **Free tier**: có giới hạn thời gian/số lượng và điều kiện; không tính vào ước tính dài hạn trừ khi xác minh còn hiệu lực.
- **Savings Plans / Reserved**: chỉ xét khi mức dùng ổn định dài hạn; không phù hợp với môi trường học thay đổi liên tục.
- **Denial of wallet**: API công khai không giới hạn tốc độ, autoscaling không trần, Lambda/SQS không giới hạn đồng thời → đặt trần và cảnh báo.

## 6. Kiểm soát chi phí (đưa vào danh sách tài nguyên đề xuất)

- **AWS Budgets** theo môi trường/tag, cảnh báo ở các ngưỡng (ví dụ 50/80/100% trần); Cost Anomaly Detection nếu cần.
- **Trần mở rộng** (`max_capacity`, giới hạn đồng thời, throttling, WAF rate rule).
- **Retention** log/metric; lifecycle S3/ECR; xoá snapshot cũ.
- **Lịch tắt/`destroy`** cho dev/stg ngoài giờ học; ghi cách tắt trong tài liệu.
- **Tag chi phí** (`project`, `env`, `service`, `owner`) để phân bổ và truy vết; bật cost allocation tag (khi áp dụng).
- **Review chi phí định kỳ** (hàng tuần trong giai đoạn học) và sau mỗi thay đổi hạ tầng.

## 7. Kiểm lại sau khi chọn

Cộng toàn stack (kể cả phần ẩn phát sinh do các lựa chọn tương tác với nhau, ví dụ chọn private subnet kéo theo NAT/endpoint; chọn nhiều service kéo theo log và truyền dữ liệu), so với phần ngân sách. Vượt → quay lại; nêu rõ phương án rẻ hơn đã bỏ vì lý do gì, và tác động nếu chọn nó.

## 8. Tín hiệu xem lại (revisit)

Ghi các ngưỡng đo được: chi phí thực tế vượt X% ước tính; RPS/GB vượt ngưỡng đổi phương án; idle cost > Y% hoá đơn; hoá đơn tăng bất thường (anomaly). Mỗi ngưỡng gắn với hành động (chuyển loại dịch vụ, đổi kích thước, thêm cache, dọn).
