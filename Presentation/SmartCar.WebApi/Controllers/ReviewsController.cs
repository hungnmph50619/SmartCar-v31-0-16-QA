using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Application.Features.Mediator.Commands.ReviewCommands;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public ReviewsController(CarBookContext context) => _context = context;

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ReviewListByCarId(int id)
        {
            var now = DateTime.UtcNow;
            var values = await _context.Reviews.AsNoTracking()
                .Where(x => x.CarID == id && !x.IsHidden && (!x.VisibleFromDate.HasValue || x.VisibleFromDate <= now))
                .OrderByDescending(x => x.ReviewDate)
                .ToListAsync();
            return Ok(values);
        }

        // Endpoint tương thích giao diện cũ: khách đánh giá xe của đơn hoàn thành gần nhất.
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateReview(CreateReviewCommand command)
        {
            var userId = CurrentUserId();
            var reservation = await _context.Reservations
                .Include(x => x.PartnerVehicle)
                .Where(x => x.CustomerAppUserID == userId && x.CarID == command.CarID && x.Status == "Hoàn thành")
                .OrderByDescending(x => x.CompletedDate ?? x.CreatedDate)
                .FirstOrDefaultAsync();
            if (reservation is null) return BadRequest("Bạn chỉ được đánh giá sau khi hoàn thành đơn thuê.");
            return await CreateForReservation(reservation, "Vehicle", command.RaytingValue, command.Comment, null);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("reservation/{reservationId:int}/customer")]
        public async Task<IActionResult> CustomerReview(int reservationId, CreateTwoWayReviewRequest request)
        {
            var reservation = await _context.Reservations
                .Include(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.ReservationID == reservationId && x.CustomerAppUserID == CurrentUserId());
            if (reservation is null) return NotFound("Không tìm thấy đơn của khách.");
            if (request.TargetType is not ("Vehicle" or "Partner" or "Driver")) return BadRequest("TargetType không hợp lệ.");
            return await CreateForReservation(reservation, request.TargetType, request.Rating, request.Comment, request.TargetDriverProfileID);
        }

        [Authorize(Roles = "VehiclePartner")]
        [HttpPost("reservation/{reservationId:int}/partner")]
        public async Task<IActionResult> PartnerReview(int reservationId, CreateTwoWayReviewRequest request)
        {
            var reservation = await _context.Reservations
                .Include(x => x.PartnerVehicle)
                .FirstOrDefaultAsync(x => x.ReservationID == reservationId && x.PartnerVehicle.OwnerAppUserID == CurrentUserId());
            if (reservation is null) return NotFound("Không tìm thấy đơn thuộc đối tác.");
            return await CreateForReservation(reservation, "Customer", request.Rating, request.Comment, null);
        }

        [Authorize(Roles = "Customer,VehiclePartner")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateReview(int id, UpdateReviewRequest request)
        {
            var entity = await _context.Reviews.FirstOrDefaultAsync(x => x.ReviewID == id);
            if (entity is null) return NotFound("Không tìm thấy đánh giá.");
            if (entity.AppUserID != CurrentUserId()) return Forbid();
            if (DateTime.UtcNow > entity.ReviewDate.AddDays(14)) return Conflict("Đã hết thời hạn chỉnh sửa đánh giá.");
            if (request.Rating is < 1 or > 5 || string.IsNullOrWhiteSpace(request.Comment)) return BadRequest("Điểm phải từ 1 đến 5 và nội dung không được trống.");
            entity.RaytingValue = request.Rating;
            entity.Comment = request.Comment.Trim();
            await _context.SaveChangesAsync();
            return Ok("Cập nhật đánh giá thành công.");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("{id:int}/moderate")]
        public async Task<IActionResult> Moderate(int id, ModerateReviewRequest request)
        {
            var entity = await _context.Reviews.FirstOrDefaultAsync(x => x.ReviewID == id);
            if (entity is null) return NotFound("Không tìm thấy đánh giá.");
            if (request.Hide && string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Bắt buộc nhập lý do ẩn đánh giá.");
            entity.IsHidden = request.Hide;
            entity.HiddenReason = request.Hide ? request.Reason?.Trim() : null;
            entity.HiddenByAppUserID = request.Hide ? CurrentUserId() : null;
            entity.HiddenAt = request.Hide ? DateTime.UtcNow : null;
            _context.AuditLogs.Add(new AuditLog
            {
                AppUserID = CurrentUserId(), Action = request.Hide ? "Ẩn đánh giá" : "Hiện lại đánh giá",
                EntityName = nameof(Review), EntityID = id.ToString(), Note = request.Reason,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        private async Task<IActionResult> CreateForReservation(Reservation reservation, string targetType, int rating, string? comment, int? targetDriverId)
        {
            var userId = CurrentUserId();
            if (reservation.Status != "Hoàn thành") return Conflict("Chỉ được đánh giá sau khi đơn hoàn thành.");
            if (reservation.CompletedDate.HasValue && DateTime.UtcNow > reservation.CompletedDate.Value.AddDays(14))
                return Conflict("Đã hết thời hạn đánh giá 14 ngày.");
            if (rating is < 1 or > 5 || string.IsNullOrWhiteSpace(comment)) return BadRequest("Điểm phải từ 1 đến 5 và nội dung không được trống.");
            if (await _context.Reviews.IgnoreQueryFilters().AnyAsync(x => x.ReservationID == reservation.ReservationID && x.AppUserID == userId && x.TargetType == targetType && !x.IsDeleted))
                return Conflict("Bạn đã đánh giá đối tượng này trong đơn.");

            var reviewer = await _context.AppUsers.AsNoTracking().FirstAsync(x => x.AppUserId == userId);
            var isCustomer = reservation.CustomerAppUserID == userId;
            if (!isCustomer && reservation.PartnerVehicle.OwnerAppUserID != userId) return Forbid();
            if (targetType == "Driver")
            {
                var assignedDriver = await _context.BookingDriverAssignments.AsNoTracking()
                    .AnyAsync(x => x.ReservationID == reservation.ReservationID && x.DriverProfileID == targetDriverId);
                if (!targetDriverId.HasValue || !assignedDriver) return BadRequest("Tài xế không thuộc đơn này.");
            }

            var review = new Review
            {
                CustomerName = $"{reviewer.Surname} {reviewer.Name}".Trim(),
                CustomerImage = string.Empty,
                Comment = comment.Trim(),
                RaytingValue = rating,
                ReviewDate = DateTime.UtcNow,
                CarID = reservation.CarID,
                AppUserID = userId,
                ReservationID = reservation.ReservationID,
                ReviewerRole = isCustomer ? "Customer" : "Partner",
                TargetType = targetType,
                TargetAppUserID = targetType switch
                {
                    "Customer" => reservation.CustomerAppUserID,
                    "Partner" => reservation.PartnerVehicle.OwnerAppUserID,
                    _ => null
                },
                TargetDriverProfileID = targetType == "Driver" ? targetDriverId : null,
                VisibleFromDate = DateTime.UtcNow.AddDays(14)
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var counterpartExists = isCustomer
                ? await _context.Reviews.AnyAsync(x => x.ReservationID == reservation.ReservationID && x.ReviewerRole == "Partner")
                : await _context.Reviews.AnyAsync(x => x.ReservationID == reservation.ReservationID && x.ReviewerRole == "Customer");
            if (counterpartExists)
            {
                var pair = await _context.Reviews.Where(x => x.ReservationID == reservation.ReservationID && !x.IsDeleted).ToListAsync();
                foreach (var item in pair) item.VisibleFromDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok(review);
        }

        private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    }

    public sealed class CreateTwoWayReviewRequest
    {
        [Range(1, 5)] public int Rating { get; set; }
        [Required, MaxLength(2000)] public string Comment { get; set; } = string.Empty;
        [MaxLength(30)] public string TargetType { get; set; } = "Vehicle";
        public int? TargetDriverProfileID { get; set; }
    }
    public sealed class UpdateReviewRequest { [Range(1, 5)] public int Rating { get; set; } [Required, MaxLength(2000)] public string Comment { get; set; } = string.Empty; }
    public sealed class ModerateReviewRequest { public bool Hide { get; set; } [MaxLength(500)] public string? Reason { get; set; } }
}
