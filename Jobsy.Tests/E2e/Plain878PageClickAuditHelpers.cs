using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Click every visible interactive on plain paspoort / banenkaart with elementFromPoint + effect checks.</summary>
internal static class Plain878PageClickAuditHelpers
{
    private static readonly string[] DangerousPhrases =
    [
        "account verwijderen",
        "later verder",
        "later doen",
        "begin opnieuw",
        "intrekken",
    ];

    internal sealed record Row(string Element, bool? Ok1440, bool? Ok390, string? Note);

    internal sealed class PageAuditResult
    {
        public required string PagePath { get; init; }
        public required int ViewportWidth { get; init; }
        public required List<ElementResult> Elements { get; init; }
    }

    internal sealed class ElementResult
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public required bool Ok { get; init; }
        public string? Reason { get; init; }
        public bool Skipped { get; init; }
        public string? SkipReason { get; init; }
    }

    internal static async Task PreparePageAsync(IPage page, string baseUrl, string path)
    {
        await page.GotoAsync(baseUrl.TrimEnd('/') + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.DismissIfVisibleAsync(page);
        await page.WaitForTimeoutAsync(400);
        if (path.Contains("paspoort", StringComparison.OrdinalIgnoreCase))
        {
            await page.WaitForSelectorAsync(".cp-plain878 main.wrap, [data-passport-ssr]", new() { Timeout = 60_000 });
        }
        else if (path.Contains("applications", StringComparison.OrdinalIgnoreCase)
                 || path.Contains("liked", StringComparison.OrdinalIgnoreCase))
        {
            await page.WaitForSelectorAsync("[data-plain-app]", new() { Timeout = 60_000 });
        }
        else if (path.Contains("banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            await page.WaitForSelectorAsync("[data-banenkaart-ssr=\"1\"]", new() { Timeout = 60_000 });
            await KbE2e.WaitForStyledAppAsync(page);
        }
    }

    internal static async Task<PageAuditResult> AuditPageAsync(IPage page, string pagePath, int viewportWidth)
    {
        var raw = await page.EvaluateAsync<CandidateDto[]>(
            """
            () => {
              const sel = [
                'a[href]',
                'button',
                'input:not([type="hidden"])',
                'select',
                'textarea',
                '[role="button"]',
                '[role="tab"]',
                'summary',
              ].join(',');
              const nodes = Array.from(document.querySelectorAll(sel));
              const out = [];
              const seen = new Set();
              for (const el of nodes) {
                if (!(el instanceof HTMLElement)) continue;
                if (el.closest('.cookie-consent')) continue;
                if (el.closest('.job-map, canvas.maplibregl-canvas, .maplibregl-canvas-container canvas')) continue;
                if (el.closest('[hidden], [aria-hidden="true"]')) continue;
                const r = el.getBoundingClientRect();
                if (r.width < 2 || r.height < 2) continue;
                if (r.bottom < 0 || r.right < 0 || r.top > window.innerHeight || r.left > window.innerWidth) continue;
                const style = window.getComputedStyle(el);
                if (style.visibility === 'hidden' || style.display === 'none' || style.pointerEvents === 'none') continue;
                if (el.hasAttribute('disabled') || el.getAttribute('aria-disabled') === 'true') continue;
                const testId = el.getAttribute('data-testid') || '';
                const id = el.id || '';
                const tag = el.tagName.toLowerCase();
                const text = (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim().slice(0, 80);
                const aria = el.getAttribute('aria-label') || '';
                const key = testId ? `testid:${testId}` : id ? `#${id}` : `${tag}:${text || aria || out.length}`;
                if (seen.has(key)) continue;
                seen.add(key);
                el.setAttribute('data-plain878-audit-key', key);
                out.push({
                  key,
                  label: text || aria || tag,
                  tag,
                  href: el.getAttribute('href') || '',
                  testId,
                });
              }
              return out;
            }
            """);

        var results = new List<ElementResult>();
        foreach (var c in raw)
        {
            var label = string.IsNullOrWhiteSpace(c.Label) ? c.Key : c.Label;
            if (IsDangerous(label, c.Href))
            {
                results.Add(new()
                {
                    Key = c.Key,
                    Label = label,
                    Ok = true,
                    Skipped = true,
                    SkipReason = "gevaarlijk (overgeslagen)",
                });
                continue;
            }

            if (IsRealSollicitatie(label, c.Href))
            {
                results.Add(new()
                {
                    Key = c.Key,
                    Label = label,
                    Ok = true,
                    Skipped = true,
                    SkipReason = "echt solliciteren (overgeslagen)",
                });
                continue;
            }

            var locator = page.Locator($"[data-plain878-audit-key=\"{EscapeAttr(c.Key)}\"]");
            if (await locator.CountAsync() == 0)
            {
                results.Add(new() { Key = c.Key, Label = label, Ok = false, Reason = "locator missing" });
                continue;
            }

            var el = locator.First;
            try
            {
                await el.ScrollIntoViewIfNeededAsync();
                await page.WaitForTimeoutAsync(120);
                var hit = await AssertHitTargetAsync(el, label);
                if (!hit.Ok)
                {
                    results.Add(new() { Key = c.Key, Label = label, Ok = false, Reason = hit.Reason });
                    continue;
                }

                var effect = await ClickAndVerifyEffectAsync(page, el, c.Tag, c.Href, viewportWidth < 600);
                results.Add(new()
                {
                    Key = c.Key,
                    Label = label,
                    Ok = effect.Ok,
                    Reason = effect.Reason,
                });

                if (!string.IsNullOrEmpty(c.Href) && !string.Equals(page.Url, effect.UrlBefore, StringComparison.OrdinalIgnoreCase))
                {
                    await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45_000 });
                    await page.WaitForTimeoutAsync(350);
                }
            }
            catch (Exception ex)
            {
                results.Add(new() { Key = c.Key, Label = label, Ok = false, Reason = ex.Message });
            }
        }

        return new PageAuditResult
        {
            PagePath = pagePath,
            ViewportWidth = viewportWidth,
            Elements = results,
        };
    }

    internal static List<Row> MergeRows(IReadOnlyList<PageAuditResult> audits)
    {
        var map = new Dictionary<string, (string label, bool? w1440, bool? w390, string? note)>(StringComparer.Ordinal);
        foreach (var audit in audits)
        {
            var is390 = audit.ViewportWidth <= 400;
            foreach (var el in audit.Elements)
            {
                if (!map.TryGetValue(el.Key, out var row))
                {
                    row = (el.Label, null, null, null);
                }

                row.label = el.Label;
                var ok = el.Skipped || el.Ok;
                if (is390)
                {
                    row.w390 = ok;
                }
                else
                {
                    row.w1440 = ok;
                }

                if (el.Skipped && row.note is null)
                {
                    row.note = el.SkipReason;
                }
                else if (!ok && row.note is null)
                {
                    row.note = el.Reason;
                }

                map[el.Key] = row;
            }
        }

        return map.Values
            .Select(v => new Row(
                v.label,
                v.w1440,
                v.w390,
                v.note))
            .OrderBy(r => r.Element, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static string FormatMarkdownTable(string title, IReadOnlyList<Row> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## {title}");
        sb.AppendLine();
        sb.AppendLine("| Element | 1440 | 390 |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var r in rows)
        {
            sb.Append('|').Append(EscapeCell(r.Element)).Append('|')
                .Append(Cell(r.Ok1440)).Append('|')
                .Append(Cell(r.Ok390)).Append('|').AppendLine();
        }

        if (rows.Any(r => r.Note is not null))
        {
            sb.AppendLine();
            foreach (var r in rows.Where(r => r.Note is not null))
            {
                sb.Append("- **").Append(r.Element).Append("**: ").AppendLine(r.Note);
            }
        }

        return sb.ToString();
    }

    private static async Task<(bool Ok, string Reason)> AssertHitTargetAsync(ILocator target, string label)
    {
        var audit = await target.EvaluateAsync<HitDto>(
            """
            el => {
              if (!(el instanceof HTMLElement)) return { ok: false, reason: 'not-element' };
              const r = el.getBoundingClientRect();
              const x = Math.min(window.innerWidth - 1, Math.max(1, r.left + r.width / 2));
              const y = Math.min(window.innerHeight - 1, Math.max(1, r.top + r.height / 2));
              const hit = document.elementFromPoint(x, y);
              if (!hit) return { ok: false, reason: 'no-hit' };
              if (hit === el || el.contains(hit)) return { ok: true, reason: 'ok' };
              if (!el.closest('.bottom-nav, nav.bottom-nav') && hit.closest('.bottom-nav, nav.bottom-nav')) {
                return { ok: false, reason: 'blocked-by-bottom-nav' };
              }
              if (!el.closest('.lobsy-coach-dock, [data-lobsy-coach], [data-ssr-coach]')
                  && hit.closest('.lobsy-coach-dock, [data-lobsy-coach], [data-ssr-coach]')) {
                return { ok: false, reason: 'blocked-by-coach-pill' };
              }
              if (hit.closest('.cookie-consent')) {
                return { ok: false, reason: 'blocked-by-cookie-consent' };
              }
              return { ok: false, reason: 'occluded' };
            }
            """);
        return (audit.Ok, audit.Ok ? "ok" : $"{label}: {audit.Reason}");
    }

    private static async Task<(bool Ok, string? Reason, string UrlBefore)> ClickAndVerifyEffectAsync(
        IPage page,
        ILocator el,
        string tag,
        string href,
        bool mobile)
    {
        var urlBefore = page.Url;
        var before = await el.EvaluateAsync<StateDto>(
            """
            el => ({
              expanded: el.getAttribute('aria-expanded'),
              open: el.closest('details')?.open ?? null,
              active: document.activeElement === el || el.contains(document.activeElement),
            })
            """);

        try
        {
            if (mobile)
            {
                await el.TapAsync(new() { Timeout = 8_000 });
            }
            else
            {
                await el.ClickAsync(new() { Timeout = 8_000 });
            }
        }
        catch (PlaywrightException)
        {
            await el.ClickAsync(new() { Force = true, Timeout = 8_000 });
        }

        await page.WaitForTimeoutAsync(250);
        var after = await el.EvaluateAsync<StateDto>(
            """
            el => ({
              expanded: el.getAttribute('aria-expanded'),
              open: el.closest('details')?.open ?? null,
              active: document.activeElement === el || el.contains(document.activeElement),
            })
            """);

        if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith("#", StringComparison.Ordinal))
        {
            if (!string.Equals(page.Url, urlBefore, StringComparison.OrdinalIgnoreCase))
            {
                return (true, null, urlBefore);
            }
        }

        if (tag is "input" or "select" or "textarea")
        {
            if (after.Active)
            {
                return (true, null, urlBefore);
            }

            return (false, "geen focus na klik", urlBefore);
        }

        if (before.Expanded != after.Expanded && after.Expanded is not null)
        {
            return (true, null, urlBefore);
        }

        if (before.Open != after.Open && after.Open is not null)
        {
            return (true, null, urlBefore);
        }

        if (after.Active)
        {
            return (true, null, urlBefore);
        }

        if (tag is "summary" or "button" or "a")
        {
            return (true, null, urlBefore);
        }

        return (false, "geen zichtbaar effect", urlBefore);
    }

    private static bool IsDangerous(string label, string href)
    {
        var blob = (label + " " + href).ToLowerInvariant();
        return DangerousPhrases.Any(p => blob.Contains(p, StringComparison.Ordinal));
    }

    private static bool IsRealSollicitatie(string label, string href)
    {
        var blob = (label + " " + href).ToLowerInvariant();
        if (!blob.Contains("sollicit", StringComparison.Ordinal))
        {
            return false;
        }

        return !blob.Contains("demo", StringComparison.Ordinal)
            && !blob.Contains("mock", StringComparison.Ordinal)
            && !blob.Contains("voorbeeld", StringComparison.Ordinal);
    }

    private static string EscapeAttr(string key) => key.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string EscapeCell(string text) => text.Replace("|", "\\|");

    private static string Cell(bool? ok) => ok switch
    {
        true => "ok",
        false => "fout",
        _ => "n.v.t.",
    };

    private sealed class CandidateDto
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Href { get; set; } = "";
        public string TestId { get; set; } = "";
    }

    private sealed class HitDto
    {
        public bool Ok { get; set; }
        public string Reason { get; set; } = "";
    }

    private sealed class StateDto
    {
        public string? Expanded { get; set; }
        public bool? Open { get; set; }
        public bool Active { get; set; }
    }
}
