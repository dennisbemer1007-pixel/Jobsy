namespace Jobsy.Core.Rules;

/// <summary>Canonical CSV column names and aliases for vacancy batch import.</summary>
public static class VacancyCsvSchema
{
    public const string Title = "titel";
    public const string Description = "omschrijving";
    public const string StartDate = "startdatum";
    public const string EndDate = "einddatum";
    public const string Branches = "branches";
    public const string SalaryTableId = "salaristabel_id";
    public const string CompanyId = "vestiging_id";
    public const string HourlyWage = "uurloon";
    public const string Image = "afbeelding";
    public const string Video = "video";
    public const string Transport = "vervoer";
    public const string DrivingLicense = "rijbewijs";
    public const string Education = "opleiding";
    public const string MinimumEmployers = "minimum_werkgevers";

    public static readonly string[] RequiredHeaders =
    [
        Title,
        Description,
        StartDate,
        EndDate,
        Branches,
        SalaryTableId
    ];

    public static readonly IReadOnlyDictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["titel"] = Title,
            ["title"] = Title,
            ["omschrijving"] = Description,
            ["description"] = Description,
            ["beschrijving"] = Description,
            ["startdatum"] = StartDate,
            ["start_date"] = StartDate,
            ["startdate"] = StartDate,
            ["einddatum"] = EndDate,
            ["end_date"] = EndDate,
            ["enddate"] = EndDate,
            ["branches"] = Branches,
            ["branche"] = Branches,
            ["work_types"] = Branches,
            ["worktypes"] = Branches,
            ["salaristabel_id"] = SalaryTableId,
            ["salary_table_id"] = SalaryTableId,
            ["salarytableid"] = SalaryTableId,
            ["vestiging_id"] = CompanyId,
            ["company_id"] = CompanyId,
            ["companyid"] = CompanyId,
            ["uurloon"] = HourlyWage,
            ["hourly_wage"] = HourlyWage,
            ["hourlywage"] = HourlyWage,
            ["afbeelding"] = Image,
            ["image"] = Image,
            ["image_url"] = Image,
            ["imageurl"] = Image,
            ["video"] = Video,
            ["video_url"] = Video,
            ["videourl"] = Video,
            ["vervoer"] = Transport,
            ["transport"] = Transport,
            ["required_transport"] = Transport,
            ["rijbewijs"] = DrivingLicense,
            ["driving_license"] = DrivingLicense,
            ["opleiding"] = Education,
            ["education"] = Education,
            ["minimum_werkgevers"] = MinimumEmployers,
            ["minimum_employers"] = MinimumEmployers,
            ["minimumemployers"] = MinimumEmployers
        };

    public static string? CanonicalHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var key = header.Trim().Trim('\uFEFF');
        return HeaderAliases.TryGetValue(key, out var canonical) ? canonical : null;
    }
}
