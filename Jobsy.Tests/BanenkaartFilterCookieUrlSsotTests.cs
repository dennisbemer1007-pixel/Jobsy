using System.Text;
using System.Text.Json;
using Jobsy.Web.Banenkaart;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

public sealed class BanenkaartFilterCookieUrlSsotTests
{
    [Fact]
    public void UrlOmitsOriginWhileFiltered_is_true_for_maxMinutes_only()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["maxMinutes"] = "30",
        });

        Assert.True(BanenkaartSsrQuery.UrlOmitsOriginWhileFiltered(query));
    }

    [Fact]
    public void Merge_does_not_restore_cookie_origin_when_url_omits_origin()
    {
        var stored = new BanenkaartSsrQuery.State(
            SearchQuery: null,
            WorkTypes: [],
            MaxTravelMinutes: 60,
            Transport: "Auto",
            RadiusKm: 15,
            MinHoursPerWeek: 0,
            MaxHoursPerWeek: 40,
            MinHourlyWageCents: null,
            MaxHourlyWageCents: null,
            AgeYears: null,
            MinMatchPercent: 0,
            CategoryIds: [],
            CompanyIds: [],
            SortMode: "distance",
            DesktopListMode: false,
            OriginLat: 51.99389784,
            OriginLng: 4.37897242,
            OriginLabel: "Delft, Delft",
            Error: null,
            Saved: false);

        var dtoJson = JsonSerializer.Serialize(new
        {
            maxTravelMinutes = stored.MaxTravelMinutes,
            transport = stored.Transport,
            originLat = stored.OriginLat,
            originLng = stored.OriginLng,
            originLabel = stored.OriginLabel,
        });
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(dtoJson));

        var http = new DefaultHttpContext();
        http.Request.QueryString = new QueryString("?maxMinutes=30");
        http.Request.Headers.Cookie = $"{BanenkaartFilterCookie.CookieName}={encoded}";

        var fromUrl = BanenkaartSsrQuery.Parse(http.Request.Query);
        var merged = BanenkaartFilterCookie.Merge(http, fromUrl);

        Assert.True(merged.UrlOriginAbsentByQuery);
        Assert.Null(merged.OriginLat);
        Assert.Null(merged.OriginLng);
        Assert.Null(merged.OriginLabel);
        Assert.Equal(30, merged.MaxTravelMinutes);
    }
}
