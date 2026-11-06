using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Scholen;

public sealed class PupilRequestApiTicketTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        var ctx = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ctx.Request.Headers.Cookie = $"{PupilApiSessionCookie.Name}=from-cookie";
        return ctx;
    }

    [Fact]
    public void GetForForwarding_reads_cookie_once_per_request()
    {
        var ticket = new PupilRequestApiTicket();
        var ctx = CreateHttpContext();

        Assert.Equal("from-cookie", ticket.GetForForwarding(ctx));
        Assert.Equal("from-cookie", ticket.GetForForwarding(ctx));

        ticket.ApplyRefresh("refreshed", ctx);
        Assert.Equal("refreshed", ticket.GetForForwarding(ctx));
    }

    [Fact]
    public void ApplyRefresh_sets_response_cookie_when_not_started()
    {
        var ticket = new PupilRequestApiTicket();
        var ctx = CreateHttpContext();

        ticket.ApplyRefresh("api-ticket-1", ctx);

        var setCookie = ctx.Response.Headers.SetCookie.ToString();
        Assert.StartsWith($"{PupilApiSessionCookie.Name}=", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public void Clear_drops_in_request_ticket()
    {
        var ticket = new PupilRequestApiTicket();
        var ctx = CreateHttpContext();
        ticket.ApplyRefresh("x", ctx);
        ticket.Clear();

        Assert.Null(ticket.GetForForwarding(ctx));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
