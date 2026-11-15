using Jobsy.Core.Authorization;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkEmployeeEndpoints
{
    public static IEndpointRouteBuilder MapWerkgeverApkEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/loopbaan-apk", LoginPage).AllowAnonymous();
        app.MapGet("/loopbaan-apk/welkom", WelkomPage).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapGet("/loopbaan-apk/test/{block}", GetTestAsync).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapPost("/loopbaan-apk/test/{block}/answer", PostTestAnswerAsync).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapGet("/loopbaan-apk/delen", GetDelenAsync).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapPost("/loopbaan-apk/delen", PostDelenAsync).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapGet("/loopbaan-apk/vergelijking", GetVergelijkingAsync).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        app.MapPost("/loopbaan-apk/welkom", WelcomePost).RequireAuthorization(JobsyPolicies.WerkgeverApkEmployeeSession);
        return app;
    }

    private static async Task<IResult> LoginPage(HttpContext http, JobsyApiClient api, IAntiforgery antiforgery)
    {
        http.Response.Headers.CacheControl = "no-store";
        var auth = await http.AuthenticateAsync(WerkgeverApkAuthDefaults.Scheme);
        if (auth.Succeeded)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Welkom);
        }

        var tokens = antiforgery.GetAndStoreTokens(http);
        var af = $"""<input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">""";
        var error = http.Request.Query["error"].ToString() switch
        {
            "invalid" => "Code klopt niet. Probeer het opnieuw.",
            "staff" => "Log eerst uit als werkgever of kandidaat.",
            "retry" => "Even opnieuw proberen.",
            _ => null
        };
        return Results.Content(
            WerkgeverApkLoginSsrHtml.RenderDocument(error, af),
            "text/html; charset=utf-8");
    }

    private static Task<IResult> WelkomPage(HttpContext http, IAntiforgery antiforgery)
    {
        http.Response.Headers.CacheControl = "no-store";
        var tokens = Chrome(http);
        var html = WerkgeverApkSsrHtml.RenderTemplate("01-welkom.html", tokens);
        var af = antiforgery.GetAndStoreTokens(http);
        var formAf = $"""<input type="hidden" name="{af.FormFieldName}" value="{af.RequestToken}">""";
        html = html.Replace(
            """<button class="btn primary" type="button">Start de interessetest →</button>""",
            $"""<form method="post" action="/loopbaan-apk/welkom">{formAf}<button class="btn primary" type="submit">Start de interessetest →</button></form>""",
            StringComparison.Ordinal);
        html = html.Replace(
            "<div class=\"coach\">",
            "<button type=\"button\" class=\"coach\" data-ssr-coach-open>",
            StringComparison.Ordinal);
        html = html.Replace("</div>\n<div class=\"wm\">", "</button>\n<script src=\"/js/lobsy-coach-ssr.js\" defer></script>\n<div class=\"wm\">", StringComparison.Ordinal);
        return Task.FromResult<IResult>(Results.Content(html, "text/html; charset=utf-8"));
    }

    private static async Task<IResult> WelcomePost(HttpContext http, JobsyApiClient api)
    {
        await api.WerkgeverApkEmployeeWelcomeAsync(http.RequestAborted);
        return Results.Redirect(WerkgeverApkEntryPaths.Test("interesse"));
    }

    private static async Task<IResult> GetTestAsync(
        HttpContext http,
        JobsyApiClient api,
        string block,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var blockKey = NormalizeBlock(block);
        if (blockKey is null)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Welkom);
        }

        var itemId = http.Request.Query["itemId"].ToString();
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = null;
        }

        var q = await api.GetWerkgeverApkQuestionAsync(blockKey, itemId, ct);
        if (q is null)
        {
            return Results.Redirect($"/loopbaan-apk/test/{blockKey}?error=load");
        }

        var tokens = antiforgery.GetAndStoreTokens(http);
        var af = $"""<input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">""";
        var html = WerkgeverApkMockupPages.TestQuestion(
            q,
            af,
            $"/loopbaan-apk/test/{Uri.EscapeDataString(blockKey)}/answer",
            http.Request.Query["error"] == "save" ? "Kon niet opslaan. Probeer opnieuw." : null);
        ApplyChromeTokens(ref html, Chrome(http));
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> PostTestAnswerAsync(
        HttpContext http,
        JobsyApiClient api,
        string block,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        var blockKey = NormalizeBlock(block);
        if (blockKey is null || !http.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect($"/loopbaan-apk/test/{block}?error=save");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var itemId = form["ItemId"].ToString();
        if (!int.TryParse(form["Value"], out var value) || string.IsNullOrWhiteSpace(itemId))
        {
            return Results.Redirect($"/loopbaan-apk/test/{blockKey}?error=save");
        }

        var (ok, _, _) = await api.SaveWerkgeverApkAnswerAsync(blockKey, itemId, value, ct);
        if (ok is null)
        {
            return Results.Redirect($"/loopbaan-apk/test/{blockKey}?error=save&itemId={Uri.EscapeDataString(itemId)}");
        }

        if (!string.IsNullOrWhiteSpace(ok.NextPath))
        {
            return Results.Redirect(ok.NextPath);
        }

        if (!string.IsNullOrWhiteSpace(ok.NextItemId))
        {
            return Results.Redirect($"/loopbaan-apk/test/{blockKey}?itemId={Uri.EscapeDataString(ok.NextItemId)}");
        }

        return Results.Redirect(WerkgeverApkEntryPaths.Delen);
    }

    private static async Task<IResult> GetDelenAsync(HttpContext http, JobsyApiClient api, IAntiforgery antiforgery, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var preview = await api.GetWerkgeverApkSharePreviewAsync(ct);
        if (preview is null)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Test("interesse"));
        }

        var tokens = antiforgery.GetAndStoreTokens(http);
        var af = $"""<input type="hidden" name="{tokens.FormFieldName}" value="{tokens.RequestToken}">""";
        var html = WerkgeverApkMockupPages.Delen(preview, af, "/loopbaan-apk/delen");
        ApplyChromeTokens(ref html, Chrome(http));
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> PostDelenAsync(
        HttpContext http,
        JobsyApiClient api,
        IAntiforgery antiforgery,
        CancellationToken ct)
    {
        if (!http.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Redirect("/loopbaan-apk/delen");
        }

        var form = await http.Request.ReadFormAsync(ct);
        var share = form.ContainsKey("share");
        var (ok, _, _) = await api.SetWerkgeverApkShareAsync(share, ct);
        if (ok is null)
        {
            return Results.Redirect("/loopbaan-apk/delen");
        }

        return Results.Redirect(share ? WerkgeverApkEntryPaths.Vergelijking : WerkgeverApkEntryPaths.Delen);
    }

    private static async Task<IResult> GetVergelijkingAsync(HttpContext http, JobsyApiClient api, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var comparison = await api.GetWerkgeverApkComparisonAsync(ct);
        if (comparison is null)
        {
            return Results.Redirect(WerkgeverApkEntryPaths.Delen);
        }

        var manager = http.User.FindFirst(WerkgeverApkClaimTypes.TeamName)?.Value ?? "je leidinggevende";
        var html = WerkgeverApkMockupPages.Vergelijking(comparison, manager);
        ApplyChromeTokens(ref html, Chrome(http));
        return Results.Content(html, "text/html; charset=utf-8");
    }

    private static string? NormalizeBlock(string block)
    {
        if (PupilMboHboEntryPaths.TryParseBlockRoute(block, out var parsed))
        {
            return PupilMboHboProgressState.BlockKey(parsed);
        }

        return WerkgeverApkEntryPaths.EmployeeBlocks.Any(b =>
                   string.Equals(PupilMboHboProgressState.BlockKey(b), block, StringComparison.OrdinalIgnoreCase))
               ? block.ToLowerInvariant()
               : null;
    }

    private static Dictionary<string, string> Chrome(HttpContext http)
        => new(StringComparer.Ordinal)
        {
            ["company"] = http.User.FindFirst(WerkgeverApkClaimTypes.CompanyName)?.Value ?? "",
            ["team"] = http.User.FindFirst(WerkgeverApkClaimTypes.TeamName)?.Value ?? "",
            ["code"] = http.User.FindFirst(WerkgeverApkClaimTypes.CodeDisplay)?.Value ?? "",
        };

    private static void ApplyChromeTokens(ref string html, IReadOnlyDictionary<string, string> chrome)
    {
        html = html.Replace("Demo Logistiek B.V.", E(chrome.GetValueOrDefault("company")), StringComparison.Ordinal);
        html = html.Replace("Team Logistiek", E(chrome.GetValueOrDefault("team")), StringComparison.Ordinal);
        html = html.Replace("Code VOOR-BEELD", E(chrome.GetValueOrDefault("code")), StringComparison.Ordinal);
        html = html.Replace("chip-ex", "chip-code", StringComparison.Ordinal);
        html = html.Replace(
            ">Voorbeeld</span>",
            $">{E(chrome.GetValueOrDefault("code"))}</span>",
            StringComparison.Ordinal);
    }

    private static string E(string? value) => System.Net.WebUtility.HtmlEncode(value ?? "");
}