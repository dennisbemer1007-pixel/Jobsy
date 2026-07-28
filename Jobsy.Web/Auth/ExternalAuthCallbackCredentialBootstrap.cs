using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using AspNetGoogleOptions = Microsoft.AspNetCore.Authentication.Google.GoogleOptions;

namespace Jobsy.Web.Auth;

/// <summary>
/// Ensures Integraties credentials are on the OAuth/OIDC options before the callback
/// handler redeems the authorization code (startup may still have "pending" placeholders).
/// </summary>
public static class ExternalAuthCallbackCredentialBootstrap
{
    public static IApplicationBuilder UseExternalAuthCallbackCredentials(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (path.Equals("/signin-entra", StringComparison.OrdinalIgnoreCase))
            {
                var source = context.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
                var entra = await source.GetEntraAsync(context.RequestAborted);
                if (entra is not null)
                {
                    var options = context.RequestServices
                        .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                        .Get(AuthServiceCollectionExtensions.EntraScheme);
                    EntraOidcOptionsApplier.Apply(options, entra);
                }
            }
            else if (path.Equals("/signin-google", StringComparison.OrdinalIgnoreCase))
            {
                var source = context.RequestServices.GetRequiredService<IExternalAuthCredentialSource>();
                var google = await source.GetGoogleAsync(context.RequestAborted);
                if (google is not null)
                {
                    var options = context.RequestServices
                        .GetRequiredService<IOptionsMonitor<AspNetGoogleOptions>>()
                        .Get(AuthServiceCollectionExtensions.GoogleScheme);
                    lock (AuthServiceCollectionExtensions.GoogleOptionsSync)
                    {
                        options.ClientId = google.ClientId;
                        options.ClientSecret = google.ClientSecret;
                    }
                }
            }

            await next();
        });
}
