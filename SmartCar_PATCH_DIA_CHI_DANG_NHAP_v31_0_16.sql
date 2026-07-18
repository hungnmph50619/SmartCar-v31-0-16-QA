/* SmartCar v31.0.16 - bổ sung mã hành chính cho hồ sơ đối tác, KHÔNG xóa dữ liệu. */
USE [SmartCarMarketplaceDb];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.VehiclePartnerProfiles', N'U') IS NULL OR
   OBJECT_ID(N'dbo.AdministrativeProvinces', N'U') IS NULL OR
   OBJECT_ID(N'dbo.AdministrativeWards', N'U') IS NULL
    THROW 53200, N'Database chưa có cấu trúc hành chính của SmartCar v31.0.15.3. Hãy chạy file FULL ONE CLICK v31.0.16.', 1;

/* Tạo cột trong batch riêng để các lệnh phía sau được biên dịch với schema mới. */
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'PermanentProvinceCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD PermanentProvinceCode varchar(2) NULL;');
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'PermanentWardCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD PermanentWardCode varchar(5) NULL;');
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'CurrentProvinceCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD CurrentProvinceCode varchar(2) NULL;');
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'CurrentWardCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD CurrentWardCode varchar(5) NULL;');
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'HeadquartersProvinceCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD HeadquartersProvinceCode varchar(2) NULL;');
IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'HeadquartersWardCode') IS NULL
    EXEC(N'ALTER TABLE dbo.VehiclePartnerProfiles ADD HeadquartersWardCode varchar(5) NULL;');
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1) <> 34
        THROW 53201, N'Database hiện tại không có đúng 34 tỉnh/thành phố. Hãy chạy file FULL ONE CLICK v31.0.16.', 1;
    IF (SELECT COUNT(*) FROM dbo.AdministrativeWards WHERE IsActive=1) <> 3321
        THROW 53202, N'Database hiện tại không có đúng 3321 xã/phường/đặc khu. Hãy chạy file FULL ONE CLICK v31.0.16.', 1;

    /* Điền mã cho hồ sơ cũ nếu tên đang lưu khớp danh mục chuẩn. */
    UPDATE vp SET PermanentProvinceCode=p.ProvinceCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeProvinces p
        ON LTRIM(RTRIM(vp.PermanentProvince)) IN (p.ProvinceName, CONCAT(p.ProvinceType,N' ',p.ProvinceName))
     WHERE vp.PermanentProvinceCode IS NULL;

    UPDATE vp SET CurrentProvinceCode=p.ProvinceCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeProvinces p
        ON LTRIM(RTRIM(vp.CurrentProvince)) IN (p.ProvinceName, CONCAT(p.ProvinceType,N' ',p.ProvinceName))
     WHERE vp.CurrentProvinceCode IS NULL;

    UPDATE vp SET HeadquartersProvinceCode=p.ProvinceCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeProvinces p
        ON LTRIM(RTRIM(vp.HeadquartersProvince)) IN (p.ProvinceName, CONCAT(p.ProvinceType,N' ',p.ProvinceName))
     WHERE vp.HeadquartersProvinceCode IS NULL;

    UPDATE vp SET PermanentWardCode=w.WardCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeWards w ON w.ProvinceCode=vp.PermanentProvinceCode
       AND LTRIM(RTRIM(vp.PermanentWard)) IN (w.WardName, CONCAT(w.WardType,N' ',w.WardName))
     WHERE vp.PermanentWardCode IS NULL;

    UPDATE vp SET CurrentWardCode=w.WardCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeWards w ON w.ProvinceCode=vp.CurrentProvinceCode
       AND LTRIM(RTRIM(vp.CurrentWard)) IN (w.WardName, CONCAT(w.WardType,N' ',w.WardName))
     WHERE vp.CurrentWardCode IS NULL;

    UPDATE vp SET HeadquartersWardCode=w.WardCode
      FROM dbo.VehiclePartnerProfiles vp
      JOIN dbo.AdministrativeWards w ON w.ProvinceCode=vp.HeadquartersProvinceCode
       AND LTRIM(RTRIM(vp.HeadquartersWard)) IN (w.WardName, CONCAT(w.WardType,N' ',w.WardName))
     WHERE vp.HeadquartersWardCode IS NULL;

    UPDATE dbo.VehiclePartnerProfiles
       SET CurrentProvinceCode=PermanentProvinceCode,
           CurrentWardCode=PermanentWardCode
     WHERE CurrentAddressSameAsPermanent=1;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_PermanentProvince')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_PermanentProvince FOREIGN KEY(PermanentProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_PermanentWard')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_PermanentWard FOREIGN KEY(PermanentWardCode) REFERENCES dbo.AdministrativeWards(WardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_CurrentProvince')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_CurrentProvince FOREIGN KEY(CurrentProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_CurrentWard')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_CurrentWard FOREIGN KEY(CurrentWardCode) REFERENCES dbo.AdministrativeWards(WardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_HeadquartersProvince')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_HeadquartersProvince FOREIGN KEY(HeadquartersProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePartnerProfiles_HeadquartersWard')
        ALTER TABLE dbo.VehiclePartnerProfiles WITH CHECK ADD CONSTRAINT FK_VehiclePartnerProfiles_HeadquartersWard FOREIGN KEY(HeadquartersWardCode) REFERENCES dbo.AdministrativeWards(WardCode);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehiclePartnerProfiles_PermanentAdministrativeCodes' AND object_id=OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
        CREATE INDEX IX_VehiclePartnerProfiles_PermanentAdministrativeCodes ON dbo.VehiclePartnerProfiles(PermanentProvinceCode,PermanentWardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehiclePartnerProfiles_CurrentAdministrativeCodes' AND object_id=OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
        CREATE INDEX IX_VehiclePartnerProfiles_CurrentAdministrativeCodes ON dbo.VehiclePartnerProfiles(CurrentProvinceCode,CurrentWardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes' AND object_id=OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
        CREATE INDEX IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes ON dbo.VehiclePartnerProfiles(HeadquartersProvinceCode,HeadquartersWardCode);

    IF OBJECT_ID(N'dbo.SystemVersions', N'U') IS NOT NULL
    BEGIN
        UPDATE dbo.SystemVersions SET IsCurrent=0 WHERE IsCurrent=1;
        IF EXISTS (SELECT 1 FROM dbo.SystemVersions WHERE DatabaseVersion=N'31.0')
            UPDATE dbo.SystemVersions
               SET ApplicationVersion=N'31.0.16', ReleasedDate=SYSUTCDATETIME(), IsCurrent=1,
                   Notes=N'v31.0.16: hồ sơ đối tác dùng đủ 34 tỉnh và 3321 xã/phường/đặc khu; khóa đăng nhập sai 5 lần trong 15 phút.'
             WHERE DatabaseVersion=N'31.0';
        ELSE
            INSERT dbo.SystemVersions(ApplicationVersion,DatabaseVersion,ReleasedDate,IsCurrent,Notes)
            VALUES(N'31.0.16',N'31.0',SYSUTCDATETIME(),1,N'v31.0.16: địa chỉ hành chính đối tác và giới hạn đăng nhập sai.');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Vá SmartCar v31.0.16 thành công, không xóa dữ liệu.';
SELECT COUNT(*) AS ActiveProvinceCount FROM dbo.AdministrativeProvinces WHERE IsActive=1;
SELECT COUNT(*) AS ActiveWardCount FROM dbo.AdministrativeWards WHERE IsActive=1;
GO
