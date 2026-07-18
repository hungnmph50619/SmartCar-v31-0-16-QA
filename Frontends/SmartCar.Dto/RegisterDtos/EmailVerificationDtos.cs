using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.RegisterDtos
{
    public class RegisterResultDto
    {
        public Guid RegistrationAttemptId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public int OtpExpiresInMinutes { get; set; } = 5;
        public int ResendAfterSeconds { get; set; } = 60;
    }

    public class VerifyEmailOtpDto
    {
        public Guid? RegistrationAttemptId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 30 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập OTP.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải gồm đúng 6 chữ số.")]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResendEmailOtpDto
    {
        public Guid? RegistrationAttemptId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 30 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;
    }
}
