# PMS IIS Deployment Script
# This script builds, publishes, and configures the PMS application on IIS

$ErrorActionPreference = "Stop"

# Configuration
$ProjectPath = $PSScriptRoot
$DeployPath = "D:\PMSDeploy"
$AppPoolName = "PMSAppPool"
$SiteName = "PMS"
$BindingIP = "172.20.228.2"
$BindingPort = "84"

Write-Host "=== PMS IIS Deployment ===" -ForegroundColor Cyan

# Import IIS Module
Import-Module WebAdministration -ErrorAction Stop

# Step 1: Stop IIS Application Pool if it exists
Write-Host "`n[1/7] Stopping IIS Application Pool..." -ForegroundColor Yellow
try {
    $appPool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue
    if ($appPool) {
        $state = (Get-WebAppPoolState -Name $AppPoolName).Value
        if ($state -eq "Started") {
            Stop-WebAppPool -Name $AppPoolName
            Write-Host "   Application pool stopped." -ForegroundColor Green
            Start-Sleep -Seconds 2
        } else {
            Write-Host "   Application pool already stopped." -ForegroundColor Gray
        }
    } else {
        Write-Host "   Application pool doesn't exist yet." -ForegroundColor Gray
    }
} catch {
    Write-Host "   Could not stop application pool (may not exist): $_" -ForegroundColor Yellow
}

# Step 2: Stop IIS Site if it exists
Write-Host "`n[2/7] Stopping IIS Site..." -ForegroundColor Yellow
try {
    $site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($site) {
        $siteState = (Get-WebsiteState -Name $SiteName).Value
        if ($siteState -eq "Started") {
            Stop-Website -Name $SiteName
            Write-Host "   Website stopped." -ForegroundColor Green
            Start-Sleep -Seconds 2
        } else {
            Write-Host "   Website already stopped." -ForegroundColor Gray
        }
    } else {
        Write-Host "   Website doesn't exist yet." -ForegroundColor Gray
    }
} catch {
    Write-Host "   Could not stop website (may not exist): $_" -ForegroundColor Yellow
}

# Step 3: Build project in Release mode
Write-Host "`n[3/7] Building project in Release mode..." -ForegroundColor Yellow
Set-Location $ProjectPath
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw "Build failed!"
}
Write-Host "   Build completed successfully." -ForegroundColor Green

# Step 4: Publish project
Write-Host "`n[4/7] Publishing project to $DeployPath..." -ForegroundColor Yellow
if (Test-Path $DeployPath) {
    Write-Host "   Removing existing deployment folder..." -ForegroundColor Gray
    Remove-Item -Path $DeployPath -Recurse -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}
New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null

dotnet publish --configuration Release --output $DeployPath --self-contained false
if ($LASTEXITCODE -ne 0) {
    throw "Publish failed!"
}
Write-Host "   Publish completed successfully." -ForegroundColor Green

# Step 5: Create IIS Application Pool
Write-Host "`n[5/7] Configuring IIS Application Pool..." -ForegroundColor Yellow
try {
    $existingPool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue
    if ($existingPool) {
        Write-Host "   Application pool already exists. Removing..." -ForegroundColor Gray
        Remove-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
    
    New-WebAppPool -Name $AppPoolName -Force
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value "ApplicationPoolIdentity"
    Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
    Write-Host "   Application pool created successfully." -ForegroundColor Green
} catch {
    throw "Failed to create application pool: $_"
}

# Step 6: Create IIS Website
Write-Host "`n[6/7] Configuring IIS Website..." -ForegroundColor Yellow
try {
    $existingSite = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if ($existingSite) {
        Write-Host "   Website already exists. Removing..." -ForegroundColor Gray
        Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
    }
    
    New-Website -Name $SiteName `
                -PhysicalPath $DeployPath `
                -ApplicationPool $AppPoolName `
                -IPAddress $BindingIP `
                -Port $BindingPort `
                -Force
    
    # Ensure web.config exists (ASP.NET Core hosting)
    $webConfigPath = Join-Path $DeployPath "web.config"
    if (-not (Test-Path $webConfigPath)) {
        $webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\PMS.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
"@
        Set-Content -Path $webConfigPath -Value $webConfigContent -Encoding UTF8
        Write-Host "   Created web.config file." -ForegroundColor Gray
    }
    
    Write-Host "   Website created successfully." -ForegroundColor Green
} catch {
    throw "Failed to create website: $_"
}

# Step 7: Start Application Pool and Website
Write-Host "`n[7/7] Starting Application Pool and Website..." -ForegroundColor Yellow
Start-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2
Start-Website -Name $SiteName
Write-Host "   Application started successfully." -ForegroundColor Green

Write-Host "`n=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "Application URL: http://$BindingIP`:$BindingPort" -ForegroundColor Green
Write-Host "Deployment Path: $DeployPath" -ForegroundColor Green
Write-Host "Application Pool: $AppPoolName" -ForegroundColor Green
Write-Host "Website Name: $SiteName" -ForegroundColor Green
