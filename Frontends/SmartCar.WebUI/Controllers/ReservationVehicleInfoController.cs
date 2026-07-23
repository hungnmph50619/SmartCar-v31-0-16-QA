using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.RentACarDtos;
using SmartCar.Dto.ReservationDtos;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Controllers
{
    [Authorize]
    public class ReservationVehicleInfoController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReservationVehicleInfoController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int reservationId)
        {
            if (reservationId <= 0) return BadRequest(new { message = "Mã đơn không hợp lệ." });

            try
            {
                var client = Client();
                var reservationResponse = await client.GetAsync($"api/operations/reservations/{reservationId}/detail");
                if (!reservationResponse.IsSuccessStatusCode)
                    return StatusCode((int)reservationResponse.StatusCode, new { message = "Không thể đọc thông tin đơn thuê." });

                var reservationDetail = JsonConvert.DeserializeObject<ReservationDetailDto>(
                    await reservationResponse.Content.ReadAsStringAsync());
                if (reservationDetail?.Reservation is null || reservationDetail.Reservation.CarID <= 0)
                    return NotFound(new { message = "Không tìm thấy xe của đơn thuê." });

                var carResponse = await client.GetAsync($"api/operations/cars/{reservationDetail.Reservation.CarID}/detail");
                if (!carResponse.IsSuccessStatusCode)
                    return StatusCode((int)carResponse.StatusCode, new { message = "Không thể đọc thông tin xe." });

                var car = JsonConvert.DeserializeObject<EnhancedCarDetailDto>(await carResponse.Content.ReadAsStringAsync());
                if (car is null) return NotFound(new { message = "Không tìm thấy thông tin xe." });

                var displayName = BuildDisplayName(car.Brand, car.Model, car.ManufactureYear);
                var imageUrl = !string.IsNullOrWhiteSpace(car.CoverImageUrl)
                    ? car.CoverImageUrl
                    : !string.IsNullOrWhiteSpace(car.BigImageUrl)
                        ? car.BigImageUrl
                        : car.GalleryImages.FirstOrDefault() ?? string.Empty;

                var isOwner = IsVehiclePartnerAccount();
                var detailUrl = isOwner
                    ? Url.Action("VehicleOperations", "VehiclePartner", new { id = reservationDetail.Reservation.PartnerVehicleID })
                    : Url.Action("CarDetail", "Car", new { id = car.CarID });

                return Json(new
                {
                    carId = car.CarID,
                    partnerVehicleId = reservationDetail.Reservation.PartnerVehicleID,
                    displayName,
                    imageUrl,
                    maskedLicensePlate = car.MaskedLicensePlate,
                    manufactureYear = car.ManufactureYear,
                    seat = car.Seat,
                    transmission = car.Transmission,
                    fuel = car.Fuel,
                    locationName = string.IsNullOrWhiteSpace(car.LocationName)
                        ? reservationDetail.Reservation.PickUpLocation
                        : car.LocationName,
                    ownerName = reservationDetail.Reservation.OwnerName,
                    ownerPhone = MaskPhone(reservationDetail.Reservation.OwnerPhone),
                    detailUrl,
                    isOwner
                });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Không kết nối được Web API." });
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Dữ liệu xe không đúng định dạng." });
            }
        }

        private HttpClient Client()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private bool IsVehiclePartnerAccount()
            => string.Equals(User.FindFirst("IsVehiclePartner")?.Value, "true", StringComparison.OrdinalIgnoreCase);

        private static string BuildDisplayName(string? brand, string? model, int year)
        {
            var cleanBrand = (brand ?? string.Empty).Trim();
            var cleanModel = (model ?? string.Empty).Trim();
            var rawName = cleanModel.StartsWith(cleanBrand + " ", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(cleanModel, cleanBrand, StringComparison.OrdinalIgnoreCase)
                ? cleanModel
                : $"{cleanBrand} {cleanModel}".Trim();

            var tokens = rawName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var normalized = new List<string>();
            foreach (var token in tokens)
            {
                if (normalized.Count == 0 || !string.Equals(normalized[^1], token, StringComparison.OrdinalIgnoreCase))
                    normalized.Add(token);
            }

            var name = string.Join(' ', normalized);
            return year > 0 ? $"{name} {year}" : name;
        }

        private static string MaskPhone(string? phone)
        {
            var value = (phone ?? string.Empty).Trim();
            if (value.Length < 7) return value;
            return value[..4] + new string('*', Math.Max(3, value.Length - 7)) + value[^3..];
        }
    }
}
