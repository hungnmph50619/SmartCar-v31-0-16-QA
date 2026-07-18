using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using SmartCar.Dto.LocationDtos;
using SmartCar.WebUI.Infrastructure;
using System.Globalization;
using System.Net.Http.Headers;

namespace SmartCar.WebUI.Controllers
{
    public class DefaultController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DefaultController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await LoadLocationsAsync();
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> AddressSuggestions(string? query, CancellationToken cancellationToken)
        {
            var text = (query ?? string.Empty).Trim();
            if (text.Length < 2) return Json(Array.Empty<object>());
            if (text.Length > 120) text = text[..120];

            try
            {
                var client = _httpClientFactory.CreateClient("Geocoding");
                var url = "search?format=jsonv2&accept-language=vi&countrycodes=vn&addressdetails=1&namedetails=1&limit=6&q="
                          + Uri.EscapeDataString(text);
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) return Json(Array.Empty<object>());

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var rows = JsonConvert.DeserializeObject<List<NominatimPlace>>(json) ?? new List<NominatimPlace>();
                var results = rows
                    .Select(ToAddressSuggestion)
                    .OfType<AddressSuggestion>()
                    .DistinctBy(x => $"{x.Latitude:0.######}|{x.Longitude:0.######}")
                    .Take(6)
                    .ToList();

                return Json(results);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
            catch
            {
                // Autocomplete là chức năng hỗ trợ; lỗi dịch vụ ngoài không được làm hỏng trang chủ.
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ReverseGeocode(double latitude, double longitude, CancellationToken cancellationToken)
        {
            if (!double.IsFinite(latitude) || !double.IsFinite(longitude) ||
                latitude is < -90 or > 90 || longitude is < -180 or > 180)
            {
                return BadRequest(new { message = "Tọa độ không hợp lệ." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("Geocoding");
                var url = FormattableString.Invariant(
                    $"reverse?format=jsonv2&accept-language=vi&addressdetails=1&zoom=18&lat={latitude:0.#######}&lon={longitude:0.#######}");
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) return Json(new { address = string.Empty });

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var row = JsonConvert.DeserializeObject<NominatimPlace>(json);
                return Json(new { address = row?.DisplayName ?? string.Empty });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new EmptyResult();
            }
            catch
            {
                return Json(new { address = string.Empty });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(
            int locationID,
            string? book_pick_date,
            string? book_off_date,
            string? time_pick,
            string? time_off,
            int? seats,
            decimal? minPrice,
            decimal? maxPrice,
            string? transmission,
            string? fuel,
            string? vehicleType,
            int? radiusKm,
            string? searchLatitude,
            string? searchLongitude,
            string? searchAddress,
            string? locationSelectionMode)
        {
            if (locationID <= 0)
            {
                TempData["SearchError"] = "Vui lòng chọn điểm nhận xe cụ thể.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDate(book_pick_date, out var pickUpDate))
            {
                TempData["SearchError"] = "Ngày nhận xe không hợp lệ. Vui lòng chọn lại ngày nhận xe.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseDate(book_off_date, out var dropOffDate))
            {
                TempData["SearchError"] = "Ngày trả xe không hợp lệ. Vui lòng chọn lại ngày trả xe.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseTime(time_pick, out var pickUpTime))
            {
                TempData["SearchError"] = "Giờ nhận xe không hợp lệ. Vui lòng chọn lại giờ nhận xe.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParseTime(time_off, out var dropOffTime))
            {
                TempData["SearchError"] = "Giờ trả xe không hợp lệ. Vui lòng chọn lại giờ trả xe.";
                return RedirectToAction(nameof(Index));
            }

            var pickUpDateTime = pickUpDate.Date.Add(pickUpTime);
            var dropOffDateTime = dropOffDate.Date.Add(dropOffTime);

            if (VietnamTime.ToUtc(pickUpDateTime) < DateTime.UtcNow.AddMinutes(-1))
            {
                TempData["SearchError"] = "Thời gian nhận xe không được nằm trong quá khứ.";
                return RedirectToAction(nameof(Index));
            }

            if (dropOffDateTime <= pickUpDateTime)
            {
                TempData["SearchError"] = "Thời gian trả xe phải sau thời gian nhận xe.";
                return RedirectToAction(nameof(Index));
            }

            if (minPrice.HasValue && maxPrice.HasValue && maxPrice.Value < minPrice.Value)
            {
                TempData["SearchError"] = "Giá tối đa phải lớn hơn hoặc bằng giá tối thiểu.";
                return RedirectToAction(nameof(Index));
            }


            var hasCoordinateInput = !string.IsNullOrWhiteSpace(searchLatitude) || !string.IsNullOrWhiteSpace(searchLongitude);
            double? exactLatitude = null;
            double? exactLongitude = null;
            if (hasCoordinateInput)
            {
                if (!TryParseCoordinate(searchLatitude, -90d, 90d, out var parsedLatitude) ||
                    !TryParseCoordinate(searchLongitude, -180d, 180d, out var parsedLongitude))
                {
                    TempData["SearchError"] = "Tọa độ điểm nhận xe không hợp lệ. Vui lòng chọn lại vị trí trên bản đồ.";
                    return RedirectToAction(nameof(Index));
                }

                exactLatitude = parsedLatitude;
                exactLongitude = parsedLongitude;
            }

            var effectiveRadius = Math.Clamp(radiusKm ?? 20, 1, 100);

            // Luôn lưu theo định dạng chuẩn, không phụ thuộc ngôn ngữ của Windows hoặc trình duyệt.
            TempData["bookpickdate"] = pickUpDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            TempData["bookoffdate"] = dropOffDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            TempData["timepick"] = pickUpTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            TempData["timeoff"] = dropOffTime.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
            TempData["locationID"] = locationID;
            TempData["radiusKm"] = effectiveRadius;
            if (exactLatitude.HasValue && exactLongitude.HasValue)
            {
                TempData["searchLatitude"] = exactLatitude.Value.ToString("0.#######", CultureInfo.InvariantCulture);
                TempData["searchLongitude"] = exactLongitude.Value.ToString("0.#######", CultureInfo.InvariantCulture);
                TempData["searchAddress"] = string.IsNullOrWhiteSpace(searchAddress) ? "Điểm nhận xe đã chọn" : searchAddress.Trim();
                TempData["locationSelectionMode"] = string.IsNullOrWhiteSpace(locationSelectionMode) ? "map" : locationSelectionMode.Trim();
            }

            return RedirectToAction("Index", "RentACarList", new
            {
                id = locationID,
                seats,
                minPrice,
                maxPrice,
                transmission,
                fuel,
                vehicleType,
                radiusKm = effectiveRadius,
                searchLatitude = exactLatitude?.ToString("0.#######", CultureInfo.InvariantCulture),
                searchLongitude = exactLongitude?.ToString("0.#######", CultureInfo.InvariantCulture),
                searchAddress = string.IsNullOrWhiteSpace(searchAddress) ? null : searchAddress.Trim()
            });
        }

        private async Task LoadLocationsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.Claims.FirstOrDefault(x => x.Type == "carbooktoken")?.Value;

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var locations = new List<ResultLocationDto>();

            try
            {
                var responseMessage = await client.GetAsync("api/Locations");
                if (responseMessage.IsSuccessStatusCode)
                {
                    var jsonData = await responseMessage.Content.ReadAsStringAsync();
                    locations = JsonConvert.DeserializeObject<List<ResultLocationDto>>(jsonData)
                                ?? new List<ResultLocationDto>();
                    locations = locations.Where(x => x.IsActive).ToList();

                    if (locations.Count == 0)
                    {
                        TempData["SearchError"] = "Cơ sở dữ liệu chưa có điểm nhận xe đang hoạt động. Hãy thêm địa điểm trong khu vực quản trị.";
                    }
                }
                else
                {
                    TempData["SearchError"] = "Không tải được danh sách địa điểm. Vui lòng kiểm tra Web API rồi thử lại.";
                }
            }
            catch (HttpRequestException)
            {
                TempData["SearchError"] = "Không kết nối được Web API. Hãy chạy đồng thời SmartCar.WebApi và SmartCar.WebUI.";
            }

            ViewBag.Locations = locations;
            ViewBag.LocationsJson = JsonConvert.SerializeObject(locations.Select(x => new
            {
                id = x.LocationID,
                name = x.Name,
                displayName = x.DisplayName,
                fullAddress = x.FullAddress,
                provinceCity = x.ProvinceCity,
                district = x.District,
                ward = x.Ward,
                latitude = x.Latitude,
                longitude = x.Longitude,
                radiusKm = x.SearchRadiusKm
            }));
            ViewBag.v = locations.Select(x => new SelectListItem
            {
                Text = x.DisplayName,
                Value = x.LocationID.ToString()
            }).ToList();
        }


        private static AddressSuggestion? ToAddressSuggestion(NominatimPlace row)
        {
            if (!double.TryParse(row.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(row.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) ||
                !double.IsFinite(latitude) || !double.IsFinite(longitude))
            {
                return null;
            }

            var title = FirstNotBlank(
                row.Name, row.Address?.Amenity, row.Address?.Tourism, row.Address?.Shop,
                row.Address?.Road, row.Address?.Pedestrian, row.Address?.Neighbourhood,
                row.Address?.Suburb, row.Address?.Quarter, row.Address?.Village,
                row.Address?.Town, row.Address?.City, row.DisplayName) ?? "Địa điểm";

            return new AddressSuggestion(
                row.PlaceId.HasValue
                    ? row.PlaceId.Value.ToString(CultureInfo.InvariantCulture)
                    : $"{latitude:0.######}-{longitude:0.######}",
                title,
                row.DisplayName ?? title,
                latitude,
                longitude,
                row.Type ?? row.Category ?? string.Empty);
        }

        private static string? FirstNotBlank(params string?[] values) =>
            values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

        private sealed record AddressSuggestion(
            string Id, string Title, string Address, double Latitude, double Longitude, string Type);

        private sealed class NominatimPlace
        {
            [JsonProperty("place_id")] public long? PlaceId { get; set; }
            [JsonProperty("lat")] public string? Latitude { get; set; }
            [JsonProperty("lon")] public string? Longitude { get; set; }
            [JsonProperty("display_name")] public string? DisplayName { get; set; }
            [JsonProperty("name")] public string? Name { get; set; }
            [JsonProperty("type")] public string? Type { get; set; }
            [JsonProperty("category")] public string? Category { get; set; }
            [JsonProperty("address")] public NominatimAddress? Address { get; set; }
        }

        private sealed class NominatimAddress
        {
            [JsonProperty("amenity")] public string? Amenity { get; set; }
            [JsonProperty("tourism")] public string? Tourism { get; set; }
            [JsonProperty("shop")] public string? Shop { get; set; }
            [JsonProperty("road")] public string? Road { get; set; }
            [JsonProperty("pedestrian")] public string? Pedestrian { get; set; }
            [JsonProperty("neighbourhood")] public string? Neighbourhood { get; set; }
            [JsonProperty("suburb")] public string? Suburb { get; set; }
            [JsonProperty("quarter")] public string? Quarter { get; set; }
            [JsonProperty("village")] public string? Village { get; set; }
            [JsonProperty("town")] public string? Town { get; set; }
            [JsonProperty("city")] public string? City { get; set; }
        }


        private static bool TryParseCoordinate(string? value, double min, double max, out double result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (!double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
                !double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out result))
                return false;
            return double.IsFinite(result) && result >= min && result <= max;
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var formats = new[]
            {
                "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy"
            };

            return DateTime.TryParseExact(
                value.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out result);
        }

        private static bool TryParseTime(string? value, out TimeSpan result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };
            return TimeSpan.TryParseExact(
                value.Trim(), formats, CultureInfo.InvariantCulture,
                TimeSpanStyles.None, out result);
        }
    }
}
