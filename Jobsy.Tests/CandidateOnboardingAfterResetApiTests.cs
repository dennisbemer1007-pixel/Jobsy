using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// API integration: profile reset and onboarding step 1 must accept saves without date of birth.
/// </summary>
public sealed class CandidateOnboardingAfterResetApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public CandidateOnboardingAfterResetApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task After_reset_put_profile_without_dob_and_onboarding_progress_succeed()
    {
        await SeedMidOnboardingAsync();

        var client = Authed(_factory.CandidateId);
        using var resetResponse = await client.PostAsJsonAsync(
            "api/privacy/reset-candidate-profile",
            new ResetCandidateProfileRequest("VERWIJDER"));
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        await using (var resetScope = _factory.Services.CreateAsyncScope())
        {
            var resetDb = resetScope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var user = await resetDb.Users.AsNoTracking().SingleAsync(u => u.Id == _factory.CandidateId);
            Assert.Null(user.FirstName);
            Assert.Null(user.LastName);
        }

        using var onboardingGet = await client.GetAsync("api/me/onboarding");
        Assert.Equal(HttpStatusCode.OK, onboardingGet.StatusCode);
        var onboarding = await onboardingGet.Content.ReadFromJsonAsync<CandidateOnboardingStateDto>();
        Assert.NotNull(onboarding);
        Assert.Equal(1, onboarding!.CurrentStep);

        using var profilePut = await client.PutAsJsonAsync("api/me/profile", new
        {
            firstName = "Dennis",
            lastName = "Bemer",
            preferences = new
            {
                roles = Array.Empty<string>(),
                maxTravelMinutes = 30,
                preferredTransport = "Fiets"
            }
        });
        var profileBody = await profilePut.Content.ReadAsStringAsync();
        Assert.True(
            profilePut.StatusCode == HttpStatusCode.OK,
            $"profile PUT: {(int)profilePut.StatusCode} {profileBody}");

        using var progressPut = await client.PutAsJsonAsync(
            "api/me/onboarding",
            new CandidateOnboardingProgressRequest(1, StepCompleted: false));
        Assert.Equal(HttpStatusCode.OK, progressPut.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db.CandidateOnboardings.AnyAsync(o => o.UserId == _factory.CandidateId));
        var prefs = await db.Users.AsNoTracking()
            .Where(u => u.Id == _factory.CandidateId)
            .Select(u => u.PreferencesJson)
            .SingleAsync();
        Assert.Contains("maxTravelMinutes", prefs ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fresh_candidate_put_profile_without_dob_succeeds()
    {
        var userId = Guid.NewGuid();
        const string email = "fresh-onboarding-step1@test.local";
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            db.Users.Add(new User
            {
                Id = userId,
                Email = email,
                FullName = "Fresh Signup",
                FirstName = "Dennis",
                LastName = "Bemer",
                Role = Jobsy.Core.Enums.UserRole.Candidate,
                IsActive = true,
                ConsentVersion = PrivacyConstants.CurrentConsentVersion,
                TermsAcceptedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var client = Authed(userId);
        using var profilePut = await client.PutAsJsonAsync("api/me/profile", new
        {
            firstName = "Dennis",
            lastName = "Bemer",
            preferences = new
            {
                roles = Array.Empty<string>(),
                maxTravelMinutes = 30,
                preferredTransport = "Fiets"
            }
        });
        var profileBody = await profilePut.Content.ReadAsStringAsync();
        Assert.True(
            profilePut.StatusCode == HttpStatusCode.OK,
            $"profile PUT: {(int)profilePut.StatusCode} {profileBody}");

        using var progressPut = await client.PutAsJsonAsync(
            "api/me/onboarding",
            new CandidateOnboardingProgressRequest(1, StepCompleted: false));
        Assert.Equal(HttpStatusCode.OK, progressPut.StatusCode);
    }

    private async Task SeedMidOnboardingAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (await db.CandidateOnboardings.AnyAsync(o => o.UserId == _factory.CandidateId))
        {
            db.CandidateOnboardings.RemoveRange(
                await db.CandidateOnboardings.Where(o => o.UserId == _factory.CandidateId).ToListAsync());
        }

        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = _factory.CandidateId,
            CurrentStep = 8,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}
