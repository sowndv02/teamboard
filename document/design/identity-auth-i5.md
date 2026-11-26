---
title: Identity I5 — Authorization Code + PKCE (phụ lục)
status: approved
owner: sondv
updated: 2026-11-26
parent: document/design/identity-auth.md
spec: document/spec/identity-auth.md
services: [identity-service]
issues: [TB-042, TB-052]
---

# Design phụ lục I5 — Authorization Code + PKCE

Bổ sung cho `document/design/identity-auth.md` (Confirm log #1: I5 hoãn đến spec FE). **TB-042 plan-review Confirm:** viết phụ lục này trước code (`design-i5`).

## Phạm vi phụ lục

- `GET /connect/authorize`, lưu `authorization_code`, PKCE (S256).
- Phiên cookie IdP trên origin identity (FR-019).
- Trang đăng nhập server-rendered (Razor tối thiểu — Confirm TB-042).
- **Không** mô tả đổi mã tại `POST /connect/token` (TB-043).

## Quyết định (draft — chốt tại plan-review các mục còn `TBD`)

| ID | Chủ đề | Quyết định / hướng |
|---|---|---|
| D-I5-1 | Đường dẫn | `GET /connect/authorize` (ngoại lệ `api-endpoints.md`; ADR riêng khi code) |
| D-I5-2 | Feature flag | `Identity:Features:AuthorizationCodeEnabled`, default **false** |
| D-I5-3 | Cookie IdP | Scheme riêng (`Identity.Idp`); 30 phút idle sliding; `HttpOnly`, `Secure`, `SameSite=Lax` (TBD prod) |
| D-I5-4 | Authorize context | Bảng `authorize_request` TTL ngắn (14b) |
| D-I5-5 | Mã uỷ quyền | Bảng `authorization_code`, `code_hash` unique, TTL **60s**, `used_at`; plaintext chỉ trong redirect một lần |
| D-I5-6 | PKCE | `code_challenge_method=S256` only |
| D-I5-7 | Lỗi authorize | URI chưa tin cậy → HTML/4xx trên origin, không redirect |
| D-I5-8 | UI | Razor Pages tối thiểu — **lát 14b** |
| D-I5-9 | Scope authorize | Bắt buộc; ⊆ `allowed_scopes` |

## Chia lát (Confirm O-9)

| Pha | Issue | Giao được |
|---|---|---|
| 14a | **TB-042** | Phụ lục I5 (bản approved), PKCE validator, bảng mã, `GET /connect/authorize` validate + lỗi an toàn, feature flag |
| 14b | **TB-052** | Cookie IdP, Razor login, redirect kèm `code`, BDD happy path |

TB-043 vẫn phụ thuộc **14 hoàn chỉnh** (authorize + login + phát mã).

## Confirm log (design)

| # | Fingerprint | Quyết định | Ngày |
|---|---|---|---|
| 1 | plan-review:TB-042:design-doc | Phụ lục `identity-auth-i5.md` trước code | 2026-11-26 |
| 2 | plan-review:TB-042:split-lat | TB-042 = 14a; login UI = lát kế (issue TBD) | 2026-11-26 |
| 3 | plan-review:TB-042:authz-error | O-3 lỗi | HTML trên identity, không redirect URI không tin | 2026-11-26 |
| 4 | plan-review:TB-042:authz-context | O-4 context | Server DB session row, TTL ngắn | 2026-11-26 |
| 5 | plan-review:TB-042:pkce-method | O-6 | S256 only | 2026-11-26 |
| 6 | plan-review:TB-042:authz-scope | O-7 | scope bắt buộc, subset allowed | 2026-11-26 |
| 7 | plan-review:TB-042:nonce | O-10 | Lưu nonce | 2026-11-26 |
| 8 | plan-review:TB-042:bdd-redirect | O-11 | `http://localhost:4200/callback` | 2026-11-26 |