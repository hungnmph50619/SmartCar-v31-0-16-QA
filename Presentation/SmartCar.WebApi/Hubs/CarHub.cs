using Microsoft.AspNetCore.SignalR;
using SmartCar.Application.Interfaces.StatisticsInterfaces;

namespace SmartCar.WebApi.Hubs
{
    public class CarHub : Hub
    {
        private readonly IStatisticsRepository _statisticsRepository;
        private readonly ILogger<CarHub> _logger;

        public CarHub(IStatisticsRepository statisticsRepository, ILogger<CarHub> logger)
        {
            _statisticsRepository = statisticsRepository;
            _logger = logger;
        }

        public async Task SendCarCount()
        {
            try
            {
                var value = _statisticsRepository.GetCarCount();
                await Clients.All.SendAsync("ReceiveCarCount", value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể lấy số lượng xe để gửi qua SignalR.");
                throw new HubException("Không thể cập nhật số lượng xe lúc này.");
            }
        }
    }
}
