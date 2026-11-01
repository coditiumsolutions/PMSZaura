#!/bin/bash
# Run on VPS (Hostinger hPanel -> Browser terminal) as root after uploading dlpestate-deploy.zip to /root/
set -e

DOMAIN="dlpestate.com"
APP_DIR="/var/www/dlpestate/app"
KEYS_DIR="/var/www/dlpestate/data-protection-keys"
SERVICE_NAME="dlpestate-pms"
APP_PORT=5000
ZIP="/root/dlpestate-deploy.zip"

echo "=== Installing PMS on $DOMAIN ==="

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y unzip nginx wget

if ! command -v dotnet >/dev/null 2>&1; then
  wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y aspnetcore-runtime-8.0
fi

if [ ! -f "$ZIP" ]; then
  echo "ERROR: Upload dlpestate-deploy.zip to /root/ first (Hostinger File Manager)."
  exit 1
fi

rm -rf /tmp/pms-deploy
mkdir -p /tmp/pms-deploy "$APP_DIR" "$KEYS_DIR"
unzip -o "$ZIP" -d /tmp/pms-deploy
cp -a /tmp/pms-deploy/. "$APP_DIR/"
chown -R www-data:www-data /var/www/dlpestate

cat > /etc/systemd/system/${SERVICE_NAME}.service <<EOF
[Unit]
Description=PMS on ${DOMAIN}
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/PMS.dll
Restart=always
RestartSec=10
User=www-data
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
    server_name ${DOMAIN} www.${DOMAIN};

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

ln -sf /etc/nginx/sites-available/${DOMAIN} /etc/nginx/sites-enabled/${DOMAIN}
rm -f /etc/nginx/sites-enabled/default

nginx -t
systemctl daemon-reload
systemctl enable ${SERVICE_NAME}
systemctl restart ${SERVICE_NAME}
systemctl reload nginx

echo ""
echo "Deployed. Test: http://${DOMAIN}/"
systemctl --no-pager status ${SERVICE_NAME} | head -20
