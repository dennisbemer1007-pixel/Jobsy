using System.Net;
using Jobsy.Web.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class ExternalAuthCredentialSourceTests
{
    [Fact]
    public async Task IsEntraConfigured_uses_public_providers_endpoint_without_provision_secret()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"entra":true,"google":false}""", System.Text.Encoding.UTF8, "application/json")
            });
        var sut = CreateSut(handler);

        Assert.True(await sut.IsEntraConfiguredAsync());
        Assert.False(await sut.IsGoogleConfiguredAsync());
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("external-providers", handler.LastPath, StringComparison.Ordinal);
        Assert.DoesNotContain("external-provider-config", handler.LastPath ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task IsEntraConfigured_falls_back_false_when_public_status_unavailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var sut = CreateSut(handler);

        Assert.False(await sut.IsEntraConfiguredAsync());
    }

    private static ExternalAuthCredentialSource CreateSut(StubHandler handler)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["JobsyAuth:ExternalProvisionSecret"] = ""
            })
            .Build();
        var options = Options.Create(new AuthOptions());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var factory = new StubHttpClientFactory(handler);
        return new ExternalAuthCredentialSource(config, options, factory, cache);
    }

    private sealed class StubHttpClientFactory(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? LastPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastPath = request.RequestUri?.PathAndQuery;
            return Task.FromResult(respond(request));
        }
    }
}
