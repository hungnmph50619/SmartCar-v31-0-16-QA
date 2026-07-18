param(
    [string]$ApiBaseUrl = 'https://localhost:7060',
    [string]$UiBaseUrl = 'https://localhost:7154',
    [string]$Password = 'a12345678'
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Invoke-Http {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][string]$Url,
        [string]$Method = 'GET',
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int[]]$Expected = @(200)
    )
    try {
        $args = @{ Uri=$Url; Method=$Method; Headers=$Headers; UseBasicParsing=$true; MaximumRedirection=0 }
        if ($null -ne $Body) {
            $args.ContentType = 'application/json'
            $args.Body = ($Body | ConvertTo-Json -Depth 8 -Compress)
        }
        $response = Invoke-WebRequest @args
        $status = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode.value__
            try {
                $reader = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())
                $content = $reader.ReadToEnd()
                $reader.Dispose()
            } catch { $content = $_.Exception.Message }
        } else { throw }
    }
    if ($Expected -notcontains $status) {
        throw "[FAIL] $Name - HTTP $status (mong đợi $($Expected -join ', ')). $content"
    }
    Write-Host "[OK] $Name - HTTP $status" -ForegroundColor Green
    return @{ Status=$status; Content=$content }
}

function Login([string]$Username) {
    $result = Invoke-Http -Name "Đăng nhập $Username" -Url "$ApiBaseUrl/api/Login" -Method POST `
        -Body @{ Username=$Username; Password=$Password } -Expected @(201)
    $token = $result.Content | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace([string]$token)) { throw "Không nhận được JWT của $Username." }
    return [string]$token
}

function Auth([string]$Token) { return @{ Authorization = "Bearer $Token" } }

Write-Host '=====================================================' -ForegroundColor Cyan
Write-Host ' SMARTCAR v31.0.16 - KIỂM TRA TRƯỚC DEMO 15 PHÚT' -ForegroundColor Cyan
Write-Host '=====================================================' -ForegroundColor Cyan

Invoke-Http -Name 'WebUI trang chủ' -Url "$UiBaseUrl/" -Expected @(200,301,302) | Out-Null
Invoke-Http -Name 'API live' -Url "$ApiBaseUrl/health/live" | Out-Null
Invoke-Http -Name 'Địa điểm công khai' -Url "$ApiBaseUrl/api/Locations" | Out-Null

$customer = Login 'khachhang'
$staff    = Login 'nhanvien'
$partner  = Login 'doitac'
$admin    = Login 'quantri'

Invoke-Http -Name 'API ready + đúng schema DB' -Url "$ApiBaseUrl/health/ready" -Headers (Auth $admin) | Out-Null

$customerResult = Invoke-Http -Name 'Customer readiness' -Url "$ApiBaseUrl/api/operations/customer/readiness" -Headers (Auth $customer)
$customerData = $customerResult.Content | ConvertFrom-Json
Write-Host ("     Customer khachhang: {0}; có thể đặt tự lái: {1}" -f $customerData.verificationStatus, $customerData.canBook) -ForegroundColor DarkCyan

$queueResult = Invoke-Http -Name 'Staff work queue' -Url "$ApiBaseUrl/api/operations/staff/work-queue" -Headers (Auth $staff)
$queueData = $queueResult.Content | ConvertFrom-Json
$pendingVerificationCount = @($queueData.items | Where-Object { $_.queueType -eq 'Xác minh khách' -and $_.bucket -in @('Cần xử lý','Đang xử lý') }).Count
if ($pendingVerificationCount -gt 0) {
    Write-Host "     Có $pendingVerificationCount hồ sơ khách để demo Staff." -ForegroundColor DarkCyan
} else {
    Write-Host '     [CẢNH BÁO] Chưa có hồ sơ khách chờ duyệt. Dùng tài khoản khachhang gửi hồ sơ hoặc chạy RESET_HO_SO_KHACHHANG01_DE_DEMO.sql (file tự chọn khachhang01 nếu có, nếu không sẽ dùng khachhang).' -ForegroundColor Yellow
}

$partnerResult = Invoke-Http -Name 'Partner dashboard' -Url "$ApiBaseUrl/api/PartnerVehicles/me/dashboard" -Headers (Auth $partner)
$partnerData = $partnerResult.Content | ConvertFrom-Json
Write-Host ("     Partner doitac: {0} xe, {1} hồ sơ đăng xe." -f @($partnerData.vehicles).Count, @($partnerData.applications).Count) -ForegroundColor DarkCyan

Invoke-Http -Name 'Admin dashboard' -Url "$ApiBaseUrl/api/AdminDashboardOverview" -Headers (Auth $admin) | Out-Null
Invoke-Http -Name 'Admin accounts' -Url "$ApiBaseUrl/api/admin-accounts?type=$([uri]::EscapeDataString('Khách hàng'))" -Headers (Auth $admin) | Out-Null
Invoke-Http -Name 'Admin audit log' -Url "$ApiBaseUrl/api/marketplace-operations/audit-logs?take=20" -Headers (Auth $admin) | Out-Null

Invoke-Http -Name 'Customer bị chặn khỏi Admin API' -Url "$ApiBaseUrl/api/AdminDashboardOverview" -Headers (Auth $customer) -Expected @(403) | Out-Null
Invoke-Http -Name 'Partner bị chặn khỏi Staff queue' -Url "$ApiBaseUrl/api/operations/staff/work-queue" -Headers (Auth $partner) -Expected @(403) | Out-Null

Write-Host ''
Write-Host 'TẤT CẢ KIỂM TRA DEMO ĐÃ ĐẠT.' -ForegroundColor Green
Write-Host 'Có thể mở 4 cửa sổ và demo theo thứ tự Customer -> Staff -> Partner -> Admin.' -ForegroundColor Green
