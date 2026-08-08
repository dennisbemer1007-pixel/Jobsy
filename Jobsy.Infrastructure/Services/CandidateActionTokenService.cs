using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CandidateActionTokenService : ICandidateActionTokenService
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(30);

    private readonly JobsyDbContext _db;

    public CandidateActionTokenService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<CandidateActionTokenIssueResult> IssueAsync(
        Guid userId,
        string purpose,
        Guid? relatedApplicationId = null,
        TimeSpan? lifetime = null,
        CancellationToken cancellationToken = default)
    {
        if (!CandidateActionPurposes.IsKnown(purpose))
        {
            throw new ArgumentException("Onbekend actiedoel.", nameof(purpose));
        }

        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var row = new CandidateActionToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            TokenHash = VerificationCodes.Hash(plaintext),
            RelatedApplicationId = relatedApplicationId,
            ExpiresAtUtc = DateTime.UtcNow.Add(lifetime ?? DefaultLifetime),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.CandidateActionTokens.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var path = purpose switch
        {
            CandidateActionPurposes.SetUnavailable => $"/candidate/actions/set-unavailable?token={plaintext}",
            CandidateActionPurposes.WithdrawOtherApplications =>
                $"/candidate/actions/withdraw-others?token={plaintext}",
            _ => $"/candidate/actions?token={plaintext}"
        };

        return new CandidateActionTokenIssueResult(row, plaintext, path);
    }

    public async Task<CandidateActionToken?> FindValidAsync(
        string plaintextToken,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken) || !CandidateActionPurposes.IsKnown(purpose))
        {
            return null;
        }

        var hash = VerificationCodes.Hash(plaintextToken.Trim());
        var now = DateTime.UtcNow;
        return await _db.CandidateActionTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash
                     && t.Purpose == purpose
                     && t.UsedAtUtc == null
                     && t.ExpiresAtUtc >= now,
                cancellationToken);
    }

    public async Task<CandidateActionToken?> TryConsumeAsync(
        string plaintextToken,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken) || !CandidateActionPurposes.IsKnown(purpose))
        {
            return null;
        }

        var hash = VerificationCodes.Hash(plaintextToken.Trim());
        var now = DateTime.UtcNow;

        // PostgreSQL (and other relational providers): atomic claim via ExecuteUpdate.
        if (_db.Database.IsRelational())
        {
            var claimed = await _db.CandidateActionTokens
                .Where(t => t.TokenHash == hash
                            && t.Purpose == purpose
                            && t.UsedAtUtc == null
                            && t.ExpiresAtUtc >= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(t => t.UsedAtUtc, now),
                    cancellationToken);

            if (claimed == 0)
            {
                return null;
            }

            return await _db.CandidateActionTokens.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.TokenHash == hash && t.Purpose == purpose,
                    cancellationToken);
        }

        // In-memory / test providers: single-process claim (no ExecuteUpdate support).
        var token = await _db.CandidateActionTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash
                     && t.Purpose == purpose
                     && t.UsedAtUtc == null
                     && t.ExpiresAtUtc >= now,
                cancellationToken);
        if (token is null)
        {
            return null;
        }

        token.UsedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return token;
    }
}
