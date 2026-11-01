# Deploy PMS Application to IIS
# Run this script from the project root directory

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PMS Application Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = $PSScriptRoot
$deployPath = "D:\PMSDeploy"

Write-Host "Project Path: $projectPath" -ForegroundColor Yellow
Write-Host "Deploy Path: $deployPath" -ForegroundColor Yellow
Write-Host ""

# Step 1: Clean previous build
Write-Host "Step 1: Cleaning previous build..." -ForegroundColor Green
dotnet clean "$projectPath\Salzee.csproj" --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Clean failed!" -ForegroundColor Red
    exit 1
}

# Step 2: Restore packages
Write-Host "Step 2: Restoring NuGet packages..." -ForegroundColor Green
dotnet restore "$projectPath\Salzee.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

# Step 3: Build in Release mode
Write-Host "Step 3: Building project (Release mode)..." -ForegroundColor Green
dotnet build "$projectPath\Salzee.csproj" --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Step 4: Publish to deployment folder
Write-Host "Step 4: Publishing to $deployPath..." -ForegroundColor Green
if (Test-Path $deployPath) {
    Write-Host "Cleaning deployment folder..." -ForegroundColor Yellow
    Remove-Item "$deployPath\*" -Recurse -Force -ErrorAction SilentlyContinue
}

dotnet publish "$projectPath\Salzee.csproj" `
    --configuration Release `
    --output "$deployPath" `
    --no-build `
    --self-contained false `
    --runtime win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

# Step 5: Ensure web.config has correct DLL name
Write-Host "Step 5: Verifying and updating web.config..." -ForegroundColor Green
if (Test-Path "$deployPath\web.config") {
    $webConfig = Get-Content "$deployPath\web.config" -Raw
    if ($webConfig -match 'PMS\.Web\.dll') {
        Write-Host "Updating web.config DLL name..." -ForegroundColor Yellow
        $webConfig = $webConfig -replace 'PMS\.Web\.dll', 'Salzee.dll'
        Set-Content "$deployPath\web.config" -Value $webConfig -NoNewline
    }
    Write-Host "✓ web.config verified" -ForegroundColor Green
} else {
    Write-Host "⚠ web.config not found - will be generated during publish" -ForegroundColor Yellow
}

# Step 6: Verify deployment
Write-Host "Step 6: Verifying deployment..." -ForegroundColor Green
if (Test-Path "$deployPath\Salzee.dll") {
    Write-Host "✓ Salzee.dll found" -ForegroundColor Green
} else {
    Write-Host "✗ Salzee.dll not found!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Deployment Location: $deployPath" -ForegroundColor Yellow
Write-Host "IIS Site URL: http://172.20.228.2:84/" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Ensure IIS site 'PMS' is pointing to: $deployPath" -ForegroundColor White
Write-Host "2. Restart IIS site if needed" -ForegroundColor White
Write-Host "3. Test the application at: http://172.20.228.2:84/" -ForegroundColor White
Write-Host ""
