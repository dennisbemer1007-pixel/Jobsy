using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Security;

public sealed class TotpVerifier : ITotpVerifier
{
    private readonly JobsyDbContext _db;

    public TotpVerifier(JobsyDbContext db) => _db = db;

    public async Task<TotpResult> VerifyAsync(
        User user,
        string? unprotectedSecret,
        string? code,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(unprotectedSecret)
            || !TotpAuthenticator.TryVerifyCode(unprotectedSecret, code, utcNow, out var matchedStep))
        {
            return new TotpResult(false, 0);
        }

        if (user.LastTotpTimeStep is long last && matchedStep <= last)
        {
            return new TotpResult(false, matchedStep);
        }

        // Relational providers: atomic optimistic update. InMemory (tests): mutate tracked entity.
        if (_db.Database.IsRelational())
        {
            var updated = await _db.Users
                .Where(u => u.Id == user.Id
                            && (u.LastTotpTimeStep == null || u.LastTotpTimeStep < matchedStep))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(u => u.LastTotpTimeStep, matchedStep),
                    cancellationToken);

            if (updated == 0)
            {
                return new TotpResult(false, matchedStep);
            }
        }
        else
        {
            if (user.LastTotpTimeStep is long current && matchedStep <= current)
            {
                return new TotpResult(false, matchedStep);
            }

            user.LastTotpTimeStep = matchedStep;
        }

        user.LastTotpTimeStep = matchedStep;
        return new TotpResult(true, matchedStep);
    }
}
