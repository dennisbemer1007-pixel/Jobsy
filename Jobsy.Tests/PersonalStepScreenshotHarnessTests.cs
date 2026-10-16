using Bunit;
using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>Exports PersonalStep markup for Playwright viewport screenshots (dev/CI tooling).</summary>
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
        Services.AddSingleton<IGeocodingClient, NoopGeocoder>();
    }

    [Fact]
    public async Task Export_current_personal_step_markup()
    {
        var dir = Environment.GetEnvironmentVariable("PERSONAL_STEP_HTML_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "personal-step.html"), await RenderStepHtmlAsync(openSuggestions: false));
        File.WriteAllText(Path.Combine(dir, "personal-step-suggestions.html"), await RenderStepHtmlAsync(openSuggestions: true));
    }

    internal static async Task<string> RenderStepHtmlAsync(bool openSuggestions)
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

        var input = cut.FindAll("input").FirstOrDefault(i =>
                         i.GetAttribute("id") == "onboarding-home-address"
                         || i.GetAttribute("autocomplete") == "postal-code")
                     ?? cut.Find("input");
        input.Input(openSuggestions ? "2671 GB" : "2671 GB Naaldwijk");
        await cut.InvokeAsync(async () =>
        {
            await Task.Delay(openSuggestions ? 400 : 50);
        });

        if (openSuggestions && draft.Suggestions.Count == 0)
        {
            draft.Suggestions.Add(new AddressSuggestion("2671 AA Naaldwijk", 51.99, 4.21));
            draft.Suggestions.Add(new AddressSuggestion("2671 AB Naaldwijk", 51.98, 4.22));
            cut.Render(ps => ps
                .Add(p => p.Draft, draft)
                .Add(p => p.ShowValidation, false)
                .Add(p => p.UseDiscoveryPostcodeHint, true));
        }

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web", "wwwroot"));
        var css = new[]
        {
            $"css/critical.css?v=1",
            $"css/app.min.css?v=1",
            $"css/features/onboarding-wizard.css?v=20261016-06",
        };

        var links = string.Join("\n", css.Select(h =>
            $"""<link rel="stylesheet" href="file://{root}/{h.Split('?')[0]}" />"""));

        return $$"""
            <!DOCTYPE html>
            <html lang="nl">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              {{links}}
              <style>body{margin:0;background:#f5f2ee;font-family:system-ui,sans-serif}.ob-wizard{padding:12px}</style>
            </head>
            <body><div class="ob-wizard">{{cut.Find(".ob-fields").InnerHtml}}</div></body>
            </html>
            """;
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class NoopGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
        {
            if ((query ?? "").Trim().Length < 3)
            {
                return Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);
            }

            return Task.FromResult<IReadOnlyList<AddressSuggestion>>(new[]
            {
                new AddressSuggestion("2671 AA Naaldwijk", 51.99, 4.21),
                new AddressSuggestion("2671 AB Naaldwijk", 51.98, 4.22),
                new AddressSuggestion("Herenstraat 20, 2671 AA Naaldwijk", 51.991, 4.211),
            });
        }

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("2671 GB Naaldwijk");
    }
}
