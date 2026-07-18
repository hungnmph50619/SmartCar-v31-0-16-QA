using SmartCar.Domain.Security;

namespace SmartCar.UnitTests;

public class PasswordSecurityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("       ")]
    [InlineData("Abc123")]
    [InlineData("12345678")]
    [InlineData("abcdefgh")]
    public void PasswordPolicy_RejectsInvalidPasswords(string? password)
        => Assert.NotNull(PasswordPolicy.Validate(password));

    [Fact]
    public void PasswordPolicy_AcceptsPasswordWithLettersAndDigits()
        => Assert.Null(PasswordPolicy.Validate("SmartCar2026"));

    [Fact]
    public void HashAndVerify_RoundTrip_IsValid()
    {
        var hash = PasswordSecurity.Hash("SmartCar2026");
        var valid = PasswordSecurity.Verify(hash, "SmartCar2026", out var needsUpgrade);

        Assert.True(valid);
        Assert.False(needsUpgrade);
        Assert.True(hash.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal));
    }

    [Fact]
    public void Verify_WrongPassword_IsRejected()
    {
        var hash = PasswordSecurity.Hash("SmartCar2026");
        Assert.False(PasswordSecurity.Verify(hash, "Wrong2026", out _));
    }

    [Fact]
    public void Verify_LegacyPlainText_RequestsUpgrade()
    {
        Assert.True(PasswordSecurity.Verify("Legacy123", "Legacy123", out var needsUpgrade));
        Assert.True(needsUpgrade);
    }
}
