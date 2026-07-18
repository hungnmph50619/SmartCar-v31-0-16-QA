using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartCar.Dto.MarketplaceDtos
{
    public class CreateVehiclePartnerAccountDto : IValidatableObject
    {
        public const string IndividualPartnerType = "Cá nhân";
        public const string OrganizationPartnerType = "Doanh nghiệp/Tổ chức";

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(30, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 30 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ, số, dấu chấm và gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(150)] public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0\d{9}|\+84\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số bắt đầu bằng 0 hoặc dạng +84.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất một chữ cái và một chữ số.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại đối tác.")]
        [StringLength(40)] public string PartnerType { get; set; } = IndividualPartnerType;

        public bool AgreePartnerTerms { get; set; }
        public bool AgreePrivacyPolicy { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamZone).Date;
            if (PartnerType is not (IndividualPartnerType or OrganizationPartnerType))
            {
                yield return new ValidationResult("Loại đối tác chỉ được chọn Cá nhân hoặc Doanh nghiệp/Tổ chức.", new[] { nameof(PartnerType) });
            }

            if (!AgreePartnerTerms)
            {
                yield return new ValidationResult("Bạn cần đồng ý với điều khoản đối tác của SmartCar.", new[] { nameof(AgreePartnerTerms) });
            }

            if (!AgreePrivacyPolicy)
            {
                yield return new ValidationResult("Bạn cần đồng ý với chính sách bảo mật của SmartCar.", new[] { nameof(AgreePrivacyPolicy) });
            }
        }
    }

    public class SubmitVehiclePartnerProfileDto : IValidatableObject
    {
        private static readonly string[] AllowedGenders = { "Nam", "Nữ", "Khác" };

        public static readonly string[] SupportedBanks =
        {
            "Agribank",
            "BIDV",
            "VietinBank",
            "Vietcombank",
            "MB Bank",
            "Techcombank",
            "ACB",
            "VPBank",
            "TPBank",
            "HDBank",
            "VIB",
            "SHB",
            "Sacombank",
            "MSB",
            "OCB",
            "SeABank",
            "LPBank",
            "Eximbank",
            "Nam A Bank",
            "ABBANK",
            "Bac A Bank",
            "BaoViet Bank",
            "BVBank",
            "VietABank",
            "VietBank",
            "KienlongBank",
            "NCB",
            "PGBank",
            "Saigonbank",
            "PVcomBank",
            "SCB",
            "DongA Bank",
            "CB Bank",
            "OceanBank",
            "GPBank",
            "Co-opBank",
            "Ngân hàng Chính sách xã hội",
            "Ngân hàng Phát triển Việt Nam",
            "HSBC Việt Nam",
            "Shinhan Bank Việt Nam",
            "Woori Bank Việt Nam",
            "Standard Chartered Việt Nam",
            "UOB Việt Nam",
            "Public Bank Việt Nam",
            "Hong Leong Bank Việt Nam",
            "CIMB Việt Nam",
            "Kookmin Bank Việt Nam",
            "Indovina Bank",
            "Ngân hàng Việt - Nga"
        };

        [Required, MaxLength(40)] public string PartnerType { get; set; } = CreateVehiclePartnerAccountDto.IndividualPartnerType;

        [MaxLength(160)] public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(20)] public string Gender { get; set; } = string.Empty;
        [MaxLength(20)] public string CitizenIdentityNumber { get; set; } = string.Empty;
        public DateTime? CitizenIssuedDate { get; set; }
        public DateTime? CitizenExpiryDate { get; set; }

        [MaxLength(2)] public string PermanentProvinceCode { get; set; } = string.Empty;
        [MaxLength(5)] public string PermanentWardCode { get; set; } = string.Empty;
        [MaxLength(80)] public string PermanentProvince { get; set; } = string.Empty;
        [MaxLength(120)] public string PermanentWard { get; set; } = string.Empty;
        [MaxLength(300)] public string PermanentDetail { get; set; } = string.Empty;
        [MaxLength(500)] public string PermanentPaperAddress { get; set; } = string.Empty;
        [MaxLength(300)] public string PermanentAddress { get; set; } = string.Empty;

        public bool CurrentAddressSameAsPermanent { get; set; }
        [MaxLength(2)] public string CurrentProvinceCode { get; set; } = string.Empty;
        [MaxLength(5)] public string CurrentWardCode { get; set; } = string.Empty;
        [MaxLength(80)] public string CurrentProvince { get; set; } = string.Empty;
        [MaxLength(120)] public string CurrentWard { get; set; } = string.Empty;
        [MaxLength(300)] public string CurrentDetail { get; set; } = string.Empty;
        [MaxLength(300)] public string CurrentAddress { get; set; } = string.Empty;

        public Guid CitizenFrontFileId { get; set; }
        public Guid CitizenBackFileId { get; set; }
        public Guid PortraitFileId { get; set; }

        [MaxLength(200)] public string BusinessName { get; set; } = string.Empty;
        [MaxLength(50)] public string TaxCode { get; set; } = string.Empty;
        [MaxLength(80)] public string BusinessRegistrationNumber { get; set; } = string.Empty;
        [MaxLength(2)] public string HeadquartersProvinceCode { get; set; } = string.Empty;
        [MaxLength(5)] public string HeadquartersWardCode { get; set; } = string.Empty;
        [MaxLength(80)] public string HeadquartersProvince { get; set; } = string.Empty;
        [MaxLength(120)] public string HeadquartersWard { get; set; } = string.Empty;
        [MaxLength(300)] public string HeadquartersDetail { get; set; } = string.Empty;
        [MaxLength(500)] public string HeadquartersPaperAddress { get; set; } = string.Empty;
        [MaxLength(300)] public string HeadquartersAddress { get; set; } = string.Empty;
        [MaxLength(160)] public string LegalRepresentativeName { get; set; } = string.Empty;
        [MaxLength(160)] public string AccountManagerName { get; set; } = string.Empty;
        [MaxLength(100)] public string AccountManagerTitle { get; set; } = string.Empty;
        [MaxLength(160)] public string RepresentativeName { get; set; } = string.Empty;
        [MaxLength(100)] public string RepresentativeTitle { get; set; } = string.Empty;
        public Guid BusinessLicenseFileId { get; set; }
        public Guid? AuthorizationDocumentFileId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng.")]
        [MaxLength(120)] public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản nhận đối soát.")]
        [MaxLength(50)] public string BankAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản nhận đối soát.")]
        [MaxLength(160)] public string BankAccountHolder { get; set; } = string.Empty;

        [MaxLength(120)] public string? BankBranch { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamZone).Date;
            if (PartnerType is not (CreateVehiclePartnerAccountDto.IndividualPartnerType or CreateVehiclePartnerAccountDto.OrganizationPartnerType))
            {
                yield return new ValidationResult("Loại đối tác không hợp lệ.", new[] { nameof(PartnerType) });
            }

            if (string.IsNullOrWhiteSpace(BankName))
            {
                yield return new ValidationResult("Vui lòng chọn ngân hàng nhận đối soát.", new[] { nameof(BankName) });
            }
            else if (!SupportedBanks.Contains(BankName.Trim()))
            {
                yield return new ValidationResult("Tên ngân hàng phải được chọn từ danh sách ngân hàng được hỗ trợ.", new[] { nameof(BankName) });
            }

            if (PartnerType == CreateVehiclePartnerAccountDto.IndividualPartnerType)
            {
                if (string.IsNullOrWhiteSpace(FullName)) yield return new ValidationResult("Vui lòng nhập họ tên theo CCCD.", new[] { nameof(FullName) });
                if (!DateOfBirth.HasValue) yield return new ValidationResult("Vui lòng nhập ngày sinh.", new[] { nameof(DateOfBirth) });
                if (DateOfBirth.HasValue && DateOfBirth.Value.Date >= today) yield return new ValidationResult("Ngày sinh phải nhỏ hơn ngày hiện tại.", new[] { nameof(DateOfBirth) });
                if (!AllowedGenders.Contains(Gender ?? string.Empty)) yield return new ValidationResult("Giới tính không hợp lệ.", new[] { nameof(Gender) });
                if (!Regex.IsMatch(CitizenIdentityNumber ?? string.Empty, @"^\d{12}$")) yield return new ValidationResult("Số CCCD phải gồm 12 chữ số.", new[] { nameof(CitizenIdentityNumber) });
                if (!CitizenIssuedDate.HasValue) yield return new ValidationResult("Vui lòng nhập ngày cấp CCCD.", new[] { nameof(CitizenIssuedDate) });
                if (!CitizenExpiryDate.HasValue) yield return new ValidationResult("Vui lòng nhập ngày hết hạn CCCD.", new[] { nameof(CitizenExpiryDate) });
                if (CitizenIssuedDate.HasValue && CitizenIssuedDate.Value.Date > today) yield return new ValidationResult("Ngày cấp CCCD không được lớn hơn ngày hiện tại.", new[] { nameof(CitizenIssuedDate) });
                if (CitizenIssuedDate.HasValue && CitizenExpiryDate.HasValue && CitizenExpiryDate.Value.Date <= CitizenIssuedDate.Value.Date) yield return new ValidationResult("Ngày hết hạn CCCD phải sau ngày cấp.", new[] { nameof(CitizenExpiryDate) });
                if (CitizenExpiryDate.HasValue && CitizenExpiryDate.Value.Date < today) yield return new ValidationResult("CCCD không được hết hạn tại thời điểm gửi hồ sơ.", new[] { nameof(CitizenExpiryDate) });
                foreach (var error in ValidateAddress(PermanentProvinceCode, PermanentWardCode, PermanentDetail, "thường trú", nameof(PermanentProvinceCode), nameof(PermanentWardCode), nameof(PermanentDetail))) yield return error;
                if (!CurrentAddressSameAsPermanent)
                {
                    foreach (var error in ValidateAddress(CurrentProvinceCode, CurrentWardCode, CurrentDetail, "hiện tại", nameof(CurrentProvinceCode), nameof(CurrentWardCode), nameof(CurrentDetail))) yield return error;
                }
                if (CitizenFrontFileId == Guid.Empty) yield return new ValidationResult("Vui lòng tải ảnh CCCD mặt trước.", new[] { nameof(CitizenFrontFileId) });
                if (CitizenBackFileId == Guid.Empty) yield return new ValidationResult("Vui lòng tải ảnh CCCD mặt sau.", new[] { nameof(CitizenBackFileId) });
                if (PortraitFileId == Guid.Empty) yield return new ValidationResult("Vui lòng tải ảnh chân dung hiện tại.", new[] { nameof(PortraitFileId) });
                if (!string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(BankAccountHolder) && NormalizeForCompare(FullName) != NormalizeForCompare(BankAccountHolder))
                {
                    yield return new ValidationResult("Tên chủ tài khoản ngân hàng phải khớp với họ tên theo CCCD của đối tác cá nhân.", new[] { nameof(BankAccountHolder) });
                }
            }
            else if (PartnerType == CreateVehiclePartnerAccountDto.OrganizationPartnerType)
            {
                if (string.IsNullOrWhiteSpace(BusinessName)) yield return new ValidationResult("Vui lòng nhập tên doanh nghiệp/tổ chức.", new[] { nameof(BusinessName) });
                if (string.IsNullOrWhiteSpace(TaxCode)) yield return new ValidationResult("Vui lòng nhập mã số thuế.", new[] { nameof(TaxCode) });
                if (!string.IsNullOrWhiteSpace(TaxCode) && !Regex.IsMatch(TaxCode.Trim(), @"^\d{10}(-\d{3})?$")) yield return new ValidationResult("Mã số thuế phải có 10 chữ số hoặc dạng 10 chữ số-3 chữ số.", new[] { nameof(TaxCode) });
                if (string.IsNullOrWhiteSpace(BusinessRegistrationNumber)) yield return new ValidationResult("Vui lòng nhập số đăng ký kinh doanh hoặc mã định danh pháp lý tương đương.", new[] { nameof(BusinessRegistrationNumber) });
                foreach (var error in ValidateAddress(HeadquartersProvinceCode, HeadquartersWardCode, HeadquartersDetail, "trụ sở", nameof(HeadquartersProvinceCode), nameof(HeadquartersWardCode), nameof(HeadquartersDetail))) yield return error;
                if (string.IsNullOrWhiteSpace(LegalRepresentativeName)) yield return new ValidationResult("Vui lòng nhập người đại diện pháp luật.", new[] { nameof(LegalRepresentativeName) });
                if (string.IsNullOrWhiteSpace(AccountManagerName)) yield return new ValidationResult("Vui lòng nhập người phụ trách tài khoản.", new[] { nameof(AccountManagerName) });
                if (string.IsNullOrWhiteSpace(AccountManagerTitle)) yield return new ValidationResult("Vui lòng nhập chức vụ của người phụ trách.", new[] { nameof(AccountManagerTitle) });
                if (BusinessLicenseFileId == Guid.Empty) yield return new ValidationResult("Vui lòng tải giấy đăng ký doanh nghiệp hoặc tài liệu pháp lý tương đương.", new[] { nameof(BusinessLicenseFileId) });
                if (!string.IsNullOrWhiteSpace(BusinessName) && !string.IsNullOrWhiteSpace(BankAccountHolder) && NormalizeForCompare(BusinessName) != NormalizeForCompare(BankAccountHolder))
                {
                    yield return new ValidationResult("Tên chủ tài khoản ngân hàng phải khớp với tên doanh nghiệp/tổ chức đã xác minh.", new[] { nameof(BankAccountHolder) });
                }
            }
        }

        private static IEnumerable<ValidationResult> ValidateAddress(string provinceCode, string wardCode, string detail, string label, string provinceMember, string wardMember, string detailMember)
        {
            if (!Regex.IsMatch(provinceCode ?? string.Empty, @"^\d{2}$"))
            {
                yield return new ValidationResult($"Vui lòng chọn tỉnh/thành phố {label} hợp lệ.", new[] { provinceMember });
            }
            if (!Regex.IsMatch(wardCode ?? string.Empty, @"^\d{5}$"))
            {
                yield return new ValidationResult($"Vui lòng chọn xã/phường/đặc khu {label} hợp lệ.", new[] { wardMember });
            }
            if (string.IsNullOrWhiteSpace(detail))
            {
                yield return new ValidationResult($"Vui lòng nhập địa chỉ chi tiết {label}.", new[] { detailMember });
            }
        }

        public static string BuildAddress(string province, string ward, string detail)
        {
            return string.Join(", ", new[] { detail?.Trim(), ward?.Trim(), province?.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public static string NormalizeForCompare(string? value)
        {
            var formD = (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
            }
            return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D").ToUpperInvariant(), @"\s+", " ").Trim();
        }
    }

    public class ReviewVehiclePartnerProfileDto
    {
        [Required] public string Status { get; set; } = string.Empty;
        [MaxLength(1000)] public string? ReviewNote { get; set; }
    }

    public class VehiclePartnerProfileDto
    {
        public int VehiclePartnerProfileID { get; set; }
        public int AppUserID { get; set; }
        public string PartnerType { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CitizenIdentityNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime? CitizenIssuedDate { get; set; }
        public DateTime? CitizenExpiryDate { get; set; }
        public string PermanentProvinceCode { get; set; } = string.Empty;
        public string PermanentWardCode { get; set; } = string.Empty;
        public string PermanentProvince { get; set; } = string.Empty;
        public string PermanentWard { get; set; } = string.Empty;
        public string PermanentDetail { get; set; } = string.Empty;
        public string PermanentPaperAddress { get; set; } = string.Empty;
        public string PermanentAddress { get; set; } = string.Empty;
        public bool CurrentAddressSameAsPermanent { get; set; }
        public string CurrentProvinceCode { get; set; } = string.Empty;
        public string CurrentWardCode { get; set; } = string.Empty;
        public string CurrentProvince { get; set; } = string.Empty;
        public string CurrentWard { get; set; } = string.Empty;
        public string CurrentDetail { get; set; } = string.Empty;
        public string CurrentAddress { get; set; } = string.Empty;
        public string CitizenFrontImageUrl { get; set; } = string.Empty;
        public string CitizenBackImageUrl { get; set; } = string.Empty;
        public string PortraitImageUrl { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string BusinessRegistrationNumber { get; set; } = string.Empty;
        public string HeadquartersProvinceCode { get; set; } = string.Empty;
        public string HeadquartersWardCode { get; set; } = string.Empty;
        public string HeadquartersProvince { get; set; } = string.Empty;
        public string HeadquartersWard { get; set; } = string.Empty;
        public string HeadquartersDetail { get; set; } = string.Empty;
        public string HeadquartersPaperAddress { get; set; } = string.Empty;
        public string HeadquartersAddress { get; set; } = string.Empty;
        public string LegalRepresentativeName { get; set; } = string.Empty;
        public string AccountManagerName { get; set; } = string.Empty;
        public string AccountManagerTitle { get; set; } = string.Empty;
        public string RepresentativeName { get; set; } = string.Empty;
        public string RepresentativeTitle { get; set; } = string.Empty;
        public string BusinessLicenseImageUrl { get; set; } = string.Empty;
        public string AuthorizationDocumentUrl { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
        public string BankBranch { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReviewNote { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
    }
}
