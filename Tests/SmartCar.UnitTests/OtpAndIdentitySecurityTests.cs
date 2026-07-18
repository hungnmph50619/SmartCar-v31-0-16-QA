using SmartCar.Domain.Security;

namespace SmartCar.UnitTests;

public class OtpAndIdentitySecurityTests
{
    private const string Key = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void OtpHash_VerifiesOnlyWithSameContext()
    {
        var hash = OtpSecurity.Hash(Key, "user:10", "ChangeEmail", "new@example.com", "123456");

        Assert.True(OtpSecurity.Verify(Key, "user:10", "ChangeEmail", "new@example.com", "123456", hash));
        Assert.False(OtpSecurity.Verify(Key, "user:11", "ChangeEmail", "new@example.com", "123456", hash));
        Assert.False(OtpSecurity.Verify(Key, "user:10", "Register", "new@example.com", "123456", hash));
        Assert.False(OtpSecurity.Verify(Key, "user:10", "ChangeEmail", "other@example.com", "123456", hash));
        Assert.False(OtpSecurity.Verify(Key, "user:10", "ChangeEmail", "new@example.com", "654321", hash));
    }

    [Fact]
    public void OtpSecurity_RejectsShortSecret()
        => Assert.Throws<InvalidOperationException>(() => OtpSecurity.Hash("short", "1", "Register", "a@b.com", "123456"));

    [Fact]
    public void GeneratedOtp_AlwaysHasSixDigits()
    {
        for (var i = 0; i < 100; i++)
            Assert.Matches("^[0-9]{6}$", OtpSecurity.GenerateSixDigits());
    }

    [Fact]
    public void IdentityFingerprint_NormalizesFormatting()
    {
        var a = IdentityFingerprintSecurity.Compute(Key, "001234567890");
        var b = IdentityFingerprintSecurity.Compute(Key, "001 234 567 890");

        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("00123456789A")]
    public void IdentityFingerprint_RejectsInvalidCitizenId(string value)
        => Assert.Throws<ArgumentException>(() => IdentityFingerprintSecurity.Compute(Key, value));
}
