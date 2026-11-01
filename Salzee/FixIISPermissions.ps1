# Fix IIS Permissions for PMS Deployment
# Run this script as Administrator

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IIS Permissions Fix Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$DeployPath = "D:\PMSDeploy"
$AppPoolName = "PMS"  # Change this if your app pool has a different name

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 1: Checking deployment folder..." -ForegroundColor Yellow
if (-not (Test-Path $DeployPath)) {
    Write-Host "ERROR: Deployment folder not found: $DeployPath" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Deployment folder exists" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Getting Application Pool identity..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    $appPool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction Stop
    $appPoolIdentity = $appPool.processModel.identityType
    
    if ($appPoolIdentity -eq "ApplicationPoolIdentity") {
        $identity = "IIS AppPool\$AppPoolName"
    } elseif ($appPoolIdentity -eq "NetworkService") {
        $identity = "NETWORK SERVICE"
    } elseif ($appPoolIdentity -eq "LocalService") {
        $identity = "LOCAL SERVICE"
    } else {
        $identity = $appPool.processModel.userName
    }
    Write-Host "[OK] Application Pool Identity: $identity" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not find App Pool '$AppPoolName'. Using default identities." -ForegroundColor Yellow
    $identity = "IIS AppPool\$AppPoolName"
}

Write-Host ""
Write-Host "Step 3: Setting folder permissions..." -ForegroundColor Yellow

# Grant permissions to IIS_IUSRS
try {
    $acl = Get-Acl $DeployPath
    $iisUsers = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-568")  # IIS_IUSRS
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($iisUsers, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $DeployPath -AclObject $acl
    Write-Host "[OK] Granted permissions to IIS_IUSRS" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not set IIS_IUSRS permissions: $_" -ForegroundColor Yellow
}

# Grant permissions to Application Pool Identity
try {
    $acl = Get-Acl $DeployPath
    $appPoolSid = New-Object System.Security.Principal.NTAccount($identity)
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($appPoolSid, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $DeployPath -AclObject $acl
    Write-Host "[OK] Granted FullControl to $identity" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Could not set App Pool permissions: $_" -ForegroundColor Yellow
    Write-Host "Trying alternative method..." -ForegroundColor Yellow
    try {
        icacls $DeployPath /grant "${identity}:F" /T
        Write-Host "[OK] Granted permissions using icacls" -ForegroundColor Green
    } catch {
        Write-Host "[ERROR] Failed to set permissions: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Step 4: Verifying web.config exists..." -ForegroundColor Yellow
if (Test-Path "$DeployPath\web.config") {
    Write-Host "[OK] web.config found" -ForegroundColor Green
} else {
    Write-Host "[WARNING] web.config not found in deployment folder!" -ForegroundColor Yellow
    Write-Host "Copying web.config from project..." -ForegroundColor Yellow
    if (Test-Path ".\web.config") {
        Copy-Item ".\web.config" -Destination "$DeployPath\web.config" -Force
        Write-Host "[OK] web.config copied" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] web.config not found in project directory!" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Step 5: Checking IIS Site configuration..." -ForegroundColor Yellow
try {
    $sites = Get-Website | Where-Object { $_.physicalPath -like "*PMSDeploy*" -or $_.Name -eq "PMS" }
    if ($sites) {
        foreach ($site in $sites) {
            Write-Host "[INFO] Found IIS Site: $($site.Name)" -ForegroundColor Cyan
            Write-Host "       Physical Path: $($site.physicalPath)" -ForegroundColor Cyan
            Write-Host "       Application Pool: $($site.applicationPool)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "[WARNING] No IIS site found pointing to PMSDeploy" -ForegroundColor Yellow
        Write-Host "You may need to create or configure the IIS site manually" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[WARNING] Could not check IIS sites: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Permissions fix completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Restart the IIS Application Pool: $AppPoolName" -ForegroundColor White
Write-Host "2. Restart the IIS Site if needed" -ForegroundColor White
Write-Host "3. Test the application at: http://172.20.228.2:84/" -ForegroundColor White
Write-Host ""
Write-Host "To restart the App Pool, run:" -ForegroundColor Yellow
Write-Host "  Restart-WebAppPool -Name '$AppPoolName'" -ForegroundColor Cyan
Write-Host ""
