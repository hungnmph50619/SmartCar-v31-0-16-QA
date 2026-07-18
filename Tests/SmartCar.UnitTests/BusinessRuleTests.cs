using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Policies;
using SmartCar.Domain.Time;

namespace SmartCar.UnitTests;

public class BusinessRuleTests
{
    private static readonly DateTime Now = new(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(168, 0)]
    [InlineData(167.99, 10)]
    [InlineData(72, 10)]
    [InlineData(71.99, 30)]
    [InlineData(24, 30)]
    [InlineData(23.99, 70)]
    [InlineData(0.01, 70)]
    public void CancellationPolicy_UsesExpectedBoundary(decimal hours, decimal expectedRate)
    {
        var quote = ReservationCancellationPolicy.Calculate(Now.AddHours((double)hours), 1_000_000m, Now);

        Assert.Equal(expectedRate, quote.FeeRate);
        Assert.Equal(1_000_000m - quote.CancellationFee, quote.RefundAmount);
        Assert.Equal(ReservationCancellationPolicy.Version, quote.PolicyVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.0166667)]
    [InlineData(-1)]
    public void CancellationPolicy_AtOrAfterPickup_IsRejected(double hours)
        => Assert.Throws<InvalidOperationException>(() =>
            ReservationCancellationPolicy.Calculate(Now.AddHours(hours), 1_000_000m, Now));

    [Fact]
    public void VietnamLocalTime_IsConvertedToUtcBeforeComparison()
    {
        var local = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Unspecified);
        var utc = VietnamTime.LocalToUtc(local);

        Assert.Equal(new DateTime(2026, 7, 13, 1, 0, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void CancellationPolicy_NoPayment_HasNoFee()
    {
        var quote = ReservationCancellationPolicy.Calculate(Now.AddHours(1), 0m, Now);
        Assert.Equal(0m, quote.CancellationFee);
        Assert.Equal(0m, quote.RefundAmount);
    }

    [Fact]
    public void CancellationPolicy_NegativePayment_IsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ReservationCancellationPolicy.Calculate(Now, -1m, Now));

    [Fact]
    public void ReservationOverlap_IncludesOneHourTurnaroundBuffer()
    {
        var existingStart = Now.AddHours(10);
        var existingEnd = Now.AddHours(12);

        Assert.True(ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
            existingStart, existingEnd, Now.AddHours(12.5), Now.AddHours(14)));
        Assert.False(ReservationAvailabilityRules.OverlapsWithTurnaroundBuffer(
            existingStart, existingEnd, Now.AddHours(13), Now.AddHours(14)));
    }

    [Fact]
    public void ExpiredPaymentHold_DoesNotBlockVehicle()
    {
        Assert.False(ReservationAvailabilityRules.IsBlocking(
            "Chờ khách đặt cọc", Now.AddMinutes(-1), Now));
        Assert.True(ReservationAvailabilityRules.IsBlocking(
            "Chờ khách đặt cọc", Now.AddMinutes(1), Now));
    }

    [Fact]
    public void ApiTimestampWithoutOffset_IsRejected()
    {
        var ambiguous = new DateTime(2026, 7, 13, 10, 30, 0, DateTimeKind.Unspecified);
        Assert.False(VietnamTime.TryNormalizeUtcInput(ambiguous, out _));
    }

    [Fact]
    public void ApiTimestampWithUtcMarker_IsAccepted()
    {
        var input = new DateTime(2026, 7, 13, 3, 30, 0, DateTimeKind.Utc);
        Assert.True(VietnamTime.TryNormalizeUtcInput(input, out var utc));
        Assert.Equal(input, utc);
    }

    [Fact]
    public void ServiceType_UsesSeparateTurnaroundBuffers()
    {
        Assert.Equal(120, ReservationAvailabilityRules.GetBufferMinutes(ServiceTypes.SelfDrive));
        Assert.Equal(60, ReservationAvailabilityRules.GetBufferMinutes(ServiceTypes.WithDriver));
    }

    [Fact]
    public void OwnerPending_BlocksOnlyUntilPartnerResponseDeadline()
    {
        Assert.True(ReservationAvailabilityRules.IsBlocking(
            ReservationStatuses.OwnerPending, null, Now.AddMinutes(1), null, Now));
        Assert.False(ReservationAvailabilityRules.IsBlocking(
            ReservationStatuses.OwnerPending, null, Now.AddMinutes(-1), null, Now));
    }

    [Fact]
    public void PaymentPending_BlocksOnlyDuringFifteenMinuteWindow()
    {
        Assert.True(ReservationAvailabilityRules.IsBlocking(
            ReservationStatuses.PaymentPending, Now.AddMinutes(15), null, Now.AddMinutes(15), Now));
        Assert.False(ReservationAvailabilityRules.IsBlocking(
            ReservationStatuses.PaymentPending, Now.AddSeconds(-1), null, Now.AddSeconds(-1), Now));
    }
}
