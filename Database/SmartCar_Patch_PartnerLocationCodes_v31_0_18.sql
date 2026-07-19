/*
 SmartCar partner location schema patch v31.0.18
 Fixes SQL Server error 207 when the Web API expects location-code columns
 in dbo.VehiclePartnerProfiles but an older local/demo database does not have them.

 Safe to run more than once. Does not delete or overwrite existing data.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.VehiclePartnerProfiles', N'U') IS NULL
    THROW 52020, N'Không tìm thấy bảng dbo.VehiclePartnerProfiles. Hãy chạy bộ cài CSDL SmartCar trước.', 1;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'CurrentProvinceCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD CurrentProvinceCode varchar(2) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'CurrentWardCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD CurrentWardCode varchar(5) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'HeadquartersProvinceCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD HeadquartersProvinceCode varchar(2) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'HeadquartersWardCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD HeadquartersWardCode varchar(5) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'PermanentProvinceCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD PermanentProvinceCode varchar(2) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'PermanentWardCode') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD PermanentWardCode varchar(5) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VehiclePartnerProfiles_CurrentAdministrativeCodes'
      AND object_id = OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
    CREATE INDEX IX_VehiclePartnerProfiles_CurrentAdministrativeCodes
        ON dbo.VehiclePartnerProfiles(CurrentProvinceCode, CurrentWardCode);

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes'
      AND object_id = OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
    CREATE INDEX IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes
        ON dbo.VehiclePartnerProfiles(HeadquartersProvinceCode, HeadquartersWardCode);

COMMIT TRANSACTION;

SELECT
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'CurrentProvinceCode') AS CurrentProvinceCode,
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'CurrentWardCode') AS CurrentWardCode,
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'HeadquartersProvinceCode') AS HeadquartersProvinceCode,
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'HeadquartersWardCode') AS HeadquartersWardCode,
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'PermanentProvinceCode') AS PermanentProvinceCode,
    COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'PermanentWardCode') AS PermanentWardCode;
