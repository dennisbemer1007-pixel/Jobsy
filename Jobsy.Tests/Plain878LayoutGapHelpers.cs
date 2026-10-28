using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Detects empty vertical bands below the shell header (Dennis: no reserved empty space).</summary>
internal static class Plain878LayoutGapHelpers
{
    internal sealed record GapReport(
        string UnderHeaderSummary,
        int GapBelowHeaderPx,
        int LargestEmptyStripPx,
        string? EmptyBandHint,
        bool Pass)
    {
        public string OneLineSummary =>
            $"Direct onder de header: {UnderHeaderSummary}; grootste lege strook: {LargestEmptyStripPx}px (ruimte tot inhoud: {GapBelowHeaderPx}px)"
            + (string.IsNullOrWhiteSpace(EmptyBandHint) ? "." : $", element: {EmptyBandHint}.");
    }

    /// <summary>Fail when an empty band &gt; 12px or idle gap below header &gt; 32px without content.</summary>
    internal const int MaxEmptyStripPx = 12;
    internal const int MaxIdleGapBelowHeaderPx = 32;

    internal static async Task<GapReport> MeasureAsync(IPage page)
    {
        var dto = await page.EvaluateAsync<GapDto>(
            """
            () => {
              const header = document.querySelector(
                'header.wg-topbar--candidate, header.wg-topbar, header.app-header, header.kb-mockup-top, header.top.kb-mockup-top, .pub-header'
              );
              const headerBottom = header ? header.getBoundingClientRect().bottom : 0;
              const ignoreTags = new Set(['SCRIPT', 'STYLE', 'META', 'LINK', 'NOSCRIPT', 'SVG', 'PATH']);
              const contentSel = 'img, canvas, map, video, iframe, button, input, select, textarea, a[href], .vc, .kb-list-row, .step, .card, .ph, .panel, .map-pane, .maplibregl-canvas, .cp-plain878 main, main.wrap, .vacancy-pane, .discovery-toolbar, [data-testid=kb-panel-head]';

              const isVisible = (el) => {
                if (!(el instanceof HTMLElement)) return false;
                const r = el.getBoundingClientRect();
                if (r.width < 2 || r.height < 2) return false;
                const s = getComputedStyle(el);
                return s.display !== 'none' && s.visibility !== 'hidden' && s.opacity !== '0';
              };

              const hasMeaningfulContent = (el) => {
                if (!(el instanceof HTMLElement)) return false;
                if (el.matches(contentSel)) return true;
                if (el.querySelector(contentSel)) return true;
                const text = (el.innerText || '').replace(/\s+/g, ' ').trim();
                return text.length > 0;
              };

              const isEmptyBand = (el) => {
                if (!(el instanceof HTMLElement)) return false;
                if (!isVisible(el)) return false;
                const r = el.getBoundingClientRect();
                if (r.top < headerBottom - 1) return false;
                if (r.top > headerBottom + Math.min(240, window.innerHeight * 0.4)) return false;
                if (r.height <= 8) return false;
                if (r.width < window.innerWidth * 0.55) return false;
                if (hasMeaningfulContent(el)) return false;
                if (el.matches('.app-shell, .app-main, .app-main__content, .pub-theme, .pub-main, .jobsy-discovery, .main.kb-lagen-main, .wrap, .passport-page-wrap, .cp-plain878, .vacancy-pane, .map-pane')) {
                  return false;
                }
                return true;
              };

              let firstContentTop = window.innerHeight;
              let underHeader = 'geen header gevonden';
              let largestEmpty = 0;
              let emptyBandHint = '';

              const nodes = Array.from(document.body.querySelectorAll('*'));
              for (const el of nodes) {
                if (ignoreTags.has(el.tagName)) continue;
                if (el.closest('.cookie-consent, .bottom-nav, .lobsy-coach, .feedback-widget, .lobsy-assistant')) continue;
                if (el.matches('[aria-hidden="true"]')) continue;
                if (!isVisible(el)) continue;
                const r = el.getBoundingClientRect();
                if (r.bottom <= headerBottom + 1) continue;

                if (hasMeaningfulContent(el)) {
                  if (el.matches('.app-shell, .app-main, .app-main__content, .pub-theme, .pub-main, .circuit-error-boundary')) {
                    continue;
                  }
                  if (r.top < firstContentTop) {
                    firstContentTop = r.top;
                    const tag = el.tagName.toLowerCase();
                    const cls = (el.className && String(el.className).split(/\s+/).filter(Boolean).slice(0, 2).join('.')) || '';
                    underHeader = cls ? `${tag}.${cls}` : tag;
                  }
                }

                if (isEmptyBand(el)) {
                  if (r.height > largestEmpty) {
                    largestEmpty = r.height;
                    emptyBandHint = el.tagName.toLowerCase()
                      + (el.className ? '.' + String(el.className).trim().split(/\s+/).slice(0, 3).join('.') : '')
                      + '|parent:' + (el.parentElement?.className || el.parentElement?.tagName || '');
                  }
                }
              }

              const gapBelowHeader = Math.max(0, Math.round(firstContentTop - headerBottom));
              return {
                underHeader,
                gapBelowHeader,
                largestEmptyStrip: Math.round(largestEmpty),
                emptyBandHint,
              };
            }
            """);

        var pass = dto.LargestEmptyStrip <= MaxEmptyStripPx
                   && dto.GapBelowHeader <= MaxIdleGapBelowHeaderPx;
        return new GapReport(
            dto.UnderHeader,
            dto.GapBelowHeader,
            dto.LargestEmptyStrip,
            dto.EmptyBandHint,
            pass);
    }

    internal static async Task<bool> CoachOverlapsPlainPaspoortContentAsync(IPage page)
    {
        return await page.EvaluateAsync<bool>(
            """
            () => {
              const coach = document.querySelector('.lobsy-coach-dock, .lobsy-coach, .lobsy-coach-ssr-tab, [data-testid="lobsy-coach-dock"]');
              if (!coach) {
                return false;
              }
              const targets = document.querySelectorAll('.cp-plain878 .chip, .cp-plain878 .ans, .cp-plain878 .mgw');
              for (const el of targets) {
                const r = el.getBoundingClientRect();
                if (r.width < 4 || r.height < 4) continue;
                const x = Math.min(Math.max(r.left + r.width / 2, 0), window.innerWidth - 1);
                const y = Math.min(Math.max(r.top + r.height / 2, 0), window.innerHeight - 1);
                const top = document.elementFromPoint(x, y);
                if (top && (coach === top || coach.contains(top))) {
                  return true;
                }
              }
              return false;
            }
            """);
    }

    private sealed class GapDto
    {
        public string UnderHeader { get; set; } = "";
        public int GapBelowHeader { get; set; }
        public int LargestEmptyStrip { get; set; }
        public string EmptyBandHint { get; set; } = "";
    }
}
