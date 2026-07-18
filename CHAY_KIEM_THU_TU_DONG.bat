@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0CHAY_KIEM_THU_TU_DONG.ps1" %*
exit /b %errorlevel%
