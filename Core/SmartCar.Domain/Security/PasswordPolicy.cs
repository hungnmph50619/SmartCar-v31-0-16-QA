namespace SmartCar.Domain.Security
{
    public static class PasswordPolicy
    {
        public const int MinimumLength = 8;
        public const int MaximumLength = 100;

        public static string? Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return "Vui lòng nhập mật khẩu.";
            if (password.Length < MinimumLength)
                return $"Mật khẩu phải có ít nhất {MinimumLength} ký tự.";
            if (password.Length > MaximumLength)
                return $"Mật khẩu không được vượt quá {MaximumLength} ký tự.";
            if (!password.Any(char.IsLetter))
                return "Mật khẩu phải có ít nhất một chữ cái.";
            if (!password.Any(char.IsDigit))
                return "Mật khẩu phải có ít nhất một chữ số.";
            if (password.Any(char.IsControl))
                return "Mật khẩu chứa ký tự điều khiển không hợp lệ.";
            return null;
        }

        public static void EnsureValid(string? password)
        {
            var error = Validate(password);
            if (error is not null) throw new ArgumentException(error, nameof(password));
        }
    }
}
