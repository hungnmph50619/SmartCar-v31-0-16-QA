namespace SmartCar.Dto.NotificationDtos
{
    public class ResultCompanyAnnouncementDto
    {
        public int CompanyAnnouncementID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AudienceRole { get; set; } = string.Empty;
        public bool IsImportant { get; set; }
        public bool IsActive { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiresDate { get; set; }
    }
}
