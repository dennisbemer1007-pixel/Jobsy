using System.Net;
using System.Text;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class PdokGeocodingClientTests
{
    [Fact]
    public void OrderFixtureDocs_ranks_adres_before_poi_types()
    {
        var ordered = PdokGeocodingClient.OrderFixtureDocs(
        [
            ("Wateringen", "woonplaats", 4.2, 52.0, 10),
            ("Herenstraat, Wateringen", "weg", 4.21, 52.01, 20),
            ("Herenstraat 20, Wateringen", "adres", 4.22, 52.02, 5),
            ("2291 AB", "postcode", 4.23, 52.03, 8),
        ]);

        Assert.Equal("Herenstraat 20, Wateringen", ordered[0].Label);
        Assert.Equal("2291 AB", ordered[1].Label);
        Assert.Equal("Herenstraat, Wateringen", ordered[2].Label);
        Assert.Equal("Wateringen", ordered[3].Label);
    }

    [Fact]
    public async Task SuggestAsync_parses_pdok_json_and_orders_by_type()
    {
        const string json = """
            {
              "response": {
                "docs": [
                  {
                    "weergavenaam": "Wateringse Veld, Den Haag",
                    "type": "woonplaats",
                    "centroide_ll": "POINT(4.35 52.03)",
                    "score": 99.0
                  },
                  {
                    "weergavenaam": "Herenstraat 20, 2291 AB Wateringen",
                    "type": "adres",
                    "centroide_ll": "POINT(4.273 51.990)",
                    "score": 12.0
                  },
                  {
                    "weergavenaam": "Herenstraat, Wateringen",
                    "type": "weg",
                    "centroide_ll": "POINT(4.274 51.991)",
                    "score": 40.0
                  }
                ]
              }
            }
            """;

        using var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        using var http = new HttpClient(handler);
        var client = new PdokGeocodingClient(http);
        var results = await client.SuggestAsync("Herenstraat 20 Wateringen");
        Assert.Equal("Herenstraat 20, 2291 AB Wateringen", results[0].Label);
        Assert.Equal(51.990, results[0].Latitude, 3);
        Assert.Equal(4.273, results[0].Longitude, 3);
    }

    [Fact]
    public async Task SuggestAsync_rewrites_postcode_docs_to_code_and_city()
    {
        const string json = """
            {
              "response": {
                "docs": [
                  {
                    "weergavenaam": "Stationsplein, 1012AB Amsterdam",
                    "type": "postcode",
                    "centroide_ll": "POINT(4.900 52.378)",
                    "score": 20.0,
                    "postcode": "1012AB",
                    "woonplaatsnaam": "Amsterdam"
                  },
                  {
                    "weergavenaam": "Stationsplein 1, 1012AB Amsterdam",
                    "type": "adres",
                    "centroide_ll": "POINT(4.901 52.379)",
                    "score": 5.0
                  }
                ]
              }
            }
            """;

        using var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        using var http = new HttpClient(handler);
        var client = new PdokGeocodingClient(http);
        var results = await client.SuggestAsync("1012 AB");
        var picked = PostcodeMatch.Pick(results, PostcodeMatch.Normalize("1012 AB"));
        Assert.NotNull(picked);
        Assert.Equal("1012AB Amsterdam", picked!.Label);
        Assert.Equal("Amsterdam", PostcodeMatch.CityFromLabel(picked.Label, "1012 AB"));
    }

    [Fact]
    public void Postcode_pick_ignores_a_street_that_does_not_start_with_the_code()
    {
        var picked = PostcodeMatch.Pick(
            [new AddressSuggestion("Stationsplein, 1012AB Amsterdam", 52.3, 4.9)],
            "1012 AB");
        Assert.Null(PostcodeMatch.CityFromLabel("Stationsplein, 1012AB Amsterdam", "1012 AB"));
        Assert.NotNull(picked);
        Assert.Contains("1012AB", picked!.Label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Composite_falls_back_to_nominatim_when_pdok_postcode_does_not_match()
    {
        var pdokHandler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"response":{"docs":[{"weergavenaam":"Stationsplein, 1012AB Amsterdam","type":"adres","centroide_ll":"POINT(4.9 52.3)","score":1}]}}
                    """, Encoding.UTF8, "application/json")
            });
        var nomiHandler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"display_name":"1012 AB, Amsterdam, Nederland","lat":"52.37","lon":"4.89","address":{"postcode":"1012 AB","city":"Amsterdam"}}]
                    """, Encoding.UTF8, "application/json")
            });
        using var pdokHttp = new HttpClient(pdokHandler);
        using var nomiHttp = new HttpClient(nomiHandler);
        var composite = new CompositeGeocodingClient(
            new PdokGeocodingClient(pdokHttp),
            new NominatimGeocodingClient(nomiHttp));
        var results = await composite.SuggestAsync("1012 AB");
        var picked = PostcodeMatch.Pick(results, "1012 AB");
        Assert.NotNull(picked);
        Assert.Equal("Amsterdam", PostcodeMatch.CityFromLabel(picked!.Label, "1012 AB"));
    }

    [Fact]
    public async Task Composite_falls_back_to_nominatim_on_pdok_500()
    {
        var pdokHandler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var nomiHandler = new StubHandler(req =>
        {
            Assert.Contains("layer=address", req.RequestUri!.Query, StringComparison.Ordinal);
            Assert.Contains("addressdetails=1", req.RequestUri.Query, StringComparison.Ordinal);
            const string body = """
                [
                  {
                    "display_name": "Herenstraat 20, Wateringen, Nederland",
                    "lat": "51.99",
                    "lon": "4.27",
                    "address": { "road": "Herenstraat", "house_number": "20", "town": "Wateringen" }
                  }
                ]
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var pdokHttp = new HttpClient(pdokHandler);
        using var nomiHttp = new HttpClient(nomiHandler);
        var composite = new CompositeGeocodingClient(
            new PdokGeocodingClient(pdokHttp),
            new NominatimGeocodingClient(nomiHttp));

        var results = await composite.SuggestAsync("Herenstraat 20 Wateringen");
        Assert.NotEmpty(results);
        Assert.Contains("Herenstraat", results[0].Label, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Nominatim_request_includes_address_layer()
    {
        Uri? seen = null;
        using var handler = new StubHandler(req =>
        {
            seen = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        });
        using var http = new HttpClient(handler);
        var client = new NominatimGeocodingClient(http);
        await client.SuggestAsync("Wateringse");
        Assert.NotNull(seen);
        Assert.Contains("layer=address", seen!.Query, StringComparison.Ordinal);
        Assert.Contains("addressdetails=1", seen.Query, StringComparison.Ordinal);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
