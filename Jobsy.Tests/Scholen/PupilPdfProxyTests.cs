using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Hosting;
using Jobsy.Web.Security;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Pupil story links point at <c>/leerling/pdf</c> on the web origin. That path exists on the
/// API only, so the web host proxies it and keeps it behind the pupil session and the schools gate.
/// </summary>
public class PupilPdfProxyTests
{
    private static readonly byte[] PdfBytes = "%PDF-1.4 verhaal"u8.ToArray();

    [Fact]
    public void Pupil_cookie_is_forwarded_for_the_story_pdf_and_pupil_api()
    {
        Assert.True(JobsyApiAuthHandler.ForwardsPupilCookie("/api/pupil/progress"));
        Assert.True(JobsyApiAuthHandler.ForwardsPupilCookie("/leerling/pdf"));
        Assert.False(JobsyApiAuthHandler.ForwardsPupilCookie("/api/school/classes"));
        Assert.False(JobsyApiAuthHandler.ForwardsPupilCookie("/leerling"));
    }

    [Fact]
    public void Web_host_maps_the_pupil_pdf_proxy()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Program.cs"));
        Assert.Contains("MapPupilPdfEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signed_in_pupil_receives_the_proxied_pdf()
    {
        await using var app = await StartAsync(schoolsEnabled: true);
        var client = app.GetTestClient();
        var cookie = await SignInAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Get, "/leerling/pdf");
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Contains("verhaal.pdf", response.Content.Headers.ContentDisposition?.FileName, StringComparison.Ordinal);
        Assert.Equal(PdfBytes, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("/leerling/pdf", app.Services.GetRequiredService<PdfHandler>().Path);
    }

    [Fact]
    public async Task Anonymous_pdf_request_goes_to_the_pupil_login()
    {
        await using var app = await StartAsync(schoolsEnabled: true);
        var client = app.GetTestClient();
        var response = await client.GetAsync("/leerling/pdf");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        var path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
        Assert.StartsWith("/leerling", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Schools_off_pdf_request_goes_to_the_friendly_page()
    {
        await using var app = await StartAsync(schoolsEnabled: false);
        var client = app.GetTestClient();
        var response = await client.GetAsync("/leerling/pdf");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(FeatureRoutes.SchoolsOffAccessDeniedPath, response.Headers.Location?.OriginalString);
    }

    private static async Task<string> SignInAsync(HttpClient client)
    {
        var signIn = await client.GetAsync("/leerling/sign-in");
        Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
        Assert.True(signIn.Headers.TryGetValues("Set-Cookie", out var setCookie));
        return string.Join("; ", setCookie!.Select(c => c.Split(';', 2)[0]));
    }

    private static async Task<WebApplication> StartAsync(bool schoolsEnabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication()
            .AddCookie(PupilAuthDefaults.Scheme, options =>
            {
                options.Cookie.Name = PupilAuthDefaults.CookieName;
                options.LoginPath = "/leerling";
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(JobsyPolicies.PupilSession, policy =>
            {
                policy.AddAuthenticationSchemes(PupilAuthDefaults.Scheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(PupilClaimTypes.PupilCodeId);
                policy.RequireClaim(PupilClaimTypes.ClassId);
                policy.RequireClaim(PupilClaimTypes.SchoolId);
                policy.RequireClaim(PupilClaimTypes.SessionVersion);
                policy.RequireClaim(PupilClaimTypes.IssuedAt);
            });
        });
        var handler = new PdfHandler();
        builder.Services.AddSingleton(handler);
        builder.Services.AddSingleton(new JobsyApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") }));
        builder.Services.AddSingleton<IFeatureFlags>(new FixedFlags(schoolsEnabled));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseMiddleware<SchoolsFeatureMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<LeerlingNoStoreMiddleware>();
        app.MapGet("/leerling/sign-in", async (HttpContext ctx) =>
        {
            await ctx.SignInAsync(PupilAuthDefaults.Scheme, PupilPrincipal());
            return Results.NoContent();
        });
        app.MapPupilPdfEndpoints();
        await app.StartAsync();
        return app;
    }

    private static ClaimsPrincipal PupilPrincipal()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(PupilClaimTypes.PupilCodeId, Guid.NewGuid().ToString("D")),
                new Claim(PupilClaimTypes.ClassId, Guid.NewGuid().ToString("D")),
                new Claim(PupilClaimTypes.SchoolId, Guid.NewGuid().ToString("D")),
                new Claim(PupilClaimTypes.SessionVersion, "1"),
                new Claim(PupilClaimTypes.IssuedAt, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(PupilClaimTypes.ClassLabel, "1A"),
                new Claim(PupilClaimTypes.CodeDisplay, "ABC-123")
            ],
            PupilAuthDefaults.Scheme);
        return new ClaimsPrincipal(identity);
    }

    private sealed class FixedFlags(bool schools) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(false, true, SchoolsEnabled: schools));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.Schools && schools);

        public void Invalidate()
        {
        }
    }

    public sealed class PdfHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(PdfBytes)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "verhaal.pdf"
            };
            return Task.FromResult(response);
        }
    }
}
