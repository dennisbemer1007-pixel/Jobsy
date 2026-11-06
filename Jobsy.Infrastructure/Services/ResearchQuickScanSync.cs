using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Infrastructure.Services;

internal static class ResearchQuickScanSync
{
    public static async Task AfterCompleteAsync(
        IAssessmentResearchService research,
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        int? totalDurationMs,
        CancellationToken cancellationToken)
    {
        if (answers.Count == 0)
        {
            return;
        }

        await research.RecordAccountAttemptAsync(
            userId,
            kind,
            AssessmentTestVersions.V1,
            testLanguage: "nl",
            answers,
            itemDurationMs,
            totalDurationMs,
            cancellationToken);
    }
}
