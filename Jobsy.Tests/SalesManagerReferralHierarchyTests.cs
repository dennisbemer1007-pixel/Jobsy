using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class SalesManagerReferralHierarchyTests
{
    [Fact]
    public async Task Admin_invite_creates_recruiting_salesmanager()
    {
        await using var db = CreateDb();
        var invite = CreateInvite(db);

        var result = await invite.InviteAsync("tier0@jobsy.local", "Tier Zero");
        var profile = await db.SalesManagerProfiles.SingleAsync(p => p.UserId == result.UserId);

        Assert.True(result.CanRecruitSalesManagers);
        Assert.Null(result.ReferredBySalesManagerUserId);
        Assert.True(profile.CanRecruitSalesManagers);
        Assert.Null(profile.ReferredBySalesManagerUserId);
    }

    [Fact]
    public async Task Referred_salesmanager_cannot_recruit_and_application_is_blocked()
    {
        await using var db = CreateDb();
        var invite = CreateInvite(db);
        var apps = CreateApplications(db, invite);

        var parent = await invite.InviteAsync("parent@jobsy.local", "Parent SM");
        CompleteOnboarding(db, parent.UserId, "SM-PARENT");

        var child = await invite.InviteAsync(
            "child@jobsy.local", "Child SM", referredBySalesManagerUserId: parent.UserId);
        Assert.False(child.CanRecruitSalesManagers);
        Assert.Equal(parent.UserId, child.ReferredBySalesManagerUserId);

        CompleteOnboarding(db, child.UserId, "SM-CHILD1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            apps.SubmitAsync(child.UserId, "newbie@jobsy.local", "Newbie", "Motivatie lang genoeg."));
    }

    [Fact]
    public async Task Application_requires_admin_approval_before_provisioning()
    {
        await using var db = CreateDb();
        var invite = CreateInvite(db);
        var apps = CreateApplications(db, invite);
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = adminId,
            Email = "admin@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var parent = await invite.InviteAsync("recruiter@jobsy.local", "Recruiter");
        CompleteOnboarding(db, parent.UserId, "SM-RECR01");

        var pending = await apps.SubmitAsync(
            parent.UserId, "candidate@jobsy.local", "Candidate SM", "Sterke netwerk in Westland.");
        Assert.Equal(nameof(SalesManagerApplicationStatus.Pending), pending.Status);
        Assert.Null(await db.Users.FirstOrDefaultAsync(u => u.Email == "candidate@jobsy.local"));

        var approved = await apps.ApproveAsync(pending.Id, adminId);
        Assert.Equal(nameof(SalesManagerApplicationStatus.Approved), approved.Status);
        Assert.NotNull(approved.ProvisionedUserId);
        Assert.False(string.IsNullOrWhiteSpace(approved.TemporaryPassword));

        var profile = await db.SalesManagerProfiles.SingleAsync(p => p.UserId == approved.ProvisionedUserId);
        Assert.False(profile.CanRecruitSalesManagers);
        Assert.Equal(parent.UserId, profile.ReferredBySalesManagerUserId);
    }

    [Fact]
    public async Task Indirect_commission_credits_referring_salesmanager_within_year()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);

        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Users.AddRange(
            new User { Id = parentId, Email = "p@t.local", FullName = "P", Role = UserRole.SalesManager, IsActive = true },
            new User { Id = childId, Email = "c@t.local", FullName = "C", Role = UserRole.SalesManager, IsActive = true });
        db.SalesManagerProfiles.AddRange(
            new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = parentId,
                TrackingCode = "SM-PAR001",
                CanRecruitSalesManagers = true,
                AgreementSignedAt = now,
                OnboardingCompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = childId,
                TrackingCode = "SM-CHD001",
                CanRecruitSalesManagers = false,
                ReferredBySalesManagerUserId = parentId,
                AgreementSignedAt = now,
                OnboardingCompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "12341234",
            KvkEstablishmentId = "12341234_0001",
            Address = "A",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4),
            ReferredBySalesManagerUserId = childId,
            FirstYearStartedAt = now.AddMonths(-2)
        });
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commissions = new CommissionLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var share = new RevenueShareService(db, tokens, commissions, commercial);

        var checkoutId = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId, companyId, null, packSize: 10, purchaseAmountExVatEuro: 100m,
            childId, now.AddMonths(-2));

        Assert.Equal(15.00m, await commissions.GetBalanceExVatAsync(childId));
        Assert.Equal(3.00m, await commissions.GetBalanceExVatAsync(parentId));

        var logs = await db.RevenueShareLogs.Where(l => l.TokenCheckoutId == checkoutId).ToListAsync();
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.SalesManager && l.AmountEuro == 15m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.IndirectSalesManager && l.AmountEuro == 3m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.Platform && l.AmountEuro == 67m);
    }

    [Fact]
    public async Task Commission_stops_after_one_year_window()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);

        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var started = DateTime.UtcNow.AddYears(-1).AddDays(-1);
        db.Users.Add(new User
        {
            Id = smId, Email = "sm@t.local", FullName = "SM", Role = UserRole.SalesManager, IsActive = true
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            TrackingCode = "SM-OLD001",
            CanRecruitSalesManagers = true,
            CreatedAt = started,
            UpdatedAt = started,
            AgreementSignedAt = started,
            OnboardingCompletedAt = started
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Old Buyer",
            KvkNumber = "99998888",
            KvkEstablishmentId = "99998888_0001",
            Address = "A",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4),
            ReferredBySalesManagerUserId = smId,
            FirstYearStartedAt = started
        });
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commissions = new CommissionLedgerService(db);
        var share = new RevenueShareService(db, tokens, commissions, new SalesCommercialService(db, tokens));

        await share.ApplyTokenPurchaseShareAsync(
            Guid.NewGuid(), companyId, null, 10, 100m, smId, started);

        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(smId));
        Assert.Equal(1.5m, await tokens.GetBalanceAsync(companyId)); // ambassador still applies
    }

    private static void CompleteOnboarding(JobsyDbContext db, Guid userId, string code)
    {
        var profile = db.SalesManagerProfiles.Single(p => p.UserId == userId);
        var now = DateTime.UtcNow;
        profile.CompanyName = "SM BV";
        profile.KvkNumber = "12345678";
        profile.VatNumber = "NL123456789B01";
        profile.Address = "Straat 1";
        profile.PostalCode = "1234AB";
        profile.City = "Delft";
        profile.Country = "NL";
        profile.TrackingCode = code;
        profile.AgreementSignedAt = now;
        profile.AgreementVersion = SalesCommissionRules.CurrentAgreementVersion;
        profile.OnboardingCompletedAt = now;
        profile.UpdatedAt = now;
        db.SaveChanges();
    }

    private static void SeedCommercialSettings(JobsyDbContext db)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = 25m,
            HighlightCarouselTokens = 2m,
            HighlightPulseTokens = 1m,
            HighlightCarouselDays = 7,
            StartHighlightBonusTokens = 2m,
            DirectCommissionRate = SalesCommissionRules.DefaultDirectCommissionRate,
            IndirectCommissionRate = SalesCommissionRules.DefaultIndirectCommissionRate,
            CommissionDurationDays = SalesCommissionRules.DefaultCommissionDurationDays,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static ISalesManagerInviteService CreateInvite(JobsyDbContext db) =>
        new SalesManagerInviteService(
            db,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            NullLogger<SalesManagerInviteService>.Instance);

    private static ISalesManagerApplicationService CreateApplications(
        JobsyDbContext db, ISalesManagerInviteService invite) =>
        new SalesManagerApplicationService(db, invite, NullLogger<SalesManagerApplicationService>.Instance);

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
