#!/usr/bin/env bash
set -euo pipefail

# Live systemd unit uses WorkingDirectory=/var/www/pms (not /var/www/pms/app).
APP_DIR="${APP_DIR:-/var/www/pms}"
KEYS_DIR="${KEYS_DIR:-/var/www/pms/data-protection-keys}"
SERVICE_NAME="${SERVICE_NAME:-pms}"
APP_PORT="${APP_PORT:-8080}"
APP_USER="${APP_USER:-www-data}"
SERVER_NAME="${SERVER_NAME:-34.93.239.49}"
DOMAIN_NAME="${DOMAIN_NAME:-zaura.coditium.com}"
# Treat blank DOMAIN_NAME (e.g. empty CI secret) as unset so HTTPS uses the real hostname.
if [ -z "${DOMAIN_NAME// }" ]; then
  DOMAIN_NAME="zaura.coditium.com"
fi
# Prefer env override from CI; keep quoted for systemd (semicolons/spaces in User Id=…).
DB_CONNECTION="${DB_CONNECTION:-Server=127.0.0.1;Database=DBZaura;User Id=sa;Password=Pakistan@786;Encrypt=Mandatory;TrustServerCertificate=true;}"
# Zaura VM hosts Python report API on loopback; PreferLocalRdlc must stay false
# because ReportViewerCore cannot render PDF on Linux (missing usp10.dll).
REPORT_SERVICE_BASE_URL="${REPORT_SERVICE_BASE_URL:-http://127.0.0.1:8000}"
REPORT_SERVICE_PREFER_LOCAL_RDLC="${REPORT_SERVICE_PREFER_LOCAL_RDLC:-false}"
REPORT_SERVICE_TIMEOUT_SECONDS="${REPORT_SERVICE_TIMEOUT_SECONDS:-60}"
DOTNET_BIN="$(command -v dotnet)"
STAGING="${STAGING:-/tmp/pms-deploy-staging}"
PACKAGE="${PACKAGE:-/tmp/pms-gcp-deploy.tar.gz}"

echo "Using dotnet: $DOTNET_BIN"
"$DOTNET_BIN" --version
echo "Server: $SERVER_NAME"
echo "Domain: $DOMAIN_NAME"
echo "App dir: $APP_DIR"
echo "Keys dir: $KEYS_DIR"
echo "Service: $SERVICE_NAME"
echo "App port: $APP_PORT"

if [[ ! -f "$PACKAGE" ]]; then
  echo "ERROR: package not found: $PACKAGE"
  exit 1
fi

if ! command -v nginx >/dev/null 2>&1; then
  echo "nginx not found; installing..."
  sudo DEBIAN_FRONTEND=noninteractive apt-get update -y
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y nginx
fi

sudo systemctl stop "$SERVICE_NAME" 2>/dev/null || true
sudo systemctl unmask "$SERVICE_NAME" 2>/dev/null || true

sudo mkdir -p "$APP_DIR" "$KEYS_DIR"

# Stage extract, then sync into APP_DIR while preserving uploads + DP keys.
rm -rf "$STAGING"
mkdir -p "$STAGING"
tar -xzf "$PACKAGE" -C "$STAGING"

if command -v rsync >/dev/null 2>&1; then
  sudo rsync -a --delete \
    --exclude 'wwwroot/uploads/' \
    --exclude 'data-protection-keys/' \
    --exclude '.cache/' \
    --exclude '.wine/' \
    --exclude 'tmp/' \
    "$STAGING/" "$APP_DIR/"
else
  # Fallback without rsync: overwrite published files; do not wipe uploads/keys.
  sudo tar -xzf "$PACKAGE" -C "$APP_DIR"
fi

rm -rf "$STAGING"
sudo mkdir -p "$APP_DIR/wwwroot/uploads" "$KEYS_DIR"
sudo find "$APP_DIR" -type d -exec chmod 755 {} +
sudo find "$APP_DIR" -type f -exec chmod 644 {} +
sudo chown -R "$APP_USER:$APP_USER" "$APP_DIR"

# EnvironmentFile keeps spaces in "User Id=..." intact (inline Environment= splits on space → HTTP 500).
sudo tee /etc/pms.env > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
DOTNET_ROLL_FORWARD=LatestMajor
DOTNET_PRINT_TELEMETRY_MESSAGE=false
AllowedHosts=*
HOME=${APP_DIR}
DataProtection__KeysPath=${KEYS_DIR}
ConnectionStrings__DefaultConnection=${DB_CONNECTION}
ReportService__BaseUrl=${REPORT_SERVICE_BASE_URL}
ReportService__PreferLocalRdlc=${REPORT_SERVICE_PREFER_LOCAL_RDLC}
ReportService__TimeoutSeconds=${REPORT_SERVICE_TIMEOUT_SECONDS}
EOF
sudo chmod 600 /etc/pms.env

sudo tee /etc/systemd/system/${SERVICE_NAME}.service > /dev/null <<EOF
[Unit]
Description=PMS on GCP VM
After=network.target mssql-server.service
Wants=mssql-server.service

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=${DOTNET_BIN} ${APP_DIR}/PMS.dll
Restart=always
RestartSec=10
User=${APP_USER}
EnvironmentFile=/etc/pms.env

[Install]
WantedBy=multi-user.target
EOF

# Prefer HTTPS when Let's Encrypt certs already exist (avoids HTTP-only wipe on redeploy).
# Cert paths under /etc/letsencrypt/live are root-only — must use sudo to detect them.
CERT_DIR="/etc/letsencrypt/live/${DOMAIN_NAME}"
if sudo test -f "${CERT_DIR}/fullchain.pem" && sudo test -f "${CERT_DIR}/privkey.pem"; then
  echo "Writing nginx HTTPS config for ${DOMAIN_NAME}..."
  DHPARAM_LINE=""
  if sudo test -f /etc/letsencrypt/ssl-dhparams.pem; then
    DHPARAM_LINE="    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;"
  fi
  SSL_OPTIONS_LINE=""
  if sudo test -f /etc/letsencrypt/options-ssl-nginx.conf; then
    SSL_OPTIONS_LINE="    include /etc/letsencrypt/options-ssl-nginx.conf;"
  fi
  sudo tee /etc/nginx/sites-available/pms > /dev/null <<NGINX
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_NAME} ${SERVER_NAME};
    return 301 https://\$host\$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name ${DOMAIN_NAME} ${SERVER_NAME};

    ssl_certificate     ${CERT_DIR}/fullchain.pem;
    ssl_certificate_key ${CERT_DIR}/privkey.pem;
${SSL_OPTIONS_LINE}
${DHPARAM_LINE}

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
NGINX
else
  echo "No certs for ${DOMAIN_NAME}; writing HTTP-only nginx (run certbot later)."
  sudo tee /etc/nginx/sites-available/pms > /dev/null <<NGINX
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_NAME} ${SERVER_NAME};

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
NGINX
fi

sudo ln -sfn /etc/nginx/sites-available/pms /etc/nginx/sites-enabled/pms
sudo rm -f /etc/nginx/sites-enabled/default

sudo nginx -t
sudo systemctl daemon-reload
sudo systemctl enable nginx
sudo systemctl restart nginx
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl restart "$SERVICE_NAME"

sleep 8
echo "=== service active ==="
systemctl is-active "$SERVICE_NAME" || true
echo "=== local curls ==="
curl -s -o /dev/null -w "app${APP_PORT}:%{http_code}\n" "http://127.0.0.1:${APP_PORT}/" || true
curl -s -o /dev/null -w 'nginx80:%{http_code}\n' http://127.0.0.1/ || true
curl -sk -o /dev/null -w "https_${DOMAIN_NAME}:%{http_code}\n" \
  --resolve "${DOMAIN_NAME}:443:127.0.0.1" "https://${DOMAIN_NAME}/" || true
echo "=== recent logs ==="
journalctl -u "$SERVICE_NAME" -n 25 --no-pager || true
