using Bunit;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class FutureJobsRegionDisplayTests : BunitContext
{
    public FutureJobsRegionDisplayTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Region_label_uses_work_region_then_city_then_national_fallback()
    {
        var culture = Services.GetRequiredService<CultureState>();
        var prefs = new CandidatePreferences
        {
            WorkRegion = "Zaandam e.o.",
            HomeAddress = "1506 AB Zaandam"
        };
        Assert.Equal("Zaandam e.o.", FutureJobsRegionDisplay.RegionLabel(culture, prefs));

        prefs.WorkRegion = null;
        Assert.Contains("Zaandam", FutureJobsRegionDisplay.RegionLabel(culture, prefs), StringComparison.Ordinal);

        prefs.HomeAddress = null;
        Assert.Equal(UiStrings.Get("FutureJobs.Region", "nl"), FutureJobsRegionDisplay.RegionLabel(culture, prefs));
    }

    [Fact]
    public void Region_note_keeps_westland_haaglanden_disclaimer()
    {
        var culture = Services.GetRequiredService<CultureState>();
        Assert.Equal(
            UiStrings.Get("FutureJobs.RegionNote", "nl"),
            FutureJobsRegionDisplay.RegionNote(culture));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
