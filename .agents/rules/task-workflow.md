# Task workflow (vibe code / làm task)

Thứ tự giai đoạn từ issue đến merge và các cổng chuyển: `pipeline-lifecycle.md`. File này là vòng lặp thao tác bên trong mỗi lát.

Vòng lặp bắt buộc cho mọi task:

1. **Hiểu**: xác định issue (`document/issues/`). Chưa có thì tạo từ `_template.md` trước. Đọc tài liệu theo `INDEX.md`.
2. **Kế hoạch**: task nhiều bước hoặc chạm nhiều service → nêu kế hoạch ngắn, chờ người dùng duyệt (plan mode). Task nhỏ một file thì làm luôn.
3. **Làm một lát nhỏ chạy được end-to-end** theo TDD/BDD (kịch bản → review `test-review.md` → đỏ → xanh → refactor), không dựng nhiều thứ cùng lúc. Một lát = build xanh + có cách kiểm chứng.
4. **Kiểm chứng thật**: qua `code-change-gate.md` (lệnh ở `COMMANDS.md` §2) và tiêu chí hoàn thành của issue. Báo "xong" chỉ khi đã chạy; nếu không chạy được, nói rõ phần nào chưa kiểm chứng.
5. **Cập nhật**: tick tiêu chí trong issue, ghi nhật ký, cập nhật docs/config/ADR bị ảnh hưởng, đổi trạng thái issue.
6. **Báo cáo ngắn**: đã làm gì, kiểm chứng ra sao, còn gì dở. Không dán lại toàn bộ code.

## Ranh giới

- Chỉ làm đúng phạm vi issue. Thấy việc khác cần làm → ghi vào mục Ghi chú của issue hoặc đề xuất issue mới, không tự làm.
- Không tự thêm tính năng, abstraction, dependency "cho chắc".
- Không sửa file ngoài phạm vi (đặc biệt `.gitignore`, CI, compose dùng chung) trừ khi issue yêu cầu.

## Khi vướng

- Test fail hoặc build lỗi: tìm nguyên nhân gốc. Không xoá/skip test, không comment code để qua.
- Thử cách khác 2 lần không được → dừng, nói rõ đã thử gì và hỏi người dùng.
- Yêu cầu mơ hồ hoặc tài liệu mâu thuẫn → hỏi, không đoán.

## Hoàn thành khi

- [ ] Tiêu chí của issue đều đạt và đã kiểm chứng bằng lệnh thật
- [ ] Build + test liên quan xanh
- [ ] Không có secret, file sinh ra, hay thay đổi ngoài phạm vi trong diff
- [ ] Docs/config/ADR/issue đã cập nhật
