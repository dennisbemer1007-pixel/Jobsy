using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Diagnostic: step-1 SSR must expose birth-date markers for e2e.</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingStep1BirthDomProbePlaywrightTests
{
    [Theory]
    [InlineData(390, 844, true)]
    [InlineData(1440, 900, false)]
    public async Task Step1_has_visible_birth_date_marker(int width, int height, bool mobile)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "birth-probe");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);

        var probe = await page.EvaluateAsync<string>(
            """
            () => {
              const nodes = [...document.querySelectorAll('[data-testid=onboarding-birth-date]')];
              const parts = nodes.map((el, i) => {
                const r = el.getBoundingClientRect();
                const st = getComputedStyle(el);
                return `#${i} tag=${el.tagName} vis=${st.visibility} disp=${st.display} w=${r.width} h=${r.height}`;
              });
              const input = document.querySelector('#onboarding-birth-date-input,[data-ob-client=birthDate]');
              return `count=${nodes.length}; ` + parts.join('; ') + `; input=${input ? 'yes' : 'no'}`;
            }
            """);

        if (!probe.Contains("count=1", StringComparison.Ordinal))
        {
            var snippet = await page.EvaluateAsync<string>(
                """
                () => {
                  const input = document.querySelector('[data-ob-client=birthDate]');
                  if (!input) return 'no-input';
                  const wrap = input.closest('.ob-client-step,.ob-fields,.ob-wizard__card,fieldset');
                  return wrap ? wrap.outerHTML.slice(0, 1200) : input.outerHTML;
                }
                """);
            Assert.Fail($"birth probe: {probe}; snippet: {snippet}");
        }

        Assert.Contains("input=yes", probe, StringComparison.Ordinal);
    }
}
