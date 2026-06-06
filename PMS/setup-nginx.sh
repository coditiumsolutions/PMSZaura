#!/bin/bash
set -e
DOMAIN=dlpestate.com
APP_DIR=/var/www/dlpestate/app
SERVICE_NAME=dlpestate-pms
APP_PORT=5000

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
sleep 4
systemctl is-active ${SERVICE_NAME}
curl -s -o /dev/null -w "app:%{http_code}\n" http://127.0.0.1:${APP_PORT}/
curl -s -o /dev/null -w "nginx:%{http_code}\n" -H "Host: dlpestate.com" http://127.0.0.1/
