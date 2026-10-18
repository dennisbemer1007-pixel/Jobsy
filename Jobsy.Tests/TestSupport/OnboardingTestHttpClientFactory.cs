using System.Net.Http;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests.TestSupport;

/// <summary>
/// Routes API calls to the test API host and same-site BFF calls (e.g. profile reset) to the Web Kestrel URL.
/// </summary>
internal sealed class OnboardingTestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _api;
    private readonly Func<string> _webBase;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OnboardingTestHttpClientFactory(
        HttpMessageHandler api,
        Func<string> webBase,
        IHttpContextAccessor httpContextAccessor)
    {
        _api = api;
        _webBase = webBase;
        _httpContextAccessor = httpContextAccessor;
    }

    public HttpClient CreateClient(string name)
    {
        if (string.Equals(name, "JobsyWebSelf", StringComparison.OrdinalIgnoreCase))
        {
            var webHandler = new ForwardCircuitCookiesHandler(_httpContextAccessor)
            {
                InnerHandler = new HttpClientHandler()
            };
            return new HttpClient(webHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(_webBase().TrimEnd('/') + "/")
            };
        }

        return new HttpClient(new TestApiForwardHandler(_api), disposeHandler: false)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }
}

internal sealed class TestApiForwardHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _api;

    public TestApiForwardHandler(HttpMessageHandler api)
        => _api = new HttpMessageInvoker(api, disposeHandler: false);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "/";
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await _api.SendAsync(clone, cancellationToken);
    }
}
