namespace SmartCar.Dto.RentACarDtos
{
    public class FilterRentACarDto
    {
        public int carID { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public int LocationID { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public double DistanceKm { get; set; }
        public byte Seat { get; set; }
        public string Transmission { get; set; } = string.Empty;
        public string Fuel { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Tự lái";
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public decimal DepositAmount { get; set; }
        public bool Available { get; set; }
    }
}

namespace SmartCar.Dto.RentACarDtos
{
    public class EnhancedCarDetailDto
    {
        public int CarID { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int ManufactureYear { get; set; }
        public string MaskedLicensePlate { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string BigImageUrl { get; set; } = string.Empty;
        // GalleryImages được giữ lại để tương thích với các client cũ.
        public List<string> GalleryImages { get; set; } = new();
        public List<CarGalleryImageDto> GalleryItems { get; set; } = new();
        public byte Seat { get; set; }
        public string Transmission { get; set; } = string.Empty;
        public string Fuel { get; set; } = string.Empty;
        public int Km { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public decimal DailyPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public int KilometerLimitPerDay { get; set; } = 300;
        public decimal ExcessKilometerFee { get; set; } = 5000;
        public decimal DeliveryFee { get; set; }
        public string RentalMode { get; set; } = "Tự lái";
        public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public string RentalConditions { get; set; } = "Khách thuê từ 18 tuổi, có bằng lái phù hợp còn hiệu lực và tài khoản đã xác minh.";
        public string CancellationPolicy { get; set; } = "Phí hủy phụ thuộc thời điểm hủy và trạng thái đặt cọc; hệ thống hiển thị số tiền trước khi xác nhận hủy.";
        public List<string> Features { get; set; } = new();
        public List<CarBusyPeriodDto> BusyPeriods { get; set; } = new();
        public bool IsActive { get; set; }
    }


    public class CarGalleryImageDto
    {
        public string Url { get; set; } = string.Empty;
        public string Label { get; set; } = "Ảnh xe";
    }

    public class CarBusyPeriodDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Label { get; set; } = "Đã có lịch";
    }
}
