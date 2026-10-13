using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Jobsy.Web.Auth;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// <c>GET /privacy/data/export</c> (public-pages 07): anonymous visitors are challenged to log
/// in, signed-in visitors always get a downloaded file (never JSON rendered on a page), and a
/// failed upstream call redirects back to the page with a translated message — never a raw error.
/// </summary>
public class PrivacyDataExportTests
{
    [Fact]
    public async Task Anonymous_request_is_redirected_to_login()
    {
        await using var app = await BuildAppAsync(signedInUserId: null, new StubExportHandler(HttpStatusCode.OK, "{}"));
        var client = app.GetTestServer().CreateClient();

        using var response = await client.GetAsync("/privacy/data/export");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Signed_in_request_downloads_a_dated_attachment_with_no_store()
    {
        var userId = Guid.NewGuid();
        await using var app = await BuildAppAsync(
            userId,
            new StubExportHandler(HttpStatusCode.OK, """{"exportedAtUtc":"2026-10-01T00:00:00Z"}"""));
        var client = app.GetTestServer().CreateClient();

        using var response = await client.GetAsync("/privacy/data/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Matches(
            @"lobsy-mijn-gegevens-\d{4}-\d{2}-\d{2}\.json",
            response.Content.Headers.ContentDisposition!.FileName ?? "");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("exportedAtUtc", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Api_failure_redirects_back_to_the_page_with_a_flag_not_a_raw_error()
    {
        var userId = Guid.NewGuid();
        await using var app = await BuildAppAsync(
            userId,
            new StubExportHandler(HttpStatusCode.InternalServerError, "boom"));
        var client = app.GetTestServer().CreateClient();

        using var response = await client.GetAsync("/privacy/data/export");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.Equal("/privacy/data?export=failed", location);
        Assert.DoesNotContain("boom", location, StringComparison.Ordinal);
    }

    private static async Task<WebApplication> BuildAppAsync(Guid? signedInUserId, StubExportHandler handler)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => o.LoginPath = "/login");
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<JobsyAccessTokenIssuer>();
        builder.Services.AddHttpClient(PrivacyDataExportEndpoints.HttpClientName, client =>
            {
                client.BaseAddress = new Uri("http://localhost:5200/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        builder.Services.AddRateLimiter(o => o.AddPolicy("export", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 1000, Window = TimeSpan.FromMinutes(1) })));

        var app = builder.Build();
        app.UseRouting();

        if (signedInUserId is { } userId)
        {
            // Simulates an already-authenticated cookie session without standing up the full
            // Jobsy cookie/device-session pipeline — only HttpContext.User matters downstream.
            app.Use((ctx, next) =>
            {
                ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, "kandidaat@test.nl")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme));
                return next();
            });
        }
        else
        {
            app.UseAuthentication();
        }

        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapPrivacyDataExportEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class StubExportHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(request.Headers.Authorization);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
