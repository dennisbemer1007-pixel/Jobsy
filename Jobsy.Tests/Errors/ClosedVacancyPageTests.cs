using System.Net;

namespace Jobsy.Tests.Errors;

/// <summary>
/// The vacancy detail page's closed-vacancy content (errors 03 / er-d05): a real 410, the
/// title/city and up to 3 similar cards in the prerendered HTML, noindex, and no JobPosting
/// JSON-LD. A draft (never-public) vacancy still answers a plain 404.
/// </summary>
public class ClosedVacancyPageTests
{
    [Fact]
    public async Task Closed_vacancy_page_answers_410_with_title_city_and_noindex()
    {
        await using var factory = new ClosedVacancyWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync($"/vacancies/{factory.Api.ArchivedId:D}");
        Assert.Equal((HttpStatusCode)410, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Deze vacature is gesloten", html, StringComparison.Ordinal);
        Assert.Contains("Archiefbaan Magazijn", html, StringComparison.Ordinal);
        Assert.Contains("Den Haag", html, StringComparison.Ordinal);

        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").First(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Closed_vacancy_page_shows_up_to_three_similar_cards_in_prerendered_html()
    {
        await using var factory = new ClosedVacancyWebFactory();
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync($"/vacancies/{factory.Api.ArchivedId:D}")).Content.ReadAsStringAsync();

        Assert.Contains("data-testid=\"closed-vacancy-similar\"", html, StringComparison.Ordinal);
        Assert.Contains("Vulploeg Vlakbij", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Vulploeg Ver Weg", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Closed_vacancy_page_has_no_jobposting_json_ld()
    {
        await using var factory = new ClosedVacancyWebFactory();
        using var client = factory.CreateHtmlClient();

        var html = await (await client.GetAsync($"/vacancies/{factory.Api.ArchivedId:D}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"@type\":\"JobPosting\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("\"@type\": \"JobPosting\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Draft_never_public_vacancy_still_answers_404()
    {
        await using var factory = new ClosedVacancyWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync($"/vacancies/{factory.Api.DraftId:D}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_vacancy_page_still_answers_200()
    {
        await using var factory = new ClosedVacancyWebFactory();
        using var client = factory.CreateHtmlClient();

        var response = await client.GetAsync($"/vacancies/{factory.Api.PublicId:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
