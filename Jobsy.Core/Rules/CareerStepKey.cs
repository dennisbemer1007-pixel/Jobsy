using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// Normalizes dream titles / step titles into stable keys for career-plan persistence.
/// </summary>
public static class CareerStepKey
{
    public static string ForDream(string? dreamTitle)
        => Compact(VacancyTextSearch.Normalize(StripDiacritics(dreamTitle ?? "")));

    public static string ForStep(string? title, int order)
    {
        var normalized = Compact(VacancyTextSearch.Normalize(StripDiacritics(title ?? "")));
        var payload = $"{order}|{normalized}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..10].ToLowerInvariant();
    }

    private static string Compact(string value)
        => Regex.Replace(value ?? "", @"\s+", " ").Trim();

    internal static string StripDiacritics(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var formD = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
