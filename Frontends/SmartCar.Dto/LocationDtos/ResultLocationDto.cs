namespace SmartCar.Dto.LocationDtos
{
    public class ResultLocationDto
    {
        public int LocationID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProvinceCity { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int SearchRadiusKm { get; set; } = 20;
        public bool IsActive { get; set; } = true;

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

        public string AdministrativeAddress => string.Join(", ", new[] { Ward, District, ProvinceCity }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        public string FullAddress => string.Join(", ", new[] { AddressDetail, Ward, District, ProvinceCity }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        public string DisplayName
        {
            get
            {
                var suffix = FullAddress;
                if (string.IsNullOrWhiteSpace(suffix)) return Name;
                if (suffix.Contains(Name, StringComparison.OrdinalIgnoreCase)) return suffix;
                return $"{Name} — {suffix}";
            }
        }
    }
}
