using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartCar.Domain.Entities;
using SmartCar.Domain.SystemInfo;
using SmartCar.WebApi.HealthChecks;

namespace SmartCar.IntegrationTests;

public class DatabaseVersionHealthCheckTests
{
    [Fact]
    public async Task CurrentVersion_IsHealthy()
    {
        await using var db = TestDatabase.Create();
        db.SystemVersions.Add(new SystemVersion
        {
            ApplicationVersion = SmartCarRelease.ApplicationVersion,
            DatabaseVersion = SmartCarRelease.DatabaseVersion,
            IsCurrent = true,
            ReleasedDate = SmartCarRelease.ReleaseDateUtc
        });
        await db.SaveChangesAsync();

        var result = await new DatabaseVersionHealthCheck(db)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task OldDatabaseVersion_IsUnhealthy()
    {
        await using var db = TestDatabase.Create();
        db.SystemVersions.Add(new SystemVersion
        {
            ApplicationVersion = "30.3",
            DatabaseVersion = "30.3",
            IsCurrent = true,
            ReleasedDate = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var result = await new DatabaseVersionHealthCheck(db)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True((result.Description ?? string.Empty).Contains("30.9", StringComparison.Ordinal));
    }
}
