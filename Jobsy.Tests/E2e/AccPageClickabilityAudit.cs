using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Acc UAT: every visible control must be hit-test clean and respond to a real click (Dennis).</summary>
internal static class AccPageClickabilityAudit
{
    private static readonly string[] DangerousLabelFragments =
    [
        "account verwijderen",
        "later verder",
        "later doen",
        "begin opnieuw",
        "intrekken",
    ];

    /// <summary>Real sollicitatie submit — skip buttons whose primary label is exactly Solliciteer.</summary>
    private static readonly string[] DangerousExactLabels = ["Solliciteer", "Apply"];

    internal sealed record ElementRow(
        string Element,
        string Kind,
        string Result1440,
        string Result390,
        bool Skipped);

    internal sealed class ViewportResult
    {
        public required string Viewport { get; init; }
        public required List<ElementAttempt> Elements { get; init; }
        public List<string> SkippedDangerous { get; init; } = [];
    }

    internal sealed record ElementAttempt(string Label, string Kind, string Result, bool Skipped, string? SkipReason);

    internal static async Task<ViewportResult> AuditPageAsync(IPage page, string viewportTag)
    {
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.WaitForTimeoutAsync(300);

        var catalog = await page.EvaluateAsync<CatalogItem[]>(
            """
            () => {
              const out = [];
              let n = 0;
              const sel = [
                'a[href]',
                'button',
                'input:not([type=hidden])',
                'select',
                'textarea',
                '[role=tab]',
                '[role=button]',
                '[role=switch]',
                '[role=checkbox]',
                '[role=radio]',
                '.jobsy-action',
                '.lobsy-choice-tile',
                '.bk-filters .jobsy-action',
                '.kb-view-toggle button',
                '.kb-view-toggle a',
                '.bk-map-toggle a',
                '.test-likert__opt',
              ].join(',');
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const mapView = root && (root.classList.contains('show-map')
                || root.getAttribute('data-kb-view') === 'map');
              for (const el of document.querySelectorAll(sel)) {
                if (el.closest('.cookie-banner, .cookie-consent, [data-testid=cookie-banner]')) continue;
                if (mapView && el.closest('.vacancy-list, .vacancy-pane.panel, .kb-list-rows')) continue;
                if (mapView && el.closest('footer, .candidate-bottom-nav, .app-footer')) continue;
                if (mapView && !el.closest('.kb-mockup-top, .map-pane, .kb-panel-head, .kb-filter-drawer, .filter-sheet, .cookie-consent')) continue;
                const st = getComputedStyle(el);
                if (st.visibility === 'hidden' || st.display === 'none') continue;
                const r = el.getBoundingClientRect();
                if (r.width < 4 || r.height < 4 || r.bottom < 0 || r.top > window.innerHeight) continue;
                if (r.right < 0 || r.left > window.innerWidth) continue;
                const id = 'acc-audit-' + (n++);
                el.setAttribute('data-acc-audit-id', id);
                const label = (el.getAttribute('aria-label')
                  || el.getAttribute('title')
                  || (el.innerText || '').trim().replace(/\\s+/g, ' ').slice(0, 80)
                  || el.getAttribute('href')
                  || el.getAttribute('name')
                  || el.tagName).trim();
                out.push({
                  id,
                  label,
                  kind: el.tagName.toLowerCase() + (el.getAttribute('role') ? ':' + el.getAttribute('role') : ''),
                  href: el.getAttribute('href') || ''
                });
              }
              return out;
            }
            """);

        var attempts = new List<ElementAttempt>();
        var skippedDangerous = new List<string>();
        var baselineUrl = page.Url;

        foreach (var item in catalog.OrderBy(c => c.Label, StringComparer.Ordinal))
        {
            var skip = ShouldSkip(item.Label, item.Href);
            if (skip is not null)
            {
                attempts.Add(new ElementAttempt(item.Label, item.Kind, "skipped", true, skip));
                skippedDangerous.Add($"{item.Label} ({skip})");
                continue;
            }

            var locator = page.Locator($"[data-acc-audit-id=\"{item.Id}\"]").First;
            var result = await TryHitAndClickAsync(page, locator, item);
            attempts.Add(new ElementAttempt(item.Label, item.Kind, result, false, null));

            if (page.Url != baselineUrl && !page.Url.StartsWith(baselineUrl.Split('?')[0], StringComparison.Ordinal))
            {
                await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await page.WaitForTimeoutAsync(400);
                baselineUrl = page.Url;
            }
        }

        return new ViewportResult
        {
            Viewport = viewportTag,
            Elements = attempts,
            SkippedDangerous = skippedDangerous
        };
    }

    internal static List<ElementRow> MergeViewports(ViewportResult wide, ViewportResult narrow)
    {
        var keys = wide.Elements.Select(e => Key(e.Label, e.Kind))
            .Union(narrow.Elements.Select(e => Key(e.Label, e.Kind)))
            .Order(StringComparer.Ordinal);
        var w = wide.Elements
            .GroupBy(e => Key(e.Label, e.Kind), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var n = narrow.Elements
            .GroupBy(e => Key(e.Label, e.Kind), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var rows = new List<ElementRow>();
        foreach (var k in keys)
        {
            w.TryGetValue(k, out var we);
            n.TryGetValue(k, out var ne);
            rows.Add(new ElementRow(
                we?.Label ?? ne!.Label,
                we?.Kind ?? ne!.Kind,
                FormatResult(we),
                FormatResult(ne),
                (we?.Skipped ?? false) || (ne?.Skipped ?? false)));
        }

        return rows;
    }

    internal static void WriteReport(
        string artifactDir,
        string fileBase,
        string pageTitle,
        IReadOnlyList<ElementRow> rows,
        IReadOnlyList<string> skippedDangerous,
        string? accHead = null)
    {
        Directory.CreateDirectory(artifactDir);
        var mdPath = Path.Combine(artifactDir, fileBase + ".md");
        var jsonPath = Path.Combine(artifactDir, fileBase + ".json");

        var sb = new StringBuilder();
        sb.AppendLine($"# Clickability audit — {pageTitle}");
        if (!string.IsNullOrWhiteSpace(accHead))
        {
            sb.AppendLine($"Acc head: `{accHead}`");
        }

        sb.AppendLine();
        sb.AppendLine("| Element | 1440 | 390 |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var row in rows)
        {
            sb.AppendLine($"| {EscapeMd(row.Element)} | {EscapeMd(row.Result1440)} | {EscapeMd(row.Result390)} |");
        }

        if (skippedDangerous.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Overgeslagen (gevaarlijk):");
            foreach (var s in skippedDangerous.Distinct())
            {
                sb.AppendLine($"- {s}");
            }
        }

        File.WriteAllText(mdPath, sb.ToString());
        var payload = new
        {
            page = pageTitle,
            acc_head = accHead,
            rows,
            skipped_dangerous = skippedDangerous.Distinct().ToList()
        };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal static void AssertAllOk(IReadOnlyList<ElementRow> rows, string context)
    {
        var bad = rows.Where(r => !r.Skipped && (IsFail(r.Result1440) || IsFail(r.Result390))).ToList();
        if (bad.Count > 0)
        {
            var sample = string.Join("; ", bad.Take(8).Select(b => $"{b.Element} (1440={b.Result1440}, 390={b.Result390})"));
            Assert.Fail($"{context}: {bad.Count} element(s) not fully clickable. {sample}");
        }
    }

    private static bool IsFail(string r)
        => r is not ("ok" or "skipped" or "n/a");

    private static string FormatResult(ElementAttempt? a)
    {
        if (a is null)
        {
            return "n/a";
        }

        if (a.Skipped)
        {
            return "skipped";
        }

        return a.Result;
    }

    private static string Key(string label, string kind) => $"{kind}|{label}";

    private static string? ShouldSkip(string label, string href)
    {
        var lower = label.ToLowerInvariant();
        foreach (var frag in DangerousLabelFragments)
        {
            if (lower.Contains(frag, StringComparison.Ordinal))
            {
                return frag;
            }
        }

        if (DangerousExactLabels.Any(ex => string.Equals(label.Trim(), ex, StringComparison.OrdinalIgnoreCase)))
        {
            return "sollicitatie-submit";
        }

        if (href.Contains("/apply", StringComparison.OrdinalIgnoreCase)
            || href.Contains("sollicit", StringComparison.OrdinalIgnoreCase))
        {
            return "sollicitatie-link";
        }

        return null;
    }

    private static async Task<string> TryHitAndClickAsync(IPage page, ILocator locator, CatalogItem item)
    {
        if (await locator.CountAsync() == 0)
        {
            return "missing";
        }

        try
        {
            await locator.ScrollIntoViewIfNeededAsync();
            await page.WaitForTimeoutAsync(100);
            var hit = await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, locator, item.Label);
            if (hit is not "ok")
            {
                return "hit:" + hit;
            }

            var tag = item.Kind.Split(':')[0];
            var beforeUrl = page.Url;
            var beforeExpanded = await locator.GetAttributeAsync("aria-expanded");
            var beforePressed = await locator.GetAttributeAsync("aria-pressed");
            var beforeChecked = await locator.GetAttributeAsync("aria-checked");

            if (tag is "input" or "textarea" or "select")
            {
                await locator.ClickAsync(new() { Timeout = 15_000 });
                await page.WaitForTimeoutAsync(150);
                var focused = await page.EvaluateAsync<bool>(
                    """
                    (id) => {
                      const el = document.querySelector('[data-acc-audit-id="' + id + '"]');
                      return el && document.activeElement === el;
                    }
                    """,
                    item.Id);
                return focused ? "ok" : "focus-fail";
            }

            await locator.ClickAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(350);

            if (page.Url != beforeUrl)
            {
                return "ok";
            }

            if (beforeExpanded is not null)
            {
                var after = await locator.GetAttributeAsync("aria-expanded");
                if (after != beforeExpanded)
                {
                    return "ok";
                }
            }

            if (beforePressed is not null)
            {
                var after = await locator.GetAttributeAsync("aria-pressed");
                if (after != beforePressed)
                {
                    return "ok";
                }
            }

            if (beforeChecked is not null)
            {
                var after = await locator.GetAttributeAsync("aria-checked");
                if (after != beforeChecked)
                {
                    return "ok";
                }
            }

            var hasActive = await locator.EvaluateAsync<bool>(
                "el => el.classList.contains('is-active') || el.classList.contains('active') || el.getAttribute('aria-selected') === 'true'");
            if (hasActive)
            {
                return "ok";
            }

            // Likert / chip: class change
            var classChanged = await page.EvaluateAsync<bool>(
                """
                (id) => {
                  const el = document.querySelector('[data-acc-audit-id="' + id + '"]');
                  if (!el) return false;
                  return el.matches(':checked, .is-selected, .is-active, [aria-checked="true"]');
                }
                """,
                item.Id);
            return classChanged ? "ok" : "no-effect";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name + ":" + (ex.Message.Length > 80 ? ex.Message[..80] : ex.Message);
        }
    }

    private static string EscapeMd(string s) => s.Replace("|", "\\|", StringComparison.Ordinal);

    private sealed class CatalogItem
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Href { get; set; } = "";
    }
}
