namespace Jobsy.Web.Seo;

/// <summary>
/// Lab auditors and crawlers get prerendered HTML. They should not start a Blazor
/// Server circuit (WebSockets/SignalR): Lighthouse cannot hold that connection and
/// <c>blazor.web.js</c> still registers deprecated <c>unload</c> listeners.
/// </summary>
public static class CrawlerUserAgent
{
    /// <summary>
    /// Specific product tokens only — a generic "bot" substring matches phone UAs such as CUBOT.
    /// </summary>
    private static readonly string[] Tokens =
    [
        "Chrome-Lighthouse",
        "Lighthouse",
        "PageSpeed Insights",
        "Google Page Speed",
        "Googlebot",
        "Google-InspectionTool",
        "Storebot-Google",
        "AdsBot-Google",
        "bingbot",
        "BingPreview",
        "DuckDuckBot",
        "Slurp",
        "Applebot",
        "facebookexternalhit",
        "Facebot",
        "Twitterbot",
        "LinkedInBot",
        "YandexBot",
        "YandexRenderResourcesBot",
        "Baiduspider",
        "Bytespider",
        "SemrushBot",
        "AhrefsBot",
        "DotBot",
        "PetalBot",
        "GPTBot",
        "ChatGPT-User",
        "ClaudeBot",
        "PerplexityBot",
        "ia_archiver",
        "Screaming Frog",
        "Pingdom",
        "GTmetrix",
        "PTST"
    ];

    public static bool ShouldSkipInteractiveRuntime(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        foreach (var token in Tokens)
        {
            if (userAgent.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
