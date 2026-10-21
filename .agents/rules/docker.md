# Docker & compose rules

- Dockerfile multi-stage (build → runtime), base image pin phiên bản (không `latest`), chạy không bằng root, có `HEALTHCHECK` hoặc dựa vào health endpoint.
- Image không chứa secret và không đọc `.env` lúc build; secret/config inject lúc runtime qua env.
- `.dockerignore` loại `bin/`, `obj/`, `.git`, `.env*`, `local/`.
- Compose: mỗi service một mục, cổng khớp `config/services.yaml`, có `depends_on` với `condition: service_healthy` cho dependency, `restart` hợp lý. Tách file theo mục đích (`docker-compose.yml` chính, `*.develop.yml`, `*.stg.yml`, observability, localstack).
- Biến môi trường: tham chiếu `${VAR}` từ file `.env*` (gitignore, có `*.example`). Không ghi giá trị secret vào compose.
- Đổi cổng/tên service → cập nhật `config/services.yaml` cùng lúc.
- Không dùng `docker system prune`, `docker volume rm`, `down -v` khi chưa được người dùng đồng ý (mất dữ liệu local).
