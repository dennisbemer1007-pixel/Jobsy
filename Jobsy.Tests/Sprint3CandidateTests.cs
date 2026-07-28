using Jobsy.Api.Controllers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class Sprint3CandidateTests
{
    [Fact]
    public void ParsePreferences_reads_seeded_json()
    {
        var prefs = MeController.ParsePreferences("""{"roles":["horeca","retail"],"maxTravelMinutes":30}""");
        Assert.Equal(2, prefs.Roles.Count);
        Assert.Contains("horeca", prefs.Roles);
        Assert.Equal(30, prefs.MaxTravelMinutes);
    }

    [Fact]
    public void ParsePreferences_handles_empty()
    {
        var prefs = MeController.ParsePreferences(null);
        Assert.Empty(prefs.Roles);
        Assert.Null(prefs.MaxTravelMinutes);
    }

    [Fact]
    public async Task Candidate_metrics_summary_counts_own_engagement()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var otherVacancyId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            Address = "Straat 1",
            KvkNumber = "123",
            Location = new GeoPoint(52.0, 4.3)
        });
        db.Users.AddRange(
            new User { Id = candidateId, Email = "a@test.nl", FullName = "A", Role = UserRole.Candidate, IsActive = true },
            new User { Id = otherId, Email = "b@test.nl", FullName = "B", Role = UserRole.Candidate, IsActive = true });
        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = vacancyId,
                Title = "Kassamedewerker",
                Description = "Demo",
                HourlyWage = 14m,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = new GeoPoint(52.0, 4.3),
                RequiredTransport = TransportMode.Bike
            },
            new Vacancy
            {
                Id = otherVacancyId,
                Title = "Hulp",
                Description = "Demo",
                HourlyWage = 14m,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = new GeoPoint(52.0, 4.3),
                RequiredTransport = TransportMode.Bike
            });

        var now = DateTime.UtcNow;
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "A",
            CandidateEmail = "a@test.nl",
            PreferredTransport = "Fiets",
            EstimatedTravelMinutes = 10,
            CreatedAt = now.AddHours(-1)
        });
        db.VacancyLikes.Add(new VacancyLike
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = candidateId,
            CreatedAt = now.AddHours(-1)
        });
        db.VacancyShares.Add(new VacancyShare
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = candidateId,
            Channel = ShareChannel.WhatsApp,
            CreatedAt = now.AddHours(-1)
        });
        db.VacancyClicks.Add(new VacancyClick
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            UserId = candidateId,
            CreatedAt = now.AddHours(-1)
        });
        db.VacancyLikes.Add(new VacancyLike
        {
            Id = Guid.NewGuid(),
            VacancyId = otherVacancyId,
            UserId = otherId,
            CreatedAt = now.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var sut = new CandidateMetricsQueryService(db);
        var summary = await sut.GetSummaryAsync(candidateId, "week");

        Assert.Equal(1, summary.Single(m => m.Key == "applications").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "likes").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "shares").Value);
        Assert.Equal(1, summary.Single(m => m.Key == "reactions").Value);
    }

    [Fact]
    public async Task Email_stub_writes_platform_log()
    {
        await using var db = CreateDb();
        var sut = new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance);
        await sut.SendAsync(new EmailMessage("a@test.nl", "Test", "<p>Hi</p>", "ApplicationConfirmation"));

        var log = Assert.Single(db.PlatformLogs);
        Assert.Equal("ApplicationConfirmation", log.Category);
        Assert.Contains("***", log.Message);
        Assert.DoesNotContain("a@test.nl", log.Message);
        Assert.DoesNotContain("<p>Hi</p>", log.DetailsJson);
        Assert.DoesNotContain("BodyHtml", log.DetailsJson);
    }

    [Theory]
    [InlineData("smtp.gmail.com", "smtp.gmail.com", 587)]
    [InlineData("smtp.gmail.com:465", "smtp.gmail.com", 465)]
    [InlineData("smtp://smtp.gmail.com:587", "smtp.gmail.com", 587)]
    public void Smtp_parses_host_and_port(string input, string expectedHost, int expectedPort)
    {
        Assert.True(SmtpEmailService.TryParseHostPort(input, out var host, out var port));
        Assert.Equal(expectedHost, host);
        Assert.Equal(expectedPort, port);
    }

    [Fact]
    public void Smtp_resolve_requires_full_credentials()
    {
        Assert.False(SmtpEmailService.TryResolveSmtp(null, out _));
        Assert.False(SmtpEmailService.TryResolveSmtp(
            new IntegrationCredentialSecrets(null, "u", "p", null, null, "smtp.gmail.com", null),
            out _));
        Assert.True(SmtpEmailService.TryResolveSmtp(
            new IntegrationCredentialSecrets(null, "u@gmail.com", "app-pass", null, null, "smtp.gmail.com", "u@gmail.com"),
            out var settings));
        Assert.Equal("smtp.gmail.com", settings.Host);
        Assert.Equal(587, settings.Port);
    }

    [Fact]
    public async Task Push_stub_includes_deeplink()
    {
        await using var db = CreateDb();
        var sut = new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance);
        await sut.SendAsync(new PushMessage(
            "a@test.nl",
            "Reactie",
            "Positief",
            "http://localhost:5201/vacancies/abc",
            "EmployerReaction"));

        var log = Assert.Single(db.PlatformLogs);
        Assert.Equal("EmployerReaction", log.Category);
        Assert.Contains("localhost:5201", log.DetailsJson);
    }

    [Fact]
    public void ApplicationRules_detects_duplicate_by_user_or_email()
    {
        var userId = Guid.NewGuid();
        Assert.True(ApplicationRules.IsSameCandidate(userId, "a@test.nl", userId, "other@test.nl"));
        Assert.True(ApplicationRules.IsSameCandidate(userId, "a@test.nl", null, "A@TEST.NL"));
        Assert.False(ApplicationRules.IsSameCandidate(userId, "a@test.nl", Guid.NewGuid(), "b@test.nl"));
    }

    [Fact]
    public void ApplicationRules_only_allows_react_when_pending()
    {
        Assert.True(ApplicationRules.CanEmployerReact(ApplicationStatus.Pending));
        Assert.False(ApplicationRules.CanEmployerReact(ApplicationStatus.Accepted));
        Assert.False(ApplicationRules.CanEmployerReact(ApplicationStatus.Rejected));

        Assert.True(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Pending, DateTime.UtcNow));
        Assert.False(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Pending, null));
        Assert.False(ApplicationRules.CanCandidateWithdraw(ApplicationStatus.Accepted, DateTime.UtcNow));
    }

    [Fact]
    public void Application_entity_has_unique_indexes_for_duplicate_prevention()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(Application));
        Assert.NotNull(entity);

        var indexes = entity!.GetIndexes().Where(i => i.IsUnique).ToList();
        Assert.Contains(indexes, i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["VacancyId", "CandidateEmail"]));
        Assert.Contains(indexes, i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["VacancyId", "CandidateUserId"])
            && i.GetFilter() is not null
            && i.GetFilter()!.Contains("CandidateUserId", StringComparison.Ordinal));
    }

    [Fact]
    public void HtmlEncode_escapes_mail_payload_characters()
    {
        var encoded = System.Net.WebUtility.HtmlEncode("<script>x</script> & \"Cafe\"");
        Assert.DoesNotContain("<script>", encoded);
        Assert.Contains("&lt;script&gt;", encoded);
        Assert.Contains("&amp;", encoded);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
