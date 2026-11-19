using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Harvest werkervaring rows from visible CV text (per line; no comma-fragment splitting).</summary>
internal static class CvWorkExperienceHarvest
{
    private static readonly Regex LineYearRangePrefix = new(
        @"^\s*(\d{4})\s*\p{Pd}+\s*(\d{4})(?:\s+|(?=[\p{L}]))",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LineSingleYearPrefix = new(
        @"^\s*(\d{4})\s+(?![-–])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static List<CandidateEmployerHistoryDto> Parse(string cvText, string haystack)
    {
        var list = new List<CandidateEmployerHistoryDto>();
        var body = ExtractWorkSection(cvText);
        if (string.IsNullOrWhiteSpace(body))
        {
            return list;
        }

        foreach (var rawLine in SplitSectionLines(body))
        {
            if (!LooksLikeExperienceLine(rawLine))
            {
                continue;
            }

            var parsed = ParseExperienceLine(rawLine);
            if (parsed is null)
            {
                continue;
            }

            if (!CvExtractionGrounding.ContainsPhrase(haystack, parsed.EmployerName))
            {
                continue;
            }

            if (parsed.Role is not null && !CvExtractionGrounding.ContainsPhrase(haystack, parsed.Role))
            {
                parsed = parsed with { Role = null };
            }

            list.Add(parsed);
        }

        return DedupeEmployers(list);
    }

    public static string? FindWorkLineContaining(string cvText, string employerName)
    {
        if (string.IsNullOrWhiteSpace(employerName))
        {
            return null;
        }

        var body = ExtractWorkSection(cvText);
        foreach (var rawLine in SplitSectionLines(body))
        {
            if (rawLine.Contains(employerName, StringComparison.OrdinalIgnoreCase))
            {
                return rawLine;
            }
        }

        return null;
    }

    public static bool YearLiteralsOnLine(string? rawLine, params string?[] years)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return false;
        }

        foreach (var year in years)
        {
            if (string.IsNullOrWhiteSpace(year))
            {
                continue;
            }

            if (!rawLine.Contains(year, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool LooksLikeExperienceLine(string rawLine)
    {
        return LineYearRangePrefix.IsMatch(rawLine) || LineSingleYearPrefix.IsMatch(rawLine);
    }

    public static CandidateEmployerHistoryDto? TryParseExperienceLine(string rawLine)
        => ParseExperienceLine(rawLine);

    private static CandidateEmployerHistoryDto? ParseExperienceLine(string rawLine)
    {
        rawLine = Regex.Replace(rawLine, @"\s+", " ").Trim();
        rawLine = Regex.Replace(rawLine, @"(\d{4})([\p{L}])", "$1 $2");
        string startYear;
        string? endYear;
        string remainder;

        var range = LineYearRangePrefix.Match(rawLine);
        if (range.Success)
        {
            startYear = range.Groups[1].Value;
            endYear = range.Groups[2].Value;
            remainder = rawLine[range.Length..].Trim();
        }
        else
        {
            var spacedYears = Regex.Match(
                rawLine,
                @"^\s*(\d{4})\s+(\d{4})(?:\s+|(?=[\p{L}]))",
                RegexOptions.CultureInvariant);
            if (spacedYears.Success)
            {
                startYear = spacedYears.Groups[1].Value;
                endYear = spacedYears.Groups[2].Value;
                remainder = rawLine[spacedYears.Length..].Trim();
            }
            else
            {
                var single = LineSingleYearPrefix.Match(rawLine);
                if (!single.Success)
                {
                    return null;
                }

                startYear = single.Groups[1].Value;
                endYear = null;
                remainder = rawLine[single.Length..].Trim();
            }
        }

        var parts = remainder.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        var role = parts[0].Trim();
        var employer = parts[1].Trim();
        if (employer.Length < 2 || role.Length < 2)
        {
            return null;
        }

        string? startMonth = $"{startYear}-01";
        string? endMonth = endYear is not null ? $"{endYear}-12" : null;

        return new CandidateEmployerHistoryDto(
            employer,
            role,
            StartMonth: startMonth,
            EndMonth: endMonth,
            IsCurrent: false);
    }

    internal static List<CandidateEmployerHistoryDto> DedupeEmployers(List<CandidateEmployerHistoryDto> items)
    {
        var result = new List<CandidateEmployerHistoryDto>();
        foreach (var item in items)
        {
            var existingIdx = FindDuplicateIndex(result, item);
            if (existingIdx < 0)
            {
                result.Add(item);
                continue;
            }

            result[existingIdx] = PreferRicher(result[existingIdx], item);
        }

        return result;
    }

    private static int FindDuplicateIndex(List<CandidateEmployerHistoryDto> list, CandidateEmployerHistoryDto item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (EmployerRoleKey(list[i]) == EmployerRoleKey(item))
            {
                return i;
            }

            if (SameEmployerDifferentLabel(list[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SameEmployerDifferentLabel(CandidateEmployerHistoryDto a, CandidateEmployerHistoryDto b)
    {
        var roleA = CvExtractionGrounding.Normalize(a.Role);
        var roleB = CvExtractionGrounding.Normalize(b.Role);
        if (roleA.Length > 0 && roleB.Length > 0 && !string.Equals(roleA, roleB, StringComparison.Ordinal))
        {
            return false;
        }

        var empA = CvExtractionGrounding.Normalize(a.EmployerName);
        var empB = CvExtractionGrounding.Normalize(b.EmployerName);
        if (empA.Length == 0 || empB.Length == 0)
        {
            return false;
        }

        return empA.Contains(empB, StringComparison.Ordinal)
               || empB.Contains(empA, StringComparison.Ordinal);
    }

    internal static CandidateEmployerHistoryDto PreferRicher(
        CandidateEmployerHistoryDto left,
        CandidateEmployerHistoryDto right)
    {
        var employer = left.EmployerName.Length >= right.EmployerName.Length
            ? left.EmployerName
            : right.EmployerName;
        var role = !string.IsNullOrWhiteSpace(left.Role) ? left.Role : right.Role;
        var start = left.StartMonth ?? right.StartMonth;
        var end = left.EndMonth ?? right.EndMonth;
        return left with
        {
            EmployerName = employer,
            Role = role,
            StartMonth = start,
            EndMonth = end,
            IsCurrent = false,
        };
    }

    internal static string EmployerRoleKey(CandidateEmployerHistoryDto e)
    {
        var employer = CvExtractionGrounding.Normalize(e.EmployerName);
        var role = CvExtractionGrounding.Normalize(e.Role);
        return $"{employer}|{role}";
    }

    private static IEnumerable<string> SplitSectionLines(string body)
    {
        body = body.Replace('\r', '\n');
        var withBoundaries = Regex.Replace(
            body,
            @"(?<!\d\s\p{Pd}\s)(?=\d{4}\s)",
            "\n",
            RegexOptions.CultureInvariant);
        return withBoundaries
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ExtractWorkSection(string cvText)
    {
        var start = cvText.IndexOf("Werkervaring", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        start += "Werkervaring".Length;
        var end = cvText.IndexOf("Opleiding", start, StringComparison.OrdinalIgnoreCase);
        var slice = end > start ? cvText[start..end] : cvText[start..];
        return slice.Trim();
    }
}
