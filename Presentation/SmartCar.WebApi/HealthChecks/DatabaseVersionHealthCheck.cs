using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartCar.Domain.SystemInfo;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.HealthChecks
{
    public sealed class DatabaseVersionHealthCheck : IHealthCheck
    {
        private static readonly string[] RequiredTables =
        [
            "__EFMigrationsHistory",
            "SystemVersions",
            "AppRoles",
            "AppUsers",
            "EmailVerificationOtps",
            "PasswordResetTokens",
            "Abouts",
            "Banners",
            "Brands",
            "Cars",
            "Features",
            "CarFeatures",
            "CarDescriptions",
            "Pricings",
            "CarPricings",
            "Categories",
            "Authors",
            "Blogs",
            "TagClouds",
            "Comments",
            "Contacts",
            "FooterAddresses",
            "Locations",
            "Services",
            "SocialMedias",
            "Testimonials",
            "RentACars",
            "Customer",
            "RentACarProcess",
            "VehiclePartnerProfiles",
            "VehiclePartnerApplications",
            "PartnerVehicles",
            "Reservations",
            "ReservationStatusHistories",
            "Reviews",
            "CompanyAnnouncements",
            "PlatformFeeSettings",
            "CommissionTransactions",
            "UserVerifications",
            "AdministrativeProvinces",
            "AdministrativeWards",
            "PrivateFiles",
            "Payments",
            "HandoverReports",
            "Disputes",
            "AuditLogs",
            "StaffOperationalIssues",
            "DataChangeHistories",
            "DataRetentionPolicies",
            "ArchivedRecords",
            "VehicleDocuments",
            "MaintenanceRecords",
            "Incidents",
            "TrafficFines",
            "DepositTransactions",
            "Settlements",
            "EmailOutboxes",
            "Notifications",
            "HandoverImages",
            "DisputeMessages",
            "AdditionalCharges",
            "FraudFlags",
            "WorkItemClaims",
            "OrphanedDataAudit",
            "PublicFileDeletionJobs",
            "RegistrationAttempts",
            "SystemSettings",
            "DriverProfiles",
            "BookingDriverAssignments",
            "VehiclePricingPlans",
            "VehicleAvailabilityBlocks",
            "BankAccountChangeRequests",
            "RefundTransactions"
        ];

        private static readonly (string Table, string Column)[] RequiredColumns =
        [
            ("AppUsers", "TokenVersion"),
            ("AppUsers", "PendingEmail"),
            ("AppUsers", "PendingEmailCreatedDate"),
            ("AppUsers", "RegistrationExpiresDate"),
            ("Reservations", "RowVersion"),
            ("Reservations", "PickUpDate"),
            ("Reservations", "PickUpTime"),
            ("Reservations", "DropOffDate"),
            ("Reservations", "DropOffTime"),
            ("Reservations", "CancellationPolicyVersion"),
            ("Payments", "RowVersion"),
            ("Payments", "IdempotencyKey"),
            ("Payments", "ProviderFeeAmount"),
            ("Payments", "ProviderFeeVerified"),
            ("Settlements", "RowVersion"),
            ("Settlements", "CreationIdempotencyKey"),
            ("Settlements", "PayoutIdempotencyKey"),
            ("PrivateFiles", "AttachedEntityType"),
            ("PrivateFiles", "AttachedEntityID"),
            ("PrivateFiles", "AttachedDate"),
            ("PrivateFiles", "DeleteRequestedDate"),
            ("PrivateFiles", "PhysicalDeletedDate"),
            ("PrivateFiles", "DeleteRetryCount"),
            ("PrivateFiles", "LastDeleteError"),
            ("PrivateFiles", "RowVersion"),
            ("UserVerifications", "CitizenIdFrontFileID"),
            ("UserVerifications", "CitizenIdBackFileID"),
            ("UserVerifications", "DriverLicenseFileID"),
            ("UserVerifications", "PortraitFileID"),
            ("UserVerifications", "PermanentProvinceCode"),
            ("UserVerifications", "PermanentWardCode"),
            ("UserVerifications", "CurrentProvinceCode"),
            ("UserVerifications", "CurrentWardCode"),
            ("VehiclePartnerProfiles", "PermanentProvinceCode"),
            ("VehiclePartnerProfiles", "PermanentWardCode"),
            ("VehiclePartnerProfiles", "CurrentProvinceCode"),
            ("VehiclePartnerProfiles", "CurrentWardCode"),
            ("VehiclePartnerProfiles", "HeadquartersProvinceCode"),
            ("VehiclePartnerProfiles", "HeadquartersWardCode"),
            ("AdministrativeProvinces", "ProvinceCode"),
            ("AdministrativeProvinces", "ProvinceName"),
            ("AdministrativeWards", "WardCode"),
            ("AdministrativeWards", "ProvinceCode"),
            ("AdministrativeWards", "WardName"),
            ("EmailOutboxes", "NextAttemptAt"),
            ("EmailOutboxes", "MessageKey"),
            ("EmailOutboxes", "LastAttemptAt"),
            ("EmailOutboxes", "LockedBy"),
            ("EmailOutboxes", "LockedUntil"),
            ("EmailOutboxes", "RowVersion"),
            ("Payments", "RelatedEntityType"),
            ("Payments", "RelatedEntityID"),
            ("AdditionalCharges", "PaymentID"),
            ("CommissionTransactions", "SettlementID"),
            ("PublicFileDeletionJobs", "FileUrl"),
            ("PublicFileDeletionJobs", "Status"),
            ("PublicFileDeletionJobs", "NextAttemptAt"),
            ("PublicFileDeletionJobs", "LastAttemptAt"),
            ("PublicFileDeletionJobs", "LockedBy"),
            ("PublicFileDeletionJobs", "LockedUntil"),
            ("PublicFileDeletionJobs", "DeletedDate"),
            ("PublicFileDeletionJobs", "RowVersion"),
            ("SystemVersions", "IsCurrent"),
            ("AppUsers", "AccountType"),
            ("PartnerVehicles", "ApprovalStatus"),
            ("PartnerVehicles", "OperationStatus"),
            ("PartnerVehicles", "InactiveReason"),
            ("Reservations", "RentalMode"),
            ("Reservations", "VehiclePricingPlanID"),
            ("Reservations", "PartnerResponseExpiresAt"),
            ("Reservations", "PaymentExpiresAt"),
            ("Reservations", "SurchargeProposalExpiresAt"),
            ("Reservations", "SurchargeResponseExpiresAt"),
            ("Reservations", "ReservationDepositAmount"),
            ("Reservations", "SecurityDepositAmount"),
            ("Locations", "ProvinceCity"),
            ("Locations", "District"),
            ("Locations", "Ward"),
            ("Locations", "AddressDetail"),
            ("Locations", "Latitude"),
            ("Locations", "Longitude"),
            ("Locations", "SearchRadiusKm"),
            ("Locations", "IsActive"),
            ("HandoverReports", "CustomerOtpHash"),
            ("HandoverReports", "PartnerOtpHash"),
            ("Settlements", "PartnerReviewDueDate"),
            ("VehiclePartnerProfiles", "IsPayoutPaused"),
            ("AdditionalCharges", "SubmittedDate"),
            ("Reviews", "ReservationID"),
            ("Reviews", "ReviewerRole"),
            ("Reviews", "TargetType")
        ];

        private static readonly (string Name, string ParentTable, string ParentColumn, string ReferencedTable, string ReferencedColumn)[] RequiredForeignKeys =
        [
            ("FK_AppUsers_AppRoles", "AppUsers", "AppRoleId", "AppRoles", "AppRoleId"),
            ("FK_Reservations_Customers", "Reservations", "CustomerAppUserID", "AppUsers", "AppUserId"),
            ("FK_Reservations_PartnerVehicles", "Reservations", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_Reservations_Cars", "Reservations", "CarID", "Cars", "CarID"),
            ("FK_Reservations_PickUpLocations", "Reservations", "PickUpLocationID", "Locations", "LocationID"),
            ("FK_Reservations_DropOffLocations", "Reservations", "DropOffLocationID", "Locations", "LocationID"),
            ("FK_Reservations_CancelledBy", "Reservations", "CancelledByAppUserID", "AppUsers", "AppUserId"),
            ("FK_Payments_Reservations", "Payments", "ReservationID", "Reservations", "ReservationID"),
            ("FK_Settlements_Reservations", "Settlements", "ReservationID", "Reservations", "ReservationID"),
            ("FK_HandoverReports_Reservations", "HandoverReports", "ReservationID", "Reservations", "ReservationID"),
            ("FK_Disputes_Reservations", "Disputes", "ReservationID", "Reservations", "ReservationID"),
            ("FK_Incidents_Reservations", "Incidents", "ReservationID", "Reservations", "ReservationID"),
            ("FK_TrafficFines_Reservations", "TrafficFines", "ReservationID", "Reservations", "ReservationID"),
            ("FK_DepositTransactions_Reservations", "DepositTransactions", "ReservationID", "Reservations", "ReservationID"),
            ("FK_AdditionalCharges_Reservations", "AdditionalCharges", "ReservationID", "Reservations", "ReservationID"),
            ("FK_AdditionalCharges_Payments", "AdditionalCharges", "PaymentID", "Payments", "PaymentID"),
            ("FK_ReservationStatusHistories_Reservations", "ReservationStatusHistories", "ReservationID", "Reservations", "ReservationID"),
            ("FK_ReservationStatusHistories_ChangedBy", "ReservationStatusHistories", "ChangedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_VehiclePartnerApplications_AppUsers", "VehiclePartnerApplications", "AppUserID", "AppUsers", "AppUserId"),
            ("FK_VehiclePartnerApplications_Locations", "VehiclePartnerApplications", "LocationID", "Locations", "LocationID"),
            ("FK_PartnerVehicles_Cars", "PartnerVehicles", "CarID", "Cars", "CarID"),
            ("FK_PartnerVehicles_Owners", "PartnerVehicles", "OwnerAppUserID", "AppUsers", "AppUserId"),
            ("FK_PartnerVehicles_Applications", "PartnerVehicles", "VehiclePartnerApplicationID", "VehiclePartnerApplications", "VehiclePartnerApplicationID"),
            ("FK_CommissionTransactions_Reservations", "CommissionTransactions", "ReservationID", "Reservations", "ReservationID"),
            ("FK_CommissionTransactions_PartnerVehicles", "CommissionTransactions", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_CommissionTransactions_AppUsers", "CommissionTransactions", "PartnerAppUserID", "AppUsers", "AppUserId"),
            ("FK_CommissionTransactions_Settlements", "CommissionTransactions", "SettlementID", "Settlements", "SettlementID"),
            ("FK_PrivateFiles_Owners", "PrivateFiles", "OwnerAppUserID", "AppUsers", "AppUserId"),
            ("FK_PrivateFiles_Reservations", "PrivateFiles", "ReservationID", "Reservations", "ReservationID"),
            ("FK_PrivateFiles_PartnerApplications", "PrivateFiles", "PartnerApplicationID", "VehiclePartnerApplications", "VehiclePartnerApplicationID"),
            ("FK_EmailVerificationOtps_AppUsers", "EmailVerificationOtps", "AppUserID", "AppUsers", "AppUserId"),
            ("FK_PasswordResetTokens_AppUsers", "PasswordResetTokens", "AppUserID", "AppUsers", "AppUserId"),
            ("FK_UserVerifications_AppUsers", "UserVerifications", "AppUserID", "AppUsers", "AppUserId"),
            ("FK_AdministrativeWards_AdministrativeProvinces", "AdministrativeWards", "ProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_UserVerifications_PermanentProvince", "UserVerifications", "PermanentProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_UserVerifications_PermanentWard", "UserVerifications", "PermanentWardCode", "AdministrativeWards", "WardCode"),
            ("FK_UserVerifications_CurrentProvince", "UserVerifications", "CurrentProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_UserVerifications_CurrentWard", "UserVerifications", "CurrentWardCode", "AdministrativeWards", "WardCode"),
            ("FK_VehiclePartnerProfiles_PermanentProvince", "VehiclePartnerProfiles", "PermanentProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_VehiclePartnerProfiles_PermanentWard", "VehiclePartnerProfiles", "PermanentWardCode", "AdministrativeWards", "WardCode"),
            ("FK_VehiclePartnerProfiles_CurrentProvince", "VehiclePartnerProfiles", "CurrentProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_VehiclePartnerProfiles_CurrentWard", "VehiclePartnerProfiles", "CurrentWardCode", "AdministrativeWards", "WardCode"),
            ("FK_VehiclePartnerProfiles_HeadquartersProvince", "VehiclePartnerProfiles", "HeadquartersProvinceCode", "AdministrativeProvinces", "ProvinceCode"),
            ("FK_VehiclePartnerProfiles_HeadquartersWard", "VehiclePartnerProfiles", "HeadquartersWardCode", "AdministrativeWards", "WardCode"),
            ("FK_WorkItemClaims_AssignedStaff", "WorkItemClaims", "AssignedStaffAppUserID", "AppUsers", "AppUserId"),
            ("FK_HandoverImages_HandoverReports", "HandoverImages", "HandoverReportID", "HandoverReports", "HandoverReportID"),
            ("FK_DisputeMessages_Disputes", "DisputeMessages", "DisputeID", "Disputes", "DisputeID"),
            ("FK_VehicleDocuments_PartnerVehicles", "VehicleDocuments", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_MaintenanceRecords_PartnerVehicles", "MaintenanceRecords", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_UserVerifications_ReviewedBy", "UserVerifications", "ReviewedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_UserVerifications_CitizenIdFrontFile", "UserVerifications", "CitizenIdFrontFileID", "PrivateFiles", "PrivateFileID"),
            ("FK_UserVerifications_CitizenIdBackFile", "UserVerifications", "CitizenIdBackFileID", "PrivateFiles", "PrivateFileID"),
            ("FK_UserVerifications_DriverLicenseFile", "UserVerifications", "DriverLicenseFileID", "PrivateFiles", "PrivateFileID"),
            ("FK_UserVerifications_PortraitFile", "UserVerifications", "PortraitFileID", "PrivateFiles", "PrivateFileID"),
            ("FK_HandoverReports_CreatedBy", "HandoverReports", "CreatedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_HandoverReports_ReplacedBy", "HandoverReports", "ReplacedByReportId", "HandoverReports", "HandoverReportID"),
            ("FK_Disputes_CreatedBy", "Disputes", "CreatedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_Disputes_AssignedStaff", "Disputes", "AssignedStaffAppUserID", "AppUsers", "AppUserId"),
            ("FK_Incidents_ReportedBy", "Incidents", "ReportedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_DepositTransactions_CreatedBy", "DepositTransactions", "CreatedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_AdditionalCharges_CreatedBy", "AdditionalCharges", "CreatedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_DisputeMessages_Sender", "DisputeMessages", "SenderAppUserID", "AppUsers", "AppUserId"),
            ("FK_VehicleDocuments_ReviewedBy", "VehicleDocuments", "ReviewedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_Settlements_CreatedBy", "Settlements", "CreatedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_Settlements_ApprovedBy", "Settlements", "ApprovedByAppUserID", "AppUsers", "AppUserId"),
            ("FK_Notifications_AppUsers", "Notifications", "AppUserID", "AppUsers", "AppUserId"),
            ("FK_DriverProfiles_Partner", "DriverProfiles", "PartnerAppUserID", "AppUsers", "AppUserId"),
            ("FK_BookingDriverAssignments_Reservations", "BookingDriverAssignments", "ReservationID", "Reservations", "ReservationID"),
            ("FK_BookingDriverAssignments_Drivers", "BookingDriverAssignments", "DriverProfileID", "DriverProfiles", "DriverProfileID"),
            ("FK_VehiclePricingPlans_PartnerVehicles", "VehiclePricingPlans", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_VehicleAvailabilityBlocks_PartnerVehicles", "VehicleAvailabilityBlocks", "PartnerVehicleID", "PartnerVehicles", "PartnerVehicleID"),
            ("FK_BankAccountChangeRequests_Profiles", "BankAccountChangeRequests", "VehiclePartnerProfileID", "VehiclePartnerProfiles", "VehiclePartnerProfileID"),
            ("FK_RefundTransactions_Reservations", "RefundTransactions", "ReservationID", "Reservations", "ReservationID"),
            ("FK_Reservations_VehiclePricingPlans", "Reservations", "VehiclePricingPlanID", "VehiclePricingPlans", "VehiclePricingPlanID")
        ];

        private static readonly (string Name, string Table)[] RequiredIndexes =
        [
            ("UX_SystemVersions_IsCurrent", "SystemVersions"),
            ("IX_AppUsers_Email", "AppUsers"),
            ("IX_AppUsers_Phone", "AppUsers"),
            ("IX_AppUsers_Username", "AppUsers"),
            ("IX_EmailVerificationOtps_AppUserID_Purpose_UsedDate_ExpiresDate", "EmailVerificationOtps"),
            ("IX_PasswordResetTokens_TokenHash", "PasswordResetTokens"),
            ("IX_Reservations_CarID_PickUpDate_DropOffDate", "Reservations"),
            ("IX_Reservations_PartnerVehicleID_Status", "Reservations"),
            ("IX_ReservationStatusHistories_ReservationID", "ReservationStatusHistories"),
            ("IX_CompanyAnnouncements_AudienceRole_IsActive_PublishDate", "CompanyAnnouncements"),
            ("IX_VehiclePartnerProfiles_AppUserID", "VehiclePartnerProfiles"),
            ("IX_VehiclePartnerProfiles_CitizenIdentityNumber", "VehiclePartnerProfiles"),
            ("IX_VehiclePartnerProfiles_TaxCode", "VehiclePartnerProfiles"),
            ("IX_VehiclePartnerProfiles_CitizenIdFingerprint", "VehiclePartnerProfiles"),
            ("IX_UserVerifications_CitizenIdFingerprint", "UserVerifications"),
            ("IX_VehiclePartnerApplications_LicensePlate", "VehiclePartnerApplications"),
            ("IX_PartnerVehicles_CarID", "PartnerVehicles"),
            ("IX_PartnerVehicles_VehiclePartnerApplicationID", "PartnerVehicles"),
            ("IX_CommissionTransactions_ReservationID", "CommissionTransactions"),
            ("IX_CommissionTransactions_SettlementID", "CommissionTransactions"),
            ("IX_UserVerifications_AppUserID_VerificationType", "UserVerifications"),
            ("IX_Payments_TransactionCode", "Payments"),
            ("IX_Payments_ReservationID_Status", "Payments"),
            ("IX_Payments_IdempotencyKey", "Payments"),
            ("IX_Payments_ReservationID_PaymentType", "Payments"),
            ("IX_Payments_RelatedEntity", "Payments"),
            ("IX_HandoverReports_ReservationID_ReportType", "HandoverReports"),
            ("IX_Disputes_ReservationID_Status", "Disputes"),
            ("IX_AuditLogs_EntityName_EntityID_CreatedDate", "AuditLogs"),
            ("IX_DataChangeHistories_EntityName_EntityID_ChangedAt", "DataChangeHistories"),
            ("IX_DataRetentionPolicies_EntityName", "DataRetentionPolicies"),
            ("IX_ArchivedRecords_EntityName_EntityID_ArchivedAt", "ArchivedRecords"),
            ("IX_VehicleDocuments_PartnerVehicleID_DocumentType", "VehicleDocuments"),
            ("IX_MaintenanceRecords_PartnerVehicleID_MaintenanceDate", "MaintenanceRecords"),
            ("IX_Incidents_ReservationID_Status", "Incidents"),
            ("IX_TrafficFines_NoticeNumber", "TrafficFines"),
            ("IX_DepositTransactions_ReservationID_Status", "DepositTransactions"),
            ("IX_DepositTransactions_TransactionCode", "DepositTransactions"),
            ("IX_DepositTransactions_ReservationID_Type", "DepositTransactions"),
            ("IX_Settlements_ReservationID", "Settlements"),
            ("IX_Settlements_PayoutTransactionCode", "Settlements"),
            ("IX_Settlements_CreationIdempotencyKey", "Settlements"),
            ("IX_Settlements_PayoutIdempotencyKey", "Settlements"),
            ("IX_EmailOutboxes_Status_NextAttemptAt_LockedUntil_CreatedDate", "EmailOutboxes"),
            ("IX_EmailOutboxes_MessageKey", "EmailOutboxes"),
            ("IX_Notifications_AppUserID_IsRead_CreatedDate", "Notifications"),
            ("IX_HandoverImages_HandoverReportID", "HandoverImages"),
            ("IX_DisputeMessages_DisputeID_CreatedDate", "DisputeMessages"),
            ("IX_AdditionalCharges_ReservationID_Status", "AdditionalCharges"),
            ("IX_AdditionalCharges_PaymentID", "AdditionalCharges"),
            ("IX_FraudFlags_Status_RiskScore", "FraudFlags"),
            ("IX_WorkItemClaims_QueueType_EntityID", "WorkItemClaims"),
            ("IX_WorkItemClaims_AssignedStaffAppUserID_Status", "WorkItemClaims"),
            ("IX_StaffOperationalIssues_StaffAppUserID_Status_CreatedDate", "StaffOperationalIssues"),
            ("IX_Reviews_ReservationID", "Reviews"),
            ("IX_Reservations_Customer_Status", "Reservations"),
            ("IX_Reservations_Car_Time", "Reservations"),
            ("IX_PrivateFiles_Owner_Category_Deleted", "PrivateFiles"),
            ("IX_PrivateFiles_ReservationID", "PrivateFiles"),
            ("IX_PrivateFiles_PartnerApplicationID", "PrivateFiles"),
            ("IX_PrivateFiles_Attachment", "PrivateFiles"),
            ("IX_PrivateFiles_IsDeleted_PhysicalDeletedDate_DeleteRequestedDate", "PrivateFiles"),
            ("IX_PublicFileDeletionJobs_Status_NextAttemptAt_LockedUntil_CreatedDate", "PublicFileDeletionJobs"),
            ("IX_PublicFileDeletionJobs_FileUrl", "PublicFileDeletionJobs"),
            ("IX_AppUsers_AccountType_Email", "AppUsers"),
            ("IX_AppUsers_AccountType_Phone", "AppUsers"),
            ("IX_RegistrationAttempts_AccountType_Email_Status", "RegistrationAttempts"),
            ("IX_SystemSettings_SettingKey", "SystemSettings"),
            ("IX_DriverProfiles_PartnerAppUserID_Status", "DriverProfiles"),
            ("IX_BookingDriverAssignments_ReservationID_Status", "BookingDriverAssignments"),
            ("IX_VehiclePricingPlans_PartnerVehicleID_ServiceType_IsActive", "VehiclePricingPlans"),
            ("IX_VehicleAvailabilityBlocks_PartnerVehicleID_StartUtc_EndUtc_IsActive", "VehicleAvailabilityBlocks"),
            ("IX_BankAccountChangeRequests_VehiclePartnerProfileID_Status", "BankAccountChangeRequests"),
            ("IX_RefundTransactions_ReservationID_Status", "RefundTransactions"),
            ("IX_AdministrativeProvinces_IsActive_ProvinceName", "AdministrativeProvinces"),
            ("IX_AdministrativeWards_ProvinceCode_IsActive_WardName", "AdministrativeWards"),
            ("IX_UserVerifications_PermanentAdministrativeCodes", "UserVerifications"),
            ("IX_UserVerifications_CurrentAdministrativeCodes", "UserVerifications"),
            ("IX_VehiclePartnerProfiles_PermanentAdministrativeCodes", "VehiclePartnerProfiles"),
            ("IX_VehiclePartnerProfiles_CurrentAdministrativeCodes", "VehiclePartnerProfiles"),
            ("IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes", "VehiclePartnerProfiles")
        ];

        private readonly CarBookContext _context;

        public DatabaseVersionHealthCheck(CarBookContext context) => _context = context;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!await _context.Database.CanConnectAsync(cancellationToken))
                    return HealthCheckResult.Unhealthy("Không kết nối được cơ sở dữ liệu SmartCar.");

                var current = await _context.SystemVersions.AsNoTracking()
                    .Where(x => x.IsCurrent)
                    .OrderByDescending(x => x.ReleasedDate)
                    .Select(x => new { x.ApplicationVersion, x.DatabaseVersion, x.ReleasedDate })
                    .FirstOrDefaultAsync(cancellationToken);

                if (current is null)
                    return HealthCheckResult.Unhealthy("Database chưa có bản ghi phiên bản hiện hành.");

                if (!string.Equals(current.DatabaseVersion, SmartCarRelease.DatabaseVersion, StringComparison.Ordinal))
                {
                    return HealthCheckResult.Unhealthy(
                        $"Sai phiên bản database. Source yêu cầu {SmartCarRelease.DatabaseVersion}, database hiện là {current.DatabaseVersion}.",
                        data: new Dictionary<string, object>
                        {
                            ["requiredDatabaseVersion"] = SmartCarRelease.DatabaseVersion,
                            ["actualDatabaseVersion"] = current.DatabaseVersion,
                            ["databaseReleaseDateUtc"] = current.ReleasedDate
                        });
                }

                var data = new Dictionary<string, object>
                {
                    ["applicationVersion"] = SmartCarRelease.ApplicationVersion,
                    ["databaseVersion"] = current.DatabaseVersion,
                    ["databaseReleaseDateUtc"] = current.ReleasedDate
                };

                if (_context.Database.IsRelational())
                {
                    var schema = await CheckRequiredSchemaAsync(cancellationToken);
                    data["requiredTables"] = RequiredTables.Length;
                    data["missingTables"] = schema.MissingTableCount;
                    data["requiredColumns"] = RequiredColumns.Length;
                    data["missingColumns"] = schema.MissingColumnCount;
                    data["requiredForeignKeys"] = RequiredForeignKeys.Length;
                    data["missingOrInvalidForeignKeys"] = schema.MissingForeignKeyCount;
                    data["untrustedForeignKeys"] = schema.UntrustedForeignKeyCount;
                    data["requiredIndexes"] = RequiredIndexes.Length;
                    data["missingOrDisabledIndexes"] = schema.MissingIndexCount;

                    if (!schema.IsValid)
                    {
                        return HealthCheckResult.Unhealthy(
                            "Database đúng số phiên bản nhưng thiếu hoặc sai bảng, cột, khóa ngoại hay index bắt buộc.",
                            data: data);
                    }
                }
                else
                {
                    data["schemaCheck"] = "Skipped for non-relational test provider";
                }

                return HealthCheckResult.Healthy("API, database và toàn bộ schema bắt buộc đúng phiên bản.", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Không kiểm tra được phiên bản/schema database.", ex);
            }
        }

        private async Task<SchemaSnapshot> CheckRequiredSchemaAsync(CancellationToken cancellationToken)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = BuildSchemaQuery();
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    throw new InvalidOperationException("Không đọc được thông tin schema.");

                return new SchemaSnapshot(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    reader.GetInt32(4));
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        private static string BuildSchemaQuery()
        {
            static string Literal(string value) => "N'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
            var tables = string.Join(",", RequiredTables.Select(x => $"({Literal(x)})"));
            var columns = string.Join(",", RequiredColumns.Select(x => $"({Literal(x.Table)},{Literal(x.Column)})"));
            var foreignKeys = string.Join(",", RequiredForeignKeys.Select(x =>
                $"({Literal(x.Name)},{Literal(x.ParentTable)},{Literal(x.ParentColumn)},{Literal(x.ReferencedTable)},{Literal(x.ReferencedColumn)})"));
            var indexes = string.Join(",", RequiredIndexes.Select(x => $"({Literal(x.Name)},{Literal(x.Table)})"));

            return $"""
                WITH RequiredTables([TableName]) AS (SELECT * FROM (VALUES {tables}) v([TableName])),
                RequiredColumns([TableName],[ColumnName]) AS (SELECT * FROM (VALUES {columns}) v([TableName],[ColumnName])),
                RequiredForeignKeys([FkName],[ParentTable],[ParentColumn],[ReferencedTable],[ReferencedColumn]) AS
                    (SELECT * FROM (VALUES {foreignKeys}) v([FkName],[ParentTable],[ParentColumn],[ReferencedTable],[ReferencedColumn])),
                RequiredIndexes([IndexName],[TableName]) AS (SELECT * FROM (VALUES {indexes}) v([IndexName],[TableName]))
                SELECT
                    (SELECT COUNT(*) FROM RequiredTables r
                     WHERE NOT EXISTS (SELECT 1 FROM sys.tables t WHERE t.[name]=r.[TableName] AND SCHEMA_NAME(t.[schema_id])=N'dbo')),
                    (SELECT COUNT(*) FROM RequiredColumns r
                     WHERE NOT EXISTS (
                        SELECT 1 FROM sys.tables t JOIN sys.columns c ON c.[object_id]=t.[object_id]
                        WHERE t.[name]=r.[TableName] AND c.[name]=r.[ColumnName] AND SCHEMA_NAME(t.[schema_id])=N'dbo')),
                    (SELECT COUNT(*) FROM RequiredForeignKeys r
                     WHERE NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys fk
                        JOIN sys.foreign_key_columns fkc ON fkc.[constraint_object_id]=fk.[object_id]
                        JOIN sys.tables pt ON pt.[object_id]=fk.[parent_object_id]
                        JOIN sys.columns pc ON pc.[object_id]=pt.[object_id] AND pc.[column_id]=fkc.[parent_column_id]
                        JOIN sys.tables rt ON rt.[object_id]=fk.[referenced_object_id]
                        JOIN sys.columns rc ON rc.[object_id]=rt.[object_id] AND rc.[column_id]=fkc.[referenced_column_id]
                        WHERE fk.[name]=r.[FkName] AND pt.[name]=r.[ParentTable] AND pc.[name]=r.[ParentColumn]
                          AND rt.[name]=r.[ReferencedTable] AND rc.[name]=r.[ReferencedColumn] AND fk.[is_disabled]=0)),
                    (SELECT COUNT(*) FROM RequiredForeignKeys r
                     JOIN sys.foreign_keys fk ON fk.[name]=r.[FkName]
                     WHERE fk.[is_not_trusted]=1 OR fk.[is_disabled]=1),
                    (SELECT COUNT(*) FROM RequiredIndexes r
                     WHERE NOT EXISTS (
                        SELECT 1 FROM sys.indexes i JOIN sys.tables t ON t.[object_id]=i.[object_id]
                        WHERE i.[name]=r.[IndexName] AND t.[name]=r.[TableName] AND i.[is_disabled]=0));
                """;
        }

        private sealed record SchemaSnapshot(
            int MissingTableCount,
            int MissingColumnCount,
            int MissingForeignKeyCount,
            int UntrustedForeignKeyCount,
            int MissingIndexCount)
        {
            public bool IsValid => MissingTableCount == 0 && MissingColumnCount == 0 &&
                                   MissingForeignKeyCount == 0 && UntrustedForeignKeyCount == 0 &&
                                   MissingIndexCount == 0;
        }
    }
}
