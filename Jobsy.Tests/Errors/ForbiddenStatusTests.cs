using System.Net;
using Jobsy.Core.Authorization;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Errors 02: a real 403 at the requested URL (no 302 to /access-denied), the account from
/// claims only, and the usual 302-to-login for anonymous visitors stays unchanged.
/// </summary>
public class ForbiddenStatusTests
{
    [Fact]
    public async Task Candidate_gets_a_real_403_on_an_admin_page_without_redirect()
    {
        await using var factory = new ForbiddenWebFactory { EmployersEnabled = true };
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var response = await client.GetAsync("/admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("Deze pagina is niet voor jouw account", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Candidate_sees_the_masked_account_and_role_from_claims()
    {
        await using var factory = new ForbiddenWebFactory { EmployersEnabled = true };
        using var client = factory.CreateSignedInClient(
            JobsyRoles.Candidate,
            email: "kim.kandidaat@example.nl",
            name: "Kim Kandidaat");

        var html = await (await client.GetAsync("/admin")).Content.ReadAsStringAsync();

        Assert.Contains("Kim Kandidaat", html, StringComparison.Ordinal);
        Assert.Contains("k***@example.nl", html, StringComparison.Ordinal);
        Assert.DoesNotContain("kim.kandidaat@example.nl", html, StringComparison.Ordinal);
        Assert.Contains("Kandidaat", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonymous_visitor_still_gets_302_to_login_unchanged()
    {
        await using var factory = new ForbiddenWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/login?returnUrl=", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AccessDenied_endpoint_answers_403_and_reads_ReturnUrl()
    {
        await using var factory = new ForbiddenWebFactory { EmployersEnabled = true };
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var response = await client.GetAsync("/access-denied?ReturnUrl=%2Fadmin%2Fusers");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value=\"/admin/users\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessDenied_endpoint_accepts_lowercase_returnUrl_too()
    {
        await using var factory = new ForbiddenWebFactory { EmployersEnabled = true };
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var html = await (await client.GetAsync("/access-denied?returnUrl=%2Fadmin%2Fusers"))
            .Content.ReadAsStringAsync();

        Assert.Contains("value=\"/admin/users\"", html, StringComparison.Ordinal);
    }
}
