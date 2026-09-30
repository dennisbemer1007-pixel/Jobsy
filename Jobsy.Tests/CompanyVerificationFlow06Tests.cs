using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.Letters;
using Jobsy.Infrastructure.Services.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class DomainMatch06Tests
{
    [Theory]
    [InlineData("j.devries@groenenzorg.nl", "https://www.groenenzorg.nl/contact", true)]
    [InlineData("hr@mail.groenenzorg.nl", "https://www.groenenzorg.nl", true)]
    [InlineData("x@groenenzorg.co.uk", "https://groenenzorg.nl", false)]
    [InlineData("info@bakkerij.amsterdam", "https://bakkerij.amsterdam", true)]
    [InlineData("loket@gemeente.denhaag.nl", "https://www.denhaag.nl", true)]
    [InlineData("someone@gmail.com", "https://gmail.com", false)]
    public void DomainMatch_matches_spec_cases(string email, string site, bool expected)
        => Assert.Equal(expected, DomainMatch.EmailMatchesWebsite(email, site));

    [Fact]
    public void Matches_by_email_domain_list()
    {
        Assert.True(DomainMatch.Matches("mail.groenenzorg.nl", ["https://www.groenenzorg.nl/contact"]));
        Assert.False(DomainMatch.Matches("gmail.com", ["https://gmail.com"]));
    }
}

public class FreeMailDomains06Tests
{
    [Theory]
    [InlineData("gmail.com", true)]
    [InlineData("user@outlook.com", true)]
    [InlineData("ziggo.nl", true)]
    [InlineData("proton.me", true)]
    [InlineData("mailinator.com", true)]
    [InlineData("groenenzorg.nl", false)]
    public void FreeMailDomains_blocklist(string value, bool expected)
        => Assert.Equal(expected, FreeMailDomains.IsFreeMail(value));
}

public class LetterVerificationCodesTests
{
    [Fact]
    public void Create_format_normalize_roundtrip()
    {
        var code = LetterVerificationCodes.Create();
        Assert.Equal(8, code.Length);
        Assert.All(code, c => Assert.Contains(c, LetterVerificationCodes.Alphabet));
        var formatted = LetterVerificationCodes.Format(code);
        Assert.Matches(@"^[2-9A-HJKMNP-TV-Z]{4}-[2-9A-HJKMNP-TV-Z]{4}$", formatted);
        Assert.Equal(code, LetterVerificationCodes.Normalize(formatted.ToLowerInvariant()));
        Assert.Equal(code, LetterVerificationCodes.Normalize(code[..4] + " " + code[4..]));
        Assert.True(LetterVerificationCodes.IsWellFormed($" {formatted.ToLowerInvariant()} "));
    }
}

public class CompanyVerificationFlow06Tests
{
    [Fact]
    public async Task Email_mismatch_returns_domain_mismatch()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedUnverifiedAsync(db, "90123456", "mgr@other.example");
        var sut = CreateFlow(db);
        var result = await sut.StartEmailAsync(user.Id, "mgr@other.example");
        Assert.False(result.Ok);
        Assert.Equal("domain_mismatch", result.ErrorCode);
    }

    [Fact]
    public async Task Email_code_is_hashed_and_confirm_verifies()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "j.devries@groenenzorg.nl");
        var email = new CapturingEmail();
        var sut = CreateFlow(db, email: email);
        var start = await sut.StartEmailAsync(user.Id, "j.devries@groenenzorg.nl");
        Assert.True(start.Ok);

        var challenge = await db.CompanyVerificationEmailChallenges.SingleAsync();
        Assert.Equal(VerificationCodes.HashLength, challenge.CodeHash.Length);
        Assert.DoesNotContain('@', challenge.CodeHash);

        var plain = email.LastOtp!;
        Assert.Equal(6, plain.Length);
        Assert.False(string.Equals(challenge.CodeHash, plain, StringComparison.Ordinal));

        var confirm = await sut.ConfirmEmailAsync(user.Id, plain);
        Assert.True(confirm.Ok);
        await db.Entry(root).ReloadAsync();
        Assert.Equal(CompanyVerificationStatus.Verified, root.VerificationStatus);
        Assert.Equal(CompanyVerificationMethod.BusinessEmail, root.VerificationMethod);
    }

    [Fact]
    public async Task Email_five_wrong_codes_dead_then_cooldown()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedUnverifiedAsync(db, "90123456", "j.devries@groenenzorg.nl");
        var sut = CreateFlow(db);
        Assert.True((await sut.StartEmailAsync(user.Id, "j.devries@groenenzorg.nl")).Ok);

        VerificationConfirmResult? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await sut.ConfirmEmailAsync(user.Id, "000000");
        }

        Assert.False(last!.Ok);
        Assert.Equal("dead", last.ErrorCode);
        var again = await sut.StartEmailAsync(user.Id, "j.devries@groenenzorg.nl");
        Assert.False(again.Ok);
        Assert.Equal("locked", again.ErrorCode);
    }

    [Fact]
    public async Task Letter_stub_sends_and_confirm_verifies()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        var store = new StubLetterStore();
        var letters = new StubLetterService(store, NullLogger<StubLetterService>.Instance);
        var sut = CreateFlow(db, letters: letters);
        var start = await sut.RequestLetterAsync(user.Id, isResend: false);
        Assert.True(start.Ok);

        var letter = await db.CompanyVerificationLetters.SingleAsync();
        Assert.Equal(VerificationCodes.HashLength, letter.CodeHash.Length);
        Assert.Equal(LetterProviderKind.Stub, letter.Provider);
        Assert.True(store.TryGet(letter.ProviderLetterId!, out var pdf, out _));
        Assert.True(pdf!.Length > 100);

        var known = "K7Q2M9PX";
        letter.CodeHash = VerificationCodes.Hash(known);
        await db.SaveChangesAsync();

        var confirm = await sut.ConfirmLetterAsync(user.Id, "k7q2-m9px");
        Assert.True(confirm.Ok);
        await db.Entry(root).ReloadAsync();
        Assert.Equal(CompanyVerificationStatus.Verified, root.VerificationStatus);
        Assert.Equal(CompanyVerificationMethod.Letter, root.VerificationMethod);
    }

    [Fact]
    public async Task Letter_one_per_kvk_and_resend_rules()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        var sut = CreateFlow(db);
        Assert.True((await sut.RequestLetterAsync(user.Id, false)).Ok);
        var second = await sut.RequestLetterAsync(user.Id, false);
        Assert.False(second.Ok);
        Assert.Equal("kvk_limit", second.ErrorCode);

        var earlyResend = await sut.RequestLetterAsync(user.Id, true);
        Assert.False(earlyResend.Ok);
        Assert.Equal("resend_too_early", earlyResend.ErrorCode);

        var letter = await db.CompanyVerificationLetters.SingleAsync();
        letter.SentAtUtc = DateTime.UtcNow.AddDays(-8);
        await db.SaveChangesAsync();
        Assert.True((await sut.RequestLetterAsync(user.Id, true)).Ok);
        await db.Entry(letter).ReloadAsync();
        Assert.Equal(CompanyVerificationLetterStatus.Expired, letter.Status);
        Assert.Equal(string.Empty, letter.CodeHash);
    }

    [Fact]
    public async Task Letter_five_wrong_blocks_queue_item()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        var store = new StubLetterStore();
        var sut = CreateFlow(db, letters: new StubLetterService(store, NullLogger<StubLetterService>.Instance));
        Assert.True((await sut.RequestLetterAsync(user.Id, false)).Ok);

        VerificationConfirmResult? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await sut.ConfirmLetterAsync(user.Id, "AAAA-AAAA");
        }

        Assert.Equal("blocked", last!.ErrorCode);
        var admin = CreateAdmin(db, store);
        var queue = await admin.ListQueueAsync("brieven");
        Assert.Contains(queue, q => q.CompanyId == root.Id);
    }

    [Fact]
    public async Task Manual_approve_writes_audit()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@jobsy.local",
            FullName = "Admin",
            Role = UserRole.Admin,
            IsActive = true
        };
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var flow = CreateFlow(db);
        Assert.True((await flow.RequestManualAsync(user.Id, "Geen website", "help", null)).Ok);

        var admin = CreateAdmin(db, new StubLetterStore());
        await admin.ApproveAsync(root.Id, adminUser.Id, "ok");
        await db.Entry(root).ReloadAsync();
        Assert.Equal(CompanyVerificationStatus.Verified, root.VerificationStatus);
        Assert.Equal(CompanyVerificationMethod.Manual, root.VerificationMethod);
        Assert.True(await db.CompanyVerificationDecisions.AnyAsync(d =>
            d.CompanyId == root.Id && d.Outcome == CompanyVerificationStatus.Verified));
        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Category == "admin.company_verification.approve"));
    }

    [Fact]
    public async Task Three_rejections_auto_flag()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        for (var i = 0; i < 3; i++)
        {
            db.CompanyVerificationDecisions.Add(new CompanyVerificationDecision
            {
                Id = Guid.NewGuid(),
                CompanyId = root.Id,
                KvkNumber = root.KvkNumber,
                AdminUserId = user.Id,
                Outcome = CompanyVerificationStatus.Rejected,
                Method = CompanyVerificationMethod.Manual,
                Reason = $"r{i}",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        await db.SaveChangesAsync();
        var flagged = await CreateAdmin(db, new StubLetterStore()).ListQueueAsync("gemarkeerd");
        Assert.Contains(flagged, f => f.CompanyId == root.Id && f.Heuristics.Contains("kvk_3_rejections"));
    }

    [Fact]
    public async Task Monthly_cap_blocks_letter()
    {
        await using var db = CreateDb();
        var (user, root) = await SeedUnverifiedAsync(db, "90123456", "person@gmail.com");
        db.CompanyVerificationLetters.Add(new CompanyVerificationLetter
        {
            Id = Guid.NewGuid(),
            CompanyId = root.Id,
            KvkNumber = "00000001",
            RequestedByUserId = user.Id,
            CodeHash = VerificationCodes.Hash("ABCDEFGH"),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            AddressLine1 = "x",
            PostalCode = "1000 AA",
            City = "A",
            Country = "NL",
            Provider = LetterProviderKind.Stub,
            Status = CompanyVerificationLetterStatus.Sent,
            CreatedAtUtc = DateTime.UtcNow,
            SentAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateFlow(db, settings: Options.Create(new CompanyVerificationSettings { MonthlyLetterCap = 1 }));
        var options = await sut.GetOptionsAsync(user.Id);
        Assert.False(options.LetterAvailable);
        Assert.Equal("monthly_cap", options.LetterUnavailableReason);
    }

    [Fact]
    public void Letter_pdf_smoke_renders_nonzero_bytes()
    {
        var pdf = VerificationLetterPdfBuilder.Build(
            "Groen & Zorg",
            "Jasmijn de Vries",
            "Bedrijfsmanager",
            ["Groen & Zorg", "Vondellaan 12"],
            "3521 GE",
            "Utrecht",
            "K7Q2-M9PX",
            DateTime.Today.AddDays(30),
            "support@lobsy.nl");
        Assert.True(pdf.Length > 500);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.AsSpan(0, 4)));
    }

    [Fact]
    public async Task Pingen_fake_handler_auth_upload_create()
    {
        var handler = new FakePingenHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api-staging.pingen.com/") };
        var sut = new PingenLetterService(
            new FixedHttpClientFactory(client),
            Options.Create(new CompanyVerificationSettings
            {
                LetterProvider = LetterProviderKind.Pingen,
                PingenClientId = "id",
                PingenClientSecret = "secret",
                PingenOrganisationId = "org-1",
                PingenEnvironment = "Staging"
            }),
            NullLogger<PingenLetterService>.Instance);
        var result = await sut.SendAsync(new LetterRequest(
            "Co", ["Co", "Street 1"], "1000 AA", "City", "NL", [1, 2, 3, 4], "ref-1"));
        Assert.True(result.Ok);
        Assert.Equal("letter-1", result.ProviderLetterId);
        Assert.Contains(handler.Paths, p => p.Contains("access-tokens", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, p => p.Contains("file-upload", StringComparison.Ordinal));
        Assert.Contains(handler.Paths, p => p.Contains("/letters", StringComparison.Ordinal));
    }

    [Fact]
    public void Address_prefers_postal_over_visiting()
    {
        var root = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Co",
            KvkNumber = "90123456",
            Address = "fallback",
            Location = new GeoPoint(52, 5),
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            KvkEstablishmentId = "90123456_0001"
        };
        var profile = new KvkCompanyProfile(
            KvkLookupStatus.Ok,
            "90123456",
            "Co",
            "x",
            null,
            [],
            ["https://groenenzorg.nl"],
            [
                new KvkEstablishmentProfile(
                    "90123456",
                    "0001",
                    "90123456_0001",
                    "Co",
                    "Vondellaan 12, 3521 GE Utrecht",
                    52, 5, false, [],
                    new KvkAddressLine("Bezoekstraat", "1", null, "1111 AA", "Utrecht"),
                    new KvkAddressLine("Vondellaan", "12", null, "3521 GE", "Utrecht"))
            ]);
        var address = CompanyVerificationFlowService.ResolveLetterAddress(root, profile);
        Assert.NotNull(address);
        Assert.Equal("3521 GE", address!.PostalCode);
        Assert.Contains(address.AddressLines, l => l.Contains("Vondellaan", StringComparison.Ordinal));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new JobsyDbContext(options);
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.NewGuid(),
            PublicWebBaseUrl = "https://lobsy.nl",
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
        return db;
    }

    private static async Task<(User User, Company Root)> SeedUnverifiedAsync(
        JobsyDbContext db, string kvk, string email)
    {
        var root = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Groen & Zorg Thuiszorg B.V.",
            KvkNumber = kvk,
            KvkEstablishmentId = $"{kvk}_0001",
            Address = "Vondellaan 12, 3521 GE Utrecht",
            Location = new GeoPoint(52.09, 5.12),
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            Type = CompanyType.Employer
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Jasmijn de Vries",
            Role = UserRole.EnterpriseManager,
            CompanyId = root.Id,
            IsActive = true
        };
        db.Companies.Add(root);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user, root);
    }

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PublicWebBaseUrl"] = "https://lobsy.nl"
                })
                .Build());

    private static CompanyVerificationService CreateVerification(JobsyDbContext db, IEmailService email)
    {
        var features = CreateFeatures(db);
        var ledger = new TokenLedgerService(db);
        var registration = new CompanyRegistrationService(
            db,
            new GreenKvk(),
            email,
            ledger,
            features,
            NullLogger<CompanyRegistrationService>.Instance);
        var products = new VacancyProductService(
            db,
            ledger,
            new SalesCommercialService(db, ledger),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            email,
            features,
            new MockRoutingService(),
            new UserNotificationService(db),
            new CandidateActionTokenService(db),
            NullLogger<VacancyProductService>.Instance);
        return new CompanyVerificationService(
            db,
            registration,
            products,
            discovery: null,
            email,
            new UserNotificationService(db),
            features,
            NullLogger<CompanyVerificationService>.Instance);
    }

    private static CompanyVerificationFlowService CreateFlow(
        JobsyDbContext db,
        ILetterService? letters = null,
        IEmailService? email = null,
        IOptions<CompanyVerificationSettings>? settings = null)
    {
        email ??= new CapturingEmail();
        return new CompanyVerificationFlowService(
            db,
            CreateVerification(db, email),
            new GreenKvk(),
            letters ?? new StubLetterService(new StubLetterStore(), NullLogger<StubLetterService>.Instance),
            email,
            settings ?? Options.Create(new CompanyVerificationSettings()),
            new NoopBrand(),
            NullLogger<CompanyVerificationFlowService>.Instance);
    }

    private static CompanyVerificationAdminService CreateAdmin(JobsyDbContext db, IStubLetterStore store)
    {
        var email = new CapturingEmail();
        return new CompanyVerificationAdminService(
            db,
            CreateVerification(db, email),
            new GreenKvk(),
            store,
            email,
            NullLogger<CompanyVerificationAdminService>.Instance);
    }

    private sealed class CapturingEmail : IEmailService
    {
        public List<EmailMessage> Sent { get; } = [];
        public string? LastOtp { get; private set; }

        public Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);
            var html = message.BodyHtml ?? "";
            const string marker = "data-lobsy-otp=\"";
            var idx = html.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = idx + marker.Length;
                var end = html.IndexOf('"', start);
                if (end > start)
                {
                    LastOtp = html[start..end];
                }
            }

            return Task.FromResult(EmailDeliveryResult.Stub);
        }
    }

    private sealed class GreenKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<KvkEstablishmentResult>>([]);

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(new KvkEstablishmentsLookup(KvkLookupStatus.Ok, []));

        public Task<KvkSearchResult> SearchAsync(KvkSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkSearchResult.Ok([], 0));

        public Task<KvkCompanyProfile> GetProfileAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(new KvkCompanyProfile(
                KvkLookupStatus.Ok,
                kvkNumber,
                "Groen & Zorg Thuiszorg B.V.",
                "Vondellaan 12, 3521 GE Utrecht",
                "BV",
                ["88101"],
                ["https://www.groenenzorg.nl"],
                [
                    new KvkEstablishmentProfile(
                        kvkNumber,
                        "0001",
                        $"{kvkNumber}_0001",
                        "Groen & Zorg Thuiszorg B.V.",
                        "Vondellaan 12, 3521 GE Utrecht",
                        52.09, 5.12, false, ["88101"],
                        new KvkAddressLine("Vondellaan", "12", null, "3521 GE", "Utrecht"),
                        new KvkAddressLine("Vondellaan", "12", null, "3521 GE", "Utrecht"))
                ]));
    }

    private sealed class NoopBrand : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(
            PlatformCompanyUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];
        public byte[] GetBrandWatermarkPng() => [];
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FakePingenHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            Paths.Add(path);
            if (path.Contains("access-tokens", StringComparison.Ordinal))
            {
                return Json(new { access_token = "tok", expires_in = 3600 });
            }

            if (path.Contains("file-upload", StringComparison.Ordinal))
            {
                return Json(new { data = new { id = "file-1", type = "files" } });
            }

            if (path.Contains("/letters", StringComparison.Ordinal) && request.Method == HttpMethod.Post)
            {
                return Json(new { data = new { id = "letter-1", type = "letters" } });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
    }
}
