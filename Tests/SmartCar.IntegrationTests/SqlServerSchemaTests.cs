using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartCar.Domain.Entities;
using SmartCar.Domain.SystemInfo;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.HealthChecks;

namespace SmartCar.IntegrationTests;

public sealed class RequiresSqlServerFactAttribute : FactAttribute
{
    public RequiresSqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SMARTCAR_TEST_SQLSERVER")))
            Skip = "Đặt SMARTCAR_TEST_SQLSERVER trỏ tới database SmartCarMarketplaceDb v30.9 để chạy kiểm thử SQL Server thật.";
    }
}

public class SqlServerSchemaTests
{
    private static string ConnectionString => Environment.GetEnvironmentVariable("SMARTCAR_TEST_SQLSERVER")!;

    private static CarBookContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CarBookContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new CarBookContext(options);
    }

    [RequiresSqlServerFact]
    public async Task ProvisionedSqlServer_HasRequiredVersionAndExactSchema()
    {
        await using var db = CreateContext();
        var health = await new DatabaseVersionHealthCheck(db)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, health.Status);
        Assert.Equal(SmartCarRelease.DatabaseVersion, health.Data["databaseVersion"]);
        Assert.Equal(0, health.Data["missingTables"]);
        Assert.Equal(0, health.Data["missingColumns"]);
        Assert.Equal(0, health.Data["missingOrInvalidForeignKeys"]);
        Assert.Equal(0, health.Data["untrustedForeignKeys"]);
        Assert.Equal(0, health.Data["missingOrDisabledIndexes"]);
    }

    [RequiresSqlServerFact]
    public async Task Payment_TwoEditors_StaleRowVersionIsRejected()
    {
        var reservationId = 0;
        var paymentId = 0;
        try
        {
            await using (var setup = CreateContext())
            {
                var reservation = BuildReservation();
                setup.Reservations.Add(reservation);
                await setup.SaveChangesAsync();
                reservationId = reservation.ReservationID;

                var payment = new Payment
                {
                    ReservationID = reservationId,
                    PaymentType = "Phụ phí kiểm thử",
                    Amount = 100000,
                    Status = "Chờ xác nhận",
                    IdempotencyKey = $"test-payment-{Guid.NewGuid():N}",
                    CreatedDate = DateTime.UtcNow
                };
                setup.Payments.Add(payment);
                await setup.SaveChangesAsync();
                paymentId = payment.PaymentID;
            }

            await using var first = CreateContext();
            await using var second = CreateContext();
            var firstCopy = await first.Payments.SingleAsync(x => x.PaymentID == paymentId);
            var staleCopy = await second.Payments.SingleAsync(x => x.PaymentID == paymentId);

            firstCopy.Status = "Đã xử lý bởi nhân viên A";
            await first.SaveChangesAsync();

            staleCopy.Status = "Đã xử lý bởi nhân viên B";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await CleanupAsync(reservationId);
        }
    }

    [RequiresSqlServerFact]
    public async Task PrivateFile_TwoAttachments_StaleRowVersionIsRejected()
    {
        var fileId = Guid.NewGuid();
        try
        {
            await using (var setup = CreateContext())
            {
                setup.PrivateFiles.Add(new PrivateFile
                {
                    PrivateFileID = fileId,
                    OwnerAppUserID = 3,
                    Category = "PartnerDocuments",
                    OriginalFileName = "concurrency.jpg",
                    StoredFileName = $"{fileId:N}.jpg",
                    ContentType = "image/jpeg",
                    FileSize = 100,
                    CreatedDate = DateTime.UtcNow
                });
                await setup.SaveChangesAsync();
            }

            await using var first = CreateContext();
            await using var second = CreateContext();
            var firstCopy = await first.PrivateFiles.SingleAsync(x => x.PrivateFileID == fileId);
            var staleCopy = await second.PrivateFiles.SingleAsync(x => x.PrivateFileID == fileId);

            firstCopy.AttachedEntityType = "VehiclePartnerProfile";
            firstCopy.AttachedEntityID = "user:3";
            firstCopy.AttachedDate = DateTime.UtcNow;
            await first.SaveChangesAsync();

            staleCopy.AttachedEntityType = "VehiclePartnerApplication";
            staleCopy.AttachedEntityID = "1";
            staleCopy.AttachedDate = DateTime.UtcNow;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.PrivateFiles WHERE PrivateFileID={fileId}");
        }
    }

    [RequiresSqlServerFact]
    public async Task Settlement_TwoEditors_StaleRowVersionIsRejected()
    {
        var reservationId = 0;
        var settlementId = 0;
        try
        {
            await using (var setup = CreateContext())
            {
                var reservation = BuildReservation();
                setup.Reservations.Add(reservation);
                await setup.SaveChangesAsync();
                reservationId = reservation.ReservationID;

                var settlement = new Settlement
                {
                    ReservationID = reservationId,
                    GrossRental = 1000000,
                    PlatformFee = 200000,
                    OwnerPayout = 800000,
                    Status = "Chờ đối soát",
                    CreationIdempotencyKey = $"test-settlement-{Guid.NewGuid():N}",
                    CreatedDate = DateTime.UtcNow
                };
                setup.Settlements.Add(settlement);
                await setup.SaveChangesAsync();
                settlementId = settlement.SettlementID;
            }

            await using var first = CreateContext();
            await using var second = CreateContext();
            var firstCopy = await first.Settlements.SingleAsync(x => x.SettlementID == settlementId);
            var staleCopy = await second.Settlements.SingleAsync(x => x.SettlementID == settlementId);

            firstCopy.Status = "Đã duyệt";
            await first.SaveChangesAsync();

            staleCopy.Status = "Từ chối";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await CleanupAsync(reservationId);
        }
    }

    [RequiresSqlServerFact]
    public async Task EmailOutbox_TwoWorkers_StaleClaimIsRejected()
    {
        long outboxId = 0;
        try
        {
            await using (var setup = CreateContext())
            {
                var item = new EmailOutbox
                {
                    MessageKey = $"test-outbox-{Guid.NewGuid():N}",
                    RecipientEmail = "test@example.invalid",
                    Subject = "Test",
                    Body = "Test",
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };
                setup.EmailOutboxes.Add(item);
                await setup.SaveChangesAsync();
                outboxId = item.EmailOutboxID;
            }

            await using var first = CreateContext();
            await using var second = CreateContext();
            var firstCopy = await first.EmailOutboxes.SingleAsync(x => x.EmailOutboxID == outboxId);
            var staleCopy = await second.EmailOutboxes.SingleAsync(x => x.EmailOutboxID == outboxId);

            firstCopy.Status = "Processing";
            firstCopy.LockedBy = "worker-a";
            firstCopy.LockedUntil = DateTime.UtcNow.AddMinutes(2);
            await first.SaveChangesAsync();

            staleCopy.Status = "Processing";
            staleCopy.LockedBy = "worker-b";
            staleCopy.LockedUntil = DateTime.UtcNow.AddMinutes(2);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
        finally
        {
            if (outboxId > 0)
            {
                await using var cleanup = CreateContext();
                await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.EmailOutboxes WHERE EmailOutboxID={outboxId}");
            }
        }
    }


    [RequiresSqlServerFact]
    public async Task EmailOutbox_DuplicateMessageKey_IsRejected()
    {
        var key = $"test-duplicate-{Guid.NewGuid():N}";
        try
        {
            await using var first = CreateContext();
            first.EmailOutboxes.Add(new EmailOutbox
            {
                MessageKey = key,
                RecipientEmail = "first@example.invalid",
                Subject = "Test",
                Body = "Test",
                Status = "Pending"
            });
            await first.SaveChangesAsync();

            await using var second = CreateContext();
            second.EmailOutboxes.Add(new EmailOutbox
            {
                MessageKey = key,
                RecipientEmail = "second@example.invalid",
                Subject = "Test",
                Body = "Test",
                Status = "Pending"
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = CreateContext();
            await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.EmailOutboxes WHERE MessageKey={key}");
        }
    }

    [RequiresSqlServerFact]
    public async Task Payment_ProviderFeeAboveAmount_IsRejectedByDatabase()
    {
        var reservationId = 0;
        try
        {
            await using var db = CreateContext();
            var reservation = BuildReservation();
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync();
            reservationId = reservation.ReservationID;
            db.Payments.Add(new Payment
            {
                ReservationID = reservationId,
                PaymentType = "Kiểm thử phí",
                Amount = 100000,
                ProviderFeeAmount = 100001,
                ProviderFeeVerified = true,
                Status = "Chờ xác nhận",
                IdempotencyKey = $"invalid-fee-{Guid.NewGuid():N}"
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await CleanupAsync(reservationId);
        }
    }

    private static Reservation BuildReservation()
    {
        var start = VietnamTime.Today.AddDays(30);
        return new Reservation
        {
            CustomerAppUserID = 4,
            PartnerVehicleID = 1,
            CarID = 1,
            PickUpLocationID = 1,
            DropOffLocationID = 1,
            Name = "Integration",
            Surname = "Test",
            Email = $"integration-{Guid.NewGuid():N}@smartcar.test",
            Phone = "0900000099",
            Age = 30,
            DriverLicenseYear = 5,
            RentalMode = "Tự lái",
            DeliveryMethod = "Nhận tại điểm giao xe",
            Status = "Chờ chủ xe xác nhận",
            PickUpDate = start,
            DropOffDate = start.AddDays(2),
            PickUpTime = new TimeSpan(9, 0, 0),
            DropOffTime = new TimeSpan(9, 0, 0),
            TotalPrice = 1000000,
            CommissionRateSnapshot = 20,
            PlatformFeeAmount = 200000,
            PartnerReceivableAmount = 800000,
            DepositAmount = 3000000,
            DepositStatus = "Chưa đặt cọc",
            CancellationPolicyVersion = "cancel-v30-dot6",
            TermsVersion = "v1",
            CreatedDate = DateTime.UtcNow
        };
    }

    private static async Task CleanupAsync(int reservationId)
    {
        if (reservationId <= 0) return;
        await using var cleanup = CreateContext();
        await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Settlements WHERE ReservationID={reservationId}");
        await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Payments WHERE ReservationID={reservationId}");
        await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.ReservationStatusHistories WHERE ReservationID={reservationId}");
        await cleanup.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.Reservations WHERE ReservationID={reservationId}");
    }
}
