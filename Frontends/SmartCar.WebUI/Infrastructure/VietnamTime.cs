namespace SmartCar.WebUI.Infrastructure
{
    public static class VietnamTime
    {
        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        public static DateTime Today => Now.Date;
        public static DateTime FromUtc(DateTime utc)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

        public static DateTime ToUtc(DateTime local)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);
    }
}
