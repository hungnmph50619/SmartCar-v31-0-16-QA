namespace SmartCar.Dto.StaffDtos
{
    public class ResultStaffDto
    {
        public int AppUserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Staff";
    }
}
