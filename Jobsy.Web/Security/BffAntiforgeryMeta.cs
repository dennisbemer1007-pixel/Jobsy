using System.Net;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Security;

public static class BffAntiforgeryMeta
{
    public const string MetaName = "jobsy-request-verification-token";

    public static string RenderMetaTag(HttpContext http, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(http);
        var token = tokens.RequestToken ?? string.Empty;
        return $"<meta name=\"{MetaName}\" content=\"{WebUtility.HtmlEncode(token)}\" />";
    }
}
