using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.WebApi.Services;

namespace SmartCar.IntegrationTests;

public class ReservationCancellationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Cancel_CreatesSingleRefundAndAuditTrail()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(new Reservation
        {
            ReservationID = 100,
            CustomerAppUserID = 10,
            PartnerVehicleID = 20,
            CarID = 30,
            Status = "Đã thanh toán",
            PickUpDate = Now.UtcDateTime.AddHours(48).Date,
            PickUpTime = Now.UtcDateTime.AddHours(48).TimeOfDay,
            DropOffDate = Now.UtcDateTime.AddDays(3).Date,
            DropOffTime = TimeSpan.FromHours(8),
            CreatedDate = Now.UtcDateTime
        });
        db.Payments.Add(new Payment
        {
            PaymentID = 1,
            ReservationID = 100,
            PaymentType = "Tiền thuê",
            Amount = 1_000_000m,
            Status = "Thành công",
            CreatedDate = Now.UtcDateTime
        });
        await db.SaveChangesAsync();

        var service = new ReservationCancellationService(db, new FixedTimeProvider(Now));
        var first = await service.CancelAsync(100, 10, false, "Khách thay đổi kế hoạch");
        var second = await service.CancelAsync(100, 10, false, "Gửi lại yêu cầu");

        Assert.True(first.CanCancel);
        Assert.Equal(10m, first.FeeRate);
        Assert.Equal(100_000m, first.CancellationFee);
        Assert.Equal(900_000m, first.RefundAmount);
        Assert.False(second.CanCancel);
        Assert.Equal(409, second.StatusCode);

        var reservation = await db.Reservations.SingleAsync(x => x.ReservationID == 100);
        Assert.Equal("Đã hủy", reservation.Status);
        Assert.Equal(100_000m, reservation.CancellationFeeAmount);
        Assert.Single(await db.Payments.Where(x => x.IdempotencyKey == "reservation-cancel-refund:100").ToListAsync());
        Assert.Single(await db.ReservationStatusHistories.Where(x => x.ReservationID == 100).ToListAsync());
        Assert.Single(await db.DataChangeHistories.Where(x => x.EntityID == "100" && x.Action == "Cancel").ToListAsync());
        Assert.Single(await db.AuditLogs.Where(x => x.EntityID == "100" && x.Action == "Hủy đơn").ToListAsync());
    }

    [Fact]
    public async Task Preview_RejectsDifferentCustomer()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(new Reservation
        {
            ReservationID = 101,
            CustomerAppUserID = 10,
            PartnerVehicleID = 20,
            CarID = 30,
            Status = "Chờ giao xe",
            PickUpDate = Now.UtcDateTime.AddDays(5).Date,
            PickUpTime = TimeSpan.Zero,
            DropOffDate = Now.UtcDateTime.AddDays(6).Date,
            DropOffTime = TimeSpan.Zero,
            CreatedDate = Now.UtcDateTime
        });
        await db.SaveChangesAsync();

        var service = new ReservationCancellationService(db, new FixedTimeProvider(Now));
        var result = await service.PreviewAsync(101, 999, false);

        Assert.False(result.CanCancel);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task Preview_RejectsAtOrAfterPickupTime()
    {
        await using var db = TestDatabase.Create();
        // 06:59 giờ Việt Nam = 23:59 UTC ngày hôm trước, đã qua so với Now 00:00 UTC.
        db.Reservations.Add(new Reservation
        {
            ReservationID = 102,
            CustomerAppUserID = 10,
            PartnerVehicleID = 20,
            CarID = 30,
            Status = "Chờ giao xe",
            PickUpDate = new DateTime(2026, 7, 13),
            PickUpTime = new TimeSpan(6, 59, 0),
            DropOffDate = new DateTime(2026, 7, 14),
            DropOffTime = TimeSpan.FromHours(7),
            CreatedDate = Now.UtcDateTime
        });
        await db.SaveChangesAsync();

        var service = new ReservationCancellationService(db, new FixedTimeProvider(Now));
        var result = await service.PreviewAsync(102, 10, false);

        Assert.False(result.CanCancel);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("quá giờ nhận xe", result.Message, StringComparison.OrdinalIgnoreCase);
    }

}
