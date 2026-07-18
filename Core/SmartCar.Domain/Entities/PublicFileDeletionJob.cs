using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class PublicFileDeletionJob
    {
        public long PublicFileDeletionJobID { get; set; }
        [MaxLength(1000)] public string FileUrl { get; set; } = string.Empty;
        [MaxLength(30)] public string Status { get; set; } = "Pending";
        public int RetryCount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? DeletedDate { get; set; }
        [MaxLength(100)] public string? LockedBy { get; set; }
        public DateTime? LockedUntil { get; set; }
        [MaxLength(2000)] public string? LastError { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
