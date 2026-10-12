using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real-stack proactive matching E2E (Kestrel + JobsyCi). Soft-skips unless
/// <c>JOBSY_E2E_BASE_URL</c> is set and <c>JOBSY_PROACTIVE_REAL=1</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ProactiveMatchingRealStackE2eTests
{
    private const string ScreenshotDir = "/cursor/stores/self/proactive/real";
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";
    private static readonly Guid DemoCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid UnavailableCandidateId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");
    private static readonly Guid WestlandVacancyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EmployerUserId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");

    [Fact]
    public async Task Proactive_flow_candidate_and_employer_real_stack()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_PROACTIVE_REAL"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Directory.CreateDirectory(ScreenshotDir);
        await using var browser = await KbE2e.LaunchChromiumAsync();

        var balanceBefore = await RunMutationFlowAsync(browser, KbE2e.DesktopContext(), baseUrl, "desktop");
        await ReshootMobileTourAsync(browser, baseUrl, balanceBefore);
    }

    private static async Task<decimal> RunMutationFlowAsync(
        IBrowser browser,
        BrowserNewContextOptions contextOptions,
        string baseUrl,
        string label)
    {
        await using var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        // —— Kandidaat: proactieve matching ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".proactive-page, .proactive-consent, .proactive-form").First.WaitForAsync();
        await Shot(page, $"01-candidate-proactive-landing", label);

        var consentBtn = page.Locator(".proactive-consent button.login-submit");
        if (await consentBtn.CountAsync() > 0)
        {
            await consentBtn.ClickAsync();
            await page.WaitForTimeoutAsync(800);
            await Shot(page, $"02-candidate-consent-accepted", label);
        }

        var enableToggle = page.Locator(".proactive-toggle input[type=checkbox]");
        if (await enableToggle.CountAsync() > 0 && !await enableToggle.IsCheckedAsync())
        {
            await enableToggle.CheckAsync();
            await page.WaitForTimeoutAsync(800);
            await Shot(page, $"03-candidate-beschikbaar-on", label);
        }

        await TriggerDailyMatchingJobAsync();

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        var suggestion = page.Locator(".proactive-suggestion-card, .proactive-suggestions article, .proactive-suggestions li").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.WaitForAsync();
            await Shot(page, $"04-candidate-match-notification", label);

            var feedbackBtn = page.Locator("button:has-text('Interessant'), button:has-text('Niet interessant')").First;
            if (await feedbackBtn.CountAsync() > 0)
            {
                await feedbackBtn.ClickAsync();
                await page.WaitForTimeoutAsync(600);
                await Shot(page, $"05-candidate-feedback", label);
            }
        }
        else
        {
            await Shot(page, $"04-candidate-no-suggestions-yet", label);
        }

        await context.ClearCookiesAsync();

        // —— Werkgever: shortlist + kennismaking ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        var balanceBefore = await GetWestlandTokenBalanceAsync();

        var editUrl = baseUrl + $"/werkgever/vacatures/nieuw?edit={WestlandVacancyId:D}";
        await page.GotoAsync(editUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=proactive-shortlist]").WaitForAsync(new() { Timeout = 90_000 });
        await Shot(page, $"06-employer-vacancy-shortlist", label);

        var shortlistJson = await FetchEmployerShortlistAsync(WestlandVacancyId);
        Assert.DoesNotContain(
            UnavailableCandidateId,
            shortlistJson.Select(e => e.GetProperty("candidateUserId").GetGuid()));
        Assert.Contains(
            DemoCandidateId,
            shortlistJson.Select(e => e.GetProperty("candidateUserId").GetGuid()));

        _ = await TryPostIntroduceViaApiAsync(DemoCandidateId, "Graag kennismaken voor deze vacature (E2E real stack).");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        await Shot(page, $"07-employer-kennismaking-sent", label);

        var balanceAfterIntro = await GetWestlandTokenBalanceAsync();
        Assert.Equal(balanceBefore, balanceAfterIntro);

        await context.ClearCookiesAsync();

        // —— Kandidaat accepteert ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await page.GotoAsync(baseUrl + "/candidate/talent-contacts", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1000);
        await Shot(page, $"08-candidate-talent-contacts", label);

        var openRequest = page.Locator(".talent-req--open .talent-req__actions .btn-primary").First;
        if (await openRequest.CountAsync() > 0)
        {
            await openRequest.ClickAsync();
            await page.WaitForTimeoutAsync(1200);
            await Shot(page, $"08b-candidate-share-dialog", label);
            var confirmShare = page.Locator(".talent-dialog__share").First;
            await confirmShare.WaitForAsync(new() { Timeout = 30_000 });
            await confirmShare.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
        }

        await Shot(page, $"09-candidate-accepted", label);

        await context.ClearCookiesAsync();

        // —— Werkgever ziet PII; token -1 ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        await page.GotoAsync(baseUrl + "/werkgever/talentpool", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1500);
        await Shot(page, $"10-employer-talentpool-after-accept", label);

        var balanceAfterAccept = await GetWestlandTokenBalanceAsync();
        Assert.Equal(balanceBefore - 1, balanceAfterAccept);
        return balanceBefore;
    }

    private static async Task ReshootMobileTourAsync(IBrowser browser, string baseUrl, decimal balanceBefore)
    {
        await using var context = await browser.NewContextAsync(KbE2e.MobileContext());
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".proactive-page").WaitForAsync();
        await Shot(page, "01-candidate-proactive-landing", "mobile");
        await page.GotoAsync(baseUrl + "/candidate/talent-contacts", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        await Shot(page, "08-candidate-talent-contacts", "mobile");
        await Shot(page, "09-candidate-accepted", "mobile");

        await context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        await page.GotoAsync(
            baseUrl + $"/werkgever/vacatures/nieuw?edit={WestlandVacancyId:D}",
            new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=proactive-shortlist]").WaitForAsync();
        await Shot(page, "06-employer-vacancy-shortlist", "mobile");
        await page.GotoAsync(baseUrl + "/werkgever/talentpool", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        await Shot(page, "10-employer-talentpool-after-accept", "mobile");

        var balanceAfterAccept = await GetWestlandTokenBalanceAsync();
        Assert.Equal(balanceBefore - 1, balanceAfterAccept);
    }

    private static async Task Shot(IPage page, string name, string viewport)
    {
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ScreenshotDir, $"{name}-{viewport}.png"),
            FullPage = true
        });
    }

    private static async Task TriggerDailyMatchingJobAsync()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project Jobsy.Api/Jobsy.Api.csproj -c Release --no-build -- proactive-matching run-daily",
            WorkingDirectory = FindRepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.Environment["ConnectionStrings__JobsyDb"] =
            Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? "Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres";
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["Jobsy__TestEmployersEnabled"] = "true";
        using var proc = System.Diagnostics.Process.Start(psi)!;
        await proc.WaitForExitAsync();
        Assert.Equal(0, proc.ExitCode);
    }

    private static async Task<decimal> GetWestlandTokenBalanceAsync()
    {
        var rows = await FetchEmployerJsonArrayAsync("/api/tokens/balance");
        var westland = Guid.Parse("11111111-1111-1111-1111-111111111111");
        foreach (var row in rows)
        {
            if (row.GetProperty("companyId").GetGuid() == westland)
            {
                return row.GetProperty("balance").GetDecimal();
            }
        }

        throw new InvalidOperationException("Westland token balance not found.");
    }

    private static async Task<List<JsonElement>> FetchEmployerShortlistAsync(Guid vacancyId)
    {
        var path = $"/api/employer/proactive-matching/vacancies/{vacancyId:D}/shortlist";
        return await FetchEmployerJsonArrayAsync(path);
    }

    private static async Task<List<JsonElement>> FetchEmployerJsonArrayAsync(string path)
    {
        var json = await GetEmployerApiJsonAsync(path);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<string> GetEmployerApiJsonAsync(string path)
    {
        using var http = CreateEmployerApiClient();
        using var response = await http.GetAsync("http://127.0.0.1:5200" + path);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return body;
    }

    private static async Task<bool> TryPostIntroduceViaApiAsync(Guid candidateUserId, string message)
    {
        using var http = CreateEmployerApiClient();
        var payload = JsonSerializer.Serialize(new { candidateUserId, message });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var path =
            $"/api/employer/proactive-matching/vacancies/{WestlandVacancyId:D}/introduce";
        using var response = await http.PostAsync("http://127.0.0.1:5200" + path, content);
        return response.IsSuccessStatusCode;
    }

    private static HttpClient CreateEmployerApiClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(EmployerUserId));
        return http;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        return "/workspace";
    }
}
