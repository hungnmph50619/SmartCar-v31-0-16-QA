using System.Security.Cryptography;
using System.Text;

namespace SmartCar.Domain.Security
{
    public static class OtpSecurity
    {
        public static string GenerateSixDigits()
            => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        public static string Hash(string key, string subject, string purpose, string target, string otp)
        {
            ValidateKey(key);
            var normalized = string.Join(":",
                Normalize(subject), Normalize(purpose), Normalize(target), Normalize(otp));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized)));
        }

        public static bool Verify(string key, string subject, string purpose, string target, string otp, string storedHash)
        {
            try
            {
                var expected = Convert.FromHexString(storedHash);
                var actual = Convert.FromHexString(Hash(key, subject, purpose, target, otp));
                return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

        private static void ValidateKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 64 || key.StartsWith("__SET_BY_", StringComparison.Ordinal))
                throw new InvalidOperationException("Security:OtpHmacKey phải được cấu hình an toàn và dài tối thiểu 64 ký tự.");
        }
    }
}
