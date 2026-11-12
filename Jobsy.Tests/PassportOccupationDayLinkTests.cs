using Bunit;
using Jobsy.Core.Careers;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Tests.Infrastructure;
using Jobsy.Web.Localization;
using Jobsy.Web.Passport;
using Jobsy.Web.Scholen;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;
using Xunit;

namespace Jobsy.Tests;

public sealed class PassportOccupationDayLinkTests : BunitContext
{
    private const string CookId = "90f75f67-495d-49fa-ab57-2f320e251d7e";

    public PassportOccupationDayLinkTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Passport_ssr_helpers_emit_day_button_only_with_esco()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var sb = new System.Text.StringBuilder();
        OccupationDayPassportSsr.AppendCandidateButton(sb, culture, CookId, "kok");
        var html = sb.ToString();
        Assert.Contains($"/candidate/beroep/{CookId}/dag?from=paspoort", html, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"occupation-day-passport-link\"", html, StringComparison.Ordinal);
        Assert.Contains("→", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Pupil_passport_ssr_renders_day_button_when_dto_has_escoid()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var model = new PupilG78PassportWebDto(
            Enabled: true,
            OccupationCards:
            [
                new("Test", "Kok", "Blurb", [], null, null, CookId),
            ]);
        var html = PupilG78PassportSsrHtml.Render(model, culture);
        Assert.Contains($"/leerling/beroep/{CookId}/dag?from=paspoort", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Future_jobs_ssr_includes_day_link_when_id_is_stored()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var list = new FutureJobsList(
            false,
            [new FutureJobRow(CookId, "kok", 80, 3, "groot", 12, null, [])],
            OccupationOutlook.Shared.Peildatum);
        var sb = new System.Text.StringBuilder();
        var stored = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { CookId };
        PassportSsrFutureJobsHtml.Append(sb, culture, list, "Nederland", "note", showCheck: false, stored);
        Assert.Contains($"/candidate/beroep/{CookId}/dag?from=paspoort", sb.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Occupation_day_available_route_is_declared_for_candidate_api()
    {
        var root = RepoRoot.Find();
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/OccupationDayInLifeController.cs"));
        Assert.Contains("[HttpGet(\"available\")]", controller, StringComparison.Ordinal);
        var pupil = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/PupilController.cs"));
        Assert.Contains("occupation-day-in-life/{escoId:guid}", pupil, StringComparison.Ordinal);
        var journey = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilLeerlingJourneyEndpoints.cs"));
        Assert.Contains("/leerling/beroep/{escoId:guid}/dag", journey, StringComparison.Ordinal);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
