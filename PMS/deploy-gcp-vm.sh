#!/usr/bin/env bash
set -euo pipefail

APP_PORT="8080"
APP_DIR="/var/www/pms/app"
SERVICE_NAME="pms"
APP_USER="www-data"
DOMAIN="${1:-34.93.208.98}"

echo "=== Deploying PMS on GCP Debian VM ==="
echo "Domain/IP: $DOMAIN"
echo "App dir: $APP_DIR"

if [[ $EUID -ne 0 ]]; then
  echo "Please run this script as root or with sudo."
  exit 1
fi

apt-get update
apt-get install -y curl wget apt-transport-https ca-certificates gnupg nginx

if ! command -v dotnet >/dev/null 2>&1; then
  wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y aspnetcore-runtime-8.0
fi

mkdir -p "$APP_DIR"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"

if [[ ! -f "$APP_DIR/PMS.dll" ]]; then
  echo "PMS.dll not found in $APP_DIR"
  echo "Upload the published app first:"
  echo "  scp -r ./publish-out/linux-x64/* USERNAME@34.93.208.98:$APP_DIR/"
  exit 1
fi

cat > /etc/systemd/system/${SERVICE_NAME}.service <<EOF
[Unit]
Description=PMS on GCP VM
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/PMS.dll
Restart=always
RestartSec=10
User=${APP_USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

cat > /etc/nginx/sites-available/${DOMAIN} <<EOF
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN};

    client_max_body_size 50M;

    location / {
        proxy_pass http://127.0.0.1:${APP_PORT};
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

ln -sfn /etc/nginx/sites-available/${DOMAIN} /etc/nginx/sites-enabled/${DOMAIN}
rm -f /etc/nginx/sites-enabled/default

systemctl daemon-reload
systemctl enable nginx
systemctl restart nginx
systemctl enable ${SERVICE_NAME}
systemctl restart ${SERVICE_NAME}

nginx -t
systemctl status ${SERVICE_NAME} --no-pager -l | head -20

echo ""
echo "Deployment complete."
echo "Open: http://${DOMAIN}/"
