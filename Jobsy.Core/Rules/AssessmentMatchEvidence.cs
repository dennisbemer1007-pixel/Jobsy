namespace Jobsy.Core.Rules;

/// <summary>
/// How strongly test scores support the match percentage shown to candidates.
/// </summary>
public enum AssessmentMatchEvidence
{
    /// <summary>No test scores in the match blend.</summary>
    None = 0,

    /// <summary>Mini (5) or partial depth — indicative only.</summary>
    Indicative = 1,

    /// <summary>Full Quick-Scan completed for the stored question-set version.</summary>
    FullQuickScan = 2,

    /// <summary>Deep analysis completed for the relevant kind.</summary>
    DeepAnalysis = 3
}

public static class AssessmentMatchEvidenceRules
{
    public const int IndicativeUncertaintyPoints = 10;
    public const int PartialDepthUncertaintyPoints = 6;

    public static AssessmentMatchEvidence FromQuickScan(
        bool scoresComplete,
        int answeredCount,
        int fullCount,
        bool isProvisional)
    {
        if (isProvisional || answeredCount < TestDepthRules.FirstCount)
        {
            return AssessmentMatchEvidence.None;
        }

        if (!scoresComplete || answeredCount < fullCount)
        {
            return AssessmentMatchEvidence.Indicative;
        }

        return AssessmentMatchEvidence.FullQuickScan;
    }

    public static int UncertaintyPoints(AssessmentMatchEvidence evidence)
        => evidence switch
        {
            AssessmentMatchEvidence.Indicative => IndicativeUncertaintyPoints,
            AssessmentMatchEvidence.FullQuickScan => 0,
            AssessmentMatchEvidence.DeepAnalysis => 0,
            _ => PartialDepthUncertaintyPoints
        };

    public static double CompetencyWeightFactor(AssessmentMatchEvidence evidence)
        => evidence switch
        {
            AssessmentMatchEvidence.Indicative => 0.72,
            AssessmentMatchEvidence.FullQuickScan => 1.0,
            AssessmentMatchEvidence.DeepAnalysis => 1.0,
            _ => 0.85
        };
}
