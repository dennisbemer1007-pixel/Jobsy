using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record OneTimeLinkCreateResult(Guid Id, string Token);

public sealed record OneTimeLinkPeekResult(
    bool Valid,
    Guid? LinkId,
    Guid? UserId,
    Guid? CompanyId,
    string? MaskedEmail,
    string? CompanyName,
    DateTime? ExpiresAtUtc);

public interface IOneTimeLinkService
{
    /// <summary>
    /// Creates a new link and invalidates older unused links for the same purpose + user
    /// (or purpose + company for <see cref="OneTimeLinkPurpose.ApiKeyReveal"/>).
    /// Returns the plaintext token exactly once.
    /// </summary>
    Task<OneTimeLinkCreateResult> CreateAsync(
        OneTimeLinkPurpose purpose,
        Guid? userId,
        Guid? companyId,
        string email,
        TimeSpan lifetime,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Safe preview; never mutates.</summary>
    Task<OneTimeLinkPeekResult> PeekAsync(
        OneTimeLinkPurpose purpose,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes a valid unused token. Returns null when invalid/expired/already used.
    /// </summary>
    Task<OneTimeLink?> ConsumeAsync(
        OneTimeLinkPurpose purpose,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes used or expired links older than retention.</summary>
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
