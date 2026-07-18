using Microsoft.EntityFrameworkCore;
using SmartCar.Domain.Entities;

namespace SmartCar.Persistence.Context
{
    public class CarBookContext : DbContext
    {
        public CarBookContext(DbContextOptions<CarBookContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppRole> AppRoles { get; set; }
        public DbSet<About> Abouts { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<CarDescription> CarDescriptions { get; set; }
        public DbSet<CarFeature> CarFeatures { get; set; }
        public DbSet<CarPricing> CarPricings { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<FooterAddress> FooterAddresses { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<SocialMedia> SocialMedias { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<TagCloud> TagClouds { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<RentACar> RentACars { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationStatusHistory> ReservationStatusHistories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EmailVerificationOtp> EmailVerificationOtps { get; set; }
        public DbSet<CompanyAnnouncement> CompanyAnnouncements { get; set; }
        public DbSet<PlatformFeeSetting> PlatformFeeSettings { get; set; }
        public DbSet<VehiclePartnerProfile> VehiclePartnerProfiles { get; set; }
        public DbSet<VehiclePartnerApplication> VehiclePartnerApplications { get; set; }
        public DbSet<PartnerVehicle> PartnerVehicles { get; set; }
        public DbSet<CommissionTransaction> CommissionTransactions { get; set; }
        public DbSet<UserVerification> UserVerifications { get; set; }
        public DbSet<AdministrativeProvince> AdministrativeProvinces { get; set; }
        public DbSet<AdministrativeWard> AdministrativeWards { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<HandoverReport> HandoverReports { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<DataChangeHistory> DataChangeHistories { get; set; }
        public DbSet<DataRetentionPolicy> DataRetentionPolicies { get; set; }
        public DbSet<ArchivedRecord> ArchivedRecords { get; set; }
        public DbSet<VehicleDocument> VehicleDocuments { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<TrafficFine> TrafficFines { get; set; }
        public DbSet<DepositTransaction> DepositTransactions { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<HandoverImage> HandoverImages { get; set; }
        public DbSet<DisputeMessage> DisputeMessages { get; set; }
        public DbSet<AdditionalCharge> AdditionalCharges { get; set; }
        public DbSet<FraudFlag> FraudFlags { get; set; }
        public DbSet<WorkItemClaim> WorkItemClaims { get; set; }
        public DbSet<StaffOperationalIssue> StaffOperationalIssues { get; set; }
        public DbSet<PrivateFile> PrivateFiles { get; set; }
        public DbSet<EmailOutbox> EmailOutboxes { get; set; }
        public DbSet<PublicFileDeletionJob> PublicFileDeletionJobs { get; set; }
        public DbSet<SystemVersion> SystemVersions { get; set; }
        public DbSet<RegistrationAttempt> RegistrationAttempts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<DriverProfile> DriverProfiles { get; set; }
        public DbSet<BookingDriverAssignment> BookingDriverAssignments { get; set; }
        public DbSet<VehiclePricingPlan> VehiclePricingPlans { get; set; }
        public DbSet<VehicleAvailabilityBlock> VehicleAvailabilityBlocks { get; set; }
        public DbSet<BankAccountChangeRequest> BankAccountChangeRequests { get; set; }
        public DbSet<RefundTransaction> RefundTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.PickUpLocation)
                .WithMany(y => y.PickUpReservation)
                .HasForeignKey(z => z.PickUpLocationID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.DropOffLocation)
                .WithMany(y => y.DropOffReservation)
                .HasForeignKey(z => z.DropOffLocationID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.CustomerAppUser)
                .WithMany()
                .HasForeignKey(x => x.CustomerAppUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.PartnerVehicle)
                .WithMany()
                .HasForeignKey(x => x.PartnerVehicleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasIndex(x => new { x.CarID, x.PickUpDate, x.DropOffDate });

            modelBuilder.Entity<Reservation>()
                .HasIndex(x => new { x.PartnerVehicleID, x.Status });

            modelBuilder.Entity<ReservationStatusHistory>()
                .HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationStatusHistory>()
                .HasOne(x => x.ChangedByAppUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByAppUserID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(x => x.TokenHash)
                .IsUnique();

            modelBuilder.Entity<EmailVerificationOtp>()
                .HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmailVerificationOtp>()
                .HasIndex(x => new { x.AppUserID, x.Purpose, x.UsedDate, x.ExpiresDate });

            modelBuilder.Entity<CompanyAnnouncement>()
                .HasIndex(x => new { x.AudienceRole, x.IsActive, x.PublishDate });

            modelBuilder.Entity<PlatformFeeSetting>()
                .HasOne(x => x.UpdatedByAppUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByAppUserID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne(x => x.AppUser)
                .WithOne()
                .HasForeignKey<VehiclePartnerProfile>(x => x.AppUserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasIndex(x => x.AppUserID)
                .IsUnique();

            modelBuilder.Entity<VehiclePartnerApplication>()
                .HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehiclePartnerApplication>()
                .HasOne(x => x.Location)
                .WithMany()
                .HasForeignKey(x => x.LocationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehiclePartnerApplication>()
                .HasIndex(x => x.LicensePlate)
                .IsUnique()
                .HasFilter("[Status] <> N'Từ chối'");

            modelBuilder.Entity<PartnerVehicle>()
                .HasOne(x => x.Car)
                .WithOne()
                .HasForeignKey<PartnerVehicle>(x => x.CarID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartnerVehicle>()
                .HasOne(x => x.OwnerAppUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerAppUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartnerVehicle>()
                .HasOne(x => x.VehiclePartnerApplication)
                .WithOne()
                .HasForeignKey<PartnerVehicle>(x => x.VehiclePartnerApplicationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PartnerVehicle>()
                .HasIndex(x => x.CarID)
                .IsUnique();

            modelBuilder.Entity<PartnerVehicle>()
                .HasIndex(x => x.VehiclePartnerApplicationID)
                .IsUnique();

            modelBuilder.Entity<CommissionTransaction>()
                .HasOne(x => x.Reservation)
                .WithOne()
                .HasForeignKey<CommissionTransaction>(x => x.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionTransaction>()
                .HasOne(x => x.Settlement)
                .WithOne()
                .HasForeignKey<CommissionTransaction>(x => x.SettlementID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionTransaction>()
                .HasOne(x => x.PartnerVehicle)
                .WithMany()
                .HasForeignKey(x => x.PartnerVehicleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionTransaction>()
                .HasOne(x => x.PartnerAppUser)
                .WithMany()
                .HasForeignKey(x => x.PartnerAppUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionTransaction>()
                .HasIndex(x => x.ReservationID)
                .IsUnique();
            modelBuilder.Entity<CommissionTransaction>()
                .HasIndex(x => x.SettlementID)
                .IsUnique();

            modelBuilder.Entity<AdministrativeProvince>(entity =>
            {
                entity.HasKey(x => x.ProvinceCode);
                entity.Property(x => x.ProvinceCode).HasMaxLength(2).IsUnicode(false);
                entity.HasIndex(x => new { x.IsActive, x.ProvinceName })
                    .HasDatabaseName("IX_AdministrativeProvinces_IsActive_ProvinceName");
            });

            modelBuilder.Entity<AdministrativeWard>(entity =>
            {
                entity.HasKey(x => x.WardCode);
                entity.Property(x => x.WardCode).HasMaxLength(5).IsUnicode(false);
                entity.Property(x => x.ProvinceCode).HasMaxLength(2).IsUnicode(false);
                entity.HasOne(x => x.Province)
                    .WithMany(x => x.Wards)
                    .HasForeignKey(x => x.ProvinceCode)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_AdministrativeWards_AdministrativeProvinces");
                entity.HasIndex(x => new { x.ProvinceCode, x.IsActive, x.WardName })
                    .HasDatabaseName("IX_AdministrativeWards_ProvinceCode_IsActive_WardName");
            });

            modelBuilder.Entity<UserVerification>().Property(x => x.PermanentProvinceCode).HasMaxLength(2).IsUnicode(false);
            modelBuilder.Entity<UserVerification>().Property(x => x.PermanentWardCode).HasMaxLength(5).IsUnicode(false);
            modelBuilder.Entity<UserVerification>().Property(x => x.CurrentProvinceCode).HasMaxLength(2).IsUnicode(false);
            modelBuilder.Entity<UserVerification>().Property(x => x.CurrentWardCode).HasMaxLength(5).IsUnicode(false);
            modelBuilder.Entity<UserVerification>().HasIndex(x => new { x.AppUserID, x.VerificationType }).IsUnique();
            modelBuilder.Entity<UserVerification>().HasIndex(x => x.CitizenIdFingerprint).IsUnique().HasFilter("[CitizenIdFingerprint] IS NOT NULL");
            modelBuilder.Entity<UserVerification>().HasIndex(x => new { x.PermanentProvinceCode, x.PermanentWardCode })
                .HasDatabaseName("IX_UserVerifications_PermanentAdministrativeCodes");
            modelBuilder.Entity<UserVerification>().HasIndex(x => new { x.CurrentProvinceCode, x.CurrentWardCode })
                .HasDatabaseName("IX_UserVerifications_CurrentAdministrativeCodes");
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.PermanentProvinceCode).HasMaxLength(2).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.PermanentWardCode).HasMaxLength(5).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.CurrentProvinceCode).HasMaxLength(2).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.CurrentWardCode).HasMaxLength(5).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.HeadquartersProvinceCode).HasMaxLength(2).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().Property(x => x.HeadquartersWardCode).HasMaxLength(5).IsUnicode(false);
            modelBuilder.Entity<VehiclePartnerProfile>().HasIndex(x => new { x.PermanentProvinceCode, x.PermanentWardCode })
                .HasDatabaseName("IX_VehiclePartnerProfiles_PermanentAdministrativeCodes");
            modelBuilder.Entity<VehiclePartnerProfile>().HasIndex(x => new { x.CurrentProvinceCode, x.CurrentWardCode })
                .HasDatabaseName("IX_VehiclePartnerProfiles_CurrentAdministrativeCodes");
            modelBuilder.Entity<VehiclePartnerProfile>().HasIndex(x => new { x.HeadquartersProvinceCode, x.HeadquartersWardCode })
                .HasDatabaseName("IX_VehiclePartnerProfiles_HeadquartersAdministrativeCodes");
            modelBuilder.Entity<VehiclePartnerProfile>().HasIndex(x => x.CitizenIdFingerprint).IsUnique().HasFilter("[CitizenIdFingerprint] IS NOT NULL");
            modelBuilder.Entity<Payment>().HasIndex(x => x.TransactionCode).IsUnique().HasFilter("[TransactionCode] IS NOT NULL");
            modelBuilder.Entity<Payment>().HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            modelBuilder.Entity<Payment>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<Payment>()
                .HasIndex(x => new { x.ReservationID, x.PaymentType })
                .IsUnique()
                .HasFilter("[Status] = N'Thành công' AND [PaymentType] IN (N'Tiền cọc', N'Cọc giữ chỗ', N'Cọc bảo đảm', N'Tiền thuê')");
            modelBuilder.Entity<Payment>()
                .HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityID })
                .IsUnique()
                .HasFilter("[Status] = N'Thành công' AND [RelatedEntityType] IS NOT NULL AND [RelatedEntityID] IS NOT NULL");
            modelBuilder.Entity<HandoverReport>().HasIndex(x => new { x.ReservationID, x.ReportType }).IsUnique().HasFilter("[IsSuperseded] = 0");
            modelBuilder.Entity<Dispute>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.EntityName, x.EntityID, x.CreatedDate });
            modelBuilder.Entity<DataChangeHistory>().HasIndex(x => new { x.EntityName, x.EntityID, x.ChangedAt });
            modelBuilder.Entity<DataRetentionPolicy>().HasIndex(x => x.EntityName).IsUnique();
            modelBuilder.Entity<ArchivedRecord>().HasIndex(x => new { x.EntityName, x.EntityID, x.ArchivedAt });


            modelBuilder.Entity<VehicleDocument>().HasIndex(x => new { x.PartnerVehicleID, x.DocumentType });
            modelBuilder.Entity<MaintenanceRecord>().HasIndex(x => new { x.PartnerVehicleID, x.MaintenanceDate });
            modelBuilder.Entity<Incident>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<TrafficFine>().HasIndex(x => x.NoticeNumber).IsUnique().HasFilter("[NoticeNumber] IS NOT NULL");
            modelBuilder.Entity<DepositTransaction>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<DepositTransaction>().HasIndex(x => x.TransactionCode).IsUnique().HasFilter("[TransactionCode] IS NOT NULL");
            modelBuilder.Entity<DepositTransaction>()
                .HasIndex(x => new { x.ReservationID, x.Type })
                .IsUnique()
                .HasFilter("[Status] = N'Hoàn thành'");
            modelBuilder.Entity<Settlement>().HasIndex(x => x.ReservationID).IsUnique();
            modelBuilder.Entity<Settlement>().HasIndex(x => x.PayoutTransactionCode).IsUnique().HasFilter("[PayoutTransactionCode] IS NOT NULL");
            modelBuilder.Entity<Settlement>().HasIndex(x => x.CreationIdempotencyKey).IsUnique().HasFilter("[CreationIdempotencyKey] IS NOT NULL");
            modelBuilder.Entity<Settlement>().HasIndex(x => x.PayoutIdempotencyKey).IsUnique().HasFilter("[PayoutIdempotencyKey] IS NOT NULL");
            modelBuilder.Entity<Notification>().HasIndex(x => new { x.AppUserID, x.IsRead, x.CreatedDate });
            modelBuilder.Entity<HandoverImage>().HasIndex(x => x.HandoverReportID);
            modelBuilder.Entity<DisputeMessage>().HasIndex(x => new { x.DisputeID, x.CreatedDate });
            modelBuilder.Entity<AdditionalCharge>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<AdditionalCharge>().HasIndex(x => x.PaymentID).IsUnique().HasFilter("[PaymentID] IS NOT NULL");
            modelBuilder.Entity<FraudFlag>().HasIndex(x => new { x.Status, x.RiskScore });
            modelBuilder.Entity<WorkItemClaim>().HasIndex(x => new { x.QueueType, x.EntityID }).IsUnique();
            modelBuilder.Entity<WorkItemClaim>().HasIndex(x => new { x.AssignedStaffAppUserID, x.Status });
            modelBuilder.Entity<StaffOperationalIssue>().HasIndex(x => new { x.StaffAppUserID, x.Status, x.CreatedDate });
            modelBuilder.Entity<PrivateFile>()
                .HasOne(x => x.OwnerAppUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerAppUserID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PrivateFile>()
                .HasOne(x => x.Reservation)
                .WithMany()
                .HasForeignKey(x => x.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PrivateFile>()
                .HasOne(x => x.PartnerApplication)
                .WithMany()
                .HasForeignKey(x => x.PartnerApplicationID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PrivateFile>().HasIndex(x => new { x.OwnerAppUserID, x.Category, x.IsDeleted });
            modelBuilder.Entity<PrivateFile>().HasIndex(x => x.ReservationID);
            modelBuilder.Entity<PrivateFile>().HasIndex(x => x.PartnerApplicationID);
            modelBuilder.Entity<PrivateFile>().HasIndex(x => new { x.AttachedEntityType, x.AttachedEntityID, x.AttachedDate });
            modelBuilder.Entity<PrivateFile>().HasIndex(x => new { x.IsDeleted, x.PhysicalDeletedDate, x.DeleteRequestedDate });
            modelBuilder.Entity<EmailOutbox>().HasIndex(x => new { x.Status, x.NextAttemptAt, x.LockedUntil, x.CreatedDate });
            modelBuilder.Entity<EmailOutbox>().HasIndex(x => x.MessageKey).IsUnique().HasFilter("[MessageKey] IS NOT NULL");
            modelBuilder.Entity<PublicFileDeletionJob>().HasIndex(x => new { x.Status, x.NextAttemptAt, x.LockedUntil, x.CreatedDate });
            modelBuilder.Entity<PublicFileDeletionJob>().HasIndex(x => x.FileUrl).IsUnique();
            modelBuilder.Entity<SystemVersion>().HasIndex(x => x.IsCurrent).IsUnique().HasFilter("[IsCurrent] = 1");

            modelBuilder.Entity<WorkItemClaim>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Incident>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Settlement>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Payment>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<EmailOutbox>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<PublicFileDeletionJob>().Property(x => x.RowVersion).IsRowVersion();



            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Settlement>()
                .HasOne(x => x.Reservation).WithOne().HasForeignKey<Settlement>(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HandoverReport>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Dispute>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Incident>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DepositTransaction>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TrafficFine>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AdditionalCharge>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AdditionalCharge>()
                .HasOne(x => x.Payment).WithOne().HasForeignKey<AdditionalCharge>(x => x.PaymentID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VehicleDocument>()
                .HasOne(x => x.PartnerVehicle).WithMany().HasForeignKey(x => x.PartnerVehicleID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MaintenanceRecord>()
                .HasOne(x => x.PartnerVehicle).WithMany().HasForeignKey(x => x.PartnerVehicleID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HandoverImage>()
                .HasOne(x => x.HandoverReport).WithMany().HasForeignKey(x => x.HandoverReportID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne(x => x.Dispute).WithMany().HasForeignKey(x => x.DisputeID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserVerification>()
                .HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserVerification>()
                .HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.PermanentProvinceCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_UserVerifications_PermanentProvince");
            modelBuilder.Entity<UserVerification>()
                .HasOne<AdministrativeWard>().WithMany().HasForeignKey(x => x.PermanentWardCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_UserVerifications_PermanentWard");
            modelBuilder.Entity<UserVerification>()
                .HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.CurrentProvinceCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_UserVerifications_CurrentProvince");
            modelBuilder.Entity<UserVerification>()
                .HasOne<AdministrativeWard>().WithMany().HasForeignKey(x => x.CurrentWardCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_UserVerifications_CurrentWard");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.PermanentProvinceCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_PermanentProvince");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeWard>().WithMany().HasForeignKey(x => x.PermanentWardCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_PermanentWard");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.CurrentProvinceCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_CurrentProvince");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeWard>().WithMany().HasForeignKey(x => x.CurrentWardCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_CurrentWard");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeProvince>().WithMany().HasForeignKey(x => x.HeadquartersProvinceCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_HeadquartersProvince");
            modelBuilder.Entity<VehiclePartnerProfile>()
                .HasOne<AdministrativeWard>().WithMany().HasForeignKey(x => x.HeadquartersWardCode)
                .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_VehiclePartnerProfiles_HeadquartersWard");
            modelBuilder.Entity<UserVerification>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReviewedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserVerification>()
                .HasOne<PrivateFile>().WithMany().HasForeignKey(x => x.CitizenIdFrontFileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserVerification>()
                .HasOne<PrivateFile>().WithMany().HasForeignKey(x => x.CitizenIdBackFileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserVerification>()
                .HasOne<PrivateFile>().WithMany().HasForeignKey(x => x.DriverLicenseFileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserVerification>()
                .HasOne<PrivateFile>().WithMany().HasForeignKey(x => x.PortraitFileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HandoverReport>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<HandoverReport>()
                .HasOne<HandoverReport>().WithMany().HasForeignKey(x => x.ReplacedByReportId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Dispute>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Dispute>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.AssignedStaffAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Incident>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReportedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DepositTransaction>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AdditionalCharge>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DisputeMessage>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.SenderAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VehicleDocument>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReviewedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Settlement>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Settlement>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ApprovedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Notification>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.AppUserID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegistrationAttempt>().HasKey(x => x.RegistrationAttemptID);
            modelBuilder.Entity<RegistrationAttempt>().HasIndex(x => new { x.AccountType, x.Email, x.Status });
            modelBuilder.Entity<RegistrationAttempt>().HasIndex(x => new { x.Status, x.ExpiresAt });
            modelBuilder.Entity<RegistrationAttempt>().HasIndex(x => new { x.AccountType, x.Email })
                .IsUnique().HasFilter("[Status] = N'Pending'")
                .HasDatabaseName("IX_RegistrationAttempts_Pending_AccountType_Email");
            modelBuilder.Entity<RegistrationAttempt>().HasIndex(x => x.Username)
                .IsUnique().HasFilter("[Status] = N'Pending'")
                .HasDatabaseName("IX_RegistrationAttempts_Pending_Username");
            modelBuilder.Entity<RegistrationAttempt>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<SystemSetting>().HasIndex(x => x.SettingKey).IsUnique();
            modelBuilder.Entity<SystemSetting>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UpdatedByAppUserID).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DriverProfile>()
                .HasOne(x => x.PartnerAppUser).WithMany().HasForeignKey(x => x.PartnerAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DriverProfile>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReviewedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DriverProfile>().HasIndex(x => new { x.PartnerAppUserID, x.Status });
            modelBuilder.Entity<DriverProfile>().HasIndex(x => x.CitizenIdFingerprint).IsUnique().HasFilter("[CitizenIdFingerprint] IS NOT NULL");
            modelBuilder.Entity<DriverProfile>().HasIndex(x => x.DriverLicenseNumber).IsUnique();
            modelBuilder.Entity<DriverProfile>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<BookingDriverAssignment>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BookingDriverAssignment>()
                .HasOne(x => x.DriverProfile).WithMany().HasForeignKey(x => x.DriverProfileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BookingDriverAssignment>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.AssignedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BookingDriverAssignment>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<BookingDriverAssignment>().HasIndex(x => new { x.DriverProfileID, x.AssignmentStartUtc, x.AssignmentEndUtc, x.Status });
            modelBuilder.Entity<BookingDriverAssignment>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<VehiclePricingPlan>()
                .HasOne(x => x.PartnerVehicle).WithMany().HasForeignKey(x => x.PartnerVehicleID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VehiclePricingPlan>().HasIndex(x => new { x.PartnerVehicleID, x.ServiceType, x.IsActive });
            modelBuilder.Entity<VehiclePricingPlan>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Reservation>()
                .HasOne(x => x.VehiclePricingPlan).WithMany().HasForeignKey(x => x.VehiclePricingPlanID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleAvailabilityBlock>()
                .HasOne(x => x.PartnerVehicle).WithMany().HasForeignKey(x => x.PartnerVehicleID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VehicleAvailabilityBlock>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VehicleAvailabilityBlock>().HasIndex(x => new { x.PartnerVehicleID, x.StartUtc, x.EndUtc, x.IsActive });
            modelBuilder.Entity<VehicleAvailabilityBlock>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<BankAccountChangeRequest>()
                .HasOne(x => x.VehiclePartnerProfile).WithMany().HasForeignKey(x => x.VehiclePartnerProfileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BankAccountChangeRequest>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.RequestedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BankAccountChangeRequest>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ReviewedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BankAccountChangeRequest>().HasIndex(x => new { x.VehiclePartnerProfileID, x.Status });
            modelBuilder.Entity<BankAccountChangeRequest>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<RefundTransaction>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundTransaction>()
                .HasOne(x => x.OriginalPayment).WithMany().HasForeignKey(x => x.OriginalPaymentID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundTransaction>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ProposedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundTransaction>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.ApprovedByAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RefundTransaction>().HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            modelBuilder.Entity<RefundTransaction>().HasIndex(x => new { x.ReservationID, x.Status });
            modelBuilder.Entity<RefundTransaction>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<PartnerVehicle>().Property(x => x.RowVersion).IsRowVersion();
            modelBuilder.Entity<Reservation>().Property(x => x.RowVersion).IsRowVersion();

            modelBuilder.Entity<AppUser>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Car>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Review>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Review>()
                .HasOne(x => x.AppUser).WithMany().HasForeignKey(x => x.AppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasIndex(x => new { x.ReservationID, x.AppUserID, x.TargetType })
                .IsUnique()
                .HasFilter("[ReservationID] IS NOT NULL AND [AppUserID] IS NOT NULL AND [IsDeleted] = 0");

            modelBuilder.Entity<Review>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.TargetAppUserID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne<DriverProfile>().WithMany().HasForeignKey(x => x.TargetDriverProfileID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne<AppUser>().WithMany().HasForeignKey(x => x.HiddenByAppUserID).OnDelete(DeleteBehavior.Restrict);

            // Username duy nhất toàn hệ thống; email/phone duy nhất theo AccountType.
            modelBuilder.Entity<AppUser>().HasIndex(x => new { x.AccountType, x.Email }).IsUnique().HasFilter("[IsDeleted] = 0");
            modelBuilder.Entity<AppUser>().HasIndex(x => new { x.AccountType, x.Phone }).IsUnique().HasFilter("[Phone] IS NOT NULL AND [IsDeleted] = 0");
            modelBuilder.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();

            modelBuilder.Entity<Dispute>()
                .Property(x => x.RowVersion)
                .IsRowVersion();
            modelBuilder.Entity<AppUser>()
                .Property(x => x.RowVersion)
                .IsRowVersion();
            modelBuilder.Entity<Car>()
                .Property(x => x.RowVersion)
                .IsRowVersion();
        }
    }
}
