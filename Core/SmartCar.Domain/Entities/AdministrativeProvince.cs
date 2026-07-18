using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class AdministrativeProvince
    {
        [Key, MaxLength(2)]
        public string ProvinceCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ProvinceName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ProvinceType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public ICollection<AdministrativeWard> Wards { get; set; } = new List<AdministrativeWard>();
    }
}
