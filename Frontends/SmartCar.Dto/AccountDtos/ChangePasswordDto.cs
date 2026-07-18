using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.AccountDtos
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
