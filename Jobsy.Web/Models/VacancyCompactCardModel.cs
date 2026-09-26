namespace Jobsy.Web.Models;

/// <summary>Client-side card payload matching /api/vacancies/{id}/card JSON.</summary>
public sealed class VacancyCompactCardModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? OfferedByLabel { get; set; }
    public string Place { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
    public string? LogoUrl { get; set; }
    public decimal? HourlyWage { get; set; }
    public bool WageVisible { get; set; } = true;
    public int? TravelMinutes { get; set; }
    public int? MatchPercent { get; set; }
    public string? MatchColorBand { get; set; }
}
