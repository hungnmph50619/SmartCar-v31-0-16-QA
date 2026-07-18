using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class PrivateFile
    {
        public Guid PrivateFileID { get; set; } = Guid.NewGuid();
        public int OwnerAppUserID { get; set; }
        public AppUser OwnerAppUser { get; set; } = null!;
        public int? ReservationID { get; set; }
        public Reservation? Reservation { get; set; }
        public int? PartnerApplicationID { get; set; }
        public VehiclePartnerApplication? PartnerApplication { get; set; }
        [MaxLength(100)] public string Category { get; set; } = string.Empty;
        [MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;
        [MaxLength(255)] public string StoredFileName { get; set; } = string.Empty;
        [MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
        public long FileSize { get; set; }
        [MaxLength(64)] public string? Sha256Hash { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        [MaxLength(100)] public string? AttachedEntityType { get; set; }
        [MaxLength(100)] public string? AttachedEntityID { get; set; }
        public DateTime? AttachedDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeleteRequestedDate { get; set; }
        public DateTime? PhysicalDeletedDate { get; set; }
        public int DeleteRetryCount { get; set; }
        [MaxLength(1000)] public string? LastDeleteError { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
