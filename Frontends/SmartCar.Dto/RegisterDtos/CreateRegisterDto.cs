using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.RegisterDtos
{
    public class CreateRegisterDto : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 30 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ, số, dấu chấm và gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50)]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0\d{9}|\+84\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số bắt đầu bằng 0 hoặc dạng +84.")]
        public string Phone { get; set; } = string.Empty;

        // Không dùng [Range(typeof(bool), "true", "true")] cho checkbox.
        // Lý do: jQuery unobtrusive validation hiểu Range là số, nên checkbox dù đã tick vẫn bị báo sai ở client-side.
        // Việc bắt buộc tick được kiểm tra thủ công bằng IValidatableObject ở phía server.
        public bool AgreeTerms { get; set; }
        public bool AgreePrivacy { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!AgreeTerms)
            {
                yield return new ValidationResult(
                    "Bạn cần đồng ý với điều khoản sử dụng.",
                    new[] { nameof(AgreeTerms) });
            }

            if (!AgreePrivacy)
            {
                yield return new ValidationResult(
                    "Bạn cần đồng ý với chính sách bảo mật.",
                    new[] { nameof(AgreePrivacy) });
            }
        }
    }
}
