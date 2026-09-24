param(
    [string]$SshHost = "34.93.239.49",
    [string]$SshUser = "zaura_coditium",
    [string]$IdentityFile = "D:\.ssh\zauracoditium_gcp\key_gcp_zaura",
    [string]$PublishOutput = "$PSScriptRoot/publish-out/linux-x64",
    [string]$RemoteDeployScript = "$PSScriptRoot/deploy-gcp-remote.sh",
    # Live systemd WorkingDirectory / ExecStart path (not /var/www/pms/app)
    [string]$AppDir = "/var/www/pms",
    [string]$KeysDir = "/var/www/pms/data-protection-keys",
    [string]$ServiceName = "pms",
    [string]$DomainName = "zaura.coditium.com",
    # Read from local (gitignored) appsettings.json so credentials stay out of source control.
    [string]$DbConnection = ""
)

$ErrorActionPreference = "Stop"

function Resolve-SshIdentityFile {
    param([string]$Preferred)

    $candidates = @(
        $Preferred,
        "D:\.ssh\zauracoditium_gcp\key_gcp_zaura",
        "D:\.ssh\Learning\gcp_coditium_vm",
        "D:\.ssh\GCP ssh\github_deploy_key"
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) { return (Resolve-Path -LiteralPath $c).Path }
    }

    throw "SSH private key not found. Tried:`n - $($candidates -join "`n - ")"
}

function Resolve-DbConnection {
    param([string]$Preferred)

    if (-not [string]::IsNullOrWhiteSpace($Preferred)) { return $Preferred }

    $settings = Join-Path $PSScriptRoot "appsettings.json"
    if (Test-Path -LiteralPath $settings) {
        $value = (Get-Content -LiteralPath $settings -Raw |
            ConvertFrom-Json).ConnectionStrings.DefaultConnection
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value }
    }

    throw "No database connection string. Pass -DbConnection or set ConnectionStrings:DefaultConnection in $settings"
}

function Get-SshKeyPath {
    param([string]$SourceKey)

    $secureKey = Join-Path $env:TEMP "gcp_vm_key_deploy"
    # A leftover copy from a failed run is read-only, which blocks Copy-Item.
    if (Test-Path -LiteralPath $secureKey) {
        icacls $secureKey /grant:r "$($env:USERNAME):F" | Out-Null
        Remove-Item -LiteralPath $secureKey -Force
    }
    Copy-Item -LiteralPath $SourceKey $secureKey -Force
    icacls $secureKey /inheritance:r | Out-Null
    icacls $secureKey /grant:r "$($env:USERNAME):R" | Out-Null
    return $secureKey
}

Write-Host "=== PMS GCP Deploy ===" -ForegroundColor Cyan
$IdentityFile = Resolve-SshIdentityFile -Preferred $IdentityFile
$DbConnection = Resolve-DbConnection -Preferred $DbConnection
# SQL Server runs on the target VM itself and 1433 is not open to the internet,
# so the app must reach it over loopback rather than the public IP.
if ($DbConnection -match [regex]::Escape($SshHost)) {
    $DbConnection = $DbConnection -replace [regex]::Escape($SshHost), "127.0.0.1"
    Write-Host "DB host $SshHost rewritten to 127.0.0.1 (database is local to the VM)" -ForegroundColor Yellow
}
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
export DB_CONNECTION='$DbConnection'
"@
    # Normalize the env block too: stray CRs leak into values (e.g. systemd "pms\x0d.service").
    $remotePayload = ($remoteEnv + "`n" + $remoteScript) -replace "`r`n", "`n" -replace "`r", ""
    # Send base64 rather than piping: PowerShell appends CRLF to piped native input,
    # which bash reports as `$'\r': command not found` on the final line.
    $encoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($remotePayload))
    & ssh @sshBase $sshTarget "echo $encoded | base64 -d | bash"
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
