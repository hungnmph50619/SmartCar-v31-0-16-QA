/* SMARTCAR v31.0.8 - Nâng cấp địa điểm chi tiết và bản đồ
   Dùng cho database v31.0 đã cài trước đó. Không xóa dữ liệu đặt xe.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
USE [SmartCarMarketplaceDb];
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Locations','ProvinceCity') IS NULL ALTER TABLE dbo.Locations ADD ProvinceCity nvarchar(120) NOT NULL CONSTRAINT DF_Locations_ProvinceCity DEFAULT(N'');
    IF COL_LENGTH('dbo.Locations','District') IS NULL ALTER TABLE dbo.Locations ADD District nvarchar(120) NOT NULL CONSTRAINT DF_Locations_District DEFAULT(N'');
    IF COL_LENGTH('dbo.Locations','Ward') IS NULL ALTER TABLE dbo.Locations ADD Ward nvarchar(120) NOT NULL CONSTRAINT DF_Locations_Ward DEFAULT(N'');
    IF COL_LENGTH('dbo.Locations','AddressDetail') IS NULL ALTER TABLE dbo.Locations ADD AddressDetail nvarchar(500) NOT NULL CONSTRAINT DF_Locations_AddressDetail DEFAULT(N'');
    IF COL_LENGTH('dbo.Locations','Latitude') IS NULL ALTER TABLE dbo.Locations ADD Latitude decimal(10,7) NULL;
    IF COL_LENGTH('dbo.Locations','Longitude') IS NULL ALTER TABLE dbo.Locations ADD Longitude decimal(10,7) NULL;
    IF COL_LENGTH('dbo.Locations','SearchRadiusKm') IS NULL ALTER TABLE dbo.Locations ADD SearchRadiusKm int NOT NULL CONSTRAINT DF_Locations_SearchRadiusKm DEFAULT(20);
    IF COL_LENGTH('dbo.Locations','IsActive') IS NULL ALTER TABLE dbo.Locations ADD IsActive bit NOT NULL CONSTRAINT DF_Locations_IsActive DEFAULT(1);

    UPDATE dbo.Locations SET Name=N'Điểm giao xe Cầu Giấy',ProvinceCity=N'Hà Nội',District=N'Cầu Giấy',Ward=N'',AddressDetail=N'Khu vực Công viên Cầu Giấy',Latitude=21.0285110,Longitude=105.7909930,SearchRadiusKm=20,IsActive=1 WHERE LocationID=1;
    UPDATE dbo.Locations SET Name=N'Điểm giao xe trung tâm Ninh Bình',ProvinceCity=N'Ninh Bình',District=N'Trung tâm Ninh Bình',Ward=N'',AddressDetail=N'Khu vực trung tâm thành phố Ninh Bình',Latitude=20.2506140,Longitude=105.9744540,SearchRadiusKm=20,IsActive=1 WHERE LocationID=2;
    UPDATE dbo.Locations SET Name=N'Điểm giao xe Bến Thành',ProvinceCity=N'TP Hồ Chí Minh',District=N'Quận 1',Ward=N'',AddressDetail=N'Khu vực Chợ Bến Thành',Latitude=10.7721550,Longitude=106.6982780,SearchRadiusKm=20,IsActive=1 WHERE LocationID=3;
    UPDATE dbo.Locations SET Name=N'Điểm giao xe Cầu Rồng',ProvinceCity=N'Đà Nẵng',District=N'Hải Châu',Ward=N'',AddressDetail=N'Khu vực đầu Cầu Rồng phía trung tâm',Latitude=16.0610990,Longitude=108.2273150,SearchRadiusKm=20,IsActive=1 WHERE LocationID=4;

    SET IDENTITY_INSERT dbo.Locations ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=5) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(5,N'Điểm giao xe Mỹ Đình',N'Hà Nội',N'Nam Từ Liêm',N'',N'Khu vực Bến xe Mỹ Đình',21.0282100,105.7783000,15,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=6) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(6,N'Điểm giao xe Nội Bài',N'Hà Nội',N'Sóc Sơn',N'',N'Khu vực Sân bay Nội Bài',21.2187149,105.8041709,30,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=7) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(7,N'Điểm giao xe Hoàn Kiếm',N'Hà Nội',N'Hoàn Kiếm',N'',N'Khu vực Hồ Hoàn Kiếm',21.0286669,105.8521484,15,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=8) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(8,N'Điểm giao xe Tràng An',N'Ninh Bình',N'Khu du lịch Tràng An',N'',N'Khu vực bến thuyền Tràng An',20.2520540,105.9178060,20,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=9) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(9,N'Điểm giao xe Bình Thạnh',N'TP Hồ Chí Minh',N'Bình Thạnh',N'',N'Khu vực trung tâm Bình Thạnh',10.8032380,106.7075480,20,1);
    IF NOT EXISTS(SELECT 1 FROM dbo.Locations WHERE LocationID=10) INSERT dbo.Locations(LocationID,Name,ProvinceCity,District,Ward,AddressDetail,Latitude,Longitude,SearchRadiusKm,IsActive) VALUES(10,N'Điểm giao xe Sơn Trà',N'Đà Nẵng',N'Sơn Trà',N'',N'Khu vực bờ đông Sông Hàn',16.0677800,108.2426400,20,1);
    SET IDENTITY_INSERT dbo.Locations OFF;

    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Locations_Latitude') ALTER TABLE dbo.Locations ADD CONSTRAINT CK_Locations_Latitude CHECK (Latitude IS NULL OR (Latitude>=-90 AND Latitude<=90));
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Locations_Longitude') ALTER TABLE dbo.Locations ADD CONSTRAINT CK_Locations_Longitude CHECK (Longitude IS NULL OR (Longitude>=-180 AND Longitude<=180));
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Locations_CoordinatesPair') ALTER TABLE dbo.Locations ADD CONSTRAINT CK_Locations_CoordinatesPair CHECK ((Latitude IS NULL AND Longitude IS NULL) OR (Latitude IS NOT NULL AND Longitude IS NOT NULL));
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Locations_SearchRadiusKm') ALTER TABLE dbo.Locations ADD CONSTRAINT CK_Locations_SearchRadiusKm CHECK (SearchRadiusKm BETWEEN 1 AND 100);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'IX_Locations_Active_Province_District' AND object_id=OBJECT_ID(N'dbo.Locations')) CREATE INDEX IX_Locations_Active_Province_District ON dbo.Locations(IsActive,ProvinceCity,District);

    IF OBJECT_ID(N'dbo.SystemVersions', N'U') IS NOT NULL
    BEGIN
        UPDATE dbo.SystemVersions
        SET ApplicationVersion=N'31.0.8',
            ReleasedDate=SYSUTCDATETIME(),
            Notes=N'v31.0.8: địa điểm chi tiết, bản đồ OpenStreetMap, tìm xe theo bán kính và địa chỉ giao nhận.'
        WHERE IsCurrent=1;
    END;

    COMMIT TRANSACTION;
    SELECT N'ĐÃ NÂNG CẤP ĐỊA ĐIỂM VÀ BẢN ĐỒ v31.0.8' AS Result, COUNT(*) AS LocationCount FROM dbo.Locations;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    IF OBJECTPROPERTY(OBJECT_ID('dbo.Locations'),'TableHasIdentity')=1 BEGIN TRY SET IDENTITY_INSERT dbo.Locations OFF; END TRY BEGIN CATCH END CATCH;
    THROW;
END CATCH;
GO
