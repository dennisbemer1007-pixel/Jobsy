using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Backfills RIASEC / match tags for Quick-Scans completed before the tag columns existed
/// (or when tags were left empty). Safe to run on every migrate.
/// </summary>
internal static class CompetencyTagBackfillSeeder
{
    public static async Task BackfillAsync(JobsyDbContext db, ILogger logger)
    {
        var rows = await db.CandidateCompetencies
            .Where(c => c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync();

        var updated = 0;
        foreach (var row in rows)
        {
            var existingMatch = CompetencyTestCatalog.ParseTagsJson(row.MatchTagsJson);
            var existingRiasec = CompetencyTestCatalog.ParseTagsJson(row.RiasecTagsJson);
            if (existingMatch.Count > 0 && existingRiasec.Count > 0)
            {
                continue;
            }

            var answers = CompetencyTestCatalog.ParseAnswersJson(row.AnswersJson);
            var preview = CompetencyTestCatalog.Score(answers);
            if (preview is not { IsComplete: true })
            {
                continue;
            }

            var riasec = CompetencyTestCatalog.DeriveRiasecTags(answers);
            row.RiasecTagsJson = CompetencyTestCatalog.SerializeTags(riasec);
            row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(
                CompetencyTestCatalog.DeriveMatchTags(preview, riasec));
            row.UpdatedAtUtc = DateTime.UtcNow;
            updated++;
        }

        if (updated == 0)
        {
            return;
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Backfilled Quick-Scan RIASEC/match tags for {Count} completed competency rows.",
            updated);
    }
}
