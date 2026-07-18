param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipPackageAudit
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Không tìm thấy dotnet. Hãy cài .NET 8 SDK hoặc Visual Studio 2022 với ASP.NET workload.'
}

Write-Host '=== SMARTCAR v31.0.16 - KIỂM THỬ TỰ ĐỘNG ===' -ForegroundColor Cyan
dotnet --version

Write-Host '[1/4] Restore NuGet...' -ForegroundColor Yellow
dotnet restore .\CarBook.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[2/4] Build $Configuration..." -ForegroundColor Yellow
dotnet build .\CarBook.sln -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$results = Join-Path $PSScriptRoot 'TestResults'
if (Test-Path $results) { Remove-Item $results -Recurse -Force }

Write-Host '[3/4] Chạy Unit + Integration Tests và thu coverage...' -ForegroundColor Yellow
dotnet test .\CarBook.sln -c $Configuration --no-build `
    --logger 'trx;LogFileName=smartcar-tests.trx' `
    --collect 'XPlat Code Coverage' `
    --results-directory $results
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipPackageAudit) {
    Write-Host '[4/4] Kiểm tra package có lỗ hổng đã công bố...' -ForegroundColor Yellow
    dotnet list .\CarBook.sln package --vulnerable --include-transitive
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host '[4/4] Bỏ qua package audit theo tham số.' -ForegroundColor DarkYellow
}

Write-Host "THÀNH CÔNG. Kết quả nằm tại: $results" -ForegroundColor Green
