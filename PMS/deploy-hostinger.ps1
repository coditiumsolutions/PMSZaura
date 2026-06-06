param(
    [Parameter(Mandatory = $true)]
    [string]$SshHost,

    [Parameter(Mandatory = $true)]
    [string]$SshUser,

    [Parameter(Mandatory = $true)]
    [string]$IdentityFile,

    [int]$SshPort = 22,
    [string]$RemotePath = "/var/www/dlpestate/app",
    [string]$ServiceName = "dlpestate-pms",
    [string]$PublishOutput = "$PSScriptRoot/publish-out/linux-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== PMS Hostinger Deploy ===" -ForegroundColor Cyan
Write-Host "Host: ${SshUser}@${SshHost}:${SshPort}" -ForegroundColor Yellow
Write-Host "Remote path: $RemotePath" -ForegroundColor Yellow

if (-not (Test-Path $IdentityFile)) {
    throw "SSH private key not found: $IdentityFile"
}

$publicKeyLine = Get-Content "$PSScriptRoot/../ssh.txt" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($publicKeyLine) {
    $expectedFingerprint = (ssh-keygen -lf "$PSScriptRoot/../ssh.txt" 2>$null)
    $actualFingerprint = (ssh-keygen -lf $IdentityFile 2>$null)
    if ($expectedFingerprint -and $actualFingerprint -and ($expectedFingerprint -notmatch ($actualFingerprint -replace ' .*',''))) {
        Write-Warning "The private key fingerprint may not match ssh.txt public key."
        Write-Warning "Expected: $expectedFingerprint"
        Write-Warning "Actual:   $actualFingerprint"
    }
}

Write-Host "`n[1/3] Publishing project..." -ForegroundColor Green
dotnet publish "$PSScriptRoot/PMS.csproj" `
    --configuration Release `
    --runtime linux-x64 `
    --self-contained false `
    --output $PublishOutput

Write-Host "`n[2/3] Uploading files..." -ForegroundColor Green
$sshTarget = "${SshUser}@${SshHost}"
$scpArgs = @("-P", $SshPort, "-i", $IdentityFile, "-r", "$PublishOutput/*", "${sshTarget}:${RemotePath}/")
scp @scpArgs

Write-Host "`n[3/3] Restarting remote service..." -ForegroundColor Green
$remoteCmd = @"
set -e
sudo mkdir -p '$RemotePath'
sudo chown -R www-data:www-data '$RemotePath' || true
if systemctl list-unit-files | grep -q '$ServiceName'; then
  sudo systemctl restart '$ServiceName'
  sudo systemctl status '$ServiceName' --no-pager -l | sed -n '1,12p'
else
  echo 'Service $ServiceName not found. Upload complete; configure systemd/nginx on the server.'
fi
"@
ssh -p $SshPort -i $IdentityFile $sshTarget $remoteCmd

Write-Host "`nDeployment upload finished." -ForegroundColor Cyan
Write-Host "Verify: https://dlpestate.com/" -ForegroundColor Green
