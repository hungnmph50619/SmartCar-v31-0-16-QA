using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Dto.MarketplaceDtos;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class CommissionTransactionsController : ControllerBase
    {
        private readonly CarBookContext _context;
        public CommissionTransactionsController(CarBookContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll(string? status)
        {
            var query = BaseQuery();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
            var values = await query.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return Ok(values.Select(Map).ToList());
        }

        [HttpPut("{id:int}/status")]
        public IActionResult UpdateStatus(int id, UpdateCommissionTransactionStatusDto dto)
            => Conflict(new
            {
                message = "CommissionTransaction là bản ghi chỉ đọc được đồng bộ từ Settlement. Hãy lập và chi trả tại API settlement.",
                commissionTransactionId = id
            });

        private IQueryable<CommissionTransaction> BaseQuery() => _context.CommissionTransactions.AsNoTracking()
            .Include(x => x.PartnerAppUser)
            .Include(x => x.PartnerVehicle).ThenInclude(x => x.Car).ThenInclude(x => x.Brand)
            .Include(x => x.Reservation);

        private static ResultCommissionTransactionDto Map(CommissionTransaction x) => new()
        {
            CommissionTransactionID = x.CommissionTransactionID,
            ReservationID = x.ReservationID,
            SettlementID = x.SettlementID,
            PartnerName = $"{x.PartnerAppUser.Surname} {x.PartnerAppUser.Name}".Trim(),
            Reference = $"{x.PartnerVehicle.Car.Brand?.Name} {x.PartnerVehicle.Car.Model} - Đơn #{x.ReservationID}",
            GrossAmount = x.GrossAmount,
            CommissionRate = x.CommissionRate,
            CommissionAmount = x.CommissionAmount,
            PartnerNetAmount = x.PartnerNetAmount,
            Status = x.Status,
            CreatedDate = x.CreatedDate,
            PaidDate = x.PaidDate,
            BankReference = x.BankReference,
            Note = x.Note
        };
    }
}
