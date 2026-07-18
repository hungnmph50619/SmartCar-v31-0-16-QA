namespace SmartCar.Domain.Policies
{
    public sealed record ReservationCancellationQuote(
        decimal PaidAmount,
        decimal FeeRate,
        decimal CancellationFee,
        decimal RefundAmount,
        string PolicyVersion);

    public static class ReservationCancellationPolicy
    {
        public const string Version = "cancel-v31.0";

        public static ReservationCancellationQuote Calculate(
            DateTime pickupUtc,
            decimal paidAmount,
            DateTime utcNow)
        {
            if (paidAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(paidAmount), "Số tiền đã thanh toán không được âm.");

            pickupUtc = DateTime.SpecifyKind(pickupUtc, DateTimeKind.Utc);
            utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            if (pickupUtc <= utcNow)
                throw new InvalidOperationException("Đã đến hoặc quá giờ nhận xe; không thể hủy theo chính sách hủy thông thường.");

            var hoursBeforePickup = (pickupUtc - utcNow).TotalHours;
            var rate = paidAmount <= 0m
                ? 0m
                : hoursBeforePickup >= 168d
                    ? 0m
                    : hoursBeforePickup >= 72d
                        ? 10m
                        : hoursBeforePickup >= 24d
                            ? 30m
                            : 70m;

            var fee = decimal.Round(paidAmount * rate / 100m, 0, MidpointRounding.AwayFromZero);
            fee = Math.Clamp(fee, 0m, paidAmount);
            var refund = Math.Max(0m, paidAmount - fee);

            return new ReservationCancellationQuote(paidAmount, rate, fee, refund, Version);
        }
    }
}
