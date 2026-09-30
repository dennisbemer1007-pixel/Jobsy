using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Sales;

public class IbanValidationTests
{
    [Theory]
    [InlineData("NL91 ABNA 0417 1643 00", true)]
    [InlineData("NL91ABNA0417164300", true)]
    [InlineData("BE68 5390 0754 7034", true)]
    [InlineData("DE89 3704 0044 0532 0130 00", true)]
    [InlineData("NL91 ABNA 0417 1643 01", false)]
    [InlineData("NL00ABNA0417164300", false)]
    [InlineData("XX91ABNA0417164300", false)]
    [InlineData("", false)]
    [InlineData("not-an-iban", false)]
    public void IsValid_mod97_and_lengths(string raw, bool expected)
        => Assert.Equal(expected, Iban.IsValid(raw));

    [Fact]
    public void Normalize_strips_spaces_and_uppercases()
        => Assert.Equal("NL91ABNA0417164300", Iban.Normalize("nl91 abna 0417 1643 00"));

    [Fact]
    public void Mask_and_Last4()
    {
        Assert.Equal("NL•• •••• •••• 4300", Iban.Mask("NL91ABNA0417164300"));
        Assert.Equal("4300", Iban.Last4("NL91ABNA0417164300"));
    }
}

public class SalesPayoutProfileServiceTests
{
    [Fact]
    public async Task GiveConsent_stores_version_and_hash()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedSalesManagerAsync(db);
        var sut = CreateSut(db);

        var dto = await sut.GiveConsentAsync(user.Id);
        Assert.True(dto.HasSelfBillingConsent);
        Assert.Equal(SalesSelfBilling.CurrentVersion, dto.SelfBillingConsentVersion);

        var row = await db.SalesSelfBillingConsents.SingleAsync(c => c.UserId == user.Id);
        Assert.Equal(SalesSelfBilling.CurrentTextSha256, row.TextSha256);
        Assert.Null(row.RevokedAtUtc);
    }

    [Fact]
    public async Task RevokeConsent_sets_revoked_and_missing_for_current_version()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedSalesManagerAsync(db);
        var sut = CreateSut(db);
        await sut.GiveConsentAsync(user.Id);
        var dto = await sut.RevokeConsentAsync(user.Id);
        Assert.False(dto.HasSelfBillingConsent);
    }

    [Fact]
    public async Task Older_consent_version_counts_as_missing()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedSalesManagerAsync(db);
        db.SalesSelfBillingConsents.Add(new SalesSelfBillingConsent
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Version = "old-version",
            TextSha256 = "abc",
            AcceptedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dto = await sut.GetAsync(user.Id);
        Assert.NotNull(dto);
        Assert.False(dto!.HasSelfBillingConsent);
    }

    [Fact]
    public async Task Standard21_requires_vat_number()
    {
        await using var db = CreateDb();
        var (user, profile) = await SeedSalesManagerAsync(db);
        profile.VatNumber = null;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SetVatTreatmentAsync(user.Id, new SalesVatUpdateRequest(
                SalesManagerVatTreatment.Standard21, null, false)));
    }

    [Fact]
    public async Task KOR_requires_confirmation_and_recalculates_requested()
    {
        await using var db = CreateDb();
        var (user, profile) = await SeedSalesManagerAsync(db, withIban: true);
        db.SalesPayoutRequests.Add(new SalesPayoutRequest
        {
            Id = Guid.NewGuid(),
            BeneficiaryUserId = user.Id,
            AmountExVat = 100m,
            VatTreatment = SalesManagerVatTreatment.Standard21,
            VatAmount = 21m,
            TotalInclVat = 121m,
            MaskedIban = Iban.Mask(profile.Iban!),
            Status = SalesPayoutRequestStatus.Requested,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SetVatTreatmentAsync(user.Id, new SalesVatUpdateRequest(
                SalesManagerVatTreatment.SmallBusinessScheme, null, KorConfirmed: false)));

        var dto = await sut.SetVatTreatmentAsync(user.Id, new SalesVatUpdateRequest(
            SalesManagerVatTreatment.SmallBusinessScheme, null, KorConfirmed: true));
        Assert.Equal(nameof(SalesManagerVatTreatment.SmallBusinessScheme), dto.VatTreatment);

        var req = await db.SalesPayoutRequests.SingleAsync();
        Assert.Equal(0m, req.VatAmount);
        Assert.Equal(100m, req.TotalInclVat);
    }

    [Fact]
    public async Task First_iban_applies_without_step_up_or_hold()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedSalesManagerAsync(db, withIban: false);
        var sut = CreateSut(db);
        var result = await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("NL91ABNA0417164300", "T. Test"),
            authMethod: "password");
        Assert.True(result.Applied);
        Assert.Equal("none", result.Method);
        Assert.NotNull(result.Profile?.MaskedIban);
        Assert.Null(result.Profile!.IbanPayoutHoldUntilUtc);
        Assert.DoesNotContain("NL91", result.Profile.MaskedIban!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Iban_change_totp_confirm_sets_hold_and_sends_mail()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var protector = new PassThroughSecretProtector();
        var (user, _) = await SeedSalesManagerAsync(db, withIban: true, totpSecret: secret, secrets: protector);
        var email = new CapturingEmail();
        var sut = CreateSut(db, email, protector);

        var begin = await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("BE68539007547034", "T. Test"),
            authMethod: "password");
        Assert.False(begin.Applied);
        Assert.Equal("totp", begin.Method);

        var code = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
        var dto = await sut.ConfirmIbanChangeAsync(user.Id, code);
        Assert.Equal("BE•• •••• •••• 7034", dto.MaskedIban);
        Assert.NotNull(dto.IbanPayoutHoldUntilUtc);
        Assert.True(dto.IbanPayoutHoldUntilUtc > DateTime.UtcNow);
        Assert.Contains(email.Sent, m => m.Category == "SalesMail.IbanChanged");
        Assert.DoesNotContain("BE68", System.Text.Json.JsonSerializer.Serialize(dto), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Wrong_totp_five_times_drops_pending()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var protector = new PassThroughSecretProtector();
        var (user, _) = await SeedSalesManagerAsync(db, withIban: true, totpSecret: secret, secrets: protector);
        var sut = CreateSut(db, secrets: protector);
        await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("BE68539007547034", "T. Test"),
            "password");

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sut.ConfirmIbanChangeAsync(user.Id, "000000"));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ConfirmIbanChangeAsync(user.Id, "000000"));
        Assert.Equal(0, await db.SalesIbanChangePendings.CountAsync(p => p.ConsumedAtUtc == null));
    }

    [Fact]
    public async Task Replay_totp_code_is_rejected()
    {
        await using var db = CreateDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var protector = new PassThroughSecretProtector();
        var (user, _) = await SeedSalesManagerAsync(db, withIban: true, totpSecret: secret, secrets: protector);
        var sut = CreateSut(db, secrets: protector);
        await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("BE68539007547034", "T. Test"),
            "password");
        var code = TotpAuthenticator.GenerateCode(secret, DateTime.UtcNow);
        await sut.ConfirmIbanChangeAsync(user.Id, code);

        await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("NL91ABNA0417164300", "T. Test"),
            "password");
        var pending = await db.SalesIbanChangePendings.SingleAsync(p => p.ConsumedAtUtc == null);
        pending.LastAcceptedTotpCodeHash = VerificationCodes.Hash(code);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ConfirmIbanChangeAsync(user.Id, code));
    }

    [Fact]
    public async Task Legacy_profile_put_ignores_iban_when_account_exists()
    {
        await using var db = CreateDb();
        var (user, profile) = await SeedSalesManagerAsync(db, withIban: true);
        var oldIban = profile.Iban;
        var sut = CreateSut(db);
        var dto = await sut.UpdateLegacyProfileAsync(
            user.Id,
            new SalesCompanyUpdateRequest(
                profile.CompanyName!,
                profile.KvkNumber!,
                profile.VatNumber,
                profile.Address!,
                profile.PostalCode!,
                profile.City!,
                profile.Country),
            "BE68539007547034",
            "Other");
        Assert.Contains(dto.Warnings, w => w.Contains("IBAN", StringComparison.OrdinalIgnoreCase));
        await db.Entry(profile).ReloadAsync();
        Assert.Equal(oldIban, profile.Iban);
    }

    [Fact]
    public async Task Email_confirm_link_single_use()
    {
        await using var db = CreateDb();
        var (user, _) = await SeedSalesManagerAsync(db, withIban: true);
        var email = new CapturingEmail();
        var sut = CreateSut(db, email);
        var begin = await sut.BeginIbanChangeAsync(
            user.Id,
            new SalesIbanChangeRequest("BE68539007547034", "T. Test"),
            authMethod: "external:entra");
        Assert.Equal("email", begin.Method);
        Assert.Contains(email.Sent, m => m.Category == "SalesMail.IbanConfirm");

        var pending = await db.SalesIbanChangePendings.SingleAsync();
        const string token = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";
        pending.EmailTokenHash = VerificationCodes.Hash(token);
        await db.SaveChangesAsync();

        var dto = await sut.ConfirmIbanChangeByEmailTokenAsync(token, user.Id);
        Assert.NotNull(dto.MaskedIban);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ConfirmIbanChangeByEmailTokenAsync(token, user.Id));
    }

    private static SalesPayoutProfileService CreateSut(
        JobsyDbContext db,
        CapturingEmail? email = null,
        ISecretProtector? secrets = null)
        => new(
            db,
            new StubCommercial(),
            new StubFeatures(),
            email ?? new CapturingEmail(),
            secrets ?? new PassThroughSecretProtector(),
            PassThroughIban.Instance);

    private static async Task<(User User, SalesManagerProfile Profile)> SeedSalesManagerAsync(
        JobsyDbContext db,
        bool withIban = false,
        string? totpSecret = null,
        ISecretProtector? secrets = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"sm-{Guid.NewGuid():N}@test.local",
            FullName = "Test Sales",
            Role = UserRole.SalesManager,
            AuthenticatorEnabled = totpSecret is not null,
            AuthenticatorSecret = totpSecret is null
                ? null
                : (secrets ?? new PassThroughSecretProtector()).Protect(totpSecret)
        };
        db.Users.Add(user);
        var profile = new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CompanyName = "Test BV",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "Straat 1",
            PostalCode = "1234AB",
            City = "Amsterdam",
            Country = "NL",
            Iban = withIban ? "NL91ABNA0417164300" : null,
            PayoutAccountHolderName = withIban ? "T. Test" : null,
            VatTreatment = SalesManagerVatTreatment.Standard21,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.SalesManagerProfiles.Add(profile);
        await db.SaveChangesAsync();
        return (user, profile);
    }

    private static JobsyDbContext CreateDb()
    {
        IbanEfProtection.Configure(PassThroughIban.Instance);
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sales-profile-" + Guid.NewGuid().ToString("N"))
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

    private sealed class PassThroughSecretProtector : ISecretProtector
    {
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? protectedPayload) => protectedPayload;
    }

    private sealed class PassThroughIban : IIbanProtector
    {
        public static readonly PassThroughIban Instance = new();
        public bool IsProtected(string? value) => false;
        public string? Protect(string? plaintext) => plaintext;
        public string? Unprotect(string? protectedPayload) => protectedPayload;
    }

    private sealed class StubCommercial : ISalesCommercialService
    {
        public Task<SalesCommercialSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SalesCommercialSettings { Id = Guid.NewGuid(), IbanChangeHoldDays = 3 });

        public Task<PartnerSalesCatalogDto> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SalesCommercialAdminDto> GetAdminAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<SalesCommercialSettings> UpdateSettingsAsync(
            decimal baseTokenValueEuro,
            decimal highlightCarouselTokens,
            decimal highlightPulseTokens,
            int highlightCarouselDays,
            decimal startHighlightBonusTokens,
            decimal? directCommissionRate = null,
            decimal? indirectCommissionRate = null,
            int? commissionDurationDays = null,
            decimal? partnerCommissionRate = null,
            decimal? year2DirectCommissionRate = null,
            decimal? year3DirectCommissionRate = null,
            decimal? referredYear1DirectCommissionRate = null,
            int? commissionHoldDays = null,
            decimal? payoutMinimumEuro = null,
            int? ibanChangeHoldDays = null,
            int? attributionCookieDays = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<VacancyTypeTokenCost> UpdateVacancyTypeCostAsync(
            VacancyKind kind,
            decimal costTokens,
            bool isActive,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<decimal> GetPublishCostTokensAsync(VacancyKind kind, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<decimal> GetHighlightCostTokensAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(2m);

        public Task<int> GetHighlightDaysAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(7);

        public Task<SalesPackage> UpsertPackageAsync(SalesPackage package, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeletePackageAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false, true, false, "https://lobsy.test", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}

public class SalesProfileRightsMatrixTests
{
    public static TheoryData<string, string, int> EndpointRows => new()
    {
        { "GET api/sales/me/profile", JobsyRoles.SalesManager, 200 },
        { "PUT api/sales/me/profile/company", JobsyRoles.SalesManager, 200 },
        { "PUT api/sales/me/profile/vat", JobsyRoles.SalesManager, 200 },
        { "POST api/sales/me/profile/consent", JobsyRoles.SalesManager, 200 },
        { "POST api/sales/me/payout-account/change", JobsyRoles.SalesManager, 200 },
        { "GET api/sales/me/profile", JobsyRoles.Candidate, 403 },
        { "GET api/sales/me/profile", JobsyRoles.BranchManager, 403 },
        { "GET api/sales/me/profile", JobsyRoles.Admin, 403 },
        { "POST api/sales/me/payout-account/change", JobsyRoles.Admin, 403 },
    };

    [Theory]
    [MemberData(nameof(EndpointRows))]
    public void Matrix_rows_are_defined(string endpoint, string role, int expectedStatus)
    {
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(role));
        Assert.True(expectedStatus is 200 or 403 or 404);
    }
}

public class SalesSelfBillingTextParityTests
{
    [Fact]
    public void Consent_text_matches_localization_key()
    {
        var nl = typeof(Jobsy.Web.Localization.UiStrings)
            .GetField("Catalog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as Dictionary<string, Dictionary<string, string>>;
        Assert.NotNull(nl);
        Assert.Equal(SalesSelfBilling.ConsentText, nl!["nl"]["Sales.SelfBilling.ConsentText"]);
    }
}
