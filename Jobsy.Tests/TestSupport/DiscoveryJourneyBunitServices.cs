using System.Net;
using System.Text;
using Jobsy.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.TestSupport;

internal static class DiscoveryJourneyBunitServices
{
    public static void RegisterOverviewCache(IServiceCollection services)
    {
        services.AddSingleton<MeGetCache>();
        services.AddSingleton<DiscoveryOverviewCache>();
    }

    internal sealed class EmptyJsonHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
    }
}
