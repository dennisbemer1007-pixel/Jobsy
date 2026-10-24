using System.Net;
using System.Net.Http.Json;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

/// <summary>
/// API like endpoint against Haaglanden a3 seed ids (acceptatie banenkaart).
/// </summary>
public sealed class VacancyEngagementLikeHaaglandenTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly Guid DelftVacancyId = Guid.Parse("a3000000-0000-4000-8000-000000000069");

    private readonly RoleFunctionalWebAppFactory _factory;

    public VacancyEngagementLikeHaaglandenTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Candidate_can_like_haaglanden_a3_vacancy_idempotently()
    {
        await EnsureHaaglandenSeedAsync();

        var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);

        var first = await client.PostAsJsonAsync($"api/vacancies/{DelftVacancyId:D}/like", new { });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var body1 = await first.Content.ReadFromJsonAsync<LikeDto>();
        Assert.True(body1?.Liked);

        var second = await client.PostAsJsonAsync($"api/vacancies/{DelftVacancyId:D}/like", new { });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.True(await db.VacancyLikes.AsNoTracking()
                .AnyAsync(l => l.VacancyId == DelftVacancyId && l.UserId == _factory.CandidateId));
        }

        var delete = await client.DeleteAsync($"api/vacancies/{DelftVacancyId:D}/like");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        var body2 = await delete.Content.ReadFromJsonAsync<LikeDto>();
        Assert.False(body2?.Liked);
    }

    [Fact]
    public async Task User_lookup_resolves_jobsy_jwt_subject_without_email_claim()
    {
        await EnsureHaaglandenSeedAsync();
        var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
        var response = await client.GetAsync($"api/vacancies/{DelftVacancyId:D}/like");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task EnsureHaaglandenSeedAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        await HaaglandenVacanciesSeeder.SeedHaaglandenBanenkaartAsync(db, NullLogger.Instance);
        Assert.True(await db.Vacancies.AsNoTracking().AnyAsync(v => v.Id == DelftVacancyId));
    }

    private sealed class LikeDto
    {
        public bool Liked { get; set; }
    }
}
