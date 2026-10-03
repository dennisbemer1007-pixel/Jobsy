using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class ShortCodeFormatTests
{
    [Theory]
    [InlineData("K7QM2P")]
    [InlineData("k7q-m2p")]
    [InlineData("K7Q-M2P")]
    [InlineData("234567")]
    [InlineData("ILO001")]
    [InlineData("ABCDEFG")]
    [InlineData("ABC")]
    [InlineData("")]
    [InlineData("AB CD")]
    public void Pupil_format_matches_shared_short_code_format(string input)
    {
        AssertSame(() => PupilCodeFormat.Normalize(input), () => ShortCodeFormat.Normalize(input));
        Assert.Equal(PupilCodeFormat.TryNormalize(input, out var pupil), ShortCodeFormat.TryNormalize(input, out var shared));
        Assert.Equal(pupil, shared);
        Assert.Equal(PupilCodeFormat.IsWellFormed(input), ShortCodeFormat.IsWellFormed(input));
        if (PupilCodeFormat.IsWellFormed(input))
        {
            Assert.Equal(PupilCodeFormat.Display(input), ShortCodeFormat.Display(input));
        }
    }

    private static void AssertSame(Func<string> left, Func<string> right)
    {
        string? a = null;
        string? b = null;
        Exception? ea = null;
        Exception? eb = null;
        try { a = left(); } catch (Exception ex) { ea = ex; }
        try { b = right(); } catch (Exception ex) { eb = ex; }
        Assert.Equal(ea?.GetType(), eb?.GetType());
        Assert.Equal(ea?.Message, eb?.Message);
        Assert.Equal(a, b);
    }
}

public class PassportPartnerServiceTests
{
    [Fact]
    public async Task Generated_codes_stay_inside_the_school_alphabet_and_round_trip()
    {
        await using var db = CreateDb();
        var (partner, company) = SeedPartner(db, maxBranches: 20);
        await db.SaveChangesAsync();
        var sut = CreateService(db, enabled: true);

        var forbidden = "ILO01";
        for (var i = 0; i < 12; i++)
        {
            var code = await sut.CreateCodeAsync(partner.Id, company.Id, null);
            var plain = code.CodeDisplay.Replace("-", "", StringComparison.Ordinal);
            Assert.Equal(6, plain.Length);
            Assert.DoesNotContain(plain, ch => forbidden.Contains(ch));
            Assert.Equal(ShortCodeFormat.Display(plain), code.CodeDisplay);
            var resolved = await sut.ResolveCodeAsync(code.CodeDisplay);
            Assert.NotNull(resolved);
            Assert.Equal(partner.DisplayName, resolved!.DisplayName);
            Assert.Equal("Uitzendbureau", resolved.Type);
        }
    }

    [Fact]
    public async Task Vanity_code_must_be_well_formed_and_hashes_for_lookup()
    {
        await using var db = CreateDb();
        var (partner, company) = SeedPartner(db, maxBranches: 2);
        await db.SaveChangesAsync();
        var sut = CreateService(db, enabled: true);

        var invalid = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateCodeAsync(partner.Id, company.Id, "LOBSY1"));
        Assert.Equal("vanity_invalid", invalid.Message);

        var code = await sut.CreateCodeAsync(partner.Id, company.Id, "K7QM2P");
        Assert.Equal("K7Q-M2P", code.CodeDisplay);
        Assert.Equal(64, code.CodeLookupHash.Length);
        var again = await sut.ResolveCodeAsync("k7q m2p");
        Assert.Equal(partner.Id, again!.PartnerId);
    }

    [Fact]
    public async Task GiveConsent_rejects_a_fourth_active_link_wrong_version_and_under_18()
    {
        await using var db = CreateDb();
        var candidate = SeedCandidate(db, new DateOnly(2000, 1, 1));
        var links = new List<PassportPartnerCandidateLink>();
        for (var i = 0; i < 4; i++)
        {
            var (partner, company) = SeedPartner(db, maxBranches: 1, name: $"Bureau {i}");
            var code = SeedCode(db, partner, company, new[] { "234567", "234568", "234569", "234578" }[i]);
            links.Add(new PassportPartnerCandidateLink
            {
                Id = Guid.NewGuid(),
                CandidateUserId = candidate.Id,
                PassportPartnerId = partner.Id,
                PartnerCodeId = code.Id,
                Source = PassportPartnerLinkSource.Code,
                StartedAtUtc = DateTime.UtcNow
            });
        }

        db.PassportPartnerCandidateLinks.AddRange(links);
        await db.SaveChangesAsync();
        var sut = CreateService(db, enabled: true);

        var wrong = await sut.GiveConsentAsync(candidate.Id, links[0].Id, "1999-01-01", false, true);
        Assert.Equal("Partners.Consent.Version", wrong.Error);

        for (var i = 0; i < 3; i++)
        {
            var ok = await sut.GiveConsentAsync(
                candidate.Id, links[i].Id, PrivacyConstants.PartnerShareConsentVersion, i == 0, true);
            Assert.True(ok.Ok);
        }

        var stamped = await db.PassportPartnerCandidateLinks.SingleAsync(l => l.Id == links[0].Id);
        Assert.Equal(PrivacyConstants.PartnerShareConsentVersion, stamped.ConsentVersion);
        Assert.NotNull(stamped.ContactConsentAtUtc);
        Assert.Equal(PassportPartnerRules.NextReconfirmDue(stamped.ConsentGivenAtUtc!.Value), stamped.ReconfirmDueAtUtc);

        var fourth = await sut.GiveConsentAsync(
            candidate.Id, links[3].Id, PrivacyConstants.PartnerShareConsentVersion, false, true);
        Assert.Equal("Partners.Consent.Max", fourth.Error);

        await sut.RevokeAsync(candidate.Id, links[2].Id, PassportPartnerRevokeReason.Candidate);
        var afterRevoke = await sut.GiveConsentAsync(
            candidate.Id, links[3].Id, PrivacyConstants.PartnerShareConsentVersion, false, true);
        Assert.True(afterRevoke.Ok);

        candidate.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17));
        await db.SaveChangesAsync();
        var (youngPartner, youngCompany) = SeedPartner(db, maxBranches: 1, name: "Jong");
        await db.SaveChangesAsync();
        var youngLink = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = youngPartner.Id,
            Source = PassportPartnerLinkSource.AddedCode,
            StartedAtUtc = DateTime.UtcNow
        };
        db.PassportPartnerCandidateLinks.Add(youngLink);
        await db.SaveChangesAsync();
        var tooYoung = await sut.GiveConsentAsync(
            candidate.Id, youngLink.Id, PrivacyConstants.PartnerShareConsentVersion, false, true);
        Assert.Equal("Partners.Consent.Age", tooYoung.Error);
        _ = youngCompany;
    }

    [Fact]
    public async Task Reconfirm_moves_the_due_date_by_six_months()
    {
        await using var db = CreateDb();
        var candidate = SeedCandidate(db, new DateOnly(1990, 5, 5));
        var (partner, _) = SeedPartner(db, maxBranches: 1);
        var link = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = partner.Id,
            Source = PassportPartnerLinkSource.Code,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-6),
            ConsentGivenAtUtc = DateTime.UtcNow.AddMonths(-6),
            ConsentVersion = PrivacyConstants.PartnerShareConsentVersion,
            ReconfirmDueAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        db.PassportPartnerCandidateLinks.Add(link);
        await db.SaveChangesAsync();
        var sut = CreateService(db, enabled: true);
        var before = DateTime.UtcNow;
        var result = await sut.ReconfirmAsync(candidate.Id, link.Id);
        Assert.True(result.Ok);
        var due = (await db.PassportPartnerCandidateLinks.SingleAsync(l => l.Id == link.Id)).ReconfirmDueAtUtc;
        Assert.NotNull(due);
        Assert.InRange(due!.Value, before.Add(PassportPartnerRules.ReconfirmInterval).AddSeconds(-2), DateTime.UtcNow.Add(PassportPartnerRules.ReconfirmInterval).AddSeconds(2));
    }

    [Fact]
    public async Task Consent_job_reminds_once_and_suspends_on_the_due_date()
    {
        await using var db = CreateDb();
        var candidate = SeedCandidate(db, new DateOnly(1992, 2, 2));
        var (partner, _) = SeedPartner(db, maxBranches: 1, name: "Herinnering");
        var (other, _) = SeedPartner(db, maxBranches: 1, name: "Te laat");
        var soon = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = partner.Id,
            Source = PassportPartnerLinkSource.Code,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-5),
            ConsentGivenAtUtc = DateTime.UtcNow.AddMonths(-5),
            ConsentVersion = PrivacyConstants.PartnerShareConsentVersion,
            ReconfirmDueAtUtc = DateTime.UtcNow.AddDays(10)
        };
        var overdue = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = other.Id,
            Source = PassportPartnerLinkSource.AddedCode,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-7),
            ConsentGivenAtUtc = DateTime.UtcNow.AddMonths(-7),
            ConsentVersion = PrivacyConstants.PartnerShareConsentVersion,
            ReconfirmDueAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        db.PassportPartnerCandidateLinks.AddRange(soon, overdue);
        var oldLog = new PassportAccessLog
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = other.Id,
            Kind = PassportAccessKind.PartnerPortalView,
            OccurredAtUtc = DateTime.UtcNow.AddDays(-400)
        };
        var freshLog = new PassportAccessLog
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            Kind = PassportAccessKind.VerificationView,
            OccurredAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        db.PassportAccessLogs.AddRange(oldLog, freshLog);
        await db.SaveChangesAsync();

        var mail = new CountingMailer();
        var job = new PassportPartnerConsentJob(db, mail, new FlagFeatures(true));
        var now = DateTime.UtcNow;
        await job.RunAsync(now);
        await job.RunAsync(now.AddMinutes(1));

        Assert.Equal(1, mail.Sent);
        Assert.NotNull((await db.PassportPartnerCandidateLinks.SingleAsync(l => l.Id == soon.Id)).ReconfirmReminderSentAtUtc);
        Assert.NotNull((await db.PassportPartnerCandidateLinks.SingleAsync(l => l.Id == overdue.Id)).SuspendedAtUtc);
        Assert.False(await db.PassportAccessLogs.AnyAsync(l => l.Id == oldLog.Id));
        Assert.True(await db.PassportAccessLogs.AnyAsync(l => l.Id == freshLog.Id));
    }

    [Fact]
    public async Task CanPartnerView_truth_table()
    {
        await using var db = CreateDb();
        var root = SeedCompany(db, "Root", CompanyType.Intermediary);
        var branch = SeedCompany(db, "Vestiging", CompanyType.Employer);
        branch.ParentCompanyId = root.Id;
        var other = SeedCompany(db, "Ander", CompanyType.Employer);
        var partner = new PassportPartner
        {
            Id = Guid.NewGuid(),
            CompanyId = root.Id,
            IsActive = true,
            DisplayName = "Root Flex",
            MaxBranches = 2,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.PassportPartners.Add(partner);
        var candidate = SeedCandidate(db, new DateOnly(1995, 3, 3));
        var link = new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = partner.Id,
            Source = PassportPartnerLinkSource.Code,
            StartedAtUtc = DateTime.UtcNow,
            ConsentGivenAtUtc = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.PartnerShareConsentVersion,
            ReconfirmDueAtUtc = DateTime.UtcNow.AddDays(180)
        };
        db.PassportPartnerCandidateLinks.Add(link);
        var branchUser = SeedStaff(db, UserRole.BranchManager, branch.Id);
        var outsider = SeedStaff(db, UserRole.BranchManager, other.Id);
        var candidateRole = SeedStaff(db, UserRole.Candidate, root.Id);
        await db.SaveChangesAsync();

        var on = CreateService(db, enabled: true);
        Assert.NotNull(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, false));
        Assert.Null(await on.CanPartnerViewAsync(outsider.Id, candidate.Id, true));
        Assert.Null(await on.CanPartnerViewAsync(candidateRole.Id, candidate.Id, true));

        var off = CreateService(db, enabled: false);
        Assert.Null(await off.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));

        partner.IsActive = false;
        await db.SaveChangesAsync();
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
        partner.IsActive = true;

        link.ConsentGivenAtUtc = null;
        await db.SaveChangesAsync();
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
        link.ConsentGivenAtUtc = DateTime.UtcNow;

        link.SuspendedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
        link.SuspendedAtUtc = null;

        link.RevokedAtUtc = DateTime.UtcNow;
        link.RevokedReason = PassportPartnerRevokeReason.Candidate;
        await db.SaveChangesAsync();
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
        link.RevokedAtUtc = null;
        link.RevokedReason = null;

        candidate.Email = $"deleted-{candidate.Id:N}@anonymized.local";
        await db.SaveChangesAsync();
        Assert.Null(await on.CanPartnerViewAsync(branchUser.Id, candidate.Id, true));
    }

    [Fact]
    public async Task Anonymize_deletes_links_and_logs_and_export_contains_them()
    {
        await using var db = CreateDb();
        var candidate = SeedCandidate(db, new DateOnly(1991, 1, 1));
        var (partner, _) = SeedPartner(db, maxBranches: 1, name: "Export Bureau");
        db.PassportPartnerCandidateLinks.Add(new PassportPartnerCandidateLink
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = partner.Id,
            Source = PassportPartnerLinkSource.Code,
            StartedAtUtc = DateTime.UtcNow,
            ConsentGivenAtUtc = DateTime.UtcNow,
            ConsentVersion = PrivacyConstants.PartnerShareConsentVersion
        });
        db.PassportAccessLogs.Add(new PassportAccessLog
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidate.Id,
            PassportPartnerId = partner.Id,
            Kind = PassportAccessKind.PartnerPdfDownload,
            OccurredAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, candidate.Email),
            new Claim(ClaimTypes.NameIdentifier, candidate.Id.ToString())
        ], "test"));
        var privacy = new PrivacyDataService(db, new EmailLookup(db), new NoMail(), new FlagFeatures(true));
        var export = await privacy.ExportAsync(principal);
        var json = System.Text.Json.JsonSerializer.Serialize(export);
        Assert.Contains("Export Bureau", json, StringComparison.Ordinal);
        Assert.Contains("PartnerPdfDownload", json, StringComparison.Ordinal);
        Assert.Contains("Uitzendbureau", json, StringComparison.Ordinal);

        await privacy.DeleteOrAnonymizeAsync(principal);
        Assert.Empty(await db.PassportPartnerCandidateLinks.Where(l => l.CandidateUserId == candidate.Id).ToListAsync());
        Assert.Empty(await db.PassportAccessLogs.Where(l => l.CandidateUserId == candidate.Id).ToListAsync());
    }

    [Fact]
    public void Disclosed_counts_hide_groups_under_five()
    {
        Assert.Equal("minder dan 5", PassportPartnerRules.FormatDisclosedCount(0));
        Assert.Equal("minder dan 5", PassportPartnerRules.FormatDisclosedCount(4));
        Assert.Equal("5", PassportPartnerRules.FormatDisclosedCount(5));
    }

    private static PassportPartnerService CreateService(JobsyDbContext db, bool enabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PassportPartners:CodeHmacKey"] = "test-passport-partner-hmac"
            })
            .Build();
        return new PassportPartnerService(db, new FlagFeatures(enabled), config, new DevEnv());
    }

    private static (PassportPartner Partner, Company Company) SeedPartner(JobsyDbContext db, int maxBranches, string name = "Flex Bureau")
    {
        var company = SeedCompany(db, name, CompanyType.Intermediary);
        var partner = new PassportPartner
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            IsActive = true,
            DisplayName = name,
            MaxBranches = maxBranches,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.PassportPartners.Add(partner);
        return (partner, company);
    }

    private static PassportPartnerCode SeedCode(JobsyDbContext db, PassportPartner partner, Company company, string plain)
    {
        var hash = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("test-passport-partner-hmac"),
            Encoding.UTF8.GetBytes(plain))).ToLowerInvariant();
        var code = new PassportPartnerCode
        {
            Id = Guid.NewGuid(),
            PassportPartnerId = partner.Id,
            BranchCompanyId = company.Id,
            CodeLookupHash = hash,
            CodeDisplay = ShortCodeFormat.Display(plain),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.PassportPartnerCodes.Add(code);
        return code;
    }

    private static Company SeedCompany(JobsyDbContext db, string name, CompanyType type)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = Random.Shared.Next(10000000, 99999999).ToString(),
            Address = "Straat 1",
            Location = new GeoPoint(52.1, 5.1),
            Type = type
        };
        db.Companies.Add(company);
        return company;
    }

    private static User SeedCandidate(JobsyDbContext db, DateOnly dob)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"cand-{Guid.NewGuid():N}@example.com",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            DateOfBirth = dob
        };
        db.Users.Add(user);
        return user;
    }

    private static User SeedStaff(JobsyDbContext db, UserRole role, Guid companyId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"staff-{Guid.NewGuid():N}@example.com",
            FullName = "Medewerker",
            Role = role,
            IsActive = true,
            CompanyId = companyId
        };
        db.Users.Add(user);
        return user;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FlagFeatures(bool enabled) : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, "https://lobsy.test", null, PassportPartnersEnabled: enabled));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class DevEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CountingMailer : ITransactionalMailer
    {
        public int Sent { get; private set; }

        public Task<EmailSendOutcome> SendAsync(
            Jobsy.Core.Email.ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Sent++;
            return Task.FromResult(new EmailSendOutcome(true, false, null));
        }
    }

    private sealed class NoMail : ITransactionalMailer
    {
        public Task<EmailSendOutcome> SendAsync(
            Jobsy.Core.Email.ComposedEmail mail,
            string to,
            EmailSendOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmailSendOutcome(true, false, null));
    }

    private sealed class EmailLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }
    }
}
