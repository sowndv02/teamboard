---
id: LL-002
title: Kiểm tra giấy phép/điều khoản của công cụ trước khi đưa vào thiết kế và tài liệu
status: prevented
tags: [dependency, docs, process]
issue: TB-002
date: 2026-10-21
severity: medium
recurrence: 0
---

# LL-002 — Kiểm tra giấy phép/điều khoản của công cụ trước khi đưa vào thiết kế và tài liệu

## Bài học (một câu, mệnh lệnh)
Trước khi đưa một thư viện hay công cụ vào thiết kế/tài liệu như lựa chọn mặc định, kiểm tra giấy phép và điều khoản hiện hành từ nguồn chính thức; đừng dựa vào trí nhớ.

## Chuyện gì đã xảy ra
Tài liệu ban đầu (ADR 0002, `testing.md`, các skill) mặc định dùng **LocalStack** để giả lập AWS cục bộ, và slide tham khảo dùng **MediatR/MassTransit**. Khi chủ dự án nêu lo ngại về thư viện thu phí và kiểm tra lại, hoá ra LocalStack cần auth token từ 2026.03, MediatR/MassTransit/FluentAssertions đã sang giấy phép thương mại, và Terraform/Redis/Docker Desktop cũng đổi điều khoản.

## Tác động
Tiềm tàng: thiết kế dựa trên công cụ có thể phát sinh chi phí hoặc rủi ro pháp lý; phải sửa nhiều tài liệu sau khi đã viết.

## Nguyên nhân gốc (5 câu hỏi "vì sao")
1. Vì sao đưa công cụ vào tài liệu? Chúng phổ biến và quen thuộc trong hệ sinh thái.
2. Vì sao không bị phát hiện sớm? Không có bước kiểm giấy phép khi chọn công cụ.
3. Vì sao không có bước đó? Dự án chưa có chính sách phụ thuộc; rule chỉ nói ghim phiên bản.
4. Vì sao tin trí nhớ? Giấy phép thay đổi nhanh, kiến thức tĩnh dễ lỗi thời.
**Nguyên nhân gốc:** thiếu chính sách và bước bắt buộc kiểm giấy phép/điều khoản (từ nguồn chính thức) khi chọn phụ thuộc.

## Cách sửa tức thời
Tra cứu và ghi nhận tình trạng hiện hành; thêm chính sách và danh bạ; đánh dấu LocalStack là chưa chốt.

## Biện pháp phòng ngừa

| # | Loại | Biện pháp | Vị trí | Trạng thái |
|---|---|---|---|---|
| 1 | rule | Chính sách phụ thuộc: hạng T0–T3, quy trình thêm phụ thuộc, bắt buộc kiểm giấy phép | `.agents/rules/dependency-policy.md` | done |
| 2 | dữ liệu | Danh bạ phụ thuộc có cột `verified` và nguồn | `config/dependencies.yaml` | done |
| 3 | ADR | Quyết định chính sách | `adr/0007-dependency-policy.md` | done |
| 4 | review | Checklist review thay đổi có phụ thuộc mới | `dependency-policy.md` mục 10 | done |
| 5 | gate tự động | Quét giấy phép trong gate | `quality-gate.md` | todo (chưa cài công cụ) |

## Cách kiểm chứng biện pháp hiệu quả
Thử thêm một thư viện mới theo `workflow`: agent phải hỏi/ghi giấy phép, hạng và nguồn kiểm trước khi thêm; thư viện T3 bị chặn hoặc hỏi người dùng.

## Phạm vi áp dụng
Mọi dự án; mọi loại phụ thuộc (NuGet, provider Terraform, image, công cụ CLI) và khi viết tài liệu có nêu công cụ cụ thể.

## Liên kết
`.agents/rules/dependency-policy.md`, `config/dependencies.yaml`, ADR 0007, ADR 0002.
