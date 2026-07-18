using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.ReservationDtos
{
    public class CreateReservationDto : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50, ErrorMessage = "Họ tối đa 50 ký tự.")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn địa điểm nhận xe.")]
        public int PickUpLocationID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn địa điểm trả xe.")]
        public int DropOffLocationID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Xe không hợp lệ.")]
        public int CarID { get; set; }

        public int Age { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BookingHolderDateOfBirth { get; set; }

        public int DriverLicenseYear { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hình thức thuê.")]
        public string RentalMode { get; set; } = "Tự lái";

        [Required(ErrorMessage = "Vui lòng chọn hình thức giao nhận.")]
        public string DeliveryMethod { get; set; } = "Nhận tại điểm giao xe";

        [StringLength(500)] public string? PickUpAddressText { get; set; }
        [StringLength(500)] public string? DropOffAddressText { get; set; }
        public int PassengerCount { get; set; }
        [StringLength(2000)] public string? Itinerary { get; set; }
        [StringLength(1000)] public string? SpecialLuggage { get; set; }
        [Range(0, 100000)] public decimal? EstimatedDistanceKm { get; set; }

        [StringLength(1000, ErrorMessage = "Thông tin bổ sung tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        // Không dùng [Range(typeof(bool), "true", "true")] cho checkbox vì client-side validation sẽ hiểu Range là số.
        public bool AcceptTerms { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày nhận xe.")]
        [DataType(DataType.Date)]
        public DateTime PickUpDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả xe.")]
        [DataType(DataType.Date)]
        public DateTime DropOffDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ nhận xe.")]
        [DataType(DataType.Time)]
        public TimeSpan PickUpTime { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ trả xe.")]
        [DataType(DataType.Time)]
        public TimeSpan DropOffTime { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RentalMode is not ("Tự lái" or "Có tài xế"))
            {
                yield return new ValidationResult("Hình thức thuê không hợp lệ.", new[] { nameof(RentalMode) });
            }

            if (DeliveryMethod is not ("Nhận tại điểm giao xe" or "Giao xe tận nơi"))
            {
                yield return new ValidationResult("Hình thức giao nhận không hợp lệ.", new[] { nameof(DeliveryMethod) });
            }

            if (RentalMode == "Tự lái")
            {
                if (DriverLicenseYear is < 1 or > 60)
                    yield return new ValidationResult("GPLX phải được cấp ít nhất 12 tháng.", new[] { nameof(DriverLicenseYear) });
            }
            else
            {
                if (!BookingHolderDateOfBirth.HasValue)
                {
                    yield return new ValidationResult("Vui lòng nhập ngày sinh của người đứng tên đặt chuyến.", new[] { nameof(BookingHolderDateOfBirth) });
                }
                else
                {
                    var ageAtPickup = CalculateYears(BookingHolderDateOfBirth.Value.Date, PickUpDate.Date);
                    if (BookingHolderDateOfBirth.Value.Date >= PickUpDate.Date || ageAtPickup is < 18 or > 100)
                        yield return new ValidationResult("Người đứng tên đặt xe có tài xế phải từ đủ 18 tuổi tại ngày bắt đầu chuyến.", new[] { nameof(BookingHolderDateOfBirth) });
                }

                if (PassengerCount < 1)
                    yield return new ValidationResult("Vui lòng nhập số hành khách.", new[] { nameof(PassengerCount) });
                if (string.IsNullOrWhiteSpace(PickUpAddressText) || string.IsNullOrWhiteSpace(DropOffAddressText))
                    yield return new ValidationResult("Xe có tài xế yêu cầu địa chỉ đón và trả cụ thể.", new[] { nameof(PickUpAddressText), nameof(DropOffAddressText) });
                if (string.IsNullOrWhiteSpace(Itinerary))
                    yield return new ValidationResult("Vui lòng nhập lịch trình hoặc điểm dừng dự kiến.", new[] { nameof(Itinerary) });
            }

            if (DeliveryMethod == "Giao xe tận nơi" &&
                (string.IsNullOrWhiteSpace(PickUpAddressText) || string.IsNullOrWhiteSpace(DropOffAddressText)))
            {
                yield return new ValidationResult(
                    "Giao xe tận nơi yêu cầu địa chỉ nhận và trả xe cụ thể.",
                    new[] { nameof(PickUpAddressText), nameof(DropOffAddressText) });
            }

            if (!AcceptTerms)
            {
                yield return new ValidationResult(
                    "Bạn cần đồng ý với điều khoản thuê xe.",
                    new[] { nameof(AcceptTerms) });
            }
        }

        private static int CalculateYears(DateTime from, DateTime to)
        {
            var years = to.Year - from.Year;
            if (from.Date > to.AddYears(-years)) years--;
            return Math.Max(0, years);
        }
    }
}

