using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartCar.WebUI.Models
{
    public class CustomerVerificationViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; } = string.Empty;


        [Required(ErrorMessage = "Vui lòng nhập họ tên pháp lý theo CCCD.")]
        [StringLength(120)]
        public string LegalFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập ngày cấp CCCD.")]
        [DataType(DataType.Date)]
        public DateTime CitizenIdIssuedDate { get; set; } = SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-2);

        [Required(ErrorMessage = "Vui lòng nhập ngày hết hạn CCCD.")]
        [DataType(DataType.Date)]
        public DateTime CitizenIdExpiryDate { get; set; } = SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(8);

        // Địa chỉ theo giấy tờ: lưu đúng dòng địa chỉ đang ghi trên CCCD/GPLX,
        // kể cả khi giấy tờ còn dùng địa chỉ hành chính cũ.
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ theo giấy tờ.")]
        [StringLength(500)]
        public string CitizenIdAddress { get; set; } = string.Empty;

        // Dropdown gửi mã hành chính; tên hiển thị được API tra từ database và lưu làm bản chụp.
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố thường trú.")]
        [StringLength(2, MinimumLength = 2)]
        public string PermanentProvinceCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn xã/phường/đặc khu thường trú.")]
        [StringLength(5, MinimumLength = 5)]
        public string PermanentWardCode { get; set; } = string.Empty;

        // Tên và địa chỉ ghép được giữ để hiển thị dữ liệu cũ; server không tin các giá trị này khi gửi hồ sơ mới.
        [StringLength(100)]
        public string PermanentProvince { get; set; } = string.Empty;

        [StringLength(150)]
        public string PermanentWard { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số nhà, đường, thôn/xóm/tổ dân phố thường trú.")]
        [StringLength(300)]
        public string PermanentDetail { get; set; } = string.Empty;

        [StringLength(500)]
        public string PermanentAddress { get; set; } = string.Empty;

        public bool CurrentAddressSameAsPermanent { get; set; }

        [StringLength(2)]
        public string CurrentProvinceCode { get; set; } = string.Empty;

        [StringLength(5)]
        public string CurrentWardCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string CurrentProvince { get; set; } = string.Empty;

        [StringLength(150)]
        public string CurrentWard { get; set; } = string.Empty;

        [StringLength(300)]
        public string CurrentDetail { get; set; } = string.Empty;

        [StringLength(500)]
        public string CurrentAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số giấy phép lái xe.")]
        [StringLength(50)]
        public string DriverLicenseNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập hạng giấy phép lái xe.")]
        [StringLength(20)]
        public string DriverLicenseClass { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-22);

        [Required(ErrorMessage = "Vui lòng nhập ngày cấp bằng lái.")]
        [DataType(DataType.Date)]
        public DateTime DriverLicenseIssuedDate { get; set; } = SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(-2);

        [Required(ErrorMessage = "Vui lòng nhập ngày hết hạn bằng lái.")]
        [DataType(DataType.Date)]
        public DateTime DriverLicenseExpiry { get; set; } = SmartCar.WebUI.Infrastructure.VietnamTime.Today.AddYears(3);

        // Mỗi lần gửi hoặc gửi lại hồ sơ phải nhập đủ 12 số. Server chỉ lưu bản che
        // và không nhận CitizenIdMasked do trình duyệt tự tạo.
        public string? CitizenIdNumber { get; set; }

        // Ảnh không dùng [Required] vì khi gửi lại khách chỉ cần thay ảnh bị sai.
        // Nếu chưa có ảnh cũ, controller sẽ yêu cầu tải ảnh tương ứng.
        public IFormFile? CitizenIdFront { get; set; }
        public IFormFile? CitizenIdBack { get; set; }
        public IFormFile? DriverLicense { get; set; }
        public IFormFile? Portrait { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
