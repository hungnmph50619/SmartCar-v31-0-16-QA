using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/vehicle-availability-blocks")]
    [Authorize(Roles = "VehiclePartner")]
    public class VehicleAvailabilityBlocksController : ControllerBase
    {
        private readonly CarBookContext _context;
        public VehicleAvailabilityBlocksController(CarBookContext context) => _context = context;

        [HttpGet("vehicle/{partnerVehicleId:int}")]
        public async Task<IActionResult> List(int partnerVehicleId)
        {
            var userId = CurrentUserId();
            if (!await _context.PartnerVehicles.AnyAsync(x => x.PartnerVehicleID == partnerVehicleId && x.OwnerAppUserID == userId)) return Forbid();
            return Ok(await _context.VehicleAvailabilityBlocks.AsNoTracking()
                .Where(x => x.PartnerVehicleID == partnerVehicleId && x.IsActive)
                .OrderBy(x => x.StartUtc)
                .ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateVehicleBlockRequest request)
        {
            var userId = CurrentUserId();
            if (request.EndUtc <= request.StartUtc) return BadRequest("Thời gian kết thúc phải sau thời gian bắt đầu.");
            if (request.StartUtc < DateTime.UtcNow.AddMinutes(-1)) return BadRequest("Không thể khóa lịch trong quá khứ.");
            var vehicle = await _context.PartnerVehicles.FirstOrDefaultAsync(x => x.PartnerVehicleID == request.PartnerVehicleID && x.OwnerAppUserID == userId);
            if (vehicle is null) return NotFound("Không tìm thấy xe thuộc đối tác.");

            var reservations = await _context.Reservations.AsNoTracking()
                .Where(x => x.PartnerVehicleID == request.PartnerVehicleID)
                .Select(x => new { x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, x.PickUpDate, x.DropOffDate, x.PickUpTime, x.DropOffTime })
                .ToListAsync();
            if (reservations.Any(x =>
                ReservationAvailabilityRules.IsBlocking(x.Status, x.HoldExpiresAt, x.PartnerResponseExpiresAt, x.PaymentExpiresAt, DateTime.UtcNow) &&
                VietnamTime.LocalToUtc(x.PickUpDate, x.PickUpTime) < request.EndUtc &&
                VietnamTime.LocalToUtc(x.DropOffDate, x.DropOffTime) > request.StartUtc))
                return Conflict("Không được khóa lịch đè lên yêu cầu hoặc đơn đang hoạt động.");

            if (await _context.VehicleAvailabilityBlocks.AnyAsync(x => x.PartnerVehicleID == request.PartnerVehicleID && x.IsActive && x.StartUtc < request.EndUtc && x.EndUtc > request.StartUtc))
                return Conflict("Khoảng thời gian đã có lịch khóa khác.");

            var entity = new VehicleAvailabilityBlock
            {
                PartnerVehicleID = request.PartnerVehicleID,
                StartUtc = request.StartUtc,
                EndUtc = request.EndUtc,
                BlockType = NormalizeBlockType(request.BlockType),
                Reason = (request.Reason ?? string.Empty).Trim(),
                CreatedByAppUserID = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.VehicleAvailabilityBlocks.Add(entity);
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = userId,
                Action = "Khóa lịch xe",
                EntityName = nameof(PartnerVehicle),
                EntityID = request.PartnerVehicleID.ToString(),
                Note = $"{entity.BlockType}: {entity.StartUtc:O} - {entity.EndUtc:O}; {entity.Reason}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = CurrentUserId();
            var entity = await _context.VehicleAvailabilityBlocks
                .Include(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.VehicleAvailabilityBlockID == id && x.PartnerVehicle.OwnerAppUserID == userId);
            if (entity is null) return NotFound("Không tìm thấy lịch khóa.");
            entity.IsActive = false;
            entity.CancelledAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        private static string NormalizeBlockType(string? value) => (value ?? string.Empty).Trim() switch
        {
            "Maintenance" or "Bảo dưỡng" => "Maintenance",
            "Retired" or "Ngừng cho thuê" => "Retired",
            _ => "OwnerPaused"
        };
    }

    public sealed class CreateVehicleBlockRequest
    {
        [Range(1, int.MaxValue)] public int PartnerVehicleID { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        [MaxLength(30)] public string BlockType { get; set; } = "OwnerPaused";
        [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
    }
}
