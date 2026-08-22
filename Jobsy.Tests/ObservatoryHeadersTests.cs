using Jobsy.Core.Security;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

/// <summary>
/// Mozilla Observatory: HSTS ≥ six months, cookies always Secure on the public host,
/// and a single CSP header (no extra Blazor frame-ancestors policy).
/// </summary>
public class ObservatoryHeadersTests
{
    [Fact]
    public void Hsts_max_age_meets_observatory_six_month_floor()
    {
        Assert.True(JobsyHsts.MaxAgeSeconds >= JobsyHsts.ObservatoryMinimumSeconds);
        Assert.Equal(63_072_000, JobsyHsts.MaxAgeSeconds);

        var web = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Program.cs"));
        Assert.Contains("AddHsts", web);
        Assert.Contains("JobsyHsts.MaxAgeSeconds", web);
        Assert.Contains("IncludeSubDomains = true", web);

        var api = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Program.cs"));
        Assert.Contains("AddHsts", api);
        Assert.Contains("JobsyHsts.MaxAgeSeconds", api);
        Assert.Contains("IncludeSubDomains = true", api);
    }

    [Fact]
    public void Antiforgery_and_oauth_cookies_force_secure_outside_development()
    {
        var auth = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Auth", "AuthServiceCollectionExtensions.cs"));
        Assert.Contains("AddAntiforgery", auth);
        Assert.Contains("CookieSecurePolicy.Always", auth);
        Assert.Contains("CorrelationCookie.SecurePolicy", auth);
        Assert.Contains("NonceCookie.SecurePolicy", auth);
    }

    [Theory]
    [InlineData("lobsy.nl", false, null, true)]
    [InlineData("localhost", false, null, false)]
    [InlineData("localhost", false, "https", true)]
    [InlineData("127.0.0.1", true, null, true)]
    public void Cookie_secure_flag_follows_public_https(string host, bool https, string? forwardedProto, bool expected)
    {
        var http = new DefaultHttpContext();
        http.Request.Host = new HostString(host);
        http.Request.Scheme = https ? "https" : "http";
        http.Request.IsHttps = https;
        if (forwardedProto is not null)
        {
            http.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        }

        Assert.Equal(expected, JobsyCookie.ShouldMarkSecure(http));
    }

    [Fact]
    public void Consent_and_culture_js_set_secure_on_https()
    {
        var consent = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "cookieConsent.js"));
        Assert.Contains("location.protocol === \"https:\" ? \"; Secure\"", consent);

        var culture = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "culture.js"));
        Assert.Contains("location.protocol === \"https:\" ? \"; Secure\"", culture);

        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("location.protocol === \"https:\" ? \"; Secure\"", bundle);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
