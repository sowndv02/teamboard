# Quan điểm review theo từng phase

Đọc cùng `review-loop.md`. File này trả lời: **mỗi bước review hỏi câu gì, tránh lỗi gì, "PASS" nghĩa là gì**, và ba lăng kính xuyên suốt **Chất lượng, Bảo mật, Chi phí** áp dụng thế nào ở từng bước.

## 1. Quan điểm tổng thể

```
spec-review    → "Yêu cầu có đủ, rõ, nhất quán để thiết kế mà không phải đoán không?"
design-review  → "Thiết kế có đáp ứng đúng spec, an toàn, tin cậy, và chi phí hợp lý không?"
plan-review    → "Kế hoạch có đúng spec/design và đủ chi tiết để viết test và code mà không phải đoán không?"
test-review    → "Bộ test (BDD + TDD) có bám tiêu chí chấp nhận, đúng chuẩn, và đỏ đúng lý do không?"
coding-review  → "Code thực tế có đúng plan/spec, an toàn, chất lượng tốt, và không phá thứ khác không?"
```

| Nguyên tắc | Ý nghĩa |
|---|---|
| **Mỗi bước một artifact** | spec → design → plan → `.feature`/test → code. Review chỉ chấm artifact của bước đó, nhưng đối chiếu với artifact trước |
| **Người dùng chốt, agent không đoán** | Điểm chưa rõ/mâu thuẫn/chưa quyết là CONFIRM; không thay bằng "Giới hạn đã biết" hay "giả định tạm" nếu người dùng chưa chấp nhận |
| **Finding có fingerprint** | `spec:*`, `design:*`, `plan:*`, `test:*`, `code:*` (ổn định giữa các vòng, không trùng namespace) |
| **Vòng lặp review → sửa → review (delta)** | Theo `review-loop.md`; vòng 1 đầy đủ, vòng sau tập trung phần thay đổi |
| **Cổng cứng** | Không sang bước sau khi còn CONFIRM mở; test phải pass trước `coding-review` |
| **Review lớn tách lăng kính/lô** | Chia theo lăng kính, mỗi lô ≤ 3 nhóm; dùng sub-agent độc lập khi bước review lớn (đã chốt) |
| **Mặc định report-only ở code** | Không tự sửa code khi review; sửa là bước riêng (`code-review.md`) |
| **Bảo mật lặp ở mọi tầng** | Hỏi từ spec, thiết kế mối đe doạ ở design, bước bảo mật có test ở plan/test, soi code ở coding (không chỉ ở PR) |

## 2. Thứ bậc nguồn thẩm quyền

Khi hai artifact mâu thuẫn, bên cao hơn thắng; mâu thuẫn **luôn là CONFIRM**, không tự chọn:

```
1. Yêu cầu gốc của khách/người dùng (nguyên văn trong spec)
2. Spec đã confirmed
3. ADR đã chấp nhận (và ràng buộc đã chốt: tech stack, kiến trúc bắt buộc)
4. Design đã confirmed
5. Plan đã approved
6. .feature và test
7. Code
```

Artifact thấp lệch artifact cao → sửa artifact thấp (FIX) **chỉ khi** có đúng một cách hiểu; nếu artifact cao mơ hồ hoặc có vẻ sai → CONFIRM với người dùng (và quay lui sửa artifact cao theo `workflow`).

## 3. Mỗi bước nói gì

| Bước | Đối tượng | Mục đích | Tránh lỗi gì | "PASS" nghĩa là |
|---|---|---|---|---|
| **spec-review** | `document/spec/*.md` | Yêu cầu đủ, rõ, kiểm chứng được | Thiết kế trên yêu cầu mơ hồ; phình phạm vi; bịa yêu cầu | Spec `confirmed`; không còn điểm chưa chốt |
| **design-review** | `document/design/*.md` | Quyết định đúng, có lý do, an toàn, tin cậy, rẻ vừa đủ | Phương án duy nhất không so sánh; phá ADR; bỏ sót chế độ lỗi/bảo mật/chi phí | Design `confirmed`; mọi quyết định lớn đã chốt |
| **plan-review** | `document/plans/*.md` | Đúng hướng và đủ chi tiết để bước sau không đoán | Code sai spec; thiếu quyết định; bước quá lớn; plan tự thêm yêu cầu | Plan `approved`; hết câu hỏi mở |
| **test-review** | `.feature` + test thực thi (đỏ) | Tiêu chí hành vi được viết ra đúng chuẩn và có test thật | AC mơ hồ; test không kiểm gì; đỏ sai lý do; thiếu đường lỗi/bảo mật | Test `ready`; đỏ đúng lý do; mọi kịch bản có test |
| **coding-review** | Diff + test (đã xanh) | Code đúng, an toàn, chất lượng, không phá consumer | Bug, lỗ hổng, lệch plan, thiếu failure case, hardcode, lãng phí | Verdict merge-ready; test xanh lần chạy cuối |

Điểm đặc thù:
- **spec/plan:** chưa chốt phạm vi → **hỏi người dùng** (decision gate), không ghi "Giới hạn đã biết" để né câu hỏi.
- **test-review:** đánh giá *spec hành vi thực thi được*, không phải "test pass". P3 phải do người dùng quyết sửa/bỏ qua có lý do.
- **coding-review:** rubric chi tiết nằm ở `code-review.md`; skill chỉ điều phối. Vào review khi test xanh hết.

## 4. Ba lăng kính xuyên suốt

Mỗi bước kiểm cả ba lăng kính ở mức phù hợp artifact. Chuẩn đối chiếu: **AWS Well-Architected** (Security, Reliability, Performance Efficiency, Cost Optimization, Operational Excellence), **OWASP Top 10 / ASVS** cho ứng dụng, **STRIDE** cho mô hình đe doạ nhẹ, nguyên tắc **FinOps** cơ bản cho chi phí. Đây là điểm xuất phát; kiểm tra tài liệu hiện hành khi cần chi tiết, không bịa số liệu.

### 4.1 Chất lượng (Reliability, Performance, Operability, Maintainability)

| Bước | Kiểm |
|---|---|
| spec | NFR có số đo (độ trễ, thông lượng, sẵn sàng, khôi phục, độ bền dữ liệu); tiêu chí chấp nhận kiểm chứng được; trường hợp biên |
| design | Chế độ lỗi, không có điểm lỗi đơn, timeout/retry/circuit breaker, idempotency, nhất quán; khả năng quan sát (trace/metric/log/cảnh báo); lộ trình mở rộng; khả năng test/debug |
| plan | Thứ tự theo rủi ro; rollback; migration an toàn; bước kiểm chứng cụ thể; không bước "hoàn thiện" mơ hồ |
| test | Độ sâu assert (trạng thái thật); đường lỗi/biên; test ổn định, độc lập; đỏ đúng lý do |
| code | Đúng, xử lý lỗi, không nuốt lỗi, không N+1, có phân trang/giới hạn, log/trace đúng, dễ đọc, theo kiến trúc service |

### 4.2 Bảo mật

| Bước | Kiểm |
|---|---|
| spec | Vai trò/quyền, dữ liệu nhạy cảm/PII, audit, tuân thủ (xem `config/project.yaml` → `quality.compliance`), yêu cầu xác thực |
| design | Mô hình đe doạ nhẹ (STRIDE) cho ranh giới tin cậy; authN/authZ ở từng tầng; chống IDOR; quản lý bí mật; mã hoá khi truyền/lưu; giảm bề mặt tấn công; least privilege (IAM); ghi log bảo mật |
| plan | Có bước bảo mật cụ thể (validate input, kiểm quyền, secret từ nguồn đúng); cấu hình không lộ; bước kiểm chứng bảo mật |
| test | Test authZ/IDOR, validation/biên đầu vào, rò rỉ thông tin trong lỗi/log, quyền theo vai trò, token hết hạn/giả |
| code | OWASP Top 10 (broken access control, injection, cryptographic failures, security misconfiguration, vulnerable components, SSRF...); không secret trong code/log; phụ thuộc có lỗ hổng (`quality-gate.md`); CORS/CSP/header; không `bypassSecurityTrust*` sai chỗ |

### 4.3 Chi phí

| Bước | Kiểm |
|---|---|
| spec | Trần ngân sách (xem `quality.monthlyBudgetUsd`), giá trị so với công sức, yêu cầu có chi phí vận hành lớn (độ sẵn sàng cao, đa vùng, lưu trữ dài) có thật sự cần không |
| design | **Ước tính chi phí AWS hằng tháng** theo thành phần (compute, DB, cân bằng tải, NAT, lưu trữ, truyền dữ liệu, log); tài nguyên chạy 24/7 vs theo nhu cầu; kích thước tối thiểu cho dev/stg; cách tắt/`destroy`; **chi phí của độ phức tạp** (vận hành, học, debug) so với giá trị; phương án rẻ hơn đã cân nhắc |
| plan | Môi trường dev/test dùng cấu hình nhỏ; bước dọn tài nguyên; không tạo tài nguyên tốn tiền ngoài Terraform; tag chi phí (`project`, `env`, `service`) |
| test | Chi phí chạy test (thời gian CI, container, dịch vụ trả phí); không gọi dịch vụ AWS thật khi LocalStack đủ |
| code | Truy vấn/gọi ngoài thừa, vòng lặp tốn kém, dữ liệu lưu/log quá mức (retention), kích thước bundle, polling không cần thiết, tài nguyên không giải phóng |

Cạm bẫy chi phí AWS hay gặp ở dự án này (kiểm khi design/plan): NAT Gateway chạy liên tục, RDS (SQL Server tốn hơn PostgreSQL) chạy 24/7, ALB, Fargate sizing quá lớn, CloudWatch Logs không đặt retention, truyền dữ liệu xuyên AZ/Internet, tài nguyên bỏ quên sau buổi lab. Con số cụ thể luôn **ước tính kèm giả định và nguồn giá**, không bịa.

## 5. Phân loại finding cho ba lăng kính

| Tình huống | Loại | Mức |
|---|---|---|
| Lỗ hổng bảo mật rõ ràng (thiếu kiểm quyền, secret lộ, injection) và có một cách sửa chuẩn | FIX | `blocker` |
| Đánh đổi bảo mật có nhiều cách hợp lý (mức mã hoá, nơi lưu token, thời hạn phiên) | CONFIRM | `blocker` |
| Rủi ro bảo mật còn lại người dùng muốn chấp nhận | CONFIRM → `Hoãn` có người chịu trách nhiệm | ghi vào Confirm log, mục rủi ro |
| Thiếu NFR/số đo chất lượng | CONFIRM `missing` | `blocker` ở spec/design |
| Chế độ lỗi/độ tin cậy chưa xử lý, có cách sửa chuẩn (timeout, idempotency) | FIX | `major` |
| Đánh đổi chi phí ↔ chất lượng/độ sẵn sàng (đa AZ, kích thước, độ dày dữ liệu) | CONFIRM | `blocker` nếu vượt trần ngân sách, ngược lại `major` |
| Lãng phí rõ ràng (thiếu retention log, tài nguyên không tag, không có cách tắt) | FIX | `major` |
| Chưa có ước tính chi phí hoặc con số ngân sách (`TODO`) khi design có tài nguyên AWS | CONFIRM `missing` | `blocker` ở design |

Quy tắc: **bảo mật mặc định nghiêm hơn chi phí**; không đánh đổi bảo mật lấy chi phí khi chưa được người dùng quyết định tường minh.

## 6. Mẫu phần "Lăng kính" trong báo cáo review

```
| Lăng kính | Đã kiểm | Kết quả | Finding |
|---|---|---|---|
| Chất lượng | NFR, chế độ lỗi, quan sát | Đạt / Có vấn đề | design:board-assign:no-timeout |
| Bảo mật | authZ, IDOR, secret | ... | ... |
| Chi phí | Ước tính, 24/7, cách tắt | ... | ... |
```

Mục nào không áp dụng ở bước này ghi `Không áp dụng` + lý do một dòng. Lăng kính áp dụng mà chưa kiểm chứng → không được `PASS`.
