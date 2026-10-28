using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>SSR GET/POST for /leerling/droombaan — no Blazor page or circuit.</summary>
public static class PupilDroombaanFormEndpoints
{
    public const string PagePath = "/leerling/droombaan";
    public const string SavePath = "/leerling/droombaan/save";

    public static IEndpointRouteBuilder MapPupilDroombaanFormEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PagePath, GetPageAsync)
            .RequireAuthorization(new AuthorizeAttribute { Policy = JobsyPolicies.PupilSession });
        app.MapPost(SavePath, PostSaveAsync)
            .RequireAuthorization(new AuthorizeAttribute { Policy = JobsyPolicies.PupilSession });

        return app;
    }

    private static async Task<IResult> GetPageAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);
        var pupil = http.User;
        if (pupil.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/leerling");
        }

        PupilResultPageDto? result;
        try
        {
            result = await api.GetPupilResultAsync(cancellationToken);
        }
        catch (HttpRequestException ex) when (PupilLoadRetry.IsSessionLost(ex.StatusCode))
        {
            return Results.Redirect("/leerling?error=expired");
        }
        catch
        {
            return Results.Content("Kon droombaan niet laden.", "text/plain; charset=utf-8", statusCode: 503);
        }

        if (result is null)
        {
            return Results.Content("Nog geen resultaat.", "text/plain; charset=utf-8", statusCode: 503);
        }

        var queryDream = http.Request.Query["dream"].ToString();
        var formError = http.Request.Query["error"].ToString() switch
        {
            "unknown" => "Kies een beroep uit de lijst of typ een bekende titel.",
            "save" => "Kon droombaan niet bewaren.",
            "retry" => culture["Common.Error.TryAgain"],
            _ => null
        };

        var fieldValue = PupilDroombaanSsrHtml.ResolveFieldValue(
            new PupilDroombaanPageModel(result, result.DreamJob, "", null),
            string.IsNullOrWhiteSpace(queryDream) ? null : queryDream);

        var tokens = antiforgery.GetAndStoreTokens(http);
        var antiforgeryHtml = tokens.RequestToken is { } token
            ? $"""<input type="hidden" name="{tokens.FormFieldName}" value="{WebUtility.HtmlEncode(token)}" />"""
            : "";

        var body = PupilDroombaanSsrHtml.BuildBody(
            new PupilDroombaanPageModel(result, result.DreamJob, fieldValue, formError),
            culture,
            antiforgeryHtml);

        var doc = PupilLeerlingSsrFrame.RenderDocument(
            culture["LeerlingDroom.Title"],
            body,
            pupil);

        return Results.Content(doc, "text/html; charset=utf-8");
    }

    private static async Task<IResult> PostSaveAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        http.Response.Headers.CacheControl = "no-store";

        if (!http.Request.HasFormContentType)
        {
            return Results.Redirect(PagePath);
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect($"{PagePath}?error=retry");
        }

        var dreamJob = form["DreamJob"].ToString().Trim();
        string key;
        string? displayForRedirect;
        if (string.IsNullOrWhiteSpace(dreamJob))
        {
            key = PupilDreamJobFit.UndecidedKey;
            displayForRedirect = null;
        }
        else if (DreamJobCatalog.TryResolveExactKey(dreamJob, out var resolved))
        {
            key = resolved;
            displayForRedirect = dreamJob;
        }
        else
        {
            key = PupilDreamJobFit.UndecidedKey;
            displayForRedirect = dreamJob;
        }

        try
        {
            var resp = await api.SavePupilDreamJobAsync(key, displayForRedirect, cancellationToken);
            if (resp is null)
            {
                return Results.Redirect($"{PagePath}?error=save");
            }
        }
        catch
        {
            return Results.Redirect($"{PagePath}?error=save");
        }

        if (string.IsNullOrEmpty(displayForRedirect))
        {
            return Results.Redirect(PagePath);
        }

        return Results.Redirect($"{PagePath}?dream={Uri.EscapeDataString(displayForRedirect)}");
    }
}
