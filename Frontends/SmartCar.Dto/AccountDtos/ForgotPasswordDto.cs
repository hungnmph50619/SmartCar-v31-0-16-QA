using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.AccountDtos
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 30 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email đã đăng ký.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}
