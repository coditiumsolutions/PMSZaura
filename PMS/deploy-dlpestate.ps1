param(
    [Parameter(Mandatory = $false)]
    [string]$SshHost = "93.127.199.220",

    [Parameter(Mandatory = $false)]
    [string]$SshUser = "root",

    [string]$IdentityFile = "",

    [int]$SshPort = 22,
    [string]$Domain = "dlpestate.com",
    [string]$RemotePath = "/var/www/dlpestate/app",
    [string]$ServiceName = "dlpestate-pms",
    [string]$PublishOutput = "$PSScriptRoot/publish-out/linux-x64",
    [switch]$SetupServer
)

$ErrorActionPreference = "Stop"

function Invoke-RemoteBash {
    param(
        [string]$Target,
        [int]$Port,
        [string]$Key,
        [string]$Script
    )
    $lfScript = ($Script -replace "`r`n", "`n") -replace "`r", ""
    $lfScript | ssh -F NUL -p $Port -i $Key $Target "bash -s"
}

Write-Host "=== PMS deploy to $Domain ===" -ForegroundColor Cyan
Write-Host "Host: ${SshUser}@${SshHost}:${SshPort}" -ForegroundColor Yellow
Write-Host "Remote path: $RemotePath" -ForegroundColor Yellow

$repoRoot = Split-Path $PSScriptRoot -Parent
$sshTxtPath = Join-Path $repoRoot "ssh.txt"
$keyCandidates = @(
    $IdentityFile,
    "D:/.ssh/hostinger_vps",
    (Join-Path $repoRoot ".ssh/hostinger_dlpestate")
) | Where-Object { $_ -and $_.Trim() }

$IdentityFile = $keyCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $IdentityFile) {
    throw "SSH private key not found. Add hostinger_vps key or pass -IdentityFile."
}

if (Test-Path $sshTxtPath) {
    $expected = ssh-keygen -lf $sshTxtPath 2>$null
    $actual = ssh-keygen -lf $IdentityFile 2>$null
    if ($expected -and $actual) {
        $expectedFp = ($expected -split '\s+')[1]
        $actualFp = ($actual -split '\s+')[1]
        if ($expectedFp -ne $actualFp) {
            throw "SSH key fingerprint mismatch. ssh.txt expects $expectedFp but key is $actualFp"
        }
        Write-Host "SSH key verified against ssh.txt ($actualFp)" -ForegroundColor Green
    }
}

Write-Host "`n[1/4] Publishing project for Linux..." -ForegroundColor Green
$env:DOTNET_ENVIRONMENT = "Production"
dotnet publish "$PSScriptRoot/PMS.csproj" `
    --configuration Release `
    --runtime linux-x64 `
    --self-contained false `
    --output $PublishOutput

Write-Host "`n[2/4] Packaging and uploading (tar.gz preserves Linux directory permissions)..." -ForegroundColor Green
$sshTarget = "${SshUser}@${SshHost}"
$tarFile = Join-Path $env:TEMP "dlpestate-deploy.tar.gz"
if (Test-Path $tarFile) { Remove-Item $tarFile -Force }

Push-Location $PublishOutput
try {
    tar -czf $tarFile .
} finally {
    Pop-Location
}

ssh -F NUL -p $SshPort -i $IdentityFile $sshTarget "mkdir -p '$RemotePath'"
scp -F NUL -P $SshPort -i $IdentityFile $tarFile "${sshTarget}:/root/dlpestate-deploy.tar.gz"
Remove-Item $tarFile -Force -ErrorAction SilentlyContinue

if ($SetupServer) {
    Write-Host "`n[3/4] Configuring nginx + systemd on server..." -ForegroundColor Green
    $setupScript = @'
#!/bin/bash
set -e
DOMAIN='__DOMAIN__'
REMOTE_PATH='__REMOTE_PATH__'
SERVICE_NAME='__SERVICE_NAME__'
APP_PORT=5000

sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0 nginx

sudo mkdir -p "$REMOTE_PATH" /var/www/dlpestate/data-protection-keys
sudo chown -R www-data:www-data /var/www/dlpestate

sudo tee /etc/systemd/system/${SERVICE_NAME}.service > /dev/null <<EOF
[Unit]
Description=PMS on ${DOMAIN}
After=network.target

[Service]
WorkingDirectory=${REMOTE_PATH}
ExecStart=/usr/bin/dotnet ${REMOTE_PATH}/PMS.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

sudo tee /etc/nginx/sites-available/${DOMAIN} > /dev/null <<EOF
server {
    listen 80;
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

sudo ln -sf /etc/nginx/sites-available/${DOMAIN} /etc/nginx/sites-enabled/${DOMAIN}
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl daemon-reload
sudo systemctl enable ${SERVICE_NAME}
sudo systemctl restart ${SERVICE_NAME}
sudo systemctl reload nginx
'@
    $setupScript = $setupScript.Replace('__DOMAIN__', $Domain).Replace('__REMOTE_PATH__', $RemotePath).Replace('__SERVICE_NAME__', $ServiceName)
    Invoke-RemoteBash -Target $sshTarget -Port $SshPort -Key $IdentityFile -Script $setupScript
} else {
    Write-Host "`n[3/4] Skipping server setup (use -SetupServer for first deploy)..." -ForegroundColor Yellow
}

Write-Host "`n[4/4] Extracting on server and restarting application..." -ForegroundColor Green
$remoteDeploy = @"
set -e
systemctl stop '$ServiceName' 2>/dev/null || true
rm -rf '$RemotePath'/*
tar -xzf /root/dlpestate-deploy.tar.gz -C '$RemotePath'
find '$RemotePath' -type d -exec chmod 755 {} +
find '$RemotePath' -type f -exec chmod 644 {} +
chown -R www-data:www-data /var/www/dlpestate
systemctl restart '$ServiceName' 2>/dev/null || echo 'Service not configured yet. Run with -SetupServer on first deploy.'
sleep 3
curl -s -o /dev/null -w 'bootstrap:%{http_code} theme:%{http_code}\n' -H 'Host: $Domain' http://127.0.0.1/lib/bootstrap/dist/css/bootstrap.min.css -H 'Host: $Domain' http://127.0.0.1/css/property-management-theme.css
"@
Invoke-RemoteBash -Target $sshTarget -Port $SshPort -Key $IdentityFile -Script $remoteDeploy

Write-Host "`nDeployment upload finished." -ForegroundColor Cyan
Write-Host "Verify: https://$Domain/" -ForegroundColor Green
Write-Host "If DNS still shows Hostinger parking page, point dlpestate.com A record to $SshHost in Hostinger DNS." -ForegroundColor Yellow
