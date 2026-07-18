# E2E smoke test SmartCar v30.9

Khởi động WebApi HTTPS cổng 7060 và WebUI HTTPS cổng 7154, sau đó chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tests\E2E\SMARTCAR_E2E_SMOKE.ps1 -SkipCertificateCheck
```

`/health/ready` chỉ dành cho Admin/Staff. Để kiểm tra readiness đầy đủ, đăng nhập Admin/Staff, lấy JWT rồi chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tests\E2E\SMARTCAR_E2E_SMOKE.ps1 `
  -SkipCertificateCheck `
  -AdminJwt '<JWT_ADMIN_HOAC_STAFF>'
```

Bài smoke test xác nhận API live, endpoint công khai, phân quyền Statistics/tệp riêng tư/readiness và WebUI phản hồi. Đây là kiểm tra nhanh sau triển khai, không thay thế ma trận trong `HUONG_DAN_KIEM_THU_V30_9.md`.
