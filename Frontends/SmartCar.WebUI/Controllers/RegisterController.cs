using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.RegisterDtos;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult CreateAppUser()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppUser(CreateRegisterDto createRegisterDto)
        {
            if (!ModelState.IsValid)
            {
                return View(createRegisterDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var jsonData = JsonConvert.SerializeObject(createRegisterDto);
                using var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Registers", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<RegisterResultDto>(await response.Content.ReadAsStringAsync())
                        ?? new RegisterResultDto { Username = createRegisterDto.Username, Email = createRegisterDto.Email, EmailSent = false };
                    TempData["RegistrationAttemptId"] = result.RegistrationAttemptId.ToString();
                    TempData["RegisterUsername"] = string.IsNullOrWhiteSpace(result.Username) ? createRegisterDto.Username : result.Username;
                    TempData["RegisterEmail"] = result.Email;
                    TempData["RegisterMessage"] = result.Message;
                    TempData["RegisterEmailSent"] = result.EmailSent ? "true" : "false";
                    return RedirectToAction(nameof(VerifyEmail), new { attemptId = result.RegistrationAttemptId, username = TempData["RegisterUsername"], email = result.Email });
                }

                var error = NormalizeApiMessage(await response.Content.ReadAsStringAsync());
                AddRegisterErrorToModelState(error);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "Không kết nối được Web API. Hãy chạy SmartCar.WebApi tại cổng 7060.");
            }

            return View(createRegisterDto);
        }

        private static string NormalizeApiMessage(string raw)
        {
            var message = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;

            // Web API có thể trả chuỗi thuần hoặc JSON lỗi dạng { "message": "..." }.
            // Không hiển thị nguyên JSON kỹ thuật cho người dùng.
            try
            {
                if (message.StartsWith("{") && message.EndsWith("}"))
                {
                    dynamic? obj = JsonConvert.DeserializeObject(message);
                    var apiMessage = (string?)obj?.message;
                    if (!string.IsNullOrWhiteSpace(apiMessage)) return apiMessage;
                }
            }
            catch
            {
                // Nếu không parse được JSON thì rơi xuống xử lý chuỗi thường.
            }

            return message.Trim('"');
        }

        private void AddRegisterErrorToModelState(string error)
        {
            var message = string.IsNullOrWhiteSpace(error) ? "Không thể tạo tài khoản." : error;

            if (message.Contains("Tên đăng nhập", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(CreateRegisterDto.Username), message);
                return;
            }

            if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(CreateRegisterDto.Email), message);
                return;
            }

            if (message.Contains("Số điện thoại", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(CreateRegisterDto.Phone), message);
                return;
            }

            if (message.Contains("Mật khẩu", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(CreateRegisterDto.Password), message);
                return;
            }

            ModelState.AddModelError(string.Empty, message);
        }

        [HttpGet]
        public IActionResult VerifyEmail(Guid? attemptId = null, string? username = null, string? email = null)
        {
            var model = new VerifyEmailOtpDto
            {
                RegistrationAttemptId = attemptId ?? (Guid.TryParse(TempData["RegistrationAttemptId"]?.ToString(), out var storedAttemptId) ? storedAttemptId : null),
                Username = username ?? TempData["RegisterUsername"]?.ToString() ?? string.Empty,
                Email = email ?? TempData["RegisterEmail"]?.ToString() ?? string.Empty
            };
            TempData.Keep("RegistrationAttemptId");
            TempData.Keep("RegisterUsername");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailOtpDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Registers/verify-email", content);
                if (response.IsSuccessStatusCode)
                {
                    var message = NormalizeApiMessage(await response.Content.ReadAsStringAsync());
                    TempData["RegisterSuccess"] = string.IsNullOrWhiteSpace(message) ? "Xác minh email thành công. Bạn có thể đăng nhập." : message;
                    return RedirectToAction("Index", "Login");
                }

                var error = NormalizeApiMessage(await response.Content.ReadAsStringAsync());
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(error) ? "OTP không hợp lệ." : error);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API. Hãy chạy SmartCar.WebApi tại cổng 7060.");
            }
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailOtp(Guid? registrationAttemptId, string username, string email)
        {
            var dto = new ResendEmailOtpDto { RegistrationAttemptId = registrationAttemptId, Username = username, Email = email };
            if (!TryValidateModel(dto))
            {
                TempData["VerifyEmailError"] = "Email không hợp lệ.";
                return RedirectToAction(nameof(VerifyEmail), new { attemptId = registrationAttemptId, username, email });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("api/Registers/resend-email-otp", content);
                var message = NormalizeApiMessage(await response.Content.ReadAsStringAsync());
                if (response.IsSuccessStatusCode) TempData["VerifyEmailSuccess"] = string.IsNullOrWhiteSpace(message) ? "Đã gửi lại OTP." : message;
                else TempData["VerifyEmailError"] = string.IsNullOrWhiteSpace(message) ? "Không gửi lại được OTP." : message;
            }
            catch (HttpRequestException)
            {
                TempData["VerifyEmailError"] = "Không kết nối được Web API.";
            }
            return RedirectToAction(nameof(VerifyEmail), new { attemptId = registrationAttemptId, username, email });
        }
    }
}
