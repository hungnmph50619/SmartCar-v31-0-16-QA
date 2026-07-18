using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.StaffDtos
{
    public class ResetStaffPasswordDto
    {
        [Required]
        [StringLength(100, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
