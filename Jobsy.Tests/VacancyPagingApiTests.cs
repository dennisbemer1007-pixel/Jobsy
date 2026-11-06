using System.Net;
using System.Net.Http.Json;
using Jobsy.Api.Models;
using Jobsy.Api.Security;

namespace Jobsy.Tests;

public sealed class VacancyPagingApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public VacancyPagingApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetActive_returns_paged_payload_with_default_take()
    {
        var client = _factory.CreateClient();
        var page = await client.GetFromJsonAsync<VacancyListPageDto>("api/vacancies");
        Assert.NotNull(page);
        Assert.Equal(PublicApiLimits.DefaultListTake, page!.Take);
        Assert.True(page.Items.Count <= PublicApiLimits.DefaultListTake);
        Assert.True(page.TotalCount >= page.Items.Count);
        Assert.Equal(page.Skip + page.Items.Count < page.TotalCount, page.HasMore);
    }

    [Fact]
    public async Task Discover_applies_default_take_without_full_dump()
    {
        var client = _factory.CreateClient();
        var page = await client.GetFromJsonAsync<VacancyListPageDto>(
            "api/vacancies/discover?transport=Fiets&maxMinutes=60");
        Assert.NotNull(page);
        Assert.True(page!.Items.Count <= PublicApiLimits.DefaultListTake);
    }

    [Fact]
    public async Task Pins_without_bbox_is_capped()
    {
        var client = _factory.CreateClient();
        var pins = await client.GetFromJsonAsync<List<VacancyPinDto>>("api/vacancies/pins");
        Assert.NotNull(pins);
        Assert.True(pins!.Count <= PublicApiLimits.MaxPinsWithoutBbox);
    }

    [Fact]
    public async Task Pins_with_bbox_returns_only_inside_box()
    {
        var client = _factory.CreateClient();
        var pins = await client.GetFromJsonAsync<List<VacancyPinDto>>(
            "api/vacancies/pins?minLat=52.0&minLng=4.2&maxLat=52.2&maxLng=4.5&zoom=11");
        Assert.NotNull(pins);
        foreach (var p in pins!)
        {
            Assert.InRange(p.Lat, 52.0, 52.2);
            Assert.InRange(p.Lng, 4.2, 4.5);
        }
    }
}
