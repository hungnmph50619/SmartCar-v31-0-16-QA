namespace SmartCar.Dto.AdminDashboardDtos
{
    public class AdminDashboardSummaryDto
    {
        public int SelectedYear { get; set; }
        public int? SelectedMonth { get; set; }
        public int? SelectedLocationID { get; set; }
        public int? SelectedPartnerID { get; set; }
        public string? SelectedStatus { get; set; }
        public List<DashboardFilterOptionDto> Locations { get; set; } = new();
        public List<DashboardFilterOptionDto> Partners { get; set; } = new();
        public List<string> StatusOptions { get; set; } = new();

        public int TotalActiveVehicles { get; set; }
        public int TotalActivePartners { get; set; }
        public int TotalReservationsInYear { get; set; }
        public int CompletedReservationsInYear { get; set; }
        public int PendingOwnerConfirmations { get; set; }
        public int PendingVehicleApplications { get; set; }

        public decimal PlatformRevenueThisMonth { get; set; }
        public decimal PlatformRevenuePreviousMonth { get; set; }
        public decimal MonthOverMonthGrowthPercent { get; set; }
        public decimal GrossRevenueInYear { get; set; }
        public decimal PlatformRevenueInYear { get; set; }
        public decimal PartnerNetRevenueInYear { get; set; }
        public decimal PendingSettlementAmount { get; set; }
        public decimal DepositsHeld { get; set; }
        public decimal CustomerRefunds { get; set; }
        public decimal CompensationCost { get; set; }
        public decimal PaymentGatewayFees { get; set; }
        public decimal OwnerPayouts { get; set; }
        public decimal EstimatedNetProfit { get; set; }
        public int OpenDisputes { get; set; }
        public int AbnormalTransactions { get; set; }

        public int HighestRevenueMonth { get; set; }
        public string HighestRevenueMonthName { get; set; } = "Chưa có dữ liệu";
        public decimal HighestRevenueAmount { get; set; }
        public int HighestProfitMonth { get; set; }
        public string HighestProfitMonthName { get; set; } = "Chưa có dữ liệu";
        public decimal HighestProfitAmount { get; set; }

        public string TopPartnerName { get; set; } = "Chưa có dữ liệu";
        public decimal TopPartnerNetRevenue { get; set; }
        public string TopVehicleName { get; set; } = "Chưa có dữ liệu";
        public int TopVehicleRentalCount { get; set; }
        public decimal CancellationRate { get; set; }

        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
        public List<ReservationStatusCountDto> ReservationStatuses { get; set; } = new();
        public List<DashboardRecentReservationDto> RecentReservations { get; set; } = new();
        public List<DashboardPendingVehicleDto> PendingVehicles { get; set; } = new();
        public List<DashboardPendingSettlementDto> PendingSettlements { get; set; } = new();
    }

    public class DashboardFilterOptionDto
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MonthlyRevenueDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal GrossRevenue { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal PartnerNetRevenue { get; set; }
        public decimal PaymentGatewayFees { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CompensationAmount { get; set; }
        public decimal NetProfit { get; set; }
        public int CompletedReservations { get; set; }
    }

    public class ReservationStatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardRecentReservationDto
    {
        public int ReservationID { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class DashboardPendingVehicleDto
    {
        public int VehiclePartnerApplicationID { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string VehicleName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class DashboardPendingSettlementDto
    {
        public int CommissionTransactionID { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal PartnerNetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
