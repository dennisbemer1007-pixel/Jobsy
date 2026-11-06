using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Dennis S16 scoped acc gate. Run:
/// JOBSY_S16_ACC_VERIFY=1 JOBSY_WALKTHROUGH_ARTIFACTS=/opt/cursor/artifacts/s16-acc-0855
/// JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl dotnet test --filter S16_acc_verification
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class S16AccVerificationPlaywrightTests
{
    private const string TeacherEmail = "leraar@jobsy.local";

    [Fact(Timeout = 600_000, DisplayName = "S16_acc_verification")]
    public async Task S16_acc_verification_on_acceptatie()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_S16_ACC_VERIFY"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(artifactDir))
        {
            artifactDir = Path.Combine(Path.GetTempPath(), "s16-acc");
        }

        Directory.CreateDirectory(artifactDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var schoolId = DemoSchoolIds.SchoolId;
        Guid classId;
        string plainCode;

        await using (var ctx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, TeacherEmail));
            await page.GotoAsync(baseUrl + "/leraar", new() { WaitUntil = WaitUntilState.Load });
            await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 180_000 });
            var codesLink = page.Locator("a[href*='/codes']").First;
            if (await codesLink.CountAsync() == 0)
            {
                foreach (var fallbackClass in new[] { DemoSchoolIds.ClassGroep78Id, DemoSchoolIds.Class1AId })
                {
                    await page.GotoAsync($"{baseUrl}/leraar/klas/{fallbackClass:D}/codes", new() { WaitUntil = WaitUntilState.Load });
                    await page.WaitForTimeoutAsync(4000);
                    if (await page.Locator(".sch-code").CountAsync() > 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                await codesLink.ClickAsync();
                await page.WaitForURLAsync("**/codes**", new() { Timeout = 180_000 });
            }

            await page.WaitForFunctionAsync(
                """
                () => document.querySelector('.sch-code')
                  || document.querySelector('[data-testid=leraar-code-table] .state-message.error')
                """,
                null,
                new() { Timeout = 180_000 });
            var codeLoc = page.Locator("[data-testid=\"leraar-code-table\"] .sch-code, .sch-code");
            var count = await codeLoc.CountAsync();
            Assert.True(count >= 3, $"Need at least 3 pupil codes on acc, saw {count}. Url={page.Url}");
            plainCode = (await codeLoc.Nth(2).InnerTextAsync()).Trim();
            Assert.Matches(@"^[A-Z0-9]{3}-[A-Z0-9]{3}$", plainCode);
            classId = Guid.Parse(
                new Uri(page.Url).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .SkipWhile(s => !string.Equals(s, "klas", StringComparison.OrdinalIgnoreCase))
                    .Skip(1)
                    .First());

            foreach (var (w, h, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await using var pCtx = await browser.NewContextAsync(KbE2e.ContextFor(w, h));
                var pPage = await pCtx.NewPageAsync();
                pPage.SetDefaultTimeout(120_000);
                var consoleErrors = CaptureConsoleErrors(pPage);
                var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
                await PupilLoginAsync(pPage, loginUrl, plainCode, classId);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var droomResp = await pPage.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                sw.Stop();
                Assert.NotNull(droomResp);
                Assert.True(droomResp!.Ok, $"droombaan HTTP {(int)droomResp.Status}");
                await pPage.Locator("[data-testid=leerling-droombaan-search]").WaitForAsync();
                var stylesheetOk = await pPage.EvaluateAsync<bool>(
                    """
                    () => [...document.querySelectorAll('link[rel=stylesheet]')]
                      .every(l => (l.getAttribute('href')||'').startsWith('/'))
                    """);
                Assert.True(stylesheetOk, "Droombaan stylesheets must be root-relative.");

                var search = pPage.Locator("[data-testid=leerling-droombaan-search]");
                const string freeText = "Dierenarts arts";
                await search.FillAsync("");
                await search.FillAsync(freeText);
                await pPage.Locator("#leerling-droombaan-form button[type=submit]").ClickAsync();
                await pPage.WaitForURLAsync(
                    url => url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });
                await Assertions.Expect(search).ToHaveValueAsync(freeText);

                await pPage.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await Assertions.Expect(search).ToHaveValueAsync(freeText);

                var bodyText = await pPage.Locator("main").InnerTextAsync();
                Assert.DoesNotContain("Er ging iets mis", bodyText, StringComparison.OrdinalIgnoreCase);
                AssertNoBadConsole(consoleErrors);
                await ShotAsync(pPage, artifactDir, $"droombaan-{tag}", sw.ElapsedMilliseconds);

                // Kok reset (seed code 3)
                await pPage.GetByRole(AriaRole.Button, new() { Name = "Kok" }).ClickAsync(new() { Timeout = 15_000 });
                await pPage.WaitForURLAsync(
                    url => url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });
                await Assertions.Expect(search).ToHaveValueAsync("Kok");

                sw.Restart();
                await pPage.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                sw.Stop();
                await pPage.Locator("[data-testid=leerling-paspoort-page]").WaitForAsync();
                var layoutJson = await pPage.EvaluateAsync<string>(
                    """
                    () => {
                      const pageEl = document.querySelector('[data-testid=leerling-paspoort-page]');
                      const actions = pageEl?.querySelector('[data-testid=leerling-paspoort-actions]');
                      const content = pageEl?.querySelector('.lp-g78');
                      const btns = actions ? [...actions.querySelectorAll('a,button')] : [];
                      const minH = btns.length ? Math.min(...btns.map(b => b.getBoundingClientRect().height)) : 0;
                      if (!pageEl || !actions || !content) return JSON.stringify({ ok:false });
                      const ar = actions.getBoundingClientRect();
                      const cr = content.getBoundingClientRect();
                      const left = Math.round(ar.left - cr.left);
                      const right = Math.round(cr.right - ar.right);
                      return JSON.stringify({ ok: Math.abs(left-right)<=2, left, right, minH });
                    }
                    """);
                using (var doc = System.Text.Json.JsonDocument.Parse(layoutJson))
                {
                    var r = doc.RootElement;
                    Assert.True(r.GetProperty("ok").GetBoolean(), layoutJson);
                    Assert.True(r.GetProperty("minH").GetDouble() >= 51, layoutJson);
                }

                await ShotAsync(pPage, artifactDir, $"paspoort-nl-{tag}", sw.ElapsedMilliseconds);

                sw.Restart();
                await pPage.GotoAsync($"{baseUrl}/taal/en?return=/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await pPage.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                sw.Stop();
                var enText = await pPage.Locator("[data-testid=leerling-paspoort-page]").InnerTextAsync();
                Assert.DoesNotContain("Mijn droombaan", enText, StringComparison.Ordinal);
                Assert.DoesNotContain("Er ging iets mis", enText, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("!", enText, StringComparison.Ordinal);
                await ShotAsync(pPage, artifactDir, $"paspoort-en-kok-{tag}", sw.ElapsedMilliseconds);

                // Dierenarts EN: switch dream via droombaan if needed
                await pPage.GotoAsync($"{baseUrl}/leerling/droombaan", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                await pPage.GetByRole(AriaRole.Button, new() { Name = "Dierenarts" }).ClickAsync(new() { Timeout = 15_000 });
                await pPage.WaitForURLAsync(
                    url => url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });

                await pPage.GotoAsync($"{baseUrl}/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                enText = await pPage.Locator("[data-testid=leerling-paspoort-page]").InnerTextAsync();
                Assert.Contains("Dream", enText, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("!", enText, StringComparison.Ordinal);
                await ShotAsync(pPage, artifactDir, $"paspoort-en-dierenarts-{tag}", 0);

                // PDF links (NL + EN)
                var pdfNl = pPage.Locator("a[href*='paspoort/pdf'], a[href*='passport/pdf']").First;
                if (await pdfNl.CountAsync() > 0)
                {
                    var pdfResp = await pPage.RunAndWaitForResponseAsync(
                        async () => await pdfNl.ClickAsync(new() { Force = true }),
                        r => r.Url.Contains("pdf", StringComparison.OrdinalIgnoreCase),
                        new() { Timeout = 60_000 });
                    Assert.True(pdfResp.Ok, $"PDF status {(int)pdfResp.Status}");
                    var bytes = await pdfResp.BodyAsync();
                    Assert.True(bytes.Length > 100 && bytes[0] == (byte)'%');
                    await File.WriteAllBytesAsync(Path.Combine(artifactDir, $"pdf-nl-{tag}.pdf"), bytes);
                }

                await pPage.GotoAsync($"{baseUrl}/taal/nl?return=/leerling/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }
        }
    }

    private static List<string> CaptureConsoleErrors(IPage page)
    {
        var list = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                list.Add(msg.Text);
            }
        };
        return list;
    }

    private static void AssertNoBadConsole(IReadOnlyList<string> errors)
    {
        var bad = errors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
            && !e.Contains("Refused to apply style", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(bad.Count == 0, "Console: " + string.Join("; ", bad));
    }

    private static async Task ShotAsync(IPage page, string dir, string name, long loadMs)
    {
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, $"{name}-load{loadMs}ms.png"),
            FullPage = true
        });
    }

    private static async Task PupilLoginAsync(IPage page, string loginUrl, string code, Guid classId)
    {
        var classValue = classId.ToString("D");
        await page.Context.ClearCookiesAsync();
        await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.EvaluateAsync(
            """
            ([id, code]) => new Promise((resolve, reject) => {
              const deadline = performance.now() + 30000;
              const tick = () => {
                const select = document.querySelector('select[name=classId]');
                const input = document.querySelector('input[name=code]');
                const option = select && [...select.options].find(o => o.value === id);
                if (select && input && option) {
                  option.selected = true;
                  select.value = id;
                  input.value = code;
                  const form = input.closest('form');
                  if (!form) { reject(new Error('no form')); return; }
                  if (typeof form.requestSubmit === 'function') form.requestSubmit();
                  else form.submit();
                  resolve(true);
                  return;
                }
                if (performance.now() > deadline) reject(new Error('login timeout'));
                else setTimeout(tick, 50);
              };
              tick();
            })
            """,
            new object[] { classValue, code });
        await page.WaitForURLAsync(
            url => !url.Contains("/leerling?", StringComparison.OrdinalIgnoreCase)
                || url.Contains("error=", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });
        Assert.DoesNotContain("error=", page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
