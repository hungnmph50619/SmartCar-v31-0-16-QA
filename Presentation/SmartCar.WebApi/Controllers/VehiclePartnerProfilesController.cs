using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Security;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Persistence.Context;
using SmartCar.WebApi.Services;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclePartnerProfilesController : ControllerBase
    {
        private static readonly string[] ReviewStatuses = { "Đã xác minh", "Yêu cầu bổ sung", "Bị từ chối", "Chờ xác minh lại" };
        private readonly CarBookContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPrivateFileService _files;
        private readonly ISensitiveDataProtector _sensitiveData;
        public VehiclePartnerProfilesController(CarBookContext context, IConfiguration configuration, IPrivateFileService files, ISensitiveDataProtector? sensitiveData = null)
        {
            _context = context;
            _configuration = configuration;
            _files = files;
            _sensitiveData = sensitiveData ?? new SensitiveDataProtector(configuration);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized();
            var profile = await _context.VehiclePartnerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.AppUserID == userId);
            if (profile is null) return NotFound("Tài khoản chưa có hồ sơ đối tác. Vui lòng tạo hồ sơ xác minh.");
            return Ok(Map(profile));
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost("me/submit")]
        public async Task<IActionResult> SubmitMine(SubmitVehiclePartnerProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)));

            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized();
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.AppUserId == userId);
            if (user is null) return Unauthorized();
            if (!user.EmailConfirmed) return BadRequest("Bạn cần xác minh email bằng OTP trước khi gửi hồ sơ đối tác.");

            var profile = await _context.VehiclePartnerProfiles.FirstOrDefaultAsync(x => x.AppUserID == userId);
            if (profile is null)
            {
                profile = new VehiclePartnerProfile { AppUserID = userId, Email = user.Email, Phone = user.Phone ?? string.Empty, CreatedDate = DateTime.UtcNow };
                _context.VehiclePartnerProfiles.Add(profile);
            }

            if (profile.Status == "Chờ duyệt") return BadRequest("Hồ sơ đang chờ nhân viên duyệt, bạn không thể sửa trực tiếp.");
            if (profile.Status == "Đã xác minh") return BadRequest("Hồ sơ đã xác minh. Nếu cần thay đổi thông tin pháp lý, vui lòng gửi yêu cầu xác minh lại.");

            dto.PartnerType = NormalizePartnerType(dto.PartnerType);
            var submissionErrors = ValidateSubmission(dto);
            if (submissionErrors.Count > 0)
                return BadRequest(string.Join(" ", submissionErrors));

            try
            {
                if (dto.PartnerType == "Cá nhân")
                {
                    var permanent = await ResolveAdministrativeAddressAsync(
                        dto.PermanentProvinceCode, dto.PermanentWardCode, "thường trú");
                    dto.PermanentProvinceCode = permanent.ProvinceCode;
                    dto.PermanentWardCode = permanent.WardCode;
                    dto.PermanentProvince = permanent.ProvinceName;
                    dto.PermanentWard = permanent.WardFullName;

                    if (dto.CurrentAddressSameAsPermanent)
                    {
                        dto.CurrentProvinceCode = permanent.ProvinceCode;
                        dto.CurrentWardCode = permanent.WardCode;
                        dto.CurrentProvince = permanent.ProvinceName;
                        dto.CurrentWard = permanent.WardFullName;
                        dto.CurrentDetail = dto.PermanentDetail;
                    }
                    else
                    {
                        var current = await ResolveAdministrativeAddressAsync(
                            dto.CurrentProvinceCode, dto.CurrentWardCode, "hiện tại");
                        dto.CurrentProvinceCode = current.ProvinceCode;
                        dto.CurrentWardCode = current.WardCode;
                        dto.CurrentProvince = current.ProvinceName;
                        dto.CurrentWard = current.WardFullName;
                    }
                }
                else
                {
                    var headquarters = await ResolveAdministrativeAddressAsync(
                        dto.HeadquartersProvinceCode, dto.HeadquartersWardCode, "trụ sở");
                    dto.HeadquartersProvinceCode = headquarters.ProvinceCode;
                    dto.HeadquartersWardCode = headquarters.WardCode;
                    dto.HeadquartersProvince = headquarters.ProvinceName;
                    dto.HeadquartersWard = headquarters.WardFullName;
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            var oldFileIds = ExtractProfileFileIds(profile).ToArray();
            var submittedFiles = new List<PrivateFile>();
            try
            {
                if (dto.PartnerType == "Cá nhân")
                {
                    submittedFiles.AddRange(await _files.ValidateForAttachmentAsync(
                        new[] { dto.CitizenFrontFileId }, userId, "PartnerCitizenFront", null, false, HttpContext.RequestAborted));
                    submittedFiles.AddRange(await _files.ValidateForAttachmentAsync(
                        new[] { dto.CitizenBackFileId }, userId, "PartnerCitizenBack", null, false, HttpContext.RequestAborted));
                    submittedFiles.AddRange(await _files.ValidateForAttachmentAsync(
                        new[] { dto.PortraitFileId }, userId, "PartnerPortrait", null, false, HttpContext.RequestAborted));
                }
                else
                {
                    submittedFiles.AddRange(await _files.ValidateForAttachmentAsync(
                        new[] { dto.BusinessLicenseFileId }, userId, "PartnerBusinessLicense", null, false, HttpContext.RequestAborted));
                    if (dto.AuthorizationDocumentFileId.HasValue)
                        submittedFiles.AddRange(await _files.ValidateForAttachmentAsync(
                            new[] { dto.AuthorizationDocumentFileId.Value }, userId, "PartnerAuthorization", null, false, HttpContext.RequestAborted));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            if (dto.PartnerType == "Cá nhân")
            {
                var cccd = NormalizeDigits(dto.CitizenIdentityNumber);
                // SmartCar tách tài khoản khách và tài khoản đối tác. Cùng một người được phép
                // dùng lại CCCD trên các tài khoản khác nhau; nhân viên vẫn đối chiếu hồ sơ và audit log.

                dto.PermanentAddress = SubmitVehiclePartnerProfileDto.BuildAddress(dto.PermanentProvince, dto.PermanentWard, dto.PermanentDetail);
                dto.CurrentAddress = SubmitVehiclePartnerProfileDto.BuildAddress(dto.CurrentProvince, dto.CurrentWard, dto.CurrentDetail);

                profile.FullName = (dto.FullName ?? string.Empty).Trim();
                profile.DateOfBirth = dto.DateOfBirth;
                profile.Gender = (dto.Gender ?? string.Empty).Trim();
                profile.CitizenIdentityNumberEncrypted = _sensitiveData.Protect(cccd, "partner-citizen-id");
                profile.CitizenIdentityNumber = string.Empty;
                profile.CitizenIdFingerprint = IdentityFingerprintSecurity.Compute(IdentityKey(), cccd);
                // Một CCCD chỉ được gắn với một hồ sơ đối tác cá nhân. Cùng CCCD
                // vẫn được phép dùng cho một tài khoản khách riêng.
                if (await _context.VehiclePartnerProfiles.AsNoTracking().AnyAsync(x =>
                        x.AppUserID != userId && x.CitizenIdFingerprint == profile.CitizenIdFingerprint))
                    return Conflict("CCCD đã được sử dụng cho một tài khoản đối tác khác.");
                profile.CitizenIssuedDate = dto.CitizenIssuedDate;
                profile.CitizenExpiryDate = dto.CitizenExpiryDate;
                profile.PermanentProvinceCode = dto.PermanentProvinceCode;
                profile.PermanentWardCode = dto.PermanentWardCode;
                profile.PermanentProvince = (dto.PermanentProvince ?? string.Empty).Trim();
                profile.PermanentWard = (dto.PermanentWard ?? string.Empty).Trim();
                profile.PermanentDetail = (dto.PermanentDetail ?? string.Empty).Trim();
                profile.PermanentPaperAddress = dto.PermanentPaperAddress?.Trim() ?? string.Empty;
                profile.PermanentAddress = dto.PermanentAddress;
                profile.CurrentAddressSameAsPermanent = dto.CurrentAddressSameAsPermanent;
                profile.CurrentProvinceCode = dto.CurrentProvinceCode;
                profile.CurrentWardCode = dto.CurrentWardCode;
                profile.CurrentProvince = (dto.CurrentProvince ?? string.Empty).Trim();
                profile.CurrentWard = (dto.CurrentWard ?? string.Empty).Trim();
                profile.CurrentDetail = (dto.CurrentDetail ?? string.Empty).Trim();
                profile.CurrentAddress = dto.CurrentAddress;
                profile.Address = profile.CurrentAddress;
                profile.CitizenFrontImageUrl = _files.BuildViewUrl(dto.CitizenFrontFileId);
                profile.CitizenBackImageUrl = _files.BuildViewUrl(dto.CitizenBackFileId);
                profile.PortraitImageUrl = _files.BuildViewUrl(dto.PortraitFileId);
                profile.BusinessName = string.Empty;
                profile.TaxCode = string.Empty;
                profile.BusinessRegistrationNumber = string.Empty;
                profile.HeadquartersProvinceCode = null;
                profile.HeadquartersWardCode = null;
                profile.HeadquartersProvince = string.Empty;
                profile.HeadquartersWard = string.Empty;
                profile.HeadquartersDetail = string.Empty;
                profile.HeadquartersPaperAddress = string.Empty;
                profile.HeadquartersAddress = string.Empty;
                profile.LegalRepresentativeName = string.Empty;
                profile.AccountManagerName = string.Empty;
                profile.AccountManagerTitle = string.Empty;
                profile.RepresentativeName = string.Empty;
                profile.RepresentativeTitle = string.Empty;
                profile.BusinessLicenseImageUrl = string.Empty;
                profile.AuthorizationDocumentUrl = string.Empty;
            }
            else
            {
                var taxCode = (dto.TaxCode ?? string.Empty).Trim();
                if (await _context.VehiclePartnerProfiles.AnyAsync(x => x.AppUserID != userId && x.TaxCode == taxCode && x.TaxCode != "" && x.Status == "Đã xác minh"))
                    return Conflict("Mã số thuế này đã thuộc hồ sơ đối tác khác đã được xác minh.");

                dto.HeadquartersAddress = SubmitVehiclePartnerProfileDto.BuildAddress(dto.HeadquartersProvince, dto.HeadquartersWard, dto.HeadquartersDetail);

                profile.BusinessName = (dto.BusinessName ?? string.Empty).Trim();
                profile.TaxCode = taxCode;
                profile.BusinessRegistrationNumber = (dto.BusinessRegistrationNumber ?? string.Empty).Trim();
                profile.HeadquartersProvinceCode = dto.HeadquartersProvinceCode;
                profile.HeadquartersWardCode = dto.HeadquartersWardCode;
                profile.HeadquartersProvince = (dto.HeadquartersProvince ?? string.Empty).Trim();
                profile.HeadquartersWard = (dto.HeadquartersWard ?? string.Empty).Trim();
                profile.HeadquartersDetail = (dto.HeadquartersDetail ?? string.Empty).Trim();
                profile.HeadquartersPaperAddress = dto.HeadquartersPaperAddress?.Trim() ?? string.Empty;
                profile.HeadquartersAddress = dto.HeadquartersAddress;
                profile.Address = profile.HeadquartersAddress;
                profile.LegalRepresentativeName = (dto.LegalRepresentativeName ?? string.Empty).Trim();
                profile.AccountManagerName = (dto.AccountManagerName ?? string.Empty).Trim();
                profile.AccountManagerTitle = (dto.AccountManagerTitle ?? string.Empty).Trim();
                profile.RepresentativeName = profile.AccountManagerName;
                profile.RepresentativeTitle = profile.AccountManagerTitle;
                profile.BusinessLicenseImageUrl = _files.BuildViewUrl(dto.BusinessLicenseFileId);
                profile.AuthorizationDocumentUrl = dto.AuthorizationDocumentFileId.HasValue ? _files.BuildViewUrl(dto.AuthorizationDocumentFileId.Value) : string.Empty;
                profile.FullName = profile.AccountManagerName;
                profile.DateOfBirth = null;
                profile.Gender = string.Empty;
                profile.CitizenIdentityNumber = string.Empty;
                profile.CitizenIdentityNumberEncrypted = null;
                profile.CitizenIdFingerprint = null;
                profile.CitizenIssuedDate = null;
                profile.CitizenExpiryDate = null;
                profile.PermanentProvinceCode = null;
                profile.PermanentWardCode = null;
                profile.PermanentProvince = string.Empty;
                profile.PermanentWard = string.Empty;
                profile.PermanentDetail = string.Empty;
                profile.PermanentPaperAddress = string.Empty;
                profile.PermanentAddress = string.Empty;
                profile.CurrentAddressSameAsPermanent = false;
                profile.CurrentProvinceCode = null;
                profile.CurrentWardCode = null;
                profile.CurrentProvince = string.Empty;
                profile.CurrentWard = string.Empty;
                profile.CurrentDetail = string.Empty;
                profile.CurrentAddress = string.Empty;
                profile.CitizenFrontImageUrl = string.Empty;
                profile.CitizenBackImageUrl = string.Empty;
                profile.PortraitImageUrl = string.Empty;
            }

            profile.PartnerType = dto.PartnerType;
            profile.Email = user.Email;
            profile.Phone = user.Phone ?? profile.Phone;
            profile.BankName = (dto.BankName ?? string.Empty).Trim();
            profile.BankAccountNumberEncrypted = _sensitiveData.Protect(NormalizeBankAccount(dto.BankAccountNumber), "partner-bank-account");
            profile.BankAccountNumber = string.Empty;
            profile.BankAccountHolder = (dto.BankAccountHolder ?? string.Empty).Trim();
            profile.BankBranch = dto.BankBranch?.Trim() ?? string.Empty;
            profile.Status = "Chờ duyệt";
            profile.SubmittedDate = DateTime.UtcNow;
            profile.ReviewedDate = null;
            profile.ReviewedByAppUserID = null;
            profile.ReviewNote = null;

            await using var transaction = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            _files.MarkAttached(submittedFiles, nameof(VehiclePartnerProfile), $"user:{userId}");
            var newFileIds = submittedFiles.Select(x => x.PrivateFileID).ToHashSet();
            var replacedIds = oldFileIds.Where(x => !newFileIds.Contains(x)).Distinct().ToArray();
            if (replacedIds.Length > 0)
            {
                var replacedFiles = await _context.PrivateFiles
                    .Where(x => replacedIds.Contains(x.PrivateFileID) && x.OwnerAppUserID == userId && !x.IsDeleted)
                    .ToListAsync(HttpContext.RequestAborted);
                MarkFilesForDeletion(replacedFiles);
            }

            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = userId,
                Action = "Gửi hồ sơ xác minh đối tác",
                EntityName = nameof(VehiclePartnerProfile),
                EntityID = $"user:{userId}",
                Note = $"Loại đối tác: {profile.PartnerType}. Đã thay thế {replacedIds.Length} tệp cũ.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync(HttpContext.RequestAborted);
            await transaction.CommitAsync(HttpContext.RequestAborted);
            return Ok("Đã gửi hồ sơ đối tác. Hồ sơ chuyển sang trạng thái Chờ duyệt. Sau khi được xác minh, bạn mới được gửi xe lên duyệt.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> GetAll(string? status)
        {
            var query = _context.VehiclePartnerProfiles.AsNoTracking().Include(x => x.AppUser).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            var values = await query.OrderByDescending(x => x.SubmittedDate ?? x.CreatedDate).ToListAsync();
            return Ok(values.Select(Map).ToList());
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> Review(int id, ReviewVehiclePartnerProfileDto dto)
        {
            var status = dto.Status?.Trim() ?? string.Empty;
            var reviewNote = dto.ReviewNote?.Trim();
            if (!ReviewStatuses.Contains(status)) return BadRequest("Trạng thái duyệt hồ sơ đối tác không hợp lệ.");
            if (status is "Yêu cầu bổ sung" or "Bị từ chối" && string.IsNullOrWhiteSpace(reviewNote))
                return BadRequest("Vui lòng nhập lý do khi yêu cầu bổ sung hoặc từ chối hồ sơ đối tác.");

            var profile = await _context.VehiclePartnerProfiles.Include(x => x.AppUser).FirstOrDefaultAsync(x => x.VehiclePartnerProfileID == id);
            if (profile is null) return NotFound("Không tìm thấy hồ sơ đối tác.");

            profile.Status = status;
            profile.ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote;
            profile.ReviewedDate = DateTime.UtcNow;
            profile.ReviewedByAppUserID = GetCurrentUserId();
            if (profile.AppUser is not null) profile.AppUser.IsVehiclePartner = true;

            var profileClaim = await _context.WorkItemClaims
                .Where(x => x.QueueType == "Hồ sơ đối tác" && x.EntityID == id && x.Status == "Đang xử lý")
                .OrderByDescending(x => x.WorkItemClaimID)
                .FirstOrDefaultAsync();
            if (profileClaim is not null) profileClaim.Status = "Đã hoàn tất";

            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = profile.ReviewedByAppUserID,
                Action = "Duyệt hồ sơ đối tác",
                EntityName = nameof(VehiclePartnerProfile),
                EntityID = profile.VehiclePartnerProfileID.ToString(),
                Note = $"Kết quả: {status}. {reviewNote}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            return Ok(status == "Đã xác minh"
                ? "Đã xác minh hồ sơ đối tác. Đối tác có thể gửi xe lên duyệt."
                : $"Đã cập nhật hồ sơ đối tác sang trạng thái {status}.");
        }

        private static IEnumerable<Guid> ExtractProfileFileIds(VehiclePartnerProfile profile)
        {
            foreach (var value in new[]
            {
                profile.CitizenFrontImageUrl, profile.CitizenBackImageUrl, profile.PortraitImageUrl,
                profile.BusinessLicenseImageUrl, profile.AuthorizationDocumentUrl
            })
            {
                if (TryParseSecureFileId(value, out var id)) yield return id;
            }
        }

        private static bool TryParseSecureFileId(string? value, out Guid id)
        {
            id = Guid.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var segment = value.Trim().TrimEnd('/').Split('/').LastOrDefault();
            return Guid.TryParse(segment, out id);
        }

        private static void MarkFilesForDeletion(IEnumerable<PrivateFile> files)
        {
            var now = DateTime.UtcNow;
            foreach (var file in files)
            {
                file.IsDeleted = true;
                file.DeleteRequestedDate = now;
                file.LastDeleteError = null;
                file.AttachedEntityType = null;
                file.AttachedEntityID = null;
                file.AttachedDate = null;
            }
        }

        private string IdentityKey()
            => _configuration["Security:IdentityHmacKey"]
               ?? throw new InvalidOperationException("Thiếu Security:IdentityHmacKey.");

        private int GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("nameid");
            return int.TryParse(raw, out var id) ? id : 0;
        }

        private static List<string> ValidateSubmission(SubmitVehiclePartnerProfileDto dto)
        {
            var errors = new List<string>();
            var today = DateTime.UtcNow.Date;

            if (dto.PartnerType == "Cá nhân")
            {
                if (string.IsNullOrWhiteSpace(dto.FullName))
                    errors.Add("Vui lòng nhập họ và tên theo CCCD.");

                if (dto.DateOfBirth is not DateTime dateOfBirth)
                    errors.Add("Vui lòng nhập ngày sinh.");
                else if (dateOfBirth.Date >= today)
                    errors.Add("Ngày sinh phải nhỏ hơn ngày hiện tại.");

                if (string.IsNullOrWhiteSpace(dto.Gender))
                    errors.Add("Vui lòng chọn giới tính.");

                if (NormalizeDigits(dto.CitizenIdentityNumber).Length != 12)
                    errors.Add("Số CCCD phải gồm đúng 12 chữ số.");

                DateTime? issuedDate = dto.CitizenIssuedDate is DateTime issued ? issued.Date : null;
                DateTime? expiryDate = dto.CitizenExpiryDate is DateTime expiry ? expiry.Date : null;
                if (!issuedDate.HasValue)
                    errors.Add("Vui lòng nhập ngày cấp CCCD.");
                else if (issuedDate.Value > today)
                    errors.Add("Ngày cấp CCCD không được lớn hơn ngày hiện tại.");

                if (!expiryDate.HasValue)
                    errors.Add("Vui lòng nhập ngày hết hạn CCCD.");
                else if (expiryDate.Value < today)
                    errors.Add("CCCD không được hết hạn tại thời điểm gửi hồ sơ.");

                if (issuedDate.HasValue && expiryDate.HasValue && expiryDate.Value <= issuedDate.Value)
                    errors.Add("Ngày hết hạn CCCD phải sau ngày cấp CCCD.");

                if (string.IsNullOrWhiteSpace(dto.PermanentDetail))
                    errors.Add("Vui lòng nhập địa chỉ chi tiết thường trú.");

                if (!dto.CurrentAddressSameAsPermanent && string.IsNullOrWhiteSpace(dto.CurrentDetail))
                    errors.Add("Vui lòng nhập địa chỉ chi tiết hiện tại.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.BusinessName))
                    errors.Add("Vui lòng nhập tên doanh nghiệp/tổ chức.");

                var taxCodeDigits = NormalizeDigits(dto.TaxCode);
                if (taxCodeDigits.Length is not (10 or 13))
                    errors.Add("Mã số thuế phải có 10 hoặc 13 chữ số.");

                if (string.IsNullOrWhiteSpace(dto.BusinessRegistrationNumber))
                    errors.Add("Vui lòng nhập số đăng ký kinh doanh hoặc mã định danh pháp lý tương đương.");

                if (string.IsNullOrWhiteSpace(dto.HeadquartersDetail))
                    errors.Add("Vui lòng nhập địa chỉ chi tiết trụ sở.");

                if (string.IsNullOrWhiteSpace(dto.LegalRepresentativeName))
                    errors.Add("Vui lòng nhập người đại diện pháp luật.");

                if (string.IsNullOrWhiteSpace(dto.AccountManagerName))
                    errors.Add("Vui lòng nhập người phụ trách tài khoản.");

                if (string.IsNullOrWhiteSpace(dto.AccountManagerTitle))
                    errors.Add("Vui lòng nhập chức vụ người phụ trách.");
            }

            if (string.IsNullOrWhiteSpace(dto.BankName))
                errors.Add("Vui lòng chọn ngân hàng nhận đối soát.");

            if (string.IsNullOrWhiteSpace(NormalizeBankAccount(dto.BankAccountNumber)))
                errors.Add("Vui lòng nhập số tài khoản nhận đối soát.");

            if (string.IsNullOrWhiteSpace(dto.BankAccountHolder))
                errors.Add("Vui lòng nhập tên chủ tài khoản nhận đối soát.");

            return errors;
        }

        private static string NormalizePartnerType(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.Equals(text, "Doanh nghiệp", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Doanh nghiệp/Tổ chức", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Tổ chức", StringComparison.OrdinalIgnoreCase))
                return "Doanh nghiệp/Tổ chức";
            return "Cá nhân";
        }

        private static string NormalizeDigits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
        private static string NormalizeBankAccount(string? value) => new((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

        private async Task<(string ProvinceCode, string ProvinceName, string WardCode, string WardFullName)> ResolveAdministrativeAddressAsync(
            string? provinceCode,
            string? wardCode,
            string label)
        {
            var normalizedProvinceCode = (provinceCode ?? string.Empty).Trim();
            var normalizedWardCode = (wardCode ?? string.Empty).Trim();
            if (normalizedProvinceCode.Length != 2 || normalizedWardCode.Length != 5 ||
                !normalizedProvinceCode.All(char.IsDigit) || !normalizedWardCode.All(char.IsDigit))
                throw new InvalidOperationException($"Tỉnh/thành phố hoặc xã/phường/đặc khu {label} không hợp lệ.");

            var address = await _context.AdministrativeWards
                .AsNoTracking()
                .Where(x => x.WardCode == normalizedWardCode &&
                            x.ProvinceCode == normalizedProvinceCode &&
                            x.IsActive && x.Province.IsActive)
                .Select(x => new
                {
                    x.ProvinceCode,
                    x.Province.ProvinceName,
                    x.WardCode,
                    WardFullName = x.WardType + " " + x.WardName
                })
                .SingleOrDefaultAsync(HttpContext.RequestAborted);

            if (address is null)
                throw new InvalidOperationException($"Xã/phường/đặc khu {label} không thuộc tỉnh/thành phố đã chọn hoặc không còn hiệu lực.");

            return (address.ProvinceCode, address.ProvinceName, address.WardCode, address.WardFullName);
        }

        private VehiclePartnerProfileDto Map(VehiclePartnerProfile x) => new()
        {
            VehiclePartnerProfileID = x.VehiclePartnerProfileID,
            AppUserID = x.AppUserID,
            PartnerType = x.PartnerType,
            FullName = x.FullName,
            Phone = x.Phone,
            Email = x.Email,
            Address = x.Address,
            CitizenIdentityNumber = _sensitiveData.UnprotectOrLegacy(x.CitizenIdentityNumberEncrypted, x.CitizenIdentityNumber, "partner-citizen-id") ?? string.Empty,
            DateOfBirth = x.DateOfBirth,
            Gender = x.Gender,
            CitizenIssuedDate = x.CitizenIssuedDate,
            CitizenExpiryDate = x.CitizenExpiryDate,
            PermanentProvinceCode = x.PermanentProvinceCode ?? string.Empty,
            PermanentWardCode = x.PermanentWardCode ?? string.Empty,
            PermanentProvince = x.PermanentProvince,
            PermanentWard = x.PermanentWard,
            PermanentDetail = x.PermanentDetail,
            PermanentPaperAddress = x.PermanentPaperAddress,
            PermanentAddress = x.PermanentAddress,
            CurrentAddressSameAsPermanent = x.CurrentAddressSameAsPermanent,
            CurrentProvinceCode = x.CurrentProvinceCode ?? string.Empty,
            CurrentWardCode = x.CurrentWardCode ?? string.Empty,
            CurrentProvince = x.CurrentProvince,
            CurrentWard = x.CurrentWard,
            CurrentDetail = x.CurrentDetail,
            CurrentAddress = x.CurrentAddress,
            CitizenFrontImageUrl = x.CitizenFrontImageUrl,
            CitizenBackImageUrl = x.CitizenBackImageUrl,
            PortraitImageUrl = x.PortraitImageUrl,
            BusinessName = x.BusinessName,
            TaxCode = x.TaxCode,
            BusinessRegistrationNumber = x.BusinessRegistrationNumber,
            HeadquartersProvinceCode = x.HeadquartersProvinceCode ?? string.Empty,
            HeadquartersWardCode = x.HeadquartersWardCode ?? string.Empty,
            HeadquartersProvince = x.HeadquartersProvince,
            HeadquartersWard = x.HeadquartersWard,
            HeadquartersDetail = x.HeadquartersDetail,
            HeadquartersPaperAddress = x.HeadquartersPaperAddress,
            HeadquartersAddress = x.HeadquartersAddress,
            LegalRepresentativeName = x.LegalRepresentativeName,
            AccountManagerName = x.AccountManagerName,
            AccountManagerTitle = x.AccountManagerTitle,
            RepresentativeName = x.RepresentativeName,
            RepresentativeTitle = x.RepresentativeTitle,
            BusinessLicenseImageUrl = x.BusinessLicenseImageUrl,
            AuthorizationDocumentUrl = x.AuthorizationDocumentUrl,
            BankName = x.BankName,
            BankAccountNumber = _sensitiveData.UnprotectOrLegacy(x.BankAccountNumberEncrypted, x.BankAccountNumber, "partner-bank-account") ?? string.Empty,
            BankAccountHolder = x.BankAccountHolder,
            BankBranch = x.BankBranch,
            Status = x.Status,
            ReviewNote = x.ReviewNote,
            CreatedDate = x.CreatedDate,
            SubmittedDate = x.SubmittedDate,
            ReviewedDate = x.ReviewedDate
        };
    }
}
