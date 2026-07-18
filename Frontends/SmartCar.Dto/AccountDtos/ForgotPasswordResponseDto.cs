namespace SmartCar.Dto.AccountDtos
{
    public class ForgotPasswordResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public string? DevelopmentResetUrl { get; set; }
    }
}
