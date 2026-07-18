using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class Review
    {
        public int ReviewID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }
        [MaxLength(500)] public string? DeleteReason { get; set; }
        [MaxLength(150)] public string CustomerName { get; set; } = string.Empty;
        [MaxLength(500)] public string CustomerImage { get; set; } = string.Empty;
        [MaxLength(2000)] public string Comment { get; set; } = string.Empty;
        public int RaytingValue { get; set; }
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
        public Car Car { get; set; } = null!;
        public int CarID { get; set; }
        public int? AppUserID { get; set; }
        public AppUser? AppUser { get; set; }
        public int? ReservationID { get; set; }
        public Reservation? Reservation { get; set; }
        [MaxLength(30)] public string ReviewerRole { get; set; } = "Customer";
        [MaxLength(30)] public string TargetType { get; set; } = "Vehicle";
        public int? TargetAppUserID { get; set; }
        public int? TargetDriverProfileID { get; set; }
        public bool IsHidden { get; set; }
        [MaxLength(500)] public string? HiddenReason { get; set; }
        public int? HiddenByAppUserID { get; set; }
        public DateTime? HiddenAt { get; set; }
        public DateTime? VisibleFromDate { get; set; }
    }
}
