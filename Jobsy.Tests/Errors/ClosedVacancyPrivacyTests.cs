using System.Net;

namespace Jobsy.Tests.Errors;

/// <summary>
/// The 410 JSON for a closed, intermediary-hidden vacancy must never contain the end-client
/// (or intermediary) company name — only city/category, same rule as the candidate banenkaart.
/// </summary>
public class ClosedVacancyPrivacyTests : IClassFixture<ClosedVacancyApiFactory>
{
    private readonly ClosedVacancyApiFactory _factory;

    public ClosedVacancyPrivacyTests(ClosedVacancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    [Fact]
    public async Task Hidden_mode_closed_vacancy_410_never_names_the_company()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.HiddenIntermediaryClosedId:D}");
        Assert.Equal((HttpStatusCode)410, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Test Vestiging Gesloten", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Geheim Uitzendbureau", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("companyName", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Marktstraat", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Hidden_mode_closed_vacancy_410_response_is_noindex()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.HiddenIntermediaryClosedId:D}");

        Assert.True(response.Headers.TryGetValues("X-Robots-Tag", out var values));
        Assert.Contains("noindex", values!.First(), StringComparison.OrdinalIgnoreCase);
    }
}
