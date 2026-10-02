using System.Net;
using Jobsy.Core.Authorization;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Errors 02.3 (Dependency F present): <c>reason=employers-off</c> swaps in its own copy and
/// drops the switch-account button; "Naar mijn start" only shows for roles with a candidate
/// home, otherwise "Naar de voorpagina".
/// </summary>
public class EmployersOffReasonTests
{
    [Fact]
    public async Task Employers_off_reason_shows_its_own_copy_without_a_switch_button()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var response = await client.GetAsync("/access-denied?reason=employers-off");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(
            "Werkgevers kunnen Lobsy nu even niet gebruiken. We laten het je weten als het weer kan.",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Inloggen met een ander account", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Candidate_gets_naar_mijn_start_because_a_candidate_home_exists()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var html = await (await client.GetAsync("/access-denied?reason=employers-off"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Naar mijn start", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Naar de voorpagina", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Employer_side_only_role_gets_naar_de_voorpagina_instead()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateSignedInClient(JobsyRoles.BranchManager);

        var html = await (await client.GetAsync("/access-denied?reason=employers-off"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Naar de voorpagina", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_reason_falls_back_to_the_role_copy()
    {
        await using var factory = new ForbiddenWebFactory();
        using var client = factory.CreateSignedInClient(JobsyRoles.Candidate);

        var html = await (await client.GetAsync("/access-denied?reason=something-else"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Deze pagina is niet voor jouw account", html, StringComparison.Ordinal);
        Assert.Contains("Inloggen met een ander account", html, StringComparison.Ordinal);
    }
}
