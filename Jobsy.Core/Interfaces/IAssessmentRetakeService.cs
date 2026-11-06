using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IAssessmentRetakeService
{
    Task<CandidateAssessmentAttempt> StartAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidateAssessmentAttempt>> ListHistoryAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default);
}
