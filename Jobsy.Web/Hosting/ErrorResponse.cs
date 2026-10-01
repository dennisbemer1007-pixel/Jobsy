using System.Net;
using System.Text;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Response contract for every page rendered through <c>ErrorLayout</c> (§IA):
/// a real status code, <c>X-Robots-Tag: noindex</c> and a cache policy that never stores a failure.
/// </summary>
public static class ErrorResponse
{
    public const string NoIndex = "noindex, nofollow";

    /// <summary>Codes <c>/status/{code}</c> answers with directly; anything else becomes 404.</summary>
    public static readonly int[] DirectStatusCodes =
    [
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status410Gone,
        StatusCodes.Status429TooManyRequests,
        StatusCodes.Status500InternalServerError,
        StatusCodes.Status503ServiceUnavailable
    ];

    public static int NormalizeStatusCode(int code)
        => Array.IndexOf(DirectStatusCodes, code) >= 0 ? code : StatusCodes.Status404NotFound;

    public static void ApplyHeaders(HttpContext? http)
    {
        if (http is null || http.Response.HasStarted)
        {
            return;
        }

        http.Response.Headers["X-Robots-Tag"] = NoIndex;
        http.Response.Headers.CacheControl = CachePolicyFor(http.Response.StatusCode);
    }

    /// <summary>5xx must never be stored; 4xx may be revalidated.</summary>
    public static string CachePolicyFor(int statusCode)
        => statusCode >= 500 ? "no-store" : "no-cache, no-store";

    /// <summary>
    /// Last resort when <c>ErrorLayout</c> itself cannot render: hard-coded nl + en markup,
    /// no stylesheet, no script, no request data. Keeps the status code the caller already set.
    /// </summary>
    public static string MinimalHtml(int statusCode)
    {
        var nl = statusCode >= 500
            ? "Er ging iets mis. Probeer het zo nog eens."
            : "Deze pagina bestaat niet.";
        var en = statusCode >= 500
            ? "Something went wrong. Please try again in a moment."
            : "This page does not exist.";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<meta name=\"robots\" content=\"noindex, nofollow\">");
        sb.Append("<title>Lobsy</title></head><body>");
        sb.Append("<h1 lang=\"nl\">").Append(WebUtility.HtmlEncode(nl)).Append("</h1>");
        sb.Append("<p lang=\"en\">").Append(WebUtility.HtmlEncode(en)).Append("</p>");
        sb.Append("<p><a href=\"/\" lang=\"nl\">Naar de startpagina</a> &middot; ");
        sb.Append("<a href=\"/\" lang=\"en\">To the home page</a></p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
