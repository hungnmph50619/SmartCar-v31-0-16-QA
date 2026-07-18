using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class StaffOperationalIssue
    {
        public int StaffOperationalIssueID { get; set; }
        public int StaffAppUserID { get; set; }
        public int? AdminAppUserID { get; set; }
        public int? CustomerAppUserID { get; set; }
        public int? UserVerificationID { get; set; }
        [MaxLength(120)] public string IssueType { get; set; } = "Thu hồi kết quả duyệt hồ sơ";
        [MaxLength(30)] public string Severity { get; set; } = "Trung bình";
        [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
        [MaxLength(30)] public string Status { get; set; } = "Mới";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedDate { get; set; }
    }
}
