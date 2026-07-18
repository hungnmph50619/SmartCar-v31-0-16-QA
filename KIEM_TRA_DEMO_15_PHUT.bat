@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0KIEM_TRA_DEMO_15_PHUT.ps1"
if errorlevel 1 (
  echo.
  echo [THAT BAI] Xem dong FAIL phia tren.
  pause
  exit /b 1
)
pause
