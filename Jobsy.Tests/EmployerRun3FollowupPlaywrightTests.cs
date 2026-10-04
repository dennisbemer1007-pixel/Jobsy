using Bunit;
using Jobsy.Web.Components.Werkgever;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Jobsy.Web.Werkgever;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real Chromium check for the employer offline confirmation and the candidate timeline at 1280px.
/// </summary>
[Collection("PlaywrightSmoke")]
public class EmployerRun3FollowupPlaywrightTests : BunitContext
{
    public EmployerRun3FollowupPlaywrightTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new Run3Auth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton(new EmployerScopeState());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }));
    }

    [Fact]
    public async Task Browser_asks_before_taking_a_vacancy_offline_and_keeps_the_timeline_inside_the_card()
    {
        var dialog = Render<EmployerVacancyOfflineConfirm>(p => p.Add(x => x.IsOpen, true));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetViewportSizeAsync(1280, 800);

        await page.SetContentAsync(Wrap(dialog.Markup));
        var note = page.GetByTestId("wg-vac-offline-confirm");
        await Assertions.Expect(note).ToBeVisibleAsync();
        var text = await note.InnerTextAsync();
        Assert.Contains("Vacature offline halen?", text, StringComparison.Ordinal);
        Assert.Contains("Weer online zetten kost 1 token.", text, StringComparison.Ordinal);
        var cancel = page.GetByTestId("wg-vac-offline-cancel");
        var confirm = page.GetByTestId("wg-vac-offline-confirm-btn");
        await Assertions.Expect(cancel).ToHaveTextAsync("Annuleren");
        await Assertions.Expect(confirm).ToHaveTextAsync("Offline halen");
        var cancelBox = await cancel.BoundingBoxAsync();
        var confirmBox = await confirm.BoundingBoxAsync();
        Assert.NotNull(cancelBox);
        Assert.NotNull(confirmBox);
        Assert.True(cancelBox.X < confirmBox.X, "Annuleren must come before Offline halen.");

        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/applications.css"))
                  + "\n"
                  + File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/kandidaat-banen.css"));
        await page.SetContentAsync($$"""
            <!DOCTYPE html>
            <html lang="nl"><head><style>
            :root { --surface:#fff; --border:#ccc; --text:#111; --muted:#666; --brand:#0b5; --shadow:none; --space-2:0.5rem; --space-3:0.75rem; --space-4:1rem; }
            {{css}}
            </style></head>
            <body>
            <div class="application-card-list" style="width:720px">
              <article class="application-card kb-apps__card" data-testid="timeline-card">
                <div class="kb-apps__card-top"><h2>Horeca</h2></div>
                <ol class="kb-timeline kb-timeline--horizontal">
                  <li class="kb-timeline__step"><span class="kb-timeline__label">Verstuurd</span></li>
                  <li class="kb-timeline__step"><span class="kb-timeline__label">Gezien</span></li>
                  <li class="kb-timeline__step"><span class="kb-timeline__label">Gesprek</span></li>
                  <li class="kb-timeline__step"><span class="kb-timeline__label">Uitslag</span></li>
                </ol>
              </article>
            </div>
            </body></html>
            """);

        var card = await page.GetByTestId("timeline-card").BoundingBoxAsync();
        var outcome = await page.GetByText("Uitslag").BoundingBoxAsync();
        Assert.NotNull(card);
        Assert.NotNull(outcome);
        Assert.True(outcome.X + outcome.Width <= card.X + card.Width + 1);
        Assert.True(outcome.X >= card.X - 1);
    }

    private static string Wrap(string body)
        => "<!DOCTYPE html><html lang=\"nl\"><body>" + body + "</body></html>";

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class Run3Auth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Intermediary")],
                    "test"))));
    }
}
