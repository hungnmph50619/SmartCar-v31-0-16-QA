using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AdminDashboardDtos;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCar.WebUI.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/AdminDashboard")]
    public class AdminDashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminDashboardController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? year, int? month, int? locationId, int? partnerId, string? status)
        {
            var selectedYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.UtcNow.Year;
            var model = await LoadDashboardAsync(selectedYear, month, locationId, partnerId, status);

            if (model is null)
            {
                model = NewDashboardModel(selectedYear, month, locationId, partnerId, status);
                ViewBag.DashboardError = "Không tải được dữ liệu tổng quan. Vui lòng kiểm tra Web API tại cổng 7060.";
            }

            ViewBag.AvailableYears = Enumerable.Range(DateTime.UtcNow.Year - 4, 5)
                .Append(selectedYear)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();
            return View(model);
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export(int? year, int? month, int? locationId, int? partnerId, string? status)
        {
            var selectedYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.UtcNow.Year;
            var model = await LoadDashboardAsync(selectedYear, month, locationId, partnerId, status);
            if (model is null)
            {
                TempData["ErrorMessage"] = "Không thể xuất báo cáo vì Web API chưa phản hồi.";
                return RedirectToAction(nameof(Index), new { year, month, locationId, partnerId, status });
            }

            static string Csv(object? value)
            {
                var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }

            var csv = new StringBuilder();
            csv.AppendLine("SMARTCAR - BÁO CÁO VẬN HÀNH");
            csv.AppendLine(string.Join(',', Csv("Năm"), Csv(model.SelectedYear), Csv("Tháng"), Csv(model.SelectedMonth?.ToString() ?? "Tất cả"), Csv("Trạng thái"), Csv(model.SelectedStatus ?? "Tất cả")));
            csv.AppendLine();
            csv.AppendLine(string.Join(',', Csv("CHỈ SỐ"), Csv("GIÁ TRỊ")));
            csv.AppendLine(string.Join(',', Csv("Tổng giá trị đơn"), Csv(model.GrossRevenueInYear)));
            csv.AppendLine(string.Join(',', Csv("Hoa hồng nền tảng"), Csv(model.PlatformRevenueInYear)));
            csv.AppendLine(string.Join(',', Csv("Tiền trả chủ xe"), Csv(model.OwnerPayouts)));
            csv.AppendLine(string.Join(',', Csv("Tiền cọc đang giữ"), Csv(model.DepositsHeld)));
            csv.AppendLine(string.Join(',', Csv("Hoàn khách"), Csv(model.CustomerRefunds)));
            csv.AppendLine(string.Join(',', Csv("Bồi thường"), Csv(model.CompensationCost)));
            csv.AppendLine(string.Join(',', Csv("Phí cổng thanh toán"), Csv(model.PaymentGatewayFees)));
            csv.AppendLine(string.Join(',', Csv("Lợi nhuận ước tính"), Csv(model.EstimatedNetProfit)));
            csv.AppendLine(string.Join(',', Csv("Đơn tranh chấp"), Csv(model.OpenDisputes)));
            csv.AppendLine(string.Join(',', Csv("Giao dịch bất thường"), Csv(model.AbnormalTransactions)));
            csv.AppendLine();
            csv.AppendLine(string.Join(',', Csv("THÁNG"), Csv("TỔNG GIÁ TRỊ"), Csv("HOA HỒNG"), Csv("TRẢ CHỦ XE"), Csv("PHÍ CỔNG"), Csv("HOÀN KHÁCH"), Csv("BỒI THƯỜNG"), Csv("LỢI NHUẬN"), Csv("ĐƠN HOÀN THÀNH")));
            foreach (var item in model.MonthlyRevenue.OrderBy(x => x.Month))
            {
                csv.AppendLine(string.Join(',', Csv(item.MonthName), Csv(item.GrossRevenue), Csv(item.PlatformRevenue), Csv(item.PartnerNetRevenue), Csv(item.PaymentGatewayFees), Csv(item.RefundAmount), Csv(item.CompensationAmount), Csv(item.NetProfit), Csv(item.CompletedReservations)));
            }

            var bom = Encoding.UTF8.GetPreamble();
            var payload = Encoding.UTF8.GetBytes(csv.ToString());
            var bytes = new byte[bom.Length + payload.Length];
            Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
            Buffer.BlockCopy(payload, 0, bytes, bom.Length, payload.Length);
            var monthPart = model.SelectedMonth.HasValue ? $"-Thang-{model.SelectedMonth.Value:00}" : string.Empty;
            return File(bytes, "text/csv; charset=utf-8", $"SmartCar-Bao-Cao-{model.SelectedYear}{monthPart}.csv");
        }

        private static AdminDashboardSummaryDto NewDashboardModel(int year, int? month, int? locationId, int? partnerId, string? status) => new()
        {
            SelectedYear = year,
            SelectedMonth = month,
            SelectedLocationID = locationId,
            SelectedPartnerID = partnerId,
            SelectedStatus = status
        };

        private async Task<AdminDashboardSummaryDto?> LoadDashboardAsync(int selectedYear, int? month, int? locationId, int? partnerId, string? status)
        {
            try
            {
                var query = new List<string> { $"year={selectedYear}" };
                if (month is >= 1 and <= 12) query.Add($"month={month}");
                if (locationId.HasValue) query.Add($"locationId={locationId.Value}");
                if (partnerId.HasValue) query.Add($"partnerId={partnerId.Value}");
                if (!string.IsNullOrWhiteSpace(status)) query.Add("status=" + Uri.EscapeDataString(status));
                var response = await CreateAuthorizedClient().GetAsync("api/AdminDashboardOverview?" + string.Join("&", query));
                if (!response.IsSuccessStatusCode) return null;
                return JsonConvert.DeserializeObject<AdminDashboardSummaryDto>(await response.Content.ReadAsStringAsync())
                       ?? NewDashboardModel(selectedYear, month, locationId, partnerId, status);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }
    }
}
