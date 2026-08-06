namespace Jobsy.Web.Models;

public class CompanySummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public string? KvkEstablishmentId { get; set; }
    public decimal TokenBalance { get; set; }
    public int ActiveVacancies { get; set; }
    public Guid? ParentCompanyId { get; set; }
    public bool TokensManagedByEnterprise { get; set; }
    public bool CsvBatchImportEnabled { get; set; }
    public bool DirectContactEnabled { get; set; }
    public bool ContactPreferMail { get; set; }
    public bool ContactPreferPhone { get; set; }
    public bool ContactPreferWhatsApp { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactWhatsApp { get; set; }
    public string? PreferredPaymentMethod { get; set; }
}

public class CompanyBillingHistoryItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CheckoutId { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public int PackSize { get; set; }
    public decimal AmountExVatEuro { get; set; }
    public decimal VatAmountEuro { get; set; }
    public decimal TotalAmountEuro { get; set; }
    public DateTime IssuedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CompanyApiKeyItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminApiKeyItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeneratedApiKeyItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string PlaintextKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Warning { get; set; } = string.Empty;
}

public class EmailApiKeyResultItem
{
    public Guid Id { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool Sent { get; set; }
}

public class TokenBalance
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Guid? ParentCompanyId { get; set; }
    public bool TokensManagedByEnterprise { get; set; }
}

public class ApplicationItem
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string VacancyTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string PreferredTransport { get; set; } = string.Empty;
    public int EstimatedTravelMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RespondedAt { get; set; }
}

public class ApplyResultItem
{
    public ApplicationItem Application { get; set; } = new();
    public bool ConfirmationEmailQueued { get; set; }
    public bool AuthenticatorStubUsed { get; set; }
    public bool RequiresVerification { get; set; }
    public bool VerificationCodeSent { get; set; }
    public EmployerDirectContactItem? DirectContact { get; set; }
    public bool RequiresSafetyNetConfirmation { get; set; }
    public int? MatchPercent { get; set; }
    public string? MatchBreakdownJson { get; set; }
    public string? SafetyNetMessage { get; set; }
}

public class EmployerDirectContactItem
{
    public bool Available { get; set; }
    public bool OfferMail { get; set; }
    public bool OfferPhone { get; set; }
    public bool OfferWhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? WhatsAppUrl { get; set; }
}

public class VacancyContactPreferenceItem
{
    public Guid VacancyId { get; set; }
    public bool OverrideContactPreference { get; set; }
    public bool DirectContactEnabled { get; set; }
    public bool ContactPreferMail { get; set; }
    public bool ContactPreferPhone { get; set; }
    public bool ContactPreferWhatsApp { get; set; }
}

public class EmployerApplicationItem
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string VacancyTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PreferredTransport { get; set; } = string.Empty;
    public int EstimatedTravelMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? RespondedAt { get; set; }
    public string? CandidateCity { get; set; }
    public double? DistanceKm { get; set; }
    public string? PreferencesSummary { get; set; }
    public string? CandidateName { get; set; }
    public string? CandidateEmail { get; set; }
    public string? CandidateAddress { get; set; }
    public bool PiiRevealed { get; set; }
    public bool WorkPermitConfirmed { get; set; }
    public string? SnapshotAvailabilityJson { get; set; }
    public string? SnapshotDrivingLicenses { get; set; }
    public string? SnapshotEducations { get; set; }
    public string? SnapshotAboutMe { get; set; }
    public int CandidateEmployerCount { get; set; }
    public int? MatchPercent { get; set; }
    public string? MatchBreakdownJson { get; set; }
    public bool ViaSafetyNet { get; set; }
    public string? Motivation { get; set; }
    public bool LegalEligible { get; set; } = true;
    public string? StudentNumber { get; set; }
    public string? SchoolEmail { get; set; }
    public string? StudyProgram { get; set; }
    public string? StudyYear { get; set; }
    public string? ExclusivityValidationStatus { get; set; }
    public bool CvPdfAvailable { get; set; }
}

public class TokenPackItem
{
    public int PackSize { get; set; }
    public decimal PriceEuro { get; set; }
}

public class TokenSpendCostItem
{
    public string Reason { get; set; } = string.Empty;
    public decimal CostTokens { get; set; }
}

public class TokenLogItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public string? Note { get; set; }
    public Guid? VacancyId { get; set; }
    public Guid? BranchCompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CheckoutResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public int PackSize { get; set; }
    public decimal AmountEuro { get; set; }
    public bool IsStub { get; set; }
    public Guid CheckoutId { get; set; }
    public string? PaymentMethod { get; set; }
}

public class CompleteCheckoutResult
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Guid CheckoutId { get; set; }
    public PendingActionResult? PendingAction { get; set; }
}

public class PendingActionResult
{
    public Guid VacancyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? Message { get; set; }
    public int PushBomRecipientCount { get; set; }
}

public class TokenTopUpQuote
{
    public Guid CompanyId { get; set; }
    public decimal Balance { get; set; }
    public decimal RequiredTokens { get; set; }
    public decimal Deficit { get; set; }
    public int ExactMatchTokens { get; set; }
    public decimal ExactMatchPriceEuro { get; set; }
    public List<TokenPackItem> BulkPacks { get; set; } = [];
}

public class InsufficientTokensInfo
{
    public string Code { get; set; } = "InsufficientTokens";
    public string Message { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VacancyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public decimal RequiredTokens { get; set; }
    public decimal Balance { get; set; }
    public decimal Deficit { get; set; }
    public bool Highlight { get; set; }
    public bool PushBom { get; set; }
    public bool Extend { get; set; }
}

public class PendingActionCheckoutRequest
{
    public Guid VacancyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Highlight { get; set; }
    public bool PushBom { get; set; }
    public bool Extend { get; set; }
    public decimal? RequiredTokens { get; set; }
}

public class KvkEstablishmentsLookupResult
{
    public string Status { get; set; } = "Ok";
    public string? Message { get; set; }
    public List<KvkEstablishmentItem> Establishments { get; set; } = [];

    public bool IsUnavailable =>
        Status.Equals("Unavailable", StringComparison.OrdinalIgnoreCase);
}

public class KvkEstablishmentItem
{
    public string KvkNumber { get; set; } = string.Empty;
    public string EstablishmentNumber { get; set; } = string.Empty;
    public string KvkEstablishmentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsInUse { get; set; }
    public List<string>? SbiCodes { get; set; }

    public bool IsIntermediarySbi =>
        SbiCodes?.Any(s =>
        {
            var digits = new string((s ?? "").Where(char.IsDigit).ToArray());
            return digits.StartsWith("78", StringComparison.Ordinal);
        }) == true;
}

public class RegistrationSubmitResult
{
    public Guid RegistrationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresTakeover { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActivationUrl { get; set; }
    public DateTime? VerificationExpiresAt { get; set; }
}

public class RegistrationActivationResult
{
    public Guid RegistrationId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public List<Guid> CompanyIds { get; set; } = [];
    public string? TemporaryPassword { get; set; }
    public Guid? OrganizationCompanyId { get; set; }
    public Guid? BranchCompanyId { get; set; }
    public bool UsedChosenPassword { get; set; }
    public bool EmailVerifiedAwaitingTakeover { get; set; }
    public bool WelcomeTokenGranted { get; set; }
    public DateOnly? FreePublishUntil { get; set; }
}

public class TakeoverInboxItem
{
    public Guid TakeoverId { get; set; }
    public Guid RegistrationId { get; set; }
    public Guid TargetCompanyId { get; set; }
    public string TargetCompanyName { get; set; } = string.Empty;
    public string KvkEstablishmentId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TakeoverDecisionResult
{
    public Guid TakeoverId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? OrganizationCompanyId { get; set; }
    public Guid? BranchCompanyId { get; set; }
}

public class RegionItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationCompanyId { get; set; }
    public string OrganizationCompanyName { get; set; } = string.Empty;
    public List<RegionCompanyItem> Companies { get; set; } = [];
}

public class RegionCompanyItem
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}

public class SalaryTableItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSystemWml { get; set; }
    public int VacancyCount { get; set; }
    public List<Guid> AllowedBranchIds { get; set; } = [];
    public List<string> AllowedBranchNames { get; set; } = [];
    public List<SalaryRateItem> Rates { get; set; } = [];
    public List<SalaryTableChangeLogItem>? ChangeLogs { get; set; }
}

public class SalaryRateItem
{
    public Guid Id { get; set; }
    public int AgeYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SalaryTableChangeLogItem
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SalaryTableVacancyItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public record UpsertSalaryTableForm(
    Guid? Id,
    Guid CompanyId,
    string Name,
    bool IsActive,
    IReadOnlyList<UpsertSalaryRateForm>? Rates = null,
    IReadOnlyList<Guid>? AllowedBranchIds = null);

public record UpsertSalaryRateForm(Guid? Id, int AgeYears, decimal HourlyRate, string Label);

public class CompanyUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public List<Guid> MembershipCompanyIds { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public string? TemporaryPassword { get; set; }
    public string? LoginUrl { get; set; }
}

public record InviteUserForm(
    string Email,
    string FullName,
    string Role,
    Guid? PrimaryCompanyId,
    Guid[]? MembershipCompanyIds = null,
    Guid? RegionId = null);

public record UpdateCompanyUserForm(
    string FullName,
    string Role,
    Guid? PrimaryCompanyId,
    Guid[]? MembershipCompanyIds = null,
    bool IsActive = true);

public class WageRateItem
{
    public Guid Id { get; set; }
    public int AgeYears { get; set; }
    public decimal HourlyRate { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
}

public class WageCheckResult
{
    public decimal HourlyWage { get; set; }
    public int AgeYears { get; set; }
    public decimal Minimum { get; set; }
    public bool MeetsMinimum { get; set; }
}

public class AdminCompanyItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string Type { get; set; } = "Employer";
    public Guid? ParentCompanyId { get; set; }
    public int UserCount { get; set; }
    public int ActiveVacancyCount { get; set; }
    public int TotalVacancyCount { get; set; }
    public int ApplicationCount { get; set; }
    public decimal TokenBalance { get; set; }
    public Guid? SalesManagerUserId { get; set; }
    public string? SalesManagerName { get; set; }
}

public class AdminUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyType { get; set; }
    public bool IsEarlyAdapter { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> MembershipCompanyIds { get; set; } = [];
}

public class AdminVacancyItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public bool IsHighlighted { get; set; }
    public int ExtensionCount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int ImpressionCount { get; set; }
    public int ClickCount { get; set; }
    public int ShareCount { get; set; }
    public int ApplicationCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsExtended { get; set; }
    public string CreatedVia { get; set; } = "Manual";
}

public class PlatformLogItem
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UnsubscribeReasonOption
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool RequiresOtherText { get; set; }
}

public class TokenPricingSettings
{
    public List<TokenPackSetting> Packs { get; set; } = [];
    public List<TokenCostSetting> Costs { get; set; } = [];
    public List<EarlyAdapterRuleItem> EarlyAdapterRules { get; set; } = [];
    public PushBomSettingsItem? PushBomSettings { get; set; }
    public List<PushBomPricingTierItem> PushBomPricingTiers { get; set; } = [];
}

public class TokenPackSetting
{
    public Guid Id { get; set; }
    public int PackSize { get; set; }
    public decimal PriceEuro { get; set; }
    public bool IsActive { get; set; }
}

public class TokenCostSetting
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal CostTokens { get; set; }
    public bool IsActive { get; set; }
}

public class PushBomSettingsItem
{
    public Guid Id { get; set; }
    public double RadiusKm { get; set; } = 10;
    public int MaxTravelMinutes { get; set; } = 30;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class PushBomPricingTierItem
{
    public Guid Id { get; set; }
    public int MinCandidates { get; set; }
    public int? MaxCandidates { get; set; }
    public decimal CostTokens { get; set; }
    public bool IsActive { get; set; }
}

public class PushBomPreview
{
    public int CandidateCount { get; set; }
    public decimal CostTokens { get; set; }
    public double RadiusKm { get; set; }
    public int MaxTravelMinutes { get; set; }
    public bool HasPricing { get; set; }
    public decimal TokenBalance { get; set; }
    public bool CanAfford { get; set; }
}

public class EarlyAdapterRuleItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MonthlyGrantTokens { get; set; }
    public decimal PurchaseDiscountPercent { get; set; }
    public bool IsActive { get; set; }
}

public class SemiAnnualWageUpdateResult
{
    public DateOnly EffectiveFrom { get; set; }
    public int RatesUpdated { get; set; }
    public string Message { get; set; } = string.Empty;
}
