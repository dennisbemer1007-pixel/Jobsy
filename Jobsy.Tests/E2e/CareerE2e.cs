using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Shared Playwright helpers for the carrière stack (file 06 §1): desktop 1440 and
/// mobile 390, five languages incl. <c>ar</c> RTL, reduced motion and Werkgevers OFF.
/// Everything soft-skips without <c>JOBSY_E2E_BASE_URL</c>, same as
/// <see cref="Jobsy.Tests.Acc2709PlaywrightTests"/>.
/// </summary>
public static class CareerE2e
{
    public const string CareerPath = "/carriere";
    public const string ContactsPath = "/candidate/talent-contacts";
    public const string HowPath = "/candidate/hoe-werkt-lobsy";

    /// <summary>Dedicated seed candidate (DemoUsersSeeder) the stack may reset; never the public demo profile.</summary>
    public const string DefaultFreshEmail = "onboarding.e2e@jobsy.local";

    public static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];

    /// <summary>Returns null when E2E must soft-skip (no base URL, or unreachable).</summary>
    public static Task<string?> TryReadyBaseUrlAsync() => KbE2e.TryReadyBaseUrlAsync();

    public static Task<IBrowser> LaunchAsync() => KbE2e.LaunchChromiumAsync();

    public static BrowserNewContextOptions DesktopContext(bool reducedMotion = false)
    {
        var options = KbE2e.DesktopContext();
        if (reducedMotion)
        {
            options.ReducedMotion = ReducedMotion.Reduce;
        }

        return options;
    }

    public static BrowserNewContextOptions MobileContext(bool reducedMotion = false)
    {
        var options = KbE2e.MobileContext();
        if (reducedMotion)
        {
            options.ReducedMotion = ReducedMotion.Reduce;
        }

        return options;
    }

    /// <summary>
    /// Logs in and leaves the account on Dutch. The language selector writes the profile
    /// language, so the flow that walks all five languages would otherwise leave the shared
    /// seed candidate in Polish for whatever runs next.
    /// </summary>
    public static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string? email = null, string? password = null)
    {
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password))
        {
            return false;
        }

        // The circuit applies the profile language after the prerender, so a short wait for a
        // non-Dutch document is what tells us the account needs switching back.
        if (await WaitForTrueAsync(
                page,
                "() => document.documentElement.lang !== 'nl'",
                timeoutMs: 5_000))
        {
            await TrySwitchLanguageAsync(page, baseUrl, "nl", CareerPath);
        }

        return true;
    }

    /// <summary>The candidate without an active plan (empty-state flow); env-overridable.</summary>
    public static Task<bool> TryLoginFreshAsync(IPage page, string baseUrl)
        => TryLoginAsync(
            page,
            baseUrl,
            Environment.GetEnvironmentVariable("JOBSY_E2E_FRESH_EMAIL") ?? DefaultFreshEmail,
            Environment.GetEnvironmentVariable("JOBSY_E2E_FRESH_PASSWORD")
                ?? Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD")
                ?? KbE2e.DefaultPassword);

    /// <summary>The employer that can create a Pending contact request; null ⇒ soft-skip that flow.</summary>
    public static (string Email, string Password)? EmployerCredentials()
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL");
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD")
            ?? KbE2e.DefaultPassword;
        return (email, password);
    }

    public static async Task GoAsync(IPage page, string baseUrl, string path)
    {
        await page.GotoAsync(
            baseUrl + path,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("h1", new() { Timeout = 60_000 });
    }

    public static string ScreenshotDir()
    {
        var dir = Path.Combine(KbE2e.FindRepoRoot(), "artifacts", "e2e", "carriere");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task ShotAsync(IPage page, string name)
    {
        var path = Path.Combine(ScreenshotDir(), name + ".png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    /// <summary>
    /// Switches the UI language through the header selector, which also persists the
    /// profile language; the <c>/taal/{lang}</c> cookie endpoint is the fallback.
    /// </summary>
    public static async Task<bool> TrySwitchLanguageAsync(IPage page, string baseUrl, string language, string returnPath)
    {
        var toggle = page.Locator(".language-selector__toggle").First;
        if (await toggle.CountAsync() > 0)
        {
            try
            {
                await toggle.ClickAsync(new() { Timeout = 15_000 });
                var option = page.Locator(".language-selector__option")
                    .Filter(new() { HasTextString = language.ToUpperInvariant() })
                    .First;
                await option.ClickAsync(new() { Timeout = 15_000 });
                return await WaitForTrueAsync(
                    page,
                    $"() => document.documentElement.lang === '{language}'");
            }
            catch (PlaywrightException)
            {
                // fall through to the cookie endpoint
            }
            catch (TimeoutException)
            {
            }
        }

        await page.GotoAsync(
            $"{baseUrl}/taal/{language}?returnUrl={Uri.EscapeDataString(returnPath)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.WaitForSelectorAsync("h1", new() { Timeout = 60_000 });
        return string.Equals(
            await page.EvaluateAsync<string?>("() => document.documentElement.lang"),
            language,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Polls from the test process instead of <c>WaitForFunctionAsync</c>: the page CSP has no
    /// <c>unsafe-eval</c>, so Playwright's in-page polling loop is refused.
    /// </summary>
    public static async Task<bool> WaitForTrueAsync(IPage page, string expression, int timeoutMs = 20_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            if (await page.EvaluateAsync<bool>(expression))
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await page.WaitForTimeoutAsync(250);
        }
    }

    /// <summary>
    /// The culture script sets <c>html dir</c> after the circuit starts, so RTL checks have to
    /// wait for it rather than read the prerendered document.
    /// </summary>
    public static Task<bool> WaitForRtlAsync(IPage page)
        => WaitForTrueAsync(page, "() => document.documentElement.dir === 'rtl'");

    /// <summary>
    /// Transforms of the scene drawings that are actually painted. Both the desktop and the
    /// mobile scene are in the DOM; only the one the viewport shows is relevant for mirroring.
    /// </summary>
    public static Task<string[]> VisibleSceneTransformsAsync(IPage page)
        => page.Locator(".career-scene__svg").EvaluateAllAsync<string[]>("""
            els => els
              .filter(el => el.getClientRects().length > 0)
              .map(el => getComputedStyle(el).transform)
            """);

    /// <summary>
    /// Missing keys surface as the raw key (see <c>UiStrings.Get</c>), so any
    /// <c>Prefix.Something</c> token in visible text means a gap in one language.
    /// </summary>
    public static async Task AssertNoMissingKeyMarkersAsync(IPage page, string where)
    {
        var text = await page.EvaluateAsync<string>("() => document.body?.innerText || ''");
        foreach (var prefix in new[] { "Career.", "CareerStep.", "CareerDream.", "CareerErr.", "TalentC.", "HowC.", "Common." })
        {
            Assert.False(
                text.Contains(prefix, StringComparison.Ordinal),
                $"{where}: missing-key marker '{prefix}…' rendered as text.");
        }
    }

    public static async Task AssertNoHorizontalOverflowAsync(IPage page, string where)
    {
        var overflow = await page.EvaluateAsync<double>("""
            () => {
              const el = document.scrollingElement || document.documentElement;
              return el.scrollWidth - el.clientWidth;
            }
            """);
        Assert.True(overflow <= 1, $"{where}: horizontal overflow of {overflow}px at 390.");
    }

    /// <summary>Main content only — the scene labels and chrome are excluded on purpose.</summary>
    public static Task<string> CardTextAsync(IPage page)
        => page.EvaluateAsync<string>("""
            () => {
              const card = document.querySelector('.career-stage__card') || document.querySelector('main') || document.body;
              return card ? card.innerText : '';
            }
            """);

    /// <summary>Count of running CSS animations on the lobster and its shell plates (§9).</summary>
    public static Task<int> SceneAnimationCountAsync(IPage page)
        => page.EvaluateAsync<int>("""
            () => {
              if (typeof document.getAnimations !== 'function') return 0;
              const selectors = [
                '.journey-lobster', '.journey-plate', '.journey-plate--falling',
                '.journey-lob', '.journey-lob__fall', '.career-scene__lob'
              ];
              const targets = new Set();
              for (const selector of selectors) {
                document.querySelectorAll(selector).forEach(el => targets.add(el));
              }
              return document.getAnimations().filter(a => a.effect && targets.has(a.effect.target)).length;
            }
            """);

    /// <summary>Werkgevers OFF only on a local host with an explicit opt-in; never on shared Acc.</summary>
    public static bool AllowsFeatureToggle(string baseUrl) => KbE2e.AllowsFeatureToggle(baseUrl);
}
