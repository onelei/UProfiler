@echo off
set PORT=8080
echo Stopping server on port %PORT%...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":%PORT%" ^| findstr "LISTENING"') do (
  taskkill /PID %%a /F >nul 2>&1
  echo Stopped PID %%a
)
echo Done.
pause
