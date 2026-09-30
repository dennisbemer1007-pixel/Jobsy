using System.Reflection;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class SalesAttributionTests
{
    [Fact]
    public async Task Typed_code_beats_cookie()
    {
        await using var db = CreateDb();
        var typed = await SeedSalesManagerAsync(db, "SM-TYPED1", "typed@corp.nl", "11112222");
        var cookie = await SeedSalesManagerAsync(db, "SM-COOKI1", "cookie@corp.nl", "33334444");

        var resolver = CreateResolver(db);
        var result = await resolver.ResolveAtRegistrationAsync(
            "SM-TYPED1",
            "SM-COOKI1",
            "employer@other.nl",
            "55556666");

        Assert.True(result.IsAttributed);
        Assert.Equal(typed, result.BeneficiaryUserId);
        Assert.Equal(SalesAttributionSource.TypedCode, result.Source);
        Assert.Equal(cookie, cookie); // silence unused
    }

    [Fact]
    public async Task Cookie_attributes_when_no_typed_code()
    {
        await using var db = CreateDb();
        var sm = await SeedSalesManagerAsync(db, "SM-LINK01", "sm@link.nl", "12121212");
        var resolver = CreateResolver(db);

        var result = await resolver.ResolveAtRegistrationAsync(
            null,
            "SM-LINK01",
            "werkgever@firma.nl",
            "99887766");

        Assert.Equal(sm, result.BeneficiaryUserId);
        Assert.Equal(SalesAttributionSource.LinkCookie, result.Source);
    }

    [Fact]
    public async Task Self_referral_same_email_blocks_quietly()
    {
        await using var db = CreateDb();
        await SeedSalesManagerAsync(db, "SM-SELF01", "same@firma.nl", "12345678");
        var resolver = CreateResolver(db);

        var result = await resolver.ResolveAtRegistrationAsync(
            "SM-SELF01",
            null,
            "same@firma.nl",
            "87654321");

        Assert.False(result.IsAttributed);
        Assert.Equal(SalesSelfReferralRule.SameEmail, result.BlockedBy);
        Assert.Contains(
            await db.PlatformLogs.ToListAsync(),
            l => l.Category == "sales.attribution.self-referral-blocked");
    }

    [Fact]
    public async Task Self_referral_same_kvk_blocks()
    {
        await using var db = CreateDb();
        await SeedSalesManagerAsync(db, "SM-SELF02", "sm@firma.nl", "12345678");
        var resolver = CreateResolver(db);

        var result = await resolver.ResolveAtRegistrationAsync(
            "SM-SELF02",
            null,
            "other@firma.nl",
            "12345678");

        Assert.Equal(SalesSelfReferralRule.SameKvk, result.BlockedBy);
    }

    [Fact]
    public async Task Self_referral_same_corporate_domain_blocks_but_freemail_does_not()
    {
        await using var db = CreateDb();
        await SeedSalesManagerAsync(db, "SM-DOMN01", "a@acme.nl", "11110001");
        await SeedSalesManagerAsync(db, "SM-GMAIL1", "sm@gmail.com", "11110002");
        var resolver = CreateResolver(db);

        var corp = await resolver.ResolveAtRegistrationAsync(
            "SM-DOMN01", null, "b@acme.nl", "22220001");
        Assert.Equal(SalesSelfReferralRule.SameEmailDomain, corp.BlockedBy);

        var free = await resolver.ResolveAtRegistrationAsync(
            "SM-GMAIL1", null, "other@gmail.com", "22220002");
        Assert.True(free.IsAttributed);
        Assert.Equal(SalesSelfReferralRule.None, free.BlockedBy);
    }

    [Fact]
    public async Task Parked_ambassadeur_code_gives_no_attribution()
    {
        await using var db = CreateDb();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.NewGuid(),
            AmbassadorsEnabled = false
        });
        var amUser = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = amUser,
            Email = "am@lobsy.nl",
            FullName = "AM",
            Role = UserRole.Ambassadeur,
            IsActive = true
        });
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = amUser,
            TrackingCode = "AM-PARK01",
            OnboardingCompletedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var resolver = CreateResolver(db);
        Assert.Null(await resolver.ResolveActiveReferralAsync("AM-PARK01"));
        var atReg = await resolver.ResolveAtRegistrationAsync(
            "AM-PARK01", "AM-PARK01", "x@y.nl", "12345678");
        Assert.False(atReg.IsAttributed);
    }

    [Fact]
    public async Task Registration_submit_stores_cookie_source_and_code()
    {
        await using var db = CreateDb();
        var sm = await SeedSalesManagerAsync(db, "SM-REG001", "sm@sales.nl", "10101010");
        SeedCommercial(db);
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());
        var registration = new CompanyRegistrationService(
            db,
            new StubKvk(),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
            new PartnerAffiliateService(db, new TokenLedgerService(db), features),
            CreateResolver(db),
            NullLogger<CompanyRegistrationService>.Instance);

        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            KvkNumber: "12345678",
            KvkEstablishmentId: "12345678_0001",
            Scope: RegistrationScope.BranchOnly,
            ContactName: "Jan Werkgever",
            ContactEmail: "jan@werkgever.nl",
            ContactPhone: null,
            AcceptedTerms: true,
            Password: "Test1234!Abcd",
            CookieTrackingCode: "SM-REG001"));

        var pending = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.Equal("SM-REG001", pending.SalesManagerTrackingCode);
        Assert.Equal(SalesAttributionSource.LinkCookie, pending.SalesAttributionSource);
        Assert.NotEqual(Guid.Empty, sm);
    }

    [Fact]
    public async Task Click_counter_upserts_per_day_channel_and_stores_only_four_columns()
    {
        await using var db = CreateDb();
        var sm = Guid.NewGuid();
        var clicks = new SalesLinkClickService(db);
        var day = SalesClock.Today();

        await clicks.RecordClickAsync(sm, SalesLinkChannel.Qr, day);
        await clicks.RecordClickAsync(sm, SalesLinkChannel.Qr, day);
        await clicks.RecordClickAsync(sm, SalesLinkChannel.Flyer, day);

        var rows = await db.SalesLinkClickDailies.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Single(r => r.Channel == SalesLinkChannel.Qr).Count);
        Assert.Equal(1, rows.Single(r => r.Channel == SalesLinkChannel.Flyer).Count);

        var props = typeof(SalesLinkClickDaily)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(
            new[] { nameof(SalesLinkClickDaily.BeneficiaryUserId), nameof(SalesLinkClickDaily.Channel), nameof(SalesLinkClickDaily.Count), nameof(SalesLinkClickDaily.Date) }.OrderBy(n => n),
            props);
    }

    [Fact]
    public async Task Retention_deletes_clicks_older_than_25_months()
    {
        await using var db = CreateDb();
        var sm = Guid.NewGuid();
        var old = SalesClock.Today().AddMonths(-26);
        var keep = SalesClock.Today().AddMonths(-12);
        db.SalesLinkClickDailies.AddRange(
            new SalesLinkClickDaily { BeneficiaryUserId = sm, Date = old, Channel = SalesLinkChannel.Link, Count = 3 },
            new SalesLinkClickDaily { BeneficiaryUserId = sm, Date = keep, Channel = SalesLinkChannel.Link, Count = 1 });
        await db.SaveChangesAsync();

        var cutoff = SalesClock.Today().AddMonths(-PrivacyConstants.SalesLinkClickRetentionMonths);
        var stale = await db.SalesLinkClickDailies.Where(c => c.Date < cutoff).ToListAsync();
        db.SalesLinkClickDailies.RemoveRange(stale);
        await db.SaveChangesAsync();
        Assert.Equal(1, stale.Count);
        Assert.Equal(1, await db.SalesLinkClickDailies.CountAsync());
    }

    [Fact]
    public async Task Reassign_updates_root_and_vestigingen_writes_history_future_only()
    {
        await using var db = CreateDb();
        var smA = await SeedSalesManagerAsync(db, "SM-AAA001", "a@sm.nl", "11111111");
        var smB = await SeedSalesManagerAsync(db, "SM-BBB001", "b@sm.nl", "22222222", referredBy: smA);
        SeedCommercial(db);

        var rootId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var paidAt = DateTime.UtcNow.AddDays(-10);
        db.Companies.Add(new Company
        {
            Id = rootId,
            Name = "Root BV",
            KvkNumber = "55555555",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 5),
            ReferredBySalesManagerUserId = smA,
            SalesAttributedAtUtc = paidAt.AddDays(-20),
            SalesAttributionSource = SalesAttributionSource.TypedCode,
            CommissionStartsAtUtc = paidAt,
            CommissionDirectRateSnapshot = 0.25m,
            CommissionYear2RateSnapshot = 0.10m,
            CommissionYear3RateSnapshot = 0.05m,
            CommissionIndirectRateSnapshot = 0m,
            CommissionDurationDaysSnapshot = 1095,
            CommissionTermsSnapshottedAtUtc = paidAt
        });
        db.Companies.Add(new Company
        {
            Id = branchId,
            Name = "Vestiging",
            KvkNumber = "55555555",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52, 5),
            ParentCompanyId = rootId,
            ReferredBySalesManagerUserId = smA,
            SalesAttributedAtUtc = paidAt.AddDays(-20),
            SalesAttributionSource = SalesAttributionSource.TypedCode,
            CommissionStartsAtUtc = paidAt,
            CommissionDirectRateSnapshot = 0.25m,
            CommissionYear2RateSnapshot = 0.10m,
            CommissionYear3RateSnapshot = 0.05m
        });
        var oldEntry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = smA,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 100m,
            CompanyId = rootId,
            CreatedAt = paidAt,
            AvailableFromUtc = paidAt.AddDays(14)
        };
        db.CommissionLedgerEntries.Add(oldEntry);
        await db.SaveChangesAsync();

        var admin = new SalesAttributionAdminService(db);
        var change = await admin.ReassignAsync(branchId, smB, "Verkeerde toewijzing hersteld", Guid.NewGuid());

        var root = await db.Companies.SingleAsync(c => c.Id == rootId);
        var branch = await db.Companies.SingleAsync(c => c.Id == branchId);
        Assert.Equal(smB, root.ReferredBySalesManagerUserId);
        Assert.Equal(smB, branch.ReferredBySalesManagerUserId);
        Assert.Equal(SalesAttributionSource.Admin, root.SalesAttributionSource);
        Assert.Equal(smA, root.CommissionIndirectSalesManagerUserId); // upline of B
        Assert.Equal(rootId, change.CompanyId);
        Assert.Equal(100m, oldEntry.AmountExVat); // past commission unchanged

        var history = await admin.GetHistoryAsync(branchId);
        Assert.Single(history);
        Assert.Contains(await db.PlatformLogs.ToListAsync(), l => l.Category == "sales.attribution.reassign");
    }

    [Fact]
    public async Task Partner_bm_code_resolves_as_partner_kind()
    {
        await using var db = CreateDb();
        var partnerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = partnerId,
            Email = "bm@partner.nl",
            FullName = "BM",
            Role = UserRole.EnterpriseManager,
            IsActive = true
        });
        db.PartnerAffiliateProfiles.Add(new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = partnerId,
            TrackingCode = "BM-PART01",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var active = await CreateResolver(db).ResolveActiveReferralAsync("BM-PART01");
        Assert.NotNull(active);
        Assert.Equal(SalesResolvedCodeKind.Partner, active!.Kind);
        Assert.Equal(partnerId, active.BeneficiaryUserId);
    }

    [Fact]
    public void Freemail_list_covers_common_nl_providers()
    {
        Assert.True(FreemailDomains.IsFreemail("gmail.com"));
        Assert.True(FreemailDomains.IsFreemail("ziggo.nl"));
        Assert.False(FreemailDomains.IsFreemail("acme.nl"));
    }

    [Theory]
    [InlineData("Besloten Vennootschap", CompanyLegalForm.Bv)]
    [InlineData("Eenmanszaak", CompanyLegalForm.Eenmanszaak)]
    [InlineData("Vennootschap onder firma", CompanyLegalForm.Vof)]
    public void LegalForm_mapper_recognises_common_rechtsvormen(string raw, CompanyLegalForm expected)
        => Assert.Equal(expected, CompanyLegalFormMapper.FromKvk(raw));

    [Fact]
    public void Channel_from_query_maps_qr_flyer_link()
    {
        Assert.Equal(SalesLinkChannel.Qr, SalesTrackingCodes.ChannelFromQuery("qr"));
        Assert.Equal(SalesLinkChannel.Flyer, SalesTrackingCodes.ChannelFromQuery("flyer"));
        Assert.Equal(SalesLinkChannel.Link, SalesTrackingCodes.ChannelFromQuery("link"));
        Assert.Equal(SalesLinkChannel.Other, SalesTrackingCodes.ChannelFromQuery("x"));
    }

    private static SalesAttributionResolver CreateResolver(JobsyDbContext db)
    {
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());
        return new SalesAttributionResolver(db, features, NullLogger<SalesAttributionResolver>.Instance);
    }

    private static async Task<Guid> SeedSalesManagerAsync(
        JobsyDbContext db,
        string code,
        string email,
        string kvk,
        Guid? referredBy = null)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            Email = email,
            FullName = "SM " + code,
            Role = UserRole.SalesManager,
            IsActive = true
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = id,
            TrackingCode = code,
            KvkNumber = kvk,
            OnboardingCompletedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            ReferredBySalesManagerUserId = referredBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static void SeedCommercial(JobsyDbContext db)
    {
        if (db.SalesCommercialSettings.Any())
        {
            return;
        }

        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = Guid.NewGuid(),
            AttributionCookieDays = 30,
            CommissionHoldDays = 14,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-attr-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class StubKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(new KvkCompanyResult(kvkNumber, "Test BV", "Straat 1", LegalForm: CompanyLegalForm.Bv));

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<KvkEstablishmentResult>)LookupEstablishmentsAsync(kvkNumber, cancellationToken).Result.Establishments);

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Ok(
            [
                new KvkEstablishmentResult(
                    kvkNumber, "0001", kvkNumber + "_0001", "Test BV", "Straat 1", 52, 5, false)
            ]));
    }
}
