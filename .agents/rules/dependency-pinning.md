# Dependency pinning

Lý do: build tái lập được, tránh lỗi "hôm qua chạy, hôm nay hỏng" do phiên bản trôi.

## NuGet (.NET)

- Ghi **phiên bản cố định** (`Version="10.0.13"`), không dùng range `*`, `10.*`, `[10.0,)`.
- Khi có từ 2 project trở lên, dùng Central Package Management (`Directory.Packages.props`) để một phiên bản cho cả solution.
- Package họ Microsoft/EF Core/ASP.NET cùng major.minor để tránh lệch (ví dụ cùng `10.0.x`).
- Thêm package mới: kiểm tra cần thật, giấy phép, bản ổn định mới nhất (không dùng bản `preview` trừ khi có lý do ghi trong issue). Thấy package `preview` đang có thì báo, không tự đổi.

## npm (Angular, `src/web`; ADR 0009)

- Phiên bản **chính xác** trong `package.json` (không `^`/`~`; đặt `save-exact=true` trong `.npmrc`), commit `package-lock.json`; gate và CI dùng `npm ci`, không `npm install`.
- Khai báo `engines` (Node) khớp `config/tool-versions.yaml` và `.nvmrc`; các gói `@angular/*` cùng một version.
- Thêm package: kiểm cần thật (ưu tiên T0 first-party), giấy phép (`dependency-policy.md`), `npm audit`, số phụ thuộc bắc cầu, mức bảo trì; cẩn thận gói có script `postinstall`; cân nhắc `ignore-scripts` trong CI.
- Nâng phiên bản Angular bằng `ng update` từng major, là một thay đổi riêng.

## Terraform

- `required_version` và provider dùng `~>` theo minor (`~> 5.0`); commit `.terraform.lock.hcl`.
- Module bên ngoài pin theo tag/version cụ thể, không theo nhánh.

## Docker

- Base image pin theo phiên bản cụ thể (ví dụ `mcr.microsoft.com/dotnet/aspnet:10.0`), không `latest`. Image bên thứ ba trong compose (Jaeger, LocalStack, Postgres) cũng pin tag.

## CI

- Action/tool pin theo version hoặc SHA, không `@main`.

## Quy trình đổi phiên bản

Nâng phiên bản là một thay đổi riêng (commit/issue riêng), có build + test xanh, không trộn với thay đổi tính năng.

## Chính sách phụ thuộc

Hạn chế thư viện bên thứ ba và tránh rủi ro giấy phép: `dependency-policy.md` (ADR 0007, danh bạ `config/dependencies.yaml`). Thêm/đổi phụ thuộc phải qua kiểm tra giấy phép trước khi ghim phiên bản.
