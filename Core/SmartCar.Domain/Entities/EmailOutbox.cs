using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class EmailOutbox
    {
        public long EmailOutboxID { get; set; }
        [MaxLength(100)] public string? MessageKey { get; set; }
        [MaxLength(256)] public string RecipientEmail { get; set; } = string.Empty;
        [MaxLength(500)] public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        [MaxLength(30)] public string Status { get; set; } = "Pending";
        public int RetryCount { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? SentDate { get; set; }
        [MaxLength(100)] public string? LockedBy { get; set; }
        public DateTime? LockedUntil { get; set; }
        [MaxLength(2000)] public string? LastError { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
