namespace SmartCar.WebUI.Models
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public string Theme { get; set; } = "public";
        public string DisplayCount => UnreadCount > 99 ? "99+" : UnreadCount.ToString();
    }
}
