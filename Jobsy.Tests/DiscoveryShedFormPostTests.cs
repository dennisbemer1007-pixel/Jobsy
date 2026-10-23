using System.Net;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Discovery;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class DiscoveryShedFormPostTests
{
    [Fact]
    public async Task Post_diepte_persists_selection_and_redirects_shed()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=shed-7"));

        var get = await client.GetAsync("/candidate/ontdekkingsreis?stap=shed-7");
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiforgery(html);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var deepest = JourneyTestFlow.BuildDepthOptions(
                OnboardingWizardCatalog.OnboardingTestKind.Competency,
                answeredCount: 12)
            .Max(o => o.Level);
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            ["shedStep"] = "7",
            ["level"] = deepest.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        var post = await client.PostAsync("/candidate/ontdekkingsreis/diepte", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.SeeOther, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.Contains("stap=shed-7", location, StringComparison.OrdinalIgnoreCase);

        var after = await client.GetAsync("/candidate/ontdekkingsreis?stap=shed-7");
        Assert.True(after.IsSuccessStatusCode, $"Reload shed failed: {(int)after.StatusCode}");
        var afterHtml = await after.Content.ReadAsStringAsync();
        Assert.Contains("data-testid=\"discovery-shed\"", afterHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("state-message--error", afterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_duik_does_not_return_error_page()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=shed-7"));

        var get = await client.GetAsync("/candidate/ontdekkingsreis?stap=shed-7");
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiforgery(html);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            ["shedStep"] = "7",
        };

        var post = await client.PostAsync("/candidate/ontdekkingsreis/duik", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.SeeOther, post.StatusCode);
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("LB-", location, StringComparison.Ordinal);

        Assert.True(
            location.Contains("stap=shed-7", StringComparison.OrdinalIgnoreCase)
            || location.Contains("stap=7", StringComparison.OrdinalIgnoreCase),
            "Location: " + location);

        var follow = await client.GetAsync("/candidate/ontdekkingsreis?stap=shed-7");
        var body = await follow.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Er ging iets mis", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stap_7_is_not_redirected_to_overview_when_journey_complete()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=7"));

        var get = await client.GetAsync("/candidate/ontdekkingsreis?stap=7");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Contains("stap=7", get.RequestMessage!.RequestUri!.Query, StringComparison.OrdinalIgnoreCase);
        var html = await get.Content.ReadAsStringAsync();
        Assert.Contains("data-testid=\"discovery-test-step\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bare_discovery_url_returns_single_303_to_overview()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis"));

        var get = await client.GetAsync("/candidate/ontdekkingsreis");
        Assert.Equal(HttpStatusCode.SeeOther, get.StatusCode);
        var location = get.Headers.Location?.ToString() ?? "";
        Assert.Contains("stap=overview", location, StringComparison.OrdinalIgnoreCase);
        var html = await get.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Verder ontdekken", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("?stap=1", html, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractAntiforgery(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" value=\"";
        var i = html.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }

        i += marker.Length;
        var j = html.IndexOf('"', i);
        return j < 0 ? null : html[i..j];
    }
}
