@echo off
setlocal
cd /d "%~dp0"
set PORT=8080
if not "%~1"=="" set PORT=%~1

for /f "usebackq delims=" %%v in ("%~dp0..\VERSION") do set APP_VERSION=%%v
if not defined APP_VERSION set APP_VERSION=unknown

echo ========================================
echo   UProfiler Server v%APP_VERSION%
echo ========================================
echo.

echo [1/4] Checking port %PORT%...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":%PORT%" ^| findstr "LISTENING"') do (
  echo   Port %PORT% is used by PID %%a, stopping it...
  taskkill /PID %%a /F >nul 2>&1
)

echo [2/4] Checking auth configuration...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-auth.ps1" -Port %PORT%
if errorlevel 2 (
  echo.
  echo   Auth check failed. Fix auth.json above, or set "enabled": false to skip login.
  pause
  exit /b 1
)

echo [3/4] Building UProfiler report server...
dotnet build UProfiler-Server.csproj -c Release
if errorlevel 1 (
  echo   Build failed.
  pause
  exit /b 1
)

if exist "auth.json" (
  if not exist "bin\Release\net8.0" mkdir "bin\Release\net8.0"
  copy /Y "auth.json" "bin\Release\net8.0\auth.json" >nul
  echo   auth.json synced to output directory.
)

echo [4/4] Starting UProfiler-Server on port %PORT% ...
echo   Local:   http://localhost:%PORT%/
echo   Login:   http://localhost:%PORT%/login
echo   Account: http://localhost:%PORT%/account/profile
echo.
echo Keep this window open. Close it to stop the server.
echo.
dotnet run --project UProfiler-Server.csproj -c Release --no-build -- --port %PORT%
pause
