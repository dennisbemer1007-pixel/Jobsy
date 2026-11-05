using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public sealed class PassportClientSaveIntegrationTests
{
    [Fact]
    public async Task Stub_api_update_profile_deserializes()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.StoredProfile.FirstName = "Paspoort";
        factory.StoredProfile.LastName = "Tester";
        var api = factory.CreateStubApiClient();
        var updated = await api.UpdateMyProfileAsync(
            firstName: "Paspoort",
            lastName: "Tester",
            phoneNumber: "0698765432");
        Assert.NotNull(updated);
        Assert.Equal("0698765432", updated.PhoneNumber);
    }

    [Fact]
    public async Task Save_data_redirects_with_saved_flag()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.CandidatePassportEnabled = true;
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.StoredProfile.FirstName = "Paspoort";
        factory.StoredProfile.LastName = "Tester";
        var root = factory.EnsureServerAddress();
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(root) };

        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort?tab=data"));
        var page = await client.GetAsync("/candidate/paspoort?tab=data");
        if (page.StatusCode == HttpStatusCode.Redirect && page.Headers.Location is not null)
        {
            page = await client.GetAsync(page.Headers.Location);
        }

        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();
        if (!html.Contains("data-testid=\"passport-page-ssr\"", StringComparison.Ordinal))
        {
            Assert.Fail("Expected passport SSR page; got: " + html[..Math.Min(html.Length, 500)]);
        }
        var tokenMatch = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
        Assert.True(tokenMatch.Success, "Expected antiforgery token on passport data tab.");

        using var form = new MultipartFormDataContent
        {
            { new StringContent(tokenMatch.Groups[1].Value), "__RequestVerificationToken" },
            { new StringContent("data"), "returnTab" },
            { new StringContent("Paspoort"), "firstName" },
            { new StringContent("Tester"), "lastName" },
            { new StringContent("0698765432"), "phoneNumber" },
        };

        var save = await client.PostAsync("/api/passport-client/save-data", form);
        Assert.Equal(HttpStatusCode.Redirect, save.StatusCode);
        var location = save.Headers.Location?.ToString() ?? "";
        Assert.True(
            location.Contains("saved=1", StringComparison.OrdinalIgnoreCase),
            "Redirect location: " + location + "; ProfilePutCount=" + factory.ProfilePutCount);
    }
}
