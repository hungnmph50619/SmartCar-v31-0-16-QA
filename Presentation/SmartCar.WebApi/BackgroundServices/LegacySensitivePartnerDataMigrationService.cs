using Microsoft.EntityFrameworkCore;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;

namespace SmartCar.WebApi.BackgroundServices;

public sealed class LegacySensitivePartnerDataMigrationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LegacySensitivePartnerDataMigrationService> _logger;

    public LegacySensitivePartnerDataMigrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<LegacySensitivePartnerDataMigrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đợi ứng dụng sẵn sàng; việc chuyển dữ liệu không được làm chậm quá trình khởi động API.
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CarBookContext>();
            var protector = scope.ServiceProvider.GetRequiredService<ISensitiveDataProtector>();
            var migrated = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var profiles = await db.VehiclePartnerProfiles
                    .Where(x => (!string.IsNullOrEmpty(x.CitizenIdentityNumber) && string.IsNullOrEmpty(x.CitizenIdentityNumberEncrypted))
                             || (!string.IsNullOrEmpty(x.BankAccountNumber) && string.IsNullOrEmpty(x.BankAccountNumberEncrypted)))
                    .Take(100)
                    .ToListAsync(stoppingToken);

                if (profiles.Count == 0) break;

                foreach (var profile in profiles)
                {
                    if (!string.IsNullOrEmpty(profile.CitizenIdentityNumber) && string.IsNullOrEmpty(profile.CitizenIdentityNumberEncrypted))
                    {
                        profile.CitizenIdentityNumberEncrypted = protector.Protect(profile.CitizenIdentityNumber, "partner-citizen-id");
                        profile.CitizenIdentityNumber = string.Empty;
                    }
                    if (!string.IsNullOrEmpty(profile.BankAccountNumber) && string.IsNullOrEmpty(profile.BankAccountNumberEncrypted))
                    {
                        profile.BankAccountNumberEncrypted = protector.Protect(profile.BankAccountNumber, "partner-bank-account");
                        profile.BankAccountNumber = string.Empty;
                    }
                    migrated++;
                }
                await db.SaveChangesAsync(stoppingToken);
            }

            if (migrated > 0)
                _logger.LogInformation("Đã mã hóa và chuyển đổi {Count} hồ sơ đối tác cũ.", migrated);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // Không log dữ liệu gốc hoặc ciphertext; nhân viên vẫn có thể kiểm tra health/log để xử lý.
            _logger.LogError(ex, "Không thể tự động chuyển đổi dữ liệu nhạy cảm hồ sơ đối tác.");
        }
    }
}
