using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Rules;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Stable revision for per-candidate match caches — changes when profile or assessments change.
/// </summary>
internal static class CandidateMatchProfileRevision
{
    public static string Compute(
        User user,
        CandidatePreferencesDto prefs,
        CandidateCompetency? competency,
        CandidateCareerInterest? career,
        CandidateCulturePersonalityProfile? culture,
        CandidateValuesProfile? values)
    {
        var sb = new StringBuilder(256);
        sb.Append(user.Id.ToString("N"));
        sb.Append('|').Append(user.OpenForWork);
        sb.Append('|').Append(user.PreferencesJson);
        sb.Append('|').Append(user.DateOfBirth?.ToString("O"));
        AppendRow(sb, competency?.UpdatedAtUtc, competency?.Status, competency?.AnswersJson);
        AppendRow(sb, career?.UpdatedAtUtc, career?.Status, career?.AnswersJson);
        AppendRow(sb, culture?.UpdatedAtUtc, culture?.Status, culture?.AnswersJson);
        AppendRow(sb, values?.UpdatedAtUtc, values?.Status, values?.AnswersJson);
        _ = prefs;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..16];
    }

    private static void AppendRow(StringBuilder sb, DateTime? updated, string? status, string? answers)
    {
        sb.Append('|').Append(updated?.Ticks.ToString("x") ?? "0");
        sb.Append(':').Append(status ?? "");
        sb.Append(':').Append(answers?.Length ?? 0);
    }
}
