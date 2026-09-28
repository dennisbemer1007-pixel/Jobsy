using Jobsy.Core.Reports.Career;

namespace Jobsy.Core.Reports.Culture;

public sealed class CultureDeepReport
{
    public int ReportVersion { get; set; } = CultureDeepReportJson.CurrentReportVersion;
    public DateTime GeneratedAtUtc { get; set; }
    public bool FromOpenAi { get; set; }

    public LocalizedReportText Summary { get; set; } = new();

    /// <summary>6 culture + 5 personality domain scores.</summary>
    public List<DeepDomainScore> Domains { get; set; } = [];

    public List<DeepEmployerFit> Employers { get; set; } = [];
    public List<DeepActionStep> ActionPlan { get; set; } = [];
    public List<string> StrengthKeys { get; set; } = [];
    public List<string> PitfallKeys { get; set; } = [];
    public bool ComparisonAvailable { get; set; }
}

public sealed class DeepEmployerFit
{
    public string OrgTypeKey { get; set; } = "";
    public int MatchPercent { get; set; }
}
