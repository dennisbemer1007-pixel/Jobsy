using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class WerkgeverAanmeldingWizard05Tests
{
    [Theory]
    [InlineData("j.devries@groenenzorg.nl", "https://www.groenenzorg.nl", true)]
    [InlineData("hr@mail.groenenzorg.nl", "groenenzorg.nl", true)]
    [InlineData("someone@gmail.com", "https://groenenzorg.nl", false)]
    [InlineData("other@example.com", "https://groenenzorg.nl", false)]
    public void DomainMatch_public_suffix_aware(string email, string site, bool expected)
        => Assert.Equal(expected, DomainMatch.EmailMatchesWebsite(email, site));

    [Theory]
    [InlineData("gmail.com", true)]
    [InlineData("user@outlook.com", true)]
    [InlineData("groenenzorg.nl", false)]
    public void FreeMailDomains_detects_consumer_mail(string value, bool expected)
        => Assert.Equal(expected, FreeMailDomains.IsFreeMail(value));

    [Fact]
    public void RegistrationPasswordRules_allows_missing_password_for_external()
    {
        RegistrationPasswordRules.Validate(null, required: false);
        Assert.Throws<ArgumentException>(() => RegistrationPasswordRules.Validate(null, required: true));
        Assert.True(RegistrationPasswordRules.IsExternalLoginProvider("microsoft"));
    }

    [Fact]
    public async Task Referral_resolver_unknown_code_is_not_known()
    {
        await using var db = CreateDb();
        var features = CreateFeatures(db);
        var partners = new PartnerAffiliateService(db, new TokenLedgerService(db), features);
        var sut = new DefaultRegistrationReferralResolver(db, partners);
        var result = await sut.ResolveAsync("SM-UNKNOWN1", null);
        Assert.Equal("SM-UNKNOWN1", result.Code);
        Assert.False(result.IsKnown);
        Assert.Equal(RegistrationReferralSource.TypedCode, result.Source);
    }

    [Fact]
    public async Task Manual_pending_registration_never_uses_nl_centre()
    {
        await using var db = CreateDb();
        var sut = CreateRegistration(db, kvk: new UnavailableKvk(), geocoder: new FailGeocoder());
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "12345678",
            "12345678_0001",
            RegistrationScope.BranchOnly,
            "Manual User",
            "manual.user@jobsy.local",
            AcceptedTerms: true,
            Password: "TestPassphrase!",
            AllowPendingKvkVerification: true,
            ManualEstablishmentName: "Handmatig BV",
            ManualEstablishmentAddress: "Onbekendepad 1, 3511 AA Utrecht",
            ManualEstablishmentNumber: "0001"));

        var reg = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.True(reg.LocationUnknown);
        Assert.NotEqual(52.1326, reg.Latitude);
        Assert.NotEqual(5.2913, reg.Longitude);
        Assert.Equal(0d, reg.Latitude);
        Assert.Equal(0d, reg.Longitude);
        Assert.Equal(KvkVerificationStatus.Pending, reg.KvkVerificationStatus);
    }

    [Fact]
    public async Task Organization_claim_honours_selected_establishment_ids()
    {
        await using var db = CreateDb();
        var sut = CreateRegistration(db);

        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990002",
            "99990002_0001",
            RegistrationScope.Organization,
            "Selective Manager",
            "selective.org@jobsy.local",
            AcceptedTerms: true,
            Password: "TestPassphrase!",
            SelectedEstablishmentIds: ["99990002_0001"]));

        var token = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        var activated = await sut.ActivateAsync(token);

        Assert.Equal(1, await db.Companies.CountAsync(c =>
            c.ParentCompanyId == activated.OrganizationCompanyId));
        Assert.False(await db.Companies.AnyAsync(c => c.KvkEstablishmentId == "99990002_0002"));

        var branch = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        Assert.Equal(CompanyLocationSource.Kvk, branch.LocationSource);
    }

    [Fact]
    public async Task External_login_submit_does_not_require_password()
    {
        await using var db = CreateDb();
        var sut = CreateRegistration(db);
        var submit = await sut.SubmitAsync(new RegistrationSubmitRequest(
            "99990001",
            "99990001_0001",
            RegistrationScope.BranchOnly,
            "Ext User",
            "ext.user@jobsy.local",
            AcceptedTerms: true,
            PreferredLoginProvider: "microsoft"));
        Assert.Equal(CompanyRegistrationStatus.PendingActivation, submit.Status);
        var reg = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.Equal("microsoft", reg.PreferredLoginProvider);
        Assert.False(string.IsNullOrWhiteSpace(reg.PasswordHash));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder().Build();
        return new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);
    }

    private static CompanyRegistrationService CreateRegistration(
        JobsyDbContext db,
        IKvkService? kvk = null,
        IGeocodingService? geocoder = null)
    {
        var features = CreateFeatures(db);
        var ledger = new TokenLedgerService(db);
        var partners = new PartnerAffiliateService(db, ledger, features);
        return new CompanyRegistrationService(
            db,
            kvk ?? new CatalogKvk(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            ledger,
            features,
            partners,
            new DefaultRegistrationReferralResolver(db, partners),
            geocoder,
            NullLogger<CompanyRegistrationService>.Instance);
    }

    private sealed class FailGeocoder : IGeocodingService
    {
        public Task<GeocodingResult?> GeocodeAsync(string addressQuery, CancellationToken cancellationToken = default)
            => Task.FromResult<GeocodingResult?>(null);
    }

    private sealed class UnavailableKvk : IKvkService
    {
        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<KvkCompanyResult?>(null);

        public Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<KvkEstablishmentResult>>([]);

        public Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(KvkEstablishmentsLookup.Unavailable("down"));
    }

    private sealed class CatalogKvk : IKvkService
    {
        private readonly JobsyDbContext _db;
        private static readonly KvkEstablishmentResult[] Catalog =
        [
            new("99990001", "0001", "99990001_0001", "Nova Branch", "Straat 1", 52, 4, false),
            new("99990002", "0001", "99990002_0001", "Org HQ", "Straat 2", 52.1, 4.1, false),
            new("99990002", "0002", "99990002_0002", "Org Sibling", "Straat 3", 52.2, 4.2, false)
        ];

        public CatalogKvk(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(string kvkNumber, CancellationToken cancellationToken = default)
        {
            var match = Catalog.FirstOrDefault(c => c.KvkNumber == kvkNumber);
            return Task.FromResult(match is null
                ? null
                : new KvkCompanyResult(match.KvkNumber, match.Name, match.Address, ["5229"]));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
            => (await LookupEstablishmentsAsync(kvkNumber, cancellationToken)).Establishments;

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber, CancellationToken cancellationToken = default)
        {
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);
            var items = Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId), SbiCodes = new[] { "5229" } })
                .ToList();
            return items.Count == 0
                ? KvkEstablishmentsLookup.NotFound()
                : KvkEstablishmentsLookup.Ok(items);
        }
    }
}
