using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class ReferenceVerificationEligibilityTests
{
    [Fact]
    public void FeatureEligible_false_without_work_experience()
    {
        var prefs = new CandidatePreferencesDto([], null, null, Employers: []);
        Assert.False(ReferenceVerificationRules.FeatureEligible(prefs, allTestsComplete: true));
    }

    [Fact]
    public void FeatureEligible_false_without_completed_tests()
    {
        var prefs = new CandidatePreferencesDto(
            [],
            null,
            null,
            Employers: [new CandidateEmployerHistoryDto("Bakkerij", "Verkoop")]);
        Assert.False(ReferenceVerificationRules.FeatureEligible(prefs, allTestsComplete: false));
    }

    [Fact]
    public void FeatureEligible_true_with_work_and_all_tests()
    {
        var prefs = new CandidatePreferencesDto(
            [],
            null,
            null,
            Employers: [new CandidateEmployerHistoryDto("Bakkerij", "Verkoop")]);
        Assert.True(ReferenceVerificationRules.FeatureEligible(prefs, allTestsComplete: true));
    }

    [Fact]
    public async Task IsEligible_false_when_dna_incomplete()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "incomplete@example.com",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = JsonSerializer.Serialize(new
            {
                employers = new[] { new { employerName = "Shop", role = "Hulp" } }
            })
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, new IncompleteDnaReader());
        Assert.False(await sut.IsEligibleAsync(userId));
    }

    [Fact]
    public async Task IsEligible_true_when_dna_complete_and_work_present()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "eligible@example.com",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = JsonSerializer.Serialize(new
            {
                employers = new[] { new { employerName = "Bakkerij", role = "Verkoop" } }
            })
        });
        SeedCompletedDna(db, userId);
        await db.SaveChangesAsync();

        var sut = CreateSut(db, new CompleteDnaReader());
        Assert.True(await sut.IsEligibleAsync(userId));
    }

    private static ReferenceVerificationService CreateSut(JobsyDbContext db, IPassportDnaReader dna)
        => new(
            db,
            new NoopMailer(),
            new UserNotificationService(db),
            new NoopPush(),
            new NlLanguage(),
            new RefFeaturesOn(),
            dna,
            new EmptyCompetencies());

    private static void SeedCompletedDna(JobsyDbContext db, Guid userId)
    {
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}",
            RiasecTagsJson = "[]",
            MatchTagsJson = "[]"
        });
        db.CandidateCareerInterests.Add(new CandidateCareerInterest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}"
        });
        db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}"
        });
        db.CandidateValuesProfiles.Add(new CandidateValuesProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = "{}"
        });
    }

    private static JobsyDbContext CreateDb()
        => new(new DbContextOptionsBuilder<JobsyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class NoopMailer : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Provider));
    }

    private sealed class NoopPush : IPushNotificationService
    {
        public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NlLanguage : IEmailLanguageResolver
    {
        public Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailCulture.Nl);
    }

    private sealed class RefFeaturesOn : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                true, false, "http://localhost:5201", null,
                ReferenceVerificationEnabled: true));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class CompleteDnaReader : IPassportDnaReader
    {
        public Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(Guid userId, string language, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PassportDnaLayerFact>>(
                PassportDnaLayer.Keys.Select(k => new PassportDnaLayerFact(k, true, DateTime.UtcNow, ["x"])).ToList());
    }

    private sealed class IncompleteDnaReader : IPassportDnaReader
    {
        public Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(Guid userId, string language, CancellationToken cancellationToken = default)
            => Task.FromResult(PassportDnaLayer.None());
    }

    private sealed class EmptyCompetencies : ICandidateCompetencyService
    {
        public Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateCompetencyStateDto(
                CandidateCompetencyStatuses.Draft, new Dictionary<int, int>(), 0, 0, null, null, null, null, [], [], "", 1, 0, false));

        public Task<CandidateCompetencyStateDto> SaveAsync(
            Guid userId,
            IReadOnlyDictionary<int, int> answers,
            bool complete,
            CancellationToken cancellationToken = default)
            => GetAsync(userId, cancellationToken);

        public Task<CompetencyScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<CompetencyScores?>(null);

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);
    }
}
