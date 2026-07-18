using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartCar.Persistence.Context;

namespace SmartCar.IntegrationTests;

internal static class TestDatabase
{
    public static CarBookContext Create()
    {
        var options = new DbContextOptionsBuilder<CarBookContext>()
            .UseInMemoryDatabase($"smartcar-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new CarBookContext(options);
    }
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;
    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;
    public override DateTimeOffset GetUtcNow() => _utcNow;
}
