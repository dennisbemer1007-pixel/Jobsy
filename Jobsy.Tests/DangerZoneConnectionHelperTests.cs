using System.Net.Http;
using Jobsy.Web.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public sealed class DangerZoneConnectionHelperTests
{
    [Fact]
    public void Transport_failure_includes_http_and_circuit_gone()
    {
        Assert.True(DangerZoneConnectionHelper.IsTransportOrCircuitFailure(new HttpRequestException()));
        Assert.True(DangerZoneConnectionHelper.IsTransportOrCircuitFailure(new JSDisconnectedException("gone")));
        Assert.False(DangerZoneConnectionHelper.IsTransportOrCircuitFailure(new InvalidOperationException()));
    }
}
