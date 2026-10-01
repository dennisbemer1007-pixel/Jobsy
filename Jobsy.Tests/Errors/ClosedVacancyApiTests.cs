using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;

namespace Jobsy.Tests.Errors;

/// <summary>
/// API contract for errors 03: closed (410, minimal public data) vs unknown (404), and the
/// <c>/similar</c> nearby list that reuses the discovery index — no new ranking.
/// </summary>
public class ClosedVacancyApiTests : IClassFixture<ClosedVacancyApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private readonly ClosedVacancyApiFactory _factory;

    public ClosedVacancyApiTests(ClosedVacancyApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    [Theory]
    [InlineData("ArchivedId")]
    [InlineData("FulfilledId")]
    [InlineData("ExpiredId")]
    public async Task Closed_vacancies_answer_410_with_only_minimal_public_fields(string field)
    {
        var id = IdFor(field);
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"api/vacancies/{id:D}");
        Assert.Equal((HttpStatusCode)410, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<ClosedVacancyDto>(raw, JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(id, dto!.Id);
        Assert.False(string.IsNullOrWhiteSpace(dto.Title));

        // Only id/title/city/category* — no company name, dates, contact or description.
        Assert.DoesNotContain("companyName", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"description\"", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("startDate", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("endDate", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contact", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hourlyWage", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("DraftId")]
    [InlineData("PendingId")]
    [InlineData("FutureStartId")]
    public async Task Never_live_or_not_yet_live_vacancies_stay_404_not_410(string field)
    {
        var id = IdFor(field);
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"api/vacancies/{id:D}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_vacancy_id_is_404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_vacancy_is_200_not_410()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.PublicId:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Owner_still_gets_200_with_the_full_dto_for_a_closed_vacancy()
    {
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        var response = await client.GetAsync($"api/vacancies/{_factory.ArchivedId:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<VacancyListItemDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.CompanyName));
        Assert.False(string.IsNullOrWhiteSpace(dto.Description));
    }

    [Fact]
    public async Task Admin_also_gets_200_with_the_full_dto_for_a_closed_vacancy()
    {
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        var response = await client.GetAsync($"api/vacancies/{_factory.FulfilledId:D}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Similar_returns_up_to_three_public_vacancies_same_category_nearby_nearest_first()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.ArchivedId:D}/similar?limit=3");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cards = await response.Content.ReadFromJsonAsync<List<VacancyCardDto>>(JsonOpts);
        Assert.NotNull(cards);
        Assert.True(cards!.Count <= 3);
        Assert.Contains(cards, c => c.Id == _factory.NearbySameCategoryId);
        Assert.DoesNotContain(cards, c => c.Id == _factory.FarSameCategoryId);
        Assert.DoesNotContain(cards, c => c.Id == _factory.ArchivedId);
    }

    [Fact]
    public async Task Similar_limit_is_clamped_between_one_and_three()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.ArchivedId:D}/similar?limit=99");
        var cards = await response.Content.ReadFromJsonAsync<List<VacancyCardDto>>(JsonOpts);
        Assert.NotNull(cards);
        Assert.True(cards!.Count <= 3);
    }

    [Fact]
    public async Task Similar_on_a_public_vacancy_is_404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{_factory.PublicId:D}/similar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Similar_on_an_unknown_vacancy_is_404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"api/vacancies/{Guid.NewGuid():D}/similar");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Guid IdFor(string field) => field switch
    {
        "ArchivedId" => _factory.ArchivedId,
        "FulfilledId" => _factory.FulfilledId,
        "ExpiredId" => _factory.ExpiredId,
        "DraftId" => _factory.DraftId,
        "PendingId" => _factory.PendingId,
        "FutureStartId" => _factory.FutureStartId,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };
}
