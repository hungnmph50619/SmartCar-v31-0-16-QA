using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Services;

public sealed record UserAnonymizationResult(
    int UserId,
    int PrivateFilesQueuedForDeletion,
    int VerificationRecordsScrubbed,
    int PartnerProfilesScrubbed,
    int PartnerApplicationsScrubbed,
    int ReservationSnapshotsScrubbed,
    int HistoryRecordsRedacted);

public interface IUserAnonymizationService
{
    Task<UserAnonymizationResult> AnonymizeAsync(
        int targetUserId,
        int actorUserId,
        string? reason,
        CancellationToken cancellationToken);
}

/// <summary>
/// Ẩn danh các định danh trực tiếp nhưng giữ khóa ngoại và số liệu giao dịch.
/// Không lưu bản sao dữ liệu trước khi ẩn danh vào audit/history.
/// </summary>
public sealed class UserAnonymizationService : IUserAnonymizationService
{
    private readonly CarBookContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserAnonymizationService> _logger;

    public UserAnonymizationService(
        CarBookContext db,
        IWebHostEnvironment environment,
        ILogger<UserAnonymizationService> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    public async Task<UserAnonymizationResult> AnonymizeAsync(
        int targetUserId,
        int actorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var user = await _db.AppUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.AppUserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

        if (!user.IsDeleted)
            throw new InvalidOperationException("Chỉ ẩn danh tài khoản đã ngừng hoạt động.");
        if (user.AnonymizedAt.HasValue)
            throw new InvalidOperationException("Tài khoản đã được ẩn danh trước đó.");

        var now = DateTime.UtcNow;
        // Không lưu nội dung lý do tự do vì có thể vô tình chứa chính dữ liệu cá nhân cần xóa.
        var safeReason = "Yêu cầu ẩn danh đã được quản trị viên xác nhận";

        // Chỉ dùng các giá trị cũ để tìm và xóa bản sao trong history; tuyệt đối không ghi lại.
        var sensitiveTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            user.Username,
            user.Email,
            user.Phone ?? string.Empty,
            user.Name,
            user.Surname,
            $"{user.Surname} {user.Name}".Trim()
        };

        var publicFilesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedEntityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            EntityKey(nameof(AppUser), targetUserId)
        };
        var historyRedacted = 0;
        var verificationCount = 0;
        var profileCount = 0;
        var applicationCount = 0;
        var reservationCount = 0;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var verifications = await _db.UserVerifications
                .Where(x => x.AppUserID == targetUserId)
                .ToListAsync(cancellationToken);
            verificationCount = verifications.Count;
            foreach (var item in verifications)
            {
                affectedEntityKeys.Add(EntityKey(nameof(UserVerification), item.UserVerificationID));
                AddToken(sensitiveTokens, item.LegalFullName);
                AddToken(sensitiveTokens, item.CitizenIdMasked);
                AddToken(sensitiveTokens, item.CitizenIdFingerprint);
                AddToken(sensitiveTokens, item.CitizenIdFrontFileID?.ToString());
                AddToken(sensitiveTokens, item.CitizenIdBackFileID?.ToString());
                AddToken(sensitiveTokens, item.DriverLicenseFileID?.ToString());
                AddToken(sensitiveTokens, item.PortraitFileID?.ToString());
                AddToken(sensitiveTokens, item.DriverLicenseNumber);
                AddToken(sensitiveTokens, item.CitizenIdAddress);
                AddToken(sensitiveTokens, item.PermanentAddress);
                AddToken(sensitiveTokens, item.CurrentAddress);

                item.LegalFullName = "Người dùng đã ẩn danh";
                item.Gender = null;
                item.CitizenIdMasked = null;
                item.CitizenIdFingerprint = null;
                item.CitizenIdIssuedDate = null;
                item.CitizenIdExpiryDate = null;
                item.CitizenIdAddress = null;
                item.PermanentProvince = null;
                item.PermanentWard = null;
                item.PermanentDetail = null;
                item.PermanentAddress = null;
                item.CurrentAddressSameAsPermanent = false;
                item.CurrentProvince = null;
                item.CurrentWard = null;
                item.CurrentDetail = null;
                item.CurrentAddress = null;
                item.DriverLicenseNumber = null;
                item.DriverLicenseClass = null;
                item.CitizenIdFrontFileID = null;
                item.CitizenIdBackFileID = null;
                item.DriverLicenseFileID = null;
                item.PortraitFileID = null;
                item.CitizenIdFrontUrl = null;
                item.CitizenIdBackUrl = null;
                item.DriverLicenseUrl = null;
                item.PortraitUrl = null;
                item.DateOfBirth = null;
                item.DriverLicenseIssuedDate = null;
                item.DriverLicenseExpiry = null;
                item.RejectionReason = null;
                item.Status = "Đã ẩn danh";
            }

            var partnerProfiles = await _db.VehiclePartnerProfiles
                .Where(x => x.AppUserID == targetUserId)
                .ToListAsync(cancellationToken);
            profileCount = partnerProfiles.Count;
            foreach (var profile in partnerProfiles)
            {
                affectedEntityKeys.Add(EntityKey(nameof(VehiclePartnerProfile), profile.VehiclePartnerProfileID));
                AddToken(sensitiveTokens, profile.FullName);
                AddToken(sensitiveTokens, profile.Email);
                AddToken(sensitiveTokens, profile.Phone);
                AddToken(sensitiveTokens, profile.CitizenIdentityNumber);
                AddToken(sensitiveTokens, profile.CitizenIdFingerprint);
                AddToken(sensitiveTokens, profile.BankAccountNumber);
                AddToken(sensitiveTokens, profile.TaxCode);
                AddToken(sensitiveTokens, profile.BusinessRegistrationNumber);

                profile.FullName = "Người dùng đã ẩn danh";
                profile.Phone = string.Empty;
                profile.Email = $"deleted_{targetUserId}@smartcar.local";
                profile.Address = string.Empty;
                profile.CitizenIdentityNumber = string.Empty;
                profile.CitizenIdFingerprint = null;
                profile.DateOfBirth = null;
                profile.Gender = string.Empty;
                profile.CitizenIssuedDate = null;
                profile.CitizenExpiryDate = null;
                profile.PermanentProvince = string.Empty;
                profile.PermanentWard = string.Empty;
                profile.PermanentDetail = string.Empty;
                profile.PermanentPaperAddress = string.Empty;
                profile.PermanentAddress = string.Empty;
                profile.CurrentAddressSameAsPermanent = false;
                profile.CurrentProvince = string.Empty;
                profile.CurrentWard = string.Empty;
                profile.CurrentDetail = string.Empty;
                profile.CurrentAddress = string.Empty;
                profile.CitizenFrontImageUrl = string.Empty;
                profile.CitizenBackImageUrl = string.Empty;
                profile.PortraitImageUrl = string.Empty;
                profile.BusinessName = string.Empty;
                profile.TaxCode = string.Empty;
                profile.BusinessRegistrationNumber = string.Empty;
                profile.HeadquartersProvince = string.Empty;
                profile.HeadquartersWard = string.Empty;
                profile.HeadquartersDetail = string.Empty;
                profile.HeadquartersPaperAddress = string.Empty;
                profile.HeadquartersAddress = string.Empty;
                profile.LegalRepresentativeName = string.Empty;
                profile.AccountManagerName = string.Empty;
                profile.AccountManagerTitle = string.Empty;
                profile.RepresentativeName = string.Empty;
                profile.RepresentativeTitle = string.Empty;
                profile.BusinessLicenseImageUrl = string.Empty;
                profile.AuthorizationDocumentUrl = string.Empty;
                profile.BankName = string.Empty;
                profile.BankAccountNumber = string.Empty;
                profile.BankAccountHolder = string.Empty;
                profile.BankBranch = string.Empty;
                profile.Status = "Đã ẩn danh";
                profile.ReviewNote = null;
            }

            var applications = await _db.VehiclePartnerApplications
                .Where(x => x.AppUserID == targetUserId)
                .ToListAsync(cancellationToken);
            applicationCount = applications.Count;
            foreach (var application in applications)
            {
                affectedEntityKeys.Add(EntityKey(nameof(VehiclePartnerApplication), application.VehiclePartnerApplicationID));
                AddToken(sensitiveTokens, application.OwnerFullName);
                AddToken(sensitiveTokens, application.Email);
                AddToken(sensitiveTokens, application.Phone);
                AddToken(sensitiveTokens, application.Address);
                AddToken(sensitiveTokens, application.CitizenIdentityNumber);
                AddToken(sensitiveTokens, application.BankAccountNumber);
                AddToken(sensitiveTokens, application.DriverFullName);
                AddToken(sensitiveTokens, application.DriverPhone);
                AddToken(sensitiveTokens, application.DriverCitizenIdentityNumber);
                AddToken(sensitiveTokens, application.DriverLicenseNumber);

                CollectPublicUrl(publicFilesToDelete, application.VehicleImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.FrontImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.RearImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.LeftImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.RightImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.InteriorImageUrl);
                CollectPublicUrl(publicFilesToDelete, application.DashboardImageUrl);

                application.OwnerFullName = "Người dùng đã ẩn danh";
                application.Email = $"deleted_{targetUserId}@smartcar.local";
                application.Phone = string.Empty;
                application.Address = string.Empty;
                application.CitizenIdentityNumber = string.Empty;
                application.BankName = string.Empty;
                application.BankAccountNumber = string.Empty;
                application.BankAccountHolder = string.Empty;
                application.ChassisNumber = $"ANON-{targetUserId}-{application.VehiclePartnerApplicationID}-C";
                application.EngineNumber = $"ANON-{targetUserId}-{application.VehiclePartnerApplicationID}-E";
                application.LicensePlate = $"AN{targetUserId:X8}{application.VehiclePartnerApplicationID:X8}";
                application.DriverFullName = null;
                application.DriverPhone = null;
                application.DriverCitizenIdentityNumber = null;
                application.DriverLicenseNumber = null;
                application.DriverLicenseClass = null;
                application.DriverLicenseExpiryDate = null;
                application.DriverLicenseImageUrl = null;
                application.DeliveryAddress = null;
                application.VehicleImageUrl = string.Empty;
                application.FrontImageUrl = string.Empty;
                application.RearImageUrl = string.Empty;
                application.LeftImageUrl = string.Empty;
                application.RightImageUrl = string.Empty;
                application.InteriorImageUrl = string.Empty;
                application.DashboardImageUrl = string.Empty;
                application.RegistrationImageUrl = string.Empty;
                application.InspectionImageUrl = string.Empty;
                application.InsuranceImageUrl = string.Empty;
                application.Status = "Đã ẩn danh";
                application.AdminNote = null;
            }

            var partnerVehicleIds = await _db.PartnerVehicles
                .Where(x => x.OwnerAppUserID == targetUserId)
                .Select(x => x.PartnerVehicleID)
                .ToListAsync(cancellationToken);
            var partnerVehicles = await _db.PartnerVehicles
                .Where(x => x.OwnerAppUserID == targetUserId)
                .ToListAsync(cancellationToken);
            foreach (var vehicle in partnerVehicles)
            {
                vehicle.IsActive = false;
                vehicle.PauseReason = "Chủ xe đã được ẩn danh";
            }

            var carIds = partnerVehicles.Select(x => x.CarID).Distinct().ToArray();
            if (carIds.Length > 0)
            {
                var cars = await _db.Cars.Where(x => carIds.Contains(x.CarID)).ToListAsync(cancellationToken);
                foreach (var car in cars)
                {
                    CollectPublicUrl(publicFilesToDelete, car.CoverImageUrl);
                    CollectPublicUrl(publicFilesToDelete, car.BigImageUrl);
                    car.CoverImageUrl = string.Empty;
                    car.BigImageUrl = string.Empty;
                    car.IsDeleted = true;
                    car.DeletedAt = now;
                    car.DeletedByUserId = actorUserId;
                    car.DeleteReason = "Chủ xe đã được ẩn danh";
                    car.LifecycleStatus = "Đã ẩn danh";
                }
            }

            if (partnerVehicleIds.Count > 0)
            {
                var vehicleDocuments = await _db.VehicleDocuments
                    .Where(x => partnerVehicleIds.Contains(x.PartnerVehicleID))
                    .ToListAsync(cancellationToken);
                foreach (var document in vehicleDocuments)
                {
                    affectedEntityKeys.Add(EntityKey(nameof(VehicleDocument), document.VehicleDocumentID));
                    AddToken(sensitiveTokens, document.DocumentNumber);
                    document.DocumentNumber = "ANONYMIZED";
                    document.FileUrl = string.Empty;
                    document.IssuedDate = null;
                    document.ExpiryDate = null;
                    document.RejectionReason = null;
                    document.Status = "Đã ẩn danh";
                }
            }

            var reservations = await _db.Reservations
                .Where(x => x.CustomerAppUserID == targetUserId)
                .ToListAsync(cancellationToken);
            reservationCount = reservations.Count;
            foreach (var reservation in reservations)
            {
                affectedEntityKeys.Add(EntityKey(nameof(Reservation), reservation.ReservationID));
                AddToken(sensitiveTokens, reservation.Name);
                AddToken(sensitiveTokens, reservation.Surname);
                AddToken(sensitiveTokens, reservation.Email);
                AddToken(sensitiveTokens, reservation.Phone);
                reservation.Name = "Người dùng";
                reservation.Surname = $"đã ẩn danh #{targetUserId}";
                reservation.Email = $"deleted_{targetUserId}@smartcar.local";
                reservation.Phone = string.Empty;
                reservation.Description = null;
            }

            var reviews = await _db.Reviews.Where(x => x.AppUserID == targetUserId).ToListAsync(cancellationToken);
            foreach (var review in reviews)
            {
                affectedEntityKeys.Add(EntityKey(nameof(Review), review.ReviewID));
                review.CustomerName = "Người dùng đã ẩn danh";
                review.CustomerImage = string.Empty;
                review.Comment = "[Nội dung đã được ẩn danh]";
            }

            var userDisputes = await _db.Disputes.Where(x => x.CreatedByAppUserID == targetUserId).ToListAsync(cancellationToken);
            foreach (var dispute in userDisputes)
            {
                affectedEntityKeys.Add(EntityKey(nameof(Dispute), dispute.DisputeID));
                dispute.Description = "[Nội dung người dùng đã được ẩn danh]";
                dispute.EvidenceUrls = null;
            }
            var userDisputeMessages = await _db.DisputeMessages.Where(x => x.SenderAppUserID == targetUserId).ToListAsync(cancellationToken);
            foreach (var message in userDisputeMessages)
            {
                affectedEntityKeys.Add(EntityKey(nameof(DisputeMessage), message.DisputeMessageID));
                message.Message = "[Nội dung người dùng đã được ẩn danh]";
                message.EvidenceUrls = null;
            }
            var userIncidents = await _db.Incidents.Where(x => x.ReportedByAppUserID == targetUserId).ToListAsync(cancellationToken);
            foreach (var incident in userIncidents)
            {
                affectedEntityKeys.Add(EntityKey(nameof(Incident), incident.IncidentID));
                incident.Description = "[Nội dung người dùng đã được ẩn danh]";
                incident.LocationText = null;
                incident.EvidenceUrls = null;
            }
            var fraudFlags = await _db.FraudFlags.Where(x => x.AppUserID == targetUserId).ToListAsync(cancellationToken);
            foreach (var flag in fraudFlags)
            {
                affectedEntityKeys.Add(EntityKey(nameof(FraudFlag), flag.FraudFlagID));
                flag.Description = "[Chi tiết định danh đã được ẩn danh]";
            }

            var notifications = await _db.Notifications.Where(x => x.AppUserID == targetUserId).ToListAsync(cancellationToken);
            _db.Notifications.RemoveRange(notifications);

            var otpRows = await _db.EmailVerificationOtps.Where(x => x.AppUserID == targetUserId).ToListAsync(cancellationToken);
            _db.EmailVerificationOtps.RemoveRange(otpRows);
            var resetRows = await _db.PasswordResetTokens.Where(x => x.AppUserID == targetUserId).ToListAsync(cancellationToken);
            _db.PasswordResetTokens.RemoveRange(resetRows);

            var privateFiles = await _db.PrivateFiles
                .Where(x => x.OwnerAppUserID == targetUserId && !x.IsDeleted)
                .ToListAsync(cancellationToken);
            await ClearPrivateFileReferencesAsync(privateFiles, cancellationToken);
            foreach (var file in privateFiles)
            {
                file.IsDeleted = true;
                file.DeleteRequestedDate = now;
                file.LastDeleteError = null;
                file.AttachedEntityType = null;
                file.AttachedEntityID = null;
                file.AttachedDate = null;
            }

            var outboxes = await _db.EmailOutboxes
                .Where(x => x.RecipientEmail == user.Email)
                .ToListAsync(cancellationToken);
            foreach (var outbox in outboxes)
            {
                outbox.RecipientEmail = $"deleted_{targetUserId}@smartcar.local";
                outbox.Subject = "[Đã ẩn danh]";
                outbox.Body = string.Empty;
                if (outbox.Status is "Pending" or "Retry" or "Processing" or "Sending" or "DeliveryUnknown" or "Failed")
                    outbox.Status = "Cancelled";
                outbox.LockedBy = null;
                outbox.LockedUntil = null;
                outbox.LastError = null;
            }

            historyRedacted += await RedactHistoricalCopiesAsync(sensitiveTokens, affectedEntityKeys, targetUserId, cancellationToken);

            user.Name = "Người dùng";
            user.Surname = $"đã xóa #{targetUserId}";
            user.Email = $"deleted_{targetUserId}_{Guid.NewGuid():N}@smartcar.local";
            user.Phone = null;
            user.PendingEmail = null;
            user.PendingEmailCreatedDate = null;
            user.RegistrationExpiresDate = null;
            user.Username = $"deleted_{targetUserId}_{Guid.NewGuid():N}";
            user.Password = PasswordSecurity.Hash(Guid.NewGuid().ToString("N"));
            user.IsActive = false;
            user.IsVehiclePartner = false;
            user.TokenVersion++;
            user.LockReason = null;
            user.BookingRestrictionReason = null;
            user.AnonymizedAt = now;

            _db.DataChangeHistories.Add(new DataChangeHistory
            {
                EntityName = nameof(AppUser),
                EntityID = targetUserId.ToString(),
                Action = "Anonymize",
                OldDataJson = null,
                NewDataJson = "{\"anonymized\":true}",
                Reason = safeReason,
                ChangedByAppUserID = actorUserId,
                ChangedAt = now
            });
            _db.AuditLogs.Add(new AuditLog
            {
                AppUserID = actorUserId,
                Action = "Ẩn danh tài khoản",
                EntityName = nameof(AppUser),
                EntityID = targetUserId.ToString(),
                Note = "Đã xóa định danh trực tiếp, tài liệu riêng tư và bản sao dữ liệu lịch sử; giữ khóa giao dịch.",
                CreatedDate = now
            });

            await EnqueuePublicFileDeletionJobsAsync(publicFilesToDelete, now, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UserAnonymizationResult(
                targetUserId,
                privateFiles.Count,
                verificationCount,
                profileCount,
                applicationCount,
                reservationCount,
                historyRedacted);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task ClearPrivateFileReferencesAsync(
        IReadOnlyCollection<PrivateFile> files,
        CancellationToken cancellationToken)
    {
        var grouped = files
            .Where(x => !string.IsNullOrWhiteSpace(x.AttachedEntityType) && !string.IsNullOrWhiteSpace(x.AttachedEntityID))
            .GroupBy(x => x.AttachedEntityType!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => ParseNumericId(x.AttachedEntityID)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);

        if (grouped.TryGetValue(nameof(Incident), out var incidentIds) && incidentIds.Length > 0)
            foreach (var item in await _db.Incidents.Where(x => incidentIds.Contains(x.IncidentID)).ToListAsync(cancellationToken)) item.EvidenceUrls = null;
        if (grouped.TryGetValue(nameof(TrafficFine), out var fineIds) && fineIds.Length > 0)
            foreach (var item in await _db.TrafficFines.Where(x => fineIds.Contains(x.TrafficFineID)).ToListAsync(cancellationToken)) item.EvidenceUrl = null;
        if (grouped.TryGetValue(nameof(AdditionalCharge), out var chargeIds) && chargeIds.Length > 0)
            foreach (var item in await _db.AdditionalCharges.Where(x => chargeIds.Contains(x.AdditionalChargeID)).ToListAsync(cancellationToken)) item.EvidenceUrls = null;
        if (grouped.TryGetValue(nameof(Dispute), out var disputeIds) && disputeIds.Length > 0)
            foreach (var item in await _db.Disputes.Where(x => disputeIds.Contains(x.DisputeID)).ToListAsync(cancellationToken)) item.EvidenceUrls = null;
        if (grouped.TryGetValue(nameof(DisputeMessage), out var messageIds) && messageIds.Length > 0)
            foreach (var item in await _db.DisputeMessages.Where(x => messageIds.Contains(x.DisputeMessageID)).ToListAsync(cancellationToken)) item.EvidenceUrls = null;
        if (grouped.TryGetValue(nameof(HandoverReport), out var reportIds) && reportIds.Length > 0)
        {
            foreach (var item in await _db.HandoverReports.Where(x => reportIds.Contains(x.HandoverReportID)).ToListAsync(cancellationToken)) item.PhotoUrls = null;
            foreach (var image in await _db.HandoverImages.Where(x => reportIds.Contains(x.HandoverReportID)).ToListAsync(cancellationToken))
            {
                image.FileUrl = string.Empty;
                image.FileHash = null;
            }
        }
        if (grouped.TryGetValue(nameof(VehicleDocument), out var documentIds) && documentIds.Length > 0)
            foreach (var item in await _db.VehicleDocuments.Where(x => documentIds.Contains(x.VehicleDocumentID)).ToListAsync(cancellationToken)) item.FileUrl = string.Empty;
    }

    private async Task<int> RedactHistoricalCopiesAsync(
        IReadOnlyCollection<string> rawTokens,
        IReadOnlySet<string> affectedEntityKeys,
        int targetUserId,
        CancellationToken cancellationToken)
    {
        var tokens = rawTokens.Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length >= 3)
            .Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var changed = 0;

        var histories = await _db.DataChangeHistories
            .Where(x => x.EntityID == targetUserId.ToString() || x.OldDataJson != null || x.NewDataJson != null)
            .ToListAsync(cancellationToken);
        foreach (var history in histories)
        {
            if (affectedEntityKeys.Contains(EntityKey(history.EntityName, history.EntityID)) ||
                ContainsAny(history.OldDataJson, tokens) || ContainsAny(history.NewDataJson, tokens) || ContainsAny(history.Reason, tokens))
            {
                history.OldDataJson = null;
                history.NewDataJson = "{\"redacted\":true}";
                if (ContainsAny(history.Reason, tokens)) history.Reason = "[Đã ẩn danh]";
                changed++;
            }
        }

        var audits = await _db.AuditLogs
            .Where(x => x.AppUserID == targetUserId || x.OldValues != null || x.NewValues != null || x.Note != null)
            .ToListAsync(cancellationToken);
        foreach (var audit in audits)
        {
            if (audit.AppUserID == targetUserId ||
                affectedEntityKeys.Contains(EntityKey(audit.EntityName, audit.EntityID)) ||
                ContainsAny(audit.OldValues, tokens) || ContainsAny(audit.NewValues, tokens) || ContainsAny(audit.Note, tokens))
            {
                audit.OldValues = null;
                audit.NewValues = null;
                if (ContainsAny(audit.Note, tokens)) audit.Note = "[Nội dung đã được ẩn danh]";
                changed++;
            }
        }

        var archives = await _db.ArchivedRecords.Where(x => x.DataJson != null).ToListAsync(cancellationToken);
        foreach (var archive in archives)
        {
            if (!affectedEntityKeys.Contains(EntityKey(archive.EntityName, archive.EntityID)) && !ContainsAny(archive.DataJson, tokens)) continue;
            archive.DataJson = "{\"redacted\":true}";
            changed++;
        }
        return changed;
    }

    private async Task EnqueuePublicFileDeletionJobsAsync(
        IReadOnlyCollection<string> urls,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (urls.Count == 0) return;
        var normalized = urls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var existing = await _db.PublicFileDeletionJobs
            .Where(x => normalized.Contains(x.FileUrl))
            .Select(x => x.FileUrl)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var url in normalized.Where(x => !existingSet.Contains(x)))
        {
            _db.PublicFileDeletionJobs.Add(new PublicFileDeletionJob
            {
                FileUrl = url,
                Status = "Pending",
                CreatedDate = now,
                NextAttemptAt = now
            });
        }
    }

    private static void CollectPublicUrl(ISet<string> destination, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("/uploads/vehicle-images/", StringComparison.OrdinalIgnoreCase))
            destination.Add(value.Trim());
    }

    private static string EntityKey(string? entityName, object? entityId)
        => $"{entityName ?? string.Empty}:{entityId?.ToString() ?? string.Empty}";

    private static int? ParseNumericId(string? value)
        => int.TryParse(value, out var id) ? id : null;

    private static void AddToken(ISet<string> destination, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) destination.Add(value.Trim());
    }

    private static bool ContainsAny(string? value, IReadOnlyCollection<string> tokens)
        => !string.IsNullOrWhiteSpace(value) && tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}
