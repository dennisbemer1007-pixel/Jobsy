using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class DiscoveryMobileLayoutAssert
{
    internal static async Task AssertMobile390LayoutAsync(IPage page)
    {
        var reason = await page.EvaluateAsync<string?>(
            """
            () => {
              const route = document.querySelector('[data-testid="discovery-overview-route"]')
                || document.querySelector('.discovery-mobile-route');
              const journey = document.querySelector('[data-discovery-ssr="1"]');
              const footer = document.querySelector('.app-footer');
              if (!route || !footer) {
                return 'route or footer missing';
              }

              const scrollY = window.scrollY;
              const footerTop = footer.getBoundingClientRect().top + scrollY;
              const contentBottom = Math.max(
                route.getBoundingClientRect().bottom + scrollY,
                journey ? journey.getBoundingClientRect().bottom + scrollY : 0,
              );
              if (footerTop < contentBottom - 2) {
                return `footer overlaps content (footerTop=${footerTop}, contentBottom=${contentBottom})`;
              }

              const openPanel = route.querySelector('.journey-route-mobile__inline:not([hidden])');
              if (openPanel) {
                const panelBottom = openPanel.getBoundingClientRect().bottom + scrollY;
                if (footerTop < panelBottom - 2) {
                  return `footer overlaps panel (footerTop=${footerTop}, panelBottom=${panelBottom})`;
                }
              }

              const problems = [];
              const vw = window.innerWidth;
              const list = route.querySelector('.discovery-mobile-stops') || route;
              list.querySelectorAll('.discovery-mobile-stop .discovery-mobile-stop__circle').forEach((circle, index) => {
                const rect = circle.getBoundingClientRect();
                if (rect.left < -1) {
                  problems.push(`stop ${index}: circle clipped left (${rect.left})`);
                }
                if (rect.right > vw + 1) {
                  problems.push(`stop ${index}: circle clipped right (${rect.right} > ${vw})`);
                }
                const badge = circle.querySelector('.journey-route__vignette.is-done');
                if (badge) {
                  const badgeRect = badge.getBoundingClientRect();
                  if (badgeRect.left < -1 || badgeRect.right > vw + 1) {
                    problems.push(`stop ${index}: done badge outside viewport`);
                  }
                }
              });
              list.querySelectorAll('.discovery-mobile-stop').forEach((stop, index) => {
                const circle = stop.querySelector('.discovery-mobile-stop__circle .journey-route__vignette');
                const text = stop.querySelector('.discovery-mobile-stop__text');
                const line = stop.querySelector('.discovery-mobile-stop__line');
                if (!circle || !text) {
                  return;
                }

                const circleRect = circle.getBoundingClientRect();
                const textRect = text.getBoundingClientRect();
                if (textRect.left < circleRect.right - 2) {
                  problems.push(`stop ${index}: text overlaps circle`);
                }

                if (line) {
                  const lineRect = line.getBoundingClientRect();
                  const pad = 2;
                  const lineBox = {
                    left: lineRect.left - pad,
                    right: lineRect.right + pad,
                    top: lineRect.top - pad,
                    bottom: lineRect.bottom + pad,
                  };
                  const intersects = !(
                    textRect.right <= lineBox.left
                    || textRect.left >= lineBox.right
                    || textRect.bottom <= lineBox.top
                    || textRect.top >= lineBox.bottom
                  );
                  if (intersects) {
                    problems.push(`stop ${index}: text overlaps connector line`);
                  }
                }
              });

              return problems.length > 0 ? problems.join('; ') : null;
            }
            """);

        Assert.Null(reason);
    }
}
