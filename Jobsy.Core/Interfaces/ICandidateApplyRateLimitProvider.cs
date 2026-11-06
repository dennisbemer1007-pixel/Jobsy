using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public sealed record CandidateApplyRateLimitValues(int PerMinute, int PerHour, int PerDay)
{
    public static CandidateApplyRateLimitValues ProductDefaults { get; } = new(
        CandidateApplyRateLimitRules.DefaultPerMinute,
        CandidateApplyRateLimitRules.DefaultPerHour,
        CandidateApplyRateLimitRules.DefaultPerDay);
}

/// <summary>Cached apply limits from platform settings (short TTL, no redeploy).</summary>
public interface ICandidateApplyRateLimitProvider
{
    CandidateApplyRateLimitValues GetCurrent();

    void Invalidate();
}
