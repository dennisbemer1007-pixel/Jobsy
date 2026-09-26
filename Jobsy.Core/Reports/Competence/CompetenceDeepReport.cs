namespace Jobsy.Core.Reports.Competence;

/// <summary>
/// Serializable output of the paid competence deep-analysis report (150-item Big Five test).
/// Stored as JSON (camelCase) so it can be persisted, re-rendered, and diffed across versions.
/// </summary>
public sealed class CompetenceDeepReport
{
    /// <summary>Bump when the report shape or scoring changes so stored reports can be migrated/rebuilt.</summary>
    public int ReportVersion { get; set; } = CompetenceDeepReportJson.CurrentReportVersion;

    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>True when the summary/action plan came from OpenAI; false when template text was used.</summary>
    public bool FromOpenAi { get; set; }

    /// <summary>Three-sentence overview of the whole profile.</summary>
    public string Summary { get; set; } = "";

    public List<CompetenceDeepTraitReport> Traits { get; set; } = [];

    public List<CompetenceDeepOccupation> Occupations { get; set; } = [];

    public List<CompetenceDeepActionStep> ActionPlan { get; set; } = [];

    /// <summary>Dutch one-liner crediting the Johnson (2014) norm source, or null when norms are unavailable.</summary>
    public string? NormSourceLine { get; set; }
}

/// <summary>One Big Five trait section of the report, including its six facets.</summary>
public sealed class CompetenceDeepTraitReport
{
    /// <summary>Internal domain key, e.g. "Consciëntieusheid".</summary>
    public string Domain { get; set; } = "";

    /// <summary>Dutch display label for the trait.</summary>
    public string LabelNl { get; set; } = "";

    /// <summary>0-100 score.</summary>
    public int Score { get; set; }

    /// <summary>Laag / Gemiddeld / Hoog.</summary>
    public string Level { get; set; } = "";

    /// <summary>Norm-group mean (0-100), or null when norms are unavailable.</summary>
    public double? NormMean { get; set; }

    /// <summary>Dutch band label ("Lager/Vergelijkbaar/Hoger dan de meeste mensen"), or null when unavailable.</summary>
    public string? NormBand { get; set; }

    public string Meaning { get; set; } = "";

    public string WorkQuote { get; set; } = "";

    public string Pitfall { get; set; } = "";

    public string Tip { get; set; } = "";

    public string Strength { get; set; } = "";

    public string ThriveAtWork { get; set; } = "";

    public string FittingManager { get; set; } = "";

    public string InTeam { get; set; } = "";

    public List<CompetenceDeepFacetReport> Facets { get; set; } = [];
}

/// <summary>One IPIP-NEO facet score within a trait section.</summary>
public sealed class CompetenceDeepFacetReport
{
    /// <summary>Facet code, e.g. "C1".</summary>
    public string Code { get; set; } = "";

    public string LabelNl { get; set; } = "";

    /// <summary>0-100 score.</summary>
    public int Score { get; set; }

    /// <summary>Norm-group mean (0-100), or null when norms are unavailable.</summary>
    public double? NormMean { get; set; }

    /// <summary>Dutch band label, or null when unavailable.</summary>
    public string? NormBand { get; set; }
}

/// <summary>A suggested occupation/role that fits the candidate's top traits.</summary>
public sealed class CompetenceDeepOccupation
{
    public string Title { get; set; } = "";

    public int MatchPercent { get; set; }

    public string Reason { get; set; } = "";
}

/// <summary>One step of the personal action plan.</summary>
public sealed class CompetenceDeepActionStep
{
    public string Title { get; set; } = "";

    public string Body { get; set; } = "";
}

/// <summary>
/// Single place for the Laag/Gemiddeld/Hoog thresholds used across scoring, text lookup, and
/// occupation matching for the competence deep-analysis report.
/// </summary>
public static class CompetenceDeepReportLevels
{
    public const string Laag = "Laag";
    public const string Gemiddeld = "Gemiddeld";
    public const string Hoog = "Hoog";

    /// <summary>Scores below this are "Laag".</summary>
    public const int LaagMax = 40;

    /// <summary>Scores below this (and at/above <see cref="LaagMax"/>) are "Gemiddeld"; the rest are "Hoog".</summary>
    public const int GemiddeldMax = 70;

    public static string LevelFor(int score0To100) => score0To100 switch
    {
        < LaagMax => Laag,
        < GemiddeldMax => Gemiddeld,
        _ => Hoog
    };
}
