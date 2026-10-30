using System.Text.Json;
using Jobsy.Core.Contracts;

namespace Jobsy.Tests;

public sealed class BedrijvenkaartSsrPinsStubTests
{
    [Fact]
    public void Illustrated_stub_filters_company_map_pins_by_radius_and_origin()
    {
        var json = IllustratedPagesApiStub.BuildJson(
            "/api/company-map-initiative/pins?radiusKm=10&lat=51.9935&lng=4.2085");
        var pins = JsonSerializer.Deserialize<List<CompanyMapPinDto>>(json, JsonOptions());
        Assert.NotNull(pins);
        Assert.Equal(2, pins!.Count);
    }

    [Fact]
    public async Task Bedrijvenkaart_bff_pins_returns_filtered_list_with_origin()
    {
        await using var web = new IllustratedPagesWebFactory(companyMapInitiativeEnabled: false);
        using var http = new HttpClient { BaseAddress = new Uri(web.ServerAddress) };
        var response = await http.GetAsync(
            "/api/bedrijvenkaart-client/pins?radiusKm=10&lat=51.9935&lng=4.2085");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var pins = JsonSerializer.Deserialize<List<CompanyMapPinDto>>(body, JsonOptions());
        Assert.NotNull(pins);
        Assert.Equal(2, pins!.Count);
    }

    [Fact]
    public async Task Company_public_page_renders_kaswerk_title()
    {
        await using var web = new IllustratedPagesWebFactory(companyMapInitiativeEnabled: false);
        using var http = new HttpClient { BaseAddress = new Uri(web.ServerAddress) };
        var html = await http.GetStringAsync("/71001001/0001");
        Assert.Contains("KasWerk Naaldwijk", html, StringComparison.Ordinal);
        Assert.Contains("id=\"company-title\"", html, StringComparison.Ordinal);
    }

    private static JsonSerializerOptions JsonOptions()
        => new() { PropertyNameCaseInsensitive = true };
}
