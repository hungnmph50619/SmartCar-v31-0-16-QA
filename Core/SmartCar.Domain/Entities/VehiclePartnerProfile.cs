using System.ComponentModel.DataAnnotations;

namespace SmartCar.Domain.Entities
{
    public class VehiclePartnerProfile
    {
        public int VehiclePartnerProfileID { get; set; }
        public int AppUserID { get; set; }
        public AppUser AppUser { get; set; } = null!;

        [MaxLength(40)] public string PartnerType { get; set; } = "Cá nhân";
        [MaxLength(160)] public string FullName { get; set; } = string.Empty;
        [MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [MaxLength(150)] public string Email { get; set; } = string.Empty;
        [MaxLength(300)] public string Address { get; set; } = string.Empty;
        [MaxLength(20)] public string CitizenIdentityNumber { get; set; } = string.Empty;
        [MaxLength(64)] public string? CitizenIdFingerprint { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [MaxLength(20)] public string Gender { get; set; } = string.Empty;
        public DateTime? CitizenIssuedDate { get; set; }
        public DateTime? CitizenExpiryDate { get; set; }
        [MaxLength(2)] public string? PermanentProvinceCode { get; set; }
        [MaxLength(5)] public string? PermanentWardCode { get; set; }
        [MaxLength(80)] public string PermanentProvince { get; set; } = string.Empty;
        [MaxLength(120)] public string PermanentWard { get; set; } = string.Empty;
        [MaxLength(300)] public string PermanentDetail { get; set; } = string.Empty;
        [MaxLength(500)] public string PermanentPaperAddress { get; set; } = string.Empty;
        [MaxLength(300)] public string PermanentAddress { get; set; } = string.Empty;
        public bool CurrentAddressSameAsPermanent { get; set; }
        [MaxLength(2)] public string? CurrentProvinceCode { get; set; }
        [MaxLength(5)] public string? CurrentWardCode { get; set; }
        [MaxLength(80)] public string CurrentProvince { get; set; } = string.Empty;
        [MaxLength(120)] public string CurrentWard { get; set; } = string.Empty;
        [MaxLength(300)] public string CurrentDetail { get; set; } = string.Empty;
        [MaxLength(300)] public string CurrentAddress { get; set; } = string.Empty;
        [MaxLength(500)] public string CitizenFrontImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string CitizenBackImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string PortraitImageUrl { get; set; } = string.Empty;

        [MaxLength(200)] public string BusinessName { get; set; } = string.Empty;
        [MaxLength(50)] public string TaxCode { get; set; } = string.Empty;
        [MaxLength(80)] public string BusinessRegistrationNumber { get; set; } = string.Empty;
        [MaxLength(2)] public string? HeadquartersProvinceCode { get; set; }
        [MaxLength(5)] public string? HeadquartersWardCode { get; set; }
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
        [MaxLength(500)] public string BusinessLicenseImageUrl { get; set; } = string.Empty;
        [MaxLength(500)] public string AuthorizationDocumentUrl { get; set; } = string.Empty;

        [MaxLength(120)] public string BankName { get; set; } = string.Empty;
        [MaxLength(50)] public string BankAccountNumber { get; set; } = string.Empty;
        [MaxLength(160)] public string BankAccountHolder { get; set; } = string.Empty;
        [MaxLength(120)] public string BankBranch { get; set; } = string.Empty;
        public bool IsPayoutPaused { get; set; }
        [MaxLength(500)] public string? PayoutPauseReason { get; set; }

        [MaxLength(40)] public string Status { get; set; } = "Bản nháp";
        [MaxLength(1000)] public string? ReviewNote { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public int? ReviewedByAppUserID { get; set; }
        [MaxLength(40)] public string? PartnerTermsVersion { get; set; }
        [MaxLength(40)] public string? PrivacyPolicyVersion { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }
        public DateTime? PrivacyAcceptedAt { get; set; }
    }
}
