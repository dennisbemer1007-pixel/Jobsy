namespace Jobsy.Core.Ops;

/// <summary>
/// Builds <see cref="TestAccountGuardInput"/> without depending on configuration packages.
/// </summary>
public static class TestAccountGuardInputBuilder
{
    public static TestAccountGuardInput Create(
        string? deploymentEnvironment,
        bool testAccountsEnabled,
        string? renderServiceName,
        string? publicWebBaseUrl,
        string? apiBaseUrl,
        string? connectionString,
        bool allowStubPayments,
        bool hasLiveMollieKey,
        string? hostEnvironmentName,
        IReadOnlyList<string>? allowedPublicHosts = null,
        string? expectedDatabaseName = null,
        bool isWebRuntime = false)
    {
        var (dbName, dbHost, parseFailed) =
            TestAccountEnvironmentGuard.TryParseDatabaseConnectionString(connectionString);

        return new TestAccountGuardInput
        {
            DeploymentEnvironment = deploymentEnvironment,
            TestAccountsEnabled = testAccountsEnabled,
            RenderServiceName = renderServiceName,
            PublicWebHost = TestAccountEnvironmentGuard.TryGetHost(publicWebBaseUrl),
            ApiBaseUrlHost = TestAccountEnvironmentGuard.TryGetHost(apiBaseUrl),
            DatabaseName = dbName,
            DatabaseHost = dbHost,
            ConnectionStringParseFailed = parseFailed,
            AllowStubPayments = allowStubPayments,
            HasLiveMollieKey = hasLiveMollieKey,
            HostEnvironmentName = hostEnvironmentName,
            AllowedPublicHosts = allowedPublicHosts ?? ["acceptatie.lobsy.nl"],
            ExpectedDatabaseName = expectedDatabaseName ?? "lobsy",
            IsWebRuntime = isWebRuntime
        };
    }
}
