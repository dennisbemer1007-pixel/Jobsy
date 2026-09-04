using Jobsy.Api.Security;
using Jobsy.Core.Privacy;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public class ProductionAuditTests
{
    [Fact]
    public void Anonymous_key_accepts_guid_and_legacy_anon_prefix()
    {
        Assert.True(AnonymousKeyRules.IsValid("anon-" + Guid.NewGuid()));
        Assert.True(AnonymousKeyRules.IsValid("anon-legacy12"));
        Assert.False(AnonymousKeyRules.IsValid(""));
        Assert.False(AnonymousKeyRules.IsValid("nope"));
        Assert.False(AnonymousKeyRules.IsValid(new string('a', 200)));
    }

    [Fact]
    public void Analytics_consent_accepts_header_or_cookie()
    {
        var viaHeader = new DefaultHttpContext();
        viaHeader.Request.Headers[CookieConsentNames.HeaderName] = CookieConsentNames.AnalyticsValue;
        Assert.True(AnalyticsConsent.IsGranted(viaHeader.Request));

        var denied = new DefaultHttpContext();
        Assert.False(AnalyticsConsent.IsGranted(denied.Request));
    }

    [Fact]
    public void Iban_masking_keeps_stored_value_for_masked_or_empty_input()
    {
        const string stored = "NL91KNAB0417164300";
        Assert.Equal("NL**4300", IbanMasking.ForApi(stored));
        Assert.Equal(stored, IbanMasking.ResolveStoredIban(null, stored));
        Assert.Equal(stored, IbanMasking.ResolveStoredIban("NL**4300", stored));
        Assert.Equal("NL20INGB0001234567", IbanMasking.ResolveStoredIban("NL20INGB0001234567", stored));
        Assert.False(IbanMasking.IsFullIbanInput("—"));
    }

    [Fact]
    public void Cookie_consent_token_roundtrips_and_rejects_tampering()
    {
        const string secret = "consent-test-secret";
        var token = CookieConsentToken.Create(secret);
        Assert.True(CookieConsentToken.IsValid(token, secret));
        Assert.True(CookieConsentToken.AllowsAnalyticsChoice(token));
        Assert.False(CookieConsentToken.IsValid(token + "x", secret));
        Assert.False(CookieConsentToken.IsValid(CookieConsentNames.AnalyticsValue, secret));
    }

    [Fact]
    public void Demo_login_is_gated_on_allow_development_auth()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Auth", "AuthServiceCollectionExtensions.cs"));
        var demoIdx = src.IndexOf("MapPost(\"/account/demo-login\"", StringComparison.Ordinal);
        Assert.True(demoIdx > 0);
        var slice = src[demoIdx..Math.Min(src.Length, demoIdx + 1800)];
        Assert.Contains("IsDemoLoginEnabled", slice);
        Assert.Contains("AllowDevelopmentAuth", src);
    }

    [Fact]
    public void Production_seed_is_not_tied_to_allow_development_auth()
    {
        var hosted = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Jobs", "DatabaseSeedHostedService.cs"));
        Assert.Contains("Seed:Enabled", hosted);
        Assert.Contains("PreferWipeOverSeed", hosted);
        Assert.Contains("PurgeDemoDataAsync", hosted);
        Assert.DoesNotContain("JobsyAuth:AllowDevelopmentAuth", hosted);

        var purge = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "DemoDataPurge.cs"));
        Assert.Contains("Seed:PurgeDemoData", purge);
        Assert.Contains("RENDER_SERVICE_NAME", purge);
        Assert.Contains("jobsy-api", purge);
        Assert.Contains("IsLiveProductionSite", purge);
        Assert.Contains("admin@jobsy.local", purge);
    }

    [Fact]
    public void Public_vacancy_reads_are_rate_limited()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Controllers", "VacanciesController.cs"));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", src);
        Assert.Contains("[EnableRateLimiting(\"public-travel\")]", src);
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Program.cs"));
        Assert.Contains("public-travel", program);

        var companies = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api", "Controllers", "PublicCompaniesController.cs"));
        Assert.Contains("[EnableRateLimiting(\"public-read\")]", companies);
        Assert.DoesNotContain("[EnableRateLimiting(\"public-write\")]", companies);
    }

    [Fact]
    public void Cookie_consent_js_writes_first_party_cookie()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "cookieConsent.js"));
        Assert.Contains("document.cookie", js);
        Assert.Contains("Jobsy.CookieConsent", js);
        Assert.Contains("; Secure", js);
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
