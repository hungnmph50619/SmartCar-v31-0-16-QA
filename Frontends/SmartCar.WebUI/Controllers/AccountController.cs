using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SmartCar.Dto.AccountDtos;
using SmartCar.Dto.ReservationDtos;
using SmartCar.WebUI.Models;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace SmartCar.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AccountController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordDto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var response = await _httpClientFactory.CreateClient().PostAsync(
                    "api/Account/forgot-password", JsonContent(dto));
                var raw = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, Clean(raw));
                    return View(dto);
                }
                var result = JsonConvert.DeserializeObject<ForgotPasswordResponseDto>(raw);
                ViewBag.Message = result?.Message;
                ViewBag.DevelopmentResetUrl = result?.DevelopmentResetUrl;
                return View(new ForgotPasswordDto());
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API tại cổng 7060.");
                return View(dto);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string username, string email, string token)
            => View(new ResetPasswordDto { Username = username ?? string.Empty, Email = email ?? string.Empty, Token = token ?? string.Empty });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var response = await _httpClientFactory.CreateClient().PostAsync(
                    "api/Account/reset-password", JsonContent(dto));
                var message = Clean(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, message);
                    return View(dto);
                }
                TempData["PasswordResetSuccess"] = message;
                return RedirectToAction("Index", "Login");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không kết nối được Web API tại cổng 7060.");
                return View(dto);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/Account/me");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["AccountError"] = Clean(await response.Content.ReadAsStringAsync());
                    return RedirectToRoleHome();
                }
                var profile = JsonConvert.DeserializeObject<UserProfileDto>(await response.Content.ReadAsStringAsync()) ?? new UserProfileDto();
                var model = new AccountProfileViewModel
                {
                    Profile = profile,
                    CustomerReadiness = User.IsInRole("Customer") && !profile.IsVehiclePartner
                        ? await LoadCustomerReadinessAsync()
                        : null
                };
                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["AccountError"] = "Không kết nối được Web API tại cổng 7060.";
                return RedirectToRoleHome();
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateUserProfileDto dto)
        {
            if (User.IsInRole("Staff"))
            {
                TempData["AccountError"] = "Tài khoản nhân viên chỉ được xem hồ sơ và đổi mật khẩu. Vui lòng liên hệ quản trị viên nếu cần cập nhật thông tin.";
                return RedirectToAction(nameof(Profile));
            }

            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var response = await CreateAuthorizedClient().PutAsync("api/Account/me", JsonContent(dto));
                var raw = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    TempData["AccountError"] = Clean(raw);
                    return RedirectToAction(nameof(Profile));
                }
                var profile = JsonConvert.DeserializeObject<UserProfileDto>(raw);
                if (profile is not null) await RefreshLocalClaimsAsync(profile);
                TempData["AccountSuccess"] = "Đã cập nhật thông tin cá nhân.";
            }
            catch (HttpRequestException)
            {
                TempData["AccountError"] = "Không kết nối được Web API tại cổng 7060.";
            }
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmailChange(RequestEmailChangeDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var response = await CreateAuthorizedClient().PostAsync("api/Account/request-email-change", JsonContent(dto));
                var message = Clean(await response.Content.ReadAsStringAsync());
                TempData[response.IsSuccessStatusCode ? "AccountSuccess" : "AccountError"] = message;
            }
            catch (HttpRequestException) { TempData["AccountError"] = "Không kết nối được Web API tại cổng 7060."; }
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmailChange(ConfirmEmailChangeDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var response = await CreateAuthorizedClient().PostAsync("api/Account/confirm-email-change", JsonContent(dto));
                var message = Clean(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    TempData["AccountError"] = message;
                    return RedirectToAction(nameof(Profile));
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["PasswordResetSuccess"] = message;
                return RedirectToAction("Index", "Login");
            }
            catch (HttpRequestException)
            {
                TempData["AccountError"] = "Không kết nối được Web API tại cổng 7060.";
                return RedirectToAction(nameof(Profile));
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            try
            {
                var response = await CreateAuthorizedClient().PutAsync("api/Account/change-password", JsonContent(dto));
                var message = Clean(await response.Content.ReadAsStringAsync());
                if (!response.IsSuccessStatusCode)
                {
                    TempData["AccountError"] = message;
                    return RedirectToAction(nameof(Profile));
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["PasswordResetSuccess"] = message;
                return RedirectToAction("Index", "Login");
            }
            catch (HttpRequestException)
            {
                TempData["AccountError"] = "Không kết nối được Web API tại cổng 7060.";
                return RedirectToAction(nameof(Profile));
            }
        }

        private IActionResult RedirectToRoleHome()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
            if (User.IsInRole("Staff"))
                return RedirectToAction("Index", "StaffDashboard");
            if (string.Equals(User.FindFirst("IsVehiclePartner")?.Value, "true", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "VehiclePartner");
            return RedirectToAction("Index", "Default");
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = User.FindFirst("carbooktoken")?.Value;
            if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<CustomerReadinessDto?> LoadCustomerReadinessAsync()
        {
            try
            {
                var response = await CreateAuthorizedClient().GetAsync("api/operations/customer/readiness");
                if (!response.IsSuccessStatusCode) return null;

                var raw = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CustomerReadinessDto>(raw);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static StringContent JsonContent(object value) => new(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");

        private async Task RefreshLocalClaimsAsync(UserProfileDto profile)
        {
            var claims = User.Claims.Where(x => x.Type != ClaimTypes.Name && x.Type != ClaimTypes.Email).ToList();
            claims.Add(new Claim(ClaimTypes.Name, profile.FullName));
            claims.Add(new Claim(ClaimTypes.Email, profile.Email));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
        }

        private static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Không thể xử lý yêu cầu.";
            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(raw);
                var message = (string?)obj?.message;
                if (!string.IsNullOrWhiteSpace(message)) return message;
            }
            catch (JsonException) { }
            return raw.Trim().Trim('"');
        }
    }
}
