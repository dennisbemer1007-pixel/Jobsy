using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Dennis layout gate: report largest vertical empty strip (px) in banenkaart/discovery chrome.
/// Empty reserved blocks (containers with height but no meaningful content) count as empty.
/// Does not inspect map canvas pixels — only structural layout gaps in the page shell.
/// </summary>
public static class AccLayoutEmptyStripAudit
{
    /// <summary>Above this gap between major chrome bands the page fails UAT.</summary>
    public const int MaxAllowedEmptyStripPx = 48;

    public static async Task<EmptyStripResult> MeasureBanenkaartAsync(IPage page)
    {
        var raw = await page.EvaluateAsync<EmptyStripResult>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const vh = window.innerHeight;
              const vw = window.innerWidth;
              if (!root || vh < 200) {
                return { largest_empty_strip_px: 0, empty_holder_px: 0, note: 'no-root' };
              }

              function isVisible(el) {
                if (!el || !el.getBoundingClientRect) return false;
                const r = el.getBoundingClientRect();
                if (r.width < 8 || r.height < 4) return false;
                const st = getComputedStyle(el);
                if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') return false;
                return true;
              }

              function hasMeaningfulContent(el) {
                if (!el) return false;
                const text = (el.innerText || '').replace(/\s+/g, ' ').trim();
                if (text.length > 0) return true;
                if (el.querySelector('canvas, img, svg, video, iframe, button, input, select, textarea, [data-testid]')) {
                  return true;
                }
                return false;
              }

              let emptyHolderPx = 0;
              root.querySelectorAll('*').forEach(el => {
                if (!isVisible(el)) return;
                const r = el.getBoundingClientRect();
                if (r.height < 24 || r.width < vw * 0.35) return;
                if (hasMeaningfulContent(el)) return;
                const childBlocks = Array.from(el.children).filter(isVisible);
                if (childBlocks.length === 0 && r.height > emptyHolderPx) {
                  emptyHolderPx = Math.round(r.height);
                }
              });

              const bandSelectors = [
                '[data-testid=kb-panel-head]',
                '[data-testid=kb-map-top-chrome]',
                '.map-pane',
                '[data-testid=kb-map-strip]',
                '[data-testid=kb-map-legend-root] aside',
                '[data-testid=vacancy-list]',
                '.kb-list-shell',
                '[data-testid=kb-empty-travel]'
              ];
              const bands = [];
              bandSelectors.forEach(sel => {
                root.querySelectorAll(sel).forEach(el => {
                  if (!isVisible(el)) return;
                  const r = el.getBoundingClientRect();
                  if (r.width < vw * 0.2) return;
                  bands.push({ top: r.top, bottom: r.bottom });
                });
              });
              bands.sort((a, b) => a.top - b.top);

              let maxGap = Math.round(bands.length ? Math.max(0, bands[0].top) : 0);
              for (let i = 1; i < bands.length; i++) {
                const gap = bands[i].top - bands[i - 1].bottom;
                if (gap > maxGap) maxGap = Math.round(gap);
              }
              if (bands.length) {
                const tail = vh - bands[bands.length - 1].bottom;
                if (tail > maxGap) maxGap = Math.round(tail);
              }

              const largest = Math.max(maxGap, emptyHolderPx);
              return {
                largest_empty_strip_px: largest,
                empty_holder_px: emptyHolderPx,
                band_gap_px: maxGap,
                note: ''
              };
            }
            """);

        return raw ?? new EmptyStripResult();
    }

    public sealed class EmptyStripResult
    {
        public int largest_empty_strip_px { get; set; }
        public int empty_holder_px { get; set; }
        public int band_gap_px { get; set; }
        public string? note { get; set; }
    }
}
