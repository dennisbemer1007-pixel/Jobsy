using Jobsy.Web.Localization;
using Jobsy.Web.Security;

namespace Jobsy.Tests;

/// <summary>
/// Guards against the Checkmarx/ZAP findings that are actually product issues
/// (error disclosure, loose CSP hosts, missing nosniff on static files).
/// Informational ZAP rows (auth detection, modern-app, session cookie id) stay as-is.
/// </summary>
public class ZapFindingsTests
{
    [Fact]
    public void Public_pages_do_not_render_exception_messages()
    {
        var root = FindRepoRoot();
        var company = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "CompanyPublicPage.razor"));
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        var client = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Services", "JobsyApiClient.cs"));

        Assert.DoesNotContain("Format(\"Discovery.LoadFailed\", ex.Message)", company);
        Assert.DoesNotContain("Format(\"Discovery.LoadFailed\", ex.Message)", discovery);
        Assert.Contains("Culture[\"Discovery.LoadFailed\"]", company);
        Assert.Contains("Culture[\"Discovery.LoadFailed\"]", discovery);
        Assert.Contains("StatusCodes.Status404NotFound", company);
        Assert.Contains("ReadPublicCompanyOrNullAsync", client);
        Assert.DoesNotContain("EnsureSuccessStatusCode", SliceAround(client, "GetPublicCompanyByKvkAsync", 900));
        Assert.DoesNotContain("EnsureSuccessStatusCode", SliceAround(client, "GetPublicCompanyByVestigingAsync", 900));
    }

    [Fact]
    public void Load_failed_copy_has_no_debug_placeholder()
    {
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var text = UiStrings.Get("Discovery.LoadFailed", lang);
            Assert.DoesNotContain("{0}", text);
            Assert.DoesNotContain("Internal Server Error", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Start de API", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Error_page_is_generic_and_has_a_correlation_id()
    {
        var error = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Error.razor"));
        Assert.DoesNotContain("Internal Server Error", error);
        Assert.DoesNotContain("Exception", error);
        Assert.DoesNotContain("stack", error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TraceIdentifier", error);
        Assert.Contains("Referentie:", error);
    }

    [Fact]
    public void Csp_has_no_scheme_wildcards_on_img_or_connect()
    {
        var csp = JobsyContentSecurityPolicy.ForWeb("abc");
        Assert.DoesNotContain("img-src 'self' data: https:", csp);
        Assert.DoesNotContain("connect-src 'self' wss: ws: https:", csp);
        Assert.Contains("https://tiles.openfreemap.org", csp);
        Assert.Contains("https://picsum.photos", csp);
        Assert.Contains("'unsafe-eval'", csp);
    }

    [Fact]
    public void App_core_comment_does_not_trip_user_word_scan()
    {
        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.False(
            System.Text.RegularExpressions.Regex.IsMatch(
                bundle,
                @"\buser\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        Assert.Contains("Mijn locatie", bundle);
    }

    [Fact]
    public void Login_sanitizes_return_url_before_attributes()
    {
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Login.razor"));
        Assert.Contains("AuthRedirects.ResolveRequestedReturnUrl", login);
        Assert.Contains("value=\"@_returnUrl\"", login);
    }

    private static string SliceAround(string source, string marker, int length)
    {
        var idx = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, marker);
        return source[idx..Math.Min(source.Length, idx + length)];
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
