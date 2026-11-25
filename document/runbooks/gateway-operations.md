# Vận hành gateway (local và dev)

Runbook cho service `gateway` (YARP). Chi tiết thiết kế: [`document/design/api-gateway.md`](../design/api-gateway.md).

## Cảnh báo bảo mật (bắt buộc đọc)

**Do not expose the gateway on the public internet before JWT validation is in place at the gateway.**

Ở các lát hiện tại, gateway chưa kiểm JWT; mọi client đều có thể gọi API công khai qua cổng published. Chỉ chạy gateway trong mạng nội bộ, VPN hoặc `localhost` cho đến khi identity và bước xác thực JWT được triển khai theo spec. Không publish cổng `8080` ra internet công cộng trước khi có JWT.

## Health và quan sát

- Health: `GET http://localhost:8080/health/live`, `GET http://localhost:8080/health/ready`.
- Jaeger cục bộ: `src/docker-compose.observability.yml` (khi bật).
- Access log và metric: xem design mục 9; không log `Authorization`, cookie hay body.

## Docker Compose

- Entry file: `src/docker-compose.yml`; gateway service `stop_grace_period: 35s`, logging `json-file` với rotation (`max-size` / `max-file`) cho retention local ~7 ngày ở dev.
- `Host:ShutdownTimeout` trong `appsettings.json` của gateway ≥ 30 giây để graceful shutdown khớp compose.

## Lệnh thường dùng

Xem `COMMANDS.md` § compose và test gateway. Sau khi đổi cấu hình: `docker compose config -q` trong `src/`.