using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;

namespace Jobsy.Web.Hosting;

internal static class SchoolPortalRender
{
    public static async Task<IResult> HtmlAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        IFeatureFlags features,
        string path,
        string pageTitle,
        Func<Task<string>> buildBodyAsync,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        culture.InitializeFromRequest(http);

        if (!await SchoolsFeatureGate.IsEnabledAsync(features, ct))
        {
            var shellOff = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
            return Results.Content(
                ScholenPortalSsrFrame.RenderDocument(pageTitle, "", shellOff, culture, path),
                "text/html; charset=utf-8");
        }

        var body = await buildBodyAsync();
        var shell = await ScholenPortalShell.BuildAsync(http, culture, api, features, path);
        return Results.Content(
            ScholenPortalSsrFrame.RenderDocument(pageTitle, body, shell, culture, path),
            "text/html; charset=utf-8");
    }

    public static string ErrorBody(string message)
        => $"""<p class="state-message error">{System.Net.WebUtility.HtmlEncode(message)}</p>""";
}
