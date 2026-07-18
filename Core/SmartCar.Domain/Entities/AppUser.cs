using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class AppUser
    {
        public int AppUserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool IsVehiclePartner { get; set; }

        
        [MaxLength(20)]
        public string AccountType { get; set; } = "Customer";
public int FailedLoginCount { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool EmailConfirmed { get; set; }

        [MaxLength(256)]
        public string? PendingEmail { get; set; }
        public DateTime? PendingEmailCreatedDate { get; set; }
        public DateTime? RegistrationExpiresDate { get; set; }

        public int TokenVersion { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; } = true;
        [MaxLength(30)] public string? LockType { get; set; }
        [MaxLength(500)] public string? LockReason { get; set; }
        public DateTime? LockedAt { get; set; }
        public int? LockedByAppUserID { get; set; }
        public DateTime? BookingRestrictedUntil { get; set; }
        [MaxLength(500)] public string? BookingRestrictionReason { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }
        [MaxLength(500)] public string? DeleteReason { get; set; }
        public DateTime? AnonymizedAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }

        public int AppRoleId { get; set; }
        public AppRole AppRole { get; set; } = null!;
    }
}
