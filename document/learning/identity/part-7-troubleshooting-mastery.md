# Part 7 — Troubleshooting & Mastery (Identity)

> [!NOTE]
> **Mục tiêu:** học từ sự cố thực tế về xác thực, luyện cách chọn và giải thích đánh đổi như trong phỏng vấn thiết kế, và tự đánh giá mức thành thạo bằng bài mở.
> **Cần biết trước:** [Part 1 đến 6](README.md).
> **Thời lượng:** ~5 giờ · **Đầu ra kiểm được:** nêu nguyên nhân gốc và bài học của 3 sự cố; trả lời 6 câu hỏi thiết kế; làm một bài mở.
> [← Part 6](part-6-projects.md) · [Part 0](README.md)

## L5 Troubleshooting — Sự cố thực tế và bài học

> [!WARNING]
> Các sự cố dưới đây **chưa được tôi mở đọc bản gốc** (⚠ chỉ qua agent): mô tả ở mức khái quát, **không có con số hay ngày chi tiết**. Trước khi trích dẫn, hãy mở báo cáo gốc của chính bên liên quan.

| Sự cố | Nguyên nhân gốc (khái quát) | Bài học, đối chiếu lab |
|---|---|---|
| Lỗi `alg=none` và key confusion trong nhiều thư viện JWT (công bố công khai khoảng 2015) | thư viện tin `alg` trong header token | Lab 5.1: danh sách thuật toán cho phép cố định |
| Microsoft, khoá ký tài khoản khách bị dùng sai để giả token (2023, báo cáo công khai của Microsoft) | khoá ký bị lấy khỏi môi trường và kiểm tra `aud`/khoá chưa chặt | Part 5 L6: bảo vệ khoá ký, kiểm `aud` và khoá theo đúng ngữ cảnh |
| Okta, tệp hỗ trợ chứa phiên đăng nhập bị lộ (2023, báo cáo công khai của Okta) | token và cookie phiên nằm trong tệp được chia sẻ | Part 4 L6: không log và không chia sẻ token; phiên ngắn hạn |
| Rò băm mật khẩu hàng loạt (nhiều vụ, trong đó có vụ băm không salt) | băm nhanh hoặc không salt khiến bẻ hàng loạt dễ | Lab 2.1: băm chậm có salt |

**Cách rút bài học:** với mỗi sự cố, tự hỏi (1) điều kiện kiểm tra nào thiếu, (2) lab nào của lộ trình tái hiện được điều kiện đó, (3) tín hiệu quan sát nào đã có thể báo sớm.

## L7 Architecture — Câu hỏi thiết kế và phỏng vấn

Mỗi câu nên trả lời bằng **đánh đổi** chứ không chỉ một đáp án.

1. **Tự xây hay Cognito?** Nêu bốn tiêu chí (học hay giao sản phẩm, người dùng, tính năng, ai rà soát) và điều kiện đổi quyết định (Part 1, 5).
2. **Session cookie hay JWT?** Thu hồi, nhiều dịch vụ, CSRF so với lộ token (Part 1).
3. **Vì sao RS256 cho nhiều service?** Khoá ký chỉ ở nơi cấp; đổi lại có JWKS và xoay khoá (Part 3).
4. **Làm sao thu hồi token tức thì?** Introspection hoặc danh sách thu hồi, mất lợi ích kiểm cục bộ; hoặc chấp nhận trễ bằng thời hạn access token (Part 4).
5. **Gateway đã kiểm JWT thì service có cần kiểm lại?** Có: `aud` và quyền theo tài nguyên là việc của service (Part 3 L7).
6. **PKCE giải quyết vấn đề gì mà `client_secret` không giải quyết được?** Client công khai (trình duyệt, di động) không giữ được bí mật; PKCE gắn code với bí mật tạo lúc khởi tạo luồng (Part 4).

## L8 Mastery — Bài mở và tiêu chí thành thạo

#### Bài mở 1 — Thiết kế hệ định danh cho một sản phẩm mới
Sản phẩm: ứng dụng ghi chú nhóm cho khoảng một vạn người dùng, có web và di động, cần đăng nhập Google. **Yêu cầu:** chọn tự xây hay dịch vụ quản lý, luồng đăng nhập, thời hạn token, thu hồi, quản lý khoá, quan sát, chi phí, và **điều kiện để đổi quyết định**. **Bằng chứng đạt:** mỗi lựa chọn có lý do và một phương án đã loại.

#### Bài mở 2 — Gỡ sự cố giả định
Người dùng báo "đăng nhập xong thì bị đăng xuất sau vài phút, lúc có lúc không". Liệt kê tối thiểu 5 giả thuyết (Part 4 L5), cách kiểm từng cái bằng log và đo, và thứ tự kiểm.

#### Bài mở 3 — Tự bổ sung lab còn thiếu
Chọn một mục "chưa kiểm" ở Part 6 (OIDC `id_token` và `nonce`, hoặc IDOR và policy) và viết lab chạy được trên `127.0.0.1` với khoá sinh lúc chạy, có **bản yếu bị tấn công thành công** và **bản đúng chặn được**.

**Tiêu chí thành thạo:**
- Giải thích được vì sao mỗi điều kiện kiểm tra tồn tại bằng cách chỉ ra tấn công nó chặn.
- Chọn giữa các giải pháp ở Bản đồ (Part 1) cho tình huống mới, kèm điều kiện đổi quyết định.
- Tự viết và chạy một tấn công vào dịch vụ của chính mình, rồi chặn nó.
- Nói được chỗ nào của lộ trình **không** chứng minh được điều gì (Part 6 L5).

## Tự kiểm tra

1. Bạn rút ra điều gì chung từ sự cố `alg=none` và vụ khoá ký bị dùng sai?
2. Khi nào "thu hồi tức thì" đáng để đánh đổi lợi ích kiểm cục bộ?
3. Bài mở nào của lộ trình này vẫn chưa có đầu ra thật, và vì sao?

<details>
<summary>Đáp án ngắn</summary>

1. Mọi quyết định tin token phải dựa trên điều kiện kiểm tra cố định và khoá được bảo vệ; thiếu một điều kiện là đủ để bị lợi dụng.
2. Khi hậu quả của token đã lộ lớn (tài chính, dữ liệu nhạy cảm) và chấp nhận thêm một lần hỏi nơi cấp mỗi lời gọi.
3. Cả ba bài mở: đây là bài tự làm, lộ trình không cung cấp đầu ra.
</details>

## Nguồn và độ tin cậy

**Đã tự chạy:** không có lab mới ở Part này; tham chiếu Lab 2.1, 5.1 ở các Part trước.
**Đã tự mở:** các Part trước của lộ trình này.
**Chỉ qua agent (⚠):** bốn sự cố ở L5 (alg=none và key confusion công khai khoảng 2015; Microsoft 2023; Okta 2023; rò băm mật khẩu). Chưa mở báo cáo gốc nên **không** đưa số liệu, ngày cụ thể hay quy kết chi tiết.
**Chưa kiểm:** mọi nội dung chi tiết của các sự cố trên; câu trả lời phỏng vấn là gợi ý, không phải đáp án duy nhất.
