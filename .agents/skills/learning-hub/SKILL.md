---
name: learning-hub
description: Gom tài liệu của một chủ đề kiến thức trong dự án (docker, docker compose, API gateway/YARP, OpenTelemetry, outbox, JWT/OAuth, Terraform...) thành một gói đọc có thứ tự - tài liệu nội bộ, code liên quan, tài liệu chính thức, bài tập - và lưu ghi chú vào document/learning/. Dùng khi người dùng muốn học/đọc về một chủ đề, hỏi "tài liệu về X ở đâu", "gom tài liệu X", hoặc muốn xem mục lục kiến thức.
---

# Learning hub

Dự án này vừa là sản phẩm vừa là bài học. Kiến thức nằm rải rác: rule (`.agents/rules/`), ADR, spec/design, code, compose, config. Skill này gom theo **chủ đề** thành một gói đọc, để người học không phải tự đi tìm.

Khác `angular-learning`: skill đó dạy một lộ trình Angular; skill này là **thư viện tra cứu** xuyên mọi chủ đề, không dạy bài tập dài.

## Chế độ

| Chế độ | Khi nào | Đầu ra |
|---|---|---|
| `pack` | "Gom tài liệu docker", "tôi muốn đọc về API gateway" | Ghi chú `document/learning/<topic>.md` (tạo mới hoặc làm mới) + tóm tắt ngắn trong chat |
| `index` | "Có những chủ đề nào?", "mục lục" | Đọc `document/learning/README.md`, liệt kê chủ đề, đã có ghi chú chưa |
| `find` | "X được nói ở đâu trong repo?" | Danh sách file:dòng, không tạo ghi chú |

Không cần gõ lệnh; suy ra từ lời người dùng.

## Quy trình `pack`

1. **Chuẩn hoá chủ đề.** Đối chiếu `references/topic-map.md` (chủ đề → nơi tìm). Chủ đề chưa có trong bảng: tìm bằng Grep/Glob trong `document/`, `.agents/rules/`, `src/`, `config/`, `infra/`; sau khi xong, thêm dòng mới vào bảng.
2. **Đọc thật** các file nguồn trước khi viết; không dựa vào tên file. File không tồn tại hoặc rỗng thì bỏ, không bịa đường dẫn.
3. **Tài liệu chính thức và real-world.** Link ra ngoài chỉ khi đã mở/kiểm được (WebFetch/WebSearch) hoặc ghi rõ `Chưa kiểm`. Phiên bản lấy từ `config/tool-versions.yaml`, không đoán. Ưu tiên tài liệu gốc (docs.docker.com, learn.microsoft.com, learn.microsoft.com/aspnet YARP, RFC...) hơn blog.
4. **Viết ghi chú** theo mẫu dưới, tiếng Việt, thuật ngữ nâng cao giải thích lần đầu theo `explain-terms.md`.
5. **Cập nhật mục lục** `document/learning/README.md` (một dòng cho chủ đề).
6. **Báo lại** trong chat: đường dẫn ghi chú, 3–5 dòng điều cốt lõi, thứ tự nên đọc, và điều chưa kiểm.

Làm mới ghi chú đã có: đọc nó trước, giữ phần người dùng đã thêm tay, chỉ sửa chỗ nguồn đã đổi, ghi ngày làm mới.

## Mẫu ghi chú `document/learning/<topic>.md`

Viết cho người **chưa biết chủ đề**: đi từ "vấn đề" đến "giải pháp" đến "cách dùng", không mở đầu bằng định nghĩa khô. Mục 1–7 là kiến thức chung (đúng ngoài dự án); mục 8–10 gắn với TeamBoard.

````markdown
# <Chủ đề>

> Làm mới lần cuối: YYYY-MM-DD · Phiên bản liên quan: <từ config/tool-versions.yaml>

## 1. Vấn đề nó giải quyết
(Chưa có nó thì đau ở đâu? Một tình huống cụ thể, 3–6 dòng.)

## 2. Là gì (hiểu trong 1 phút)
(Định nghĩa bằng lời thường + phép ví von hoặc sơ đồ ASCII/mermaid nếu giúp. Thuật ngữ mới giải thích ngay.)

## 3. Khái niệm cốt lõi
(Bảng "khái niệm | nghĩa | ví dụ ngắn". Chỉ những khái niệm cần để dùng được.)

## 4. Cách dùng
(Đơn giản đến thực tế: ví dụ tối thiểu chạy được, rồi tình huống hay gặp. Mỗi ví dụ có lệnh/code và kết quả mong đợi.)

## 5. Ưu và nhược điểm
| Ưu | Nhược / cái giá |
|---|---|
(Cả hai cột bắt buộc; không viết như quảng cáo.)

**Nên dùng khi / không nên dùng khi:** ...
**Lựa chọn thay thế:** tên + khác biệt cốt lõi, một dòng mỗi cái.

## 6. Lỗi hay gặp và cách tránh
(3–5 mục: triệu chứng → nguyên nhân → cách phát hiện/sửa.)

## 7. Trong thực tế (real-world)
(Dự án/công ty thật dùng thế nào: mô hình phổ biến, quy mô, thực hành tốt, thứ hay bị làm sai ở production. Mỗi khẳng định có nguồn hoặc nhãn Chắc / Vừa / Chưa kiểm.)

## 8. Trong TeamBoard
(Dùng ở đâu, file:dòng thật, lệnh chạy được, port; quyết định trỏ về ADR/rule. Điểm lệch so với rule hoặc thực hành tốt: ghi lại, không tự sửa.)

## 9. Học tiếp
| # | Tài liệu | Loại | Đọc để biết |
|---|---|---|---|
(Nội bộ trước, chính thức sau; mức xác minh Chắc / Vừa / Chưa kiểm.)

## 10. Tự kiểm tra / thực hành
(2–4 câu hỏi hoặc việc nhỏ có tiêu chí đúng; ít nhất một câu "vì sao" và một việc chạy được trên repo.)
````

Mục 7 (real-world): nguồn chấp nhận được là tài liệu chính thức, engineering blog của chính công ty, sách, RFC. Không có nguồn thì chỉ viết thực hành phổ biến ở mức chung, gắn `Vừa`; không nêu tên công ty hay số liệu nếu chưa kiểm được. Mục 5 phải có cái giá thật, không chỉ lợi ích.

## Nguyên tắc
1. **Không thay nguồn sự thật.** Ghi chú trỏ về ADR/rule/spec, không chép lại quyết định; lệch nhau thì nguồn gốc thắng và ghi chú phải sửa.
2. **Không bịa.** Mọi đường dẫn, port, lệnh phải tồn tại/chạy được; không chắc thì ghi `Chưa kiểm`.
3. **Ngắn và có thứ tự.** Giải thích đủ để nắm kiến thức mới và biết đọc tiếp ở đâu, không chép cả giáo trình; một chủ đề tối đa khoảng 200 dòng.
4. **Không lộ secret** từ `.env*`, compose hay config; chỉ nêu tên biến.
5. **Chỉ ghi `document/learning/`** và `references/topic-map.md`; không sửa code hay tài liệu khác. Thấy tài liệu nguồn sai/lỗi thời thì báo người dùng.
6. Thay đổi file này là thay đổi docs: đi qua issue/branch theo `task-workflow.md`; chỉ commit khi được yêu cầu.
