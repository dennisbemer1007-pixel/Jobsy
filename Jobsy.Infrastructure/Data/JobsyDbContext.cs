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

    /// <summary>
    /// When non-null, company-scoped entities (Vacancy, Application, TokenTransaction)
    /// are auto-filtered to these company IDs. Null disables the filter (admin, public, jobs).
    /// Set per-request by <see cref="Jobsy.Infrastructure.Services.CompanyTenantScopeInitializer"/>.
    /// </summary>
    public HashSet<Guid>? EnforceCompanyScopeIds { get; set; }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<Vacancy> Vacancies => Set<Vacancy>();
    public DbSet<AtsScrapeSource> AtsScrapeSources => Set<AtsScrapeSource>();
    public DbSet<AtsScrapedListing> AtsScrapedListings => Set<AtsScrapedListing>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<CandidateUploadedCv> CandidateUploadedCvs => Set<CandidateUploadedCv>();
    public DbSet<CandidateReference> CandidateReferences => Set<CandidateReference>();
    public DbSet<CandidateCompetency> CandidateCompetencies => Set<CandidateCompetency>();
    public DbSet<CandidateCulturePersonalityProfile> CandidateCulturePersonalityProfiles => Set<CandidateCulturePersonalityProfile>();
    public DbSet<CandidateValuesProfile> CandidateValuesProfiles => Set<CandidateValuesProfile>();
    public DbSet<CompanyCultureProfile> CompanyCultureProfiles => Set<CompanyCultureProfile>();
    public DbSet<CandidateWhoAmIProfile> CandidateWhoAmIProfiles => Set<CandidateWhoAmIProfile>();
    public DbSet<CandidateCareerPlan> CandidateCareerPlans => Set<CandidateCareerPlan>();
    public DbSet<CandidateCareerStepProgress> CandidateCareerStepProgress => Set<CandidateCareerStepProgress>();
    public DbSet<CandidateCareerInterest> CandidateCareerInterests => Set<CandidateCareerInterest>();
    public DbSet<CandidateRoleFitCheck> CandidateRoleFitChecks => Set<CandidateRoleFitCheck>();
    public DbSet<CandidateMatchSnapshot> CandidateMatchSnapshots => Set<CandidateMatchSnapshot>();
    public DbSet<CandidateVacancyCultureFit> CandidateVacancyCultureFits => Set<CandidateVacancyCultureFit>();
    public DbSet<VacancyTranslation> VacancyTranslations => Set<VacancyTranslation>();
    public DbSet<TrainingProvider> TrainingProviders => Set<TrainingProvider>();
    public DbSet<TrainingOffer> TrainingOffers => Set<TrainingOffer>();
    public DbSet<TrainingClick> TrainingClicks => Set<TrainingClick>();
    public DbSet<TrainingConversion> TrainingConversions => Set<TrainingConversion>();
    public DbSet<CandidateDeepAnalysis> CandidateDeepAnalyses => Set<CandidateDeepAnalysis>();
    public DbSet<DeepAnalysisCheckout> DeepAnalysisCheckouts => Set<DeepAnalysisCheckout>();
    public DbSet<TalentContactRequest> TalentContactRequests => Set<TalentContactRequest>();
    public DbSet<FlexCommercialSettings> FlexCommercialSettings => Set<FlexCommercialSettings>();
    public DbSet<AgencyAnnualSubscription> AgencyAnnualSubscriptions => Set<AgencyAnnualSubscription>();
    public DbSet<ApplicationUploadedCv> ApplicationUploadedCvs => Set<ApplicationUploadedCv>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<CandidateActionToken> CandidateActionTokens => Set<CandidateActionToken>();
    public DbSet<MinimumWageRate> MinimumWageRates => Set<MinimumWageRate>();
    public DbSet<VacancyClick> VacancyClicks => Set<VacancyClick>();
    public DbSet<VacancyLike> VacancyLikes => Set<VacancyLike>();
    public DbSet<VacancyShare> VacancyShares => Set<VacancyShare>();
    public DbSet<VacancySearchImpression> VacancySearchImpressions => Set<VacancySearchImpression>();
    public DbSet<SiteVisit> SiteVisits => Set<SiteVisit>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<RegionCompany> RegionCompanies => Set<RegionCompany>();
    public DbSet<RegionHost> RegionHosts => Set<RegionHost>();
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
    public DbSet<MarketingFlyerSettings> MarketingFlyerSettings => Set<MarketingFlyerSettings>();
    public DbSet<PlatformLog> PlatformLogs => Set<PlatformLog>();
    public DbSet<TokenPurchaseCheckout> TokenPurchaseCheckouts => Set<TokenPurchaseCheckout>();
    public DbSet<PendingTokenAction> PendingTokenActions => Set<PendingTokenAction>();
    public DbSet<TokenPurchaseInvoice> TokenPurchaseInvoices => Set<TokenPurchaseInvoice>();
    public DbSet<VatBufferTransfer> VatBufferTransfers => Set<VatBufferTransfer>();
    public DbSet<VatDeclaration> VatDeclarations => Set<VatDeclaration>();
    public DbSet<CompanyRegistration> CompanyRegistrations => Set<CompanyRegistration>();
    public DbSet<EstablishmentTakeoverRequest> EstablishmentTakeoverRequests => Set<EstablishmentTakeoverRequest>();
    public DbSet<LocalAuthCredential> LocalAuthCredentials => Set<LocalAuthCredential>();
    public DbSet<SalesManagerProfile> SalesManagerProfiles => Set<SalesManagerProfile>();
    public DbSet<AmbassadeurProfile> AmbassadeurProfiles => Set<AmbassadeurProfile>();
    public DbSet<PartnerAffiliateProfile> PartnerAffiliateProfiles => Set<PartnerAffiliateProfile>();
    public DbSet<AmbassadeurSettings> AmbassadeurSettings => Set<AmbassadeurSettings>();
    public DbSet<SalesManagerApplication> SalesManagerApplications => Set<SalesManagerApplication>();
    public DbSet<SupplierOnboardingCheckout> SupplierOnboardingCheckouts => Set<SupplierOnboardingCheckout>();
    public DbSet<CommissionLedgerEntry> CommissionLedgerEntries => Set<CommissionLedgerEntry>();
    public DbSet<RevenueShareLog> RevenueShareLogs => Set<RevenueShareLog>();
    public DbSet<SelfBillingInvoice> SelfBillingInvoices => Set<SelfBillingInvoice>();
    public DbSet<SelfBillingInvoiceLine> SelfBillingInvoiceLines => Set<SelfBillingInvoiceLine>();
    public DbSet<SalesManagerPayoutCheckout> SalesManagerPayoutCheckouts => Set<SalesManagerPayoutCheckout>();
    public DbSet<MasterdataOption> MasterdataOptions => Set<MasterdataOption>();
    public DbSet<ExclusivitySetting> ExclusivitySettings => Set<ExclusivitySetting>();
    public DbSet<ExclusivityEducation> ExclusivityEducations => Set<ExclusivityEducation>();
    public DbSet<SalesCommercialSettings> SalesCommercialSettings => Set<SalesCommercialSettings>();
    public DbSet<VacancyTypeTokenCost> VacancyTypeTokenCosts => Set<VacancyTypeTokenCost>();
    public DbSet<VacancyCategory> VacancyCategories => Set<VacancyCategory>();
    public DbSet<SalesPackage> SalesPackages => Set<SalesPackage>();
    public DbSet<PlatformFeedback> PlatformFeedbacks => Set<PlatformFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(128);
            entity.Property(e => e.LastName).HasMaxLength(128);
            entity.Property(e => e.PhoneNumber).HasMaxLength(32);
            entity.Property(e => e.PreferencesJson).HasMaxLength(8000);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.UnsubscribeVerificationCode).HasMaxLength(64);
            entity.Property(e => e.UnsubscribeReasonCode).HasMaxLength(64);
            entity.Property(e => e.UnsubscribeReasonOther).HasMaxLength(1000);
            entity.Property(e => e.ReferredByAmbassadeurTrackingCode).HasMaxLength(32);
            entity.Property(e => e.HomeLocation)
                .HasConversion(new NullableGeoPointConverter())
                .HasColumnType("geometry(Point, 4326)")
                .IsRequired(false);
            entity.HasIndex(e => e.HomeLocation).HasMethod("GIST");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.ReferredByAmbassadeurUserId);
            // PushBom + OpenForWork metrics hot path.
            entity.HasIndex(e => new { e.OpenForWork, e.IsActive, e.Role })
                .HasDatabaseName("IX_Users_OpenForWork_IsActive_Role")
                .HasFilter("\"OpenForWork\" = TRUE AND \"IsActive\" = TRUE");
            entity.HasOne(e => e.Company)
                .WithMany(c => c.PrimaryUsers)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ReferredByAmbassadeurUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredByAmbassadeurUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserExternalLogin>(entity =>
        {
            entity.ToTable("UserExternalLogins");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProviderSubject).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EmailAtLink).HasMaxLength(256);
            entity.HasIndex(e => new { e.Provider, e.ProviderSubject }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.Property(e => e.PreferredPaymentMethod).HasMaxLength(32);
            entity.Property(e => e.CommissionDirectRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.CommissionIndirectRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.CommissionAmbassadeurRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.Location)
                .HasConversion(new GeoPointConverter())
                .HasColumnType("geometry(Point, 4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.HasIndex(e => e.KvkEstablishmentId).IsUnique();
            entity.HasIndex(e => e.KvkVerificationStatus);
            entity.HasOne(e => e.ParentCompany)
                .WithMany(c => c.ChildCompanies)
                .HasForeignKey(e => e.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReferredBySalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredBySalesManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ReferredByAmbassadeurUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredByAmbassadeurUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ReferredByPartnerUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredByPartnerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CommissionIndirectSalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.CommissionIndirectSalesManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.ReferredBySalesManagerUserId);
            entity.HasIndex(e => e.ReferredByAmbassadeurUserId);
            entity.HasIndex(e => e.ReferredByPartnerUserId);
            entity.HasIndex(e => e.FirstYearSupplierSlot)
                .IsUnique()
                .HasFilter("\"FirstYearSupplierSlot\" IS NOT NULL");
        });

        modelBuilder.Entity<Vacancy>(entity =>
        {
            // Defense-in-depth tenant filter: off when EnforceCompanyScopeIds is null
            // (public listings, admin, background jobs). Intermediaries also match via IntermediaryCompanyId.
            entity.HasQueryFilter(v =>
                EnforceCompanyScopeIds == null
                || EnforceCompanyScopeIds.Contains(v.CompanyId)
                || (v.IntermediaryCompanyId != null
                    && EnforceCompanyScopeIds.Contains(v.IntermediaryCompanyId.Value)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(20000).IsRequired();
            entity.Property(e => e.HourlyWage).HasPrecision(8, 2);
            entity.Property(e => e.ImageUrl).HasMaxLength(HtmlSanitize.MaxImageUrlLength);
            entity.Property(e => e.VideoUrl).HasMaxLength(1024);
            entity.Property(e => e.RequiredDrivingLicense).HasMaxLength(256);
            entity.Property(e => e.RequiredEducation).HasMaxLength(256);
            entity.Property(e => e.ContentModerationPassed).HasDefaultValue(true);
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
            entity.HasIndex(e => new { e.IsHighlighted, e.HighlightedUntil });
            entity.HasIndex(e => e.ClosedAtUtc);
            entity.HasIndex(e => e.IntermediaryCompanyId);
            entity.HasIndex(e => new { e.Status, e.Kind });
            entity.HasIndex(e => e.ExclusivitySettingId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => new { e.Status, e.CategoryId });
            entity.HasIndex(e => new { e.Status, e.SuitableFor65Plus });
            entity.Property(e => e.CategoryFieldsJson).HasMaxLength(8000);
            entity.Property(e => e.CulturePillarsJson).HasMaxLength(2000);
            entity.Property(e => e.BarrierRequirementsJson).HasMaxLength(4000);
            entity.Property(e => e.EngagementReminderTip).HasMaxLength(2000);
            entity.HasIndex(e => new { e.Status, e.EngagementReminderSentAtUtc, e.PublishedAtUtc });
            entity.HasOne(e => e.Company)
                .WithMany(c => c.Vacancies)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.IntermediaryCompany)
                .WithMany()
                .HasForeignKey(e => e.IntermediaryCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SalaryTable)
                .WithMany(t => t.Vacancies)
                .HasForeignKey(e => e.SalaryTableId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ExclusivitySetting)
                .WithMany(s => s.Vacancies)
                .HasForeignKey(e => e.ExclusivitySettingId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Vacancies)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AtsScrapeSource>(entity =>
        {
            entity.ToTable("AtsScrapeSources");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Domain).HasMaxLength(256).IsRequired();
            entity.Property(e => e.ListUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.DefaultLocationLabel).HasMaxLength(256);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.HasIndex(e => new { e.IsEnabled, e.LastScrapedAtUtc });
            entity.HasOne(e => e.PreferredCompany)
                .WithMany()
                .HasForeignKey(e => e.PreferredCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AtsScrapedListing>(entity =>
        {
            entity.ToTable("AtsScrapedListings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DedupHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.CompanyName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.LocationLabel).HasMaxLength(256);
            entity.Property(e => e.PostalCode).HasMaxLength(16);
            entity.Property(e => e.Description).HasMaxLength(20000).IsRequired();
            entity.Property(e => e.SalaryText).HasMaxLength(512);
            entity.Property(e => e.HourlyWage).HasPrecision(8, 2);
            entity.Property(e => e.HoursText).HasMaxLength(256);
            entity.Property(e => e.MinHoursPerWeek).HasPrecision(5, 1);
            entity.Property(e => e.MaxHoursPerWeek).HasPrecision(5, 1);
            entity.Property(e => e.TagsJson).HasMaxLength(2000);
            entity.Property(e => e.ImageUrl).HasMaxLength(HtmlSanitize.MaxImageUrlLength);
            entity.Property(e => e.RejectReason).HasMaxLength(1000);
            entity.Property(e => e.ScrapedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.DedupHash).IsUnique();
            entity.HasIndex(e => new { e.Status, e.ScrapedAtUtc });
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.LinkedVacancyId);
            entity.HasIndex(e => e.LastCheckedAtUtc);
            entity.HasOne(e => e.Source)
                .WithMany(s => s.Listings)
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LinkedVacancy)
                .WithMany()
                .HasForeignKey(e => e.LinkedVacancyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VacancyCategory>(entity =>
        {
            entity.ToTable("VacancyCategories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ColorHex).HasMaxLength(7).IsRequired();
            entity.Property(e => e.PublishCostTokens).HasPrecision(10, 2);
            entity.Property(e => e.HighlightCostTokens).HasPrecision(10, 2);
            entity.Property(e => e.PushBomCostTokens).HasPrecision(10, 2);
            entity.Property(e => e.ExtraFieldsJson).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => new { e.IsActive, e.SortOrder });
            entity.HasIndex(e => e.PlacementKind);
        });

        modelBuilder.Entity<RevenueShareLog>(entity =>
        {
            entity.ToTable("RevenueShareLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.AmountEuro).HasPrecision(12, 2);
            entity.Property(e => e.Tokens).HasPrecision(12, 2);
            entity.HasIndex(e => e.TokenCheckoutId);
            entity.HasIndex(e => new { e.TokenCheckoutId, e.RecipientKind }).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RecipientCompany)
                .WithMany()
                .HasForeignKey(e => e.RecipientCompanyId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.RecipientUser)
                .WithMany()
                .HasForeignKey(e => e.RecipientUserId)
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
            entity.HasQueryFilter(t =>
                EnforceCompanyScopeIds == null
                || EnforceCompanyScopeIds.Contains(t.CompanyId));
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
            // At most one ledger row per (checkout, kind) — blocks double purchase/grant races.
            entity.HasIndex(e => new { e.TokenPurchaseCheckoutId, e.Kind })
                .IsUnique()
                .HasFilter("\"TokenPurchaseCheckoutId\" IS NOT NULL")
                .HasDatabaseName("IX_TokenTransactions_Checkout_Kind");
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
            entity.Property(e => e.SnapshotAvailabilityJson).HasMaxLength(4000);
            entity.Property(e => e.SnapshotDrivingLicenses).HasMaxLength(512);
            entity.Property(e => e.SnapshotEducations).HasMaxLength(512);
            entity.Property(e => e.SnapshotAboutMe).HasMaxLength(1024);
            entity.Property(e => e.SnapshotPhoneNumber).HasMaxLength(32);
            entity.Property(e => e.SnapshotCertificatesJson).HasMaxLength(4000);
            entity.Property(e => e.Motivation).HasMaxLength(500);
            entity.Property(e => e.StudentNumber).HasMaxLength(64);
            entity.Property(e => e.SchoolEmail).HasMaxLength(256);
            entity.Property(e => e.StudyProgram).HasMaxLength(256);
            entity.Property(e => e.StudyYear).HasMaxLength(64);
            entity.Property(e => e.ExclusivityValidationStatus).HasMaxLength(32);
            entity.Property(e => e.MatchBreakdownJson).HasMaxLength(4000);
            entity.Property(e => e.SnapshotWhoAmIJson).HasColumnType("text");
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
            entity.HasOne(e => e.UploadedCv)
                .WithOne(c => c.Application)
                .HasForeignKey<ApplicationUploadedCv>(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateUploadedCv>(entity =>
        {
            entity.ToTable("CandidateUploadedCvs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(180).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.FilledFieldsJson).HasMaxLength(1000);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateReference>(entity =>
        {
            entity.ToTable("CandidateReferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.SortOrder });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCompetency>(entity =>
        {
            entity.ToTable("CandidateCompetencies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.RiasecTagsJson).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MatchTagsJson).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCulturePersonalityProfile>(entity =>
        {
            entity.ToTable("CandidateCulturePersonalityProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.MatchTagsJson).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateValuesProfile>(entity =>
        {
            entity.ToTable("CandidateValuesProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.MatchTagsJson).HasMaxLength(1000).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyCultureProfile>(entity =>
        {
            entity.ToTable("CompanyCultureProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(e => e.CompanyId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateWhoAmIProfile>(entity =>
        {
            entity.ToTable("CandidateWhoAmIProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StoryText).HasColumnType("text").IsRequired();
            entity.Property(e => e.KeywordsJson).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.InputFingerprint).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCareerPlan>(entity =>
        {
            entity.ToTable("CandidateCareerPlans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DreamTitle).HasMaxLength(80).IsRequired();
            entity.Property(e => e.DreamKey).HasMaxLength(120).IsRequired();
            entity.Property(e => e.PlanJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.MatchSummary).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCareerStepProgress>(entity =>
        {
            entity.ToTable("CandidateCareerStepProgress");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepKey).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => new { e.PlanId, e.StepKey }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.StepProgress)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCareerInterest>(entity =>
        {
            entity.ToTable("CandidateCareerInterests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.HollandCode).HasMaxLength(8).IsRequired();
            entity.Property(e => e.RiasecTagsJson).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MatchTagsJson).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CompassJson).HasColumnType("text").IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateRoleFitCheck>(entity =>
        {
            entity.ToTable("CandidateRoleFitChecks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JobTitle).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ResultJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.InputFingerprint).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateMatchSnapshot>(entity =>
        {
            entity.ToTable("CandidateMatchSnapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchesJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.InputFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateVacancyCultureFit>(entity =>
        {
            entity.ToTable("CandidateVacancyCultureFits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResultJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.InputFingerprint).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.VacancyId }).IsUnique();
            entity.HasIndex(e => e.VacancyId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Vacancy)
                .WithMany()
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyTranslation>(entity =>
        {
            entity.ToTable("VacancyTranslations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Language).HasMaxLength(8).IsRequired();
            entity.Property(e => e.SourceHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TranslatedJson).HasColumnType("text").IsRequired();
            entity.HasIndex(e => new { e.VacancyId, e.Language }).IsUnique();
            entity.HasOne(e => e.Vacancy)
                .WithMany()
                .HasForeignKey(e => e.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateDeepAnalysis>(entity =>
        {
            entity.ToTable("CandidateDeepAnalyses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.TagsJson).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Kind }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeepAnalysisCheckout>(entity =>
        {
            entity.ToTable("DeepAnalysisCheckouts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Status });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TalentContactRequest>(entity =>
        {
            entity.ToTable("TalentContactRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => new { e.CompanyId, e.CandidateUserId, e.Status });
            entity.HasIndex(e => e.RespondByUtc);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.EmployerUser)
                .WithMany()
                .HasForeignKey(e => e.EmployerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CandidateUser)
                .WithMany()
                .HasForeignKey(e => e.CandidateUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SpendTransaction)
                .WithMany()
                .HasForeignKey(e => e.SpendTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.RefundTransaction)
                .WithMany()
                .HasForeignKey(e => e.RefundTransactionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FlexCommercialSettings>(entity =>
        {
            entity.ToTable("FlexCommercialSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MarginPerHourEuro).HasPrecision(10, 2);
            entity.Property(e => e.DeepAnalysisPriceEuro).HasPrecision(10, 2);
            entity.Property(e => e.AgencyAnnualPriceEuro).HasPrecision(12, 2);
            entity.Property(e => e.ContactUnlockCostTokens).HasPrecision(10, 2);
            entity.Property(e => e.BackofficePartnerName).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<AgencyAnnualSubscription>(entity =>
        {
            entity.ToTable("AgencyAnnualSubscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PriceEuro).HasPrecision(12, 2);
            entity.HasIndex(e => new { e.CompanyId, e.IsActive, e.EndsAtUtc });
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUploadedCv>(entity =>
        {
            entity.ToTable("ApplicationUploadedCvs");
            entity.HasKey(e => e.ApplicationId);
            entity.Property(e => e.FileName).HasMaxLength(180).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("UserNotifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DeepLink).HasMaxLength(1024);
            entity.Property(e => e.ActionLabel).HasMaxLength(128);
            entity.Property(e => e.ActionUrl).HasMaxLength(1024);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(64);
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAtUtc });
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WebPushSubscription>(entity =>
        {
            entity.ToTable("WebPushSubscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Endpoint).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.P256dh).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Auth).HasMaxLength(256).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.HasIndex(e => e.Endpoint).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateActionToken>(entity =>
        {
            entity.ToTable("CandidateActionTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Purpose).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

        modelBuilder.Entity<RegionHost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Hostname).HasMaxLength(RegionHostRules.MaxHostnameLength).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(RegionHostRules.MaxDisplayNameLength).IsRequired();
            entity.Property(e => e.Slogan).HasMaxLength(RegionHostRules.MaxSloganLength);
            entity.Property(e => e.AddressLabel).HasMaxLength(RegionHostRules.MaxAddressLength);
            entity.Property(e => e.BackgroundImageUrl).HasMaxLength(RegionHostRules.MaxBackgroundUrlLength);
            entity.HasIndex(e => e.Hostname).IsUnique();
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

        modelBuilder.Entity<SalesCommercialSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BaseTokenValueEuro).HasPrecision(10, 2);
            entity.Property(e => e.HighlightCarouselTokens).HasPrecision(10, 2);
            entity.Property(e => e.HighlightPulseTokens).HasPrecision(10, 2);
            entity.Property(e => e.StartHighlightBonusTokens).HasPrecision(10, 2);
            entity.Property(e => e.DirectCommissionRate).HasPrecision(5, 4);
            entity.Property(e => e.IndirectCommissionRate).HasPrecision(5, 4);
            entity.Property(e => e.PartnerCommissionRate).HasPrecision(5, 4);
            entity.Property(e => e.Year2DirectCommissionRate).HasPrecision(5, 4);
            entity.Property(e => e.Year3DirectCommissionRate).HasPrecision(5, 4);
            entity.Property(e => e.ReferredYear1DirectCommissionRate).HasPrecision(5, 4);
        });

        modelBuilder.Entity<VacancyTypeTokenCost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostTokens).HasPrecision(10, 2);
            entity.HasIndex(e => e.Kind).IsUnique();
        });

        modelBuilder.Entity<SalesPackage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(32);
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.Property(e => e.PriceEuro).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.Category, e.SortOrder });
            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasFilter("\"Code\" IS NOT NULL");
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

        modelBuilder.Entity<MarketingFlyerSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Headline).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subheadline).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Intro).HasMaxLength(600).IsRequired();
            entity.Property(e => e.BulletPoints).IsRequired();
            entity.Property(e => e.PromoFreeText).HasMaxLength(400).IsRequired();
            entity.Property(e => e.PromoDiscountText).HasMaxLength(400).IsRequired();
            entity.Property(e => e.CtaTitle).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CtaBody).HasMaxLength(500).IsRequired();
            entity.Property(e => e.QrCaption).HasMaxLength(200).IsRequired();
            entity.Property(e => e.QrPath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FooterNote).HasMaxLength(300).IsRequired();
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
            entity.Property(e => e.PaymentMethod).HasMaxLength(32);
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingTokenAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequiredTokens).HasPrecision(18, 2);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(e => e.TokenPurchaseCheckoutId).IsUnique();
            entity.HasIndex(e => e.VacancyId);
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Checkout)
                .WithOne(c => c.PendingAction)
                .HasForeignKey<PendingTokenAction>(e => e.TokenPurchaseCheckoutId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Vacancy)
                .WithMany()
                .HasForeignKey(e => e.VacancyId)
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
            entity.Property(e => e.EmailVerificationCode).HasMaxLength(64);
            entity.Property(e => e.EmailVerificationExpiresAt);
            entity.Property(e => e.EmailVerificationFailedAttempts);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.PrimarySbiCode).HasMaxLength(16);
            entity.Property(e => e.ContactEmailVerifiedAt);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.SalesManagerTrackingCode).HasMaxLength(32);
            entity.Property(e => e.PartnerTrackingCode).HasMaxLength(32);
            entity.HasIndex(e => e.ActivationToken).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EmailVerificationExpiresAt);
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
            entity.HasIndex(e => e.ReferredBySalesManagerUserId);
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<SalesManagerProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReferredBySalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ReferredBySalesManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AmbassadeurProfile>(entity =>
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
            entity.Property(e => e.BaseCommissionPercentage).HasPrecision(5, 2);
            entity.Property(e => e.CommissionPercentageOverride).HasPrecision(5, 2);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.TrackingCode)
                .IsUnique()
                .HasFilter("\"TrackingCode\" IS NOT NULL");
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<AmbassadeurProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartnerAffiliateProfile>(entity =>
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
            entity.Property(e => e.TrackingCode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.AgreementVersion).HasMaxLength(64);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.TrackingCode).IsUnique();
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<PartnerAffiliateProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AmbassadeurSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PercentPerThreshold).HasPrecision(5, 2);
            entity.Property(e => e.MaxCommissionPercentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<SalesManagerApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReferrerTrackingCode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CandidateEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CandidateFullName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Motivation).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.CandidateEmail);
            entity.HasIndex(e => new { e.CandidateEmail, e.Status });
            entity.HasOne(e => e.ReferrerSalesManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ReferrerSalesManagerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReviewedByAdminUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByAdminUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ProvisionedUser)
                .WithMany()
                .HasForeignKey(e => e.ProvisionedUserId)
                .OnDelete(DeleteBehavior.SetNull);
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
            // Direct + indirect commissions may share a checkout; uniqueness is per SM + kind.
            entity.HasIndex(e => new { e.SourceTokenCheckoutId, e.SalesManagerUserId, e.Kind })
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

        modelBuilder.Entity<ExclusivitySetting>(entity =>
        {
            entity.ToTable("ExclusivitySettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.SchoolDomain).HasMaxLength(256);
            entity.Property(e => e.StudentNumberPattern).HasMaxLength(512);
            entity.HasIndex(e => e.SortOrder);
            entity.HasIndex(e => e.SchoolDomain)
                .IsUnique()
                .HasFilter("\"SchoolDomain\" IS NOT NULL");
            entity.HasIndex(e => e.IsOpenOption)
                .IsUnique()
                .HasFilter("\"IsOpenOption\" = TRUE");
        });

        modelBuilder.Entity<ExclusivityEducation>(entity =>
        {
            entity.ToTable("ExclusivityEducations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.ExclusivitySettingId, e.SortOrder });
            entity.HasOne(e => e.ExclusivitySetting)
                .WithMany(s => s.Educations)
                .HasForeignKey(e => e.ExclusivitySettingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformFeedback>(entity =>
        {
            entity.ToTable("PlatformFeedbacks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.PageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.UserRole).HasMaxLength(64);
            entity.Property(e => e.UserDisplayName).HasMaxLength(128);
            entity.Property(e => e.BrowserInfo).HasMaxLength(512);
            entity.Property(e => e.DeviceInfo).HasMaxLength(256);
            entity.Property(e => e.ScreenshotContentType).HasMaxLength(64);
            entity.Property(e => e.GeneratedPrompt).HasMaxLength(16000);
            entity.Property(e => e.CursorAgentId).HasMaxLength(128);
            entity.Property(e => e.BranchName).HasMaxLength(128);
            entity.Property(e => e.PullRequestUrl).HasMaxLength(1024);
            entity.Property(e => e.AutomationError).HasMaxLength(512);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CursorAgentId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingProvider>(entity =>
        {
            entity.ToTable("TrainingProviders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseUrl).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.FieldsCsv).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Region).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CplEuro).HasPrecision(10, 2);
            entity.Property(e => e.CpaEuro).HasPrecision(10, 2);
            entity.Property(e => e.IntakeFeeEuro).HasPrecision(10, 2);
            entity.Property(e => e.StartFeeEuro).HasPrecision(10, 2);
            entity.HasIndex(e => e.Kind);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<TrainingOffer>(entity =>
        {
            entity.ToTable("TrainingOffers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FieldsCsv).HasMaxLength(200).IsRequired();
            entity.Property(e => e.KeysCsv).HasMaxLength(400).IsRequired();
            entity.Property(e => e.ExternalPath).HasMaxLength(1024);
            entity.HasIndex(e => e.ProviderId);
            entity.HasOne(e => e.Provider)
                .WithMany(p => p.Offers)
                .HasForeignKey(e => e.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrainingClick>(entity =>
        {
            entity.ToTable("TrainingClicks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CandidateHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.EmailHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Campaign).HasMaxLength(64).IsRequired();
            entity.Property(e => e.OutboundUrl).HasMaxLength(2000).IsRequired();
            entity.HasIndex(e => e.OfferId);
            entity.HasIndex(e => e.CandidateHash);
            entity.HasIndex(e => e.EmailHash);
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.Offer)
                .WithMany()
                .HasForeignKey(e => e.OfferId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrainingConversion>(entity =>
        {
            entity.ToTable("TrainingConversions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => new { e.ClickId, e.Kind }).IsUnique();
            entity.HasOne(e => e.Click)
                .WithMany(c => c.Conversions)
                .HasForeignKey(e => e.ClickId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
