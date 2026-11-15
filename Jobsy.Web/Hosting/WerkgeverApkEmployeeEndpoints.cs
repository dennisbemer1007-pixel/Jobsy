using Jobsy.Core.Authorization;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class WerkgeverApkEmployeeEndpoints
{
    public static IEndpointRouteBuilder MapWerkgeverApkEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/loopbaan-apk", LoginPage).AllowAnonymous();
        app.MapGet("/loopbaan-apk/welkom", (HttpContext http) => Template(http, "01-welkom.html"))
            .RequireAuthorization(WerkgeverApkAuthDefaults.Scheme);
        app.MapGet("/loopbaan-apk/test/{block}", (HttpContext http, string block) => Template(http, "02-vraag.html", new Dictionary<string, string> { ["block"] = block }))
            .RequireAuthorization(WerkgeverApkAuthDefaults.Scheme);
        app.MapGet("/loopbaan-apk/delen", (HttpContext http) => Template(http, "03-delen.html"))
            .RequireAuthorization(WerkgeverApkAuthDefaults.Scheme);
        app.MapGet("/loopbaan-apk/vergelijking", (HttpContext http) => Template(http, "06-vergelijking.html"))
            .RequireAuthorization(WerkgeverApkAuthDefaults.Scheme);
        app.MapPost("/loopbaan-apk/welkom", WelcomePost).RequireAuthorization(WerkgeverApkAuthDefaults.Scheme);
        return app;
    }

    private static async Task<IResult> LoginPage(HttpContext http, IAntiforgery antiforgery)
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
        return Results.Content(WerkgeverApkSsrHtml.RenderLoginPage(error, af), "text/html; charset=utf-8");
    }

    private static async Task<IResult> WelcomePost(HttpContext http, JobsyApiClient api)
    {
        await api.WerkgeverApkEmployeeWelcomeAsync(http.RequestAborted);
        return Results.Redirect(WerkgeverApkEntryPaths.Test("interesse"));
    }

    private static IResult Template(
        HttpContext http,
        string template,
        IReadOnlyDictionary<string, string>? extra = null)
    {
        http.Response.Headers.CacheControl = "no-store";
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["company"] = http.User.FindFirst(WerkgeverApkClaimTypes.CompanyName)?.Value ?? "",
            ["team"] = http.User.FindFirst(WerkgeverApkClaimTypes.TeamName)?.Value ?? "",
            ["code"] = http.User.FindFirst(WerkgeverApkClaimTypes.CodeDisplay)?.Value ?? "",
        };
        if (extra is not null)
        {
            foreach (var (k, v) in extra)
            {
                tokens[k] = v;
            }
        }

        var html = WerkgeverApkSsrHtml.RenderTemplate(template, tokens);
        return Results.Content(html, "text/html; charset=utf-8");
    }
}