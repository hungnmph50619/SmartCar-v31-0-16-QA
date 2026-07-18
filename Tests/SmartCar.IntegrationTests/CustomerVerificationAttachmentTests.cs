using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SmartCar.Domain.Entities;
using SmartCar.Dto.ReservationDtos;
using SmartCar.Persistence.Repositories.ReservationRepositories;
using SmartCar.WebApi.Controllers;
using SmartCar.WebApi.Services;
using System.Security.Claims;

namespace SmartCar.IntegrationTests;

public class CustomerVerificationAttachmentTests
{
    [Fact]
    public async Task SubmitVerification_MarksAllNewFilesAttached_ToPreventCleanupDeletion()
    {
        await using var db = TestDatabase.Create();
        db.AppRoles.Add(new AppRole { AppRoleId = 3, AppRoleName = "Customer", AppUsers = new List<AppUser>() });
        db.AppUsers.Add(new AppUser
        {
            AppUserId = 10,
            AppRoleId = 3,
            Username = "customer10",
            Password = "hash",
            Name = "An",
            Surname = "Nguyen",
            Email = "customer10@example.test",
            EmailConfirmed = true,
            IsActive = true
        });
        db.AdministrativeProvinces.Add(new AdministrativeProvince
        {
            ProvinceCode = "01",
            ProvinceName = "Hà Nội",
            ProvinceType = "Thành phố",
            IsActive = true
        });
        db.AdministrativeWards.Add(new AdministrativeWard
        {
            WardCode = "00166",
            ProvinceCode = "01",
            WardName = "Cầu Giấy",
            WardType = "Phường",
            IsActive = true
        });

        var ids = new Dictionary<string, Guid>
        {
            ["CustomerCitizenIdFront"] = Guid.NewGuid(),
            ["CustomerCitizenIdBack"] = Guid.NewGuid(),
            ["CustomerDriverLicense"] = Guid.NewGuid(),
            ["CustomerPortrait"] = Guid.NewGuid()
        };
        foreach (var pair in ids)
        {
            db.PrivateFiles.Add(new PrivateFile
            {
                PrivateFileID = pair.Value,
                OwnerAppUserID = 10,
                Category = pair.Key,
                OriginalFileName = pair.Key + ".jpg",
                StoredFileName = pair.Value.ToString("N") + ".jpg",
                ContentType = "image/jpeg",
                FileSize = 100,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            });
        }
        await db.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:IdentityHmacKey"] = new string('K', 80)
            })
            .Build();
        var files = new PrivateFileService(db, new FakeWebHostEnvironment());
        var controller = new OperationsReadController(db, configuration, files, new ReservationRepository(db), new SystemSettingService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "10"),
                        new Claim(ClaimTypes.Role, "Customer"),
                        new Claim("IsVehiclePartner", "false")
                    }, "TestAuth"))
                }
            }
        };

        var today = SmartCar.Domain.Time.VietnamTime.Today;
        var result = await controller.SubmitVerification(new SubmitVerificationDto
        {
            Phone = "0900000010",
            LegalFullName = "Nguyen Van An",
            Gender = "Nam",
            CitizenIdAddress = "Hà Nội",
            PermanentProvinceCode = "01",
            PermanentWardCode = "00166",
            PermanentDetail = "Số 1",
            CurrentAddressSameAsPermanent = true,
            DriverLicenseNumber = "GPLX0010",
            DriverLicenseClass = "B2",
            CitizenIdIssuedDate = today.AddYears(-2),
            CitizenIdExpiryDate = today.AddYears(8),
            CitizenIdFrontFileId = ids["CustomerCitizenIdFront"],
            CitizenIdBackFileId = ids["CustomerCitizenIdBack"],
            DriverLicenseFileId = ids["CustomerDriverLicense"],
            PortraitFileId = ids["CustomerPortrait"],
            DateOfBirth = today.AddYears(-25),
            DriverLicenseIssuedDate = today.AddYears(-2),
            DriverLicenseExpiry = today.AddYears(3),
            CitizenIdentityNumber = "012345678901"
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var verification = Assert.Single(db.UserVerifications);
        Assert.Equal("01", verification.PermanentProvinceCode);
        Assert.Equal("00166", verification.PermanentWardCode);
        Assert.Equal("Thành phố Hà Nội", verification.PermanentProvince);
        Assert.Equal("Phường Cầu Giấy", verification.PermanentWard);
        Assert.Equal("Số 1, Phường Cầu Giấy, Thành phố Hà Nội", verification.PermanentAddress);
        Assert.Equal(verification.PermanentProvinceCode, verification.CurrentProvinceCode);
        Assert.Equal(verification.PermanentWardCode, verification.CurrentWardCode);
        var attached = db.PrivateFiles.ToList();
        Assert.Equal(4, attached.Count);
        Assert.All(attached, file =>
        {
            Assert.Equal(nameof(UserVerification), file.AttachedEntityType);
            Assert.Equal(verification.UserVerificationID.ToString(), file.AttachedEntityID);
            Assert.NotNull(file.AttachedDate);
            Assert.False(file.IsDeleted);
        });
        Assert.DoesNotContain(attached, file => file.AttachedDate == null && file.CreatedDate < DateTime.UtcNow.AddHours(-24));
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SmartCar.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), $"smartcar-tests-{Guid.NewGuid():N}");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
