using System.Net.Http.Json;
using Jobsy.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Web.Services;

/// <summary>
/// Public deep-analysis price for landing tiles / FAQ. Falls back to
/// <see cref="FlexCommercialSettings.DefaultDeepAnalysisPriceEuro"/> when the API is unavailable.
/// </summary>
public sealed class LandingPriceClient(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache)
{
    public const string HttpClientName = "JobsyLandingPrice";
    public const string CacheKey = "Jobsy.Landing.DeepAnalysisPriceEuro";
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMilliseconds(300);

    public async Task<decimal?> GetDeepAnalysisPriceEuroAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out decimal cached))
        {
            return cached;
        }

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(RequestTimeout);
            var http = httpClientFactory.CreateClient(HttpClientName);
            var dto = await http.GetFromJsonAsync<LandingPriceDto>("api/public/landing-price", linked.Token);
            if (dto is null)
            {
                return FlexCommercialSettings.DefaultDeepAnalysisPriceEuro;
            }

            cache.Set(CacheKey, dto.DeepAnalysisPriceEuro, CacheDuration);
            return dto.DeepAnalysisPriceEuro;
        }
        catch
        {
            return FlexCommercialSettings.DefaultDeepAnalysisPriceEuro;
        }
    }

    private sealed record LandingPriceDto(decimal DeepAnalysisPriceEuro);
}
