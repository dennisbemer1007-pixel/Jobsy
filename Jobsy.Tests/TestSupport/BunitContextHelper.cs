using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Jobsy.Tests.TestSupport;

/// <summary>Shared bUnit DI for Blazor components that inject antiforgery or HTTP context.</summary>
public static class BunitContextHelper
{
    public static void RegisterAntiforgeryAndHttpContext(IServiceCollection services, HttpContext? httpContext = null)
    {
        var http = httpContext ?? new DefaultHttpContext();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });
        services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
    }

    /// <summary>Registers a factory that returns the same <see cref="HttpClient"/> instance (for reset dialog, etc.).</summary>
    public static void RegisterHttpClientFactory(IServiceCollection services, HttpClient client)
    {
        services.AddSingleton<IHttpClientFactory>(new SingleHttpClientFactory(client));
    }

    private sealed class SingleHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    public sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext httpContext)
        {
            _ = httpContext;
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            _ = httpContext;
            return Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            _ = httpContext;
        }
    }
}
