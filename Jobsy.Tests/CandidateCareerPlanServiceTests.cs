using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CandidateCareerPlanServiceTests
{
    private sealed class StubGenerator : ICareerPathPlanGenerationService
    {
        public Task<HorizonCareerPathPlan> GenerateAsync(
            string dreamTitle,
            HorizonCareerProfileSnapshot? profile = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(HorizonCareerPathBuilder.BuildLocal(dreamTitle, profile));
    }

    private static (JobsyDbContext Db, CandidateCareerPlanService Svc, Guid UserId) Create()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new JobsyDbContext(options);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "kandidaat@test.local",
            FullName = "Test",
            Role = UserRole.Candidate,
            IsActive = true
        });
        db.SaveChanges();
        var svc = new CandidateCareerPlanService(db, new StubGenerator());
        return (db, svc, userId);
    }

    [Fact]
    public async Task Generate_persists_plan_and_get_returns_it()
    {
        var (db, svc, userId) = Create();
        await using (db)
        {
            Assert.Null(await svc.GetAsync(userId));
            var created = await svc.GenerateAndSaveAsync(userId, "Teamleider logistiek", null);
            Assert.Equal("Teamleider logistiek", created.DreamTitle);
            Assert.Equal(4, created.Steps.Count);
            Assert.Contains(created.Steps, s => s.Status == HorizonCareerStepKind.Active);

            var loaded = await svc.GetAsync(userId);
            Assert.NotNull(loaded);
            Assert.Equal(created.PlanId, loaded!.PlanId);
            Assert.Equal(created.Steps.Select(s => s.StepKey), loaded.Steps.Select(s => s.StepKey));
        }
    }

    [Fact]
    public async Task Complete_and_uncomplete_update_status()
    {
        var (db, svc, userId) = Create();
        await using (db)
        {
            var plan = await svc.GenerateAndSaveAsync(userId, "Teamleider logistiek", null);
            var first = plan.Steps[0];
            var after = await svc.CompleteStepAsync(userId, first.StepKey);
            Assert.Equal(HorizonCareerStepKind.Completed, after.Steps[0].Status);
            Assert.Equal(HorizonCareerStepKind.Active, after.Steps[1].Status);

            var undone = await svc.UncompleteStepAsync(userId, first.StepKey);
            Assert.Equal(HorizonCareerStepKind.Active, undone.Steps[0].Status);
        }
    }

    [Fact]
    public async Task MarkCourseOwned_adds_certificate_and_can_auto_complete()
    {
        var (db, svc, userId) = Create();
        await using (db)
        {
            var plan = await svc.GenerateAndSaveAsync(userId, "Teamleider logistiek", null);
            var skills = plan.Steps.First(s => s.CoursesTotal > 0);
            Assert.True(skills.CoursesTotal >= 1);
            Assert.Equal(0, skills.CoursesOnProfile);

            CareerPlanView? updated = null;
            foreach (var course in skills.Courses.Where(c => !c.OnProfile))
            {
                updated = await svc.MarkCourseOwnedAsync(userId, course.Name);
            }

            Assert.NotNull(updated);
            var refreshed = updated!.Steps.First(s => s.StepKey == skills.StepKey);
            Assert.Equal(refreshed.CoursesTotal, refreshed.CoursesOnProfile);
            Assert.Equal(HorizonCareerStepKind.Completed, refreshed.Status);

            var user = await db.Users.SingleAsync(u => u.Id == userId);
            Assert.Contains(skills.Courses[0].Name, user.PreferencesJson!, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MergeCertificateIntoPreferences_preserves_other_fields()
    {
        var json = """{"roles":["Kok"],"maxTravelMinutes":30,"certificates":[{"name":"VCA","year":2022}]}""";
        var merged = CandidateCareerPlanService.MergeCertificateIntoPreferences(json, "BHV", 2026);
        Assert.Contains("Kok", merged, StringComparison.Ordinal);
        Assert.Contains("VCA", merged, StringComparison.Ordinal);
        Assert.Contains("BHV", merged, StringComparison.Ordinal);
        Assert.Contains("30", merged, StringComparison.Ordinal);
    }
}
