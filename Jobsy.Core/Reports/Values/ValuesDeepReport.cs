using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;

namespace Jobsy.Core.Reports.Values;

public sealed class ValuesDeepReport
{
    public int ReportVersion { get; set; } = ValuesDeepReportJson.CurrentReportVersion;
    public DateTime GeneratedAtUtc { get; set; }
    public bool FromOpenAi { get; set; }

    public LocalizedReportText Summary { get; set; } = new();

    /// <summary>5 value domains, already ranked high → low.</summary>
    public List<DeepDomainScore> Domains { get; set; } = [];

    public List<DeepEmployerFit> Employers { get; set; } = [];
    public List<DeepActionStep> ActionPlan { get; set; } = [];
    public List<string> StrengthKeys { get; set; } = [];
    public List<string> PitfallKeys { get; set; } = [];
    public bool ComparisonAvailable { get; set; }
}
