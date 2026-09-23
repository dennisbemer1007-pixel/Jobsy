namespace Jobsy.Core.Enums;

public enum VacancySource
{
    Manual = 0,
    Api = 1,
    Csv = 2,
    /// <summary>Approved listing from the ATS scrape pipeline (direct employer sites).</summary>
    Ats = 3
}
