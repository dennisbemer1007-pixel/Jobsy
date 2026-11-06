using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public sealed class CandidateInstellingenRedirectTests
{
    [Theory]
    [InlineData("/candidate/instellingen", "/candidate/settings")]
    [InlineData("/candidate/instellingen/", "/candidate/settings")]
    [InlineData("/candidate/instellingen/mail", "/candidate/settings/mail")]
    [InlineData("/candidate/meldingen", "/notifications")]
    [InlineData("/candidate/meldingen/", "/notifications")]
    public void Legacy_instellingen_maps_to_settings(string path, string expected)
    {
        Assert.True(CandidateLegacyRoutes.TryMap(path, out var dest));
        Assert.Equal(expected, dest);
    }

    [Fact]
    public async Task Http_get_instellingen_redirects_to_settings()
    {
        await using var factory = new IllustratedPagesWebFactory();
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/candidate/instellingen?pane=mail");
        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/candidate/settings?pane=mail", response.Headers.Location?.OriginalString);
    }
}
