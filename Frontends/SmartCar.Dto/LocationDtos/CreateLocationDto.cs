using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.LocationDtos
{
    public class CreateLocationDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên điểm giao nhận.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố.")]
        [StringLength(120)]
        public string ProvinceCity { get; set; } = string.Empty;

        [StringLength(120)] public string District { get; set; } = string.Empty;
        [StringLength(120)] public string Ward { get; set; } = string.Empty;
        [StringLength(500)] public string AddressDetail { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90.")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180.")]
        public decimal? Longitude { get; set; }

        [Range(1, 100, ErrorMessage = "Bán kính tìm kiếm phải từ 1 đến 100 km.")]
        public int SearchRadiusKm { get; set; } = 20;

        public bool IsActive { get; set; } = true;
    }
}
