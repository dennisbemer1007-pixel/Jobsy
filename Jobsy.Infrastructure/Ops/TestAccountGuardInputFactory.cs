using Jobsy.Core.Ops;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Infrastructure.Ops;

public static class TestAccountGuardInputFactory
{
    public static TestAccountGuardInput FromConfiguration(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        bool isWebRuntime = false,
        bool? hasLiveMollieKey = null)
    {
        var raw = configuration.GetConnectionString("JobsyDb")
                  ?? configuration["DATABASE_URL"];
        var (dbName, dbHost, parseFailed) =
            TestAccountEnvironmentGuard.TryParseDatabaseConnectionString(raw);
        var publicUrl = configuration["PublicWebBaseUrl"];
        var apiUrl = configuration["ApiBaseUrl"] ?? configuration["JobsyApi:BaseUrl"];
        var allowed = configuration.GetSection("TestAccounts:AllowedPublicHosts").Get<string[]>()
                      ?? ["acceptatie.lobsy.nl"];
        var expectedDb = configuration["TestAccounts:ExpectedDatabaseName"] ?? "lobsy";

        return new TestAccountGuardInput
        {
            DeploymentEnvironment = configuration["Lobsy:DeploymentEnvironment"],
            TestAccountsEnabled = string.Equals(
                configuration["TestAccounts:Enabled"], "true", StringComparison.OrdinalIgnoreCase)
                || configuration.GetValue("TestAccounts:Enabled", false),
            RenderServiceName = configuration["RENDER_SERVICE_NAME"],
            PublicWebHost = TestAccountEnvironmentGuard.TryGetHost(publicUrl),
            ApiBaseUrlHost = TestAccountEnvironmentGuard.TryGetHost(apiUrl),
            DatabaseName = dbName,
            DatabaseHost = dbHost,
            ConnectionStringParseFailed = parseFailed,
            AllowStubPayments = configuration.GetValue("JobsyAuth:AllowStubPayments", false),
            HasLiveMollieKey = hasLiveMollieKey
                ?? LooksLikeLiveMollieKey(configuration["Mollie:ApiKey"]
                    ?? configuration["Integrations:Mollie:ApiKey"]),
            HostEnvironmentName = hostEnvironment.EnvironmentName,
            AllowedPublicHosts = allowed,
            ExpectedDatabaseName = expectedDb,
            IsWebRuntime = isWebRuntime
        };
    }

    private static bool LooksLikeLiveMollieKey(string? key)
        => !string.IsNullOrWhiteSpace(key)
           && key.TrimStart().StartsWith("live_", StringComparison.Ordinal);
}
