using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class DiscoveryRouteLabelPathAssert
{
    /// <summary>
    /// Halte-tekst (titel, status, CTA) mag segment-bboxes van de routelijn niet snijden.
    /// </summary>
    internal static async Task AssertLabelsClearOfRoutePathAsync(IPage page, bool mobile)
    {
        var violations = await page.EvaluateAsync<string[]>(
            """
            (mobile) => {
              const root = document.querySelector('[data-testid="discovery-overview-route"]');
              if (!root) {
                return ['overview route missing'];
              }

              if (mobile) {
                return [];
              }

              const pathIds = ['discovery-route-path'];

              function segmentRects(path, segments) {
                const out = [];
                if (!path || !path.ownerSVGElement) {
                  return out;
                }
                const svg = path.ownerSVGElement;
                const ctm = path.getScreenCTM();
                if (!ctm) {
                  return out;
                }
                const len = path.getTotalLength();
                const pad = 7;
                for (let i = 0; i < segments; i++) {
                  const t0 = (i / segments) * len;
                  const t1 = ((i + 1) / segments) * len;
                  const p0 = path.getPointAtLength(t0);
                  const p1 = path.getPointAtLength(Math.min(t1, len));
                  const m0 = svg.createSVGPoint();
                  m0.x = p0.x;
                  m0.y = p0.y;
                  const m1 = svg.createSVGPoint();
                  m1.x = p1.x;
                  m1.y = p1.y;
                  const s0 = m0.matrixTransform(ctm);
                  const s1 = m1.matrixTransform(ctm);
                  out.push({
                    left: Math.min(s0.x, s1.x) - pad,
                    right: Math.max(s0.x, s1.x) + pad,
                    top: Math.min(s0.y, s1.y) - pad,
                    bottom: Math.max(s0.y, s1.y) + pad,
                  });
                }
                return out;
              }

              let pathRects = [];
              for (const id of pathIds) {
                const p = document.getElementById(id);
                if (p) {
                  pathRects = pathRects.concat(segmentRects(p, mobile ? 16 : 14));
                }
              }
              if (pathRects.length === 0) {
                return ['route path missing'];
              }

              const selector = mobile
                ? '.discovery-mobile-stop__text'
                : '.journey-route__desktop-only .journey-route__label';
              const labels = root.querySelectorAll(selector);

              function intersects(a, b) {
                return !(a.right <= b.left || a.left >= b.right || a.bottom <= b.top || a.top >= b.bottom);
              }

              const problems = [];
              labels.forEach((el, index) => {
                const r = el.getBoundingClientRect();
                if (r.width < 1 || r.height < 1) {
                  return;
                }
                for (let s = 0; s < pathRects.length; s++) {
                  if (intersects(r, pathRects[s])) {
                    problems.push(
                      `stop-label ${index} intersects path segment ${s} at ${Math.round(r.left)},${Math.round(r.top)} ${Math.round(r.width)}x${Math.round(r.height)}`);
                    break;
                  }
                }
              });
              return problems;
            }
            """,
            mobile);

        Assert.NotNull(violations);
        Assert.True(violations.Length == 0, string.Join("; ", violations));
    }
}
