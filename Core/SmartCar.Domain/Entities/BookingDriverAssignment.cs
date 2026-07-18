using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class BookingDriverAssignment
    {
        public int BookingDriverAssignmentID { get; set; }
        public int ReservationID { get; set; }
        public Reservation Reservation { get; set; } = null!;
        public int DriverProfileID { get; set; }
        public DriverProfile DriverProfile { get; set; } = null!;
        public int AssignedByAppUserID { get; set; }
        [MaxLength(30)] public string Status { get; set; } = "Active";
        public bool IsPrimary { get; set; } = true;
        public DateTime AssignmentStartUtc { get; set; }
        public DateTime AssignmentEndUtc { get; set; }
        [MaxLength(1000)] public string? ChangeReason { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
