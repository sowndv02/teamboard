---
name: system-design
description: Thiết kế hệ thống từ một spec đã rõ, kèm giải thích đánh đổi và lý do chọn/không chọn từng phương án, đối chiếu use case tương tự, nhằm nâng cao trình độ system design. Dùng sau khi spec đã xác nhận (xem requirements-discovery) và trước khi code; cũng dùng để so sánh hai phương án, review một thiết kế có sẵn, giải thích một pattern theo ngữ cảnh dự án, hoặc phân tích khi tải tăng 10x/100x.
---

# System design có giải thích

Từ **spec đã xác nhận** → **design doc** (`document/design/<ten>.md`) + **bản nháp ADR** cho mỗi quyết định lớn. Mục tiêu kép: ra thiết kế đủ tốt cho yêu cầu, **và** giúp người dùng hiểu *vì sao* (đánh đổi, phương án bị loại, khi nào đổi ý) để học system design.

Rule liên quan: `architecture.md` (kiến trúc bắt buộc từng service), `api-endpoints.md`, `messaging.md`, `observability.md`, `terraform.md`, `docs-conventions.md`, `docs-update-specs.md`, `agent-conduct.md` (trung thực, không bịa).

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `design` (mặc định) | Có spec `confirmed`, cần thiết kế | Design doc + ADR nháp + đề xuất issue |
| `compare` | Phân vân giữa A và B cho một quyết định | Bảng so sánh + khuyến nghị + "vì sao không chọn" |
| `review` | Có thiết kế sẵn, cần phản biện | Danh sách điểm yếu, rủi ro, phương án thay thế |
| `explain` | Muốn hiểu một pattern (outbox, saga, CQRS, cache...) trong ngữ cảnh dự án | Giải thích: là gì, vì sao, khi nào dùng/không, ví dụ trong dự án |
| `scale` | "Điều gì hỏng khi tải gấp 10x/100x?" | Điểm nghẽn theo thứ tự + cách xử lý + chi phí |

Không cần gõ lệnh; suy ra chế độ từ lời người dùng, không rõ thì hỏi.

## Nguyên tắc

1. **Quyết định dựa trên yêu cầu, không dựa trên sở thích hay mốt.** Mỗi lựa chọn nối về một FR/NFR hoặc ràng buộc cụ thể trong spec.
2. **Luôn có ≥ 2 phương án thật** (kể cả "đơn giản nhất có thể chạy được"). Phương án giả chỉ để bị loại không tính.
3. **Giải thích cả cái bị loại.** Mỗi quyết định phải có mục "Vì sao không chọn B/C", nói rõ điều kiện nào thì B/C lại đúng.
4. **Đánh đổi nói thẳng**: lựa chọn nào cũng mất cái gì; ghi cái mất, ai chịu, khi nào thành vấn đề.
5. **Đơn giản trước, có lộ trình mở rộng.** Chọn phương án đơn giản nhất đáp ứng NFR **có dự phòng hợp lý**, kèm tín hiệu cho biết khi nào phải nâng cấp (revisit trigger). Không thiết kế cho tải không có trong spec.
6. **Tách "cần" và "chọn để luyện tập".** Dự án này cố ý bắt dùng nhiều kiến trúc để học (`architecture.md`). Khi một lựa chọn *không cần* cho yêu cầu nhưng *được chọn để luyện*, ghi rõ nhãn `[Chọn để học]` và nêu phương án đơn giản hơn nếu làm thật. Không giả vờ nó là bắt buộc kỹ thuật.
7. **Không bịa số liệu hay ví dụ.** Số ước lượng ghi rõ giả định; use case tương tự dựa trên pattern công khai, nói mức tin cậy, không bịa nội dung/số liệu cụ thể của công ty nào (xem bước 5).
8. **Tôn trọng ràng buộc đã chốt**: tech stack (ADR 0005), AWS/Terraform, kiến trúc bắt buộc từng service. Muốn phá ràng buộc → nêu thành đề xuất ADR riêng, không tự phá.
9. **Chỉ thiết kế, không code.** Trừ ví dụ ngắn minh hoạ trong tài liệu.

## Quy trình `design`

### 0. Cổng đầu vào
- Spec tồn tại ở `document/spec/`, trạng thái `confirmed` (hoặc `draft` với "Câu hỏi mở" không chặn quyết định đang thiết kế). Chưa đạt → quay lại `requirements-discovery`, không thiết kế trên yêu cầu mơ hồ.
- Đọc spec, `document/architecture/README.md`, ADR hiện có, `config/services.yaml`, design doc liên quan. Không đoán hiện trạng.

### 1. Trích yếu tố quyết định (drivers)
Lập bảng từ spec:
- **FR then chốt** (những FR tác động tới cấu trúc, không phải mọi FR).
- **NFR có số đo**: số người dùng/đồng thời, request/giây, kích thước dữ liệu và tốc độ tăng, độ trễ (p95), độ sẵn sàng, mức nhất quán, độ bền dữ liệu, bảo mật, chi phí trần, thời gian khôi phục (RTO/RPO).
- **SLI/SLO**: với luồng chính, chọn SLI đo được (tỉ lệ thành công, độ trễ p95) và SLO mục tiêu (ví dụ 99,5%/30 ngày) → suy ra **error budget**; SLO là cơ sở cho cảnh báo và cho quyết định "đủ tốt chưa". Dự án học: đặt SLO khiêm tốn, ghi là giả định.
- **Ràng buộc**: stack, AWS, ngân sách, quy mô đội (đây là dự án học), thời hạn.
- NFR **thiếu số** → hỏi (`AskUserQuestion`, tối đa 4 câu/lô, kèm mặc định đề xuất). Người dùng không biết → đặt **giả định có ghi rõ** và đánh dấu để xác nhận.

### 2. Ước lượng nhanh (back-of-envelope)
Tính tải, dữ liệu, băng thông từ NFR (xem `references/design-checklist.md`). Ghi công thức và giả định; mục đích là biết **bậc độ lớn** (100 hay 100.000 req/s?) vì nó quyết định kiến trúc. Nếu tải nhỏ, nói thẳng: "kiến trúc đơn giản là đủ".

### 3. Liệt kê điểm quyết định
Xác định các quyết định thực sự có nhiều lựa chọn hợp lý (ví dụ: ranh giới service, đồng bộ/bất đồng bộ, lưu trữ, nhất quán, bộ nhớ đệm, xử lý trùng lặp, phân trang/truy vấn, xác thực, triển khai). Tối đa ~5–7 quyết định lớn; việc vặt không phải quyết định. Dùng `references/decision-framework.md` làm danh mục và tiêu chí.

### 4. Phân tích từng quyết định (định dạng bắt buộc)

Cho **mỗi** quyết định `D1..Dn`:

1. **Bài toán** (một câu) và tiêu chí liên quan (lấy từ drivers, có trọng số cao/vừa/thấp).
2. **Các phương án** (≥ 2): mô tả ngắn mỗi phương án.
3. **Bảng so sánh** theo tiêu chí (nhất quán, độ phức tạp vận hành, độ trễ, khả năng mở rộng, chi phí, độ khó test/debug, phù hợp ràng buộc dự án, khả năng đảo ngược). Điểm bằng chữ (Tốt/Vừa/Kém) kèm lý do ngắn; không chấm số giả chính xác.
4. **Khuyến nghị** + lý do nối về driver cụ thể.
5. **Vì sao không chọn từng phương án còn lại** + **điều kiện để phương án đó trở nên đúng**.
6. **Đánh đổi chấp nhận** và **rủi ro**; mức độ khó đảo ngược.
7. **Tín hiệu xem lại (revisit trigger)**: chỉ số/sự kiện cụ thể buộc phải đổi (ví dụ "khi p95 ghi vượt 300 ms" / "khi cần quá 3 consumer").
8. **Nhãn** `[Cần]` hoặc `[Chọn để học]` (nguyên tắc 6).
9. **Use case tương tự** (bước 5).

### 5. Use case tương tự
Với mỗi quyết định lớn, nêu 2–3 bài toán có cấu trúc giống (không cần cùng ngành), ví dụ: đặt chỗ/bán vé (tranh chấp tài nguyên), thanh toán (idempotency), chat/thông báo (fan-out), feed hoạt động (đọc nhiều, ghi append), quản lý tác vụ/công việc, hàng đợi xử lý nền, tìm kiếm.
- Nêu: **bài toán gốc → lựa chọn phổ biến → vì sao hợp → điểm khác với trường hợp của ta**.
- Dựa trên pattern công khai, kiến thức kỹ thuật phổ biến. **Không bịa** chi tiết nội bộ hay số liệu của công ty cụ thể. Gắn mức tin cậy: `Chắc` (pattern kinh điển, tài liệu công khai rộng) / `Vừa` (dựa trí nhớ, nên kiểm lại) / `Cần xác minh`.
- Có công cụ tìm kiếm web thì kiểm tra nguồn công khai (bài kỹ thuật, tài liệu AWS/Microsoft, sách) và ghi link; không có thì ghi "chưa xác minh nguồn" và gợi ý từ khoá để người dùng tự tra.
- Mục đích là **đối chiếu để học**, không phải để cho thiết kế có vẻ uy tín.
- Danh mục bài toán kinh điển (tranh chấp tài nguyên, idempotency, fan-out, feed, rate limiter, saga, CQRS...) kèm ánh xạ vào TeamBoard, câu hỏi tự thử và nguồn đọc thêm nằm ở `references/classic-problems.md`. Chọn bài khớp với quyết định đang xét, không liệt kê hết.

### 6. Thiết kế chi tiết (khớp ngữ cảnh dự án)
Điền design doc (`document/design/_template.md`):
- Service sở hữu gì (dữ liệu, trách nhiệm), tuân kiến trúc bắt buộc của service (`architecture.md`); ranh giới nhất quán (transaction trong một service, eventual consistency giữa các service).
- **Frontend (khi có UI, ADR 0009):** chiến lược render (SPA mặc định, SSR/hydration chỉ khi có lý do và ADR), **nơi lưu token phía client** (bộ nhớ + refresh cookie `HttpOnly`, hay BFF; nêu đánh đổi XSS/CSRF), CSP/CORS, cache và triển khai tĩnh; không quyết ở đây nếu ADR chưa có, ghi thành câu hỏi mở.
- Hợp đồng: API (theo `api-endpoints.md`), sự kiện (`messaging.md`: outbox, idempotent consumer, DLQ).
- Mô hình dữ liệu mức khái niệm, chỉ mục chính, vòng đời dữ liệu.
- **Sơ đồ Mermaid**: bối cảnh/thành phần và ít nhất một sequence cho luồng chính (theo `docs-conventions.md`), và một cho luồng lỗi quan trọng.
- **Chế độ lỗi (failure modes)**: dịch vụ phụ thuộc chết, timeout, message trùng/mất/sai thứ tự, hỏng giữa chừng, dữ liệu xấu; mỗi cái có giải pháp (timeout/retry/circuit breaker, idempotency, DLQ, bồi hoàn/saga).
- **Chọn dịch vụ AWS cụ thể cho từng service** (compute, DB, hàng đợi, mạng...) không làm trong design chung: dùng skill `aws-service-selection` (chi phí đi đầu, 7 tiêu chí); design chỉ nêu quyết định ở mức kiến trúc và trỏ tới `document/infra/<service>.md`.
- Bảo mật (authN/authZ, IDOR, dữ liệu nhạy cảm) kèm **mô hình đe doạ nhẹ STRIDE** cho mỗi ranh giới tin cậy mới (internet → gateway, service → service, service → DB); quan sát (trace, metric, log, cảnh báo gắn SLO), triển khai (ECS, cấu hình), chi phí ước tính (AWS), chiến lược test (BDD/TDD, contract, tải).
- **Thay đổi tương thích ngược**: API/event có versioning và chính sách bỏ dùng (thêm trường tuỳ chọn thay vì đổi nghĩa; consumer chịu được trường lạ); đổi schema DB theo **expand/contract** (thêm → ghi kép/backfill → chuyển đọc → gỡ cũ, mỗi pha deploy độc lập, quay lại được); thứ tự deploy producer/consumer.
- **Rollout và rollback**: bật dần qua feature flag hoặc canary khi thay đổi rủi ro cao; điều kiện rollback (tín hiệu SLO nào) và cách quay lại; dữ liệu đã ghi bằng phiên bản mới có đọc được bởi phiên bản cũ không.

### 7. Lộ trình mở rộng
Phiên bản 1 (đáp ứng spec) và bước nâng cấp khi tải tăng (10x, 100x): điểm nghẽn đầu tiên, cách xử lý, chi phí. Nêu rõ cái gì **đã chủ ý chưa làm** (và vì sao chưa cần).

### 8. Tự phản biện (pre-mortem)
Trước khi trình bày: "Giả sử 6 tháng sau thiết kế này thất bại, vì sao?" Liệt kê 3–5 nguyên nhân khả dĩ nhất, kiểm lại các giả định ước lượng, tìm phần phức tạp không cần thiết (over-engineering) và phần thiếu (điểm lỗi đơn, mất dữ liệu). Ghi vào mục Rủi ro.

### 9. Chế độ học
- Mặc định, giải thích khái niệm ở mức người đã biết lập trình nhưng chưa chắc biết pattern; định nghĩa thuật ngữ lần đầu.
- Nếu người dùng muốn **luyện chủ động** (hoặc nói "cho tôi thử chọn trước"): trước khi đưa khuyến nghị cho một quyết định, đưa bài toán + tiêu chí và **hỏi người dùng chọn phương án nào và vì sao**, rồi so với phân tích, chỉ ra điểm họ nhìn đúng/sót.
- Cuối thiết kế đưa 3–5 **câu hỏi thử thách** ("nếu message bị gửi hai lần thì sao?", "nếu DB chết 5 phút?", "tải tăng 10x thì hỏng chỗ nào trước?") để người dùng tự trả lời; trả lời khi họ yêu cầu.
- Ghi vài **bài học chính** (mục "Ghi chú học tập") để tra lại sau.

### 10. Xác nhận và bàn giao
- Trình bày tóm tắt ≤ 15 dòng: bối cảnh, driver chính, các quyết định và lựa chọn kèm lý do một dòng, rủi ro lớn, câu hỏi mở. **Không dán cả design doc vào chat.** Đưa đường dẫn.
- Chờ người dùng xác nhận/phản biện. Phản biện hợp lý → cập nhật, ghi vào lịch sử.
- Sau xác nhận, đề xuất (tạo khi được đồng ý): **ADR** cho từng quyết định lớn (dùng `document/architecture/adr/_template.md`, nêu phương án đã cân nhắc và vì sao không chọn), **bảng lát** (`plan-init` chế độ `slices`, `../workflow/references/slicing.md`: chia thành lát nhỏ giao được rồi mới tạo issue; không tự chia ad hoc ở đây), cập nhật `config/services.yaml` và `document/architecture/README.md` nếu thêm/đổi service.
- Thêm design doc vào `document/design/README.md`.

## Làn và cỡ thiết kế
Chỉ làn **lớn** (và làn vừa khi chạm contract/quyết định đáng ghi ADR) mới cần design doc đầy đủ (`../workflow/references/lanes.md`). Làn vừa: mục tiêu là 1–2 quyết định + failure modes + rollout, đủ trong một trang; làn nhỏ không có design.

## Ngoài đời thực
Ở công ty, design doc/RFC thường 2–6 trang, được đồng đội và người sở hữu service liên quan góp ý bằng comment trước khi code, và kết thúc bằng ADR cho quyết định khó đảo ngược. Họ ít khi so sánh 3 phương án cho mọi thứ: chỉ làm kỹ chỗ đắt khi sai (dữ liệu, contract công khai, bảo mật). Tập cách đó: bỏ việc vặt, dồn công sức vào quyết định khó đảo ngược, luôn kèm cách rollout và rollback.

## Chế độ `compare`
Chỉ làm bước 4 cho một quyết định: bài toán, tiêu chí (hỏi người dùng ưu tiên nếu chưa rõ), bảng so sánh, khuyến nghị, vì sao không chọn cái kia, điều kiện đảo ngược, use case tương tự. Hỏi 1–2 câu về ngữ cảnh nếu thiếu (tải, ràng buộc).

## Chế độ `review`
Đọc thiết kế, ghi: (1) giả định ngầm chưa nêu, (2) điểm lỗi đơn và chế độ lỗi chưa xử lý, (3) quyết định thiếu phương án thay thế, (4) phức tạp thừa, (5) lệch ràng buộc/kiến trúc dự án, (6) thiếu số đo. Mỗi điểm: mức độ, bằng chứng, gợi ý. Phân biệt lỗi chắc chắn với nghi ngờ.

## Chế độ `explain`
Cấu trúc: **vấn đề nó giải** → **cơ chế (sơ đồ nhỏ)** → **khi nào dùng / không dùng** → **chi phí và cạm bẫy** → **nó xuất hiện ở đâu trong dự án này** (service nào, file nào) → **bài tập nhỏ** để thử → **bài toán kinh điển tương ứng và nguồn đọc thêm** (`references/classic-problems.md`, kèm mức xác minh).

## Chế độ `scale`
Với thiết kế hiện tại và tải mục tiêu: liệt kê điểm nghẽn theo thứ tự sẽ gặp (cơ sở dữ liệu, kết nối, hàng đợi, phụ thuộc đồng bộ, nóng một khoá), cách đo, cách xử lý (đọc phụ, cache, phân vùng, bất đồng bộ hoá, tách dịch vụ), chi phí và rủi ro mỗi bước.

## Không làm
- Không thiết kế trên spec chưa rõ; không tự thêm yêu cầu.
- Không đưa khuyến nghị mà không nêu phương án khác và lý do loại.
- Không bịa số liệu, benchmark, hay thông tin nội bộ của công ty.
- Không tự phá ràng buộc đã chốt; không tự tạo ADR/issue khi chưa được đồng ý.
- Không sửa ADR đã chấp nhận (`docs-update-specs.md`); đổi quyết định = ADR mới.
- Không biến lựa chọn "để học" thành lý do kỹ thuật giả.

## Hoàn thành khi
- [ ] Spec đầu vào đã `confirmed` (hoặc câu hỏi mở không chặn)
- [ ] Drivers có số đo hoặc giả định ghi rõ; có ước lượng bậc độ lớn
- [ ] Mỗi quyết định lớn: ≥ 2 phương án, bảng so sánh, khuyến nghị, **vì sao không chọn từng phương án còn lại**, đánh đổi, revisit trigger, nhãn `[Cần]/[Chọn để học]`
- [ ] Có use case tương tự kèm mức tin cậy; không có nội dung bịa
- [ ] Có sơ đồ (bối cảnh + sequence luồng chính/lỗi), failure modes, bảo mật, quan sát, chi phí, lộ trình mở rộng
- [ ] Đã pre-mortem; đã ghi điều chủ ý chưa làm
- [ ] Có câu hỏi thử thách và ghi chú học tập
- [ ] Người dùng đã xem tóm tắt và xác nhận (hoặc ghi rõ còn chờ gì); design doc nằm đúng chỗ, có trong mục lục

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
