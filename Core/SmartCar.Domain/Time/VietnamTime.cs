namespace SmartCar.Domain.Time
{
    /// <summary>
    /// Reservation PickUpDate/DropOffDate and PickUpTime/DropOffTime are stored as
    /// Vietnam local wall-clock values. Convert to UTC only when comparing with
    /// system time or producing global timestamps.
    /// </summary>
    public static class VietnamTime
    {
        private static readonly Lazy<TimeZoneInfo> Zone = new(() =>
        {
            var id = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        });

        public static TimeZoneInfo TimeZone => Zone.Value;

        public static DateTime Today => UtcToLocal(DateTime.UtcNow).Date;

        public static DateTime ComposeLocal(DateTime date, TimeSpan time)
            => DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);

        public static DateTime LocalToUtc(DateTime localDateTime)
        {
            var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
            if (TimeZone.IsInvalidTime(unspecified))
                throw new ArgumentException("Thời gian địa phương không hợp lệ.", nameof(localDateTime));
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, TimeZone);
        }

        public static DateTime LocalToUtc(DateTime date, TimeSpan time)
            => LocalToUtc(ComposeLocal(date, time));

        public static DateTime UtcToLocal(DateTime utcDateTime)
        {
            var utc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
        }

        /// <summary>
        /// Normalizes a timestamp received from an API client. Values without an
        /// offset/UTC marker are rejected because their time zone is ambiguous.
        /// </summary>
        public static bool TryNormalizeUtcInput(DateTime value, out DateTime utc)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                utc = default;
                return false;
            }

            utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            return true;
        }

        public static DateTime UtcNowLocal(TimeProvider timeProvider)
            => UtcToLocal(timeProvider.GetUtcNow().UtcDateTime);
    }
}
