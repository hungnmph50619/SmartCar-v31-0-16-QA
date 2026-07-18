using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.AccountDtos
{
    public class UpdateUserProfileDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(80)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? Phone { get; set; }
    }
}
