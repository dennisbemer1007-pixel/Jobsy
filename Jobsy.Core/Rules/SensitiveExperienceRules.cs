using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Privacy rules for candidate work history: employer-visible vs matching-only signals.
/// </summary>
public static class SensitiveExperienceRules
{
    public const int GapMinimumMonths = 6;

    public const string GapOtherPeriod = "other-period";
    public const string GapCare = "care";
    public const string GapEducation = "education";
    public const string GapLeave = "leave";

    public static bool IsOnlyForMe(CandidateEmployerHistoryDto entry)
        => entry.OnlyForMe == true;

    public static IReadOnlyList<CandidateEmployerHistoryDto> VisibleToEmployers(
        IReadOnlyList<CandidateEmployerHistoryDto>? employers)
        => (employers ?? Array.Empty<CandidateEmployerHistoryDto>())
            .Where(e => !string.IsNullOrWhiteSpace(e.EmployerName) || !string.IsNullOrWhiteSpace(e.Role))
            .Where(e => !IsOnlyForMe(e))
            .ToList();

    /// <summary>Employer-facing preferences: hidden jobs and gap metadata stripped.</summary>
    public static CandidatePreferencesDto ForEmployerView(CandidatePreferencesDto preferences)
    {
        var visible = VisibleToEmployers(preferences.Employers);
        return preferences with
        {
            Employers = visible,
            TimelineGaps = null
        };
    }

    /// <summary>Labels used in matching (roles, duties, extracted skills). Includes hidden jobs.</summary>
    public static IReadOnlyList<string> MatchingExperienceLabels(CandidatePreferencesDto preferences)
    {
        var labels = new List<string>();
        foreach (var entry in preferences.Employers ?? Array.Empty<CandidateEmployerHistoryDto>())
        {
            if (!string.IsNullOrWhiteSpace(entry.Role))
            {
                labels.Add(entry.Role.Trim());
            }

            if (entry.Skills is { Count: > 0 })
            {
                labels.AddRange(entry.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
            }
            else if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                labels.Add(entry.Description.Trim());
            }
        }

        return labels
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
    }

    public static int CountForMinimumEmployers(CandidatePreferencesDto preferences)
        => (preferences.Employers ?? Array.Empty<CandidateEmployerHistoryDto>())
            .Count(e => !string.IsNullOrWhiteSpace(e.EmployerName) || !string.IsNullOrWhiteSpace(e.Role));

    public static IReadOnlyList<TimelineGap> DetectGaps(IReadOnlyList<CandidateEmployerHistoryDto>? employers)
    {
        var ranges = (employers ?? Array.Empty<CandidateEmployerHistoryDto>())
            .Select(TryRange)
            .Where(r => r is not null)
            .Select(r => r!.Value)
            .OrderBy(r => r.Start)
            .ToList();

        if (ranges.Count < 2)
        {
            return Array.Empty<TimelineGap>();
        }

        var gaps = new List<TimelineGap>();
        for (var i = 0; i < ranges.Count - 1; i++)
        {
            var end = ranges[i].End ?? YearMonth.Now();
            var nextStart = ranges[i + 1].Start;
            if (nextStart.TotalMonths - end.TotalMonths < GapMinimumMonths)
            {
                continue;
            }

            gaps.Add(new TimelineGap(
                end.AddMonths(1).ToStorage(),
                nextStart.AddMonths(-1).ToStorage()));
        }

        return gaps;
    }

    public static string? NormalizeGapChoice(string? choice)
    {
        var raw = (choice ?? string.Empty).Trim().ToLowerInvariant();
        return raw switch
        {
            GapOtherPeriod or GapCare or GapEducation or GapLeave => raw,
            _ => null
        };
    }

    public static CandidateTimelineGapDto? FindStoredGap(
        IReadOnlyList<CandidateTimelineGapDto>? stored,
        TimelineGap gap)
        => stored?.FirstOrDefault(g =>
            string.Equals(g.StartMonth, gap.StartMonth, StringComparison.Ordinal)
            && string.Equals(g.EndMonth, gap.EndMonth, StringComparison.Ordinal));

    private static (YearMonth Start, YearMonth? End)? TryRange(CandidateEmployerHistoryDto entry)
    {
        if (!YearMonth.TryParse(entry.StartMonth, out var start))
        {
            if (entry.Years is > 0 and <= 80)
            {
                var end = YearMonth.Now();
                start = end.AddYears(-entry.Years.Value);
                return (start, end);
            }

            return null;
        }

        YearMonth? endMonth = null;
        if (YearMonth.TryParse(entry.EndMonth, out var parsedEnd))
        {
            endMonth = parsedEnd;
        }
        else if (entry.IsCurrent != true && !string.IsNullOrWhiteSpace(entry.EndMonth))
        {
            return null;
        }

        return (start, endMonth);
    }

    public readonly record struct TimelineGap(string StartMonth, string EndMonth);

    private readonly record struct YearMonth(int Year, int Month)
    {
        public int TotalMonths => Year * 12 + Month;

        public static YearMonth Now()
        {
            var now = DateTime.UtcNow;
            return new YearMonth(now.Year, now.Month);
        }

        public static bool TryParse(string? value, out YearMonth result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2
                || !int.TryParse(parts[0], out var year)
                || !int.TryParse(parts[1], out var month)
                || month is < 1 or > 12)
            {
                return false;
            }

            result = new YearMonth(year, month);
            return true;
        }

        public YearMonth AddMonths(int delta)
        {
            var total = TotalMonths + delta;
            var year = total / 12;
            var month = total % 12;
            if (month <= 0)
            {
                month += 12;
                year -= 1;
            }

            return new YearMonth(year, month);
        }

        public YearMonth AddYears(int delta) => AddMonths(delta * 12);

        public string ToStorage() => $"{Year:D4}-{Month:D2}";
    }
}
