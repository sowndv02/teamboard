# Bộ công cụ quản lý dự án (kinh điển, để học và áp dụng)

Dùng trong skill `project-manager` (đặc biệt `coach`). Đây là kiến thức tổng quát về quản lý dự án (Agile/Scrum/Kanban, PMBOK, sách nổi tiếng); **không phải số liệu thực tế của dự án**. Đối chiếu nguồn gốc khi học sâu (Scrum Guide, PMBOK, *The Mythical Man-Month*, *Peopleware*, *Making Work Visible*, *Accelerate*, *Project Management for the Unofficial Project Manager*...). Tên chuẩn và con số cụ thể: kiểm tra tài liệu hiện hành.

Mỗi kỹ thuật: **là gì → khi dùng → cách làm nhanh → cạm bẫy → ví dụ trong dự án này**.

## 1. Ưu tiên hoá

| Kỹ thuật | Là gì | Khi dùng | Cạm bẫy | Ví dụ (TeamBoard) |
|---|---|---|---|---|
| **MoSCoW** | Must / Should / Could / Won't (lần này) | Cắt phạm vi cho một mốc | Quá nhiều Must; Won't không được ghi | Giai đoạn 1: Must = gateway + identity I1–I2; Could = rate limit |
| **Giá trị so với công sức** (2×2) | Làm trước việc giá trị cao, công sức thấp | Chọn nhanh việc đầu tiên | Ước lượng giá trị mơ hồ | Gỡ `TODO` chặn nhiều việc: giá trị cao, công sức thấp |
| **RICE** | (Reach × Impact × Confidence) ÷ Effort | Nhiều ứng viên cần xếp hạng có số | Số giả chính xác; ít dùng cho dự án một người | Xếp bài tập học tuỳ chọn (gRPC, cache) |
| **WSJF** (Cost of Delay ÷ Duration) | Ưu tiên theo "cái giá của việc trễ" trên thời gian làm | Có phụ thuộc/thời hạn | Khó đo Cost of Delay | Việc mở khoá nhiều việc khác (BDD runner) |
| **Ma trận Eisenhower** | Khẩn × Quan trọng | Quản lý thời gian cá nhân | Ham việc khẩn bỏ việc quan trọng | Xoay mật khẩu (quan trọng, khẩn vừa) |
| **Kano** | Cơ bản / hiệu năng / hấp dẫn | Quyết định tính năng sản phẩm | Cần dữ liệu người dùng | Ít áp dụng cho dự án học |

Quy tắc: **chặn nhiều việc khác (đường găng) lên đầu**; luôn ghi **cái không làm** (Won't) để chống phình phạm vi.

## 2. Chia việc và định nghĩa "sẵn sàng/xong"

- **Chia lát dọc (vertical slice):** mỗi lát đi hết các tầng và giao được giá trị kiểm chứng; tránh chia theo tầng kỹ thuật ("làm hết DB trước"). Khớp `pipeline-lifecycle.md` (một issue một lát).
- **INVEST** cho mục backlog: Independent, Negotiable, Valuable, Estimable, Small, Testable.
- **Definition of Ready (DoR):** mục đã đủ để bắt đầu: tiêu chí hoàn thành rõ, phụ thuộc đã giải quyết, các `TODO` chặn đã chốt, ước lượng được.
- **Definition of Done (DoD):** gate xanh, test pass, review PASS, tài liệu cập nhật, bài học ghi (xem `code-change-gate.md`, `pipeline-lifecycle.md`).
- **Cạm bẫy:** mục quá lớn không bao giờ xong; DoD mơ hồ dẫn tới "gần xong" kéo dài.

## 3. Ước lượng

| Kỹ thuật | Cách làm | Ghi chú |
|---|---|---|
| **Kích thước tương đối** (S/M/L hoặc điểm Fibonacci) | So sánh với mục đã làm | Nhanh, tránh giả chính xác; hiệu chỉnh bằng dữ liệu |
| **Ba điểm (PERT)** | Lạc quan (O), khả dĩ (M), bi quan (P); kỳ vọng ≈ (O + 4M + P) ÷ 6 | Cho thấy độ bất định |
| **Khoảng + độ tin cậy** | "3–6 giờ, tin cậy 70%" | Trung thực hơn một con số |
| **Dự báo theo throughput** | Dùng số issue xong/tuần đã đo | Cần vài tuần dữ liệu |
| **Reference-class** | Nhìn các việc tương tự đã làm (nhóm tham chiếu) | Chống "thiên kiến lạc quan" |

Cạm bẫy: **ngụy biện lập kế hoạch** (planning fallacy, luôn lạc quan), **luật Hofstadter** ("luôn lâu hơn dự kiến, kể cả khi đã tính luật Hofstadter"), **luật Parkinson** (việc giãn ra bằng thời gian cho nó), **hình nón bất định** (đầu dự án ước lượng sai biên độ rộng), **luật Brooks** (thêm người vào dự án trễ làm nó trễ hơn; ít liên quan khi làm một mình, nhưng nhắc về chi phí phối hợp). Ghi **ước lượng vs thực tế** để hiệu chỉnh.

## 4. Lập lịch và phụ thuộc

- **Đường găng (critical path):** chuỗi việc phụ thuộc dài nhất; trễ một mắt = trễ cả. Tìm bằng vẽ sơ đồ phụ thuộc; giảm bằng cách song song hoá hoặc bỏ phụ thuộc.
- **Phụ thuộc cứng/mềm/ngoài:** cứng (B cần A xong), mềm (nên làm A trước), ngoài (giấy phép, công cụ, quyết định của người khác). Phụ thuộc ngoài là nguồn rủi ro lớn nhất.
- **Mốc (milestone):** điểm kiểm chứng được, không phải "thời điểm"; gắn tiêu chí hoàn thành.
- **Buffer:** đệm theo mốc/dự án, không đệm ẩn trong từng việc; theo dõi mức tiêu thụ đệm.
- **Burn-up** (công việc hoàn thành vs tổng phạm vi): hiện cả tiến độ và phạm vi tăng; tốt hơn burn-down vì thấy phình phạm vi.

## 5. Quản lý luồng (Kanban) và nhịp (Scrum)

- **Kanban:** hình dung việc (cột: Backlog → Sẵn sàng → Đang làm → Review → Xong), **giới hạn WIP**, đo **cycle time**, cải tiến luồng. Hợp dự án một người, việc đến không đều.
- **Định luật Little:** thời gian chờ trung bình ≈ WIP ÷ throughput → **giảm WIP thì nhanh hơn**.
- **Scrum:** sprint cố định, sự kiện (planning, daily, review, retro), vai trò (PO, SM, đội). Với người học một mình, dùng **phiên bản rút gọn**: kế hoạch tuần + review + retro ngắn.
- **Cạm bẫy:** WIP không giới hạn (làm dang dở mọi thứ), sprint nhồi quá năng lực, bỏ retro.

## 6. Rủi ro (RAID)

- **Ma trận xác suất × tác động** (thấp/vừa/cao) để xếp ưu tiên.
- **Chiến lược đối phó:** **tránh** (đổi kế hoạch để rủi ro không còn), **giảm** (giảm xác suất/tác động), **chuyển** (bảo hiểm, nhà cung cấp), **chấp nhận** (có chủ ý, có ngưỡng và kế hoạch dự phòng).
- **Dấu hiệu cảnh báo sớm (trigger)** và **người chịu trách nhiệm**; xem lại định kỳ.
- **Assumptions** (giả định) là rủi ro chưa kiểm chứng: ghi và xác nhận sớm.
- **Issues:** rủi ro đã xảy ra; xử lý, rồi rút bài học (`lesson-learned`).
- **Dependencies:** phụ thuộc ngoài; ai cung cấp, khi nào, nếu không có thì sao.
- **Pre-mortem:** "dự án thất bại sau 3 tháng, vì sao?"; tìm rủi ro trước.

## 7. Bên liên quan và giao tiếp

- **Phân tích bên liên quan:** quyền lực × quan tâm → quản lý chặt / giữ hài lòng / thông tin / theo dõi.
- **RACI:** Responsible (làm), Accountable (chịu trách nhiệm cuối, một người), Consulted (hỏi ý kiến), Informed (được báo). Với dự án một người, vẫn dùng để hình dung vai trò (chủ dự án, agent, người duyệt).
- **Kế hoạch giao tiếp:** ai nhận gì, tần suất, kênh; báo cáo ngắn, có quyết định cần đưa ra.
- **Báo cáo RAG:** Xanh (đúng hướng), Vàng (có rủi ro cần chú ý, có hành động), Đỏ (cần quyết định/trợ giúp); mỗi màu có **bằng chứng**; tránh "mãi vàng" mà không hành động ("watermelon status": xanh ngoài, đỏ trong).
- **Tin xấu:** báo sớm, kèm lựa chọn và khuyến nghị.

## 8. Retro và cải tiến

| Khung | Câu hỏi |
|---|---|
| **Start / Stop / Continue** | Nên bắt đầu, dừng, tiếp tục điều gì |
| **4Ls** | Liked, Learned, Lacked, Longed for |
| **Sailboat** | Gió (đẩy tới), neo (kéo lại), đá ngầm (rủi ro), đảo (mục tiêu) |
| **5 Whys** | Hỏi "vì sao" lặp để tới nguyên nhân gốc (dùng trong `lesson-learned`) |

Quy tắc: **ít hành động, cụ thể, có người chịu trách nhiệm, hạn, cách kiểm chứng**; kiểm hành động kỳ trước. Cải tiến hệ thống > nhắc nhở.

## 9. Kiểm soát thay đổi và phạm vi

- **Scope creep:** phạm vi phình dần không kiểm soát. Đối phó: mọi thay đổi qua quy trình `change`, đổi cái gì ra, ghi quyết định.
- **Tam giác sắt:** Phạm vi – Thời gian – Chi phí (và Chất lượng làm trung tâm); đổi một cạnh ảnh hưởng cạnh khác; **chất lượng không nên là biến để hy sinh ngầm**.
- **Sunk cost fallacy:** đừng tiếp tục chỉ vì đã đầu tư; quyết theo giá trị còn lại.
- **Dừng/xoay hướng (pivot/kill):** có tiêu chí rõ để dừng một hướng.
- **Nợ kỹ thuật:** ghi và lên kế hoạch trả, không để vô hình.

## 10. Mục tiêu và đo lường

- **SMART:** Cụ thể, Đo được, Khả thi, Liên quan, Có thời hạn.
- **OKR (Objectives & Key Results):** mục tiêu định tính + kết quả then chốt đo được; ít, tham vọng vừa phải. Hợp để định nghĩa **kết quả học tập**.
- **Điều lệ dự án (charter):** mục tiêu, phạm vi, tiêu chí thành công, ràng buộc, bên liên quan, rủi ro chính (một trang).
- **DORA/Accelerate** (nếu muốn học): tần suất triển khai, lead time thay đổi, tỷ lệ lỗi thay đổi, thời gian khôi phục; chỉ mang tính tham khảo cho dự án một người.
- **Đo cái ra quyết định được**, không đo cho đẹp.

## 11. Các mẫu sai lầm thường gặp

- Kế hoạch chi tiết ngay từ đầu rồi cứng nhắc (thay vì lập kế hoạch cuốn chiếu: rõ gần, mơ hồ xa).
- Không có tiêu chí hoàn thành → "xong 90%" mãi.
- Làm quá nhiều việc cùng lúc (WIP cao).
- Giấu rủi ro đến khi quá muộn.
- Phụ thuộc ngoài không được theo dõi.
- Đo vanity metrics.
- Họp/hồ sơ nhiều hơn giá trị tạo ra.
