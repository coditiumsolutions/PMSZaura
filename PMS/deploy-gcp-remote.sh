#!/usr/bin/env bash
set -euo pipefail

APP_DIR="/var/www/pms/app"
KEYS_DIR="/var/www/pms/data-protection-keys"
SERVICE_NAME="pms"
APP_PORT="8080"
APP_USER="www-data"
SERVER_NAME="34.93.208.98"
DOTNET_BIN="$(command -v dotnet)"

echo "Using dotnet: $DOTNET_BIN"
"$DOTNET_BIN" --version

sudo systemctl stop "$SERVICE_NAME" 2>/dev/null || true
sudo systemctl unmask "$SERVICE_NAME" 2>/dev/null || true

sudo mkdir -p "$APP_DIR" "$KEYS_DIR"
sudo find "$APP_DIR" -mindepth 1 -delete 2>/dev/null || true
sudo tar -xzf /tmp/pms-gcp-deploy.tar.gz -C "$APP_DIR"
sudo find "$APP_DIR" -type d -exec chmod 755 {} +
sudo find "$APP_DIR" -type f -exec chmod 644 {} +
sudo chown -R "$APP_USER:$APP_USER" /var/www/pms

sudo tee /etc/systemd/system/${SERVICE_NAME}.service > /dev/null <<EOF
[Unit]
Description=PMS on GCP VM
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=${DOTNET_BIN} ${APP_DIR}/PMS.dll
Restart=always
RestartSec=10
User=${APP_USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_ROLL_FORWARD=LatestMajor
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=AllowedHosts=*
Environment=DataProtection__KeysPath=${KEYS_DIR}
Environment=ConnectionStrings__DefaultConnection=Server=localhost;Database=PMSAbbas;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;

[Install]
WantedBy=multi-user.target
EOF

sudo tee /etc/nginx/sites-available/pms > /dev/null <<'NGINX'
server {
    listen 80;
    listen [::]:80;
    server_name 34.93.208.98;

    client_max_body_size 50M;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
NGINX

sudo ln -sfn /etc/nginx/sites-available/pms /etc/nginx/sites-enabled/pms
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl daemon-reload
sudo systemctl enable nginx
sudo systemctl restart nginx
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl restart "$SERVICE_NAME"

sleep 5
echo "=== service active ==="
systemctl is-active "$SERVICE_NAME" || true
echo "=== local curls ==="
curl -s -o /dev/null -w 'app8080:%{http_code}\n' http://127.0.0.1:8080/ || true
curl -s -o /dev/null -w 'nginx80:%{http_code}\n' http://127.0.0.1/ || true
echo "=== recent logs ==="
journalctl -u "$SERVICE_NAME" -n 25 --no-pager || true
