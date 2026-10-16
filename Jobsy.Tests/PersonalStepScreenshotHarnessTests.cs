using Bunit;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>Exports styled PersonalStep HTML for Playwright screenshots (not a production code path).</summary>
public class PersonalStepScreenshotHarnessTests : BunitContext
{
    public PersonalStepScreenshotHarnessTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IGeocodingClient, FakeGeocoder>();
    }

    [Fact]
    public async Task Export_current_personal_step_markup()
    {
        var dir = Environment.GetEnvironmentVariable("PERSONAL_STEP_HTML_DIR");
        var staticBase = Environment.GetEnvironmentVariable("PERSONAL_STEP_STATIC_BASE");
        if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(staticBase))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "personal-step.html"),
            await RenderDocumentAsync(openSuggestions: false, staticBase));
        File.WriteAllText(
            Path.Combine(dir, "personal-step-suggestions.html"),
            await RenderDocumentAsync(openSuggestions: true, staticBase));
    }

    internal static async Task<string> RenderDocumentAsync(bool openSuggestions, string staticBase)
    {
        using var ctx = new PersonalStepScreenshotHarnessTests();
        var draft = new OnboardingProfileDraft
        {
            FirstName = "Alex",
            LastName = "Jansen",
        };

        var cut = ctx.Render<PersonalStep>(ps => ps
            .Add(p => p.Draft, draft)
            .Add(p => p.ShowValidation, false)
            .Add(p => p.UseDiscoveryPostcodeHint, true));

        var addressInput = cut.Find("#onboarding-home-address");
        var query = openSuggestions ? "Kerkstraat Naaldwijk" : "2671 GB Naaldwijk";
        addressInput.Input(query);
        await cut.InvokeAsync(async () => await Task.Delay(openSuggestions ? 700 : 100));

        if (openSuggestions)
        {
            cut.Find(".address-suggestions");
        }

        var fieldsHtml = cut.Find(".ob-fields").InnerHtml;
        return PersonalStepScreenshotExporter.BuildDocument(fieldsHtml, staticBase);
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class FakeGeocoder : IGeocodingClient
    {
        public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            if ((query ?? "").Trim().Length < 3)
            {
                return [];
            }

            return
            [
                new AddressSuggestion("Kerkstraat, 2671 Naaldwijk", 51.994, 4.205),
                new AddressSuggestion("Kerkstraat 20, 2671 AA Naaldwijk", 51.991, 4.211),
                new AddressSuggestion("2671 AA Naaldwijk", 51.99, 4.21),
            ];
        }

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("2671 GB Naaldwijk");
    }
}
