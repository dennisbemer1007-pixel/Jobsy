using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

public class JobsyDbContext : DbContext
{
    public JobsyDbContext(DbContextOptions<JobsyDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<Vacancy> Vacancies => Set<Vacancy>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<MinimumWageRate> MinimumWageRates => Set<MinimumWageRate>();
    public DbSet<VacancyClick> VacancyClicks => Set<VacancyClick>();
    public DbSet<VacancyLike> VacancyLikes => Set<VacancyLike>();
    public DbSet<VacancyShare> VacancyShares => Set<VacancyShare>();
    public DbSet<VacancySearchImpression> VacancySearchImpressions => Set<VacancySearchImpression>();
    public DbSet<SiteVisit> SiteVisits => Set<SiteVisit>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<RegionCompany> RegionCompanies => Set<RegionCompany>();
    public DbSet<CompanySalaryTable> CompanySalaryTables => Set<CompanySalaryTable>();
    public DbSet<CompanySalaryRate> CompanySalaryRates => Set<CompanySalaryRate>();
    public DbSet<CompanySalaryTableAllowedBranch> CompanySalaryTableAllowedBranches => Set<CompanySalaryTableAllowedBranch>();
    public DbSet<CompanySalaryTableChangeLog> CompanySalaryTableChangeLogs => Set<CompanySalaryTableChangeLog>();
    public DbSet<TokenPricing> TokenPricings => Set<TokenPricing>();
    public DbSet<TokenSpendCost> TokenSpendCosts => Set<TokenSpendCost>();
    public DbSet<PushBomSettings> PushBomSettings => Set<PushBomSettings>();
    public DbSet<PushBomPricingTier> PushBomPricingTiers => Set<PushBomPricingTier>();
    public DbSet<EarlyAdapterRule> EarlyAdapterRules => Set<EarlyAdapterRule>();
    public DbSet<IntegrationCredential> IntegrationCredentials => Set<IntegrationCredential>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<PlatformFeatureSettings> PlatformFeatureSettings => Set<PlatformFeatureSettings>();
    public DbSet<PlatformCompanySettings> PlatformCompanySettings => Set<PlatformCompanySettings>();
    public DbSet<AboutPageSettings> AboutPageSettings => Set<AboutPageSettings>();
    public DbSet<PlatformLog> PlatformLogs => Set<PlatformLog>();
    public DbSet<TokenPurchaseCheckout> TokenPurchaseCheckouts => Set<TokenPurchaseCheckout>();
    public DbSet<TokenPurchaseInvoice> TokenPurchaseInvoices => Set<TokenPurchaseInvoice>();
    public DbSet<VatBufferTransfer> VatBufferTransfers => Set<VatBufferTransfer>();
    public DbSet<VatDeclaration> VatDeclarations => Set<VatDeclaration>();
    public DbSet<CompanyRegistration> CompanyRegistrations => Set<CompanyRegistration>();
    public DbSet<EstablishmentTakeoverRequest> EstablishmentTakeoverRequests => Set<EstablishmentTakeoverRequest>();
    public DbSet<LocalAuthCredential> LocalAuthCredentials => Set<LocalAuthCredential>();
    public DbSet<SalesManagerProfile> SalesManagerProfiles => Set<SalesManagerProfile>();
    public DbSet<SupplierOnboardingCheckout> SupplierOnboardingCheckouts => Set<SupplierOnboardingCheckout>();
    public DbSet<CommissionLedgerEntry> CommissionLedgerEntries => Set<CommissionLedgerEntry>();
    public DbSet<SelfBillingInvoice> SelfBillingInvoices => Set<SelfBillingInvoice>();
    public DbSet<SelfBillingInvoiceLine> SelfBillingInvoiceLines => Set<SelfBillingInvoiceLine>();
    public DbSet<SalesManagerPayoutCheckout> SalesManagerPayoutCheckouts => Set<SalesManagerPayoutCheckout>();
    public DbSet<MasterdataOption> MasterdataOptions => Set<MasterdataOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PreferencesJson).HasMaxLength(8000);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.UnsubscribeVerificationCode).HasMaxLength(64);
            entity.Property(e => e.UnsubscribeReasonCode).HasMaxLength(64);
            entity.Property(e => e.UnsubscribeReasonOther).HasMaxLength(1000);
            entity.Property(e => e.HomeLocation)
                .HasConversion(new NullableGeoPointConverter())
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired(false);
            entity.HasIndex(e => e.HomeLocation).HasMethod("GIST");
            entity.HasIndex(e => e.Email).IsUnique();
            // PushBom + OpenForWork metrics hot path.
            entity.HasIndex(e => new { e.OpenForWork, e.IsActive, e.Role })
                .HasDatabaseName("IX_Users_OpenForWork_IsActive_Role")
                .HasFilter("\"OpenForWork\" = TRUE AND \"IsActive\" = TRUE");
            entity.HasOne(e => e.Company)
                .WithMany(c => c.PrimaryUsers)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserCompany>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CompanyId });
            entity.HasOne(e => e.User)
                .WithMany(u => u.CompanyMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.UserMemberships)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.KvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.KvkEstablishmentId).HasMaxLength(40);
            entity.Property(e => e.Address).HasMaxLength(512).IsRequired();
            entity.Property(e => e.LogoUrl).HasMaxLength(1024);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(64);
            entity.Property(e => e.ContactWhatsApp).HasMaxLength(64);
            entity.Property(e => e.Location)
                .HasConversion(new GeoPointConverter())
                .HasColumnType("geometry(Point, 4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.HasIndex(e => e.KvkEstablishmentId).IsUnique();
            entity.HasOne(e => e.ParentCompany)
                .WithMany(c => c.ChildCompanies)
                .HasForeignKey(e => e.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReferredBySalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredBySalesManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.ReferredBySalesManagerUserId);
            entity.HasIndex(e => e.FirstYearSupplierSlot)
                .IsUnique()
                .HasFilter("\"FirstYearSupplierSlot\" IS NOT NULL");
        });

        modelBuilder.Entity<Vacancy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(20000).IsRequired();
            entity.Property(e => e.HourlyWage).HasPrecision(8, 2);
            entity.Property(e => e.ImageUrl).HasMaxLength(HtmlSanitize.MaxImageUrlLength);
            entity.Property(e => e.VideoUrl).HasMaxLength(1024);
            entity.Property(e => e.RequiredDrivingLicense).HasMaxLength(256);
            entity.Property(e => e.RequiredEducation).HasMaxLength(256);
            entity.Property(e => e.WorkTypeLabels).HasMaxLength(512);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.MinHoursPerWeek).HasPrecision(5, 1);
            entity.Property(e => e.MaxHoursPerWeek).HasPrecision(5, 1);
            entity.Property(e => e.ScheduleJson).HasMaxLength(4000);
            entity.Property(e => e.FlexibleScheduleSource).HasMaxLength(32);
            entity.HasIndex(e => new { e.MinHoursPerWeek, e.MaxHoursPerWeek })
                .HasDatabaseName("IX_Vacancies_HoursPerWeek");
            entity.Property(e => e.Location)
                .HasConversion(new GeoPointConverter())
                .HasColumnType("geometry(Point, 4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            // Discover / public feed: Status + date window (and employer manage by company).
            entity.HasIndex(e => new { e.Status, e.EndDate, e.StartDate });
            entity.HasIndex(e => new { e.CompanyId, e.Status });
            entity.HasIndex(e => new { e.Status, e.PublishedAtUtc, e.CreatedAtUtc });
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Vacancies)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SalaryTable)
                .WithMany(t => t.Vacancies)
                .HasForeignKey(e => e.SalaryTableId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApiKeyHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.KeyPrefix).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.ApiKeyHash).IsUnique();
            entity.HasIndex(e => new { e.CompanyId, e.IsActive });
            // At most one active key per company (Postgres partial unique index).
            entity.HasIndex(e => e.CompanyId)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE")
                .HasDatabaseName("IX_ApiKeys_CompanyId_Active");
            entity.HasOne(e => e.Company)
                .WithMany(c => c.ApiKeys)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TokenTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.OldBalance).HasPrecision(10, 2);
            entity.Property(e => e.NewBalance).HasPrecision(10, 2);
            entity.Property(e => e.Note).HasMaxLength(512);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.TokenTransactions)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Vacancy)
                .WithMany()
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.BranchCompany)
                .WithMany()
                .HasForeignKey(e => e.BranchCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.TokenPurchaseCheckout)
                .WithMany()
                .HasForeignKey(e => e.TokenPurchaseCheckoutId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.TokenPurchaseInvoice)
                .WithMany()
                .HasForeignKey(e => e.TokenPurchaseInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Kind);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CandidateName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CandidateEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CandidateCity).HasMaxLength(128);
            entity.Property(e => e.CandidateAddress).HasMaxLength(512);
            entity.Property(e => e.PreferredTransport).HasMaxLength(64).IsRequired();
            entity.Property(e => e.PreferencesSummary).HasMaxLength(2048);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.EmailVerificationCode).HasMaxLength(64);
            entity.Property(e => e.SnapshotAvailabilityJson).HasMaxLength(2048);
            entity.Property(e => e.SnapshotDrivingLicenses).HasMaxLength(512);
            entity.Property(e => e.SnapshotEducations).HasMaxLength(512);
            entity.Property(e => e.SnapshotAboutMe).HasMaxLength(1024);
            entity.Property(e => e.Motivation).HasMaxLength(500);
            entity.Property(e => e.MatchBreakdownJson).HasMaxLength(4000);
            entity.HasIndex(e => e.MatchPercent);
            entity.HasIndex(e => e.ViaSafetyNet)
                .HasFilter("\"ViaSafetyNet\" = TRUE");
            entity.HasOne(e => e.Vacancy)
                .WithMany(v => v.Applications)
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CandidateUser)
                .WithMany()
                .HasForeignKey(e => e.CandidateUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
            // Employer inbox filters verified applications across managed companies.
            entity.HasIndex(e => e.EmailVerifiedAt)
                .HasFilter("\"EmailVerifiedAt\" IS NOT NULL");
            // Prevents double-apply races when CandidateUserId is set (NULLs are distinct in PostgreSQL).
            entity.HasIndex(e => new { e.VacancyId, e.CandidateUserId })
                .IsUnique()
                .HasFilter("\"CandidateUserId\" IS NOT NULL");
            entity.HasIndex(e => new { e.VacancyId, e.CandidateEmail }).IsUnique();
        });

        modelBuilder.Entity<MinimumWageRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).HasMaxLength(128).IsRequired();
            entity.Property(e => e.HourlyRate).HasPrecision(8, 2);
            entity.HasIndex(e => e.AgeYears);
        });

        modelBuilder.Entity<VacancyClick>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnonymousKey).HasMaxLength(128);
            entity.HasOne(e => e.Vacancy)
                .WithMany(v => v.Clicks)
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<VacancyLike>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VacancyId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Vacancy)
                .WithMany(v => v.Likes)
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<VacancyShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Vacancy)
                .WithMany(v => v.Shares)
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<VacancySearchImpression>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnonymousKey).HasMaxLength(128);
            entity.HasOne(e => e.Vacancy)
                .WithMany(v => v.SearchImpressions)
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.VacancyId);
        });

        modelBuilder.Entity<SiteVisit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnonymousKey).HasMaxLength(128);
            entity.Property(e => e.Path).HasMaxLength(512);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.AnonymousKey);
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.HasOne(e => e.OrganizationCompany)
                .WithMany()
                .HasForeignKey(e => e.OrganizationCompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegionCompany>(entity =>
        {
            entity.HasKey(e => new { e.RegionId, e.CompanyId });
            entity.HasOne(e => e.Region)
                .WithMany(r => r.Companies)
                .HasForeignKey(e => e.RegionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany(c => c.RegionMemberships)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanySalaryTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.HasOne(e => e.Company)
                .WithMany(c => c.SalaryTables)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.CompanyId, e.IsSystemWml })
                .IsUnique()
                .HasFilter("\"IsSystemWml\" = TRUE");
        });

        modelBuilder.Entity<CompanySalaryRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Label).HasMaxLength(128).IsRequired();
            entity.Property(e => e.HourlyRate).HasPrecision(8, 2);
            entity.HasOne(e => e.SalaryTable)
                .WithMany(t => t.Rates)
                .HasForeignKey(e => e.SalaryTableId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanySalaryTableAllowedBranch>(entity =>
        {
            entity.HasKey(e => new { e.SalaryTableId, e.CompanyId });
            entity.HasOne(e => e.SalaryTable)
                .WithMany(t => t.AllowedBranches)
                .HasForeignKey(e => e.SalaryTableId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanySalaryTableChangeLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ActorEmail).HasMaxLength(256);
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.SalaryTable)
                .WithMany(t => t.ChangeLogs)
                .HasForeignKey(e => e.SalaryTableId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TokenPricing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.PackSize).IsUnique();
        });

        modelBuilder.Entity<TokenSpendCost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostTokens).HasPrecision(10, 2);
            entity.HasIndex(e => e.Reason).IsUnique();
        });

        modelBuilder.Entity<PushBomSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PushBomPricingTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostTokens).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.MinCandidates, e.MaxCandidates });
        });

        modelBuilder.Entity<EarlyAdapterRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PurchaseDiscountPercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<IntegrationCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApiKey).HasMaxLength(2048);
            entity.Property(e => e.ClientId).HasMaxLength(256);
            entity.Property(e => e.ClientSecret).HasMaxLength(2048);
            entity.Property(e => e.TenantId).HasMaxLength(128);
            entity.Property(e => e.Model).HasMaxLength(64);
            entity.Property(e => e.BaseUrl).HasMaxLength(512);
            entity.Property(e => e.FromAddress).HasMaxLength(256);
            entity.Property(e => e.LastPingMessage).HasMaxLength(500);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        modelBuilder.Entity<PlatformFeatureSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PublicWebBaseUrl).HasMaxLength(512);
        });

        modelBuilder.Entity<PlatformCompanySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slogan).HasMaxLength(240);
            entity.Property(e => e.Address).HasMaxLength(240);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(120);
            entity.Property(e => e.Country).HasMaxLength(80);
            entity.Property(e => e.KvkNumber).HasMaxLength(32);
            entity.Property(e => e.VatNumber).HasMaxLength(32);
            entity.Property(e => e.Phone).HasMaxLength(40);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.VatBufferIban).HasMaxLength(34);
        });

        modelBuilder.Entity<AboutPageSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Lead).HasMaxLength(400);
            entity.Property(e => e.BodyHtml).IsRequired();
        });

        modelBuilder.Entity<PlatformLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Category);
        });

        modelBuilder.Entity<TokenPurchaseCheckout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).HasMaxLength(80).IsRequired();
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TokenPurchaseInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(e => e.MolliePaymentId).HasMaxLength(80).IsRequired();
            entity.Property(e => e.CompanyName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CompanyKvkNumber).HasMaxLength(32);
            entity.Property(e => e.CompanyAddress).HasMaxLength(512);
            entity.Property(e => e.VatRate).HasPrecision(5, 4);
            entity.Property(e => e.VatDeclarationStatusLabel).HasMaxLength(80);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.TokenPurchaseCheckoutId).IsUnique();
            entity.HasIndex(e => e.IssuedAt);
            entity.HasIndex(e => e.VatDeclarationId);
            entity.HasOne(e => e.Checkout)
                .WithOne(c => c.Invoice)
                .HasForeignKey<TokenPurchaseInvoice>(e => e.TokenPurchaseCheckoutId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TokenTransaction)
                .WithMany()
                .HasForeignKey(e => e.TokenTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.VatDeclaration)
                .WithMany(d => d.TokenPurchaseInvoices)
                .HasForeignKey(e => e.VatDeclarationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VatBufferTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(e => e.DestinationIban).HasMaxLength(34).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(512);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.TokenPurchaseInvoiceId).IsUnique();
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.VatBufferTransfers)
                .HasForeignKey(e => e.TokenPurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VatDeclaration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PeriodLabel).HasMaxLength(16).IsRequired();
            entity.Property(e => e.GeneratedByName).HasMaxLength(256);
            entity.Property(e => e.PdfFileName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.PlatformCompanyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PlatformKvkNumber).HasMaxLength(32);
            entity.Property(e => e.PlatformVatNumber).HasMaxLength(32);
            entity.Property(e => e.PlatformAddress).HasMaxLength(512);
            entity.HasIndex(e => new { e.Year, e.Quarter });
            entity.HasIndex(e => e.PeriodLabel);
            entity.HasOne(e => e.GeneratedByUser)
                .WithMany()
                .HasForeignKey(e => e.GeneratedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CompanyRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.KvkEstablishmentId).HasMaxLength(40).IsRequired();
            entity.Property(e => e.EstablishmentName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EstablishmentAddress).HasMaxLength(512).IsRequired();
            entity.Property(e => e.ContactName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ContactEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ContactPhone).HasMaxLength(64);
            entity.Property(e => e.ActivationToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.SalesManagerTrackingCode).HasMaxLength(32);
            entity.HasIndex(e => e.ActivationToken).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.CreatedUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedOrganizationCompany)
                .WithMany()
                .HasForeignKey(e => e.CreatedOrganizationCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedBranchCompany)
                .WithMany()
                .HasForeignKey(e => e.CreatedBranchCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EstablishmentTakeoverRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionNote).HasMaxLength(1024);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Registration)
                .WithMany(r => r.TakeoverRequests)
                .HasForeignKey(e => e.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TargetCompany)
                .WithMany()
                .HasForeignKey(e => e.TargetCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DecidedByUser)
                .WithMany()
                .HasForeignKey(e => e.DecidedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LocalAuthCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesManagerProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).HasMaxLength(256);
            entity.Property(e => e.KvkNumber).HasMaxLength(20);
            entity.Property(e => e.VatNumber).HasMaxLength(32);
            entity.Property(e => e.Address).HasMaxLength(512);
            entity.Property(e => e.PostalCode).HasMaxLength(16);
            entity.Property(e => e.City).HasMaxLength(128);
            entity.Property(e => e.Country).HasMaxLength(64);
            entity.Property(e => e.Iban).HasMaxLength(34);
            entity.Property(e => e.TrackingCode).HasMaxLength(32);
            entity.Property(e => e.AgreementVersion).HasMaxLength(64);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.TrackingCode)
                .IsUnique()
                .HasFilter("\"TrackingCode\" IS NOT NULL");
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<SalesManagerProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierOnboardingCheckout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).HasMaxLength(80).IsRequired();
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CommissionLedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountExVat).HasPrecision(10, 2);
            entity.Property(e => e.VatAmount).HasPrecision(10, 2);
            entity.Property(e => e.VatRate).HasPrecision(5, 4);
            entity.Property(e => e.Note).HasMaxLength(512);
            entity.Property(e => e.SourcePaymentId).HasMaxLength(80);
            entity.HasIndex(e => e.SalesManagerUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SourcePaymentId)
                .IsUnique()
                .HasFilter("\"SourcePaymentId\" IS NOT NULL");
            entity.HasIndex(e => e.SourceTokenCheckoutId)
                .IsUnique()
                .HasFilter("\"SourceTokenCheckoutId\" IS NOT NULL");
            // At most one founder bonus per referred supplier company.
            entity.HasIndex(e => e.CompanyId)
                .IsUnique()
                .HasFilter("\"Kind\" = 0 AND \"CompanyId\" IS NOT NULL");
            entity.HasOne(e => e.SalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.SalesManagerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SelfBillingInvoice)
                .WithMany(i => i.LinkedLedgerEntries)
                .HasForeignKey(e => e.SelfBillingInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SelfBillingInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SalesManagerCompanyName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.SalesManagerKvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.SalesManagerVatNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SalesManagerAddress).HasMaxLength(512).IsRequired();
            entity.Property(e => e.SubtotalExVat).HasPrecision(10, 2);
            entity.Property(e => e.VatAmount).HasPrecision(10, 2);
            entity.Property(e => e.TotalInclVat).HasPrecision(10, 2);
            entity.Property(e => e.VatRate).HasPrecision(5, 4);
            entity.Property(e => e.VatDeclarationStatusLabel).HasMaxLength(80);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.SalesManagerUserId);
            entity.HasIndex(e => e.VatDeclarationId);
            entity.HasOne(e => e.SalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.SalesManagerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.VatDeclaration)
                .WithMany(d => d.SelfBillingInvoices)
                .HasForeignKey(e => e.VatDeclarationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SelfBillingInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(512).IsRequired();
            entity.Property(e => e.AmountExVat).HasPrecision(10, 2);
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(e => e.SelfBillingInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesManagerPayoutCheckout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).HasMaxLength(80).IsRequired();
            entity.Property(e => e.MaskedIban).HasMaxLength(34).IsRequired();
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.Property(e => e.AmountExVat).HasPrecision(10, 2);
            entity.Property(e => e.VatAmount).HasPrecision(10, 2);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasIndex(e => e.SalesManagerUserId);
            entity.HasOne(e => e.SalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.SalesManagerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MasterdataOption>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => new { e.Category, e.Value }).IsUnique();
            entity.HasIndex(e => new { e.Category, e.SortOrder });
        });
    }
}
