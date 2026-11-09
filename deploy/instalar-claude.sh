#!/bin/bash
# Registra el CLI de Claude Code que ya hay en el servidor para el servicio love (usuario www-data) en /etc/love.env.
# Uso: sudo ./deploy/instalar-claude.sh [/ruta/al/claude]   (por defecto /home/dev/.local/bin/claude, enlace que el
# actualizador de Claude Code mantiene al día). El token de la suscripción NO va aquí: se añade a mano a /etc/love.env
# como CLAUDE_CODE_OAUTH_TOKEN=... (salida de `claude setup-token`).
set -euo pipefail
CLAUDE="${1:-/home/dev/.local/bin/claude}"
ENV=/etc/love.env
[ -x "$CLAUDE" ] || { echo "No encuentro el binario claude en $CLAUDE"; exit 1; }
touch "$ENV"; chmod 600 "$ENV"; chown root:root "$ENV"
grep -q '^Agente__Claude=' "$ENV" || echo "Agente__Claude=$CLAUDE" >> "$ENV"
grep -q '^DISABLE_AUTOUPDATER=' "$ENV" || echo "DISABLE_AUTOUPDATER=1" >> "$ENV"   # que www-data no intente actualizarlo
mkdir -p /opt/love/App_Data && chown www-data:www-data /opt/love/App_Data && chmod u+rwx /opt/love/App_Data
echo "Prueba como www-data:"; sudo -u www-data -H env HOME=/opt/love/App_Data "$CLAUDE" --version
systemctl restart love
echo "Servicio reiniciado. Falta (si no está) CLAUDE_CODE_OAUTH_TOKEN=... en $ENV y otro restart."
