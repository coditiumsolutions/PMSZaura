@echo off
REM Deploy PMS Application to IIS (uses temp obj/bin to bypass locked obj\Release\win-x64)

echo ========================================
echo PMS Application Deployment Script
echo ========================================
echo.

set PROJECT_PATH=%~dp0
set DEPLOY_PATH=D:\PMSDeploy
set TEMP_OBJ_PATH=%PROJECT_PATH%obj\_deploy_tmp
set TEMP_BIN_PATH=%PROJECT_PATH%bin\_deploy_tmp

echo Project Path: %PROJECT_PATH%
echo Deploy Path: %DEPLOY_PATH%
echo Temp Obj Path: %TEMP_OBJ_PATH%
echo Temp Bin Path: %TEMP_BIN_PATH%
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
if exist "%TEMP_OBJ_PATH%" (
    rmdir /s /q "%TEMP_OBJ_PATH%"
)
if exist "%TEMP_BIN_PATH%" (
    rmdir /s /q "%TEMP_BIN_PATH%"
)
dotnet restore "%PROJECT_PATH%Salzee.csproj" --runtime win-x64 -p:BaseIntermediateOutputPath="%TEMP_OBJ_PATH%\\" -p:BaseOutputPath="%TEMP_BIN_PATH%\\"
if %ERRORLEVEL% NEQ 0 (
    echo Restore failed!
    exit /b 1
)

REM Step 3: Pre-check compile in Release mode
echo Step 3: Validating project build (Release mode)...
dotnet build "%PROJECT_PATH%Salzee.csproj" --configuration Release --no-restore --runtime win-x64 -p:BaseIntermediateOutputPath="%TEMP_OBJ_PATH%\\" -p:BaseOutputPath="%TEMP_BIN_PATH%\\" -p:GenerateAssemblyInfo=false -p:GenerateTargetFrameworkAttribute=false -p:GenerateRazorAssemblyInfo=false
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

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

dotnet publish "%PROJECT_PATH%Salzee.csproj" --configuration Release --output "%DEPLOY_PATH%" --self-contained false --runtime win-x64 --no-build -p:BaseIntermediateOutputPath="%TEMP_OBJ_PATH%\\" -p:BaseOutputPath="%TEMP_BIN_PATH%\\" -p:GenerateAssemblyInfo=false -p:GenerateTargetFrameworkAttribute=false -p:GenerateRazorAssemblyInfo=false

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

echo.
echo ========================================
echo Deployment completed successfully!
echo ========================================
echo.
echo Deployment Location: %DEPLOY_PATH%
echo IIS Site URL: http://172.20.228.2:84/
echo.
echo Next Steps:
echo 1. Ensure IIS site 'PMS' is pointing to: %DEPLOY_PATH%
echo 2. Restart IIS site if needed
echo 3. Test the application at: http://172.20.228.2:84/
echo.

pause
