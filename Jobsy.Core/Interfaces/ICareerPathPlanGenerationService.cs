using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICareerPathPlanGenerationService
{
    /// <summary>
    /// Generates plan content for <paramref name="dreamTitle"/>. Returns the plan plus whether the
    /// OpenAI path produced it (<c>FromAi</c>) so the caller can persist that flag (D13/B6).
    /// </summary>
    Task<CareerPathGenerationResult> GenerateAsync(
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile = null,
        string planLanguage = "nl",
        CancellationToken cancellationToken = default);
}

public sealed record CareerPathGenerationResult(HorizonCareerPathPlan Plan, bool FromAi);
