param(
    [string]$SshHost = "34.93.239.49",
    [string]$SshUser = "zaura_coditium",
    [string]$IdentityFile = "D:\.ssh\zauracoditium_gcp\key_gcp_zaura",
    [int]$LocalPort = 14330
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $IdentityFile)) {
    throw "SSH key not found: $IdentityFile"
}

$existing = Get-NetTCPConnection -LocalPort $LocalPort -State Listen -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "SQL tunnel already listening on 127.0.0.1:$LocalPort" -ForegroundColor Green
    exit 0
}

Write-Host "Starting SQL tunnel: 127.0.0.1:$LocalPort -> ${SshHost}:1433" -ForegroundColor Cyan
Write-Host "Keep this window open while developing locally." -ForegroundColor Yellow

ssh -F NUL `
    -o StrictHostKeyChecking=accept-new `
    -o ServerAliveInterval=30 `
    -o ExitOnForwardFailure=yes `
    -i $IdentityFile `
    -N -L "${LocalPort}:127.0.0.1:1433" `
    "${SshUser}@${SshHost}"
