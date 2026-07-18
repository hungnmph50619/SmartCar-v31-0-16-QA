namespace SmartCar.Dto.NotificationDtos
{
    public class NotificationItemDto
    {
        public int NotificationID { get; set; }
        public int AppUserID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}
