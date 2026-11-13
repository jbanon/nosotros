#!/usr/bin/env bash
# Publica la app en /opt/love y reinicia el servicio. Ejecutar desde la carpeta del proyecto.
set -euo pipefail
# Desde noviembre de 2026 la base es PostgreSQL: sin la cadena en /etc/love.env la app no arranca.
if ! sudo grep -q '^ConnectionStrings__Love=' /etc/love.env; then
  echo "Falta PostgreSQL: ejecuta antes  sudo ./deploy/instalar-postgres.sh"; exit 1
fi
export PATH=$PATH:$HOME/.dotnet DOTNET_ROOT=${DOTNET_ROOT:-$([ -x /usr/lib/dotnet/dotnet ] && echo /usr/lib/dotnet || echo $HOME/.dotnet)}
rm -rf publish   # la salida anterior dentro del proyecto se colaría como contenido en la siguiente publicación
dotnet publish -c Release -o publish --self-contained false
sudo mkdir -p /opt/love/App_Data
sudo rsync -a --delete --exclude App_Data --exclude appsettings.Development.json publish/ /opt/love/
sudo chown -R www-data:www-data /opt/love
sudo cp deploy/love.service /etc/systemd/system/love.service
sudo systemctl daemon-reload
sudo systemctl enable --now love
sudo systemctl restart love
sleep 4
sudo journalctl -u love --since "-1 min" --no-pager | grep -iE "import|fail|exception" | tail -5 || true
systemctl status love --no-pager | head -5
