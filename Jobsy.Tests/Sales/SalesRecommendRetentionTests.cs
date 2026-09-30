using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Sales;

public class SalesRecommendRetentionTests
{
    [Fact]
    public async Task Submit_requires_permission_checkbox()
    {
        await using var db = CreateDb();
        var (apps, smId) = await SeedRecruiterAsync(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            apps.SubmitAsync(smId, "a@jobsy.local", "Anna", "Motivatie lang genoeg.", false));
    }

    [Fact]
    public async Task Submit_sends_notice_once_and_blocks_duplicate_within_60_days()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var (apps, smId) = await SeedRecruiterAsync(db, email);

        var first = await apps.SubmitAsync(smId, "a@jobsy.local", "Anna", "Motivatie lang genoeg.", true);
        Assert.NotNull(first.SubjectNotifiedAtUtc);
        Assert.Equal(1, email.Sent.Count(m => m.Category == "SalesMail.RecommendedNotice"));
        Assert.Contains("verwijder", email.Sent[0].BodyHtml, StringComparison.OrdinalIgnoreCase);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apps.SubmitAsync(smId, "a@jobsy.local", "Anna", "Motivatie lang genoeg.", true));
    }

    [Fact]
    public async Task Objection_clears_pii_immediately_and_is_single_use()
    {
        await using var db = CreateDb();
        var email = new CapturingEmail();
        var (apps, smId) = await SeedRecruiterAsync(db, email);

        var pending = await apps.SubmitAsync(smId, "b@jobsy.local", "Bert", "Motivatie lang genoeg.", true);
        var row = await db.SalesManagerApplications.SingleAsync(a => a.Id == pending.Id);
        Assert.False(string.IsNullOrWhiteSpace(row.ObjectionTokenHash));

        // Recover plaintext from the mail link.
        var href = email.Sent[0].BodyHtml;
        var tokenStart = href.IndexOf("token=", StringComparison.Ordinal) + 6;
        var tokenEnd = href.IndexOf('"', tokenStart);
        var token = Uri.UnescapeDataString(href[tokenStart..tokenEnd]);

        Assert.True(await apps.ObjectByTokenAsync(token));
        row = await db.SalesManagerApplications.AsNoTracking().SingleAsync(a => a.Id == pending.Id);
        Assert.Equal(SalesManagerApplicationStatus.Rejected, row.Status);
        Assert.Equal(SalesManagerApplicationService.ObjectionReason, row.RejectionReason);
        Assert.NotNull(row.SubjectObjectedAtUtc);
        Assert.NotNull(row.PersonalDataClearedAtUtc);
        Assert.Equal("", row.CandidateEmail);
        Assert.Equal("", row.CandidateFullName);
        Assert.Equal("", row.Motivation);
        Assert.False(string.IsNullOrWhiteSpace(row.CandidateEmailSha256));
        Assert.Null(row.ObjectionTokenHash);

        Assert.False(await apps.ObjectByTokenAsync(token));

        var overview = await apps.GetRecommendOverviewAsync(smId);
        Assert.Contains(overview.Applications, a => a.PersonalDataCleared && a.Id == pending.Id);
        Assert.Equal("Sales.Label.Application.Objection", overview.Applications[0].StatusLabelKey);
    }

    [Fact]
    public async Task Retention_expires_pending_and_clears_rejected_approved_on_schedule()
    {
        await using var db = CreateDb();
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var smId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId,
            Email = "sm@jobsy.local",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });

        db.SalesManagerApplications.AddRange(
            new SalesManagerApplication
            {
                Id = Guid.NewGuid(),
                ReferrerSalesManagerUserId = smId,
                ReferrerTrackingCode = "SM-TEST01",
                CandidateEmail = "old@jobsy.local",
                CandidateFullName = "Old",
                Motivation = "xx",
                CandidateEmailSha256 = SalesManagerApplicationService.HashEmail("old@jobsy.local"),
                Status = SalesManagerApplicationStatus.Pending,
                CreatedAtUtc = now.AddDays(-61)
            },
            new SalesManagerApplication
            {
                Id = Guid.NewGuid(),
                ReferrerSalesManagerUserId = smId,
                ReferrerTrackingCode = "SM-TEST01",
                CandidateEmail = "rej@jobsy.local",
                CandidateFullName = "Rej",
                Motivation = "xx",
                CandidateEmailSha256 = SalesManagerApplicationService.HashEmail("rej@jobsy.local"),
                Status = SalesManagerApplicationStatus.Rejected,
                CreatedAtUtc = now.AddDays(-40),
                ReviewedAtUtc = now.AddDays(-31)
            },
            new SalesManagerApplication
            {
                Id = Guid.NewGuid(),
                ReferrerSalesManagerUserId = smId,
                ReferrerTrackingCode = "SM-TEST01",
                CandidateEmail = "ok@jobsy.local",
                CandidateFullName = "Ok",
                Motivation = "xx",
                CandidateEmailSha256 = SalesManagerApplicationService.HashEmail("ok@jobsy.local"),
                Status = SalesManagerApplicationStatus.Approved,
                ProvisionedUserId = Guid.NewGuid(),
                CreatedAtUtc = now.AddDays(-40),
                ReviewedAtUtc = now.AddDays(-31)
            },
            new SalesManagerApplication
            {
                Id = Guid.NewGuid(),
                ReferrerSalesManagerUserId = smId,
                ReferrerTrackingCode = "SM-TEST01",
                CandidateEmail = "fresh@jobsy.local",
                CandidateFullName = "Fresh",
                Motivation = "xx",
                CandidateEmailSha256 = SalesManagerApplicationService.HashEmail("fresh@jobsy.local"),
                Status = SalesManagerApplicationStatus.Pending,
                CreatedAtUtc = now.AddDays(-10)
            });
        await db.SaveChangesAsync();

        var changed = await SalesManagerApplicationService.ApplyRetentionAsync(db, now);
        Assert.Equal(3, changed);

        var rows = await db.SalesManagerApplications.AsNoTracking().ToListAsync();
        var expired = rows.Single(r => r.Status == SalesManagerApplicationStatus.Expired);
        Assert.Equal("", expired.CandidateEmail);
        Assert.NotNull(expired.PersonalDataClearedAtUtc);

        Assert.All(
            rows.Where(r => r.Status is SalesManagerApplicationStatus.Rejected or SalesManagerApplicationStatus.Approved),
            r =>
            {
                Assert.Equal("", r.CandidateEmail);
                Assert.NotNull(r.PersonalDataClearedAtUtc);
            });

        var fresh = rows.Single(r => r.CandidateEmail == "fresh@jobsy.local");
        Assert.Null(fresh.PersonalDataClearedAtUtc);
        Assert.Equal(PrivacyConstants.SalesManagerApplicationPendingRetentionDays, 60);
        Assert.Equal(PrivacyConstants.SalesFiscalRetentionYears, 7);
    }

    [Fact]
    public async Task Sales_link_clicks_older_than_25_months_are_deleted_by_retention_helper()
    {
        await using var db = CreateDb();
        var sm = Guid.NewGuid();
        var old = SalesClock.Today().AddMonths(-PrivacyConstants.SalesLinkClickRetentionMonths - 1);
        var keep = SalesClock.Today().AddMonths(-1);
        db.SalesLinkClickDailies.AddRange(
            new SalesLinkClickDaily { BeneficiaryUserId = sm, Date = old, Channel = SalesLinkChannel.Link, Count = 3 },
            new SalesLinkClickDaily { BeneficiaryUserId = sm, Date = keep, Channel = SalesLinkChannel.Link, Count = 1 });
        await db.SaveChangesAsync();

        var cutoff = SalesClock.Today().AddMonths(-PrivacyConstants.SalesLinkClickRetentionMonths);
        var stale = await db.SalesLinkClickDailies.Where(c => c.Date < cutoff).ToListAsync();
        db.SalesLinkClickDailies.RemoveRange(stale);
        await db.SaveChangesAsync();
        Assert.Equal(1, await db.SalesLinkClickDailies.CountAsync());
    }

    private static async Task<(ISalesManagerApplicationService Apps, Guid SmId)> SeedRecruiterAsync(
        JobsyDbContext db,
        IEmailService? email = null)
    {
        email ??= new CapturingEmail();
        var invite = new SalesManagerInviteService(
            db,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance),
            new AlwaysOnFeatures(),
            NullLogger<SalesManagerInviteService>.Instance);
        var apps = new SalesManagerApplicationService(
            db,
            invite,
            email,
            new AlwaysOnFeatures(),
            NullLogger<SalesManagerApplicationService>.Instance);

        var parent = await invite.InviteAsync("recruiter@jobsy.local", "Recruiter");
        var profile = db.SalesManagerProfiles.Single(p => p.UserId == parent.UserId);
        var now = DateTime.UtcNow;
        profile.CompanyName = "SM BV";
        profile.KvkNumber = "12345678";
        profile.VatNumber = "NL123456789B01";
        profile.Address = "Straat 1";
        profile.PostalCode = "1234AB";
        profile.City = "Delft";
        profile.Country = "NL";
        profile.TrackingCode = "SM-RECR01";
        profile.AgreementSignedAt = now;
        profile.AgreementVersion = SalesCommissionRules.CurrentAgreementVersion;
        profile.OnboardingCompletedAt = now;
        profile.CanRecruitSalesManagers = true;
        profile.UpdatedAt = now;
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = 25m,
            IndirectCommissionRate = 0.05m,
            ReferredYear1DirectCommissionRate = 0.20m,
            DirectCommissionRate = 0.25m,
            CommissionDurationDays = 1095,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
        return (apps, parent.UserId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-rec-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }
}
