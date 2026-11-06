using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.TestSupport;

internal static class BunitAntiforgery
{
    /// <summary>Minimal DI for <c>CandidateProfileResetDialog</c> in bunit hosts.</summary>
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        services.AddSingleton<IHttpClientFactory>(sp =>
        {
            var client = sp.GetService<HttpClient>()
                ?? new HttpClient(new HttpClientHandler()) { BaseAddress = new Uri("http://localhost/") };
            return new StubHttpClientFactory(client);
        });
    }

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext _) { }

        public Task ValidateRequestAsync(HttpContext _) => Task.CompletedTask;

        public void SetCookieTokenAndHeader(HttpContext httpContext) { }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
