using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

/// <summary>
/// Aligns passport DNA progress with candidate UI: quick scan completed or deep analysis finished.
/// </summary>
public static class PassportDnaCompletion
{
    public static bool QuickScanCompleted(string? status)
        => CandidateCompetencyStatuses.IsCompleted(status);

    public static bool DeepCompleted(CandidateDeepAnalysis? row, AssessmentKind kind)
    {
        if (row is null || !CandidateDeepAnalysisStatuses.IsCompleted(row.Status))
        {
            return false;
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson, kind);
        var expected = DeepAnalysisCatalog.QuestionCountFor(kind);
        return answers.Count >= expected;
    }

    public static bool LayerCompleted(
        string? quickStatus,
        CandidateDeepAnalysis? deepRow,
        AssessmentKind kind)
        => QuickScanCompleted(quickStatus) || DeepCompleted(deepRow, kind);
}
