namespace Jobsy.Api.Security;

/// <summary>Hard caps on anonymous list endpoints (anti-scraping; does not replace UX pagination).</summary>
public static class PublicApiLimits
{
    /// <summary>Masterdata options returned in one anonymous response.</summary>
    public const int MaxMasterdataOptions = 500;
}
