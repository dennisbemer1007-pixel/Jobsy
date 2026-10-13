namespace Jobsy.Core.Ops;

public enum TestAccountGuardCheck
{
    DeploymentMarker,
    SwitchOff,
    ServiceName,
    PublicHost,
    Database,
    Payments,
    HostEnvironment,
    ApiBaseUrl
}

public sealed record TestAccountGuardFailure(TestAccountGuardCheck Check, string Code, string Message);

public sealed class TestAccountGuardResult
{
    public bool Allowed { get; init; }
    public IReadOnlyList<TestAccountGuardFailure> Failures { get; init; } = [];

    public static TestAccountGuardResult Ok() => new() { Allowed = true };

    public static TestAccountGuardResult Refused(params TestAccountGuardFailure[] failures)
        => new() { Allowed = false, Failures = failures };

    public string CodesSummary()
        => string.Join(", ", Failures.Select(f => f.Code));
}

public sealed record TestAccountGuardInput
{
    public string? DeploymentEnvironment { get; init; }
    public bool TestAccountsEnabled { get; init; }
    public string? RenderServiceName { get; init; }
    public string? PublicWebHost { get; init; }
    public string? ApiBaseUrlHost { get; init; }
    public string? DatabaseName { get; init; }
    public string? DatabaseHost { get; init; }
    public bool ConnectionStringParseFailed { get; init; }
    public bool AllowStubPayments { get; init; }
    public bool HasLiveMollieKey { get; init; }
    public string? HostEnvironmentName { get; init; }
    public IReadOnlyList<string> AllowedPublicHosts { get; init; } = ["acceptatie.lobsy.nl"];
    public string ExpectedDatabaseName { get; init; } = "lobsy";
    public bool IsWebRuntime { get; init; }
}

/// <summary>
/// Pure acceptatie-only gate for the test-accounts CLI and runtime MFA exemption.
/// Messages never include configuration values.
/// </summary>
public static class TestAccountEnvironmentGuard
{
    private static readonly HashSet<string> ForbiddenPublicHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "lobsy.nl",
        "www.lobsy.nl"
    };

    public static TestAccountGuardResult Evaluate(TestAccountGuardInput input)
    {
        var failures = new List<TestAccountGuardFailure>();

        if (!string.Equals(input.DeploymentEnvironment, "Acceptatie", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new(
                TestAccountGuardCheck.DeploymentMarker,
                "deployment_marker",
                "Deployment marker is not Acceptatie."));
        }

        if (!input.TestAccountsEnabled)
        {
            failures.Add(new(
                TestAccountGuardCheck.SwitchOff,
                "switch_off",
                "TestAccounts switch is not enabled."));
        }

        if (string.IsNullOrWhiteSpace(input.RenderServiceName)
            || !input.RenderServiceName.StartsWith("lobsy-acc-", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new(
                TestAccountGuardCheck.ServiceName,
                "service_name",
                "Render service name is not an acceptatie service."));
        }

        if (input.IsWebRuntime)
        {
            if (!string.IsNullOrWhiteSpace(input.ApiBaseUrlHost)
                && input.ApiBaseUrlHost.Contains("jobsy-api", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(new(
                    TestAccountGuardCheck.ApiBaseUrl,
                    "api_base_url",
                    "API base URL points at a production service."));
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.PublicWebHost)
                || ForbiddenPublicHosts.Contains(input.PublicWebHost)
                || !input.AllowedPublicHosts.Any(h =>
                    string.Equals(h, input.PublicWebHost, StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(new(
                    TestAccountGuardCheck.PublicHost,
                    "public_host",
                    "Public web host is not an allowed acceptatie host."));
            }

            if (!input.AllowStubPayments || input.HasLiveMollieKey)
            {
                failures.Add(new(
                    TestAccountGuardCheck.Payments,
                    "payments",
                    "Payments configuration is not acceptatie-safe."));
            }
        }

        if (input.ConnectionStringParseFailed
            || string.IsNullOrWhiteSpace(input.DatabaseName)
            || string.Equals(input.DatabaseName, "jobsy", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                input.DatabaseName,
                input.ExpectedDatabaseName,
                StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new(
                TestAccountGuardCheck.Database,
                "database",
                "Database name is not the acceptatie database."));
        }

        if (string.Equals(input.HostEnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(new(
                TestAccountGuardCheck.HostEnvironment,
                "host_environment",
                "Host environment Development is refused (acceptatie-only)."));
        }

        return failures.Count == 0
            ? TestAccountGuardResult.Ok()
            : TestAccountGuardResult.Refused(failures.ToArray());
    }

    /// <summary>Minimal key=value parser so Core stays free of Npgsql.</summary>
    public static (string? Name, string? Host, bool ParseFailed) ParseKeyValueConnectionString(string value)
    {
        string? database = null;
        string? host = null;
        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = part[..eq].Trim();
            var val = part[(eq + 1)..].Trim();
            if (key.Equals("Database", StringComparison.OrdinalIgnoreCase))
            {
                database = val;
            }
            else if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)
                     || key.Equals("Server", StringComparison.OrdinalIgnoreCase))
            {
                host = val;
            }
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            return (null, host, true);
        }

        return (database, host, false);
    }

    public static (string? Name, string? Host, bool ParseFailed) TryParseDatabaseConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null, true);
        }

        try
        {
            var value = raw.Trim().Trim('"', '\'');
            if (value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(value);
                var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
                return (database, uri.Host, false);
            }

            return ParseKeyValueConnectionString(value);
        }
        catch
        {
            return (null, null, true);
        }
    }

    public static string? TryGetHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }
}
