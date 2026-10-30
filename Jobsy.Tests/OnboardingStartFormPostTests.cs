using System.Net;
using System.Text.Json;

namespace Jobsy.Tests;

public sealed class OnboardingStartFormPostTests
{
    [Fact]
    public async Task Post_step1_advance_redirects_to_step2()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        _ = await client.GetAsync("/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));

        var get = await client.GetAsync("/candidate/start?step=1");
        var html = await get.Content.ReadAsStringAsync();
        Assert.DoesNotContain("blazor.web.js", html, StringComparison.OrdinalIgnoreCase);
        var token = ExtractAntiforgery(html);
        Assert.False(string.IsNullOrWhiteSpace(token));

        var payload = JsonSerializer.Serialize(new
        {
            firstName = "Onboarding",
            lastName = "Tester",
            phone = "",
            whatsAppAllowed = false,
            workStatuses = new[] { "scholier" },
            addressQuery = "2671GB Den Haag",
            homeLat = 52.1,
            homeLng = 4.2,
            birthDateText = "12-03-1990",
        });

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token!,
            ["obNav"] = "next",
            ["obPayload"] = payload,
        };

        var post = await client.PostAsync("/candidate/start?step=1", new FormUrlEncodedContent(form));
        Assert.True(post.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther,
            $"Expected redirect, got {(int)post.StatusCode} {await post.Content.ReadAsStringAsync()}");
        var location = post.Headers.Location?.ToString() ?? "";
        Assert.True(location.Contains("step=2", StringComparison.OrdinalIgnoreCase), "Location: " + location);
    }

    private static string? ExtractAntiforgery(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" value=\"";
        var i = html.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return null;
        i += marker.Length;
        var j = html.IndexOf('"', i);
        return j < 0 ? null : html[i..j];
    }
}
