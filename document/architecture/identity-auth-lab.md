# Auth lab: identity tự xây và Keycloak (chọn bằng settings)

Trạng thái: **đề xuất đã chốt tài liệu** (2026-11-25). Triển khai code/compose: [TB-050](../issues/TB-050-identity-auth-lab-dual-provider.md).

## Mục đích

- Học lab 1–4 của bộ `auth_std_roadmap-main` **trên dự án** mà không thay `identity-service` bằng Keycloak làm IdP sản phẩm ([ADR 0006](adr/0006-identity-self-built.md)).
- **Một lần chạy chỉ một Authorization Server active**, chọn bằng settings (`Auth:Provider`).
- Keycloak chỉ dùng **local, Development, profile compose tùy chọn** — đối chiếu hành vi khi identity chưa đủ slice hoặc khi debug.

## Vai trò (không đổi dù chọn provider nào)

| Vai trò | Provider = Identity | Provider = Keycloak |
|---|---|---|
| Authorization Server / IdP | `identity-service` | Keycloak realm `demo` |
| Resource Server (lab JWT) | `board-service` / gateway (I9) hoặc RS tối thiểu trong test | Cùng RS; đổi `Authority` |
| Client PKCE / CC | `src/identity-client-lab` (dự kiến TB-050) | Cùng client; đổi endpoint đăng ký |

Lab 5 (SAML) **không** thuộc `identity-service` (I1–I8); chỉ đọc trong [auth-standards Part 4](../learning/auth-standards/part-4-advanced.md).

## Settings — nguồn sự thật cấu hình

Mẫu contract (chưa wire code cho đến TB-050): [`config/auth-development.example.yaml`](../../config/auth-development.example.yaml).

| Khóa | Ý nghĩa |
|---|---|
| `Auth:Provider` | `Identity` (mặc định, đường học chính) hoặc `Keycloak` (lab đối chiếu) |
| `Auth:Identity:Authority` | Issuer/JWKS của identity (vd. `http://localhost:6061`) |
| `Auth:Identity:Audience` | `aud` API mong đợi (vd. `boards-api` hoặc tên client API lab) |
| `Auth:Keycloak:Authority` | `http://localhost:8090/realms/demo` (tránh trùng gateway `8080`) |
| `Auth:Keycloak:Audience` | `demo-api` (theo lab gốc) |

**ASP.NET Core:** bind section `Auth` → mở rộng `AddJwtBearer` đọc `Authority`/`Audience` theo provider. **Biến môi trường:** `Auth__Provider=Keycloak` (chuẩn .NET).

**Ràng buộc vận hành**

- Đổi provider → **restart** process RS/client (cache JWKS/metadata).
- Không hot-swap issuer giữa hai request trong cùng một instance.
- Production / CI chính: **chỉ** `Identity`; Keycloak không được bật (guard `IsDevelopment` khi implement).

## Docker Compose

| Profile | Container |
|---|---|
| (mặc định) | `postgres`, `identity-service` |
| `keycloak-lab` | thêm Keycloak; map host **8090** → 8080 container |

Ví dụ: `docker compose --profile keycloak-lab up` chỉ khi `Auth:Provider=Keycloak`.

Keycloak **không** route qua gateway; gateway hiện chiếm host `8080` (`config/services.yaml`).

## Lab 1–4 → slice identity (deliverable)

| Lab gốc | Output lab (bộ nguồn) | Trên TeamBoard | Slice / issue |
|---|---|---|---|
| 1 — IdP | Realm, client, scope, bảng endpoint | Seed client/user + discovery/JWKS (identity) hoặc cấu hình Keycloak | I5/I6 **TB-040**; user **TB-029–030** |
| 2 — JWT API | DemoApi + bảng 6× 401/403 | JwtBearer trên RS; bảng lỗi giữ nguyên kịch bản | I2/I3 **TB-032–034**, **TB-033** |
| 3 — PKCE client | Console + loopback | `identity-client-lab` → identity authorize/token | I5 **TB-042–043**; I7 **TB-044** (tùy) |
| 4 — CC, refresh, introspection | worker + quan sát token | Endpoint trên identity | I4 **TB-036–038**; I6 **TB-041**; I8 **TB-046** |

Identity **cấp** token; lab 2 **kiểm** token — hai phần output khác nhau, ghép khi RS và identity cùng provider.

## Đặt code (không dùng thư mục `samples/`)

| Thành phần | Vị trí dự kiến |
|---|---|
| OAuth/OIDC server | `src/identity-service` |
| Kịch bản BDD + WebApplicationFactory | `src/identity-service.Tests` |
| Client console PKCE (chạy tay) | `src/identity-client-lab` |
| RS bảng lỗi lab | RS trong test; sau I9 → `board-service` + gateway |

## Khác lab Keycloak vs output identity tự xây

| | Keycloak | identity-service |
|---|---|---|
| Cấu hình | Admin UI, realm | Migration, code, BDD |
| Bạn sửa | Client/scope sai | Logic endpoint, claim, PKCE |
| Mục tiêu dài hạn | Chuẩn vàng để so | Sản phẩm học trong repo |

## Liên kết

- Lộ trình đọc: [auth-standards](../learning/auth-standards/README.md), [identity hands-on](../learning/identity/README.md).
- Lộ trình build: [identity-learning.md](identity-learning.md), [identity-auth-slices.md](../plans/identity-auth-slices.md).
- Issue triển khai: [TB-050](../issues/TB-050-identity-auth-lab-dual-provider.md).