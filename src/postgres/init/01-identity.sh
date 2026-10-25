#!/bin/sh
# Tạo database và user riêng cho identity-service (design D0.2): user không có quyền quản trị,
# và role khác dùng chung instance không kết nối được vào database này.
# Chạy một lần khi volume dữ liệu còn trống; task-service dùng lại instance và thêm database riêng sau.
set -eu

psql -v ON_ERROR_STOP=1 -v identity_password="$IDENTITY_DB_PASSWORD" --username "$POSTGRES_USER" --dbname postgres <<'SQL'
CREATE ROLE identity LOGIN PASSWORD :'identity_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
CREATE DATABASE identity OWNER identity;
-- Mặc định PostgreSQL cho mọi role quyền CONNECT; thu lại để chỉ chủ sở hữu (và superuser) vào được.
REVOKE CONNECT ON DATABASE identity FROM PUBLIC;
SQL
