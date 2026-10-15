using Bunit;
using Jobsy.Web.Components;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Components.Registration;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

/// <summary>Writes static HTML previews using real Blazor mascot markup + production CSS (for screenshot artifacts).</summary>
public class MascotAppPreviewArtifactTests : BunitContext
{
    public MascotAppPreviewArtifactTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider());
        Services.AddSingleton<CoachTipBus>();
        Services.AddSingleton<AssistantChatHost>();
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Write_app_mascot_preview_pages()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("WRITE_MASCOT_OVERVIEW"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var root = TestRepo.FindRoot();
        var outDir = Environment.GetEnvironmentVariable("CURSOR_ARTIFACTS_DIR") ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(outDir);

        var journeyDesktop = Render<JourneyLobster>(p => p
            .Add(c => c.CoachPose, MascotPose.Uitleggen)
            .Add(c => c.PlatesShed, 2)
            .Add(c => c.Size, 120)
            .Add(c => c.Label, "Lobsy"));

        var journeyMobile = Render<JourneyLobster>(p => p
            .Add(c => c.CoachPose, MascotPose.Uitleggen)
            .Add(c => c.PlatesShed, 2)
            .Add(c => c.Size, 72)
            .Add(c => c.Mobile, true)
            .Add(c => c.Label, "Lobsy"));

        var coach = Render<LobsyCoach>(p => p
            .Add(c => c.AskLabel, "Lobsy, je coach")
            .Add(c => c.CloseLabel, "Sluiten"));

        var onboarding = Render<WaWizardChrome>(p => p
            .Add(c => c.Speech, "Leuk dat je meedoet! Samen zetten we je profiel neer.")
            .Add(c => c.CurrentStep, 1)
            .Add(c => c.Pose, WaMascot.MascotPose.Waving));

        var cssBase = Path.Combine(root, "Jobsy.Web/wwwroot/css");
        var html = $$"""
<!DOCTYPE html>
<html lang="nl">
<head>
<meta charset="utf-8"/>
<meta name="viewport" content="width=device-width, initial-scale=1"/>
<title>Lobsy — ontdekkingsreis + coach (live markup)</title>
<link rel="stylesheet" href="file://{{cssBase}}/critical.css"/>
<link rel="stylesheet" href="file://{{cssBase}}/app.min.css"/>
<link rel="stylesheet" href="file://{{cssBase}}/features/ontdekkingsreis.css"/>
<link rel="stylesheet" href="file://{{cssBase}}/features/onboarding-wizard.css"/>
<style>
  body { margin: 0; background: var(--bg); }
  .shot { min-height: 100vh; position: relative; }
  .shot--desktop { width: 1280px; min-height: 800px; }
  .shot--mobile { width: 390px; min-height: 844px; }
  .app-shell { min-height: inherit; position: relative; }
  .app-main { padding: 16px; }
  .preview-label { font: 600 12px system-ui; color: var(--muted); padding: 8px 12px; }
</style>
</head>
<body>
<section class="shot shot--desktop" id="desktop">
  <p class="preview-label">Desktop 1280×800 — ontdekkingsreis vraag + coach + onboarding</p>
  <div class="app-shell has-bottom-nav">
    <main class="app-main">
      <div class="wa-layout wa-layout--split" style="margin-bottom:24px">
        {{onboarding.Markup}}
        <div class="wa-main card"><h1>Account maken</h1><p>Stap 1 — je gegevens</p></div>
      </div>
      <div class="journey">
        <div class="journey-stage">
          <section class="journey-step card">
            <p class="journey-step__eyebrow">Aan de kust · Stap 3</p>
            <h1>Woonplaats</h1>
            <p class="journey-step__lead">Waar woon je? Dan kunnen we reistijd goed inschatten.</p>
          </section>
          <div class="journey-lz journey-only-desktop">
            <div class="journey-bubble"><p>Tip: je postcode hoeft niet perfect — bijna goed is genoeg.</p></div>
            {{journeyDesktop.Markup}}
          </div>
        </div>
      </div>
    </main>
    {{coach.Markup}}
  </div>
</section>
<section class="shot shot--mobile" id="mobile">
  <p class="preview-label">Mobiel 390×844 — zelfde markup</p>
  <div class="app-shell has-bottom-nav">
    <main class="app-main">
      <div class="journey">
        <div class="journey-coach journey-coach--mobile journey-only-mobile">
          <div class="journey-bubble journey-coach__bubble"><p>Tip voor onderweg.</p></div>
          {{journeyMobile.Markup}}
        </div>
        <section class="journey-step card"><h1>Woonplaats</h1></section>
      </div>
    </main>
    {{coach.Markup}}
  </div>
</section>
</body>
</html>
""";

        var path = Path.Combine(outDir, "mascot-app-live-preview.html");
        File.WriteAllText(path, html);
        Assert.True(File.Exists(path));
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
