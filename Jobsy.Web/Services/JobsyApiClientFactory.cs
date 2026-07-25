using Jobsy.Core;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jobsy.Web.Services;

/// <summary>
/// Builds an API <see cref="HttpClient"/> in the Blazor circuit scope
/// so <see cref="JobsyApiAuthHandler"/> can safely use <c>AuthenticationStateProvider</c>.
/// </summary>
public static class JobsyApiClientFactory
{
    public static HttpClient Create(IServiceProvider sp, IConfiguration configuration)
    {
        var handler = new JobsyApiAuthHandler(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp)
        {
            InnerHandler = new HttpClientHandler()
        };

        var apiBaseUrl = JobsyPublicUrl.NormalizeBaseUrl(
            configuration["ApiBaseUrl"],
            "http://localhost:5200/");
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(apiBaseUrl)
        };
    }
}
