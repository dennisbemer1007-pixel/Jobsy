using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Data;

/// <summary>Dev/acceptatie demo: active dierenarts career plan for screenshot UAT (kandidaat@jobsy.local).</summary>
public static class CareerDemoPlanSeed
{
    private const string DemoEmail = "kandidaat@jobsy.local";

    public static async Task EnsureAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var userId = await db.Users.AsNoTracking()
            .Where(u => u.Email == DemoEmail)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (userId == Guid.Empty)
        {
            return;
        }

        var hasActive = await db.CandidateCareerPlans
            .AnyAsync(p => p.UserId == userId && p.Status == CareerPlanStatuses.Active, cancellationToken);
        if (hasActive)
        {
            return;
        }

        var generated = HorizonCareerPathBuilder.BuildLocal("Dierenarts");
        var plan = CareerPlanJson.WithStableKeys(generated);
        var stored = CareerPlanJson.FromGenerated(plan);
        var now = DateTime.UtcNow;

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

        await db.SaveChangesAsync(cancellationToken);
    }
}
