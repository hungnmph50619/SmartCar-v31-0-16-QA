using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.LocationDtos;
using SmartCar.Dto.RentACarDtos;
using System.Globalization;
using System.Net;

namespace SmartCar.WebUI.Controllers
{
    public class RentACarListController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RentACarListController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index(
            int id,
            int? seats,
            decimal? minPrice,
            decimal? maxPrice,
            string? transmission,
            string? fuel,
            string? vehicleType,
            int? radiusKm,
            double? searchLatitude,
            double? searchLongitude,
            string? searchAddress)
        {
            if (id <= 0)
            {
                int.TryParse(TempData.Peek("locationID")?.ToString(), out id);
            }

            var rawPickUpDate = TempData.Peek("bookpickdate")?.ToString();
            var rawDropOffDate = TempData.Peek("bookoffdate")?.ToString();
            var rawPickUpTime = TempData.Peek("timepick")?.ToString();
            var rawDropOffTime = TempData.Peek("timeoff")?.ToString();

            if (id <= 0 ||
                !TryParseDate(rawPickUpDate, out var pickUpDate) ||
                !TryParseDate(rawDropOffDate, out var dropOffDate) ||
                !TryParseTime(rawPickUpTime, out var pickUpTime) ||
                !TryParseTime(rawDropOffTime, out var dropOffTime))
            {
                TempData["SearchError"] = "Thông tin ngày, giờ hoặc điểm nhận xe chưa hợp lệ. Vui lòng chọn lại trên trang chủ.";
                return RedirectToAction("Index", "Default");
            }

            var pickUpDateTime = pickUpDate.Date.Add(pickUpTime);
            var dropOffDateTime = dropOffDate.Date.Add(dropOffTime);
            if (dropOffDateTime <= pickUpDateTime)
            {
                TempData["SearchError"] = "Thời gian trả xe phải sau thời gian nhận xe.";
                return RedirectToAction("Index", "Default");
            }

            searchLatitude ??= ParseDouble(TempData.Peek("searchLatitude"));
            searchLongitude ??= ParseDouble(TempData.Peek("searchLongitude"));
            searchAddress = string.IsNullOrWhiteSpace(searchAddress)
                ? TempData.Peek("searchAddress")?.ToString()
                : searchAddress.Trim();

            var hasCoordinateInput = searchLatitude.HasValue || searchLongitude.HasValue;
            var hasExactCoordinates = searchLatitude.HasValue && searchLongitude.HasValue &&
                                      searchLatitude.Value is >= -90 and <= 90 &&
                                      searchLongitude.Value is >= -180 and <= 180;
            if (hasCoordinateInput && !hasExactCoordinates)
            {
                TempData["SearchError"] = "Tọa độ điểm nhận xe chưa đầy đủ hoặc không hợp lệ. Vui lòng chọn lại vị trí.";
                return RedirectToAction("Index", "Default");
            }

            var effectiveRadius = Math.Clamp(radiusKm ?? ParseInt(TempData.Peek("radiusKm")) ?? 20, 1, 100);
            var pickUpDateIso = pickUpDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var dropOffDateIso = dropOffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var pickUpTimeIso = pickUpTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
            var dropOffTimeIso = dropOffTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

            ViewBag.locationID = id;
            ViewBag.RadiusKm = effectiveRadius;
            ViewBag.SearchLatitude = hasExactCoordinates ? searchLatitude : null;
            ViewBag.SearchLongitude = hasExactCoordinates ? searchLongitude : null;
            ViewBag.SearchAddress = hasExactCoordinates ? searchAddress : null;
            ViewBag.bookPickDate = pickUpDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            ViewBag.bookOffDate = dropOffDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            ViewBag.timePick = pickUpTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            ViewBag.timeOff = dropOffTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            ViewBag.bookPickDateIso = pickUpDateIso;
            ViewBag.bookOffDateIso = dropOffDateIso;
            ViewBag.timePickIso = pickUpTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            ViewBag.timeOffIso = dropOffTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);

            var requestUrl = "api/RentACars" +
                             $"?locationID={id}&available=true" +
                             $"&radiusKm={effectiveRadius}" +
                             $"&pickUpDate={Uri.EscapeDataString(pickUpDateIso)}" +
                             $"&dropOffDate={Uri.EscapeDataString(dropOffDateIso)}" +
                             $"&pickUpTime={Uri.EscapeDataString(pickUpTimeIso)}" +
                             $"&dropOffTime={Uri.EscapeDataString(dropOffTimeIso)}" +
                             (seats.HasValue ? $"&seats={seats.Value}" : string.Empty) +
                             (minPrice.HasValue ? $"&minPrice={minPrice.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty) +
                             (maxPrice.HasValue ? $"&maxPrice={maxPrice.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty) +
                             (!string.IsNullOrWhiteSpace(transmission) ? $"&transmission={Uri.EscapeDataString(transmission)}" : string.Empty) +
                             (!string.IsNullOrWhiteSpace(fuel) ? $"&fuel={Uri.EscapeDataString(fuel)}" : string.Empty) +
                             (!string.IsNullOrWhiteSpace(vehicleType) ? $"&vehicleType={Uri.EscapeDataString(vehicleType)}" : string.Empty) +
                             (hasExactCoordinates ? $"&searchLatitude={searchLatitude.Value.ToString(CultureInfo.InvariantCulture)}&searchLongitude={searchLongitude.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty);

            ViewBag.Seats = seats;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Transmission = transmission;
            ViewBag.Fuel = fuel;
            ViewBag.VehicleType = vehicleType;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var locationResponse = await client.GetAsync($"api/Locations/{id}");
                if (locationResponse.IsSuccessStatusCode)
                {
                    ViewBag.SelectedLocation = JsonConvert.DeserializeObject<ResultLocationDto>(
                        await locationResponse.Content.ReadAsStringAsync());
                }

                var responseMessage = await client.GetAsync(requestUrl);
                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonData = await responseMessage.Content.ReadAsStringAsync();
                    var values = JsonConvert.DeserializeObject<List<FilterRentACarDto>>(jsonData)
                                 ?? new List<FilterRentACarDto>();
                    ViewBag.MapCarsJson = JsonConvert.SerializeObject(values
                        .Where(x => x.Latitude.HasValue && x.Longitude.HasValue)
                        .Select(x => new
                        {
                            carId = x.carID,
                            title = $"{x.Brand} {x.Model}".Trim(),
                            location = x.LocationName,
                            address = x.LocationAddress,
                            latitude = x.Latitude,
                            longitude = x.Longitude,
                            distanceKm = x.DistanceKm,
                            price = x.Amount
                        }));
                    return View(values);
                }

                ViewBag.ErrorMessage = await GetVietnameseErrorMessageAsync(responseMessage);
            }
            catch (HttpRequestException)
            {
                ViewBag.ErrorMessage = "Không kết nối được Web API. Hãy kiểm tra SmartCar.WebApi đang chạy tại cổng 7060.";
            }
            catch (JsonException)
            {
                ViewBag.ErrorMessage = "Dữ liệu xe hoặc địa điểm do Web API trả về không đúng định dạng.";
            }

            return View(new List<FilterRentACarDto>());
        }

        private static int? ParseInt(object? value) => int.TryParse(value?.ToString(), out var result) ? result : null;

        private static double? ParseDouble(object? value)
        {
            return double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private static async Task<string> GetVietnameseErrorMessageAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var plainText = content.Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(plainText) && !plainText.StartsWith("{")) return plainText;

            try
            {
                var json = JObject.Parse(content);
                var message = json["message"]?.ToString();
                if (!string.IsNullOrWhiteSpace(message)) return message;

                var errors = json["errors"] as JObject;
                if (errors is not null)
                {
                    if (errors.Properties().Any(x => x.Name.Contains("pickUpDate", StringComparison.OrdinalIgnoreCase)))
                        return "Ngày nhận xe không đúng định dạng. Vui lòng chọn lại ngày nhận xe.";
                    if (errors.Properties().Any(x => x.Name.Contains("dropOffDate", StringComparison.OrdinalIgnoreCase)))
                        return "Ngày trả xe không đúng định dạng. Vui lòng chọn lại ngày trả xe.";
                    if (errors.Properties().Any(x => x.Name.Contains("pickUpTime", StringComparison.OrdinalIgnoreCase)))
                        return "Giờ nhận xe không đúng định dạng. Vui lòng chọn lại giờ nhận xe.";
                    if (errors.Properties().Any(x => x.Name.Contains("dropOffTime", StringComparison.OrdinalIgnoreCase)))
                        return "Giờ trả xe không đúng định dạng. Vui lòng chọn lại giờ trả xe.";
                }
            }
            catch (JsonException) { }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Thông tin tìm xe chưa hợp lệ. Vui lòng chọn lại địa điểm, ngày và giờ.",
                HttpStatusCode.Unauthorized => "Bạn chưa được phép thực hiện thao tác này.",
                HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
                HttpStatusCode.NotFound => "Không tìm thấy dữ liệu xe phù hợp.",
                _ => "Web API gặp lỗi khi tìm xe. Vui lòng thử lại hoặc kiểm tra cửa sổ Web API."
            };
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.Trim();
            var formats = new[]
            {
                "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "d/M/yyyy",
                "dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "MM/dd/yyyy", "M/d/yyyy"
            };
            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out result)) return true;
            return DateTime.TryParse(text, new CultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces, out result);
        }

        private static bool TryParseTime(string? value, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };
            return TimeSpan.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, TimeSpanStyles.None, out result);
        }
    }
}
