# Tình huống mô phỏng cho PM (skill `project-manager`, chế độ `simulate`)

Mục đích: luyện **ra quyết định quản lý** dưới áp lực, không có đáp án duy nhất. Agent đóng vai bên liên quan (nói rõ đang mô phỏng); người dùng trả lời như PM; agent phản hồi theo mẫu cuối file.

Có thể dựng kịch bản từ tình huống thật của dự án (có `TODO` chặn, giấy phép đổi, chi phí vượt). Mỗi kịch bản nêu: **bối cảnh → lời nhân vật → câu hỏi cho PM → kỹ thuật liên quan** (xem `pm-toolkit.md`).

## Danh sách kịch bản

### S1. Khách thêm yêu cầu giữa chừng (scope creep)
- **Bối cảnh:** đang làm Giai đoạn 1 (gateway + identity). Khách: "Tiện thể thêm đăng nhập bằng Google và gửi email xác nhận nhé, chắc không mất nhiều đâu."
- **Hỏi PM:** bạn trả lời gì? Tác động tới mốc, chi phí, rủi ro? Đổi cái gì ra?
- **Kỹ thuật:** kiểm soát thay đổi, tam giác sắt, MoSCoW, tác động lên spec/design đã `confirmed`.

### S2. Ép đẩy sớm mốc
- **Bối cảnh:** "Phải demo luồng tạo board và task trong 2 tuần thay vì 5 tuần."
- **Hỏi PM:** những lựa chọn nào (cắt phạm vi, bỏ chất lượng, thêm người, dời demo)? Bạn sẽ không thoả hiệp điều gì (bảo mật, test)?
- **Kỹ thuật:** đường găng, cắt phạm vi, chất lượng không phải biến để hy sinh ngầm, luật Brooks.

### S3. Xung đột ưu tiên giữa hai bên liên quan
- **Bối cảnh:** một bên muốn hoàn thiện hạ tầng AWS trước, bên kia muốn có thêm tính năng nghiệp vụ chạy được trước.
- **Hỏi PM:** bạn ra quyết định thế nào, dựa vào tiêu chí nào? Làm sao thông báo cho bên không được ưu tiên?
- **Kỹ thuật:** phân tích bên liên quan, giá trị so với công sức, WSJF, thoả thuận tiêu chí.

### S4. Chi phí cloud vượt dự kiến
- **Bối cảnh:** hoá đơn tuần đầu cao gấp 3 so với ước tính do NAT Gateway và RDS chạy liên tục.
- **Hỏi PM:** việc đầu tiên bạn làm? Điều tra gì? Chỉnh kế hoạch thế nào? Ghi bài học gì?
- **Kỹ thuật:** xử lý Issue, RAID, `lesson-learned`, `aws-service-selection`, kiểm soát chi phí.

### S5. Thư viện then chốt đổi giấy phép
- **Bối cảnh:** một thư viện đang dùng ở nhiều chỗ chuyển sang giấy phép thương mại.
- **Hỏi PM:** đánh giá tác động, các lựa chọn (đóng băng, thay thế, tự viết, trả phí), ai quyết?
- **Kỹ thuật:** quản lý phụ thuộc, `dependency-policy.md`, phân tích lựa chọn.

### S6. Mốc sắp trễ, chưa ai muốn nói
- **Bối cảnh:** tiến độ đang vàng nhưng báo cáo đang xanh; còn 3 ngày trước mốc, còn 40% việc.
- **Hỏi PM:** bạn báo cáo thế nào, với ai, kèm lựa chọn nào? Điều này nói gì về văn hoá báo cáo?
- **Kỹ thuật:** báo cáo RAG trung thực, tin xấu sớm, watermelon status.

### S7. Phát hiện lỗi nghiêm trọng gần mốc
- **Bối cảnh:** coding-review tìm ra lỗ hổng truy cập chéo dữ liệu (IDOR) hai ngày trước demo.
- **Hỏi PM:** hoãn demo hay vá nhanh? Rủi ro mỗi lựa chọn? Làm gì để lỗi này không lặp lại?
- **Kỹ thuật:** đánh đổi chất lượng–thời gian, bảo mật không thoả hiệp, `lesson-learned`.

### S8. Yêu cầu quá mơ hồ để ước lượng
- **Bối cảnh:** "Làm chức năng báo cáo cho tốt nhé." Khách hỏi bao giờ xong.
- **Hỏi PM:** bạn làm gì trước khi trả lời? Khi nào ước lượng được?
- **Kỹ thuật:** hình nón bất định, khai phá yêu cầu (`requirements-discovery`), ước lượng theo khoảng, spike.

### S9. Năng lực giảm (người học bận)
- **Bối cảnh:** tuần này chỉ còn một nửa thời gian khả dụng, nhưng sprint đã cam kết đầy.
- **Hỏi PM:** cắt gì, hoãn gì, thông báo thế nào? Điều chỉnh kỳ vọng ra sao?
- **Kỹ thuật:** quản lý năng lực, WIP limit, kế hoạch cuốn chiếu, ưu tiên MoSCoW.

### S10. Tranh luận kỹ thuật kéo dài (phân tích tê liệt)
- **Bối cảnh:** tranh cãi 2 tuần giữa tự xây identity hay dùng Cognito, chưa ai chốt.
- **Hỏi PM:** bạn phá thế bế tắc thế nào? Tiêu chí quyết định? Thời hạn quyết định?
- **Kỹ thuật:** quyết định có thời hạn (timebox), ADR, tiêu chí đánh đổi (`system-design`), "disagree and commit".

### S11. Một thành viên (agent) đi lệch phạm vi
- **Bối cảnh:** trong lúc làm issue, agent tự thêm tính năng ngoài plan.
- **Hỏi PM:** xử lý thế nào ngay lúc đó? Điều chỉnh quy trình ra sao? Có lesson không?
- **Kỹ thuật:** kiểm soát phạm vi, hợp đồng phạm vi (`generation-principles.md`), retro, `lesson-learned`.

### S12. Dự án quá rộng so với nguồn lực
- **Bối cảnh:** 6 service + Terraform + observability cho một người học trong vài tháng.
- **Hỏi PM:** bạn cắt/hoãn gì? Tiêu chí? Làm sao giữ mục tiêu học?
- **Kỹ thuật:** MoSCoW, lát dọc, mốc học tập, "đủ tốt trước", chấp nhận Won't.

## Mẫu phản hồi của agent sau mỗi lượt

1. **Tóm tắt quyết định** của người dùng (một câu).
2. **Điểm tốt** (cụ thể).
3. **Rủi ro/đánh đổi chưa nêu** hoặc câu hỏi một PM giỏi sẽ hỏi thêm.
4. **Cách tiếp cận thường gặp** (nêu **đánh đổi**, không phải đáp án duy nhất), kèm kỹ thuật liên quan (`pm-toolkit.md` mục nào).
5. **Một bài tập nhỏ** tiếp theo hoặc câu hỏi đào sâu.
6. Ghi vào `pm-journal.md` (khái niệm, điều học được, điều còn mơ hồ).

Quy tắc: phản hồi mang tính **dạy**, không phán xét; nói rõ khi chuyển từ mô phỏng sang việc thật.
