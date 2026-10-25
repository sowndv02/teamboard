---
title: API gateway
status: confirmed            # draft | confirmed | superseded
owner: sondv
updated: 2026-10-22
services: [gateway, board-service]
issues: [TB-007]
---

# API gateway

## 1. Yêu cầu gốc

> Checkout sang nhánh mới để làm api gateway trước khi làm các service bên trong

Nguồn: sondv, chat với Claude Code, 2026-10-22. Phạm vi gateway mỏng (routing, health, correlation, rate limit, CORS; kiểm JWT để sau) lấy từ quyết định thứ tự làm việc đã chốt cùng ngày: TB-001 → gateway → identity → gateway kiểm JWT.

## 2. Bối cảnh và mục tiêu

- Vấn đề cần giải: kiến trúc quy định client → gateway → service (`document/architecture/README.md` §2) nhưng chưa có gateway. Các service sau (identity, task...) cần một đường vào chung để không phải tự lo routing, correlation, giới hạn tải.
- Giá trị/kết quả mong muốn: một điểm vào duy nhất, chốt sớm hợp đồng routing và correlation để service sau cắm vào; kiểm JWT bổ sung sau khi có identity mà không đổi hợp đồng với client.
- Thước đo thành công: gọi `GET /api/v1/boards` qua gateway ra đúng response của `board-service`, trace xuyên gateway → service thấy trong Jaeger, vượt ngưỡng thì nhận `429`.

## 3. Người dùng và vai trò

| Vai trò | Mô tả | Mục đích sử dụng |
|---|---|---|
| Client (sau này là web Angular, hiện là curl/test) | Gọi API công khai | Chỉ biết một địa chỉ gateway, không biết địa chỉ service |
| Lập trình viên/vận hành (sondv) | Debug, theo dõi | Lấy `correlationId`/`traceId` từ response để tra log và trace |
| Service phía sau (board-service) | Nhận request đã được chuyển tiếp | Nhận `X-Correlation-Id` và `traceparent` |

## 4. Phạm vi

**Trong phạm vi (lần này):**
- Định tuyến `/api/v1/boards/**` tới `board-service`, giữ nguyên path.
- `/health/live`, `/health/ready` của gateway; OpenTelemetry (trace, metric, log).
- Tạo hoặc chuyển tiếp `X-Correlation-Id`, truyền `traceparent`.
- Rate limit theo IP, mức riêng cho từng route.
- CORS theo allowlist origin trong config.
- Timeout và lỗi `502/503/504` dạng `problem+json` khi service phía sau lỗi.
- Giới hạn kích thước body request (`413`).
- Chạy được trong docker compose; ghi vào `config/services.yaml`.

**Ngoài phạm vi:**
- Kiểm JWT, xác thực, phân quyền (làm sau khi có identity-service, giai đoạn gateway kiểm JWT).
- Rate limit theo user (cần JWT).
- Retry, circuit breaker (ngắt mạch tạm thời khi service lỗi liên tục): bài sau.
- Route tới service chưa tồn tại (identity, task...); mỗi service mới tự thêm route khi nó xuất hiện.
- Frontend (ADR 0008), Terraform/ALB.
- Gộp/biến đổi response (BFF), cache ở gateway.

## 5. Thuật ngữ

| Thuật ngữ | Nghĩa |
|---|---|
| Gateway | Cửa vào duy nhất: nhận request công khai rồi chuyển tới service phù hợp |
| YARP | Thư viện reverse proxy của Microsoft chạy trong ASP.NET Core, dùng làm gateway (quyết định ở `PROJECT.md`) |
| Rate limit | Giới hạn số request trong một khoảng thời gian cho mỗi "khoá" (ở đây là IP) |
| `correlationId` | Mã gắn vào một request để nối log/trace của nhiều service lại với nhau |
| `traceparent` | Header chuẩn W3C mang trace id/span id để trace xuyên service |
| CORS | Cơ chế trình duyệt chỉ cho trang ở origin được phép gọi API |

## 6. Yêu cầu chức năng

| ID | Mô tả | Ưu tiên (M/S/C/W) | Tiêu chí chấp nhận | Nguồn |
|---|---|---|---|---|
| FR-001 | Là client, tôi muốn gọi `/api/v1/boards/**` qua gateway để không cần biết địa chỉ `board-service`. | Must | Given `board-service` chạy, When `GET /api/v1/boards` qua gateway, Then nhận đúng status/body của service, path tới service không đổi. Given path không khớp route nào, When gọi, Then `404` `problem+json`. | Lời người dùng (cổng vào) + Q routing 2026-10-22 |
| FR-002 | Là vận hành, tôi muốn gateway có `/health/live` và `/health/ready` để orchestrator kiểm tra. | Must | Given gateway khởi động xong, When `GET /health/live` và `/health/ready`, Then `200`. Given `board-service` chết, When `GET /health/ready`, Then vẫn `200` (ready không phụ thuộc downstream). Health không bị rate limit. | `PROJECT.md` (mọi service) + Q lỗi phía sau |
| FR-003 | Là lập trình viên, tôi muốn mỗi request mang `X-Correlation-Id` và `traceparent` xuống service để tra xuyên service. | Must | Given request không có `X-Correlation-Id`, When qua gateway, Then gateway tạo mới, truyền xuống service và trả lại trong response. Given request có `X-Correlation-Id` hợp lệ (ký tự `A-Z a-z 0-9 - _ .`, tối đa 64 ký tự), Then giữ nguyên giá trị đó. Given `X-Correlation-Id` chứa ký tự khác hoặc dài 65 ký tự, Then gateway bỏ giá trị đó và tạo mới. Given có `traceparent`, Then span của gateway nối vào cùng trace. | `PROJECT.md`, `api-endpoints.md` |
| FR-004 | Là vận hành, tôi muốn giới hạn số request theo IP, mức riêng từng route, để chặn spam và quá tải. | Must | Ngưỡng khởi điểm trên `/api/v1/boards/**` (cấu hình được): đọc (`GET`) 120/phút/IP, ghi (`POST/PUT/PATCH/DELETE`) 30/phút/IP. Given một IP đã gửi 30 request ghi trong phút, When gửi request ghi thứ 31, Then `429` `problem+json` kèm `Retry-After`. Given cùng IP đó, When `GET` lần thứ 121 trong phút, Then `429`; `GET` thứ 120 vẫn thành công. Given hai địa chỉ IPv6 cùng dải /64, Then dùng chung một bộ đếm (IPv4 tính theo địa chỉ đầy đủ). Given IP khác, Then không bị ảnh hưởng. | Lời người dùng + Q rate limit 2026-10-22 |
| FR-005 | Là web client (sau này), tôi muốn trình duyệt từ origin được phép gọi gateway mà không bị chặn CORS. | Should | Given origin nằm trong allowlist, When preflight `OPTIONS`, Then trả header CORS cho origin đó. Given origin ngoài allowlist, Then không có header cho phép CORS. | Q CORS 2026-10-22 |
| FR-007 | Là vận hành, tôi muốn gateway từ chối body quá lớn để chặn request khổng lồ sớm. | Should | Giới hạn 1 MiB = 1.048.576 byte (cấu hình được). Given body đúng 1.048.576 byte, When gửi, Then chuyển tiếp bình thường. Given `Content-Length` 1.048.577 byte, When gửi, Then `413` `problem+json`, không chuyển xuống service. Given body chunked (không có `Content-Length`) vượt giới hạn, Then gateway ngắt luồng và trả lỗi; service có thể đã nhận một phần body (đã chấp nhận ở design-review). | Q-03 đã chốt 2026-10-22; làm rõ ở design-review 2026-10-23 |
| FR-006 | Là client, tôi muốn lỗi từ phía sau được báo rõ để biết là gateway hay service lỗi. | Must | Given service phía sau không có đích khả dụng hoặc không kết nối được, When gọi, Then `503` `problem+json`. Given service trả lỗi giao thức hoặc ngắt kết nối trước khi trả header, Then `502`. Given service không trả header trong thời gian timeout (tổng 30 giây, NFR-003) hoặc không kết nối được trong thời gian timeout kết nối (mặc định 15 giây), Then `504`. Given service đã trả header rồi mới ngắt hoặc treo, Then gateway ngắt kết nối với client và ghi log có `correlationId` và tăng metric đếm (metric không mang `correlationId` để tránh cardinality cao; không đổi được status đã gửi). Mọi lỗi do gateway có `code`, `traceId`, `correlationId`, không lộ host nội bộ hay stack trace. | Q lỗi phía sau 2026-10-22, `api-endpoints.md` §2, §5 |

Ghi chú ưu tiên: 5 trên 7 FR là Must vì FR-001 là lý do tồn tại của gateway, FR-002 và FR-003 là yêu cầu bắt buộc mọi service (`PROJECT.md`), FR-004 do người dùng nêu trực tiếp, FR-006 là điều kiện để debug được khi service phía sau lỗi.

## 7. Quy tắc nghiệp vụ

| ID | Quy tắc | Ví dụ | Nguồn |
|---|---|---|---|
| BR-001 | Gateway định tuyến theo tài nguyên, không có tên service trong URL công khai. | `/api/v1/boards/...` → `board-service`, không phải `/board-service/...` | `api-endpoints.md` §1 |
| BR-002 | Gateway không chứa nghiệp vụ và không truy cập database. | Không có DB trong `config/services.yaml` cho gateway | `PROJECT.md`, `architecture.md` |
| BR-003 | Health endpoint nằm ngoài versioning và ngoài rate limit. | `/health/live` không có `/api/v1` | `api-endpoints.md` §1 |

## 8. Luồng nghiệp vụ

### 8.1 Luồng chính
1. Client gửi `GET /api/v1/boards`.
2. Gateway kiểm CORS nếu có `Origin` (preflight trả luôn, không tính rate limit), rồi kiểm rate limit theo IP và route.
3. Gateway gán hoặc giữ `X-Correlation-Id`, mở span, gắn `traceparent`.
4. Gateway chuyển request tới `board-service` giữ nguyên path.
5. Gateway trả response của service về client kèm `X-Correlation-Id`.

### 8.2 Luồng thay thế
- Preflight `OPTIONS` từ origin trong allowlist: gateway trả header CORS, không chuyển xuống service.
- Client gửi `X-Correlation-Id` hợp lệ: giữ nguyên.

### 8.3 Luồng lỗi và trường hợp biên
- Vượt rate limit: `429` + `Retry-After`, không chuyển xuống service.
- Không khớp route: `404` `problem+json`.
- Service phía sau không kết nối được: `503`; quá timeout: `504`.
- `X-Correlation-Id` sai định dạng hoặc quá dài: gateway tạo mới (FR-003).
- Body vượt 1 MiB: `413` (FR-007).
- Origin ngoài allowlist: response không có header CORS (trình duyệt chặn).

## 9. Dữ liệu

Không áp dụng: gateway không lưu dữ liệu nghiệp vụ. Trạng thái rate limit nằm trong bộ nhớ hoặc kho chung do design quyết định (xem mục 16).

## 10. Phân quyền và bảo mật

| Hành động | Ai được phép | Ghi chú (dữ liệu nhạy cảm, audit) |
|---|---|---|
| Gọi `/api/v1/boards/**` qua gateway | Mọi client (lần này chưa kiểm JWT) | Tạm thời không xác thực ở gateway; `board-service` hiện cũng chưa xác thực, không đưa ra internet thật trước khi có JWT |
| Gọi `/health/*` | Mọi nơi trong mạng nội bộ | Không lộ chi tiết nội bộ trong body |

Ranh giới tin cậy mới (internet → gateway) cần STRIDE nhẹ trong design (giả mạo `X-Forwarded-For`, giả mạo `X-Correlation-Id`, DoS).

## 11. Tích hợp và sự kiện

| Với service/hệ thống | Cách giao tiếp (REST/sự kiện) | Dữ liệu/sự kiện |
|---|---|---|
| `board-service` | REST, HTTP | Request/response `/api/v1/boards/**`, header `X-Correlation-Id`, `traceparent` |
| Jaeger / OTLP collector | OpenTelemetry | Trace, metric, log của gateway |
| `src/shared/ServiceDefaults` (TB-001) | Thư viện dùng chung | OTel, health |

Không phát/nhận sự kiện (SNS/SQS).

## 12. Yêu cầu phi chức năng

| ID | Nội dung (kèm số đo) |
|---|---|
| NFR-001 | Độ trễ gateway thêm vào mỗi request: p95 < 20 ms ở tải dev (giả định A-02, chưa đo; đo ở bước test/vận hành). |
| NFR-002 | Mọi request qua gateway có trace nối được với span của service phía sau (kiểm bằng Jaeger). |
| NFR-003 | Timeout tới service phía sau có giá trị cấu hình được, mặc định tổng 30 giây (`Timeout` theo route) và idle tối đa 30 giây (`ActivityTimeout`) (giả định A-03). |
| NFR-004 | Ngưỡng rate limit, origin CORS, địa chỉ service nằm trong cấu hình, không hardcode, không chứa secret. |
| NFR-005 | Gateway khởi động và `ready` trong < 10 giây ở máy dev (giả định A-06). |
| NFR-006 | Tuân thủ GDPR (đã chốt 2026-10-22; Q-04 chọn B): IP client là dữ liệu cá nhân và được ghi nguyên văn trong log/trace của gateway; thời hạn lưu log tối đa 7 ngày ở dev (cấu hình ở tầng lưu log, kiểm khi có hạ tầng giai đoạn 4; ở local chỉ ghi nhận), truy cập log hạn chế cho chủ dự án. Không bao giờ log token, header `Authorization`, cookie hay body. Kiểm chứng: Given một request có `Authorization` và body, When gateway ghi log, Then log có IP và `correlationId` nhưng không chứa giá trị `Authorization` hay nội dung body. |

## 13. Giao diện (chưa áp dụng)

Giai đoạn hiện tại không phát triển frontend (ADR 0008). CORS (FR-005) chuẩn bị cho web Angular của ADR 0009, nhưng không có giao diện trong lát này.

## 14. Giả định

| # | Giả định | Người xác nhận | Trạng thái |
|---|---|---|---|
| A-01 | `board-service` sẽ phục vụ `/api/v1/boards/**` (hiện mới có `WeatherForecastController`, chưa đổi tên). Test của gateway dùng **upstream giả (stub) chạy trong test**, không phụ thuộc board-service; sau khi `board-service` có endpoint thì thêm một lần smoke với service thật (ngoài lát này nếu TB-001 chưa xong). | sondv | đã xác nhận 2026-10-22 (chọn stub) |
| A-02 | Mục tiêu độ trễ thêm vào của gateway p95 < 20 ms là đủ cho dự án học. | sondv | đã xác nhận 2026-10-22 |
| A-03 | Timeout mặc định tới service phía sau là 30 giây. | sondv | đã xác nhận 2026-10-22 |
| A-04 | Gateway chạy một instance ở local; chạy nhiều instance chỉ tính từ giai đoạn Terraform/ECS (ảnh hưởng nơi lưu bộ đếm rate limit). | sondv | đã xác nhận 2026-10-22 |
| A-05 | Gateway đứng trực tiếp trước client ở local; khi có ALB (giai đoạn 4) IP client lấy từ `X-Forwarded-For` của proxy tin cậy. | sondv | đã xác nhận 2026-10-22 |
| A-06 | Gateway sẵn sàng (`ready`) trong dưới 10 giây ở máy dev (NFR-005). Mặc định nếu sai: nới ngưỡng, không chặn việc chạy. | sondv | đã xác nhận 2026-10-22 |

## 15. Câu hỏi mở

| # | Câu hỏi | Ảnh hưởng tới | Người trả lời | Hạn | Trạng thái (`open`/`answered`/`deferred`) |
|---|---|---|---|---|---|
| Q-01 | Ngưỡng rate limit cụ thể từng route là bao nhiêu? | FR-004 | sondv | trước design-review | answered (FR-004) |
| Q-02 | Gateway có tin `X-Correlation-Id` do client gửi không? | FR-003 | sondv | trước design-review | answered (FR-003) |
| Q-03 | Giới hạn kích thước body request ở gateway? | FR-007 | sondv | trước plan | answered (FR-007) |
| Q-04 | Che IP và thời hạn lưu log theo GDPR? | NFR-006 | sondv | trước design-review | answered (NFR-006) |

### Q-01 Ngưỡng rate limit từng route
- **Lựa chọn:** A) mức mặc định chung 100 req/phút/IP cho `/api/v1/**`, ngưỡng riêng khi có route nhạy cảm (login) sau này; B) đặt số riêng cho từng route ngay (ví dụ đọc 120/phút, ghi 30/phút); C) để giá trị trong config và chỉ chốt khi đo.
- **Khuyến nghị:** B với giá trị khởi điểm trong config (đọc 120/phút, ghi 30/phút), vì bạn chọn "mức riêng cho từng route" và hiện route duy nhất là boards có cả đọc lẫn ghi; chỉnh được không cần sửa code. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** test của FR-004 và cấu hình mặc định phải đoán số.
- **Mặc định nếu quá hạn:** B với số nêu trên (không phải `blocker`).

### Q-02 Tin `X-Correlation-Id` từ client
- **Lựa chọn:** A) giữ nếu đúng định dạng (chuỗi an toàn, tối đa 64 ký tự), thiếu hoặc sai thì tạo mới; B) luôn tạo mới và lưu id client gửi ở trường khác; C) giữ nguyên mọi giá trị.
- **Khuyến nghị:** A, vì `api-endpoints.md` §6 quy định "nhận từ client hoặc gateway tạo nếu thiếu", còn kiểm định dạng chặn log injection và id quá dài. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** kịch bản test cho id sai định dạng chưa viết được; có rủi ro giả mạo/log injection ở design.
- **Mặc định nếu quá hạn:** A.

### Q-03 Giới hạn kích thước body
- **Lựa chọn:** A) 1 MB mặc định cấu hình được; B) không giới hạn ở gateway, để service tự lo; C) 10 MB.
- **Khuyến nghị:** A, vì API chỉ JSON nhỏ (board/task) và giới hạn ở cửa vào chặn request khổng lồ sớm. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** thiếu một cấu hình bảo vệ, không chặn việc chạy.
- **Mặc định nếu quá hạn:** A.

### Q-04 Che IP và thời hạn lưu log (GDPR)
- **Lựa chọn:** A) che octet cuối IPv4 (`203.0.113.x`; IPv6 giữ /48) trong log và trace, bộ đếm rate limit chỉ giữ trong bộ nhớ, thời hạn lưu log theo môi trường (dev 7 ngày); B) ghi IP nguyên văn nhưng hạn lưu ngắn (dev 7 ngày) và hạn chế truy cập; C) không ghi IP vào log/trace, chỉ dùng trong bộ nhớ cho rate limit.
- **Khuyến nghị:** A, vì vẫn tra được theo vùng/nguồn khi debug nhưng không lưu địa chỉ đầy đủ; C mất khả năng điều tra lạm dụng, B giữ dữ liệu cá nhân đầy đủ. Chỉ để tham khảo.
- **Hậu quả nếu chưa chốt:** NFR-006 không kiểm chứng được; design và test log chưa viết được.
- **Mặc định nếu quá hạn:** không có (chặn PASS vì liên quan bảo mật/tuân thủ).
- **Quyết định (2026-10-22):** B, ghi IP nguyên văn, hạn lưu ngắn (dev 7 ngày), hạn chế truy cập.

## 16. Rủi ro và quyết định cần ADR

| Mục | Mô tả | Hướng xử lý |
|---|---|---|
| Phụ thuộc TB-001 | Gateway dùng `ServiceDefaults` và tên `board-service`, hiện chưa có | Chốt thứ tự ở design/plan: làm TB-001 trước hoặc tách phần dùng chung tối thiểu |
| Quyết định cần ADR | Nơi lưu bộ đếm rate limit (bộ nhớ vs kho chung) khi chạy nhiều instance | Design chọn, ghi ADR nếu khó đảo ngược |
| Quyết định cần ADR | Lý do chọn YARP (D0 ranh giới/kiến trúc nội bộ); ADR hiện chỉ ghi kết quả | Design làm D0, đề xuất bổ sung ADR |
| Rủi ro | Ghi IP nguyên văn là dữ liệu cá nhân theo GDPR (Q-04 chọn B): cần giới hạn truy cập log, thời hạn lưu 7 ngày, và mục đích xử lý chỉ để vận hành/chống lạm dụng | Design nêu cách cấu hình lưu log; kiểm khi có hạ tầng (giai đoạn 4) |
| Rủi ro | Gateway không xác thực lúc này: không đưa ra internet trước khi có kiểm JWT | Ghi rõ trong runbook/README; không deploy công khai |
| Rủi ro | Hành vi khi gateway tắt dần (graceful shutdown) chưa quy định | Design làm rõ; không có FR trong lát này |
| Rủi ro | Gateway là điểm lỗi đơn của mọi request | Ở local chấp nhận; HA (nhiều instance sau ALB) tính ở giai đoạn 4 |
| Blast radius | Đổi `config/services.yaml` (cổng gateway), compose, `PROJECT.md` | Cập nhật cùng thay đổi |

## 17. Truy vết

| Yêu cầu | Issue | Kịch bản (`.feature`) | ADR |
|---|---|---|---|
| FR-001..FR-007 | TB-007 | chưa có | chưa có |

## 18. Lịch sử thay đổi

| Ngày | Thay đổi | Nguồn |
|---|---|---|
| 2026-10-22 | Tạo bản nháp; chốt rate limit theo IP mức riêng từng route, routing `/api/v1/boards/**` giữ nguyên path, CORS allowlist mặc định `http://localhost:4200`, lỗi phía sau `502/503/504` và `ready` không phụ thuộc downstream | Trả lời của sondv qua AskUserQuestion |
| 2026-10-22 | Spec-review vòng 1-2: áp dụng Q-01..Q-03 (số rate limit, định dạng correlation id, giới hạn body, thêm FR-007), làm rõ 502/503/504, A-01 chọn stub upstream, thêm A-06 và NFR-006 (GDPR, Q-04 chọn B), compliance = GDPR; spec `confirmed` | Spec-review, sondv |
| 2026-10-23 | Làm rõ sau design-review: FR-004 (IPv6 gộp /64), FR-006 (502/504 chỉ trước khi gửi header), FR-007 (1 MiB, body chunked), NFR-003 (tổng 30 giây + idle), thứ tự CORS trước rate limit ở 8.1 | sondv chọn ở design-review TB-007 |
| 2026-10-25 | Làm rõ sau plan-review TB-023: FR-006 timeout kết nối là `504`; log có `correlationId`, metric chỉ đếm | sondv chọn ở plan-review TB-023 |

## 19. Confirm log

| # | Điểm cần chốt (fingerprint) | Câu hỏi | Quyết định của người dùng | Vòng | Ngày |
|---|---|---|---|---|---|
| 1 | spec-review:api-gateway:spec-accept | Xác nhận spec draft, Q-01..Q-03 và A-01..A-05 | Xác nhận spec; chấp nhận mặc định khuyến nghị cho Q-01..Q-03 và A-01..A-05 | 1 | 2026-10-22 |
| 2 | spec-review:api-gateway:compliance-todo | `quality.compliance` đang TODO; gateway ghi IP client | GDPR (`config/project.yaml` đã cập nhật) | 1 | 2026-10-22 |
| 3 | spec-review:api-gateway:a01-upstream-for-test | Cách kiểm chứng FR-001 khi board-service chưa có `/api/v1/boards` | Stub upstream trong test, không cần board-service | 1 | 2026-10-22 |
| 4 | spec-review:api-gateway:nfr005-no-source | NFR-005 do agent tự thêm, giữ hay bỏ | Giữ, thêm vào giả định A-06 | 1 | 2026-10-22 |
| 5 | spec-review:api-gateway:q04-ip-logging | Che IP và thời hạn lưu log theo GDPR | B: ghi IP nguyên văn, hạn lưu ngắn (dev 7 ngày), hạn chế truy cập | 2 | 2026-10-22 |

## 20. Review rounds

| Vòng | Ngày | Phạm vi | CONFIRM mở | FIX mở | Verdict |
|---|---|---|---|---|---|
| 1 | 2026-10-22 | Toàn bộ checklist A–G | 3 (compliance, A-01, NFR-005) | 0 sau khi sửa 8 FIX | NEEDS_CONFIRM |
| 2 | 2026-10-22 | Delta (GDPR → NFR-006, Q-04, A-01, A-06) | 1 (Q-04) → đã chốt | 0 | NEEDS_CONFIRM → PASS sau khi chốt Q-04 |

FIX vòng 1 đã sửa: gắn số vào FR-004 (Q-01), định dạng correlation id (Q-02), thêm FR-007 (Q-03), làm rõ 502/503/504 ở FR-006, bỏ dòng graceful shutdown khỏi luồng lỗi sang Rủi ro, giải thích tỉ lệ Must, đóng trạng thái Q-01..Q-03 và A-01..A-05.
