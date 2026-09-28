namespace Jobsy.Core.Enums;

/// <summary>
/// OpenAI-backed product features that share credential resolution (DB → config → defaults)
/// but may differ in default model or base-URL fallback style.
/// </summary>
public enum OpenAiFeature
{
    WhoAmI,
    AssistantChat,
    CareerPathPlan,
    CultureFit,
    VacancyContentModeration,
    CvExtraction,
    MockInterview,
    Translation,
    CareerCompass,
    RoleFitCheck,
    CompetenceDeepReport,
}
