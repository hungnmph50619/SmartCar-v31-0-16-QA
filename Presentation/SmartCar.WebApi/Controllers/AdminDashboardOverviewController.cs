using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;
using SmartCar.Domain.BusinessRules;
using SmartCar.Dto.AdminDashboardDtos;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardOverviewController : ControllerBase
    {
        private static readonly string[] ReservationStatusOrder =
        {
            "Chờ chủ xe xác nhận", "Chờ thanh toán", "Chờ khách đặt cọc", "Chờ khách thanh toán giữ chỗ", "Chờ nhân viên xác nhận cọc", "Chờ nhân viên xác nhận thanh toán", "Đã thanh toán", "Đã xác nhận", "Đã đặt cọc",
            "Chờ giao xe", "Đang thuê", "Chờ trả xe", "Chờ đối soát", "Hoàn thành", "Đã hủy",
            "Đang tranh chấp", "Đang xử lý sự cố"
        };

        private readonly CarBookContext _context;
        public AdminDashboardOverviewController(CarBookContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<AdminDashboardSummaryDto>> Get(
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? locationId,
            [FromQuery] int? partnerId,
            [FromQuery] string? status)
        {
            var now = DateTime.UtcNow;
            var selectedYear = year is >= 2000 and <= 2100 ? year.Value : now.Year;
            var selectedMonth = month is >= 1 and <= 12 ? month : null;
            var selectedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            var yearStart = new DateTime(selectedYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var yearTransactions = await _context.CommissionTransactions.AsNoTracking()
                .Include(x => x.PartnerAppUser)
                .Include(x => x.Reservation).ThenInclude(x => x.Car).ThenInclude(x => x.Brand)
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .Where(x => x.CreatedDate >= yearStart && x.CreatedDate < yearEnd && x.Status != "Đã hủy")
                .ToListAsync();

            var yearReservations = await _context.Reservations.AsNoTracking()
                .Include(x => x.CustomerAppUser)
                .Include(x => x.PartnerVehicle).ThenInclude(x => x.OwnerAppUser)
                .Include(x => x.Car).ThenInclude(x => x.Brand)
                .Where(x => x.CreatedDate >= yearStart && x.CreatedDate < yearEnd)
                .ToListAsync();

            bool MatchReservation(Reservation r, bool applyMonth = true)
            {
                if (applyMonth && selectedMonth.HasValue && r.CreatedDate.Month != selectedMonth.Value) return false;
                if (locationId.HasValue && r.PickUpLocationID != locationId.Value) return false;
                if (partnerId.HasValue && r.PartnerVehicle.OwnerAppUserID != partnerId.Value) return false;
                if (!string.IsNullOrWhiteSpace(selectedStatus) && !string.Equals(r.Status, selectedStatus, StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }

            var filteredReservations = yearReservations.Where(x => MatchReservation(x)).ToList();
            var filteredTransactions = yearTransactions.Where(x => MatchReservation(x.Reservation)).ToList();
            var reservationIds = filteredReservations.Select(x => x.ReservationID).ToHashSet();

            var yearSettlements = await _context.Settlements.AsNoTracking()
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .Where(x => x.CreatedDate >= yearStart && x.CreatedDate < yearEnd)
                .ToListAsync();
            var filteredSettlements = yearSettlements.Where(x => MatchReservation(x.Reservation)).ToList();

            var yearPayments = await _context.Payments.AsNoTracking()
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .Where(x => x.CreatedDate >= yearStart && x.CreatedDate < yearEnd)
                .ToListAsync();
            var filteredPayments = yearPayments.Where(x => MatchReservation(x.Reservation)).ToList();

            var currentMonthPlatformRevenue = await _context.CommissionTransactions.AsNoTracking()
                .Where(x => x.CreatedDate >= currentMonthStart && x.CreatedDate < nextMonthStart && x.Status != "Đã hủy")
                .SumAsync(x => (decimal?)x.CommissionAmount) ?? 0m;
            var previousMonthPlatformRevenue = await _context.CommissionTransactions.AsNoTracking()
                .Where(x => x.CreatedDate >= previousMonthStart && x.CreatedDate < currentMonthStart && x.Status != "Đã hủy")
                .SumAsync(x => (decimal?)x.CommissionAmount) ?? 0m;

            var monthlyRevenue = Enumerable.Range(1, 12).Select(m =>
            {
                var transactions = yearTransactions.Where(x => x.CreatedDate.Month == m && MatchReservation(x.Reservation, false)).ToList();
                var settlements = yearSettlements.Where(x => x.CreatedDate.Month == m && MatchReservation(x.Reservation, false)).ToList();
                var gateway = settlements.Sum(x => Math.Max(0, x.PaymentGatewayFee));
                var refunds = settlements.Sum(x => Math.Max(0, x.RefundAmount));
                var compensation = settlements.Sum(x => Math.Max(0, x.CompensationAmount));
                var platform = transactions.Sum(x => x.CommissionAmount);
                return new MonthlyRevenueDto
                {
                    Month = m,
                    MonthName = $"Tháng {m}",
                    GrossRevenue = transactions.Sum(x => x.GrossAmount),
                    PlatformRevenue = platform,
                    PartnerNetRevenue = transactions.Sum(x => x.PartnerNetAmount),
                    PaymentGatewayFees = gateway,
                    RefundAmount = refunds,
                    CompensationAmount = compensation,
                    NetProfit = platform - gateway - refunds - compensation,
                    CompletedReservations = transactions.Count
                };
            }).ToList();

            var highestRevenueMonth = monthlyRevenue.OrderByDescending(x => x.GrossRevenue).ThenBy(x => x.Month).First();
            var highestProfitMonth = monthlyRevenue.OrderByDescending(x => x.NetProfit).ThenBy(x => x.Month).First();
            var topPartner = filteredTransactions.GroupBy(x => new { x.PartnerAppUserID, Name = DisplayName(x.PartnerAppUser) })
                .Select(x => new { x.Key.Name, NetRevenue = x.Sum(y => y.PartnerNetAmount) }).OrderByDescending(x => x.NetRevenue).FirstOrDefault();
            var topVehicle = filteredTransactions.GroupBy(x => new { x.Reservation.CarID, Name = VehicleName(x.Reservation.Car) })
                .Select(x => new { x.Key.Name, Count = x.Count() }).OrderByDescending(x => x.Count).ThenBy(x => x.Name).FirstOrDefault();

            var statusGroups = filteredReservations.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
            var statusCounts = ReservationStatusOrder.Select(s => new ReservationStatusCountDto
            {
                Status = s,
                Count = statusGroups.TryGetValue(s, out var count) ? count : 0
            }).Where(x => x.Count > 0 || x.Status is "Chờ chủ xe xác nhận" or "Đang thuê" or "Hoàn thành" or "Đã hủy").ToList();

            var pendingSettlementsQuery = _context.CommissionTransactions.AsNoTracking().Include(x => x.PartnerAppUser)
                .Include(x => x.Reservation).ThenInclude(x => x.PartnerVehicle)
                .Where(x => x.Status != "Đã thanh toán" && x.Status != "Đã hủy");
            var pendingSettlementEntities = (await pendingSettlementsQuery.OrderByDescending(x => x.CreatedDate).ToListAsync())
                .Where(x => MatchReservation(x.Reservation)).Take(8).ToList();

            var pendingVehicles = await _context.VehiclePartnerApplications.AsNoTracking()
                .Where(x => x.Status == "Chờ duyệt").OrderBy(x => x.CreatedDate).Take(8)
                .Select(x => new DashboardPendingVehicleDto
                {
                    VehiclePartnerApplicationID = x.VehiclePartnerApplicationID,
                    OwnerName = x.OwnerFullName,
                    VehicleName = (x.BrandName + " " + x.Model).Trim(),
                    LicensePlate = x.LicensePlate,
                    CreatedDate = x.CreatedDate
                }).ToListAsync();

            var activeDepositStatuses = new[] { "Đã xác nhận", "Đã thanh toán", "Đã đặt cọc", "Chờ giao xe", "Đang thuê", "Chờ trả xe", "Chờ đối soát", "Đang tranh chấp", "Đang xử lý sự cố" };
            var depositsHeld = filteredPayments.Where(x =>
                (x.PaymentType == PaymentTypes.LegacyDeposit || x.PaymentType == PaymentTypes.SecurityDeposit) &&
                x.Status == "Thành công" && activeDepositStatuses.Contains(x.Reservation.Status)).Sum(x => x.Amount);
            var refunds = filteredSettlements.Sum(x => Math.Max(0, x.RefundAmount));
            var compensation = filteredSettlements.Sum(x => Math.Max(0, x.CompensationAmount));
            var gatewayFees = filteredSettlements.Sum(x => Math.Max(0, x.PaymentGatewayFee));
            var ownerPayouts = filteredSettlements.Where(x => x.Status == "Đã thanh toán").Sum(x => Math.Max(0, x.OwnerPayout));
            var platformRevenue = filteredTransactions.Sum(x => x.CommissionAmount);
            var openDisputes = await _context.Disputes.AsNoTracking().CountAsync(x => reservationIds.Contains(x.ReservationID) && x.Status != "Đã giải quyết");
            var abnormal = await _context.FraudFlags.AsNoTracking().CountAsync(x => x.CreatedDate >= yearStart && x.CreatedDate < yearEnd && x.Status == "Mới" && (!x.ReservationID.HasValue || reservationIds.Contains(x.ReservationID.Value)));

            var locations = await _context.Locations.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new DashboardFilterOptionDto { ID = x.LocationID, Name = x.Name }).ToListAsync();
            var partners = await _context.VehiclePartnerProfiles.AsNoTracking().Where(x => x.Status == "Đã xác minh")
                .OrderBy(x => x.FullName).Select(x => new DashboardFilterOptionDto { ID = x.AppUserID, Name = x.FullName }).ToListAsync();

            var totalReservations = filteredReservations.Count;
            var cancelledReservations = filteredReservations.Count(x => x.Status == "Đã hủy");
            var model = new AdminDashboardSummaryDto
            {
                SelectedYear = selectedYear,
                SelectedMonth = selectedMonth,
                SelectedLocationID = locationId,
                SelectedPartnerID = partnerId,
                SelectedStatus = selectedStatus,
                Locations = locations,
                Partners = partners,
                StatusOptions = ReservationStatusOrder.ToList(),
                TotalActiveVehicles = await _context.PartnerVehicles.AsNoTracking().CountAsync(x => x.IsActive),
                TotalActivePartners = await _context.VehiclePartnerProfiles.AsNoTracking().CountAsync(x => x.Status == "Đã xác minh"),
                TotalReservationsInYear = totalReservations,
                CompletedReservationsInYear = filteredTransactions.Count,
                PendingOwnerConfirmations = filteredReservations.Count(x => x.Status == "Chờ chủ xe xác nhận"),
                PendingVehicleApplications = await _context.VehiclePartnerApplications.AsNoTracking().CountAsync(x => x.Status == "Chờ duyệt"),
                PlatformRevenueThisMonth = currentMonthPlatformRevenue,
                PlatformRevenuePreviousMonth = previousMonthPlatformRevenue,
                MonthOverMonthGrowthPercent = GrowthRate(currentMonthPlatformRevenue, previousMonthPlatformRevenue),
                GrossRevenueInYear = filteredTransactions.Sum(x => x.GrossAmount),
                PlatformRevenueInYear = platformRevenue,
                PartnerNetRevenueInYear = filteredTransactions.Sum(x => x.PartnerNetAmount),
                PendingSettlementAmount = pendingSettlementEntities.Sum(x => x.PartnerNetAmount),
                DepositsHeld = depositsHeld,
                CustomerRefunds = refunds,
                CompensationCost = compensation,
                PaymentGatewayFees = gatewayFees,
                OwnerPayouts = ownerPayouts,
                EstimatedNetProfit = platformRevenue - gatewayFees - refunds - compensation,
                OpenDisputes = openDisputes,
                AbnormalTransactions = abnormal,
                HighestRevenueMonth = highestRevenueMonth.GrossRevenue > 0 ? highestRevenueMonth.Month : 0,
                HighestRevenueMonthName = highestRevenueMonth.GrossRevenue > 0 ? highestRevenueMonth.MonthName : "Chưa có dữ liệu",
                HighestRevenueAmount = highestRevenueMonth.GrossRevenue,
                HighestProfitMonth = highestProfitMonth.NetProfit != 0 ? highestProfitMonth.Month : 0,
                HighestProfitMonthName = highestProfitMonth.NetProfit != 0 ? highestProfitMonth.MonthName : "Chưa có dữ liệu",
                HighestProfitAmount = highestProfitMonth.NetProfit,
                TopPartnerName = topPartner?.Name ?? "Chưa có dữ liệu",
                TopPartnerNetRevenue = topPartner?.NetRevenue ?? 0,
                TopVehicleName = topVehicle?.Name ?? "Chưa có dữ liệu",
                TopVehicleRentalCount = topVehicle?.Count ?? 0,
                CancellationRate = totalReservations == 0 ? 0 : Math.Round(cancelledReservations * 100m / totalReservations, 1),
                MonthlyRevenue = monthlyRevenue,
                ReservationStatuses = statusCounts,
                PendingVehicles = pendingVehicles,
                PendingSettlements = pendingSettlementEntities.Select(x => new DashboardPendingSettlementDto
                {
                    CommissionTransactionID = x.CommissionTransactionID,
                    PartnerName = DisplayName(x.PartnerAppUser),
                    GrossAmount = x.GrossAmount,
                    CommissionAmount = x.CommissionAmount,
                    PartnerNetAmount = x.PartnerNetAmount,
                    Status = x.Status,
                    CreatedDate = x.CreatedDate
                }).ToList(),
                RecentReservations = filteredReservations.OrderByDescending(x => x.CreatedDate).Take(8).Select(x => new DashboardRecentReservationDto
                {
                    ReservationID = x.ReservationID,
                    VehicleName = VehicleName(x.Car),
                    CustomerName = DisplayName(x.CustomerAppUser),
                    PartnerName = DisplayName(x.PartnerVehicle.OwnerAppUser),
                    TotalPrice = x.TotalPrice,
                    Status = x.Status,
                    CreatedDate = x.CreatedDate
                }).ToList()
            };
            return Ok(model);
        }

        private static decimal GrowthRate(decimal current, decimal previous) => previous == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - previous) * 100m / previous, 1);
        private static string DisplayName(AppUser? user)
        {
            if (user is null) return "Không xác định";
            var fullName = $"{user.Surname} {user.Name}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
        }
        private static string VehicleName(Car? car) => car is null ? "Không xác định" : $"{car.Brand?.Name} {car.Model}".Trim();
    }
}
