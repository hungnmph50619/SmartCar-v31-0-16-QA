using System.Data;
using Microsoft.EntityFrameworkCore;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.BackgroundServices;

/// <summary>
/// Xóa ảnh xe công khai theo hàng đợi có claim nguyên tử và retry. Chỉ đường dẫn nằm trong
/// /uploads/vehicle-images/ mới được phép ánh xạ xuống wwwroot.
/// </summary>
public sealed class PublicFileDeletionBackgroundService : BackgroundService
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PublicFileDeletionBackgroundService> _logger;
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

    public PublicFileDeletionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        ILogger<PublicFileDeletionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Lỗi xử lý hàng đợi xóa ảnh công khai."); }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CarBookContext>();
        var ids = await ClaimBatchAsync(db, 20, cancellationToken);
        var deleted = 0;

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var job = await db.PublicFileDeletionJobs.FirstOrDefaultAsync(x => x.PublicFileDeletionJobID == id, cancellationToken);
            if (job is null || job.Status != "Processing" || job.LockedBy != _workerId) continue;

            try
            {
                var physicalPath = GetSafePhysicalPath(job.FileUrl);
                if (File.Exists(physicalPath)) File.Delete(physicalPath);
                job.Status = "Deleted";
                job.DeletedDate = DateTime.UtcNow;
                job.NextAttemptAt = null;
                job.LastError = null;
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or UriFormatException)
            {
                job.RetryCount++;
                job.Status = job.RetryCount >= 10 ? "Failed" : "Retry";
                job.NextAttemptAt = job.Status == "Failed"
                    ? null
                    : DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Max(1, job.RetryCount * 5)));
                job.LastError = Truncate(ex.Message, 2000);
                _logger.LogWarning(ex, "Không xóa được ảnh công khai {FileUrl}, lần {RetryCount}.", job.FileUrl, job.RetryCount);
            }
            finally
            {
                job.LastAttemptAt = DateTime.UtcNow;
                job.LockedBy = null;
                job.LockedUntil = null;
            }

            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in db.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged))
                    entry.State = EntityState.Detached;
            }
        }
        return deleted;
    }

    private async Task<IReadOnlyList<long>> ClaimBatchAsync(CarBookContext db, int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var lockedUntil = now.Add(LockDuration);

        if (!db.Database.IsSqlServer())
        {
            var jobs = await db.PublicFileDeletionJobs
                .Where(x =>
                    ((x.Status == "Pending" || x.Status == "Retry") ||
                     (x.Status == "Processing" && x.LockedUntil.HasValue && x.LockedUntil < now)) &&
                    (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= now) &&
                    x.RetryCount < 10)
                .OrderBy(x => x.CreatedDate)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
            foreach (var job in jobs)
            {
                job.Status = "Processing";
                job.LockedBy = _workerId;
                job.LockedUntil = lockedUntil;
                job.LastAttemptAt = now;
            }
            await db.SaveChangesAsync(cancellationToken);
            return jobs.Select(x => x.PublicFileDeletionJobID).ToArray();
        }

        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
;WITH candidates AS
(
    SELECT TOP (@batchSize) *
    FROM dbo.PublicFileDeletionJobs WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE
        (
            [Status] IN (N'Pending', N'Retry')
            OR ([Status] = N'Processing' AND [LockedUntil] IS NOT NULL AND [LockedUntil] < @now)
        )
        AND ([NextAttemptAt] IS NULL OR [NextAttemptAt] <= @now)
        AND [RetryCount] < 10
    ORDER BY [CreatedDate], [PublicFileDeletionJobID]
)
UPDATE candidates
SET [Status] = N'Processing',
    [LockedBy] = @workerId,
    [LockedUntil] = @lockedUntil,
    [LastAttemptAt] = @now
OUTPUT INSERTED.[PublicFileDeletionJobID];";
        AddParameter(command, "@batchSize", batchSize);
        AddParameter(command, "@now", now);
        AddParameter(command, "@workerId", _workerId);
        AddParameter(command, "@lockedUntil", lockedUntil);

        var ids = new List<long>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken)) ids.Add(reader.GetInt64(0));
        }
        await transaction.CommitAsync(cancellationToken);
        return ids;
    }

    private string GetSafePhysicalPath(string fileUrl)
    {
        var pathOnly = fileUrl.Split('?', '#')[0].Replace('\\', '/');
        if (!pathOnly.StartsWith("/uploads/vehicle-images/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn ảnh công khai không thuộc thư mục được phép.");

        var decoded = Uri.UnescapeDataString(pathOnly);
        if (decoded.Contains("..", StringComparison.Ordinal) || decoded.Contains('\0'))
            throw new InvalidOperationException("Đường dẫn ảnh công khai không hợp lệ.");

        var webRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath);
        var relative = decoded.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relative));
        var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "vehicle-images"));
        if (!fullPath.StartsWith(allowedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn ảnh công khai vượt ra ngoài thư mục được phép.");
        return fullPath;
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
