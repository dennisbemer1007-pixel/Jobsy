using Bunit;
using Jobsy.Web.Components.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests;

public class KbAddressFieldBunitTests : BunitContext
{
    public KbAddressFieldBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Fast_input_then_stale_suggestions_do_not_change_value()
    {
        var cut = Render<KbAddressField>(ps => ps
            .Add(p => p.InputId, "test-address")
            .Add(p => p.Query, "")
            .Add(p => p.Suggestions, Array.Empty<AddressSuggestion>())
            .Add(p => p.Suggesting, false)
            .Add(p => p.ShowSuggestions, false)
            .Add(p => p.ShowLocate, false));

        const string typed = "Herenstraat 20 Wateringen";
        cut.Find("input").Focus(new FocusEventArgs());
        cut.Find("input").Input(typed);

        cut.Render(ps => ps
            .Add(p => p.Query, "Heta")
            .Add(p => p.Suggestions, new[]
            {
                new AddressSuggestion("Herenstraat 20, Wateringen", 51.99, 4.27)
            })
            .Add(p => p.ShowSuggestions, true));

        Assert.Equal(typed, cut.Find("input").GetAttribute("value"));
    }

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

public class KbFilterBarBunitTests
{
    [Fact]
    public void Discovery_markup_has_visible_search_and_chip_row()
    {
        var root = FindRepoRoot();
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("kb-filter-chips", discovery, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.SearchPlaceholder", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-filter-search--desktop", discovery, StringComparison.Ordinal);
        Assert.Contains("KbAddressField", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-start-prompt", discovery, StringComparison.Ordinal);
        Assert.Contains("KbMapStart.DefaultMaxTravelMinutes", discovery, StringComparison.Ordinal);
    }

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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
