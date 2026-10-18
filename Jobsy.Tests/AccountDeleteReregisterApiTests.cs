using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Contracts;
using Jobsy.Core.Dev;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>
/// Full account delete via API, then e-mail sign-up with the same address: identity bindings and
/// unique indexes must be released; onboarding step 1 must save on a clean profile.
/// </summary>
[Collection("SequentialApi")]
public sealed class AccountDeleteReregisterApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly RoleFunctionalWebAppFactory _factory;

    public AccountDeleteReregisterApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Delete_account_then_reregister_same_email_yields_clean_onboarding_step1()
    {
        Environment.SetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES", "1");
        DevEmailCodeCapture.Clear();

        var oldUserId = Guid.NewGuid();
        const string email = "delete-reregister-api@test.local";
        const string normalized = "delete-reregister-api@test.local";

        await SeedHeavyCandidateAsync(oldUserId, email);

        var client = Authed(oldUserId);
        using (var request = await client.PostAsJsonAsync(
                   "api/privacy/request-unsubscribe",
                   new RequestUnsubscribeRequest(AccountUnsubscribeReasons.Privacy)))
        {
            Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        }

        var deleteCode = await ReadCapturedCodeAsync(client, email);
        using (var delete = await client.PostAsJsonAsync(
                   "api/privacy/delete-account",
                   new ConfirmUnsubscribeRequest(deleteCode)))
        {
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var old = await db.Users.SingleAsync(u => u.Id == oldUserId);
            Assert.False(old.IsActive);
            Assert.StartsWith("deleted-", old.Email, StringComparison.Ordinal);
            Assert.False(await db.LocalAuthCredentials.AnyAsync(c => c.UserId == oldUserId));
            Assert.False(await db.UserExternalLogins.AnyAsync(l => l.UserId == oldUserId));
            Assert.False(await db.UserDeviceSessions.AnyAsync(s => s.UserId == oldUserId));
            Assert.False(await db.CandidateActionTokens.AnyAsync(t => t.UserId == oldUserId));
            Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == oldUserId));
            Assert.False(await db.Users.AnyAsync(u => u.Email == email && u.IsActive));
            Assert.False(await db.LocalAuthCredentials.AnyAsync(c => c.Email == email));
        }

        var anon = _factory.CreateClient();
        anon.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Jobsy-Provision-Secret",
            "local-dev-jobsy-provision-secret");
        using (var start = await anon.PostAsJsonAsync(
                   "api/auth/email-code/start",
                   new EmailCodeStartRequest(email, FirstName: "Fris", Culture: "nl")))
        {
            Assert.Equal(HttpStatusCode.Accepted, start.StatusCode);
            var startBody = await start.Content.ReadFromJsonAsync<EmailCodeStartResponse>(JsonOpts);
            Assert.NotNull(startBody);

            var signUpCode = await ReadCapturedCodeAsync(anon, email);
            using var verify = await anon.PostAsJsonAsync(
                "api/auth/email-code/verify",
                new EmailCodeVerifyRequest(startBody!.ChallengeId, signUpCode, RememberDevice: false));
            Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
            var login = await verify.Content.ReadFromJsonAsync<LocalLoginResponse>(JsonOpts);
            Assert.NotNull(login);
            Assert.Equal(email, login!.Email);

            var newUserId = login.UserId ?? Guid.Empty;
            Assert.NotEqual(oldUserId, newUserId);

            JobsyTestAuth.Authorize(anon, newUserId);
            using var profilePut = await anon.PutAsJsonAsync("api/me/profile", new
            {
                firstName = "Fris",
                lastName = "Start",
                preferences = new
                {
                    roles = Array.Empty<string>(),
                    maxTravelMinutes = 30,
                    preferredTransport = "Fiets"
                }
            });
            Assert.Equal(HttpStatusCode.OK, profilePut.StatusCode);

            using var onboardingPut = await anon.PutAsJsonAsync(
                "api/me/onboarding",
                new CandidateOnboardingProgressRequest(1, StepCompleted: false));
            Assert.Equal(HttpStatusCode.OK, onboardingPut.StatusCode);

            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var fresh = await db.Users.SingleAsync(u => u.Id == newUserId);
            Assert.True(fresh.IsActive);
            Assert.Equal(email, fresh.Email);
            Assert.Equal("Fris", fresh.FirstName);
            Assert.DoesNotContain("Winkel", fresh.PreferencesJson ?? "", StringComparison.Ordinal);
            Assert.Null(fresh.CandidateHowToCompletedAt);
            Assert.False(await db.CandidateCompetencies.AnyAsync(c => c.UserId == newUserId));
            Assert.True(await db.CandidateOnboardings.AnyAsync(o => o.UserId == newUserId));
            Assert.True(await db.EmailSignInChallenges.AnyAsync(c =>
                c.EmailNormalized == normalized && c.ConsumedAtUtc != null));
        }
    }

    private async Task SeedHeavyCandidateAsync(Guid userId, string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            FullName = "Te verwijderen",
            FirstName = "Oud",
            LastName = "Data",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1995, 3, 3),
            OpenForWork = true,
            PreferencesJson = """{"roles":["Winkel"],"maxTravelMinutes":45}""",
            CandidateHowToCompletedAt = DateTime.UtcNow.AddDays(-3),
            ConsentVersion = PrivacyConstants.CurrentConsentVersion,
            TermsAcceptedAt = DateTime.UtcNow.AddDays(-10)
        });
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            PasswordHash = "hash"
        });
        db.UserExternalLogins.Add(new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = "google",
            ProviderSubject = "sub-delete-reregister"
        });
        db.UserDeviceSessions.Add(new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = "refresh",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        });
        db.CandidateActionTokens.Add(new CandidateActionToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = "SetUnavailable",
            TokenHash = "abc",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 4,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = userId });
        await db.SaveChangesAsync();
    }

    private static async Task<string> ReadCapturedCodeAsync(HttpClient client, string email)
    {
        using var response = await client.GetAsync($"api/e2e/last-email-code?email={Uri.EscapeDataString(email)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("code").GetString()
               ?? throw new InvalidOperationException("Geen OTP in capture.");
    }

    private HttpClient Authed(Guid userId)
    {
        var client = _factory.CreateClient();
        JobsyTestAuth.Authorize(client, userId);
        return client;
    }
}
