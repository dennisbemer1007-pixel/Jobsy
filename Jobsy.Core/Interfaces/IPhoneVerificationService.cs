namespace Jobsy.Core.Interfaces;

public interface ISmsSender
{
    Task SendAsync(string e164, string body, CancellationToken cancellationToken = default);
}

public interface IPhoneVerificationService
{
    Task<PhoneVerificationStartResult> StartAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PhoneVerificationVerifyResult> VerifyAsync(
        Guid userId,
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default);
}

public sealed record PhoneVerificationStartResult(bool Ok, string? Error, Guid? ChallengeId);

public sealed record PhoneVerificationVerifyResult(bool Ok, string? Error);
