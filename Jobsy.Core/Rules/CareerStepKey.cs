using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Stable short key for a career-plan step (normalized title + order).</summary>
public static class CareerStepKey
{
    public static string Create(int order, string? title)
    {
        var normalized = CareerCourseMatcher.Normalize(title);
        var payload = $"{order}|{normalized}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant();
    }

    public static string NormalizeDreamKey(string? dreamTitle)
        => CareerCourseMatcher.Normalize(dreamTitle);
}
