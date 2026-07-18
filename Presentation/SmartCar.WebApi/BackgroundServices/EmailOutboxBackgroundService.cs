using System.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.BackgroundServices
{
    /// <summary>
    /// Claim nguyên tử từng email. Trước khi gọi SMTP, trạng thái được lưu thành Sending.
    /// Nếu tiến trình dừng hoặc SMTP trả lỗi không xác định, email chuyển sang DeliveryUnknown
    /// và KHÔNG tự động gửi lại; người dùng có thể chủ động yêu cầu mã/thông báo mới.
    /// SMTP không có giao thức exactly-once, vì vậy cách này ưu tiên không gửi trùng.
    /// </summary>
    public sealed class EmailOutboxBackgroundService : BackgroundService
    {
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(2);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailOutboxBackgroundService> _logger;
        private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

        public EmailOutboxBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<EmailOutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await ProcessBatchAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Lỗi xử lý EmailOutbox."); }
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CarBookContext>();
            await MarkStaleSendingAsUnknownAsync(db, cancellationToken);

            var claimedIds = await ClaimBatchAsync(db, 10, cancellationToken);
            var processed = 0;

            foreach (var id in claimedIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = await db.EmailOutboxes.FirstOrDefaultAsync(x => x.EmailOutboxID == id, cancellationToken);
                if (item is null || item.Status != "Processing" || item.LockedBy != _workerId) continue;

                try
                {
                    // Kiểm tra cấu hình trước vùng không thể biết chắc SMTP đã nhận thư hay chưa.
                    ValidateEmailConfiguration();
                }
                catch (InvalidOperationException ex)
                {
                    item.RetryCount++;
                    item.Status = item.RetryCount >= 5 ? "Failed" : "Retry";
                    item.NextAttemptAt = item.Status == "Failed" ? null : DateTime.UtcNow.AddMinutes(Math.Min(30, item.RetryCount * 2));
                    item.LastError = Truncate(ex.Message, 2000);
                    item.LockedBy = null;
                    item.LockedUntil = null;
                    await SafeSaveAsync(db, CancellationToken.None);
                    _logger.LogWarning(ex, "Cấu hình email chưa hợp lệ cho EmailOutbox {EmailOutboxId}.", item.EmailOutboxID);
                    continue;
                }

                try
                {
                    item.Status = "Sending";
                    item.LastAttemptAt = DateTime.UtcNow;
                    item.LockedUntil = DateTime.UtcNow.Add(LockDuration);
                    await db.SaveChangesAsync(cancellationToken);

                    await SendAsync(item, cancellationToken);
                    item.Status = "Sent";
                    item.SentDate = DateTime.UtcNow;
                    item.NextAttemptAt = null;
                    item.LastError = null;
                    item.LockedBy = null;
                    item.LockedUntil = null;
                    await SafeSaveAsync(db, CancellationToken.None);
                    processed++;
                }
                catch (Exception ex)
                {
                    // Mọi lỗi sau khi đã lưu Sending đều có thể xảy ra sau lúc SMTP nhận thư.
                    // Không tự retry để tránh người dùng nhận thư/OTP hai lần.
                    item.RetryCount++;
                    item.Status = "DeliveryUnknown";
                    item.NextAttemptAt = null;
                    item.LastError = Truncate($"Không xác định đã gửi hay chưa: {ex.Message}", 2000);
                    item.LockedBy = null;
                    item.LockedUntil = null;
                    await SafeSaveAsync(db, CancellationToken.None);
                    _logger.LogWarning(ex, "EmailOutbox {EmailOutboxId} có trạng thái gửi không xác định; không tự gửi lại.", item.EmailOutboxID);
                    if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested) throw;
                }
            }
            return processed;
        }

        internal static async Task<int> MarkStaleSendingAsUnknownAsync(CarBookContext db, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var stale = await db.EmailOutboxes
                .Where(x => x.Status == "Sending" && (!x.LockedUntil.HasValue || x.LockedUntil < now))
                .ToListAsync(cancellationToken);
            foreach (var item in stale)
            {
                item.Status = "DeliveryUnknown";
                item.NextAttemptAt = null;
                item.LockedBy = null;
                item.LockedUntil = null;
                item.LastError = "Ứng dụng dừng trong khi gửi; không tự gửi lại để tránh trùng email.";
            }
            if (stale.Count > 0) await db.SaveChangesAsync(cancellationToken);
            return stale.Count;
        }

        private async Task<IReadOnlyList<long>> ClaimBatchAsync(CarBookContext db, int batchSize, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var lockedUntil = now.Add(LockDuration);

            if (!db.Database.IsSqlServer())
            {
                var items = await db.EmailOutboxes
                    .Where(x =>
                        ((x.Status == "Pending" || x.Status == "Retry") ||
                         (x.Status == "Processing" && x.LockedUntil.HasValue && x.LockedUntil < now)) &&
                        (!x.NextAttemptAt.HasValue || x.NextAttemptAt <= now) &&
                        x.RetryCount < 5)
                    .OrderBy(x => x.CreatedDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);
                foreach (var item in items)
                {
                    item.Status = "Processing";
                    item.LockedBy = _workerId;
                    item.LockedUntil = lockedUntil;
                    item.LastAttemptAt = now;
                }
                await db.SaveChangesAsync(cancellationToken);
                return items.Select(x => x.EmailOutboxID).ToArray();
            }

            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
;WITH candidates AS
(
    SELECT TOP (@batchSize) *
    FROM dbo.EmailOutboxes WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE
        (
            [Status] IN (N'Pending', N'Retry')
            OR ([Status] = N'Processing' AND [LockedUntil] IS NOT NULL AND [LockedUntil] < @now)
        )
        AND ([NextAttemptAt] IS NULL OR [NextAttemptAt] <= @now)
        AND [RetryCount] < 5
    ORDER BY [CreatedDate], [EmailOutboxID]
)
UPDATE candidates
SET [Status] = N'Processing',
    [LockedBy] = @workerId,
    [LockedUntil] = @lockedUntil,
    [LastAttemptAt] = @now
OUTPUT INSERTED.[EmailOutboxID];";

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

        private void ValidateEmailConfiguration()
        {
            var section = _configuration.GetSection("EmailSettings");
            var userName = section["UserName"];
            var appPassword = section["AppPassword"];
            var fromEmail = section["FromEmail"] ?? userName;
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(appPassword) || string.IsNullOrWhiteSpace(fromEmail))
                throw new InvalidOperationException("EmailSettings chưa được cấu hình.");
        }

        private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private async Task SendAsync(EmailOutbox item, CancellationToken cancellationToken)
        {
            var section = _configuration.GetSection("EmailSettings");
            var host = section["Host"] ?? "smtp.gmail.com";
            var port = int.TryParse(section["Port"], out var parsedPort) ? parsedPort : 587;
            var userName = section["UserName"]!;
            var appPassword = section["AppPassword"]!;
            var fromEmail = section["FromEmail"] ?? userName;
            var fromName = section["FromName"] ?? "Smart Car";

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail!, fromName),
                Subject = item.Subject,
                IsBodyHtml = true,
                Body = item.Body
            };
            message.To.Add(item.RecipientEmail);
            var messageKey = string.IsNullOrWhiteSpace(item.MessageKey) ? $"outbox-{item.EmailOutboxID}" : item.MessageKey;
            message.Headers.Add("X-SmartCar-Message-Key", messageKey);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(userName, appPassword),
                Timeout = 30000
            };
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(message);
        }

        private static async Task SafeSaveAsync(CarBookContext db, CancellationToken cancellationToken)
        {
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in db.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged))
                    entry.State = EntityState.Detached;
            }
        }

        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];
    }
}
