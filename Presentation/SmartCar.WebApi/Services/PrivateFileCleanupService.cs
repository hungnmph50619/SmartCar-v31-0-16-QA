using Microsoft.EntityFrameworkCore;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Services;

/// <summary>
/// Dọn file upload bị bỏ dở và thử lại việc xóa vật lý nếu ổ đĩa từng bị khóa.
/// File đã gắn vào nghiệp vụ không bao giờ thuộc hàng đợi dọn file bỏ dở.
/// </summary>
public sealed class PrivateFileCleanupService : BackgroundService
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private static readonly TimeSpan TemporaryFileRetention = TimeSpan.FromHours(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PrivateFileCleanupService> _logger;

    public PrivateFileCleanupService(
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        ILogger<PrivateFileCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể dọn tệp riêng tư.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task<int> CleanupOnceAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(Retention);
        var processed = 0;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CarBookContext>();
        var files = scope.ServiceProvider.GetRequiredService<IPrivateFileService>();

        var staleIds = await db.PrivateFiles.AsNoTracking()
            .Where(x => !x.IsDeleted && x.AttachedDate == null && x.CreatedDate < cutoff)
            .OrderBy(x => x.CreatedDate)
            .Select(x => x.PrivateFileID)
            .Take(500)
            .ToListAsync(cancellationToken);

        foreach (var id in staleIds)
        {
            try
            {
                if (await files.DeleteUnattachedAsync(id, 0, true, cancellationToken)) processed++;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Đã khóa tệp {FileId} nhưng chưa xóa vật lý; sẽ thử lại ở chu kỳ sau.", id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "Bỏ qua tệp {FileId} vì tệp đã được gắn trong lúc cleanup chạy.", id);
            }
        }

        var pendingPhysicalIds = await db.PrivateFiles.AsNoTracking()
            .Where(x => x.IsDeleted && x.PhysicalDeletedDate == null)
            .OrderBy(x => x.DeleteRequestedDate ?? x.CreatedDate)
            .Select(x => x.PrivateFileID)
            .Take(500)
            .ToListAsync(cancellationToken);

        foreach (var id in pendingPhysicalIds)
        {
            if (await files.DeletePhysicalIfPendingAsync(id, cancellationToken)) processed++;
        }

        processed += DeleteStaleTemporaryFiles();
        processed += await DeleteUntrackedPhysicalFilesAsync(db, cancellationToken);

        if (processed > 0)
            _logger.LogInformation("Đã xử lý {Count} tệp riêng tư trong chu kỳ cleanup.", processed);
        return processed;
    }


    private async Task<int> DeleteUntrackedPhysicalFilesAsync(CarBookContext db, CancellationToken cancellationToken)
    {
        var root = Path.Combine(_environment.ContentRootPath, "PrivateUploads");
        if (!Directory.Exists(root)) return 0;

        var cutoff = DateTime.UtcNow.Subtract(TemporaryFileRetention);
        var candidates = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".uploading", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.GetLastWriteTimeUtc(path) < cutoff)
            .Select(path => new { Path = path, Name = System.IO.Path.GetFileNameWithoutExtension(path) })
            .Where(x => Guid.TryParseExact(x.Name, "N", out _))
            .Take(500)
            .ToList();

        var deleted = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Guid.TryParseExact(candidate.Name, "N", out var fileId)) continue;
            if (await db.PrivateFiles.AsNoTracking().AnyAsync(x => x.PrivateFileID == fileId, cancellationToken)) continue;
            try
            {
                File.Delete(candidate.Path);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Không thể xóa file vật lý không có bản ghi DB {Path}.", candidate.Path);
            }
        }
        return deleted;
    }

    private int DeleteStaleTemporaryFiles()
    {
        var root = Path.Combine(_environment.ContentRootPath, "PrivateUploads");
        if (!Directory.Exists(root)) return 0;

        var cutoff = DateTime.UtcNow.Subtract(TemporaryFileRetention);
        var deleted = 0;
        foreach (var path in Directory.EnumerateFiles(root, "*.uploading", SearchOption.AllDirectories))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) >= cutoff) continue;
                File.Delete(path);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Không thể xóa file tạm {Path}.", path);
            }
        }
        return deleted;
    }
}
