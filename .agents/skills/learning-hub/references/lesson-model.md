# Mô hình bài học: lộ trình hai chiều

Một chủ đề lớn được dạy như **một lộ trình**, không phải một ghi chú. Lộ trình có hai chiều:

- **Chiều sâu: 8 tầng (L1–L8).** Mỗi nội dung đi từ "là gì" tới "tự thiết kế được".
- **Chiều ngang: 8 Part (0–7).** Mỗi Part là một nhóm kiến thức; trong mỗi Part, nội dung được xếp theo các tầng L phù hợp.

## Chiều sâu: 8 tầng

| Tầng | Câu hỏi cần trả lời | Bằng chứng người học cần thấy |
|---|---|---|
| L1 What | Khái niệm này là gì? | Định nghĩa một dòng, sơ đồ, ví dụ đời thường |
| L2 Why | Tại sao cần nó? Giải quyết vấn đề gì? | Một tình huống đau thật khi chưa có nó |
| L3 How | Cơ chế bên trong hoạt động thế nào? | Luồng dữ liệu/xử lý từng bước, không chỉ tên gọi |
| L4 Hands-on | Tự triển khai bằng lệnh hoặc cấu hình thế nào? | Lab đánh số: mục tiêu, lệnh, **đầu ra thật**, dọn dẹp |
| L5 Troubleshooting | Khi hỏng, kiểm tra và tìm root cause ra sao? | Triệu chứng → lệnh kiểm tra → nguyên nhân gốc → cách sửa |
| L6 Production | Bảo mật, hiệu năng, độ tin cậy, chi phí cần cân nhắc gì? | Danh sách quyết định kèm đánh đổi |
| L7 Architecture | Khi nào chọn giải pháp này thay vì giải pháp khác? | So sánh với lựa chọn khác, tiêu chí chọn |
| L8 Mastery | Có tự thiết kế, giải thích trade-off, xử lý tình huống lạ không? | Bài tập mở có tiêu chí chấm, không có đáp án duy nhất |

## Chiều ngang: 8 Part

Tên Part theo chủ đề; dưới đây là khung chung (ví dụ Docker trong ngoặc).

| Part | Nội dung | Ví dụ với Docker |
|---|---|---|
| 0 Roadmap & Assessment | Mục tiêu, prerequisite, các level, ma trận phủ, tiêu chí hoàn thành, cách tự đánh giá | `README.md` của lộ trình |
| 1 Fundamentals & Mental Models | Mô hình tư duy nền tảng | Container vs VM, kiến trúc Docker, namespaces, cgroups, vòng đời |
| 2 Core Engineering Skills | Kỹ năng lõi dùng hằng ngày | CLI, Dockerfile, layer, mạng, lưu trữ, cổng, môi trường, log |
| 3 Multi-component Applications | Ghép nhiều thành phần | Compose, service discovery, phụ thuộc, healthcheck, cấu hình, secret |
| 4 Advanced Engineering | Kỹ thuật nâng cao | BuildKit, multi-stage, cache, đa nền tảng, hiệu năng, tài nguyên |
| 5 Production, Security & Operations | Vận hành thật | Least privilege, quét image, chuỗi cung ứng, quan sát, debug, CI/CD, rollback |
| 6 Real-world Projects | Dự án tích hợp kiến thức | Từ ứng dụng đơn giản tới triển khai cloud |
| 7 Troubleshooting, Interview & Mastery | Sự cố thật, system design, phỏng vấn, kiểm tra tổng hợp | Kịch bản sự cố, câu hỏi thiết kế, bài kiểm tra |

## Ma trận phủ (đặt trong Part 0)

Hàng là Part, cột là L1…L8. Ô **✓** nghĩa là Part đó có mục tương ứng; ô **–** nghĩa là không áp dụng và **phải kèm lý do ngắn**. Ma trận là cam kết: mỗi ô ✓ phải có đúng một mục `## Lx` trong file Part.

```
          L1 L2 L3 L4 L5 L6 L7 L8
Part 1    ✓  ✓  ✓  ✓  –  –  ✓  –
Part 2    ✓  ✓  ✓  ✓  ✓  –  –  –
...
```

**Quy tắc tối thiểu để không điền cho đủ:**
- Part 1 đến Part 5 có L1, L3, L4.
- Part 1 và Part 2 có L2.
- Part 2 đến Part 5 có L5.
- Part 5 có L6.
- Part 1, 4, 5 và 7 có L7.
- Part 6 (dự án) có L4; L1 chỉ để mô tả dự án.
- Part 7 có L5, L7 và L8.
- Ô còn lại: ✓ nếu có nội dung thật, nếu không thì **–** kèm lý do. Không viết mục rỗng hay mục lặp lại để lấp ô.

## Khi nào dùng mô hình đầy đủ

| Loại chủ đề | Hình thức |
|---|---|
| Công cụ lớn (Docker, Kubernetes, Terraform...) | Thư mục `document/learning/<chủ-đề>/` gồm `README.md` (Part 0) và `part-1-…md` đến `part-7-…md` |
| Khái niệm gốc | Tối thiểu Part 0, Part 1 (nền tảng, **Bản đồ giải pháp**, từng cơ chế) và Part 7 (đánh đổi kiến trúc, mastery); thêm Part khác khi cần |
| Chủ đề nhỏ | Một file theo khuôn rút gọn (mẫu 10 mục trong `SKILL.md`) cho tới khi người dùng duyệt chuyển sang mô hình này |

## Phân tầng khái niệm và công cụ trong lộ trình

- Lộ trình **công cụ** có dòng "Một trong các lựa chọn của [khái niệm]" ở Part 0, và mục **"Phạm vi và những gì <công cụ> KHÔNG đại diện"** trong Part 1 (L7) và Part 4.
- Lộ trình **khái niệm** có "Bản đồ giải pháp" (≥3 lựa chọn, kể cả "không dùng") trong Part 1 (L7), và mỗi cơ chế là một thẻ riêng trong L1–L3.
- Công cụ không lặp phần chung của khái niệm; chỉ trỏ tới.

## Yêu cầu cho từng tầng

- **L4:** mỗi lab có mục tiêu, lệnh, **đầu ra thật** (ghi môi trường và ngày) hoặc gắn ⚠ chưa chạy, và bước dọn dẹp chỉ động tới tài nguyên đã tạo.
- **L5:** mỗi sự cố có triệu chứng quan sát được, lệnh kiểm tra, nguyên nhân gốc (không dừng ở bề mặt) và cách sửa.
- **L6:** nêu rõ đánh đổi (bảo mật vs tiện, hiệu năng vs chi phí), không chỉ liệt kê khuyến nghị.
- **L7:** so với lựa chọn khác theo tiêu chí, nói khi nào **không** nên dùng.
- **L8:** bài tập mở kèm tiêu chí chấm; đáp án gợi ý nêu các hướng hợp lệ chứ không một đáp án duy nhất.

## Tiêu chí hoàn thành một lộ trình

- Ma trận phủ khớp nội dung (kiểm bằng script).
- Mọi lệnh L4/L5 đã chạy thật hoặc gắn ⚠.
- Mỗi Part có "Tự kiểm tra" với đáp án ngắn đúng và mục "Nguồn và độ tin cậy".
- Mọi thẻ khái niệm có **Use case**; real-world có ≥3 case có nguồn đã mở (xem `SKILL.md` mục 7).
