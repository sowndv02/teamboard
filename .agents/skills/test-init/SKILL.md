---
name: test-init
description: Khởi tạo test theo BDD và TDD cho một issue đã có plan được duyệt - viết kịch bản .feature, step definition, test thực thi, rồi chạy để xác nhận test ĐỎ đúng lý do, chưa viết code production. Dùng sau plan-review PASS và trước test-review.
---

# Test init (TDD/BDD)

Sinh bộ test **trước** code production: kịch bản BDD (`.feature`) từ spec/plan, test thực thi (unit/integration/contract) theo TDD, rồi chạy để thấy **đỏ đúng lý do**. Không viết code production ngoài **khung tối thiểu để biên dịch** (stub ném `NotImplementedException`/trả giá trị rỗng không có logic).

Rule liên quan: `test-review.md` (chuẩn `.feature`, tag, reconcile), `testing.md`, `quality-gate.md`, `api-endpoints.md` (test mỗi endpoint), `messaging.md` (test outbox/consumer), `dotnet.md`.

## Đầu vào và điều kiện
- Plan `approved` (bước 6 PASS), issue `TB-NNN` và branch `feature/TB-NNN-*` đã có (kiểm `git branch --show-current`).
- **Công cụ test/BDD đã chốt** trong `config/tool-versions.yaml` (`dotnet_test_framework`, `bdd_runner_dotnet`). Còn `TODO` cho phần cần dùng → **dừng, báo người dùng chọn** (đề xuất kèm đánh đổi, nêu cần ADR); **không tự chọn** và không bắt đầu viết test.
- Môi trường chạy được test (Docker cho Testcontainers/LocalStack, SDK...). Không chạy được → báo, không giả vờ.

## Quy trình
1. **Đọc** spec (tiêu chí chấp nhận), plan (bảng truy vết, kịch bản dự kiến), design (hợp đồng, chế độ lỗi) và code hiện có/test hiện có để theo style.
2. **Viết `.feature`** tại `document/spec/features/<service>/<ten>.feature` theo `test-review.md` mục 1–2: khai báo, ngôn ngữ nghiệp vụ, một `When`, tag issue (`@TB-NNN`) và ID kịch bản (`@S01`...), đường lỗi và biên (quyền, trùng, validation, đồng thời, hết hạn, idempotency). Mỗi kịch bản truy được về một FR/BR/NFR. **Hành vi spec không nói → không tự viết kịch bản đoán**; ghi vào Câu hỏi mở (xem dưới).
3. **Viết step definition và test thực thi** theo plan (từng bước một chu kỳ), đặt đúng project/thư mục theo cấu trúc service; tên test `Method_Condition_ExpectedResult`; không phụ thuộc thứ tự/thời gian thực; clock/ID giả khi cần; integration dùng DB thật trong container (không mock DB).
4. **Khung tối thiểu để biên dịch**: interface/stub rỗng để test chạy được. Không logic.
5. **Chạy test** → phải **đỏ đúng lý do** (assertion fail vì chưa có hành vi / `NotImplemented`), **không** đỏ vì lỗi biên dịch, thiếu cấu hình, thiếu dữ liệu, môi trường. Điều tra và sửa nguyên nhân đỏ sai.
6. **Ghi vào issue** mục "Kịch bản và TDD log": bảng `Kịch bản | Test thực thi | Đỏ | Xanh`, lệnh đã chạy và kết quả đỏ (số test, tên, lý do).
7. **Cập nhật bảng Workflow** của issue (`test-init: done`, "đỏ: N test"); báo `workflow` mở `test-review`.

## Cơ cấu test (kim tự tháp) và chất lượng
- **Kim tự tháp:** phần lớn là unit test nhanh cho logic; integration test ít hơn, chỉ cho biên thật (DB, hàng đợi, HTTP); **contract test** cho API/event giữa service (producer không phá consumer; dùng công cụ contract khi dự án có, nếu chưa thì test schema/ví dụ payload dùng chung từ `src/shared/`); e2e rất ít, chỉ luồng sống còn.
- **BDD chọn lọc:** `.feature` cho hành vi người dùng/nghiệp vụ nhìn thấy được (đã là chuẩn của dự án, `test-review.md`); logic nội bộ, thuật toán, mapper chỉ cần unit test thường, không ép thành kịch bản.
- **Test migration:** thay đổi schema có test chạy migration trên DB container (lên và, nếu có đường lùi, xuống) và kiểm tương thích dữ liệu cũ.
- **Không flaky:** cấm `Thread.Sleep`/chờ cố định; dùng chờ có điều kiện + timeout, clock/ID giả, dữ liệu độc lập từng test, không phụ thuộc thứ tự. Test flaky là lỗi phải sửa ngay, không retry cho qua.
- **Frontend (`src/web`):** unit/component test bằng Vitest theo hành vi; kịch bản BDD cho luồng người dùng thấy chạy qua **Playwright** (e2e ít, luồng sống còn); kiểm axe cho màn hình mới (`frontend-testing.md`, skill `angular-feature`).
- Làn nhỏ: chỉ test tái hiện lỗi (đỏ→xanh), không cần `.feature` mới trừ khi đổi hành vi nhìn thấy; làn vừa: `.feature` gọn cho hành vi mới (`../workflow/references/lanes.md`).

## Ngoài đời thực
Ở công ty, test được viết cùng code trong PR và CI chạy bắt buộc xanh; không ai chạy BDD cho mọi dòng. Giá trị thật là test nhanh, ổn định, bắt được lỗi trước khi lên môi trường. Giữ thói quen: hỏi "test này bắt lỗi gì mà test rẻ hơn không bắt được?" trước khi thêm integration/e2e.

## Plan of record và tự kiểm trước khi mời review
- **Plan `approved` là neo**: không "nghĩ lại" tiêu chí chấp nhận khi viết test. Thấy plan/spec sai hoặc thiếu → ghi Câu hỏi mở, không tự đổi.
- **Tự kiểm cơ học trước khi chuyển `test-review`** (`../workflow/references/generation-principles.md` mục 3A): mọi FR/BR/NFR của lát có kịch bản; mọi kịch bản truy được về yêu cầu; tag đủ (`@TB-NNN`, `@Sxx`); không `@wip`/skip; step nhất quán; checklist `test-review.md` mục 1–3 đạt. Còn lỗi → sửa, **không** chuyển bước.
- Có `.feature` không có nghĩa test chạy đúng: bắt buộc chạy và thấy đỏ đúng lý do (bước 5 ở trên).

## Điểm cần người dùng (không tự quyết)
- Công cụ test/BDD còn `TODO`.
- Hành vi spec/plan không nói đủ để viết kịch bản (mã lỗi, thông điệp, thứ tự, giới hạn).
- Spec/plan mâu thuẫn nhau hoặc với rule.
Ghi vào "Câu hỏi mở" của plan (hoặc `.feature` bằng tag `@wip` kèm lý do) và để `test-review` hỏi người dùng. Không dùng `@wip` để lách: kịch bản `@wip` không được tồn tại khi sang `coding-start`.

## Không làm
- Không viết code production có logic; không sửa spec/plan để cho test dễ viết.
- Không skip/ignore/xoá test để "đỏ rồi xanh".
- Không commit/push khi chưa được phép (xem `git-workflow.md`); khi được phép, commit theo `git-commit.md` loại `test`.

## Hoàn thành khi
- [ ] Mọi kịch bản của plan có `.feature` + test thực thi, tag đúng, bảng truy vết đủ
- [ ] Chạy test: **đỏ đúng lý do**, không có lỗi biên dịch/môi trường
- [ ] Không có `@wip`/skip; không có logic production
- [ ] Issue ghi nhật ký đỏ; Workflow cập nhật

## Ghi trạng thái workflow

Theo `../workflow/references/state-protocol.md`: ghi state file `document/workflow/<id>.yaml` **trước khi bắt đầu** (in_progress, owner, hash đầu vào), **sau mỗi vòng/khi cần người dùng chốt/khi bị chặn**, và **khi hoàn thành** (bằng chứng, next_action), rồi cập nhật bảng Workflow trong issue. Đọc state trước khi làm để biết có agent khác đang nhận việc hay đầu vào đã đổi.
