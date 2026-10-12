namespace Jobsy.Core.Interfaces;

/// <summary>Deduplicated in-process queue of vacancy ids for instant proactive matching after publish.</summary>
public interface IProactiveVacancyInstantMatchQueue
{
    bool TryEnqueue(Guid vacancyId);

    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);

    void MarkCompleted(Guid vacancyId);
}
