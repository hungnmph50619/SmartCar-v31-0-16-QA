using System.ComponentModel.DataAnnotations;

namespace SmartCar.Dto.ContactDtos
{
    public class CreateContactDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập chủ đề.")]
        [StringLength(200, ErrorMessage = "Chủ đề tối đa 200 ký tự.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn.")]
        [StringLength(2000, ErrorMessage = "Tin nhắn tối đa 2000 ký tự.")]
        public string Message { get; set; } = string.Empty;

        public DateTime SendDate { get; set; }
    }
}
