using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.NotificationDtos
{
    public class CreateCompanyAnnouncementDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề thông báo.")]
        [StringLength(180)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung thông báo.")]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string AudienceRole { get; set; } = "Customer";
        public bool IsImportant { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiresDate { get; set; }
    }
}
