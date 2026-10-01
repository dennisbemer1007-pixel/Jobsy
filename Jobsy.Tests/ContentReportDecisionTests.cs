using Microsoft.Extensions.DependencyInjection;
using Jobsy.Core.Contracts;
using Jobsy.Core.Admin;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

/// <summary>
/// The admin side of DSA notice and action (public-pages 06): a decision needs a reason, takes the
/// content offline, closes every open report of that target and leaves an audit trail.
/// </summary>
public class ContentReportDecisionTests
{
    [Fact]
    public async Task Restrict_without_a_reason_is_refused()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync();

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.Restricted,
            Reason: "   "));

        Assert.False(result.Succeeded);
        Assert.Equal("reason_required", result.ErrorCode);
        Assert.Equal(
            ContentReportStatus.Open,
            (await harness.Db.ContentReports.FirstAsync()).Status);
    }

    [Fact]
    public async Task Restrict_on_a_company_is_not_allowed()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportCompanyAsync();

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Company,
            harness.CompanyId,
            ContentReportStatus.Restricted,
            "Klopt niet."));

        Assert.False(result.Succeeded);
        Assert.Equal("decision_not_allowed", result.ErrorCode);
    }

    [Fact]
    public async Task Remove_takes_the_vacancy_offline_closes_all_open_reports_and_audits()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("een@test.nl");
        await harness.ReportVacancyAsync("twee@test.nl");

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.Removed,
            "Nep vacature, breekt de regel over echte banen.",
            ActorUserId: harness.AdminId,
            ActorRole: nameof(UserRole.Admin)));

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.ClosedReportCount);

        var reports = await harness.Db.ContentReports.ToListAsync();
        Assert.All(reports, r => Assert.Equal(ContentReportStatus.Removed, r.Status));
        Assert.All(reports, r => Assert.NotNull(r.DecidedAtUtc));

        var vacancy = await harness.Db.Vacancies.FirstAsync(v => v.Id == harness.VacancyId);
        Assert.NotEqual(VacancyStatus.Active, vacancy.Status);

        var audit = Assert.Single(harness.Audit.Entries);
        Assert.Equal(AdminAuditKeys.ReportDecided, audit.Action);
        Assert.DoesNotContain("Nep vacature", audit.DetailsJson ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Remove_on_a_company_blocks_the_public_page()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportCompanyAsync();

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Company,
            harness.CompanyId,
            ContentReportStatus.Removed,
            "Bedrijfspagina klopt niet."));

        Assert.True(result.Succeeded);
        var company = await harness.Db.Companies.FirstAsync(c => c.Id == harness.CompanyId);
        Assert.NotNull(company.PublicPageBlockedAtUtc);

        var query = new PublicCompanyQuery(harness.Db, harness.Discovery);
        Assert.Empty(await query.GetByKvkAsync(harness.Kvk));
    }

    [Fact]
    public async Task No_action_keeps_the_vacancy_online_and_needs_no_reason()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync();

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.NoAction,
            Reason: null));

        Assert.True(result.Succeeded);
        var vacancy = await harness.Db.Vacancies.FirstAsync(v => v.Id == harness.VacancyId);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
    }

    [Fact]
    public async Task Deciding_without_open_reports_is_a_not_found()
    {
        var harness = await ContentReportHarness.CreateAsync();

        var result = await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Vacancy,
            harness.VacancyId,
            ContentReportStatus.NoAction,
            null));

        Assert.False(result.Succeeded);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task List_puts_open_reports_first_and_masks_the_reporter_email()
    {
        var harness = await ContentReportHarness.CreateAsync();
        await harness.ReportVacancyAsync("melder@voorbeeld.nl");
        await harness.ReportCompanyAsync();
        await harness.Service.DecideAsync(new ContentReportDecisionRequest(
            ContentReportTargetType.Company,
            harness.CompanyId,
            ContentReportStatus.NoAction,
            null));

        var rows = await harness.Service.ListAsync(openOnly: null);
        Assert.Equal(ContentReportStatus.Open, rows[0].Status);
        var masked = rows[0].ReporterEmailMasked;
        Assert.NotNull(masked);
        Assert.NotEqual("melder@voorbeeld.nl", masked);
    }
}

/// <summary>In-memory service harness for the 06 decision, mail and retention suites.</summary>
internal sealed class ContentReportHarness
{
    public const string ManagerEmail = "manager@meldbv.nl";

    public JobsyDbContext Db { get; private init; } = null!;
    public ContentReportService Service { get; private init; } = null!;
    public CapturingMailer Mailer { get; private init; } = null!;
    public RecordingAuditLog Audit { get; private init; } = null!;
    public Guid CompanyId { get; private init; }
    public Guid VacancyId { get; private init; }
    public Guid AdminId { get; private init; }
    public Guid ManagerId { get; private init; }
    public string Kvk { get; private init; } = "90000602";
    public IVacancyDiscoveryIndex Discovery { get; private init; } = null!;
    public IServiceProvider Provider { get; private init; } = null!;

    public static async Task<ContentReportHarness> CreateAsync()
    {
        var dbName = "ContentReportHarness-" + Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<IServiceScopeFactory>()
            .CreateScope().ServiceProvider.GetRequiredService<JobsyDbContext>();
        var discovery = new VacancyDiscoveryIndex(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<VacancyDiscoveryIndex>.Instance);
        var mailer = new CapturingMailer();
        var audit = new RecordingAuditLog();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        const string kvk = "90000602";

        ContentReportTestDb.SeedPublicVacancy(db, companyId, vacancyId, kvk);
        db.Users.Add(new User
        {
            Id = managerId,
            Email = ManagerEmail,
            FullName = "Manager",
            Role = UserRole.BranchManager,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = managerId, CompanyId = companyId });
        await db.SaveChangesAsync();

        return new ContentReportHarness
        {
            Db = db,
            Mailer = mailer,
            Audit = audit,
            CompanyId = companyId,
            VacancyId = vacancyId,
            AdminId = Guid.NewGuid(),
            ManagerId = managerId,
            Kvk = kvk,
            Discovery = discovery,
            Provider = provider,
            Service = new ContentReportService(
                db,
                new PublicCompanyQuery(db, discovery),
                new ArchivingVacancyProducts(db),
                mailer,
                new FixedLanguage(),
                new StubPlatformFeatures(),
                audit,
                NullLogger<ContentReportService>.Instance)
        };
    }

    public Task<ContentReportSubmitResult> ReportVacancyAsync(string? email = null)
        => Service.SubmitAsync(new ContentReportSubmission(
            "vacancy",
            VacancyId.ToString(),
            ContentReportReason.Fake,
            "Dit klopt niet.",
            email));

    public Task<ContentReportSubmitResult> ReportCompanyAsync(string? email = null)
        => Service.SubmitAsync(new ContentReportSubmission(
            "company",
            Kvk,
            ContentReportReason.WrongInfo,
            null,
            email));

    internal sealed class CapturingMailer : ITransactionalMailer
    {
        public List<(string To, ComposedEmail Mail)> Sent { get; } = [];

        public Task<EmailSendOutcome> SendAsync(
            ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent.Add((to, mail));
            return Task.FromResult(new EmailSendOutcome(true, false, null));
        }
    }

    internal sealed class RecordingAuditLog : IAdminAuditLog
    {
        public List<AdminAuditEntry> Entries { get; } = [];

        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public void Stage(AdminAuditEntry entry) => Entries.Add(entry);
    }

    /// <summary>Only <see cref="DeactivateAsync"/> is reachable from a moderation decision.</summary>
    private sealed class ArchivingVacancyProducts(JobsyDbContext db) : IVacancyProductService
    {
        public async Task<VacancyProductOutcome> DeactivateAsync(
            Vacancy vacancy,
            CancellationToken cancellationToken = default)
        {
            vacancy.Status = VacancyStatus.Archived;
            await db.SaveChangesAsync(cancellationToken);
            return new VacancyProductOutcome(true, null, vacancy);
        }

        public Task<VacancyProductOutcome> PublishAsync(Vacancy vacancy, VacancyPublishOptions options, Guid? actorUserId, CancellationToken cancellationToken = default, bool allowPendingApproval = true)
            => throw new NotSupportedException();

        public Task<VacancyProductOutcome> ApprovePublishAsync(Vacancy vacancy, Guid? actorUserId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VacancyProductOutcome> HighlightAsync(Vacancy vacancy, Guid? actorUserId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PushBomPreview> PreviewPushBomAsync(Vacancy vacancy, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VacancyProductOutcome> PushBomAsync(Vacancy vacancy, Guid? actorUserId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VacancyProductOutcome> ExtendAsync(Vacancy vacancy, Guid? actorUserId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FixedLanguage : IEmailLanguageResolver
    {
        public Task<EmailCulture> ResolveAsync(
            EmailRecipient recipient,
            CancellationToken cancellationToken = default)
            => Task.FromResult(EmailCulture.ForLanguage("nl"));
    }

    private sealed class StubPlatformFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.test", DateTime.UtcNow));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
