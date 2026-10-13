using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ITotpVerifier
{
    Task<TotpResult> VerifyAsync(
        User user,
        string? unprotectedSecret,
        string? code,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

public sealed record TotpResult(bool Ok, long Step);
