namespace Jobsy.Core.Interfaces;

public interface IKvkVerificationRetryService
{
    /// <summary>
    /// Retries KVK verification for companies marked Pending. Returns how many were verified.
    /// </summary>
    Task<int> RetryPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries KVK verification immediately for one company (Pending or Failed with establishment id).
    /// Returns false when the company is missing or not eligible.
    /// </summary>
    Task<bool> RetryNowAsync(Guid companyId, CancellationToken cancellationToken = default);
}
