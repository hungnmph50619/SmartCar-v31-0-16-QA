param(
    [string]$ApiBaseUrl = 'https://localhost:7060',
    [string]$UiBaseUrl = 'https://localhost:7154',
    [string]$AdminJwt = '',
    [switch]$SkipCertificateCheck
)

$ErrorActionPreference = 'Stop'
$invokeArgs = @{}
if ($SkipCertificateCheck -and (Get-Command Invoke-WebRequest).Parameters.ContainsKey('SkipCertificateCheck')) {
    $invokeArgs.SkipCertificateCheck = $true
}

function Assert-Status {
    param([string]$Name, [string]$Url, [int[]]$Expected, [hashtable]$Headers = @{})
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -Headers $Headers -MaximumRedirection 0 @invokeArgs
        $status = [int]$response.StatusCode
    } catch {
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode.value__ }
        else { throw }
    }
    if ($Expected -notcontains $status) {
        throw "$Name thất bại: HTTP $status, mong đợi $($Expected -join ', '). URL: $Url"
    }
    Write-Host "[OK] $Name - HTTP $status" -ForegroundColor Green
}

Write-Host '=== SMARTCAR v30.9 E2E SMOKE ===' -ForegroundColor Cyan
Assert-Status 'API live' "$ApiBaseUrl/health/live" @(200)
Assert-Status 'Readiness chặn anonymous' "$ApiBaseUrl/health/ready" @(401,403)
if (-not [string]::IsNullOrWhiteSpace($AdminJwt)) {
    Assert-Status 'API ready và đúng DB version/schema' "$ApiBaseUrl/health/ready" @(200) @{ Authorization = "Bearer $AdminJwt" }
} else {
    Write-Host '[SKIP] Chưa truyền -AdminJwt, bỏ qua readiness có xác thực.' -ForegroundColor DarkYellow
}
Assert-Status 'Danh sách địa điểm công khai' "$ApiBaseUrl/api/Locations" @(200)
Assert-Status 'Statistics chặn anonymous' "$ApiBaseUrl/api/Statistics/GetCarCount" @(401)
Assert-Status 'File riêng tư bắt buộc đăng nhập' "$ApiBaseUrl/api/secure-files/00000000-0000-0000-0000-000000000001" @(401)
Assert-Status 'WebUI phản hồi' "$UiBaseUrl/" @(200,301,302)
Write-Host 'SMOKE TEST THÀNH CÔNG.' -ForegroundColor Green
