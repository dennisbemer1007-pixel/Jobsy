using System.Text;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Jobsy.Infrastructure.Data;

namespace Jobsy.Tests.Scholen;

/// <summary>One-off acc diagnostic: RUN_MBO_HBO_E2E=1 dotnet test --filter MboHbo_acc_pdf_diagnostic</summary>
[Collection("PlaywrightSmoke")]
public sealed class MboHboAccPdfDiagnosticTest
{
    [Fact(Timeout = 300_000, DisplayName = "MboHbo_acc_pdf_diagnostic")]
    public async Task Log_pdf_response_without_redirects()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MBO_HBO_E2E"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
            ?? "/cursor/stores/bot/mbohbo3";
        Directory.CreateDirectory(artifactDir);
        var classId = DemoSchoolIds.ClassMbo2Id;
        var schoolId = DemoSchoolIds.SchoolId;
        var plainCode = (Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "4CY-VND").Trim();

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(KbE2e.DesktopContext());
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);

        var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
        await PupilLoginInlineAsync(page, loginUrl, plainCode, classId);
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 180_000 });

        var cookies = await ctx.CookiesAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Cookies after login:");
        foreach (var c in cookies.Where(c => c.Domain.Contains("lobsy", StringComparison.OrdinalIgnoreCase)))
        {
            sb.AppendLine($"  {c.Name} len={c.Value?.Length ?? 0} httpOnly={c.HttpOnly}");
        }

        var pdfUrl = baseUrl + "/leerling/pdf";
        var resp = await ctx.APIRequest.GetAsync(pdfUrl, new() { MaxRedirects = 0 });
        sb.AppendLine($"GET /leerling/pdf MaxRedirects=0 -> {(int)resp.Status} {resp.StatusText}");
        sb.AppendLine($"  Location: {resp.Headers.GetValueOrDefault("location") ?? resp.Headers.GetValueOrDefault("Location") ?? "(none)"}");
        sb.AppendLine($"  Content-Type: {resp.Headers.GetValueOrDefault("content-type") ?? "(none)"}");
        var body = await resp.BodyAsync();
        sb.AppendLine($"  Body length: {body.Length}, first byte: {(body.Length > 0 ? body[0] : 0)} ({(body.Length > 0 ? (char)body[0] : '?')})");

        var respFollow = await ctx.APIRequest.GetAsync(pdfUrl);
        var bodyFollow = await respFollow.BodyAsync();
        sb.AppendLine($"GET /leerling/pdf follow redirects -> {(int)respFollow.Status}, len={bodyFollow.Length}, first={ (bodyFollow.Length > 0 ? bodyFollow[0] : 0)}");

        await File.WriteAllTextAsync(Path.Combine(artifactDir, "pdf-diagnostic.txt"), sb.ToString());
        TestContext.Current.SendDiagnosticMessage(sb.ToString());
    }

    private static async Task PupilLoginInlineAsync(IPage page, string loginUrl, string code, Guid classId)
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
        await page.WaitForURLAsync(url => !url.Contains("/leerling?", StringComparison.Ordinal) || url.Contains("/mbo-hbo", StringComparison.Ordinal), new() { Timeout = 120_000 });
    }
}
