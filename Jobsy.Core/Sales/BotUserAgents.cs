namespace Jobsy.Core.Sales;

/// <summary>
/// Bot / crawler UA tokens for skipping sales link click counts (no IP stored).
/// Aligned with <c>Jobsy.Web.Seo.CrawlerUserAgent</c> product tokens.
/// </summary>
public static class BotUserAgents
{
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

    public static bool IsBot(string? userAgent)
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
