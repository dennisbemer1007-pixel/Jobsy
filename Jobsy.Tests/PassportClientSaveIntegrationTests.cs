using System.Net;
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
        factory.StoredProfile.FirstName = "Paspoort";
        factory.StoredProfile.LastName = "Tester";
        var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort?tab=data"));
        var page = await client.GetAsync("/candidate/paspoort?tab=data");
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();
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
