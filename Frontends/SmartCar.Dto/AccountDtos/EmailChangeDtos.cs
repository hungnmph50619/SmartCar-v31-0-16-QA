using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.AccountDtos
{
    public class RequestEmailChangeDto
    {
        [Required(ErrorMessage = "Vui lòng nhập email mới.")]
        [EmailAddress(ErrorMessage = "Email mới không hợp lệ.")]
        [StringLength(256)]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public class ConfirmEmailChangeDto
    {
        [Required(ErrorMessage = "Vui lòng nhập OTP.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải gồm 6 chữ số.")]
        public string Otp { get; set; } = string.Empty;
    }
}
