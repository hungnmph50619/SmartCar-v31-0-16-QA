using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.ReservationDtos
{
    public class UpdateReservationStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Note { get; set; }
    }
}
