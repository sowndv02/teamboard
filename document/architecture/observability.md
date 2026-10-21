# Observability và monitoring (local và AWS)

Mục tiêu: biết hệ thống **đang làm gì và vì sao hỏng** qua log, metric, trace, health. Học cách làm trên **.NET** và triển khai trên **AWS** (có chi phí). Quy tắc ngắn nằm ở `.agents/rules/observability.md`; đây là tài liệu kiến trúc đầy đủ. Khả năng chịu lỗi: `resilience.md`.

**Nguồn:** slide "Microservices Observability, Resilience, Monitoring on .NET" (xem `references.md`; slide gồm: Distributed Logging với Elastic Stack + Serilog, Resilience với Polly, Health Checks với WatchDog, Tracing với OpenTelemetry + Zipkin) và slide .NET 8 (OpenTelemetry/health checks tích hợp). Phần AWS là kiến thức tổng hợp; **kiểm tra tài liệu và trang giá AWS hiện hành** trước khi dựa vào.

## 1. Ba trụ cột và health

| Tín hiệu | Trả lời | Công cụ trong slide | Hướng cho TeamBoard |
|---|---|---|---|
| **Log** (có cấu trúc) | Chuyện gì đã xảy ra, chi tiết | Serilog → Elasticsearch/Kibana | `ILogger` + OpenTelemetry logs, JSON có `traceId`/`correlationId` |
| **Metric** | Mức độ, xu hướng, cảnh báo | (OpenTelemetry metrics) | OpenTelemetry metrics (RED/USE) |
| **Trace** | Request đi qua những service nào, chậm ở đâu | OpenTelemetry → Zipkin | OpenTelemetry traces (W3C `traceparent`) |
| **Health** | Có sống/sẵn sàng không | ASP.NET Health Checks + WatchDog UI | `/health/live`, `/health/ready` (`observability.md` rule) |

Chuẩn tích hợp: **OpenTelemetry** (OTLP) làm lớp trung lập nhà cung cấp, để đổi nơi đến (Jaeger, X-Ray, CloudWatch...) mà không sửa code ứng dụng.

## 2. Log có cấu trúc

- **JSON một dòng mỗi sự kiện**, trường nhất quán: `timestamp`, `level`, `message` (template), `service`, `env`, `traceId`, `spanId`, `correlationId`, và các trường nghiệp vụ (`boardId`...). Không nối chuỗi; dùng message template (`LoggerMessage` source-generated, `backend-engineering.md` mục 3).
- **Mức log:** `Information` sự kiện nghiệp vụ, `Warning` phục hồi được, `Error` cần người xem. Không log DEBUG ở môi trường thật khi không cần (chi phí).
- **Không log** secret, token, mật khẩu, PII đầy đủ. Dùng che dữ liệu (redaction) cho trường nhạy cảm (.NET 8 có hỗ trợ redaction trong `Microsoft.Extensions.Compliance.Redaction`, kiểm tra hiện hành).
- **Correlation:** nhận `X-Correlation-Id` hoặc tạo ở gateway, đưa vào mọi log/call/message; `traceId` từ OpenTelemetry nối log với trace.
- **Lựa chọn thư viện log** (chưa chốt):
  | Lựa chọn | Ưu | Nhược |
  |---|---|---|
  | `ILogger` + OpenTelemetry logging (đề xuất) | Chuẩn của .NET, một pipeline với trace/metric, đổi đích dễ | Cần cấu hình thêm cho enrichment |
  | Serilog (như slide) | Hệ sinh thái sink/enricher phong phú, phổ biến thực tế | Thêm một thư viện, có thể trùng chức năng với OTel |
  Cả hai đều chấp nhận được; ghi quyết định bằng ADR khi chốt.

## 3. Metric và SLO

- **RED** cho dịch vụ (Rate, Errors, Duration) và **USE** cho tài nguyên (Utilization, Saturation, Errors).
- Metric nghiệp vụ tối thiểu: số request/giây, tỷ lệ lỗi, p50/p95/p99 độ trễ, **độ dài hàng đợi SQS và tuổi message cũ nhất**, số message vào DLQ, số lần retry, trạng thái circuit breaker, kết nối DB.
- **SLI/SLO** đơn giản cho mỗi service (ví dụ 99% request thành công, p95 < X ms); cảnh báo theo **tốc độ đốt ngân sách lỗi** thay vì ngưỡng thô khi đã quen.
- **Cardinality:** không đặt `userId`/`taskId` làm nhãn metric (phình chi phí); đưa chúng vào log/trace.

## 4. Trace phân tán

- OpenTelemetry tự đo ASP.NET Core, `HttpClient`, EF Core/SQL, và có thể thêm AWS SDK; truyền `traceparent` qua HTTP và **qua message SQS/SNS** (tự đưa vào message attributes, vì không tự lan truyền qua hàng đợi).
- **Sampling:** lấy mẫu theo tỷ lệ để kiểm soát chi phí; luôn giữ trace lỗi/chậm nếu công cụ hỗ trợ (tail sampling ở collector).
- Quy tắc tên span/metric: `<service>.<operation>` (`observability.md` rule).

## 5. Health checks

- **Liveness** (`/health/live`): chỉ kiểm tiến trình còn sống, **không** kiểm phụ thuộc (tránh khởi động lại vô ích khi DB chậm).
- **Readiness** (`/health/ready`): kiểm phụ thuộc cần thiết (DB, hàng đợi); dùng để ALB/ECS quyết định có nhận lưu lượng. Không để lộ chi tiết nội bộ ra ngoài.
- ASP.NET Core có sẵn `Microsoft.Extensions.Diagnostics.HealthChecks` (phụ thuộc DB/AWS dùng gói cộng đồng hoặc viết check riêng). Giao diện tổng hợp như WatchDog của slide **không cần** khi đã có Grafana/CloudWatch.
- Health checks gắn với ALB target group, ECS (container health check) và đặt ngưỡng hợp lý (`resilience.md` mục AWS).

## 6. Stack chạy local (docker-compose)

| Thành phần | Vai trò | Lựa chọn |
|---|---|---|
| **OpenTelemetry Collector** | Nhận OTLP, xử lý, chuyển tiếp | Một collector chung (nhẹ) |
| Trace | Xem trace | **Jaeger** (đã nêu trong kiến trúc) hoặc Zipkin (như slide) hoặc Grafana Tempo |
| Metric | Lưu và xem | **Prometheus + Grafana** |
| Log | Lưu và tìm | **Seq** (đơn giản nhất cho .NET), hoặc **Loki + Grafana**, hoặc Elastic Stack (như slide, nặng hơn) |

Chọn nhẹ nhất đủ dùng khi học: **Collector + Jaeger + Prometheus + Grafana + Seq/Loki**. Elastic Stack (Elasticsearch + Logstash + Kibana) như slide mạnh nhưng nặng bộ nhớ trên máy dev; chỉ chọn nếu muốn học riêng ELK. Ghi vào `compose.observability.yml` (`architecture/README.md`).

## 7. Trên AWS

### 7.1 Các dịch vụ và cách dùng

| Nhu cầu | Dịch vụ AWS | Ghi chú |
|---|---|---|
| **Log** | **CloudWatch Logs** | ECS Fargate gửi stdout qua driver `awslogs` (hoặc FireLens/Fluent Bit nếu cần định tuyến). **Đặt retention** cho mọi log group. Truy vấn bằng **Logs Insights**. Có lớp lưu log rẻ hơn cho truy cập hiếm (kiểm tra hiện hành) |
| **Metric hệ thống** | **CloudWatch Metrics**, **Container Insights** cho ECS | CPU/RAM/số tác vụ; dịch vụ SQS/ALB/RDS tự có metric |
| **Metric ứng dụng** | **Embedded Metric Format (EMF)** (log JSON đặc biệt tự sinh metric) hoặc OTLP qua collector | EMF thuận tiện cho .NET trên Lambda/ECS; chú ý chi phí metric tuỳ chỉnh và nhãn |
| **Trace** | **AWS X-Ray** nhận trace qua **ADOT** (AWS Distro for OpenTelemetry) collector chạy **sidecar** trong ECS task | Hướng hiện nay là **OpenTelemetry (ADOT)**; SDK X-Ray riêng đang chuyển sang chế độ bảo trì (kiểm tra thông báo hiện hành) |
| **Cảnh báo** | **CloudWatch Alarms** (có composite alarm) → **SNS** (email/Chatbot) | Cảnh báo theo SLO/ngưỡng, không cảnh báo mọi thứ |
| **Bảng điều khiển** | **CloudWatch Dashboards**; hoặc **Amazon Managed Grafana** + **Amazon Managed Service for Prometheus** | Managed Grafana/Prometheus có chi phí riêng (theo người dùng/lượng metric); với ngân sách học thấp, CloudWatch đủ |
| **Kiểm tra từ ngoài** | **CloudWatch Synthetics** (canary) | Mô phỏng người dùng gọi `/health` hoặc luồng chính; có chi phí theo lần chạy |
| **APM tổng hợp** | **CloudWatch Application Signals** | Mới hơn; kiểm tra tình trạng và chi phí hiện hành |
| **Audit API** | **CloudTrail** | Không phải quan sát ứng dụng; dành cho kiểm toán/bảo mật (`document/security/`) |
| **Phát hiện bất thường chi phí** | **AWS Budgets**, Cost Anomaly Detection | Phần của kiểm soát chi phí (`terraform.md` mục 4) |

### 7.2 Hai kiến trúc tham khảo (chọn theo ngân sách, cost-first)

**A. Gọn, rẻ (khuyến nghị cho dự án học):**
```
Service (OTel SDK) ──OTLP──> ADOT collector (sidecar) ──> X-Ray (trace)
        └─ stdout JSON ──awslogs──> CloudWatch Logs (retention) ── Logs Insights
        └─ metric (EMF/OTLP) ──> CloudWatch Metrics ── Alarms ──> SNS
```
Không thêm cụm lưu trữ riêng; chi phí chủ yếu theo lượng log/metric/trace.

**B. Giữ hệ sinh thái OSS (học Grafana/Prometheus):**
```
Service ──OTLP──> ADOT/OTel collector ──> Amazon Managed Prometheus (metric) ──> Amazon Managed Grafana
                                      └─> X-Ray hoặc Tempo (trace)
```
Giàu tính năng hơn nhưng **thêm chi phí cố định/theo người dùng**; chỉ chọn khi muốn học chính nhóm công cụ này và ngân sách cho phép.

**Không khuyến nghị cho học:** OpenSearch Service để chạy lại "ELK" như slide, vì chi phí chạy rỗng cao (`aws-service-catalog.md`).

### 7.3 Chi phí quan sát (cost-first)
- Nguồn chi phí chính: **log ingest (GB)**, **lưu giữ**, **metric tuỳ chỉnh**, **trace theo lượng**. Biện pháp: retention ngắn ở dev/stg, không log DEBUG, sampling trace, nhãn metric ít cardinality, lọc log ở nguồn.
- Quyết định lựa chọn A hay B nên đi qua `aws-service-selection` (ước tính chi phí theo thành phần).

### 7.4 Cảnh báo tối thiểu nên có
Tỷ lệ lỗi 5xx cao; p95 độ trễ vượt ngưỡng; độ sâu/tuổi hàng đợi SQS tăng và message vào DLQ; tác vụ ECS khởi động lại liên tục; RDS CPU/kết nối/dung lượng; chi phí vượt Budget. Mỗi cảnh báo có **runbook** (`document/runbooks/`).

## 8. Gỡ lỗi sự cố (liên kết với debug lab)
Quy trình ở `.agents/rules/observability.md`: tái hiện → lấy `traceId/correlationId` → theo trace tìm span lỗi/chậm → đọc log cùng `traceId` → nguyên nhân gốc → test tái hiện → runbook. Cờ `FAULT_*` để tạo lỗi có chủ đích (độ trễ, 500, mất message, poison message) và luyện đọc tín hiệu.

## 9. Lộ trình triển khai gợi ý
1. `src/shared/ServiceDefaults`: OpenTelemetry (trace, metric, log), health, correlation (giai đoạn 0, TB-001).
2. Compose observability local: collector + Jaeger + Prometheus + Grafana + Seq/Loki.
3. Metric/alarm nghiệp vụ và runbook cho luồng chính.
4. AWS: ADOT sidecar, log group có retention, alarms, dashboard; chọn A hoặc B theo chi phí.
5. Cảnh báo theo SLO; canary Synthetics nếu có ngân sách.

## 10. Việc cần chốt
- `ILogger` + OTel hay Serilog (ADR).
- Công cụ log/trace local: Seq/Loki, Jaeger/Tempo/Zipkin.
- AWS: kiến trúc A hay B; ngân sách quan sát (`quality.monthlyBudgetUsd` còn `TODO`).
