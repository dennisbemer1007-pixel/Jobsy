using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class DiplomaEvaluationPrivacyTests
{
    [Fact]
    public async Task Export_includes_the_fact_and_file_and_delete_removes_both()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "privacy-eval@example.com",
            FullName = "Privacy Eval",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1990, 1, 1)
        };
        db.Users.Add(user);
        var bytes = "%PDF-1.4 privacy"u8.ToArray();
        db.CandidateDiplomaEvaluations.Add(new CandidateDiplomaEvaluation
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            IssuingBody = DiplomaEvaluationRules.BodyNuffic,
            EquivalentLevelText = "exact zoals op het papier",
            EvaluationDate = new DateOnly(2022, 3, 4),
            ReferenceNumber = "PRIV-1",
            DocumentFileName = "waardering.pdf",
            DocumentContentType = "application/pdf",
            DocumentContent = bytes,
            DocumentSizeBytes = bytes.Length,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = Guid.NewGuid(),
            CandidateUserId = user.Id,
            CandidateName = user.FullName,
            CandidateEmail = user.Email,
            PreferredTransport = "Fiets",
            SnapshotDiplomaEvaluationsJson = """[{"referenceNumber":"PRIV-1"}]"""
        });
        await db.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, user.Email)],
            "test"));
        var privacy = new PrivacyDataService(db, new EmailLookup(db), new NoMail(), new FlagFeatures());
        var export = await privacy.ExportAsync(principal);
        var json = JsonSerializer.Serialize(export);
        Assert.Contains("exact zoals op het papier", json, StringComparison.Ordinal);
        Assert.Contains("PRIV-1", json, StringComparison.Ordinal);
        Assert.Contains(Convert.ToBase64String(bytes), json, StringComparison.Ordinal);

        await privacy.DeleteOrAnonymizeAsync(principal);
        Assert.Empty(await db.CandidateDiplomaEvaluations.Where(e => e.UserId == user.Id).ToListAsync());
        var app = await db.Applications.SingleAsync();
        Assert.Null(app.SnapshotDiplomaEvaluationsJson);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class EmailLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }

    private sealed class NoMail : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }

    private sealed class FlagFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, "https://lobsy.test", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}
