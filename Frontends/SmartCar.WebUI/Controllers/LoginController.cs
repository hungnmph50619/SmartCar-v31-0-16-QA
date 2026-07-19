using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SmartCar.Dto.LoginDtos;
using SmartCar.WebUI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SmartCar.WebUI.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CreateLoginDto createLoginDto, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(createLoginDto);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(
                    JsonSerializer.Serialize(createLoginDto),
                    Encoding.UTF8,
                    "application/json");
                var response = await client.PostAsync("api/Login", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var message = responseBody.Trim().Trim('"');
                    try
                    {
                        using var errorJson = JsonDocument.Parse(responseBody);
                        var root = errorJson.RootElement;
                        if (root.TryGetProperty("message", out var messageValue))
                            message = messageValue.GetString() ?? message;
                        if (root.TryGetProperty("remainingAttempts", out var attemptsValue) &&
                            attemptsValue.ValueKind == JsonValueKind.Number &&
                            attemptsValue.TryGetInt32(out var remainingAttempts))
                            ViewBag.RemainingAttempts = remainingAttempts;
                        if (root.TryGetProperty("lockoutEnd", out var lockoutValue) &&
                            lockoutValue.ValueKind == JsonValueKind.String)
                            ViewBag.LockoutEnd = lockoutValue.GetString();
                    }
                    catch (JsonException) { }

                    if (!string.IsNullOrWhiteSpace(message) &&
                        message.Contains("chưa xác minh email", StringComparison.OrdinalIgnoreCase))
                    {
                        ViewBag.ShowVerifyEmailLink = true;
                    }
                    ModelState.AddModelError(string.Empty,
                        string.IsNullOrWhiteSpace(message) ? "Tên đăng nhập hoặc mật khẩu không đúng." : message);
                    return View(createLoginDto);
                }

                var jsonData = await response.Content.ReadAsStringAsync();
                var tokenModel = JsonSerializer.Deserialize<JwtResponseModel>(jsonData,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (string.IsNullOrWhiteSpace(tokenModel?.Token))
                {
                    ModelState.AddModelError(string.Empty, "Web API không trả về mã đăng nhập hợp lệ.");
                    return View(createLoginDto);
                }

                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(tokenModel.Token);
                var claims = token.Claims.ToList();
                claims.Add(new Claim("carbooktoken", tokenModel.Token));

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        ExpiresUtc = tokenModel.ExpireDate,
                        IsPersistent = true,
                        AllowRefresh = true
                    });

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                var role = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role || x.Type == "role")?.Value;
                var isVehiclePartner = claims.Any(x => x.Type == "IsVehiclePartner" && string.Equals(x.Value, "true", StringComparison.OrdinalIgnoreCase));
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
                }
                if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "StaffDashboard");
                }
                if (isVehiclePartner)
                {
                    return RedirectToAction("Dashboard", "VehiclePartner");
                }
                if (string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Index", "Verification");
                }
                return RedirectToAction("Index", "Default");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty,
                    "Hệ thống đăng nhập đang tạm thời gián đoạn. Vui lòng thử lại sau ít phút.");
                return View(createLoginDto);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Không thể đăng nhập. Vui lòng thử lại.");
                return View(createLoginDto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Default");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
