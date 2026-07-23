using MediatR;

namespace SmartCar.Application.Features.Mediator.Commands.ReservationCommands
{
    public class CreateReservationCommand : IRequest<int>
    {
        private string _name = string.Empty;
        private string _surname = string.Empty;

        public int CustomerAppUserID { get; set; }
        public int PartnerVehicleID { get; set; }
        public int? VehiclePricingPlanID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                // Luồng tự lái lấy họ tên pháp lý từ hồ sơ xác minh. Controller cũ gán
                // Name = "" trước khi gán toàn bộ LegalFullName vào Surname, làm bước
                // kiểm tra phía sau hiểu nhầm là thiếu họ tên. Giữ lại tên hợp lệ đã
                // bind từ tài khoản thay vì xóa nó bằng một giá trị rỗng.
                if (!string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(_name))
                    _name = value?.Trim() ?? string.Empty;
            }
        }

        public string Surname
        {
            get => _surname;
            set
            {
                var normalized = value?.Trim() ?? string.Empty;

                // Khi API gán LegalFullName (ví dụ "Nguyễn Minh Anh") trong lúc Name
                // đã là "Anh", chỉ lưu phần họ và tên đệm để tránh hiển thị trùng tên.
                if (!string.IsNullOrWhiteSpace(_name) &&
                    normalized.EndsWith($" {_name}", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized[..^( _name.Length + 1)].Trim();
                }

                _surname = normalized;
            }
        }

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int PickUpLocationID { get; set; }
        public int DropOffLocationID { get; set; }
        public string? PickUpAddressText { get; set; }
        public string? DropOffAddressText { get; set; }
        public int CarID { get; set; }
        public int Age { get; set; }
        public DateTime? BookingHolderDateOfBirth { get; set; }
        public int DriverLicenseYear { get; set; }
        public string RentalMode { get; set; } = "Tự lái";
        public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";
        public int PassengerCount { get; set; }
        public string? Itinerary { get; set; }
        public string? SpecialLuggage { get; set; }
        public decimal? EstimatedDistanceKm { get; set; }
        public string? Description { get; set; }
        public DateTime PickUpDate { get; set; }
        public DateTime DropOffDate { get; set; }
        public TimeSpan PickUpTime { get; set; }
        public TimeSpan DropOffTime { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CommissionRateSnapshot { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal PartnerReceivableAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal ReservationDepositAmount { get; set; }
        public decimal SecurityDepositAmount { get; set; }
        public int BufferMinutesSnapshot { get; set; }
        public DateTime? HoldExpiresAt { get; set; }
        public DateTime? PartnerResponseExpiresAt { get; set; }
        public DateTime? PaymentExpiresAt { get; set; }
    }
}