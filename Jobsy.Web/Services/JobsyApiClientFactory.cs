using System.Net;
using Jobsy.Core;
using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Services;

/// <summary>
/// Builds an API <see cref="HttpClient"/> in the Blazor circuit scope
/// so <see cref="JobsyApiAuthHandler"/> can safely use <c>AuthenticationStateProvider</c>.
/// </summary>
public static class JobsyApiClientFactory
{
    /// <summary>
    /// Shared handler for connection pooling / TLS reuse across circuit scopes.
    /// Cookies are disabled so users never share a <see cref="CookieContainer"/>.
    /// </summary>
    public static readonly SocketsHttpHandler SharedSocketsHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = false
    };

    public static HttpClient Create(IServiceProvider sp, IConfiguration configuration)
    {
        var auth = new JobsyApiAuthHandler(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp,
            configuration,
            sp.GetRequiredService<JobsyAccessTokenIssuer>())
        {
            // Do not dispose the shared sockets handler when a scoped HttpClient is disposed.
            InnerHandler = new NonDisposingHandler(SharedSocketsHandler)
        };
        var retry = new JobsyApiTransientRetryHandler
        {
            InnerHandler = auth
        };
        var handler = new ApiCallTrackingHandler(sp.GetService<ApiCallTracker>())
        {
            InnerHandler = retry
        };

        var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
            configuration["ApiBaseUrl"],
            "http://localhost:5200/");
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(apiBaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    /// <summary>Delegates to an inner handler without disposing it.</summary>
    internal sealed class NonDisposingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override void Dispose(bool disposing)
        {
            // Intentionally skip base.Dispose so the shared SocketsHttpHandler stays alive.
        }
    }
}
