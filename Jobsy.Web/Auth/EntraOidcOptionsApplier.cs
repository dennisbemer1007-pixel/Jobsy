using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Jobsy.Web.Auth;

/// <summary>
/// Applies Integraties/appsettings Entra credentials onto the shared OIDC options
/// and keeps metadata in sync when the tenant changes from the startup placeholder.
/// </summary>
public static class EntraOidcOptionsApplier
{
    private static readonly object Gate = new();

    public static void Apply(OpenIdConnectOptions options, ExternalOAuthCredentials entra)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(entra);

        var tenant = string.IsNullOrWhiteSpace(entra.TenantId) ? "common" : entra.TenantId.Trim();
        var authority = $"https://login.microsoftonline.com/{tenant}/v2.0";
        var metadata = $"{authority}/.well-known/openid-configuration";

        lock (Gate)
        {
            var refreshMetadata = options.ConfigurationManager is null
                || !string.Equals(options.Authority, authority, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(options.ClientId, entra.ClientId, StringComparison.Ordinal)
                || string.Equals(options.ClientId, "pending", StringComparison.Ordinal);

            options.ClientId = entra.ClientId;
            options.ClientSecret = entra.ClientSecret;
            options.Authority = authority;
            options.MetadataAddress = metadata;

            if (refreshMetadata)
            {
                options.Configuration = null;
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadata,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever { RequireHttps = options.RequireHttpsMetadata });
            }
        }
    }

    /// <summary>
    /// Accepts Entra single- and multi-tenant issuer formats when Authority is "common".
    /// </summary>
    public static string ValidateMicrosoftIssuer(
        string issuer,
        SecurityToken _token,
        TokenValidationParameters _parameters)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new SecurityTokenInvalidIssuerException("Issuer ontbreekt op het Microsoft-token.");
        }

        if (issuer.StartsWith("https://login.microsoftonline.com/", StringComparison.OrdinalIgnoreCase)
            || issuer.StartsWith("https://sts.windows.net/", StringComparison.OrdinalIgnoreCase)
            || issuer.StartsWith("https://login.microsoft.com/", StringComparison.OrdinalIgnoreCase))
        {
            return issuer;
        }

        throw new SecurityTokenInvalidIssuerException(
            $"Issuer '{issuer}' is geen vertrouwde Microsoft Entra-issuer.");
    }
}
