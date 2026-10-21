# Làn công việc theo cỡ thay đổi

Mục đích: dự án thật không đưa mọi thay đổi qua cùng một quy trình nặng. Sửa lỗi chính tả đi một đường, tính năng đa service đi đường khác, sự cố production đi đường thứ ba. Skill `workflow` **phân làn ở bước 0**, rồi chỉ chạy các bước mà làn đó yêu cầu.

Quan hệ với rule: `pipeline-lifecycle.md` đã có "làn nhẹ" và "làn hạ tầng"; file này là bản chi tiết cho skill và thêm làn vừa, hotfix. Khi mâu thuẫn, rule thắng; sửa rule trước rồi mới sửa file này.

## 1. Bốn làn

| Làn | Dùng khi | Bước chạy | Review | State file |
|---|---|---|---|---|
| **Nhỏ** (= làn nhẹ) | Sửa docs/comment/định dạng, đổi cấu hình không đổi hành vi, bug nhỏ một file đã có test | Issue ngắn → (test tái hiện nếu là bug) → code → gate → PR | Tự review diff một vòng; không sub-agent | Không |
| **Vừa** | Một tính năng/thay đổi hành vi trong **một service**, không đổi kiến trúc, không migration phá vỡ | Spec ngắn (có thể nằm trong issue) → plan → test-init/test-review → coding → coding-review → PR. `system-design` chỉ khi chạm contract hoặc có quyết định đáng ghi ADR | Tối đa 3 vòng mỗi bước review | Tuỳ chọn (khuyến nghị nếu có sub-agent) |
| **Lớn** | Tính năng mới, đa service, đổi contract công khai, migration dữ liệu, bảo mật/xác thực, hạ tầng mới | Đủ 11 bước của `workflow/SKILL.md`, thêm Demo/UAT & bàn giao khi có khách | Tối đa 5 vòng; review độc lập (sub-agent) cho design, plan, coding | Bắt buộc |
| **Hotfix** | Sự cố đang ảnh hưởng người dùng/dữ liệu | Xem mục 3 | Review nhanh một người khác nếu có; bù review đầy đủ sau | Không (ghi vào issue + post-mortem) |

Không chắc làn nào → hỏi người dùng (nêu làn đề xuất và vì sao). Chọn làn thấp hơn rồi phát hiện phình to (đổi contract, đụng bảo mật, quá ~400 dòng diff, cần migration) → **dừng, nâng làn**, ghi lý do vào issue, bổ sung các bước còn thiếu.

## 2. Cách chọn làn (đi từ trên xuống, gặp điều kiện đầu tiên thì dừng)

1. Có sự cố production hoặc rò rỉ dữ liệu? → **Hotfix**.
2. Đổi contract (API/event), schema dữ liệu, xác thực/phân quyền, hạ tầng, hoặc chạm ≥ 2 service? → **Lớn**.
3. Đổi hành vi nhìn thấy được trong một service? → **Vừa**.
4. Còn lại (không đổi hành vi) → **Nhỏ**.

Người dùng yêu cầu đủ quy trình thì luôn **Lớn**.

## 3. Hotfix

- Branch `hotfix/TB-NNN-<slug>` từ nhánh đang chạy production (`main`).
- Sửa **tối thiểu**; không refactor kèm. Có **test tái hiện lỗi** (đỏ trước, xanh sau) trừ khi bất khả thi; nói rõ nếu bỏ.
- Gate đầy đủ vẫn chạy (build, test, secret); không `--no-verify`.
- PR tiêu đề `TB-NNN: hotfix <tóm tắt>`; mô tả có: ảnh hưởng, nguyên nhân sơ bộ, cách kiểm chứng, **cách rollback**.
- Cần người dùng đồng ý trước push/PR như mọi làn. Không có "ngoại lệ vì khẩn cấp" cho thao tác phá huỷ hoặc ghi lên môi trường thật.
- Sau khi ổn định (trong vài ngày): post-mortem không đổ lỗi qua skill `lesson-learned` (timeline, impact, nguyên nhân gốc, hành động có owner/hạn); bù spec/test/tài liệu còn thiếu bằng issue riêng.

## 4. Ghi làn vào issue

Thêm dòng `Làn: nhỏ | vừa | lớn | hotfix` vào đầu mục Workflow của issue (cùng lý do nếu nâng làn). Bảng Workflow chỉ liệt kê bước thuộc làn đó; bước bỏ ghi `n/a (làn nhỏ)` thay vì để trống.

## 5. Điều không đổi ở mọi làn

- Mọi thay đổi gắn issue và branch; commit theo `git-commit.md`.
- Test trước code cho hành vi mới (TDD); bug có test tái hiện.
- Gate xanh trước khi báo xong; không bỏ qua test hay hook fail.
- Không tự quyết điều spec chưa nói; không commit/push/PR khi chưa được phép.
- Quét secret, không đưa secret vào repo.

## 6. Ngoài đời thực

Nhóm thật thường gọi các làn này là "chore/bug/feature/incident" và có tiêu chí cỡ PR (dưới vài trăm dòng), người review thật, CI bắt buộc xanh. Ở đây agent đóng vai người viết và người tự review; người dùng là người review/duyệt cuối. Tập thói quen: **luôn tự hỏi "làn nào?" trước khi bắt đầu**, và nâng làn khi thấy việc phình.
