using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

/// <summary>
/// Documents payload bounds for the HIGH anti-scraping fix (3500 public vacancies scenario).
/// </summary>
public sealed class VacancyPublicApiScaleBenchmarkTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public VacancyPublicApiScaleBenchmarkTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Scale_3500_public_vacancies_paged_and_bbox_stay_bounded()
    {
        const int catalogSize = 3500;
        var sampleItem = MinimalListItem();
        var fullCatalogBytes = JsonSerializer.SerializeToUtf8Bytes(
            Enumerable.Repeat(sampleItem, catalogSize).ToList(),
            JsonOpts);
        var pageBytes = JsonSerializer.SerializeToUtf8Bytes(
            new VacancyListPageDto(
                Enumerable.Repeat(sampleItem, PublicApiLimits.DefaultListTake).ToList(),
                Skip: 0,
                Take: PublicApiLimits.DefaultListTake,
                TotalCount: catalogSize,
                HasMore: true),
            JsonOpts);

        var client = _factory.CreateClient();
        var sw = Stopwatch.StartNew();
        var page = await client.GetFromJsonAsync<VacancyListPageDto>("api/vacancies", JsonOpts);
        sw.Stop();
        Assert.NotNull(page);
        var livePageBytes = JsonSerializer.SerializeToUtf8Bytes(page, JsonOpts);

        var pinsSw = Stopwatch.StartNew();
        var pinsResponse = await client.GetAsync(
            "api/vacancies/pins?minLat=51.9&minLng=4.2&maxLat=52.4&maxLng=4.8&zoom=10&transport=Fiets&maxMinutes=60");
        pinsSw.Stop();
        pinsResponse.EnsureSuccessStatusCode();
        var pinsBytes = await pinsResponse.Content.ReadAsByteArrayAsync();

        var legacyPinsCap = PublicApiLimits.MaxPinsWithoutBbox;
        var pinDto = new VacancyPinDto(Guid.NewGuid(), 52.1, 4.3, "#3366cc", null);
        var legacyPinsBytes = JsonSerializer.SerializeToUtf8Bytes(
            Enumerable.Repeat(pinDto, legacyPinsCap).ToList(),
            JsonOpts);

        // Regression guard + measurement anchors for the Dutch PR report.
        Assert.True(page!.Take <= PublicApiLimits.MaxDiscoverTakePublic);
        Assert.True(livePageBytes.Length < fullCatalogBytes.Length / 10);
        Assert.True(pinsBytes.Length <= legacyPinsBytes.Length * 12);

        var report = $"""
            scale-benchmark catalogSize={catalogSize}
            fullCatalogJsonBytes={fullCatalogBytes.Length}
            singlePageJsonBytes={pageBytes.Length}
            liveGetActiveBytes={livePageBytes.Length}
            liveGetActiveMs={sw.ElapsedMilliseconds}
            livePinsBboxBytes={pinsBytes.Length}
            livePinsBboxMs={pinsSw.ElapsedMilliseconds}
            legacyUnboundedPinsCap={legacyPinsCap}
            """;
        Assert.Contains("scale-benchmark", report);
    }

    private static VacancyListItemDto MinimalListItem()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new VacancyListItemDto(
            Guid.NewGuid(),
            "Hovenier",
            "Korte omschrijving",
            15m,
            today,
            today.AddMonths(2),
            VacancyStatus.Active.ToString(),
            Guid.NewGuid(),
            "Jobsy Demo BV",
            "Stationsweg 1, Den Haag",
            null,
            null,
            52.07,
            4.31,
            [TransportLabels.Bike]);
    }
}
