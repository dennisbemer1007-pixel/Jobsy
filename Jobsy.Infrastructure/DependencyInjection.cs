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
        var connectionString = configuration.GetConnectionString("JobsyDb");
        var isDev = environment?.IsDevelopment() ?? true;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (!isDev)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:JobsyDb is required outside Development.");
            }

            connectionString =
                "Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres";
        }
        else if (!isDev
                 && connectionString.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Default postgres password is not allowed outside Development.");
        }

        services.AddDataProtection();
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

        services.AddScoped<IRoutingService, MockRoutingService>();
        services.AddScoped<ISalaryService, SalaryService>();
        services.AddScoped<ICompanyAuthorizationService, CompanyAuthorizationService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<ITokenLedgerService, TokenLedgerService>();
        services.AddScoped<IVacancyProductService, VacancyProductService>();
        services.AddScoped<IMetricsQueryService, MetricsQueryService>();
        services.AddScoped<ICandidateMetricsQueryService, CandidateMetricsQueryService>();

        if (isDev)
        {
            services.AddScoped<IPaymentService, MolliePaymentStub>();
        }
        else
        {
            services.AddScoped<IPaymentService, DisabledPaymentService>();
        }

        services.AddScoped<IKvkService, KvkServiceStub>();
        services.AddScoped<IEmailService, EmailServiceStub>();
        services.AddScoped<IPushNotificationService, PushNotificationServiceStub>();
        services.AddScoped<IIntegrationHealthService, IntegrationHealthStub>();
        services.AddScoped<IIntegrationCredentialService, IntegrationCredentialService>();
        services.AddScoped<IPlatformFeatureService, PlatformFeatureService>();
        services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();
        services.AddScoped<IVacancyContentModerationService, VacancyContentModerationService>();
        services.AddScoped<IMockInterviewService, MockInterviewService>();
        services.AddScoped<ITranslationService, TranslationServiceStub>();
        services.AddScoped<IPrivacyDataService, PrivacyDataService>();
        services.AddHostedService<DataRetentionHostedService>();

        return services;
    }
}
