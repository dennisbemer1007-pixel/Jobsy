using Jobsy.Core.Entities;

namespace Jobsy.Core.Features;

/// <summary>
/// Pure passport-readiness for candidate post-login landing (paspoort flag ON).
/// Ready = <see cref="CandidateOnboarding.CompletedAtUtc"/> set (v1/v2/v3, including the
/// "Tot hier: 6 van 10" end variant).
/// </summary>
public static class CandidateLanding
{
    public static bool IsPassportReady(CandidateOnboarding? onboarding)
        => onboarding?.CompletedAtUtc is not null;

    public static bool IsPassportReady(DateTime? completedAtUtc)
        => completedAtUtc is not null;
}
