using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>Dev/acceptatie demo profile for opleidingen UAT (kandidaat@jobsy.local).</summary>
public static class CareerDemoPlanSeed
{
    private const string DemoEmail = "kandidaat@jobsy.local";
    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == DemoEmail, cancellationToken);
        if (user is null)
        {
            return;
        }

        if (!string.Equals(user.FullName, "Sanne de Vries", StringComparison.Ordinal))
        {
            user.FullName = "Sanne de Vries";
            user.FirstName = "Sanne";
        }

        await EnsureCompetencyAsync(db, user.Id, cancellationToken);
        await EnsureCareerPlanAsync(db, user.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCompetencyAsync(JobsyDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.CandidateCompetencies
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            db.CandidateCompetencies.Add(new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CandidateCompetencyStatuses.Completed,
                SamenwerkenPercent = 78,
                ResultaatgerichtheidPercent = 38,
                StressbestendigheidPercent = 32,
                InnovatiePercent = 52,
                ExtraversiePercent = 61,
                AnswersJson = "{}",
                CompletedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        if (!CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            row.Status = CandidateCompetencyStatuses.Completed;
            row.CompletedAtUtc ??= now;
        }

        row.SamenwerkenPercent = 78;
        row.ResultaatgerichtheidPercent = 38;
        row.StressbestendigheidPercent = 32;
        row.InnovatiePercent = 52;
        row.ExtraversiePercent = 61;
        row.UpdatedAtUtc = now;
    }

    private static async Task EnsureCareerPlanAsync(JobsyDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var generated = HorizonCareerPathBuilder.BuildLocal("Dierenarts");
        var plan = CareerPlanJson.WithStableKeys(generated);
        var stored = CareerPlanJson.FromGenerated(plan);
        var now = DateTime.UtcNow;

        var existing = await db.CandidateCareerPlans
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Status == CareerPlanStatuses.Active, cancellationToken);

        if (existing is null)
        {
            db.CandidateCareerPlans.Add(new CandidateCareerPlan
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DreamTitle = "Dierenarts",
                DreamKey = CareerStepKey.NormalizeDreamKey("Dierenarts"),
                PlanJson = CareerPlanJson.Serialize(stored),
                MatchPercent = plan.MatchPercent,
                MatchSummary = plan.MatchSummary ?? "",
                Status = CareerPlanStatuses.Active,
                PlanLanguage = "nl",
                FromAi = false,
                DreamSource = CareerDreamSources.Catalog,
                DreamCatalogKey = "dierenarts",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            return;
        }

        existing.DreamTitle = "Dierenarts";
        existing.DreamKey = CareerStepKey.NormalizeDreamKey("Dierenarts");
        existing.PlanJson = CareerPlanJson.Serialize(stored);
        existing.MatchPercent = plan.MatchPercent;
        existing.MatchSummary = plan.MatchSummary ?? "";
        existing.UpdatedAtUtc = now;
    }
}
