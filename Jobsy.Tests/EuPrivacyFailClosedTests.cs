using Jobsy.Core.Ai;
using Jobsy.Core.Email;
using Jobsy.Core.Geo;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class ActiveProviderStatusTests
{
    [Fact]
    public void Mistral_without_a_key_is_off_and_does_not_name_openai()
    {
        var status = ActiveProviderStatus.DescribeAi("Mistral", "  ", null, null, "gpt-4o-mini", null);

        Assert.False(status.Available);
        Assert.Equal(ActiveProviderStatus.RegionOff, status.RegionCode);
        Assert.Equal("Unavailable", status.Provider);
        Assert.Null(status.EndpointHost);
    }

    [Fact]
    public void Explicit_openai_stays_on_the_us_host()
    {
        var status = ActiveProviderStatus.DescribeAi("OpenAI", null, null, null, "gpt-4o-mini", null);

        Assert.True(status.Available);
        Assert.Equal(AiProviderNames.OpenAI, status.Provider);
        Assert.Equal("gpt-4o-mini", status.Model);
        Assert.Equal(ActiveProviderStatus.RegionUs, status.RegionCode);
        Assert.Equal("api.openai.com", status.EndpointHost);
    }

    [Fact]
    public void Mistral_with_a_key_shows_the_eu_host()
    {
        var status = ActiveProviderStatus.DescribeAi(
            "Mistral",
            "secret",
            null,
            "https://api.eu.mistral.ai/v1/",
            null,
            null);

        Assert.True(status.Available);
        Assert.Equal(AiProviderNames.Mistral, status.Provider);
        Assert.Equal(MistralOptions.DefaultModel, status.Model);
        Assert.Equal(ActiveProviderStatus.RegionEu, status.RegionCode);
        Assert.Equal("api.eu.mistral.ai", status.EndpointHost);
    }
}

public class ActiveMailStatusTests
{
    [Fact]
    public void Lettermint_without_a_key_is_not_configured()
    {
        var status = ActiveMailStatus.Describe("Lettermint", lettermintApiKeyConfigured: false, lettermintBaseUrl: null);

        Assert.False(status.Available);
        Assert.Equal(ActiveMailStatus.RegionOff, status.RegionCode);
        Assert.Equal(MailProviderNames.Lettermint, status.Provider);
        Assert.Null(status.EndpointHost);
    }

    [Fact]
    public void Lettermint_with_a_key_shows_the_nl_host()
    {
        var status = ActiveMailStatus.Describe("Lettermint", lettermintApiKeyConfigured: true, lettermintBaseUrl: null);

        Assert.True(status.Available);
        Assert.Equal(MailProviderNames.Lettermint, status.Provider);
        Assert.Equal(ActiveMailStatus.RegionNl, status.RegionCode);
        Assert.Equal("api.lettermint.co", status.EndpointHost);
    }

    [Fact]
    public void Explicit_resend_stays_on_resend()
    {
        var status = ActiveMailStatus.Describe("Resend", lettermintApiKeyConfigured: false, lettermintBaseUrl: null);

        Assert.True(status.Available);
        Assert.Equal(MailProviderNames.Resend, status.Provider);
        Assert.Equal(ActiveMailStatus.RegionUs, status.RegionCode);
        Assert.Equal("api.resend.com", status.EndpointHost);
    }
}

public class NominatimPolicyTests
{
    [Fact]
    public void User_agent_uses_the_configured_contact_and_defaults_to_lobsy()
    {
        Assert.Equal("Lobsy/1.0 (info@lobsy.nl)", GeoOptions.UserAgent(null));
        Assert.Equal("Lobsy/1.0 (info@lobsy.nl)", GeoOptions.UserAgent("  "));
        Assert.Equal("Lobsy/1.0 (privacy@lobsy.nl)", GeoOptions.UserAgent(" privacy@lobsy.nl "));

        var root = TestRepo.FindRoot();
        var web = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Program.cs"));
        var api = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "DependencyInjection.cs"));
        Assert.Contains("GeoOptions.UserAgent", web, StringComparison.Ordinal);
        Assert.Contains("GeoOptions.UserAgent", api, StringComparison.Ordinal);
        Assert.DoesNotContain("contact@jobsy.local", web, StringComparison.Ordinal);
        Assert.DoesNotContain("contact@jobsy.local", api, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pace_lets_the_first_call_through_and_spaces_the_next()
    {
        var pace = new NominatimPace(TimeSpan.FromMilliseconds(80));
        var started = DateTime.UtcNow;
        await pace.WaitAsync();
        await pace.WaitAsync();
        Assert.True(DateTime.UtcNow - started >= TimeSpan.FromMilliseconds(70));
    }

    [Fact]
    public async Task Cache_returns_a_fresh_value_and_drops_an_expired_one()
    {
        var cache = new GeoLookupCache();
        cache.Set("web-suggest:westland", "ok", TimeSpan.FromMinutes(10));
        Assert.True(cache.TryGet("web-suggest:westland", out string? hit));
        Assert.Equal("ok", hit);

        cache.Set("web-suggest:oud", "stale", TimeSpan.FromMilliseconds(30));
        await Task.Delay(80);
        Assert.False(cache.TryGet("web-suggest:oud", out string? miss));
        Assert.Null(miss);
    }
}
