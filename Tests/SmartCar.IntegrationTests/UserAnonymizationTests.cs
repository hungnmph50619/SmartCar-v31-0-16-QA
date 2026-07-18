using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCar.Domain.Entities;
using SmartCar.WebApi.Services;

namespace SmartCar.IntegrationTests;

public class UserAnonymizationTests
{
    [Fact]
    public async Task Anonymize_ScrubsDirectIdentifiersAndQueuesPrivateFiles()
    {
        await using var db = TestDatabase.Create();
        var user = new AppUser
        {
            AppUserId = 100,
            AppRoleId = 1,
            Username = "personal-user",
            Password = "pbkdf2$hash",
            Name = "An",
            Surname = "Nguyen",
            Email = "an@example.com",
            Phone = "0912345678",
            IsDeleted = true,
            IsActive = false
        };
        var fileId = Guid.NewGuid();
        db.AppUsers.Add(user);
        db.UserVerifications.Add(new UserVerification
        {
            UserVerificationID = 10,
            AppUserID = 100,
            LegalFullName = "Nguyen An",
            CitizenIdMasked = "********1234",
            CitizenIdFingerprint = "fingerprint",
            CitizenIdFrontFileID = fileId,
            Status = "Đã xác minh"
        });
        db.VehiclePartnerProfiles.Add(new VehiclePartnerProfile
        {
            VehiclePartnerProfileID = 20,
            AppUserID = 100,
            FullName = "Nguyen An",
            Email = "an@example.com",
            Phone = "0912345678",
            CitizenIdentityNumber = "001234567890",
            BankAccountNumber = "123456789",
            Status = "Đã xác minh"
        });
        db.PrivateFiles.Add(new PrivateFile
        {
            PrivateFileID = fileId,
            OwnerAppUserID = 100,
            Category = "CustomerCitizenIdFront",
            OriginalFileName = "cccd.jpg",
            StoredFileName = $"{fileId:N}.jpg",
            ContentType = "image/jpeg",
            FileSize = 100,
            AttachedEntityType = nameof(UserVerification),
            AttachedEntityID = "10",
            AttachedDate = DateTime.UtcNow
        });
        db.DataChangeHistories.Add(new DataChangeHistory
        {
            EntityName = nameof(AppUser),
            EntityID = "100",
            Action = "Update",
            OldDataJson = "{\"email\":\"an@example.com\"}",
            NewDataJson = "{\"email\":\"new@example.com\"}",
            ChangedByAppUserID = 1
        });
        db.Reservations.Add(new Reservation
        {
            ReservationID = 30,
            CustomerAppUserID = 100,
            PartnerVehicleID = 1,
            CarID = 1,
            Name = "An",
            Surname = "Nguyen",
            Email = "an@example.com",
            Phone = "0912345678",
            Status = "Hoàn thành",
            PickUpDate = DateTime.Today.AddDays(-2),
            DropOffDate = DateTime.Today.AddDays(-1),
            PickUpTime = TimeSpan.FromHours(9),
            DropOffTime = TimeSpan.FromHours(9),
            Description = "Gọi cho tôi theo thông tin riêng",
            CreatedDate = DateTime.UtcNow.AddDays(-3)
        });
        db.DataChangeHistories.Add(new DataChangeHistory
        {
            EntityName = nameof(Reservation),
            EntityID = "30",
            Action = "Update",
            OldDataJson = "{\"description\":\"secret-value-not-token\"}",
            ChangedByAppUserID = 1
        });
        db.Reviews.Add(new Review
        {
            ReviewID = 40,
            AppUserID = 100,
            CustomerName = "Nguyen An",
            CustomerImage = "/avatar/an.jpg",
            Comment = "Liên hệ tôi qua số cá nhân",
            CarID = 1,
            ReviewDate = DateTime.UtcNow
        });
        db.Disputes.Add(new Dispute
        {
            DisputeID = 50,
            ReservationID = 30,
            CreatedByAppUserID = 100,
            Description = "Nội dung có dữ liệu cá nhân",
            EvidenceUrls = "/api/secure-files/test",
            Status = "Đã đóng"
        });
        db.FraudFlags.Add(new FraudFlag
        {
            FraudFlagID = 60,
            AppUserID = 100,
            RuleCode = "IDENTITY_DUPLICATE",
            Description = "CCCD trùng với hồ sơ khác",
            RiskScore = 20
        });
        db.EmailOutboxes.Add(new EmailOutbox
        {
            EmailOutboxID = 70,
            RecipientEmail = "an@example.com",
            Subject = "Thông tin riêng",
            Body = "Nội dung riêng",
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        var service = new UserAnonymizationService(
            db,
            new FakeEnvironment(),
            NullLogger<UserAnonymizationService>.Instance);
        var result = await service.AnonymizeAsync(100, 1, "Yêu cầu xóa dữ liệu", CancellationToken.None);

        var anonymized = await db.AppUsers.FindAsync(100);
        Assert.NotNull(anonymized!.AnonymizedAt);
        Assert.DoesNotContain("personal-user", anonymized.Username, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("an@example.com", anonymized.Email, StringComparison.OrdinalIgnoreCase);

        var verification = db.UserVerifications.Single(x => x.AppUserID == 100);
        Assert.Null(verification.CitizenIdFingerprint);
        Assert.Null(verification.CitizenIdFrontFileID);
        Assert.Equal("Đã ẩn danh", verification.Status);

        var privateFile = db.PrivateFiles.Single(x => x.PrivateFileID == fileId);
        Assert.True(privateFile.IsDeleted);
        Assert.NotNull(privateFile.DeleteRequestedDate);
        Assert.Null(privateFile.AttachedDate);

        Assert.DoesNotContain(db.DataChangeHistories,
            x => (x.OldDataJson ?? string.Empty).Contains("an@example.com", StringComparison.OrdinalIgnoreCase));
        Assert.Null(db.DataChangeHistories.Single(x => x.EntityName == nameof(Reservation) && x.EntityID == "30").OldDataJson);
        Assert.Null(db.Reservations.Single(x => x.ReservationID == 30).Description);
        Assert.Equal("[Nội dung đã được ẩn danh]", db.Reviews.Single(x => x.ReviewID == 40).Comment);
        Assert.Equal("[Nội dung người dùng đã được ẩn danh]", db.Disputes.Single(x => x.DisputeID == 50).Description);
        Assert.Equal("[Chi tiết định danh đã được ẩn danh]", db.FraudFlags.Single(x => x.FraudFlagID == 60).Description);
        var outbox = db.EmailOutboxes.Single(x => x.EmailOutboxID == 70);
        Assert.Equal("Cancelled", outbox.Status);
        Assert.Empty(outbox.Body);
        Assert.Equal(1, result.PrivateFilesQueuedForDeletion);
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SmartCar.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
