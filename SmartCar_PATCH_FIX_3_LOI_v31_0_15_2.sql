/* SmartCar v31.0.15.2 - vá 3 lỗi, KHÔNG xóa database. */
USE [SmartCarMarketplaceDb];
GO
SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.AppUsers
       SET [Password] = CASE Username
            WHEN N'quantri' THEN N'PBKDF2-SHA256$120000$+iz/0agLHmtX2LtBzlnP4Q==$rV0X0k4Sb3Lk3RJPrNXLsFdVb2OzocxC7ncL/miET1I='
            WHEN N'nhanvien' THEN N'PBKDF2-SHA256$120000$0wuxcistq87Ecoe5xBPi0Q==$lCfm+qvkxaiDjS921flx2udPbhgsLb3Y7tyukl+SdvQ='
            WHEN N'doitac' THEN N'PBKDF2-SHA256$120000$OGsnRH3WKvllseB4D/jp2A==$cnKBsz86jT0OQx8P3HP/JM+dqA0G9PxHIF0qVCJS2kk='
            WHEN N'khachhang' THEN N'PBKDF2-SHA256$120000$jzUXLvFuR3jQC5Pza4Snww==$V22/M5qZOnsZfTM4iaXbdiyn+PxGJg2ufg2k3J3CUgE='
            ELSE [Password] END,
           EmailConfirmed=1, IsActive=1, IsDeleted=0,
           FailedLoginCount=0, LockoutEnd=NULL,
           LockType=NULL, LockReason=NULL, LockedAt=NULL, LockedByAppUserID=NULL,
           TokenVersion=TokenVersion+1
     WHERE Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang');

    IF @@ROWCOUNT <> 4
        THROW 52200, N'Không tìm thấy đủ 4 tài khoản gốc để cập nhật.', 1;

    UPDATE dbo.AdministrativeProvinces
       SET ProvinceType=N'Tỉnh', IsActive=1, EffectiveTo=NULL
     WHERE ProvinceCode='75' AND ProvinceName=N'Đồng Nai';

    IF @@ROWCOUNT <> 1
        THROW 52201, N'Không tìm thấy đúng bản ghi Đồng Nai mã 75.', 1;

    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1) <> 34
        THROW 52202, N'Số tỉnh/thành đang hoạt động không bằng 34.', 1;
    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1 AND ProvinceType=N'Tỉnh') <> 28
        THROW 52203, N'Số tỉnh không bằng 28.', 1;
    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1 AND ProvinceType=N'Thành phố') <> 6
        THROW 52204, N'Số thành phố không bằng 6.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
PRINT N'Vá v31.0.15.2 thành công. Mật khẩu 4 tài khoản gốc: a12345678';
SELECT u.Username,N'a12345678' AS [Password],r.AppRoleName,u.EmailConfirmed,u.IsActive,u.IsDeleted
FROM dbo.AppUsers u JOIN dbo.AppRoles r ON r.AppRoleId=u.AppRoleId
WHERE u.Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang')
ORDER BY u.AppUserId;
SELECT ProvinceCode,ProvinceType,ProvinceName,IsActive
FROM dbo.AdministrativeProvinces WHERE ProvinceCode='75';
GO
