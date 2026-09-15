using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class KvkApiBaseUrlTests
{
    [Theory]
    [InlineData(null, KvkApiBaseUrl.Production)]
    [InlineData("", KvkApiBaseUrl.Production)]
    [InlineData("   ", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/api/", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/api", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/api/v2/zoeken", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/api/v1/basisprofielen", KvkApiBaseUrl.Production)]
    [InlineData("https://developers.kvk.nl/", KvkApiBaseUrl.Production)]
    [InlineData("https://developers.kvk.nl/nl/documentation/quickstart", KvkApiBaseUrl.Production)]
    [InlineData("https://api.kvk.nl/test/api/", KvkApiBaseUrl.Test)]
    [InlineData("https://api.kvk.nl/test/api", KvkApiBaseUrl.Test)]
    [InlineData("https://api.kvk.nl/test", KvkApiBaseUrl.Test)]
    [InlineData("https://api.kvk.nl/test/api/v2/zoeken", KvkApiBaseUrl.Test)]
    public void Resolve_normalizes_kvk_hosts(string? input, string expected)
        => Assert.Equal(expected, KvkApiBaseUrl.Resolve(input));

    [Fact]
    public void Resolve_keeps_non_kvk_hosts_for_tests()
        => Assert.Equal(
            "https://kvk.example.test/api/",
            KvkApiBaseUrl.Resolve("https://kvk.example.test/api"));

    [Fact]
    public void EnvironmentLabel_detects_test_path()
    {
        Assert.Equal("testomgeving", KvkApiBaseUrl.EnvironmentLabel(KvkApiBaseUrl.Test));
        Assert.Equal("productie", KvkApiBaseUrl.EnvironmentLabel(KvkApiBaseUrl.Production));
    }
}
