using Jobsy.Core.Hosting;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Features;

/// <summary>
/// Resolves ON/OFF once per request (cached in <see cref="HttpContext.Items"/>).
/// Overrides: query <c>?_variant=on|zw</c> (Development only) and config <c>Landing:ForceVariant</c>
/// (Development and Acceptatie — set on lobsy-acc-web, not production).
/// </summary>
public sealed class LandingVariantResolver
{
    public const string HttpContextItemsKey = "Jobsy.LandingVariant";
    public const string QueryName = "_variant";
    public const string ConfigKey = "Landing:ForceVariant";

    private readonly IEmployersSwitch _employers;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public LandingVariantResolver(
        IEmployersSwitch employers,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _employers = employers;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _configuration = configuration;
    }

    public async ValueTask<LandingVariant> GetAsync(CancellationToken ct = default)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.Items[HttpContextItemsKey] is LandingVariant cached)
        {
            return cached;
        }

        var resolved = await ResolveCoreAsync(http, ct);
        if (http is not null)
        {
            http.Items[HttpContextItemsKey] = resolved;
        }

        return resolved;
    }

    private async ValueTask<LandingVariant> ResolveCoreAsync(HttpContext? http, CancellationToken ct)
    {
        if (_environment.IsDevelopment()
            && http is not null
            && http.Request.Query.TryGetValue(QueryName, out var query)
            && TryParse(query.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        if (AllowsConfigOverride(_environment, _configuration)
            && TryParse(_configuration[ConfigKey], out var fromConfig))
        {
            return fromConfig;
        }

        return await _employers.VariantAsync(ct);
    }

    /// <summary>Acceptatie preview and local dev may force the landing variant via config; production may not.</summary>
    public static bool AllowsConfigOverride(IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            return true;
        }

        var label = configuration["Lobsy:DeploymentEnvironment"];
        return string.Equals(label, DeploymentEnvironment.Acceptatie, StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParse(string? raw, out LandingVariant variant)
    {
        variant = LandingVariant.On;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        switch (raw.Trim().ToLowerInvariant())
        {
            case "on":
                variant = LandingVariant.On;
                return true;
            case "zw":
            case "off":
                variant = LandingVariant.Zw;
                return true;
            default:
                return false;
        }
    }
}
