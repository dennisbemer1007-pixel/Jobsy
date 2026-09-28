namespace Jobsy.Web.Models;

public sealed class SuppressedCountModel
{
    public string Status { get; set; } = "insufficient";
    public int? Value { get; set; }
}

public sealed class SuppressedPercentModel
{
    public string Status { get; set; } = "insufficient";
    public int? Percent { get; set; }
}

public sealed class InsightsBranchRefModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public sealed class InsightsScopeModel
{
    public List<InsightsBranchRefModel> Branches { get; set; } = [];
    public int RadiusKm { get; set; }
    public int PeriodDays { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public bool IsFullAccess { get; set; }
}

public sealed class InsightsKpisModel
{
    public SuppressedCountModel CandidatesInRadius { get; set; } = new();
    public SuppressedCountModel AvgHoursPerWeek { get; set; } = new();
    public SuppressedCountModel Candidates32PlusHours { get; set; } = new();
    public SuppressedCountModel Active30d { get; set; } = new();
    public SuppressedCountModel MatchingYourVacancies { get; set; } = new();
}

public sealed class RankedItemModel
{
    public string Label { get; set; } = "";
    public SuppressedCountModel Count { get; set; } = new();
}

public sealed class InsightsDistributionBucketModel
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public SuppressedPercentModel Share { get; set; } = new();
}

public sealed class InsightsDistributionModel
{
    public string Status { get; set; } = "insufficient";
    public List<InsightsDistributionBucketModel> Buckets { get; set; } = [];
}

public sealed class DensityCellModel
{
    public string CellId { get; set; } = "";
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public int Band { get; set; }
}

public sealed class InsightsVacancyChecklistModel
{
    public bool? SalaryMentioned { get; set; }
    public bool? FlexibleHours { get; set; }
    public bool? AtmosphereAndTeam { get; set; }
}

public sealed class InsightsTipsModel
{
    public List<string> TipKeys { get; set; } = [];
    public InsightsVacancyChecklistModel? Checklist { get; set; }
}

public sealed class VacancyReachModel
{
    public Guid VacancyId { get; set; }
    public string Title { get; set; } = "";
    public string BranchName { get; set; } = "";
    public SuppressedCountModel MatchingCandidates { get; set; } = new();
    public InsightsTipsModel? Tips { get; set; }
}

public sealed class InsightsTrendModel
{
    public string Status { get; set; } = "insufficient_history";
    public string? MessageKey { get; set; }
}

public sealed class CandidateInsightsDto
{
    public InsightsScopeModel Scope { get; set; } = new();
    public InsightsKpisModel Kpis { get; set; } = new();
    public List<RankedItemModel> DreamJobsTop { get; set; } = [];
    public InsightsDistributionModel? WorkFields { get; set; }
    public InsightsDistributionModel? DnaRiasec { get; set; }
    public InsightsDistributionModel? Competences { get; set; }
    public InsightsDistributionModel? Personality { get; set; }
    public InsightsDistributionModel? Priorities { get; set; }
    public InsightsDistributionModel? WorkKinds { get; set; }
    public List<DensityCellModel> Density { get; set; } = [];
    public List<VacancyReachModel> Vacancies { get; set; } = [];
    public InsightsTrendModel Trend { get; set; } = new();
    public List<string> LockedSections { get; set; } = [];
}

public sealed class CandidateInsightsBranchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsLocked { get; set; }
}
