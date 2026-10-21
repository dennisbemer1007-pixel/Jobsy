using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Shared Playwright browser contexts for desktop and iPhone-like mobile (390px, touch).</summary>
public static class PlaywrightViewportContexts
{
    public const int MobileWidth = 390;
    public const int MobileHeight = 844;
    public const int DesktopWidth = 1280;
    public const int DesktopHeight = 900;

    public static BrowserNewContextOptions ForSize(int width, int height) => new()
    {
        ViewportSize = new ViewportSize { Width = width, Height = height },
        IsMobile = width < 500,
        HasTouch = width < 500,
        IgnoreHTTPSErrors = true,
    };

    public static BrowserNewContextOptions Mobile() => ForSize(MobileWidth, MobileHeight);

    public static BrowserNewContextOptions Desktop() => ForSize(DesktopWidth, DesktopHeight);
}
