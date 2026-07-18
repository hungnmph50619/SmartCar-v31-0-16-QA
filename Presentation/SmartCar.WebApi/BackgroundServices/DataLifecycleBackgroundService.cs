using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.BusinessRules;
using SmartCar.Domain.Entities;
using SmartCar.Domain.Time;
using SmartCar.Persistence.Context;

namespace SmartCar.WebApi.BackgroundServices
{
    /// <summary>
    /// Đồng bộ các mốc thời gian nghiệp vụ trong đặc tả SmartCar v1.0.
    /// Tác vụ chỉ chuyển trạng thái theo điều kiện xác định, luôn ghi lịch sử và không xóa cứng dữ liệu nghiệp vụ.
    /// </summary>
    public class DataLifecycleBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataLifecycleBackgroundService> _logger;

        public DataLifecycleBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DataLifecycleBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<CarBookContext>();
                    var now = DateTime.UtcNow;
                    var affected = new Dictionary<string, int>();

                    var expiredRegistrations = await context.RegistrationAttempts
                        .Where(x => x.Status == "Pending" && x.ExpiresAt <= now)
                        .ToListAsync(stoppingToken);
                    foreach (var item in expiredRegistrations) item.Status = "Expired";
                    affected["đăng ký hết hạn"] = expiredRegistrations.Count;

                    var expiredOwnerRequests = await context.Reservations
                        .Where(x => x.Status == ReservationStatuses.OwnerPending &&
                                    (x.PartnerResponseExpiresAt ?? x.CreatedDate.AddMinutes(ReservationAvailabilityRules.OwnerResponseMinutes)) <= now)
                        .ToListAsync(stoppingToken);
                    foreach (var reservation in expiredOwnerRequests)
                        ChangeStatus(context, reservation, ReservationStatuses.Cancelled,
                            $"Tự động đóng do đối tác không phản hồi trong {ReservationAvailabilityRules.OwnerResponseMinutes} phút.");
                    affected["hết chờ đối tác"] = expiredOwnerRequests.Count;

                    var paymentStatuses = new[]
                    {
                        ReservationStatuses.PaymentPending,
                        "Chờ khách đặt cọc", "Chờ khách thanh toán giữ chỗ"
                    };
                    var expiredPayments = await context.Reservations
                        .Where(x => paymentStatuses.Contains(x.Status) &&
                                    (x.PaymentExpiresAt ?? x.HoldExpiresAt) != null &&
                                    (x.PaymentExpiresAt ?? x.HoldExpiresAt) <= now &&
                                    !context.Payments.Any(p => p.ReservationID == x.ReservationID && p.Status == "Chờ xác nhận"))
                        .ToListAsync(stoppingToken);
                    foreach (var reservation in expiredPayments)
                        ChangeStatus(context, reservation, ReservationStatuses.PaymentExpired,
                            "Tự động giải phóng lịch do quá hạn thanh toán 15 phút.");
                    affected["hết thanh toán"] = expiredPayments.Count;

                    var confirmedReservations = await context.Reservations
                        .Where(x => x.Status == ReservationStatuses.Confirmed)
                        .ToListAsync(stoppingToken);
                    var movedToHandover = 0;
                    foreach (var reservation in confirmedReservations)
                    {
                        var pickupUtc = VietnamTime.LocalToUtc(reservation.PickUpDate, reservation.PickUpTime);
                        if (pickupUtc <= now.AddHours(24))
                        {
                            ChangeStatus(context, reservation, ReservationStatuses.HandoverPending,
                                "Tự động chuyển Chờ giao xe trước giờ nhận 24 giờ.");
                            movedToHandover++;
                        }
                    }
                    affected["chờ giao"] = movedToHandover;

                    var inProgress = await context.Reservations
                        .Where(x => x.Status == ReservationStatuses.InProgress)
                        .ToListAsync(stoppingToken);
                    var movedToReturn = 0;
                    foreach (var reservation in inProgress)
                    {
                        var returnUtc = VietnamTime.LocalToUtc(reservation.DropOffDate, reservation.DropOffTime);
                        if (returnUtc <= now.AddHours(2))
                        {
                            ChangeStatus(context, reservation, ReservationStatuses.ReturnPending,
                                "Tự động chuyển Chờ trả xe trước giờ trả 2 giờ.");
                            movedToReturn++;
                        }
                    }
                    affected["chờ trả"] = movedToReturn;

                    var proposalExpired = await context.Reservations
                        .Where(x => x.Status == ReservationStatuses.SurchargeProposalPending &&
                                    x.SurchargeProposalExpiresAt != null && x.SurchargeProposalExpiresAt <= now)
                        .ToListAsync(stoppingToken);
                    var movedToSettlement = 0;
                    foreach (var reservation in proposalExpired)
                    {
                        var hasCharge = await context.AdditionalCharges.AnyAsync(x =>
                            x.ReservationID == reservation.ReservationID && x.Status != "Rejected", stoppingToken);
                        if (!hasCharge)
                        {
                            ChangeStatus(context, reservation, ReservationStatuses.SettlementPending,
                                "Hết 24 giờ đề xuất phụ phí và không có yêu cầu phát sinh.");
                            movedToSettlement++;
                        }
                    }
                    affected["tự chờ đối soát"] = movedToSettlement;

                    var responseExpiredReservations = await context.Reservations
                        .Where(x => x.Status == ReservationStatuses.SurchargeResponsePending &&
                                    x.SurchargeResponseExpiresAt != null && x.SurchargeResponseExpiresAt <= now)
                        .ToListAsync(stoppingToken);
                    var escalatedCharges = 0;
                    foreach (var reservation in responseExpiredReservations)
                    {
                        var unanswered = await context.AdditionalCharges
                            .Where(x => x.ReservationID == reservation.ReservationID && x.Status == AdditionalChargeStatuses.Submitted)
                            .ToListAsync(stoppingToken);
                        foreach (var charge in unanswered)
                        {
                            charge.Status = AdditionalChargeStatuses.StaffReview;
                            charge.ResolvedDate = null;
                            escalatedCharges++;
                        }
                        if (unanswered.Count > 0)
                            ChangeStatus(context, reservation, ReservationStatuses.Disputed,
                                "Khách không phản hồi phụ phí đúng hạn; chuyển nhân viên kiểm tra, không tự động coi là đồng ý.");
                    }
                    affected["phụ phí chuyển kiểm tra"] = escalatedCharges;

                    var settlementsDue = await context.Settlements
                        .Where(x => x.Status == SettlementStatuses.PartnerReview && x.PartnerReviewDueDate != null && x.PartnerReviewDueDate <= now &&
                                    string.IsNullOrEmpty(x.PartnerDisputeReason))
                        .ToListAsync(stoppingToken);
                    foreach (var settlement in settlementsDue) settlement.Status = SettlementStatuses.AwaitingApproval;
                    affected["đối soát tự chốt"] = settlementsDue.Count;

                    var expiredOtps = await context.HandoverReports
                        .Where(x => !x.IsLocked && x.OtpExpiresAt != null && x.OtpExpiresAt <= now)
                        .ToListAsync(stoppingToken);
                    foreach (var report in expiredOtps)
                    {
                        report.OtpHash = null;
                        report.CustomerOtpHash = null;
                        report.PartnerOtpHash = null;
                        report.OtpExpiresAt = null;
                    }
                    affected["OTP biên bản hết hạn"] = expiredOtps.Count;

                    var expiredTokens = await context.PasswordResetTokens
                        .Where(x => x.ExpiresDate < now || x.UsedDate != null)
                        .ToListAsync(stoppingToken);
                    if (expiredTokens.Count > 0) context.PasswordResetTokens.RemoveRange(expiredTokens);
                    affected["token dọn"] = expiredTokens.Count;

                    var expiredVehicleIds = await context.VehicleDocuments
                        .Where(x => x.ExpiryDate != null && x.ExpiryDate < now && x.Status == "Đã xác minh")
                        .Select(x => x.PartnerVehicleID).Distinct().ToListAsync(stoppingToken);
                    var vehiclesToPause = await context.PartnerVehicles
                        .Where(x => expiredVehicleIds.Contains(x.PartnerVehicleID) &&
                                    (x.IsActive || x.OperationStatus == VehicleOperationStatuses.Active))
                        .ToListAsync(stoppingToken);
                    foreach (var vehicle in vehiclesToPause)
                    {
                        vehicle.IsActive = false;
                        vehicle.OperationStatus = VehicleOperationStatuses.Inactive;
                        vehicle.InactiveReason = "DocumentExpired";
                        vehicle.PauseReason = "Tự động ngừng hoạt động vì đăng kiểm, bảo hiểm hoặc giấy tờ xe đã hết hạn.";
                        context.Notifications.Add(new Notification
                        {
                            AppUserID = vehicle.OwnerAppUserID,
                            Title = "Xe đã ngừng hoạt động",
                            Message = vehicle.PauseReason,
                            Type = "VehicleDocument"
                        });
                    }
                    affected["xe hết giấy tờ"] = vehiclesToPause.Count;

                    var overdueFines = await context.TrafficFines.Include(x => x.Reservation)
                        .Where(x => x.DueDate != null && x.DueDate < now && x.Status == "Chờ khách thanh toán")
                        .ToListAsync(stoppingToken);
                    foreach (var fine in overdueFines)
                    {
                        fine.Status = "Quá hạn";
                        context.Notifications.Add(new Notification
                        {
                            AppUserID = fine.Reservation.CustomerAppUserID,
                            Title = "Phạt nguội quá hạn",
                            Message = $"Khoản phạt {fine.Amount.ToString("#,#", CultureInfo.InvariantCulture)} đồng đã quá hạn.",
                            Type = "TrafficFine"
                        });
                    }
                    affected["phạt quá hạn"] = overdueFines.Count;

                    if (affected.Values.Sum() > 0)
                    {
                        context.AuditLogs.Add(new AuditLog
                        {
                            Action = "Tác vụ vòng đời nghiệp vụ v31",
                            EntityName = "System",
                            Note = string.Join("; ", affected.Select(x => $"{x.Key}: {x.Value}"))
                        });
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi tác vụ vòng đời dữ liệu SmartCar");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private static void ChangeStatus(CarBookContext context, Reservation reservation, string newStatus, string note)
        {
            if (reservation.Status == newStatus) return;
            var oldStatus = reservation.Status;
            reservation.Status = newStatus;
            reservation.StateVersion++;
            context.ReservationStatusHistories.Add(new ReservationStatusHistory
            {
                ReservationID = reservation.ReservationID,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Note = note,
                ChangedDate = DateTime.UtcNow
            });
        }
    }
}
