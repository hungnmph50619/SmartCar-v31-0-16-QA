namespace SmartCar.Domain.BusinessRules
{
    public static class ReservationAvailabilityRules
    {
        public const int PaymentHoldMinutes = 15;
        public const int OwnerResponseMinutes = 120;
        public const int SelfDriveBufferMinutes = 120;
        public const int WithDriverBufferMinutes = 60;

        private static readonly HashSet<string> HardBlockingStatuses = new(StringComparer.Ordinal)
        {
            ReservationStatuses.Confirmed,
            ReservationStatuses.HandoverPending,
            ReservationStatuses.InProgress,
            ReservationStatuses.ReturnPending,
            ReservationStatuses.SurchargeProposalPending,
            ReservationStatuses.SurchargeResponsePending,
            ReservationStatuses.SettlementPending,
            ReservationStatuses.Disputed,
            ReservationStatuses.IncidentProcessing,
            "Chờ nhân viên xác nhận cọc",
            "Chờ nhân viên xác nhận thanh toán",
            "Đã thanh toán",
            "Đã đặt cọc"
        };

        public static bool IsBlocking(
            string? status,
            DateTime? holdExpiresAt,
            DateTime? partnerResponseExpiresAt,
            DateTime? paymentExpiresAt,
            DateTime now)
        {
            if (status == ReservationStatuses.OwnerPending)
                return partnerResponseExpiresAt.HasValue && partnerResponseExpiresAt.Value > now;

            if (status is ReservationStatuses.PaymentPending or "Chờ khách đặt cọc" or "Chờ khách thanh toán giữ chỗ")
            {
                var expiry = paymentExpiresAt ?? holdExpiresAt;
                return expiry.HasValue && expiry.Value > now;
            }

            return !string.IsNullOrWhiteSpace(status) && HardBlockingStatuses.Contains(status);
        }

        // Giữ overload cũ để không làm hỏng mã đang gọi; các luồng mới phải truyền đủ hạn phản hồi/thanh toán.
        public static bool IsBlocking(string? status, DateTime? holdExpiresAt, DateTime now)
            => IsBlocking(status, holdExpiresAt, null, holdExpiresAt, now);

        public static int GetBufferMinutes(string? serviceType)
            => string.Equals(serviceType, ServiceTypes.SelfDrive, StringComparison.OrdinalIgnoreCase)
                ? SelfDriveBufferMinutes
                : WithDriverBufferMinutes;

        public static bool OverlapsWithTurnaroundBuffer(
            DateTime existingStart,
            DateTime existingEnd,
            DateTime requestedStart,
            DateTime requestedEnd,
            int bufferMinutes)
        {
            var buffer = TimeSpan.FromMinutes(Math.Max(0, bufferMinutes));
            return existingStart < requestedEnd.Add(buffer)
                   && existingEnd.Add(buffer) > requestedStart;
        }

        public static bool OverlapsWithTurnaroundBuffer(
            DateTime existingStart,
            DateTime existingEnd,
            DateTime requestedStart,
            DateTime requestedEnd)
            => OverlapsWithTurnaroundBuffer(existingStart, existingEnd, requestedStart, requestedEnd, WithDriverBufferMinutes);

        public static bool IsOwnerResponseExpired(DateTime createdDate, DateTime now)
            => createdDate.AddMinutes(OwnerResponseMinutes) <= now;
    }
}
