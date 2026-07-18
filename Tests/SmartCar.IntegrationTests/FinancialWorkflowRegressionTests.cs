using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCar.Domain.Entities;
using SmartCar.Domain.BusinessRules;
using SmartCar.WebApi.Controllers;
using SmartCar.WebApi.Services;

namespace SmartCar.IntegrationTests;

public class FinancialWorkflowRegressionTests
{
    [Fact]
    public async Task CreateSettlement_DepositOnly_IsRejected()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(BuildReservation(1, "Chờ đối soát"));
        db.HandoverReports.Add(BuildConfirmedReturn(1));
        db.Payments.Add(new Payment { ReservationID=1, PaymentType="Tiền cọc", Amount=300000, Status="Thành công" });
        await db.SaveChangesAsync();
        var controller = Comprehensive(db, 2, "Staff");

        var result = await controller.CreateSettlement(1);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("Chưa thu đủ tiền thuê", conflict.Value?.ToString());
        Assert.Empty(db.Settlements);
    }

    [Fact]
    public async Task CreateSettlement_AcceptedChargeWithoutSuccessfulPayment_IsRejected()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(BuildReservation(2, "Chờ đối soát"));
        db.HandoverReports.Add(BuildConfirmedReturn(2));
        db.Payments.Add(new Payment { ReservationID=2, PaymentType="Tiền thuê", Amount=1000000, Status="Thành công" });
        db.AdditionalCharges.Add(new AdditionalCharge
{
    AdditionalChargeID = 20,
    ReservationID = 2,
    Amount = 100000,
    ChargeType = "Vệ sinh",
    Reason = "Test",
    Status = AdditionalChargeStatuses.CustomerAccepted,
    CreatedByAppUserID = 2
});
        await db.SaveChangesAsync();

        var result = await Comprehensive(db, 2, "Staff").CreateSettlement(2);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(db.Settlements);
    }

    [Fact]
    public async Task CreateSettlement_UsesOnlySuccessfulCollectedRentalAndCharge()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(BuildReservation(3, "Chờ đối soát"));
        db.HandoverReports.Add(BuildConfirmedReturn(3));
        var charge = new AdditionalCharge
{
    AdditionalChargeID = 30,
    ReservationID = 3,
    Amount = 100000,
    ChargeType = "Vệ sinh",
    Reason = "Test",
    Status = AdditionalChargeStatuses.CustomerAccepted,
    CreatedByAppUserID = 2
};
        var payment = new Payment { PaymentID=31, ReservationID=3, PaymentType="Phụ phí", RelatedEntityType=nameof(AdditionalCharge), RelatedEntityID=30, Amount=100000, Status="Thành công" };
        charge.PaymentID=31;
        db.Payments.AddRange(new Payment { ReservationID=3, PaymentType="Tiền thuê", Amount=1000000, Status="Thành công" }, payment);
        db.AdditionalCharges.Add(charge);
        await db.SaveChangesAsync();

        var result = await Comprehensive(db, 2, "Staff").CreateSettlement(3);

        Assert.IsType<OkObjectResult>(result);
        var settlement = Assert.Single(db.Settlements);
        Assert.Equal(1100000m, settlement.GrossRental);
        Assert.Equal(900000m, settlement.OwnerPayout);
    }

    [Fact]
    public async Task CreateSettlement_WithoutConfirmedReturnHandover_IsRejected()
    {
        await using var db = TestDatabase.Create();
        db.Reservations.Add(BuildReservation(4, "Chờ đối soát"));
        db.Payments.Add(new Payment { ReservationID=4, PaymentType="Tiền thuê", Amount=1000000, Status="Thành công" });
        await db.SaveChangesAsync();

        var result = await Comprehensive(db, 2, "Staff").CreateSettlement(4);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("biên bản trả xe", conflict.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.Settlements);
    }

    [Fact]
    public async Task ReservationsGenericStatusEndpoint_CannotBypassHandoverOrSettlement()
    {
        await using var db = TestDatabase.Create();
        var controller = new ReservationsController(null!, null!, db, new FakeCancellation(), new SystemSettingService(db));
        SetUser(controller, 1, "Admin");

        var result = await controller.UpdateStatus(99, new SmartCar.Dto.ReservationDtos.UpdateReservationStatusDto
        {
            Status = "Hoàn thành",
            Note = "bypass"
        });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GenericTransition_CannotBypassSpecializedWorkflow()
    {
        await using var db = TestDatabase.Create();
        var controller = new DataGovernanceController(db, new FakeCancellation(), new FakeAnonymization());
        SetUser(controller, 1, "Admin");

        var result = await controller.TransitionReservation(1, new TransitionRequest("Hoàn thành", null));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void CommissionTransaction_StatusEndpoint_IsReadOnly()
    {
        using var db = TestDatabase.Create();
        var controller = new CommissionTransactionsController(db);
        var result = controller.UpdateStatus(1, new SmartCar.Dto.MarketplaceDtos.UpdateCommissionTransactionStatusDto());
        Assert.IsType<ConflictObjectResult>(result);
    }

    private static ComprehensiveOperationsController Comprehensive(SmartCar.Persistence.Context.CarBookContext db, int userId, string role)
    {
        var controller = new ComprehensiveOperationsController(db, new PrivateFileService(db, new FakeEnvironment()));
        SetUser(controller, userId, role);
        return controller;
    }

    private static Reservation BuildReservation(int id, string status) => new()
    {
        ReservationID=id, CustomerAppUserID=4, PartnerVehicleID=1, CarID=1,
        Name="A", Surname="B", Email="a@test.local", Phone="0900000000", Age=30, DriverLicenseYear=5,
        Status=status, PickUpDate=DateTime.Today.AddDays(-3), DropOffDate=DateTime.Today.AddDays(-1),
        PickUpTime=TimeSpan.FromHours(9), DropOffTime=TimeSpan.FromHours(9), TotalPrice=1000000,
        PlatformFeeAmount=200000, CommissionRateSnapshot=20, CreatedDate=DateTime.UtcNow.AddDays(-4)
    };

    private static HandoverReport BuildConfirmedReturn(int reservationId) => new()
    {
        ReservationID = reservationId,
        ReportType = "Trả xe",
        IsLocked = true,
        IsSuperseded = false,
        ConfirmedDate = DateTime.UtcNow.AddDays(-1),
        CreatedByAppUserID = 2,
        CreatedDate = DateTime.UtcNow.AddDays(-1)
    };

    private static void SetUser(ControllerBase controller, int id, string role)
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)
        }, "test"));
    }

    private sealed class FakeCancellation : IReservationCancellationService
    {
        public Task<CancellationPreviewResult> PreviewAsync(int reservationId, int actorUserId, bool privileged, CancellationToken cancellationToken = default)
            => Task.FromResult(new CancellationPreviewResult(false,409,"not used",0,0,0,0,"test"));
        public Task<CancellationPreviewResult> CancelAsync(int reservationId, int actorUserId, bool privileged, string reason, CancellationToken cancellationToken = default)
            => Task.FromResult(new CancellationPreviewResult(false,409,"not used",0,0,0,0,"test"));
    }
    private sealed class FakeAnonymization : IUserAnonymizationService
    {
        public Task<UserAnonymizationResult> AnonymizeAsync(int targetUserId, int actorUserId, string? reason, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; }="Tests"; public IFileProvider WebRootFileProvider { get; set; }=new NullFileProvider();
        public string WebRootPath { get; set; }=Path.GetTempPath(); public string EnvironmentName { get; set; }="Testing";
        public string ContentRootPath { get; set; }=Path.GetTempPath(); public IFileProvider ContentRootFileProvider { get; set; }=new NullFileProvider();
    }
}
