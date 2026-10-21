# Code change gate

Lý do tồn tại: mọi thay đổi code phải qua cùng một cổng kiểm tra trước khi báo "xong", commit hay mở PR. Gate thất bại = chưa xong.

Công cụ, ngưỡng, quy ước severity và phạm vi test (targeted/full) nằm ở `quality-gate.md`. Lệnh cụ thể nằm ở `COMMANDS.md` §2. Lệnh 🚧 là chưa dùng được vì project chưa tạo; khi tạo project thì thêm lệnh và bỏ 🚧.

## 1. Chọn phạm vi gate theo file đã đổi

| Đổi ở | Gate bắt buộc |
|---|---|
| `src/**/*.cs`, `*.csproj`, `*.slnx` | **.NET**: build (warning = lỗi), test liên quan, format |
| `**/Dockerfile`, `docker-compose*.yml` | **Docker**: `docker compose config`; build image của service bị đổi |
| `infra/**/*.tf` | **Terraform**: `fmt -check`, `validate`, `plan` (không apply) |
| `config/**`, `document/**`, `.agents/**`, `*.md` | **Docs**: link không chết, số liệu khớp `config/`, bảng mục lục đã cập nhật |
| Mọi thay đổi | **Chung**: mục 3 và 4 dưới đây |

Chạm nhiều vùng thì chạy gate của tất cả các vùng. Đổi contract (API, event schema) thì chạy gate của **cả provider và consumer**.

## 2. Hard stop (dừng, không báo xong)

- Build lỗi, test fail, lint/format fail, `terraform validate` lỗi.
- Warning mới do thay đổi gây ra (nợ cũ thì báo, không sửa ngoài phạm vi).
- Test bị skip/xoá/nới lỏng để qua gate; hook/lint bị tắt; `--no-verify`.
- Có secret, file sinh ra (`bin`, `obj`, `*.tfstate`) hoặc nội dung `local/` trong diff.
- Contract (API/event) đổi nhưng chưa cập nhật test và tài liệu tương ứng.
- Thay đổi vi phạm kiến trúc bắt buộc (`architecture.md`): truy cập DB service khác, logic nghiệp vụ trong `shared/`, đơn giản hoá về CRUD.

Gặp hard stop: tìm nguyên nhân gốc và sửa. Không workaround (`agent-conduct.md`).

## 3. Review diff trước khi báo xong

Chạy `git diff` (và `git status`) rồi tự trả lời:

- [ ] Mọi file đổi đều thuộc phạm vi issue? Có đổi ngoài phạm vi thì tách ra hoặc báo.
- [ ] Có code chết, `TODO` chưa ghi vào issue, `Console.WriteLine`/debug sót lại?
- [ ] Có giá trị hardcode (URL, cổng, connection string, tên môi trường) đáng lẽ lấy từ config?
- [ ] Xử lý lỗi và trường hợp biên đã có test? Logic quan trọng (idempotency, outbox, auth) đã có test?
- [ ] Log/response không lộ secret hay PII?
- [ ] Đã cập nhật: issue (tiêu chí, trạng thái), `config/` nếu đổi cổng/tên service, docs/ADR nếu đổi hành vi hay quyết định.

## 4. Kiểm chứng hành vi (không chỉ build)

- Chạy thật tiêu chí hoàn thành của issue (ví dụ `curl` health endpoint, xem trace trong Jaeger).
- Sửa lỗi: có test tái hiện lỗi, fail trước khi sửa và pass sau.

## 5. Báo cáo kết quả gate

Cuối task ghi ngắn trong báo cáo (và nhật ký issue):

```
Gate: .NET build ✅ · test 24/24 ✅ · format ✅
Chưa kiểm chứng: <phần nào, vì sao>
```

Phần không chạy được phải ghi rõ lý do, không để trống hay ghi "đã kiểm tra".
