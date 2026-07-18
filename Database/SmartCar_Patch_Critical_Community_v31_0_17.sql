/*
 SmartCar Critical Community Patch v31.0.17
 Run once after SmartCar_FULL_ONE_CLICK_RESET_INSTALL_v31_0_16.sql.
 Adds encrypted-at-rest columns for vehicle partner identity and bank data.
 The Web API automatically encrypts and clears legacy plaintext values after startup.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'CitizenIdentityNumberEncrypted') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD CitizenIdentityNumberEncrypted nvarchar(max) NULL;

IF COL_LENGTH(N'dbo.VehiclePartnerProfiles', N'BankAccountNumberEncrypted') IS NULL
    ALTER TABLE dbo.VehiclePartnerProfiles ADD BankAccountNumberEncrypted nvarchar(max) NULL;

COMMIT TRANSACTION;
