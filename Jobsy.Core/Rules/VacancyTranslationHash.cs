using System.Security.Cryptography;
using System.Text;

namespace Jobsy.Core.Rules;

/// <summary>Source hash for stored vacancy translations (Dutch title + description).</summary>
public static class VacancyTranslationHash
{
    public static string ForSource(string? title, string? description)
    {
        var payload = $"{(title ?? "").Trim()}\n{(description ?? "").Trim()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }
}
