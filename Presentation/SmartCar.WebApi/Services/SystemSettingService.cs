using Microsoft.EntityFrameworkCore;
using SmartCar.Persistence.Context;
using System.Globalization;

namespace SmartCar.WebApi.Services
{
    public interface ISystemSettingService
    {
        Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default);
        Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken = default);
        Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default);
        Task<string> GetStringAsync(string key, string fallback, CancellationToken cancellationToken = default);
    }

    public sealed class SystemSettingService : ISystemSettingService
    {
        private readonly CarBookContext _context;

        public SystemSettingService(CarBookContext context) => _context = context;

        public async Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default)
        {
            var value = await GetValueAsync(key, cancellationToken);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        }

        public async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken = default)
        {
            var value = await GetValueAsync(key, cancellationToken);
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : fallback;
        }

        public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken = default)
        {
            var value = await GetValueAsync(key, cancellationToken);
            return bool.TryParse(value, out var result) ? result : fallback;
        }

        public async Task<string> GetStringAsync(string key, string fallback, CancellationToken cancellationToken = default)
            => await GetValueAsync(key, cancellationToken) ?? fallback;

        private Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
            => _context.SystemSettings.AsNoTracking()
                .Where(x => x.SettingKey == key && x.IsActive)
                .Select(x => x.SettingValue)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
