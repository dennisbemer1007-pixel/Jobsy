using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<CandidateUploadedCv> CandidateUploadedCvs => Set<CandidateUploadedCv>();
    public DbSet<CandidateDiplomaEvaluation> CandidateDiplomaEvaluations => Set<CandidateDiplomaEvaluation>();
    public DbSet<CandidateReference> CandidateReferences => Set<CandidateReference>();
    public DbSet<ReferenceConfirmation> ReferenceConfirmations => Set<ReferenceConfirmation>();
    public DbSet<ReferenceConfirmationToken> ReferenceConfirmationTokens => Set<ReferenceConfirmationToken>();
    public DbSet<ReferenceConfirmationConsentLog> ReferenceConfirmationConsentLogs => Set<ReferenceConfirmationConsentLog>();
    public DbSet<ReferenceMisuseReport> ReferenceMisuseReports => Set<ReferenceMisuseReport>();
    public DbSet<CandidateCompetency> CandidateCompetencies => Set<CandidateCompetency>();
    public DbSet<CandidateCulturePersonalityProfile> CandidateCulturePersonalityProfiles => Set<CandidateCulturePersonalityProfile>();
    public DbSet<CandidateValuesProfile> CandidateValuesProfiles => Set<CandidateValuesProfile>();
    public DbSet<CompanyCultureProfile> CompanyCultureProfiles => Set<CompanyCultureProfile>();
    public DbSet<CompanyValuesProfile> CompanyValuesProfiles => Set<CompanyValuesProfile>();
    public DbSet<CompanyEngagementClaim> CompanyEngagementClaims => Set<CompanyEngagementClaim>();
    public DbSet<CompanyEngagementReport> CompanyEngagementReports => Set<CompanyEngagementReport>();
    public DbSet<LenderRegistration> LenderRegistrations => Set<LenderRegistration>();
    public DbSet<CandidateWhoAmIProfile> CandidateWhoAmIProfiles => Set<CandidateWhoAmIProfile>();
    public DbSet<CandidatePrivatePreferences> CandidatePrivatePreferences => Set<CandidatePrivatePreferences>();
    public DbSet<CandidateCareerPlan> CandidateCareerPlans => Set<CandidateCareerPlan>();
    public DbSet<CandidateCareerStepProgress> CandidateCareerStepProgress => Set<CandidateCareerStepProgress>();
    public DbSet<CandidateCareerGeneration> CandidateCareerGenerations => Set<CandidateCareerGeneration>();
    public DbSet<CandidateCareerInterest> CandidateCareerInterests => Set<CandidateCareerInterest>();
    public DbSet<CandidateOnboarding> CandidateOnboardings => Set<CandidateOnboarding>();
    public DbSet<CandidateRoleFitCheck> CandidateRoleFitChecks => Set<CandidateRoleFitCheck>();
    public DbSet<CandidateMatchSnapshot> CandidateMatchSnapshots => Set<CandidateMatchSnapshot>();
    public DbSet<CandidateVacancyCultureFit> CandidateVacancyCultureFits => Set<CandidateVacancyCultureFit>();
    public DbSet<VacancyTranslation> VacancyTranslations => Set<VacancyTranslation>();
    public DbSet<TrainingProvider> TrainingProviders => Set<TrainingProvider>();
    public DbSet<TrainingOffer> TrainingOffers => Set<TrainingOffer>();
    public DbSet<TrainingClick> TrainingClicks => Set<TrainingClick>();
    public DbSet<TrainingConversion> TrainingConversions => Set<TrainingConversion>();
    public DbSet<CandidateDeepAnalysis> CandidateDeepAnalyses => Set<CandidateDeepAnalysis>();
    public DbSet<CandidateAssessmentAdjustment> CandidateAssessmentAdjustments => Set<CandidateAssessmentAdjustment>();
    public DbSet<CandidateAssessmentAttempt> CandidateAssessmentAttempts => Set<CandidateAssessmentAttempt>();
    public DbSet<AssessmentNormSnapshot> AssessmentNormSnapshots => Set<AssessmentNormSnapshot>();
    public DbSet<DeepAnalysisCheckout> DeepAnalysisCheckouts => Set<DeepAnalysisCheckout>();
    public DbSet<ConsumerPurchaseInvoice> ConsumerPurchaseInvoices => Set<ConsumerPurchaseInvoice>();
    public DbSet<TalentContactRequest> TalentContactRequests => Set<TalentContactRequest>();
    public DbSet<FlexCommercialSettings> FlexCommercialSettings => Set<FlexCommercialSettings>();
    public DbSet<AgencyAnnualSubscription> AgencyAnnualSubscriptions => Set<AgencyAnnualSubscription>();
    public DbSet<ApplicationUploadedCv> ApplicationUploadedCvs => Set<ApplicationUploadedCv>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<UserDeviceSession> UserDeviceSessions => Set<UserDeviceSession>();
    public DbSet<MfaTrustedDevice> MfaTrustedDevices => Set<MfaTrustedDevice>();
    public DbSet<DeviceLoginHandoff> DeviceLoginHandoffs => Set<DeviceLoginHandoff>();
    public DbSet<CandidateActionToken> CandidateActionTokens => Set<CandidateActionToken>();
    public DbSet<OneTimeLink> OneTimeLinks => Set<OneTimeLink>();
    public DbSet<EmailOptOut> EmailOptOuts => Set<EmailOptOut>();
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
    public DbSet<TokenRequest> TokenRequests => Set<TokenRequest>();
    public DbSet<CandidateInsightsUnlock> CandidateInsightsUnlocks => Set<CandidateInsightsUnlock>();
    public DbSet<CandidateInsightsUnlockRequest> CandidateInsightsUnlockRequests => Set<CandidateInsightsUnlockRequest>();
    public DbSet<PushBomSettings> PushBomSettings => Set<PushBomSettings>();
    public DbSet<PushBomPricingTier> PushBomPricingTiers => Set<PushBomPricingTier>();
    public DbSet<EarlyAdapterRule> EarlyAdapterRules => Set<EarlyAdapterRule>();
    public DbSet<IntegrationCredential> IntegrationCredentials => Set<IntegrationCredential>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<PlatformFeatureSettings> PlatformFeatureSettings => Set<PlatformFeatureSettings>();
    public DbSet<PhoneVerificationChallenge> PhoneVerificationChallenges => Set<PhoneVerificationChallenge>();
    public DbSet<PassportPartner> PassportPartners => Set<PassportPartner>();
    public DbSet<PassportPartnerCode> PassportPartnerCodes => Set<PassportPartnerCode>();
    public DbSet<PassportPartnerCandidateLink> PassportPartnerCandidateLinks => Set<PassportPartnerCandidateLink>();
    public DbSet<PassportAccessLog> PassportAccessLogs => Set<PassportAccessLog>();
    public DbSet<PlatformCompanySettings> PlatformCompanySettings => Set<PlatformCompanySettings>();
#pragma warning disable CS0618 // Table kept until the public-pages 08 cleanup migration drops it.
    public DbSet<AboutPageSettings> AboutPageSettings => Set<AboutPageSettings>();
#pragma warning restore CS0618
    public DbSet<MarketingFlyerSettings> MarketingFlyerSettings => Set<MarketingFlyerSettings>();
    public DbSet<PlatformLog> PlatformLogs => Set<PlatformLog>();
    public DbSet<PersonalDataAccessLog> PersonalDataAccessLogs => Set<PersonalDataAccessLog>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();
    public DbSet<SupportAccessGrant> SupportAccessGrants => Set<SupportAccessGrant>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<TeacherClassAssignment> TeacherClassAssignments => Set<TeacherClassAssignment>();
    public DbSet<PupilCode> PupilCodes => Set<PupilCode>();
    public DbSet<PupilProgress> PupilProgresses => Set<PupilProgress>();
    public DbSet<PupilResult> PupilResults => Set<PupilResult>();
    public DbSet<SchoolClassAggregate> SchoolClassAggregates => Set<SchoolClassAggregate>();
    public DbSet<SchoolYearAggregate> SchoolYearAggregates => Set<SchoolYearAggregate>();
    public DbSet<SchoolRetentionRun> SchoolRetentionRuns => Set<SchoolRetentionRun>();
    public DbSet<SchoolStaffInvite> SchoolStaffInvites => Set<SchoolStaffInvite>();
    public DbSet<TokenPurchaseCheckout> TokenPurchaseCheckouts => Set<TokenPurchaseCheckout>();
    public DbSet<PendingTokenAction> PendingTokenActions => Set<PendingTokenAction>();
    public DbSet<TokenPurchaseInvoice> TokenPurchaseInvoices => Set<TokenPurchaseInvoice>();
    public DbSet<VatBufferTransfer> VatBufferTransfers => Set<VatBufferTransfer>();
    public DbSet<VatDeclaration> VatDeclarations => Set<VatDeclaration>();
    public DbSet<CompanyRegistration> CompanyRegistrations => Set<CompanyRegistration>();
    public DbSet<CompanyVerificationLetter> CompanyVerificationLetters => Set<CompanyVerificationLetter>();
    public DbSet<CompanyVerificationEmailChallenge> CompanyVerificationEmailChallenges => Set<CompanyVerificationEmailChallenge>();
    public DbSet<CompanyManualVerificationRequest> CompanyManualVerificationRequests => Set<CompanyManualVerificationRequest>();
    public DbSet<CompanyVerificationDecision> CompanyVerificationDecisions => Set<CompanyVerificationDecision>();
    public DbSet<KvkUsageDaily> KvkUsageDaily => Set<KvkUsageDaily>();
    public DbSet<EstablishmentTakeoverRequest> EstablishmentTakeoverRequests => Set<EstablishmentTakeoverRequest>();
    public DbSet<CompanyAccessRequest> CompanyAccessRequests => Set<CompanyAccessRequest>();
    public DbSet<DismissedVestigingSuggestion> DismissedVestigingSuggestions => Set<DismissedVestigingSuggestion>();
    public DbSet<LocalAuthCredential> LocalAuthCredentials => Set<LocalAuthCredential>();
    public DbSet<EmailSignInChallenge> EmailSignInChallenges => Set<EmailSignInChallenge>();
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
    public DbSet<SalesSelfBillingConsent> SalesSelfBillingConsents => Set<SalesSelfBillingConsent>();
    public DbSet<SalesIbanChangePending> SalesIbanChangePendings => Set<SalesIbanChangePending>();
    public DbSet<SalesPayoutRequest> SalesPayoutRequests => Set<SalesPayoutRequest>();
    public DbSet<SalesPayoutRun> SalesPayoutRuns => Set<SalesPayoutRun>();
    public DbSet<SalesAttributionChange> SalesAttributionChanges => Set<SalesAttributionChange>();
    public DbSet<SalesLinkClickDaily> SalesLinkClickDailies => Set<SalesLinkClickDaily>();
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
            entity.Property(e => e.PhoneVerifiedE164).HasMaxLength(20);
            entity.Property(e => e.PreferencesJson).HasMaxLength(8000);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.Property(e => e.TalentPoolConsentVersion).HasMaxLength(32);
            entity.Property(e => e.TestAiConsentVersion).HasMaxLength(32);
            entity.Property(e => e.ParentalConsentEmail).HasMaxLength(256);
            entity.Property(e => e.ParentalConsentTokenHash).HasMaxLength(128);
            entity.Property(e => e.AuthenticatorSecret).HasMaxLength(1024);
            entity.Property(e => e.RecoveryCodesHash).HasMaxLength(4096);
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
            entity.HasIndex(e => e.IsTestAccount);
            entity.HasIndex(e => e.ReferredByAmbassadeurUserId);
            entity.HasIndex(e => e.SchoolId);
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
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.Property(e => e.WorkTypeLabels).HasMaxLength(512);
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(64);
            entity.Property(e => e.ContactWhatsApp).HasMaxLength(64);
            entity.Property(e => e.PreferredPaymentMethod).HasMaxLength(32);
            entity.Property(e => e.CommissionDirectRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.CommissionIndirectRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.CommissionAmbassadeurRateSnapshot).HasPrecision(5, 4);
            entity.Property(e => e.CommissionYear2RateSnapshot).HasPrecision(9, 4);
            entity.Property(e => e.CommissionYear3RateSnapshot).HasPrecision(9, 4);
            entity.Property(e => e.Location)
                .HasConversion(new GeoPointConverter())
                .HasColumnType("geometry(Point, 4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.Property(e => e.LocationSource);
            entity.HasIndex(e => e.KvkEstablishmentId).IsUnique();
            entity.HasIndex(e => e.KvkVerificationStatus);
            entity.HasIndex(e => e.VerificationStatus);
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
            entity.HasIndex(e => e.IsTestData);
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
            entity.HasIndex(e => e.IsTestData);
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
            entity.Property(e => e.SnapshotDiplomaEvaluationsJson).HasMaxLength(4000);
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
            entity.HasMany(e => e.StatusHistory)
                .WithOne(h => h.Application)
                .HasForeignKey(h => h.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).HasConversion<int>();
            entity.Property(e => e.ActorKind).HasConversion<int>();
            entity.Property(e => e.FromStatus).HasConversion<int?>();
            entity.Property(e => e.ToStatus).HasConversion<int?>();
            entity.HasIndex(e => new { e.ApplicationId, e.OccurredAtUtc });
            // At most one EmployerViewed row per application (D4).
            entity.HasIndex(e => e.ApplicationId)
                .IsUnique()
                .HasFilter("\"Kind\" = 2")
                .HasDatabaseName("IX_ApplicationStatusHistories_ApplicationId_EmployerViewed");
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

        modelBuilder.Entity<CandidateDiplomaEvaluation>(entity =>
        {
            entity.ToTable("CandidateDiplomaEvaluations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiplomaTitle).HasMaxLength(200);
            entity.Property(e => e.IssuingBody).HasMaxLength(16).IsRequired();
            entity.Property(e => e.IssuingBodyOther).HasMaxLength(120);
            entity.Property(e => e.EquivalentLevelText).HasMaxLength(200).IsRequired();
            entity.Property(e => e.EquivalentLevelCode).HasMaxLength(32);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(80).IsRequired();
            entity.Property(e => e.DocumentFileName).HasMaxLength(180);
            entity.Property(e => e.DocumentContentType).HasMaxLength(128);
            entity.HasIndex(e => new { e.UserId, e.CreatedAtUtc });
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

        modelBuilder.Entity<ReferenceConfirmation>(entity =>
        {
            entity.ToTable("ReferenceConfirmations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleTitle).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.ConsentVersion).HasMaxLength(32).IsRequired();
            entity.Property(e => e.PeriodText).HasMaxLength(120);
            entity.Property(e => e.DidWell).HasMaxLength(400);
            entity.Property(e => e.WorkAgain).HasMaxLength(16);
            entity.Property(e => e.ExtraText).HasMaxLength(400);
            entity.HasIndex(e => e.CandidateReferenceId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.CandidateReference)
                .WithMany()
                .HasForeignKey(e => e.CandidateReferenceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReferenceConfirmationToken>(entity =>
        {
            entity.ToTable("ReferenceConfirmationTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasOne(e => e.ReferenceConfirmation)
                .WithMany()
                .HasForeignKey(e => e.ReferenceConfirmationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReferenceConfirmationConsentLog>(entity =>
        {
            entity.ToTable("ReferenceConfirmationConsentLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Actor).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(16).IsRequired();
            entity.Property(e => e.ConsentVersion).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Text).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReferenceMisuseReport>(entity =>
        {
            entity.ToTable("ReferenceMisuseReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.HasIndex(e => e.ReferenceConfirmationId);
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
            entity.Property(e => e.Source).HasMaxLength(16).IsRequired().HasDefaultValue(CompanyCultureSources.Full);
            entity.Property(e => e.AnswersJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(e => e.CompanyId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyValuesProfile>(entity =>
        {
            entity.ToTable("CompanyValuesProfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardIdsJson).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.CompanyId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyEngagementClaim>(entity =>
        {
            entity.ToTable("CompanyEngagementClaims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProofUrl).HasMaxLength(500);
            entity.Property(e => e.ProofText).HasMaxLength(300);
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CheckedSource).HasMaxLength(16);
            entity.Property(e => e.RemovedReason).HasMaxLength(500);
            entity.HasIndex(e => new { e.CompanyId, e.ItemId }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CheckedByUser)
                .WithMany()
                .HasForeignKey(e => e.CheckedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CompanyEngagementReport>(entity =>
        {
            entity.ToTable("CompanyEngagementReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ReporterEmail).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.ClaimId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.Claim)
                .WithMany()
                .HasForeignKey(e => e.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LenderRegistration>(entity =>
        {
            entity.ToTable("LenderRegistrations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(32);
            entity.Property(e => e.Reference).HasMaxLength(128);
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => new { e.CompanyId, e.CreatedAtUtc });
            entity.HasIndex(e => e.Status);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DecidedByUser)
                .WithMany()
                .HasForeignKey(e => e.DecidedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<CandidatePrivatePreferences>(entity =>
        {
            entity.ToTable("CandidatePrivatePreferences");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.DislikesJson).HasMaxLength(2000).IsRequired().HasDefaultValue("[]");
            entity.Property(e => e.CustomDislikesJson).HasMaxLength(2000).IsRequired().HasDefaultValue("[]");
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
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired().HasDefaultValue(CareerPlanStatuses.Active);
            entity.Property(e => e.PlanLanguage).HasMaxLength(8).IsRequired().HasDefaultValue("nl");
            entity.Property(e => e.DreamSource).HasMaxLength(16).IsRequired().HasDefaultValue(CareerDreamSources.Wizard);
            entity.Property(e => e.DreamCatalogKey).HasMaxLength(80);
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_CandidateCareerPlans_UserId_Active")
                .HasFilter("\"Status\" = 'Active'")
                .IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Status, e.ArchivedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateCareerGeneration>(entity =>
        {
            entity.ToTable("CandidateCareerGenerations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DreamKey).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Outcome).HasMaxLength(16).IsRequired().HasDefaultValue(CareerGenerationOutcomes.Ok);
            entity.HasIndex(e => new { e.UserId, e.StartedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateOnboarding>(entity =>
        {
            entity.ToTable("CandidateOnboardings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).HasMaxLength(64);
            entity.Property(e => e.StepsJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.WizardVersion).HasDefaultValue(1);
            entity.Property(e => e.FinishReached).HasDefaultValue(false);
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
            entity.Property(e => e.UndoFingerprint).HasMaxLength(64);
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
            entity.Property(e => e.ReportJson).HasColumnType("text").IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Kind }).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateAssessmentAdjustment>(entity =>
        {
            entity.ToTable("CandidateAssessmentAdjustments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(128);
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Variant });
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Variant, e.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.HasIndex(e => e.AttemptId)
                .IsUnique()
                .HasFilter("\"AttemptId\" IS NOT NULL");
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CandidateAssessmentAttempt>(entity =>
        {
            entity.ToTable("CandidateAssessmentAttempts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Origin).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AnswersJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.ScoresJson).HasColumnType("text");
            entity.Property(e => e.ReportJson).HasColumnType("text");
            entity.Property(e => e.PreviousSnapshotJson).HasColumnType("text");
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Variant, e.Status });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssessmentNormSnapshot>(entity =>
        {
            entity.ToTable("AssessmentNormSnapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Domain).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.Kind, e.Domain, e.ComputedAtUtc });
        });

        modelBuilder.Entity<DeepAnalysisCheckout>(entity =>
        {
            entity.ToTable("DeepAnalysisCheckouts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AmountEuro).HasPrecision(10, 2);
            entity.Property(e => e.PaymentMethod).HasMaxLength(32);
            entity.Property(e => e.ProviderStatus).HasMaxLength(32);
            entity.Property(e => e.WaiverTextVersion).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Locale).HasMaxLength(8).IsRequired();
            entity.HasIndex(e => e.PaymentId)
                .IsUnique()
                .HasFilter("\"PaymentId\" <> ''");
            entity.HasIndex(e => new { e.UserId, e.Kind, e.Status });
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Ignore(e => e.Invoice);
        });

        modelBuilder.Entity<ConsumerPurchaseInvoice>(entity =>
        {
            entity.ToTable("ConsumerPurchaseInvoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(40).IsRequired();
            entity.Property(e => e.CustomerName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CustomerCountry).HasMaxLength(2);
            entity.Property(e => e.Description).HasMaxLength(256).IsRequired();
            entity.Property(e => e.MolliePaymentId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.PaymentMethod).HasMaxLength(32);
            entity.Property(e => e.VatRate).HasPrecision(5, 4);
            entity.Property(e => e.VatDeclarationStatusLabel).HasMaxLength(80);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.DeepAnalysisCheckoutId).IsUnique();
            entity.HasIndex(e => e.IssuedAt);
            entity.HasIndex(e => e.VatDeclarationId);
            entity.HasOne(e => e.Checkout)
                .WithMany()
                .HasForeignKey(e => e.DeepAnalysisCheckoutId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.VatDeclaration)
                .WithMany()
                .HasForeignKey(e => e.VatDeclarationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TalentContactRequest>(entity =>
        {
            entity.ToTable("TalentContactRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.CandidateDeclineReason)
                .HasMaxLength(Jobsy.Core.Rules.TalentContactDeclineReasons.MaxLength);
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
            entity.Property(e => e.DeepTestPriceCompetenceEuro).HasPrecision(10, 2);
            entity.Property(e => e.DeepTestPriceCareerEuro).HasPrecision(10, 2);
            entity.Property(e => e.DeepTestPriceValuesEuro).HasPrecision(10, 2);
            entity.Property(e => e.DeepTestPriceCultureEuro).HasPrecision(10, 2);
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
            entity.HasIndex(e => e.DeviceSessionId);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DeviceSession)
                .WithMany()
                .HasForeignKey(e => e.DeviceSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserDeviceSession>(entity =>
        {
            entity.ToTable("UserDeviceSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.PreviousRefreshTokenHash).HasMaxLength(64);
            entity.Property(e => e.RevokedReason).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.DeviceName).HasMaxLength(128);
            entity.HasIndex(e => e.RefreshTokenHash);
            entity.HasIndex(e => e.PreviousRefreshTokenHash);
            entity.HasIndex(e => e.FamilyId);
            entity.HasIndex(e => new { e.UserId, e.RevokedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MfaTrustedDevice>(entity =>
        {
            entity.ToTable("MfaTrustedDevices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.UserAgentSummary).HasMaxLength(120);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.RevokedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeviceLoginHandoff>(entity =>
        {
            entity.ToTable("DeviceLoginHandoffs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ReturnUrl).HasMaxLength(2048);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.HasIndex(e => e.CodeHash).IsUnique();
            entity.HasIndex(e => e.ExpiresAtUtc);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailSignInChallenge>(entity =>
        {
            entity.ToTable("EmailSignInChallenges");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailNormalized).HasMaxLength(320).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Purpose).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(60);
            entity.Property(e => e.ReferralCode).HasMaxLength(64);
            entity.Property(e => e.ReturnUrl).HasMaxLength(2048);
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => new { e.EmailNormalized, e.CreatedAtUtc });
            entity.HasIndex(e => e.ExpiresAtUtc);
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

        modelBuilder.Entity<OneTimeLink>(entity =>
        {
            entity.ToTable("OneTimeLinks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Purpose).HasConversion<int>();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.Purpose, e.UserId, e.UsedAtUtc });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailOptOut>(entity =>
        {
            entity.ToTable("EmailOptOuts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => new { e.EmailHash, e.Category }).IsUnique();
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

        modelBuilder.Entity<TokenRequest>(entity =>
        {
            entity.ToTable("TokenRequests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(TokenRequest.MaxNoteLength);
            entity.HasIndex(e => new { e.OrganisationCompanyId, e.Status });
            entity.HasIndex(e => new { e.BranchCompanyId, e.Status });
            entity.HasOne(e => e.OrganisationCompany)
                .WithMany()
                .HasForeignKey(e => e.OrganisationCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BranchCompany)
                .WithMany()
                .HasForeignKey(e => e.BranchCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HandledByUser)
                .WithMany()
                .HasForeignKey(e => e.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CandidateInsightsUnlock>(entity =>
        {
            entity.ToTable("CandidateInsightsUnlocks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceTokens).HasPrecision(10, 2);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => new { e.ScopeCompanyId, e.ExpiresAtUtc });
            entity.HasIndex(e => new { e.WalletCompanyId, e.ScopeKind, e.ScopeCompanyId });
            entity.HasOne(e => e.WalletCompany)
                .WithMany()
                .HasForeignKey(e => e.WalletCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ScopeCompany)
                .WithMany()
                .HasForeignKey(e => e.ScopeCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.TokenTransaction)
                .WithMany()
                .HasForeignKey(e => e.TokenTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CandidateInsightsUnlockRequest>(entity =>
        {
            entity.ToTable("CandidateInsightsUnlockRequests");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BranchCompanyId, e.Status });
            entity.HasIndex(e => new { e.WalletCompanyId, e.Status });
            entity.HasOne(e => e.WalletCompany)
                .WithMany()
                .HasForeignKey(e => e.WalletCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BranchCompany)
                .WithMany()
                .HasForeignKey(e => e.BranchCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.HandledByUser)
                .WithMany()
                .HasForeignKey(e => e.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(e => e.PayoutMinimumEuro).HasPrecision(10, 2);
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
            entity.Property(e => e.MaintenanceNote).HasMaxLength(200);
            entity.Property(e => e.EmployersEnabled).HasDefaultValue(false);
            entity.Property(e => e.CandidatePassportEnabled).HasDefaultValue(true);
            entity.Property(e => e.PassportPartnersEnabled).HasDefaultValue(false);
            entity.Property(e => e.PassportPdfV2Enabled).HasDefaultValue(false);
            entity.Property(e => e.PhoneVerificationEnabled).HasDefaultValue(false);
        });

        modelBuilder.Entity<PassportPartner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.LogoContentType).HasMaxLength(32);
            entity.Property(e => e.TermsVersion).HasMaxLength(32);
            entity.Property(e => e.MaxBranches).HasDefaultValue(1);
            entity.HasIndex(e => e.CompanyId).IsUnique();
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.TermsAcceptedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PassportPartnerCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodeLookupHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CodeDisplay).HasMaxLength(7).IsRequired();
            entity.HasIndex(e => e.CodeLookupHash).IsUnique();
            entity.HasOne(e => e.PassportPartner)
                .WithMany()
                .HasForeignKey(e => e.PassportPartnerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.BranchCompany)
                .WithMany()
                .HasForeignKey(e => e.BranchCompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PassportPartnerCandidateLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConsentVersion).HasMaxLength(32);
            entity.HasIndex(e => new { e.CandidateUserId, e.PassportPartnerId }).IsUnique();
            entity.HasOne(e => e.Candidate)
                .WithMany()
                .HasForeignKey(e => e.CandidateUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PassportPartner)
                .WithMany()
                .HasForeignKey(e => e.PassportPartnerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PartnerCode)
                .WithMany()
                .HasForeignKey(e => e.PartnerCodeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PassportAccessLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => e.CandidateUserId);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.CandidateUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.ViewerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<PassportPartner>()
                .WithMany()
                .HasForeignKey(e => e.PassportPartnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlatformCompanySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.LegalName).HasMaxLength(200);
            entity.Property(e => e.TradeName).HasMaxLength(200);
            entity.Property(e => e.Slogan).HasMaxLength(240);
            entity.Property(e => e.Address).HasMaxLength(240);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(120);
            entity.Property(e => e.Country).HasMaxLength(80);
            entity.Property(e => e.PostalStreet).HasMaxLength(240);
            entity.Property(e => e.PostalPostalCode).HasMaxLength(20);
            entity.Property(e => e.PostalCity).HasMaxLength(120);
            entity.Property(e => e.KvkNumber).HasMaxLength(32);
            entity.Property(e => e.VatNumber).HasMaxLength(32);
            entity.Property(e => e.Phone).HasMaxLength(40);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.SupportEmail).HasMaxLength(200);
            entity.Property(e => e.PrivacyEmail).HasMaxLength(200);
            entity.Property(e => e.VatBufferIban).HasMaxLength(34);
        });

#pragma warning disable CS0618 // Table kept until the public-pages 08 cleanup migration drops it.
        modelBuilder.Entity<AboutPageSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Lead).HasMaxLength(400);
            entity.Property(e => e.BodyHtml).IsRequired();
        });
#pragma warning restore CS0618

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
            entity.HasIndex(e => e.TokenPurchaseInvoiceId)
                .IsUnique()
                .HasFilter("\"TokenPurchaseInvoiceId\" IS NOT NULL");
            entity.HasIndex(e => e.ConsumerPurchaseInvoiceId)
                .IsUnique()
                .HasFilter("\"ConsumerPurchaseInvoiceId\" IS NOT NULL");
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.VatBufferTransfers)
                .HasForeignKey(e => e.TokenPurchaseInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ConsumerInvoice)
                .WithMany()
                .HasForeignKey(e => e.ConsumerPurchaseInvoiceId)
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
            entity.Property(e => e.SelectedEstablishmentIdsJson).HasMaxLength(4000);
            entity.Property(e => e.RepresentationConsentVersion).HasMaxLength(64);
            entity.Property(e => e.PreferredLoginProvider).HasMaxLength(32);
            entity.HasIndex(e => e.ActivationToken).IsUnique();
            entity.HasIndex(e => e.ContactEmail);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EmailVerificationExpiresAt);
            entity.HasIndex(e => e.SalesManagerUserId);
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

        modelBuilder.Entity<CompanyVerificationLetter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.AddressLine1).HasMaxLength(256).IsRequired();
            entity.Property(e => e.AddressLine2).HasMaxLength(256);
            entity.Property(e => e.PostalCode).HasMaxLength(16).IsRequired();
            entity.Property(e => e.City).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(2).IsRequired();
            entity.Property(e => e.ProviderLetterId).HasMaxLength(128);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.KvkNumber);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ResendOfLetter).WithMany().HasForeignKey(e => e.ResendOfLetterId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CompanyVerificationEmailChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyManualVerificationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(4000);
            entity.Property(e => e.AttachmentIdsJson).HasMaxLength(2000);
            entity.Property(e => e.DecisionNote).HasMaxLength(2000);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.DecidedAtUtc);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DecidedByUser).WithMany().HasForeignKey(e => e.DecidedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CompanyVerificationDecision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(2000);
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.KvkNumber);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AdminUser).WithMany().HasForeignKey(e => e.AdminUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KvkUsageDaily>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CallType).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => new { e.Date, e.CallType }).IsUnique();
        });

        modelBuilder.Entity<DismissedVestigingSuggestion>(entity =>
        {
            entity.ToTable("DismissedVestigingSuggestions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KvkEstablishmentId).HasMaxLength(40).IsRequired();
            entity.HasIndex(e => new { e.CompanyId, e.KvkEstablishmentId }).IsUnique();
            entity.HasIndex(e => e.HiddenUntilUtc);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EstablishmentTakeoverRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DecisionNote).HasMaxLength(1024);
            entity.Property(e => e.LetterCodeHash).HasMaxLength(128);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Kind);
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

        modelBuilder.Entity<CompanyAccessRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KvkNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RequestedVestigingIdsJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.RequesterName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RequesterFunction).HasMaxLength(200);
            entity.Property(e => e.RequesterEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.RequesterPhone).HasMaxLength(40);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.EmailConfirmationCodeHash).HasMaxLength(128);
            entity.Property(e => e.RequesterToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.DecisionReason).HasMaxLength(1000);
            entity.Property(e => e.GrantedVestigingIdsJson).HasMaxLength(4000);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.RequesterEmail);
            entity.HasIndex(e => new { e.TargetCompanyId, e.RequesterEmail, e.Status });
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
            entity.HasIndex(e => e.LockoutUntil);
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
            entity.Property(e => e.Iban).HasMaxLength(512).HasConversion(IbanValueConverter);
            entity.Property(e => e.TrackingCode).HasMaxLength(32);
            entity.Property(e => e.AgreementVersion).HasMaxLength(64);
            entity.Property(e => e.PayoutAccountHolderName).HasMaxLength(70);
            entity.Property(e => e.EmailPrefsJson).HasMaxLength(2048);
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
            entity.Property(e => e.Iban).HasMaxLength(512).HasConversion(IbanValueConverter);
            entity.Property(e => e.TrackingCode).HasMaxLength(32);
            entity.Property(e => e.AgreementVersion).HasMaxLength(64);
            entity.Property(e => e.PayoutAccountHolderName).HasMaxLength(70);
            entity.Property(e => e.EmailPrefsJson).HasMaxLength(2048);
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
            entity.Property(e => e.Iban).HasMaxLength(512).HasConversion(IbanValueConverter);
            entity.Property(e => e.TrackingCode).HasMaxLength(32).IsRequired();
            entity.Property(e => e.AgreementVersion).HasMaxLength(64);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.TrackingCode).IsUnique();
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<PartnerAffiliateProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalDataAccessLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActorRole).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Resource).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(512);
            entity.Property(e => e.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.IpHash).HasMaxLength(64);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.ActorUserId);
            entity.HasIndex(e => e.SubjectUserId);
            entity.HasIndex(e => e.SubjectPupilCodeId);
            entity.HasIndex(e => new { e.Resource, e.OccurredAt });
        });

        modelBuilder.Entity<AdminAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActorRole).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActorKind).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TargetId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TargetLabel).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.DetailsJson).HasMaxLength(4096);
            entity.Property(e => e.Result).HasMaxLength(16).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.IpHash).HasMaxLength(64);
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => new { e.TargetType, e.TargetId });
            entity.HasIndex(e => e.ActorUserId);
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.ToTable("ContentReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetType).HasConversion<int>();
            entity.Property(e => e.Reason).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.TargetKvk).HasMaxLength(20);
            entity.Property(e => e.TargetLabel).HasMaxLength(256);
            entity.Property(e => e.Details).HasMaxLength(ContentReportRules.DetailsMaxLength);
            entity.Property(e => e.DecisionReason).HasMaxLength(ContentReportRules.DecisionReasonMaxLength);
            entity.Property(e => e.ReporterEmail).HasMaxLength(ContentReportRules.ReporterEmailMaxLength);
            entity.HasIndex(e => new { e.Status, e.CreatedAtUtc });
            entity.HasIndex(e => new { e.TargetType, e.TargetId });
        });

        modelBuilder.Entity<SupportAccessGrant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(512).IsRequired();
            entity.Property(e => e.TicketReference).HasMaxLength(128);
            entity.Property(e => e.Scope).HasConversion<int>();
            entity.HasIndex(e => new { e.AdminUserId, e.ExpiresAt });
            entity.HasIndex(e => e.SubjectUserId);
            entity.HasIndex(e => e.SubjectCompanyId);
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
            entity.Property(e => e.CandidateEmailSha256).HasMaxLength(64);
            entity.Property(e => e.ObjectionTokenHash).HasMaxLength(64);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.CandidateEmail);
            entity.HasIndex(e => new { e.CandidateEmail, e.Status });
            entity.HasIndex(e => e.CandidateEmailSha256);
            entity.HasIndex(e => e.ObjectionTokenHash);
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
            entity.Property(e => e.SourceRefundKey).HasMaxLength(80);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.HasIndex(e => e.SalesManagerUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.SourcePaymentId)
                .IsUnique()
                .HasFilter("\"SourcePaymentId\" IS NOT NULL");
            // Direct + indirect commissions may share a checkout; uniqueness is per SM + kind.
            entity.HasIndex(e => new { e.SourceTokenCheckoutId, e.SalesManagerUserId, e.Kind })
                .IsUnique()
                .HasFilter("\"SourceTokenCheckoutId\" IS NOT NULL AND \"SourceRefundKey\" IS NULL");
            entity.HasIndex(e => new { e.SourceTokenCheckoutId, e.SalesManagerUserId, e.Kind, e.SourceRefundKey })
                .IsUnique()
                .HasFilter("\"SourceRefundKey\" IS NOT NULL");
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
            entity.HasOne(e => e.SalesPayoutRequest)
                .WithMany()
                .HasForeignKey(e => e.SalesPayoutRequestId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CorrectsEntry)
                .WithMany()
                .HasForeignKey(e => e.CorrectsEntryId)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.HasOne(e => e.SelfBillingConsent)
                .WithMany()
                .HasForeignKey(e => e.SelfBillingConsentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalesSelfBillingConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TextSha256).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Version });
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesPayoutRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExportFileSha256).HasMaxLength(64);
            entity.Property(e => e.ProviderKey).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.RunDate)
                .IsUnique()
                .HasFilter("\"IsExtra\" = FALSE");
        });

        modelBuilder.Entity<SalesIbanChangePending>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EncryptedIban).HasMaxLength(512).IsRequired();
            entity.Property(e => e.HolderName).HasMaxLength(70).IsRequired();
            entity.Property(e => e.Method).HasMaxLength(16).IsRequired();
            entity.Property(e => e.EmailTokenHash).HasMaxLength(64);
            entity.Property(e => e.LastAcceptedTotpCodeHash).HasMaxLength(64);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EmailTokenHash);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesPayoutRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountExVat).HasPrecision(18, 2);
            entity.Property(e => e.VatAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalInclVat).HasPrecision(18, 2);
            entity.Property(e => e.MaskedIban).HasMaxLength(34).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.HasIndex(e => e.BeneficiaryUserId);
            entity.HasIndex(e => e.BeneficiaryUserId)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1, 2)");
            entity.HasOne(e => e.BeneficiaryUser)
                .WithMany()
                .HasForeignKey(e => e.BeneficiaryUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SalesPayoutRun)
                .WithMany(r => r.Requests)
                .HasForeignKey(e => e.SalesPayoutRunId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SelfBillingInvoice)
                .WithMany()
                .HasForeignKey(e => e.SelfBillingInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalesAttributionChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.CompanyId);
            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SalesLinkClickDaily>(entity =>
        {
            entity.HasKey(e => new { e.BeneficiaryUserId, e.Date, e.Channel });
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
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.AffiliateCode).HasMaxLength(120);
            entity.Property(e => e.ShowInPassport).HasDefaultValue(false);
            entity.Property(e => e.IsFree).HasDefaultValue(false);
            entity.Property(e => e.IsPartner).HasDefaultValue(false);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.ShowInPassport);
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

        ConfigureScholen(modelBuilder);
    }

    private static void ConfigureScholen(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>(entity =>
        {
            entity.ToTable("Schools");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.City).HasMaxLength(128).IsRequired();
            entity.Property(e => e.BrinCode).HasMaxLength(16);
            entity.Property(e => e.AllowedEmailDomains).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ProcessorAgreementVersion).HasMaxLength(64);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsTestData);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("SchoolClasses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(12).IsRequired();
            entity.Property(e => e.Level).HasConversion<int>();
            entity.Property(e => e.QuestionSet)
                .HasConversion<int>()
                .HasDefaultValue(PupilQuestionSet.Vo)
                // 0 is not a named question set. Without an explicit sentinel, EF Core warns
                // (20601) and would substitute the database default whenever the value is 0.
                .HasSentinel((PupilQuestionSet)0);
            entity.Property(e => e.TestWindow).HasConversion<int>();
            entity.Property(e => e.ParentalInfoTextVersion).HasMaxLength(64);
            entity.HasIndex(e => new { e.SchoolId, e.SchoolYearStart, e.Name }).IsUnique();
            entity.HasIndex(e => e.TestWindow);
            entity.HasIndex(e => e.IsTestData);
            entity.HasOne(e => e.School)
                .WithMany(s => s.Classes)
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherClassAssignment>(entity =>
        {
            entity.ToTable("TeacherClassAssignments");
            entity.HasKey(e => new { e.TeacherUserId, e.SchoolClassId });
            entity.HasOne(e => e.SchoolClass)
                .WithMany(c => c.TeacherAssignments)
                .HasForeignKey(e => e.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.TeacherUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PupilCode>(entity =>
        {
            entity.ToTable("PupilCodes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CodeLookupHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CodeProtected).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Status).HasConversion<int>();
            entity.HasIndex(e => new { e.SchoolClassId, e.CodeLookupHash }).IsUnique();
            entity.HasOne(e => e.SchoolClass)
                .WithMany(c => c.PupilCodes)
                .HasForeignKey(e => e.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PupilProgress>(entity =>
        {
            entity.ToTable("PupilProgresses");
            entity.HasKey(e => e.PupilCodeId);
            entity.Property(e => e.AnswersJson).IsRequired();
            entity.Property(e => e.LikesJson).IsRequired();
            entity.Property(e => e.DislikesJson).IsRequired();
            entity.Property(e => e.LikeOtherWord).HasMaxLength(24);
            entity.Property(e => e.DislikeOtherWord).HasMaxLength(24);
            entity.Property(e => e.DreamJobKey).HasMaxLength(64);
            entity.HasOne(e => e.PupilCode)
                .WithOne(c => c.Progress)
                .HasForeignKey<PupilProgress>(e => e.PupilCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PupilResult>(entity =>
        {
            entity.ToTable("PupilResults");
            entity.HasKey(e => e.PupilCodeId);
            entity.Property(e => e.HollandCode).HasMaxLength(8);
            entity.Property(e => e.TopValue).HasMaxLength(64);
            entity.Property(e => e.TopCulture).HasMaxLength(64);
            entity.Property(e => e.ScoringVersion).HasMaxLength(32).IsRequired();
            entity.Property(e => e.StoryTemplateVersion).HasMaxLength(32).IsRequired();
            entity.Property(e => e.DreamJobKey).HasMaxLength(64);
            entity.HasIndex(e => e.SchoolClassId);
            entity.HasOne(e => e.PupilCode)
                .WithOne(c => c.Result)
                .HasForeignKey<PupilResult>(e => e.PupilCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SchoolClassAggregate>(entity =>
        {
            entity.ToTable("SchoolClassAggregates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClassLabel).HasMaxLength(12).IsRequired();
            entity.Property(e => e.Level).HasConversion<int>();
            entity.Property(e => e.QuestionSet).HasConversion<int>();
            entity.HasIndex(e => new { e.SchoolId, e.SchoolYearStart, e.QuestionSet });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SchoolYearAggregate>(entity =>
        {
            entity.ToTable("SchoolYearAggregates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuestionSet).HasConversion<int>();
            entity.HasIndex(e => new { e.SchoolId, e.SchoolYearStart, e.QuestionSet });
            entity.HasOne<School>()
                .WithMany()
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SchoolRetentionRun>(entity =>
        {
            entity.ToTable("SchoolRetentionRuns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Outcome).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.RanAtUtc);
        });

        modelBuilder.Entity<SchoolStaffInvite>(entity =>
        {
            entity.ToTable("SchoolStaffInvites");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ClassIdsJson).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => new { e.SchoolId, e.Email });
            entity.HasOne(e => e.School)
                .WithMany()
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Encrypts IBAN at rest (Data Protection purpose Jobsy.Iban.v1).
    /// Configure <see cref="IbanEfProtection"/> at DI startup before first DbContext use.
    /// </summary>
    private static readonly ValueConverter<string?, string?> IbanValueConverter = new(
        v => IbanEfProtection.Protect(v),
        v => IbanEfProtection.Unprotect(v));
}
