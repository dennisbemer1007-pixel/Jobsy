using System.Text.Json;
using System.Text.RegularExpressions;
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

public class ReferenceVerificationTests
{
    [Fact]
    public async Task Token_is_single_use_and_expires()
    {
        await using var db = CreateDb();
        var user = await SeedCandidateWithWorkAsync(db);
        var mail = new CapturingMailer();
        var sut = CreateSut(db, mail);

        var entryId = Guid.Parse(JsonDocument.Parse(user.PreferencesJson!).RootElement.GetProperty("employers")[0].GetProperty("entryId").GetString()!);
        var ok = await sut.RequestAsync(user.Id, entryId, "Mark Peters", "leidinggevende", "mark@example.com", true);
        Assert.True(ok.Ok);
        var token = TokenFrom(mail);

        Assert.Equal("open", (await sut.OpenAsync(token))!.State);
        Assert.Equal("ok", await sut.SubmitAsync(token, true, true, true, null, ["samenwerken"], null));
        Assert.Equal("used", (await sut.OpenAsync(token))!.State);

        var expired = await IssueExpiredAsync(db, sut, user.Id, entryId, mail);
        Assert.Equal("expired", await sut.SubmitAsync(expired, true, true, true, null, [], null));
    }

    private static async Task<string> IssueExpiredAsync(
        JobsyDbContext db,
        ReferenceVerificationService sut,
        Guid userId,
        Guid entryId,
        CapturingMailer mail)
    {
        foreach (var request in await db.WorkExperienceReferenceRequests.ToListAsync())
        {
            request.RemovedByCandidate = true;
        }

        db.WorkExperienceReferenceTokens.RemoveRange(await db.WorkExperienceReferenceTokens.ToListAsync());
        await db.SaveChangesAsync();
        Assert.True((await sut.RequestAsync(userId, entryId, "Mark", "collega", "mark@example.com", true)).Ok);
        var token = TokenFrom(mail);
        var row = await db.WorkExperienceReferenceTokens.SingleAsync();
        row.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        return token;
    }

    private static string TokenFrom(CapturingMailer mail)
    {
        var token = Regex.Match(mail.Sent[^1].Mail.Text, @"/werk-referentie/([a-f0-9]{64})").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private static ReferenceVerificationService CreateSut(JobsyDbContext db, CapturingMailer mail)
        => new(
            db,
            mail,
            new UserNotificationService(db),
            new CapturingPush(),
            new Language(),
            new AlwaysOnFeatures(),
            new FakeDnaReader(),
            new FakeCompetencies());

    private static async Task<User> SeedCandidateWithWorkAsync(JobsyDbContext db)
    {
        var entryId = Guid.NewGuid();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sanne@example.com",
            FullName = "Sanne de Vries",
            FirstName = "Sanne",
            Role = UserRole.Candidate,
            IsActive = true,
            PreferencesJson = JsonSerializer.Serialize(new
            {
                roles = Array.Empty<string>(),
                employers = new[]
                {
                    new
                    {
                        entryId,
                        employerName = "Bakkerij Jansen",
                        role = "Verkoopmedewerker",
                        startMonth = "2021-03"
                    }
                }
            })
        };
        db.Users.Add(user);
        SeedCompletedDna(db, user.Id);
        await db.SaveChangesAsync();
        return user;
    }

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

    private sealed class CapturingMailer : ITransactionalMailer
    {
        public List<(ComposedEmail Mail, string To)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((mail, to));
            return Task.FromResult(new EmailSendOutcome(true, false, null, EmailDeliveryKind.Provider));
        }
    }

    private sealed class CapturingPush : IPushNotificationService
    {
        public List<PushMessage> Messages { get; } = [];

        public Task SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Language : IEmailLanguageResolver
    {
        public Task<EmailCulture> ResolveAsync(EmailRecipient recipient, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailCulture.Nl);
    }

    private sealed class AlwaysOnFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                true, false, "http://localhost:5201", null,
                ReferenceVerificationEnabled: true));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class FakeDnaReader : IPassportDnaReader
    {
        public Task<IReadOnlyList<PassportDnaLayerFact>> ReadAsync(Guid userId, string language, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PassportDnaLayerFact>>(
                PassportDnaLayer.Keys.Select(k => new PassportDnaLayerFact(k, true, DateTime.UtcNow, ["x"])).ToList());
    }

    private sealed class FakeCompetencies : ICandidateCompetencyService
    {
        public Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new CandidateCompetencyStateDto(
                CandidateCompetencyStatuses.Completed, new Dictionary<int, int>(), 0, 0, null, null, null, null, [], [], "", 1, 0, false));

        public Task<CandidateCompetencyStateDto> SaveAsync(
            Guid userId,
            IReadOnlyDictionary<int, int> answers,
            bool complete,
            CancellationToken cancellationToken = default)
            => GetAsync(userId, cancellationToken);

        public Task<CompetencyScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<CompetencyScores?>(new CompetencyScores(80, 70, 60, 50, 40));

        public Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CandidateMatchedVacancyDto>>([]);
    }
}
