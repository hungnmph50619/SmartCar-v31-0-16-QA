using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartCar.Dto.LocationDtos;
using SmartCar.Dto.RentACarDtos;
using SmartCar.Dto.ReservationDtos;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    [Authorize(Roles = "Customer")]
    public class ReservationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReservationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int id,
            int? pickUpLocationID,
            string? pickUpDate,
            string? dropOffDate,
            string? pickUpTime,
            string? dropOffTime,
            string? rentalMode)
        {
            if (IsVehiclePartnerAccount())
            {
                TempData["ReservationBlocked"] = "Tài khoản chủ xe không được sử dụng để thuê xe. Vui lòng đăng nhập bằng tài khoản khách hàng.";
                return RedirectToAction("Dashboard", "VehiclePartner");
            }

            if (id <= 0)
            {
                TempData["SearchError"] = "Không xác định được xe cần thuê.";
                return RedirectToAction("Index", "Default");
            }

            pickUpLocationID ??= ParseInt(TempData.Peek("locationID"));
            var parsedPickUpDate = ParseDate(pickUpDate) ?? ParseDate(TempData.Peek("bookpickdate")?.ToString());
            var parsedDropOffDate = ParseDate(dropOffDate) ?? ParseDate(TempData.Peek("bookoffdate")?.ToString());
            var parsedPickUpTime = ParseTime(pickUpTime) ?? ParseTime(TempData.Peek("timepick")?.ToString());
            var parsedDropOffTime = ParseTime(dropOffTime) ?? ParseTime(TempData.Peek("timeoff")?.ToString());

            var model = new CreateReservationDto
            {
                CarID = id,
                PickUpLocationID = pickUpLocationID ?? 0,
                DropOffLocationID = pickUpLocationID ?? 0,
                PickUpDate = parsedPickUpDate ?? VietnamUtcNowLocal().Date.AddDays(1),
                DropOffDate = parsedDropOffDate ?? VietnamUtcNowLocal().Date.AddDays(2),
                PickUpTime = parsedPickUpTime ?? new TimeSpan(8, 0, 0),
                DropOffTime = parsedDropOffTime ?? new TimeSpan(8, 0, 0),
                RentalMode = rentalMode is "Tự lái" or "Có tài xế" ? rentalMode : "Tự lái",
                DeliveryMethod = "Nhận tại điểm giao xe",
                PickUpAddressText = TempData.Peek("searchAddress")?.ToString(),
                DropOffAddressText = TempData.Peek("searchAddress")?.ToString()
            };

            var readiness = await LoadReadinessAsync();
            ApplyReadiness(model, readiness);
            await PrepareViewAsync(model, readiness, normalizeSelectionFromOffer: true);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CreateReservationDto model)
        {
            if (IsVehiclePartnerAccount())
            {
                TempData["ReservationBlocked"] = "Tài khoản chủ xe không được sử dụng để thuê xe. Vui lòng đăng nhập bằng tài khoản khách hàng.";
                return RedirectToAction("Dashboard", "VehiclePartner");
            }

            var readiness = await LoadReadinessAsync();
            RemoveProfileModelState();
            ApplyReadiness(model, readiness);
            TranslateBindingErrorsToVietnamese();

            if (readiness is null)
            {
                ModelState.AddModelError(string.Empty, "Không tải được hồ sơ tài khoản. Vui lòng đăng nhập lại.");
            }
            else if (model.RentalMode == "Có tài xế")
            {
                if (!readiness.CanBookWithDriver)
                    ModelState.AddModelError(string.Empty, "Để đặt xe có tài xế, bạn cần xác minh email OTP và có số điện thoại liên hệ. Không bắt buộc CCCD hoặc bằng lái của khách.");
            }
            else if (!readiness.CanBook)
            {
                ModelState.AddModelError(string.Empty, "Để thuê xe tự lái, bạn phải hoàn tất xác minh CCCD và giấy phép lái xe còn hiệu lực.");
            }

            var scheduleFieldsAreValid =
                !HasModelStateError(nameof(CreateReservationDto.PickUpDate)) &&
                !HasModelStateError(nameof(CreateReservationDto.DropOffDate)) &&
                !HasModelStateError(nameof(CreateReservationDto.PickUpTime)) &&
                !HasModelStateError(nameof(CreateReservationDto.DropOffTime));

            if (scheduleFieldsAreValid)
            {
                var pickUpDateTime = ComposeVietnamLocal(model.PickUpDate, model.PickUpTime);
                var dropOffDateTime = ComposeVietnamLocal(model.DropOffDate, model.DropOffTime);
                var pickUpUtc = VietnamLocalToUtc(pickUpDateTime);

                if (pickUpUtc < DateTime.UtcNow.AddMinutes(-1))
                    ModelState.AddModelError(string.Empty, "Thời gian nhận xe không được nằm trong quá khứ.");
                if (dropOffDateTime <= pickUpDateTime)
                    ModelState.AddModelError(string.Empty, "Thời gian trả xe phải sau thời gian nhận xe.");
            }

            if (!ModelState.IsValid)
            {
                await PrepareViewAsync(model, readiness, normalizeSelectionFromOffer: false);
                return View(model);
            }

            try
            {
                var client = CreateAuthorizedClient();
                var jsonData = JsonConvert.SerializeObject(model);
                using var stringContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var responseMessage = await client.PostAsync("api/Reservations", stringContent);

                if (responseMessage.IsSuccessStatusCode)
                {
                    var responseData = JsonConvert.DeserializeObject<CreateReservationResponseDto>(
                        await responseMessage.Content.ReadAsStringAsync());
                    TempData["ReservationSuccess"] = responseData is null
                        ? "Đã gửi yêu cầu thuê xe và giữ lịch trong thời hạn chủ xe phản hồi."
                        : $"Đã gửi yêu cầu thành công. Đơn #{responseData.ReservationID}, tổng tiền thuê {responseData.TotalPrice.ToString("#,0", CultureInfo.InvariantCulture)} đồng. Lịch xe được giữ tối đa 120 phút chờ chủ xe phản hồi; sau khi chấp nhận, bạn có 15 phút để thanh toán giữ chỗ.";
                    return RedirectToAction("Details", "ReservationLookup", new { id = responseData?.ReservationID });
                }

                ModelState.AddModelError(string.Empty, CleanApiMessage(await responseMessage.Content.ReadAsStringAsync()));
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API. Hãy chạy SmartCar.WebApi trước khi đặt xe.");
            }
            catch (JsonException)
            {
                ModelState.AddModelError(string.Empty, "Dữ liệu phản hồi từ Web API không đúng định dạng.");
            }

            await PrepareViewAsync(model, readiness, normalizeSelectionFromOffer: false);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Quote(
            int carId,
            DateTime pickUpDate,
            DateTime dropOffDate,
            TimeSpan pickUpTime,
            TimeSpan dropOffTime,
            string rentalMode,
            string deliveryMethod,
            decimal? estimatedDistanceKm = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = BuildQuoteUrl(carId, pickUpDate, dropOffDate, pickUpTime, dropOffTime, rentalMode, deliveryMethod, estimatedDistanceKm);
                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8",
                    Content = body
                };
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { message = "Không kết nối được máy chủ tính giá." });
            }
        }

        private async Task PrepareViewAsync(
            CreateReservationDto model,
            CustomerReadinessDto? readiness,
            bool normalizeSelectionFromOffer)
        {
            ViewBag.v1 = "Thuê xe";
            ViewBag.v2 = "Xác nhận lịch và giá";
            ViewBag.v3 = model.CarID;
            ViewBag.Readiness = readiness;

            var locations = new List<ResultLocationDto>();
            EnhancedCarDetailDto? car = null;
            ReservationQuoteDto? quote = null;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var locationsResponse = await client.GetAsync("api/Locations");
                if (locationsResponse.IsSuccessStatusCode)
                {
                    locations = JsonConvert.DeserializeObject<List<ResultLocationDto>>(
                                    await locationsResponse.Content.ReadAsStringAsync())
                                ?? new List<ResultLocationDto>();
                }

                var carResponse = await client.GetAsync($"api/operations/cars/{model.CarID}/detail");
                if (carResponse.IsSuccessStatusCode)
                {
                    car = JsonConvert.DeserializeObject<EnhancedCarDetailDto>(await carResponse.Content.ReadAsStringAsync());
                    if (car is not null && normalizeSelectionFromOffer)
                        NormalizeSelectionFromOffer(model, car);
                }

                if (model.CarID > 0 && model.PickUpDate != default && model.DropOffDate != default)
                {
                    var quoteUrl = BuildQuoteUrl(
                        model.CarID,
                        model.PickUpDate,
                        model.DropOffDate,
                        model.PickUpTime,
                        model.DropOffTime,
                        model.RentalMode,
                        model.DeliveryMethod,
                        model.EstimatedDistanceKm);
                    var quoteResponse = await client.GetAsync(quoteUrl);
                    if (quoteResponse.IsSuccessStatusCode)
                        quote = JsonConvert.DeserializeObject<ReservationQuoteDto>(await quoteResponse.Content.ReadAsStringAsync());
                }
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không tải được dữ liệu xe và địa điểm từ Web API.");
            }

            locations = locations.Where(x => x.IsActive).ToList();
            ViewBag.Locations = locations;
            ViewBag.LocationsJson = JsonConvert.SerializeObject(locations.Select(x => new
            {
                id = x.LocationID,
                name = x.Name,
                displayName = x.DisplayName,
                fullAddress = x.FullAddress,
                latitude = x.Latitude,
                longitude = x.Longitude
            }));
            ViewBag.v = locations.Select(x => new SelectListItem { Text = x.DisplayName, Value = x.LocationID.ToString() }).ToList();
            ViewBag.CarName = car is null ? $"Xe #{model.CarID}" : $"{car.Brand} {car.Model}".Trim();
            ViewBag.CarImage = car?.CoverImageUrl;
            ViewBag.CarDetail = car;
            ViewBag.Quote = quote;
        }

        private async Task<CustomerReadinessDto?> LoadReadinessAsync()
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/operations/customer/readiness");
                if (!response.IsSuccessStatusCode) return null;
                return JsonConvert.DeserializeObject<CustomerReadinessDto>(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException) { return null; }
            catch (JsonException) { return null; }
        }

        private static void ApplyReadiness(CreateReservationDto model, CustomerReadinessDto? readiness)
        {
            if (readiness is null) return;
            model.Name = readiness.Name;
            model.Surname = readiness.Surname;
            model.Email = readiness.Email;
            model.Phone = readiness.Phone;
            model.Age = readiness.DateOfBirth.HasValue ? CalculateYears(readiness.DateOfBirth.Value, SmartCar.WebUI.Infrastructure.VietnamTime.Today) : 0;
            if (readiness.DateOfBirth.HasValue)
                model.BookingHolderDateOfBirth = readiness.DateOfBirth.Value.Date;
            model.DriverLicenseYear = readiness.DriverLicenseIssuedDate.HasValue
                ? Math.Max(1, CalculateYears(readiness.DriverLicenseIssuedDate.Value, SmartCar.WebUI.Infrastructure.VietnamTime.Today))
                : 0;
        }

        private static void NormalizeSelectionFromOffer(CreateReservationDto model, EnhancedCarDetailDto car)
        {
            var offeredRentalMode = string.IsNullOrWhiteSpace(car.RentalMode) ? "Tự lái" : car.RentalMode.Trim();
            model.RentalMode = offeredRentalMode switch
            {
                "Có tài xế" => "Có tài xế",
                "Tự lái" => "Tự lái",
                _ => model.RentalMode is "Tự lái" or "Có tài xế" ? model.RentalMode : "Tự lái"
            };

            var offeredDeliveryMethod = string.IsNullOrWhiteSpace(car.DeliveryMethod)
                ? "Nhận tại điểm giao xe"
                : car.DeliveryMethod.Trim();
            model.DeliveryMethod = offeredDeliveryMethod switch
            {
                "Giao xe tận nơi" => "Giao xe tận nơi",
                "Nhận tại điểm giao xe" => "Nhận tại điểm giao xe",
                _ => model.DeliveryMethod is "Nhận tại điểm giao xe" or "Giao xe tận nơi"
                    ? model.DeliveryMethod
                    : "Nhận tại điểm giao xe"
            };
        }

        private static string BuildQuoteUrl(
            int carId,
            DateTime pickUpDate,
            DateTime dropOffDate,
            TimeSpan pickUpTime,
            TimeSpan dropOffTime,
            string? rentalMode,
            string? deliveryMethod,
            decimal? estimatedDistanceKm = null)
        {
            return $"api/operations/cars/{carId}/quote" +
                   $"?pickUpDate={Uri.EscapeDataString(pickUpDate.ToString("yyyy-MM-dd"))}" +
                   $"&dropOffDate={Uri.EscapeDataString(dropOffDate.ToString("yyyy-MM-dd"))}" +
                   $"&pickUpTime={Uri.EscapeDataString(pickUpTime.ToString(@"hh\:mm\:ss"))}" +
                   $"&dropOffTime={Uri.EscapeDataString(dropOffTime.ToString(@"hh\:mm\:ss"))}" +
                   $"&rentalMode={Uri.EscapeDataString(rentalMode ?? "Tự lái")}" +
                   $"&deliveryMethod={Uri.EscapeDataString(deliveryMethod ?? "Nhận tại điểm giao xe")}" +
                   (estimatedDistanceKm.HasValue ? $"&estimatedDistanceKm={Uri.EscapeDataString(estimatedDistanceKm.Value.ToString(CultureInfo.InvariantCulture))}" : string.Empty);
        }

        private void RemoveProfileModelState()
        {
            foreach (var key in new[]
                     {
                         nameof(CreateReservationDto.Name), nameof(CreateReservationDto.Surname),
                         nameof(CreateReservationDto.Email), nameof(CreateReservationDto.Phone),
                         nameof(CreateReservationDto.Age), nameof(CreateReservationDto.DriverLicenseYear)
                     })
                ModelState.Remove(key);
        }

        private static int CalculateYears(DateTime from, DateTime to)
        {
            var years = to.Year - from.Year;
            if (from.Date > to.AddYears(-years)) years--;
            return Math.Max(0, years);
        }

        private bool IsVehiclePartnerAccount()
            => string.Equals(User.FindFirstValue("IsVehiclePartner"), "true", StringComparison.OrdinalIgnoreCase);

        private bool HasModelStateError(string key)
            => ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0;

        private void TranslateBindingErrorsToVietnamese()
        {
            ReplaceBindingError(nameof(CreateReservationDto.PickUpLocationID), "Vui lòng chọn địa điểm nhận xe.");
            ReplaceBindingError(nameof(CreateReservationDto.DropOffLocationID), "Vui lòng chọn địa điểm trả xe.");
            ReplaceBindingError(nameof(CreateReservationDto.CarID), "Xe đã chọn không hợp lệ.");
            ReplaceBindingError(nameof(CreateReservationDto.PickUpDate), "Vui lòng chọn ngày nhận xe hợp lệ.");
            ReplaceBindingError(nameof(CreateReservationDto.DropOffDate), "Vui lòng chọn ngày trả xe hợp lệ.");
            ReplaceBindingError(nameof(CreateReservationDto.PickUpTime), "Vui lòng chọn giờ nhận xe hợp lệ.");
            ReplaceBindingError(nameof(CreateReservationDto.DropOffTime), "Vui lòng chọn giờ trả xe hợp lệ.");
        }

        private void ReplaceBindingError(string key, string vietnameseMessage)
        {
            if (!ModelState.TryGetValue(key, out var entry) || entry.Errors.Count == 0) return;
            var hasDefaultError = entry.Errors.Any(error =>
                error.Exception is not null ||
                string.IsNullOrWhiteSpace(error.ErrorMessage) ||
                error.ErrorMessage.Contains("The value", StringComparison.OrdinalIgnoreCase) ||
                error.ErrorMessage.Contains("not valid", StringComparison.OrdinalIgnoreCase) ||
                error.ErrorMessage.Contains("is invalid", StringComparison.OrdinalIgnoreCase));
            if (!hasDefaultError) return;
            ModelState.Remove(key);
            ModelState.AddModelError(key, vietnameseMessage);
        }

        private static int? ParseInt(object? value) => int.TryParse(value?.ToString(), out var result) ? result : null;

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var formats = new[]
            {
                "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy", "d/M/yyyy",
                "dd/MM/yyyy HH:mm:ss", "d/M/yyyy H:mm:ss", "MM/dd/yyyy", "M/d/yyyy"
            };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result))
                return result;
            return DateTime.TryParse(value.Trim(), new CultureInfo("vi-VN"), DateTimeStyles.AllowWhiteSpaces, out result)
                ? result
                : null;
        }

        private static TimeSpan? ParseTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var formats = new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" };
            return TimeSpan.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, TimeSpanStyles.None, out var result)
                ? result
                : null;
        }

        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

        private static DateTime ComposeVietnamLocal(DateTime date, TimeSpan time)
            => DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);

        private static DateTime VietnamUtcNowLocal()
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

        private static DateTime VietnamLocalToUtc(DateTime local)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), VietnamTimeZone);

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static string CleanApiMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Không thể tạo đơn đặt xe. Vui lòng kiểm tra lại thông tin.";
            var cleaned = message.Trim().Trim('"');
            if (!cleaned.StartsWith("{")) return cleaned;
            try
            {
                var json = JObject.Parse(message);
                return json["message"]?.ToString() ?? json["title"]?.ToString() ??
                       "Thông tin đặt xe chưa hợp lệ. Vui lòng kiểm tra lại.";
            }
            catch (JsonException)
            {
                return "Không thể tạo đơn đặt xe. Vui lòng kiểm tra lại thông tin.";
            }
        }
    }
}
