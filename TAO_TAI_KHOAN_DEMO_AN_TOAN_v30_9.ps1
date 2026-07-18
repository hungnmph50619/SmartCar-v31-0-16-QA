# Tạo hash PBKDF2-SHA256 và sinh SQL kích hoạt 4 tài khoản demo.
# Tương thích Windows PowerShell 5.1 và PowerShell 7.
# Mật khẩu chỉ tồn tại trong bộ nhớ của phiên PowerShell, không được ghi dạng rõ vào file.
$ErrorActionPreference = 'Stop'

function Read-PlainPassword([string]$Prompt) {
    $secure = Read-Host $Prompt -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

function Test-PasswordPolicy([string]$Password) {
    if ([string]::IsNullOrWhiteSpace($Password)) { throw 'Vui lòng nhập mật khẩu.' }
    if ($Password.Length -lt 8) { throw 'Mật khẩu phải có ít nhất 8 ký tự.' }
    if ($Password.Length -gt 100) { throw 'Mật khẩu không được vượt quá 100 ký tự.' }
    if ($Password -notmatch '[A-Za-zÀ-ỹ]') { throw 'Mật khẩu phải có ít nhất một chữ cái.' }
    if ($Password -notmatch '\d') { throw 'Mật khẩu phải có ít nhất một chữ số.' }
}

function New-RandomBytes([int]$Length) {
    $bytes = New-Object byte[] $Length
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return $bytes
}

function New-Pbkdf2Hash([string]$Password) {
    Test-PasswordPolicy $Password
    $salt = New-RandomBytes 16
    $pbkdf2 = New-Object Security.Cryptography.Rfc2898DeriveBytes(
        [Text.Encoding]::UTF8.GetBytes($Password),
        $salt,
        120000,
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    try { $key = $pbkdf2.GetBytes(32) } finally { $pbkdf2.Dispose() }
    return 'PBKDF2-SHA256$120000$' + [Convert]::ToBase64String($salt) + '$' + [Convert]::ToBase64String($key)
}

$accounts = @(
    @{ Id = 1; Username = 'quantri'; Label = 'Admin quantri' },
    @{ Id = 2; Username = 'nhanvien'; Label = 'Staff nhanvien' },
    @{ Id = 3; Username = 'doitac'; Label = 'Đối tác doitac' },
    @{ Id = 4; Username = 'khachhang'; Label = 'Khách khachhang' }
)

$updates = @()
foreach ($account in $accounts) {
    $password = Read-PlainPassword "Nhập mật khẩu mới cho $($account.Label)"
    $confirm = Read-PlainPassword "Nhập lại mật khẩu cho $($account.Label)"
    if ($password -cne $confirm) { throw "Mật khẩu nhập lại không khớp cho $($account.Username)." }
    $hash = New-Pbkdf2Hash $password
    $updates += "UPDATE dbo.AppUsers SET [Password]=N'$hash', IsActive=1, EmailConfirmed=1, FailedLoginCount=0, LockoutEnd=NULL, LockReason=NULL, TokenVersion=TokenVersion+1 WHERE AppUserId=$($account.Id) AND Username=N'$($account.Username)';"
    $password = $null
    $confirm = $null
}

$sql = @"
USE [SmartCarMarketplaceDb];
SET XACT_ABORT ON;
BEGIN TRANSACTION;
$($updates -join "`r`n")
COMMIT TRANSACTION;
PRINT N'Đã đặt mật khẩu băm và kích hoạt tài khoản demo.';
"@
$outFile = Join-Path $PSScriptRoot 'Database\KICH_HOAT_TAI_KHOAN_DEMO_GENERATED.sql'
[IO.File]::WriteAllText($outFile, $sql, (New-Object Text.UTF8Encoding($true)))
Write-Host "Đã tạo: $outFile" -ForegroundColor Green
Write-Host 'Mở file này trong SQL Server Management Studio và chạy. File chỉ chứa hash, không chứa mật khẩu rõ.'
