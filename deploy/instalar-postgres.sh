#!/usr/bin/env bash
# Instala PostgreSQL en el servidor y crea la base "love" con su usuario. Idempotente: se puede repetir.
# Uso (en /srv/nosotros):  sudo ./deploy/instalar-postgres.sh
# Escribe ConnectionStrings__Love en /etc/love.env con una contraseña aleatoria (nunca va al repo).
# Al siguiente arranque, la app ve Postgres vacío y /opt/love/App_Data/love.db, y lo importa sola.
set -euo pipefail
[ "$(id -u)" = 0 ] || { echo "Ejecuta con sudo"; exit 1; }
ENV=${LOVE_ENV:-/etc/love.env}   # en desarrollo: sudo LOVE_ENV=App_Data/love.env ./deploy/instalar-postgres.sh

if ! command -v psql >/dev/null; then
  apt-get update -qq
  apt-get install -y -qq postgresql
fi
systemctl enable --now postgresql

existe_rol=$(sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='love'")
if grep -q '^ConnectionStrings__Love=' "$ENV" 2>/dev/null && [ "$existe_rol" = 1 ]; then
  echo "Ya configurado (rol love y ConnectionStrings__Love en $ENV)."
else
  PASS=$(openssl rand -hex 24)
  if [ "$existe_rol" = 1 ]; then
    sudo -u postgres psql -q -v ON_ERROR_STOP=1 -c "ALTER ROLE love WITH LOGIN PASSWORD '$PASS'"
  else
    sudo -u postgres psql -q -v ON_ERROR_STOP=1 -c "CREATE ROLE love WITH LOGIN PASSWORD '$PASS'"
  fi
  touch "$ENV"; chmod 600 "$ENV"
  sed -i '/^ConnectionStrings__Love=/d' "$ENV"
  echo "ConnectionStrings__Love=Host=localhost;Database=love;Username=love;Password=$PASS" >> "$ENV"
  [ -n "${LOVE_ENV:-}" ] && [ -n "${SUDO_USER:-}" ] && chown "$SUDO_USER" "$ENV"   # en desarrollo, legible por el usuario
  echo "Rol love listo y cadena escrita en $ENV."
fi
if [ "$(sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='love'")" != 1 ]; then
  sudo -u postgres createdb -O love -E UTF8 -T template0 love
  echo "Base love creada."
fi
sudo -u postgres psql -tAc "SELECT version()"
echo "Siguiente paso: ./deploy/publicar.sh (importa la base SQLite al arrancar; mira: journalctl -u love | grep -i import)"
