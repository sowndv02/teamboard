# Danh mục dịch vụ AWS để cân nhắc

Dùng ở bước 1–2 của skill `aws-service-selection` để **gợi ý ứng viên** và nhắc **bẫy**. Mỗi mục: *Dùng khi / Tránh khi / Dạng chi phí / Bẫy*. Đây là kiến thức tổng quát từ trí nhớ: **giới hạn và tính năng dịch vụ thay đổi theo thời gian**, luôn kiểm tra tài liệu và trang giá AWS hiện hành trước khi dựa vào (đặc biệt quota, kích thước tối đa, tính năng mới). Không phải đáp án: đối chiếu với drivers của service.

Ký hiệu chi phí: **R** = chạy rỗng (tính giờ/cố định), **U** = theo mức dùng, **H** = chi phí ẩn cần tính.

## 1. Chạy mã (compute)

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **Lambda** | Tải thất thường/sự kiện, xử lý ngắn, ít muốn vận hành | Chạy dài (giới hạn thời gian), cần kết nối bền, tải đều rất cao (có thể đắt hơn container), khởi động nguội nhạy cảm | U (request + thời gian × bộ nhớ); không R | Cold start; giới hạn thời gian, payload, đồng thời; gọi VPC/DB cần quản lý kết nối (pool); log CloudWatch (H) |
| **ECS Fargate** | Dịch vụ HTTP/worker chạy liên tục, không muốn quản OS, hợp ADR 0003 | Cần GPU/đặc thù phần cứng, tải rất ổn định dài hạn (EC2 rẻ hơn theo đơn vị) | R nếu luôn chạy (vCPU/RAM theo giây) | Kích thước thừa; `desired_count` cao; không `max_capacity`; image kéo qua NAT (H) |
| **ECS trên EC2** | Tải ổn định, tận dụng Savings Plans, kiểm soát instance | Đội nhỏ, không muốn vá OS/AMI | R (instance) | Công vận hành (vá, scale cluster, bin packing) |
| **EC2** | Cần kiểm soát hệ điều hành/phần mềm đặc thù | Dịch vụ web thông thường | R | Công vận hành cao; bảo mật OS; IMDSv2 |
| **App Runner** | Triển khai container đơn giản, ít cấu hình | Cần kiểm soát mạng/tuỳ biến sâu; kiểm tra phạm vi hỗ trợ hiện hành | R một phần (bộ nhớ cấp phát khi rỗi) + U | Ít tuỳ chọn hơn ECS; lock-in; kiểm tra tình trạng/lộ trình dịch vụ |
| **EKS** | Cần hệ sinh thái Kubernetes, đội có người vận hành | Đội nhỏ, một vài service | R (phí control plane + node) | Phức tạp và đắt so với nhu cầu (đã chọn ECS ở ADR 0003) |
| **AWS Batch** | Xử lý theo lô/hàng đợi công việc tính toán | API thời gian thực | U (tài nguyên tính toán bên dưới) | Cấu hình queue/compute env |

## 2. Lưu trữ đối tượng/tệp

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **S3** | Lưu tệp, tài nguyên tĩnh, backup, log, state Terraform | Cần filesystem POSIX, truy vấn quan hệ | U (GB-tháng + request + truyền) | Public nhầm (Block Public Access); versioning phình; lifecycle; request nhỏ lẻ |
| **EFS** | Cần filesystem chia sẻ giữa nhiều tác vụ | Chỉ lưu tệp thường (S3 rẻ hơn) | U (GB-tháng, throughput) | Đắt hơn S3 theo GB |
| **EBS** | Ổ đĩa cho EC2 | Chia sẻ nhiều instance | U (GB, IOPS) | Volume/snapshot mồ côi (H) |

## 3. Cơ sở dữ liệu

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **RDS PostgreSQL** | Dữ liệu quan hệ, giao dịch, truy vấn đa dạng; thường rẻ hơn SQL Server | Tải khoá-giá trị cực lớn dự đoán được (DynamoDB hợp hơn) | R (instance) + U (storage, IOPS, backup) | Multi-AZ ≈ nhân đôi; luôn chạy 24/7; connection limit; dừng/xoá sau lab |
| **RDS SQL Server** | Bắt buộc SQL Server (đang dùng ở board-service/EF Core) | Có thể thay bằng PostgreSQL | R, cộng giấy phép | **Đắt hơn** do giấy phép; edition và tính năng khác nhau; cân nhắc có thật sự cần |
| **Aurora (PostgreSQL/MySQL)** | Cần hiệu năng/đọc replicas/HA cao hơn RDS thường | Tải nhỏ (RDS rẻ hơn); dự án học ngân sách nhỏ | R (instance hoặc ACU) + U (I/O) | Serverless v2 có mức tối thiểu; kiểm tra khả năng giảm về rất thấp/tạm dừng hiện hành |
| **DynamoDB** | Truy cập theo khoá rõ, tải lớn/đột biến, độ trễ thấp ổn định, không join | Truy vấn ad-hoc/quan hệ phức tạp, giao dịch đa bảng nặng | U (on-demand) hoặc theo dung lượng cấp phát; không R với on-demand | Mô hình truy cập phải biết trước; hot partition; GSI tốn thêm; giới hạn kích thước item |
| **ElastiCache (Redis/Valkey)** | Cache, phiên, bộ đếm, rate limit tập trung | Tải thấp (chưa cần); dữ liệu nguồn sự thật | R (node) | Thêm một thành phần phải vận hành; vô hiệu hoá cache; chi phí 24/7 |
| **OpenSearch** | Tìm kiếm văn bản/lọc phức tạp, phân tích log | Chỉ lọc đơn giản (SQL chỉ mục đủ) | R (domain) hoặc serverless theo dùng | Đắt, vận hành phức tạp; đồng bộ dữ liệu |
| **DocumentDB / Neptune / Timestream / Keyspaces** | Nhu cầu đặc thù (tài liệu tương thích Mongo, đồ thị, chuỗi thời gian, Cassandra) | Không có nhu cầu đặc thù | Thường R | Kiểm tra giới hạn/tương thích; thường quá mức cho dự án này |

## 4. Hàng đợi, sự kiện, luồng, điều phối

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **SQS** (standard/FIFO) | Tác vụ nền, tách rời, retry + DLQ, một nhóm consumer | Cần phát lại lịch sử, nhiều consumer đọc cùng luồng có thứ tự | U (request) | Giới hạn kích thước message (kiểm tra hiện hành); FIFO giới hạn thông lượng; visibility timeout; at-least-once → idempotent |
| **SNS** | Fan-out pub/sub tới nhiều SQS/Lambda/HTTP | Cần lọc/định tuyến phức tạp (EventBridge) | U | Policy mở (`Principal: "*"`); delivery retry/DLQ |
| **SNS + SQS fan-out** | Một sự kiện, nhiều consumer độc lập, mỗi consumer queue + DLQ riêng (hợp `messaging.md`) | – | U | Cấu hình policy `SourceArn` đúng |
| **EventBridge** | Định tuyến sự kiện theo quy tắc, tích hợp dịch vụ AWS/SaaS, lập lịch | Thông lượng rất cao đơn giản (SNS/SQS rẻ hơn) | U (theo sự kiện) | Giới hạn kích thước/tốc độ; thứ tự không đảm bảo |
| **Kinesis Data Streams** | Luồng có thứ tự theo khoá, phát lại, nhiều consumer đọc | Tải thấp, không cần phát lại | R (shard) hoặc on-demand | Quản shard; chi phí nền |
| **MSK (Kafka)** | Đã có hệ sinh thái Kafka, throughput rất cao, phát lại lâu | Dự án nhỏ; chưa có nhu cầu | R (broker) cao | Đắt, vận hành phức tạp |
| **Amazon MQ** | Cần giao thức tương thích (AMQP/JMS) | Dự án mới không bị ràng buộc | R (broker) | Chi phí nền |
| **Step Functions** | Điều phối saga/quy trình nhiều bước có trạng thái | Luồng đơn giản | U (theo chuyển trạng thái) | Giới hạn thời gian/payload; Standard vs Express |

## 5. API và biên (edge)

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **ALB** | Điểm vào HTTP cho container, định tuyến theo host/path, dùng chung nhiều service | Chỉ dùng Lambda đơn giản | R (giờ) + U (LCU) | Một ALB dùng chung thay vì mỗi service một ALB |
| **API Gateway (HTTP API / REST API)** | Điểm vào cho Lambda, cần throttling/authorizer/usage plan | Dịch vụ container sau ALB, tải rất lớn (đắt theo request) | U (request) | HTTP API thường rẻ hơn REST API (kiểm tra khác biệt tính năng) |
| **CloudFront** | CDN, phục vụ nội dung tĩnh từ S3, giảm tải/độ trễ, nơi gắn WAF | Nội bộ thuần | U (truyền + request) | Cache invalidation; origin access control |
| **WAF** | Lọc tấn công, rate limit, chặn bot ở biên | Dự án nội bộ chưa công khai | U + phí theo web ACL/rule | Chi phí theo rule và request |
| **Route 53** | DNS, health check, định tuyến | – | U (hosted zone + query) | DNS treo (dangling) khi xoá tài nguyên |

## 6. Mạng

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **VPC + subnet public/private** | Mọi workload cần cô lập | – | Miễn phí (VPC); thành phần đi kèm tính phí | Thiết kế CIDR; số AZ |
| **NAT Gateway** | Tác vụ ở private subnet cần ra Internet | Không cần ra Internet; có thể dùng VPC endpoint | **R** (giờ) + U (GB) | Phát sinh thường xuyên; xem `cost-model.md` mục 5 |
| **VPC endpoint** (gateway S3/DynamoDB; interface cho dịch vụ khác) | Truy cập dịch vụ AWS không qua NAT/Internet | Số lượng endpoint interface lớn (mỗi AZ tính giờ) | Gateway thường không tính phí endpoint; interface có R + U | Cân nhắc số AZ |
| **PrivateLink / Transit Gateway / VPN** | Kết nối riêng giữa VPC/mạng ngoài | Dự án nhỏ một VPC | R + U | Thường quá mức |

## 7. Bảo mật, danh tính, bí mật

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **IAM** (role, policy, OIDC) | Mọi nơi: task role riêng từng service, OIDC cho CI | – | Miễn phí | Policy rộng; `iam:PassRole` |
| **KMS** | Mã hoá tự quản khoá (CMK), kiểm soát truy cập khoá | Mã hoá mặc định của dịch vụ đủ | R (mỗi khoá) + U (request) | Chi phí theo khoá và số lần gọi |
| **Secrets Manager** | Bí mật cần xoay vòng tự động | Cấu hình thường (Parameter Store rẻ hơn) | R (mỗi secret) + U | Chi phí theo số secret |
| **SSM Parameter Store** | Cấu hình, secret đơn giản (SecureString) | Cần xoay vòng tự động | Miễn phí/thấp (standard) | Giới hạn throughput/kích thước (kiểm tra) |
| **Cognito** | Đăng nhập người dùng (OIDC) không tự quản mật khẩu | Cần kiểm soát hoàn toàn (tự xây `identity-service` để học: `[Chọn để học]`) | U (theo MAU) | Tuỳ biến hạn chế; lock-in |
| **ACM** | Chứng chỉ TLS cho ALB/CloudFront | – | Miễn phí cho chứng chỉ công khai gắn dịch vụ AWS | Xác thực DNS |
| **GuardDuty / Security Hub / Config** | Phát hiện mối đe doạ, tư thế, tuân thủ | Ngân sách học rất thấp (cân nhắc bật có chọn lọc) | U (theo dữ liệu/rule) | Chi phí tăng theo khối lượng log/tài nguyên |

## 8. Quan sát và nhật ký

| Dịch vụ | Dùng khi | Tránh khi | Dạng chi phí | Bẫy |
|---|---|---|---|---|
| **CloudWatch Logs/Metrics/Alarms** | Log, metric, cảnh báo mặc định cho mọi dịch vụ | – | U (ingest GB, lưu, metric tuỳ chỉnh) | **Đặt retention**; không log DEBUG ở prod; cardinality metric |
| **X-Ray / ADOT (OpenTelemetry)** | Trace phân tán | Tải thấp, đã có Jaeger local đủ | U (theo trace) | Sampling để kiểm soát chi phí |
| **CloudTrail** | Ghi API bất biến, audit/phát hiện | – | Free (sự kiện quản lý mặc định) + U (data events/trail bổ sung) | Data events tốn khi bật rộng |

## 9. Giao hàng (CI/CD, image)

| Dịch vụ | Dùng khi | Dạng chi phí | Bẫy |
|---|---|---|---|
| **ECR** | Lưu image cho ECS | U (GB-tháng, truyền) | Lifecycle policy; bật quét image |
| **GitHub Actions (ngoài AWS)** | CI/CD với OIDC → AWS | Ngoài hoá đơn AWS (theo gói GitHub) | Trust policy chặt (`terraform.md` mục 3) |

## 10. Giả thuyết khởi đầu cho TeamBoard (không ràng buộc, để minh hoạ cách đặt ứng viên)

Chỉ là điểm bắt đầu để skill đối chiếu với tải và ngân sách thật của từng service. Luôn đánh giá lại theo quy trình.

| Service | Ứng viên đáng xét (rẻ đủ dùng trước) | Ghi chú |
|---|---|---|
| `gateway` (YARP) | Fargate sau một **ALB dùng chung**; hoặc API Gateway nếu chọn serverless | ALB và Fargate là chi phí chạy rỗng |
| `identity-service` | Fargate + RDS PostgreSQL (tự xây `[Chọn để học]`) so với Cognito | Cognito rẻ hơn công, tự xây để học |
| `board-service` | Fargate + RDS **SQL Server** (đang dùng EF Core) so với PostgreSQL | SQL Server tốn giấy phép: quyết định có đáng không |
| `task-service` | Fargate + PostgreSQL (event store + read model); outbox → SNS | CQRS/ES `[Chọn để học]` |
| `notification-service` | SQS (từ SNS) + Lambda hoặc Fargate worker; DynamoDB (dedupe) | Tải thấp → Lambda/trả theo dùng có thể rẻ hơn Fargate luôn chạy |
| `activity-service` | SQS + Lambda + DynamoDB (serverless, không chạy rỗng) | Hợp mô hình trả theo dùng |
| Chung | Một ALB, một cụm ECS, SNS topic `teamboard-events`, CloudWatch retention rõ, Budgets | Dùng chung giảm chi phí cố định |

Các chi phí cố định lớn nhất khả dĩ ở dự án này: **RDS (đặc biệt SQL Server), ALB, NAT Gateway, Fargate luôn chạy**. Cân nhắc dùng PostgreSQL dùng chung nhiều DB logic ở dev, tắt môi trường ngoài giờ học, và VPC endpoint thay NAT khi hợp lý.
