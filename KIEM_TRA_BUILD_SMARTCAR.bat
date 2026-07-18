@echo off
setlocal
cd /d "%~dp0"

echo =====================================================
echo SMARTCAR v31.0.16 - RESTORE, BUILD VA TEST
echo =====================================================
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [LOI] Khong tim thay dotnet. Hay cai .NET 8 SDK hoac Visual Studio 2022 ASP.NET workload.
    pause
    exit /b 1
)

echo [1/4] Phien ban .NET:
dotnet --version
if errorlevel 1 goto :fail

echo [2/4] Khoi phuc goi NuGet...
dotnet restore CarBook.sln
if errorlevel 1 goto :fail

echo [3/4] Build solution Release...
dotnet build CarBook.sln -c Release --no-restore
if errorlevel 1 goto :fail

echo [4/4] Chay Unit va Integration Tests...
dotnet test CarBook.sln -c Release --no-build --logger "trx;LogFileName=smartcar-tests.trx" --results-directory TestResults
if errorlevel 1 goto :fail

echo.
echo [THANH CONG] Solution da build va test thanh cong.
echo Kiem tra tiep API: https://localhost:7060/health/live va /health/ready
pause
exit /b 0

:fail
echo.
echo [THAT BAI] Build hoac test chua thanh cong. Xem dong loi phia tren.
pause
exit /b 1
