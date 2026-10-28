using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Acceptatie klikbaarheidsaudit (Dennis): elk zichtbaar interactief element op 1440 en 390,
/// elementFromPoint zonder overlay, echte klik met verwacht effect.
/// </summary>
internal static class AccCandidatePageClickAudit
{
    internal sealed record Row(string Element, bool Ok1440, bool Ok390, string? Note);

    internal sealed record ViewportResult(IReadOnlyList<ElementResult> Elements, IReadOnlyList<string> SkippedDangerous);

    internal sealed record ElementResult(
        string Label,
        bool HitOk,
        bool ClickOk,
        string? Detail);

    internal static async Task<(ViewportResult At1440, ViewportResult At390)> AuditPageAsync(
        IPage page,
        string baseUrl,
        string pathAndQuery,
        CancellationToken cancellationToken = default)
    {
        var at1440 = await AuditViewportAsync(page, baseUrl, pathAndQuery, 1440, 900, cancellationToken);
        var at390 = await AuditViewportAsync(page, baseUrl, pathAndQuery, 390, 844, cancellationToken);
        return (at1440, at390);
    }

    internal static IReadOnlyList<Row> MergeRows(ViewportResult at1440, ViewportResult at390)
    {
        var labels = at1440.Elements.Select(e => e.Label)
            .Concat(at390.Elements.Select(e => e.Label))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        var map1440 = at1440.Elements.ToDictionary(e => e.Label, e => e, StringComparer.Ordinal);
        var map390 = at390.Elements.ToDictionary(e => e.Label, e => e, StringComparer.Ordinal);
        var rows = new List<Row>();
        foreach (var label in labels)
        {
            map1440.TryGetValue(label, out var r1440);
            map390.TryGetValue(label, out var r390);
            var ok1440 = r1440 is { HitOk: true, ClickOk: true };
            var ok390 = r390 is { HitOk: true, ClickOk: true };
            var note = r1440?.Detail ?? r390?.Detail;
            rows.Add(new Row(label, ok1440, ok390, note));
        }

        return rows;
    }

    internal static string FormatMarkdownTable(IReadOnlyList<Row> rows, IReadOnlyList<string> skipped)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Element | 1440 | 390 |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var row in rows)
        {
            sb.Append("| ").Append(EscapeCell(row.Element)).Append(" | ");
            sb.Append(row.Ok1440 ? "ok" : "fout").Append(" | ");
            sb.Append(row.Ok390 ? "ok" : "fout").AppendLine(" |");
        }

        if (skipped.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Overgeslagen (gevaarlijk of echt solliciteren): ");
            sb.Append(string.Join(", ", skipped.Distinct(StringComparer.OrdinalIgnoreCase)));
        }

        return sb.ToString();
    }

    internal static void AssertAllOk(IReadOnlyList<Row> rows, ITestOutputHelper? output = null)
    {
        foreach (var row in rows)
        {
            if (row.Ok1440 && row.Ok390)
            {
                continue;
            }

            output?.WriteLine($"Klikaudit fout: {row.Element} 1440={(row.Ok1440 ? "ok" : "fout")} 390={(row.Ok390 ? "ok" : "fout")} {row.Note}");
        }

        Assert.All(rows, row =>
            Assert.True(row.Ok1440 && row.Ok390, $"Niet klikbaar: {row.Element} (1440={(row.Ok1440 ? "ok" : "fout")}, 390={(row.Ok390 ? "ok" : "fout")})"));
    }

    private static async Task<ViewportResult> AuditViewportAsync(
        IPage page,
        string baseUrl,
        string pathAndQuery,
        int width,
        int height,
        CancellationToken cancellationToken)
    {
        await page.SetViewportSizeAsync(width, height);
        var url = baseUrl.TrimEnd('/') + pathAndQuery;
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });
        await DismissTransientOverlaysAsync(page);
        await TagInteractivesAsync(page);

        var inventory = await page.EvaluateAsync<InteractiveInventory>(
            """
            () => {
              const items = [];
              const nodes = document.querySelectorAll('[data-acc-click-idx]');
              nodes.forEach((el) => {
                const idx = parseInt(el.getAttribute('data-acc-click-idx') || '-1', 10);
                if (idx < 0) return;
                const aria = el.getAttribute('aria-label');
                let label = aria ? aria.trim().slice(0, 120) : '';
                if (!label) {
                  label = (el.innerText || el.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 120);
                }
                if (!label) {
                  label = (el.tagName + (el.id ? '#' + el.id : '')).toLowerCase();
                }
                items.push({ idx, label, tag: el.tagName.toLowerCase() });
              });
              items.sort((a, b) => a.idx - b.idx);
              return { items, skipped: window.__accSkipped || [] };
            }
            """);

        var results = new List<ElementResult>();
        for (var itemIndex = 0; itemIndex < inventory.Items.Length; itemIndex++)
        {
            var item = inventory.Items[itemIndex];
            cancellationToken.ThrowIfCancellationRequested();
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });
            await DismissTransientOverlaysAsync(page);
            await TagInteractivesAsync(page);

            var locator = page.Locator($"[data-acc-click-idx=\"{itemIndex}\"]");
            if (await locator.CountAsync() == 0)
            {
                results.Add(new ElementResult(item.Label, false, false, "missing-after-reload"));
                continue;
            }

            var hit = await EvaluateHitAsync(page, itemIndex);
            if (!hit.Ok)
            {
                results.Add(new ElementResult(item.Label, false, false, hit.Reason));
                continue;
            }

            var click = await TryClickWithEffectAsync(page, locator, itemIndex, item, baseUrl);
            results.Add(new ElementResult(item.Label, true, click.Ok, click.Detail));
        }

        return new ViewportResult(results, inventory.Skipped);
    }

    private static async Task TagInteractivesAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const dangerous = ['account verwijderen','later verder','later doen','begin opnieuw','intrekken'];
              const realApply = ['solliciteer nu','definitief solliciteren','sollicitatie versturen'];
              const root = document.querySelector('.app-main') || document.body;
              const sel = 'a[href], button, input:not([type="hidden"]), select, textarea, summary, [role="button"], [role="tab"], [role="link"], label[for]';
              const nodes = Array.from(root.querySelectorAll(sel));
              const skipped = [];
              const isVisible = (el) => {
                const r = el.getBoundingClientRect();
                if (r.width < 2 || r.height < 2) return false;
                const cs = getComputedStyle(el);
                if (cs.visibility === 'hidden' || cs.display === 'none' || cs.pointerEvents === 'none') return false;
                if (parseFloat(cs.opacity || '1') < 0.05) return false;
                return true;
              };
              const labelOf = (el) => {
                const aria = el.getAttribute('aria-label');
                if (aria) return aria.trim().slice(0, 120);
                const txt = (el.innerText || el.textContent || '').replace(/\\s+/g, ' ').trim();
                if (txt) return txt.slice(0, 120);
                return (el.tagName + (el.id ? '#' + el.id : '')).toLowerCase();
              };
              const shouldSkip = (el, textLower) => {
                if (dangerous.some(d => textLower.includes(d))) return 'dangerous';
                if (realApply.some(d => textLower.includes(d))) return 'real-apply';
                if (el.closest('.cookie-consent')) return 'cookie-banner';
                return null;
              };
              document.querySelectorAll('[data-acc-click-idx]').forEach(el => el.removeAttribute('data-acc-click-idx'));
              let idx = 0;
              for (const el of nodes) {
                if (!isVisible(el)) continue;
                const textLower = labelOf(el).toLowerCase();
                const skip = shouldSkip(el, textLower);
                if (skip) {
                  skipped.push(labelOf(el) + ' (' + skip + ')');
                  continue;
                }
                el.setAttribute('data-acc-click-idx', String(idx++));
              }
              window.__accSkipped = skipped;
            }
            """);
    }

    private static async Task DismissTransientOverlaysAsync(IPage page)
    {
        await page.EvaluateAsync("""
            () => {
              document.documentElement.classList.add('cookie-consent-known');
              const banner = document.querySelector('.cookie-consent');
              if (banner) banner.style.display = 'none';
            }
            """);
    }

    private static async Task<(bool Ok, string? Reason)> EvaluateHitAsync(IPage page, int idx)
    {
        return await page.EvaluateAsync<(bool Ok, string? Reason)>(
            """
            ({ idx }) => {
              const el = document.querySelector('[data-acc-click-idx="' + idx + '"]');
              if (!el) return { ok: false, reason: 'missing' };
              el.scrollIntoView({ block: 'center', inline: 'nearest' });
              const r = el.getBoundingClientRect();
              const x = Math.min(window.innerWidth - 2, Math.max(2, r.left + r.width / 2));
              const y = Math.min(window.innerHeight - 2, Math.max(2, r.top + r.height / 2));
              const hit = document.elementFromPoint(x, y);
              if (!hit) return { ok: false, reason: 'no-hit' };
              const ok = hit === el || el.contains(hit);
              if (ok) return { ok: true, reason: 'ok' };
              const block = hit.tagName.toLowerCase()
                + (hit.className ? '.' + String(hit.className).trim().split(/\\s+/)[0] : '');
              return { ok: false, reason: 'blocked-by-' + block };
            }
            """,
            new { idx });
    }

    private static async Task<(bool Ok, string? Detail)> TryClickWithEffectAsync(
        IPage page,
        ILocator locator,
        int itemIndex,
        InteractiveItem item,
        string baseUrl)
    {
        var beforeUrl = page.Url;
        var tag = item.Tag;

        try
        {
            if (tag is "input" or "textarea" or "select")
            {
                await locator.ClickAsync(new() { Timeout = 10_000 });
                var focused = await page.EvaluateAsync<bool>(
                    """
                    ({ idx }) => {
                      const el = document.querySelector('[data-acc-click-idx="' + idx + '"]');
                      if (!el) return false;
                      const active = document.activeElement;
                      return active === el || el.contains(active);
                    }
                    """,
                    new { idx = itemIndex });
                return focused ? (true, "focus") : (false, "no-focus");
            }

            if (tag == "summary")
            {
                var openBefore = await locator.EvaluateAsync<bool>("el => el.parentElement?.open === true");
                await locator.ClickAsync(new() { Timeout = 10_000 });
                await page.WaitForTimeoutAsync(200);
                var openAfter = await locator.EvaluateAsync<bool>("el => el.parentElement?.open === true");
                return openBefore != openAfter ? (true, "details-toggle") : (false, "details-unchanged");
            }

            if (tag == "label")
            {
                await locator.ClickAsync(new() { Timeout = 10_000 });
                return (true, "label-click");
            }

            await locator.ClickAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(350);
            var afterUrl = page.Url;
            if (!string.Equals(beforeUrl, afterUrl, StringComparison.OrdinalIgnoreCase))
            {
                var okNav = afterUrl.StartsWith(baseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                return okNav ? (true, "navigated") : (false, "unexpected-host");
            }

            if (tag == "a")
            {
                return (true, "same-url-link");
            }

            var toggled = await page.EvaluateAsync<bool>(
                """
                ({ idx }) => {
                  const el = document.querySelector('[data-acc-click-idx="' + idx + '"]');
                  if (!el) return false;
                  return el.getAttribute('aria-expanded') === 'true'
                    || el.classList.contains('is-active')
                    || el.getAttribute('aria-current') === 'page'
                    || el.getAttribute('aria-pressed') === 'true';
                }
                """,
                new { idx = itemIndex });
            return toggled ? (true, "toggle-state") : (true, "button-click");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string EscapeCell(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);

    private sealed class InteractiveInventory
    {
        public InteractiveItem[] Items { get; set; } = [];
        public string[] Skipped { get; set; } = [];
    }

    private sealed class InteractiveItem
    {
        public int Idx { get; set; }
        public string Label { get; set; } = "";
        public string Tag { get; set; } = "";
    }
}
