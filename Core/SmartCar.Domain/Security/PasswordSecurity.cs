using System.Security.Cryptography;

namespace SmartCar.Domain.Security
{
    public static class PasswordSecurity
    {
        private const int Iterations = 120_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(password);
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        public static bool Verify(string stored, string supplied, out bool needsUpgrade)
        {
            needsUpgrade = false;
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(supplied)) return false;
            if (!stored.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal))
            {
                needsUpgrade = CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(stored),
                    System.Text.Encoding.UTF8.GetBytes(supplied));
                return needsUpgrade;
            }
            var parts = stored.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;
            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(supplied, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                needsUpgrade = iterations < Iterations;
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch (FormatException) { return false; }
        }
    }
}
