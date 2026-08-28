using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>
/// Comprehensive end-to-end coverage for the Lobsy registration → prepaid tokens →
/// Mollie webhook → commission → session-timeout quality gate (Company Manager,
/// Salesmanager hierarchy, Admin).
/// </summary>
public class CoreFunctionalFlowE2ETests
{
    [Fact]
    public async Task Full_chain_company_manager_salesmanager_admin_prepaid_and_commissions()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);
        SeedSpendCosts(db);
        SeedTokenPacks(db);

        // ── 1. Salesmanager hierarchy (upline + direct) ──────────────────────
        var invite = CreateInvite(db);
        var upline = await invite.InviteAsync("upline.e2e@jobsy.local", "Upline SM");
        Assert.True(upline.CanRecruitSalesManagers);
        CompleteOnboarding(db, upline.UserId, "SM-UPLINE1");

        var direct = await invite.InviteAsync(
            "direct.e2e@jobsy.local",
            "Direct SM",
            referredBySalesManagerUserId: upline.UserId);
        Assert.False(direct.CanRecruitSalesManagers);
        Assert.Equal(upline.UserId, direct.ReferredBySalesManagerUserId);
        CompleteOnboarding(db, direct.UserId, "SM-DIRECT1");

        var commercial = new SalesCommercialService(db, new TokenLedgerService(db));
        var adminCommercial = await commercial.GetAdminAsync();
        Assert.Equal(0.25m, adminCommercial.DirectCommissionRate);
        Assert.Equal(0.05m, adminCommercial.IndirectCommissionRate);
        Assert.Equal(SalesCommissionRules.DefaultCommissionDurationDays, adminCommercial.CommissionDurationDays);

        // ── 2. Company Manager registration + email verification ─────────────
        var registration = CreateRegistrationService(db);
        var submit = await registration.SubmitAsync(new RegistrationSubmitRequest(
            "88880001",
            "88880001_0001",
            RegistrationScope.BranchOnly,
            "E2E Manager",
            "manager.e2e@jobsy.local",
            null,
            AcceptedTerms: true,
            SalesManagerTrackingCode: "SM-DIRECT1",
            Password: "TestPass1!"));

        Assert.Equal(CompanyRegistrationStatus.PendingActivation, submit.Status);
        var activationToken = await db.CompanyRegistrations
            .Where(r => r.Id == submit.RegistrationId)
            .Select(r => r.ActivationToken)
            .SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(activationToken));

        var activated = await registration.ActivateAsync(activationToken);
        Assert.Equal("EnterpriseManager", activated.Role);
        Assert.Null(activated.OrganizationCompanyId);
        Assert.NotNull(activated.BranchCompanyId);

        var company = await db.Companies.SingleAsync(c => c.Id == activated.BranchCompanyId);
        var regRow = await db.CompanyRegistrations.SingleAsync(r => r.Id == submit.RegistrationId);
        Assert.NotNull(regRow.ContactEmailVerifiedAt);
        Assert.Equal(direct.UserId, company.ReferredBySalesManagerUserId);
        Assert.Equal(upline.UserId, company.CommissionIndirectSalesManagerUserId);
        Assert.NotNull(company.FirstYearStartedAt);
        Assert.True(company.HasReceivedWelcomeToken);
        Assert.Equal(1m, await new TokenLedgerService(db).GetBalanceAsync(company.Id));

        // Billing preference: iDEAL ↔ creditcard as defaults for Mollie checkout.
        company.PreferredPaymentMethod = MolliePaymentMethods.CreditCard;
        await db.SaveChangesAsync();
        var features = CreateFeatures(db);
        var payments = new MolliePaymentStub(db, features, NullLogger<MolliePaymentStub>.Instance);
        var ccCheckout = await payments.CreateTokenPurchaseCheckoutAsync(company.Id, 5);
        Assert.Equal(MolliePaymentMethods.CreditCard, ccCheckout.PaymentMethod);

        company.PreferredPaymentMethod = MolliePaymentMethods.Ideal;
        await db.SaveChangesAsync();
        var idealCheckout = await payments.CreateTokenPurchaseCheckoutAsync(company.Id, 5);
        Assert.Equal(MolliePaymentMethods.Ideal, idealCheckout.PaymentMethod);

        // ── 3. Spend welcome token, then "no tokens, no action" ───────────────
        var products = CreateProducts(db);
        var vacancy1 = await SeedDraftVacancyAsync(db, company.Id, "Welkomst vacature");
        var firstPublish = await products.PublishAsync(
            vacancy1,
            new VacancyPublishOptions(),
            actorUserId: activated.UserId,
            allowPendingApproval: false);
        Assert.True(firstPublish.Succeeded, firstPublish.ErrorMessage);
        Assert.Equal(0m, await new TokenLedgerService(db).GetBalanceAsync(company.Id));
        // SM referral grants a free start-highlight on the first publish.
        Assert.False(company.PendingStartHighlightBonus);

        var vacancy2 = await SeedDraftVacancyAsync(db, company.Id, "Blocked vacature");
        var blocked = await products.PublishAsync(
            vacancy2,
            new VacancyPublishOptions(),
            actorUserId: activated.UserId,
            allowPendingApproval: false);
        Assert.False(blocked.Succeeded);
        Assert.True(blocked.InsufficientTokens);
        Assert.Equal(0m, blocked.Balance);
        Assert.True(blocked.RequiredTokens >= 1m);
        Assert.Contains("token", blocked.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(VacancyStatus.Draft, vacancy2.Status);

        // Nearby OpenForWork candidate so PushBom reaches the token gate (not the empty-reach gate).
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "candidate.e2e@jobsy.local",
            FullName = "E2E Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            HomeLocation = new GeoPoint(51.991, 4.221),
            PreferencesJson = """{"preferredTransport":"Fiets","maxTravelMinutes":45}"""
        });

        // Highlight / PushBom / Extend also blocked on empty balance (separate active listing).
        var activeNoTokens = await SeedDraftVacancyAsync(db, company.Id, "Active no tokens");
        activeNoTokens.Status = VacancyStatus.Active;
        activeNoTokens.PublishedAtUtc = DateTime.UtcNow;
        activeNoTokens.HighlightedUntil = null;
        await db.SaveChangesAsync();
        var highlightBlocked = await products.HighlightAsync(activeNoTokens, activated.UserId);
        Assert.True(highlightBlocked.InsufficientTokens, highlightBlocked.ErrorMessage);
        var pushBlocked = await products.PushBomAsync(activeNoTokens, activated.UserId);
        Assert.True(pushBlocked.InsufficientTokens, pushBlocked.ErrorMessage);
        var extendBlocked = await products.ExtendAsync(activeNoTokens, activated.UserId);
        Assert.True(extendBlocked.InsufficientTokens, extendBlocked.ErrorMessage);

        // In-context top-up quote: Exact Match + bulk packs (mirrors GET top-up-quote).
        var balance = await new TokenLedgerService(db).GetBalanceAsync(company.Id);
        var required = blocked.RequiredTokens;
        var deficit = Math.Max(0m, required - balance);
        var exactMatch = (int)Math.Ceiling(deficit);
        Assert.Equal(1, exactMatch);
        var packs = await db.TokenPricings.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.PackSize)
            .Select(p => p.PackSize)
            .ToListAsync();
        Assert.Contains(1, packs);
        Assert.Contains(10, packs);
        Assert.Contains(50, packs);

        // Checkout with Exact Match + iDEAL + pending Publish (in-context Mollie path).
        var paidCheckout = await payments.CreateTokenPurchaseCheckoutAsync(
            company.Id, exactMatch, MolliePaymentMethods.Ideal);
        Assert.Equal(MolliePaymentMethods.Ideal, paidCheckout.PaymentMethod);
        Assert.Equal(exactMatch, paidCheckout.PackSize);

        var pending = new PendingTokenActionService(
            db, products, new TokenLedgerService(db), NullLogger<PendingTokenActionService>.Instance);
        await pending.AttachAsync(
            paidCheckout.CheckoutId,
            company.Id,
            vacancy2.Id,
            PendingTokenActionKind.Publish,
            optionHighlight: false,
            optionPushBom: false,
            optionExtend: false,
            requiredTokens: required,
            actorUserId: activated.UserId);

        // ── 4. Webhook fulfillment → tokens + auto-publish + commissions ─────
        var fulfillment = CreateFulfillment(db, pending);
        var fulfill = await fulfillment.TryFulfillPaidCheckoutAsync(
            paidCheckout.CheckoutId,
            allowDevStubMarkPaid: true);
        Assert.NotNull(fulfill);
        Assert.False(fulfill!.AlreadyFulfilled);
        Assert.NotNull(fulfill.PendingAction);
        Assert.True(fulfill.PendingAction!.Succeeded);

        await db.Entry(vacancy2).ReloadAsync();
        Assert.Equal(VacancyStatus.Active, vacancy2.Status);
        Assert.Equal(
            PendingTokenActionStatus.Executed,
            await db.PendingTokenActions
                .Where(a => a.TokenPurchaseCheckoutId == paidCheckout.CheckoutId)
                .Select(a => a.Status)
                .SingleAsync());

        // Exact-match pack spent on publish; ambassador share (15% of pack) remains as company tegoed.
        Assert.Equal(
            SalesCommissionRules.AmbassadorTokens(exactMatch),
            await new TokenLedgerService(db).GetBalanceAsync(company.Id));

        var (exVatCents, _, _) = TokenVatPricing.SplitInclVatEuros(paidCheckout.AmountEuro);
        var purchaseExVat = TokenVatPricing.FromCents(exVatCents);
        var expectedDirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultDirectCommissionRate);
        var expectedIndirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultIndirectCommissionRate);
        Assert.True(expectedDirect > 0m);
        Assert.True(expectedIndirect > 0m);
        Assert.Equal(
            Math.Round(purchaseExVat * 0.25m, 2, MidpointRounding.AwayFromZero),
            expectedDirect);
        Assert.Equal(
            Math.Round(purchaseExVat * 0.05m, 2, MidpointRounding.AwayFromZero),
            expectedIndirect);

        var commissions = new CommissionLedgerService(db);
        Assert.Equal(expectedDirect, await commissions.GetBalanceExVatAsync(direct.UserId));
        Assert.Equal(expectedIndirect, await commissions.GetBalanceExVatAsync(upline.UserId));

        var dashboard = new SalesManagerDashboardService(db, commissions);
        var directDash = await dashboard.GetDashboardAsync(direct.UserId);
        var uplineDash = await dashboard.GetDashboardAsync(upline.UserId);
        Assert.NotNull(directDash);
        Assert.NotNull(uplineDash);
        Assert.Equal(expectedDirect, directDash!.BalanceExVat);
        Assert.Equal(expectedIndirect, uplineDash!.BalanceExVat);
        Assert.Contains(directDash.Suppliers, s => s.CompanyId == company.Id);
        Assert.True(directDash.IsOnboardingComplete);
        Assert.Equal("SM-DIRECT1", directDash.TrackingCode);

        // Idempotent webhook replay must not double-credit.
        var again = await fulfillment.TryFulfillPaidCheckoutAsync(paidCheckout.CheckoutId);
        Assert.True(again!.AlreadyFulfilled);
        Assert.Equal(expectedDirect, await commissions.GetBalanceExVatAsync(direct.UserId));
        Assert.Equal(expectedIndirect, await commissions.GetBalanceExVatAsync(upline.UserId));

        // ── 5. Admin dynamic session inactivity timeout ──────────────────────
        var platform = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());

        var defaults = await platform.GetAsync();
        Assert.Equal(SessionSecurityRules.DefaultInactivityTimeoutMinutes, defaults.SessionInactivityTimeoutMinutes);

        var custom = await platform.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            InactiveCompanyDays: 120,
            SessionInactivityTimeoutMinutes: 5));
        Assert.Equal(5, custom.SessionInactivityTimeoutMinutes);

        // Simulate idle past the custom 5-minute threshold → graceful re-auth.
        var http = CreateAuthedContext("/employer/vacancies", "manager.e2e@jobsy.local");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-6));
        CopySetCookieToRequest(http);
        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var middleware = new SessionInactivityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(5));

        Assert.True(authService.SignedOut);
        Assert.Equal(StatusCodes.Status302Found, http.Response.StatusCode);
        var expiredLocation = http.Response.Headers.Location.ToString();
        Assert.Contains("error=session-expired", expiredLocation, StringComparison.Ordinal);
        Assert.Contains("returnUrl=", expiredLocation, StringComparison.Ordinal);

        // Restore default 30 minutes and confirm activity within window stays signed in.
        await platform.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            InactiveCompanyDays: 120,
            SessionInactivityTimeoutMinutes: 30));

        var activeHttp = CreateAuthedContext("/admin/settings", "admin@jobsy.local");
        SessionActivityCookie.Stamp(activeHttp, DateTimeOffset.UtcNow.AddMinutes(-10));
        CopySetCookieToRequest(activeHttp);
        var activeAuth = new FakeAuthService();
        ReplaceAuthService(activeHttp, activeAuth);
        var nextCalled = false;
        await new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }).InvokeAsync(activeHttp, new FixedTimeoutProvider(30));
        Assert.False(activeAuth.SignedOut);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Commission_window_stops_after_one_year_from_entrepreneur_onboarding()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);

        var invite = CreateInvite(db);
        var upline = await invite.InviteAsync("upline.year@jobsy.local", "Upline");
        CompleteOnboarding(db, upline.UserId, "SM-YEARU1");
        var direct = await invite.InviteAsync(
            "direct.year@jobsy.local", "Direct", referredBySalesManagerUserId: upline.UserId);
        CompleteOnboarding(db, direct.UserId, "SM-YEARD1");

        var companyId = Guid.NewGuid();
        var started = DateTime.UtcNow.AddYears(-1).AddDays(-1);
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Expired Window Co",
            KvkNumber = "77770001",
            KvkEstablishmentId = "77770001_0001",
            Address = "Westland",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            ReferredBySalesManagerUserId = direct.UserId,
            CommissionIndirectSalesManagerUserId = upline.UserId,
            CommissionDirectRateSnapshot = SalesCommissionRules.DefaultDirectCommissionRate,
            CommissionIndirectRateSnapshot = SalesCommissionRules.DefaultIndirectCommissionRate,
            CommissionDurationDaysSnapshot = SalesCommissionRules.DefaultCommissionDurationDays,
            CommissionTermsSnapshottedAtUtc = started,
            FirstYearStartedAt = started
        });
        await db.SaveChangesAsync();

        Assert.False(SalesCommissionRules.IsWithinCommissionWindow(
            started, DateTime.UtcNow, SalesCommissionRules.DefaultCommissionDurationDays));

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40.00m);
        var checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_year_cap",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40.00m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            PaymentMethod = MolliePaymentMethods.Ideal,
            Status = TokenPurchaseCheckoutStatus.Paid,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var fulfillment = CreateFulfillment(db, new NoopPendingActions());
        var result = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId);
        Assert.NotNull(result);

        var commissions = new CommissionLedgerService(db);
        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(direct.UserId));
        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(upline.UserId));
    }

    [Fact]
    public async Task Bulk_pack_checkout_with_creditcard_fulfills_pending_highlight()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);
        SeedSpendCosts(db);
        SeedTokenPacks(db);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Bulk Co",
            KvkNumber = "66660001",
            Address = "Delft",
            Location = new GeoPoint(52.01, 4.36),
            PreferredPaymentMethod = MolliePaymentMethods.CreditCard
        });
        var vacancy = await SeedDraftVacancyAsync(db, companyId, "Highlight me");
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var products = CreateProducts(db);
        var payments = new MolliePaymentStub(
            db, CreateFeatures(db), NullLogger<MolliePaymentStub>.Instance);
        var checkout = await payments.CreateTokenPurchaseCheckoutAsync(
            companyId, packSize: 10, MolliePaymentMethods.CreditCard);
        Assert.Equal(MolliePaymentMethods.CreditCard, checkout.PaymentMethod);

        var pending = new PendingTokenActionService(
            db, products, new TokenLedgerService(db), NullLogger<PendingTokenActionService>.Instance);
        var highlightCost = await new SalesCommercialService(db, new TokenLedgerService(db))
            .GetHighlightCostTokensAsync();
        Assert.Equal(2m, highlightCost);

        await pending.AttachAsync(
            checkout.CheckoutId,
            companyId,
            vacancy.Id,
            PendingTokenActionKind.Highlight,
            optionHighlight: true,
            optionPushBom: false,
            optionExtend: false,
            requiredTokens: highlightCost,
            actorUserId: null);

        var fulfill = await CreateFulfillment(db, pending)
            .TryFulfillPaidCheckoutAsync(checkout.CheckoutId, allowDevStubMarkPaid: true);
        Assert.NotNull(fulfill);
        Assert.True(fulfill!.PendingAction!.Succeeded);
        Assert.Equal(PendingTokenActionKind.Highlight, fulfill.PendingAction.ActionKind);

        var balance = await new TokenLedgerService(db).GetBalanceAsync(companyId);
        Assert.Equal(10m - highlightCost, balance);
    }

    [Fact]
    public void Session_expired_login_copy_and_draft_preservation_contract_are_present()
    {
        // UI contract: login explains timeout; client script preserves opt-in form drafts.
        var root = FindRepoRoot();
        var loginSource = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Login.razor"));
        var uiStrings = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Localization", "UiStrings.cs"));
        var idleJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "sessionIdle.js"));

        Assert.Contains("session-expired", loginSource, StringComparison.Ordinal);
        Assert.Contains("Login.ErrorSessionExpired", uiStrings, StringComparison.Ordinal);
        Assert.Contains("form drafts", uiStrings, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-session-draft", idleJs, StringComparison.Ordinal);
        Assert.Contains("saveCriticalDrafts", idleJs, StringComparison.Ordinal);
        Assert.Contains("sessionStorage", idleJs, StringComparison.Ordinal);
        Assert.Contains("session-expired", idleJs, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static async Task<Vacancy> SeedDraftVacancyAsync(JobsyDbContext db, Guid companyId, string title)
    {
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Title = title,
            Description = "E2E draft",
            Status = VacancyStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Location = new GeoPoint(51.99, 4.22),
            RequiredTransport = TransportMode.Bike,
            HourlyWage = 14.50m
        };
        db.Vacancies.Add(vacancy);
        await db.SaveChangesAsync();
        vacancy.Company = await db.Companies.SingleAsync(c => c.Id == companyId);
        return vacancy;
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
        db.SaveChanges();
    }

    private static void SeedTokenPacks(JobsyDbContext db)
    {
        db.TokenPricings.AddRange(
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 1, PriceEuro = 5.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 5, PriceEuro = 22.50m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 10, PriceEuro = 40.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 50, PriceEuro = 175.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 100, PriceEuro = 300.00m, IsActive = true });
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

    private static void SeedPlatformCompany(JobsyDbContext db)
    {
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            VatBufferIban = "NL91KNAB0417164300"
        });
        db.SaveChanges();
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

    private static ISalesManagerInviteService CreateInvite(JobsyDbContext db) =>
        new SalesManagerInviteService(
            db,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            NullLogger<SalesManagerInviteService>.Instance);

    private static CompanyRegistrationService CreateRegistrationService(JobsyDbContext db)
    {
        EnsurePaidPublishPeriod(db, exposeActivationLinks: true);
        var config = new ConfigurationBuilder().Build();
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { ExposeRegistrationActivationLinks = true }),
            config);

        return new CompanyRegistrationService(
            db,
            new E2EKvkAdapter(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new TokenLedgerService(db),
            features,
            NullLogger<CompanyRegistrationService>.Instance);
    }

    private static IPlatformFeatureService CreateFeatures(JobsyDbContext db)
    {
        EnsurePaidPublishPeriod(db, exposeActivationLinks: true);
        return new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { ExposeRegistrationActivationLinks = true }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());
    }

    private static void EnsurePaidPublishPeriod(JobsyDbContext db, bool exposeActivationLinks = true)
    {
        var row = db.PlatformFeatureSettings.Local.FirstOrDefault()
                  ?? db.PlatformFeatureSettings.FirstOrDefault();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ExposeRegistrationActivationLinks = exposeActivationLinks,
                FreePublishUntil = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.FreePublishUntil = null;
            row.ExposeRegistrationActivationLinks = exposeActivationLinks;
        }

        db.SaveChanges();
    }

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
    {
        var features = CreateFeatures(db);
        return new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            new UserNotificationService(db),
            new CandidateActionTokenService(db),
            NullLogger<VacancyProductService>.Instance);
    }

    private static TokenPurchaseFulfillmentService CreateFulfillment(
        JobsyDbContext db,
        IPendingTokenActionService pending)
    {
        var companySettings = new PlatformCompanySettingsService(db);
        var tokens = new TokenLedgerService(db);
        var commissions = new CommissionLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var revenueShare = new RevenueShareService(db, tokens, commissions, commercial);
        var features = CreateFeatures(db);

        return new TokenPurchaseFulfillmentService(
            db,
            tokens,
            new MolliePaymentStub(db, features, NullLogger<MolliePaymentStub>.Instance),
            new TokenPurchaseInvoiceService(db, companySettings),
            new VatBufferTransferService(db, companySettings, NullLogger<VatBufferTransferService>.Instance),
            revenueShare,
            new CommissionLedgerService(db),
            pending,
            new FakeHostEnvironment(),
            NullLogger<TokenPurchaseFulfillmentService>.Instance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static DefaultHttpContext CreateAuthedContext(string path, string email)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Jobsy.Tests.CoreFlow");
        services.AddSingleton<IAuthenticationService>(new FakeAuthService());
        var sp = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = sp };
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, "EnterpriseManager")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));
        return http;
    }

    private static void ReplaceAuthService(HttpContext http, FakeAuthService authService)
    {
        var existingDp = http.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(existingDp);
        services.AddSingleton<IAuthenticationService>(authService);
        http.RequestServices = services.BuildServiceProvider();
    }

    private static void CopySetCookieToRequest(HttpContext http)
    {
        var setCookie = http.Response.Headers.SetCookie.FirstOrDefault(v =>
            v is not null && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(setCookie));
        var segment = setCookie!.Split(';', 2)[0];
        var eq = segment.IndexOf('=');
        Assert.True(eq > 0);
        http.Request.Headers.Cookie = $"{segment[..eq]}={segment[(eq + 1)..]}";
        http.Response.Headers.SetCookie = new Microsoft.Extensions.Primitives.StringValues();
    }

    private sealed class FixedTimeoutProvider(int minutes) : ISessionTimeoutProvider
    {
        public Task<int> GetInactivityTimeoutMinutesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(minutes);
    }

    private sealed class FakeAuthService : IAuthenticationService
    {
        public bool SignedOut { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }

    private sealed class NoopPendingActions : IPendingTokenActionService
    {
        public Task<PendingTokenAction> AttachAsync(
            Guid checkoutId,
            Guid spendCompanyId,
            Guid vacancyId,
            PendingTokenActionKind actionKind,
            bool optionHighlight,
            bool optionPushBom,
            bool optionExtend,
            decimal requiredTokens,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PendingTokenAction { Id = Guid.NewGuid() });

        public Task<PendingTokenActionExecutionResult?> TryExecuteForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<PendingTokenActionExecutionResult?>(null);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class E2EKvkAdapter : IKvkService
    {
        private readonly JobsyDbContext _db;

        private static readonly KvkEstablishmentResult[] Catalog =
        [
            new("88880001", "0001", "88880001_0001", "E2E Branch", "Veilingweg 1", 52.0, 4.2, false)
        ];

        public E2EKvkAdapter(JobsyDbContext db) => _db = db;

        public Task<KvkCompanyResult?> GetByKvkNumberAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var match = Catalog.FirstOrDefault(c => c.KvkNumber == kvkNumber);
            return Task.FromResult(match is null
                ? null
                : new KvkCompanyResult(match.KvkNumber, match.Name, match.Address));
        }

        public async Task<IReadOnlyList<KvkEstablishmentResult>> GetEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var lookup = await LookupEstablishmentsAsync(kvkNumber, cancellationToken);
            return lookup.Establishments;
        }

        public async Task<KvkEstablishmentsLookup> LookupEstablishmentsAsync(
            string kvkNumber,
            CancellationToken cancellationToken = default)
        {
            var inUse = await _db.Companies.AsNoTracking()
                .Where(c => c.KvkNumber == kvkNumber && c.KvkEstablishmentId != null)
                .Select(c => c.KvkEstablishmentId!)
                .ToListAsync(cancellationToken);

            var items = Catalog
                .Where(c => c.KvkNumber == kvkNumber)
                .Select(c => c with { IsInUse = inUse.Contains(c.KvkEstablishmentId) })
                .ToList();
            return items.Count == 0
                ? KvkEstablishmentsLookup.NotFound()
                : KvkEstablishmentsLookup.Ok(items);
        }
    }
}
