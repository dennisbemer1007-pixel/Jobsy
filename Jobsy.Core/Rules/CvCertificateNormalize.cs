using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

internal static class CvCertificateNormalize
{
    private static readonly Regex CertificaatSuffix = new(
        @"[\s-]*certificaat\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string DisplayName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        var withoutSuffix = CertificaatSuffix.Replace(trimmed, string.Empty).Trim();
        var token = CvExtractionGrounding.Normalize(withoutSuffix);
        if (token.StartsWith("vca", StringComparison.Ordinal))
        {
            return token.Contains("basis", StringComparison.Ordinal) ? "VCA-basis" : "VCA";
        }

        if (token == "bhv")
        {
            return "BHV";
        }

        return withoutSuffix.Length > 0 ? withoutSuffix : trimmed;
    }

    public static string DedupKey(string name) => CvExtractionGrounding.Normalize(DisplayName(name));

    public static List<CandidateCertificateDto> Dedupe(IEnumerable<CandidateCertificateDto> items)
    {
        var list = new List<CandidateCertificateDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            var label = DisplayName(item.Name);
            var key = DedupKey(label);
            if (!seen.Add(key))
            {
                continue;
            }

            list.Add(item with { Name = label });
        }

        return list;
    }
}