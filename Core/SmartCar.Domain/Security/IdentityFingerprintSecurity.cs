using System.Security.Cryptography;
using System.Text;

namespace SmartCar.Domain.Security
{
    public static class IdentityFingerprintSecurity
    {
        public static string Compute(string key, string identityNumber)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length < 64 || key.StartsWith("__SET_BY_", StringComparison.Ordinal))
                throw new InvalidOperationException("Security:IdentityHmacKey phải được cấu hình an toàn và dài tối thiểu 64 ký tự.");
            var normalized = new string((identityNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (normalized.Length != 12)
                throw new ArgumentException("CCCD phải gồm đúng 12 chữ số.", nameof(identityNumber));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized)));
        }
    }
}
