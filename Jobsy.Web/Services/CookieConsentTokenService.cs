using Jobsy.Core.Privacy;
using Jobsy.Core.Security;

namespace Jobsy.Web.Services;

/// <summary>Shared minting of analytics-consent tokens for interactive and static cookie banners.</summary>
public interface ICookieConsentTokenService
{
    string MintAnalyticsToken();
}

public sealed class CookieConsentTokenService : ICookieConsentTokenService
{
    private readonly IConfiguration _configuration;

    public CookieConsentTokenService(IConfiguration configuration)
        => _configuration = configuration;

    public string MintAnalyticsToken()
    {
        var key = JobsyLocalSessionToken.ResolveSigningKey(
            _configuration["JobsyAuth:LocalSessionSigningKey"],
            _configuration["JobsyAuth:DevelopmentAuthSecret"]);
        return string.IsNullOrWhiteSpace(key)
            ? CookieConsentNames.AnalyticsValue
            : CookieConsentToken.Create(key);
    }
}
