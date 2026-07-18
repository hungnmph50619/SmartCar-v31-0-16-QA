@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo [LOI] Khong tim thay dotnet trong PATH. Mo solution bang Visual Studio 2022 va cai workload ASP.NET and web development.
  pause
  exit /b 1
)

echo Dang khoi dong Web API tai https://localhost:7060 ...
start "SmartCar WebApi" cmd /k "cd /d ""%~dp0"" && dotnet run --project Presentation\SmartCar.WebApi\SmartCar.WebApi.csproj --launch-profile https"

timeout /t 5 /nobreak >nul

echo Dang khoi dong WebUI tai https://localhost:7154 ...
start "SmartCar WebUI" cmd /k "cd /d ""%~dp0"" && dotnet run --project Frontends\SmartCar.WebUI\SmartCar.WebUI.csproj --launch-profile https"

echo.
echo Cho 10-20 giay, sau do mo https://localhost:7154
echo Chay KIEM_TRA_DEMO_15_PHUT.bat de kiem tra 4 vai tro.
pause
