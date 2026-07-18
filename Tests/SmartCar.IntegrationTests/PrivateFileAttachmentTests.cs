using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using SmartCar.Domain.Entities;
using SmartCar.WebApi.Services;

namespace SmartCar.IntegrationTests;

public class PrivateFileAttachmentTests
{
    [Fact]
    public async Task CorrectOwnerCategoryAndReservation_AreAccepted()
    {
        await using var db = TestDatabase.Create();
        var id = Guid.NewGuid();
        db.PrivateFiles.Add(new PrivateFile
        {
            PrivateFileID = id,
            OwnerAppUserID = 10,
            ReservationID = 99,
            Category = "IncidentEvidence",
            OriginalFileName = "incident.jpg",
            StoredFileName = $"{id:N}.jpg",
            ContentType = "image/jpeg",
            FileSize = 100,
            CreatedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new PrivateFileService(db, new FakeWebHostEnvironment());

        var result = await service.ValidateForAttachmentAsync(
            new[] { id }, 10, "IncidentEvidence", 99, false, CancellationToken.None);

        Assert.Single(result);
        service.MarkAttached(result, "Incident", "123");
        await db.SaveChangesAsync();
        Assert.Equal("Incident", result[0].AttachedEntityType);
        Assert.Equal("123", result[0].AttachedEntityID);
        Assert.NotNull(result[0].AttachedDate);
    }

    [Theory]
    [InlineData(11, "IncidentEvidence", 99)]
    [InlineData(10, "DisputeEvidence", 99)]
    [InlineData(10, "IncidentEvidence", 100)]
    public async Task WrongOwnerCategoryOrReservation_IsRejected(int userId, string category, int reservationId)
    {
        await using var db = TestDatabase.Create();
        var id = Guid.NewGuid();
        db.PrivateFiles.Add(new PrivateFile
        {
            PrivateFileID = id,
            OwnerAppUserID = 10,
            ReservationID = 99,
            Category = "IncidentEvidence",
            OriginalFileName = "incident.jpg",
            StoredFileName = $"{id:N}.jpg",
            ContentType = "image/jpeg",
            FileSize = 100,
            CreatedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new PrivateFileService(db, new FakeWebHostEnvironment());

        await Assert.ThrowsAnyAsync<Exception>(() => service.ValidateForAttachmentAsync(
            new[] { id }, userId, category, reservationId, false, CancellationToken.None));
    }

    [Fact]
    public async Task AlreadyAttachedFile_CannotBeReused()
    {
        await using var db = TestDatabase.Create();
        var id = Guid.NewGuid();
        db.PrivateFiles.Add(new PrivateFile
        {
            PrivateFileID = id,
            OwnerAppUserID = 10,
            Category = "PartnerDocuments",
            OriginalFileName = "cccd.jpg",
            StoredFileName = $"{id:N}.jpg",
            ContentType = "image/jpeg",
            FileSize = 100,
            CreatedDate = DateTime.UtcNow,
            AttachedEntityType = "VehiclePartnerProfile",
            AttachedEntityID = "user:10",
            AttachedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new PrivateFileService(db, new FakeWebHostEnvironment());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateForAttachmentAsync(
            new[] { id }, 10, "PartnerDocuments", null, false, CancellationToken.None));
    }


    [Fact]
    public async Task DeleteUnattached_RecordsLogicalAndPhysicalDeletion()
    {
        await using var db = TestDatabase.Create();
        var id = Guid.NewGuid();
        var environment = new FakeWebHostEnvironment
        {
            ContentRootPath = Path.Combine(Path.GetTempPath(), $"smartcar-delete-{Guid.NewGuid():N}")
        };
        var folder = Path.Combine(environment.ContentRootPath, "PrivateUploads", "PartnerDocuments", "10");
        Directory.CreateDirectory(folder);
        var storedName = $"{id:N}.jpg";
        var physicalPath = Path.Combine(folder, storedName);
        await File.WriteAllBytesAsync(physicalPath, new byte[] { 1, 2, 3, 4 });

        db.PrivateFiles.Add(new PrivateFile
        {
            PrivateFileID = id,
            OwnerAppUserID = 10,
            Category = "PartnerDocuments",
            OriginalFileName = "document.jpg",
            StoredFileName = storedName,
            ContentType = "image/jpeg",
            FileSize = 4,
            CreatedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var service = new PrivateFileService(db, environment);

        var deleted = await service.DeleteUnattachedAsync(id, 10, false, CancellationToken.None);

        Assert.True(deleted);
        var record = await db.PrivateFiles.FindAsync(id);
        Assert.NotNull(record);
        Assert.True(record!.IsDeleted);
        Assert.NotNull(record.DeleteRequestedDate);
        Assert.NotNull(record.PhysicalDeletedDate);
        Assert.Equal(0, record.DeleteRetryCount);
        Assert.Null(record.LastDeleteError);
        Assert.False(File.Exists(physicalPath));
        if (Directory.Exists(environment.ContentRootPath)) Directory.Delete(environment.ContentRootPath, true);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SmartCar.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
