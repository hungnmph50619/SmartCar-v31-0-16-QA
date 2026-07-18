namespace SmartCar.Dto.AccountDtos
{
    public class UserProfileDto
    {
        public int AppUserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PendingEmail { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsVehiclePartner { get; set; }
        public string? Address { get; set; }
        public string? CitizenIdentityNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolder { get; set; }
        public decimal CustomerRating { get; set; }
        public int CustomerRatingCount { get; set; }
    }
}
