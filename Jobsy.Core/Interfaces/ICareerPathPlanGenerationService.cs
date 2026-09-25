using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICareerPathPlanGenerationService
{
    Task<HorizonCareerPathPlan> GenerateAsync(
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile = null,
        CancellationToken cancellationToken = default);
}
