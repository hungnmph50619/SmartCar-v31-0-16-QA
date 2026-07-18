/*
 SmartCar v31.0.15.2
 Reset hồ sơ xác minh khách để demo lại quy trình Customer -> Staff -> phê duyệt.
 Ưu tiên username khachhang01 nếu tồn tại; nếu không sẽ reset tài khoản khachhang.
 Không xóa đơn thuê, thanh toán hoặc dữ liệu xe.
*/
USE [SmartCarMarketplaceDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AppUsers',N'U') IS NULL OR OBJECT_ID(N'dbo.UserVerifications',N'U') IS NULL
    THROW 52100, N'Thiếu bảng AppUsers hoặc UserVerifications. Hãy cài đúng database SmartCar.', 1;
GO

DECLARE @Username nvarchar(100) =
    CASE
        WHEN EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username=N'khachhang01' AND IsDeleted=0) THEN N'khachhang01'
        WHEN EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username=N'khachhang' AND IsDeleted=0) THEN N'khachhang'
        ELSE NULL
    END;

IF @Username IS NULL
    THROW 52101, N'Không tìm thấy tài khoản khachhang01 hoặc khachhang.', 1;

DECLARE @AppUserID int = (SELECT TOP(1) AppUserId FROM dbo.AppUsers WHERE Username=@Username AND IsDeleted=0);
DECLARE @UserVerificationID int = (SELECT TOP(1) UserVerificationID FROM dbo.UserVerifications WHERE AppUserID=@AppUserID AND VerificationType=N'Khách thuê');

BEGIN TRY
    BEGIN TRANSACTION;

    IF @UserVerificationID IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.WorkItemClaims',N'U') IS NOT NULL
            DELETE FROM dbo.WorkItemClaims
             WHERE QueueType=N'Xác minh khách' AND EntityID=@UserVerificationID;

        IF OBJECT_ID(N'dbo.PrivateFiles',N'U') IS NOT NULL
        BEGIN
            UPDATE pf
               SET IsDeleted=1,
                   DeleteRequestedDate=COALESCE(DeleteRequestedDate,SYSUTCDATETIME()),
                   AttachedEntityType=NULL,
                   AttachedEntityID=NULL,
                   AttachedDate=NULL
              FROM dbo.PrivateFiles pf
              JOIN dbo.UserVerifications uv ON uv.UserVerificationID=@UserVerificationID
             WHERE pf.PrivateFileID IN
                   (uv.CitizenIdFrontFileID,uv.CitizenIdBackFileID,uv.DriverLicenseFileID,uv.PortraitFileID);
        END;

        DELETE FROM dbo.UserVerifications WHERE UserVerificationID=@UserVerificationID;
    END;

    IF OBJECT_ID(N'dbo.Notifications',N'U') IS NOT NULL
        DELETE FROM dbo.Notifications WHERE AppUserID=@AppUserID AND [Type]=N'Verification';

    UPDATE dbo.AppUsers
       SET IsActive=1,
           IsDeleted=0,
           EmailConfirmed=1,
           FailedLoginCount=0,
           LockoutEnd=NULL,
           LockType=NULL,
           LockReason=NULL,
           LockedAt=NULL,
           LockedByAppUserID=NULL,
           BookingRestrictedUntil=NULL,
           BookingRestrictionReason=NULL,
           TokenVersion=TokenVersion+1
     WHERE AppUserId=@AppUserID;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'Đã reset hồ sơ xác minh cho tài khoản: ' + @Username;
SELECT
    u.AppUserId,
    u.Username,
    u.Email,
    u.EmailConfirmed,
    u.IsActive,
    CASE WHEN uv.UserVerificationID IS NULL THEN N'Chưa có hồ sơ - sẵn sàng nhập và gửi mới' ELSE uv.Status END AS VerificationStatus
FROM dbo.AppUsers u
LEFT JOIN dbo.UserVerifications uv ON uv.AppUserID=u.AppUserId AND uv.VerificationType=N'Khách thuê'
WHERE u.AppUserId=@AppUserID;
GO
