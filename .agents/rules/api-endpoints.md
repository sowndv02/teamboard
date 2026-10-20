# API endpoint rules

Lý do tồn tại: các service dùng kiến trúc nội bộ khác nhau, nhưng **bề mặt HTTP phải đồng nhất** để Angular, gateway và service khác dùng chung một cách hiểu. Áp dụng cho mọi endpoint của mọi service. Đổi quy ước ở đây = ADR mới.

Kết hợp với: `architecture.md` (ranh giới service), `dotnet.md`, `testing.md`, `code-review.md` (probe B2).

## 1. URL & versioning

- Dạng: `/api/v{major}/{resource}[/{id}[/{subresource}...]]`, ví dụ `/api/v1/boards/{boardId}/tasks`.
- Tài nguyên là **danh từ số nhiều**, `kebab-case`, chữ thường (`/task-comments`). Không động từ trong path (`/createTask` ✗).
- Hành động không phải CRUD: dùng sub-resource kiểu hành động, `POST /api/v1/tasks/{id}/assign`, `POST .../archive`.
- **Không có tên service trong path.** Gateway định tuyến theo tài nguyên; service sở hữu tài nguyên nào thì phục vụ path đó giống hệt nhau ở local, gateway và AWS.
- Path lồng nhau do service sở hữu **tài nguyên lá** (cuối path) xử lý (`/boards/{id}/tasks` → task-service). Tối đa 2 cấp lồng; sâu hơn thì dùng query hoặc id trực tiếp.
- `{id}`: GUID dạng chuỗi (hoặc kiểu id đã chốt trong spec của service); không lộ id tự tăng.
- Version ở path, chỉ tăng major khi **breaking change**. Thay đổi tương thích (thêm field tuỳ chọn) không tăng version. Breaking → giữ bản cũ song song, đánh dấu deprecated, ghi ADR.
- Health và metrics **ngoài** versioning: `/health/live`, `/health/ready`.

## 2. Method & status code

| Thao tác | Method | Thành công | Ghi chú |
|---|---|---|---|
| Lấy danh sách | `GET /resources` | `200` | Có phân trang (mục 4) |
| Lấy một | `GET /resources/{id}` | `200` | |
| Tạo | `POST /resources` | `201` + `Location` + body tài nguyên | |
| Thay thế/cập nhật toàn phần | `PUT /resources/{id}` | `200` (body) hoặc `204` | Idempotent |
| Cập nhật một phần | `PATCH /resources/{id}` | `200` hoặc `204` | JSON Merge Patch; chỉ field được gửi mới đổi |
| Xoá | `DELETE /resources/{id}` | `204` | Idempotent: xoá lại vẫn `204` hoặc `404`, ghi rõ trong spec |
| Hành động bất đồng bộ | `POST ...` | `202` + `Location` tới trạng thái | Command chỉ nhận, xử lý sau |

Mã lỗi: `400` request sai cú pháp/validation, `401` chưa xác thực, `403` không đủ quyền, `404` không có, `409` xung đột (version, trùng), `412` ETag không khớp, `422` hợp lệ cú pháp nhưng vi phạm quy tắc nghiệp vụ, `429` quá giới hạn (kèm `Retry-After`), `500` lỗi không lường trước, `502/503/504` do phụ thuộc/timeout.

- `GET` không đổi trạng thái, `POST` không idempotent mặc định. Không dùng `200` kèm `{ "success": false }`.
- `404` thay `403` khi lộ sự tồn tại của tài nguyên cho người không có quyền là rủi ro.

## 3. Dữ liệu (JSON)

- `Content-Type: application/json; charset=utf-8`. Field **camelCase**; enum là **chuỗi** (`"inProgress"`), không dùng số.
- Thời gian **ISO 8601 UTC** có hậu tố `Z` (`2026-10-20T08:30:00Z`); chỉ ngày thì `YYYY-MM-DD`. Server lưu và trả UTC.
- Không trả `null` cho tập hợp: trả `[]`. Field tuỳ chọn không có giá trị thì bỏ hẳn hoặc `null` theo một lựa chọn nhất quán trong toàn bộ service (mặc định: bỏ field).
- Tiền/số chính xác: chuỗi hoặc số nguyên đơn vị nhỏ nhất, không `float`.
- Response là **DTO riêng**, không trả entity/EF model/event ra ngoài. Không lộ field nội bộ (hash, flag nội bộ).
- Thành công: trả tài nguyên trực tiếp, **không** bọc `{ data: ... }` (trừ danh sách ở mục 4).
- Request body lạ field → `400` (strict) hay bỏ qua: chọn một cách, mặc định **bỏ qua field thừa** nhưng validate field bắt buộc.

## 4. Danh sách: phân trang, lọc, sắp xếp

Response danh sách thống nhất:

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 134,
  "totalPages": 7
}
```

- Query: `?page=1&pageSize=20` (mặc định `page=1`, `pageSize=20`, tối đa `100`; vượt tối đa thì kẹp xuống tối đa hoặc `400`, ghi trong spec). Nguồn dữ liệu lớn/ổn định thứ tự dùng **cursor** (`?cursor=&limit=`, trả `nextCursor`); cần ghi rõ trong spec của service.
- Lọc: query riêng từng field (`?status=open&assigneeId=...`); nhiều giá trị phân tách bằng dấu phẩy.
- Sắp xếp: `?sort=createdAt` (tăng), `?sort=-createdAt` (giảm); nhiều trường phân tách bằng dấu phẩy. Chỉ cho sort trên field có index; field lạ → `400`.
- Phải có thứ tự ổn định mặc định (thêm khoá phụ như `id`) để phân trang không lặp/sót.

## 5. Lỗi: `application/problem+json` (RFC 9457)

Mọi lỗi của mọi service dùng cùng một dạng:

```json
{
  "type": "https://teamboard/errors/validation",
  "title": "Dữ liệu không hợp lệ",
  "status": 400,
  "detail": "Một hoặc nhiều trường không hợp lệ.",
  "code": "VALIDATION_FAILED",
  "traceId": "00-4bf92f35...-01",
  "correlationId": "…",
  "errors": [ { "field": "title", "code": "REQUIRED", "message": "Tiêu đề là bắt buộc." } ]
}
```

- `code`: mã **ổn định, UPPER_SNAKE** để client xử lý (Angular map sang thông báo); không đổi tuỳ tiện. `message`/`detail` cho người đọc, có thể đổi.
- Luôn có `traceId` và `correlationId`. **Không** lộ stack trace, SQL, đường dẫn, tên host nội bộ hay chi tiết framework.
- Validation: `400` kèm `errors[]` theo từng field. Vi phạm quy tắc nghiệp vụ: `422` hoặc `409` kèm `code` riêng.
- Lỗi chưa lường: `500` với `code: "INTERNAL_ERROR"`, chi tiết chỉ ở log (có `traceId` để tra).
- Một middleware/handler chung (trong `src/shared`, phần cross-cutting) dựng lỗi; service không tự chế định dạng.

## 6. Header chung

| Header | Vai trò |
|---|---|
| `Authorization: Bearer <JWT>` | Xác thực; gateway kiểm tra, service tin cậy token đã xác thực nhưng vẫn **tự kiểm tra quyền** trên tài nguyên của mình |
| `X-Correlation-Id` | Nhận từ client hoặc gateway tạo nếu thiếu; trả lại trong response và truyền xuống mọi call/message |
| `traceparent` | W3C trace context, truyền theo OpenTelemetry |
| `Idempotency-Key` | Với `POST` có thể bị retry (tạo, thanh toán…): cùng key + cùng body → cùng kết quả, không tạo trùng; key khác body → `422` |
| `ETag` / `If-Match` | Chống ghi đè đồng thời: `GET` trả `ETag`, `PUT/PATCH/DELETE` gửi `If-Match`; không khớp → `412` (hoặc `409` nếu dùng trường `version` trong body, chọn một cách theo service và ghi trong spec) |
| `Retry-After` | Kèm `429`/`503` |

## 7. Bảo mật

- Mọi endpoint (trừ health và login/đăng ký) yêu cầu xác thực. **Mặc định đóng**: endpoint mới phải khai báo rõ chính sách quyền (`[Authorize(Policy=...)]`).
- Kiểm tra quyền trên **từng tài nguyên** (chống IDOR), không chỉ theo vai trò: người dùng chỉ truy cập board/task mình có quyền.
- Validate đầu vào ở biên (độ dài, định dạng, phạm vi); giới hạn kích thước body.
- Không đưa secret/PII vào URL/query (log URL sẽ lộ). Không log body chứa dữ liệu nhạy cảm.
- CORS cấu hình **ở gateway**, không ở từng service; không `*` khi có credential.
- Rate limit ở gateway; trả `429` đúng chuẩn.

## 8. Tài liệu & contract

- Mỗi service xuất **OpenAPI** (đã có Swagger UI ở dự án); mỗi endpoint có summary, mô tả tham số, mọi response (kể cả lỗi) và ví dụ. Chỉ bật Swagger UI ở môi trường không phải prod.
- Dùng `ProducesResponseType`/`TypedResults` để schema khớp thực tế; không để OpenAPI lệch code.
- Contract (request/response/error/phân trang) mô tả trong `document/spec/<service>.md` hoặc `document/contracts/`. **Đổi contract = cập nhật spec + test contract + consumer (Angular, service khác) cùng thay đổi.**
- Breaking change: tăng version, giữ bản cũ song song, ADR, và báo consumer.

## 9. Triển khai trong code (.NET)

- Dùng Minimal API (hoặc Controller) **nhất quán trong từng service**; đặt route trong lớp/nhóm theo tài nguyên (`MapGroup("/api/v1/boards")`).
- Endpoint **mỏng**: nhận request → validate → gọi use case/handler → map kết quả sang DTO. Không logic nghiệp vụ, không truy cập DB trực tiếp trong endpoint (cách tách phụ thuộc kiến trúc từng service, xem `architecture.md`).
- DTO request/response riêng cho từng endpoint; không dùng chung giữa các tài nguyên khi hình dạng khác nhau.
- `CancellationToken` truyền xuống mọi lời gọi I/O. Handler lỗi, `problem+json`, correlation, phân trang dùng thành phần chung từ `src/shared` (không copy giữa service).
- Tên endpoint/operationId rõ nghĩa (`GetBoardById`, `CreateTask`) để sinh client và đọc OpenAPI.

## 10. Test bắt buộc cho mỗi endpoint

- Thành công (đúng status + body + header như `Location`).
- Validation sai → `400` với `errors[]` đúng field.
- Không xác thực → `401`; không đủ quyền/không phải chủ → `403`/`404`.
- Không tồn tại → `404`; xung đột → `409`/`412` nếu có.
- `Idempotency-Key` gửi hai lần (nếu endpoint hỗ trợ).
- Phân trang: trang đầu/cuối, `pageSize` vượt tối đa, sort lạ.
- Contract test cho hình dạng response và `problem+json` (đổi hình dạng thì test fail).

## 11. Checklist trước khi báo xong một endpoint

- [ ] Path, method, status đúng bảng mục 1–2; không có tên service/động từ trong path
- [ ] DTO riêng, camelCase, thời gian UTC ISO 8601, enum chuỗi
- [ ] Lỗi dùng `problem+json` chung, có `code`, `traceId`, `correlationId`
- [ ] Danh sách có phân trang/sort/lọc theo mục 4
- [ ] Xác thực + kiểm tra quyền trên tài nguyên; không lộ dữ liệu nhạy cảm
- [ ] OpenAPI cập nhật đủ response; Swagger không bật ở prod
- [ ] Spec/contract + test + consumer đã cập nhật (nếu đổi hình dạng)
- [ ] Test ở mục 10 đã có và xanh
