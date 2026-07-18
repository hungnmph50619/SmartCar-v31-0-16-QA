using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Dto.Security;
using SmartCar.Persistence.Context;
using System.Security.Cryptography;

namespace SmartCar.WebApi.Services
{
    public interface IPrivateFileService
    {
        Task<PrivateFile> SaveAsync(IFormFile file, int ownerId, string category, int? reservationId, int? partnerApplicationId, CancellationToken cancellationToken);
        string GetPhysicalPath(PrivateFile file);
        string BuildViewUrl(Guid fileId);
        Task<bool> CanReadAsync(PrivateFile file, int currentUserId, bool privileged, CancellationToken cancellationToken);
        Task<IReadOnlyList<PrivateFile>> ValidateForAttachmentAsync(
            IReadOnlyCollection<Guid>? fileIds,
            int currentUserId,
            string expectedCategory,
            int? reservationId,
            bool privileged,
            CancellationToken cancellationToken);
        void MarkAttached(IEnumerable<PrivateFile> files, string entityType, string entityId);
        Task<bool> DeleteUnattachedAsync(Guid fileId, int currentUserId, bool privileged, CancellationToken cancellationToken);
        Task<bool> DeletePhysicalIfPendingAsync(Guid fileId, CancellationToken cancellationToken);
    }

    public class PrivateFileService : IPrivateFileService
    {
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            // Category tổng quát được giữ để đọc dữ liệu legacy; upload mới dùng category chi tiết.
            "PartnerDocuments", "VehicleDocuments",
            "PartnerCitizenFront", "PartnerCitizenBack", "PartnerPortrait",
            "PartnerBusinessLicense", "PartnerAuthorization",
            "VehicleRegistration", "VehicleInspection", "VehicleInsurance", "VehicleDriverLicense",
            "HandoverEvidence", "IncidentEvidence", "DisputeEvidence",
            "CustomerCitizenIdFront", "CustomerCitizenIdBack",
            "CustomerDriverLicense", "CustomerPortrait",
            "TrafficFineEvidence", "AdditionalChargeEvidence"
        };
        private static readonly HashSet<string> ReservationCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "HandoverEvidence", "IncidentEvidence", "DisputeEvidence",
            "TrafficFineEvidence", "AdditionalChargeEvidence"
        };
        private static readonly HashSet<string> CustomerVerificationCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "CustomerCitizenIdFront", "CustomerCitizenIdBack",
            "CustomerDriverLicense", "CustomerPortrait"
        };
        private static readonly HashSet<string> PartnerCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "PartnerDocuments", "VehicleDocuments",
            "PartnerCitizenFront", "PartnerCitizenBack", "PartnerPortrait",
            "PartnerBusinessLicense", "PartnerAuthorization",
            "VehicleRegistration", "VehicleInspection", "VehicleInsurance", "VehicleDriverLicense"
        };

        private readonly CarBookContext _context;
        private readonly IWebHostEnvironment _environment;

        public PrivateFileService(CarBookContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<PrivateFile> SaveAsync(IFormFile file, int ownerId, string category, int? reservationId, int? partnerApplicationId, CancellationToken cancellationToken)
        {
            if (file is null || file.Length <= 0) throw new InvalidOperationException("Tệp tải lên bị trống.");
            if (!AllowedCategories.Contains(category)) throw new InvalidOperationException("Loại tài liệu không hợp lệ.");
            await ValidateOwnershipAsync(ownerId, category, reservationId, partnerApplicationId, cancellationToken);

            var profile = GetUploadProfile(category);
            var id = Guid.NewGuid();
            string? finalPath = null;
            string? temporaryPath = null;

            try
            {
                await using var validationStream = file.OpenReadStream();
                var inspected = await FileUploadSecurity.InspectAsync(
                    validationStream, file.FileName, file.ContentType, file.Length, profile, cancellationToken);

                var storedName = id.ToString("N") + inspected.Extension;
                var folder = Path.Combine(_environment.ContentRootPath, "PrivateUploads", category, ownerId.ToString());
                Directory.CreateDirectory(folder);
                finalPath = Path.Combine(folder, storedName);
                temporaryPath = finalPath + ".uploading";

                await using (var source = file.OpenReadStream())
                await using (var target = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await FileUploadSecurity.WriteSafeContentAsync(source, target, inspected, cancellationToken);
                    await target.FlushAsync(cancellationToken);
                }

                string hash;
                await using (var stored = new FileStream(temporaryPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                using (var sha = SHA256.Create())
                {
                    hash = Convert.ToHexString(await sha.ComputeHashAsync(stored, cancellationToken));
                }

                File.Move(temporaryPath!, finalPath!);
                temporaryPath = null;

                var entity = new PrivateFile
                {
                    PrivateFileID = id,
                    OwnerAppUserID = ownerId,
                    ReservationID = reservationId,
                    PartnerApplicationID = partnerApplicationId,
                    Category = category,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = storedName,
                    ContentType = inspected.ContentType,
                    FileSize = new FileInfo(finalPath!).Length,
                    Sha256Hash = hash,
                    CreatedDate = DateTime.UtcNow
                };
                _context.PrivateFiles.Add(entity);
                await _context.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch
            {
                TryDeleteLocalFile(temporaryPath);
                TryDeleteLocalFile(finalPath);
                throw;
            }
        }

        public string BuildViewUrl(Guid fileId) => $"/SecureFiles/View/{fileId:D}";

        public string GetPhysicalPath(PrivateFile file)
        {
            var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "PrivateUploads", file.Category, file.OwnerAppUserID.ToString()));
            var path = Path.GetFullPath(Path.Combine(root, Path.GetFileName(file.StoredFileName)));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Đường dẫn tệp không hợp lệ.");
            return path;
        }

        public async Task<bool> CanReadAsync(PrivateFile file, int currentUserId, bool privileged, CancellationToken cancellationToken)
        {
            if (file.IsDeleted) return false;
            if (privileged || file.OwnerAppUserID == currentUserId) return true;
            if (file.ReservationID.HasValue)
            {
                return await _context.Reservations.AsNoTracking().AnyAsync(r =>
                    r.ReservationID == file.ReservationID &&
                    (r.CustomerAppUserID == currentUserId ||
                     (r.PartnerVehicle != null && r.PartnerVehicle.OwnerAppUserID == currentUserId)), cancellationToken);
            }
            if (file.PartnerApplicationID.HasValue)
                return await _context.VehiclePartnerApplications.AsNoTracking().AnyAsync(x =>
                    x.VehiclePartnerApplicationID == file.PartnerApplicationID && x.AppUserID == currentUserId, cancellationToken);
            return false;
        }

        public async Task<IReadOnlyList<PrivateFile>> ValidateForAttachmentAsync(
            IReadOnlyCollection<Guid>? fileIds,
            int currentUserId,
            string expectedCategory,
            int? reservationId,
            bool privileged,
            CancellationToken cancellationToken)
        {
            if (fileIds is null || fileIds.Count == 0) return Array.Empty<PrivateFile>();
            var ids = fileIds.Where(x => x != Guid.Empty).Distinct().ToArray();
            if (ids.Length != fileIds.Count) throw new InvalidOperationException("Danh sách FileId chứa giá trị trống hoặc trùng lặp.");
            if (ids.Length > 10) throw new InvalidOperationException("Mỗi thao tác chỉ được gắn tối đa 10 tệp.");
            if (!AllowedCategories.Contains(expectedCategory)) throw new InvalidOperationException("Loại tài liệu không hợp lệ.");

            var files = await _context.PrivateFiles
                .Where(x => ids.Contains(x.PrivateFileID) && !x.IsDeleted)
                .ToListAsync(cancellationToken);
            if (files.Count != ids.Length) throw new InvalidOperationException("Có tệp không tồn tại hoặc đã bị xóa.");

            foreach (var file in files)
            {
                if (!string.Equals(file.Category, expectedCategory, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Tệp không đúng loại nghiệp vụ.");
                if (file.ReservationID != reservationId)
                    throw new InvalidOperationException("Tệp không thuộc đúng đơn thuê.");
                if (!privileged && file.OwnerAppUserID != currentUserId)
                    throw new UnauthorizedAccessException("Bạn không sở hữu một hoặc nhiều tệp đã chọn.");
                if (file.AttachedDate.HasValue)
                    throw new InvalidOperationException("Một hoặc nhiều tệp đã được gắn vào hồ sơ khác.");
            }
            return files;
        }

        public void MarkAttached(IEnumerable<PrivateFile> files, string entityType, string entityId)
        {
            var type = (entityType ?? string.Empty).Trim();
            var id = (entityId ?? string.Empty).Trim();
            if (type.Length == 0 || type.Length > 100 || id.Length == 0 || id.Length > 100)
                throw new InvalidOperationException("Thông tin liên kết tệp không hợp lệ.");
            var now = DateTime.UtcNow;
            foreach (var file in files)
            {
                if (file.IsDeleted) throw new InvalidOperationException("Tệp đã được yêu cầu xóa.");
                if (file.AttachedDate.HasValue) throw new InvalidOperationException("Tệp đã được sử dụng trước đó.");
                file.AttachedEntityType = type;
                file.AttachedEntityID = id;
                file.AttachedDate = now;
            }
        }

        public async Task<bool> DeleteUnattachedAsync(Guid fileId, int currentUserId, bool privileged, CancellationToken cancellationToken)
        {
            var file = await _context.PrivateFiles.FirstOrDefaultAsync(x => x.PrivateFileID == fileId, cancellationToken);
            if (file is null) return false;
            if (!privileged && file.OwnerAppUserID != currentUserId) throw new UnauthorizedAccessException("Bạn không có quyền xóa tệp này.");
            if (file.AttachedDate.HasValue) throw new InvalidOperationException("Tệp đã được gắn vào hồ sơ và không thể xóa trực tiếp.");

            if (!file.IsDeleted)
            {
                file.IsDeleted = true;
                file.DeleteRequestedDate = DateTime.UtcNow;
                file.LastDeleteError = null;
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (file.PhysicalDeletedDate.HasValue) return true;
            var physicalDeleted = await TryDeletePhysicalAsync(file, cancellationToken);
            if (!physicalDeleted)
                throw new IOException("Tệp đã được khóa khỏi hệ thống nhưng chưa xóa được khỏi ổ đĩa. Tác vụ nền sẽ tự thử lại.");
            return true;
        }

        public async Task<bool> DeletePhysicalIfPendingAsync(Guid fileId, CancellationToken cancellationToken)
        {
            var file = await _context.PrivateFiles.FirstOrDefaultAsync(x =>
                x.PrivateFileID == fileId && x.IsDeleted && x.PhysicalDeletedDate == null, cancellationToken);
            if (file is null) return false;
            return await TryDeletePhysicalAsync(file, cancellationToken);
        }

        private async Task<bool> TryDeletePhysicalAsync(PrivateFile file, CancellationToken cancellationToken)
        {
            try
            {
                var path = GetPhysicalPath(file);
                if (File.Exists(path)) File.Delete(path);
                file.PhysicalDeletedDate = DateTime.UtcNow;
                file.LastDeleteError = null;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                file.DeleteRetryCount++;
                file.LastDeleteError = Truncate(ex.Message, 1000);
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }

        private async Task ValidateOwnershipAsync(int ownerId, string category, int? reservationId, int? partnerApplicationId, CancellationToken cancellationToken)
        {
            var user = await _context.AppUsers.AsNoTracking().Include(x => x.AppRole)
                .FirstOrDefaultAsync(x => x.AppUserId == ownerId, cancellationToken)
                ?? throw new UnauthorizedAccessException("Không xác định được tài khoản tải tệp.");
            var privileged = string.Equals(user.AppRole.AppRoleName, "Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.AppRole.AppRoleName, "Staff", StringComparison.OrdinalIgnoreCase);

            if (ReservationCategories.Contains(category))
            {
                if (!reservationId.HasValue) throw new InvalidOperationException("Bằng chứng nghiệp vụ phải gắn với một đơn thuê.");
                var allowed = privileged || await _context.Reservations.AsNoTracking().AnyAsync(r =>
                    r.ReservationID == reservationId &&
                    (r.CustomerAppUserID == ownerId || r.PartnerVehicle.OwnerAppUserID == ownerId), cancellationToken);
                if (!allowed) throw new UnauthorizedAccessException("Bạn không có quyền tải tệp cho đơn thuê này.");
                if (partnerApplicationId.HasValue) throw new InvalidOperationException("Bằng chứng đơn thuê không được gắn với hồ sơ đối tác.");
                return;
            }

            if (CustomerVerificationCategories.Contains(category))
            {
                if (user.IsVehiclePartner || privileged)
                    throw new UnauthorizedAccessException("Loại tệp này chỉ dành cho hồ sơ khách thuê của chính tài khoản.");
                if (reservationId.HasValue || partnerApplicationId.HasValue)
                    throw new InvalidOperationException("Ảnh xác minh khách không được gắn với đơn thuê hoặc hồ sơ đối tác.");
                return;
            }

            if (PartnerCategories.Contains(category))
            {
                if (!user.IsVehiclePartner && !privileged)
                    throw new UnauthorizedAccessException("Loại tài liệu này chỉ dành cho tài khoản đối tác.");
                if (reservationId.HasValue) throw new InvalidOperationException("Tài liệu đối tác/xe không được gắn với đơn thuê.");
                if (partnerApplicationId.HasValue && !privileged && !await _context.VehiclePartnerApplications.AsNoTracking()
                    .AnyAsync(x => x.VehiclePartnerApplicationID == partnerApplicationId && x.AppUserID == ownerId, cancellationToken))
                    throw new UnauthorizedAccessException("Bạn không sở hữu hồ sơ đối tác này.");
                return;
            }

            throw new InvalidOperationException("Loại tài liệu không có quy tắc phân quyền.");
        }

        private static FileUploadProfile GetUploadProfile(string category)
        {
            if (CustomerVerificationCategories.Contains(category) ||
                category is "PartnerCitizenFront" or "PartnerCitizenBack" or "PartnerPortrait" or "VehicleDriverLicense")
                return FileUploadProfile.CustomerIdentityImage;
            if (PartnerCategories.Contains(category)) return FileUploadProfile.PartnerDocument;
            return FileUploadProfile.ReservationEvidence;
        }

        private static void TryDeleteLocalFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Không che lấp lỗi gốc. File .uploading còn sót sẽ được tác vụ dọn thư mục xử lý.
            }
        }

        private static string Truncate(string value, int maxLength)
            => value.Length <= maxLength ? value : value[..maxLength];
    }
}
