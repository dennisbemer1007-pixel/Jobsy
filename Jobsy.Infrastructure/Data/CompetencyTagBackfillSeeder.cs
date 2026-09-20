using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

/// <summary>
/// Backfills competence match tags and copies legacy combined Quick-Scan RIASEC into CandidateCareerInterests.
/// Safe to run on every migrate.
/// </summary>
internal static class CompetencyTagBackfillSeeder
{
    public static async Task BackfillAsync(JobsyDbContext db, ILogger logger)
    {
        var rows = await db.CandidateCompetencies
            .Where(c => c.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync();

        var existingCareer = await db.CandidateCareerInterests.Select(c => c.UserId).ToListAsync();
        var careerIds = existingCareer.ToHashSet();

        var updated = 0;
        var careers = 0;
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            var answers = CompetencyTestCatalog.ParseAnswersJson(row.AnswersJson);
            var preview = CompetencyTestCatalog.Score(answers);
            if (preview is { IsComplete: true })
            {
                var competenceTags = CompetencyTestCatalog.DeriveMatchTags(preview);
                var current = CompetencyTestCatalog.ParseTagsJson(row.MatchTagsJson);
                if (current.Count == 0)
                {
                    row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(competenceTags);
                    row.UpdatedAtUtc = now;
                    updated++;
                }
            }

            if (careerIds.Contains(row.UserId))
            {
                continue;
            }

            var legacy = CompetencyTestCatalog.ParseTagsJson(row.RiasecTagsJson);
            if (legacy.Count == 0)
            {
                legacy = CareerTestCatalog.DeriveLegacyCompactRiasecTags(answers).ToList();
            }

            if (legacy.Count == 0)
            {
                continue;
            }

            db.CandidateCareerInterests.Add(new CandidateCareerInterest
            {
                Id = Guid.NewGuid(),
                UserId = row.UserId,
                Status = CandidateCompetencyStatuses.Completed,
                AnswersJson = "{}",
                HollandCode = string.Concat(legacy.Take(3).Select(t =>
                    CareerTestCatalog.HollandLetter.TryGetValue(t, out var letter) ? letter : ' ')).Replace(" ", ""),
                RiasecTagsJson = CareerTestCatalog.SerializeTags(legacy),
                MatchTagsJson = CareerTestCatalog.SerializeTags(legacy),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CompletedAtUtc = row.CompletedAtUtc ?? now
            });
            careerIds.Add(row.UserId);
            careers++;
        }

        if (updated == 0 && careers == 0)
        {
            return;
        }

        await db.SaveChangesAsync();
        logger.LogInformation(
            "Backfilled competence tags for {Updated} rows and created {Careers} career-interest rows from legacy RIASEC.",
            updated,
            careers);
    }
}
