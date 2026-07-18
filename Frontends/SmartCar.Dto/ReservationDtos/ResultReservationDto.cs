namespace SmartCar.Dto.ReservationDtos
{
    public class ResultReservationDto
    {
        public int ReservationID { get; set; }
        public int CustomerAppUserID { get; set; }
        public int PartnerVehicleID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int CarID { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerPhone { get; set; } = string.Empty;
        public string PickUpLocation { get; set; } = string.Empty;
        public string DropOffLocation { get; set; } = string.Empty;
        public string RentalMode { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty;
        public DateTime PickUpDate { get; set; }
        public DateTime DropOffDate { get; set; }
        public TimeSpan PickUpTime { get; set; }
        public TimeSpan DropOffTime { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CommissionRateSnapshot { get; set; }
        public decimal PlatformFeeAmount { get; set; }
        public decimal PartnerReceivableAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public string DepositStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
        public string? OwnerNote { get; set; }
    }
}
