using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class ReservationStatusHistory
    {
        public int ReservationStatusHistoryID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        [MaxLength(50)] public string OldStatus { get; set; } = string.Empty;
        [MaxLength(50)] public string NewStatus { get; set; } = string.Empty;
        public int? ChangedByAppUserID { get; set; }
        public AppUser? ChangedByAppUser { get; set; }
        [MaxLength(1000)] public string? Note { get; set; }
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    }
}
