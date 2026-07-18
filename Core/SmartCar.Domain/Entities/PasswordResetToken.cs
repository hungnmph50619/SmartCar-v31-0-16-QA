using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class PasswordResetToken
    {
        public int PasswordResetTokenID { get; set; }
        public int AppUserID { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
        public DateTime ExpiresDate { get; set; }
        public DateTime? UsedDate { get; set; }
    }
}
