---
key: TB-051
title: Khung lộ trình học Testing (TDD, BDD và chủ đề liên quan) theo Part 0–7 × L1–L8
status: in-progress
phase: 0
branch: feature/TB-051-learning-roadmap-testing-tdd-bdd
estimate: S
created: 2026-11-26
started: 2026-11-26
closed: null
---

# TB-051 — Khung lộ trình học Testing (TDD, BDD) theo Part 0–7 × L1–L8

## Bối cảnh
Người dùng muốn học TDD, BDD và các chủ đề kề cận (test double, test pyramid, ATDD, Specification by Example, contract test, mutation testing...). `references/topic-map.md` đã có dòng "Testing (TDD/BDD)" (khái niệm) nhưng chưa có ghi chú. Dự án vừa là sản phẩm vừa là bài học (xem `PROJECT.md`); quy trình dự án dùng TDD/BDD (`.agents/rules/testing.md`, `test-review.md`).

## Phạm vi
- Làm: dựng **khung** lộ trình khái niệm `document/learning/testing/` (README = Part 0 có ma trận phủ, `part-1-…md` đến `part-7-…md`), các mục `## Lx` đúng ma trận, nội dung ở trạng thái `TODO`; thêm dòng vào mục lục `document/learning/README.md`.
- Không làm: viết nội dung sâu, nghiên cứu nguồn, chạy lab (đợt `pack` riêng cho từng Part); thư mục `tools/` (xUnit, Reqnroll, Testcontainers, Stryker.NET: để sau); sửa `.agents/**` (kể cả `topic-map.md`, báo lại), `src/`, spec, ADR; commit/push khi chưa được yêu cầu.

## Tiêu chí hoàn thành (kiểm chứng được)
- [ ] Đủ 8 file: `README.md` và `part-1…7`
- [ ] Ma trận phủ ở Part 0 tuân quy tắc tối thiểu của `lesson-model.md`; ô `–` kèm lý do
- [ ] Mỗi ô `✓` có đúng một mục `## Lx` trong file Part tương ứng; mỗi file có hộp `> [!NOTE]`, "Tự kiểm tra", "Nguồn và độ tin cậy"
- [ ] Link `.md` chéo và neo không gãy
- [ ] Mục lục `document/learning/README.md` có dòng Testing (khung, Bản đồ giải pháp: chưa viết, real-world: Chưa đạt)

## Kế hoạch
1. Issue và nhánh (file này), worktree `H:\learning\board-tb-051`.
2. Dựng Part 0 và Part 1–7 (khung).
3. Cập nhật mục lục, kiểm link và ma trận.
4. Đợt sau: điền nội dung từng Part bằng `learning-hub` `pack`, bắt đầu Part 1 (có Bản đồ giải pháp).

## Ghi chú / Nhật ký
- 2026-11-26: Part 1 được điền nội dung theo yêu cầu người dùng (mở rộng phạm vi "khung" sang Part 1).
- 2026-11-26: Part 2 được điền nội dung theo yêu cầu người dùng.
- 2026-11-26: Part 3 được điền nội dung theo yêu cầu người dùng.
- 2026-11-26: Part 4 được điền nội dung theo yêu cầu người dùng.
- 2026-11-26: Part 5 được điền nội dung theo yêu cầu người dùng; Part 6–7 vẫn là khung. Lab 5.2 xác minh nguyên nhân làm tròn còn ⚠ ở Part 4 nên dòng ⚠ đó đã được đóng.
- 2026-11-26: Part 6 được điền nội dung theo yêu cầu người dùng; Part 7 vẫn là khung. Dự án chạy trong bản sao ngoài repo (`git archive`), không sửa `src/` hay spec; kịch bản S12 (BR-001 khoảng trắng) là đề xuất, chưa đưa vào repo.
- 2026-11-26: Part 7 được điền nội dung theo yêu cầu người dùng; cả bảy Part đã viết. Kiểm tra cuối: ma trận khớp ở cả 7 Part, link `.md` không gãy, không còn `TODO`. Phát hiện ngoài phạm vi: bảng "Bản đồ giải pháp" của Part 1 có 5 cột (quy tắc mẫu nói tối đa 4); `.feature` lặp step setup nhiều lần, chỉ 3 file dùng `Background`. Chưa sửa, báo lại.
- 2026-11-26: tạo từ nhánh `origin/main` trong worktree riêng vì nhánh TB-034 còn thay đổi dở.

## Bài học (lessons learned)
- không có
