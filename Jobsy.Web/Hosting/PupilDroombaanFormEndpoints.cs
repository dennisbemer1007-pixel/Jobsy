using System.Net;
using System.Text;
using Jobsy.Core.Authorization;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR POST for /leerling/droombaan — no Blazor binding or autosave.</summary>
public static class PupilDroombaanFormEndpoints
{
    public const string Path = "/leerling/droombaan";

    public static IEndpointRouteBuilder MapPupilDroombaanFormEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Path, PostAsync)
            .RequireAuthorization(new AuthorizeAttribute { Policy = JobsyPolicies.PupilSession });

        return app;
    }

    private static async Task<IResult> PostAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";

        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect(Path);
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect($"{Path}?error=retry");
        }

        var action = form["action"].ToString();
        string key;
        if (string.Equals(action, "undecided", StringComparison.OrdinalIgnoreCase))
        {
            key = PupilDreamJobFit.UndecidedKey;
        }
        else if (!string.IsNullOrWhiteSpace(form["key"]))
        {
            key = form["key"].ToString().Trim();
        }
        else
        {
            var title = form["jobTitle"].ToString();
            if (!DreamJobCatalog.TryResolveKey(title, out var resolved))
            {
                return Results.Redirect($"{Path}?error=unknown");
            }

            key = resolved;
        }

        try
        {
            var resp = await api.SavePupilDreamJobAsync(key, cancellationToken);
            if (resp is null)
            {
                return Results.Redirect($"{Path}?error=save");
            }
        }
        catch
        {
            return Results.Redirect($"{Path}?error=save");
        }

        return Results.Redirect(Path);
    }
}
