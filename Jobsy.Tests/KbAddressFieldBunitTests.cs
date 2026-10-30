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

    [Fact]
    public void Clear_button_hidden_when_query_empty_even_if_origin_set()
    {
        var cut = Render<KbAddressField>(ps => ps
            .Add(p => p.InputId, "test-address-clear")
            .Add(p => p.Query, "")
            .Add(p => p.Suggestions, Array.Empty<AddressSuggestion>())
            .Add(p => p.OriginSet, true)
            .Add(p => p.ShowLocate, true));

        Assert.Empty(cut.FindAll("button.kb-address__clear"));
    }

    [Fact]
    public void Clear_button_visible_when_query_has_text()
    {
        var cut = Render<KbAddressField>(ps => ps
            .Add(p => p.InputId, "test-address-clear2")
            .Add(p => p.Query, "2671 GB Naaldwijk")
            .Add(p => p.Suggestions, Array.Empty<AddressSuggestion>())
            .Add(p => p.OriginSet, true)
            .Add(p => p.ShowLocate, true));

        Assert.NotEmpty(cut.FindAll("button.kb-address__clear"));
    }

    [Fact]
    public void Renders_geo_button_and_fld_field_structure()
    {
        var cut = Render<KbAddressField>(ps => ps
            .Add(p => p.InputId, "test-address-geo")
            .Add(p => p.Query, "")
            .Add(p => p.Suggestions, Array.Empty<AddressSuggestion>())
            .Add(p => p.Suggesting, false)
            .Add(p => p.ShowSuggestions, false)
            .Add(p => p.ShowLocate, true)
            .Add(p => p.OriginSet, true));

        Assert.NotNull(cut.Find(".kb-address__field input"));
        Assert.NotNull(cut.Find("button.kb-address__geo"));
        Assert.Empty(cut.FindAll("button.kb-address__clear"));
        Assert.Equal("combobox", cut.Find("input").GetAttribute("role"));
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
        Assert.Contains("kb-filter-bar", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-filter-search--bar", discovery, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.KeywordPlaceholder", discovery, StringComparison.Ordinal);
        Assert.Contains("kb-panel-head__search-row", discovery, StringComparison.Ordinal);
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
