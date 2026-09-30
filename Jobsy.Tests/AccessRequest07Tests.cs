using System.Text.Json;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class AccessRequest07Tests
{
    [Fact]
    public void WorkingDays_skips_weekend_and_counts_across_weekend()
    {
        // Friday + 1 working day = Monday
        Assert.Equal(new DateOnly(2026, 10, 5), WorkingDays.AddWorkingDays(new DateOnly(2026, 10, 2), 1));
        // Friday + 3 working days = Wednesday
        Assert.Equal(new DateOnly(2026, 10, 7), WorkingDays.AddWorkingDays(new DateOnly(2026, 10, 2), 3));
        // Friday + 5 working days = Friday next week
        Assert.Equal(new DateOnly(2026, 10, 9), WorkingDays.AddWorkingDays(new DateOnly(2026, 10, 2), 5));

        var dutch = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");
        // Fri 2026-10-02 10:00 → Wed 2026-10-07 10:00 = Mon,Tue,Wed = 3 working days
        var from = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc);
        Assert.Equal(3, WorkingDays.CountWorkingDaysBetween(from, to, dutch));
    }

    [Fact]
    public async Task Access_request_requires_email_code_and_hides_manager_pii()
    {
        await using var db = CreateDb();
        var (companyId, emId) = await SeedManagedCompanyAsync(db);
        var capture = new CapturingEmailService(db);
        var sut = CreateAccess(db, capture);

        var submit = await sut.SubmitAsync(new AccessRequestSubmitRequest(
            "90123456",
            "90123456_0001",
            [],
            UserRole.BranchManager,
            "Sara Bakker",
            "Teamleider",
            "s.bakker@groenenzorg.nl",
            null,
            "Ik ben de nieuwe teamleider."));

        Assert.Equal(CompanyAccessRequestStatus.AwaitingEmail, submit.Status);
        Assert.False(string.IsNullOrWhiteSpace(capture.LastNumericCode));

        var json = JsonSerializer.Serialize(new
        {
            submit.RequestId,
            submit.Status,
            submit.Message
        });
        Assert.DoesNotContain("em@jobsy.local", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Eigenaar", json, StringComparison.OrdinalIgnoreCase);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ConfirmEmailAsync(submit.RequestId, "000000"));

        var confirmed = await sut.ConfirmEmailAsync(submit.RequestId, capture.LastNumericCode!);
        Assert.Equal(CompanyAccessRequestStatus.Open, confirmed.Status);
        Assert.Contains("beslist", confirmed.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("em@jobsy.local", confirmed.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Contains(capture.SentSubjects, s => s.Contains("Toegangsverzoek", StringComparison.OrdinalIgnoreCase));
        Assert.True(await db.UserNotifications.AnyAsync(n => n.UserId == emId));
    }

    [Fact]
    public async Task Access_approve_creates_membership_and_cannot_raise_role()
    {
        await using var db = CreateDb();
        var (companyId, emId) = await SeedManagedCompanyAsync(db);
        var capture = new CapturingEmailService(db);
        var sut = CreateAccess(db, capture);

        var submit = await sut.SubmitAsync(new AccessRequestSubmitRequest(
            "90123456", null, [], UserRole.EnterpriseManager,
            "Colleague", null, "colleague@example.com", null, null));
        await sut.ConfirmEmailAsync(submit.RequestId, capture.LastNumericCode!);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ApproveAsync(
                submit.RequestId,
                emId,
                UserRole.BranchManager,
                [companyId],
                isAdmin: false,
                grantedRole: UserRole.EnterpriseManager,
                grantedCompanyIds: [companyId]));

        // Manager may lower below requested, never raise above own rights.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ApproveAsync(
                submit.RequestId, emId, UserRole.EnterpriseManager, [companyId], false,
                UserRole.Admin, [companyId]));

        var bmId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = bmId,
            Email = "bm.only@jobsy.local",
            FullName = "BM",
            Role = UserRole.BranchManager,
            CompanyId = companyId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = bmId, CompanyId = companyId });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ApproveAsync(
                submit.RequestId, bmId, UserRole.BranchManager, [companyId], false,
                UserRole.EnterpriseManager, [companyId]));

        var decision = await sut.ApproveAsync(
            submit.RequestId, emId, UserRole.EnterpriseManager, [companyId], false,
            UserRole.BranchManager, [companyId]);
        Assert.Equal(CompanyAccessRequestStatus.Approved, decision.Status);
        Assert.NotNull(decision.CreatedUserId);

        var user = await db.Users.Include(u => u.CompanyMemberships)
            .SingleAsync(u => u.Email == "colleague@example.com");
        Assert.Equal(UserRole.BranchManager, user.Role);
        Assert.Contains(user.CompanyMemberships, m => m.CompanyId == companyId);
        Assert.Contains(capture.SentSubjects, s => s.Contains("Uitnodiging", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Access_reject_mails_reason()
    {
        await using var db = CreateDb();
        var (companyId, emId) = await SeedManagedCompanyAsync(db);
        var capture = new CapturingEmailService(db);
        var sut = CreateAccess(db, capture);

        var submit = await sut.SubmitAsync(new AccessRequestSubmitRequest(
            "90123456", null, [], UserRole.BranchManager,
            "X", null, "x@example.com", null, null));
        await sut.ConfirmEmailAsync(submit.RequestId, capture.LastNumericCode!);

        await sut.RejectAsync(submit.RequestId, emId, [companyId], false, "Geen plek");
        Assert.Contains(capture.Bodies, b => b.Contains("Geen plek", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Escalation_reminder_day3_escalation_day5_expiry_day30()
    {
        await using var db = CreateDb();
        var (companyId, _) = await SeedManagedCompanyAsync(db);
        var capture = new CapturingEmailService(db);
        var sut = CreateAccess(db, capture);

        var submit = await sut.SubmitAsync(new AccessRequestSubmitRequest(
            "90123456", null, [], UserRole.BranchManager,
            "Esc", null, "esc@example.com", null, null));
        await sut.ConfirmEmailAsync(submit.RequestId, capture.LastNumericCode!);

        var row = await db.CompanyAccessRequests.SingleAsync(r => r.Id == submit.RequestId);
        // Created Friday 2026-10-02
        row.CreatedAtUtc = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync();

        // Wednesday 2026-10-07 = 3 working days later
        var (reminders, escalations, _) = await sut.ProcessEscalationsAsync(
            new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, reminders);
        Assert.Equal(0, escalations);
        row = await db.CompanyAccessRequests.SingleAsync(r => r.Id == submit.RequestId);
        Assert.NotNull(row.ReminderSentAtUtc);
        Assert.Equal(CompanyAccessRequestStatus.Open, row.Status);

        // Friday 2026-10-09 = 5 working days later
        (_, escalations, _) = await sut.ProcessEscalationsAsync(
            new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, escalations);
        row = await db.CompanyAccessRequests.SingleAsync(r => r.Id == submit.RequestId);
        Assert.Equal(CompanyAccessRequestStatus.Escalated, row.Status);

        var (_, _, expiries) = await sut.ProcessEscalationsAsync(
            row.CreatedAtUtc.AddDays(31));
        Assert.Equal(1, expiries);
        row = await db.CompanyAccessRequests.SingleAsync(r => r.Id == submit.RequestId);
        Assert.Equal(CompanyAccessRequestStatus.Expired, row.Status);
    }

    [Fact]
    public async Task Claim_intermediary_only_company_keeps_intermediary_link()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Client Co",
            KvkNumber = "99990003",
            KvkEstablishmentId = "99990003_0001",
            Address = "X",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.IntermediaryClient,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        var imId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = imId,
            Email = "im@jobsy.local",
            FullName = "IM",
            Role = UserRole.Intermediary,
            CompanyId = companyId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = imId, CompanyId = companyId });
        await db.SaveChangesAsync();

        Assert.False(await CompanyOccupancy.HasActiveManagingEmployersAsync(db, companyId));

        var registration = new CompanyRegistrationService(
            db,
            new TestKvk(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            CreateFeatures(db),
            NullLogger<CompanyRegistrationService>.Instance);

        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "99990003", "99990003_0001", RegistrationScope.BranchOnly,
            "Claimer", "claimer@jobsy.local", null, AcceptedTerms: true,
            Password: "TestPassphrase!"));
        Assert.False(submit.RequiresTakeover);

        var token = await db.CompanyRegistrations.Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken).SingleAsync();
        var activated = await registration.ActivateAsync(token);
        Assert.Equal(companyId, activated.BranchCompanyId);

        var im = await db.Users.Include(u => u.CompanyMemberships).SingleAsync(u => u.Id == imId);
        Assert.True(im.IsActive);
        Assert.Contains(im.CompanyMemberships, m => m.CompanyId == companyId);

        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        Assert.Equal(CompanyVerificationMethod.IntermediaryClient, company.VerificationMethod);
    }

    [Fact]
    public void Guard_no_IntermediaryClient_entity_in_core()
    {
        // Dependencies G STILL ABSENT — registration/claims/takeovers must not invent IntermediaryClient.
        var type = Type.GetType("Jobsy.Core.Entities.IntermediaryClient, Jobsy.Core");
        Assert.Null(type);
        Assert.DoesNotContain(
            "IntermediaryClient",
            typeof(CompanyAccessRequestService).Assembly.GetTypes()
                .Select(t => t.Name));
        Assert.DoesNotContain(
            typeof(CompanyRegistrationService).Assembly.GetTypes()
                .Select(t => t.Name),
            name => name == "IntermediaryClient");
    }

    private static async Task<(Guid CompanyId, Guid EmId)> SeedManagedCompanyAsync(JobsyDbContext db)
    {
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Groen & Zorg",
            KvkNumber = "90123456",
            KvkEstablishmentId = "90123456_0001",
            Address = "X",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Backfill,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        var emId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = emId,
            Email = "em@jobsy.local",
            FullName = "Eigenaar",
            Role = UserRole.EnterpriseManager,
            CompanyId = companyId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = emId, CompanyId = companyId });
        await db.SaveChangesAsync();
        return (companyId, emId);
    }

    private static CompanyAccessRequestService CreateAccess(JobsyDbContext db, CapturingEmailService email)
        => new(
            db,
            email,
            new UserNotificationService(db),
            CreateFeatures(db),
            new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance),
            NullLogger<CompanyAccessRequestService>.Instance);

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        return new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            config);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmailService : IEmailService
    {
        private readonly EmailServiceStub _inner;
        public CapturingEmailService(JobsyDbContext db)
            => _inner = new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance);

        public string? LastNumericCode { get; private set; }
        public List<string> SentSubjects { get; } = [];
        public List<string> Bodies { get; } = [];

        public async Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            SentSubjects.Add(message.Subject ?? "");
            Bodies.Add(message.BodyHtml ?? "");
            var match = System.Text.RegularExpressions.Regex.Match(
                message.BodyHtml ?? "",
                @"data-lobsy-otp=""(\d{6})""");
            if (!match.Success)
            {
                match = System.Text.RegularExpressions.Regex.Match(message.BodyHtml ?? "", @"\b(\d{6})\b");
            }

            if (match.Success)
            {
                LastNumericCode = match.Groups[1].Value;
            }

            return await _inner.SendAsync(message, cancellationToken);
        }
    }

    private sealed class TestKvk : IKvkService
    {
        private readonly JobsyDbContext _db;
        public TestKvk(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(new KvkCompanyResult(kvkNumber, "Client Co", "X", ["5610"]));

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => (await LookupEstablishmentsAsync(kvkNumber, cancellationToken)).Establishments;

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
        {
            var managed = await CompanyOccupancy.LoadManagedCompanyIdsAsync(_db, cancellationToken);
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null && managed.Contains(c.Id))
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);
            return KvkEstablishmentsLookup.Ok(
            [
                new KvkEstablishmentResult(
                    kvkNumber, "0001", $"{kvkNumber}_0001", "Client Co", "X", 52, 4,
                    inUse.Contains($"{kvkNumber}_0001"), ["5610"])
            ]);
        }

        public Task<KvkSearchResult> SearchAsync(KvkSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkSearchResult.Ok([], 0));

        public Task<KvkCompanyProfile> GetProfileAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkCompanyProfile.NotFound(kvkNumber));
    }
}
