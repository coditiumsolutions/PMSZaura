param(
    [string]$SshHost = "34.131.132.158",
    [string]$SshUser = "coditiums",
    [string]$IdentityFile = "D:\.ssh\Learning\gcp_coditium_vm",
    [string]$PublishOutput = "$PSScriptRoot/publish-out/linux-x64",
    [string]$RemoteDeployScript = "$PSScriptRoot/deploy-gcp-remote.sh",
    # Live systemd WorkingDirectory / ExecStart path (not /var/www/pms/app)
    [string]$AppDir = "/var/www/pms",
    [string]$KeysDir = "/var/www/pms/data-protection-keys",
    [string]$ServiceName = "pms",
    [string]$DomainName = "pms.coditium.com"
)

$ErrorActionPreference = "Stop"

function Resolve-SshIdentityFile {
    param([string]$Preferred)

    $candidates = @(
        $Preferred,
        "D:\.ssh\Learning\gcp_coditium_vm",
        "D:\.ssh\GCP ssh\github_deploy_key"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { return (Resolve-Path -LiteralPath $c).Path }
    }

    throw "SSH private key not found. Tried:`n - $($candidates -join "`n - ")"
}

function Get-SshKeyPath {
    param([string]$SourceKey)

    $secureKey = Join-Path $env:TEMP "gcp_vm_key_deploy"
    Copy-Item -LiteralPath $SourceKey $secureKey -Force
    icacls $secureKey /inheritance:r | Out-Null
    icacls $secureKey /grant:r "$($env:USERNAME):R" | Out-Null
    return $secureKey
}

Write-Host "=== PMS GCP Deploy ===" -ForegroundColor Cyan
$IdentityFile = Resolve-SshIdentityFile -Preferred $IdentityFile
Write-Host "Target: ${SshUser}@${SshHost}" -ForegroundColor Yellow
Write-Host "Key: $IdentityFile" -ForegroundColor Yellow
Write-Host "App dir: $AppDir" -ForegroundColor Yellow

$keyPath = Get-SshKeyPath -SourceKey $IdentityFile
$sshTarget = "${SshUser}@${SshHost}"
$sshBase = @("-F", "NUL", "-o", "StrictHostKeyChecking=accept-new", "-o", "ConnectTimeout=20", "-i", $keyPath)
$tarPath = Join-Path $env:TEMP "pms-gcp-deploy.tar.gz"

try {
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
    if ($LASTEXITCODE -ne 0) { throw "scp upload failed (exit $LASTEXITCODE)" }

    Write-Host "`n[4/4] Running remote deploy script..." -ForegroundColor Green
    if (-not (Test-Path $RemoteDeployScript)) {
        throw "Remote deploy script not found: $RemoteDeployScript"
    }
    $remoteScript = [IO.File]::ReadAllText((Resolve-Path $RemoteDeployScript)) -replace "`r`n", "`n"
    $remoteEnv = @"
export SERVER_NAME='$SshHost'
export DOMAIN_NAME='$DomainName'
export APP_DIR='$AppDir'
export SERVICE_NAME='$ServiceName'
export KEYS_DIR='$KeysDir'
"@
    ($remoteEnv + "`n" + $remoteScript) | & ssh @sshBase $sshTarget "bash -s"
    if ($LASTEXITCODE -ne 0) { throw "Remote deploy failed (exit $LASTEXITCODE)" }

    Write-Host "`nDeployment complete." -ForegroundColor Cyan
    Write-Host "Site: https://${DomainName}/" -ForegroundColor Green
    Write-Host "IP:   http://${SshHost}/" -ForegroundColor Green
}
finally {
    & ssh @sshBase $sshTarget "rm -f /tmp/pms-gcp-deploy.tar.gz" 2>$null | Out-Null
    Remove-Item $keyPath -Force -ErrorAction SilentlyContinue
    Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
}
