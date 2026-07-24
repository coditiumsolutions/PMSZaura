param(
    [string]$SshHost = "34.131.132.158",
    [string]$SshUser = "coditiumsolutions",
    [string]$IdentityFile = "D:\.ssh\GCP ssh\github_deploy_key",
    [string]$PublishOutput = "$PSScriptRoot/publish-out/linux-x64",
    [string]$RemoteDeployScript = "$PSScriptRoot/deploy-gcp-remote.sh"
)

$ErrorActionPreference = "Stop"

function Get-SshKeyPath {
    param([string]$SourceKey)

    if (-not (Test-Path $SourceKey)) {
        throw "SSH private key not found: $SourceKey"
    }

    $secureKey = Join-Path $env:TEMP "gcp_vm_key_deploy"
    Copy-Item $SourceKey $secureKey -Force
    icacls $secureKey /inheritance:r | Out-Null
    icacls $secureKey /grant:r "$($env:USERNAME):R" | Out-Null
    return $secureKey
}

Write-Host "=== PMS GCP Deploy ===" -ForegroundColor Cyan
Write-Host "Target: ${SshUser}@${SshHost}" -ForegroundColor Yellow

$keyPath = Get-SshKeyPath -SourceKey $IdentityFile
$sshTarget = "${SshUser}@${SshHost}"
$sshBase = @("-F", "NUL", "-o", "StrictHostKeyChecking=accept-new", "-i", $keyPath)
$tarPath = Join-Path $env:TEMP "pms-gcp-deploy.tar.gz"

Write-Host "`n[1/4] Publishing project..." -ForegroundColor Green
dotnet publish "$PSScriptRoot/PMS.csproj" `
    --configuration Release `
    --runtime linux-x64 `
    --self-contained false `
    --output $PublishOutput

if (-not (Test-Path "$PublishOutput/PMS.dll")) {
    throw "Publish failed: PMS.dll not found in $PublishOutput"
}

Write-Host "`n[2/4] Creating deployment package..." -ForegroundColor Green
if (Test-Path $tarPath) {
    Remove-Item $tarPath -Force
}
Push-Location $PublishOutput
try {
    tar -czf $tarPath .
}
finally {
    Pop-Location
}
Write-Host ("Package size: {0:N2} MB" -f ((Get-Item $tarPath).Length / 1MB))

Write-Host "`n[3/4] Uploading package..." -ForegroundColor Green
& scp @sshBase $tarPath "${sshTarget}:/tmp/pms-gcp-deploy.tar.gz"

Write-Host "`n[4/4] Running remote deploy script..." -ForegroundColor Green
if (-not (Test-Path $RemoteDeployScript)) {
    throw "Remote deploy script not found: $RemoteDeployScript"
}
$remoteScript = [IO.File]::ReadAllText((Resolve-Path $RemoteDeployScript)) -replace "`r`n", "`n"
$remoteEnv = @"
export SERVER_NAME='$SshHost'
export APP_DIR='/var/www/pms/app'
export SERVICE_NAME='pms'
export KEYS_DIR='/var/www/pms/data-protection-keys'
"@
($remoteEnv + "`n" + $remoteScript) | & ssh @sshBase $sshTarget "bash -s"

Write-Host "`nDeployment complete." -ForegroundColor Cyan
Write-Host "Site: http://${SshHost}/" -ForegroundColor Green

& ssh @sshBase $sshTarget "rm -f /tmp/pms-gcp-deploy.tar.gz" | Out-Null
Remove-Item $keyPath -Force -ErrorAction SilentlyContinue
Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
