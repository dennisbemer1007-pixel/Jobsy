using Jobsy.Api.Security;
using Jobsy.Core.Hosting;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public sealed class AcceptatieDiagnosticHeadersTests
{
    [Fact]
    public void Sets_header_on_acceptatie_only()
    {
        var http = new DefaultHttpContext();
        AcceptatieDiagnosticHeaders.TrySet(
            http.Response,
            new DeploymentEnvironmentLabel(DeploymentEnvironment.Acceptatie),
            new InvalidOperationException("seed fk missing"));
        Assert.True(http.Response.Headers.ContainsKey(AcceptatieDiagnosticHeaders.ErrorHeaderName));
    }

    [Fact]
    public void Skips_header_on_production()
    {
        var http = new DefaultHttpContext();
        AcceptatieDiagnosticHeaders.TrySet(
            http.Response,
            new DeploymentEnvironmentLabel(DeploymentEnvironment.Productie),
            new InvalidOperationException("hidden"));
        Assert.False(http.Response.Headers.ContainsKey(AcceptatieDiagnosticHeaders.ErrorHeaderName));
    }
}
