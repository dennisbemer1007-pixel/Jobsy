using System.Net;
using Jobsy.Core;
using Jobsy.Core.Security;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
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

    /// <summary>
    /// Used only when <c>CLOUDFLARE_ORIGIN_SECRET</c> is set, so cross-host image
    /// redirects can drop the secret. Unset keeps <see cref="SharedSocketsHandler"/>.
    /// </summary>
    public static readonly SocketsHttpHandler SharedSocketsHandlerNoRedirect = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        AutomaticDecompression = DecompressionMethods.All,
        UseCookies = false,
        AllowAutoRedirect = false
    };

    public static HttpClient Create(IServiceProvider sp, IConfiguration configuration)
    {
        var originSecret = CloudflareOriginSecret.Normalize(
            configuration[CloudflareOriginMiddleware.ConfigKey]
            ?? configuration[CloudflareOriginMiddleware.ConfigKeyAlt]);
        var sockets = originSecret is null ? SharedSocketsHandler : SharedSocketsHandlerNoRedirect;
        HttpMessageHandler transport = new NonDisposingHandler(sockets);
        if (originSecret is not null)
        {
            transport = new JobsyApiRedirectHandler { InnerHandler = transport };
        }

        var auth = new JobsyApiAuthHandler(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp,
            configuration,
            sp.GetRequiredService<JobsyAccessTokenIssuer>())
        {
            // Do not dispose the shared sockets handler when a scoped HttpClient is disposed.
            InnerHandler = transport
        };
        // Inside the retry handler: retries strip X-Jobsy-* and this puts the origin secret back.
        var origin = new CloudflareOriginHeaderHandler(configuration)
        {
            InnerHandler = auth
        };
        var retry = new JobsyApiTransientRetryHandler
        {
            InnerHandler = origin
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
#pragma warning disable CA2215 // shared inner handler must outlive this wrapper
        protected override void Dispose(bool disposing)
        {
            // Intentionally skip base.Dispose so the shared SocketsHttpHandler stays alive.
        }
#pragma warning restore CA2215
    }
}
