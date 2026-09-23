using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Dedup key for ATS scrapes: company + title + location/postcode.</summary>
public static class AtsDedupeHash
{
    private static readonly Regex CollapseWs = new(@"\s+", RegexOptions.Compiled);

    public static string Compute(string? companyName, string? title, string? locationOrPostal)
    {
        var raw = string.Join('|',
            Normalize(companyName),
            Normalize(title),
            Normalize(locationOrPostal));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var form = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(ch);
        }

        return CollapseWs.Replace(sb.ToString().Normalize(NormalizationForm.FormC), " ").Trim();
    }
}
