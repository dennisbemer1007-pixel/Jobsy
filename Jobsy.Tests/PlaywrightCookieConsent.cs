using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Seeds <c>Jobsy.CookieConsent</c> so <c>html.cookie-consent-known</c> hides the banner
/// before first paint (same path as returning visitors in <c>App.razor</c>).
/// </summary>
internal static class PlaywrightCookieConsent
{
    public const string InitScript = """
        try {
          localStorage.setItem('Jobsy.CookieConsent', 'necessary');
        } catch (e) {}
        """;

    public static async Task AcceptAsync(IBrowserContext context)
    {
        await context.AddInitScriptAsync(InitScript);
    }

    public static async Task AcceptOnPageAsync(IPage page)
    {
        await page.EvaluateAsync("""
            () => {
              try { localStorage.setItem('Jobsy.CookieConsent', 'necessary'); } catch (e) {}
              document.documentElement.classList.add('cookie-consent-known');
              const banner = document.querySelector('.cookie-consent');
              if (banner) banner.style.display = 'none';
            }
            """);
    }
}
