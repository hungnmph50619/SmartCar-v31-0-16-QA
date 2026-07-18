using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/vehicle-pricing-plans")]
    public class VehiclePricingPlansController : ControllerBase
    {
        private readonly CarBookContext _context;
        public VehiclePricingPlansController(CarBookContext context) => _context = context;

        [AllowAnonymous]
        [HttpGet("vehicle/{partnerVehicleId:int}")]
        public async Task<IActionResult> GetActive(int partnerVehicleId)
            => Ok(await _context.VehiclePricingPlans.AsNoTracking()
                .Where(x => x.PartnerVehicleID == partnerVehicleId && x.IsActive)
                .OrderBy(x => x.ServiceType)
                .ToListAsync());

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost]
        public async Task<IActionResult> CreateVersion(UpsertVehiclePricingPlanRequest request)
        {
            request.Normalize();
            var validation = request.Validate();
            if (validation is not null) return BadRequest(validation);

            var userId = CurrentUserId();
            var vehicle = await _context.PartnerVehicles
                .Include(x => x.VehiclePartnerApplication)
                .FirstOrDefaultAsync(x => x.PartnerVehicleID == request.PartnerVehicleID && x.OwnerAppUserID == userId);
            if (vehicle is null) return NotFound("Không tìm thấy xe thuộc đối tác.");
            if (vehicle.ApprovalStatus != VehicleApprovalStatuses.Approved)
                return BadRequest("Xe phải được duyệt trước khi kích hoạt bảng giá.");
            if (!SupportsService(vehicle.VehiclePartnerApplication.RentalMode, request.ServiceType))
                return BadRequest("Hình thức thuê của bảng giá không thuộc phạm vi xe đã đăng.");

            var oldPlans = await _context.VehiclePricingPlans
                .Where(x => x.PartnerVehicleID == request.PartnerVehicleID && x.ServiceType == request.ServiceType && x.IsActive)
                .ToListAsync();
            foreach (var old in oldPlans)
            {
                old.IsActive = false;
                old.EffectiveToUtc = DateTime.UtcNow;
                old.UpdatedAt = DateTime.UtcNow;
            }

            var entity = request.ToEntity();
            entity.EffectiveFromUtc = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _context.VehiclePricingPlans.Add(entity);
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = userId,
                Action = "Tạo phiên bản bảng giá",
                EntityName = nameof(VehiclePricingPlan),
                EntityID = request.PartnerVehicleID.ToString(),
                NewValues = JsonSerializer.Serialize(request),
                Note = "Bảng giá cũ được giữ lịch sử; giá mới chỉ áp dụng cho đơn tạo sau thời điểm hiệu lực.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var userId = CurrentUserId();
            var entity = await _context.VehiclePricingPlans
                .Include(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.VehiclePricingPlanID == id && x.PartnerVehicle.OwnerAppUserID == userId);
            if (entity is null) return NotFound("Không tìm thấy bảng giá.");
            entity.IsActive = false;
            entity.EffectiveToUtc = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private static bool SupportsService(string? offered, string selected)
            => offered == "Tự lái hoặc có tài xế" || offered == selected;
    }

    public sealed class UpsertVehiclePricingPlanRequest
    {
        [Range(1, int.MaxValue)] public int PartnerVehicleID { get; set; }
        [Required, MaxLength(30)] public string ServiceType { get; set; } = ServiceTypes.SelfDrive;
        [Range(0, 1000000000)] public decimal? HourlyRate { get; set; }
        [Range(0, 1000000000)] public decimal? DailyRate { get; set; }
        [Range(0, 1000000000)] public decimal? TripRate { get; set; }
        [Range(0, 100000000)] public decimal? PerKilometerRate { get; set; }
        [Range(0, 720)] public int MinimumHours { get; set; }
        [Range(0, 365)] public int MinimumDays { get; set; }
        [Range(0, 1000000000)] public decimal ReservationDepositAmount { get; set; }
        [Range(0, 1000000000)] public decimal SecurityDepositAmount { get; set; }
        [Range(0, 100000)] public int KilometerLimitPerDay { get; set; }
        [Range(0, 100000000)] public decimal ExtraKilometerFee { get; set; }
        [Range(0, 100000000)] public decimal LateReturnFeePerHour { get; set; }
        [Range(0, 100000000)] public decimal DeliveryFee { get; set; }
        [Range(0, 100000000)] public decimal DriverFee { get; set; }
        [Range(0, 100000000)] public decimal WaitingFeePerHour { get; set; }
        [Range(0, 100000000)] public decimal OvertimeFeePerHour { get; set; }
        [Range(0, 100000000)] public decimal OutOfProvinceFee { get; set; }
        [Range(0, 100000000)] public decimal OvernightFee { get; set; }
        [Range(1, 5)] public decimal WeekendMultiplier { get; set; } = 1m;
        [Range(1, 5)] public decimal HolidayMultiplier { get; set; } = 1m;
        public bool TollIncluded { get; set; }
        public bool ParkingIncluded { get; set; }
        [Required, MaxLength(50)] public string CancellationPolicyVersion { get; set; } = "cancel-v31.0";
        [MaxLength(1000)] public string? FuelPolicy { get; set; }
        [MaxLength(1000)] public string? CleaningPolicy { get; set; }
        [MaxLength(2000)] public string? Notes { get; set; }

        public void Normalize()
        {
            ServiceType = (ServiceType ?? string.Empty).Trim();
            CancellationPolicyVersion = (CancellationPolicyVersion ?? string.Empty).Trim();
            FuelPolicy = FuelPolicy?.Trim();
            CleaningPolicy = CleaningPolicy?.Trim();
            Notes = Notes?.Trim();
        }

        public string? Validate()
        {
            if (ServiceType is not (ServiceTypes.SelfDrive or ServiceTypes.WithDriver)) return "Hình thức thuê không hợp lệ.";
            if (HourlyRate.GetValueOrDefault() <= 0 && DailyRate.GetValueOrDefault() <= 0 &&
                (ServiceType != ServiceTypes.WithDriver || (TripRate.GetValueOrDefault() <= 0 && PerKilometerRate.GetValueOrDefault() <= 0)))
                return "Phải thiết lập ít nhất một loại giá hợp lệ.";
            var minHours = ServiceType == ServiceTypes.SelfDrive ? 4 : 2;
            if (MinimumHours > 0 && MinimumHours < minHours) return $"Thời gian tối thiểu của {ServiceType.ToLowerInvariant()} không được dưới {minHours} giờ.";
            if (ServiceType == ServiceTypes.WithDriver && DriverFee < 0) return "Phí tài xế không hợp lệ.";
            return null;
        }

        public VehiclePricingPlan ToEntity() => new()
        {
            PartnerVehicleID = PartnerVehicleID,
            ServiceType = ServiceType,
            HourlyRate = HourlyRate,
            DailyRate = DailyRate,
            TripRate = TripRate,
            PerKilometerRate = PerKilometerRate,
            MinimumHours = MinimumHours,
            MinimumDays = MinimumDays,
            ReservationDepositAmount = ReservationDepositAmount,
            SecurityDepositAmount = SecurityDepositAmount,
            KilometerLimitPerDay = KilometerLimitPerDay,
            ExtraKilometerFee = ExtraKilometerFee,
            LateReturnFeePerHour = LateReturnFeePerHour,
            DeliveryFee = DeliveryFee,
            DriverFee = DriverFee,
            WaitingFeePerHour = WaitingFeePerHour,
            OvertimeFeePerHour = OvertimeFeePerHour,
            OutOfProvinceFee = OutOfProvinceFee,
            OvernightFee = OvernightFee,
            WeekendMultiplier = WeekendMultiplier,
            HolidayMultiplier = HolidayMultiplier,
            TollIncluded = TollIncluded,
            ParkingIncluded = ParkingIncluded,
            CancellationPolicyVersion = CancellationPolicyVersion,
            FuelPolicy = FuelPolicy,
            CleaningPolicy = CleaningPolicy,
            Notes = Notes,
            IsActive = true
        };
    }
}
