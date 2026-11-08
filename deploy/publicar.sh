#!/usr/bin/env bash
# Publica la app en /opt/love y reinicia el servicio. Ejecutar desde la carpeta del proyecto.
set -euo pipefail
export PATH=$PATH:$HOME/.dotnet DOTNET_ROOT=${DOTNET_ROOT:-$([ -x /usr/lib/dotnet/dotnet ] && echo /usr/lib/dotnet || echo $HOME/.dotnet)}
dotnet publish -c Release -o publish --self-contained false
sudo mkdir -p /opt/love/App_Data
sudo rsync -a --delete --exclude App_Data --exclude appsettings.Development.json publish/ /opt/love/
sudo chown -R www-data:www-data /opt/love
sudo cp deploy/love.service /etc/systemd/system/love.service
sudo systemctl daemon-reload
sudo systemctl enable --now love
sudo systemctl restart love
systemctl status love --no-pager | head -5
