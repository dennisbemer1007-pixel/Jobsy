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
              const activeVig = list.querySelector('[data-testid="discovery-active-vignette"]');
              if (activeVig) {
                const vigRect = activeVig.getBoundingClientRect();
                if (vigRect.left < -1) {
                  problems.push(`active ring clipped left (${vigRect.left})`);
                }
                if (vigRect.right > vw + 1) {
                  problems.push(`active ring clipped right (${vigRect.right} > ${vw})`);
                }
                const circle = activeVig.closest('.discovery-mobile-stop__circle');
                if (circle) {
                  const circleRect = circle.getBoundingClientRect();
                  if (circleRect.left < -1 || circleRect.right > vw + 1) {
                    problems.push(`active circle outside viewport (${circleRect.left}-${circleRect.right})`);
                  }
                }
              }

              list.querySelectorAll('.discovery-mobile-stop__circle').forEach((circle, index) => {
                const img = circle.querySelector('img');
                if (!img) {
                  problems.push(`stop ${index}: circle img missing`);
                  return;
                }
                const circleRect = circle.getBoundingClientRect();
                const imgRect = img.getBoundingClientRect();
                const circleCx = circleRect.left + circleRect.width / 2;
                const circleCy = circleRect.top + circleRect.height / 2;
                const imgCx = imgRect.left + imgRect.width / 2;
                const imgCy = imgRect.top + imgRect.height / 2;
                if (Math.abs(circleCx - imgCx) > 1 || Math.abs(circleCy - imgCy) > 1) {
                  problems.push(`stop ${index}: circle/img center mismatch (${Math.abs(circleCx - imgCx)}, ${Math.abs(circleCy - imgCy)}px)`);
                }
                if (Math.abs(circleRect.width - 44) > 1 || Math.abs(circleRect.height - 44) > 1) {
                  problems.push(`stop ${index}: circle not 44px (${circleRect.width}x${circleRect.height})`);
                }
                const vig = circle.querySelector('.journey-route__vignette');
                if (vig) {
                  const vigStyle = getComputedStyle(vig);
                  if (vigStyle.boxShadow && vigStyle.boxShadow !== 'none') {
                    problems.push(`stop ${index}: vignette still has box-shadow`);
                  }
                }
              });

              const testsBlock = document.querySelector('.journey-route__tests');
              if (testsBlock && footer) {
                const testsBottom = testsBlock.getBoundingClientRect().bottom + scrollY;
                const footerTopGap = footerTop - testsBottom;
                if (footerTopGap > 96) {
                  problems.push(`large empty gap before footer (${Math.round(footerTopGap)}px after Je tests)`);
                }
              }

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
                const rail = stop.querySelector('.discovery-mobile-stop__rail');
                const text = stop.querySelector('.discovery-mobile-stop__text');
                const line = stop.querySelector('.discovery-mobile-stop__line');
                if (!rail || !text) {
                  return;
                }

                const railRect = rail.getBoundingClientRect();
                const textRect = text.getBoundingClientRect();
                if (textRect.left < railRect.right - 2) {
                  problems.push(`stop ${index}: text overlaps rail column (${textRect.left} < ${railRect.right})`);
                }

                if (line) {
                  const lineRect = line.getBoundingClientRect();
                  if (textRect.left < lineRect.right + 2) {
                    problems.push(`stop ${index}: text overlaps connector line`);
                  }
                }
              });

              return problems.length > 0 ? problems.join('; ') : null;
            }
            """);

        Assert.True(string.IsNullOrEmpty(reason), reason ?? "unknown mobile layout problem");
    }
}
