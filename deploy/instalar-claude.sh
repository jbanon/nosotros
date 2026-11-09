#!/bin/bash
# Instala el CLI de Claude Code para el servicio love (usuario www-data) y lo registra en /etc/love.env.
# Uso: sudo ./deploy/instalar-claude.sh [/ruta/al/binario/claude]   (por defecto, el claude del usuario que invoca sudo)
# El token de la suscripción NO va aquí: se añade a mano a /etc/love.env como CLAUDE_CODE_OAUTH_TOKEN=... (claude setup-token).
set -euo pipefail
ORIGEN="${1:-$(sudo -u "${SUDO_USER:-$USER}" -H bash -lc 'readlink -f "$(command -v claude)"')}"
DESTINO=/usr/local/bin/claude
ENV=/etc/love.env
[ -x "$ORIGEN" ] || { echo "No encuentro el binario claude en $ORIGEN"; exit 1; }
install -m 755 -o root -g root "$ORIGEN" "$DESTINO"
echo "Instalado $DESTINO ($("$DESTINO" --version))"
touch "$ENV"; chmod 600 "$ENV"; chown root:root "$ENV"
grep -q '^Agente__Claude=' "$ENV" || echo "Agente__Claude=$DESTINO" >> "$ENV"
grep -q '^DISABLE_AUTOUPDATER=' "$ENV" || echo "DISABLE_AUTOUPDATER=1" >> "$ENV"   # la copia se actualiza con este script, no sola
mkdir -p /opt/love/App_Data && chown www-data:www-data /opt/love/App_Data && chmod u+rwx /opt/love/App_Data
echo "Prueba como www-data:"; sudo -u www-data -H env HOME=/opt/love/App_Data "$DESTINO" --version
systemctl restart love
echo "Servicio reiniciado. Falta (si no está) CLAUDE_CODE_OAUTH_TOKEN=... en $ENV y otro restart."
