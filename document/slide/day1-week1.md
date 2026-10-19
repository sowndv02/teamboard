# Xoá mù chữ Cloud — Slide day 1 / week 1

---

<!-- Slide 1 -->

**Team SBU3 · Kaopiz**

# Xoá mù chữ Cloud

Lộ trình 6 tuần: Docker · AWS · CI/CD · System design

NamNT · Khởi động: [ngày bắt đầu]

> **Ghi chú diễn giả:** (~1 phút) Chào cả nhà. Hôm nay mình dành khoảng 15–20 phút để giới thiệu một khoá học nội bộ mà mình tạm đặt tên là "Xoá mù chữ Cloud". Khoá này không phải mình tự nghĩ ra. Nó được xây từ chính kết quả khảo sát mà mọi người điền tuần trước. Mình đọc kỹ từng phiếu, và thấy có một điểm chung rất rõ, lát nữa mình sẽ cho mọi người xem. Hôm nay mình sẽ đi qua 4 phần: vì sao có khoá học, khoá học dạy gì, học như thế nào, và mọi người cần chuẩn bị gì. Cuối buổi có phần hỏi đáp, nên có gì thắc mắc mọi người cứ ghi lại. Chuyển: Trước tiên, mình đi vào lý do.

---

<!-- Slide 2 -->

*Vì sao có khoá học này*

## Cả team muốn học đúng những mảng mình đang yếu nhất

| Số người | Muốn học | Điểm tự chấm TB |
|---|---|---|
| 8/8 | Cloud | 1,4 / 5 |
| 7/8 | System design | 1,6 / 5 |
| 6/8 | DevOps / CI-CD | 1,8 / 5 |

Nhiều bạn chia sẻ: dự án hiện tại ít cơ hội chạm vào web và cloud, công việc dễ lặp lại, và lo bị ngợp khi chuyển sang dự án mới.

> **Ghi chú diễn giả:** (~2 phút) Đây là 3 con số mình thấy nổi bật nhất trong 8 phiếu khảo sát. Cả 8/8 người đều muốn học Cloud, nhưng điểm tự chấm trung bình chỉ 1,4 trên 5. 7/8 người muốn học System design, điểm 1,6. 6/8 người muốn học DevOps, CI/CD, điểm 1,8. Nghĩa là: thứ mọi người muốn học nhất lại đúng là thứ team đang yếu nhất. Đây là tín hiệu rất tốt, vì động lực đã có sẵn, chỉ thiếu một lộ trình. Ở phần góp ý, nhiều bạn cũng chia sẻ khá thật: dự án hiện tại ít cơ hội chạm vào web và cloud, công việc dễ lặp lại, và lo bị ngợp khi chuyển sang dự án mới. Mình đọc và thấy hoàn toàn hợp lý. Đây không phải lỗi của ai, mà do đặc thù dự án. Việc của mình là tạo cơ hội để mọi người bù lại phần đó. Chuyển: Vậy học xong thì mọi người sẽ làm được gì?

---

<!-- Slide 3 -->

*Mục tiêu sau 6 tuần*

## Mỗi người tự đưa một app ASP.NET Core lên AWS bằng Docker và CI/CD, rồi bảo vệ được thiết kế hệ thống của nó trước team.

> **Ghi chú diễn giả:** (~1 phút, đọc chậm câu trên slide) Nếu hôm nay mọi người chỉ nhớ một câu, thì là câu này. [Đọc to câu trên slide.] Mình muốn nhấn vào 3 chữ. "Mỗi người": không phải cả team làm chung rồi một người hiểu, mà từng người tự làm được. "Tự đưa": không phải xem tutorial cho biết, mà tự tay deploy, gặp lỗi và tự gỡ. "Bảo vệ được": không chỉ làm cho chạy, mà phải giải thích được vì sao chọn thiết kế đó, đánh đổi những gì. Sau 6 tuần, mỗi người sẽ có một sản phẩm thật trên repo của mình để chỉ vào và nói "cái này mình làm". Cái đó có giá trị hơn nhiều so với một chứng chỉ. Chuyển: Để làm được vậy, cách học sẽ hơi khác so với tự đọc tài liệu.

---

<!-- Slide 4 -->

*Cách học*

## Học bằng cách làm thật trên một app chung

- **Một app xuyên suốt:** Cả team cùng xây TeamBoard (ASP.NET Core + EF Core + SQL Server). Mỗi tuần thêm một lớp hạ tầng lên chính app đó.
- **Khoảng 70% thực hành:** Đọc và xem ít, tự làm nhiều. Mỗi thứ Bảy có một buổi lab dài để ghép các phần trong tuần thành sản phẩm.
- **Repo riêng, nộp bằng PR:** Mỗi người một repo GitHub. Bài tuần nào nộp bằng Pull Request tuần đó và có người khác review.

> **Ghi chú diễn giả:** (~2 phút) Khoá học có 3 nguyên tắc. Thứ nhất, một app xuyên suốt. Cả team cùng xây TeamBoard, một app quản lý task rất đơn giản, chỉ có Board và Task. Mình cố tình chọn app nhỏ để mọi người không mất thời gian vào nghiệp vụ, mà tập trung vào hạ tầng. Mỗi tuần thêm một lớp lên chính app đó: tuần 1 đóng gói Docker, tuần 2–3 đưa lên AWS, tuần 4–5 tự động hoá, tuần 6 thiết kế lại cho quy mô lớn. Cuối khoá mọi người sẽ thấy toàn bộ bức tranh từ code đến production. Thứ hai, khoảng 70% thực hành. Đọc và xem ít thôi, chủ yếu là tự làm. Cloud và DevOps là thứ phải làm mới nhớ, đọc 10 bài không bằng tự deploy hỏng 1 lần. Thứ ba, mỗi người một repo, nộp bằng Pull Request. Vừa để có người review, vừa để mọi người quen quy trình làm việc chuẩn như dự án thật. Chuyển: Cụ thể 6 tuần sẽ học những gì?

---

<!-- Slide 5 -->

*Lộ trình*

## 6 tuần, mỗi tuần một giai đoạn và một mốc

| Tuần | Giai đoạn | Nội dung | Mốc |
|---|---|---|---|
| 1 · ~18h | Docker | Dockerfile, Compose, SQL Server, Redis | chạy bằng docker compose |
| 2 · ~13h | AWS cơ bản | IAM, VPC, RDS, ECR, App Runner | app live trên AWS |
| 3 · ~13h | AWS vận hành | Secrets Manager, S3, CloudWatch, alarm | không lộ secret, có alarm |
| 4 · ~13h | CI/CD | GitHub Actions, OIDC, staging, duyệt production | merge là lên staging |
| 5 · ~13h | IaC | AWS CDK viết bằng C#, diễn tập sự cố | dựng lại môi trường từ code |
| 6 · ~18h | System design | Scale, cache, queue, capstone | bảo vệ thiết kế trước team |

> **Ghi chú diễn giả:** (~2–3 phút, đi từ trái sang phải) Đây là toàn bộ lộ trình. Mỗi tuần một chủ đề, và cuối mỗi tuần có một mốc cụ thể để biết mình đã qua hay chưa. Tuần 1, Docker: đóng gói app cùng SQL Server và Redis, chạy được bằng một lệnh docker compose. Tuần 2, AWS cơ bản: IAM, mạng VPC, database RDS, đưa app lên App Runner. Mốc là app chạy thật trên AWS, có URL. Tuần 3, AWS vận hành: quản lý secret, lưu file lên S3, giám sát bằng CloudWatch. Mốc là không còn mật khẩu nào trong code và có cảnh báo khi app lỗi. Tuần 4, CI/CD: merge code là tự động lên staging, lên production phải qua bước duyệt. Tuần 5, hạ tầng dạng code: viết hạ tầng bằng CDK, mà CDK viết được bằng C#, rất hợp với team mình. Có cả buổi diễn tập sự cố. Tuần 6, System design: học cách thiết kế cho quy mô lớn, rồi mỗi người bảo vệ một bản thiết kế. Lưu ý: tuần 2 đến 5 là phần mới với đa số mọi người nên mình đã dàn mỏng còn khoảng 13 giờ/tuần, để có thời gian gỡ lỗi. Các tuần nối tiếp nhau, nên chưa qua mốc tuần trước thì chưa sang tuần sau. Chuyển: Nghe thì nhiều, vậy một tuần cụ thể sẽ trông thế nào?

---

<!-- Slide 6 -->

*Lịch học*

## Một tuần học: giờ linh hoạt, mốc cố định

| Khi nào | Làm gì | Tuần 2–5 | Tuần 1 và 6 |
|---|---|---|---|
| Thứ 2 – Thứ 6 | Tự học, làm bài nhỏ theo lịch từng ngày | ~1,5h/ngày | ~2h/ngày |
| Thứ Bảy | Lab dài: ghép các phần trong tuần thành sản phẩm | ~3h | ~5h |
| Chủ nhật | Review PR chéo + sharing online 1h buổi tối | ~2,5h | ~3h |
| **Cả tuần** | | **~13h** | **~18h** |

> Số giờ chỉ là ước tính. Mỗi người tự chia lại trong tuần, miễn xong trước buổi sharing Chủ nhật. Cả khoá khoảng 88 giờ mỗi người.

> **Ghi chú diễn giả:** (~2 phút) Một tuần sẽ chia làm 3 phần. Từ thứ 2 đến thứ 6: mỗi tối khoảng 1,5 đến 2 tiếng, tự học và làm bài nhỏ theo lịch từng ngày có sẵn trong file lộ trình. Thứ Bảy: một buổi lab dài, ghép các phần nhỏ trong tuần thành sản phẩm hoàn chỉnh. Buổi này có mentor hỗ trợ. Chủ nhật: review PR của nhau, và tối có 1 tiếng sharing online cả team. Mỗi tuần một người demo bài của mình. Tổng cộng khoảng 13 giờ mỗi tuần ở giữa khoá, 18 giờ ở tuần đầu và tuần cuối. Điều mình muốn nhấn mạnh: số giờ chỉ là ước tính. Mình không yêu cầu ai ngồi đúng 2 tiếng mỗi tối. Ai bận tối thứ 3 thì bù vào thứ 5, không sao cả. Thứ cố định duy nhất là bài nộp trước buổi sharing Chủ nhật. Mình biết 13–18 giờ mỗi tuần là không ít, nhất là khi còn công việc dự án. Nên lát nữa có một quy tắc dành riêng cho tuần bận. Chuyển: Nói về quy tắc, có 6 điều cả team cùng giữ.

---

<!-- Slide 7 -->

*Quy tắc chung*

## Sáu điều cả team cùng giữ

1. **Nộp bằng PR trước Chủ nhật:** Mỗi PR cần ít nhất 1 người khác approve.
2. **Kẹt quá 30 phút thì hỏi:** Hỏi ngay trên nhóm chat chung, không ngồi một mình.
3. **Không commit secret:** Không để mật khẩu hay access key AWS trong repo.
4. **Tắt tài nguyên khi không dùng:** Đặc biệt là RDS. Tài khoản nào cũng có budget alert.
5. **Bận dự án thì lùi 1 tuần:** Được lùi, nhưng không bỏ qua giai đoạn nào.
6. **Phần “Nâng cao” là tuỳ chọn:** Ai đi nhanh thì làm thêm, không đi trước lộ trình.

> **Ghi chú diễn giả:** (~2–3 phút) 6 quy tắc này để khoá học chạy đều và an toàn. Một, nộp bài bằng PR trước Chủ nhật, mỗi PR cần ít nhất một người khác approve. Review code của nhau cũng là một cách học, nhiều khi đọc code bạn khác mới thấy cách hay hơn. Hai, kẹt quá 30 phút thì hỏi. Cái này mình rất muốn mọi người giữ. Với Cloud, nhiều lỗi chỉ là sai một dòng cấu hình quyền, người biết nhìn 1 phút là ra, còn tự mò có thể mất cả buổi tối. Hỏi không phải là yếu, hỏi là tiết kiệm thời gian cho cả team. Ba, không commit secret: không mật khẩu, không access key AWS trong repo. Đây là lỗi bảo mật có thật ngoài dự án, nên tập thói quen ngay từ bây giờ. Bốn, tắt tài nguyên khi không dùng, đặc biệt là database RDS. Tài khoản đều có cảnh báo chi phí, nhưng mọi người vẫn nên tự có ý thức. Năm, tuần nào bận dự án thì được lùi 1 tuần, chỉ cần báo sớm. Nhưng không bỏ qua giai đoạn nào, vì tuần sau xây trên tuần trước. Sáu, phần đánh dấu "Nâng cao" là tuỳ chọn. Ai đi nhanh thì làm thêm như ECS Fargate hay Terraform, chứ không đi trước lộ trình, để buổi sharing cả team vẫn cùng nhịp. Chuyển: Mỗi tuần cụ thể cần nộp gì?

---

<!-- Slide 8 -->

*Đầu ra*

## Mỗi tuần nộp gì, qua mốc khi nào

| Tuần | Nộp | Qua mốc khi |
|---|---|---|
| 1 | PR: Dockerfile, docker-compose.yml, README | `docker compose up` chạy được trên máy người khác |
| 2 | URL app, sơ đồ VPC + IAM tự vẽ | App chạy trên AWS với URL public, dùng RDS |
| 3 | Dashboard CloudWatch + alarm, sơ đồ hạ tầng | Không có secret hay access key trong repo |
| 4 | File workflow, link pipeline tới production | Merge PR là lên staging, production có bước duyệt |
| 5 | Code CDK, ghi chú diễn tập sự cố | Dựng lại staging hoàn toàn từ code |
| 6 | Tài liệu thiết kế 3–5 trang | Bảo vệ 20 phút trước team (demo day) |

> **Ghi chú diễn giả:** (~1–2 phút, không cần đọc hết từng dòng) Bảng này mọi người không cần nhớ, vì đã có trong file lộ trình. Mình chỉ muốn mọi người thấy là mỗi tuần đều có đầu ra rõ ràng, và tiêu chí qua mốc kiểm tra được, không phải cảm tính. Ví dụ tuần 1: không phải "mình hiểu Docker rồi", mà là "người khác clone repo về, chạy một lệnh là app lên". Tuần 3: mở repo ra không thấy một mật khẩu nào. Riêng tuần 6 là phần lớn nhất: mỗi người viết một tài liệu thiết kế 3–5 trang, gồm yêu cầu, sơ đồ kiến trúc, 3 quyết định thiết kế quan trọng kèm lý do, và ước tính chi phí. Sau đó bảo vệ 20 phút trước team ở buổi demo day. Phần này cũng là dịp luyện viết spec và thuyết trình. Trong khảo sát có mấy bạn muốn học kỹ năng mềm và muốn tham gia viết spec, thì đây là cơ hội. Chuyển: Mình biết với nhiều bạn đây là lần đầu chạm vào AWS, nên phần hỗ trợ rất quan trọng.

---

<!-- Slide 9 -->

*Hỗ trợ*

## Không ai phải học một mình

- **Mentor kèm cặp:** [Mentor 1] và [Mentor 2] mỗi bạn kèm 2–3 người, hỗ trợ trực tiếp trong buổi lab thứ Bảy.
- **Team lead review:** NamNT review ngẫu nhiên 2 PR mỗi tuần để phát hiện chỗ hiểu sai sớm.
- **Môi trường dựng sẵn:** Tài khoản AWS sandbox (đã có IAM và budget) và repo mẫu TeamBoard sẵn sàng trước tuần 2.
- **Tài liệu theo từng tuần:** Lịch từng ngày và link tài liệu nằm trong file lộ trình chung của team.

> **Ghi chú diễn giả:** (~2 phút) Không ai phải tự bơi. Có 4 thứ hỗ trợ. Thứ nhất, mentor. [Mentor 1] và [Mentor 2] là hai bạn đã có nền Cloud và DevOps. Mỗi bạn kèm 2–3 người, có mặt hỗ trợ trực tiếp trong buổi lab thứ Bảy. Với hai bạn mentor, đây cũng là cơ hội thử vai trò dẫn dắt, vì trong khảo sát có khá nhiều bạn muốn đi theo hướng Tech Lead. Thứ hai, mình sẽ review ngẫu nhiên 2 PR mỗi tuần. Không phải để chấm điểm, mà để phát hiện sớm nếu có chỗ hiểu sai, trước khi nó thành thói quen. Thứ ba, môi trường dựng sẵn. Trước tuần 2, mình sẽ chuẩn bị tài khoản AWS sandbox đã cấu hình quyền và cảnh báo chi phí, cùng repo mẫu TeamBoard. Mọi người không phải tự tạo tài khoản AWS hay lo bị trừ tiền thẻ. Thứ tư, tài liệu theo từng tuần, có lịch từng ngày và link đọc thêm, nằm trong file lộ trình chung. Chuyển: Vậy trước buổi đầu tiên, mọi người cần chuẩn bị gì?

---

<!-- Slide 10 -->

*Yêu cầu*

## Chuẩn bị trước buổi đầu tiên

### Cài đặt

- Docker Desktop
- .NET SDK bản team đang dùng
- Visual Studio hoặc Rider
- Git và tài khoản GitHub
- AWS CLI (có thể cài ở tuần 2)

### Cam kết

- 13–18 giờ mỗi tuần, trong 6 tuần
- Có mặt ở buổi sharing tối Chủ nhật
- Nộp bài đúng hạn bằng PR
- Báo sớm nếu tuần nào bận dự án

> **Ghi chú diễn giả:** (~1–2 phút) Có hai phần: cài đặt và cam kết. Cài đặt: Docker Desktop, .NET SDK bản team đang dùng, Visual Studio hoặc Rider, Git và tài khoản GitHub. AWS CLI thì có thể đợi tới tuần 2. Nhờ mọi người cài xong trước ngày bắt đầu, vì cài Docker trên Windows đôi khi mất thời gian, đừng để dồn vào tối thứ 2 tuần 1. Ai cài lỗi thì báo lên nhóm. Cam kết: mình xin mọi người 4 điều. Dành 13–18 giờ mỗi tuần trong 6 tuần. Có mặt ở buổi sharing tối Chủ nhật. Nộp bài đúng hạn bằng PR. Và nếu tuần nào bận dự án thì báo sớm, để mình và mentor sắp xếp, chứ đừng im lặng rồi bị tụt lại. 6 tuần không dài. Nếu mọi người giữ được nhịp này thì đến cuối khoá sẽ thấy khác biệt rất rõ. Chuyển: Và làm sao biết mình đã tiến bộ?

---

<!-- Slide 11 -->

*Đo kết quả*

## Sau 6 tuần, cả team chấm lại kỹ năng

| Kỹ năng | Hiện tại | Mục tiêu sau khoá |
|---|---|---|
| Cloud | 1,4 | ~3 |
| DevOps / CI-CD | 1,8 | ~3 |
| System design | 1,6 | ~2,5 |

Điểm trung bình tự chấm trên thang 5. Kết hợp với bài bảo vệ thiết kế ở demo day.

> **Ghi chú diễn giả:** (~1 phút) Cách đo rất đơn giản: sau tuần 6, mọi người điền lại phần kỹ năng trong đúng bảng khảo sát vừa rồi, rồi mình so trước và sau. Mục tiêu của cả team: Cloud và DevOps từ dưới 2 lên khoảng 3, tức là tự làm độc lập được. System design từ 1,6 lên khoảng 2,5. Mình muốn nói rõ: con số này không phải để chấm điểm hay đánh giá từng người. Nó để cả team nhìn thấy mình đã đi được bao xa. Còn thước đo thật thì nằm ở buổi demo day: mỗi người đứng lên trình bày được thiết kế của mình. Chuyển: Vậy là hết phần giới thiệu.

---

<!-- Slide 12 -->

*Bước tiếp theo*

# Câu hỏi?

- Cài đặt theo danh sách chuẩn bị
- Tuần 1 bắt đầu: [ngày bắt đầu]
- Buổi sharing đầu tiên: tối Chủ nhật cuối tuần 1

Lịch chi tiết và tài liệu: [link file lộ trình]

> **Ghi chú diễn giả:** (Hỏi đáp, ~5 phút) Tóm lại 3 bước tiếp theo: cài đặt theo danh sách vừa rồi, tuần 1 bắt đầu từ [ngày bắt đầu], và buổi sharing đầu tiên là tối Chủ nhật cuối tuần 1. Sau buổi này mình sẽ gửi link file lộ trình vào nhóm chat. Giờ mọi người có câu hỏi gì không? Gợi ý nếu không ai hỏi, có thể chủ động hỏi lại team: - "Có ai thấy 13–18 giờ/tuần là quá sức không? Mình có thể điều chỉnh." - "Tối Chủ nhật có hợp với mọi người không, hay nên đổi sang tối khác?" Câu hỏi có thể gặp: - Có bắt buộc không? Khuyến khích mọi người tham gia vì lộ trình xây từ chính mong muốn của team, nhưng tuần bận thì được lùi. - Có tốn tiền không? Không, dùng tài khoản sandbox mình chuẩn bị. - Ai đã biết Docker/AWS thì sao? Làm phần Nâng cao, hoặc làm mentor phụ. Kết: Cảm ơn mọi người. Mình rất mong 6 tuần này, và hẹn gặp ở buổi sharing đầu tiên.
 