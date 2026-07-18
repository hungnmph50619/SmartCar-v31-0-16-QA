using Microsoft.Extensions.Configuration;
using SmartCar.WebApi.Services;

namespace SmartCar.IntegrationTests;

public sealed class SensitiveDataProtectorTests
{
    private static SensitiveDataProtector CreateProtector()
    {
        var values = new Dictionary<string, string?>
        {
            ["Security:IdentityHmacKey"] = new string('k', 80)
        };
        return new SensitiveDataProtector(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    [Fact]
    public void Protect_round_trips_without_exposing_plaintext()
    {
        var protector = CreateProtector();
        const string citizenId = "012345678901";

        var encrypted = protector.Protect(citizenId, "partner-citizen-id");

        Assert.NotNull(encrypted);
        Assert.DoesNotContain(citizenId, encrypted, StringComparison.Ordinal);
        Assert.Equal(citizenId, protector.Unprotect(encrypted, "partner-citizen-id"));
    }

    [Fact]
    public void Protected_value_cannot_be_read_with_a_different_purpose()
    {
        var protector = CreateProtector();
        var encrypted = protector.Protect("012345678901", "partner-citizen-id");

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(
            () => protector.Unprotect(encrypted, "partner-bank-account"));
    }

    [Theory]
    [InlineData("123456789", "*****6789")]
    [InlineData("1234", "****")]
    public void Mask_hides_sensitive_digits(string value, string expected)
    {
        Assert.Equal(expected, CreateProtector().Mask(value));
    }
}
