using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class PrivacyDataCompletenessTests
{
    [Fact]
    public async Task Export_is_own_data_only_and_includes_stored_files_and_history()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "ik@example.com",
            FullName = "Ik Zelf",
            FirstName = "Ik",
            PhoneNumber = "0612345678",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = new DateOnly(1992, 2, 2),
            AuthenticatorSecret = "otp-secret-must-stay"
        };
        var other = new User
        {
            Id = Guid.NewGuid(),
            Email = "ander@example.com",
            FullName = "Iemand Anders",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.AddRange(user, other);
        var cv = "mijn-cv"u8.ToArray();
        db.CandidateUploadedCvs.Add(new CandidateUploadedCv
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FileName = "cv.pdf",
            ContentType = "application/pdf",
            Content = cv,
            SizeBytes = cv.Length,
            UploadedAtUtc = DateTime.UtcNow
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrentStep = 4,
            WizardVersion = 2,
            StartedAtUtc = DateTime.UtcNow,
            StepsJson = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });
        var applicationId = Guid.NewGuid();
        db.Applications.Add(new Application
        {
            Id = applicationId,
            VacancyId = Guid.NewGuid(),
            CandidateUserId = user.Id,
            CandidateName = user.FullName,
            CandidateEmail = user.Email,
            PreferredTransport = "Fiets",
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Kind = ApplicationStatusEventKind.Created,
            ToStatus = ApplicationStatus.Pending,
            OccurredAtUtc = DateTime.UtcNow,
            ActorKind = ApplicationStatusActorKind.Candidate
        });
        var appCv = "sollicitatie-cv"u8.ToArray();
        db.ApplicationUploadedCvs.Add(new ApplicationUploadedCv
        {
            ApplicationId = applicationId,
            FileName = "mee.pdf",
            ContentType = "application/pdf",
            Content = appCv,
            SizeBytes = appCv.Length
        });
        db.PersonalDataAccessLogs.Add(new PersonalDataAccessLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            ActorUserId = other.Id,
            ActorRole = "Admin",
            SubjectUserId = user.Id,
            Resource = "application.cv.download",
            Action = "download",
            Reason = "support",
            CorrelationId = "corr",
            IpHash = "ip-hash-must-stay"
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = other.Id,
            CurrentStep = 9,
            StartedAtUtc = DateTime.UtcNow,
            StepsJson = "[]",
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var privacy = new PrivacyDataService(db, new EmailLookup(db), new NoMail(), new FlagFeatures());
        var export = await privacy.ExportAsync(Principal(user.Email));
        var json = JsonSerializer.Serialize(export);

        Assert.Contains("Dit bestand is voor jou", json, StringComparison.Ordinal);
        Assert.Contains("Ik Zelf", json, StringComparison.Ordinal);
        Assert.Contains("0612345678", json, StringComparison.Ordinal);
        Assert.Contains(Convert.ToBase64String(cv), json, StringComparison.Ordinal);
        Assert.Contains(Convert.ToBase64String(appCv), json, StringComparison.Ordinal);
        Assert.Contains("StartHulp", json, StringComparison.Ordinal);
        Assert.Contains("SollicitatieVerloop", json, StringComparison.Ordinal);
        Assert.Contains("InzageLog", json, StringComparison.Ordinal);
        Assert.Contains("application.cv.download", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ander@example.com", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Iemand Anders", json, StringComparison.Ordinal);
        Assert.DoesNotContain("otp-secret-must-stay", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ip-hash-must-stay", json, StringComparison.Ordinal);
        Assert.Equal(1, await db.PlatformLogs.CountAsync(l => l.Category == "privacy.export"));
    }

    private static ClaimsPrincipal Principal(string email) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test"));

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

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }
}
