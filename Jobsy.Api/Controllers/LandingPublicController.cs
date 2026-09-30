using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Api.Controllers;

/// <summary>Anonymous landing helpers (vacancy count + deep-analysis price).</summary>
[ApiController]
[Route("api/public")]
public sealed class LandingPublicController(
    IVacancyDiscoveryIndex discovery,
    IFlexCommercialService flexCommercial,
    IMemoryCache cache) : ControllerBase
{
    private const string VacancyCacheKey = "Jobsy.Api.Landing.ActiveVacancies";
    private static readonly TimeSpan VacancyCacheDuration = TimeSpan.FromMinutes(10);

    [HttpGet("landing-stats")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<object>> GetLandingStats(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(VacancyCacheKey, out int cached))
        {
            return Ok(new { activeVacancies = cached });
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var records = await discovery.GetActiveAsync(cancellationToken);
        var count = records.Count(r => VacancyVisibilityRules.IsPubliclyVisible(r, today));
        cache.Set(VacancyCacheKey, count, VacancyCacheDuration);
        return Ok(new { activeVacancies = count });
    }

    [HttpGet("landing-price")]
    [AllowAnonymous]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<object>> GetLandingPrice(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await flexCommercial.GetAsync(cancellationToken);
            return Ok(new { deepAnalysisPriceEuro = settings.DeepAnalysisPriceEuro });
        }
        catch
        {
            return Ok(new { deepAnalysisPriceEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro });
        }
    }
}
