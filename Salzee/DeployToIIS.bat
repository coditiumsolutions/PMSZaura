@echo off
REM Deploy PMS Application to IIS
REM Run this script from the project root directory

echo ========================================
echo PMS Application Deployment Script
echo ========================================
echo.

set PROJECT_PATH=%~dp0
set DEPLOY_PATH=C:\PMSDeploy

echo Project Path: %PROJECT_PATH%
echo Deploy Path: %DEPLOY_PATH%
echo.

REM Step 1: Clean previous build
echo Step 1: Cleaning previous build...
dotnet clean "%PROJECT_PATH%Salzee.csproj" --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo Clean failed!
    exit /b 1
)

REM Step 2: Restore packages
echo Step 2: Restoring NuGet packages...
dotnet restore "%PROJECT_PATH%Salzee.csproj"
if %ERRORLEVEL% NEQ 0 (
    echo Restore failed!
    exit /b 1
)

REM Step 3: Build in Release mode
echo Step 3: Building project (Release mode)...
dotnet build "%PROJECT_PATH%Salzee.csproj" --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

REM Step 3.5: Stop running PMS app to avoid file locks
echo Step 3.5: Stopping PMS app (IIS/app)...
powershell -Command "Import-Module WebAdministration; Stop-WebAppPool -Name 'PMS' -ErrorAction SilentlyContinue | Out-Null; Stop-Website -Name 'PMS' -ErrorAction SilentlyContinue | Out-Null"
taskkill /F /IM Salzee.exe >nul 2>&1

REM Step 4: Publish to deployment folder
echo Step 4: Publishing to %DEPLOY_PATH%...
if exist "%DEPLOY_PATH%" (
    echo Cleaning deployment folder...
    powershell -Command "if (Test-Path '%DEPLOY_PATH%') { Remove-Item -Path '%DEPLOY_PATH%' -Recurse -Force -ErrorAction SilentlyContinue }"
    timeout /t 1 /nobreak >nul
)
if not exist "%DEPLOY_PATH%" (
    mkdir "%DEPLOY_PATH%"
)

dotnet publish "%PROJECT_PATH%Salzee.csproj" --configuration Release --output "%DEPLOY_PATH%" --self-contained false --runtime win-x64

if %ERRORLEVEL% NEQ 0 (
    echo Publish failed!
    exit /b 1
)

REM Step 5: Update web.config DLL name if needed
echo Step 5: Verifying web.config...
if exist "%DEPLOY_PATH%\web.config" (
    powershell -Command "(Get-Content '%DEPLOY_PATH%\web.config') -replace 'PMS\.Web\.dll', 'Salzee.dll' | Set-Content '%DEPLOY_PATH%\web.config'"
    echo [OK] web.config verified
) else (
    echo [WARNING] web.config not found - will be generated
)

REM Step 6: Verify deployment
echo Step 6: Verifying deployment...
if exist "%DEPLOY_PATH%\Salzee.dll" (
    echo [OK] Salzee.dll found
) else (
    echo [ERROR] Salzee.dll not found!
    exit /b 1
)

REM Step 7: Start IIS site/app pool again
echo Step 7: Starting IIS site/app pool...
powershell -Command "Import-Module WebAdministration; Start-WebAppPool -Name 'PMS' -ErrorAction SilentlyContinue | Out-Null; Start-Website -Name 'PMS' -ErrorAction SilentlyContinue | Out-Null"

echo.
echo ========================================
echo Deployment completed successfully!
echo ========================================
echo.
echo Deployment Location: %DEPLOY_PATH%
echo IIS Site URL: http://178.105.20.255/
echo.
echo Next Steps:
echo 1. Ensure IIS site 'PMS' is pointing to: %DEPLOY_PATH%
echo 2. Restart IIS site if needed
echo 3. Test the application at: http://178.105.20.255/
echo.
echo.
echo Deploy script finished.
