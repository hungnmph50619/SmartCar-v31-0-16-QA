using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.BackgroundServices;

namespace SmartCar.IntegrationTests;

public class OutboxAndPublicDeletionTests
{
    [Fact]
    public async Task StaleSendingEmail_BecomesDeliveryUnknown_AndIsNotRetried()
    {
        await using var db = TestDatabase.Create();
        db.EmailOutboxes.Add(new EmailOutbox { RecipientEmail="x@test", Subject="x", Body="x", Status="Sending", LockedUntil=DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();

        var count = await EmailOutboxBackgroundService.MarkStaleSendingAsUnknownAsync(db, CancellationToken.None);

        Assert.Equal(1, count);
        var item=Assert.Single(db.EmailOutboxes);
        Assert.Equal("DeliveryUnknown", item.Status);
        Assert.Null(item.NextAttemptAt);
    }

    [Fact]
    public async Task PublicDeletionWorker_DeletesAllowedFile_AndMarksJobDeleted()
    {
        var baseDir=Path.Combine(Path.GetTempPath(), $"smartcar-public-delete-{Guid.NewGuid():N}");
        var webRoot=Path.Combine(baseDir,"wwwroot");
        var folder=Path.Combine(webRoot,"uploads","vehicle-images");
        Directory.CreateDirectory(folder);
        var file=Path.Combine(folder,"old.jpg");
        await File.WriteAllBytesAsync(file, new byte[] {1,2,3});
        var dbName=$"public-delete-{Guid.NewGuid():N}";
        var services=new ServiceCollection();
        services.AddDbContext<CarBookContext>(o=>o.UseInMemoryDatabase(dbName));
        await using var provider=services.BuildServiceProvider();
        await using (var scope=provider.CreateAsyncScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<CarBookContext>();
            db.PublicFileDeletionJobs.Add(new PublicFileDeletionJob { FileUrl="/uploads/vehicle-images/old.jpg", Status="Pending", NextAttemptAt=DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        var worker=new PublicFileDeletionBackgroundService(provider.GetRequiredService<IServiceScopeFactory>(), new FakeEnvironment(baseDir,webRoot), NullLogger<PublicFileDeletionBackgroundService>.Instance);

        var deleted=await worker.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1,deleted); Assert.False(File.Exists(file));
        await using var verify=provider.CreateAsyncScope();
        Assert.Equal("Deleted", (await verify.ServiceProvider.GetRequiredService<CarBookContext>().PublicFileDeletionJobs.SingleAsync()).Status);
        Directory.Delete(baseDir,true);
    }

    private sealed class FakeEnvironment(string contentRoot,string webRoot) : IWebHostEnvironment
    {
        public string ApplicationName {get;set;}="Tests"; public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
        public string WebRootPath {get;set;}=webRoot; public string EnvironmentName {get;set;}="Testing";
        public string ContentRootPath {get;set;}=contentRoot; public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();
    }
}
