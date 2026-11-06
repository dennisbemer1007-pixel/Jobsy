using Jobsy.Core.Enums;

namespace Jobsy.Core.Options;

public sealed class CompanyVerificationSettings
{
    public const string SectionName = "CompanyVerification";

    /// <summary>Default Stub — Pingen only when explicitly configured (never from Dev/CI/Acc by accident).</summary>
    public LetterProviderKind LetterProvider { get; set; } = LetterProviderKind.Stub;

    public string? PingenClientId { get; set; }
    public string? PingenClientSecret { get; set; }
    public string? PingenOrganisationId { get; set; }

    /// <summary>Staging or Production. Production only with production app settings.</summary>
    public string PingenEnvironment { get; set; } = "Staging";

    /// <summary>Prefer economy ("cheap"); set true only via config for express.</summary>
    public bool PingenPreferFastDelivery { get; set; }

    public int MonthlyLetterCap { get; set; } = 500;

    /// <summary>Alert admin when monthly sends reach this fraction of the cap (default 80 %).</summary>
    public double MonthlyLetterAlertRatio { get; set; } = 0.8;

    public int LetterValidityDays { get; set; } = 30;

    public int LetterResendAfterDays { get; set; } = 7;

    public int LetterMaxResends { get; set; } = 2;

    public int EmailCodeValidityMinutes { get; set; } = 10;

    public int EmailMaxSendsPerHour { get; set; } = 3;

    public int EmailCooldownAfterLockoutMinutes { get; set; } = 15;

    public string SupportEmail { get; set; } = "support@lobsy.nl";
}
