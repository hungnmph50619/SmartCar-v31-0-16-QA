using System.Security.Cryptography;
using System.Text;

namespace SmartCar.WebApi.Services;

public interface ISensitiveDataProtector
{
    string? Protect(string? value, string purpose);
    string? Unprotect(string? value, string purpose);
    string? UnprotectOrLegacy(string? encryptedValue, string? legacyValue, string purpose);
    string Mask(string? value, int visibleSuffix = 4);
}

public sealed class SensitiveDataProtector : ISensitiveDataProtector
{
    private readonly byte[] _masterKey;

    public SensitiveDataProtector(IConfiguration configuration)
    {
        var secret = configuration["Security:IdentityHmacKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("__SET_BY_", StringComparison.Ordinal) || secret.Length < 64)
            throw new InvalidOperationException("Security:IdentityHmacKey phải được cấu hình trước khi mã hóa dữ liệu nhạy cảm.");
        _masterKey = Encoding.UTF8.GetBytes(secret);
    }

    public string? Protect(string? value, string purpose)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(value);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(DeriveKey(purpose), 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return $"v1.{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(cipher)}";
    }

    public string? Unprotect(string? value, string purpose)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('.', 4);
        if (parts.Length != 4 || parts[0] != "v1") throw new CryptographicException("Định dạng dữ liệu nhạy cảm không hợp lệ.");
        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var cipher = Convert.FromBase64String(parts[3]);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(DeriveKey(purpose), 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public string? UnprotectOrLegacy(string? encryptedValue, string? legacyValue, string purpose)
        => !string.IsNullOrWhiteSpace(encryptedValue) ? Unprotect(encryptedValue, purpose) : legacyValue;

    public string Mask(string? value, int visibleSuffix = 4)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim();
        if (normalized.Length <= visibleSuffix) return new string('*', normalized.Length);
        return new string('*', normalized.Length - visibleSuffix) + normalized[^visibleSuffix..];
    }

    private byte[] DeriveKey(string purpose)
    {
        using var hmac = new HMACSHA256(_masterKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes("SmartCar:sensitive:v1:" + purpose));
    }
}
