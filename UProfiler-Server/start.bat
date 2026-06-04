@echo off
cd /d "%~dp0"
set PORT=8080

echo Checking port %PORT%...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":%PORT%" ^| findstr "LISTENING"') do (
  echo Port %PORT% is used by PID %%a, stopping it...
  taskkill /PID %%a /F >nul 2>&1
)

echo Building UProfiler report server...
dotnet build UProfiler-Server.csproj -c Release
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Starting UProfiler-Server on port %PORT% ...
echo Keep this window open. Close it to stop the server.
echo.
dotnet run --project UProfiler-Server.csproj -c Release --no-build -- --port %PORT%
pause
