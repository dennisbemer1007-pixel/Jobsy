namespace Jobsy.Core.Rules;

public static class AtsVacancyRules
{
    /// <summary>Hard TTL for scraped listings (and approved vacancies from ATS).</summary>
    public const int TimeToLiveDays = 30;

    public static DateTime DefaultExpiresAt(DateTime scrapedAtUtc)
        => scrapedAtUtc.AddDays(TimeToLiveDays);
}
