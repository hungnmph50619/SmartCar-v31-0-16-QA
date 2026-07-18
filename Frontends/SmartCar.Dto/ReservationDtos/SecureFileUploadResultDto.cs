namespace SmartCar.Dto.ReservationDtos
{
    public class SecureFileUploadResultDto
    {
        public Guid PrivateFileId { get; set; }
        public string ViewUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
