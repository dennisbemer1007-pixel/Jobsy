using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>Same competence source as the coach: deep traits when that test is done, otherwise basic.</summary>
public static class WhoAmICompetenceLoader
{
    public static async Task<(IReadOnlyList<(string Code, int Score)> Traits, bool Deep)> LoadAsync(
        JobsyDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var deep = await db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.UserId == userId
                     && d.Kind == AssessmentKind.Competence
                     && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);
        if (deep is null)
        {
            return ([], false);
        }

        var report = CompetenceDeepReportJson.Deserialize(deep.ReportJson);
        if (report?.Traits is { Count: > 0 })
        {
            return (report.Traits.Select(trait => (trait.Domain, trait.Score)).ToList(), true);
        }

        var answers = DeepAnalysisCatalog.ParseAnswersJson(deep.AnswersJson);
        if (answers.Count > 0)
        {
            return (
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Competence)
                    .Select(domain => (domain.Domain, domain.Percent))
                    .ToList(),
                true);
        }

        return ([], true);
    }
}
