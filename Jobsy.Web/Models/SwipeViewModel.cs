using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Web.Models;

/// <summary>
/// View-model for the mobile Match &amp; Swipe card. Null-safe and mappable from
/// <see cref="VacancyListItem"/> / API payloads.
/// </summary>
public sealed class SwipeViewModel
{
    public Guid? VacancyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string? ImageUrl { get; set; }

    public string JobTitle { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    /// <summary>Short inviting line explaining why this candidate fits the role.</summary>
    public string? WhyYouFit { get; set; }

    public string? Location { get; set; }
    public double? DistanceKm { get; set; }
    public int? TravelTimeMinutes { get; set; }
    public string? TransportMode { get; set; }

    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    /// <summary>Display period, e.g. "per maand" or "per uur".</summary>
    public string SalaryPeriod { get; set; } = "per maand";
    public decimal? HourlyWage { get; set; }
    public bool WageVisible { get; set; } = true;

    public decimal? HoursMin { get; set; }
    public decimal? HoursMax { get; set; }

    public List<string> Tags { get; set; } = [];

    public int? MatchPercentage { get; set; }
    public bool ShowMatchPercentage { get; set; }
    public string? FitGate { get; set; }
    public string? FitBand { get; set; }
    public List<string> FitWhyKinds { get; set; } = [];
    public CandidateFitDimensionsModel? FitDimensions { get; set; }
    /// <summary>Localization key for "Staat lager: …" (candidate-own).</summary>
    public string? RankLowerReason { get; set; }
    public bool IsHiddenMode { get; set; }
    public bool TravelToBureau { get; set; }

    public string? LocationPrimary =>
        string.IsNullOrWhiteSpace(Location) ? null : Location.Trim();

    public string? LocationSecondary =>
        DistanceKm is null
            ? (TravelTimeMinutes is null
                ? null
                : $"{TravelTimeMinutes} min{(string.IsNullOrWhiteSpace(TransportMode) ? "" : $" · {TransportMode}")}")
            : $"{FormatDistance(DistanceKm.Value)}{(TravelTimeMinutes is null ? "" : $" · {TravelTimeMinutes} min")}";

    public string? SalaryPrimary
    {
        get
        {
            if (HourlyWage is decimal hourly && hourly > 0 && WageVisible)
            {
                return $"€ {FormatMoney(hourly)}";
            }

            if (SalaryMin is null && SalaryMax is null)
            {
                return null;
            }

            if (SalaryMin is not null && SalaryMax is not null && SalaryMin != SalaryMax)
            {
                return $"{FormatMoney(SalaryMin.Value)}-{FormatMoney(SalaryMax.Value)}";
            }

            return FormatMoney(SalaryMin ?? SalaryMax ?? 0);
        }
    }

    public string? SalarySecondary =>
        HourlyWage is > 0 && WageVisible
            ? null
            : (string.IsNullOrWhiteSpace(SalaryPeriod) ? null : SalaryPeriod.Trim());

    public string? HoursPrimary
    {
        get
        {
            if (HoursMin is null && HoursMax is null)
            {
                return null;
            }

            if (HoursMin is not null && HoursMax is not null && HoursMin != HoursMax)
            {
                return $"{FormatHours(HoursMin.Value)}–{FormatHours(HoursMax.Value)}";
            }

            return FormatHours(HoursMin ?? HoursMax ?? 0);
        }
    }

    public string HoursSecondary => "uur";

    public static SwipeViewModel FromVacancy(VacancyListItem item, bool showMatchPercentage = true)
    {
        ArgumentNullException.ThrowIfNull(item);

        var tags = new List<string>();
        if (item.CulturePillars.Count > 0)
        {
            tags.AddRange(item.CulturePillars.Where(t => !string.IsNullOrWhiteSpace(t)));
        }
        else if (item.WorkTypes.Length > 0)
        {
            tags.AddRange(item.WorkTypes.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        if (!string.IsNullOrWhiteSpace(item.CategoryName)
            && !tags.Contains(item.CategoryName, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(item.CategoryName);
        }

        decimal? salaryMin = null;
        decimal? salaryMax = null;
        var period = "per maand";
        if (item.HourlyWage is decimal hourly && hourly > 0 && item.WageVisible)
        {
            var monthly = Math.Round(hourly * 160m, 0);
            salaryMin = monthly;
            salaryMax = monthly;
            period = "per maand";
        }

        var gateClosed = string.Equals(item.FitGate, CandidateFitApply.FitGateClosed, StringComparison.OrdinalIgnoreCase);
        var hidden = item.IntermediaryCompanyId is not null && !item.ShowClientAddressOnMap;

        return new SwipeViewModel
        {
            VacancyId = item.Id,
            CompanyName = item.CompanyName ?? string.Empty,
            CompanyLogoUrl = item.CompanyLogoUrl,
            ImageUrl = item.ImageUrl,
            JobTitle = string.IsNullOrWhiteSpace(item.Title) ? "Vacature" : item.Title,
            ShortDescription = Truncate(item.Description, 140),
            WhyYouFit = item.FitWhyLine ?? BuildWhyYouFit(item),
            Location = FirstNonEmpty(item.CompanyAddress, item.OfferedByLabel),
            DistanceKm = item.DistanceKm,
            TravelTimeMinutes = item.TravelMinutes,
            TransportMode = item.RequiredTransport.FirstOrDefault() ?? "Fiets",
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            SalaryPeriod = period,
            HourlyWage = item.WageVisible ? item.HourlyWage : null,
            WageVisible = item.WageVisible,
            HoursMin = item.MinHoursPerWeek,
            HoursMax = item.MaxHoursPerWeek,
            Tags = tags,
            MatchPercentage = item.FitPercent ?? item.MatchPercent,
            ShowMatchPercentage = showMatchPercentage
                && !gateClosed
                && (item.FitPercent is not null || item.MatchPercent is not null),
            FitGate = item.FitGate,
            FitBand = item.FitBand,
            FitWhyKinds = item.FitWhyKinds?.ToList() ?? [],
            FitDimensions = item.FitDimensions,
            RankLowerReason = item.RankLowerReason,
            IsHiddenMode = hidden,
            TravelToBureau = hidden
        };
    }

    private static string FormatDistance(double km) =>
        km < 10
            ? $"{km.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"))} km"
            : $"{Math.Round(km):0} km";

    private static string FormatMoney(decimal value) =>
        value % 1 == 0
            ? value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));

    private static string FormatHours(decimal value) =>
        value % 1 == 0
            ? value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));

    private static string? Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var flat = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        flat = System.Text.RegularExpressions.Regex.Replace(flat, @"\s+", " ").Trim();
        return flat.Length <= max ? flat : flat[..(max - 1)].TrimEnd() + "…";
    }

    private static string? BuildWhyYouFit(VacancyListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.MatchRationale))
        {
            return Truncate(item.MatchRationale, 150) ?? item.MatchRationale!;
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }
}

public enum SwipeDirection
{
    Left,
    Right
}
