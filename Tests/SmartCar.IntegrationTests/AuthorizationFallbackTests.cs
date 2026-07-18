using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.BackgroundServices;
using SmartCar.WebApi.Services;
namespace SmartCar.IntegrationTests;

public class AuthorizationFallbackTests
{
    [Fact]
    public async Task Statistics_AnonymousRequest_IsRejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/Statistics/GetCarCount");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LiveHealthCheck_RemainsAnonymous()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


    [Fact]
    public async Task ReadyHealthCheck_AnonymousRequest_IsRejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicLocations_RemainsAnonymous()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Locations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SecureFiles_AnonymousRequest_IsRejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/secure-files/00000000-0000-0000-0000-000000000001");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=127.0.0.1,1;Database=SmartCarNoConnectionNeeded;User Id=sa;Password=NotUsed_123456!;TrustServerCertificate=True;Connect Timeout=1",
                    ["Jwt:Issuer"] = "https://smartcar.test",
                    ["Jwt:Audience"] = "https://smartcar.test",
                    ["Jwt:Key"] = new string('J', 64),
                    ["Security:OtpHmacKey"] = new string('O', 64),
                    ["Security:IdentityHmacKey"] = new string('I', 64),
                    ["Cors:AllowedOrigins:0"] = "https://localhost"
                });
            });
            builder.ConfigureServices(services =>
            {
                var backgroundServices = services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService) &&
                        (descriptor.ImplementationType == typeof(DataLifecycleBackgroundService) ||
                         descriptor.ImplementationType == typeof(EmailOutboxBackgroundService) ||
                         descriptor.ImplementationType == typeof(PrivateFileCleanupService) ||
                         descriptor.ImplementationType == typeof(PublicFileDeletionBackgroundService)))
                    .ToList();
                foreach (var descriptor in backgroundServices) services.Remove(descriptor);

                services.RemoveAll<DbContextOptions<CarBookContext>>();
                services.RemoveAll<CarBookContext>();
                services.AddDbContext<CarBookContext>(options =>
                    options.UseInMemoryDatabase($"authorization-tests-{Guid.NewGuid():N}"));
            });
        });
}
