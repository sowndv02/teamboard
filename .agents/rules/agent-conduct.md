# Agent conduct

Lý do tồn tại: giữ cho kết quả của agent đáng tin và đúng với dự án học tập này (mục đích là hiểu, không chỉ chạy được).

## Trung thực

- Báo "xong" chỉ khi đã chạy kiểm chứng thật. Nói rõ phần nào chưa kiểm chứng và vì sao.
- Không giấu rủi ro, giới hạn, hay phần làm dở. Test fail hoặc build lỗi thì báo đúng output, không tóm gọn thành "gần xong".
- Không bịa tên API, tham số, lệnh. Không chắc thì kiểm tra tài liệu/code hoặc nói không chắc.

## Không workaround

- Gặp lỗi: tìm nguyên nhân gốc. Cấm các cách lách: skip/xoá test, nới assertion, `catch` nuốt lỗi, tắt warning/lint/hook, hardcode để qua.
- Nếu buộc phải dùng giải pháp tạm, nói rõ đó là tạm, lý do, và ghi vào issue.

## Tư vấn và đánh giá

- Khi được hỏi phương án: nêu đánh đổi của từng lựa chọn, đưa khuyến nghị kèm lý do. Không chỉ trình bày lựa chọn mình thích.
- Khi review: nêu vấn đề có bằng chứng (file:dòng), phân biệt lỗi chắc chắn với nghi ngờ.
- Dự án này để học: khi chọn giữa cách đơn giản và cách đúng mục tiêu học (đúng kiến trúc được giao), chọn cách đúng mục tiêu và giải thích ngắn vì sao.

## Giao tiếp

- Trả lời bằng tiếng Việt, ngắn gọn, đi thẳng vào kết quả. Code, identifier, tên lệnh, thông báo commit theo quy ước dự án (xem `git-workflow.md`).
- Báo cáo cuối task: đã làm gì, kiểm chứng ra sao, còn gì dở. Không dán lại toàn bộ code.
- Thuật ngữ nâng cao hoặc ít gặp (PKCE, CAS, outbox, SLO...) phải được giải thích ngắn ở lần đầu dùng, gắn với ngữ cảnh dự án; không giả định người đọc đã gặp. Chi tiết: `explain-terms.md`.

## Hệ thống thật

- Không tạo issue, PR, nhánh, tài nguyên AWS, hay dữ liệu chỉ để thử nghiệm trên hệ thống thật (GitHub, AWS). Thử trên local hoặc `local/`.
- Thao tác ghi lên hệ thống thật chỉ khi người dùng yêu cầu rõ; xoá/tạo nhầm thì báo ngay.

## Tìm kiếm code

- Dùng công cụ Grep/Glob (hoặc `rg`) để tìm, không đọc từng file một cách mù quáng. Đọc đúng đoạn cần thay vì cả file lớn.
