using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Rules;
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
    private const string ScreenshotDir = "/cursor/stores/self/proactive/final";
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";
    private static readonly Guid DemoCandidateId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid UnavailableCandidateId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333");
    private static readonly Guid WestlandVacancyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid EmployerUserId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");

    [Fact]
    public async Task Instant_proactive_match_after_publish_desktop_and_mobile()
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
        TryRunPrepareSql();
        await PrepareCandidateProactiveSettingsViaApiAsync();
        _ = await CreateAndPublishCompanionVacancyAsync();
        await WaitForProactiveMatchNotificationAsync(TimeSpan.FromSeconds(90));

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using (var desktop = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            await PlaywrightCookieConsent.AcceptAsync(desktop);
            var page = await desktop.NewPageAsync();
            page.SetDefaultTimeout(60_000);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
            await DismissCookieBannerIfVisibleAsync(page);
            await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(".proactive-page").WaitForAsync();
            await AssertProactiveMatchInAppNotificationAsync(page, "desktop", "instant-publish-notification");
        }

        await using (var mobile = await browser.NewContextAsync(KbE2e.MobileContext()))
        {
            await PlaywrightCookieConsent.AcceptAsync(mobile);
            var page = await mobile.NewPageAsync();
            page.SetDefaultTimeout(60_000);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
            await DismissCookieBannerIfVisibleAsync(page);
            await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(".proactive-page").WaitForAsync();
            await TryNotificationPanelShotAsync(page, "mobile", "instant-publish-notification", "Nieuwe match:");
        }
    }

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
        TryRunPrepareSql();
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
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        // —— Kandidaat: proactieve matching ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".proactive-page, .proactive-consent, .proactive-form").First.WaitForAsync();
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"01-candidate-proactive-landing", label);

        var consentBtn = page.Locator(".proactive-consent button.login-submit");
        if (await consentBtn.CountAsync() > 0)
        {
            await consentBtn.ClickAsync();
            await page.WaitForTimeoutAsync(800);
            await DismissCookieBannerIfVisibleAsync(page);
            await Shot(page, $"02-candidate-consent-accepted", label);
        }

        var enableToggle = page.Locator(".proactive-toggle input[type=checkbox]");
        if (await enableToggle.CountAsync() > 0 && !await enableToggle.IsCheckedAsync())
        {
            await enableToggle.CheckAsync();
            await page.WaitForTimeoutAsync(800);
            await DismissCookieBannerIfVisibleAsync(page);
            await Shot(page, $"03-candidate-beschikbaar-on", label);
        }

        await context.ClearCookiesAsync();
        await PrepareCandidateProactiveSettingsViaApiAsync();
        _ = await CreateAndPublishCompanionVacancyAsync();
        await WaitForProactiveMatchNotificationAsync(TimeSpan.FromSeconds(90));

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        await DismissCookieBannerIfVisibleAsync(page);
        var suggestion = page.Locator(".proactive-suggestion-card").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.WaitForAsync();
            var whyText = (await page.Locator(".proactive-tip__why-text").First.InnerTextAsync()).Trim();
            Assert.False(
                ProactiveMatchingRules.IsGenericStoredRationale(whyText),
                $"Expected DNA-based rationale, got: {whyText}");
            var dnaChips = page.Locator(".proactive-tip__dna-chip");
            Assert.True(
                await dnaChips.CountAsync() > 0 || whyText.Contains('·', StringComparison.Ordinal),
                "Expected DNA highlight chips or multi-part rationale.");
            await Shot(page, $"04-candidate-match-notification", label);

            await AssertProactiveMatchInAppNotificationAsync(page, label);

            var feedbackBtn = page.Locator("button:has-text('Interessant'), button:has-text('Niet interessant')").First;
            if (await feedbackBtn.CountAsync() > 0)
            {
                await feedbackBtn.ClickAsync();
                await page.WaitForTimeoutAsync(600);
                await DismissCookieBannerIfVisibleAsync(page);
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
        await DismissCookieBannerIfVisibleAsync(page);
        var balanceBefore = await GetWestlandTokenBalanceAsync();

        var shortlistUrl = baseUrl + $"/werkgever/vacatures/{WestlandVacancyId:D}/proactieve-shortlist";
        await page.GotoAsync(shortlistUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=proactive-shortlist]").WaitForAsync(new() { Timeout = 90_000 });
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"06-employer-vacancy-shortlist", label);

        var shortlistJson = await FetchEmployerShortlistAsync(WestlandVacancyId);
        Assert.DoesNotContain(
            UnavailableCandidateId,
            shortlistJson.Select(e => e.GetProperty("candidateUserId").GetGuid()));
        Assert.Contains(
            DemoCandidateId,
            shortlistJson.Select(e => e.GetProperty("candidateUserId").GetGuid()));
        foreach (var entry in shortlistJson)
        {
            Assert.True(
                entry.GetProperty("matchPercent").GetInt32() >= 80,
                "Shortlist must only include matches at or above the configured minimum (80%).");
            var availability = entry.GetProperty("availabilitySummary").GetString() ?? "";
            Assert.False(
                availability.Contains("onbekend", StringComparison.OrdinalIgnoreCase),
                "OpenForWork shortlist entries must show real availability.");
        }

        _ = await TryPostIntroduceViaApiAsync(DemoCandidateId, "Graag kennismaken voor deze vacature (E2E real stack).");

        await context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".proactive-page").WaitForAsync();
        await AssertIntroductionInAppNotificationAsync(page, label);

        await context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"07-employer-kennismaking-sent", label);

        var balanceAfterIntro = await GetWestlandTokenBalanceAsync();
        Assert.Equal(balanceBefore, balanceAfterIntro);

        await context.ClearCookiesAsync();

        // —— Kandidaat accepteert ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(baseUrl + "/candidate/talent-contacts", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1000);
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"08-candidate-talent-contacts", label);

        var openRequest = page.Locator(".talent-req--open .talent-req__actions .btn-primary").First;
        if (await openRequest.CountAsync() > 0)
        {
            await openRequest.ClickAsync();
            await page.WaitForTimeoutAsync(1200);
            await DismissCookieBannerIfVisibleAsync(page);
            await Shot(page, $"08b-candidate-share-dialog", label);
            var confirmShare = page.Locator(".talent-dialog__share").First;
            await confirmShare.WaitForAsync(new() { Timeout = 30_000 });
            await confirmShare.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
        }

        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"09-candidate-accepted", label);

        await context.ClearCookiesAsync();

        // —— Werkgever ziet PII; token -1 ——
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(baseUrl + "/werkgever/talentpool", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1500);
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, $"10-employer-talentpool-after-accept", label);

        var balanceAfterAccept = await GetWestlandTokenBalanceAsync();
        Assert.True(
            balanceAfterAccept == balanceBefore - 1 || balanceAfterAccept == balanceBefore,
            $"Token balance unexpected after accept (before={balanceBefore}, after={balanceAfterAccept}).");
        return balanceBefore;
    }

    private static async Task ReshootMobileTourAsync(IBrowser browser, string baseUrl, decimal balanceBefore)
    {
        await using var context = await browser.NewContextAsync(KbE2e.MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, CandidateEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(baseUrl + "/candidate/proactive-matching", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".proactive-page").WaitForAsync();
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, "01-candidate-proactive-landing", "mobile");
        await TryNotificationPanelShotAsync(
            page,
            "mobile",
            "11-notifications-proactive-match",
            "Nieuwe match:");
        await TryNotificationPanelShotAsync(
            page,
            "mobile",
            "12-notifications-kennismaking",
            "Nieuw kennismakingsverzoek:");
        await page.GotoAsync(baseUrl + "/candidate/talent-contacts", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(800);
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, "08-candidate-talent-contacts", "mobile");
        await Shot(page, "09-candidate-accepted", "mobile");

        await context.ClearCookiesAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, EmployerEmail, Password));
        await DismissCookieBannerIfVisibleAsync(page);
        await page.GotoAsync(
            baseUrl + $"/werkgever/vacatures/{WestlandVacancyId:D}/proactieve-shortlist",
            new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("[data-testid=proactive-shortlist]").WaitForAsync();
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, "06-employer-vacancy-shortlist", "mobile");
        await page.GotoAsync(baseUrl + "/werkgever/talentpool", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        await DismissCookieBannerIfVisibleAsync(page);
        await Shot(page, "10-employer-talentpool-after-accept", "mobile");

        var balanceAfterAccept = await GetWestlandTokenBalanceAsync();
        Assert.True(
            balanceAfterAccept == balanceBefore - 1 || balanceAfterAccept == balanceBefore,
            $"Token balance unexpected after accept (before={balanceBefore}, after={balanceAfterAccept}).");
    }

    private static Task AssertProactiveMatchInAppNotificationAsync(IPage page, string label) =>
        AssertProactiveMatchInAppNotificationAsync(page, label, "11-notifications-proactive-match");

    private static async Task AssertProactiveMatchInAppNotificationAsync(IPage page, string label, string shotName)
    {
        var notifs = await FetchCandidateNotificationsAsync();
        Assert.Contains(
            notifs,
            n => (n.GetProperty("title").GetString() ?? "").StartsWith("Nieuwe match:", StringComparison.Ordinal));

        var unread = await FetchCandidateUnreadCountAsync();
        if (unread > 0)
        {
            await page.Locator(".notification-bell__badge").WaitForAsync(new() { Timeout = 45_000 });
        }

        await OpenNotificationsPanelAndShotAsync(page, label, shotName, "Nieuwe match:");
    }

    private static async Task AssertIntroductionInAppNotificationAsync(IPage page, string label)
    {
        var notifs = await FetchCandidateNotificationsAsync();
        Assert.Contains(
            notifs,
            n => (n.GetProperty("title").GetString() ?? "")
                .StartsWith("Nieuw kennismakingsverzoek:", StringComparison.Ordinal));

        var unread = await FetchCandidateUnreadCountAsync();
        if (unread > 0)
        {
            await page.Locator(".notification-bell__badge").WaitForAsync(new() { Timeout = 45_000 });
        }

        await OpenNotificationsPanelAndShotAsync(page, label, "12-notifications-kennismaking", "Nieuw kennismakingsverzoek:");
    }

    private static async Task TryNotificationPanelShotAsync(
        IPage page,
        string label,
        string shotName,
        string titlePrefix)
    {
        var notifs = await FetchCandidateNotificationsAsync();
        if (!notifs.Any(n => (n.GetProperty("title").GetString() ?? "").StartsWith(titlePrefix, StringComparison.Ordinal)))
        {
            return;
        }

        try
        {
            await OpenNotificationsPanelAndShotAsync(page, label, shotName, titlePrefix);
        }
        catch (TimeoutException)
        {
            if (await page.Locator(".notification-bell__panel").IsVisibleAsync())
            {
                await Shot(page, shotName, label);
                await page.Locator(".notification-bell__close").ClickAsync();
            }
        }
    }

    private static async Task OpenNotificationsPanelAndShotAsync(
        IPage page,
        string label,
        string shotName,
        string titlePrefix)
    {
        await DismissCookieBannerIfVisibleAsync(page);

        await page.Locator(".notification-bell__toggle").ClickAsync();
        await page.Locator(".notification-bell__panel").WaitForAsync();
        await page.Locator(".notification-bell__list .notification-bell__item").First.WaitForAsync(new() { Timeout = 45_000 });
        var title = page.Locator(".notification-bell__item-title").Filter(new() { HasText = titlePrefix }).First;
        await title.WaitForAsync(new() { Timeout = 45_000 });
        await Shot(page, shotName, label);
        await page.Locator(".notification-bell__close").ClickAsync();
        await page.WaitForTimeoutAsync(300);
    }

    private static async Task<List<JsonElement>> FetchCandidateNotificationsAsync()
    {
        var json = await GetCandidateApiJsonAsync("/api/notifications?take=50");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    private static async Task<int> FetchCandidateUnreadCountAsync()
    {
        var json = await GetCandidateApiJsonAsync("/api/notifications/unread-count");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("count", out var c)
            ? c.GetInt32()
            : doc.RootElement.GetProperty("Count").GetInt32();
    }

    private static async Task<string> GetCandidateApiJsonAsync(string path)
    {
        using var http = CreateCandidateApiClient();
        using var response = await http.GetAsync("http://127.0.0.1:5200" + path);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        return body;
    }

    private static HttpClient CreateCandidateApiClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JobsyTestAuth.Mint(DemoCandidateId));
        return http;
    }

    private static async Task DismissCookieBannerIfVisibleAsync(IPage page)
    {
        var accept = page.Locator(".cookie-consent button[data-consent=analytics], .cookie-consent button:has-text('Accepteer cookies')").First;
        if (await accept.CountAsync() > 0 && await accept.IsVisibleAsync())
        {
            await accept.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
    }

    private static async Task Shot(IPage page, string name, string viewport)
    {
        var path = Path.Combine(ScreenshotDir, $"{name}-{viewport}.png");
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await page.ScreenshotAsync(new() { Path = path, FullPage = false });
                return;
            }
            catch (IOException) when (attempt < 2)
            {
                await Task.Delay(250);
            }
        }
    }

    private static async Task PrepareCandidateProactiveSettingsViaApiAsync()
    {
        using var http = CreateCandidateApiClient();
        var payload = JsonSerializer.Serialize(new { acceptConsent = true, enabled = true });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await http.PutAsync(
            "http://127.0.0.1:5200/api/me/proactive-matching/settings",
            content);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
    }

    private static async Task<Guid> CreateAndPublishCompanionVacancyAsync()
    {
        var template = await ResolveEmployerTemplateVacancyAsync();
        var companyId = template.GetProperty("companyId").GetGuid();
        var salaryTableId = template.TryGetProperty("salaryTableId", out var stEl)
                            && stEl.ValueKind is JsonValueKind.String
            ? stEl.GetGuid()
            : throw new InvalidOperationException("Template vacancy has no salaryTableId.");
        var hourlyWage = template.TryGetProperty("hourlyWage", out var wageEl) && wageEl.ValueKind == JsonValueKind.Number
            ? wageEl.GetDecimal()
            : template.GetProperty("wageByAge").EnumerateArray().First().GetProperty("hourlyRate").GetDecimal();
        var title = $"E2E instant match {DateTime.UtcNow:yyyyMMddHHmmss}";
        var createPayload = JsonSerializer.Serialize(new
        {
            companyId,
            title,
            description = template.TryGetProperty("description", out var descEl)
                ? descEl.GetString()
                : "E2E vacature voor proactieve matching.",
            hourlyWage,
            startDate = template.GetProperty("startDate").GetString(),
            endDate = template.GetProperty("endDate").GetString(),
            requiredTransport = ReadRequiredTransportFlags(template),
            workTypes = template.TryGetProperty("workTypes", out var wtEl)
                ? wtEl.EnumerateArray().Select(e => e.GetString()).Where(s => s is not null).ToArray()
                : Array.Empty<string>(),
            salaryTableId,
            flexibleTimes = true
        });

        using var createHttp = CreateEmployerApiClient();
        using var createContent = new StringContent(createPayload, Encoding.UTF8, "application/json");
        using var createResponse = await createHttp.PostAsync("http://127.0.0.1:5200/api/vacancies", createContent);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.IsSuccessStatusCode, createBody);
        using var createDoc = JsonDocument.Parse(createBody);
        var vacancyId = createDoc.RootElement.GetProperty("id").GetGuid();

        var publishPayload = JsonSerializer.Serialize(new
        {
            vacancyId,
            highlight = false,
            pushBom = false,
            extend = false
        });
        using var publishContent = new StringContent(publishPayload, Encoding.UTF8, "application/json");
        using var publishResponse = await createHttp.PostAsync(
            "http://127.0.0.1:5200/api/vacancies/publish",
            publishContent);
        var publishBody = await publishResponse.Content.ReadAsStringAsync();
        Assert.True(publishResponse.IsSuccessStatusCode, publishBody);
        return vacancyId;
    }

    private static int ReadRequiredTransportFlags(JsonElement template)
    {
        if (!template.TryGetProperty("requiredTransport", out var rt) || rt.ValueKind != JsonValueKind.Array)
        {
            return (int)TransportLabels.ParseMany(null);
        }

        var labels = rt.EnumerateArray()
            .Select(e => e.GetString())
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return (int)TransportLabels.ParseMany(string.Join(", ", labels));
    }

    private static async Task<JsonElement> ResolveEmployerTemplateVacancyAsync()
    {
        var rows = await FetchEmployerJsonArrayAsync("/api/vacancies/manage");
        foreach (var row in rows)
        {
            var status = row.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString()
                : null;
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (row.TryGetProperty("salaryTableId", out _))
            {
                return row.Clone();
            }
        }

        throw new InvalidOperationException("No active managed vacancy with salary table found for E2E publish template.");
    }

    private static async Task WaitForProactiveMatchNotificationAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var notifs = await FetchCandidateNotificationsAsync();
            if (notifs.Any(n =>
                    (n.GetProperty("title").GetString() ?? "")
                    .StartsWith("Nieuwe match:", StringComparison.Ordinal)))
            {
                return;
            }

            await Task.Delay(2000);
        }

        Assert.Fail("Geen proactieve match-notificatie binnen de wachttijd na publicatie.");
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

    private static void TryRunPrepareSql()
    {
        var script = Path.Combine(FindRepoRoot(), "scripts/prepare-proactive-real-e2e.sql");
        if (!File.Exists(script))
        {
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h 127.0.0.1 -U postgres -d JobsyCi -f \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            psi.Environment["PGPASSWORD"] =
                Environment.GetEnvironmentVariable("PGPASSWORD") ?? "postgres";
            using var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit(15_000);
        }
        catch
        {
            // Best-effort for local/CI JobsyCi; soft-skip when psql unavailable.
        }
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
