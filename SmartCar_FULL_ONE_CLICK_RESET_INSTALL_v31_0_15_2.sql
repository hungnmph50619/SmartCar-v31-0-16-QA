/* ============================================================================
   SMARTCAR FULL ONE-CLICK RESET & INSTALL v31.0.15.1

   CÁCH DÙNG:
     1. Mở file này bằng SQL Server Management Studio.
     2. Kết nối đúng SQL Server instance.
     3. Nhấn Execute đúng 1 lần.

   SCRIPT TỰ ĐỘNG:
     - Xóa database SmartCarMarketplaceDb cũ nếu có.
     - Tạo lại toàn bộ cấu trúc và dữ liệu nền.
     - Nạp 34 tỉnh/thành phố và 3.321 xã/phường/đặc khu.
     - Tạo 4 tài khoản test, mật khẩu chung a12345678.

   CẢNH BÁO NGHIÊM TRỌNG:
     - MỌI DỮ LIỆU CŨ TRONG SmartCarMarketplaceDb SẼ BỊ XÓA VĨNH VIỄN.
     - Chỉ dùng cho môi trường LOCAL/DEMO. Không dùng trên production.
   ============================================================================ */

SET NOCOUNT ON;
GO

USE [master];
GO

/*
    ONE-CLICK RESET:
    - Tự ngắt mọi kết nối tới SmartCarMarketplaceDb.
    - Xóa toàn bộ database cũ nếu đang tồn tại.
    - Tạo lại database mới từ đầu.

    CẢNH BÁO: MỌI DỮ LIỆU CŨ TRONG SmartCarMarketplaceDb SẼ BỊ XÓA.
*/
IF DB_ID(N'SmartCarMarketplaceDb') IS NOT NULL
BEGIN
    PRINT N'Đang ngắt kết nối và xóa database SmartCarMarketplaceDb cũ...';
    ALTER DATABASE [SmartCarMarketplaceDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [SmartCarMarketplaceDb];
END
GO

PRINT N'Đang tạo database SmartCarMarketplaceDb mới...';
CREATE DATABASE [SmartCarMarketplaceDb];
GO
USE [SmartCarMarketplaceDb];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY,
    [ProductVersion] nvarchar(32) NOT NULL
);
GO

CREATE TABLE [dbo].[SystemVersions] (
    [SystemVersionID] int IDENTITY(1,1) NOT NULL,
    [ApplicationVersion] nvarchar(30) NOT NULL,
    [DatabaseVersion] nvarchar(30) NOT NULL,
    [ReleasedDate] datetime2 NOT NULL CONSTRAINT [DF_SystemVersions_ReleasedDate] DEFAULT(SYSUTCDATETIME()),
    [IsCurrent] bit NOT NULL CONSTRAINT [DF_SystemVersions_IsCurrent] DEFAULT(0),
    [Notes] nvarchar(1000) NULL,
    CONSTRAINT [PK_SystemVersions] PRIMARY KEY ([SystemVersionID])
);
GO

CREATE TABLE [dbo].[AppRoles] (
    [AppRoleId] int IDENTITY(1,1) NOT NULL,
    [AppRoleName] nvarchar(450) NOT NULL CONSTRAINT [DF_AppRoles_AppRoleName] DEFAULT(N''),
    CONSTRAINT [PK_AppRoles] PRIMARY KEY ([AppRoleId])
);
GO

CREATE TABLE [dbo].[AppUsers] (
    [AppUserId] int IDENTITY(1,1) NOT NULL,
    [Username] nvarchar(450) NOT NULL CONSTRAINT [DF_AppUsers_Username] DEFAULT(N''),
    [Password] nvarchar(max) NOT NULL CONSTRAINT [DF_AppUsers_Password] DEFAULT(N''),
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_AppUsers_Name] DEFAULT(N''),
    [Surname] nvarchar(450) NOT NULL CONSTRAINT [DF_AppUsers_Surname] DEFAULT(N''),
    [Email] nvarchar(450) NOT NULL CONSTRAINT [DF_AppUsers_Email] DEFAULT(N''),
    [Phone] nvarchar(20) NULL,
    [IsVehiclePartner] bit NOT NULL CONSTRAINT [DF_AppUsers_IsVehiclePartner] DEFAULT(0),
    [FailedLoginCount] int NOT NULL CONSTRAINT [DF_AppUsers_FailedLoginCount] DEFAULT(0),
    [LockoutEnd] datetime2 NULL,
    [LastLoginAt] datetime2 NULL,
    [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_AppUsers_EmailConfirmed] DEFAULT(0),
    [PendingEmail] nvarchar(256) NULL,
    [PendingEmailCreatedDate] datetime2 NULL,
    [RegistrationExpiresDate] datetime2 NULL,
    [TokenVersion] int NOT NULL CONSTRAINT [DF_AppUsers_TokenVersion] DEFAULT(0),
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_AppUsers_IsDeleted] DEFAULT(0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_AppUsers_IsActive] DEFAULT(0),
    [LockType] nvarchar(30) NULL,
    [LockReason] nvarchar(500) NULL,
    [LockedAt] datetime2 NULL,
    [LockedByAppUserID] int NULL,
    [BookingRestrictedUntil] datetime2 NULL,
    [BookingRestrictionReason] nvarchar(500) NULL,
    [DeletedAt] datetime2 NULL,
    [DeletedByUserId] int NULL,
    [DeleteReason] nvarchar(500) NULL,
    [AnonymizedAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    [AppRoleId] int NOT NULL CONSTRAINT [DF_AppUsers_AppRoleId] DEFAULT(0),
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([AppUserId])
);
GO

CREATE TABLE [dbo].[EmailVerificationOtps] (
    [EmailVerificationOtpID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_EmailVerificationOtps_AppUserID] DEFAULT(0),
    [OtpHash] nvarchar(128) NOT NULL CONSTRAINT [DF_EmailVerificationOtps_OtpHash] DEFAULT(N''),
    [Purpose] nvarchar(50) NOT NULL CONSTRAINT [DF_EmailVerificationOtps_Purpose] DEFAULT(N'Register'),
    [TargetEmail] nvarchar(256) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_EmailVerificationOtps_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ExpiresDate] datetime2 NOT NULL CONSTRAINT [DF_EmailVerificationOtps_ExpiresDate] DEFAULT(SYSUTCDATETIME()),
    [UsedDate] datetime2 NULL,
    [LastSentAt] datetime2 NULL,
    [FailedAttempts] int NOT NULL CONSTRAINT [DF_EmailVerificationOtps_FailedAttempts] DEFAULT(0),
    [LockedUntil] datetime2 NULL,
    CONSTRAINT [PK_EmailVerificationOtps] PRIMARY KEY ([EmailVerificationOtpID])
);
GO

CREATE TABLE [dbo].[PasswordResetTokens] (
    [PasswordResetTokenID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_PasswordResetTokens_AppUserID] DEFAULT(0),
    [TokenHash] nvarchar(128) NOT NULL CONSTRAINT [DF_PasswordResetTokens_TokenHash] DEFAULT(N''),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PasswordResetTokens_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ExpiresDate] datetime2 NOT NULL CONSTRAINT [DF_PasswordResetTokens_ExpiresDate] DEFAULT(SYSUTCDATETIME()),
    [UsedDate] datetime2 NULL,
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([PasswordResetTokenID])
);
GO

CREATE TABLE [dbo].[Abouts] (
    [AboutID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_Abouts_Title] DEFAULT(N''),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Abouts_Description] DEFAULT(N''),
    [ImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Abouts_ImageUrl] DEFAULT(N''),
    CONSTRAINT [PK_Abouts] PRIMARY KEY ([AboutID])
);
GO

CREATE TABLE [dbo].[Banners] (
    [BannerID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_Banners_Title] DEFAULT(N''),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Banners_Description] DEFAULT(N''),
    [VideoDescription] nvarchar(max) NOT NULL CONSTRAINT [DF_Banners_VideoDescription] DEFAULT(N''),
    [VideoUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Banners_VideoUrl] DEFAULT(N''),
    CONSTRAINT [PK_Banners] PRIMARY KEY ([BannerID])
);
GO

CREATE TABLE [dbo].[Brands] (
    [BrandID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Brands_Name] DEFAULT(N''),
    CONSTRAINT [PK_Brands] PRIMARY KEY ([BrandID])
);
GO

CREATE TABLE [dbo].[Cars] (
    [CarID] int IDENTITY(1,1) NOT NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_Cars_IsDeleted] DEFAULT(0),
    [DeletedAt] datetime2 NULL,
    [DeletedByUserId] int NULL,
    [DeleteReason] nvarchar(500) NULL,
    [LifecycleStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_Cars_LifecycleStatus] DEFAULT(N''),
    [RowVersion] rowversion NOT NULL,
    [BrandID] int NOT NULL CONSTRAINT [DF_Cars_BrandID] DEFAULT(0),
    [Model] nvarchar(max) NOT NULL CONSTRAINT [DF_Cars_Model] DEFAULT(N''),
    [CoverImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Cars_CoverImageUrl] DEFAULT(N''),
    [Km] int NOT NULL CONSTRAINT [DF_Cars_Km] DEFAULT(0),
    [Transmission] nvarchar(max) NOT NULL CONSTRAINT [DF_Cars_Transmission] DEFAULT(N''),
    [Seat] tinyint NOT NULL CONSTRAINT [DF_Cars_Seat] DEFAULT(0),
    [Luggage] tinyint NOT NULL CONSTRAINT [DF_Cars_Luggage] DEFAULT(0),
    [Fuel] nvarchar(max) NOT NULL CONSTRAINT [DF_Cars_Fuel] DEFAULT(N''),
    [BigImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Cars_BigImageUrl] DEFAULT(N''),
    CONSTRAINT [PK_Cars] PRIMARY KEY ([CarID])
);
GO

CREATE TABLE [dbo].[Features] (
    [FeatureID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Features_Name] DEFAULT(N''),
    CONSTRAINT [PK_Features] PRIMARY KEY ([FeatureID])
);
GO

CREATE TABLE [dbo].[CarFeatures] (
    [CarFeatureID] int IDENTITY(1,1) NOT NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_CarFeatures_CarID] DEFAULT(0),
    [FeatureID] int NOT NULL CONSTRAINT [DF_CarFeatures_FeatureID] DEFAULT(0),
    [Available] bit NOT NULL CONSTRAINT [DF_CarFeatures_Available] DEFAULT(0),
    CONSTRAINT [PK_CarFeatures] PRIMARY KEY ([CarFeatureID])
);
GO

CREATE TABLE [dbo].[CarDescriptions] (
    [CarDescriptionID] int IDENTITY(1,1) NOT NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_CarDescriptions_CarID] DEFAULT(0),
    [Details] nvarchar(max) NOT NULL CONSTRAINT [DF_CarDescriptions_Details] DEFAULT(N''),
    CONSTRAINT [PK_CarDescriptions] PRIMARY KEY ([CarDescriptionID])
);
GO

CREATE TABLE [dbo].[Pricings] (
    [PricingID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Pricings_Name] DEFAULT(N''),
    CONSTRAINT [PK_Pricings] PRIMARY KEY ([PricingID])
);
GO

CREATE TABLE [dbo].[CarPricings] (
    [CarPricingID] int IDENTITY(1,1) NOT NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_CarPricings_CarID] DEFAULT(0),
    [PricingID] int NOT NULL CONSTRAINT [DF_CarPricings_PricingID] DEFAULT(0),
    [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_CarPricings_Amount] DEFAULT(0),
    CONSTRAINT [PK_CarPricings] PRIMARY KEY ([CarPricingID])
);
GO

CREATE TABLE [dbo].[Categories] (
    [CategoryID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Categories_Name] DEFAULT(N''),
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID])
);
GO

CREATE TABLE [dbo].[Authors] (
    [AuthorID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Authors_Name] DEFAULT(N''),
    [ImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Authors_ImageUrl] DEFAULT(N''),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Authors_Description] DEFAULT(N''),
    CONSTRAINT [PK_Authors] PRIMARY KEY ([AuthorID])
);
GO

CREATE TABLE [dbo].[Blogs] (
    [BlogID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_Blogs_Title] DEFAULT(N''),
    [AuthorID] int NOT NULL CONSTRAINT [DF_Blogs_AuthorID] DEFAULT(0),
    [CoverImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Blogs_CoverImageUrl] DEFAULT(N''),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Blogs_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [CategoryID] int NOT NULL CONSTRAINT [DF_Blogs_CategoryID] DEFAULT(0),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Blogs_Description] DEFAULT(N''),
    CONSTRAINT [PK_Blogs] PRIMARY KEY ([BlogID])
);
GO

CREATE TABLE [dbo].[TagClouds] (
    [TagCloudID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_TagClouds_Title] DEFAULT(N''),
    [BlogID] int NOT NULL CONSTRAINT [DF_TagClouds_BlogID] DEFAULT(0),
    CONSTRAINT [PK_TagClouds] PRIMARY KEY ([TagCloudID])
);
GO

CREATE TABLE [dbo].[Comments] (
    [CommentID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Comments_Name] DEFAULT(N''),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Comments_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Comments_Description] DEFAULT(N''),
    [Email] nvarchar(450) NOT NULL CONSTRAINT [DF_Comments_Email] DEFAULT(N''),
    [BlogID] int NOT NULL CONSTRAINT [DF_Comments_BlogID] DEFAULT(0),
    CONSTRAINT [PK_Comments] PRIMARY KEY ([CommentID])
);
GO

CREATE TABLE [dbo].[Contacts] (
    [ContactID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Contacts_Name] DEFAULT(N''),
    [Email] nvarchar(450) NOT NULL CONSTRAINT [DF_Contacts_Email] DEFAULT(N''),
    [Subject] nvarchar(max) NOT NULL CONSTRAINT [DF_Contacts_Subject] DEFAULT(N''),
    [Message] nvarchar(max) NOT NULL CONSTRAINT [DF_Contacts_Message] DEFAULT(N''),
    [SendDate] datetime2 NOT NULL CONSTRAINT [DF_Contacts_SendDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_Contacts] PRIMARY KEY ([ContactID])
);
GO

CREATE TABLE [dbo].[FooterAddresses] (
    [FooterAddressID] int IDENTITY(1,1) NOT NULL,
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_FooterAddresses_Description] DEFAULT(N''),
    [Address] nvarchar(max) NOT NULL CONSTRAINT [DF_FooterAddresses_Address] DEFAULT(N''),
    [Phone] nvarchar(450) NOT NULL CONSTRAINT [DF_FooterAddresses_Phone] DEFAULT(N''),
    [Email] nvarchar(450) NOT NULL CONSTRAINT [DF_FooterAddresses_Email] DEFAULT(N''),
    CONSTRAINT [PK_FooterAddresses] PRIMARY KEY ([FooterAddressID])
);
GO

CREATE TABLE [dbo].[Locations] (
    [LocationID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(200) NOT NULL CONSTRAINT [DF_Locations_Name] DEFAULT(N''),
    [ProvinceCity] nvarchar(120) NOT NULL CONSTRAINT [DF_Locations_ProvinceCity] DEFAULT(N''),
    [District] nvarchar(120) NOT NULL CONSTRAINT [DF_Locations_District] DEFAULT(N''),
    [Ward] nvarchar(120) NOT NULL CONSTRAINT [DF_Locations_Ward] DEFAULT(N''),
    [AddressDetail] nvarchar(500) NOT NULL CONSTRAINT [DF_Locations_AddressDetail] DEFAULT(N''),
    [Latitude] decimal(10,7) NULL,
    [Longitude] decimal(10,7) NULL,
    [SearchRadiusKm] int NOT NULL CONSTRAINT [DF_Locations_SearchRadiusKm] DEFAULT(20),
    [IsActive] bit NOT NULL CONSTRAINT [DF_Locations_IsActive] DEFAULT(1),
    CONSTRAINT [CK_Locations_Latitude] CHECK ([Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)),
    CONSTRAINT [CK_Locations_Longitude] CHECK ([Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)),
    CONSTRAINT [CK_Locations_CoordinatesPair] CHECK (([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)),
    CONSTRAINT [CK_Locations_SearchRadiusKm] CHECK ([SearchRadiusKm] BETWEEN 1 AND 100),
    CONSTRAINT [PK_Locations] PRIMARY KEY ([LocationID])
);
GO
CREATE INDEX [IX_Locations_Active_Province_District] ON [dbo].[Locations]([IsActive],[ProvinceCity],[District]);
GO

CREATE TABLE [dbo].[Services] (
    [ServiceID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_Services_Title] DEFAULT(N''),
    [Description] nvarchar(max) NOT NULL CONSTRAINT [DF_Services_Description] DEFAULT(N''),
    [IconUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Services_IconUrl] DEFAULT(N''),
    CONSTRAINT [PK_Services] PRIMARY KEY ([ServiceID])
);
GO

CREATE TABLE [dbo].[SocialMedias] (
    [SocialMediaID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_SocialMedias_Name] DEFAULT(N''),
    [Url] nvarchar(max) NOT NULL CONSTRAINT [DF_SocialMedias_Url] DEFAULT(N''),
    [Icon] nvarchar(max) NOT NULL CONSTRAINT [DF_SocialMedias_Icon] DEFAULT(N''),
    CONSTRAINT [PK_SocialMedias] PRIMARY KEY ([SocialMediaID])
);
GO

CREATE TABLE [dbo].[Testimonials] (
    [TestimonialID] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Testimonials_Name] DEFAULT(N''),
    [Title] nvarchar(max) NOT NULL CONSTRAINT [DF_Testimonials_Title] DEFAULT(N''),
    [Comment] nvarchar(max) NOT NULL CONSTRAINT [DF_Testimonials_Comment] DEFAULT(N''),
    [ImageUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Testimonials_ImageUrl] DEFAULT(N''),
    CONSTRAINT [PK_Testimonials] PRIMARY KEY ([TestimonialID])
);
GO

CREATE TABLE [dbo].[RentACars] (
    [RentACarId] int IDENTITY(1,1) NOT NULL,
    [LocationID] int NOT NULL CONSTRAINT [DF_RentACars_LocationID] DEFAULT(0),
    [CarID] int NOT NULL CONSTRAINT [DF_RentACars_CarID] DEFAULT(0),
    [Available] bit NOT NULL CONSTRAINT [DF_RentACars_Available] DEFAULT(0),
    CONSTRAINT [PK_RentACars] PRIMARY KEY ([RentACarId])
);
GO

CREATE TABLE [dbo].[Customer] (
    [CustomerID] int IDENTITY(1,1) NOT NULL,
    [CustomerName] nvarchar(max) NOT NULL CONSTRAINT [DF_Customer_CustomerName] DEFAULT(N''),
    [CustomerSurname] nvarchar(max) NOT NULL CONSTRAINT [DF_Customer_CustomerSurname] DEFAULT(N''),
    [CustomerMail] nvarchar(max) NOT NULL CONSTRAINT [DF_Customer_CustomerMail] DEFAULT(N''),
    CONSTRAINT [PK_Customer] PRIMARY KEY ([CustomerID])
);
GO

CREATE TABLE [dbo].[RentACarProcess] (
    [RentACarProcessID] int IDENTITY(1,1) NOT NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_RentACarProcess_CarID] DEFAULT(0),
    [PickUpLocation] int NOT NULL CONSTRAINT [DF_RentACarProcess_PickUpLocation] DEFAULT(0),
    [DropOffLocation] int NOT NULL CONSTRAINT [DF_RentACarProcess_DropOffLocation] DEFAULT(0),
    [PickUpDate] date NOT NULL,
    [DropOffDate] date NOT NULL,
    [PickUpTime] time NOT NULL CONSTRAINT [DF_RentACarProcess_PickUpTime] DEFAULT(CONVERT(time,'00:00:00')),
    [DropOffTime] time NOT NULL CONSTRAINT [DF_RentACarProcess_DropOffTime] DEFAULT(CONVERT(time,'00:00:00')),
    [CustomerID] int NOT NULL CONSTRAINT [DF_RentACarProcess_CustomerID] DEFAULT(0),
    [PickUpDescription] nvarchar(max) NOT NULL CONSTRAINT [DF_RentACarProcess_PickUpDescription] DEFAULT(N''),
    [DropOffDescription] nvarchar(max) NOT NULL CONSTRAINT [DF_RentACarProcess_DropOffDescription] DEFAULT(N''),
    [TotalPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_RentACarProcess_TotalPrice] DEFAULT(0),
    CONSTRAINT [PK_RentACarProcess] PRIMARY KEY ([RentACarProcessID])
);
GO

CREATE TABLE [dbo].[VehiclePartnerProfiles] (
    [VehiclePartnerProfileID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_AppUserID] DEFAULT(0),
    [PartnerType] nvarchar(40) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PartnerType] DEFAULT(N'Cá nhân'),
    [FullName] nvarchar(160) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_FullName] DEFAULT(N''),
    [Phone] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_Phone] DEFAULT(N''),
    [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_Email] DEFAULT(N''),
    [Address] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_Address] DEFAULT(N''),
    [CitizenIdentityNumber] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CitizenIdentityNumber] DEFAULT(N''),
    [CitizenIdFingerprint] varchar(64) NULL,
    [DateOfBirth] datetime2 NULL,
    [Gender] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_Gender] DEFAULT(N''),
    [CitizenIssuedDate] datetime2 NULL,
    [CitizenExpiryDate] datetime2 NULL,
    [PermanentProvince] nvarchar(80) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PermanentProvince] DEFAULT(N''),
    [PermanentWard] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PermanentWard] DEFAULT(N''),
    [PermanentDetail] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PermanentDetail] DEFAULT(N''),
    [PermanentPaperAddress] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PermanentPaperAddress] DEFAULT(N''),
    [PermanentAddress] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PermanentAddress] DEFAULT(N''),
    [CurrentAddressSameAsPermanent] bit NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CurrentAddressSameAsPermanent] DEFAULT(0),
    [CurrentProvince] nvarchar(80) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CurrentProvince] DEFAULT(N''),
    [CurrentWard] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CurrentWard] DEFAULT(N''),
    [CurrentDetail] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CurrentDetail] DEFAULT(N''),
    [CurrentAddress] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CurrentAddress] DEFAULT(N''),
    [CitizenFrontImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CitizenFrontImageUrl] DEFAULT(N''),
    [CitizenBackImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CitizenBackImageUrl] DEFAULT(N''),
    [PortraitImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_PortraitImageUrl] DEFAULT(N''),
    [BusinessName] nvarchar(200) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BusinessName] DEFAULT(N''),
    [TaxCode] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_TaxCode] DEFAULT(N''),
    [BusinessRegistrationNumber] nvarchar(80) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BusinessRegistrationNumber] DEFAULT(N''),
    [HeadquartersProvince] nvarchar(80) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_HeadquartersProvince] DEFAULT(N''),
    [HeadquartersWard] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_HeadquartersWard] DEFAULT(N''),
    [HeadquartersDetail] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_HeadquartersDetail] DEFAULT(N''),
    [HeadquartersPaperAddress] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_HeadquartersPaperAddress] DEFAULT(N''),
    [HeadquartersAddress] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_HeadquartersAddress] DEFAULT(N''),
    [LegalRepresentativeName] nvarchar(160) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_LegalRepresentativeName] DEFAULT(N''),
    [AccountManagerName] nvarchar(160) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_AccountManagerName] DEFAULT(N''),
    [AccountManagerTitle] nvarchar(100) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_AccountManagerTitle] DEFAULT(N''),
    [RepresentativeName] nvarchar(160) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_RepresentativeName] DEFAULT(N''),
    [RepresentativeTitle] nvarchar(100) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_RepresentativeTitle] DEFAULT(N''),
    [BusinessLicenseImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BusinessLicenseImageUrl] DEFAULT(N''),
    [AuthorizationDocumentUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_AuthorizationDocumentUrl] DEFAULT(N''),
    [BankName] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BankName] DEFAULT(N''),
    [BankAccountNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BankAccountNumber] DEFAULT(N''),
    [BankAccountHolder] nvarchar(160) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BankAccountHolder] DEFAULT(N''),
    [BankBranch] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_BankBranch] DEFAULT(N''),
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_Status] DEFAULT(N'Bản nháp'),
    [ReviewNote] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_VehiclePartnerProfiles_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [SubmittedDate] datetime2 NULL,
    [ReviewedDate] datetime2 NULL,
    [ReviewedByAppUserID] int NULL,
    [PartnerTermsVersion] nvarchar(40) NULL,
    [PrivacyPolicyVersion] nvarchar(40) NULL,
    [TermsAcceptedAt] datetime2 NULL,
    [PrivacyAcceptedAt] datetime2 NULL,
    CONSTRAINT [PK_VehiclePartnerProfiles] PRIMARY KEY ([VehiclePartnerProfileID])
);
GO

CREATE TABLE [dbo].[VehiclePartnerApplications] (
    [VehiclePartnerApplicationID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_AppUserID] DEFAULT(0),
    [OwnerFullName] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_OwnerFullName] DEFAULT(N''),
    [Email] nvarchar(150) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Email] DEFAULT(N''),
    [Phone] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Phone] DEFAULT(N''),
    [Address] nvarchar(300) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Address] DEFAULT(N''),
    [CitizenIdentityNumber] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_CitizenIdentityNumber] DEFAULT(N''),
    [BankName] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_BankName] DEFAULT(N''),
    [BankAccountNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_BankAccountNumber] DEFAULT(N''),
    [BankAccountHolder] nvarchar(120) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_BankAccountHolder] DEFAULT(N''),
    [BrandName] nvarchar(100) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_BrandName] DEFAULT(N''),
    [Model] nvarchar(100) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Model] DEFAULT(N''),
    [VehicleVersion] nvarchar(100) NULL,
    [ChassisNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_ChassisNumber] DEFAULT(N''),
    [EngineNumber] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_EngineNumber] DEFAULT(N''),
    [ManufactureYear] int NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_ManufactureYear] DEFAULT(0),
    [LicensePlate] nvarchar(20) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_LicensePlate] DEFAULT(N''),
    [Color] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Color] DEFAULT(N''),
    [Transmission] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Transmission] DEFAULT(N''),
    [Fuel] nvarchar(50) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Fuel] DEFAULT(N''),
    [Seat] tinyint NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Seat] DEFAULT(0),
    [Km] int NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Km] DEFAULT(0),
    [LocationID] int NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_LocationID] DEFAULT(0),
    [ProposedDailyPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_ProposedDailyPrice] DEFAULT(0),
    [ProposedDepositAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_ProposedDepositAmount] DEFAULT(0),
    [RentalMode] nvarchar(30) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_RentalMode] DEFAULT(N'Tự lái'),
    [DriverFullName] nvarchar(120) NULL,
    [DriverPhone] nvarchar(20) NULL,
    [DriverCitizenIdentityNumber] nvarchar(20) NULL,
    [DriverLicenseNumber] nvarchar(50) NULL,
    [DriverLicenseClass] nvarchar(20) NULL,
    [DriverLicenseExpiryDate] datetime2 NULL,
    [DriverLicenseImageUrl] nvarchar(500) NULL,
    [DeliveryMethod] nvarchar(40) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_DeliveryMethod] DEFAULT(N'Nhận tại điểm giao xe'),
    [DeliveryAddress] nvarchar(300) NULL,
    [KmLimitPerDay] int NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_KmLimitPerDay] DEFAULT(0),
    [ExtraKmFee] decimal(18,2) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_ExtraKmFee] DEFAULT(0),
    [DeliveryFee] decimal(18,2) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_DeliveryFee] DEFAULT(0),
    [Amenities] nvarchar(1500) NULL,
    [Accessories] nvarchar(1500) NULL,
    [RentalConditions] nvarchar(1500) NULL,
    [CancellationPolicy] nvarchar(1500) NULL,
    [VehicleImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_VehicleImageUrl] DEFAULT(N''),
    [FrontImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_FrontImageUrl] DEFAULT(N''),
    [RearImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_RearImageUrl] DEFAULT(N''),
    [LeftImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_LeftImageUrl] DEFAULT(N''),
    [RightImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_RightImageUrl] DEFAULT(N''),
    [InteriorImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_InteriorImageUrl] DEFAULT(N''),
    [DashboardImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_DashboardImageUrl] DEFAULT(N''),
    [RegistrationImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_RegistrationImageUrl] DEFAULT(N''),
    [InspectionImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_InspectionImageUrl] DEFAULT(N''),
    [InsuranceImageUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_InsuranceImageUrl] DEFAULT(N''),
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_Status] DEFAULT(N''),
    [AdminNote] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_VehiclePartnerApplications_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ReviewedDate] datetime2 NULL,
    [ApprovedCarID] int NULL,
    CONSTRAINT [PK_VehiclePartnerApplications] PRIMARY KEY ([VehiclePartnerApplicationID]),
    CONSTRAINT [CK_VehiclePartnerApplications_RentalMode_ThreeOptions] CHECK ([RentalMode] IN (N'Tự lái', N'Có tài xế', N'Tự lái hoặc có tài xế')),
    CONSTRAINT [CK_VehiclePartnerApplications_DepositByRentalMode] CHECK ([RentalMode] <> N'Có tài xế' OR [ProposedDepositAmount] = 0),
    CONSTRAINT [CK_VehiclePartnerApplications_DeliveryFeeByMethod] CHECK ([DeliveryMethod] <> N'Nhận tại điểm giao xe' OR [DeliveryFee] = 0)
);
GO

CREATE TABLE [dbo].[PartnerVehicles] (
    [PartnerVehicleID] int IDENTITY(1,1) NOT NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_PartnerVehicles_CarID] DEFAULT(0),
    [OwnerAppUserID] int NOT NULL CONSTRAINT [DF_PartnerVehicles_OwnerAppUserID] DEFAULT(0),
    [VehiclePartnerApplicationID] int NOT NULL CONSTRAINT [DF_PartnerVehicles_VehiclePartnerApplicationID] DEFAULT(0),
    [CommissionRateOverride] decimal(5,2) NULL,
    [DepositAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_PartnerVehicles_DepositAmount] DEFAULT(0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_PartnerVehicles_IsActive] DEFAULT(0),
    [PauseReason] nvarchar(500) NULL,
    [ListedDate] datetime2 NOT NULL CONSTRAINT [DF_PartnerVehicles_ListedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_PartnerVehicles] PRIMARY KEY ([PartnerVehicleID])
);
GO

CREATE TABLE [dbo].[Reservations] (
    [ReservationID] int IDENTITY(1,1) NOT NULL,
    [CustomerAppUserID] int NOT NULL CONSTRAINT [DF_Reservations_CustomerAppUserID] DEFAULT(0),
    [PartnerVehicleID] int NOT NULL CONSTRAINT [DF_Reservations_PartnerVehicleID] DEFAULT(0),
    [Name] nvarchar(450) NOT NULL CONSTRAINT [DF_Reservations_Name] DEFAULT(N''),
    [Surname] nvarchar(450) NOT NULL CONSTRAINT [DF_Reservations_Surname] DEFAULT(N''),
    [Email] nvarchar(450) NOT NULL CONSTRAINT [DF_Reservations_Email] DEFAULT(N''),
    [Phone] nvarchar(450) NOT NULL CONSTRAINT [DF_Reservations_Phone] DEFAULT(N''),
    [PickUpLocationID] int NULL,
    [DropOffLocationID] int NULL,
    [CarID] int NOT NULL CONSTRAINT [DF_Reservations_CarID] DEFAULT(0),
    [Age] int NOT NULL CONSTRAINT [DF_Reservations_Age] DEFAULT(0),
    [DriverLicenseYear] int NOT NULL CONSTRAINT [DF_Reservations_DriverLicenseYear] DEFAULT(0),
    [RentalMode] nvarchar(30) NOT NULL CONSTRAINT [DF_Reservations_RentalMode] DEFAULT(N'Tự lái'),
    [DeliveryMethod] nvarchar(40) NOT NULL CONSTRAINT [DF_Reservations_DeliveryMethod] DEFAULT(N'Nhận tại điểm giao xe'),
    [Description] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_Reservations_Status] DEFAULT(N''),
    [OwnerNote] nvarchar(1000) NULL,
    [OwnerResponseDate] datetime2 NULL,
    [PickUpDate] date NOT NULL,
    [DropOffDate] date NOT NULL,
    [PickUpTime] time NOT NULL CONSTRAINT [DF_Reservations_PickUpTime] DEFAULT(CONVERT(time,'00:00:00')),
    [DropOffTime] time NOT NULL CONSTRAINT [DF_Reservations_DropOffTime] DEFAULT(CONVERT(time,'00:00:00')),
    [TotalPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_Reservations_TotalPrice] DEFAULT(0),
    [CommissionRateSnapshot] decimal(5,2) NOT NULL CONSTRAINT [DF_Reservations_CommissionRateSnapshot] DEFAULT(0),
    [PlatformFeeAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Reservations_PlatformFeeAmount] DEFAULT(0),
    [PartnerReceivableAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Reservations_PartnerReceivableAmount] DEFAULT(0),
    [DepositAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Reservations_DepositAmount] DEFAULT(0),
    [DepositStatus] nvarchar(40) NOT NULL CONSTRAINT [DF_Reservations_DepositStatus] DEFAULT(N''),
    [CancellationPolicyVersion] nvarchar(50) NOT NULL CONSTRAINT [DF_Reservations_CancellationPolicyVersion] DEFAULT(N''),
    [TermsVersion] nvarchar(50) NOT NULL CONSTRAINT [DF_Reservations_TermsVersion] DEFAULT(N''),
    [PriceSnapshotJson] nvarchar(4000) NULL,
    [CancellationFeeAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Reservations_CancellationFeeAmount] DEFAULT(0),
    [CancellationReason] nvarchar(500) NULL,
    [CancelledByAppUserID] int NULL,
    [CancelledDate] datetime2 NULL,
    [HoldExpiresAt] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Reservations_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [CompletedDate] datetime2 NULL,
    CONSTRAINT [CK_Reservations_RentalMode] CHECK ([RentalMode] IN (N'Tự lái', N'Có tài xế')),
    CONSTRAINT [CK_Reservations_DeliveryMethod] CHECK ([DeliveryMethod] IN (N'Nhận tại điểm giao xe', N'Giao xe tận nơi')),
    CONSTRAINT [PK_Reservations] PRIMARY KEY ([ReservationID])
);
GO

CREATE TABLE [dbo].[ReservationStatusHistories] (
    [ReservationStatusHistoryID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_ReservationStatusHistories_ReservationID] DEFAULT(0),
    [OldStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_ReservationStatusHistories_OldStatus] DEFAULT(N''),
    [NewStatus] nvarchar(50) NOT NULL CONSTRAINT [DF_ReservationStatusHistories_NewStatus] DEFAULT(N''),
    [ChangedByAppUserID] int NULL,
    [Note] nvarchar(1000) NULL,
    [ChangedDate] datetime2 NOT NULL CONSTRAINT [DF_ReservationStatusHistories_ChangedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_ReservationStatusHistories] PRIMARY KEY ([ReservationStatusHistoryID])
);
GO

CREATE TABLE [dbo].[Reviews] (
    [ReviewID] int IDENTITY(1,1) NOT NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_Reviews_IsDeleted] DEFAULT(0),
    [DeletedAt] datetime2 NULL,
    [DeletedByUserId] int NULL,
    [DeleteReason] nvarchar(500) NULL,
    [CustomerName] nvarchar(max) NOT NULL CONSTRAINT [DF_Reviews_CustomerName] DEFAULT(N''),
    [CustomerImage] nvarchar(max) NOT NULL CONSTRAINT [DF_Reviews_CustomerImage] DEFAULT(N''),
    [Comment] nvarchar(max) NOT NULL CONSTRAINT [DF_Reviews_Comment] DEFAULT(N''),
    [RaytingValue] int NOT NULL CONSTRAINT [DF_Reviews_RaytingValue] DEFAULT(0),
    [ReviewDate] datetime2 NOT NULL CONSTRAINT [DF_Reviews_ReviewDate] DEFAULT(SYSUTCDATETIME()),
    [CarID] int NOT NULL CONSTRAINT [DF_Reviews_CarID] DEFAULT(0),
    [AppUserID] int NULL,
    [ReservationID] int NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewID])
);
GO

CREATE TABLE [dbo].[CompanyAnnouncements] (
    [CompanyAnnouncementID] int IDENTITY(1,1) NOT NULL,
    [Title] nvarchar(180) NOT NULL CONSTRAINT [DF_CompanyAnnouncements_Title] DEFAULT(N''),
    [Content] nvarchar(4000) NOT NULL CONSTRAINT [DF_CompanyAnnouncements_Content] DEFAULT(N''),
    [AudienceRole] nvarchar(30) NOT NULL CONSTRAINT [DF_CompanyAnnouncements_AudienceRole] DEFAULT(N''),
    [IsImportant] bit NOT NULL CONSTRAINT [DF_CompanyAnnouncements_IsImportant] DEFAULT(0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_CompanyAnnouncements_IsActive] DEFAULT(0),
    [PublishDate] datetime2 NOT NULL CONSTRAINT [DF_CompanyAnnouncements_PublishDate] DEFAULT(SYSUTCDATETIME()),
    [ExpiresDate] datetime2 NULL,
    [CreatedByAppUserID] int NULL,
    CONSTRAINT [PK_CompanyAnnouncements] PRIMARY KEY ([CompanyAnnouncementID])
);
GO

CREATE TABLE [dbo].[PlatformFeeSettings] (
    [PlatformFeeSettingID] int IDENTITY(1,1) NOT NULL,
    [VehiclePartnerCommissionPercent] decimal(5,2) NOT NULL CONSTRAINT [DF_PlatformFeeSettings_VehiclePartnerCommissionPercent] DEFAULT(0),
    [Note] nvarchar(500) NULL,
    [UpdatedDate] datetime2 NOT NULL CONSTRAINT [DF_PlatformFeeSettings_UpdatedDate] DEFAULT(SYSUTCDATETIME()),
    [UpdatedByAppUserID] int NULL,
    CONSTRAINT [PK_PlatformFeeSettings] PRIMARY KEY ([PlatformFeeSettingID])
);
GO

CREATE TABLE [dbo].[CommissionTransactions] (
    [CommissionTransactionID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_CommissionTransactions_ReservationID] DEFAULT(0),
    [SettlementID] int NOT NULL,
    [PartnerVehicleID] int NOT NULL CONSTRAINT [DF_CommissionTransactions_PartnerVehicleID] DEFAULT(0),
    [PartnerAppUserID] int NOT NULL CONSTRAINT [DF_CommissionTransactions_PartnerAppUserID] DEFAULT(0),
    [GrossAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_CommissionTransactions_GrossAmount] DEFAULT(0),
    [CommissionRate] decimal(5,2) NOT NULL CONSTRAINT [DF_CommissionTransactions_CommissionRate] DEFAULT(0),
    [CommissionAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_CommissionTransactions_CommissionAmount] DEFAULT(0),
    [PartnerNetAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_CommissionTransactions_PartnerNetAmount] DEFAULT(0),
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_CommissionTransactions_Status] DEFAULT(N''),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_CommissionTransactions_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ReconciledDate] datetime2 NULL,
    [PaidDate] datetime2 NULL,
    [BankReference] nvarchar(100) NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_CommissionTransactions] PRIMARY KEY ([CommissionTransactionID])
);
GO

CREATE TABLE [dbo].[UserVerifications] (
    [UserVerificationID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_UserVerifications_AppUserID] DEFAULT(0),
    [VerificationType] nvarchar(30) NOT NULL CONSTRAINT [DF_UserVerifications_VerificationType] DEFAULT(N''),
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_UserVerifications_Status] DEFAULT(N''),
    [LegalFullName] nvarchar(120) NULL,
    [Gender] nvarchar(20) NULL,
    [CitizenIdMasked] nvarchar(50) NULL,
    [CitizenIdFingerprint] varchar(64) NULL,
    [CitizenIdIssuedDate] datetime2 NULL,
    [CitizenIdExpiryDate] datetime2 NULL,
    [CitizenIdAddress] nvarchar(500) NULL,
    [PermanentProvince] nvarchar(100) NULL,
    [PermanentWard] nvarchar(150) NULL,
    [PermanentDetail] nvarchar(300) NULL,
    [PermanentAddress] nvarchar(500) NULL,
    [CurrentAddressSameAsPermanent] bit NOT NULL CONSTRAINT [DF_UserVerifications_CurrentAddressSameAsPermanent] DEFAULT(0),
    [CurrentProvince] nvarchar(100) NULL,
    [CurrentWard] nvarchar(150) NULL,
    [CurrentDetail] nvarchar(300) NULL,
    [CurrentAddress] nvarchar(500) NULL,
    [DriverLicenseNumber] nvarchar(50) NULL,
    [DriverLicenseClass] nvarchar(20) NULL,
    [CitizenIdFrontFileID] uniqueidentifier NULL,
    [CitizenIdBackFileID] uniqueidentifier NULL,
    [DriverLicenseFileID] uniqueidentifier NULL,
    [PortraitFileID] uniqueidentifier NULL,
    [CitizenIdFrontUrl] nvarchar(500) NULL,
    [CitizenIdBackUrl] nvarchar(500) NULL,
    [DriverLicenseUrl] nvarchar(500) NULL,
    [PortraitUrl] nvarchar(500) NULL,
    [DateOfBirth] datetime2 NULL,
    [DriverLicenseIssuedDate] datetime2 NULL,
    [DriverLicenseExpiry] datetime2 NULL,
    [ReviewedByAppUserID] int NULL,
    [RejectionReason] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_UserVerifications_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ReviewedDate] datetime2 NULL,
    CONSTRAINT [PK_UserVerifications] PRIMARY KEY ([UserVerificationID])
);
GO

CREATE TABLE [dbo].[PrivateFiles] (
    [PrivateFileID] uniqueidentifier NOT NULL,
    [OwnerAppUserID] int NOT NULL,
    [ReservationID] int NULL,
    [PartnerApplicationID] int NULL,
    [Category] nvarchar(100) NOT NULL,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StoredFileName] nvarchar(255) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [FileSize] bigint NOT NULL,
    [Sha256Hash] varchar(64) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PrivateFiles_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [AttachedEntityType] nvarchar(100) NULL,
    [AttachedEntityID] nvarchar(100) NULL,
    [AttachedDate] datetime2 NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF_PrivateFiles_IsDeleted] DEFAULT(0),
    [DeleteRequestedDate] datetime2 NULL,
    [PhysicalDeletedDate] datetime2 NULL,
    [DeleteRetryCount] int NOT NULL CONSTRAINT [DF_PrivateFiles_DeleteRetryCount] DEFAULT(0),
    [LastDeleteError] nvarchar(1000) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PrivateFiles] PRIMARY KEY ([PrivateFileID])
);
GO

CREATE TABLE [dbo].[Payments] (
    [PaymentID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_Payments_ReservationID] DEFAULT(0),
    [PaymentType] nvarchar(30) NOT NULL CONSTRAINT [DF_Payments_PaymentType] DEFAULT(N''),
    [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_Payments_Amount] DEFAULT(0),
    [ProviderFeeAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Payments_ProviderFeeAmount] DEFAULT(0),
    [ProviderFeeVerified] bit NOT NULL CONSTRAINT [DF_Payments_ProviderFeeVerified] DEFAULT(0),
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_Payments_Status] DEFAULT(N''),
    [TransactionCode] nvarchar(100) NULL,
    [IdempotencyKey] nvarchar(100) NULL,
    [Provider] nvarchar(50) NULL,
    [TransferContent] nvarchar(50) NULL,
    [RelatedEntityType] nvarchar(50) NULL,
    [RelatedEntityID] int NULL,
    [CustomerReportedDate] datetime2 NULL,
    [IsSimulated] bit NOT NULL CONSTRAINT [DF_Payments_IsSimulated] DEFAULT(0),
    [VerificationNote] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Payments_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ConfirmedDate] datetime2 NULL,
    [RefundedDate] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentID]),
    CONSTRAINT [CK_Payments_ProviderFeeAmount] CHECK ([ProviderFeeAmount] >= 0 AND [ProviderFeeAmount] <= [Amount] AND ([ProviderFeeVerified] = 1 OR [ProviderFeeAmount] = 0))
);
GO

CREATE TABLE [dbo].[HandoverReports] (
    [HandoverReportID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_HandoverReports_ReservationID] DEFAULT(0),
    [ReportType] nvarchar(20) NOT NULL CONSTRAINT [DF_HandoverReports_ReportType] DEFAULT(N''),
    [OdometerKm] int NOT NULL CONSTRAINT [DF_HandoverReports_OdometerKm] DEFAULT(0),
    [FuelPercent] int NOT NULL CONSTRAINT [DF_HandoverReports_FuelPercent] DEFAULT(0),
    [ExistingDamage] nvarchar(2000) NULL,
    [Accessories] nvarchar(1000) NULL,
    [LocationText] nvarchar(500) NULL,
    [PhotoUrls] nvarchar(4000) NULL,
    [OtpHash] nvarchar(100) NULL,
    [OtpExpiresAt] datetime2 NULL,
    [ConfirmedDate] datetime2 NULL,
    [OtpFailedAttempts] int NOT NULL CONSTRAINT [DF_HandoverReports_OtpFailedAttempts] DEFAULT(0),
    [OtpLastSentAt] datetime2 NULL,
    [OtpLockedUntil] datetime2 NULL,
    [CreatedByAppUserID] int NOT NULL CONSTRAINT [DF_HandoverReports_CreatedByAppUserID] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_HandoverReports_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [IsLocked] bit NOT NULL CONSTRAINT [DF_HandoverReports_IsLocked] DEFAULT(0),
    [IsSuperseded] bit NOT NULL CONSTRAINT [DF_HandoverReports_IsSuperseded] DEFAULT(0),
    [ReplacedByReportId] int NULL,
    [CorrectionReason] nvarchar(1000) NULL,
    CONSTRAINT [PK_HandoverReports] PRIMARY KEY ([HandoverReportID])
);
GO

CREATE TABLE [dbo].[Disputes] (
    [DisputeID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_Disputes_ReservationID] DEFAULT(0),
    [CreatedByAppUserID] int NOT NULL CONSTRAINT [DF_Disputes_CreatedByAppUserID] DEFAULT(0),
    [AssignedStaffAppUserID] int NULL,
    [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Disputes_Type] DEFAULT(N''),
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_Disputes_Status] DEFAULT(N''),
    [Description] nvarchar(3000) NOT NULL CONSTRAINT [DF_Disputes_Description] DEFAULT(N''),
    [EvidenceUrls] nvarchar(2000) NULL,
    [Resolution] nvarchar(3000) NULL,
    [CompensationAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Disputes_CompensationAmount] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Disputes_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ResolvedDate] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Disputes] PRIMARY KEY ([DisputeID])
);
GO

CREATE TABLE [dbo].[AuditLogs] (
    [AuditLogID] bigint IDENTITY(1,1) NOT NULL,
    [AppUserID] int NULL,
    [Action] nvarchar(100) NOT NULL CONSTRAINT [DF_AuditLogs_Action] DEFAULT(N''),
    [EntityName] nvarchar(100) NOT NULL CONSTRAINT [DF_AuditLogs_EntityName] DEFAULT(N''),
    [EntityID] nvarchar(100) NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [Note] nvarchar(1000) NULL,
    [IpAddress] nvarchar(64) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_AuditLogs_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogID])
);
GO

CREATE TABLE [dbo].[StaffOperationalIssues] (
    [StaffOperationalIssueID] int IDENTITY(1,1) NOT NULL,
    [StaffAppUserID] int NOT NULL CONSTRAINT [DF_StaffOperationalIssues_StaffAppUserID] DEFAULT(0),
    [AdminAppUserID] int NULL,
    [CustomerAppUserID] int NULL,
    [UserVerificationID] int NULL,
    [IssueType] nvarchar(120) NOT NULL CONSTRAINT [DF_StaffOperationalIssues_IssueType] DEFAULT(N'Thu hồi kết quả duyệt hồ sơ'),
    [Severity] nvarchar(30) NOT NULL CONSTRAINT [DF_StaffOperationalIssues_Severity] DEFAULT(N'Trung bình'),
    [Reason] nvarchar(1000) NOT NULL CONSTRAINT [DF_StaffOperationalIssues_Reason] DEFAULT(N''),
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_StaffOperationalIssues_Status] DEFAULT(N'Mới'),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_StaffOperationalIssues_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ResolvedDate] datetime2 NULL,
    CONSTRAINT [PK_StaffOperationalIssues] PRIMARY KEY ([StaffOperationalIssueID])
);
GO

CREATE TABLE [dbo].[DataChangeHistories] (
    [DataChangeHistoryID] bigint IDENTITY(1,1) NOT NULL,
    [EntityName] nvarchar(100) NOT NULL CONSTRAINT [DF_DataChangeHistories_EntityName] DEFAULT(N''),
    [EntityID] nvarchar(100) NOT NULL CONSTRAINT [DF_DataChangeHistories_EntityID] DEFAULT(N''),
    [Action] nvarchar(50) NOT NULL CONSTRAINT [DF_DataChangeHistories_Action] DEFAULT(N''),
    [OldDataJson] nvarchar(max) NULL,
    [NewDataJson] nvarchar(max) NULL,
    [Reason] nvarchar(1000) NULL,
    [ChangedByAppUserID] int NOT NULL CONSTRAINT [DF_DataChangeHistories_ChangedByAppUserID] DEFAULT(0),
    [ChangedAt] datetime2 NOT NULL CONSTRAINT [DF_DataChangeHistories_ChangedAt] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_DataChangeHistories] PRIMARY KEY ([DataChangeHistoryID])
);
GO

CREATE TABLE [dbo].[DataRetentionPolicies] (
    [DataRetentionPolicyID] int IDENTITY(1,1) NOT NULL,
    [EntityName] nvarchar(100) NOT NULL CONSTRAINT [DF_DataRetentionPolicies_EntityName] DEFAULT(N''),
    [RetentionDays] int NOT NULL CONSTRAINT [DF_DataRetentionPolicies_RetentionDays] DEFAULT(0),
    [AllowHardDelete] bit NOT NULL CONSTRAINT [DF_DataRetentionPolicies_AllowHardDelete] DEFAULT(0),
    [RequireAnonymization] bit NOT NULL CONSTRAINT [DF_DataRetentionPolicies_RequireAnonymization] DEFAULT(0),
    [IsActive] bit NOT NULL CONSTRAINT [DF_DataRetentionPolicies_IsActive] DEFAULT(0),
    [LegalBasis] nvarchar(1000) NULL,
    [UpdatedAt] datetime2 NOT NULL CONSTRAINT [DF_DataRetentionPolicies_UpdatedAt] DEFAULT(SYSUTCDATETIME()),
    [UpdatedByAppUserID] int NULL,
    CONSTRAINT [PK_DataRetentionPolicies] PRIMARY KEY ([DataRetentionPolicyID])
);
GO

CREATE TABLE [dbo].[ArchivedRecords] (
    [ArchivedRecordID] bigint IDENTITY(1,1) NOT NULL,
    [EntityName] nvarchar(100) NOT NULL CONSTRAINT [DF_ArchivedRecords_EntityName] DEFAULT(N''),
    [EntityID] nvarchar(100) NOT NULL CONSTRAINT [DF_ArchivedRecords_EntityID] DEFAULT(N''),
    [DataJson] nvarchar(max) NOT NULL CONSTRAINT [DF_ArchivedRecords_DataJson] DEFAULT(N''),
    [ArchivedAt] datetime2 NOT NULL CONSTRAINT [DF_ArchivedRecords_ArchivedAt] DEFAULT(SYSUTCDATETIME()),
    [ArchivedByAppUserID] int NULL,
    CONSTRAINT [PK_ArchivedRecords] PRIMARY KEY ([ArchivedRecordID])
);
GO

CREATE TABLE [dbo].[VehicleDocuments] (
    [VehicleDocumentID] int IDENTITY(1,1) NOT NULL,
    [PartnerVehicleID] int NOT NULL CONSTRAINT [DF_VehicleDocuments_PartnerVehicleID] DEFAULT(0),
    [DocumentType] nvarchar(40) NOT NULL CONSTRAINT [DF_VehicleDocuments_DocumentType] DEFAULT(N''),
    [DocumentNumber] nvarchar(100) NOT NULL CONSTRAINT [DF_VehicleDocuments_DocumentNumber] DEFAULT(N''),
    [FileUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_VehicleDocuments_FileUrl] DEFAULT(N''),
    [IssuedDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_VehicleDocuments_Status] DEFAULT(N''),
    [ReviewedByAppUserID] int NULL,
    [ReviewedDate] datetime2 NULL,
    [RejectionReason] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_VehicleDocuments_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_VehicleDocuments] PRIMARY KEY ([VehicleDocumentID])
);
GO

CREATE TABLE [dbo].[MaintenanceRecords] (
    [MaintenanceRecordID] int IDENTITY(1,1) NOT NULL,
    [PartnerVehicleID] int NOT NULL CONSTRAINT [DF_MaintenanceRecords_PartnerVehicleID] DEFAULT(0),
    [MaintenanceDate] datetime2 NOT NULL CONSTRAINT [DF_MaintenanceRecords_MaintenanceDate] DEFAULT(SYSUTCDATETIME()),
    [OdometerKm] int NOT NULL CONSTRAINT [DF_MaintenanceRecords_OdometerKm] DEFAULT(0),
    [NextMaintenanceKm] int NULL,
    [NextMaintenanceDate] datetime2 NULL,
    [WorkPerformed] nvarchar(1000) NOT NULL CONSTRAINT [DF_MaintenanceRecords_WorkPerformed] DEFAULT(N''),
    [Garage] nvarchar(500) NULL,
    [Cost] decimal(18,2) NOT NULL CONSTRAINT [DF_MaintenanceRecords_Cost] DEFAULT(0),
    [HasUnresolvedSafetyIssue] bit NOT NULL CONSTRAINT [DF_MaintenanceRecords_HasUnresolvedSafetyIssue] DEFAULT(0),
    [SafetyIssueNote] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_MaintenanceRecords_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([MaintenanceRecordID])
);
GO

CREATE TABLE [dbo].[Incidents] (
    [IncidentID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_Incidents_ReservationID] DEFAULT(0),
    [ReportedByAppUserID] int NOT NULL CONSTRAINT [DF_Incidents_ReportedByAppUserID] DEFAULT(0),
    [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Incidents_Type] DEFAULT(N''),
    [Description] nvarchar(2000) NOT NULL CONSTRAINT [DF_Incidents_Description] DEFAULT(N''),
    [LocationText] nvarchar(500) NULL,
    [EvidenceUrls] nvarchar(4000) NULL,
    [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_Incidents_Status] DEFAULT(N''),
    [VehicleImmobilized] bit NOT NULL CONSTRAINT [DF_Incidents_VehicleImmobilized] DEFAULT(0),
    [PoliceInvolved] bit NOT NULL CONSTRAINT [DF_Incidents_PoliceInvolved] DEFAULT(0),
    [InsuranceNotified] bit NOT NULL CONSTRAINT [DF_Incidents_InsuranceNotified] DEFAULT(0),
    [EstimatedDamage] decimal(18,2) NOT NULL CONSTRAINT [DF_Incidents_EstimatedDamage] DEFAULT(0),
    [CustomerLiability] decimal(18,2) NOT NULL CONSTRAINT [DF_Incidents_CustomerLiability] DEFAULT(0),
    [OccurredAt] datetime2 NOT NULL CONSTRAINT [DF_Incidents_OccurredAt] DEFAULT(SYSUTCDATETIME()),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Incidents_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ResolvedDate] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Incidents] PRIMARY KEY ([IncidentID])
);
GO

CREATE TABLE [dbo].[TrafficFines] (
    [TrafficFineID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_TrafficFines_ReservationID] DEFAULT(0),
    [ViolationAt] datetime2 NOT NULL CONSTRAINT [DF_TrafficFines_ViolationAt] DEFAULT(SYSUTCDATETIME()),
    [Violation] nvarchar(500) NOT NULL CONSTRAINT [DF_TrafficFines_Violation] DEFAULT(N''),
    [LocationText] nvarchar(500) NULL,
    [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_TrafficFines_Amount] DEFAULT(0),
    [NoticeNumber] nvarchar(100) NULL,
    [EvidenceUrl] nvarchar(500) NULL,
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_TrafficFines_Status] DEFAULT(N''),
    [DueDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_TrafficFines_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [PaidDate] datetime2 NULL,
    CONSTRAINT [PK_TrafficFines] PRIMARY KEY ([TrafficFineID])
);
GO

CREATE TABLE [dbo].[DepositTransactions] (
    [DepositTransactionID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_DepositTransactions_ReservationID] DEFAULT(0),
    [Type] nvarchar(30) NOT NULL CONSTRAINT [DF_DepositTransactions_Type] DEFAULT(N''),
    [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_DepositTransactions_Amount] DEFAULT(0),
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_DepositTransactions_Status] DEFAULT(N''),
    [Reason] nvarchar(500) NULL,
    [TransactionCode] nvarchar(100) NULL,
    [CreatedByAppUserID] int NOT NULL CONSTRAINT [DF_DepositTransactions_CreatedByAppUserID] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_DepositTransactions_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [CompletedDate] datetime2 NULL,
    CONSTRAINT [PK_DepositTransactions] PRIMARY KEY ([DepositTransactionID])
);
GO

CREATE TABLE [dbo].[Settlements] (
    [SettlementID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_Settlements_ReservationID] DEFAULT(0),
    [GrossRental] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_GrossRental] DEFAULT(0),
    [PlatformFee] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_PlatformFee] DEFAULT(0),
    [PaymentGatewayFee] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_PaymentGatewayFee] DEFAULT(0),
    [RefundAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_RefundAmount] DEFAULT(0),
    [CompensationAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_CompensationAmount] DEFAULT(0),
    [OwnerPayout] decimal(18,2) NOT NULL CONSTRAINT [DF_Settlements_OwnerPayout] DEFAULT(0),
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_Settlements_Status] DEFAULT(N''),
    [CreationIdempotencyKey] nvarchar(100) NULL,
    [PayoutIdempotencyKey] nvarchar(100) NULL,
    [PayoutTransactionCode] nvarchar(100) NULL,
    [CreatedByAppUserID] int NULL,
    [ApprovedByAppUserID] int NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Settlements_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [PaidDate] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Settlements] PRIMARY KEY ([SettlementID])
);
GO

CREATE TABLE [dbo].[EmailOutboxes] (
    [EmailOutboxID] bigint IDENTITY(1,1) NOT NULL,
    [MessageKey] nvarchar(100) NULL,
    [RecipientEmail] nvarchar(256) NOT NULL,
    [Subject] nvarchar(500) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_EmailOutboxes_Status] DEFAULT(N'Pending'),
    [RetryCount] int NOT NULL CONSTRAINT [DF_EmailOutboxes_RetryCount] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_EmailOutboxes_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [NextAttemptAt] datetime2 NULL,
    [LastAttemptAt] datetime2 NULL,
    [SentDate] datetime2 NULL,
    [LockedBy] nvarchar(100) NULL,
    [LockedUntil] datetime2 NULL,
    [LastError] nvarchar(2000) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_EmailOutboxes] PRIMARY KEY ([EmailOutboxID])
);
GO

CREATE TABLE [dbo].[PublicFileDeletionJobs] (
    [PublicFileDeletionJobID] bigint IDENTITY(1,1) NOT NULL,
    [FileUrl] nvarchar(1000) NOT NULL,
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_PublicFileDeletionJobs_Status] DEFAULT(N'Pending'),
    [RetryCount] int NOT NULL CONSTRAINT [DF_PublicFileDeletionJobs_RetryCount] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_PublicFileDeletionJobs_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [NextAttemptAt] datetime2 NULL,
    [LastAttemptAt] datetime2 NULL,
    [DeletedDate] datetime2 NULL,
    [LockedBy] nvarchar(100) NULL,
    [LockedUntil] datetime2 NULL,
    [LastError] nvarchar(2000) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_PublicFileDeletionJobs] PRIMARY KEY ([PublicFileDeletionJobID])
);
GO

CREATE TABLE [dbo].[Notifications] (
    [NotificationID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NOT NULL CONSTRAINT [DF_Notifications_AppUserID] DEFAULT(0),
    [Title] nvarchar(150) NOT NULL CONSTRAINT [DF_Notifications_Title] DEFAULT(N''),
    [Message] nvarchar(2000) NOT NULL CONSTRAINT [DF_Notifications_Message] DEFAULT(N''),
    [Type] nvarchar(50) NOT NULL CONSTRAINT [DF_Notifications_Type] DEFAULT(N''),
    [Link] nvarchar(500) NULL,
    [IsRead] bit NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT(0),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_Notifications_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ReadDate] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationID])
);
GO

CREATE TABLE [dbo].[HandoverImages] (
    [HandoverImageID] int IDENTITY(1,1) NOT NULL,
    [HandoverReportID] int NOT NULL CONSTRAINT [DF_HandoverImages_HandoverReportID] DEFAULT(0),
    [FileUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_HandoverImages_FileUrl] DEFAULT(N''),
    [ImageType] nvarchar(50) NOT NULL CONSTRAINT [DF_HandoverImages_ImageType] DEFAULT(N''),
    [FileHash] nvarchar(128) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_HandoverImages_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_HandoverImages] PRIMARY KEY ([HandoverImageID])
);
GO

CREATE TABLE [dbo].[DisputeMessages] (
    [DisputeMessageID] int IDENTITY(1,1) NOT NULL,
    [DisputeID] int NOT NULL CONSTRAINT [DF_DisputeMessages_DisputeID] DEFAULT(0),
    [SenderAppUserID] int NOT NULL CONSTRAINT [DF_DisputeMessages_SenderAppUserID] DEFAULT(0),
    [Message] nvarchar(2000) NOT NULL CONSTRAINT [DF_DisputeMessages_Message] DEFAULT(N''),
    [EvidenceUrls] nvarchar(4000) NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_DisputeMessages_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT [PK_DisputeMessages] PRIMARY KEY ([DisputeMessageID])
);
GO

CREATE TABLE [dbo].[AdditionalCharges] (
    [AdditionalChargeID] int IDENTITY(1,1) NOT NULL,
    [ReservationID] int NOT NULL CONSTRAINT [DF_AdditionalCharges_ReservationID] DEFAULT(0),
    [ChargeType] nvarchar(50) NOT NULL CONSTRAINT [DF_AdditionalCharges_ChargeType] DEFAULT(N''),
    [Amount] decimal(18,2) NOT NULL CONSTRAINT [DF_AdditionalCharges_Amount] DEFAULT(0),
    [Reason] nvarchar(1000) NOT NULL CONSTRAINT [DF_AdditionalCharges_Reason] DEFAULT(N''),
    [EvidenceUrls] nvarchar(4000) NULL,
    [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_AdditionalCharges_Status] DEFAULT(N''),
    [CreatedByAppUserID] int NOT NULL CONSTRAINT [DF_AdditionalCharges_CreatedByAppUserID] DEFAULT(0),
    [PaymentID] int NULL,
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_AdditionalCharges_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ResolvedDate] datetime2 NULL,
    CONSTRAINT [PK_AdditionalCharges] PRIMARY KEY ([AdditionalChargeID])
);
GO

CREATE TABLE [dbo].[FraudFlags] (
    [FraudFlagID] int IDENTITY(1,1) NOT NULL,
    [AppUserID] int NULL,
    [ReservationID] int NULL,
    [RuleCode] nvarchar(80) NOT NULL CONSTRAINT [DF_FraudFlags_RuleCode] DEFAULT(N''),
    [Description] nvarchar(1000) NOT NULL CONSTRAINT [DF_FraudFlags_Description] DEFAULT(N''),
    [RiskScore] int NOT NULL CONSTRAINT [DF_FraudFlags_RiskScore] DEFAULT(0),
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_FraudFlags_Status] DEFAULT(N''),
    [CreatedDate] datetime2 NOT NULL CONSTRAINT [DF_FraudFlags_CreatedDate] DEFAULT(SYSUTCDATETIME()),
    [ReviewedByAppUserID] int NULL,
    [ReviewedDate] datetime2 NULL,
    CONSTRAINT [PK_FraudFlags] PRIMARY KEY ([FraudFlagID])
);
GO

CREATE TABLE [dbo].[WorkItemClaims] (
    [WorkItemClaimID] int IDENTITY(1,1) NOT NULL,
    [QueueType] nvarchar(50) NOT NULL CONSTRAINT [DF_WorkItemClaims_QueueType] DEFAULT(N''),
    [EntityID] int NOT NULL CONSTRAINT [DF_WorkItemClaims_EntityID] DEFAULT(0),
    [AssignedStaffAppUserID] int NOT NULL CONSTRAINT [DF_WorkItemClaims_AssignedStaffAppUserID] DEFAULT(0),
    [AssignedAt] datetime2 NOT NULL CONSTRAINT [DF_WorkItemClaims_AssignedAt] DEFAULT(SYSUTCDATETIME()),
    [DueAt] datetime2 NULL,
    [Status] nvarchar(30) NOT NULL CONSTRAINT [DF_WorkItemClaims_Status] DEFAULT(N''),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_WorkItemClaims] PRIMARY KEY ([WorkItemClaimID])
);
GO

CREATE UNIQUE INDEX [UX_SystemVersions_IsCurrent] ON [dbo].[SystemVersions]([IsCurrent]) WHERE [IsCurrent] = 1;
GO

CREATE INDEX [IX_AppUsers_Email] ON [dbo].[AppUsers]([Email]);
GO
CREATE INDEX [IX_AppUsers_Phone] ON [dbo].[AppUsers]([Phone]) WHERE [Phone] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_AppUsers_Username] ON [dbo].[AppUsers]([Username]);
GO
CREATE INDEX [IX_EmailVerificationOtps_AppUserID_Purpose_UsedDate_ExpiresDate] ON [dbo].[EmailVerificationOtps]([AppUserID], [Purpose], [UsedDate], [ExpiresDate]);
GO
CREATE UNIQUE INDEX [IX_PasswordResetTokens_TokenHash] ON [dbo].[PasswordResetTokens]([TokenHash]);
GO
CREATE INDEX [IX_Reservations_CarID_PickUpDate_DropOffDate] ON [dbo].[Reservations]([CarID], [PickUpDate], [DropOffDate]);
GO
CREATE INDEX [IX_Reservations_PartnerVehicleID_Status] ON [dbo].[Reservations]([PartnerVehicleID], [Status]);
GO
CREATE INDEX [IX_ReservationStatusHistories_ReservationID] ON [dbo].[ReservationStatusHistories]([ReservationID]);
GO
CREATE INDEX [IX_CompanyAnnouncements_AudienceRole_IsActive_PublishDate] ON [dbo].[CompanyAnnouncements]([AudienceRole], [IsActive], [PublishDate]);
GO
CREATE UNIQUE INDEX [IX_VehiclePartnerProfiles_AppUserID] ON [dbo].[VehiclePartnerProfiles]([AppUserID]);
CREATE INDEX [IX_VehiclePartnerProfiles_CitizenIdentityNumber] ON [dbo].[VehiclePartnerProfiles]([CitizenIdentityNumber]);
CREATE INDEX [IX_VehiclePartnerProfiles_TaxCode] ON [dbo].[VehiclePartnerProfiles]([TaxCode]);
CREATE INDEX [IX_VehiclePartnerProfiles_CitizenIdFingerprint] ON [dbo].[VehiclePartnerProfiles]([CitizenIdFingerprint]) WHERE [CitizenIdFingerprint] IS NOT NULL;
CREATE INDEX [IX_UserVerifications_CitizenIdFingerprint] ON [dbo].[UserVerifications]([CitizenIdFingerprint]) WHERE [CitizenIdFingerprint] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_VehiclePartnerApplications_LicensePlate] ON [dbo].[VehiclePartnerApplications]([LicensePlate]) WHERE [Status] <> N'Từ chối';
GO
CREATE UNIQUE INDEX [IX_PartnerVehicles_CarID] ON [dbo].[PartnerVehicles]([CarID]);
GO
CREATE UNIQUE INDEX [IX_PartnerVehicles_VehiclePartnerApplicationID] ON [dbo].[PartnerVehicles]([VehiclePartnerApplicationID]);
GO
CREATE UNIQUE INDEX [IX_CommissionTransactions_ReservationID] ON [dbo].[CommissionTransactions]([ReservationID]);
GO
CREATE UNIQUE INDEX [IX_CommissionTransactions_SettlementID] ON [dbo].[CommissionTransactions]([SettlementID]);
GO
CREATE UNIQUE INDEX [IX_UserVerifications_AppUserID_VerificationType] ON [dbo].[UserVerifications]([AppUserID], [VerificationType]);
GO
CREATE UNIQUE INDEX [IX_Payments_TransactionCode] ON [dbo].[Payments]([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;
GO
CREATE INDEX [IX_Payments_ReservationID_Status] ON [dbo].[Payments]([ReservationID], [Status]);
GO
CREATE UNIQUE INDEX [IX_Payments_IdempotencyKey] ON [dbo].[Payments]([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_Payments_ReservationID_PaymentType] ON [dbo].[Payments]([ReservationID], [PaymentType]) WHERE [Status] = N'Thành công' AND [PaymentType] IN (N'Tiền cọc', N'Tiền thuê');
GO
CREATE UNIQUE INDEX [IX_Payments_RelatedEntity] ON [dbo].[Payments]([RelatedEntityType], [RelatedEntityID])
WHERE [Status] = N'Thành công' AND [RelatedEntityType] IS NOT NULL AND [RelatedEntityID] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_HandoverReports_ReservationID_ReportType] ON [dbo].[HandoverReports]([ReservationID], [ReportType]) WHERE [IsSuperseded] = 0;
GO
CREATE INDEX [IX_Disputes_ReservationID_Status] ON [dbo].[Disputes]([ReservationID], [Status]);
GO
CREATE INDEX [IX_AuditLogs_EntityName_EntityID_CreatedDate] ON [dbo].[AuditLogs]([EntityName], [EntityID], [CreatedDate]);
GO
CREATE INDEX [IX_DataChangeHistories_EntityName_EntityID_ChangedAt] ON [dbo].[DataChangeHistories]([EntityName], [EntityID], [ChangedAt]);
GO
CREATE UNIQUE INDEX [IX_DataRetentionPolicies_EntityName] ON [dbo].[DataRetentionPolicies]([EntityName]);
GO
CREATE INDEX [IX_ArchivedRecords_EntityName_EntityID_ArchivedAt] ON [dbo].[ArchivedRecords]([EntityName], [EntityID], [ArchivedAt]);
GO
CREATE INDEX [IX_VehicleDocuments_PartnerVehicleID_DocumentType] ON [dbo].[VehicleDocuments]([PartnerVehicleID], [DocumentType]);
GO
CREATE INDEX [IX_MaintenanceRecords_PartnerVehicleID_MaintenanceDate] ON [dbo].[MaintenanceRecords]([PartnerVehicleID], [MaintenanceDate]);
GO
CREATE INDEX [IX_Incidents_ReservationID_Status] ON [dbo].[Incidents]([ReservationID], [Status]);
GO
CREATE UNIQUE INDEX [IX_TrafficFines_NoticeNumber] ON [dbo].[TrafficFines]([NoticeNumber]) WHERE [NoticeNumber] IS NOT NULL;
GO
CREATE INDEX [IX_DepositTransactions_ReservationID_Status] ON [dbo].[DepositTransactions]([ReservationID], [Status]);
GO
CREATE UNIQUE INDEX [IX_DepositTransactions_TransactionCode] ON [dbo].[DepositTransactions]([TransactionCode]) WHERE [TransactionCode] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_DepositTransactions_ReservationID_Type] ON [dbo].[DepositTransactions]([ReservationID], [Type]) WHERE [Status] = N'Hoàn thành';
GO
CREATE UNIQUE INDEX [IX_Settlements_ReservationID] ON [dbo].[Settlements]([ReservationID]);
GO
CREATE UNIQUE INDEX [IX_Settlements_PayoutTransactionCode] ON [dbo].[Settlements]([PayoutTransactionCode]) WHERE [PayoutTransactionCode] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_Settlements_CreationIdempotencyKey] ON [dbo].[Settlements]([CreationIdempotencyKey]) WHERE [CreationIdempotencyKey] IS NOT NULL;
GO
CREATE UNIQUE INDEX [IX_Settlements_PayoutIdempotencyKey] ON [dbo].[Settlements]([PayoutIdempotencyKey]) WHERE [PayoutIdempotencyKey] IS NOT NULL;
GO
CREATE INDEX [IX_EmailOutboxes_Status_NextAttemptAt_LockedUntil_CreatedDate] ON [dbo].[EmailOutboxes]([Status], [NextAttemptAt], [LockedUntil], [CreatedDate]);
GO
CREATE UNIQUE INDEX [IX_EmailOutboxes_MessageKey] ON [dbo].[EmailOutboxes]([MessageKey]) WHERE [MessageKey] IS NOT NULL;
GO
CREATE INDEX [IX_PublicFileDeletionJobs_Status_NextAttemptAt_LockedUntil_CreatedDate]
ON [dbo].[PublicFileDeletionJobs]([Status], [NextAttemptAt], [LockedUntil], [CreatedDate]);
GO
CREATE UNIQUE INDEX [IX_PublicFileDeletionJobs_FileUrl] ON [dbo].[PublicFileDeletionJobs]([FileUrl]);
GO
CREATE INDEX [IX_Notifications_AppUserID_IsRead_CreatedDate] ON [dbo].[Notifications]([AppUserID], [IsRead], [CreatedDate]);
GO
CREATE INDEX [IX_HandoverImages_HandoverReportID] ON [dbo].[HandoverImages]([HandoverReportID]);
GO
CREATE INDEX [IX_DisputeMessages_DisputeID_CreatedDate] ON [dbo].[DisputeMessages]([DisputeID], [CreatedDate]);
GO
CREATE INDEX [IX_AdditionalCharges_ReservationID_Status] ON [dbo].[AdditionalCharges]([ReservationID], [Status]);
GO
CREATE UNIQUE INDEX [IX_AdditionalCharges_PaymentID] ON [dbo].[AdditionalCharges]([PaymentID]) WHERE [PaymentID] IS NOT NULL;
GO
CREATE INDEX [IX_FraudFlags_Status_RiskScore] ON [dbo].[FraudFlags]([Status], [RiskScore]);
GO
CREATE UNIQUE INDEX [IX_WorkItemClaims_QueueType_EntityID] ON [dbo].[WorkItemClaims]([QueueType], [EntityID]);
GO
CREATE INDEX [IX_WorkItemClaims_AssignedStaffAppUserID_Status] ON [dbo].[WorkItemClaims]([AssignedStaffAppUserID], [Status]);
GO
CREATE INDEX [IX_StaffOperationalIssues_StaffAppUserID_Status_CreatedDate] ON [dbo].[StaffOperationalIssues]([StaffAppUserID], [Status], [CreatedDate]);
GO
CREATE UNIQUE INDEX [IX_Reviews_ReservationID] ON [dbo].[Reviews]([ReservationID]) WHERE [ReservationID] IS NOT NULL AND [IsDeleted] = 0;
GO

/* Seed dữ liệu nền đầy đủ để giao diện trang chủ không bị trống/lộn xộn */
SET IDENTITY_INSERT [dbo].[AppRoles] ON;
INSERT INTO [dbo].[SystemVersions] ([ApplicationVersion],[DatabaseVersion],[ReleasedDate],[IsCurrent],[Notes])
VALUES (N'30.9',N'30.9',SYSUTCDATETIME(),1,N'v30.9: thu đủ tiền thuê trước giao xe; Settlement theo tiền thực thu; khóa chuyển trạng thái/chi trả đi vòng; Outbox DeliveryUnknown và xóa ảnh công khai có retry.');
GO

INSERT INTO [dbo].[AppRoles] ([AppRoleId], [AppRoleName]) VALUES
(1, N'Admin'), (2, N'Staff'), (3, N'Customer'), (4, N'VehiclePartner'); -- Chuỗi kỹ thuật dùng cho phân quyền trong code, không đổi sang tiếng Việt để tránh lỗi [Authorize(Roles=...)]
SET IDENTITY_INSERT [dbo].[AppRoles] OFF;
GO

SET IDENTITY_INSERT [dbo].[AppUsers] ON;
INSERT INTO [dbo].[AppUsers] ([AppUserId],[Username],[Password],[Name],[Surname],[Email],[Phone],[IsVehiclePartner],[FailedLoginCount],[EmailConfirmed],[TokenVersion],[IsDeleted],[IsActive],[AppRoleId]) VALUES
(1,N'quantri',N'PBKDF2-SHA256$120000$+iz/0agLHmtX2LtBzlnP4Q==$rV0X0k4Sb3Lk3RJPrNXLsFdVb2OzocxC7ncL/miET1I=',N'Trị',N'Quản',N'quantri@smartcar.local',N'0900000001',0,0,1,0,0,1,1),
(2,N'nhanvien',N'PBKDF2-SHA256$120000$0wuxcistq87Ecoe5xBPi0Q==$lCfm+qvkxaiDjS921flx2udPbhgsLb3Y7tyukl+SdvQ=',N'Viên',N'Nhân',N'nhanvien@smartcar.local',N'0900000002',0,0,1,0,0,1,2),
(3,N'doitac',N'PBKDF2-SHA256$120000$OGsnRH3WKvllseB4D/jp2A==$cnKBsz86jT0OQx8P3HP/JM+dqA0G9PxHIF0qVCJS2kk=',N'Tác',N'Đối',N'doitac@smartcar.local',N'0900000003',1,0,1,0,0,1,4),
(4,N'khachhang',N'PBKDF2-SHA256$120000$jzUXLvFuR3jQC5Pza4Snww==$V22/M5qZOnsZfTM4iaXbdiyn+PxGJg2ufg2k3J3CUgE=',N'Hàng Demo',N'Khách',N'khachhang@smartcar.local',N'0900000004',0,0,1,0,0,1,3);
SET IDENTITY_INSERT [dbo].[AppUsers] OFF;
GO

INSERT INTO [dbo].[PlatformFeeSettings] ([VehiclePartnerCommissionPercent],[Note],[UpdatedDate]) VALUES
(20.00, N'Mặc định: SmartCar thu 20% hoa hồng đối tác.', SYSUTCDATETIME());
GO

INSERT INTO [dbo].[DataRetentionPolicies] ([EntityName],[RetentionDays],[AllowHardDelete],[RequireAnonymization],[IsActive],[LegalBasis],[UpdatedAt]) VALUES
(N'AppUser', 3650, 0, 1, 1, N'Lưu phục vụ hợp đồng, tranh chấp và kiểm toán.', SYSUTCDATETIME()),
(N'Reservation', 3650, 0, 0, 1, N'Lưu hồ sơ chuyến thuê và đối soát.', SYSUTCDATETIME()),
(N'Payment', 3650, 0, 0, 1, N'Lưu lịch sử thanh toán.', SYSUTCDATETIME());
GO

SET IDENTITY_INSERT [dbo].[Locations] ON;
INSERT INTO [dbo].[Locations]
([LocationID],[Name],[ProvinceCity],[District],[Ward],[AddressDetail],[Latitude],[Longitude],[SearchRadiusKm],[IsActive]) VALUES
(1,N'Điểm giao xe Cầu Giấy',N'Hà Nội',N'Cầu Giấy',N'',N'Khu vực Công viên Cầu Giấy',21.0285110,105.7909930,20,1),
(2,N'Điểm giao xe trung tâm Ninh Bình',N'Ninh Bình',N'Trung tâm Ninh Bình',N'',N'Khu vực trung tâm thành phố Ninh Bình',20.2506140,105.9744540,20,1),
(3,N'Điểm giao xe Bến Thành',N'TP Hồ Chí Minh',N'Quận 1',N'',N'Khu vực Chợ Bến Thành',10.7721550,106.6982780,20,1),
(4,N'Điểm giao xe Cầu Rồng',N'Đà Nẵng',N'Hải Châu',N'',N'Khu vực đầu Cầu Rồng phía trung tâm',16.0610990,108.2273150,20,1),
(5,N'Điểm giao xe Mỹ Đình',N'Hà Nội',N'Nam Từ Liêm',N'',N'Khu vực Bến xe Mỹ Đình',21.0282100,105.7783000,15,1),
(6,N'Điểm giao xe Nội Bài',N'Hà Nội',N'Sóc Sơn',N'',N'Khu vực Sân bay Nội Bài',21.2187149,105.8041709,30,1),
(7,N'Điểm giao xe Hoàn Kiếm',N'Hà Nội',N'Hoàn Kiếm',N'',N'Khu vực Hồ Hoàn Kiếm',21.0286669,105.8521484,15,1),
(8,N'Điểm giao xe Tràng An',N'Ninh Bình',N'Khu du lịch Tràng An',N'',N'Khu vực bến thuyền Tràng An',20.2520540,105.9178060,20,1),
(9,N'Điểm giao xe Bình Thạnh',N'TP Hồ Chí Minh',N'Bình Thạnh',N'',N'Khu vực trung tâm Bình Thạnh',10.8032380,106.7075480,20,1),
(10,N'Điểm giao xe Sơn Trà',N'Đà Nẵng',N'Sơn Trà',N'',N'Khu vực bờ đông Sông Hàn',16.0677800,108.2426400,20,1);
SET IDENTITY_INSERT [dbo].[Locations] OFF;
GO

/* Quan trọng: code đang lọc đúng chuỗi "Theo ngày", "Theo tuần", "Theo tháng" */
SET IDENTITY_INSERT [dbo].[Pricings] ON;
INSERT INTO [dbo].[Pricings] ([PricingID],[Name]) VALUES
(1,N'Theo ngày'), (2,N'Theo tuần'), (3,N'Theo tháng'), (4,N'Theo giờ'), (5,N'Cuối tuần');
SET IDENTITY_INSERT [dbo].[Pricings] OFF;
GO

SET IDENTITY_INSERT [dbo].[Banners] ON;
INSERT INTO [dbo].[Banners] ([BannerID],[Title],[Description],[VideoDescription],[VideoUrl]) VALUES
(1,N'Thuê xe tự lái dễ dàng cùng Smart Car',N'Tìm xe phù hợp, gửi yêu cầu đặt xe, xác minh hồ sơ và nhận xe chỉ trong vài bước.',N'Xem giới thiệu Smart Car',N'https://www.youtube.com/');
SET IDENTITY_INSERT [dbo].[Banners] OFF;
GO

SET IDENTITY_INSERT [dbo].[Abouts] ON;
INSERT INTO [dbo].[Abouts] ([AboutID],[Title],[Description],[ImageUrl]) VALUES
(1,N'Về Smart Car',N'Smart Car là nền tảng trung gian kết nối khách thuê xe với chủ xe/đối tác. Hệ thống hỗ trợ xác minh hồ sơ, đặt xe, thanh toán, giao nhận xe, phụ phí và đối soát.',N'/carbook-master/images/about.jpg');
SET IDENTITY_INSERT [dbo].[Abouts] OFF;
GO

SET IDENTITY_INSERT [dbo].[FooterAddresses] ON;
INSERT INTO [dbo].[FooterAddresses] ([FooterAddressID],[Description],[Address],[Phone],[Email]) VALUES
(1,N'Dịch vụ thuê xe Smart Car - đồ án mô phỏng quy trình thuê xe tự lái.',N'Hà Nội, Việt Nam',N'0900000000',N'support@smartcar.local');
SET IDENTITY_INSERT [dbo].[FooterAddresses] OFF;
GO

SET IDENTITY_INSERT [dbo].[Services] ON;
INSERT INTO [dbo].[Services] ([ServiceID],[Title],[Description],[IconUrl]) VALUES
(1,N'Đặt xe nhanh',N'Khách tìm xe theo địa điểm, thời gian, số chỗ, hộp số, nhiên liệu và khoảng giá.',N'flaticon-route'),
(2,N'Hồ sơ rõ ràng',N'Khách cần xác minh email, CCCD và giấy phép lái xe trước khi gửi yêu cầu đặt xe.',N'flaticon-handshake'),
(3,N'Giao nhận có biên bản',N'Biên bản giao/trả xe có ảnh, thông tin kilomet, nhiên liệu, phụ kiện và xác nhận hai bên.',N'flaticon-rent');
SET IDENTITY_INSERT [dbo].[Services] OFF;
GO

SET IDENTITY_INSERT [dbo].[SocialMedias] ON;
INSERT INTO [dbo].[SocialMedias] ([SocialMediaID],[Name],[Url],[Icon]) VALUES
(1,N'Facebook',N'https://facebook.com',N'icon-facebook'),
(2,N'Youtube',N'https://youtube.com',N'icon-youtube'),
(3,N'Twitter',N'https://twitter.com',N'icon-twitter');
SET IDENTITY_INSERT [dbo].[SocialMedias] OFF;
GO

SET IDENTITY_INSERT [dbo].[Testimonials] ON;
INSERT INTO [dbo].[Testimonials] ([TestimonialID],[Name],[Title],[Comment],[ImageUrl]) VALUES
(1,N'Nguyễn Văn A',N'Khách thuê',N'Quy trình tìm xe và đặt xe rõ ràng, dễ thao tác.',N'/carbook-master/images/person_1.jpg'),
(2,N'Trần Thị B',N'Chủ xe',N'Hệ thống giúp quản lý xe, lịch trống và đơn thuê thuận tiện.',N'/carbook-master/images/person_2.jpg'),
(3,N'Lê Văn C',N'Khách thuê',N'Tôi thích phần biên bản giao xe có ảnh và xác nhận OTP.',N'/carbook-master/images/person_3.jpg');
SET IDENTITY_INSERT [dbo].[Testimonials] OFF;
GO

SET IDENTITY_INSERT [dbo].[Brands] ON;
INSERT INTO [dbo].[Brands] ([BrandID],[Name]) VALUES
(1,N'Toyota'), (2,N'Hyundai'), (3,N'Kia'), (4,N'Mazda'), (5,N'VinFast');
SET IDENTITY_INSERT [dbo].[Brands] OFF;
GO

SET IDENTITY_INSERT [dbo].[Cars] ON;
INSERT INTO [dbo].[Cars] ([CarID],[IsDeleted],[DeletedAt],[DeletedByUserId],[DeleteReason],[LifecycleStatus],[BrandID],[Model],[CoverImageUrl],[Km],[Transmission],[Seat],[Luggage],[Fuel],[BigImageUrl]) VALUES
(1,0,NULL,NULL,NULL,N'Đang hoạt động',1,N'Toyota Vios 2022',N'/carbook-master/images/car-1.jpg',18000,N'Tự động',5,2,N'Xăng',N'/carbook-master/images/car-1.jpg'),
(2,0,NULL,NULL,NULL,N'Đang hoạt động',2,N'Hyundai Accent 2021',N'/carbook-master/images/car-2.jpg',22000,N'Tự động',5,2,N'Xăng',N'/carbook-master/images/car-2.jpg'),
(3,0,NULL,NULL,NULL,N'Đang hoạt động',3,N'Kia Seltos 2023',N'/carbook-master/images/car-3.jpg',9000,N'Tự động',5,3,N'Xăng',N'/carbook-master/images/car-3.jpg'),
(4,0,NULL,NULL,NULL,N'Đang hoạt động',4,N'Mazda CX-5 2022',N'/carbook-master/images/car-4.jpg',15000,N'Tự động',5,4,N'Xăng',N'/carbook-master/images/car-4.jpg'),
(5,0,NULL,NULL,NULL,N'Đang hoạt động',5,N'VinFast VF e34',N'/carbook-master/images/car-5.jpg',12000,N'Tự động',5,3,N'Điện',N'/carbook-master/images/car-5.jpg'),
(6,0,NULL,NULL,NULL,N'Đang hoạt động',1,N'Toyota Fortuner 2021',N'/carbook-master/images/car-6.jpg',30000,N'Tự động',7,4,N'Dầu',N'/carbook-master/images/car-6.jpg');
SET IDENTITY_INSERT [dbo].[Cars] OFF;
GO

SET IDENTITY_INSERT [dbo].[Features] ON;
INSERT INTO [dbo].[Features] ([FeatureID],[Name]) VALUES
(1,N'Camera lùi'),(2,N'Bluetooth'),(3,N'Điều hòa'),(4,N'GPS'),(5,N'Camera hành trình'),(6,N'Cảm biến lùi'),(7,N'Ghế trẻ em'),(8,N'Lốp dự phòng');
SET IDENTITY_INSERT [dbo].[Features] OFF;
GO

INSERT INTO [dbo].[CarFeatures] ([CarID],[FeatureID],[Available]) VALUES
(1,1,1),(1,2,1),(1,3,1),(1,8,1),
(2,1,1),(2,2,1),(2,3,1),(2,6,1),
(3,1,1),(3,2,1),(3,3,1),(3,4,1),(3,5,1),
(4,1,1),(4,2,1),(4,3,1),(4,4,1),(4,5,1),(4,6,1),
(5,1,1),(5,2,1),(5,3,1),(5,4,1),
(6,1,1),(6,2,1),(6,3,1),(6,8,1);
GO

INSERT INTO [dbo].[CarDescriptions] ([CarID],[Details]) VALUES
(1,N'Xe 5 chỗ tiết kiệm nhiên liệu, phù hợp đi trong thành phố và công tác ngắn ngày.'),
(2,N'Sedan 5 chỗ dễ lái, khoang hành lý vừa đủ, phù hợp gia đình nhỏ.'),
(3,N'Gầm cao đô thị, tiện nghi, phù hợp đi chơi cuối tuần.'),
(4,N'SUV 5 chỗ rộng rãi, vận hành ổn định, phù hợp đi xa.'),
(5,N'Xe điện vận hành êm, phù hợp di chuyển nội đô.'),
(6,N'SUV 7 chỗ mạnh mẽ, phù hợp gia đình hoặc nhóm đi đường dài.');
GO

INSERT INTO [dbo].[CarPricings] ([CarID],[PricingID],[Amount]) VALUES
(1,1,700000),(1,2,4200000),(1,3,15000000),(1,4,120000),
(2,1,650000),(2,2,3900000),(2,3,14000000),(2,4,110000),
(3,1,900000),(3,2,5400000),(3,3,19500000),(3,4,150000),
(4,1,1100000),(4,2,6600000),(4,3,24000000),(4,4,180000),
(5,1,850000),(5,2,5100000),(5,3,18000000),(5,4,140000),
(6,1,1300000),(6,2,7800000),(6,3,28000000),(6,4,220000);
GO

INSERT INTO [dbo].[RentACars] ([LocationID],[CarID],[Available]) VALUES
(1,1,1),(1,2,1),(1,3,1),(1,4,1),(1,5,1),
(2,1,1),(2,6,1),
(3,2,1),(3,3,1),(3,4,1),
(4,4,1),(4,5,1);
GO

/* Để xe hiển thị ở trang chủ, repository yêu cầu có PartnerVehicles active và VehiclePartnerApplication Status = 'Đã duyệt' */
SET IDENTITY_INSERT [dbo].[VehiclePartnerApplications] ON;
INSERT INTO [dbo].[VehiclePartnerApplications]
([VehiclePartnerApplicationID],[AppUserID],[OwnerFullName],[Email],[Phone],[Address],[CitizenIdentityNumber],[BankName],[BankAccountNumber],[BankAccountHolder],
 [BrandName],[Model],[ManufactureYear],[LicensePlate],[Color],[Transmission],[Fuel],[Seat],[Km],[LocationID],[ProposedDailyPrice],[ProposedDepositAmount],
 [RentalMode],[DeliveryMethod],[DeliveryAddress],[KmLimitPerDay],[ExtraKmFee],[DeliveryFee],[Amenities],[Accessories],[RentalConditions],[CancellationPolicy],
 [VehicleImageUrl],[FrontImageUrl],[RearImageUrl],[LeftImageUrl],[RightImageUrl],[InteriorImageUrl],[DashboardImageUrl],[RegistrationImageUrl],[InspectionImageUrl],[InsuranceImageUrl],
 [Status],[AdminNote],[CreatedDate],[ReviewedDate],[ApprovedCarID])
VALUES
(1,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Hà Nội',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'Toyota',N'Toyota Vios 2022',2022,N'30A-111.11',N'Trắng',N'Tự động',N'Xăng',5,18000,1,700000,3000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Hà Nội',300,5000,0,N'Bluetooth, Điều hòa',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'/carbook-master/images/car-1.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),1),
(2,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Hà Nội',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'Hyundai',N'Hyundai Accent 2021',2021,N'30A-222.22',N'Đen',N'Tự động',N'Xăng',5,22000,1,650000,3000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Hà Nội',300,5000,0,N'Bluetooth, Camera lùi',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'/carbook-master/images/car-2.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),2),
(3,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Hà Nội',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'Kia',N'Kia Seltos 2023',2023,N'30A-333.33',N'Đỏ',N'Tự động',N'Xăng',5,9000,1,900000,4000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Hà Nội',300,6000,0,N'GPS, Camera hành trình',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'/carbook-master/images/car-3.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),3),
(4,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Hà Nội',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'Mazda',N'Mazda CX-5 2022',2022,N'30A-444.44',N'Xám',N'Tự động',N'Xăng',5,15000,1,1100000,5000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Hà Nội',300,7000,0,N'GPS, Camera hành trình',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'/carbook-master/images/car-4.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),4),
(5,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Hà Nội',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'VinFast',N'VinFast VF e34',2022,N'30A-555.55',N'Xanh',N'Tự động',N'Điện',5,12000,1,850000,4000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Hà Nội',300,6000,0,N'GPS, Camera lùi',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'/carbook-master/images/car-5.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),5),
(6,3,N'Nguyễn Văn Đối Tác',N'doitac@smartcar.local',N'0900000003',N'Ninh Bình',N'001234567890',N'VCB',N'0123456789',N'NGUYEN VAN DOI TAC',N'Toyota',N'Toyota Fortuner 2021',2021,N'35A-666.66',N'Bạc',N'Tự động',N'Dầu',7,30000,2,1300000,6000000,N'Tự lái',N'Nhận tại điểm giao xe',N'Ninh Bình',300,8000,0,N'Camera lùi, Điều hòa',N'Lốp dự phòng',N'Khách có GPLX hợp lệ.',N'Hủy trước 24h.',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'/carbook-master/images/car-6.jpg',N'',N'',N'',N'Đã duyệt',N'Dữ liệu minh họa.',SYSUTCDATETIME(),SYSUTCDATETIME(),6);
SET IDENTITY_INSERT [dbo].[VehiclePartnerApplications] OFF;
GO

SET IDENTITY_INSERT [dbo].[PartnerVehicles] ON;
INSERT INTO [dbo].[PartnerVehicles] ([PartnerVehicleID],[CarID],[OwnerAppUserID],[VehiclePartnerApplicationID],[CommissionRateOverride],[DepositAmount],[IsActive],[PauseReason],[ListedDate]) VALUES
(1,1,3,1,NULL,3000000,1,NULL,SYSUTCDATETIME()),
(2,2,3,2,NULL,3000000,1,NULL,SYSUTCDATETIME()),
(3,3,3,3,NULL,4000000,1,NULL,SYSUTCDATETIME()),
(4,4,3,4,NULL,5000000,1,NULL,SYSUTCDATETIME()),
(5,5,3,5,NULL,4000000,1,NULL,SYSUTCDATETIME()),
(6,6,3,6,NULL,6000000,1,NULL,SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[PartnerVehicles] OFF;
GO

SET IDENTITY_INSERT [dbo].[VehiclePartnerProfiles] ON;
INSERT INTO [dbo].[VehiclePartnerProfiles]
([VehiclePartnerProfileID],[AppUserID],[PartnerType],[FullName],[Phone],[Email],[Address],[CitizenIdentityNumber],[DateOfBirth],[Gender],[CitizenIssuedDate],[CitizenExpiryDate],[PermanentProvince],[PermanentWard],[PermanentDetail],[PermanentPaperAddress],[PermanentAddress],[CurrentAddressSameAsPermanent],[CurrentProvince],[CurrentWard],[CurrentDetail],[CurrentAddress],[CitizenFrontImageUrl],[CitizenBackImageUrl],[PortraitImageUrl],[BankName],[BankAccountNumber],[BankAccountHolder],[BankBranch],[Status],[ReviewNote],[CreatedDate],[SubmittedDate],[ReviewedDate],[ReviewedByAppUserID],[PartnerTermsVersion],[PrivacyPolicyVersion],[TermsAcceptedAt],[PrivacyAcceptedAt]) VALUES
(1,3,N'Cá nhân',N'Nguyễn Văn Đối Tác',N'0900000003',N'doitac@smartcar.local',N'Số 12 đường Láng, Phường Cầu Giấy, Hà Nội',N'001234567890','1992-05-20',N'Nam','2020-01-01','2035-01-01',N'Hà Nội',N'Phường Cầu Giấy',N'Số 12 đường Láng',N'',N'Số 12 đường Láng, Phường Cầu Giấy, Hà Nội',1,N'Hà Nội',N'Phường Cầu Giấy',N'Số 12 đường Láng',N'Số 12 đường Láng, Phường Cầu Giấy, Hà Nội',N'',N'',N'',N'VCB',N'0123456789',N'Nguyễn Văn Đối Tác',N'',N'Đã xác minh',NULL,SYSUTCDATETIME(),SYSUTCDATETIME(),SYSUTCDATETIME(),2,N'Partner-Terms-v1.0',N'Privacy-v1.0',SYSUTCDATETIME(),SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[VehiclePartnerProfiles] OFF;
GO

SET IDENTITY_INSERT [dbo].[UserVerifications] ON;
INSERT INTO [dbo].[UserVerifications] ([UserVerificationID],[AppUserID],[VerificationType],[Status],[LegalFullName],[Gender],[CitizenIdMasked],[CitizenIdIssuedDate],[CitizenIdExpiryDate],[CitizenIdAddress],[PermanentProvince],[PermanentWard],[PermanentDetail],[PermanentAddress],[CurrentAddressSameAsPermanent],[CurrentProvince],[CurrentWard],[CurrentDetail],[CurrentAddress],[DriverLicenseNumber],[DriverLicenseClass],[DateOfBirth],[DriverLicenseIssuedDate],[DriverLicenseExpiry],[ReviewedByAppUserID],[CreatedDate],[ReviewedDate]) VALUES
(1,4,N'Khách thuê',N'Đã xác minh',N'Nguyễn Văn Khách',N'Nam',N'0012******90','2020-01-01','2035-01-01',N'Phố Phong Lạc, Thị trấn Nho Quan, huyện Nho Quan, tỉnh Ninh Bình',N'Ninh Bình',N'Xã Nho Quan',N'Số nhà 5, ngõ 27, đường Thiên Quan, thôn Phong Lạc',N'Số nhà 5, ngõ 27, đường Thiên Quan, thôn Phong Lạc, Xã Nho Quan, Ninh Bình',1,N'Ninh Bình',N'Xã Nho Quan',N'Số nhà 5, ngõ 27, đường Thiên Quan, thôn Phong Lạc',N'Số nhà 5, ngõ 27, đường Thiên Quan, thôn Phong Lạc, Xã Nho Quan, Ninh Bình',N'GPLXKHACH001',N'B2','2000-01-01','2020-01-01','2035-01-01',2,SYSUTCDATETIME(),SYSUTCDATETIME());
SET IDENTITY_INSERT [dbo].[UserVerifications] OFF;
GO

SET IDENTITY_INSERT [dbo].[Categories] ON;
INSERT INTO [dbo].[Categories] ([CategoryID],[Name]) VALUES
(1,N'Kinh nghiệm thuê xe'),(2,N'An toàn giao thông'),(3,N'Tin SmartCar');
SET IDENTITY_INSERT [dbo].[Categories] OFF;
GO

SET IDENTITY_INSERT [dbo].[Authors] ON;
INSERT INTO [dbo].[Authors] ([AuthorID],[Name],[ImageUrl],[Description]) VALUES
(1,N'Nhóm SmartCar',N'/carbook-master/images/person_1.jpg',N'Nhóm phát triển nội dung SmartCar');
SET IDENTITY_INSERT [dbo].[Authors] OFF;
GO

SET IDENTITY_INSERT [dbo].[Blogs] ON;
INSERT INTO [dbo].[Blogs] ([BlogID],[Title],[AuthorID],[CoverImageUrl],[CreatedDate],[CategoryID],[Description]) VALUES
(1,N'Những lưu ý trước khi thuê xe tự lái',1,N'/carbook-master/images/image_1.jpg',SYSUTCDATETIME(),1,N'Kiểm tra giấy tờ, biên bản giao xe, kilomet, nhiên liệu và phụ kiện trước khi nhận xe.'),
(2,N'Vì sao cần xác minh hồ sơ trước khi đặt xe?',1,N'/carbook-master/images/image_2.jpg',SYSUTCDATETIME(),3,N'Xác minh CCCD và GPLX giúp bảo vệ khách thuê, chủ xe và nền tảng.'),
(3,N'Cách xử lý khi phát sinh phụ phí',1,N'/carbook-master/images/image_3.jpg',SYSUTCDATETIME(),1,N'Phụ phí cần có căn cứ, ảnh/video và được xử lý qua hệ thống để tránh tranh chấp.');
SET IDENTITY_INSERT [dbo].[Blogs] OFF;
GO

INSERT INTO [dbo].[TagClouds] ([Title]) VALUES
(N'Thuê xe'),(N'Tự lái'),(N'GPLX'),(N'Giao xe'),(N'Đặt cọc');
GO

GO


-- V27: nhận/trả tại điểm giao xe luôn có phí giao xe bằng 0.
UPDATE [dbo].[VehiclePartnerApplications]
SET [DeliveryFee] = 0
WHERE [DeliveryMethod] = N'Nhận tại điểm giao xe' AND [DeliveryFee] <> 0;
GO


/* Các ràng buộc an toàn kế thừa từ Đợt 1; phần dưới tiếp tục áp dụng Đợt 2. */
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* Index hỗ trợ truy vấn đơn thuê và kiểm tra trùng lịch. */
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_Reservations_Customer_Status'
          AND [object_id] = OBJECT_ID(N'dbo.Reservations')
    )
        CREATE INDEX [IX_Reservations_Customer_Status]
        ON [dbo].[Reservations]([CustomerAppUserID], [Status]);

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE [name] = N'IX_Reservations_Car_Time'
          AND [object_id] = OBJECT_ID(N'dbo.Reservations')
    )
        CREATE INDEX [IX_Reservations_Car_Time]
        ON [dbo].[Reservations]([CarID], [PickUpDate], [DropOffDate], [Status]);

    /* Kiểm tra miền dữ liệu kế thừa từ các bản trước. */
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE [name] = N'CK_Reservations_TotalPrice_NonNegative'
          AND [parent_object_id] = OBJECT_ID(N'dbo.Reservations')
    )
        ALTER TABLE [dbo].[Reservations] WITH CHECK
        ADD CONSTRAINT [CK_Reservations_TotalPrice_NonNegative]
        CHECK ([TotalPrice] >= 0 AND [DepositAmount] >= 0 AND [CancellationFeeAmount] >= 0);

    /*
       Các ràng buộc RentalMode/DeliveryMethod đã được tạo ngay trong phần
       CREATE TABLE của bản OneRun, nên không tạo thêm ràng buộc trùng lặp.
    */

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

CREATE INDEX [IX_PrivateFiles_Owner_Category_Deleted] ON [dbo].[PrivateFiles] ([OwnerAppUserID], [Category], [IsDeleted]);
GO
CREATE INDEX [IX_PrivateFiles_ReservationID] ON [dbo].[PrivateFiles] ([ReservationID]);
GO
CREATE INDEX [IX_PrivateFiles_PartnerApplicationID] ON [dbo].[PrivateFiles] ([PartnerApplicationID]);
GO
CREATE INDEX [IX_PrivateFiles_Attachment] ON [dbo].[PrivateFiles] ([AttachedEntityType], [AttachedEntityID], [AttachedDate]);
GO
CREATE INDEX [IX_PrivateFiles_IsDeleted_PhysicalDeletedDate_DeleteRequestedDate]
ON [dbo].[PrivateFiles] ([IsDeleted], [PhysicalDeletedDate], [DeleteRequestedDate]);
GO
/* KẾ THỪA HARDENING ĐỢT 3 - CHUẨN HÓA BẢO MẬT, UPLOAD VÀ UTC */
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.OrphanedDataAudit', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[OrphanedDataAudit]
        (
            [OrphanedDataAuditID] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrphanedDataAudit] PRIMARY KEY,
            [EntityName] nvarchar(100) NOT NULL,
            [EntityID] nvarchar(100) NULL,
            [Reason] nvarchar(1000) NOT NULL,
            [DetectedAt] datetime2 NOT NULL CONSTRAINT [DF_OrphanedDataAudit_DetectedAt] DEFAULT(SYSUTCDATETIME())
        );
    END;

    /* Dọn các khóa tùy chọn không còn bản ghi cha. */
    UPDATE r SET [PickUpLocationID] = NULL
    FROM [dbo].[Reservations] r LEFT JOIN [dbo].[Locations] l ON l.[LocationID] = r.[PickUpLocationID]
    WHERE r.[PickUpLocationID] IS NOT NULL AND l.[LocationID] IS NULL;
    UPDATE r SET [DropOffLocationID] = NULL
    FROM [dbo].[Reservations] r LEFT JOIN [dbo].[Locations] l ON l.[LocationID] = r.[DropOffLocationID]
    WHERE r.[DropOffLocationID] IS NOT NULL AND l.[LocationID] IS NULL;
    UPDATE r SET [CancelledByAppUserID] = NULL
    FROM [dbo].[Reservations] r LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = r.[CancelledByAppUserID]
    WHERE r.[CancelledByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE h SET [ChangedByAppUserID] = NULL
    FROM [dbo].[ReservationStatusHistories] h LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = h.[ChangedByAppUserID]
    WHERE h.[ChangedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE d SET [AssignedStaffAppUserID] = NULL
    FROM [dbo].[Disputes] d LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = d.[AssignedStaffAppUserID]
    WHERE d.[AssignedStaffAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE f SET [ReservationID] = NULL
    FROM [dbo].[PrivateFiles] f LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID] = f.[ReservationID]
    WHERE f.[ReservationID] IS NOT NULL AND r.[ReservationID] IS NULL;
    UPDATE f SET [PartnerApplicationID] = NULL
    FROM [dbo].[PrivateFiles] f LEFT JOIN [dbo].[VehiclePartnerApplications] a ON a.[VehiclePartnerApplicationID] = f.[PartnerApplicationID]
    WHERE f.[PartnerApplicationID] IS NOT NULL AND a.[VehiclePartnerApplicationID] IS NULL;

    /* Tài khoản có role không tồn tại được chuyển sang role Customer nếu có. */
    DECLARE @FallbackRoleID int = (SELECT TOP (1) [AppRoleId] FROM [dbo].[AppRoles] WHERE [AppRoleName] = N'Customer' ORDER BY [AppRoleId]);
    IF @FallbackRoleID IS NULL SELECT TOP (1) @FallbackRoleID = [AppRoleId] FROM [dbo].[AppRoles] ORDER BY [AppRoleId];
    IF @FallbackRoleID IS NULL
    BEGIN
        THROW 52001, N'Không có AppRole để sửa tài khoản mồ côi.', 1;
    END;
    UPDATE u SET [AppRoleId] = @FallbackRoleID
    FROM [dbo].[AppUsers] u LEFT JOIN [dbo].[AppRoles] r ON r.[AppRoleId] = u.[AppRoleId]
    WHERE r.[AppRoleId] IS NULL;


    /* Chuẩn hóa các khóa người thao tác và FileID trước khi tạo FK bổ sung. */
    DECLARE @FallbackUserID int =
    (
        SELECT TOP (1) u.[AppUserId]
        FROM [dbo].[AppUsers] u
        INNER JOIN [dbo].[AppRoles] r ON r.[AppRoleId] = u.[AppRoleId]
        ORDER BY CASE WHEN r.[AppRoleName] = N'Admin' THEN 0 ELSE 1 END, u.[AppUserId]
    );

    IF @FallbackUserID IS NULL AND
       (EXISTS (SELECT 1 FROM [dbo].[HandoverReports]) OR
        EXISTS (SELECT 1 FROM [dbo].[Disputes]) OR
        EXISTS (SELECT 1 FROM [dbo].[Incidents]) OR
        EXISTS (SELECT 1 FROM [dbo].[DepositTransactions]) OR
        EXISTS (SELECT 1 FROM [dbo].[AdditionalCharges]) OR
        EXISTS (SELECT 1 FROM [dbo].[DisputeMessages]) OR
        EXISTS (SELECT 1 FROM [dbo].[Notifications]))
    BEGIN
        THROW 52002, N'Không có AppUser hợp lệ để sửa khóa người thao tác mồ côi.', 1;
    END;

    UPDATE v SET [ReviewedByAppUserID] = NULL
    FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = v.[ReviewedByAppUserID]
    WHERE v.[ReviewedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE v SET [CitizenIdFrontFileID] = NULL
    FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[PrivateFiles] f ON f.[PrivateFileID] = v.[CitizenIdFrontFileID]
    WHERE v.[CitizenIdFrontFileID] IS NOT NULL AND f.[PrivateFileID] IS NULL;
    UPDATE v SET [CitizenIdBackFileID] = NULL
    FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[PrivateFiles] f ON f.[PrivateFileID] = v.[CitizenIdBackFileID]
    WHERE v.[CitizenIdBackFileID] IS NOT NULL AND f.[PrivateFileID] IS NULL;
    UPDATE v SET [DriverLicenseFileID] = NULL
    FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[PrivateFiles] f ON f.[PrivateFileID] = v.[DriverLicenseFileID]
    WHERE v.[DriverLicenseFileID] IS NOT NULL AND f.[PrivateFileID] IS NULL;
    UPDATE v SET [PortraitFileID] = NULL
    FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[PrivateFiles] f ON f.[PrivateFileID] = v.[PortraitFileID]
    WHERE v.[PortraitFileID] IS NOT NULL AND f.[PrivateFileID] IS NULL;

    UPDATE h SET [CreatedByAppUserID] = @FallbackUserID
    FROM [dbo].[HandoverReports] h LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = h.[CreatedByAppUserID]
    WHERE u.[AppUserId] IS NULL;
    UPDATE h SET [ReplacedByReportId] = NULL
    FROM [dbo].[HandoverReports] h LEFT JOIN [dbo].[HandoverReports] replacement ON replacement.[HandoverReportID] = h.[ReplacedByReportId]
    WHERE h.[ReplacedByReportId] IS NOT NULL AND replacement.[HandoverReportID] IS NULL;
    UPDATE d SET [CreatedByAppUserID] = @FallbackUserID
    FROM [dbo].[Disputes] d LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = d.[CreatedByAppUserID]
    WHERE u.[AppUserId] IS NULL;
    UPDATE i SET [ReportedByAppUserID] = @FallbackUserID
    FROM [dbo].[Incidents] i LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = i.[ReportedByAppUserID]
    WHERE u.[AppUserId] IS NULL;
    UPDATE d SET [CreatedByAppUserID] = @FallbackUserID
    FROM [dbo].[DepositTransactions] d LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = d.[CreatedByAppUserID]
    WHERE u.[AppUserId] IS NULL;
    UPDATE c SET [CreatedByAppUserID] = @FallbackUserID
    FROM [dbo].[AdditionalCharges] c LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = c.[CreatedByAppUserID]
    WHERE u.[AppUserId] IS NULL;
    UPDATE m SET [SenderAppUserID] = @FallbackUserID
    FROM [dbo].[DisputeMessages] m LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = m.[SenderAppUserID]
    WHERE u.[AppUserId] IS NULL;

    UPDATE s SET [CreatedByAppUserID] = NULL
    FROM [dbo].[Settlements] s LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = s.[CreatedByAppUserID]
    WHERE s.[CreatedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE s SET [ApprovedByAppUserID] = NULL
    FROM [dbo].[Settlements] s LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = s.[ApprovedByAppUserID]
    WHERE s.[ApprovedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    UPDATE d SET [ReviewedByAppUserID] = NULL
    FROM [dbo].[VehicleDocuments] d LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = d.[ReviewedByAppUserID]
    WHERE d.[ReviewedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    DELETE n FROM [dbo].[Notifications] n LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = n.[AppUserID]
    WHERE u.[AppUserId] IS NULL;

    /* Xác định các gốc dữ liệu không hợp lệ trước khi xóa phụ thuộc. */
    CREATE TABLE #BadPartnerApplications ([ID] int NOT NULL PRIMARY KEY);
    INSERT INTO #BadPartnerApplications([ID])
    SELECT a.[VehiclePartnerApplicationID]
    FROM [dbo].[VehiclePartnerApplications] a
    LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = a.[AppUserID]
    LEFT JOIN [dbo].[Locations] l ON l.[LocationID] = a.[LocationID]
    WHERE u.[AppUserId] IS NULL OR l.[LocationID] IS NULL;

    CREATE TABLE #BadPartnerVehicles ([ID] int NOT NULL PRIMARY KEY);
    INSERT INTO #BadPartnerVehicles([ID])
    SELECT v.[PartnerVehicleID]
    FROM [dbo].[PartnerVehicles] v
    LEFT JOIN [dbo].[Cars] c ON c.[CarID] = v.[CarID]
    LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = v.[OwnerAppUserID]
    LEFT JOIN [dbo].[VehiclePartnerApplications] a ON a.[VehiclePartnerApplicationID] = v.[VehiclePartnerApplicationID]
    WHERE c.[CarID] IS NULL OR u.[AppUserId] IS NULL OR a.[VehiclePartnerApplicationID] IS NULL
       OR v.[VehiclePartnerApplicationID] IN (SELECT [ID] FROM #BadPartnerApplications);

    CREATE TABLE #BadReservations ([ID] int NOT NULL PRIMARY KEY);
    INSERT INTO #BadReservations([ID])
    SELECT r.[ReservationID]
    FROM [dbo].[Reservations] r
    LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId] = r.[CustomerAppUserID]
    LEFT JOIN [dbo].[PartnerVehicles] v ON v.[PartnerVehicleID] = r.[PartnerVehicleID]
    LEFT JOIN [dbo].[Cars] c ON c.[CarID] = r.[CarID]
    WHERE u.[AppUserId] IS NULL OR v.[PartnerVehicleID] IS NULL OR c.[CarID] IS NULL
       OR r.[PartnerVehicleID] IN (SELECT [ID] FROM #BadPartnerVehicles);

    INSERT INTO [dbo].[OrphanedDataAudit]([EntityName],[EntityID],[Reason])
    SELECT N'Reservation', CONVERT(nvarchar(100), [ID]), N'Xóa khi nâng cấp v30 Đợt 2 vì thiếu khách, xe đối tác hoặc xe gốc.' FROM #BadReservations;
    INSERT INTO [dbo].[OrphanedDataAudit]([EntityName],[EntityID],[Reason])
    SELECT N'PartnerVehicle', CONVERT(nvarchar(100), [ID]), N'Xóa khi nâng cấp v30 Đợt 2 vì thiếu xe, chủ xe hoặc hồ sơ đăng xe.' FROM #BadPartnerVehicles;
    INSERT INTO [dbo].[OrphanedDataAudit]([EntityName],[EntityID],[Reason])
    SELECT N'VehiclePartnerApplication', CONVERT(nvarchar(100), [ID]), N'Xóa khi nâng cấp v30 Đợt 2 vì thiếu tài khoản hoặc địa điểm.' FROM #BadPartnerApplications;

    /* Xóa theo thứ tự con -> cha cho các Reservation gốc bị lỗi. */
    DELETE m FROM [dbo].[DisputeMessages] m INNER JOIN [dbo].[Disputes] d ON d.[DisputeID] = m.[DisputeID] WHERE d.[ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE hi FROM [dbo].[HandoverImages] hi INNER JOIN [dbo].[HandoverReports] hr ON hr.[HandoverReportID] = hi.[HandoverReportID] WHERE hr.[ReservationID] IN (SELECT [ID] FROM #BadReservations);
    UPDATE [dbo].[PrivateFiles] SET [ReservationID] = NULL WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[CommissionTransactions] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Settlements] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[DepositTransactions] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[TrafficFines] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Incidents] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[AdditionalCharges] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Disputes] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[HandoverReports] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Payments] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[ReservationStatusHistories] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Reviews] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    UPDATE [dbo].[FraudFlags] SET [ReservationID] = NULL WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);
    DELETE FROM [dbo].[Reservations] WHERE [ReservationID] IN (SELECT [ID] FROM #BadReservations);

    /* Dọn các bản ghi con mồ côi độc lập còn lại. */
    DELETE p FROM [dbo].[Payments] p LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=p.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE s FROM [dbo].[Settlements] s LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=s.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE h FROM [dbo].[HandoverReports] h LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=h.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE d FROM [dbo].[Disputes] d LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=d.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE i FROM [dbo].[Incidents] i LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=i.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE t FROM [dbo].[TrafficFines] t LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=t.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE d FROM [dbo].[DepositTransactions] d LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=d.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE a FROM [dbo].[AdditionalCharges] a LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=a.[ReservationID] WHERE r.[ReservationID] IS NULL;
    DELETE h FROM [dbo].[ReservationStatusHistories] h LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=h.[ReservationID] WHERE r.[ReservationID] IS NULL;

    DELETE hi FROM [dbo].[HandoverImages] hi LEFT JOIN [dbo].[HandoverReports] h ON h.[HandoverReportID]=hi.[HandoverReportID] WHERE h.[HandoverReportID] IS NULL;
    DELETE dm FROM [dbo].[DisputeMessages] dm LEFT JOIN [dbo].[Disputes] d ON d.[DisputeID]=dm.[DisputeID] WHERE d.[DisputeID] IS NULL;
    DELETE c FROM [dbo].[CommissionTransactions] c LEFT JOIN [dbo].[Reservations] r ON r.[ReservationID]=c.[ReservationID] LEFT JOIN [dbo].[PartnerVehicles] v ON v.[PartnerVehicleID]=c.[PartnerVehicleID] LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=c.[PartnerAppUserID] WHERE r.[ReservationID] IS NULL OR v.[PartnerVehicleID] IS NULL OR u.[AppUserId] IS NULL;

    /* Dọn các hồ sơ gốc xe không hợp lệ sau khi không còn Reservation tham chiếu. */
    DELETE FROM [dbo].[VehicleDocuments] WHERE [PartnerVehicleID] IN (SELECT [ID] FROM #BadPartnerVehicles);
    DELETE FROM [dbo].[MaintenanceRecords] WHERE [PartnerVehicleID] IN (SELECT [ID] FROM #BadPartnerVehicles);
    DELETE FROM [dbo].[PartnerVehicles] WHERE [PartnerVehicleID] IN (SELECT [ID] FROM #BadPartnerVehicles);
    UPDATE [dbo].[PrivateFiles] SET [PartnerApplicationID] = NULL WHERE [PartnerApplicationID] IN (SELECT [ID] FROM #BadPartnerApplications);
    DELETE FROM [dbo].[VehiclePartnerApplications] WHERE [VehiclePartnerApplicationID] IN (SELECT [ID] FROM #BadPartnerApplications);

    DELETE f FROM [dbo].[PrivateFiles] f LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=f.[OwnerAppUserID] WHERE u.[AppUserId] IS NULL;
    DELETE o FROM [dbo].[EmailVerificationOtps] o LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=o.[AppUserID] WHERE u.[AppUserId] IS NULL;
    DELETE p FROM [dbo].[PasswordResetTokens] p LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=p.[AppUserID] WHERE u.[AppUserId] IS NULL;
    DELETE v FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=v.[AppUserID] WHERE u.[AppUserId] IS NULL;
    UPDATE v SET [ReviewedByAppUserID] = NULL FROM [dbo].[UserVerifications] v LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=v.[ReviewedByAppUserID] WHERE v.[ReviewedByAppUserID] IS NOT NULL AND u.[AppUserId] IS NULL;
    DELETE w FROM [dbo].[WorkItemClaims] w LEFT JOIN [dbo].[AppUsers] u ON u.[AppUserId]=w.[AssignedStaffAppUserID] WHERE u.[AppUserId] IS NULL;

    /* Tạo khóa ngoại bằng NO ACTION để không xóa dây chuyền dữ liệu tài chính. */
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_AppUsers_AppRoles') ALTER TABLE [dbo].[AppUsers] WITH CHECK ADD CONSTRAINT [FK_AppUsers_AppRoles] FOREIGN KEY([AppRoleId]) REFERENCES [dbo].[AppRoles]([AppRoleId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_Customers') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_Customers] FOREIGN KEY([CustomerAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_PartnerVehicles') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_PartnerVehicles] FOREIGN KEY([PartnerVehicleID]) REFERENCES [dbo].[PartnerVehicles]([PartnerVehicleID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_Cars') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_Cars] FOREIGN KEY([CarID]) REFERENCES [dbo].[Cars]([CarID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_PickUpLocations') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_PickUpLocations] FOREIGN KEY([PickUpLocationID]) REFERENCES [dbo].[Locations]([LocationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_DropOffLocations') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_DropOffLocations] FOREIGN KEY([DropOffLocationID]) REFERENCES [dbo].[Locations]([LocationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Reservations_CancelledBy') ALTER TABLE [dbo].[Reservations] WITH CHECK ADD CONSTRAINT [FK_Reservations_CancelledBy] FOREIGN KEY([CancelledByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Payments_Reservations') ALTER TABLE [dbo].[Payments] WITH CHECK ADD CONSTRAINT [FK_Payments_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Settlements_Reservations') ALTER TABLE [dbo].[Settlements] WITH CHECK ADD CONSTRAINT [FK_Settlements_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_HandoverReports_Reservations') ALTER TABLE [dbo].[HandoverReports] WITH CHECK ADD CONSTRAINT [FK_HandoverReports_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Disputes_Reservations') ALTER TABLE [dbo].[Disputes] WITH CHECK ADD CONSTRAINT [FK_Disputes_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Incidents_Reservations') ALTER TABLE [dbo].[Incidents] WITH CHECK ADD CONSTRAINT [FK_Incidents_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_TrafficFines_Reservations') ALTER TABLE [dbo].[TrafficFines] WITH CHECK ADD CONSTRAINT [FK_TrafficFines_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DepositTransactions_Reservations') ALTER TABLE [dbo].[DepositTransactions] WITH CHECK ADD CONSTRAINT [FK_DepositTransactions_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_AdditionalCharges_Reservations') ALTER TABLE [dbo].[AdditionalCharges] WITH CHECK ADD CONSTRAINT [FK_AdditionalCharges_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_AdditionalCharges_Payments') ALTER TABLE [dbo].[AdditionalCharges] WITH CHECK ADD CONSTRAINT [FK_AdditionalCharges_Payments] FOREIGN KEY([PaymentID]) REFERENCES [dbo].[Payments]([PaymentID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_ReservationStatusHistories_Reservations') ALTER TABLE [dbo].[ReservationStatusHistories] WITH CHECK ADD CONSTRAINT [FK_ReservationStatusHistories_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_ReservationStatusHistories_ChangedBy') ALTER TABLE [dbo].[ReservationStatusHistories] WITH CHECK ADD CONSTRAINT [FK_ReservationStatusHistories_ChangedBy] FOREIGN KEY([ChangedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]) ON DELETE SET NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_VehiclePartnerApplications_AppUsers') ALTER TABLE [dbo].[VehiclePartnerApplications] WITH CHECK ADD CONSTRAINT [FK_VehiclePartnerApplications_AppUsers] FOREIGN KEY([AppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_VehiclePartnerApplications_Locations') ALTER TABLE [dbo].[VehiclePartnerApplications] WITH CHECK ADD CONSTRAINT [FK_VehiclePartnerApplications_Locations] FOREIGN KEY([LocationID]) REFERENCES [dbo].[Locations]([LocationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PartnerVehicles_Cars') ALTER TABLE [dbo].[PartnerVehicles] WITH CHECK ADD CONSTRAINT [FK_PartnerVehicles_Cars] FOREIGN KEY([CarID]) REFERENCES [dbo].[Cars]([CarID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PartnerVehicles_Owners') ALTER TABLE [dbo].[PartnerVehicles] WITH CHECK ADD CONSTRAINT [FK_PartnerVehicles_Owners] FOREIGN KEY([OwnerAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PartnerVehicles_Applications') ALTER TABLE [dbo].[PartnerVehicles] WITH CHECK ADD CONSTRAINT [FK_PartnerVehicles_Applications] FOREIGN KEY([VehiclePartnerApplicationID]) REFERENCES [dbo].[VehiclePartnerApplications]([VehiclePartnerApplicationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_CommissionTransactions_Reservations') ALTER TABLE [dbo].[CommissionTransactions] WITH CHECK ADD CONSTRAINT [FK_CommissionTransactions_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_CommissionTransactions_PartnerVehicles') ALTER TABLE [dbo].[CommissionTransactions] WITH CHECK ADD CONSTRAINT [FK_CommissionTransactions_PartnerVehicles] FOREIGN KEY([PartnerVehicleID]) REFERENCES [dbo].[PartnerVehicles]([PartnerVehicleID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_CommissionTransactions_AppUsers') ALTER TABLE [dbo].[CommissionTransactions] WITH CHECK ADD CONSTRAINT [FK_CommissionTransactions_AppUsers] FOREIGN KEY([PartnerAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_CommissionTransactions_Settlements') ALTER TABLE [dbo].[CommissionTransactions] WITH CHECK ADD CONSTRAINT [FK_CommissionTransactions_Settlements] FOREIGN KEY([SettlementID]) REFERENCES [dbo].[Settlements]([SettlementID]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PrivateFiles_Owners') ALTER TABLE [dbo].[PrivateFiles] WITH CHECK ADD CONSTRAINT [FK_PrivateFiles_Owners] FOREIGN KEY([OwnerAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PrivateFiles_Reservations') ALTER TABLE [dbo].[PrivateFiles] WITH CHECK ADD CONSTRAINT [FK_PrivateFiles_Reservations] FOREIGN KEY([ReservationID]) REFERENCES [dbo].[Reservations]([ReservationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PrivateFiles_PartnerApplications') ALTER TABLE [dbo].[PrivateFiles] WITH CHECK ADD CONSTRAINT [FK_PrivateFiles_PartnerApplications] FOREIGN KEY([PartnerApplicationID]) REFERENCES [dbo].[VehiclePartnerApplications]([VehiclePartnerApplicationID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_EmailVerificationOtps_AppUsers') ALTER TABLE [dbo].[EmailVerificationOtps] WITH CHECK ADD CONSTRAINT [FK_EmailVerificationOtps_AppUsers] FOREIGN KEY([AppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_PasswordResetTokens_AppUsers') ALTER TABLE [dbo].[PasswordResetTokens] WITH CHECK ADD CONSTRAINT [FK_PasswordResetTokens_AppUsers] FOREIGN KEY([AppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_AppUsers') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_AppUsers] FOREIGN KEY([AppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_WorkItemClaims_AssignedStaff') ALTER TABLE [dbo].[WorkItemClaims] WITH CHECK ADD CONSTRAINT [FK_WorkItemClaims_AssignedStaff] FOREIGN KEY([AssignedStaffAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_HandoverImages_HandoverReports') ALTER TABLE [dbo].[HandoverImages] WITH CHECK ADD CONSTRAINT [FK_HandoverImages_HandoverReports] FOREIGN KEY([HandoverReportID]) REFERENCES [dbo].[HandoverReports]([HandoverReportID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DisputeMessages_Disputes') ALTER TABLE [dbo].[DisputeMessages] WITH CHECK ADD CONSTRAINT [FK_DisputeMessages_Disputes] FOREIGN KEY([DisputeID]) REFERENCES [dbo].[Disputes]([DisputeID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_VehicleDocuments_PartnerVehicles') ALTER TABLE [dbo].[VehicleDocuments] WITH CHECK ADD CONSTRAINT [FK_VehicleDocuments_PartnerVehicles] FOREIGN KEY([PartnerVehicleID]) REFERENCES [dbo].[PartnerVehicles]([PartnerVehicleID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_MaintenanceRecords_PartnerVehicles') ALTER TABLE [dbo].[MaintenanceRecords] WITH CHECK ADD CONSTRAINT [FK_MaintenanceRecords_PartnerVehicles] FOREIGN KEY([PartnerVehicleID]) REFERENCES [dbo].[PartnerVehicles]([PartnerVehicleID]);


    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_ReviewedBy') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_ReviewedBy] FOREIGN KEY([ReviewedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_CitizenIdFrontFile') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_CitizenIdFrontFile] FOREIGN KEY([CitizenIdFrontFileID]) REFERENCES [dbo].[PrivateFiles]([PrivateFileID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_CitizenIdBackFile') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_CitizenIdBackFile] FOREIGN KEY([CitizenIdBackFileID]) REFERENCES [dbo].[PrivateFiles]([PrivateFileID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_DriverLicenseFile') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_DriverLicenseFile] FOREIGN KEY([DriverLicenseFileID]) REFERENCES [dbo].[PrivateFiles]([PrivateFileID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_UserVerifications_PortraitFile') ALTER TABLE [dbo].[UserVerifications] WITH CHECK ADD CONSTRAINT [FK_UserVerifications_PortraitFile] FOREIGN KEY([PortraitFileID]) REFERENCES [dbo].[PrivateFiles]([PrivateFileID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_HandoverReports_CreatedBy') ALTER TABLE [dbo].[HandoverReports] WITH CHECK ADD CONSTRAINT [FK_HandoverReports_CreatedBy] FOREIGN KEY([CreatedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_HandoverReports_ReplacedBy') ALTER TABLE [dbo].[HandoverReports] WITH CHECK ADD CONSTRAINT [FK_HandoverReports_ReplacedBy] FOREIGN KEY([ReplacedByReportId]) REFERENCES [dbo].[HandoverReports]([HandoverReportID]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Disputes_CreatedBy') ALTER TABLE [dbo].[Disputes] WITH CHECK ADD CONSTRAINT [FK_Disputes_CreatedBy] FOREIGN KEY([CreatedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Disputes_AssignedStaff') ALTER TABLE [dbo].[Disputes] WITH CHECK ADD CONSTRAINT [FK_Disputes_AssignedStaff] FOREIGN KEY([AssignedStaffAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Incidents_ReportedBy') ALTER TABLE [dbo].[Incidents] WITH CHECK ADD CONSTRAINT [FK_Incidents_ReportedBy] FOREIGN KEY([ReportedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DepositTransactions_CreatedBy') ALTER TABLE [dbo].[DepositTransactions] WITH CHECK ADD CONSTRAINT [FK_DepositTransactions_CreatedBy] FOREIGN KEY([CreatedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_AdditionalCharges_CreatedBy') ALTER TABLE [dbo].[AdditionalCharges] WITH CHECK ADD CONSTRAINT [FK_AdditionalCharges_CreatedBy] FOREIGN KEY([CreatedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DisputeMessages_Sender') ALTER TABLE [dbo].[DisputeMessages] WITH CHECK ADD CONSTRAINT [FK_DisputeMessages_Sender] FOREIGN KEY([SenderAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_VehicleDocuments_ReviewedBy') ALTER TABLE [dbo].[VehicleDocuments] WITH CHECK ADD CONSTRAINT [FK_VehicleDocuments_ReviewedBy] FOREIGN KEY([ReviewedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Settlements_CreatedBy') ALTER TABLE [dbo].[Settlements] WITH CHECK ADD CONSTRAINT [FK_Settlements_CreatedBy] FOREIGN KEY([CreatedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Settlements_ApprovedBy') ALTER TABLE [dbo].[Settlements] WITH CHECK ADD CONSTRAINT [FK_Settlements_ApprovedBy] FOREIGN KEY([ApprovedByAppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_Notifications_AppUsers') ALTER TABLE [dbo].[Notifications] WITH CHECK ADD CONSTRAINT [FK_Notifications_AppUsers] FOREIGN KEY([AppUserID]) REFERENCES [dbo].[AppUsers]([AppUserId]);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO


GO
PRINT N'HOÀN TẤT SmartCar v30.9: schema, 55 khóa ngoại, 64 index và dữ liệu mẫu đã được tạo. Tài khoản demo đang vô hiệu hóa; chạy TAO_TAI_KHOAN_DEMO_AN_TOAN_v30_9.ps1 để đặt mật khẩu.';
GO

GO

-- SMARTCAR MIGRATION v30.9 -> v31.0 (FIXED BATCH VERSION)
-- Đồng bộ Tài liệu đặc tả nghiệp vụ và yêu cầu hệ thống SmartCar v1.0.
-- Sửa lỗi SQL Server Msg 207: cột mới được tham chiếu trong cùng batch ALTER TABLE ADD.
-- Có thể chạy lại an toàn; không xóa cứng dữ liệu.
SET NOCOUNT ON;
GO
USE [SmartCarMarketplaceDb];
GO
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.SystemVersions', N'U') IS NULL
    THROW 51000, N'Không tìm thấy dbo.SystemVersions. Hãy chắc chắn đây là database SmartCar v30.9.', 1;
GO

/* PHASE A: Chỉ bổ sung cột vào các bảng hiện có.
   Batch phải kết thúc bằng GO trước khi bất kỳ câu lệnh nào tham chiếu các cột mới. */
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.AppUsers', N'AccountType') IS NULL
            ALTER TABLE dbo.AppUsers ADD AccountType nvarchar(20) NOT NULL CONSTRAINT DF_AppUsers_AccountType DEFAULT(N'Customer');
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'ApprovalStatus') IS NULL ALTER TABLE dbo.PartnerVehicles ADD ApprovalStatus nvarchar(30) NOT NULL CONSTRAINT DF_PartnerVehicles_ApprovalStatus DEFAULT(N'Đã duyệt');
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'OperationStatus') IS NULL ALTER TABLE dbo.PartnerVehicles ADD OperationStatus nvarchar(30) NOT NULL CONSTRAINT DF_PartnerVehicles_OperationStatus DEFAULT(N'Đang hoạt động');
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'InactiveReason') IS NULL ALTER TABLE dbo.PartnerVehicles ADD InactiveReason nvarchar(50) NULL;
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'OperationStatusChangedAt') IS NULL ALTER TABLE dbo.PartnerVehicles ADD OperationStatusChangedAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'OperationStatusChangedByAppUserID') IS NULL ALTER TABLE dbo.PartnerVehicles ADD OperationStatusChangedByAppUserID int NULL;
    IF COL_LENGTH(N'dbo.PartnerVehicles',N'RowVersion') IS NULL ALTER TABLE dbo.PartnerVehicles ADD RowVersion rowversion;
    IF COL_LENGTH(N'dbo.Reservations',N'VehiclePricingPlanID') IS NULL ALTER TABLE dbo.Reservations ADD VehiclePricingPlanID int NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'PickUpAddressText') IS NULL ALTER TABLE dbo.Reservations ADD PickUpAddressText nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'DropOffAddressText') IS NULL ALTER TABLE dbo.Reservations ADD DropOffAddressText nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'PassengerCount') IS NULL ALTER TABLE dbo.Reservations ADD PassengerCount int NOT NULL CONSTRAINT DF_Reservations_PassengerCount DEFAULT(0);
    IF COL_LENGTH(N'dbo.Reservations',N'Itinerary') IS NULL ALTER TABLE dbo.Reservations ADD Itinerary nvarchar(2000) NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'SpecialLuggage') IS NULL ALTER TABLE dbo.Reservations ADD SpecialLuggage nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'EstimatedDistanceKm') IS NULL ALTER TABLE dbo.Reservations ADD EstimatedDistanceKm decimal(18,2) NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'ReservationDepositAmount') IS NULL ALTER TABLE dbo.Reservations ADD ReservationDepositAmount decimal(18,2) NOT NULL CONSTRAINT DF_Reservations_ReservationDeposit DEFAULT(0);
    IF COL_LENGTH(N'dbo.Reservations',N'SecurityDepositAmount') IS NULL ALTER TABLE dbo.Reservations ADD SecurityDepositAmount decimal(18,2) NOT NULL CONSTRAINT DF_Reservations_SecurityDeposit DEFAULT(0);
    IF COL_LENGTH(N'dbo.Reservations',N'PartnerResponseExpiresAt') IS NULL ALTER TABLE dbo.Reservations ADD PartnerResponseExpiresAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'PaymentExpiresAt') IS NULL ALTER TABLE dbo.Reservations ADD PaymentExpiresAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'SurchargeProposalExpiresAt') IS NULL ALTER TABLE dbo.Reservations ADD SurchargeProposalExpiresAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'SurchargeResponseExpiresAt') IS NULL ALTER TABLE dbo.Reservations ADD SurchargeResponseExpiresAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'ReviewExpiresAt') IS NULL ALTER TABLE dbo.Reservations ADD ReviewExpiresAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reservations',N'BufferMinutesSnapshot') IS NULL ALTER TABLE dbo.Reservations ADD BufferMinutesSnapshot int NOT NULL CONSTRAINT DF_Reservations_Buffer DEFAULT(0);
    IF COL_LENGTH(N'dbo.Reservations',N'StateVersion') IS NULL ALTER TABLE dbo.Reservations ADD StateVersion int NOT NULL CONSTRAINT DF_Reservations_StateVersion DEFAULT(0);
    IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'IsPayoutPaused') IS NULL ALTER TABLE dbo.VehiclePartnerProfiles ADD IsPayoutPaused bit NOT NULL CONSTRAINT DF_VehiclePartnerProfiles_IsPayoutPaused DEFAULT(0);
    IF COL_LENGTH(N'dbo.VehiclePartnerProfiles',N'PayoutPauseReason') IS NULL ALTER TABLE dbo.VehiclePartnerProfiles ADD PayoutPauseReason nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'CustomerOtpHash') IS NULL ALTER TABLE dbo.HandoverReports ADD CustomerOtpHash nvarchar(128) NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'PartnerOtpHash') IS NULL ALTER TABLE dbo.HandoverReports ADD PartnerOtpHash nvarchar(128) NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'CustomerConfirmedDate') IS NULL ALTER TABLE dbo.HandoverReports ADD CustomerConfirmedDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'PartnerConfirmedDate') IS NULL ALTER TABLE dbo.HandoverReports ADD PartnerConfirmedDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'CustomerOtpLastSentAt') IS NULL ALTER TABLE dbo.HandoverReports ADD CustomerOtpLastSentAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'PartnerOtpLastSentAt') IS NULL ALTER TABLE dbo.HandoverReports ADD PartnerOtpLastSentAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'CustomerOtpFailedAttempts') IS NULL ALTER TABLE dbo.HandoverReports ADD CustomerOtpFailedAttempts int NOT NULL CONSTRAINT DF_HandoverReports_CustomerFailed DEFAULT(0);
    IF COL_LENGTH(N'dbo.HandoverReports',N'PartnerOtpFailedAttempts') IS NULL ALTER TABLE dbo.HandoverReports ADD PartnerOtpFailedAttempts int NOT NULL CONSTRAINT DF_HandoverReports_PartnerFailed DEFAULT(0);
    IF COL_LENGTH(N'dbo.HandoverReports',N'CustomerOtpLockedUntil') IS NULL ALTER TABLE dbo.HandoverReports ADD CustomerOtpLockedUntil datetime2 NULL;
    IF COL_LENGTH(N'dbo.HandoverReports',N'PartnerOtpLockedUntil') IS NULL ALTER TABLE dbo.HandoverReports ADD PartnerOtpLockedUntil datetime2 NULL;
    IF COL_LENGTH(N'dbo.AdditionalCharges',N'SubmittedDate') IS NULL ALTER TABLE dbo.AdditionalCharges ADD SubmittedDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.AdditionalCharges',N'CustomerResponseDate') IS NULL ALTER TABLE dbo.AdditionalCharges ADD CustomerResponseDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.AdditionalCharges',N'ResolvedByAppUserID') IS NULL ALTER TABLE dbo.AdditionalCharges ADD ResolvedByAppUserID int NULL;
    IF COL_LENGTH(N'dbo.Settlements',N'PartnerReviewDueDate') IS NULL ALTER TABLE dbo.Settlements ADD PartnerReviewDueDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.Settlements',N'PartnerConfirmedDate') IS NULL ALTER TABLE dbo.Settlements ADD PartnerConfirmedDate datetime2 NULL;
    IF COL_LENGTH(N'dbo.Settlements',N'PartnerDisputeReason') IS NULL ALTER TABLE dbo.Settlements ADD PartnerDisputeReason nvarchar(1000) NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'ReviewerRole') IS NULL ALTER TABLE dbo.Reviews ADD ReviewerRole nvarchar(30) NOT NULL CONSTRAINT DF_Reviews_ReviewerRole DEFAULT(N'Customer');
    IF COL_LENGTH(N'dbo.Reviews',N'TargetType') IS NULL ALTER TABLE dbo.Reviews ADD TargetType nvarchar(30) NOT NULL CONSTRAINT DF_Reviews_TargetType DEFAULT(N'Vehicle');
    IF COL_LENGTH(N'dbo.Reviews',N'TargetAppUserID') IS NULL ALTER TABLE dbo.Reviews ADD TargetAppUserID int NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'TargetDriverProfileID') IS NULL ALTER TABLE dbo.Reviews ADD TargetDriverProfileID int NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'IsHidden') IS NULL ALTER TABLE dbo.Reviews ADD IsHidden bit NOT NULL CONSTRAINT DF_Reviews_IsHidden DEFAULT(0);
    IF COL_LENGTH(N'dbo.Reviews',N'HiddenReason') IS NULL ALTER TABLE dbo.Reviews ADD HiddenReason nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'HiddenByAppUserID') IS NULL ALTER TABLE dbo.Reviews ADD HiddenByAppUserID int NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'HiddenAt') IS NULL ALTER TABLE dbo.Reviews ADD HiddenAt datetime2 NULL;
    IF COL_LENGTH(N'dbo.Reviews',N'VisibleFromDate') IS NULL ALTER TABLE dbo.Reviews ADD VisibleFromDate datetime2 NULL;
    COMMIT TRANSACTION;
    PRINT N'PHASE A hoàn tất: đã bảo đảm các cột v31.0 tồn tại.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* PHASE B: Cập nhật dữ liệu, tạo index/bảng/FK và ghi phiên bản. */
IF EXISTS (SELECT 1 FROM dbo.SystemVersions WHERE IsCurrent=1 AND DatabaseVersion=N'31.0')
BEGIN
    PRINT N'Database đã ở phiên bản 31.0; bỏ qua migration.';
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;
    /* 1. Tài khoản: username duy nhất toàn hệ thống; email/phone duy nhất theo loại tài khoản. */

    UPDATE u SET AccountType = CASE
        WHEN r.AppRoleName = N'Admin' THEN N'Admin'
        WHEN r.AppRoleName = N'Staff' THEN N'Staff'
        WHEN r.AppRoleName = N'VehiclePartner' OR u.IsVehiclePartner = 1 THEN N'Partner'
        ELSE N'Customer' END
    FROM dbo.AppUsers u LEFT JOIN dbo.AppRoles r ON r.AppRoleId = u.AppRoleId;

    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers WHERE IsDeleted = 0
        GROUP BY AccountType, LOWER(LTRIM(RTRIM(Email))) HAVING COUNT(*) > 1)
        THROW 51001, N'Tồn tại email trùng trong cùng loại tài khoản. Hãy làm sạch dữ liệu trước khi tạo unique index v31.0.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers WHERE IsDeleted = 0 AND Phone IS NOT NULL AND LTRIM(RTRIM(Phone)) <> N''
        GROUP BY AccountType, LTRIM(RTRIM(Phone)) HAVING COUNT(*) > 1)
        THROW 51002, N'Tồn tại số điện thoại trùng trong cùng loại tài khoản. Hãy làm sạch dữ liệu trước khi tạo unique index v31.0.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AppUsers_AccountType_Email' AND object_id=OBJECT_ID(N'dbo.AppUsers'))
        CREATE UNIQUE INDEX IX_AppUsers_AccountType_Email ON dbo.AppUsers(AccountType, Email) WHERE IsDeleted = 0;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AppUsers_AccountType_Phone' AND object_id=OBJECT_ID(N'dbo.AppUsers'))
        CREATE UNIQUE INDEX IX_AppUsers_AccountType_Phone ON dbo.AppUsers(AccountType, Phone) WHERE Phone IS NOT NULL AND IsDeleted = 0;

    /* 1b. Một CCCD chỉ gắn với một hồ sơ khách và một hồ sơ đối tác; hai loại vẫn được phép dùng cùng CCCD. */
    IF EXISTS (SELECT 1 FROM dbo.UserVerifications WHERE CitizenIdFingerprint IS NOT NULL GROUP BY CitizenIdFingerprint HAVING COUNT(*) > 1)
        THROW 51003, N'Tồn tại CCCD trùng giữa nhiều hồ sơ khách. Hãy xử lý dữ liệu trước khi nâng v31.0.', 1;
    IF EXISTS (SELECT 1 FROM dbo.VehiclePartnerProfiles WHERE CitizenIdFingerprint IS NOT NULL GROUP BY CitizenIdFingerprint HAVING COUNT(*) > 1)
        THROW 51004, N'Tồn tại CCCD trùng giữa nhiều hồ sơ đối tác. Hãy xử lý dữ liệu trước khi nâng v31.0.', 1;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserVerifications_CitizenIdFingerprint' AND object_id=OBJECT_ID(N'dbo.UserVerifications'))
        DROP INDEX IX_UserVerifications_CitizenIdFingerprint ON dbo.UserVerifications;
    CREATE UNIQUE INDEX IX_UserVerifications_CitizenIdFingerprint ON dbo.UserVerifications(CitizenIdFingerprint) WHERE CitizenIdFingerprint IS NOT NULL;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehiclePartnerProfiles_CitizenIdFingerprint' AND object_id=OBJECT_ID(N'dbo.VehiclePartnerProfiles'))
        DROP INDEX IX_VehiclePartnerProfiles_CitizenIdFingerprint ON dbo.VehiclePartnerProfiles;
    CREATE UNIQUE INDEX IX_VehiclePartnerProfiles_CitizenIdFingerprint ON dbo.VehiclePartnerProfiles(CitizenIdFingerprint) WHERE CitizenIdFingerprint IS NOT NULL;

    /* 2. Yêu cầu đăng ký chờ OTP: chỉ tạo AppUser sau OTP hợp lệ. */
    IF OBJECT_ID(N'dbo.RegistrationAttempts', N'U') IS NULL
    CREATE TABLE dbo.RegistrationAttempts(
        RegistrationAttemptID uniqueidentifier NOT NULL CONSTRAINT PK_RegistrationAttempts PRIMARY KEY,
        Username nvarchar(30) NOT NULL, PasswordHash nvarchar(500) NOT NULL,
        Name nvarchar(100) NOT NULL, Surname nvarchar(100) NOT NULL,
        Email nvarchar(256) NOT NULL, Phone nvarchar(20) NOT NULL,
        AccountType nvarchar(20) NOT NULL, PartnerType nvarchar(50) NULL,
        TermsVersion nvarchar(50) NOT NULL, PrivacyVersion nvarchar(50) NOT NULL,
        TermsAcceptedAt datetime2 NOT NULL, PrivacyAcceptedAt datetime2 NOT NULL,
        Status nvarchar(20) NOT NULL CONSTRAINT DF_RegistrationAttempts_Status DEFAULT(N'Pending'),
        OtpHash nvarchar(128) NOT NULL, FailedAttempts int NOT NULL CONSTRAINT DF_RegistrationAttempts_FailedAttempts DEFAULT(0),
        SendCountHour int NOT NULL CONSTRAINT DF_RegistrationAttempts_SendCountHour DEFAULT(0),
        HourWindowStartedAt datetime2 NOT NULL, SendCountDay int NOT NULL CONSTRAINT DF_RegistrationAttempts_SendCountDay DEFAULT(0),
        DayWindowStartedAt datetime2 NOT NULL, LastSentAt datetime2 NULL,
        OtpExpiresAt datetime2 NOT NULL, ExpiresAt datetime2 NOT NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_RegistrationAttempts_CreatedAt DEFAULT(SYSUTCDATETIME()),
        VerifiedAt datetime2 NULL, CreatedAppUserID int NULL, RowVersion rowversion NOT NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RegistrationAttempts_AccountType_Email_Status' AND object_id=OBJECT_ID(N'dbo.RegistrationAttempts'))
        CREATE INDEX IX_RegistrationAttempts_AccountType_Email_Status ON dbo.RegistrationAttempts(AccountType,Email,Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RegistrationAttempts_Status_ExpiresAt' AND object_id=OBJECT_ID(N'dbo.RegistrationAttempts'))
        CREATE INDEX IX_RegistrationAttempts_Status_ExpiresAt ON dbo.RegistrationAttempts(Status,ExpiresAt);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RegistrationAttempts_Pending_AccountType_Email' AND object_id=OBJECT_ID(N'dbo.RegistrationAttempts'))
        CREATE UNIQUE INDEX IX_RegistrationAttempts_Pending_AccountType_Email ON dbo.RegistrationAttempts(AccountType,Email) WHERE Status=N'Pending';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RegistrationAttempts_Pending_Username' AND object_id=OBJECT_ID(N'dbo.RegistrationAttempts'))
        CREATE UNIQUE INDEX IX_RegistrationAttempts_Pending_Username ON dbo.RegistrationAttempts(Username) WHERE Status=N'Pending';

    /* 3. SystemSettings để không hard-code thời hạn/hạn mức. */
    IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
    CREATE TABLE dbo.SystemSettings(
        SystemSettingID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
        SettingKey nvarchar(100) NOT NULL, SettingValue nvarchar(1000) NOT NULL,
        ValueType nvarchar(30) NOT NULL CONSTRAINT DF_SystemSettings_ValueType DEFAULT(N'Integer'),
        Description nvarchar(500) NULL, IsActive bit NOT NULL CONSTRAINT DF_SystemSettings_IsActive DEFAULT(1),
        UpdatedAt datetime2 NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT(SYSUTCDATETIME()), UpdatedByAppUserID int NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_SystemSettings_SettingKey' AND object_id=OBJECT_ID(N'dbo.SystemSettings'))
        CREATE UNIQUE INDEX IX_SystemSettings_SettingKey ON dbo.SystemSettings(SettingKey);

    MERGE dbo.SystemSettings AS t USING (VALUES
      (N'OtpExpiryMinutes',N'5',N'Integer',N'OTP đăng ký hiệu lực 5 phút'),
      (N'OtpMaxAttempts',N'5',N'Integer',N'Số lần nhập sai tối đa cho một OTP'),
      (N'OtpResendCooldownSeconds',N'60',N'Integer',N'Thời gian chờ gửi lại OTP'),
      (N'OtpMaxPerHour',N'5',N'Integer',N'Số OTP tối đa mỗi giờ'),
      (N'OtpMaxPerDay',N'10',N'Integer',N'Số OTP tối đa mỗi ngày'),
      (N'BookingHoldMinutes',N'15',N'Integer',N'Giữ chỗ kỹ thuật'),
      (N'PartnerResponseMinutes',N'120',N'Integer',N'Đối tác phản hồi yêu cầu'),
      (N'PaymentWindowMinutes',N'15',N'Integer',N'Khách thanh toán sau khi được chấp nhận'),
      (N'SelfDriveMinHours',N'4',N'Integer',N'Thời gian thuê tự lái tối thiểu'),
      (N'DriverServiceMinHours',N'2',N'Integer',N'Thời gian thuê có tài xế tối thiểu'),
      (N'SelfDriveBufferMinutes',N'120',N'Integer',N'Khoảng đệm tự lái'),
      (N'DriverServiceBufferMinutes',N'60',N'Integer',N'Khoảng đệm có tài xế'),
      (N'MaxAdvanceBookingDays',N'90',N'Integer',N'Được đặt trước tối đa'),
      (N'DocumentWarningDays',N'30',N'Integer',N'Cảnh báo giấy tờ'),
      (N'DocumentCriticalWarningDays',N'7',N'Integer',N'Cảnh báo mạnh giấy tờ'),
      (N'SurchargeProposalHours',N'24',N'Integer',N'Thời hạn đề xuất phụ phí'),
      (N'SurchargeResponseHours',N'24',N'Integer',N'Thời hạn khách phản hồi phụ phí'),
      (N'CustomerNoShowMinutes',N'60',N'Integer',N'Ngưỡng no-show'),
      (N'DisputeOpenDays',N'7',N'Integer',N'Thời hạn mở tranh chấp'),
      (N'DisputeAppealDays',N'3',N'Integer',N'Thời hạn khiếu nại lại'),
      (N'ReviewWindowDays',N'14',N'Integer',N'Thời hạn đánh giá'),
      (N'TrafficFineClaimDays',N'180',N'Integer',N'Thời hạn gửi phạt nguội'),
      (N'StaffSurchargeApprovalLimit',N'2000000',N'Decimal',N'Hạn mức nhân viên duyệt phụ phí')
    ) s(SettingKey,SettingValue,ValueType,Description)
    ON t.SettingKey=s.SettingKey
    WHEN MATCHED THEN UPDATE SET SettingValue=s.SettingValue,ValueType=s.ValueType,Description=s.Description,IsActive=1,UpdatedAt=SYSUTCDATETIME()
    WHEN NOT MATCHED THEN INSERT(SettingKey,SettingValue,ValueType,Description) VALUES(s.SettingKey,s.SettingValue,s.ValueType,s.Description);

    /* 4. Xe: tách trạng thái duyệt và trạng thái vận hành. */
    UPDATE dbo.PartnerVehicles SET OperationStatus=CASE WHEN IsActive=1 THEN N'Đang hoạt động' ELSE N'Ngừng hoạt động' END WHERE OperationStatus IS NULL OR OperationStatus=N'';

    /* 5. Hai bảng giá độc lập. */
    IF OBJECT_ID(N'dbo.VehiclePricingPlans', N'U') IS NULL
    CREATE TABLE dbo.VehiclePricingPlans(
        VehiclePricingPlanID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehiclePricingPlans PRIMARY KEY,
        PartnerVehicleID int NOT NULL, ServiceType nvarchar(30) NOT NULL,
        HourlyRate decimal(18,2) NULL, DailyRate decimal(18,2) NULL, TripRate decimal(18,2) NULL, PerKilometerRate decimal(18,2) NULL,
        MinimumHours int NOT NULL CONSTRAINT DF_VehiclePricingPlans_MinimumHours DEFAULT(0), MinimumDays int NOT NULL CONSTRAINT DF_VehiclePricingPlans_MinimumDays DEFAULT(0),
        ReservationDepositAmount decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_ReservationDeposit DEFAULT(0),
        SecurityDepositAmount decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_SecurityDeposit DEFAULT(0),
        KilometerLimitPerDay int NOT NULL CONSTRAINT DF_VehiclePricingPlans_KmLimit DEFAULT(0), ExtraKilometerFee decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_ExtraKm DEFAULT(0),
        LateReturnFeePerHour decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_LateFee DEFAULT(0), DeliveryFee decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_DeliveryFee DEFAULT(0),
        DriverFee decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_DriverFee DEFAULT(0), WaitingFeePerHour decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_WaitingFee DEFAULT(0),
        OvertimeFeePerHour decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_OvertimeFee DEFAULT(0), OutOfProvinceFee decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_ProvinceFee DEFAULT(0), OvernightFee decimal(18,2) NOT NULL CONSTRAINT DF_VehiclePricingPlans_OvernightFee DEFAULT(0),
        WeekendMultiplier decimal(8,4) NOT NULL CONSTRAINT DF_VehiclePricingPlans_Weekend DEFAULT(1), HolidayMultiplier decimal(8,4) NOT NULL CONSTRAINT DF_VehiclePricingPlans_Holiday DEFAULT(1),
        TollIncluded bit NOT NULL CONSTRAINT DF_VehiclePricingPlans_Toll DEFAULT(0), ParkingIncluded bit NOT NULL CONSTRAINT DF_VehiclePricingPlans_Parking DEFAULT(0),
        CancellationPolicyVersion nvarchar(50) NOT NULL CONSTRAINT DF_VehiclePricingPlans_Cancel DEFAULT(N'cancel-v31.0'),
        FuelPolicy nvarchar(1000) NULL, CleaningPolicy nvarchar(1000) NULL, Notes nvarchar(2000) NULL,
        IsActive bit NOT NULL CONSTRAINT DF_VehiclePricingPlans_IsActive DEFAULT(1), EffectiveFromUtc datetime2 NOT NULL CONSTRAINT DF_VehiclePricingPlans_From DEFAULT(SYSUTCDATETIME()), EffectiveToUtc datetime2 NULL, UpdatedAt datetime2 NOT NULL CONSTRAINT DF_VehiclePricingPlans_Updated DEFAULT(SYSUTCDATETIME()), RowVersion rowversion NOT NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehiclePricingPlans_PartnerVehicleID_ServiceType_IsActive' AND object_id=OBJECT_ID(N'dbo.VehiclePricingPlans'))
        CREATE INDEX IX_VehiclePricingPlans_PartnerVehicleID_ServiceType_IsActive ON dbo.VehiclePricingPlans(PartnerVehicleID,ServiceType,IsActive);

    /* 6. Đơn thuê: trạng thái, lịch trình, hai loại cọc và deadline. */
    UPDATE dbo.Reservations SET ReservationDepositAmount=DepositAmount WHERE ReservationDepositAmount=0 AND DepositAmount>0;

    /* 7. Hồ sơ tài xế và phân công theo đơn. */
    IF OBJECT_ID(N'dbo.DriverProfiles', N'U') IS NULL
    CREATE TABLE dbo.DriverProfiles(
        DriverProfileID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DriverProfiles PRIMARY KEY,
        PartnerAppUserID int NOT NULL, FullName nvarchar(150) NOT NULL, Phone nvarchar(20) NOT NULL,
        CitizenIdentityNumber nvarchar(20) NOT NULL, CitizenIdFingerprint nvarchar(64) NULL,
        DriverLicenseNumber nvarchar(50) NOT NULL, DriverLicenseClass nvarchar(20) NOT NULL,
        DriverLicenseIssuedDate datetime2 NOT NULL, DriverLicenseExpiryDate datetime2 NOT NULL,
        RelationshipType nvarchar(50) NOT NULL CONSTRAINT DF_DriverProfiles_Relationship DEFAULT(N'Nhân viên'),
        CitizenIdFrontFileID uniqueidentifier NULL, CitizenIdBackFileID uniqueidentifier NULL, PortraitFileID uniqueidentifier NULL, DriverLicenseFileID uniqueidentifier NULL,
        Status nvarchar(40) NOT NULL CONSTRAINT DF_DriverProfiles_Status DEFAULT(N'Bản nháp'), ReviewReason nvarchar(1000) NULL,
        CanResubmit bit NOT NULL CONSTRAINT DF_DriverProfiles_CanResubmit DEFAULT(1), ReviewedByAppUserID int NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_DriverProfiles_CreatedAt DEFAULT(SYSUTCDATETIME()), SubmittedAt datetime2 NULL, ReviewedAt datetime2 NULL, RowVersion rowversion NOT NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_DriverProfiles_PartnerAppUserID_Status' AND object_id=OBJECT_ID(N'dbo.DriverProfiles')) CREATE INDEX IX_DriverProfiles_PartnerAppUserID_Status ON dbo.DriverProfiles(PartnerAppUserID,Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_DriverProfiles_DriverLicenseNumber' AND object_id=OBJECT_ID(N'dbo.DriverProfiles')) CREATE UNIQUE INDEX IX_DriverProfiles_DriverLicenseNumber ON dbo.DriverProfiles(DriverLicenseNumber);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_DriverProfiles_CitizenIdFingerprint' AND object_id=OBJECT_ID(N'dbo.DriverProfiles')) CREATE UNIQUE INDEX IX_DriverProfiles_CitizenIdFingerprint ON dbo.DriverProfiles(CitizenIdFingerprint) WHERE CitizenIdFingerprint IS NOT NULL;

    IF OBJECT_ID(N'dbo.BookingDriverAssignments', N'U') IS NULL
    CREATE TABLE dbo.BookingDriverAssignments(
        BookingDriverAssignmentID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingDriverAssignments PRIMARY KEY,
        ReservationID int NOT NULL, DriverProfileID int NOT NULL, AssignedByAppUserID int NOT NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_BookingDriverAssignments_Status DEFAULT(N'Active'), IsPrimary bit NOT NULL CONSTRAINT DF_BookingDriverAssignments_Primary DEFAULT(1),
        AssignmentStartUtc datetime2 NOT NULL, AssignmentEndUtc datetime2 NOT NULL, ChangeReason nvarchar(1000) NULL,
        AssignedAt datetime2 NOT NULL CONSTRAINT DF_BookingDriverAssignments_AssignedAt DEFAULT(SYSUTCDATETIME()), EndedAt datetime2 NULL, RowVersion rowversion NOT NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BookingDriverAssignments_ReservationID_Status' AND object_id=OBJECT_ID(N'dbo.BookingDriverAssignments')) CREATE INDEX IX_BookingDriverAssignments_ReservationID_Status ON dbo.BookingDriverAssignments(ReservationID,Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BookingDriverAssignments_DriverProfileID_AssignmentStartUtc_AssignmentEndUtc_Status' AND object_id=OBJECT_ID(N'dbo.BookingDriverAssignments')) CREATE INDEX IX_BookingDriverAssignments_DriverProfileID_AssignmentStartUtc_AssignmentEndUtc_Status ON dbo.BookingDriverAssignments(DriverProfileID,AssignmentStartUtc,AssignmentEndUtc,Status);

    /* 8. Chủ xe khóa lịch. */
    IF OBJECT_ID(N'dbo.VehicleAvailabilityBlocks', N'U') IS NULL
    CREATE TABLE dbo.VehicleAvailabilityBlocks(
        VehicleAvailabilityBlockID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleAvailabilityBlocks PRIMARY KEY,
        PartnerVehicleID int NOT NULL, StartUtc datetime2 NOT NULL, EndUtc datetime2 NOT NULL,
        BlockType nvarchar(30) NOT NULL CONSTRAINT DF_VehicleAvailabilityBlocks_Type DEFAULT(N'OwnerPaused'), Reason nvarchar(500) NOT NULL,
        CreatedByAppUserID int NOT NULL, IsActive bit NOT NULL CONSTRAINT DF_VehicleAvailabilityBlocks_Active DEFAULT(1),
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_VehicleAvailabilityBlocks_Created DEFAULT(SYSUTCDATETIME()), CancelledAt datetime2 NULL, RowVersion rowversion NOT NULL,
        CONSTRAINT CK_VehicleAvailabilityBlocks_Time CHECK(EndUtc>StartUtc)
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_VehicleAvailabilityBlocks_PartnerVehicleID_StartUtc_EndUtc_IsActive' AND object_id=OBJECT_ID(N'dbo.VehicleAvailabilityBlocks')) CREATE INDEX IX_VehicleAvailabilityBlocks_PartnerVehicleID_StartUtc_EndUtc_IsActive ON dbo.VehicleAvailabilityBlocks(PartnerVehicleID,StartUtc,EndUtc,IsActive);

    /* 9. Thay đổi tài khoản ngân hàng phải duyệt lại, tạm dừng chi trả. */
    IF OBJECT_ID(N'dbo.BankAccountChangeRequests', N'U') IS NULL
    CREATE TABLE dbo.BankAccountChangeRequests(
        BankAccountChangeRequestID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BankAccountChangeRequests PRIMARY KEY,
        VehiclePartnerProfileID int NOT NULL,
        OldBankName nvarchar(100) NOT NULL, OldAccountNumber nvarchar(50) NOT NULL, OldAccountHolder nvarchar(200) NOT NULL,
        NewBankName nvarchar(100) NOT NULL, NewAccountNumber nvarchar(50) NOT NULL, NewAccountHolder nvarchar(200) NOT NULL, NewBankBranch nvarchar(100) NULL,
        Status nvarchar(30) NOT NULL CONSTRAINT DF_BankAccountChangeRequests_Status DEFAULT(N'Chờ duyệt'), ReviewReason nvarchar(1000) NULL,
        RequestedByAppUserID int NOT NULL, ReviewedByAppUserID int NULL,
        RequestedAt datetime2 NOT NULL CONSTRAINT DF_BankAccountChangeRequests_RequestedAt DEFAULT(SYSUTCDATETIME()), ReviewedAt datetime2 NULL, RowVersion rowversion NOT NULL
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_BankAccountChangeRequests_VehiclePartnerProfileID_Status' AND object_id=OBJECT_ID(N'dbo.BankAccountChangeRequests')) CREATE INDEX IX_BankAccountChangeRequests_VehiclePartnerProfileID_Status ON dbo.BankAccountChangeRequests(VehiclePartnerProfileID,Status);

    /* 10. Giao/trả hai OTP độc lập. */

    /* 11. Phụ phí, đối soát, đánh giá hai chiều. */


    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Reviews_ReservationID_AppUserID_TargetType' AND object_id=OBJECT_ID(N'dbo.Reviews'))
        CREATE UNIQUE INDEX IX_Reviews_ReservationID_AppUserID_TargetType ON dbo.Reviews(ReservationID,AppUserID,TargetType) WHERE ReservationID IS NOT NULL AND AppUserID IS NOT NULL AND IsDeleted=0;

    /* 12. Hoàn tiền là giao dịch riêng, người đề xuất không tự duyệt. */
    IF OBJECT_ID(N'dbo.RefundTransactions', N'U') IS NULL
    CREATE TABLE dbo.RefundTransactions(
        RefundTransactionID int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefundTransactions PRIMARY KEY,
        ReservationID int NOT NULL, OriginalPaymentID int NULL, Amount decimal(18,2) NOT NULL,
        Reason nvarchar(500) NOT NULL, Status nvarchar(30) NOT NULL CONSTRAINT DF_RefundTransactions_Status DEFAULT(N'Proposed'),
        IdempotencyKey nvarchar(100) NULL, BankReference nvarchar(100) NULL,
        ProposedByAppUserID int NOT NULL, ApprovedByAppUserID int NULL,
        ProposedAt datetime2 NOT NULL CONSTRAINT DF_RefundTransactions_ProposedAt DEFAULT(SYSUTCDATETIME()), ApprovedAt datetime2 NULL, CompletedAt datetime2 NULL, RowVersion rowversion NOT NULL,
        CONSTRAINT CK_RefundTransactions_Amount CHECK(Amount>0)
    );
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RefundTransactions_ReservationID_Status' AND object_id=OBJECT_ID(N'dbo.RefundTransactions')) CREATE INDEX IX_RefundTransactions_ReservationID_Status ON dbo.RefundTransactions(ReservationID,Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_RefundTransactions_IdempotencyKey' AND object_id=OBJECT_ID(N'dbo.RefundTransactions')) CREATE UNIQUE INDEX IX_RefundTransactions_IdempotencyKey ON dbo.RefundTransactions(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;

    /* 13. Khóa ngoại mới - NO ACTION để giữ lịch sử. */
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_SystemSettings_UpdatedBy') ALTER TABLE dbo.SystemSettings WITH CHECK ADD CONSTRAINT FK_SystemSettings_UpdatedBy FOREIGN KEY(UpdatedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DriverProfiles_Partner') ALTER TABLE dbo.DriverProfiles WITH CHECK ADD CONSTRAINT FK_DriverProfiles_Partner FOREIGN KEY(PartnerAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DriverProfiles_ReviewedBy') ALTER TABLE dbo.DriverProfiles WITH CHECK ADD CONSTRAINT FK_DriverProfiles_ReviewedBy FOREIGN KEY(ReviewedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehiclePricingPlans_PartnerVehicles') ALTER TABLE dbo.VehiclePricingPlans WITH CHECK ADD CONSTRAINT FK_VehiclePricingPlans_PartnerVehicles FOREIGN KEY(PartnerVehicleID) REFERENCES dbo.PartnerVehicles(PartnerVehicleID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Reservations_VehiclePricingPlans') ALTER TABLE dbo.Reservations WITH CHECK ADD CONSTRAINT FK_Reservations_VehiclePricingPlans FOREIGN KEY(VehiclePricingPlanID) REFERENCES dbo.VehiclePricingPlans(VehiclePricingPlanID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BookingDriverAssignments_Reservations') ALTER TABLE dbo.BookingDriverAssignments WITH CHECK ADD CONSTRAINT FK_BookingDriverAssignments_Reservations FOREIGN KEY(ReservationID) REFERENCES dbo.Reservations(ReservationID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BookingDriverAssignments_Drivers') ALTER TABLE dbo.BookingDriverAssignments WITH CHECK ADD CONSTRAINT FK_BookingDriverAssignments_Drivers FOREIGN KEY(DriverProfileID) REFERENCES dbo.DriverProfiles(DriverProfileID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BookingDriverAssignments_AssignedBy') ALTER TABLE dbo.BookingDriverAssignments WITH CHECK ADD CONSTRAINT FK_BookingDriverAssignments_AssignedBy FOREIGN KEY(AssignedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehicleAvailabilityBlocks_PartnerVehicles') ALTER TABLE dbo.VehicleAvailabilityBlocks WITH CHECK ADD CONSTRAINT FK_VehicleAvailabilityBlocks_PartnerVehicles FOREIGN KEY(PartnerVehicleID) REFERENCES dbo.PartnerVehicles(PartnerVehicleID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_VehicleAvailabilityBlocks_CreatedBy') ALTER TABLE dbo.VehicleAvailabilityBlocks WITH CHECK ADD CONSTRAINT FK_VehicleAvailabilityBlocks_CreatedBy FOREIGN KEY(CreatedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BankAccountChangeRequests_Profiles') ALTER TABLE dbo.BankAccountChangeRequests WITH CHECK ADD CONSTRAINT FK_BankAccountChangeRequests_Profiles FOREIGN KEY(VehiclePartnerProfileID) REFERENCES dbo.VehiclePartnerProfiles(VehiclePartnerProfileID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BankAccountChangeRequests_RequestedBy') ALTER TABLE dbo.BankAccountChangeRequests WITH CHECK ADD CONSTRAINT FK_BankAccountChangeRequests_RequestedBy FOREIGN KEY(RequestedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_BankAccountChangeRequests_ReviewedBy') ALTER TABLE dbo.BankAccountChangeRequests WITH CHECK ADD CONSTRAINT FK_BankAccountChangeRequests_ReviewedBy FOREIGN KEY(ReviewedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_RefundTransactions_Reservations') ALTER TABLE dbo.RefundTransactions WITH CHECK ADD CONSTRAINT FK_RefundTransactions_Reservations FOREIGN KEY(ReservationID) REFERENCES dbo.Reservations(ReservationID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_RefundTransactions_Payments') ALTER TABLE dbo.RefundTransactions WITH CHECK ADD CONSTRAINT FK_RefundTransactions_Payments FOREIGN KEY(OriginalPaymentID) REFERENCES dbo.Payments(PaymentID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_RefundTransactions_ProposedBy') ALTER TABLE dbo.RefundTransactions WITH CHECK ADD CONSTRAINT FK_RefundTransactions_ProposedBy FOREIGN KEY(ProposedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_RefundTransactions_ApprovedBy') ALTER TABLE dbo.RefundTransactions WITH CHECK ADD CONSTRAINT FK_RefundTransactions_ApprovedBy FOREIGN KEY(ApprovedByAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Reviews_TargetAppUser') ALTER TABLE dbo.Reviews WITH CHECK ADD CONSTRAINT FK_Reviews_TargetAppUser FOREIGN KEY(TargetAppUserID) REFERENCES dbo.AppUsers(AppUserId);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Reviews_TargetDriver') ALTER TABLE dbo.Reviews WITH CHECK ADD CONSTRAINT FK_Reviews_TargetDriver FOREIGN KEY(TargetDriverProfileID) REFERENCES dbo.DriverProfiles(DriverProfileID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Reviews_HiddenBy') ALTER TABLE dbo.Reviews WITH CHECK ADD CONSTRAINT FK_Reviews_HiddenBy FOREIGN KEY(HiddenByAppUserID) REFERENCES dbo.AppUsers(AppUserId);

    /* 13b. Tách cọc giữ chỗ và cọc bảo đảm; mỗi loại có tối đa một giao dịch thành công/đơn. */
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Payments_ReservationID_PaymentType' AND object_id=OBJECT_ID(N'dbo.Payments'))
        DROP INDEX IX_Payments_ReservationID_PaymentType ON dbo.Payments;
    CREATE UNIQUE INDEX IX_Payments_ReservationID_PaymentType ON dbo.Payments(ReservationID, PaymentType)
        WHERE Status = N'Thành công' AND PaymentType IN (N'Tiền cọc', N'Cọc giữ chỗ', N'Cọc bảo đảm', N'Tiền thuê');

    /* 14. Cập nhật phiên bản database. */
    UPDATE dbo.SystemVersions SET IsCurrent=0 WHERE IsCurrent=1;
    IF EXISTS (SELECT 1 FROM dbo.SystemVersions WHERE DatabaseVersion=N'31.0')
        UPDATE dbo.SystemVersions SET ApplicationVersion=N'31.0.8', ReleasedDate=SYSUTCDATETIME(), IsCurrent=1, Notes=N'v31.0.8: bổ sung địa điểm chi tiết, tọa độ bản đồ, tìm xe theo bán kính và địa chỉ giao nhận.' WHERE DatabaseVersion=N'31.0';
    ELSE
        INSERT dbo.SystemVersions(ApplicationVersion,DatabaseVersion,ReleasedDate,IsCurrent,Notes)
        VALUES(N'31.0.8',N'31.0',SYSUTCDATETIME(),1,N'v31.0.8: bổ sung địa điểm chi tiết, tọa độ bản đồ, tìm xe theo bán kính và địa chỉ giao nhận.');

    IF NOT EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260715_SmartCar_v31_SpecV1')
        INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion) VALUES(N'20260715_SmartCar_v31_SpecV1',N'7.0.12');


    COMMIT TRANSACTION;
    PRINT N'HOÀN TẤT migration SmartCar v31.0 theo đặc tả nghiệp vụ v1.0.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO


-- ===== NÂNG CẤP DANH MỤC HÀNH CHÍNH v31.0.15 =====
-- SMARTCAR UPGRADE v31.0.15 - DANH MỤC HÀNH CHÍNH HAI CẤP
-- Chạy trên database SmartCarMarketplaceDb hiện có của bản v31.0.x.
-- Dữ liệu seed: 34 tỉnh/thành phố và 3.321 xã/phường/đặc khu; bộ dữ liệu tạo ngày 12/07/2026.
-- Script idempotent: có thể chạy lại để đồng bộ danh mục mà không xóa hồ sơ người dùng.
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 53115, N'Hãy chọn database SmartCarMarketplaceDb trước khi chạy script nâng cấp.', 1;
IF OBJECT_ID(N'dbo.UserVerifications', N'U') IS NULL OR OBJECT_ID(N'dbo.SystemVersions', N'U') IS NULL
    THROW 53116, N'Không tìm thấy schema SmartCar v31.0. Hãy kiểm tra đúng database.', 1;
GO

-- PHA 1: tạo bảng/cột trong một batch riêng.
-- SQL Server biên dịch cả batch trước khi chạy; tách batch để các câu lệnh sau nhận ra schema mới.
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.AdministrativeProvinces', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AdministrativeProvinces
        (
            ProvinceCode varchar(2) NOT NULL CONSTRAINT PK_AdministrativeProvinces PRIMARY KEY,
            ProvinceName nvarchar(100) NOT NULL,
            ProvinceType nvarchar(30) NOT NULL,
            IsActive bit NOT NULL CONSTRAINT DF_AdministrativeProvinces_IsActive DEFAULT(1),
            EffectiveFrom date NULL,
            EffectiveTo date NULL
        );
    END;

    IF OBJECT_ID(N'dbo.AdministrativeWards', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AdministrativeWards
        (
            WardCode varchar(5) NOT NULL CONSTRAINT PK_AdministrativeWards PRIMARY KEY,
            ProvinceCode varchar(2) NOT NULL,
            WardName nvarchar(150) NOT NULL,
            WardType nvarchar(30) NOT NULL,
            IsActive bit NOT NULL CONSTRAINT DF_AdministrativeWards_IsActive DEFAULT(1),
            EffectiveFrom date NULL,
            EffectiveTo date NULL
        );
    END;

    IF COL_LENGTH(N'dbo.UserVerifications', N'PermanentProvinceCode') IS NULL ALTER TABLE dbo.UserVerifications ADD PermanentProvinceCode varchar(2) NULL;
    IF COL_LENGTH(N'dbo.UserVerifications', N'PermanentWardCode') IS NULL ALTER TABLE dbo.UserVerifications ADD PermanentWardCode varchar(5) NULL;
    IF COL_LENGTH(N'dbo.UserVerifications', N'CurrentProvinceCode') IS NULL ALTER TABLE dbo.UserVerifications ADD CurrentProvinceCode varchar(2) NULL;
    IF COL_LENGTH(N'dbo.UserVerifications', N'CurrentWardCode') IS NULL ALTER TABLE dbo.UserVerifications ADD CurrentWardCode varchar(5) NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Kiểm tra schema sau pha 1 trước khi seed dữ liệu.
IF OBJECT_ID(N'dbo.AdministrativeProvinces', N'U') IS NULL
    THROW 53117, N'Không tạo được bảng dbo.AdministrativeProvinces.', 1;
IF OBJECT_ID(N'dbo.AdministrativeWards', N'U') IS NULL
    THROW 53118, N'Không tạo được bảng dbo.AdministrativeWards.', 1;
IF COL_LENGTH(N'dbo.UserVerifications', N'PermanentProvinceCode') IS NULL
    THROW 53119, N'Không tạo được cột PermanentProvinceCode.', 1;
IF COL_LENGTH(N'dbo.UserVerifications', N'PermanentWardCode') IS NULL
    THROW 53120, N'Không tạo được cột PermanentWardCode.', 1;
IF COL_LENGTH(N'dbo.UserVerifications', N'CurrentProvinceCode') IS NULL
    THROW 53121, N'Không tạo được cột CurrentProvinceCode.', 1;
IF COL_LENGTH(N'dbo.UserVerifications', N'CurrentWardCode') IS NULL
    THROW 53122, N'Không tạo được cột CurrentWardCode.', 1;
GO

-- PHA 2: seed, ánh xạ dữ liệu cũ, tạo khóa ngoại/index và cập nhật phiên bản.
BEGIN TRY
    BEGIN TRANSACTION;
    CREATE TABLE #ProvinceSeed
    (
        ProvinceCode varchar(2) NOT NULL PRIMARY KEY,
        ProvinceName nvarchar(100) NOT NULL,
        ProvinceType nvarchar(30) NOT NULL,
        IsActive bit NOT NULL,
        EffectiveFrom date NULL
    );
    INSERT #ProvinceSeed(ProvinceCode,ProvinceName,ProvinceType,IsActive,EffectiveFrom) VALUES
        ('01',N'Hà Nội',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('04',N'Cao Bằng',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('08',N'Tuyên Quang',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('11',N'Điện Biên',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('12',N'Lai Châu',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('14',N'Sơn La',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('15',N'Lào Cai',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('19',N'Thái Nguyên',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('20',N'Lạng Sơn',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('22',N'Quảng Ninh',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('24',N'Bắc Ninh',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('25',N'Phú Thọ',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('31',N'Hải Phòng',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('33',N'Hưng Yên',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('37',N'Ninh Bình',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('38',N'Thanh Hoá',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('40',N'Nghệ An',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('42',N'Hà Tĩnh',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('44',N'Quảng Trị',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('46',N'Huế',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('48',N'Đà Nẵng',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('51',N'Quảng Ngãi',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('52',N'Gia Lai',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('56',N'Khánh Hoà',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('66',N'Đắk Lắk',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('68',N'Lâm Đồng',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('75',N'Đồng Nai',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('79',N'Hồ Chí Minh',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('80',N'Tây Ninh',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('82',N'Đồng Tháp',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('86',N'Vĩnh Long',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('91',N'An Giang',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date)),
        ('92',N'Cần Thơ',N'Thành phố',CAST(1 AS bit),CAST(NULL AS date)),
        ('96',N'Cà Mau',N'Tỉnh',CAST(1 AS bit),CAST(NULL AS date));

    CREATE TABLE #WardSeed
    (
        WardCode varchar(5) NOT NULL PRIMARY KEY,
        ProvinceCode varchar(2) NOT NULL,
        WardName nvarchar(150) NOT NULL,
        WardType nvarchar(30) NOT NULL,
        IsActive bit NOT NULL,
        EffectiveFrom date NULL
    );
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('00004','01',N'Ba Đình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00008','01',N'Ngọc Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00025','01',N'Giảng Võ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00070','01',N'Hoàn Kiếm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00082','01',N'Cửa Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00091','01',N'Phú Thượng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00097','01',N'Hồng Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00103','01',N'Tây Hồ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00118','01',N'Bồ Đề',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00127','01',N'Việt Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00136','01',N'Phúc Lợi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00145','01',N'Long Biên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00160','01',N'Nghĩa Đô',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00166','01',N'Cầu Giấy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00175','01',N'Yên Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00190','01',N'Ô Chợ Dừa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00199','01',N'Láng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00226','01',N'Văn Miếu - Quốc Tử Giám',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00229','01',N'Kim Liên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00235','01',N'Đống Đa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00256','01',N'Hai Bà Trưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00283','01',N'Vĩnh Tuy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00292','01',N'Bạch Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00301','01',N'Vĩnh Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00316','01',N'Định Công',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00322','01',N'Tương Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00328','01',N'Lĩnh Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00331','01',N'Hoàng Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00337','01',N'Hoàng Liệt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00340','01',N'Yên Sở',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00352','01',N'Phương Liệt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00364','01',N'Khương Đình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00367','01',N'Thanh Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00376','01',N'Sóc Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00382','01',N'Kim Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00385','01',N'Trung Giã',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00430','01',N'Đa Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00433','01',N'Nội Bài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00454','01',N'Đông Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00466','01',N'Phúc Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00475','01',N'Thư Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00493','01',N'Thiên Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00508','01',N'Vĩnh Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00541','01',N'Phù Đổng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00562','01',N'Thuận An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00565','01',N'Gia Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00577','01',N'Bát Tràng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00592','01',N'Từ Liêm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00598','01',N'Thượng Cát',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00602','01',N'Đông Ngạc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00611','01',N'Xuân Đỉnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00613','01',N'Tây Tựu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00619','01',N'Phú Diễn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00622','01',N'Xuân Phương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00634','01',N'Tây Mỗ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00637','01',N'Đại Mỗ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00640','01',N'Thanh Trì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00643','01',N'Thanh Liệt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00664','01',N'Đại Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00679','01',N'Ngọc Hồi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00685','01',N'Nam Phù',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04930','01',N'Yên Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08974','01',N'Quang Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08980','01',N'Yên Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08995','01',N'Tiến Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09022','01',N'Mê Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09552','01',N'Kiến Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09556','01',N'Hà Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09562','01',N'Yên Nghĩa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09568','01',N'Phú Lương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09574','01',N'Sơn Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09604','01',N'Tùng Thiện',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09616','01',N'Đoài Phương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09619','01',N'Quảng Oai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09634','01',N'Cổ Đô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09661','01',N'Minh Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09664','01',N'Vật Lại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09676','01',N'Bất Bạt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09694','01',N'Suối Hai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09700','01',N'Ba Vì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09706','01',N'Yên Bài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09715','01',N'Phúc Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09739','01',N'Phúc Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09772','01',N'Hát Môn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09784','01',N'Đan Phượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09787','01',N'Liên Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09817','01',N'Ô Diên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09832','01',N'Hoài Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09856','01',N'Dương Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09871','01',N'Sơn Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09877','01',N'An Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09886','01',N'Dương Nội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09895','01',N'Quốc Oai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09910','01',N'Kiều Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09931','01',N'Hưng Đạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09952','01',N'Phú Cát',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09955','01',N'Thạch Thất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09982','01',N'Hạ Bằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09988','01',N'Hoà Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10003','01',N'Tây Phương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10015','01',N'Chương Mỹ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10030','01',N'Phú Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10045','01',N'Xuân Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10072','01',N'Quảng Bị',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10081','01',N'Trần Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10096','01',N'Hoà Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10114','01',N'Thanh Oai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10126','01',N'Bình Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10144','01',N'Tam Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10180','01',N'Dân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10183','01',N'Thường Tín',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10210','01',N'Hồng Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10231','01',N'Thượng Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10237','01',N'Chương Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10273','01',N'Phú Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10279','01',N'Phượng Dực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10330','01',N'Chuyên Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10342','01',N'Đại Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10354','01',N'Vân Đình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10369','01',N'Ứng Thiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10402','01',N'Ứng Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10417','01',N'Hoà Xá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10441','01',N'Mỹ Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10459','01',N'Phúc Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10465','01',N'Hồng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10489','01',N'Hương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01273','04',N'Thục Phán',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('01279','04',N'Nùng Trí Cao',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('01288','04',N'Tân Giang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('01290','04',N'Bảo Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01294','04',N'Lý Bôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01297','04',N'Nam Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01304','04',N'Quảng Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01318','04',N'Yên Thổ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01321','04',N'Bảo Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01324','04',N'Cốc Pàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01327','04',N'Cô Ba',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01336','04',N'Khánh Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01339','04',N'Xuân Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01351','04',N'Hưng Đạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01354','04',N'Huy Giáp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01360','04',N'Sơn Lộ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01363','04',N'Thông Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01366','04',N'Cần Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01387','04',N'Thanh Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01392','04',N'Trường Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01393','04',N'Lũng Nặm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01414','04',N'Tổng Cọt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01438','04',N'Hà Quảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01447','04',N'Trà Lĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01456','04',N'Quang Hán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01465','04',N'Quang Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01477','04',N'Trùng Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01489','04',N'Đình Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01501','04',N'Đàm Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01525','04',N'Đoài Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01537','04',N'Lý Quốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01552','04',N'Quang Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01558','04',N'Hạ Lang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01561','04',N'Vinh Quý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01576','04',N'Quảng Uyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01594','04',N'Độc Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01618','04',N'Hạnh Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01636','04',N'Bế Văn Đàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01648','04',N'Phục Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01654','04',N'Hoà An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01660','04',N'Nam Tuấn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01699','04',N'Nguyễn Huệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01708','04',N'Bạch Đằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01726','04',N'Nguyên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01729','04',N'Tĩnh Túc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01738','04',N'Ca Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01747','04',N'Minh Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01768','04',N'Phan Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01774','04',N'Tam Kim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01777','04',N'Thành Công',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01786','04',N'Đông Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01789','04',N'Canh Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01792','04',N'Kim Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01795','04',N'Minh Khai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01807','04',N'Thạch An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01822','04',N'Đức Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00691','08',N'Hà Giang 2',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00694','08',N'Hà Giang 1',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('00700','08',N'Ngọc Đường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00706','08',N'Phú Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00715','08',N'Lũng Cú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00721','08',N'Đồng Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00733','08',N'Sà Phìn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00745','08',N'Phố Bảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00763','08',N'Lũng Phìn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00769','08',N'Mèo Vạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00778','08',N'Sơn Vĩ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00787','08',N'Sủng Máng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00802','08',N'Khâu Vai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00808','08',N'Tát Ngà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00817','08',N'Niêm Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00820','08',N'Yên Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00829','08',N'Thắng Mố',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00832','08',N'Bạch Đích',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00847','08',N'Mậu Duệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00859','08',N'Ngọc Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00865','08',N'Đường Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00871','08',N'Du Già',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00874','08',N'Quản Bạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00883','08',N'Cán Tỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00889','08',N'Nghĩa Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00892','08',N'Tùng Vài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00901','08',N'Lùng Tám',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00913','08',N'Vị Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00919','08',N'Minh Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00922','08',N'Thuận Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00925','08',N'Tùng Bá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00928','08',N'Thanh Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00937','08',N'Lao Chải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00952','08',N'Cao Bồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00958','08',N'Thượng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00967','08',N'Việt Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00970','08',N'Linh Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00976','08',N'Bạch Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00982','08',N'Minh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00985','08',N'Giáp Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00991','08',N'Bắc Mê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('00994','08',N'Minh Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01006','08',N'Yên Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01012','08',N'Đường Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01021','08',N'Hoàng Su Phì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01024','08',N'Bản Máy',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01033','08',N'Thàng Tín',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01051','08',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01057','08',N'Pờ Ly Ngài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01075','08',N'Nậm Dịch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01084','08',N'Hồ Thầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01090','08',N'Thông Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01096','08',N'Pà Vầy Sủ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01108','08',N'Xín Mần',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01117','08',N'Trung Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01141','08',N'Nấm Dẩn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01144','08',N'Quảng Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01147','08',N'Khuôn Lùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01153','08',N'Bắc Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01156','08',N'Vĩnh Tuy',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01165','08',N'Đồng Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01171','08',N'Tân Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01180','08',N'Bằng Hành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01192','08',N'Liên Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01201','08',N'Hùng An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01216','08',N'Đồng Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01225','08',N'Tiên Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01234','08',N'Yên Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01237','08',N'Quang Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01243','08',N'Tân Trịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01246','08',N'Bằng Lang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01255','08',N'Xuân Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01261','08',N'Tiên Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02212','08',N'Nông Tiến',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02215','08',N'Minh Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02221','08',N'Nà Hang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02239','08',N'Thượng Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02245','08',N'Côn Lôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02248','08',N'Yên Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02260','08',N'Hồng Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02266','08',N'Lâm Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02269','08',N'Thượng Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02287','08',N'Chiêm Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02296','08',N'Bình An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02302','08',N'Minh Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02305','08',N'Trung Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02308','08',N'Tân Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02317','08',N'Yên Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02320','08',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02332','08',N'Kiên Đài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02350','08',N'Kim Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02353','08',N'Hoà An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02359','08',N'Tri Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02365','08',N'Yên Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02374','08',N'Hàm Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02380','08',N'Bạch Xa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02392','08',N'Phù Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02398','08',N'Yên Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02404','08',N'Bình Xa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02407','08',N'Thái Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02419','08',N'Thái Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02425','08',N'Hùng Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02434','08',N'Lực Hành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02437','08',N'Kiến Thiết',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02449','08',N'Xuân Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02455','08',N'Hùng Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02458','08',N'Trung Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02470','08',N'Tân Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02473','08',N'Yên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02494','08',N'Thái Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02509','08',N'Mỹ Lâm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02512','08',N'An Tường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02524','08',N'Bình Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02530','08',N'Nhữ Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02536','08',N'Sơn Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02545','08',N'Tân Trào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02548','08',N'Bình Ca',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02554','08',N'Minh Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02572','08',N'Đông Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02578','08',N'Tân Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02608','08',N'Hồng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02611','08',N'Phú Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02620','08',N'Sơn Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02623','08',N'Trường Sinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03127','11',N'Điện Biên Phủ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03151','11',N'Mường Lay',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03158','11',N'Sín Thầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03160','11',N'Mường Nhé',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03162','11',N'Nậm Kè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03163','11',N'Mường Toong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03164','11',N'Quảng Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03166','11',N'Mường Chà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03169','11',N'Nà Hỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03172','11',N'Na Sang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03175','11',N'Chà Tở',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03176','11',N'Nà Bủng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03181','11',N'Mường Tùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03193','11',N'Pa Ham',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03194','11',N'Nậm Nèn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03199','11',N'Si Pa Phìn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03202','11',N'Mường Pồn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03203','11',N'Na Son',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03208','11',N'Xa Dung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03214','11',N'Mường Luân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03217','11',N'Tủa Chùa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03220','11',N'Tủa Thàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03226','11',N'Sín Chải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03241','11',N'Sính Phình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03244','11',N'Sáng Nhè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03253','11',N'Tuần Giáo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03256','11',N'Mường Ảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03260','11',N'Pú Nhung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03268','11',N'Mường Mùn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03283','11',N'Chiềng Sinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03295','11',N'Quài Tở',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03301','11',N'Búng Lao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03313','11',N'Mường Lạn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03316','11',N'Nà Tấu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03325','11',N'Mường Phăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03328','11',N'Thanh Nưa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03334','11',N'Mường Thanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03349','11',N'Thanh Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03352','11',N'Thanh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03356','11',N'Sam Mứn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03358','11',N'Núa Ngam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03368','11',N'Mường Nhà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03370','11',N'Pu Nhi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03382','11',N'Phình Giàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03385','11',N'Tìa Dình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03388','12',N'Đoàn Kết',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03390','12',N'Bình Lư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03394','12',N'Sin Suối Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03405','12',N'Tả Lèng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03408','12',N'Tân Phong',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03424','12',N'Bản Bo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03430','12',N'Khun Há',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03433','12',N'Bum Tở',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03434','12',N'Nậm Hàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03439','12',N'Thu Lũm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03442','12',N'Pa Ủ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03445','12',N'Mường Tè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03451','12',N'Mù Cả',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03460','12',N'Hua Bum',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03463','12',N'Tà Tổng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03466','12',N'Bum Nưa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03472','12',N'Mường Mô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03478','12',N'Sìn Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03487','12',N'Lê Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03503','12',N'Pa Tần',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03508','12',N'Hồng Thu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03517','12',N'Nậm Tăm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03529','12',N'Tủa Sín Chải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03532','12',N'Pu Sam Cáp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03538','12',N'Nậm Mạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03544','12',N'Nậm Cuổi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03549','12',N'Phong Thổ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03562','12',N'Sì Lở Lầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03571','12',N'Dào San',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03583','12',N'Khổng Lào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03595','12',N'Than Uyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03598','12',N'Tân Uyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03601','12',N'Mường Khoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03613','12',N'Nậm Sỏ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03616','12',N'Pắc Ta',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03618','12',N'Mường Than',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03637','12',N'Mường Kim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03640','12',N'Khoen On',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03646','14',N'Tô Hiệu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03664','14',N'Chiềng An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03670','14',N'Chiềng Cơi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03679','14',N'Chiềng Sinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03688','14',N'Mường Chiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03694','14',N'Mường Giôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03703','14',N'Quỳnh Nhai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03712','14',N'Mường Sại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03721','14',N'Thuận Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03724','14',N'Bình Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03727','14',N'Mường É',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03754','14',N'Chiềng La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03757','14',N'Mường Khiêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03760','14',N'Mường Bám',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03763','14',N'Long Hẹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03781','14',N'Co Mạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03784','14',N'Nậm Lầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03799','14',N'Muổi Nọi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03808','14',N'Mường La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03814','14',N'Chiềng Lao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03820','14',N'Ngọc Chiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03847','14',N'Mường Bú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03850','14',N'Chiềng Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03856','14',N'Bắc Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03862','14',N'Xím Vàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03868','14',N'Tà Xùa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03871','14',N'Pắc Ngà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03880','14',N'Tạ Khoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03892','14',N'Chiềng Sại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03901','14',N'Suối Tọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03907','14',N'Mường Cơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03910','14',N'Phù Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03922','14',N'Gia Phù',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03943','14',N'Mường Bang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03958','14',N'Tường Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03961','14',N'Kim Bon',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03970','14',N'Tân Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03979','14',N'Mộc Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03980','14',N'Mộc Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03982','14',N'Thảo Nguyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03985','14',N'Chiềng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03997','14',N'Tân Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04000','14',N'Đoàn Kết',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04006','14',N'Song Khủa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04018','14',N'Tô Múa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04033','14',N'Vân Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04045','14',N'Lóng Sập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04048','14',N'Vân Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04057','14',N'Xuân Nha',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04075','14',N'Yên Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04078','14',N'Chiềng Hặc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04087','14',N'Yên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04096','14',N'Lóng Phiêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04099','14',N'Phiêng Khoài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04105','14',N'Mai Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04108','14',N'Chiềng Sung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04117','14',N'Mường Chanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04123','14',N'Chiềng Mung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04132','14',N'Chiềng Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04136','14',N'Tà Hộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04144','14',N'Phiêng Cằm',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('04159','14',N'Phiêng Pằn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04168','14',N'Sông Mã',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04171','14',N'Bó Sinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04183','14',N'Mường Lầm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04186','14',N'Nậm Ty',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04195','14',N'Chiềng Sơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04204','14',N'Chiềng Khoong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04210','14',N'Huổi Một',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04219','14',N'Mường Hung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04222','14',N'Chiềng Khương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04228','14',N'Púng Bánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04231','14',N'Sốp Cộp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04240','14',N'Mường Lèo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04246','14',N'Mường Lạn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02647','15',N'Lào Cai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02671','15',N'Cam Đường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('02680','15',N'Hợp Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02683','15',N'Bát Xát',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02686','15',N'A Mú Sung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02695','15',N'Trịnh Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02701','15',N'Y Tý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02707','15',N'Dền Sáng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02725','15',N'Bản Xèo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02728','15',N'Mường Hum',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02746','15',N'Cốc San',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02752','15',N'Pha Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02761','15',N'Mường Khương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02782','15',N'Cao Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02788','15',N'Bản Lầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02809','15',N'Si Ma Cai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02824','15',N'Sín Chéng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02839','15',N'Bắc Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02842','15',N'Tả Củ Tỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02848','15',N'Lùng Phình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02869','15',N'Bản Liền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02890','15',N'Bảo Nhai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02896','15',N'Cốc Lầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02902','15',N'Phong Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02905','15',N'Bảo Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02908','15',N'Tằng Loỏng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02923','15',N'Gia Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02926','15',N'Xuân Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02947','15',N'Bảo Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02953','15',N'Nghĩa Đô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02962','15',N'Xuân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02968','15',N'Thượng Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02989','15',N'Bảo Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02998','15',N'Phúc Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03004','15',N'Ngũ Chỉ Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03006','15',N'Sa Pa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('03013','15',N'Tả Phìn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03037','15',N'Tả Van',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03043','15',N'Mường Bo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03046','15',N'Bản Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03061','15',N'Võ Lao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03076','15',N'Nậm Chày',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03082','15',N'Văn Bàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03085','15',N'Nậm Xé',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03091','15',N'Chiềng Ken',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03103','15',N'Khánh Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03106','15',N'Dương Quỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('03121','15',N'Minh Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04252','15',N'Yên Bái',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04273','15',N'Nam Cường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04279','15',N'Văn Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04288','15',N'Nghĩa Lộ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04303','15',N'Lục Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04309','15',N'Lâm Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04336','15',N'Tân Lĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04342','15',N'Khánh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04345','15',N'Mường Lai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04363','15',N'Phúc Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04375','15',N'Mậu A',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04381','15',N'Lâm Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04387','15',N'Châu Quế',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04399','15',N'Đông Cuông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04402','15',N'Phong Dụ Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04423','15',N'Phong Dụ Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04429','15',N'Tân Hợp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04441','15',N'Xuân Ái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04450','15',N'Mỏ Vàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04456','15',N'Mù Cang Chải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04462','15',N'Nậm Có',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04465','15',N'Khao Mang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04474','15',N'Lao Chải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04489','15',N'Chế Tạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04492','15',N'Púng Luông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04498','15',N'Trấn Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04531','15',N'Quy Mông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04537','15',N'Lương Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04543','15',N'Âu Lâu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04564','15',N'Việt Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04576','15',N'Hưng Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04585','15',N'Hạnh Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04603','15',N'Tà Xi Láng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04606','15',N'Trạm Tấu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04609','15',N'Phình Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04630','15',N'Tú Lệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04636','15',N'Gia Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04651','15',N'Sơn Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04660','15',N'Liên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04663','15',N'Trung Tâm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04672','15',N'Văn Chấn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04681','15',N'Cầu Thia',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04693','15',N'Cát Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04699','15',N'Chấn Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04705','15',N'Thượng Bằng La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04711','15',N'Nghĩa Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04714','15',N'Yên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04717','15',N'Thác Bà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04726','15',N'Cảm Nhân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04744','15',N'Yên Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04750','15',N'Bảo Ái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01840','19',N'Đức Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('01843','19',N'Bắc Kạn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('01849','19',N'Phong Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01864','19',N'Bằng Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01879','19',N'Cao Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01882','19',N'Nghiên Loan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01894','19',N'Phúc Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01906','19',N'Ba Bể',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01912','19',N'Chợ Rã',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01921','19',N'Thượng Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01933','19',N'Đồng Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01936','19',N'Nà Phặc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01942','19',N'Bằng Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01954','19',N'Ngân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01957','19',N'Thượng Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01960','19',N'Hiệp Lực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01969','19',N'Phủ Thông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('01981','19',N'Vĩnh Thông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02008','19',N'Cẩm Giàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02014','19',N'Bạch Thông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02020','19',N'Chợ Đồn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02026','19',N'Nam Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02038','19',N'Quảng Bạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02044','19',N'Yên Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02071','19',N'Nghĩa Tá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02083','19',N'Yên Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02086','19',N'Chợ Mới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02101','19',N'Thanh Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02104','19',N'Tân Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02107','19',N'Thanh Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02116','19',N'Yên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02143','19',N'Văn Lang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02152','19',N'Cường Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02155','19',N'Na Rì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02176','19',N'Trần Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02185','19',N'Côn Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('02191','19',N'Xuân Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05443','19',N'Phan Đình Phùng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05455','19',N'Quyết Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05467','19',N'Gia Sàng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05482','19',N'Quan Triều',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05488','19',N'Đại Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05500','19',N'Tích Lương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05503','19',N'Tân Cương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05518','19',N'Sông Công',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05528','19',N'Bách Quang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05533','19',N'Bá Xuyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05542','19',N'Lam Vỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05551','19',N'Kim Phượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05563','19',N'Phượng Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05569','19',N'Định Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05581','19',N'Trung Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05587','19',N'Bình Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05602','19',N'Phú Đình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05605','19',N'Bình Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05611','19',N'Phú Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05620','19',N'Yên Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05632','19',N'Hợp Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05641','19',N'Vô Tranh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05662','19',N'Trại Cau',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05665','19',N'Văn Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05674','19',N'Quang Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05680','19',N'Văn Hán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05692','19',N'Đồng Hỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05707','19',N'Nam Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05710','19',N'Linh Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05716','19',N'Võ Nhai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05719','19',N'Sảng Mộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05722','19',N'Nghinh Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05725','19',N'Thần Sa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05740','19',N'La Hiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05746','19',N'Tràng Xá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05755','19',N'Dân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05773','19',N'Phú Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05776','19',N'Đức Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05788','19',N'Phú Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05800','19',N'Phú Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05809','19',N'An Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05818','19',N'La Bằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05830','19',N'Đại Từ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05845','19',N'Vạn Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05851','19',N'Quân Chu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05857','19',N'Phúc Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05860','19',N'Phổ Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05881','19',N'Thành Công',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05890','19',N'Vạn Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05899','19',N'Trung Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05908','19',N'Phú Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05917','19',N'Tân Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05923','19',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05941','19',N'Điềm Thuỵ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05953','19',N'Kha Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05977','20',N'Đông Kinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05983','20',N'Lương Văn Tri',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('05986','20',N'Tam Thanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06001','20',N'Đoàn Kết',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06004','20',N'Quốc Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06019','20',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06037','20',N'Kháng Chiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06040','20',N'Thất Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06046','20',N'Tràng Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06058','20',N'Quốc Việt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06073','20',N'Hoa Thám',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06076','20',N'Quý Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06079','20',N'Hồng Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06085','20',N'Thiện Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05128','25',N'Tân Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06091','20',N'Thiện Thuật',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06103','20',N'Thiện Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06112','20',N'Bình Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06115','20',N'Tân Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06124','20',N'Na Sầm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06148','20',N'Thuỵ Hùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06151','20',N'Hội Hoan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06154','20',N'Văn Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06172','20',N'Hoàng Văn Thụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06184','20',N'Đồng Đăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06187','20',N'Kỳ Lừa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06196','20',N'Ba Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06211','20',N'Cao Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06220','20',N'Công Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06253','20',N'Văn Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06280','20',N'Điềm He',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06286','20',N'Khánh Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06298','20',N'Yên Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06313','20',N'Tri Lễ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06316','20',N'Tân Đoàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06325','20',N'Bắc Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06337','20',N'Tân Tri',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06349','20',N'Hưng Vũ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06364','20',N'Vũ Lễ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06367','20',N'Vũ Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06376','20',N'Nhất Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06385','20',N'Hữu Lũng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06391','20',N'Yên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06400','20',N'Hữu Liên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06415','20',N'Vân Nham',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06427','20',N'Cai Kinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06436','20',N'Thiện Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06445','20',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06457','20',N'Tuấn Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06463','20',N'Chi Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06475','20',N'Bằng Mạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06481','20',N'Chiến Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06496','20',N'Nhân Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06505','20',N'Vạn Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06517','20',N'Quan Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06526','20',N'Na Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06529','20',N'Lộc Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06541','20',N'Mẫu Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06565','20',N'Khuất Xá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06577','20',N'Thống Nhất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06601','20',N'Lợi Bác',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06607','20',N'Xuân Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06613','20',N'Đình Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06616','20',N'Thái Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06625','20',N'Kiên Mộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06637','20',N'Châu Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06652','22',N'Hà Tu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06658','22',N'Cao Xanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06661','22',N'Việt Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06673','22',N'Bãi Cháy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06676','22',N'Hà Lầm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06685','22',N'Hồng Gai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06688','22',N'Hạ Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06706','22',N'Tuần Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06709','22',N'Móng Cái 2',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06712','22',N'Móng Cái 1',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06724','22',N'Hải Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06733','22',N'Hải Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06736','22',N'Móng Cái 3',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06757','22',N'Vĩnh Thực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06760','22',N'Mông Dương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06778','22',N'Quang Hanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06781','22',N'Cửa Ông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06793','22',N'Cẩm Phả',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06799','22',N'Hải Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06811','22',N'Uông Bí',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06820','22',N'Vàng Danh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06832','22',N'Yên Tử',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('06838','22',N'Bình Liêu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06841','22',N'Hoành Mô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06856','22',N'Lục Hồn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06862','22',N'Tiên Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06874','22',N'Điền Xá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06877','22',N'Đông Ngũ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06886','22',N'Hải Lạng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06895','22',N'Đầm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06913','22',N'Quảng Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06922','22',N'Quảng Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06931','22',N'Quảng Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06946','22',N'Đường Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06967','22',N'Cái Chiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06978','22',N'Ba Chẽ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06979','22',N'Kỳ Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06985','22',N'Lương Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('06994','22',N'Vân Đồn',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('07030','22',N'Hoành Bồ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07054','22',N'Quảng La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07060','22',N'Thống Nhất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07069','22',N'Mạo Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07081','22',N'Bình Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07090','22',N'An Sinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07093','22',N'Đông Triều',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07114','22',N'Hoàng Quế',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07132','22',N'Quảng Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07135','22',N'Đông Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07147','22',N'Hiệp Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07168','22',N'Hà An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07180','22',N'Liên Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07183','22',N'Phong Cốc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07192','22',N'Cô Tô',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('07210','24',N'Bắc Giang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07228','24',N'Đa Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07246','24',N'Xuân Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07264','24',N'Tam Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07282','24',N'Đồng Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07288','24',N'Yên Thế',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07294','24',N'Bố Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07306','24',N'Nhã Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07330','24',N'Phúc Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07333','24',N'Quang Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07339','24',N'Tân Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07351','24',N'Ngọc Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07375','24',N'Lạng Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07381','24',N'Tiên Lục',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07399','24',N'Kép',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07420','24',N'Mỹ Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07432','24',N'Tân Dĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07444','24',N'Lục Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07450','24',N'Đông Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07462','24',N'Bảo Đài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07486','24',N'Nghĩa Phương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07489','24',N'Trường Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07492','24',N'Lục Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07498','24',N'Bắc Lũng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07519','24',N'Cẩm Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07525','24',N'Chũ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07531','24',N'Tân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07534','24',N'Sa Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07537','24',N'Biên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07543','24',N'Sơn Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07552','24',N'Kiên Lao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07573','24',N'Biển Động',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07582','24',N'Lục Ngạn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07594','24',N'Đèo Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07603','24',N'Nam Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07612','24',N'Phượng Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07615','24',N'Sơn Động',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07616','24',N'Tây Yên Tử',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07621','24',N'Vân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07627','24',N'Đại Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07642','24',N'Yên Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07654','24',N'An Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07663','24',N'Tuấn Đạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07672','24',N'Dương Hưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07681','24',N'Yên Dũng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07682','24',N'Tân An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12619','33',N'Diên Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07696','24',N'Tiền Phong',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07699','24',N'Tân Tiến',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07735','24',N'Đồng Việt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07738','24',N'Cảnh Thuỵ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07774','24',N'Tự Lạn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07777','24',N'Việt Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07795','24',N'Nếnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07798','24',N'Vân Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07822','24',N'Hoàng Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07840','24',N'Hiệp Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07864','24',N'Hợp Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07870','24',N'Xuân Cẩm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09169','24',N'Vũ Ninh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09187','24',N'Kinh Bắc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09190','24',N'Võ Cường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09193','24',N'Yên Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09202','24',N'Tam Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09205','24',N'Yên Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09208','24',N'Tam Đa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09238','24',N'Văn Môn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09247','24',N'Quế Võ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09253','24',N'Nhân Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09265','24',N'Phương Liễu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09286','24',N'Nam Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09292','24',N'Phù Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09295','24',N'Bồng Lai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09301','24',N'Đào Viên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09313','24',N'Chi Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09319','24',N'Tiên Du',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09325','24',N'Hạp Lĩnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09334','24',N'Liên Bão',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09340','24',N'Đại Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09343','24',N'Tân Chi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09349','24',N'Phật Tích',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09367','24',N'Từ Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09370','24',N'Tam Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09379','24',N'Phù Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09385','24',N'Đồng Nguyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09400','24',N'Thuận Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09409','24',N'Mão Điền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09427','24',N'Trí Quả',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09430','24',N'Trạm Lộ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09433','24',N'Song Liễu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09445','24',N'Ninh Xá',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('09454','24',N'Gia Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09466','24',N'Cao Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09469','24',N'Đại Lai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09475','24',N'Nhân Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09487','24',N'Đông Cứu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09496','24',N'Lương Tài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09499','24',N'Trung Kênh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09523','24',N'Trung Chính',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09529','24',N'Lâm Thao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04792','25',N'Tân Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04795','25',N'Hoà Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04828','25',N'Thống Nhất',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04831','25',N'Đà Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04846','25',N'Đức Nhàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04849','25',N'Tân Pheo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04873','25',N'Quy Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04876','25',N'Cao Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04891','25',N'Tiền Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04894','25',N'Kỳ Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('04897','25',N'Thịnh Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04924','25',N'Lương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04960','25',N'Liên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04978','25',N'Kim Bôi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('04990','25',N'Nật Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05014','25',N'Mường Động',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05047','25',N'Cao Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05068','25',N'Hợp Kim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05086','25',N'Dũng Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05089','25',N'Cao Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05092','25',N'Thung Nai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05116','25',N'Mường Thàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05134','25',N'Mường Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05152','25',N'Vân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05158','25',N'Mường Bi',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('05191','25',N'Toàn Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05200','25',N'Mai Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05206','25',N'Tân Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05212','25',N'Pà Cò',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05245','25',N'Bao La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05251','25',N'Mai Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05266','25',N'Lạc Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05287','25',N'Mường Vang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05290','25',N'Nhân Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05293','25',N'Thượng Cốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05305','25',N'Yên Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05323','25',N'Quyết Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05329','25',N'Ngọc Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05347','25',N'Đại Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05353','25',N'Yên Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05362','25',N'Lạc Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05386','25',N'Yên Trị',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05392','25',N'Lạc Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05395','25',N'An Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('05425','25',N'An Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07894','25',N'Nông Trang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07900','25',N'Việt Trì',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07909','25',N'Thanh Miếu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07918','25',N'Vân Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07942','25',N'Phú Thọ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07948','25',N'Âu Cơ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07954','25',N'Phong Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('07969','25',N'Đoan Hùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07996','25',N'Bằng Luân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('07999','25',N'Chí Đám',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08023','25',N'Tây Cốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08038','25',N'Chân Mộng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08053','25',N'Hạ Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08071','25',N'Đan Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08110','25',N'Hiền Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08113','25',N'Yên Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08134','25',N'Văn Lang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08143','25',N'Vĩnh Chân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08152','25',N'Thanh Ba',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08173','25',N'Quảng Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08203','25',N'Hoàng Cương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08209','25',N'Đông Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08218','25',N'Chí Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08227','25',N'Liên Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08230','25',N'Phù Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08236','25',N'Phú Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08245','25',N'Trạm Thản',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08254','25',N'Dân Chủ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08275','25',N'Bình Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08290','25',N'Yên Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08296','25',N'Sơn Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08305','25',N'Xuân Viên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08311','25',N'Trung Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08323','25',N'Thượng Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08338','25',N'Minh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08341','25',N'Cẩm Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08344','25',N'Tiên Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08377','25',N'Vân Bán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08398','25',N'Phú Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08416','25',N'Hùng Việt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08431','25',N'Đồng Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08434','25',N'Tam Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08443','25',N'Hiền Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08467','25',N'Vạn Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08479','25',N'Thọ Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08494','25',N'Lâm Thao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08500','25',N'Xuân Lũng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08515','25',N'Hy Cương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08521','25',N'Phùng Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08527','25',N'Bản Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08542','25',N'Thanh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08545','25',N'Thu Cúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08560','25',N'Lai Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08566','25',N'Tân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08584','25',N'Võ Miếu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08590','25',N'Xuân Đài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08593','25',N'Minh Đài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08611','25',N'Văn Miếu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08614','25',N'Cự Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08620','25',N'Long Cốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08632','25',N'Hương Cần',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08635','25',N'Khả Cửu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08656','25',N'Yên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08662','25',N'Đào Xá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08674','25',N'Thanh Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08686','25',N'Tu Vũ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08707','25',N'Vĩnh Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('08716','25',N'Vĩnh Phúc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('08740','25',N'Phúc Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('08746','25',N'Xuân Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('08761','25',N'Lập Thạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08770','25',N'Hợp Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08773','25',N'Yên Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08782','25',N'Hải Lựu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08788','25',N'Thái Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08812','25',N'Liên Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08824','25',N'Tam Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08842','25',N'Tiên Lữ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08848','25',N'Sông Lô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08866','25',N'Sơn Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08869','25',N'Tam Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08872','25',N'Tam Dương Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08896','25',N'Hoàng An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08905','25',N'Hội Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08911','25',N'Tam Đảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08914','25',N'Đạo Trù',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08923','25',N'Đại Đình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08935','25',N'Bình Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08944','25',N'Bình Tuyền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08950','25',N'Bình Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('08971','25',N'Xuân Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09025','25',N'Yên Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09040','25',N'Tề Lỗ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09043','25',N'Tam Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09052','25',N'Nguyệt Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09064','25',N'Liên Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09076','25',N'Vĩnh Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09079','25',N'Vĩnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09100','25',N'Vĩnh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09106','25',N'Vĩnh Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09112','25',N'Thổ Tang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('09154','25',N'Vĩnh Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10507','31',N'Thành Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10525','31',N'Hải Dương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10532','31',N'Lê Thanh Nghị',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10537','31',N'Tân Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10543','31',N'Việt Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10546','31',N'Chí Linh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10549','31',N'Chu Văn An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10552','31',N'Nguyễn Trãi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10570','31',N'Trần Hưng Đạo',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10573','31',N'Trần Nhân Tông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10603','31',N'Lê Đại Hành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10606','31',N'Nam Sách',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10615','31',N'Hợp Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10633','31',N'Trần Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10642','31',N'Thái Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10645','31',N'An Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10660','31',N'Ái Quốc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10675','31',N'Kinh Môn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10678','31',N'Bắc An Phụ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10705','31',N'Nam An Phụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10714','31',N'Nhị Chiểu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10726','31',N'Phạm Sư Mạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10729','31',N'Trần Liễu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10744','31',N'Nguyễn Đại Năng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10750','31',N'Phú Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10756','31',N'Lai Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10792','31',N'An Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10804','31',N'Kim Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10813','31',N'Thanh Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10816','31',N'Hà Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10837','31',N'Nam Đồng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10843','31',N'Hà Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10846','31',N'Hà Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10882','31',N'Hà Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10888','31',N'Cẩm Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10891','31',N'Tứ Minh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('10903','31',N'Cẩm Giàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10909','31',N'Tuệ Tĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10930','31',N'Mao Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10945','31',N'Kẻ Sặt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10966','31',N'Bình Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10972','31',N'Đường An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10993','31',N'Thượng Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('10999','31',N'Gia Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11002','31',N'Thạch Khôi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11020','31',N'Yết Kiêu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11050','31',N'Gia Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11065','31',N'Trường Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11074','31',N'Tứ Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11086','31',N'Đại Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11113','31',N'Tân Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11131','31',N'Chí Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11140','31',N'Lạc Phượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11146','31',N'Nguyên Giáp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11164','31',N'Vĩnh Lại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11167','31',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11203','31',N'Ninh Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11218','31',N'Hồng Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11224','31',N'Khúc Thừa Dụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11239','31',N'Thanh Miện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11242','31',N'Nguyễn Lương Bằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11254','31',N'Bắc Thanh Miện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11257','31',N'Hải Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11284','31',N'Nam Thanh Miện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11311','31',N'Hồng Bàng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11329','31',N'Ngô Quyền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11359','31',N'Gia Viên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11383','31',N'Lê Chân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11407','31',N'An Biên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11411','31',N'Đông Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11413','31',N'Hải An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11443','31',N'Kiến An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11446','31',N'Phù Liễn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11455','31',N'Đồ Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11473','31',N'Bạch Đằng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11488','31',N'Lưu Kiếm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11503','31',N'Việt Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11506','31',N'Lê Ích Mộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11533','31',N'Hoà Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11542','31',N'Nam Triệu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11557','31',N'Thiên Hương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11560','31',N'Thuỷ Nguyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11581','31',N'An Dương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11593','31',N'An Phong',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11602','31',N'Hồng An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11617','31',N'An Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11629','31',N'An Lão',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11635','31',N'An Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11647','31',N'An Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11668','31',N'An Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11674','31',N'An Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11680','31',N'Kiến Thuỵ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11689','31',N'Hưng Đạo',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11692','31',N'Dương Kinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11713','31',N'Nghi Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11725','31',N'Kiến Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11728','31',N'Kiến Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11737','31',N'Nam Đồ Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11749','31',N'Kiến Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11755','31',N'Tiên Lãng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11761','31',N'Quyết Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11779','31',N'Tân Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11791','31',N'Tiên Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11806','31',N'Chấn Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11809','31',N'Hùng Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11824','31',N'Vĩnh Bảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11836','31',N'Vĩnh Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11842','31',N'Vĩnh Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11848','31',N'Vĩnh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11875','31',N'Vĩnh Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11887','31',N'Vĩnh Am',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11911','31',N'Nguyễn Bỉnh Khiêm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11914','31',N'Cát Hải',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('11948','31',N'Bạch Long Vĩ',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('11953','33',N'Phố Hiến',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11977','33',N'Tân Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11980','33',N'Hồng Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11983','33',N'Sơn Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('11992','33',N'Lạc Đạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('11995','33',N'Đại Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12004','33',N'Như Quỳnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12019','33',N'Văn Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12025','33',N'Phụng Công',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12031','33',N'Nghĩa Trụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12049','33',N'Mễ Sở',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12064','33',N'Nguyễn Văn Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12070','33',N'Hoàn Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12073','33',N'Yên Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12091','33',N'Việt Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12103','33',N'Mỹ Hào',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12127','33',N'Thượng Hồng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12133','33',N'Đường Hào',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12142','33',N'Ân Thi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12148','33',N'Phạm Ngũ Lão',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12166','33',N'Xuân Trúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12184','33',N'Nguyễn Trãi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12196','33',N'Hồng Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12205','33',N'Khoái Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12223','33',N'Triệu Việt Vương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12238','33',N'Việt Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12247','33',N'Châu Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12271','33',N'Chí Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12280','33',N'Lương Bằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12286','33',N'Nghĩa Dân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12313','33',N'Đức Hợp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12322','33',N'Hiệp Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12337','33',N'Hoàng Hoa Thám',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12361','33',N'Tiên Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12364','33',N'Tiên Lữ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12391','33',N'Quang Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12406','33',N'Đoàn Đào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12424','33',N'Tiên Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12427','33',N'Tống Trân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12452','33',N'Trần Hưng Đạo',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12454','33',N'Trần Lãm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12466','33',N'Vũ Phúc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12472','33',N'Quỳnh Phụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12499','33',N'A Sào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12511','33',N'Minh Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12517','33',N'Ngọc Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12523','33',N'Phụ Dực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12526','33',N'Đồng Bằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12532','33',N'Nguyễn Du',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12577','33',N'Quỳnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12583','33',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12586','33',N'Hưng Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12595','33',N'Ngự Thiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12613','33',N'Long Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12631','33',N'Thần Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12634','33',N'Tiên La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12676','33',N'Lê Quý Đôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12685','33',N'Hồng Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12688','33',N'Đông Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12694','33',N'Bắc Đông Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12700','33',N'Bắc Tiên Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12736','33',N'Đông Tiên Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12745','33',N'Bắc Đông Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12754','33',N'Tiên Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12763','33',N'Nam Tiên Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12775','33',N'Nam Đông Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12793','33',N'Đông Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12817','33',N'Trà Lý',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('12826','33',N'Thái Thuỵ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12850','33',N'Tây Thuỵ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12859','33',N'Bắc Thuỵ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12862','33',N'Đông Thuỵ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12865','33',N'Thuỵ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12904','33',N'Nam Thuỵ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12916','33',N'Bắc Thái Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12919','33',N'Tây Thái Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12922','33',N'Thái Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12943','33',N'Đông Thái Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12961','33',N'Nam Thái Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12970','33',N'Tiền Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('12988','33',N'Đông Tiền Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13003','33',N'Đồng Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13021','33',N'Ái Quốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13039','33',N'Tây Tiền Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13057','33',N'Nam Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13063','33',N'Nam Tiền Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13066','33',N'Hưng Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13075','33',N'Kiến Xương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13093','33',N'Trà Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13096','33',N'Bình Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13120','33',N'Lê Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13132','33',N'Quang Lịch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13141','33',N'Vũ Quý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13159','33',N'Hồng Vũ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13183','33',N'Bình Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13186','33',N'Bình Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13192','33',N'Vũ Thư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13219','33',N'Vạn Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13222','33',N'Thư Trì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13225','33',N'Thái Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13246','33',N'Tân Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13264','33',N'Thư Vũ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13279','33',N'Vũ Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13285','37',N'Phủ Lý',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13291','37',N'Phù Vân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13318','37',N'Châu Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13324','37',N'Duy Tiên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13330','37',N'Duy Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13336','37',N'Duy Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13348','37',N'Đồng Văn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13363','37',N'Tiên Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13366','37',N'Hà Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13384','37',N'Kim Bảng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13393','37',N'Lê Hồ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13396','37',N'Nguyễn Úy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13402','37',N'Kim Thanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13420','37',N'Tam Chúc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13435','37',N'Lý Thường Kiệt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13444','37',N'Liêm Tuyền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13456','37',N'Liêm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13474','37',N'Tân Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13483','37',N'Thanh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13489','37',N'Thanh Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13495','37',N'Thanh Liêm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13501','37',N'Bình Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13504','37',N'Bình Lục',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13531','37',N'Bình Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13540','37',N'Bình An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13558','37',N'Bình Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13573','37',N'Lý Nhân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13579','37',N'Bắc Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13591','37',N'Nam Xang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13594','37',N'Trần Thương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13597','37',N'Vĩnh Trụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13609','37',N'Nhân Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13627','37',N'Nam Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13669','37',N'Nam Định',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13684','37',N'Thiên Trường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13693','37',N'Đông A',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13699','37',N'Thành Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13708','37',N'Mỹ Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13741','37',N'Vụ Bản',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13750','37',N'Minh Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13753','37',N'Hiển Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13777','37',N'Trường Thi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13786','37',N'Liên Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13795','37',N'Ý Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13807','37',N'Tân Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13822','37',N'Phong Doanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13834','37',N'Vũ Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13864','37',N'Vạn Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13870','37',N'Yên Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13879','37',N'Yên Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13891','37',N'Nghĩa Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13894','37',N'Rạng Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13900','37',N'Đồng Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13918','37',N'Nghĩa Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13927','37',N'Hồng Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13939','37',N'Quỹ Nhất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13957','37',N'Nghĩa Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13966','37',N'Nam Trực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('13972','37',N'Vị Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13984','37',N'Hồng Quang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('13987','37',N'Nam Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14005','37',N'Nam Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14011','37',N'Nam Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14014','37',N'Nam Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14026','37',N'Cổ Lễ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14038','37',N'Ninh Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14053','37',N'Trực Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14056','37',N'Cát Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14062','37',N'Quang Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14071','37',N'Minh Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14077','37',N'Ninh Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14089','37',N'Xuân Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14095','37',N'Xuân Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14104','37',N'Xuân Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14122','37',N'Xuân Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14161','37',N'Giao Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14167','37',N'Giao Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14179','37',N'Giao Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14182','37',N'Giao Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14194','37',N'Giao Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14203','37',N'Giao Phúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14212','37',N'Giao Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14215','37',N'Hải Hậu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14218','37',N'Hải Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14221','37',N'Hải Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14236','37',N'Hải Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14248','37',N'Hải Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14281','37',N'Hải An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14287','37',N'Hải Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14308','37',N'Hải Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14329','37',N'Hoa Lư',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14359','37',N'Nam Hoa Lư',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14362','37',N'Tam Điệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14365','37',N'Trung Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14371','37',N'Yên Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14389','37',N'Gia Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14401','37',N'Gia Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14404','37',N'Cúc Phương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14407','37',N'Phú Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14428','37',N'Nho Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14434','37',N'Thanh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14452','37',N'Quỳnh Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14458','37',N'Phú Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14464','37',N'Gia Viễn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14482','37',N'Gia Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14488','37',N'Gia Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14494','37',N'Gia Trấn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14500','37',N'Đại Hoàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14524','37',N'Gia Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14533','37',N'Tây Hoa Lư',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14560','37',N'Yên Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('14563','37',N'Khánh Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14566','37',N'Đông Hoa Lư',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14608','37',N'Khánh Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14611','37',N'Khánh Nhạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14614','37',N'Khánh Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14620','37',N'Phát Diệm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14623','37',N'Bình Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14638','37',N'Kim Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14647','37',N'Quang Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14653','37',N'Chất Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14674','37',N'Lai Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14677','37',N'Định Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14698','37',N'Kim Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14701','37',N'Yên Mô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14725','37',N'Yên Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14728','37',N'Yên Từ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14743','37',N'Yên Mạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14746','37',N'Đồng Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14758','38',N'Hàm Rồng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14797','38',N'Hạc Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14812','38',N'Bỉm Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14818','38',N'Quang Trung',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('14845','38',N'Mường Lát',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14848','38',N'Tam Chung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14854','38',N'Mường Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14857','38',N'Trung Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14860','38',N'Quang Chiểu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14863','38',N'Pù Nhi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14864','38',N'Nhi Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14866','38',N'Mường Chanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14869','38',N'Hồi Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14872','38',N'Trung Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14875','38',N'Trung Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14878','38',N'Phú Lệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14890','38',N'Phú Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14896','38',N'Hiền Kiệt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14902','38',N'Nam Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14908','38',N'Thiên Phủ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14923','38',N'Bá Thước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14932','38',N'Điền Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14950','38',N'Điền Lư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14953','38',N'Quý Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14956','38',N'Pù Luông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14959','38',N'Cổ Lũng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14974','38',N'Văn Nho',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('14980','38',N'Thiết Ống',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15001','38',N'Trung Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15007','38',N'Tam Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15010','38',N'Sơn Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15013','38',N'Na Mèo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15016','38',N'Quan Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15019','38',N'Tam Lư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15022','38',N'Sơn Điện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15025','38',N'Mường Mìn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15031','38',N'Yên Khương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15034','38',N'Yên Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15043','38',N'Giao An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15049','38',N'Văn Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15055','38',N'Linh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15058','38',N'Đồng Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17611','40',N'Vân Tụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15061','38',N'Ngọc Lặc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15085','38',N'Thạch Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15091','38',N'Ngọc Liên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15106','38',N'Nguyệt Ấn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15112','38',N'Kiên Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15124','38',N'Minh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15127','38',N'Cẩm Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15142','38',N'Cẩm Thạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15148','38',N'Cẩm Tú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15163','38',N'Cẩm Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15178','38',N'Cẩm Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15187','38',N'Kim Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15190','38',N'Vân Du',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15199','38',N'Thạch Quảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15211','38',N'Thạch Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15229','38',N'Thành Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15250','38',N'Ngọc Trạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15271','38',N'Hà Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15274','38',N'Hà Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15286','38',N'Hoạt Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15298','38',N'Lĩnh Toại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15316','38',N'Tống Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15349','38',N'Vĩnh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15361','38',N'Tây Đô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15382','38',N'Biện Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15409','38',N'Yên Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15412','38',N'Quý Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15421','38',N'Yên Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15442','38',N'Yên Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15448','38',N'Định Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15457','38',N'Định Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15469','38',N'Yên Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15499','38',N'Thọ Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15505','38',N'Thọ Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15520','38',N'Xuân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15544','38',N'Lam Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15553','38',N'Sao Vàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15568','38',N'Thọ Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15574','38',N'Xuân Tín',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15592','38',N'Xuân Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15607','38',N'Bát Mọt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15610','38',N'Yên Nhân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15622','38',N'Vạn Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15628','38',N'Lương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15634','38',N'Luận Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15643','38',N'Thắng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15646','38',N'Thường Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15658','38',N'Xuân Chinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15661','38',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15664','38',N'Triệu Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15667','38',N'Thọ Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15682','38',N'Hợp Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15715','38',N'Tân Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15724','38',N'Đồng Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15754','38',N'Thọ Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15763','38',N'Thọ Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15766','38',N'An Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15772','38',N'Thiệu Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15778','38',N'Thiệu Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15796','38',N'Thiệu Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15820','38',N'Thiệu Toán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15835','38',N'Thiệu Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15853','38',N'Đông Tiến',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('15865','38',N'Hoằng Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15880','38',N'Hoằng Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15889','38',N'Hoằng Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15910','38',N'Hoằng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15925','38',N'Nguyệt Viên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('15961','38',N'Hoằng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15976','38',N'Hoằng Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('15991','38',N'Hoằng Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16000','38',N'Hoằng Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16012','38',N'Hậu Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23293','51',N'Kon Tum',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16021','38',N'Triệu Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16033','38',N'Đông Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16072','38',N'Hoa Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16078','38',N'Vạn Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16093','38',N'Nga Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16108','38',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16114','38',N'Nga Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16138','38',N'Hồ Vương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16144','38',N'Nga An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16171','38',N'Ba Đình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16174','38',N'Như Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16177','38',N'Xuân Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16186','38',N'Hoá Quỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16213','38',N'Thanh Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16222','38',N'Thanh Quân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16225','38',N'Thượng Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16228','38',N'Như Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16234','38',N'Xuân Du',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16249','38',N'Mậu Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16258','38',N'Xuân Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16264','38',N'Yên Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16273','38',N'Thanh Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16279','38',N'Nông Cống',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16297','38',N'Trung Chính',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16309','38',N'Thắng Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16342','38',N'Thăng Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16348','38',N'Trường Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16363','38',N'Tượng Lĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16369','38',N'Công Chính',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16378','38',N'Đông Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16417','38',N'Đông Quang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16438','38',N'Lưu Vệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16480','38',N'Quảng Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16489','38',N'Quảng Chính',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16498','38',N'Quảng Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16516','38',N'Nam Sầm Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16522','38',N'Quảng Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16531','38',N'Sầm Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16540','38',N'Quảng Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16543','38',N'Quảng Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16549','38',N'Tiên Trang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16561','38',N'Tĩnh Gia',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16576','38',N'Ngọc Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16591','38',N'Các Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16594','38',N'Tân Dân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16597','38',N'Hải Lĩnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16609','38',N'Đào Duy Từ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16624','38',N'Trúc Lâm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16636','38',N'Trường Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16645','38',N'Hải Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16654','38',N'Nghi Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16681','40',N'Thành Vinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16690','40',N'Trường Vinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16702','40',N'Vinh Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16708','40',N'Vinh Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16732','40',N'Cửa Lò',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16738','40',N'Quế Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16744','40',N'Thông Thụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16750','40',N'Tiền Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16756','40',N'Tri Lễ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16774','40',N'Mường Quàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16777','40',N'Quỳ Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16792','40',N'Châu Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16801','40',N'Hùng Chân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16804','40',N'Châu Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16813','40',N'Mường Xén',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16816','40',N'Mỹ Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16819','40',N'Bắc Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16822','40',N'Keng Đu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16828','40',N'Huồi Tụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16831','40',N'Mường Lống',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16834','40',N'Na Loi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16837','40',N'Nậm Cắn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16849','40',N'Hữu Kiệm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16855','40',N'Chiêu Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16858','40',N'Mường Típ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16870','40',N'Na Ngoi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16876','40',N'Tương Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16882','40',N'Nhôn Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16885','40',N'Hữu Khuông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16903','40',N'Nga My',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16906','40',N'Lượng Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16909','40',N'Yên Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16912','40',N'Yên Na',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16933','40',N'Tam Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16936','40',N'Tam Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16939','40',N'Thái Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('16941','40',N'Nghĩa Đàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16951','40',N'Nghĩa Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16969','40',N'Nghĩa Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16972','40',N'Nghĩa Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('16975','40',N'Nghĩa Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17011','40',N'Tây Hiếu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('17017','40',N'Đông Hiếu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17029','40',N'Nghĩa Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17032','40',N'Nghĩa Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17035','40',N'Quỳ Hợp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17044','40',N'Châu Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17056','40',N'Châu Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17059','40',N'Tam Hợp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17071','40',N'Minh Hợp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17077','40',N'Mường Ham',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17089','40',N'Mường Chọng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17110','40',N'Hoàng Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('17125','40',N'Quỳnh Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('17128','40',N'Tân Mai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('17143','40',N'Quỳnh Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17149','40',N'Quỳnh Tam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17170','40',N'Quỳnh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17176','40',N'Quỳnh Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17179','40',N'Quỳnh Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17212','40',N'Quỳnh Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17224','40',N'Quỳnh Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17230','40',N'Bình Chuẩn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17239','40',N'Mậu Thạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17242','40',N'Cam Phục',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17248','40',N'Châu Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17254','40',N'Con Cuông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17263','40',N'Môn Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17266','40',N'Tân Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17272','40',N'Tân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17278','40',N'Giai Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17284','40',N'Nghĩa Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17287','40',N'Tiên Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17305','40',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17326','40',N'Nghĩa Hành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17329','40',N'Anh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17335','40',N'Thành Bình Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17344','40',N'Nhân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17357','40',N'Vĩnh Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17365','40',N'Anh Sơn Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17380','40',N'Yên Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17395','40',N'Hùng Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17416','40',N'Đức Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17419','40',N'Hải Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17443','40',N'Quảng Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17464','40',N'Diễn Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17476','40',N'Minh Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17479','40',N'An Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17488','40',N'Tân Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17506','40',N'Yên Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17515','40',N'Bình Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17521','40',N'Quang Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17524','40',N'Giai Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17530','40',N'Đông Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17560','40',N'Vân Du',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17569','40',N'Quan Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17605','40',N'Hợp Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17623','40',N'Bạch Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17641','40',N'Lương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17662','40',N'Đô Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17677','40',N'Văn Hiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17689','40',N'Thuần Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17707','40',N'Bạch Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17713','40',N'Đại Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17722','40',N'Hạnh Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17728','40',N'Cát Ngạn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17743','40',N'Tam Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17759','40',N'Sơn Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17770','40',N'Hoa Quân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17779','40',N'Xuân Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17791','40',N'Kim Bảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17818','40',N'Bích Hào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17827','40',N'Nghi Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17833','40',N'Hải Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17842','40',N'Thần Lĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17854','40',N'Văn Kiều',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17857','40',N'Phúc Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17866','40',N'Trung Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17878','40',N'Đông Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17920','40',N'Vinh Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('17935','40',N'Nam Đàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17944','40',N'Đại Huệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17950','40',N'Vạn An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17971','40',N'Kim Liên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('17989','40',N'Thiên Nhẫn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18001','40',N'Hưng Nguyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18007','40',N'Yên Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18028','40',N'Hưng Nguyên Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18040','40',N'Lam Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18073','42',N'Thành Sen',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18100','42',N'Trần Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18115','42',N'Bắc Hồng Lĩnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18118','42',N'Nam Hồng Lĩnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18133','42',N'Hương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18160','42',N'Sơn Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18163','42',N'Sơn Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18172','42',N'Sơn Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18184','42',N'Sơn Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18196','42',N'Sơn Kim 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18199','42',N'Sơn Kim 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18202','42',N'Tứ Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18223','42',N'Kim Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18229','42',N'Đức Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18244','42',N'Đức Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18262','42',N'Đức Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18277','42',N'Đức Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18304','42',N'Đức Đồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18313','42',N'Vũ Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18322','42',N'Mai Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18328','42',N'Thượng Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18352','42',N'Nghi Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18364','42',N'Đan Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18373','42',N'Tiên Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18394','42',N'Cổ Đạm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18406','42',N'Can Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18409','42',N'Hồng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18418','42',N'Tùng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18436','42',N'Trường Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18466','42',N'Gia Hanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18481','42',N'Xuân Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18484','42',N'Đồng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18496','42',N'Hương Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18502','42',N'Hà Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18523','42',N'Hương Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18532','42',N'Hương Phố',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18544','42',N'Hương Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18547','42',N'Phúc Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18550','42',N'Hương Đô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18562','42',N'Thạch Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18568','42',N'Lộc Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18583','42',N'Mai Phụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18586','42',N'Đông Kinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18601','42',N'Việt Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18604','42',N'Thạch Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18619','42',N'Đồng Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18628','42',N'Thạch Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18634','42',N'Toàn Lưu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18652','42',N'Hà Huy Tập',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18667','42',N'Thạch Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18673','42',N'Cẩm Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18676','42',N'Thiên Cầm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18682','42',N'Yên Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18685','42',N'Cẩm Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18736','42',N'Cẩm Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18739','42',N'Cẩm Duệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18742','42',N'Cẩm Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18748','42',N'Cẩm Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18754','42',N'Sông Trí',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18766','42',N'Kỳ Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18775','42',N'Kỳ Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18781','42',N'Hải Ninh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18787','42',N'Kỳ Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18790','42',N'Kỳ Khang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18814','42',N'Kỳ Hoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18823','42',N'Vũng Áng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18832','42',N'Hoành Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18838','42',N'Kỳ Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18844','42',N'Kỳ Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18859','44',N'Đồng Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18871','44',N'Đồng Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18880','44',N'Đồng Hới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('18901','44',N'Minh Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18904','44',N'Dân Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18919','44',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18922','44',N'Kim Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18943','44',N'Kim Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18949','44',N'Đồng Lê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18952','44',N'Tuyên Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18958','44',N'Tuyên Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18985','44',N'Tuyên Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18991','44',N'Tuyên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('18997','44',N'Tuyên Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19009','44',N'Ba Đồn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19021','44',N'Phú Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19030','44',N'Trung Thuần',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19033','44',N'Hoà Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19051','44',N'Tân Gianh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19057','44',N'Quảng Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19066','44',N'Bắc Gianh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19075','44',N'Nam Ba Đồn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19093','44',N'Nam Gianh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19111','44',N'Hoàn Lão',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19126','44',N'Bắc Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19138','44',N'Phong Nha',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19141','44',N'Bố Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19147','44',N'Thượng Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19159','44',N'Đông Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19198','44',N'Nam Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19204','44',N'Trường Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19207','44',N'Quảng Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19225','44',N'Ninh Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19237','44',N'Trường Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19246','44',N'Lệ Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19249','44',N'Lệ Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19255','44',N'Cam Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19288','44',N'Sen Ngư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19291','44',N'Tân Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19309','44',N'Trường Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19318','44',N'Kim Ngân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19333','44',N'Đông Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19351','44',N'Nam Đông Hà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19360','44',N'Quảng Trị',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19363','44',N'Vĩnh Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19366','44',N'Bến Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19372','44',N'Vĩnh Hoàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19405','44',N'Vĩnh Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19414','44',N'Cửa Tùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19429','44',N'Khe Sanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19432','44',N'Lao Bảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19435','44',N'Hướng Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19441','44',N'Hướng Phùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19462','44',N'Tân Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19483','44',N'A Dơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19489','44',N'Lìa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19495','44',N'Gio Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19496','44',N'Cửa Việt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19501','44',N'Bến Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19537','44',N'Cồn Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19555','44',N'Hướng Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19564','44',N'Đakrông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19567','44',N'Ba Lòng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19588','44',N'Tà Rụt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19594','44',N'La Lay',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19597','44',N'Cam Lộ',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('19603','44',N'Hiếu Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19624','44',N'Triệu Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19639','44',N'Nam Cửa Việt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19645','44',N'Triệu Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19654','44',N'Triệu Cơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19669','44',N'Ái Tử',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19681','44',N'Diên Sanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19699','44',N'Vĩnh Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19702','44',N'Hải Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19735','44',N'Nam Hải Lăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19741','44',N'Mỹ Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19742','44',N'Cồn Cỏ',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('19753','46',N'Phú Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19774','46',N'Kim Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19777','46',N'Vỹ Dạ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19789','46',N'Thuận Hoá',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19804','46',N'Hương An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19813','46',N'Thuỷ Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19815','46',N'An Cựu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19819','46',N'Phong Điền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19828','46',N'Phong Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19831','46',N'Phong Dinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19858','46',N'Phong Thái',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19867','46',N'Quảng Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19873','46',N'Phong Quảng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19885','46',N'Đan Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19900','46',N'Thuận An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19909','46',N'Dương Nỗ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19918','46',N'Phú Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19930','46',N'Mỹ Thượng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19942','46',N'Phú Vang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19945','46',N'Phú Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('19960','46',N'Phú Bài',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19969','46',N'Thanh Thuỷ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19975','46',N'Hương Thuỷ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('19996','46',N'Hương Trà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20014','46',N'Hoá Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20017','46',N'Kim Trà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20035','46',N'Bình Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20044','46',N'A Lưới 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20050','46',N'A Lưới 5',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20056','46',N'A Lưới 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20071','46',N'A Lưới 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20101','46',N'A Lưới 4',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20107','46',N'Phú Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20122','46',N'Vinh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20131','46',N'Hưng Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20137','46',N'Chân Mây - Lăng Cô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20140','46',N'Lộc An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20161','46',N'Khe Tre',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20179','46',N'Nam Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20182','46',N'Long Quảng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20194','48',N'Hải Vân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20197','48',N'Liên Chiểu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20200','48',N'Hoà Khánh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20209','48',N'Thanh Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20242','48',N'Hải Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20257','48',N'Hoà Cường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20260','48',N'Cẩm Lệ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20263','48',N'Sơn Trà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20275','48',N'An Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20285','48',N'Ngũ Hành Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20305','48',N'An Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20308','48',N'Bà Nà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20314','48',N'Hoà Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20320','48',N'Hoà Vang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20332','48',N'Hoà Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20333','48',N'Hoàng Sa',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('20335','48',N'Bàn Thạch',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20341','48',N'Tam Kỳ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20350','48',N'Hương Trà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20356','48',N'Quảng Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20364','48',N'Chiên Đàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20380','48',N'Tây Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20392','48',N'Phú Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20401','48',N'Hội An Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20410','48',N'Hội An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20413','48',N'Hội An Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20434','48',N'Tân Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20443','48',N'Hùng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20455','48',N'Tây Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20458','48',N'Avương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20467','48',N'Đông Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20476','48',N'Sông Kôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20485','48',N'Sông Vàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20494','48',N'Bến Hiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20500','48',N'Đại Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20506','48',N'Thượng Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20515','48',N'Hà Nha',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20539','48',N'Vu Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20542','48',N'Phú Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20551','48',N'Điện Bàn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20557','48',N'Điện Bàn Bắc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20569','48',N'Điện Bàn Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20575','48',N'An Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20579','48',N'Điện Bàn Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('20587','48',N'Gò Nổi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20599','48',N'Nam Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20611','48',N'Thu Bồn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20623','48',N'Duy Xuyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20635','48',N'Duy Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20641','48',N'Quế Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20650','48',N'Xuân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20656','48',N'Nông Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20662','48',N'Quế Sơn Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20669','48',N'Quế Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20695','48',N'Thạnh Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20698','48',N'La Êê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20704','48',N'La Dêê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20707','48',N'Nam Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20710','48',N'Bến Giằng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20716','48',N'Đắc Pring',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20722','48',N'Khâm Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20728','48',N'Phước Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20734','48',N'Phước Năng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20740','48',N'Phước Chánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20752','48',N'Phước Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20767','48',N'Việt An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20770','48',N'Phước Trà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20779','48',N'Hiệp Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20791','48',N'Thăng Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20794','48',N'Thăng An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20818','48',N'Đồng Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20827','48',N'Thăng Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20836','48',N'Thăng Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20848','48',N'Thăng Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20854','48',N'Tiên Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20857','48',N'Sơn Cẩm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20875','48',N'Lãnh Ngọc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20878','48',N'Thạnh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20900','48',N'Trà My',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20908','48',N'Trà Liên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20920','48',N'Trà Đốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20923','48',N'Trà Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20929','48',N'Trà Giáp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20938','48',N'Trà Leng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20941','48',N'Trà Tập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20944','48',N'Nam Trà My',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20950','48',N'Trà Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20959','48',N'Trà Vân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20965','48',N'Núi Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20971','48',N'Tam Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20977','48',N'Đức Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20984','48',N'Tam Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('20992','48',N'Tam Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21004','48',N'Tam Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21025','51',N'Cẩm Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21028','51',N'Nghĩa Lộ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21034','51',N'An Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21040','51',N'Bình Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21061','51',N'Vạn Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21085','51',N'Bình Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21100','51',N'Bình Chương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21109','51',N'Đông Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21115','51',N'Trà Bồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21124','51',N'Thanh Bồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21127','51',N'Đông Trà Bồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21136','51',N'Cà Đam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21154','51',N'Tây Trà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21157','51',N'Tây Trà Bồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21172','51',N'Trương Quang Trọng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21181','51',N'Thọ Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21196','51',N'Trường Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21205','51',N'Ba Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21211','51',N'Tịnh Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21220','51',N'Sơn Tịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21235','51',N'Tư Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21238','51',N'Vệ Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21244','51',N'Trà Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21250','51',N'Nghĩa Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21289','51',N'Sơn Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21292','51',N'Sơn Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21307','51',N'Sơn Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21319','51',N'Sơn Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21325','51',N'Sơn Kỳ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21334','51',N'Sơn Tây Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21340','51',N'Sơn Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21343','51',N'Sơn Tây Hạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21349','51',N'Sơn Mai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21361','51',N'Minh Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21364','51',N'Nghĩa Hành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21370','51',N'Phước Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21385','51',N'Đình Cương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21388','51',N'Thiện Tín',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21400','51',N'Mộ Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21409','51',N'Long Phụng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21421','51',N'Mỏ Cày',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21433','51',N'Lân Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21439','51',N'Đức Phổ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21451','51',N'Trà Câu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21457','51',N'Nguyễn Nghiêm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21472','51',N'Khánh Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21478','51',N'Sa Huỳnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21484','51',N'Ba Tơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21490','51',N'Ba Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21496','51',N'Ba Động',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21499','51',N'Ba Dinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21520','51',N'Đặng Thuỳ Trâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21523','51',N'Ba Tô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21529','51',N'Ba Vì',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21538','51',N'Ba Xa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21548','51',N'Lý Sơn',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('23284','51',N'Đăk Cấm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23302','51',N'Đăk Bla',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23317','51',N'Ngọk Bay',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23326','51',N'Ia Chim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23332','51',N'Đăk Rơ Wa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23341','51',N'Đăk Pék',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23344','51',N'Đăk Plô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23356','51',N'Xốp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23365','51',N'Ngọc Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23368','51',N'Đăk Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23374','51',N'Đăk Môn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23377','51',N'Bờ Y',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23383','51',N'Dục Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23392','51',N'Sa Loong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23401','51',N'Đăk Tô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23416','51',N'Đăk Sao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23419','51',N'Đăk Tờ Kan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23425','51',N'Tu Mơ Rông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23428','51',N'Ngọk Tụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23430','51',N'Kon Đào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23446','51',N'Măng Ri',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23455','51',N'Măng Bút',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23473','51',N'Măng Đen',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23476','51',N'Kon Plông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23479','51',N'Đăk Rve',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23485','51',N'Đăk Kôi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23497','51',N'Kon Braih',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23500','51',N'Đăk Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23504','51',N'Đăk Pxi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23510','51',N'Đăk Ui',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23512','51',N'Đăk Mar',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23515','51',N'Ngọk Réo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23527','51',N'Sa Thầy',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23530','51',N'Rờ Kơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23534','51',N'Sa Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23535','51',N'Ia Đal',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23536','51',N'Mô Rai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23538','51',N'Ia Tơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23548','51',N'Ya Ly',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21553','52',N'Quy Nhơn Bắc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21583','52',N'Quy Nhơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21589','52',N'Quy Nhơn Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21592','52',N'Quy Nhơn Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21601','52',N'Quy Nhơn Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21607','52',N'Nhơn Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21609','52',N'An Lão',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21616','52',N'An Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21622','52',N'An Toàn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21628','52',N'An Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21637','52',N'Tam Quan',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21640','52',N'Bồng Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21655','52',N'Hoài Nhơn Bắc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21661','52',N'Hoài Nhơn Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21664','52',N'Hoài Nhơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21670','52',N'Hoài Nhơn Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21673','52',N'Hoài Nhơn Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21688','52',N'Hoài Ân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21697','52',N'Ân Hảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21703','52',N'Vạn Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21715','52',N'Ân Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21727','52',N'Kim Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21730','52',N'Phù Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21733','52',N'Bình Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21739','52',N'Phù Mỹ Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21751','52',N'Phù Mỹ Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21757','52',N'Phù Mỹ Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21769','52',N'An Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21775','52',N'Phù Mỹ Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21786','52',N'Vĩnh Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21787','52',N'Vĩnh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21796','52',N'Vĩnh Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21805','52',N'Vĩnh Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21808','52',N'Tây Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21817','52',N'Bình Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21820','52',N'Bình Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21829','52',N'Bình An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21835','52',N'Bình Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21853','52',N'Phù Cát',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21859','52',N'Đề Gi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21868','52',N'Hội Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21871','52',N'Hoà Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21880','52',N'Cát Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21892','52',N'Xuân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21901','52',N'Ngô Mây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21907','52',N'Bình Định',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21910','52',N'An Nhơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21925','52',N'An Nhơn Bắc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21934','52',N'An Nhơn Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21940','52',N'An Nhơn Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21943','52',N'An Nhơn Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('21952','52',N'Tuy Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21964','52',N'Tuy Phước Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21970','52',N'Tuy Phước Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21985','52',N'Tuy Phước Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21994','52',N'Vân Canh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('21997','52',N'Canh Liên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22006','52',N'Canh Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23563','52',N'Diên Hồng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23575','52',N'Pleiku',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23584','52',N'Thống Nhất',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23586','52',N'Hội Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23590','52',N'Biển Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23602','52',N'An Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23611','52',N'Gào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23614','52',N'An Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23617','52',N'An Khê',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23629','52',N'Cửu An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23638','52',N'Kbang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23644','52',N'Đak Rong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23647','52',N'Sơn Lang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23650','52',N'Krong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23668','52',N'Tơ Tung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23674','52',N'Kông Bơ La',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23677','52',N'Đak Đoa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23683','52',N'Đak Sơmei',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23701','52',N'Kon Gang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23710','52',N'Ia Băng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23714','52',N'KDang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23722','52',N'Chư Păh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23728','52',N'Ia Khươl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23734','52',N'Ia Ly',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23749','52',N'Ia Phí',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23764','52',N'Ia Grai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23767','52',N'Ia Hrung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23776','52',N'Ia Krái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23782','52',N'Ia O',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23788','52',N'Ia Chia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23794','52',N'Mang Yang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23798','52',N'Ayun',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23799','52',N'Hra',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23812','52',N'Lơ Pang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23818','52',N'Kon Chiêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23824','52',N'Kông Chro',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23830','52',N'Chư Krey',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23833','52',N'Ya Ma',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23839','52',N'SRó',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23842','52',N'Đăk Song',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23851','52',N'Chơ Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23857','52',N'Đức Cơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23866','52',N'Ia Krêl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23869','52',N'Ia Dơk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23872','52',N'Ia Dom',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23881','52',N'Ia Pnôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23884','52',N'Ia Nan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23887','52',N'Chư Prông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23896','52',N'Bàu Cạn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23908','52',N'Ia Tôr',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23911','52',N'Ia Boòng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23917','52',N'Ia Púch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23926','52',N'Ia Pia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23935','52',N'Ia Lâu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23938','52',N'Ia Mơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23941','52',N'Chư Sê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23942','52',N'Chư Pưh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23947','52',N'Bờ Ngoong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23954','52',N'Al Bá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23971','52',N'Ia Hrú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23977','52',N'Ia Ko',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23986','52',N'Ia Le',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23995','52',N'Đak Pơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24007','52',N'Ya Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24013','52',N'Pờ Tó',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24022','52',N'Ia Pa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24028','52',N'Ia Tul',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24043','52',N'Phú Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24044','52',N'Ayun Pa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24049','52',N'Chư A Thai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24061','52',N'Ia Hiao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24065','52',N'Ia Rbol',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24073','52',N'Ia Sao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24076','52',N'Phú Túc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24100','52',N'Ia Dreh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24109','52',N'Uar',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24112','52',N'Ia Rsai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22333','56',N'Bắc Nha Trang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22366','56',N'Nha Trang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22390','56',N'Tây Nha Trang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22402','56',N'Nam Nha Trang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22411','56',N'Bắc Cam Ranh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22420','56',N'Cam Ranh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22423','56',N'Ba Ngòi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22432','56',N'Cam Linh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22435','56',N'Cam Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22453','56',N'Cam Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22465','56',N'Cam An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22480','56',N'Nam Cam Ranh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22489','56',N'Vạn Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22498','56',N'Tu Bông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22504','56',N'Đại Lãnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22516','56',N'Vạn Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22525','56',N'Vạn Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22528','56',N'Ninh Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22546','56',N'Bắc Ninh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22552','56',N'Tây Ninh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22558','56',N'Hoà Trí',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22561','56',N'Đông Ninh Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22576','56',N'Tân Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22591','56',N'Hoà Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22597','56',N'Nam Ninh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22609','56',N'Khánh Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22612','56',N'Trung Khánh Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22615','56',N'Bắc Khánh Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22624','56',N'Tây Khánh Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22648','56',N'Nam Khánh Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22651','56',N'Diên Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22657','56',N'Diên Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22660','56',N'Diên Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22672','56',N'Diên Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22678','56',N'Diên Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22702','56',N'Suối Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22708','56',N'Suối Dầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22714','56',N'Khánh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22720','56',N'Tây Khánh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22732','56',N'Đông Khánh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22736','56',N'Trường Sa',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('22738','56',N'Đô Vinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22741','56',N'Bảo An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22759','56',N'Phan Rang',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22780','56',N'Đông Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22786','56',N'Bác Ái Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22795','56',N'Bác Ái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22801','56',N'Bác Ái Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22810','56',N'Ninh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22813','56',N'Lâm Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22822','56',N'Mỹ Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22828','56',N'Anh Dũng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22834','56',N'Ninh Chử',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22840','56',N'Công Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22846','56',N'Vĩnh Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22849','56',N'Thuận Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22852','56',N'Ninh Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22861','56',N'Xuân Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22870','56',N'Ninh Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22873','56',N'Phước Hậu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22888','56',N'Phước Dinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22891','56',N'Phước Hữu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22897','56',N'Thuận Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22900','56',N'Phước Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22909','56',N'Cà Ná',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22015','66',N'Tuy Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22045','66',N'Bình Kiến',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22051','66',N'Sông Cầu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22057','66',N'Xuân Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22060','66',N'Xuân Cảnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22075','66',N'Xuân Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22076','66',N'Xuân Đài',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22081','66',N'Đồng Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22090','66',N'Xuân Lãnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('22096','66',N'Phú Mỡ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22111','66',N'Xuân Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22114','66',N'Tuy An Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22120','66',N'Tuy An Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22132','66',N'Tuy An Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22147','66',N'Ô Loan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22153','66',N'Tuy An Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22165','66',N'Sơn Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22171','66',N'Tây Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22177','66',N'Vân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22192','66',N'Suối Trai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22207','66',N'Sông Hinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22222','66',N'Đức Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22225','66',N'Ea Bá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22237','66',N'Ea Ly',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22240','66',N'Phú Yên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22250','66',N'Sơn Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22255','66',N'Tây Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22258','66',N'Đông Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22261','66',N'Hoà Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22276','66',N'Hoà Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22285','66',N'Hoà Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22291','66',N'Hoà Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22303','66',N'Phú Hoà 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22319','66',N'Phú Hoà 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24121','66',N'Tân Lập',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24133','66',N'Buôn Ma Thuột',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24154','66',N'Thành Nhất',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24163','66',N'Tân An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24169','66',N'Ea Kao',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24175','66',N'Hoà Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24181','66',N'Ea Drăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24184','66',N'Ea H''Leo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24187','66',N'Ea Hiao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24193','66',N'Ea Wy',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24208','66',N'Ea Khăl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24211','66',N'Ea Súp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24214','66',N'Ia Lốp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24217','66',N'Ea Rốk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24221','66',N'Ia Rvê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24229','66',N'Ea Bung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24235','66',N'Buôn Đôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24241','66',N'Ea Wer',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24250','66',N'Ea Nuôl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24259','66',N'Quảng Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24265','66',N'Ea Kiết',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24277','66',N'Ea Tul',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24280','66',N'Cư M''gar',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24286','66',N'Ea M''Droh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24301','66',N'Cuôr Đăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24305','66',N'Buôn Hồ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24310','66',N'Krông Búk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24313','66',N'Cư Pơng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24316','66',N'Pơng Drang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24328','66',N'Ea Drông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24340','66',N'Cư Bao',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24343','66',N'Krông Năng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24346','66',N'Dliê Ya',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24352','66',N'Tam Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24364','66',N'Phú Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24373','66',N'Ea Kar',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24376','66',N'Ea Knốp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24400','66',N'Ea Păl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24403','66',N'Ea Ô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24406','66',N'Cư Yang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24412','66',N'M''Drắk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24415','66',N'Cư Prao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24433','66',N'Ea Riêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24436','66',N'Cư M''ta',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24444','66',N'Krông Á',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24445','66',N'Ea Trang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24448','66',N'Krông Bông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24454','66',N'Dang Kang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24472','66',N'Hoà Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24478','66',N'Cư Pui',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24484','66',N'Yang Mao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24490','66',N'Krông Pắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24496','66',N'Ea Kly',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24502','66',N'Ea Phê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24505','66',N'Ea Knuếc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24526','66',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24529','66',N'Vụ Bổn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24538','66',N'Krông Ana',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24540','66',N'Ea Ning',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24544','66',N'Ea Ktur',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24559','66',N'Ea Na',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24561','66',N'Dray Bhăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24568','66',N'Dur Kmăl',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24580','66',N'Liên Sơn Lắk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24595','66',N'Đắk Liêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24598','66',N'Đắk Phơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24604','66',N'Krông Nô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24607','66',N'Nam Ka',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22918','68',N'Mũi Né',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22924','68',N'Phú Thuỷ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22933','68',N'Hàm Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22945','68',N'Phan Thiết',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22954','68',N'Tiến Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22960','68',N'Bình Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('22963','68',N'Tuyên Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22969','68',N'Liên Hương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22972','68',N'Phan Rí Cửa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22978','68',N'Tuy Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('22981','68',N'Vĩnh Hảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23005','68',N'Bắc Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23008','68',N'Phan Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23020','68',N'Hải Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23023','68',N'Sông Luỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23032','68',N'Lương Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23041','68',N'Hồng Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23053','68',N'Hoà Thắng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23059','68',N'Hàm Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23065','68',N'La Dạ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23074','68',N'Đông Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23086','68',N'Hồng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23089','68',N'Hàm Thuận Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23095','68',N'Hàm Liêm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23110','68',N'Hàm Thuận Nam',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23122','68',N'Hàm Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23128','68',N'Hàm Kiệm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23134','68',N'Tân Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23143','68',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23149','68',N'Tánh Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23152','68',N'Bắc Ruộng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23158','68',N'Nghị Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23173','68',N'Đồng Kho',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23188','68',N'Suối Kiết',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23191','68',N'Đức Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23194','68',N'Hoài Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23200','68',N'Nam Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23227','68',N'Trà Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23230','68',N'Tân Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23231','68',N'Phước Hội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23235','68',N'La Gi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('23236','68',N'Hàm Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23246','68',N'Tân Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23266','68',N'Sơn Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('23272','68',N'Phú Quý',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('24611','68',N'Bắc Gia Nghĩa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24615','68',N'Nam Gia Nghĩa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24616','68',N'Quảng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24617','68',N'Đông Gia Nghĩa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24620','68',N'Quảng Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24631','68',N'Quảng Khê',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24637','68',N'Tà Đùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24640','68',N'Cư Jút',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24646','68',N'Đắk Wil',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24649','68',N'Nam Dong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24664','68',N'Đức Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24670','68',N'Đắk Mil',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24678','68',N'Đắk Sắk',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24682','68',N'Thuận An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24688','68',N'Krông Nô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24697','68',N'Nam Đà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24703','68',N'Nâm Nung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24712','68',N'Quảng Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24717','68',N'Đức An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24718','68',N'Đắk Song',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24722','68',N'Thuận Hạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24730','68',N'Trường Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24733','68',N'Kiến Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24736','68',N'Quảng Trực',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24739','68',N'Tuy Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24748','68',N'Quảng Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24751','68',N'Nhân Cơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24760','68',N'Quảng Tín',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24778','68',N'Lâm Viên - Đà Lạt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24781','68',N'Xuân Hương - Đà Lạt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24787','68',N'Cam Ly - Đà Lạt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24805','68',N'Xuân Trường - Đà Lạt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24820','68',N'2 Bảo Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24823','68',N'1 Bảo Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24829','68',N'B''Lao',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24841','68',N'3 Bảo Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24846','68',N'Lang Biang - Đà Lạt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('24848','68',N'Lạc Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24853','68',N'Đam Rông 4',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24868','68',N'Nam Ban Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24871','68',N'Đinh Văn Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24875','68',N'Đam Rông 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24877','68',N'Đam Rông 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24883','68',N'Nam Hà Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24886','68',N'Đam Rông 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24895','68',N'Phú Sơn Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24907','68',N'Phúc Thọ Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24916','68',N'Tân Hà Lâm Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24931','68',N'Đơn Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24934','68',N'D''Ran',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24943','68',N'Ka Đô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24955','68',N'Quảng Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24958','68',N'Đức Trọng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24967','68',N'Hiệp Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24976','68',N'Tân Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24985','68',N'Ninh Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24988','68',N'Tà Năng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('24991','68',N'Tà Hine',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25000','68',N'Di Linh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25007','68',N'Đinh Trang Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25015','68',N'Gia Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25018','68',N'Bảo Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25036','68',N'Hoà Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25042','68',N'Hoà Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25051','68',N'Sơn Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25054','68',N'Bảo Lâm 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25057','68',N'Bảo Lâm 5',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25063','68',N'Bảo Lâm 4',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25084','68',N'Bảo Lâm 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25093','68',N'Bảo Lâm 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25099','68',N'Đạ Huoai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25105','68',N'Đạ Huoai 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25114','68',N'Đạ Huoai 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25126','68',N'Đạ Tẻh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25135','68',N'Đạ Tẻh 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25138','68',N'Đạ Tẻh 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25159','68',N'Cát Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25162','68',N'Cát Tiên 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25180','68',N'Cát Tiên 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25195','75',N'Bình Phước',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25210','75',N'Đồng Xoài',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25217','75',N'Phước Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25220','75',N'Phước Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25222','75',N'Bù Gia Mập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25225','75',N'Đăk Ơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25231','75',N'Đa Kia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25246','75',N'Bình Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25252','75',N'Phú Riềng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25255','75',N'Long Hà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25261','75',N'Phú Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25267','75',N'Phú Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25270','75',N'Lộc Ninh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25279','75',N'Lộc Tấn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25280','75',N'Lộc Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25292','75',N'Lộc Quang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25294','75',N'Lộc Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25303','75',N'Lộc Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25308','75',N'Thiện Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25309','75',N'Hưng Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25318','75',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25326','75',N'Bình Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25333','75',N'An Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25345','75',N'Tân Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25349','75',N'Minh Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25351','75',N'Tân Quan',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25357','75',N'Tân Khai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25363','75',N'Đồng Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25378','75',N'Tân Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25387','75',N'Thuận Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25390','75',N'Đồng Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25396','75',N'Bù Đăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25399','75',N'Đak Nhau',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25402','75',N'Thọ Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25405','75',N'Bom Bo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25417','75',N'Nghĩa Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25420','75',N'Phước Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25432','75',N'Chơn Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25441','75',N'Minh Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25450','75',N'Nha Bích',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25993','75',N'Trảng Dài',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26005','75',N'Hố Nai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26017','75',N'Tam Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26020','75',N'Long Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26041','75',N'Trấn Biên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26068','75',N'Biên Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26080','75',N'Long Khánh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26089','75',N'Bình Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26098','75',N'Bảo Vinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26104','75',N'Xuân Lập',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26113','75',N'Hàng Gòn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26116','75',N'Tân Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26119','75',N'Đak Lua',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26122','75',N'Nam Cát Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26134','75',N'Tà Lài',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26158','75',N'Phú Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26170','75',N'Trị An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26173','75',N'Phú Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26179','75',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26188','75',N'Tân Triều',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26206','75',N'Định Quán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26209','75',N'Thanh Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26215','75',N'Phú Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26221','75',N'Phú Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26227','75',N'La Ngà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26248','75',N'Trảng Bom',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26254','75',N'Bàu Hàm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26278','75',N'Bình Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26281','75',N'Hưng Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26296','75',N'An Viễn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26299','75',N'Thống Nhất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26311','75',N'Gia Kiệm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26326','75',N'Dầu Giây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26332','75',N'Xuân Quế',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26341','75',N'Cẩm Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26347','75',N'Xuân Đường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26359','75',N'Xuân Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26362','75',N'Sông Ray',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26368','75',N'Long Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26374','75',N'Tam Phước',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26377','75',N'Phước Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26380','75',N'Long Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26383','75',N'An Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26389','75',N'Bình An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26413','75',N'Long Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26422','75',N'Phước Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26425','75',N'Xuân Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26428','75',N'Xuân Bắc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26434','75',N'Xuân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26446','75',N'Xuân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26458','75',N'Xuân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26461','75',N'Xuân Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26485','75',N'Nhơn Trạch',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26491','75',N'Đại Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26503','75',N'Phước An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25747','79',N'Thủ Dầu Một',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25750','79',N'Phú Lợi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25760','79',N'Bình Dương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25768','79',N'Phú An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25771','79',N'Chánh Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25777','79',N'Dầu Tiếng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25780','79',N'Minh Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25792','79',N'Long Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25807','79',N'Thanh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25813','79',N'Bến Cát',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25819','79',N'Trừ Văn Thố',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25822','79',N'Bàu Bàng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25837','79',N'Chánh Phú Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25840','79',N'Long Nguyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25843','79',N'Tây Nam',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25846','79',N'Thới Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25849','79',N'Hoà Lợi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25858','79',N'Phú Giáo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25864','79',N'Phước Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25867','79',N'An Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25882','79',N'Phước Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25888','79',N'Tân Uyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25891','79',N'Tân Khánh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25906','79',N'Bắc Tân Uyên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25909','79',N'Thường Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25912','79',N'Vĩnh Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25915','79',N'Bình Cơ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25920','79',N'Tân Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25942','79',N'Dĩ An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25945','79',N'Tân Đông Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25951','79',N'Đông Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25966','79',N'Lái Thiêu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25969','79',N'Thuận Giao',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25975','79',N'An Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25978','79',N'Thuận An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25987','79',N'Bình Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26506','79',N'Vũng Tàu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26526','79',N'Tam Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26536','79',N'Rạch Dừa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26542','79',N'Phước Thắng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26545','79',N'Long Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26560','79',N'Bà Rịa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26566','79',N'Long Hương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26572','79',N'Tam Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26575','79',N'Ngãi Giao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26584','79',N'Xuân Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26590','79',N'Bình Giã',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26596','79',N'Châu Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26608','79',N'Kim Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26617','79',N'Nghĩa Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26620','79',N'Hồ Tràm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26632','79',N'Xuyên Mộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26638','79',N'Bàu Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26641','79',N'Hoà Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26647','79',N'Hoà Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26656','79',N'Bình Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26659','79',N'Long Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26662','79',N'Long Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26680','79',N'Đất Đỏ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26686','79',N'Phước Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26704','79',N'Phú Mỹ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26710','79',N'Tân Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26713','79',N'Tân Phước',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26725','79',N'Tân Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26728','79',N'Châu Pha',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('26732','79',N'Côn Đảo',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('26737','79',N'Tân Định',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26740','79',N'Sài Gòn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26743','79',N'Bến Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26758','79',N'Cầu Ông Lãnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26767','79',N'An Phú Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26773','79',N'Thới An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26782','79',N'Tân Thới Hiệp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26785','79',N'Trung Mỹ Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26791','79',N'Đông Hưng Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26800','79',N'Linh Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26803','79',N'Tam Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26809','79',N'Hiệp Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26824','79',N'Thủ Đức',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26833','79',N'Long Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26842','79',N'Tăng Nhơn Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26848','79',N'Phước Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26857','79',N'Long Phước',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26860','79',N'Long Trường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26876','79',N'An Nhơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26878','79',N'An Hội Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26882','79',N'An Hội Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26884','79',N'Gò Vấp',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26890','79',N'Hạnh Thông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26898','79',N'Thông Tây Hội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26905','79',N'Bình Lợi Trung',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26911','79',N'Bình Quới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26929','79',N'Bình Thạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26944','79',N'Gia Định',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26956','79',N'Thạnh Mỹ Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26968','79',N'Tân Sơn Nhất',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26977','79',N'Tân Sơn Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26983','79',N'Bảy Hiền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('26995','79',N'Tân Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27004','79',N'Tân Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27007','79',N'Tân Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27013','79',N'Tây Thạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27019','79',N'Tân Sơn Nhì',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27022','79',N'Phú Thọ Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27028','79',N'Phú Thạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27031','79',N'Tân Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27043','79',N'Đức Nhuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27058','79',N'Cầu Kiệu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27073','79',N'Phú Nhuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27094','79',N'An Khánh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27097','79',N'Bình Trưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27112','79',N'Cát Lái',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27139','79',N'Xuân Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27142','79',N'Nhiêu Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27154','79',N'Bàn Cờ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27163','79',N'Hoà Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27169','79',N'Diên Hồng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27190','79',N'Vườn Lài',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27211','79',N'Hoà Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27226','79',N'Phú Thọ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27232','79',N'Bình Thới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27238','79',N'Minh Phụng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27259','79',N'Xóm Chiếu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27265','79',N'Khánh Hội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27286','79',N'Vĩnh Hội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27301','79',N'Chợ Quán',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27316','79',N'An Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27343','79',N'Chợ Lớn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27349','79',N'Phú Lâm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27364','79',N'Bình Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27367','79',N'Bình Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27373','79',N'Bình Tiên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27418','79',N'Chánh Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27424','79',N'Bình Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27427','79',N'Phú Định',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27439','79',N'Bình Hưng Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27442','79',N'Bình Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27448','79',N'Bình Trị Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('27457','79',N'Tân Tạo',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27460','79',N'An Lạc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27475','79',N'Tân Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27478','79',N'Tân Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27484','79',N'Phú Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27487','79',N'Tân Mỹ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27496','79',N'Tân An Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27508','79',N'An Nhơn Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27511','79',N'Nhuận Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27526','79',N'Thái Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27541','79',N'Phú Hoà Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27544','79',N'Bình Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27553','79',N'Củ Chi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27559','79',N'Hóc Môn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27568','79',N'Đông Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27577','79',N'Xuân Thới Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27592','79',N'Bà Điểm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27595','79',N'Tân Nhựt',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27601','79',N'Vĩnh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27604','79',N'Tân Vĩnh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27610','79',N'Bình Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27619','79',N'Bình Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27628','79',N'Hưng Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27637','79',N'Bình Chánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27655','79',N'Nhà Bè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27658','79',N'Hiệp Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27664','79',N'Cần Giờ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27667','79',N'Bình Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27673','79',N'An Thới Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27676','79',N'Thạnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25459','80',N'Tân Ninh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25480','80',N'Bình Minh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25486','80',N'Tân Biên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25489','80',N'Tân Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25498','80',N'Thạnh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25510','80',N'Trà Vong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25516','80',N'Tân Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25522','80',N'Tân Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25525','80',N'Tân Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25531','80',N'Tân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25534','80',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25549','80',N'Tân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25552','80',N'Dương Minh Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25567','80',N'Ninh Thạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25573','80',N'Cầu Khởi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25579','80',N'Lộc Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25585','80',N'Châu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25588','80',N'Hảo Đước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25591','80',N'Phước Vinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25606','80',N'Hoà Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25621','80',N'Ninh Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25630','80',N'Long Hoa',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25633','80',N'Thanh Điền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25645','80',N'Hoà Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25654','80',N'Gò Dầu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25657','80',N'Thạnh Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25663','80',N'Phước Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25666','80',N'Truông Mít',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25672','80',N'Gia Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25681','80',N'Bến Cầu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25684','80',N'Long Chữ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25702','80',N'Long Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25708','80',N'Trảng Bàng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('25711','80',N'Hưng Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25729','80',N'Phước Chỉ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('25732','80',N'An Tịnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27694','80',N'Long An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27712','80',N'Tân An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27715','80',N'Khánh Hậu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27721','80',N'Tân Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27727','80',N'Hưng Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27736','80',N'Vĩnh Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27748','80',N'Vĩnh Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27757','80',N'Vĩnh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27763','80',N'Khánh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27775','80',N'Tuyên Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27787','80',N'Kiến Tường',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('27793','80',N'Bình Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27811','80',N'Bình Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27817','80',N'Tuyên Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27823','80',N'Mộc Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27826','80',N'Tân Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27838','80',N'Nhơn Hoà Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27841','80',N'Hậu Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27856','80',N'Nhơn Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27865','80',N'Thạnh Hoá',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27868','80',N'Bình Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27877','80',N'Thạnh Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27889','80',N'Tân Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27898','80',N'Đông Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27907','80',N'Mỹ Quý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27925','80',N'Đức Huệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27931','80',N'Hậu Nghĩa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27937','80',N'Đức Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27943','80',N'An Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27952','80',N'Hiệp Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27964','80',N'Đức Lập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27976','80',N'Mỹ Hạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27979','80',N'Hoà Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27991','80',N'Bến Lức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('27994','80',N'Thạnh Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28003','80',N'Lương Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28015','80',N'Bình Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28018','80',N'Mỹ Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28036','80',N'Thủ Thừa',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28051','80',N'Mỹ Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28066','80',N'Mỹ An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28072','80',N'Tân Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28075','80',N'Tân Trụ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28087','80',N'Nhựt Tảo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28093','80',N'Vàm Cỏ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28108','80',N'Cần Đước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28114','80',N'Rạch Kiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28126','80',N'Long Cang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28132','80',N'Mỹ Lệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28138','80',N'Tân Lân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28144','80',N'Long Hựu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28159','80',N'Cần Giuộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28165','80',N'Phước Lý',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28177','80',N'Mỹ Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28201','80',N'Phước Vĩnh Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28207','80',N'Tân Tập',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28210','80',N'Tầm Vu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28222','80',N'Vĩnh Công',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28225','80',N'Thuận Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28243','80',N'An Lục Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28249','82',N'Đạo Thạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28261','82',N'Mỹ Tho',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28270','82',N'Thới Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28273','82',N'Mỹ Phong',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28285','82',N'Trung An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28297','82',N'Long Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28306','82',N'Gò Công',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28315','82',N'Bình Xuân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28321','82',N'Tân Phước 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28327','82',N'Tân Phước 2',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28336','82',N'Hưng Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28345','82',N'Tân Phước 3',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28360','82',N'Cái Bè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28366','82',N'Hậu Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28378','82',N'Mỹ Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28393','82',N'Hội Cư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28405','82',N'Mỹ Đức Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28414','82',N'Mỹ Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28426','82',N'Thanh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28429','82',N'An Hữu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28435','82',N'Mỹ Phước Tây',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28436','82',N'Thanh Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28439','82',N'Cai Lậy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28444','82',N'Thạnh Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28456','82',N'Mỹ Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28468','82',N'Tân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28471','82',N'Bình Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28477','82',N'Nhị Quý',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28501','82',N'Hiệp Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28504','82',N'Long Tiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28516','82',N'Ngũ Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28519','82',N'Châu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28525','82',N'Tân Hương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28537','82',N'Long Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28543','82',N'Long Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28564','82',N'Bình Trưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28576','82',N'Vĩnh Kim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28582','82',N'Kim Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28594','82',N'Chợ Gạo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28603','82',N'Mỹ Tịnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28615','82',N'Lương Hoà Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28627','82',N'Tân Thuận Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28633','82',N'An Thạnh Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28648','82',N'Bình Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28651','82',N'Vĩnh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28660','82',N'Đồng Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28663','82',N'Phú Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28678','82',N'Vĩnh Hựu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28687','82',N'Long Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28693','82',N'Tân Thới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28696','82',N'Tân Phú Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28702','82',N'Tân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28720','82',N'Gia Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28723','82',N'Tân Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28729','82',N'Sơn Qui',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28738','82',N'Tân Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28747','82',N'Gò Công Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29869','82',N'Cao Lãnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29884','82',N'Mỹ Ngãi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29888','82',N'Mỹ Trà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29905','82',N'Sa Đéc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29926','82',N'Tân Hồng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29929','82',N'Tân Hộ Cơ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29938','82',N'Tân Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29944','82',N'An Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29954','82',N'An Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29955','82',N'Hồng Ngự',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29971','82',N'Thường Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29978','82',N'Thường Lạc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29983','82',N'Long Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29992','82',N'Long Phú Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30001','82',N'Tràm Chim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30010','82',N'Tam Nông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30019','82',N'An Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30025','82',N'Phú Cường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30028','82',N'An Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30034','82',N'Phú Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30037','82',N'Tháp Mười',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30043','82',N'Phương Thịnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30046','82',N'Trường Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30055','82',N'Mỹ Quí',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30061','82',N'Đốc Binh Kiều',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30073','82',N'Thanh Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30076','82',N'Mỹ Thọ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30085','82',N'Ba Sao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30088','82',N'Phong Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30112','82',N'Mỹ Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30118','82',N'Bình Hàng Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30130','82',N'Thanh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30154','82',N'Tân Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30157','82',N'Tân Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30163','82',N'Bình Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30169','82',N'Lấp Vò',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30178','82',N'Mỹ An Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30184','82',N'Tân Khánh Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30208','82',N'Hoà Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30214','82',N'Tân Dương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30226','82',N'Lai Vung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30235','82',N'Phong Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30244','82',N'Phú Hựu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30253','82',N'Tân Nhuận Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30259','82',N'Tân Phú Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28756','86',N'Phú Khương',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28777','86',N'An Hội',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28783','86',N'Sơn Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28789','86',N'Bến Tre',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28807','86',N'Giao Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28810','86',N'Phú Túc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28840','86',N'Tân Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28858','86',N'Phú Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('28861','86',N'Tiên Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28870','86',N'Chợ Lách',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28879','86',N'Phú Phụng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28894','86',N'Vĩnh Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28901','86',N'Hưng Khánh Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28903','86',N'Mỏ Cày',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28915','86',N'Phước Mỹ Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28921','86',N'Tân Thành Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28945','86',N'Đồng Khởi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28948','86',N'Nhuận Phú Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28957','86',N'An Định',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28969','86',N'Thành Thới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28981','86',N'Hương Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28984','86',N'Giồng Trôm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28987','86',N'Lương Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28993','86',N'Lương Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('28996','86',N'Châu Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29020','86',N'Phước Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29029','86',N'Tân Hào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29044','86',N'Hưng Nhượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29050','86',N'Bình Đại',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29062','86',N'Phú Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29077','86',N'Lộc Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29083','86',N'Châu Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29089','86',N'Thạnh Trị',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29104','86',N'Thạnh Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29107','86',N'Thới Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29110','86',N'Ba Tri',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29122','86',N'Mỹ Chánh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29125','86',N'Bảo Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29137','86',N'Tân Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29143','86',N'An Ngãi Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29158','86',N'An Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29167','86',N'Tân Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29182','86',N'Thạnh Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29191','86',N'Quới Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29194','86',N'Đại Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29221','86',N'Thạnh Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29224','86',N'An Qui',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29227','86',N'Thạnh Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29242','86',N'Trà Vinh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29254','86',N'Nguyệt Hoá',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29263','86',N'Long Đức',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29266','86',N'Càng Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29275','86',N'An Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29278','86',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29287','86',N'Bình Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29302','86',N'Nhị Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29308','86',N'Cầu Kè',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29317','86',N'An Phú Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29329','86',N'Phong Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29335','86',N'Tam Ngãi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29341','86',N'Tiểu Cần',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29362','86',N'Hùng Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29365','86',N'Tập Ngãi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29371','86',N'Tân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29374','86',N'Châu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29386','86',N'Song Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29398','86',N'Hoà Thuận',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29407','86',N'Hưng Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29410','86',N'Hoà Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29413','86',N'Long Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29416','86',N'Cầu Ngang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29419','86',N'Mỹ Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29431','86',N'Vinh Kim',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29446','86',N'Nhị Trường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29455','86',N'Hiệp Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29461','86',N'Trà Cú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29467','86',N'Tập Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29476','86',N'Lưu Nghiệp Anh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29489','86',N'Hàm Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29491','86',N'Đại An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29497','86',N'Đôn Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29506','86',N'Long Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29512','86',N'Duyên Hải',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29513','86',N'Long Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29516','86',N'Trường Long Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29518','86',N'Long Hữu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29530','86',N'Ngũ Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29533','86',N'Long Vĩnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29536','86',N'Đông Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29551','86',N'Long Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29557','86',N'Phước Hậu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29566','86',N'Tân Ngãi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29584','86',N'An Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29590','86',N'Thanh Đức',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29593','86',N'Tân Hạnh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29602','86',N'Long Hồ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29611','86',N'Phú Quới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29623','86',N'Nhơn Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29638','86',N'Bình Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29641','86',N'Cái Nhum',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29653','86',N'Tân Long Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29659','86',N'Trung Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29668','86',N'Quới An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29677','86',N'Quới Thiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29683','86',N'Trung Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29698','86',N'Trung Ngãi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29701','86',N'Hiếu Phụng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29713','86',N'Hiếu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29719','86',N'Tam Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29728','86',N'Cái Ngang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29734','86',N'Hoà Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29740','86',N'Song Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29767','86',N'Ngãi Tứ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29770','86',N'Cái Vồn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29771','86',N'Bình Minh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29785','86',N'Tân Lược',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29788','86',N'Mỹ Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29800','86',N'Tân Quới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29812','86',N'Đông Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('29821','86',N'Trà Ôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29830','86',N'Hoà Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29836','86',N'Trà Côn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29845','86',N'Vĩnh Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('29857','86',N'Lục Sĩ Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30292','91',N'Bình Đức',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30301','91',N'Mỹ Thới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30307','91',N'Long Xuyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30313','91',N'Mỹ Hoà Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30316','91',N'Châu Đốc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30325','91',N'Vĩnh Tế',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30337','91',N'An Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30341','91',N'Khánh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30346','91',N'Nhơn Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30352','91',N'Phú Hữu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30367','91',N'Vĩnh Hậu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30376','91',N'Tân Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30377','91',N'Long Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30385','91',N'Vĩnh Xương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30388','91',N'Tân An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30403','91',N'Châu Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30406','91',N'Phú Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30409','91',N'Chợ Vàm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30421','91',N'Phú Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30430','91',N'Hoà Lạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30436','91',N'Phú An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30445','91',N'Bình Thạnh Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30463','91',N'Châu Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30469','91',N'Mỹ Đức',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30478','91',N'Vĩnh Thạnh Trung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30481','91',N'Thạnh Mỹ Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30487','91',N'Bình Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30502','91',N'Thới Sơn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30505','91',N'Chi Lăng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30520','91',N'Tịnh Biên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30526','91',N'An Cư',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30538','91',N'Núi Cấm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30544','91',N'Tri Tôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30547','91',N'Ba Chúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30568','91',N'Vĩnh Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30577','91',N'Ô Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30580','91',N'Cô Tô',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30589','91',N'An Châu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30595','91',N'Cần Đăng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30604','91',N'Vĩnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30607','91',N'Bình Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30619','91',N'Vĩnh Hanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30628','91',N'Chợ Mới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30631','91',N'Long Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30643','91',N'Cù Lao Giêng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30658','91',N'Nhơn Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30664','91',N'Long Kiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30673','91',N'Hội An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30682','91',N'Thoại Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30685','91',N'Phú Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30688','91',N'Óc Eo',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30691','91',N'Tây Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30697','91',N'Vĩnh Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30709','91',N'Định Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30742','91',N'Rạch Giá',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30760','91',N'Vĩnh Thông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30766','91',N'Tô Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30769','91',N'Hà Tiên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('30781','91',N'Tiên Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30787','91',N'Kiên Lương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30790','91',N'Hoà Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30793','91',N'Vĩnh Điều',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30796','91',N'Giang Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30811','91',N'Sơn Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30814','91',N'Hòn Nghệ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30817','91',N'Hòn Đất',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30823','91',N'Bình Sơn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30826','91',N'Bình Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30835','91',N'Sơn Kiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30838','91',N'Mỹ Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30850','91',N'Tân Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30856','91',N'Tân Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30874','91',N'Thạnh Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30880','91',N'Châu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30886','91',N'Thạnh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30898','91',N'Bình An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30904','91',N'Giồng Riềng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30910','91',N'Thạnh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30928','91',N'Ngọc Chúc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30934','91',N'Hoà Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30943','91',N'Long Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30949','91',N'Hoà Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30952','91',N'Gò Quao',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30958','91',N'Định Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30970','91',N'Vĩnh Hoà Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30982','91',N'Vĩnh Tuy',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30985','91',N'An Biên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('30988','91',N'Tây Yên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31006','91',N'Đông Thái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31012','91',N'Vĩnh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31018','91',N'An Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31024','91',N'Đông Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31027','91',N'U Minh Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31031','91',N'Tân Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31036','91',N'Đông Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31042','91',N'Vân Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31051','91',N'Vĩnh Phong',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31064','91',N'Vĩnh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date));
    INSERT #WardSeed(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom) VALUES
        ('31069','91',N'Vĩnh Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31078','91',N'Phú Quốc',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('31105','91',N'Thổ Châu',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('31108','91',N'Kiên Hải',N'Đặc khu',CAST(1 AS bit),CAST(NULL AS date)),
        ('31120','92',N'Cái Khế',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31135','92',N'Ninh Kiều',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31147','92',N'Tân An',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31150','92',N'An Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31153','92',N'Ô Môn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31157','92',N'Thới Long',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31162','92',N'Phước Thới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31168','92',N'Bình Thuỷ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31174','92',N'Thới An Đông',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31183','92',N'Long Tuyền',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31186','92',N'Cái Răng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31201','92',N'Hưng Phú',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31207','92',N'Thốt Nốt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31213','92',N'Tân Lộc',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31217','92',N'Trung Nhứt',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31228','92',N'Thuận Hưng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31231','92',N'Thạnh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31232','92',N'Vĩnh Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31237','92',N'Vĩnh Trinh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31246','92',N'Thạnh Quới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31249','92',N'Thạnh Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31255','92',N'Trung Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31258','92',N'Thới Lai',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31261','92',N'Cờ Đỏ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31264','92',N'Thới Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31273','92',N'Đông Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31282','92',N'Đông Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31288','92',N'Trường Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31294','92',N'Trường Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31299','92',N'Phong Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31309','92',N'Trường Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31315','92',N'Nhơn Ái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31321','92',N'Vị Thanh',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31333','92',N'Vị Tân',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31338','92',N'Hoả Lựu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31340','92',N'Ngã Bảy',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31342','92',N'Tân Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31348','92',N'Trường Long Tây',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31360','92',N'Thạnh Xuân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31366','92',N'Châu Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31369','92',N'Đông Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31378','92',N'Phú Hữu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31393','92',N'Hoà An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31396','92',N'Hiệp Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31399','92',N'Tân Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31408','92',N'Thạnh Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31411','92',N'Đại Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31420','92',N'Phụng Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31426','92',N'Phương Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31432','92',N'Tân Phước Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31441','92',N'Vị Thuỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31453','92',N'Vĩnh Thuận Đông',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31459','92',N'Vĩnh Tường',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31465','92',N'Vị Thanh 1',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31471','92',N'Long Mỹ',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31473','92',N'Long Bình',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31480','92',N'Long Phú 1',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31489','92',N'Vĩnh Viễn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31492','92',N'Lương Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31495','92',N'Xà Phiên',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31507','92',N'Sóc Trăng',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31510','92',N'Phú Lợi',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31528','92',N'Kế Sách',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31531','92',N'An Lạc Thôn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31537','92',N'Phong Nẫm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31540','92',N'Thới An Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31552','92',N'Nhơn Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31561','92',N'Đại Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31567','92',N'Mỹ Tú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31569','92',N'Phú Tâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31570','92',N'Hồ Đắc Kiện',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31579','92',N'Long Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31582','92',N'Thuận Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31591','92',N'Mỹ Hương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31594','92',N'An Ninh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31603','92',N'Mỹ Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31615','92',N'An Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31633','92',N'Cù Lao Dung',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31639','92',N'Long Phú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31645','92',N'Đại Ngãi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31654','92',N'Trường Khánh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31666','92',N'Tân Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31673','92',N'Trần Đề',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31675','92',N'Liêu Tú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31679','92',N'Lịch Hội Thượng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31684','92',N'Mỹ Xuyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31687','92',N'Tài Văn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31699','92',N'Thạnh Thới An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31708','92',N'Nhu Gia',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31717','92',N'Hoà Tú',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31723','92',N'Ngọc Tố',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31726','92',N'Gia Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31732','92',N'Ngã Năm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31741','92',N'Tân Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31753','92',N'Mỹ Quới',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31756','92',N'Phú Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31759','92',N'Lâm Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31777','92',N'Vĩnh Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31783','92',N'Vĩnh Châu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31789','92',N'Khánh Hoà',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31795','92',N'Vĩnh Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31804','92',N'Vĩnh Phước',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31810','92',N'Lai Hoà',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31825','96',N'Bạc Liêu',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31834','96',N'Vĩnh Trạch',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31840','96',N'Hiệp Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31843','96',N'Hồng Dân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31849','96',N'Ninh Quới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31858','96',N'Vĩnh Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31864','96',N'Ninh Thạnh Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31867','96',N'Phước Long',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31876','96',N'Vĩnh Phước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31882','96',N'Vĩnh Thanh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31885','96',N'Phong Hiệp',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31891','96',N'Hoà Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31894','96',N'Châu Thới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31900','96',N'Vĩnh Lợi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31906','96',N'Hưng Hội',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31918','96',N'Vĩnh Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31927','96',N'Vĩnh Hậu',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31942','96',N'Giá Rai',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31951','96',N'Láng Tròn',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('31957','96',N'Phong Thạnh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31972','96',N'Gành Hào',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31975','96',N'Đông Hải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31985','96',N'Long Điền',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31988','96',N'An Trạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('31993','96',N'Định Thành',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32002','96',N'An Xuyên',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('32014','96',N'Lý Văn Lâm',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('32025','96',N'Tân Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('32041','96',N'Hoà Thành',N'Phường',CAST(1 AS bit),CAST(NULL AS date)),
        ('32044','96',N'Nguyễn Phích',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32047','96',N'U Minh',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32059','96',N'Khánh An',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32062','96',N'Khánh Lâm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32065','96',N'Thới Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32069','96',N'Biển Bạch',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32071','96',N'Trí Phải',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32083','96',N'Tân Lộc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32092','96',N'Hồ Thị Kỷ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32095','96',N'Trần Văn Thời',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32098','96',N'Sông Đốc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32104','96',N'Đá Bạc',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32110','96',N'Khánh Bình',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32119','96',N'Khánh Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32128','96',N'Cái Nước',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32134','96',N'Lương Thế Trân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32137','96',N'Tân Hưng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32140','96',N'Hưng Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32152','96',N'Đầm Dơi',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32155','96',N'Tạ An Khương',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32161','96',N'Trần Phán',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32167','96',N'Tân Thuận',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32182','96',N'Quách Phẩm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32185','96',N'Thanh Tùng',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32188','96',N'Tân Tiến',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32191','96',N'Năm Căn',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32201','96',N'Đất Mới',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32206','96',N'Tam Giang',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32212','96',N'Cái Đôi Vàm',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32214','96',N'Phú Mỹ',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32218','96',N'Phú Tân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32227','96',N'Nguyễn Việt Khái',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32236','96',N'Tân Ân',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32244','96',N'Phan Ngọc Hiển',N'Xã',CAST(1 AS bit),CAST(NULL AS date)),
        ('32248','96',N'Đất Mũi',N'Xã',CAST(1 AS bit),CAST(NULL AS date));

    MERGE dbo.AdministrativeProvinces AS target
    USING #ProvinceSeed AS source ON source.ProvinceCode = target.ProvinceCode
    WHEN MATCHED THEN UPDATE SET
        ProvinceName=source.ProvinceName,
        ProvinceType=source.ProvinceType,
        IsActive=source.IsActive,
        EffectiveFrom=COALESCE(target.EffectiveFrom,source.EffectiveFrom),
        EffectiveTo=NULL
    WHEN NOT MATCHED BY TARGET THEN
        INSERT(ProvinceCode,ProvinceName,ProvinceType,IsActive,EffectiveFrom)
        VALUES(source.ProvinceCode,source.ProvinceName,source.ProvinceType,source.IsActive,source.EffectiveFrom)
    WHEN NOT MATCHED BY SOURCE THEN
        UPDATE SET IsActive=0, EffectiveTo=COALESCE(target.EffectiveTo,CONVERT(date,SYSUTCDATETIME()));

    MERGE dbo.AdministrativeWards AS target
    USING #WardSeed AS source ON source.WardCode = target.WardCode
    WHEN MATCHED THEN UPDATE SET
        ProvinceCode=source.ProvinceCode,
        WardName=source.WardName,
        WardType=source.WardType,
        IsActive=source.IsActive,
        EffectiveFrom=COALESCE(target.EffectiveFrom,source.EffectiveFrom),
        EffectiveTo=NULL
    WHEN NOT MATCHED BY TARGET THEN
        INSERT(WardCode,ProvinceCode,WardName,WardType,IsActive,EffectiveFrom)
        VALUES(source.WardCode,source.ProvinceCode,source.WardName,source.WardType,source.IsActive,source.EffectiveFrom)
    WHEN NOT MATCHED BY SOURCE THEN
        UPDATE SET IsActive=0, EffectiveTo=COALESCE(target.EffectiveTo,CONVERT(date,SYSUTCDATETIME()));

    -- Cố gắng ánh xạ dữ liệu cũ theo tên; không đoán nếu tên không khớp duy nhất.
    UPDATE uv
       SET PermanentProvinceCode = p.ProvinceCode
    FROM dbo.UserVerifications uv
    JOIN dbo.AdministrativeProvinces p
      ON REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(uv.PermanentProvince)),N'TP. ',N''),N'Thành phố ',N''),N'Tỉnh ',N'') COLLATE Vietnamese_CI_AI
       = p.ProvinceName COLLATE Vietnamese_CI_AI
    WHERE uv.PermanentProvinceCode IS NULL AND NULLIF(LTRIM(RTRIM(uv.PermanentProvince)),N'') IS NOT NULL;

    UPDATE uv
       SET CurrentProvinceCode = p.ProvinceCode
    FROM dbo.UserVerifications uv
    JOIN dbo.AdministrativeProvinces p
      ON REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(uv.CurrentProvince)),N'TP. ',N''),N'Thành phố ',N''),N'Tỉnh ',N'') COLLATE Vietnamese_CI_AI
       = p.ProvinceName COLLATE Vietnamese_CI_AI
    WHERE uv.CurrentProvinceCode IS NULL AND NULLIF(LTRIM(RTRIM(uv.CurrentProvince)),N'') IS NOT NULL;

    UPDATE uv
       SET PermanentWardCode = w.WardCode
    FROM dbo.UserVerifications uv
    JOIN dbo.AdministrativeWards w ON w.ProvinceCode=uv.PermanentProvinceCode
      AND REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(uv.PermanentWard)),N'Phường ',N''),N'Xã ',N''),N'Đặc khu ',N'') COLLATE Vietnamese_CI_AI
        = w.WardName COLLATE Vietnamese_CI_AI
    WHERE uv.PermanentWardCode IS NULL AND NULLIF(LTRIM(RTRIM(uv.PermanentWard)),N'') IS NOT NULL;

    UPDATE uv
       SET CurrentWardCode = w.WardCode
    FROM dbo.UserVerifications uv
    JOIN dbo.AdministrativeWards w ON w.ProvinceCode=uv.CurrentProvinceCode
      AND REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(uv.CurrentWard)),N'Phường ',N''),N'Xã ',N''),N'Đặc khu ',N'') COLLATE Vietnamese_CI_AI
        = w.WardName COLLATE Vietnamese_CI_AI
    WHERE uv.CurrentWardCode IS NULL AND NULLIF(LTRIM(RTRIM(uv.CurrentWard)),N'') IS NOT NULL;

    -- Dọn mã không hợp lệ từ lần chạy thử hoặc dữ liệu nhập tay trước khi tạo khóa ngoại.
    UPDATE uv
       SET PermanentProvinceCode=NULL, PermanentWardCode=NULL
    FROM dbo.UserVerifications uv
    WHERE uv.PermanentProvinceCode IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.AdministrativeProvinces p WHERE p.ProvinceCode=uv.PermanentProvinceCode);

    UPDATE uv
       SET CurrentProvinceCode=NULL, CurrentWardCode=NULL
    FROM dbo.UserVerifications uv
    WHERE uv.CurrentProvinceCode IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.AdministrativeProvinces p WHERE p.ProvinceCode=uv.CurrentProvinceCode);

    UPDATE uv
       SET PermanentWardCode=NULL
    FROM dbo.UserVerifications uv
    WHERE uv.PermanentWardCode IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.AdministrativeWards w
          WHERE w.WardCode=uv.PermanentWardCode
            AND w.ProvinceCode=uv.PermanentProvinceCode
      );

    UPDATE uv
       SET CurrentWardCode=NULL
    FROM dbo.UserVerifications uv
    WHERE uv.CurrentWardCode IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.AdministrativeWards w
          WHERE w.WardCode=uv.CurrentWardCode
            AND w.ProvinceCode=uv.CurrentProvinceCode
      );

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_AdministrativeWards_AdministrativeProvinces')
        ALTER TABLE dbo.AdministrativeWards WITH CHECK ADD CONSTRAINT FK_AdministrativeWards_AdministrativeProvinces FOREIGN KEY(ProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_UserVerifications_PermanentProvince')
        ALTER TABLE dbo.UserVerifications WITH CHECK ADD CONSTRAINT FK_UserVerifications_PermanentProvince FOREIGN KEY(PermanentProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_UserVerifications_PermanentWard')
        ALTER TABLE dbo.UserVerifications WITH CHECK ADD CONSTRAINT FK_UserVerifications_PermanentWard FOREIGN KEY(PermanentWardCode) REFERENCES dbo.AdministrativeWards(WardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_UserVerifications_CurrentProvince')
        ALTER TABLE dbo.UserVerifications WITH CHECK ADD CONSTRAINT FK_UserVerifications_CurrentProvince FOREIGN KEY(CurrentProvinceCode) REFERENCES dbo.AdministrativeProvinces(ProvinceCode);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_UserVerifications_CurrentWard')
        ALTER TABLE dbo.UserVerifications WITH CHECK ADD CONSTRAINT FK_UserVerifications_CurrentWard FOREIGN KEY(CurrentWardCode) REFERENCES dbo.AdministrativeWards(WardCode);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AdministrativeProvinces_IsActive_ProvinceName' AND object_id=OBJECT_ID(N'dbo.AdministrativeProvinces'))
        CREATE INDEX IX_AdministrativeProvinces_IsActive_ProvinceName ON dbo.AdministrativeProvinces(IsActive,ProvinceName);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_AdministrativeWards_ProvinceCode_IsActive_WardName' AND object_id=OBJECT_ID(N'dbo.AdministrativeWards'))
        CREATE INDEX IX_AdministrativeWards_ProvinceCode_IsActive_WardName ON dbo.AdministrativeWards(ProvinceCode,IsActive,WardName);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserVerifications_PermanentAdministrativeCodes' AND object_id=OBJECT_ID(N'dbo.UserVerifications'))
        CREATE INDEX IX_UserVerifications_PermanentAdministrativeCodes ON dbo.UserVerifications(PermanentProvinceCode,PermanentWardCode);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_UserVerifications_CurrentAdministrativeCodes' AND object_id=OBJECT_ID(N'dbo.UserVerifications'))
        CREATE INDEX IX_UserVerifications_CurrentAdministrativeCodes ON dbo.UserVerifications(CurrentProvinceCode,CurrentWardCode);

    UPDATE dbo.SystemVersions SET IsCurrent=0 WHERE IsCurrent=1;
    IF EXISTS (SELECT 1 FROM dbo.SystemVersions WHERE DatabaseVersion=N'31.0')
        UPDATE dbo.SystemVersions
           SET ApplicationVersion=N'31.0.15', ReleasedDate=SYSUTCDATETIME(), IsCurrent=1,
               Notes=N'v31.0.15: danh mục hành chính hai cấp trong database, dropdown động và kiểm tra mã tỉnh/xã phía server.'
         WHERE DatabaseVersion=N'31.0';
    ELSE
        INSERT dbo.SystemVersions(ApplicationVersion,DatabaseVersion,ReleasedDate,IsCurrent,Notes)
        VALUES(N'31.0.15',N'31.0',SYSUTCDATETIME(),1,N'v31.0.15: danh mục hành chính hai cấp trong database, dropdown động và kiểm tra mã tỉnh/xã phía server.');

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20260717_SmartCar_v31_0_15_AdministrativeUnits')
        INSERT dbo.__EFMigrationsHistory(MigrationId,ProductVersion)
        VALUES(N'20260717_SmartCar_v31_0_15_AdministrativeUnits',N'8.0.11');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT COUNT(*) AS ActiveProvinceCount FROM dbo.AdministrativeProvinces WHERE IsActive=1;
SELECT COUNT(*) AS ActiveWardCount FROM dbo.AdministrativeWards WHERE IsActive=1;
SELECT TOP(20) ProvinceCode, ProvinceType, ProvinceName FROM dbo.AdministrativeProvinces WHERE IsActive=1 ORDER BY ProvinceName;
GO

-- ============================================================================
-- PHẦN CUỐI: TẠO 4 TÀI KHOẢN TEST
-- ============================================================================
/*
    SMARTCAR - TẠO 4 TÀI KHOẢN TEST MỚI
    Dùng cho môi trường LOCAL/DEMO, không dùng cho production.

    Tài khoản được tạo/kích hoạt:
      - quantri_test     | Admin
      - nhanvien_test    | Staff
      - doitac_test      | VehiclePartner
      - khachhang_test   | Customer

    Mật khẩu chung: a12345678
    Mật khẩu chỉ được lưu dưới dạng PBKDF2-SHA256, không lưu bản rõ trong AppUsers.

    Script có thể chạy lại an toàn:
      - Nếu username chưa tồn tại: tạo mới.
      - Nếu đã tồn tại: đặt lại mật khẩu, mở khóa và kích hoạt lại.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO


/* ============================================================================
   v31.0.15.2 - ĐỒNG BỘ 4 TÀI KHOẢN DEMO GỐC
   Username: quantri / nhanvien / doitac / khachhang
   Mật khẩu chung: a12345678
   ============================================================================ */
USE [SmartCarMarketplaceDb];
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
           EmailConfirmed = 1,
           IsActive = 1,
           IsDeleted = 0,
           FailedLoginCount = 0,
           LockoutEnd = NULL,
           LockType = NULL,
           LockReason = NULL,
           LockedAt = NULL,
           LockedByAppUserID = NULL,
           TokenVersion = TokenVersion + 1
     WHERE Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang');

    IF (SELECT COUNT(*) FROM dbo.AppUsers
        WHERE Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang')
          AND EmailConfirmed=1 AND IsActive=1 AND IsDeleted=0) <> 4
        THROW 52020, N'Không đồng bộ đủ 4 tài khoản demo gốc.', 1;

    UPDATE dbo.AdministrativeProvinces
       SET ProvinceType=N'Tỉnh', IsActive=1, EffectiveTo=NULL
     WHERE ProvinceCode='75' AND ProvinceName=N'Đồng Nai';

    IF NOT EXISTS (SELECT 1 FROM dbo.AdministrativeProvinces
                   WHERE ProvinceCode='75' AND ProvinceName=N'Đồng Nai' AND ProvinceType=N'Tỉnh' AND IsActive=1)
        THROW 52021, N'Không sửa được loại đơn vị hành chính của Đồng Nai.', 1;

    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1) <> 34
        THROW 52022, N'Số lượng tỉnh/thành phố hoạt động không bằng 34.', 1;

    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1 AND ProvinceType=N'Tỉnh') <> 28
        THROW 52023, N'Số lượng tỉnh hoạt động không bằng 28.', 1;

    IF (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive=1 AND ProvinceType=N'Thành phố') <> 6
        THROW 52024, N'Số lượng thành phố trực thuộc trung ương không bằng 6.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Đã đồng bộ 4 tài khoản demo gốc và sửa Đồng Nai thành Tỉnh.';
SELECT u.Username, N'a12345678' AS [Password], r.AppRoleName, u.EmailConfirmed, u.IsActive, u.IsDeleted
FROM dbo.AppUsers u
JOIN dbo.AppRoles r ON r.AppRoleId=u.AppRoleId
WHERE u.Username IN (N'quantri',N'nhanvien',N'doitac',N'khachhang')
ORDER BY u.AppUserId;
GO

IF DB_ID(N'SmartCarMarketplaceDb') IS NULL
    THROW 52001, N'Không tìm thấy database SmartCarMarketplaceDb.', 1;
GO

USE [SmartCarMarketplaceDb];
GO

IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL OR OBJECT_ID(N'dbo.AppRoles', N'U') IS NULL
    THROW 52002, N'Thiếu bảng AppUsers hoặc AppRoles. Hãy chạy đúng database SmartCar v31.0 trước.', 1;
GO

IF COL_LENGTH(N'dbo.AppUsers', N'AccountType') IS NULL
    THROW 52003, N'Database chưa có cột AppUsers.AccountType. Hãy nâng database lên v31.0 trước.', 1;
GO

DECLARE @AdminRoleId int = (SELECT TOP (1) AppRoleId FROM dbo.AppRoles WHERE AppRoleName = N'Admin');
DECLARE @StaffRoleId int = (SELECT TOP (1) AppRoleId FROM dbo.AppRoles WHERE AppRoleName = N'Staff');
DECLARE @CustomerRoleId int = (SELECT TOP (1) AppRoleId FROM dbo.AppRoles WHERE AppRoleName = N'Customer');
DECLARE @PartnerRoleId int = (SELECT TOP (1) AppRoleId FROM dbo.AppRoles WHERE AppRoleName = N'VehiclePartner');

IF @AdminRoleId IS NULL OR @StaffRoleId IS NULL OR @CustomerRoleId IS NULL OR @PartnerRoleId IS NULL
    THROW 52004, N'Thiếu một hoặc nhiều vai trò Admin/Staff/Customer/VehiclePartner trong AppRoles.', 1;

/*
   Bốn hash dưới đây đều xác minh bằng mật khẩu a12345678.
   Mỗi tài khoản có salt riêng theo PasswordSecurity của source SmartCar.
*/
DECLARE @AdminHash nvarchar(max) = N'PBKDF2-SHA256$120000$+iz/0agLHmtX2LtBzlnP4Q==$rV0X0k4Sb3Lk3RJPrNXLsFdVb2OzocxC7ncL/miET1I=';
DECLARE @StaffHash nvarchar(max) = N'PBKDF2-SHA256$120000$0wuxcistq87Ecoe5xBPi0Q==$lCfm+qvkxaiDjS921flx2udPbhgsLb3Y7tyukl+SdvQ=';
DECLARE @PartnerHash nvarchar(max) = N'PBKDF2-SHA256$120000$OGsnRH3WKvllseB4D/jp2A==$cnKBsz86jT0OQx8P3HP/JM+dqA0G9PxHIF0qVCJS2kk=';
DECLARE @CustomerHash nvarchar(max) = N'PBKDF2-SHA256$120000$jzUXLvFuR3jQC5Pza4Snww==$V22/M5qZOnsZfTM4iaXbdiyn+PxGJg2ufg2k3J3CUgE=';

BEGIN TRY
    BEGIN TRANSACTION;

    /* Chặn trường hợp email/số điện thoại test đã bị gắn vào username khác. */
    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers
        WHERE Username <> N'quantri_test'
          AND (Email = N'quantri.test@smartcar.local' OR Phone = N'0910000001')
          AND IsDeleted = 0)
        THROW 52010, N'Email hoặc số điện thoại của quantri_test đang thuộc tài khoản khác.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers
        WHERE Username <> N'nhanvien_test'
          AND (Email = N'nhanvien.test@smartcar.local' OR Phone = N'0910000002')
          AND IsDeleted = 0)
        THROW 52011, N'Email hoặc số điện thoại của nhanvien_test đang thuộc tài khoản khác.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers
        WHERE Username <> N'doitac_test'
          AND (Email = N'doitac.test@smartcar.local' OR Phone = N'0910000003')
          AND IsDeleted = 0)
        THROW 52012, N'Email hoặc số điện thoại của doitac_test đang thuộc tài khoản khác.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AppUsers
        WHERE Username <> N'khachhang_test'
          AND (Email = N'khachhang.test@smartcar.local' OR Phone = N'0910000004')
          AND IsDeleted = 0)
        THROW 52013, N'Email hoặc số điện thoại của khachhang_test đang thuộc tài khoản khác.', 1;

    /* ADMIN */
    IF EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = N'quantri_test')
    BEGIN
        UPDATE dbo.AppUsers
        SET [Password] = @AdminHash,
            [Name] = N'Test', [Surname] = N'Quản trị',
            Email = N'quantri.test@smartcar.local', Phone = N'0910000001',
            IsVehiclePartner = 0, AppRoleId = @AdminRoleId, AccountType = N'Admin',
            EmailConfirmed = 1, IsActive = 1, IsDeleted = 0,
            FailedLoginCount = 0, LockoutEnd = NULL, LastLoginAt = NULL,
            PendingEmail = NULL, PendingEmailCreatedDate = NULL, RegistrationExpiresDate = NULL,
            LockType = NULL, LockReason = NULL, LockedAt = NULL, LockedByAppUserID = NULL,
            BookingRestrictedUntil = NULL, BookingRestrictionReason = NULL,
            DeletedAt = NULL, DeletedByUserId = NULL, DeleteReason = NULL, AnonymizedAt = NULL,
            TokenVersion = TokenVersion + 1
        WHERE Username = N'quantri_test';
    END
    ELSE
    BEGIN
        INSERT dbo.AppUsers
        ([Username],[Password],[Name],[Surname],[Email],[Phone],[IsVehiclePartner],
         [FailedLoginCount],[EmailConfirmed],[TokenVersion],[IsDeleted],[IsActive],[AppRoleId],[AccountType])
        VALUES
        (N'quantri_test',@AdminHash,N'Test',N'Quản trị',N'quantri.test@smartcar.local',N'0910000001',0,
         0,1,0,0,1,@AdminRoleId,N'Admin');
    END;

    /* STAFF */
    IF EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = N'nhanvien_test')
    BEGIN
        UPDATE dbo.AppUsers
        SET [Password] = @StaffHash,
            [Name] = N'Test', [Surname] = N'Nhân viên',
            Email = N'nhanvien.test@smartcar.local', Phone = N'0910000002',
            IsVehiclePartner = 0, AppRoleId = @StaffRoleId, AccountType = N'Staff',
            EmailConfirmed = 1, IsActive = 1, IsDeleted = 0,
            FailedLoginCount = 0, LockoutEnd = NULL, LastLoginAt = NULL,
            PendingEmail = NULL, PendingEmailCreatedDate = NULL, RegistrationExpiresDate = NULL,
            LockType = NULL, LockReason = NULL, LockedAt = NULL, LockedByAppUserID = NULL,
            BookingRestrictedUntil = NULL, BookingRestrictionReason = NULL,
            DeletedAt = NULL, DeletedByUserId = NULL, DeleteReason = NULL, AnonymizedAt = NULL,
            TokenVersion = TokenVersion + 1
        WHERE Username = N'nhanvien_test';
    END
    ELSE
    BEGIN
        INSERT dbo.AppUsers
        ([Username],[Password],[Name],[Surname],[Email],[Phone],[IsVehiclePartner],
         [FailedLoginCount],[EmailConfirmed],[TokenVersion],[IsDeleted],[IsActive],[AppRoleId],[AccountType])
        VALUES
        (N'nhanvien_test',@StaffHash,N'Test',N'Nhân viên',N'nhanvien.test@smartcar.local',N'0910000002',0,
         0,1,0,0,1,@StaffRoleId,N'Staff');
    END;

    /* PARTNER */
    IF EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = N'doitac_test')
    BEGIN
        UPDATE dbo.AppUsers
        SET [Password] = @PartnerHash,
            [Name] = N'Test', [Surname] = N'Đối tác',
            Email = N'doitac.test@smartcar.local', Phone = N'0910000003',
            IsVehiclePartner = 1, AppRoleId = @PartnerRoleId, AccountType = N'Partner',
            EmailConfirmed = 1, IsActive = 1, IsDeleted = 0,
            FailedLoginCount = 0, LockoutEnd = NULL, LastLoginAt = NULL,
            PendingEmail = NULL, PendingEmailCreatedDate = NULL, RegistrationExpiresDate = NULL,
            LockType = NULL, LockReason = NULL, LockedAt = NULL, LockedByAppUserID = NULL,
            BookingRestrictedUntil = NULL, BookingRestrictionReason = NULL,
            DeletedAt = NULL, DeletedByUserId = NULL, DeleteReason = NULL, AnonymizedAt = NULL,
            TokenVersion = TokenVersion + 1
        WHERE Username = N'doitac_test';
    END
    ELSE
    BEGIN
        INSERT dbo.AppUsers
        ([Username],[Password],[Name],[Surname],[Email],[Phone],[IsVehiclePartner],
         [FailedLoginCount],[EmailConfirmed],[TokenVersion],[IsDeleted],[IsActive],[AppRoleId],[AccountType])
        VALUES
        (N'doitac_test',@PartnerHash,N'Test',N'Đối tác',N'doitac.test@smartcar.local',N'0910000003',1,
         0,1,0,0,1,@PartnerRoleId,N'Partner');
    END;

    /* Tạo hồ sơ nháp giống tài khoản đối tác được đăng ký và xác minh OTP bình thường. */
    DECLARE @PartnerAppUserId int = (SELECT AppUserId FROM dbo.AppUsers WHERE Username = N'doitac_test');
    IF OBJECT_ID(N'dbo.VehiclePartnerProfiles', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.VehiclePartnerProfiles WHERE AppUserID = @PartnerAppUserId)
    BEGIN
        INSERT dbo.VehiclePartnerProfiles
        ([AppUserID],[PartnerType],[FullName],[Phone],[Email],[Status],[CreatedDate],
         [PartnerTermsVersion],[PrivacyPolicyVersion],[TermsAcceptedAt],[PrivacyAcceptedAt])
        VALUES
        (@PartnerAppUserId,N'Cá nhân',N'Đối tác Test',N'0910000003',N'doitac.test@smartcar.local',N'Bản nháp',SYSUTCDATETIME(),
         N'Partner-Terms-v1.0',N'Privacy-v1.0',SYSUTCDATETIME(),SYSUTCDATETIME());
    END;

    /* CUSTOMER */
    IF EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = N'khachhang_test')
    BEGIN
        UPDATE dbo.AppUsers
        SET [Password] = @CustomerHash,
            [Name] = N'Test', [Surname] = N'Khách hàng',
            Email = N'khachhang.test@smartcar.local', Phone = N'0910000004',
            IsVehiclePartner = 0, AppRoleId = @CustomerRoleId, AccountType = N'Customer',
            EmailConfirmed = 1, IsActive = 1, IsDeleted = 0,
            FailedLoginCount = 0, LockoutEnd = NULL, LastLoginAt = NULL,
            PendingEmail = NULL, PendingEmailCreatedDate = NULL, RegistrationExpiresDate = NULL,
            LockType = NULL, LockReason = NULL, LockedAt = NULL, LockedByAppUserID = NULL,
            BookingRestrictedUntil = NULL, BookingRestrictionReason = NULL,
            DeletedAt = NULL, DeletedByUserId = NULL, DeleteReason = NULL, AnonymizedAt = NULL,
            TokenVersion = TokenVersion + 1
        WHERE Username = N'khachhang_test';
    END
    ELSE
    BEGIN
        INSERT dbo.AppUsers
        ([Username],[Password],[Name],[Surname],[Email],[Phone],[IsVehiclePartner],
         [FailedLoginCount],[EmailConfirmed],[TokenVersion],[IsDeleted],[IsActive],[AppRoleId],[AccountType])
        VALUES
        (N'khachhang_test',@CustomerHash,N'Test',N'Khách hàng',N'khachhang.test@smartcar.local',N'0910000004',0,
         0,1,0,0,1,@CustomerRoleId,N'Customer');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'Đã tạo/kích hoạt 4 tài khoản test. Mật khẩu chung: a12345678';

SELECT
    u.AppUserId,
    u.Username,
    N'a12345678' AS TestPassword,
    r.AppRoleName AS RoleName,
    u.AccountType,
    u.Email,
    u.Phone,
    u.EmailConfirmed,
    u.IsActive,
    u.IsDeleted
FROM dbo.AppUsers u
JOIN dbo.AppRoles r ON r.AppRoleId = u.AppRoleId
WHERE u.Username IN (N'quantri_test',N'nhanvien_test',N'doitac_test',N'khachhang_test')
ORDER BY CASE u.Username
    WHEN N'quantri_test' THEN 1
    WHEN N'nhanvien_test' THEN 2
    WHEN N'doitac_test' THEN 3
    WHEN N'khachhang_test' THEN 4
    ELSE 99 END;
GO


USE [SmartCarMarketplaceDb];
GO

PRINT N'============================================================';
PRINT N'HOÀN TẤT CÀI ĐẶT SMARTCAR v31.0.15.1';
PRINT N'Database: SmartCarMarketplaceDb';
PRINT N'Mật khẩu chung của 4 tài khoản test: a12345678';
PRINT N'============================================================';

SELECT
    DB_NAME() AS DatabaseName,
    (SELECT COUNT(*) FROM dbo.AdministrativeProvinces WHERE IsActive = 1) AS ActiveProvinceCount,
    (SELECT COUNT(*) FROM dbo.AdministrativeWards WHERE IsActive = 1) AS ActiveWardCount,
    (SELECT COUNT(*) FROM dbo.AppUsers
      WHERE Username IN (N'quantri_test', N'nhanvien_test', N'doitac_test', N'khachhang_test')
        AND IsActive = 1 AND IsDeleted = 0) AS ActiveTestAccountCount;

SELECT
    u.Username,
    N'a12345678' AS [Password],
    r.AppRoleName AS [Role],
    u.Email,
    u.IsActive
FROM dbo.AppUsers u
JOIN dbo.AppRoles r ON r.AppRoleId = u.AppRoleId
WHERE u.Username IN (N'quantri_test', N'nhanvien_test', N'doitac_test', N'khachhang_test')
ORDER BY CASE u.Username
    WHEN N'quantri_test' THEN 1
    WHEN N'nhanvien_test' THEN 2
    WHEN N'doitac_test' THEN 3
    WHEN N'khachhang_test' THEN 4
    ELSE 99
END;
GO
