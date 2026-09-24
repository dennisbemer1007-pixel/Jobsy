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

    public decimal? HoursMin { get; set; }
    public decimal? HoursMax { get; set; }

    public List<string> Tags { get; set; } = [];

    public int? MatchPercentage { get; set; }
    public bool ShowMatchPercentage { get; set; }

    public string LocationPrimary =>
        string.IsNullOrWhiteSpace(Location) ? "Locatie onbekend" : Location.Trim();

    public string? LocationSecondary =>
        DistanceKm is null
            ? (TravelTimeMinutes is null
                ? null
                : $"{TravelTimeMinutes} min{(string.IsNullOrWhiteSpace(TransportMode) ? "" : $" · {TransportMode}")}")
            : $"{FormatDistance(DistanceKm.Value)}{(TravelTimeMinutes is null ? "" : $" · {TravelTimeMinutes} min")}";

    public string SalaryPrimary
    {
        get
        {
            if (SalaryMin is null && SalaryMax is null)
            {
                return "In overleg";
            }

            if (SalaryMin is not null && SalaryMax is not null && SalaryMin != SalaryMax)
            {
                return $"{FormatMoney(SalaryMin.Value)}-{FormatMoney(SalaryMax.Value)}";
            }

            return FormatMoney(SalaryMin ?? SalaryMax ?? 0);
        }
    }

    public string SalarySecondary =>
        string.IsNullOrWhiteSpace(SalaryPeriod) ? "per maand" : SalaryPeriod.Trim();

    public string HoursPrimary
    {
        get
        {
            if (HoursMin is null && HoursMax is null)
            {
                return "Flexibel";
            }

            if (HoursMin is not null && HoursMax is not null && HoursMin != HoursMax)
            {
                return $"{FormatHours(HoursMin.Value)} - {FormatHours(HoursMax.Value)}";
            }

            return FormatHours(HoursMin ?? HoursMax ?? 0);
        }
    }

    public string HoursSecondary => "per week";

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

        // Prefer monthly indication when only hourly wage is known (approx. 160h/month).
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

        return new SwipeViewModel
        {
            VacancyId = item.Id,
            CompanyName = item.CompanyName ?? string.Empty,
            CompanyLogoUrl = item.CompanyLogoUrl,
            ImageUrl = item.ImageUrl,
            JobTitle = string.IsNullOrWhiteSpace(item.Title) ? "Vacature" : item.Title,
            ShortDescription = Truncate(item.Description, 140),
            WhyYouFit = BuildWhyYouFit(item),
            Location = FirstNonEmpty(item.CompanyAddress, item.OfferedByLabel),
            DistanceKm = item.DistanceKm,
            TravelTimeMinutes = item.TravelMinutes,
            TransportMode = item.RequiredTransport.FirstOrDefault(),
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            SalaryPeriod = period,
            HoursMin = item.MinHoursPerWeek,
            HoursMax = item.MaxHoursPerWeek,
            Tags = tags,
            MatchPercentage = item.MatchPercent,
            ShowMatchPercentage = showMatchPercentage && item.MatchPercent is not null
        };
    }

    /// <summary>Demo card matching the VDBH Grevelingen Groen design reference.</summary>
    public static SwipeViewModel CreateDemoSample() => new()
    {
        VacancyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        CompanyName = "VDBH Grevelingen Groen",
        CompanyLogoUrl = null,
        ImageUrl = "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?w=800&q=80",
        JobTitle = "Hovenier | werken in het groen | afwisselende projecten in de regio",
        ShortDescription = "Afwisselend groenwerk in de regio: onderhoud, aanleg en seizoensklussen.",
        WhyYouFit = "Sterke match (86%): jouw praktische inzet en groene werkrichting sluiten goed aan.",
        Location = "Brouwershaven",
        DistanceKm = 36,
        TravelTimeMinutes = 32,
        TransportMode = "Auto",
        SalaryMin = 2900,
        SalaryMax = 3500,
        SalaryPeriod = "per maand",
        HoursMin = 32,
        HoursMax = 40,
        Tags =
        [
            "Gewasverzorging",
            "Maaien",
            "Onkruidbestrijding",
            "Schoffelen",
            "Snoeien"
        ],
        MatchPercentage = 86,
        ShowMatchPercentage = true
    };

    public static IReadOnlyList<SwipeViewModel> CreateDemoDeck() =>
    [
        CreateDemoSample(),
        new SwipeViewModel
        {
            VacancyId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
            CompanyName = "Westland Groen Service",
            ImageUrl = "https://images.unsplash.com/photo-1466692476866-aef1dfb1e735?w=800&q=80",
            JobTitle = "Medewerker groenvoorziening | fulltime | dichtbij huis",
            ShortDescription = "Groenonderhoud dichtbij huis: planten, seizoenswerk en nette buitenruimtes.",
            WhyYouFit = "Goede kans (74%): jouw voorkeur voor praktisch buitenwerk past hier goed.",
            Location = "Naaldwijk",
            DistanceKm = 8,
            TravelTimeMinutes = 12,
            TransportMode = "Fiets",
            SalaryMin = 2600,
            SalaryMax = 3100,
            SalaryPeriod = "per maand",
            HoursMin = 36,
            HoursMax = 40,
            Tags = ["Planten", "Onderhoud", "Seizoenswerk"],
            MatchPercentage = 74,
            ShowMatchPercentage = true
        },
        new SwipeViewModel
        {
            VacancyId = Guid.Parse("cccccccc-dddd-eeee-ffff-aaaaaaaaaaaa"),
            CompanyName = "De Tuinkamer",
            ImageUrl = "https://images.unsplash.com/photo-1585320806297-9794b3e4eeae?w=800&q=80",
            JobTitle = "Allround hovenier | projecten & particulieren",
            ShortDescription = "Aanleg en onderhoud bij particulieren: snoeien, terrassen en afwisselende klussen.",
            WhyYouFit = "Match 68% — check of de mix van projecten en particulieren jou uitnodigt.",
            Location = "Goes",
            DistanceKm = 22,
            TravelTimeMinutes = 28,
            TransportMode = "Auto",
            SalaryMin = 2800,
            SalaryMax = 3400,
            SalaryPeriod = "per maand",
            HoursMin = 24,
            HoursMax = 32,
            Tags = ["Aanleg", "Snoeien", "Terrassen"],
            MatchPercentage = 68,
            ShowMatchPercentage = true
        }
    ];

    private static string FormatDistance(double km) =>
        km < 10
            ? $"{km.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("nl-NL"))} km"
            : $"{Math.Round(km):0} km";

    private static string FormatMoney(decimal value) =>
        value % 1 == 0
            ? value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
            : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

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

    private static string BuildWhyYouFit(VacancyListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.MatchRationale))
        {
            return Truncate(item.MatchRationale, 150) ?? item.MatchRationale!;
        }

        if (item.MatchPercent is int pct and >= 85)
        {
            return $"Sterke match ({pct}%): jouw profiel en deze rol liggen dicht bij elkaar.";
        }

        if (item.MatchPercent is int mid and >= 60)
        {
            return $"Goede kans ({mid}%): jouw skills en voorkeuren sluiten aan op wat hier gevraagd wordt.";
        }

        if (item.MatchPercent is int low)
        {
            return $"Match {low}% — bekijk of de sfeer en taken jou aanspreken.";
        }

        return "Deze vacature past bij wat jij zoekt — check of de sfeer en taken jou uitnodigen.";
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
