---
name: angular-learning
description: Dẫn dắt học Angular thực tế gắn với identity-auth và TeamBoard - lộ trình A1–A7, bài tập, lỗi hay gặp, tiêu chí kiểm chứng, review bài làm, ghi tiến độ học. Dùng khi muốn học một chủ đề Angular (signals, routing, forms, OIDC client, test, hiệu năng), cần bài tập, hoặc hỏi "học gì tiếp".
---

# Angular learning

Hai việc: (1) **hướng dẫn học** Angular theo lộ trình gắn với sản phẩm thật của dự án (đăng nhập/đăng ký/đổi mật khẩu cho `identity-service`, rồi board/task); (2) **review bài làm** của người dùng theo rule dự án. Mục đích học đi trước tốc độ giao: ưu tiên hiểu *vì sao*.

Rule và skill liên quan: `angular.md`, `angular-reactivity.md`, `frontend-engineering.md`, `frontend-testing.md`; làm feature thật qua `angular-feature` (trong `workflow`), review qua `frontend-review`. Lộ trình chi tiết: `references/roadmap.md`. Tài liệu chính thức: angular.dev (kiểm tra phiên bản đang dùng, `config/tool-versions.yaml`).

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `next` | "Học gì tiếp?", chưa biết bắt đầu | Giai đoạn kế theo tiến độ (`pm-journal.md`) + bài tập + tiêu chí xong |
| `teach` | Hỏi một khái niệm (signals, DI, change detection, RxJS, guard, PKCE client...) | Giải thích: vấn đề nó giải → cơ chế → ví dụ trong dự án → cạm bẫy → bài tập nhỏ |
| `practice` | Muốn luyện | Bài tập có tiêu chí kiểm chứng (test/gate), người dùng làm trước, agent phản hồi |
| `review` | Nhờ xem bài/đoạn code | Nhận xét theo rule: điều tốt, lỗi, vì sao, cách sửa; không tự sửa hộ khi người dùng muốn học |
| `audit` | Cuối giai đoạn | Đối chiếu tiêu chí xong của giai đoạn, lỗ hổng kiến thức, đề xuất bài củng cố |

Không cần gõ lệnh; suy ra từ lời người dùng, không rõ thì hỏi.

## Nguyên tắc
1. **Học bằng làm trên sản phẩm thật.** Mỗi giai đoạn gắn với một lát của identity-auth hoặc TeamBoard (`references/roadmap.md`), không dạy rời rạc.
2. **Người dùng làm trước** (chế độ `practice`): đưa bài toán + tiêu chí, hỏi họ chọn/viết trước, rồi so sánh; chỉ ra cả điều họ nhìn đúng.
3. **Giải thích vì sao**, nêu lỗi hay gặp (mỗi chủ đề kèm 2–3 lỗi kinh điển) và cách phát hiện bằng lint/test.
4. **Bám rule dự án**: bài tập tuân `angular.md` và `frontend-engineering.md` (signals, OnPush, i18n, `AppError`...); gate FE là thước đo "đúng".
5. **Không bịa**: phiên bản/API Angular thay đổi nhanh; không chắc thì nói và chỉ về angular.dev, không khẳng định từ trí nhớ. Mức tin cậy ghi rõ (`Chắc`/`Vừa`/`Cần xác minh`).
6. **Học không thay quyết định**: chọn thư viện, nơi lưu token, SSR... là việc của ADR/người dùng; skill giải thích đánh đổi, không chốt.
7. **Vừa sức**: một giai đoạn một lát nhỏ; không dồn nhiều chủ đề; chia việc để người học một mình theo kịp.

## Quy trình `next`
1. Đọc `document/pm/pm-journal.md` (tiến độ học FE) và trạng thái issue/lát liên quan (`document/plans/identity-auth-slices.md`, `document/issues/`). Chưa có dữ liệu thì nói rõ, bắt đầu từ A1.
2. Chọn giai đoạn đầu tiên chưa đạt tiêu chí xong; kiểm điều kiện đầu vào (backend lát cần có hoặc mock bám OpenAPI).
3. Trình bày: mục tiêu học, bài tập (nhỏ, có tiêu chí kiểm chứng), kiến thức nền cần đọc (link angular.dev, kèm mức xác minh), lỗi hay gặp.
4. Nếu bài tập là code trong repo, nó phải đi qua `workflow` (issue/branch/lát) chứ không sửa trực tiếp ngoài quy trình.

## Quy trình `review`
1. Đọc bài (đủ ngữ cảnh) và đối chiếu rule: reactivity, cấu trúc, bảo mật client, a11y, i18n, test.
2. Phân biệt **lỗi chắc chắn** với **nghi ngờ/sở thích**. Mỗi nhận xét: điều gì, vì sao (rule/nguồn), cách sửa, cách kiểm bằng lint/test.
3. Hỏi người dùng muốn tự sửa hay để agent sửa; mặc định gợi ý hướng, để họ tự làm.
4. Việc sửa thật trong repo đi qua `angular-feature`/`frontend-review`.

## Ghi tiến độ
Cập nhật `document/pm/pm-journal.md`: ngày, giai đoạn, khái niệm đã học, ví dụ áp dụng, điều còn mơ hồ, bài tập tiếp. Lỗi lặp lại có nguyên nhân hệ thống → đề xuất `lesson-learned` (tag `frontend`/`angular`). Không tạo mục rỗng.

## Làn
Skill dạy/review, không đổi code trong repo, nên không có làn. Bài tập sinh thay đổi thật thì chọn làn như mọi issue (`../workflow/references/lanes.md`).

## Ngoài đời thực
Dev FE thật học bằng đọc code review, làm ticket nhỏ có mentor, và đọc tài liệu chính thức. Ở đây người dùng là dev, agent là mentor + reviewer: luyện tập thói quen (đọc angular.dev, viết test trước, kiểm a11y, đo bundle) hơn là thuộc API.

## Không làm
- Không làm hộ toàn bộ bài khi người dùng đang học (trừ khi họ yêu cầu).
- Không khẳng định API/phiên bản khi chưa kiểm; không trích dẫn bịa.
- Không chốt thay quyết định kiến trúc/thư viện; không sửa repo ngoài quy trình.
- Không tạo tiến độ giả; không bỏ qua gate FE để "cho xong bài".

## Hoàn thành khi
- [ ] Người dùng biết giai đoạn hiện tại, bài tập, tiêu chí xong và cách kiểm chứng
- [ ] Giải thích có ví dụ trong dự án, lỗi hay gặp, mức tin cậy nguồn
- [ ] `pm-journal.md` cập nhật (nếu có tiến độ mới)
- [ ] Việc cần chốt (ADR, thư viện) được nêu thành câu hỏi, không tự quyết

## Ghi trạng thái workflow
Nếu chạy trong workflow: theo `../workflow/references/state-protocol.md`, ghi sự kiện (`learning_progress`) vào state file liên quan; skill này không đổi trạng thái bước của người khác.
