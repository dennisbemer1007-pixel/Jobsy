using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// Carrière 05 §2: <c>GET api/me/journey-summary</c> is candidate-only, read-only and reports the
/// done state per stone from data the candidate already produced.
/// </summary>
[Collection("SequentialApi")]
public class CandidateJourneySummaryApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly RoleFunctionalWebAppFactory _factory;

    public CandidateJourneySummaryApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Employers_and_admins_are_rejected()
    {
        foreach (var userId in new[] { _factory.EmployerId, _factory.AdminId, _factory.SalesId })
        {
            var response = await JobsyTestAuth
                .CreateAuthenticatedClient(_factory, userId)
                .GetAsync("api/me/journey-summary");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        var response = await _factory.CreateClient().GetAsync("api/me/journey-summary");
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Unexpected status {response.StatusCode}");
    }

    [Fact]
    public async Task Candidate_sees_every_stone_flag_and_nothing_is_written()
    {
        await ClearAsync();
        var before = await CountsAsync();

        var summary = await Candidate().GetFromJsonAsync<JsonElement>("api/me/journey-summary", JsonOpts);

        foreach (var name in new[]
                 {
                     "discoveryDone", "passportDone", "careerDone", "jobMapDone", "applicationsDone"
                 })
        {
            Assert.True(summary.TryGetProperty(name, out var value), $"Missing {name}");
            Assert.True(
                value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                $"{name} should be a boolean but was {value.ValueKind}");
        }

        Assert.Equal(before, await CountsAsync());
    }

    [Fact]
    public async Task The_onboarding_finish_screen_marks_the_discovery_stone_done()
    {
        await ClearAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.CandidateOnboardings.Add(new CandidateOnboarding
            {
                Id = Guid.NewGuid(),
                UserId = _factory.CandidateId,
                CurrentStep = 10,
                FinishReached = true,
                StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var summary = await Candidate().GetFromJsonAsync<JsonElement>("api/me/journey-summary", JsonOpts);
        Assert.True(summary.GetProperty("discoveryDone").GetBoolean());

        await ClearAsync();
    }

    [Fact]
    public async Task A_liked_vacancy_marks_the_job_map_stone_done()
    {
        await ClearAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.VacancyLikes.Add(new VacancyLike
            {
                Id = Guid.NewGuid(),
                UserId = _factory.CandidateId,
                VacancyId = _factory.VacancyId,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var summary = await Candidate().GetFromJsonAsync<JsonElement>("api/me/journey-summary", JsonOpts);
        Assert.True(summary.GetProperty("jobMapDone").GetBoolean());

        await ClearAsync();
    }

    private HttpClient Candidate()
        => JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);

    private async Task<(int Onboardings, int Likes, int Shares, int References, int Progress)> CountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var id = _factory.CandidateId;
        return (
            await db.CandidateOnboardings.CountAsync(o => o.UserId == id),
            await db.VacancyLikes.CountAsync(l => l.UserId == id),
            await db.VacancyShares.CountAsync(s => s.UserId == id),
            await db.CandidateReferences.CountAsync(r => r.UserId == id),
            await db.CandidateCareerStepProgress.CountAsync(p => p.UserId == id));
    }

    private async Task ClearAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var id = _factory.CandidateId;

        db.CandidateOnboardings.RemoveRange(
            await db.CandidateOnboardings.Where(o => o.UserId == id).ToListAsync());
        db.VacancyLikes.RemoveRange(
            await db.VacancyLikes.Where(l => l.UserId == id).ToListAsync());
        db.VacancyShares.RemoveRange(
            await db.VacancyShares.Where(s => s.UserId == id).ToListAsync());
        db.CandidateReferences.RemoveRange(
            await db.CandidateReferences.Where(r => r.UserId == id).ToListAsync());
        db.CandidateCareerPlans.RemoveRange(
            await db.CandidateCareerPlans.Where(p => p.UserId == id).ToListAsync());
        await db.SaveChangesAsync();
    }
}
