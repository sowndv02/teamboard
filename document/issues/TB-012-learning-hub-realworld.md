---
key: TB-012
title: learning-hub bắt buộc real-world có nguồn và phân tầng khái niệm/công cụ
status: review
phase: 0
branch: feature/TB-012-learning-hub-realworld
estimate: M
created: 2026-10-23
started: 2026-10-23
closed: null
---

# TB-012 — learning-hub bắt buộc real-world có nguồn và phân tầng khái niệm/công cụ

## Bối cảnh
Ghi chú đầu tiên của skill `learning-hub` (TB-011) có mục real-world mỏng, nguồn ít, ví dụ code thiếu; ghi chú công cụ (Redis) đứng riêng thay vì dựa trên khái niệm gốc (caching). Người học cần biết các dự án thực tế đang làm gì, có nguồn kiểm được, và hiểu khái niệm trước khi học công cụ.

## Phạm vi
- Làm: sửa `.agents/skills/learning-hub/SKILL.md` và `references/topic-map.md` (cột "Khái niệm gốc"); mục real-world bắt buộc ≥3 case có nguồn đã mở; nguyên tắc "gốc trước, công cụ sau" và "công cụ không đại diện khái niệm" (Bản đồ giải pháp ở ghi chú khái niệm, mục Phạm vi ở ghi chú công cụ); viết `caching.md`, `container.md`; viết lại `redis.md`; tách `api-gateway.md` + `yarp.md`; bổ sung `microservice.md`, `docker-compose.md`; cập nhật `document/learning/README.md`.
- Không làm: sửa code hay tài liệu nguồn (ADR, rule, spec, design, `config/`); sửa các điểm lệch tìm thấy (báo riêng); commit khi chưa được yêu cầu. Ngoại lệ có phê duyệt của người dùng: thêm một dòng `learning-hub` vào `.agents/rules/INDEX.md`.

## Tiêu chí hoàn thành (kiểm chứng được)
- [x] `SKILL.md` có quy tắc real-world ≥3 case có nguồn, quy tắc khái niệm/công cụ, giới hạn ~300 dòng.
- [x] `topic-map.md` có cột "Khái niệm gốc" cho mọi dòng chủ đề công cụ.
- [x] Mỗi ghi chú trong `document/learning/` có dòng Loại/Gốc và mục 7 với ≥3 case có link đã mở, hoặc ghi rõ "chưa đạt chuẩn" kèm lý do.
- [x] Mọi đường dẫn nội bộ trong ghi chú tồn tại (đã kiểm bằng script); ví dụ lệnh/code **chưa chạy**, đều ghi `Chưa kiểm`.

## Kế hoạch
1. Issue và branch.
2. Nghiên cứu real-world theo chủ đề (caching, Redis/Valkey, microservice, container/Compose, API gateway).
3. Sửa skill và topic-map.
4. Viết caching, redis; bổ sung microservice; đưa docker-compose, api-gateway về chuẩn.
5. Cập nhật mục lục, kiểm chứng.

## Ghi chú / Nhật ký
- 2026-10-23: tạo issue; kế hoạch được người dùng duyệt (plan mode).
- 2026-10-23: sửa skill + topic-map; viết `caching.md`, viết lại `redis.md`, bổ sung `microservice.md`, đưa `docker-compose.md` và `api-gateway.md` về chuẩn. Nghiên cứu bằng 5 subagent; các nguồn quan trọng được tự mở lại (ghi mức Chắc), phần còn lại ghi Vừa. Một lỗi phát hiện khi kiểm: ví dụ số trong bài Cloudflare "Sometimes I cache" không khớp phép tính (1/500 ở 10 req/s ≈ 1 request mỗi 50 giây, bài ghi 5 giây), nên ghi chú không trích số.
- 2026-10-23 (đợt 2, theo yêu cầu người dùng "công cụ không được đại diện cho khái niệm"): SKILL.md thêm quy tắc "Bản đồ giải pháp" (khái niệm) và "Phạm vi và KHÔNG đại diện" (công cụ), hỏi công cụ thì ra cặp ghi chú; topic-map thêm cột gốc và danh sách lựa chọn khác. Viết mới `container.md`; tách `api-gateway.md` (khái niệm) và `yarp.md` (công cụ); thêm bản đồ giải pháp cho `caching.md`, `microservice.md`; thêm phần phạm vi cho `docker-compose.md`, `redis.md`. Nghiên cứu bằng 3 agent; các nguồn then chốt tự mở lại (OCI, Borg, Kaniko, Envoy, Istio, Azure gateway, MDN, AWS ElastiCache). Trang dockershim FAQ không nêu lý do gỡ trong phần đọc được nên ghi chú ghi lý do là kiến thức chung, Chưa kiểm.
- 2026-10-23 (đợt 3, theo yêu cầu "ghi rõ ràng từng cơ chế, đừng chung chung"): SKILL.md thêm mục 3.1 "Các cơ chế/biến thể/pattern chính" (mỗi cơ chế một mục con: ai làm gì, sơ đồ, ưu, nhược, khi dùng, mức nguồn) và nguyên tắc 6. `caching.md` có 6 cơ chế riêng (cache-aside, read-through, write-through, write-behind, write-around, refresh-ahead) + bảng so sánh; `api-gateway.md`, `container.md`, `microservice.md` cũng có mục 3.1. Nguồn tự mở: Azure Cache-Aside, AWS whitepaper caching patterns, Azure caching guidance. Write-behind chỉ có Azure nhắc tên; write-around và refresh-ahead chưa có nguồn định nghĩa nên ghi `Chưa kiểm`.
- 2026-10-23 (đợt 4): rà các skill khác, chỉ `learning-hub` cần sửa (mô tả, chế độ `index`, bước nghiên cứu, cột mục lục, thêm "Checklist trước khi báo xong"). Phát hiện `.agents/rules/INDEX.md` chưa liệt kê `learning-hub` (thiếu từ TB-011); theo yêu cầu người dùng, đã thêm một dòng vào bảng skill (sau `angular-learning`).
- 2026-10-23: push branch và mở PR [#9](https://github.com/sowndv02/teamboard/pull/9), chuyển sang review. Merge thử với `main` sạch; plan TB-001 đã có trên `main` nên link trong `docker-compose.md` sẽ hết gãy sau khi merge.
- Còn lại, chưa làm: mở lại các nguồn mức Vừa (Zuul, AWS API Gateway, K8s Gateway API, Stack Overflow, Slack, Honeycomb, Netflix migration...); chạy thử ví dụ Valkey/C#/unshare; chưa commit.
- Điểm lệch ngoài phạm vi, báo riêng: `patterns-catalog.md` (dòng 38, 78, 91) viết "Redis" trong khi chính sách yêu cầu Valkey; `config/dependencies.yaml:58` ghi "ElastiCache dùng Valkey (kiểm tra)" chưa kiểm; client .NET Redis chưa có trong `dependencies.yaml`.

## Bài học (lessons learned)

- Chưa có.
