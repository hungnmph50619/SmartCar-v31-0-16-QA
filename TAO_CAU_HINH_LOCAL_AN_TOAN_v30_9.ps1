# Tạo appsettings.Local.json bằng khóa ngẫu nhiên; file này đã được .gitignore loại trừ.
# Tương thích Windows PowerShell 5.1 và PowerShell 7.
$ErrorActionPreference = 'Stop'

function New-RandomBase64([int]$Length) {
    $bytes = New-Object byte[] $Length
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}

$apiPath = Join-Path $PSScriptRoot 'Presentation\SmartCar.WebApi\appsettings.Local.json'
$webPath = Join-Path $PSScriptRoot 'Frontends\SmartCar.WebUI\appsettings.Local.json'

if ((Test-Path $apiPath) -or (Test-Path $webPath)) {
    $answer = Read-Host 'Đã tồn tại cấu hình local. Ghi đè? Nhập YES để tiếp tục'
    if ($answer -cne 'YES') { throw 'Đã hủy để tránh ghi đè secret hiện có.' }
}

$emailUser = Read-Host 'Gmail gửi OTP (có thể để trống và sửa sau)'
$emailPassword = Read-Host 'Gmail App Password (có thể để trống và sửa sau)'
$bankName = Read-Host 'Tên ngân hàng nhận tiền mô phỏng (có thể để trống)'
$accountNumber = Read-Host 'Số tài khoản nhận tiền mô phỏng (có thể để trống)'
$accountHolder = Read-Host 'Tên chủ tài khoản nhận tiền mô phỏng (có thể để trống)'
$qrImagePath = Read-Host 'Đường dẫn QR trong wwwroot, ví dụ /images/payment/qr.png (có thể để trống)'

$api = [ordered]@{
    ConnectionStrings = [ordered]@{
        DefaultConnection = 'Server=.;Database=SmartCarMarketplaceDb;Integrated Security=true;TrustServerCertificate=true;'
    }
    EmailSettings = [ordered]@{
        Host = 'smtp.gmail.com'
        Port = 587
        UserName = $emailUser
        AppPassword = $emailPassword
        FromEmail = $emailUser
        FromName = 'SmartCar OTP'
        WebBaseUrl = 'https://localhost:7154'
    }
    Jwt = [ordered]@{
        Issuer = 'https://localhost'
        Audience = 'https://localhost'
        Key = New-RandomBase64 64
        ExpireDays = 5
    }
    Security = [ordered]@{
        OtpHmacKey = New-RandomBase64 64
        IdentityHmacKey = New-RandomBase64 64
    }
}

$paymentEnabled = -not ([string]::IsNullOrWhiteSpace($bankName) -or [string]::IsNullOrWhiteSpace($accountNumber) -or [string]::IsNullOrWhiteSpace($accountHolder))
$web = [ordered]@{
    ApiSettings = [ordered]@{ BaseUrl = 'https://localhost:7060/' }
    ManualPayment = [ordered]@{
        Enabled = $paymentEnabled
        BankName = $bankName
        AccountNumber = $accountNumber
        AccountHolder = $accountHolder
        QrImagePath = $qrImagePath
        IsDemoQr = $true
        TransferPrefix = 'SC'
    }
}

$apiJson = $api | ConvertTo-Json -Depth 8
$webJson = $web | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText($apiPath, $apiJson, (New-Object Text.UTF8Encoding($true)))
[IO.File]::WriteAllText($webPath, $webJson, (New-Object Text.UTF8Encoding($true)))
Write-Host "Đã tạo: $apiPath" -ForegroundColor Green
Write-Host "Đã tạo: $webPath" -ForegroundColor Green
Write-Host 'Không commit hoặc gửi hai file appsettings.Local.json cho người khác.' -ForegroundColor Yellow
