using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class Car
    {
        public int CarID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }
        [MaxLength(500)] public string? DeleteReason { get; set; }
        [MaxLength(30)] public string LifecycleStatus { get; set; } = "Đang hoạt động";
        [Timestamp] public byte[]? RowVersion { get; set; }
        public int BrandID { get; set; }
        public Brand Brand { get; set; }
        public string Model { get; set; }
        public string CoverImageUrl { get; set; }
        public int Km { get; set; }
        public string Transmission { get; set; }
        public byte Seat { get; set; }
        public byte Luggage { get; set; }
        public string Fuel { get; set; }
        public string BigImageUrl { get; set; }
        public List<CarFeature> CarFeatures { get; set; }
        public List<CarDescription> CarDescriptions { get; set; }
        public List<CarPricing> CarPricings { get; set; }
        public List<RentACar> RentACars { get; set; }
        public List<RentACarProcess> RentACarProcesses { get; set; }
        public List<Reservation> Reservations { get; set; }
        public List<Review> Reviews { get; set; }
    }
}
