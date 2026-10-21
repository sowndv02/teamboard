# Giải thích thuật ngữ nâng cao

Lý do tồn tại: người dùng là dev nhưng chưa gặp nhiều tình huống thực tế, nên nhiều thuật ngữ (pattern, giao thức, vận hành, bảo mật) có thể chưa quen. Dùng thuật ngữ mà không giải thích làm tài liệu và câu trả lời khó học, dễ gật đầu mà không hiểu. Áp dụng cho **mọi** câu trả lời của agent và mọi tài liệu agent viết (spec, design, ADR, plan, lesson, báo cáo).

## 1. Thuật ngữ nào cần giải thích

Thuật ngữ **nâng cao hoặc ít gặp**: tên pattern, giao thức, viết tắt chuyên ngành, khái niệm vận hành/bảo mật mà một dev biết lập trình nhưng chưa làm hệ thống phân tán hay bảo mật có thể chưa biết. Danh sách gợi ý (không đầy đủ, nghi ngờ thì giải thích):

| Nhóm | Ví dụ |
|---|---|
| Bảo mật, xác thực | PKCE, JWKS, `kid`, claim, grant, OIDC, CSRF, IDOR, CSP, STRIDE, refresh token rotation và reuse detection, salt, PBKDF2, xoay khoá, BFF |
| Dữ liệu, đồng thời | CAS (compare-and-set), idempotency, optimistic concurrency, outbox, dual write, eventual consistency, event sourcing, CQRS, saga, DLQ, at-least-once |
| Vận hành, tin cậy | SLI/SLO, error budget, p95, blast radius, canary, feature flag, rollback, expand/contract, drift, pre-mortem |
| Kiến trúc | Vertical Slice, Clean Architecture, Hexagonal, shared kernel, bounded context |

**Không cần** giải thích khái niệm cơ bản mà dev nào cũng biết (HTTP, JSON, SQL, git, REST ở mức cơ bản).

## 2. Giải thích thế nào

Lần đầu thuật ngữ xuất hiện, kèm **tối đa ba câu**, gắn với ngữ cảnh dự án:

1. **Là gì** (một câu, từ ngữ thường, không dùng thuật ngữ khác chưa giải thích để giải thích).
2. **Vì sao cần ở đây / giải quyết tình huống nào** (một câu).
3. **Ví dụ rất ngắn** nếu khái niệm trừu tượng (tình huống cụ thể trong TeamBoard).

Ví dụ: "**CAS (compare-and-set)**: cập nhật một giá trị chỉ khi nó vẫn đúng như lúc mình đọc. Ở đây dùng để hai request đổi cùng một refresh token chỉ có một request thành công. Ví dụ: `UPDATE ... WHERE used_at IS NULL` cập nhật được 1 dòng thì thắng, 0 dòng thì thua."

## 3. Ở đâu và khi nào

- **Câu trả lời trong chat, báo cáo:** giải thích ngay trong câu hoặc trong ngoặc ở lần đầu dùng, không chỉ gọi tên và không bắt người đọc mở link.
- **Tài liệu:** lần đầu trong **mỗi tài liệu**, giải thích inline hoặc đưa vào mục "Thuật ngữ" (spec đã có mục này). Không chỉ dán link tới RFC/bài viết.
- **Không lặp:** đã giải thích trong cùng tài liệu hoặc cùng câu trả lời thì không giải thích lại; tài liệu khác có thể trỏ về chỗ đã giải thích.
- **Người dùng hỏi "X là gì":** trả lời đúng ba phần ở mục 2 kèm nơi X xuất hiện trong dự án; không chỉ đưa định nghĩa sách giáo khoa.

## 4. Chất lượng

- **Chính xác, không bịa.** Không chắc nghĩa hoặc chi tiết chuẩn (số hiệu RFC, tên tham số) thì nói không chắc và trỏ nguồn cần đọc.
- Giữ tên tiếng Anh của thuật ngữ, giải thích bằng tiếng Việt, câu ngắn.
- Giải thích đúng **nghĩa trong ngữ cảnh dự án**, không phải liệt kê mọi nghĩa của từ.

## 5. Review

Trong review tài liệu (`spec-review`, `design-review`...), thuật ngữ nâng cao xuất hiện lần đầu mà không có giải thích là lỗi hình thức (FIX). Thêm giải thích đúng, ngắn, không đổi nội dung quyết định.

## Liên kết

`agent-conduct.md` (giao tiếp), `docs-conventions.md` (nội dung tài liệu), template spec (mục "Thuật ngữ"). Chưa có bảng thuật ngữ chung của dự án (`document/glossary.md`); nếu được người dùng đồng ý tạo, thuật ngữ nào đã giải thích nhiều lần nên chuyển vào đó và các tài liệu trỏ về.
