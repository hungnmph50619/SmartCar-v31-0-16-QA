using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class AdministrativeWard
    {
        [Key, MaxLength(5)]
        public string WardCode { get; set; } = string.Empty;

        [Required, MaxLength(2)]
        public string ProvinceCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string WardName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string WardType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public AdministrativeProvince Province { get; set; } = null!;
    }
}
