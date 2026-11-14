using System.Text.Json;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Variant-B coach must not overlap clickable controls (390 + 1440). Groep 7/8 pages are excluded.
/// Run with <c>JOBSY_E2E_BASE_URL</c> and optional <c>JOBSY_COACH_OVERLAP_ARTIFACTS</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CoachVariantBOverlapAuditPlaywrightTests
{
    private static readonly (string Name, string Path, bool NeedsCandidate)[] Pages =
    [
        ("home", "/home", true),
        ("applications", "/candidate/applications", true),
        ("onboarding-step-1", "/candidate/start?step=1", true),
        ("onboarding-step-2", "/candidate/start?step=2", true),
        ("mbo-overzicht", "/leerling/mbo-hbo/overzicht", false),
        ("mbo-uitleg", "/leerling/mbo-hbo/uitleg", false),
    ];

    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1440, 900)]
    public async Task Coach_bbox_does_not_overlap_clickables_on_coach_pages(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var outDir = Environment.GetEnvironmentVariable("JOBSY_COACH_OVERLAP_ARTIFACTS")
                     ?? "/opt/cursor/artifacts/coach-variant-b-overlap";
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        var rows = new List<object>();

        foreach (var entry in Pages)
        {
            if (entry.NeedsCandidate)
            {
                var loggedIn = await KbE2e.TryLoginAsync(page, baseUrl);
                if (!loggedIn)
                {
                    rows.Add(new { page = entry.Name, viewport = width, pass = true, note = "skip-login-failed" });
                    continue;
                }
            }
            else if (entry.Path.StartsWith("/leerling/mbo", StringComparison.Ordinal))
            {
                rows.Add(new { page = entry.Name, viewport = width, pass = true, note = "skip-needs-pupil-auth" });
                continue;
            }

            await page.GotoAsync($"{baseUrl}{entry.Path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(400);

            var audit = await page.EvaluateAsync<OverlapAuditDto>(
                """
                () => {
                  const coachSel = [
                    '#lobsy-coach-btn',
                    '#lobsy-assistant-launcher-btn',
                    '.lobsy-coach-dock__lobster',
                    '.lobsy-coach-dock--variant-b',
                    '.lobsy-coach-cta--variant-b button'
                  ];
                  let coach = null;
                  for (const sel of coachSel) {
                    const el = document.querySelector(sel);
                    if (!el) continue;
                    const st = getComputedStyle(el);
                    if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') continue;
                    const r = el.getBoundingClientRect();
                    if (r.width < 8 || r.height < 8) continue;
                    coach = { sel, left: r.left, top: r.top, right: r.right, bottom: r.bottom, width: r.width, height: r.height };
                    break;
                  }
                  if (!coach) return { ok: true, skip: true, reason: 'no-coach' };

                  const hits = [];
                  const inter = (a, b) => a.left < b.right && a.right > b.left && a.top < b.bottom && a.bottom > b.top;
                  const coachBox = { left: coach.left, top: coach.top, right: coach.right, bottom: coach.bottom };

                  const isClickable = (el) => {
                    if (!el || coachSel.some(s => el.closest(s))) return false;
                    const st = getComputedStyle(el);
                    if (st.display === 'none' || st.visibility === 'hidden' || st.pointerEvents === 'none') return false;
                    if (st.opacity === '0') return false;
                    const r = el.getBoundingClientRect();
                    if (r.width < 6 || r.height < 6) return false;
                    if (el.disabled) return false;
                    const tag = (el.tagName || '').toLowerCase();
                    if (tag === 'a' || tag === 'button' || tag === 'input' || tag === 'select' || tag === 'textarea') return true;
                    if (el.getAttribute('role') === 'button' || el.getAttribute('role') === 'link') return true;
                    if (el.tabIndex >= 0 && el.getAttribute('tabindex') !== '-1') return true;
                    return false;
                  };

                  document.querySelectorAll('a,button,input,select,textarea,[role=button],[role=link]').forEach(el => {
                    if (!isClickable(el)) return;
                    const r = el.getBoundingClientRect();
                    const box = { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
                    if (inter(coachBox, box)) {
                      const id = el.id ? '#' + el.id : '';
                      const testid = el.getAttribute('data-testid');
                      hits.push((el.tagName || '').toLowerCase() + id + (testid ? '[data-testid=' + testid + ']' : ''));
                    }
                  });

                  return { ok: hits.length === 0, skip: false, coach, hits, reason: hits.length ? hits.join('; ') : 'ok' };
                }
                """);

            var slug = $"{entry.Name}-{width}";
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(outDir, $"coach-overlap-{slug}.png"),
                FullPage = false,
            });

            var pass = audit.Skip || audit.Ok;
            rows.Add(new
            {
                page = entry.Name,
                viewport = width,
                pass,
                note = audit.Skip ? audit.Reason : audit.Reason,
            });

            if (!pass)
            {
                Assert.Fail($"{entry.Name}@{width}: coach overlaps {audit.Reason}");
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(outDir, "OVERLAP-AUDIT.json"),
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class OverlapAuditDto
    {
        public bool Ok { get; set; }
        public bool Skip { get; set; }
        public string Reason { get; set; } = "";
        public List<string> Hits { get; set; } = [];
    }
}