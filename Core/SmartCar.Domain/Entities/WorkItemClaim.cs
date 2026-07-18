using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class WorkItemClaim
    {
        public int WorkItemClaimID { get; set; }
        [MaxLength(50)] public string QueueType { get; set; } = string.Empty;
        public int EntityID { get; set; }
        public int AssignedStaffAppUserID { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueAt { get; set; }
        [MaxLength(30)] public string Status { get; set; } = "Đang xử lý";
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
