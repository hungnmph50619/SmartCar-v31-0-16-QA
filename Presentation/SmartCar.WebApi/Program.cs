using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.Json;
using SmartCar.Application.Features.CQRS.Handlers.AboutHandlers;
using SmartCar.Application.Features.CQRS.Handlers.BannerHandlers;
using SmartCar.Application.Features.CQRS.Handlers.BrandHandlers;
using SmartCar.Application.Features.CQRS.Handlers.CarHandlers;
using SmartCar.Application.Features.CQRS.Handlers.CategoryHandlers;
using SmartCar.Application.Features.CQRS.Handlers.ContactHandlers;
using SmartCar.Application.Features.RepositoryPattern;
using SmartCar.Application.Interfaces;
using SmartCar.Application.Interfaces.BlogInterfaces;
using SmartCar.Application.Interfaces.CarDescriptionInterfaces;
using SmartCar.Application.Interfaces.CarFeatureInterfaces;
using SmartCar.Application.Interfaces.CarInterfaces;
using SmartCar.Application.Interfaces.CarPricingInterfaces;
using SmartCar.Application.Interfaces.RentACarInterfaces;
using SmartCar.Application.Interfaces.ReviewInterfaces;
using SmartCar.Application.Interfaces.ReservationInterfaces;
using SmartCar.Application.Interfaces.StatisticsInterfaces;
using SmartCar.Application.Interfaces.TagCloudInterfaces;
using SmartCar.Application.Services;
using SmartCar.Application.Tools;
using SmartCar.Application.Validators.ReviewValidators;
using SmartCar.Domain.Entities;
using SmartCar.Persistence.Context;
using SmartCar.Persistence.Repositories;
using SmartCar.Persistence.Repositories.BlogRepositories;
using SmartCar.Persistence.Repositories.CarDescriptionRepositories;
using SmartCar.Persistence.Repositories.CarFeatureRepositories;
using SmartCar.Persistence.Repositories.CarPricingRepositories;
using SmartCar.Persistence.Repositories.CarRepositories;
using SmartCar.Persistence.Repositories.CommentRepositories;
using SmartCar.Persistence.Repositories.RentACarRepositories;
using SmartCar.Persistence.Repositories.ReviewRepositories;
using SmartCar.Persistence.Repositories.ReservationRepositories;
using SmartCar.Persistence.Repositories.StatisticsRepositories;
using SmartCar.Persistence.Repositories.TagCloudRepositories;
using SmartCar.WebApi.Hubs;
using SmartCar.WebApi.BackgroundServices;
using SmartCar.WebApi.Services;
using SmartCar.WebApi.HealthChecks;
using SmartCar.Domain.SystemInfo;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddHttpClient();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton(TimeProvider.System);
var authPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:AuthPermitLimit")
    ?? (builder.Environment.IsDevelopment() ? 50 : 10);
var uploadPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:UploadPermitLimit")
    ?? (builder.Environment.IsDevelopment() ? 60 : 20);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("upload", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = uploadPermitLimit,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]?.Trim();
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("__SET_BY_", StringComparison.Ordinal) || jwtKey.Length < 64)
    throw new InvalidOperationException("Jwt:Key phải được cấu hình bằng biến môi trường hoặc User Secrets và dài tối thiểu 64 ký tự.");
var otpKey = builder.Configuration["Security:OtpHmacKey"]?.Trim();
if (string.IsNullOrWhiteSpace(otpKey) || otpKey.StartsWith("__SET_BY_", StringComparison.Ordinal) || otpKey.Length < 64)
    throw new InvalidOperationException("Security:OtpHmacKey phải được cấu hình an toàn và dài tối thiểu 64 ký tự.");
var identityKey = builder.Configuration["Security:IdentityHmacKey"]?.Trim();
if (string.IsNullOrWhiteSpace(identityKey) || identityKey.StartsWith("__SET_BY_", StringComparison.Ordinal) || identityKey.Length < 64)
    throw new InvalidOperationException("Security:IdentityHmacKey phải được cấu hình an toàn và dài tối thiểu 64 ký tự.");
JwtTokenDefaults.Configure(
    jwtSection["Issuer"] ?? "https://localhost",
    jwtSection["Audience"] ?? "https://localhost",
    jwtKey,
    int.TryParse(jwtSection["ExpireDays"], out var expireDays) ? expireDays : 5);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection chưa được cấu hình.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:7154", "http://localhost:5008" };

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSignalR();
builder.Services.AddHostedService<DataLifecycleBackgroundService>();
builder.Services.AddHostedService<EmailOutboxBackgroundService>();
builder.Services.AddScoped<IPrivateFileService, PrivateFileService>();
builder.Services.AddHostedService<PrivateFileCleanupService>();
builder.Services.AddHostedService<PublicFileDeletionBackgroundService>();
builder.Services.AddScoped<IReservationCancellationService, ReservationCancellationService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IUserAnonymizationService, UserAnonymizationService>();
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("SmartCar WebApi đang chạy."), tags: new[] { "live" })
    .AddCheck<DatabaseVersionHealthCheck>("database-version", tags: new[] { "ready" });


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidAudience = JwtTokenDefaults.ValidAudience,
        ValidIssuer = JwtTokenDefaults.ValidIssuer,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTokenDefaults.Key)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.NameIdentifier,
        ValidateLifetime = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true
    };
    opt.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var versionValue = context.Principal?.FindFirst("token_version")?.Value;
            if (!int.TryParse(idValue, out var userId) || !int.TryParse(versionValue, out var tokenVersion))
            {
                context.Fail("Phiên đăng nhập không hợp lệ.");
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<CarBookContext>();
            var user = await db.AppUsers.IgnoreQueryFilters().AsNoTracking()
                .Where(x => x.AppUserId == userId)
                .Select(x => new { x.IsActive, x.IsDeleted, x.TokenVersion, x.LockoutEnd })
                .FirstOrDefaultAsync();
            if (user is null || !user.IsActive || user.IsDeleted || user.TokenVersion != tokenVersion || (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow))
                context.Fail("Phiên đăng nhập đã bị thu hồi hoặc tài khoản đã bị khóa.");
        }
    };
});


#region Registirations
// Add services to the container.
// Bản demo local mặc định tắt SQL retry. Mã nguồn còn nhiều nghiệp vụ cũ tự mở
// transaction; bật retry trước khi bọc toàn bộ transaction bằng execution strategy
// sẽ gây lỗi 500 "does not support user-initiated transactions".
var enableSqlRetry = builder.Configuration.GetValue<bool>("Database:EnableRetryOnFailure");
builder.Services.AddDbContext<CarBookContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        if (enableSqlRetry)
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }
    });
});
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(ICarRepository), typeof(CarRepository));
builder.Services.AddScoped(typeof(IStatisticsRepository), typeof(StatisticsRepository));
builder.Services.AddScoped(typeof(IBlogRepository), typeof(BlogRepository));
builder.Services.AddScoped(typeof(ICarPricingRepository), typeof(CarPricingRepository));
builder.Services.AddScoped(typeof(ITagCloudRepository), typeof(TagCloudRepository));
builder.Services.AddScoped(typeof(IRentACarRepository), typeof(RentACarRepository));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(CommentRepository<>));
builder.Services.AddScoped(typeof(ICarFeatureRepository), typeof(CarFeatureRepository));
builder.Services.AddScoped(typeof(ICarDescriptionRepository), typeof(CarDescriptionRepository));
builder.Services.AddScoped(typeof(IReviewRepository), typeof(ReviewRepository));
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();


builder.Services.AddScoped<GetAboutQueryHandler>();
builder.Services.AddScoped<GetAboutByIdQueryHandler>();
builder.Services.AddScoped<CreateAboutCommandHandler>();
builder.Services.AddScoped<UpdateAboutCommandHandler>();
builder.Services.AddScoped<RemoveAboutCommandHandler>();

builder.Services.AddScoped<GetBannerQueryHandler>();
builder.Services.AddScoped<GetBannerByIdQueryHandler>();
builder.Services.AddScoped<CreateBannerCommandHandler>();
builder.Services.AddScoped<UpdateBannerCommandHandler>();
builder.Services.AddScoped<RemoveBannerCommandHandler>();

builder.Services.AddScoped<GetBrandQueryHandler>();
builder.Services.AddScoped<GetBrandByIdQueryHandler>();
builder.Services.AddScoped<CreateBrandCommandHandler>();
builder.Services.AddScoped<UpdateBrandCommandHandler>();
builder.Services.AddScoped<RemoveBrandCommandHandler>();

builder.Services.AddScoped<GetCarQueryHandler>();
builder.Services.AddScoped<GetCarByIdQueryHandler>();
builder.Services.AddScoped<CreateCarCommandHandler>();
builder.Services.AddScoped<UpdateCarCommandHandler>();
builder.Services.AddScoped<RemoveCarCommandHandler>();
builder.Services.AddScoped<GetCarWithBrandQueryHandler>();
builder.Services.AddScoped<GetLast5CarsWithBrandQueryHandler>();

builder.Services.AddScoped<GetCategoryQueryHandler>();
builder.Services.AddScoped<GetCategoryByIdQueryHandler>();
builder.Services.AddScoped<CreateCategoryCommandHandler>();
builder.Services.AddScoped<UpdateCategoryCommandHandler>();
builder.Services.AddScoped<RemoveCategoryCommandHandler>();

builder.Services.AddScoped<GetContactQueryHandler>();
builder.Services.AddScoped<GetContactByIdQueryHandler>();
builder.Services.AddScoped<CreateContactCommandHandler>();
builder.Services.AddScoped<UpdateContactCommandHandler>();
builder.Services.AddScoped<RemoveContactCommandHandler>();

#endregion

builder.Services.AddApplicationService(builder.Configuration);

builder.Services.AddControllers(options =>
{
    // Không để ASP.NET tự bắt buộc mọi string không nullable.
    // DTO xác minh đối tác dùng validate có điều kiện theo loại đối tác.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
}).AddFluentValidation(x =>
{
    x.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
});

// Không trả thông báo ModelState mặc định bằng tiếng Anh ra giao diện.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(new
        {
            message = "Dữ liệu gửi lên chưa hợp lệ. Vui lòng kiểm tra lại các trường đã nhập."
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var traceId = context.TraceIdentifier;
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var isConcurrency = exception is DbUpdateConcurrencyException;
        context.Response.StatusCode = isConcurrency
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = isConcurrency
                ? "Dữ liệu vừa được xử lý bởi yêu cầu khác. Vui lòng tải lại và thử lại."
                : "Không thể xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ quản trị viên.",
            errorCode = isConcurrency ? "SC-API-409-CONCURRENCY" : "SC-API-500",
            traceId
        });
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";
    var payload = new
    {
        status = report.Status.ToString(),
        applicationVersion = SmartCarRelease.ApplicationVersion,
        databaseVersionRequired = SmartCarRelease.DatabaseVersion,
        durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
        checks = report.Entries.ToDictionary(
            x => x.Key,
            x => new
            {
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                durationMs = Math.Round(x.Value.Duration.TotalMilliseconds, 2),
                data = x.Value.Data
            })
    };
    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Staff" });

app.MapControllers();
app.MapHub<CarHub>("/carhub");

app.Run();

public partial class Program { }