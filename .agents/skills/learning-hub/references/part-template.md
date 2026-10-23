# Khuôn một file Part và quy tắc trình bày

Dùng cho mọi file trong lộ trình (`part-N-….md`) và cho `README.md` (Part 0). Mục tiêu: người đọc nắm ý chính trong 30 giây, đào sâu theo tầng L, không phải cuộn ngang.

## Khuôn

````markdown
# Part N — <Tên Part> (<Chủ đề>)

> [!NOTE]
> **Mục tiêu:** <người học làm được gì sau Part này, 1–2 câu>
> **Cần biết trước:** <prerequisite, link Part trước>
> **Thời lượng:** ~<số> giờ · **Đầu ra kiểm được:** <việc cụ thể kiểm được>
> **Part trước / sau:** [←](…) · [→](…)

## L1 What — <tên>
(thẻ khái niệm)

## L2 Why — <tên>
## L3 How — <tên>
## L4 Hands-on — <tên>
(lab đánh số)

## L5 Troubleshooting — <tên>
## L6 Production — <tên>
## L7 Architecture — <tên>
## L8 Mastery — <tên>
(chỉ giữ những tầng đã cam kết trong ma trận phủ của Part 0)

## Tự kiểm tra
(câu hỏi; đáp án ngắn trong `<details>`)

## Nguồn và độ tin cậy
(xem quy tắc dưới)
````

Tiêu đề mục luôn dạng `## Lx <Tên tầng> — <chủ đề con>` để script đối chiếu với ma trận.

## Thẻ khái niệm (dùng trong L1–L3, L6, L7)

````markdown
#### <Tên khái niệm hoặc cơ chế>
**Là gì:** <một dòng>.
**Ví dụ đời thường:** <một câu so sánh>.
**Use case:**
- <tình huống> → <cách dùng>
- <tình huống> → <cách dùng>

```bash
<lệnh hoặc code ngắn>
```
**Lưu ý:** <cạm bẫy hoặc khi không dùng>.
````

Mỗi khái niệm/cơ chế/kiểu dữ liệu/lệnh nhóm đều phải có **Use case**. Không liệt kê tên kèm một câu định nghĩa rồi dừng.

## Lab (L4)

````markdown
#### Lab 2.1 — <tên>
**Mục tiêu:** <một dòng>
```bash
<lệnh>
```
**Đầu ra thật** (<môi trường>, <ngày>):
```text
<đầu ra>
```
**Giải thích:** <vì sao ra như vậy>
**Dọn dẹp:** `<lệnh chỉ xoá tài nguyên vừa tạo>`
````

Đầu ra chưa chạy thật thì ghi ⚠ **chưa chạy** thay vì viết đầu ra tưởng tượng.

## Sự cố (L5)

Bảng tối đa 4 cột, ô ngắn; giải thích dài đặt ở thẻ bên dưới.

| Triệu chứng | Lệnh kiểm tra | Nguyên nhân gốc | Cách sửa |
|---|---|---|---|

## Case real-world (thẻ, không dùng bảng nhiều cột)

````markdown
#### <Công ty hoặc dự án> (<năm>): <tiêu đề ngắn>
- **Vấn đề:** …
- **Cách làm:** …
- **Bài học:** …
- Nguồn: [<tên bài>](<url>)
````

## Quy tắc trình bày

1. **Bảng:** tối đa 4 cột (so sánh 5 cột thì phải ngắn); mỗi ô ≤ ~120 ký tự; ô dài hơn thì chuyển thành thẻ hoặc đoạn văn. Ngoại lệ duy nhất về số cột: **ma trận phủ** ở Part 0 (9 cột, mỗi ô một ký tự).
2. **Văn phong:** câu ngắn, trung tính, không dùng "tôi"; thuật ngữ tiếng Anh kèm giải nghĩa ở lần đầu; ví dụ lấy từ domain Board/Task khi có thể; không lặp ý giữa các mục.
3. **Không kể quy trình làm việc** trong thân bài ("đã mở", "chưa mở lại", "agent đã đọc"). Thông tin đó chỉ nằm ở mục "Nguồn và độ tin cậy".
4. **Nhãn ⚠** gắn ngay sau điều **chưa tự xác minh**: nguồn chỉ đọc qua agent, kiến thức chung chưa có nguồn, lệnh chưa chạy. Điều đã tự mở nguồn hoặc đã chạy thật thì không gắn gì.
5. **Callout:** `> [!NOTE]` cho tóm tắt, `> [!TIP]` cho "dùng khi", `> [!WARNING]` cho cạm bẫy; dùng tiết kiệm.
6. **Sơ đồ:** ASCII hoặc mermaid ngắn cho luồng; không sơ đồ trang trí.
7. **Số liệu:** chỉ nêu khi có trong nguồn đã mở; tự kiểm phép tính; không trích số không kiểm.
8. **Secret:** không in giá trị từ `.env*`, chỉ tên biến.

## Mục "Nguồn và độ tin cậy" (cuối mỗi file)

Ba danh sách ngắn:
- **Đã tự mở và đối chiếu:** link, một dòng nói dùng cho điều gì.
- **Chỉ qua nguồn thứ cấp hoặc agent (⚠):** link và điều cần kiểm lại.
- **Chưa kiểm hoặc chưa tìm được:** liệt kê rõ để không bị hiểu thành đã kiểm.
