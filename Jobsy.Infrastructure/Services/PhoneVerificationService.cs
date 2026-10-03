using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class PhoneVerificationService : IPhoneVerificationService
{
    private static readonly TimeSpan CodeTtl = TimeSpan.FromMinutes(10);

    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly ISmsSender _sms;

    public PhoneVerificationService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        ISmsSender sms)
    {
        _db = db;
        _features = features;
        _sms = sms;
    }

    public async Task<PhoneVerificationStartResult> StartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var flags = await _features.GetAsync(cancellationToken);
        if (!flags.PhoneVerificationEnabled)
        {
            return new PhoneVerificationStartResult(false, "phone_verification_disabled", null);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (user is null)
        {
            return new PhoneVerificationStartResult(false, "not_found", null);
        }

        var phone = CandidatePhoneRules.Normalize(user.PhoneNumber);
        if (!CandidatePhoneRules.IsValid(phone) || string.IsNullOrWhiteSpace(phone))
        {
            return new PhoneVerificationStartResult(false, "invalid_phone", null);
        }

        var now = DateTime.UtcNow;
        var code = VerificationCodes.CreateNumericCode();
        var challenge = new PhoneVerificationChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PhoneE164 = phone,
            CodeHash = VerificationCodes.Hash(code),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(CodeTtl)
        };
        _db.PhoneVerificationChallenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);
        await _sms.SendAsync(phone, $"Lobsy code: {code}", cancellationToken);
        return new PhoneVerificationStartResult(true, null, challenge.Id);
    }

    public async Task<PhoneVerificationVerifyResult> VerifyAsync(
        Guid userId,
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var flags = await _features.GetAsync(cancellationToken);
        if (!flags.PhoneVerificationEnabled)
        {
            return new PhoneVerificationVerifyResult(false, "phone_verification_disabled");
        }

        var challenge = await _db.PhoneVerificationChallenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;
        if (challenge is null
            || challenge.ConsumedAtUtc is not null
            || challenge.ExpiresAtUtc <= now
            || challenge.FailedAttempts >= VerificationCodes.MaxFailedAttempts)
        {
            return new PhoneVerificationVerifyResult(false, "code_expired");
        }

        if (!VerificationCodes.MatchesHash(challenge.CodeHash, code))
        {
            var attempts = challenge.FailedAttempts;
            var burned = VerificationCodes.RegisterFailedAttempt(ref attempts);
            challenge.FailedAttempts = attempts;
            if (burned)
            {
                challenge.ConsumedAtUtc = now;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new PhoneVerificationVerifyResult(false, burned ? "code_expired" : "invalid_code");
        }

        challenge.ConsumedAtUtc = now;
        var user = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        user.PhoneVerifiedAtUtc = now;
        user.PhoneVerifiedE164 = challenge.PhoneE164;
        await _db.SaveChangesAsync(cancellationToken);
        return new PhoneVerificationVerifyResult(true, null);
    }
}
