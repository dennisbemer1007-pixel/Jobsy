namespace Jobsy.Core.Reports.Career;

/// <summary>Stored paid career (RIASEC) deep report — keys + scores; labels resolved at render.</summary>
public sealed class CareerDeepReport
{
    public int ReportVersion { get; set; } = CareerDeepReportJson.CurrentReportVersion;
    public DateTime GeneratedAtUtc { get; set; }
    public bool FromOpenAi { get; set; }

    public LocalizedReportText Summary { get; set; } = new();

    /// <summary>RIASEC domain scores (R/I/A/S/E/C).</summary>
    public List<DeepDomainScore> Domains { get; set; } = [];

    /// <summary>3-letter Holland code, e.g. SEC.</summary>
    public string HollandCode { get; set; } = "";

    public List<DeepOccupationFit> Occupations { get; set; } = [];
    public List<DeepActionStep> ActionPlan { get; set; } = [];
    public List<string> StrengthKeys { get; set; } = [];
    public List<string> PitfallKeys { get; set; } = [];

    /// <summary>When false, hide comparison card / norm polygon (N &lt; 100).</summary>
    public bool ComparisonAvailable { get; set; }
}

public sealed class DeepDomainScore
{
    public string Domain { get; set; } = "";
    public int Score { get; set; }
    public string LevelKey { get; set; } = "level.mid";
    public double? NormMean { get; set; }
}

public sealed class DeepOccupationFit
{
    public string TitleKey { get; set; } = "";
    public string TitleNl { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public int MatchPercent { get; set; }
    public string ReasonNl { get; set; } = "";
    public string ReasonEn { get; set; } = "";
    public string Band { get; set; } = "";

    public string Title(string? lang)
        => ReportLanguage.IsEnglish(lang)
            ? (string.IsNullOrWhiteSpace(TitleEn) ? TitleNl : TitleEn)
            : TitleNl;

    public string Reason(string? lang)
        => ReportLanguage.IsEnglish(lang)
            ? (string.IsNullOrWhiteSpace(ReasonEn) ? ReasonNl : ReasonEn)
            : ReasonNl;
}

public sealed class DeepActionStep
{
    public LocalizedReportText Title { get; set; } = new();
    public LocalizedReportText Body { get; set; } = new();
}
