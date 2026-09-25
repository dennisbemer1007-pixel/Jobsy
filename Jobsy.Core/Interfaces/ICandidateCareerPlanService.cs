using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCareerPlanService
{
    Task<CareerPlanView?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CareerPlanView> GenerateAndSaveAsync(
        Guid userId,
        string dreamTitle,
        HorizonCareerProfileSnapshot? profile,
        CancellationToken cancellationToken = default);

    Task<CareerPlanView> CompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    Task<CareerPlanView> UncompleteStepAsync(
        Guid userId,
        string stepKey,
        CancellationToken cancellationToken = default);

    Task<CareerPlanView> MarkCourseOwnedAsync(
        Guid userId,
        string courseName,
        CancellationToken cancellationToken = default);
}
