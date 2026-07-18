namespace SmartCar.Dto.ReservationDtos
{
    public class CreateReservationResponseDto
    {
        public int ReservationID { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int RentalDays { get; set; }
    }
}
