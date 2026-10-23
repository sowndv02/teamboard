---
name: learning-hub
description: Gom tài liệu của một chủ đề kiến thức trong dự án (docker, docker compose, API gateway/YARP, caching/Redis, microservice, OpenTelemetry, outbox, JWT/OAuth, Terraform...) thành một gói đọc có thứ tự - khái niệm gốc trước, công cụ sau (công cụ không đại diện khái niệm: khái niệm có bản đồ giải pháp so sánh các lựa chọn và từng cơ chế nêu riêng); tài liệu nội bộ, code liên quan, tài liệu chính thức, case real-world có nguồn, bài tập - và lưu ghi chú vào document/learning/. Dùng khi người dùng muốn học/đọc về một chủ đề, hỏi "tài liệu về X ở đâu", "gom tài liệu X", hoặc muốn xem mục lục kiến thức.
---

# Learning hub

Dự án này vừa là sản phẩm vừa là bài học. Kiến thức nằm rải rác: rule (`.agents/rules/`), ADR, spec/design, code, compose, config. Skill này gom theo **chủ đề** thành một gói đọc, để người học không phải tự đi tìm.

Khác `angular-learning`: skill đó dạy một lộ trình Angular; skill này là **thư viện tra cứu** xuyên mọi chủ đề, không dạy bài tập dài.

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `pack` | "Gom tài liệu docker", "tôi muốn đọc về API gateway" | Hỏi một công cụ: **cặp** ghi chú `document/learning/` (khái niệm gốc + công cụ, tạo mới hoặc làm mới); hỏi một khái niệm: ghi chú khái niệm. Kèm tóm tắt ngắn trong chat |
| `index` | "Có những chủ đề nào?", "mục lục" | Đọc `document/learning/README.md`, liệt kê chủ đề theo loại (khái niệm/công cụ), công cụ thuộc khái niệm nào, ghi chú khái niệm đã có "Bản đồ giải pháp" chưa, real-world đạt chuẩn chưa; chỉ ra khái niệm gốc còn thiếu |
| `find` | "X được nói ở đâu trong repo?" | Danh sách file:dòng, không tạo ghi chú |

Không cần gõ lệnh; suy ra từ lời người dùng.

## Quy trình `pack`

1. **Chuẩn hoá chủ đề và xác định tầng.** Đối chiếu `references/topic-map.md` (chủ đề → khái niệm gốc → nơi tìm). Chủ đề chưa có trong bảng: tìm bằng Grep/Glob trong `document/`, `.agents/rules/`, `src/`, `config/`, `infra/`; sau khi xong, thêm dòng mới vào bảng.
   - **Người dùng hỏi một công cụ (Redis, YARP, Docker...) thì luôn ra một cặp ghi chú: khái niệm gốc toàn diện + công cụ.** Người học có thể chỉ biết công cụ, nên không được để họ hiểu "công cụ này là cả khái niệm". Ghi chú khái niệm chưa có thì viết **trước**; có rồi nhưng thiếu "Bản đồ giải pháp" (mục 5 của mẫu) hoặc chỉ trình bày qua một công cụ thì bổ sung trước khi viết ghi chú công cụ.
   - Hỏi một khái niệm thì viết ghi chú khái niệm, và thêm ghi chú công cụ chỉ khi dự án dùng/sắp dùng công cụ đó.
2. **Đọc thật** các file nguồn trước khi viết; không dựa vào tên file. File không tồn tại hoặc rỗng thì bỏ, không bịa đường dẫn.
3. **Tài liệu chính thức.** Link ra ngoài chỉ khi đã mở/kiểm được (WebFetch/WebSearch) hoặc ghi rõ `Chưa kiểm`. Phiên bản lấy từ `config/tool-versions.yaml`, không đoán. Ưu tiên tài liệu gốc (docs.docker.com, learn.microsoft.com, RFC...) hơn blog.
4. **Nghiên cứu bên ngoài (bắt buộc), ba phần:** (a) **từng cơ chế/biến thể** của khái niệm để viết mục 3.1: mỗi cái cần nguồn riêng, cái nào không có thì ghi `Chưa kiểm`; (b) **các lựa chọn khác** để lập Bản đồ giải pháp (docs chính thức, giấy phép, khi chọn, cái giá của từng lựa chọn); (c) **real-world**: **mở đọc bài gốc** của dự án/công ty thật đã dùng chủ đề này (engineering blog của chính họ, paper, bài hội nghị, tài liệu chính thức, sách), gồm cả case thất bại hoặc bị bỏ. Chủ đề rộng thì giao tối đa 3 subagent đọc song song mỗi đợt, mỗi agent trả danh sách kèm URL đã mở; agent không ghi file. **Người gọi phải tự mở lại các nguồn then chốt** (nguồn của bản đồ, của số liệu, của từng cơ chế) trước khi ghi mức Chắc, và tự kiểm phép tính của số liệu (đã từng bắt được ví dụ số sai trong một blog). Chuẩn tối thiểu ở mục 7 của mẫu.
5. **Viết ghi chú** theo mẫu dưới, tiếng Việt, thuật ngữ nâng cao giải thích lần đầu theo `explain-terms.md`. Khái niệm trước, công cụ sau.
6. **Cập nhật mục lục** `document/learning/README.md`: tách bảng Khái niệm và bảng Công cụ; cột: loại, "một trong các lựa chọn của" (với công cụ), có Bản đồ giải pháp (với khái niệm), làm mới, real-world đạt chuẩn hay chưa và vì sao.
7. **Chạy checklist cuối** (mục "Checklist trước khi báo xong").
8. **Báo lại** trong chat: đường dẫn ghi chú, 3–5 dòng điều cốt lõi, thứ tự nên đọc, điều chưa kiểm và nguồn chỉ ở mức Vừa.

Làm mới ghi chú đã có: đọc nó trước, giữ phần người dùng đã thêm tay, chỉ sửa chỗ nguồn đã đổi, ghi ngày làm mới.

## Mẫu ghi chú `document/learning/<topic>.md`

Viết cho người **chưa biết chủ đề**: đi từ "vấn đề" đến "giải pháp" đến "cách dùng", không mở đầu bằng định nghĩa khô. Mục 1–7 là kiến thức chung (đúng ngoài dự án); mục 8–10 gắn với TeamBoard.

**Khái niệm trước, công cụ sau, và công cụ không đại diện cho khái niệm.** Ghi chú **khái niệm** (caching, container, API gateway, microservice) dạy cái gốc toàn diện, không phụ thuộc công cụ, và **so sánh các họ giải pháp** (mục 5). Ghi chú **công cụ** (Redis/Valkey, Docker, YARP) trỏ về khái niệm gốc, **không lặp** phần chung; chỉ nói phần riêng của công cụ (cấu trúc, cấu hình, giấy phép, giá, lỗi riêng), có real-world riêng, và **tự nêu phạm vi** của nó (mục 5) để người đọc không nhầm công cụ với cả khái niệm.

````markdown
# <Chủ đề>

> Loại: Khái niệm
> hoặc: Loại: Công cụ · Một trong các lựa chọn của [<khái niệm>](<file>.md#bản-đồ-giải-pháp) (xem phần "Phạm vi" ở mục 5)
> Làm mới lần cuối: YYYY-MM-DD · Phiên bản liên quan: <từ config/tool-versions.yaml>

## 1. Vấn đề nó giải quyết
(Chưa có nó thì đau ở đâu? Một tình huống cụ thể, 3–6 dòng.)

## 2. Là gì (hiểu trong 1 phút)
(Định nghĩa bằng lời thường + phép ví von hoặc sơ đồ ASCII/mermaid nếu giúp. Thuật ngữ mới giải thích ngay.)

## 3. Khái niệm cốt lõi
(Bảng "khái niệm | nghĩa | ví dụ ngắn" cho các thuật ngữ cơ bản. Khái niệm khó (saga, invalidation, idempotent...) phải có đoạn giải thích riêng ngay dưới bảng, không chỉ liệt kê tên.)

### 3.1 Các cơ chế / biến thể / pattern chính (bắt buộc với ghi chú khái niệm có nhiều cách làm)
**Không gói các cơ chế vào một dòng bảng "khái niệm".** Mỗi cơ chế (ví dụ caching: cache-aside, read-through, write-through, write-behind, write-around, refresh-ahead; gateway: routing, aggregation, offloading, BFF; microservice: outbox, saga, idempotent consumer...) có **một mục con riêng** gồm: ai làm gì và theo thứ tự nào; sơ đồ luồng (ASCII/mermaid); ưu; nhược; dùng khi / không dùng khi; ghi rõ mức nguồn của riêng nó (Chắc / Vừa / Chưa kiểm). Cơ chế chưa có nguồn đã mở thì vẫn nêu nhưng gắn `Chưa kiểm` và nói thẳng "chưa có nguồn". Kết thúc bằng **bảng so sánh** các cơ chế và cách kết hợp thường gặp. Lưu ý các tài liệu có thể gọi tên khác nhau; mô tả theo "ai làm gì, thứ tự nào".

## 4. Cách dùng
(Đơn giản đến thực tế: ví dụ tối thiểu chạy được, rồi tình huống hay gặp. Mỗi ví dụ có lệnh/code và kết quả mong đợi. Bắt buộc có ít nhất một ví dụ code hoặc lệnh; chưa chạy thì ghi `Chưa kiểm`.)

## 5. Ưu và nhược điểm
| Ưu | Nhược / cái giá |
|---|---|
(Cả hai cột bắt buộc; không viết như quảng cáo.)

**Nên dùng khi / không nên dùng khi:** ...

### Bản đồ giải pháp
**Bắt buộc với ghi chú KHÁI NIỆM. Giữ nguyên đúng tiêu đề này, không thêm chú thích vào heading**, vì ghi chú công cụ link tới neo `#bản-đồ-giải-pháp`. Bảng các **họ giải pháp** và **≥3 lựa chọn cụ thể**, kể cả lựa chọn "không dùng" nếu có (không cache, modular monolith...). Không để công cụ nào đứng làm đại diện duy nhất của khái niệm.

| Họ / cách làm | Công cụ ví dụ | Khi chọn | Cái giá | Nguồn (đã mở hoặc Chưa kiểm) |
|---|---|---|---|---|

### Phạm vi và những gì công cụ này KHÔNG đại diện
**Bắt buộc với ghi chú CÔNG CỤ; giữ nguyên tiêu đề này.** Nói thẳng: công cụ thuộc họ nào trong bản đồ của khái niệm; phần nào của khái niệm nó **không** bao quát; các lựa chọn khác khác nó ở điểm cốt lõi nào (một dòng mỗi lựa chọn, link về bản đồ). Ghi chú công cụ không có mục này thì chưa đạt.

## 6. Lỗi hay gặp và cách tránh
(3–5 mục: triệu chứng → nguyên nhân → cách phát hiện/sửa.)

## 7. Trong thực tế (real-world)
(Bắt buộc **≥3 case** có nguồn gốc đã mở đọc, dạng bảng:)

| Công ty / dự án | Bối cảnh, vấn đề | Cách làm | Kết quả, bài học | Nguồn (link đã mở, năm) | Mức |
|---|---|---|---|---|---|

(Rồi hai tiểu mục: **Mô hình phổ biến** (cách dùng thường gặp, thực hành tốt, thứ hay làm sai ở production) và **Khi nó thất bại hoặc bị bỏ** (case quay lại hướng cũ, sự cố; bỏ tiểu mục nếu không tìm được nguồn). Mỗi khẳng định có nguồn hoặc nhãn Chắc / Vừa / Chưa kiểm.)

## 8. Trong TeamBoard
(Dùng ở đâu, file:dòng thật, lệnh chạy được, port; quyết định trỏ về ADR/rule. Điểm lệch so với rule hoặc thực hành tốt: ghi lại, không tự sửa.)

## 9. Học tiếp
| # | Tài liệu | Loại | Đọc để biết |
|---|---|---|---|
(Nội bộ trước, chính thức sau; mức xác minh Chắc / Vừa / Chưa kiểm.)

## 10. Tự kiểm tra / thực hành
(2–4 câu hỏi hoặc việc nhỏ có tiêu chí đúng; ít nhất một câu "vì sao" và một việc chạy được trên repo.)
````

Mục 7 (real-world), quy tắc cứng:
- **Nguồn chấp nhận:** engineering blog của chính công ty, paper, bài hội nghị, tài liệu chính thức, sách, RFC. Bài thứ cấp (blog bên thứ ba, tin tức) chỉ làm nguồn phụ, gắn `Vừa`.
- **Mở thật mới ghi.** Mỗi case phải đã mở được bài gốc ở phiên làm việc này; không dựa vào trí nhớ. Link không mở được thì bỏ hoặc gắn `Chưa kiểm`, không tính vào 3 case.
- **Trạng thái real-world trong mục lục (ba mức, ghi kèm lý do):** **Đạt** = ≥3 case của công ty/dự án có bài gốc người viết tự mở; **Đạt một phần** = đủ 3 nguồn nhưng chưa đủ 3 case công ty tự mở (ví dụ phần còn lại là tài liệu chính thức hoặc chỉ qua agent); **Chưa đạt** = dưới 3 nguồn có thể kiểm. Không đủ thì vẫn viết số case có được, ghi lý do thiếu. Không bịa tên công ty, số liệu, kiến trúc.
- **Số liệu** chỉ nêu khi nằm trong bài gốc, kèm nguồn và ngữ cảnh. Diễn đạt lại bằng lời mình; trích nguyên văn tối đa một câu ngắn có dẫn nguồn.
- **Cân bằng:** nếu có case quay lại hướng cũ hoặc sự cố thì nêu, để người học thấy cả cái giá.

Mục 5 phải có cái giá thật, không chỉ lợi ích.

## Checklist trước khi báo xong

Ghi chú **khái niệm**:
- [ ] Có mục 3.1 với **mỗi cơ chế/biến thể một mục con** (ai làm gì, sơ đồ, ưu, nhược, khi dùng, mức nguồn) và bảng so sánh.
- [ ] Có "### Bản đồ giải pháp" với ≥3 lựa chọn (kể cả "không dùng") và cái giá của từng lựa chọn.
- [ ] Mục 7 có ≥3 case có nguồn đã mở; mức Chắc chỉ cho nguồn người viết tự mở.
- [ ] Có ví dụ code/lệnh (ghi `Chưa kiểm` nếu chưa chạy).

Ghi chú **công cụ**:
- [ ] Dòng đầu "Một trong các lựa chọn của [khái niệm](…#bản-đồ-giải-pháp)" và link đó không gãy.
- [ ] Có "### Phạm vi và những gì công cụ này KHÔNG đại diện".
- [ ] Không lặp phần chung của khái niệm; có real-world riêng của công cụ.

Cả hai:
- [ ] Mọi đường dẫn nội bộ tồn tại, mọi link `.md` chéo và anchor không gãy (kiểm bằng script).
- [ ] Số liệu đã tự kiểm phép tính; không trích số không kiểm.
- [ ] Mục lục đã cập nhật; điểm lệch tài liệu nguồn được **báo**, không tự sửa.

## Nguyên tắc
1. **Không thay nguồn sự thật.** Ghi chú trỏ về ADR/rule/spec, không chép lại quyết định; lệch nhau thì nguồn gốc thắng và ghi chú phải sửa.
2. **Không bịa.** Mọi đường dẫn, port, lệnh phải tồn tại/chạy được; không chắc thì ghi `Chưa kiểm`.
3. **Đủ sâu, có thứ tự.** Giải thích đủ để nắm kiến thức mới, biết real-world đang làm gì và đọc tiếp ở đâu, không chép cả giáo trình; một ghi chú khoảng 300 dòng. Dài hơn thì tách file con (ví dụ `<topic>-<khía cạnh>.md`) và link từ ghi chú chính, không nén bớt real-world.
4. **Không lộ secret** từ `.env*`, compose hay config; chỉ nêu tên biến.
5. **Chỉ ghi `document/learning/`** và `references/topic-map.md`; không sửa code hay tài liệu khác. Thấy tài liệu nguồn sai/lỗi thời thì báo người dùng.
6. **Nêu rõ từng cơ chế, đừng nói chung chung.** Người học cần biết từng biến thể làm gì và khác nhau ở đâu (mục 3.1 của mẫu); một cụm như "khái niệm cốt lõi" liệt kê tên mà không giải thích là chưa đạt.
7. **Công cụ không đại diện khái niệm.** Mọi ghi chú khái niệm phải có "Bản đồ giải pháp" ≥3 lựa chọn; mọi ghi chú công cụ phải có mục "Phạm vi và những gì công cụ này KHÔNG đại diện". Mục lục ghi cột "Bản đồ giải pháp" (có/không); thiếu thì tính là chưa đạt.
8. Thay đổi file này là thay đổi docs: đi qua issue/branch theo `task-workflow.md`; chỉ commit khi được yêu cầu.
