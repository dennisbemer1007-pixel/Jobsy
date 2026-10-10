using Jobsy.Core.Enums;

namespace Jobsy.Core.Ai;

/// <summary>
/// Which AI calls use the cheaper model. Unlisted features stay on the quality model.
/// </summary>
public static class AiModelRouting
{
    /// <summary>
    /// Mechanical calls: classify a vacancy, read a CV, or translate stored text.
    /// Stories, the coach, plans, fit checks and the practice interview stay on the quality model.
    /// </summary>
    public static bool UsesSmallModel(OpenAiFeature feature) => feature is
        OpenAiFeature.VacancyContentModeration
        or OpenAiFeature.CvExtraction
        or OpenAiFeature.Translation
        or OpenAiFeature.ExternalVacancyExtraction;

    /// <summary>First non-blank value, trimmed. Null when every value is missing.</summary>
    public static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
