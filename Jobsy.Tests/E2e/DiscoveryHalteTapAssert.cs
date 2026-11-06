using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class DiscoveryHalteTapAssert
{
    internal static async Task AssertHalteTapDoesNotReloadAsync(IPage page, bool mobile)
    {
        await page.EvaluateAsync(
            "window.scrollTo(0, Math.min(520, Math.max(0, document.body.scrollHeight - window.innerHeight)))");
        var scrollBefore = await page.EvaluateAsync<double>("window.scrollY");
        Assert.True(scrollBefore > 80, $"Expected scrolled page before tap, scrollY={scrollBefore}");

        var documentNavigations = 0;
        void OnRequest(object? _, IRequest request)
        {
            if (request.IsNavigationRequest
                && string.Equals(request.ResourceType, "document", StringComparison.OrdinalIgnoreCase))
            {
                documentNavigations++;
            }
        }

        page.Request += OnRequest;
        try
        {
            if (mobile)
            {
                var circleImg = page.Locator(".discovery-mobile-stop[data-phase=rots] .discovery-mobile-stop__circle img");
                await circleImg.ScrollIntoViewIfNeededAsync();
                await circleImg.TapAsync();
            }
            else
            {
                var link = page.Locator(".journey-route__desktop-only a.discovery-halte-link[data-phase=rots]").First;
                await link.ScrollIntoViewIfNeededAsync();
                await link.ClickAsync();
            }
            await page.WaitForFunctionAsync(
                "() => window.location.search.includes('fase=rots')",
                null,
                new() { Timeout = 15_000 });
        }
        finally
        {
            page.Request -= OnRequest;
        }

        Assert.Equal(0, documentNavigations);
        Assert.Contains("fase=rots", page.Url, StringComparison.OrdinalIgnoreCase);
        var scrollAfter = await page.EvaluateAsync<double>("window.scrollY");
        Assert.True(scrollAfter > 40, $"Expected scroll position preserved after tap, scrollY={scrollAfter}");
        var panelSelector = mobile
            ? "#discovery-panel-rots:not([hidden])"
            : ".journey-route__desktop-only .discovery-phase-panel[data-phase=rots]:not([hidden])";
        await page.Locator(panelSelector).WaitForAsync(new() { Timeout = 15_000 });
    }
}
