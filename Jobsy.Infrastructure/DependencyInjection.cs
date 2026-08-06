using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var connectionString = ResolveJobsyConnectionString(configuration);
        var isDev = environment?.IsDevelopment() ?? true;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!isDev)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:JobsyDb (or DATABASE_URL) is required outside Development. " +
                    "On Render: jobsy-api → Environment → set ConnectionStrings__JobsyDb to the " +
                    "Internal Database URL from jobsy-db → Info.");
            }

            connectionString =
                "Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres";
        }

        try
        {
            connectionString = NormalizePostgresConnectionString(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:JobsyDb is not a valid Postgres connection string. " +
                "Paste the Internal Database URL from Render (jobsy-db → Info).", ex);
        }

        if (!isDev
            && connectionString.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase)
            && connectionString.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Default local postgres connection is not allowed outside Development.");
        }

        services.AddJobsyDataProtection(connectionString);
        services.AddSingleton<ISecretProtector, SecretProtector>();

        services.AddOptions<JobsyFeatureOptions>()
            .Bind(configuration.GetSection(JobsyFeatureOptions.SectionName));

        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName));

        services.AddDbContext<JobsyDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.MigrationsAssembly(typeof(JobsyDbContext).Assembly.FullName);
            }));

        var openAiBaseUrl = configuration.GetSection(OpenAiOptions.SectionName)["BaseUrl"]
            ?? "https://api.openai.com/v1/";
        services.AddHttpClient("OpenAI", client =>
        {
            client.BaseAddress = new Uri(openAiBaseUrl.EndsWith('/') ? openAiBaseUrl : openAiBaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        services.AddHttpClient("IntegrationProbe", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        services.AddHttpClient(SmtpEmailService.ResendHttpClientName, client =>
        {
            client.BaseAddress = new Uri(SmtpEmailService.DefaultResendApiBase);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<IRoutingService, MockRoutingService>();
        services.AddScoped<ISalaryService, SalaryService>();
        services.AddScoped<ICompanyAuthorizationService, CompanyAuthorizationService>();
        services.AddScoped<ICompanyApiKeyService, CompanyApiKeyService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<ITokenLedgerService, TokenLedgerService>();
        services.AddScoped<ITokenPurchaseInvoiceService, TokenPurchaseInvoiceService>();
        services.AddScoped<IVatBufferTransferService, VatBufferTransferService>();
        services.AddScoped<IPendingTokenActionService, PendingTokenActionService>();
        services.AddScoped<ITokenPurchaseFulfillmentService, TokenPurchaseFulfillmentService>();
        services.AddScoped<ITokenFinanceQueryService, TokenFinanceQueryService>();
        services.AddScoped<IVatDeclarationService, VatDeclarationService>();
        services.AddScoped<IVacancyProductService, VacancyProductService>();
        services.AddScoped<IVacancyDraftCreationService, VacancyDraftCreationService>();
        services.AddScoped<IMetricsQueryService, MetricsQueryService>();
        services.AddScoped<ICandidateMetricsQueryService, CandidateMetricsQueryService>();

        services.AddHttpClient(MolliePaymentService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        // Real Mollie when API key is configured; Development falls back to stub without a key.
        services.AddScoped<MolliePaymentStub>();
        services.AddScoped<IPaymentService, MolliePaymentService>();

        services.AddScoped<IKvkService, KvkServiceStub>();
        services.AddScoped<IKvkVerificationRetryService, KvkVerificationRetryService>();
        services.AddScoped<EmailServiceStub>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPushNotificationService, PushNotificationServiceStub>();
        services.AddScoped<IIntegrationHealthService, IntegrationHealthStub>();
        services.AddScoped<IIntegrationCredentialService, IntegrationCredentialService>();
        services.AddScoped<IPlatformFeatureService, PlatformFeatureService>();
        services.AddScoped<IPlatformCompanySettingsService, PlatformCompanySettingsService>();
        services.AddScoped<IAboutPageSettingsService, AboutPageSettingsService>();
        services.AddScoped<IRegionHostService, RegionHostService>();
        services.AddScoped<CompanyRegistrationService>();
        services.AddScoped<ICompanyRegistrationService>(sp => sp.GetRequiredService<CompanyRegistrationService>());
        services.AddScoped<ISalesManagerInviteService, SalesManagerInviteService>();
        services.AddScoped<ISalesManagerApplicationService, SalesManagerApplicationService>();
        services.AddScoped<ISalesManagerOnboardingService, SalesManagerOnboardingService>();
        services.AddScoped<IAmbassadeurInviteService, AmbassadeurInviteService>();
        services.AddScoped<IAmbassadeurOnboardingService, AmbassadeurOnboardingService>();
        services.AddScoped<IAmbassadeurSettingsService, AmbassadeurSettingsService>();
        services.AddScoped<IAmbassadeurAttributionService, AmbassadeurAttributionService>();
        services.AddScoped<IAmbassadeurDashboardService, AmbassadeurDashboardService>();
        services.AddScoped<IAmbassadeurFlyerPdfService, AmbassadeurFlyerPdfService>();
        services.AddScoped<ILobsyCvPdfService, LobsyCvPdfService>();
        services.AddScoped<ICandidateMapImageService, OsmTileMapImageService>();
        services.AddHttpClient("OsmTiles", OsmTileMapImageService.ConfigureHttpClient);
        services.AddScoped<ICommissionLedgerService, CommissionLedgerService>();
        services.AddScoped<IRevenueShareService, RevenueShareService>();
        services.AddScoped<ISupplierOnboardingPaymentService, SupplierOnboardingPaymentService>();
        services.AddScoped<ISelfBillingInvoiceService, SelfBillingInvoiceService>();
        services.AddScoped<ISalesManagerPayoutService, SalesManagerPayoutService>();
        services.AddScoped<ISalesManagerDashboardService, SalesManagerDashboardService>();
        services.AddScoped<ISalesCommercialService, SalesCommercialService>();
        services.AddScoped<IPartnerAffiliateService, PartnerAffiliateService>();
        services.AddScoped<IVacancyCategoryService, VacancyCategoryService>();
        services.AddScoped<IPartnerFlyerPdfService, PartnerFlyerPdfService>();
        services.AddScoped<IEmployerRaamflyerService, EmployerRaamflyerService>();
        services.AddScoped<IVacancyContentModerationService, VacancyContentModerationService>();
        services.AddScoped<IMockInterviewService, MockInterviewService>();
        services.AddScoped<IAssistantChatService, AssistantChatService>();
        services.AddScoped<ITranslationService, OpenAiTranslationService>();
        services.AddScoped<IPrivacyDataService, PrivacyDataService>();
        services.AddScoped<IExclusivitySettingService, ExclusivitySettingService>();
        services.AddHostedService<DataRetentionHostedService>();
        services.AddHostedService<UnconfirmedRegistrationCleanupHostedService>();
        services.AddHostedService<DraftVacancyCleanupHostedService>();
        services.AddHostedService<CompanyReengagementHostedService>();
        services.AddHostedService<VatBufferTransferHostedService>();
        services.AddHostedService<TokenCheckoutReconcileHostedService>();
        services.AddHostedService<KvkVerificationRetryHostedService>();

        return services;
    }

    internal static string? ResolveJobsyConnectionString(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("JobsyDb");
        if (!string.IsNullOrWhiteSpace(fromConfig))
        {
            return fromConfig;
        }

        // Render / Heroku-style fallback when a database is linked in the dashboard.
        return configuration["DATABASE_URL"];
    }

    /// <summary>
    /// Converts postgres:// URLs (Render) to Npgsql key=value form and trims quotes.
    /// </summary>
    internal static string NormalizePostgresConnectionString(string raw)
    {
        var value = raw.Trim().Trim('"', '\'');
        if (value.Length == 0)
        {
            throw new ArgumentException("Connection string is empty.");
        }

        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        var port = uri.Port > 0 ? uri.Port : 5432;

        // Render requires SSL for external hosts; Internal hostnames usually contain -a / dpg-
        var ssl = "SSL Mode=Require;Trust Server Certificate=true";

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};{ssl}";
    }
}
