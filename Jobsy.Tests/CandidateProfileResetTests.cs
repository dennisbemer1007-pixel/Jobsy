using System.Security.Claims;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class CandidateProfileResetTests
{
    [Theory]
    [InlineData("nl", "VERWIJDER", true)]
    [InlineData("nl", "verwijder", false)]
    [InlineData("en", "DELETE", true)]
    [InlineData("pl", "USUŃ", true)]
    [InlineData("ro", "STERGE", true)]
    public void Confirm_phrase_is_language_specific(string lang, string typed, bool ok)
        => Assert.Equal(ok, CandidateProfileResetConfirm.Matches(lang, typed));

    [Fact]
    public async Task Reset_clears_candidate_tables_keeps_login_and_anonymises_verified_application()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        const string email = "reset-me@test.nl";

        db.Users.AddRange(
            new User
            {
                Id = candidateId,
                Email = email,
                FullName = "Reset Kandidaat",
                Role = UserRole.Candidate,
                IsActive = true,
                PreferencesJson = """{"roles":["horeca"]}""",
                CandidateHowToCompletedAt = DateTime.UtcNow,
                ResearchConsentAt = DateTime.UtcNow,
                ResearchConsentVersion = PrivacyConstants.ResearchConsentTextVersion,
                OpenForWork = true
            },
            new User
            {
                Id = otherId,
                Email = "other@test.nl",
                FullName = "Andere",
                Role = UserRole.Candidate,
                IsActive = true,
                PreferencesJson = """{"roles":["retail"]}"""
            });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            Address = "Straat 1",
            KvkNumber = "123",
            Location = new GeoPoint(52.0, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Test",
            Description = "x",
            HourlyWage = 14m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        var verifiedAppId = Guid.NewGuid();
        var draftAppId = Guid.NewGuid();
        db.Applications.AddRange(
            new Application
            {
                Id = verifiedAppId,
                VacancyId = vacancyId,
                CandidateUserId = candidateId,
                CandidateName = "Reset Kandidaat",
                CandidateEmail = email,
                PreferredTransport = "Bike",
                Status = ApplicationStatus.Pending,
                EmailVerifiedAt = DateTime.UtcNow,
                SnapshotAboutMe = "Bio",
                CreatedAt = DateTime.UtcNow
            },
            new Application
            {
                Id = draftAppId,
                VacancyId = vacancyId,
                CandidateUserId = candidateId,
                CandidateName = "Reset Kandidaat",
                CandidateEmail = email,
                PreferredTransport = "Bike",
                Status = ApplicationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = candidateId });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            CurrentStep = 5,
            CompletedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.VacancyLikes.Add(new VacancyLike { Id = Guid.NewGuid(), UserId = candidateId, VacancyId = vacancyId, CreatedAt = DateTime.UtcNow });
        db.AssessmentResearchAttempts.Add(new AssessmentResearchAttempt
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            RespondentHash = "abc",
            TestKey = "competence",
            CompletedAtUtc = DateTime.UtcNow
        });
        db.CandidateCompetencies.Add(new CandidateCompetency { Id = Guid.NewGuid(), UserId = otherId });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        var principal = CreatePrincipal(email);
        await service.ResetAsync(principal, "VERWIJDER", "nl");

        var user = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.True(user.IsActive);
        Assert.Equal(email, user.Email);
        Assert.Null(user.PreferencesJson);
        Assert.Null(user.CandidateHowToCompletedAt);
        Assert.Null(user.ResearchConsentAt);
        Assert.False(user.OpenForWork);

        Assert.False(await db.CandidateCompetencies.AnyAsync(c => c.UserId == candidateId));
        Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == candidateId));
        Assert.False(await db.VacancyLikes.AnyAsync(l => l.UserId == candidateId));
        Assert.False(await db.AssessmentResearchAttempts.AnyAsync(a => a.UserId == candidateId));
        Assert.True(await db.CandidateCompetencies.AnyAsync(c => c.UserId == otherId));

        Assert.False(await db.Applications.AnyAsync(a => a.Id == draftAppId));
        var verified = await db.Applications.SingleAsync(a => a.Id == verifiedAppId);
        Assert.Null(verified.CandidateUserId);
        Assert.Equal("Verwijderde gebruiker", verified.CandidateName);
        Assert.Null(verified.SnapshotAboutMe);

        var log = await db.PlatformLogs.OrderByDescending(l => l.CreatedAt).FirstAsync();
        Assert.Contains("Kandidaat-reset uitgevoerd", log.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(email, log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_candidate_cannot_reset()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "sm@test.nl",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetAsync(CreatePrincipal("sm@test.nl"), "VERWIJDER", "nl"));
    }

    private static ClaimsPrincipal CreatePrincipal(string email)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test"));

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubUserLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        }
    }
}
