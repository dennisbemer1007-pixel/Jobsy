using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// HTTP-level coverage for prepaid top-up, billing preference, 402 "no tokens",
/// and Admin session-timeout configuration against the real API pipeline.
/// </summary>
public class CoreFunctionalFlowApiTests : IClassFixture<CoreFunctionalFlowApiFactory>
{
    private readonly CoreFunctionalFlowApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public CoreFunctionalFlowApiTests(CoreFunctionalFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Employer_publish_with_empty_balance_returns_402_insufficient_tokens()
    {
        var client = EmployerClient();
        var response = await client.PostAsJsonAsync("api/vacancies/publish", new
        {
            vacancyId = _factory.DraftVacancyId,
            highlight = false,
            pushBom = false,
            extend = false
        });

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InsufficientTokensDto>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal("InsufficientTokens", body!.Code);
        Assert.Equal(_factory.CompanyId, body.CompanyId);
        Assert.Equal(_factory.DraftVacancyId, body.VacancyId);
        Assert.Equal("Publish", body.Action);
        Assert.Equal(0m, body.Balance);
        Assert.True(body.RequiredTokens >= 1m);
        Assert.True(body.Deficit >= 1m);
        Assert.Contains("token", body.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Employer_top_up_quote_returns_exact_match_and_bulk_packs()
    {
        var client = EmployerClient();
        var response = await client.GetAsync(
            $"api/tokens/top-up-quote?companyId={_factory.CompanyId:D}&requiredTokens=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<TokenTopUpQuoteDto>(JsonOpts);
        Assert.NotNull(quote);
        Assert.Equal(_factory.CompanyId, quote!.CompanyId);
        Assert.Equal(0m, quote.Balance);
        Assert.Equal(3m, quote.RequiredTokens);
        Assert.Equal(3m, quote.Deficit);
        Assert.Equal(3, quote.ExactMatchTokens);
        Assert.True(quote.ExactMatchPriceEuro > 0m);
        Assert.Contains(quote.BulkPacks, p => p.PackSize == 1);
        Assert.Contains(quote.BulkPacks, p => p.PackSize == 10);
        Assert.Contains(quote.BulkPacks, p => p.PackSize == 50);
    }

    [Fact]
    public async Task Employer_can_set_ideal_and_creditcard_billing_preference()
    {
        var client = EmployerClient();

        var cc = await client.PutAsJsonAsync(
            $"api/companies/{_factory.CompanyId:D}/billing-preference",
            new UpdateBillingPreferenceRequest(MolliePaymentMethods.CreditCard));
        Assert.Equal(HttpStatusCode.OK, cc.StatusCode);
        var ccBody = await cc.Content.ReadFromJsonAsync<CompanySummaryDto>(JsonOpts);
        Assert.Equal(MolliePaymentMethods.CreditCard, ccBody!.PreferredPaymentMethod);

        var ideal = await client.PutAsJsonAsync(
            $"api/companies/{_factory.CompanyId:D}/billing-preference",
            new UpdateBillingPreferenceRequest(MolliePaymentMethods.Ideal));
        Assert.Equal(HttpStatusCode.OK, ideal.StatusCode);
        var idealBody = await ideal.Content.ReadFromJsonAsync<CompanySummaryDto>(JsonOpts);
        Assert.Equal(MolliePaymentMethods.Ideal, idealBody!.PreferredPaymentMethod);

        var bad = await client.PutAsJsonAsync(
            $"api/companies/{_factory.CompanyId:D}/billing-preference",
            new UpdateBillingPreferenceRequest("paypal"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Employer_checkout_accepts_payment_method_and_pending_publish_action()
    {
        var client = EmployerClient();
        var response = await client.PostAsJsonAsync("api/tokens/checkout", new CreateCheckoutRequest(
            _factory.CompanyId,
            PackSize: 1,
            PendingAction: new PendingActionCheckoutRequest(
                _factory.DraftVacancyId,
                "Publish",
                RequiredTokens: 1m),
            PaymentMethod: MolliePaymentMethods.Ideal));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var checkout = await response.Content.ReadFromJsonAsync<CheckoutResultDto>(JsonOpts);
        Assert.NotNull(checkout);
        Assert.Equal(1, checkout!.PackSize);
        Assert.Equal(MolliePaymentMethods.Ideal, checkout.PaymentMethod);
        Assert.False(string.IsNullOrWhiteSpace(checkout.CheckoutUrl));
        Assert.NotEqual(Guid.Empty, checkout.CheckoutId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var pending = await db.PendingTokenActions.SingleAsync(a =>
            a.TokenPurchaseCheckoutId == checkout.CheckoutId);
        Assert.Equal(PendingTokenActionKind.Publish, pending.ActionKind);
        Assert.Equal(PendingTokenActionStatus.Pending, pending.Status);
        Assert.Equal(_factory.DraftVacancyId, pending.VacancyId);
    }

    [Fact]
    public async Task Employer_checkout_rejects_unsupported_payment_method()
    {
        var client = EmployerClient();
        var response = await client.PostAsJsonAsync("api/tokens/checkout", new CreateCheckoutRequest(
            _factory.CompanyId,
            PackSize: 5,
            PaymentMethod: "bancontact"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("betaalmethode", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_configure_session_timeout_and_public_endpoint_reflects_it()
    {
        var admin = AdminClient();
        var anon = _factory.CreateClient();

        var before = await anon.GetFromJsonAsync<SessionSecurityDto>("api/settings/session-security", JsonOpts);
        Assert.NotNull(before);
        Assert.Equal(SessionSecurityRules.DefaultInactivityTimeoutMinutes, before!.InactivityTimeoutMinutes);

        var update = await admin.PutAsJsonAsync("api/settings/platform-features", new UpdatePlatformFeatureRequest(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            InactiveCompanyDays: 120,
            SessionInactivityTimeoutMinutes: 5));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var features = await update.Content.ReadFromJsonAsync<PlatformFeatureDto>(JsonOpts);
        Assert.Equal(5, features!.SessionInactivityTimeoutMinutes);

        var after = await anon.GetFromJsonAsync<SessionSecurityDto>("api/settings/session-security", JsonOpts);
        Assert.Equal(5, after!.InactivityTimeoutMinutes);

        // Restore default for other tests sharing the factory DB.
        var restore = await admin.PutAsJsonAsync("api/settings/platform-features", new UpdatePlatformFeatureRequest(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            InactiveCompanyDays: 120,
            SessionInactivityTimeoutMinutes: 30));
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    [Fact]
    public async Task Guest_cannot_access_token_checkout_or_billing_preference()
    {
        var client = _factory.CreateClient();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync($"api/tokens/top-up-quote?companyId={_factory.CompanyId:D}&requiredTokens=1")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("api/tokens/checkout", new CreateCheckoutRequest(_factory.CompanyId, 1))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync(
                $"api/companies/{_factory.CompanyId:D}/billing-preference",
                new UpdateBillingPreferenceRequest(MolliePaymentMethods.Ideal))).StatusCode);
    }

    private HttpClient EmployerClient() => Authed(_factory.EmployerEmail);
    private HttpClient AdminClient() => Authed(_factory.AdminEmail);

    private HttpClient Authed(string email)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Jobsy-Email", email);
        client.DefaultRequestHeaders.Add("X-Jobsy-Dev-Secret", CoreFunctionalFlowApiFactory.DevSecret);
        return client;
    }
}

public sealed class CoreFunctionalFlowApiFactory : WebApplicationFactory<Program>
{
    public const string DevSecret = "core-functional-flow-secret";

    public Guid CompanyId { get; } = Guid.Parse("d2000000-0000-0000-0000-000000000001");
    public Guid DraftVacancyId { get; } = Guid.Parse("d2000000-0000-0000-0000-000000000010");
    public Guid EmployerId { get; } = Guid.Parse("d2000000-0000-0000-0000-000000000021");
    public Guid AdminId { get; } = Guid.Parse("d2000000-0000-0000-0000-000000000022");

    public string EmployerEmail => "flow.manager@jobsy.local";
    public string AdminEmail => "flow.admin@jobsy.local";

    private readonly string _dbName = "CoreFunctionalFlow-" + Guid.NewGuid();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("JobsyAuth:AllowDevelopmentAuth", "true");
        builder.UseSetting("JobsyAuth:DevelopmentAuthSecret", DevSecret);
        builder.UseSetting("Seed:Enabled", "false");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:JobsyDb",
            "Host=127.0.0.1;Port=5432;Database=JobsyTest;Username=postgres;Password=postgres");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();

            var efDescriptors = services
                .Where(d =>
                    d.ServiceType == typeof(JobsyDbContext)
                    || d.ServiceType == typeof(DbContextOptions<JobsyDbContext>)
                    || (d.ServiceType.IsGenericType
                        && d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContext", StringComparison.Ordinal))
                    || (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                    || (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) == true
                        && d.ServiceType.FullName.Contains("JobsyDbContext", StringComparison.Ordinal)))
                .ToList();
            foreach (var d in efDescriptors)
            {
                services.Remove(d);
            }

            foreach (var d in services.Where(d =>
                         d.ServiceType.IsGenericType
                         && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                         && d.ServiceType.GenericTypeArguments[0] == typeof(JobsyDbContext)).ToList())
            {
                services.Remove(d);
            }

            services.AddDbContext<JobsyDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IVacancyContentModerationService>();
            services.AddSingleton<IVacancyContentModerationService>(new AllowAllModeration());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        EnsureSeeded();
        base.ConfigureClient(client);
    }

    private void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        if (db.Users.Any())
        {
            _seeded = true;
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Companies.Add(new Company
        {
            Id = CompanyId,
            Name = "Flow Vestiging",
            KvkNumber = "88880001",
            KvkEstablishmentId = "88880001_0001",
            Address = "Veilingweg 1, Naaldwijk",
            Location = new GeoPoint(52.0, 4.2),
            Type = CompanyType.Employer,
            TokensManagedByEnterprise = false,
            PreferredPaymentMethod = MolliePaymentMethods.Ideal
        });

        db.Users.AddRange(
            new User
            {
                Id = EmployerId,
                Email = EmployerEmail,
                FullName = "Flow Manager",
                Role = UserRole.BranchManager,
                IsActive = true,
                CompanyId = CompanyId
            },
            new User
            {
                Id = AdminId,
                Email = AdminEmail,
                FullName = "Flow Admin",
                Role = UserRole.Admin,
                IsActive = true
            });

        db.Vacancies.Add(new Vacancy
        {
            Id = DraftVacancyId,
            Title = "Kasmedewerker Flow",
            Description = "Draft for prepaid top-up flow.",
            HourlyWage = 14.50m,
            StartDate = today,
            EndDate = today.AddMonths(1),
            Status = VacancyStatus.Draft,
            CompanyId = CompanyId,
            Location = new GeoPoint(52.0, 4.2),
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Winkel,
            WorkTypeLabels = "Winkel",
            MinHoursPerWeek = 12,
            MaxHoursPerWeek = 24,
            FlexibleTimes = true,
            MaxApplications = 10
        });

        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });

        db.TokenPricings.AddRange(
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 1, PriceEuro = 5.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 5, PriceEuro = 22.50m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 10, PriceEuro = 40.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 50, PriceEuro = 175.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 100, PriceEuro = 300.00m, IsActive = true });

        db.SaveChanges();
        _seeded = true;
    }

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VacancyContentModerationResult(true, null));
    }
}
