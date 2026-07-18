namespace SmartCar.Application.Tools
{
    public static class JwtTokenDefaults
    {
        public static string ValidAudience { get; private set; } = "https://localhost";
        public static string ValidIssuer { get; private set; } = "https://localhost";
        public static string Key { get; private set; } = string.Empty;
        public static int Expire { get; private set; } = 5;

        public static void Configure(string issuer, string audience, string key, int expireDays)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
                throw new InvalidOperationException("JWT key phải có ít nhất 32 ký tự và phải được cấu hình ngoài mã nguồn.");

            ValidIssuer = string.IsNullOrWhiteSpace(issuer) ? "https://localhost" : issuer;
            ValidAudience = string.IsNullOrWhiteSpace(audience) ? "https://localhost" : audience;
            Key = key;
            Expire = expireDays > 0 ? expireDays : 5;
        }
    }
}
