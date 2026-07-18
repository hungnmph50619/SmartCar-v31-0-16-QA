using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SmartCar.WebUI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllersWithViews(options =>
{
    // Tự động kiểm tra anti-forgery token cho mọi POST/PUT/PATCH/DELETE của WebUI.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Không để ASP.NET tự bắt buộc mọi string không nullable.
    // Form xác minh đối tác có nhiều nhóm trường hiển thị theo loại đối tác.
    // Nếu chọn Cá nhân thì các trường Doanh nghiệp/Tổ chức có thể để trống và ngược lại.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "SmartCar.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7060/";
if (!apiBaseUrl.EndsWith('/')) apiBaseUrl += "/";
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<UserAccessTokenHandler>();
builder.Services.AddHttpClient(Options.DefaultName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<UserAccessTokenHandler>();
// Client riêng cho OpenStreetMap/Nominatim. Không gắn access token của SmartCar ra dịch vụ ngoài.
builder.Services.AddHttpClient("Geocoding", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartCar-SD46/31.0.16 (student demo project)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt =>
    {
        opt.LoginPath = "/Login/Index/";
        opt.LogoutPath = "/Login/LogOut/";
        opt.AccessDeniedPath = "/Login/AccessDenied/";
        opt.Cookie.SameSite = SameSiteMode.Strict;
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opt.Cookie.Name = "SmartCar.Auth";
        opt.ExpireTimeSpan = TimeSpan.FromDays(5);
        opt.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var vietnamCulture = new CultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = vietnamCulture;
CultureInfo.DefaultThreadCurrentUICulture = vietnamCulture;

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
    await next();
});

app.UseHttpsRedirection();

// Chặn hồ sơ xác minh cũ từng nằm trong wwwroot; tệp mới được phục vụ qua SecureFilesController.
app.Use(async (context, next) =>
{
    var blockedPublicUploadRoots = new[]
    {
        "/uploads/verifications",
        "/uploads/operations",
        "/uploads/vehicle-partners"
    };
    if (blockedPublicUploadRoots.Any(root => context.Request.Path.StartsWithSegments(root, StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    var isStaffAccount = context.User.IsInRole("Staff");
    var isVehiclePartner = string.Equals(context.User.FindFirst("IsVehiclePartner")?.Value, "true", StringComparison.OrdinalIgnoreCase);

    // Trang chi tiết đơn được dùng chung cho khách, chủ xe, nhân viên và admin.
    // Các thao tác POST phát sinh từ trang này (xác nhận cọc, đối soát, xử lý sự cố...)
    // cũng phải được đi tiếp tới MVC controller. Trước đây middleware chỉ cho phép URL
    // /ReservationLookup/Details, nên POST /ReservationLookup/ConfirmPayment bị chuyển hướng
    // thẳng về StaffDashboard trước khi action chạy; vì vậy bấm OK không cập nhật CSDL.
    var isSharedReservationDetail = path.StartsWith("/ReservationLookup/Details", StringComparison.OrdinalIgnoreCase);
    var isReservationOperation =
        !HttpMethods.IsGet(context.Request.Method) &&
        path.StartsWith("/ReservationLookup/", StringComparison.OrdinalIgnoreCase);
    var allowSharedReservationFlow = isSharedReservationDetail || isReservationOperation;

    if (context.User.Identity?.IsAuthenticated == true && isStaffAccount)
    {
        var customerFacingPrefixes = new[]
        {
            "/Default", "/About", "/Service", "/CarPricing", "/Car/", "/Blog", "/Contact",
            "/ReservationLookup", "/VehiclePartner", "/RentACarList", "/Reservation"
        };
        if (!allowSharedReservationFlow && (path == "/" || customerFacingPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
        {
            context.Response.Redirect("/StaffDashboard/Index");
            return;
        }
    }

    if (context.User.Identity?.IsAuthenticated == true && isVehiclePartner)
    {
        var publicCustomerPrefixes = new[]
        {
            "/Default", "/About", "/Service", "/CarPricing", "/Car/", "/Blog", "/Contact", "/ReservationLookup"
        };
        if (!allowSharedReservationFlow && (path == "/" || publicCustomerPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
        {
            context.Response.Redirect("/VehiclePartner/Dashboard");
            return;
        }
    }

    if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"/Login/Index?returnUrl={returnUrl}");
            return;
        }

        var isAdmin = context.User.IsInRole("Admin");
        var isStaff = context.User.IsInRole("Staff");
        if (!isAdmin && !isStaff)
        {
            context.Response.Redirect("/Login/AccessDenied");
            return;
        }

        if (isStaff)
        {
            var staffAllowedPrefixes = new[]
            {
                "/Admin/AdminReservation",
                "/Admin/AdminMarketplace",
                "/Admin/AdminVehiclePartner"
            };
            if (!staffAllowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.Redirect("/Login/AccessDenied");
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Default}/{action=Index}/{id?}");

app.Run();
