using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class CompanyAnnouncement
    {
        public int CompanyAnnouncementID { get; set; }

        [MaxLength(180)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(30)]
        public string AudienceRole { get; set; } = "Customer";

        public bool IsImportant { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiresDate { get; set; }
        public int? CreatedByAppUserID { get; set; }
    }
}
