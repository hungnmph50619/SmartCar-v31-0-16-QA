using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.LocationDtos
{
    public class UpdateLocationDto : CreateLocationDto
    {
        [Range(1, int.MaxValue)]
        public int LocationID { get; set; }
    }
}
