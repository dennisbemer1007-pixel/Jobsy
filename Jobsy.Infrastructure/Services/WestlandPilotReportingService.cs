using System.Globalization;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Golf2;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed record WestlandPilotStatsDto(
    string EnrollmentsStatus,
    int? Enrollments,
    string FeedbackResponsesStatus,
    int? FeedbackResponses,
    string AvgHelpfulnessStatus,
    int? AvgHelpfulnessRating);

public sealed class WestlandPilotReportingService(JobsyDbContext db)
{
    public static readonly string[] CsvHeaderColumns =
    [
        "task_id",
        "occupation_title",
        "task_title",
        "choice_count",
        "count_status"
    ];

    public static readonly string[] ForbiddenCsvColumns =
    [
        "open_answer",
        "description",
        "strengths_text",
        "motivation_text",
        "custom_text",
        "feedback_text"
    ];

    public async Task<WestlandPilotStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var enrollmentCount = await db.WestlandPilotEnrollments.AsNoTracking().CountAsync(cancellationToken);
        var (enrollStatus, enrollValue) = WestlandPilotPrivacy.SuppressCount(enrollmentCount);

        var feedbackCount = await db.PassportFourTestsFeedbacks.AsNoTracking()
            .CountAsync(f => f.ShareWithPilot, cancellationToken);
        var (feedbackStatus, feedbackValue) = WestlandPilotPrivacy.SuppressCount(feedbackCount);

        int? avgRating = null;
        string avgStatus = WestlandPilotPrivacy.StatusInsufficient;
        if (feedbackCount >= WestlandPilotPrivacy.KAnonymityThreshold)
        {
            var sum = await db.PassportFourTestsFeedbacks.AsNoTracking()
                .Where(f => f.ShareWithPilot)
                .SumAsync(f => (int?)f.HelpfulnessRating, cancellationToken);
            if (sum is int total)
            {
                avgRating = (int)Math.Round(total / (double)feedbackCount, MidpointRounding.AwayFromZero);
                avgStatus = WestlandPilotPrivacy.StatusOk;
            }
        }

        return new WestlandPilotStatsDto(
            enrollStatus,
            enrollValue,
            feedbackStatus,
            feedbackValue,
            avgStatus,
            avgRating);
    }

    public async Task<string> BuildTaskChoicesCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.CandidateWestlandTaskChoices.AsNoTracking()
            .Where(c => c.Task.Gecontroleerd)
            .GroupBy(c => new { c.TaskId, c.Task.TitleNl, Occupation = c.Task.Occupation.TitleNl })
            .Select(g => new
            {
                g.Key.TaskId,
                g.Key.Occupation,
                g.Key.TitleNl,
                Count = g.Count()
            })
            .OrderBy(r => r.Occupation)
            .ThenBy(r => r.TitleNl)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', CsvHeaderColumns));
        foreach (var row in rows)
        {
            var (status, value) = WestlandPilotPrivacy.SuppressCount(row.Count);
            sb.AppendLine(string.Join(',',
                Csv(row.TaskId.ToString("D")),
                Csv(row.Occupation),
                Csv(row.TitleNl),
                value?.ToString(CultureInfo.InvariantCulture) ?? "",
                Csv(status)));
        }

        return sb.ToString();
    }

    public static bool CsvIsAggregateOnly(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        var header = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.ToLowerInvariant() ?? "";
        return ForbiddenCsvColumns.All(col => !header.Contains(col, StringComparison.Ordinal));
    }

    private static string Csv(string? value)
    {
        var text = value ?? "";
        if (text.Contains('"', StringComparison.Ordinal) || text.Contains(',', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal))
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
