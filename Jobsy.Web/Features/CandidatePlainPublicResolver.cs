using Jobsy.Core.Hosting;
using Jobsy.Web.Navigation;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Features;

/// <summary>
/// Plain warm public shell for candidate landing and login (mockup kandidaat/plain).
/// Config <c>CandidatePublic:PlainSkin</c> on Acceptatie and Development; query <c>?_plainSkin=0|1</c> in Development only.
/// </summary>
public sealed class CandidatePlainPublicResolver
{
    public const string HttpContextItemsKey = "Jobsy.CandidatePlainPublic";
    public const string ConfigKey = "CandidatePublic:PlainSkin";
    public const string QueryName = "_plainSkin";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public CandidatePlainPublicResolver(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _configuration = configuration;
    }

    public ValueTask<bool> IsPlainShellAsync()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.Items[HttpContextItemsKey] is bool cached)
        {
            return ValueTask.FromResult(cached);
        }

        var enabled = IsPlainShellForRequest(http, _environment, _configuration);
        if (http is not null)
        {
            http.Items[HttpContextItemsKey] = enabled;
        }

        return ValueTask.FromResult(enabled);
    }

    /// <summary>
    /// Static entry for <c>App.razor</c> head (renders before layout HeadContent on NoBlazorRuntime SSR).
    /// </summary>
    public static bool IsPlainShellForRequest(
        HttpContext? http,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var path = http?.Request.Path.Value;
        if (!ResolveEnabled(http, environment, configuration))
        {
            return false;
        }

        var normalized = Jobsy.Web.Seo.PageSeoCatalog.Normalize(path);
        return IsPlainPublicPath(normalized) || IsPlainCandidateAppPath(normalized);
    }

    private static bool ResolveEnabled(HttpContext? http, IHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment()
            && http is not null
            && http.Request.Query.TryGetValue(QueryName, out var query)
            && TryParse(query.ToString(), out var fromQuery))
        {
            return fromQuery;
        }

        if (LandingVariantResolver.AllowsConfigOverride(environment, configuration)
            && TryParse(configuration[ConfigKey], out var fromConfig))
        {
            return fromConfig;
        }

        return false;
    }

    public static bool IsPlainPublicPath(string? path)
        => string.Equals(path, PublicRoutes.Landing, StringComparison.OrdinalIgnoreCase)
           || string.Equals(path, PublicRoutes.Login, StringComparison.OrdinalIgnoreCase)
           || string.Equals(path, PublicRoutes.CreateAccount, StringComparison.OrdinalIgnoreCase)
           || string.Equals(path, PublicRoutes.ForgotPassword, StringComparison.OrdinalIgnoreCase);

    /// <summary>Plain candidate app: paspoort and banenkaart (mockup plain #878).</summary>
    public static bool IsPlainCandidateAppPath(string? path)
        => string.Equals(path, "/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
           || path?.StartsWith("/candidate/paspoort/", StringComparison.OrdinalIgnoreCase) == true
           || string.Equals(path, "/banenkaart", StringComparison.OrdinalIgnoreCase)
           || string.Equals(path, "/banen", StringComparison.OrdinalIgnoreCase);

    public static bool TryParse(string? raw, out bool enabled)
    {
        enabled = false;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        switch (raw.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "on":
            case "yes":
                enabled = true;
                return true;
            case "0":
            case "false":
            case "off":
            case "no":
                enabled = false;
                return true;
            default:
                return false;
        }
    }
}
