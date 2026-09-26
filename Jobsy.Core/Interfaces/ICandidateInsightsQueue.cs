namespace Jobsy.Core.Interfaces;

/// <summary>Deduplicated in-process queue of candidate userIds needing insight recompute.</summary>
public interface ICandidateInsightsQueue
{
    /// <summary>Enqueue a user; no-op if already queued.</summary>
    bool TryEnqueue(Guid userId);

    /// <summary>Wait for the next userId (blocks until available or cancelled).</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>Mark a user as no longer in-flight so future enqueues are accepted.</summary>
    void MarkCompleted(Guid userId);
}
