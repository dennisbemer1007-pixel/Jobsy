using Jobsy.Core.Authorization;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkPakketEndpoints
{
    public static IEndpointRouteBuilder MapWerkgeverApkPakketEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/werkgever-apk/voorbeeld/{mockup}", PreviewPage).AllowAnonymous();

        app.MapGet("/loopbaan-apk/vraag", VraagPage)
            .RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapPost("/loopbaan-apk/vraag/answer", VraagAnswerPost)
            .RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapGet("/loopbaan-apk/pauze", PauzePage)
            .RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);

        app.MapGet("/werkgever/loopbaan-apk/jullie-pakket", ManagerPakketPage)
            .RequireAuthorization(p =>
            {
                p.RequireAuthenticatedUser();
                p.RequireRole(JobsyRoles.EnterpriseManager, JobsyRoles.Admin);
            });

        app.MapGet("/admin/bedrijven/{companyId:guid}/loopbaan-apk/pakket", AdminPakketPage)
            .RequireAuthorization(p => p.RequireRole(JobsyRoles.Admin));

        return app;
    }

    private static IResult PreviewPage(string mockup)
    {
        var file = mockup switch
        {
            "11" => "11-pakket-kiezen.html",
            "12" => "12-werkgever-pakket.html",
            "13" => "13-ab-vraag.html",
            "13b" => "13b-ab-pauze.html",
            _ => null
        };
        if (file is null)
        {
            return Results.NotFound();
        }

        var html = WerkgeverApkPakketMockupPages.Preview(file);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> VraagPage(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var itemId = http.Request.Query["itemId"].ToString();
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = null;
        }

        var q = await api.GetWerkgeverApkAbQuestionAsync(itemId, ct);
        if (q is null)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Welkom);
        }

        var tokens = antiforgery.GetAndStoreTokens(http);
        var af = $"""<input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">""";
        var html = WerkgeverApkPakketMockupPages.AbQuestion(
            q,
            af,
            "/loopbaan-apk/vraag/answer",
            http.Request.Query["error"] == "save" ? "Kon niet opslaan. Probeer opnieuw." : null);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> VraagAnswerPost(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!http.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect("/loopbaan-apk/vraag?error=save");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var itemId = form["ItemId"].ToString();
        if (!int.TryParse(form["Value"], out var value) || string.IsNullOrWhiteSpace(itemId))
        {
            return Results.Redirect("/loopbaan-apk/vraag?error=save");
        }

        var (ok, _, _) = await api.SaveWerkgeverApkAbAnswerAsync(itemId, value, ct);
        if (ok is null)
        {
            return Results.Redirect($"/loopbaan-apk/vraag?error=save&itemId={Uri.EscapeDataString(itemId)}");
        }

        if (!string.IsNullOrWhiteSpace(ok.NextPath))
        {
            return Results.Redirect(ok.NextPath);
        }

        if (!string.IsNullOrWhiteSpace(ok.NextItemId))
        {
            return Results.Redirect($"/loopbaan-apk/vraag?itemId={Uri.EscapeDataString(ok.NextItemId)}");
        }

        return Results.Redirect(WerkgeverApkEntryPaths.Delen);
    }

    private static async Task<IResult> PauzePage(HttpContext http, JobsyApiClient api, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (!int.TryParse(http.Request.Query["after"], out var after) || after < 1)
        {
            after = 2;
        }

        var pause = await api.GetWerkgeverApkPauseAsync(after, ct);
        if (pause is null)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Vraag);
        }

        var continuePath = $"/loopbaan-apk/vraag";
        var html = WerkgeverApkPakketMockupPages.AbPause(pause, continuePath);
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static IResult ManagerPakketPage()
        => Results.Content(WerkgeverApkPakketMockupPages.Preview("12-werkgever-pakket.html"), "text/html; charset=utf-8");

    private static IResult AdminPakketPage(Guid companyId)
    {
        _ = companyId;
        return Results.Content(WerkgeverApkPakketMockupPages.Preview("11-pakket-kiezen.html"), "text/html; charset=utf-8");
    }
}