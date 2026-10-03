using System.Net;
using System.Text.RegularExpressions;

namespace Jobsy.Tests.Errors;

public class StatusPagesTests
{
    [Fact]
    public async Task Unknown_html_page_answers_404_with_the_friendly_page()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/bestaat-niet");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Deze pagina bestaat niet", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_api_path_stays_a_bare_404()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/api/bestaat-niet");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Deze pagina bestaat niet", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_static_file_stays_a_bare_404()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/img/missing.png");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Direct_status_404_answers_404()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/status/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(
            "Deze pagina bestaat niet",
            await response.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(410, HttpStatusCode.Gone)]
    [InlineData(429, (HttpStatusCode)429)]
    [InlineData(503, HttpStatusCode.ServiceUnavailable)]
    [InlineData(418, HttpStatusCode.NotFound)]
    public async Task Direct_status_route_keeps_known_codes_and_falls_back_to_404(int code, HttpStatusCode expected)
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync($"/status/{code}");

        Assert.Equal(expected, response.StatusCode);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_vacancy_answers_404_and_noindex()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true, ApiAnswersNotFound = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync($"/vacancies/{Guid.NewGuid()}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Status_page_never_caches_and_links_the_three_actions()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/status/404");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/banenkaart\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/ontdek\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/hoe-werkt-lobsy\"", html, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(html, "<h1", RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task Status_page_swaps_banenkaart_for_paspoort_when_employers_are_off()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = false };
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync("/status/404");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Mijn Paspoort", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/banenkaart\"", html, StringComparison.Ordinal);
    }
}
