using System.Net;
using System.Text.Json;

namespace Jobsy.Tests;

public class SiteLegalEndpointTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public SiteLegalEndpointTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_get_legal_returns_200_omits_empty_and_sets_cache()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("api/site/legal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cache = response.Headers.CacheControl?.ToString()
            ?? response.Headers.GetValues("Cache-Control").FirstOrDefault()
            ?? "";
        Assert.Contains("max-age=300", cache, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public", cache, StringComparison.OrdinalIgnoreCase);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                Assert.False(string.IsNullOrWhiteSpace(prop.Value.GetString()), prop.Name);
            }
            else
            {
                Assert.NotEqual(JsonValueKind.Null, prop.Value.ValueKind);
            }
        }
    }
}
