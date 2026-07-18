using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartCar.Domain.Entities
{
    public class Location
    {
        public int LocationID { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(120)]
        public string ProvinceCity { get; set; } = string.Empty;

        [MaxLength(120)]
        public string District { get; set; } = string.Empty;

        [MaxLength(120)]
        public string Ward { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AddressDetail { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        public int SearchRadiusKm { get; set; } = 20;
        public bool IsActive { get; set; } = true;

        public List<RentACar> RentACars { get; set; } = new();
        public List<Reservation> PickUpReservation { get; set; } = new();
        public List<Reservation> DropOffReservation { get; set; } = new();
    }
}
