#!/bin/sh
# Create a dedicated database and user for identity-service (design D0.2): the user has no admin rights,
# and other roles sharing the instance cannot connect to this database.
# Runs once, while the data volume is still empty; task-service reuses the instance and adds its own database later.
set -eu

psql -v ON_ERROR_STOP=1 -v identity_password="$IDENTITY_DB_PASSWORD" --username "$POSTGRES_USER" --dbname postgres <<'SQL'
CREATE ROLE identity LOGIN PASSWORD :'identity_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
CREATE DATABASE identity OWNER identity;
-- PostgreSQL grants CONNECT to every role by default; revoke it so only the owner (and superusers) can connect.
REVOKE CONNECT ON DATABASE identity FROM PUBLIC;
SQL
