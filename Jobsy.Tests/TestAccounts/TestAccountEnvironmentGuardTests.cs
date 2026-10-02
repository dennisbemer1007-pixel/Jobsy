using Jobsy.Core.Ops;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountEnvironmentGuardTests
{
    private static TestAccountGuardInput ValidAcceptatie() => new()
    {
        DeploymentEnvironment = "Acceptatie",
        TestAccountsEnabled = true,
        RenderServiceName = "lobsy-acc-api",
        PublicWebHost = "acceptatie.lobsy.nl",
        DatabaseName = "lobsy",
        ConnectionStringParseFailed = false,
        AllowStubPayments = true,
        HasLiveMollieKey = false,
        HostEnvironmentName = "Production",
        AllowedPublicHosts = ["acceptatie.lobsy.nl"],
        ExpectedDatabaseName = "lobsy",
        IsWebRuntime = false
    };

    [Fact]
    public void Valid_acceptatie_input_is_allowed()
    {
        var result = TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie());
        Assert.True(result.Allowed);
        Assert.Empty(result.Failures);
    }

    [Theory]
    [InlineData("Production", "deployment_marker")]
    [InlineData("", "deployment_marker")]
    public void Deployment_marker_refuses(string marker, string code)
    {
        var input = ValidAcceptatie() with { DeploymentEnvironment = marker };
        var result = TestAccountEnvironmentGuard.Evaluate(input);
        Assert.False(result.Allowed);
        Assert.Contains(result.Failures, f => f.Code == code);
    }

    [Fact]
    public void Switch_off_refuses()
    {
        var result = TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie() with { TestAccountsEnabled = false });
        Assert.Contains(result.Failures, f => f.Code == "switch_off");
    }

    [Theory]
    [InlineData("jobsy-api")]
    [InlineData("jobsy-web")]
    [InlineData(null)]
    [InlineData("")]
    public void Service_name_refuses(string? name)
    {
        var result = TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie() with { RenderServiceName = name });
        Assert.Contains(result.Failures, f => f.Code == "service_name");
    }

    [Theory]
    [InlineData("lobsy.nl")]
    [InlineData("www.lobsy.nl")]
    [InlineData("evil.example")]
    [InlineData(null)]
    public void Public_host_refuses(string? host)
    {
        var result = TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie() with { PublicWebHost = host });
        Assert.Contains(result.Failures, f => f.Code == "public_host");
    }

    [Theory]
    [InlineData("jobsy")]
    [InlineData("other")]
    [InlineData(null)]
    public void Database_refuses(string? name)
    {
        var input = ValidAcceptatie() with
        {
            DatabaseName = name,
            ConnectionStringParseFailed = name is null
        };
        var result = TestAccountEnvironmentGuard.Evaluate(input);
        Assert.Contains(result.Failures, f => f.Code == "database");
    }

    [Fact]
    public void Live_mollie_or_no_stub_payments_refuses()
    {
        Assert.Contains(
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie() with { HasLiveMollieKey = true }).Failures,
            f => f.Code == "payments");
        Assert.Contains(
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatie() with { AllowStubPayments = false }).Failures,
            f => f.Code == "payments");
    }

    [Fact]
    public void Development_host_environment_refuses()
    {
        var result = TestAccountEnvironmentGuard.Evaluate(
            ValidAcceptatie() with { HostEnvironmentName = "Development" });
        Assert.Contains(result.Failures, f => f.Code == "host_environment");
    }

    [Fact]
    public void Full_production_profile_refuses_every_check()
    {
        var input = new TestAccountGuardInput
        {
            DeploymentEnvironment = "Production",
            TestAccountsEnabled = false,
            RenderServiceName = "jobsy-api",
            PublicWebHost = "lobsy.nl",
            DatabaseName = "jobsy",
            AllowStubPayments = false,
            HasLiveMollieKey = true,
            HostEnvironmentName = "Production",
            IsWebRuntime = false
        };
        var result = TestAccountEnvironmentGuard.Evaluate(input);
        Assert.False(result.Allowed);
        Assert.Contains(result.Failures, f => f.Code == "deployment_marker");
        Assert.Contains(result.Failures, f => f.Code == "switch_off");
        Assert.Contains(result.Failures, f => f.Code == "service_name");
        Assert.Contains(result.Failures, f => f.Code == "public_host");
        Assert.Contains(result.Failures, f => f.Code == "database");
        Assert.Contains(result.Failures, f => f.Code == "payments");
    }

    [Fact]
    public void Messages_never_contain_input_values()
    {
        const string fakeConn = "Host=prod-secret-db;Database=jobsy;Password=super-secret-xyz";
        const string fakeKey = "live_secret_mollie_key_abc";
        var (name, host, failed) = TestAccountEnvironmentGuard.TryParseDatabaseConnectionString(fakeConn);
        var input = ValidAcceptatie() with
        {
            DeploymentEnvironment = "Production",
            DatabaseName = name,
            DatabaseHost = host,
            ConnectionStringParseFailed = failed,
            HasLiveMollieKey = true,
            RenderServiceName = "jobsy-api",
            PublicWebHost = "lobsy.nl"
        };
        var result = TestAccountEnvironmentGuard.Evaluate(input);
        var blob = string.Join(' ', result.Failures.Select(f => f.Message));
        Assert.DoesNotContain("super-secret-xyz", blob, StringComparison.Ordinal);
        Assert.DoesNotContain(fakeKey, blob, StringComparison.Ordinal);
        Assert.DoesNotContain("prod-secret-db", blob, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_runtime_refuses_production_api_host()
    {
        var input = ValidAcceptatie() with
        {
            IsWebRuntime = true,
            ApiBaseUrlHost = "jobsy-api.onrender.com",
            PublicWebHost = null,
            AllowStubPayments = false
        };
        var result = TestAccountEnvironmentGuard.Evaluate(input);
        Assert.Contains(result.Failures, f => f.Code == "api_base_url");
    }
}
