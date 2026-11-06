using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Full-page Playwright shots repeat <c>position:sticky/fixed</c> chrome in the scroll — disable it for captures.
/// </summary>
internal static class PlaywrightScreenshotSanitizer
{
    private const string Css = """
        .jobsy-app-header,
        .app-header,
        .site-header,
        .pub-header,
        .bottom-nav,
        .passport-tabs,
        .lobsy-coach-dock,
        .feedback-widget,
        .feedback-widget__tab--edge,
        .lobsy-assistant-tab,
        .lobsy-assistant-tab--edge,
        .cookie-consent,
        .passport-layout__card--desktop {
          position: static !important;
          inset: auto !important;
          transform: none !important;
          box-shadow: none !important;
        }

        .bottom-nav,
        .lobsy-coach-dock,
        .feedback-widget,
        .lobsy-assistant-tab,
        .pub-fixed-bottom {
          display: none !important;
        }
        """;

    public static async Task PreparePageAsync(IPage page)
    {
        await page.AddStyleTagAsync(new() { Content = Css });
        await page.EvaluateAsync("""
            () => {
              document.querySelectorAll(
              '.bottom-nav, .lobsy-coach-dock, .feedback-widget, .lobsy-assistant-tab'
            ).forEach(el => el.remove());
            }
            """);
    }

    public static async Task CaptureFullPageAsync(IPage page, string path)
    {
        await PreparePageAsync(page);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path, FullPage = true });
    }
}
