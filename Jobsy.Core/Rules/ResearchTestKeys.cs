using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class ResearchTestKeys
{
    public static string QuickScanExportKey(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => "career_quickscan",
        AssessmentKind.Culture => "culture_quickscan",
        AssessmentKind.Values => "values_quickscan",
        _ => "competence_quickscan"
    };

    public static AssessmentKind? TryParseExportKey(string? testKey)
    {
        if (string.IsNullOrWhiteSpace(testKey))
        {
            return null;
        }

        return testKey.ToLowerInvariant() switch
        {
            "competence_quickscan" => AssessmentKind.Competence,
            "career_quickscan" => AssessmentKind.Career,
            "culture_quickscan" => AssessmentKind.Culture,
            "values_quickscan" => AssessmentKind.Values,
            _ => AssessmentKindLabels.TryParse(testKey, out var kind) ? kind : null
        };
    }
}
