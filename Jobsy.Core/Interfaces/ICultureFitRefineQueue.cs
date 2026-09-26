namespace Jobsy.Core.Interfaces;

/// <summary>Deduplicated in-process queue of (userId, vacancyId) culture-fit AI refine jobs.</summary>
public interface ICultureFitRefineQueue
{
    bool TryEnqueue(Guid userId, Guid vacancyId);

    ValueTask<(Guid UserId, Guid VacancyId)> DequeueAsync(CancellationToken cancellationToken);

    void MarkCompleted(Guid userId, Guid vacancyId);
}
