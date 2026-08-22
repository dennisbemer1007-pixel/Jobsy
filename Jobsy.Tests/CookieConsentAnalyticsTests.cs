namespace Jobsy.Tests;

/// <summary>
/// Guards ePrivacy wiring: optional analytics must stay behind Jobsy.CookieConsent.
/// </summary>
public class CookieConsentAnalyticsTests
{
    [Fact]
    public void Cookie_consent_helper_exposes_analytics_gate()
    {
        var root = FindRepoRoot();
        var consentJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "cookieConsent.js"));
        Assert.Contains("Jobsy.CookieConsent", consentJs);
        Assert.Contains("allowsAnalytics", consentJs);
        Assert.Contains("analytics", consentJs);
    }

    [Fact]
    public void Geo_anonymous_key_requires_analytics_consent()
    {
        var root = FindRepoRoot();
        var geoJs = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "geo.js"));
        Assert.Contains("analyticsAllowed", geoJs);
        Assert.Contains("getOrCreateAnonymousKey", geoJs);
        Assert.Contains("jobsyCookieConsent", geoJs);
    }

    [Fact]
    public void Api_client_gates_engagement_recording_on_consent()
    {
        var root = FindRepoRoot();
        var client = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Services", "JobsyApiClient.cs"));
        Assert.Contains("AllowsAnalyticsAsync", client);

        var impressionsIdx = client.IndexOf("public async Task RecordImpressionsAsync", StringComparison.Ordinal);
        var clicksIdx = client.IndexOf("public async Task RecordClickOnceAsync", StringComparison.Ordinal);
        var visitsIdx = client.IndexOf("public async Task RecordSiteVisitOnceAsync", StringComparison.Ordinal);
        Assert.True(impressionsIdx > 0 && clicksIdx > 0 && visitsIdx > 0);

        static bool MethodGatesConsent(string source, int methodStart)
        {
            var nextMethod = source.IndexOf("public async Task", methodStart + 10, StringComparison.Ordinal);
            var slice = nextMethod > methodStart
                ? source[methodStart..nextMethod]
                : source[methodStart..];
            return slice.Contains("AllowsAnalyticsAsync", StringComparison.Ordinal);
        }

        Assert.True(MethodGatesConsent(client, clicksIdx));
        Assert.True(MethodGatesConsent(client, impressionsIdx));
        Assert.True(MethodGatesConsent(client, visitsIdx));
        Assert.Contains("CookieConsentNames.HeaderName", client);
        Assert.Contains("jobsyCookieConsent.get", client);
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
