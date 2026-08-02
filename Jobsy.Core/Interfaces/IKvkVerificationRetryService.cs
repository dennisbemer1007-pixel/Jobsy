namespace Jobsy.Core.Interfaces;

public interface IKvkVerificationRetryService
{
    /// <summary>
    /// Retries KVK verification for companies marked Pending. Returns how many were verified.
    /// </summary>
    Task<int> RetryPendingAsync(CancellationToken cancellationToken = default);
}
