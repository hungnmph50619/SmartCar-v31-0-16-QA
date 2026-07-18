using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.StaffDtos
{
    public class CreateStaffDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}
