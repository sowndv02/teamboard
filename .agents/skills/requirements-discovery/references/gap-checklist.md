# Checklist phân tích khoảng trống yêu cầu

Dùng ở bước 3 của skill. Với mỗi dòng đánh dấu **Biết / Giả định / Chưa biết**. Chỉ "Chưa biết" tác động lớn mới thành câu hỏi.

| # | Chiều | Câu hỏi gợi ý | Tác động nếu bỏ sót |
|---|---|---|---|
| 1 | **Mục tiêu & giá trị** | Vấn đề gì cần giải? Thành công đo bằng gì? Nếu không làm thì sao? | Làm sai hướng, không biết khi nào xong |
| 2 | **Người dùng & vai trò** | Ai dùng? Có vai trò nào khác nhau? Tần suất, mức thành thạo? | Sai phân quyền |
| 3 | **Phạm vi** | Trong lần này gồm gì, **không** gồm gì? Có giai đoạn sau? | Phình việc, hiểu lầm kỳ vọng |
| 4 | **Dữ liệu** | Thực thể nào, thuộc tính, ràng buộc (bắt buộc, duy nhất, độ dài)? Nguồn? Lưu bao lâu? | Sai mô hình, migration đau |
| 5 | **Quy tắc nghiệp vụ** | Điều kiện, tính toán, trạng thái và chuyển trạng thái hợp lệ, ngoại lệ? | Lỗi logic khó sửa |
| 6 | **Luồng chính** | Các bước từ đầu đến cuối? Ai làm bước nào? | Thiếu chức năng |
| 7 | **Luồng thay thế & lỗi** | Dữ liệu sai, trùng, thiếu quyền, hết hạn, huỷ giữa chừng thì sao? | Trải nghiệm tệ, lỗi vận hành |
| 8 | **Đồng thời & nhất quán** | Hai người sửa cùng lúc? Thao tác lặp lại (bấm hai lần)? Thứ tự? | Mất dữ liệu, trùng bản ghi |
| 9 | **Phân quyền & bảo mật** | Ai xem/sửa/xoá? Dữ liệu nhạy cảm/PII? Cần ghi vết (audit)? | Lộ dữ liệu, vi phạm quy định |
| 10 | **Tích hợp & sự kiện** | Hệ thống/dịch vụ nào liên quan? Cần thông báo (email, webhook)? Ai phát/nhận sự kiện? | Thiếu liên kết, sai ranh giới service |
| 11 | **Phi chức năng** | Số người dùng, số bản ghi, độ trễ chấp nhận, sẵn sàng, khôi phục sự cố? | Không đạt hiệu năng/độ tin cậy |
| 12 | **Giao diện & trải nghiệm** | Chưa áp dụng ở giai đoạn này (không có frontend, ADR 0008); nếu yêu cầu có phần giao diện, ghi vào Ngoài phạm vi hoặc Câu hỏi mở | Làm lại khi có frontend |
| 13 | **Vận hành & quan sát** | Cần báo cáo, số liệu, cảnh báo? Ai hỗ trợ khi lỗi? | Không theo dõi được |
| 14 | **Di chuyển & tương thích** | Dữ liệu cũ cần chuyển? Có hệ thống cũ chạy song song? | Mất dữ liệu, gián đoạn |
| 15 | **Ràng buộc** | Hạn chót, ngân sách, quy định, công nghệ bắt buộc, môi trường triển khai? | Phương án không khả thi |
| 16 | **Tiêu chí chấp nhận** | Với mỗi chức năng: tình huống mẫu nào chứng minh "đạt"? Ví dụ số liệu cụ thể? | Không kiểm chứng/nghiệm thu được |

## Phân loại mỗi thông tin tìm được

Ghi mỗi mục vào đúng một nhóm: **yêu cầu** (có nguồn từ khách), **giả định** (chưa xác nhận: cần owner + hạn + mặc định), **ràng buộc** (không thương lượng: hạn, ngân sách, quy định, công nghệ bắt buộc), hoặc **ngoài phạm vi** (khách có thể mong nhưng lần này không làm). Câu hỏi mở nào chặn estimate/thiết kế thì đặt hạn sớm nhất.

## Dấu hiệu yêu cầu còn mơ hồ

- Từ định tính không có số đo: "nhanh", "dễ dùng", "linh hoạt", "thân thiện", "nhiều người".
- Từ bao quát: "tất cả", "luôn", "không bao giờ", "mọi trường hợp".
- Đại từ/chủ ngữ ẩn: "hệ thống sẽ tự động..." (ai/khi nào/theo quy tắc nào?).
- Giải pháp đội lốt yêu cầu: "dùng Redis", "làm dashboard", "thêm nút X" (hỏi: để giải quyết điều gì?).
- Thiếu người dùng, thiếu "để làm gì", hoặc thiếu trường hợp lỗi.
- Hai yêu cầu có thể mâu thuẫn (ví dụ "xoá được" và "giữ lịch sử đầy đủ").

## Kỹ thuật hỏi

- **Ví dụ cụ thể**: "Cho tôi một ví dụ thật gần đây."
- **Tình huống biên**: "Nếu ... xảy ra thì mong muốn gì?"
- **Ưu tiên bắt buộc**: "Nếu chỉ làm được một việc trong lần này, là việc nào?"
- **Đối chiếu hiện trạng**: "Hiện đang làm bằng cách nào? Điều gì phiền nhất?"
- **Loại trừ**: "Có điều gì chắc chắn **không** cần?"
- **Số đo**: "Bao nhiêu thì đủ/quá nhiều?"
