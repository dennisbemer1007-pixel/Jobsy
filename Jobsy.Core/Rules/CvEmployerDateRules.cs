using System.Text.RegularExpressions;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Parses employer start/end from visible CV text; never assumes "heden" without a literal present marker.</summary>
internal static class CvEmployerDateRules
{
    private static readonly string[] PresentMarkers = ["heden", "nu", "present", "now"];

    private static readonly Regex YearRangeRegex = new(
        @"(\d{4})\s*[-–]\s*(\d{4}|heden|nu|present|now)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex YearRangeSpacedRegex = new(
        @"(\d{4})\s+(\d{4})\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static CandidateEmployerHistoryDto Apply(
        CandidateEmployerHistoryDto employer,
        string haystack,
        string? rawCvText = null)
    {
        var name = CvExtractionGrounding.Normalize(employer.EmployerName);
        if (name.Length == 0)
        {
            return employer;
        }

        var idx = haystack.IndexOf(name, StringComparison.Ordinal);
        if (idx < 0)
        {
            return employer with { IsCurrent = employer.IsCurrent ?? false };
        }

        var windowLength = Math.Min(96, haystack.Length - idx);
        var window = haystack.Substring(idx, windowLength);
        var rawWindow = BuildRawWindow(rawCvText, employer.EmployerName);
        var match = YearRangeRegex.Match(rawWindow.Length > 0 ? rawWindow : window);
        if (!match.Success)
        {
            match = YearRangeRegex.Match(window);
        }

        if (!match.Success)
        {
            match = YearRangeSpacedRegex.Match(window);
        }

        if (!match.Success)
        {
            return employer with
            {
                EndMonth = employer.EndMonth,
                StartMonth = employer.StartMonth,
                IsCurrent = employer.IsCurrent ?? false,
            };
        }

        var startYear = match.Groups[1].Value;
        var endToken = match.Groups[2].Value;
        var startMonth = LobsyCvModelFactory.NormalizeMonth($"{startYear}-01") ?? $"{startYear}-01";
        string? endMonth = null;
        bool isCurrent = false;

        if (IsPresentMarker(endToken))
        {
            if (WindowContainsPresentMarker(window))
            {
                isCurrent = true;
            }
        }
        else
        {
            endMonth = LobsyCvModelFactory.NormalizeMonth($"{endToken}-12") ?? $"{endToken}-12";
        }

        return employer with
        {
            StartMonth = startMonth,
            EndMonth = endMonth,
            IsCurrent = isCurrent,
        };
    }

    private static string BuildRawWindow(string? rawCvText, string employerName)
    {
        if (string.IsNullOrWhiteSpace(rawCvText) || string.IsNullOrWhiteSpace(employerName))
        {
            return string.Empty;
        }

        var idx = rawCvText.IndexOf(employerName, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return string.Empty;
        }

        var length = Math.Min(120, rawCvText.Length - idx);
        return rawCvText.Substring(idx, length);
    }

    public static bool YearAllowedInHaystack(string haystack, string? yyyyMm)
    {
        if (string.IsNullOrWhiteSpace(yyyyMm) || yyyyMm.Length < 4)
        {
            return true;
        }

        var year = yyyyMm[..4];
        return haystack.Contains(year, StringComparison.Ordinal);
    }

    public static bool MayShowAsCurrent(string haystack, CandidateEmployerHistoryDto employer)
    {
        if (employer.IsCurrent == true)
        {
            return WindowNearEmployerContainsPresent(haystack, employer.EmployerName);
        }

        return false;
    }

    private static bool WindowNearEmployerContainsPresent(string haystack, string employerName)
    {
        var name = CvExtractionGrounding.Normalize(employerName);
        var idx = haystack.IndexOf(name, StringComparison.Ordinal);
        if (idx < 0)
        {
            return false;
        }

        var windowLength = Math.Min(96, haystack.Length - idx);
        return WindowContainsPresentMarker(haystack.Substring(idx, windowLength));
    }

    private static bool WindowContainsPresentMarker(string window)
        => PresentMarkers.Any(m => window.Contains(m, StringComparison.Ordinal));

    private static bool IsPresentMarker(string token)
        => PresentMarkers.Any(m => string.Equals(m, CvExtractionGrounding.Normalize(token), StringComparison.Ordinal));
}
