using System.Net;

namespace Jobsy.Tests;

public sealed class BanenkaartClientAllEndpointTests
{
    [Fact]
    public async Task Banenkaart_client_all_returns_200_with_catalog_body()
    {
        using var factory = new IllustratedPagesWebFactory();
        using var client = factory.CreateClient(new() { AllowAutoRedirect = true });
        var response = await client.GetAsync("/api/banenkaart-client/all");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.True(body.Length > 32, "Expected non-empty catalog JSON.");
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        Assert.Contains("application/json", mediaType, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.ETag?.Tag));
    }
}
