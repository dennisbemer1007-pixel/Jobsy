using Bunit;
using Jobsy.Web.Components.Employer;
using Jobsy.Web.Components.Werkgever.Insights;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Jobsy.Tests;

/// <summary>Code-health 01 §1: RZ10012 components must render, not as raw HTML tags.</summary>
public class CodeHealth01Rz10012Tests : TestContext
{
    public CodeHealth01Rz10012Tests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public void Insights_components_render_css_hooks_not_raw_tags()
    {
        var story = RenderComponent<InsightsStoryCard>(p => p
            .Add(c => c.Index, 0)
            .Add(c => c.Current, 0)
            .Add(c => c.Title, "Story")
            .Add(c => c.TestId, "insights-story-card"));
        AssertNoRawTag(story.Markup, "insightsstorycard");
        Assert.Contains("insights-story-card", story.Markup, StringComparison.Ordinal);

        var locked = RenderComponent<WgLockedCard>(p => p
            .Add(c => c.Title, "Locked")
            .Add(c => c.TestId, "wg-locked"));
        AssertNoRawTag(locked.Markup, "wglockedcard");
        Assert.Contains("wg-locked-card", locked.Markup, StringComparison.Ordinal);

        var bars = RenderComponent<InsightsDistributionBars>();
        AssertNoRawTag(bars.Markup, "insightsdistributionbars");

        var wrapper = RenderComponent<InsightsLockedBlock>(p => p
            .Add(c => c.TestId, "insights-locked-block"));
        AssertNoRawTag(wrapper.Markup, "wglockedcard");
        Assert.Contains("wg-locked-card", wrapper.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Imports_include_employer_and_werkgever_insights_namespaces()
    {
        var root = FindRepoRoot();
        var imports = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "_Imports.razor"));
        Assert.Contains("@using Jobsy.Web.Components.Employer", imports, StringComparison.Ordinal);
        Assert.Contains("@using Jobsy.Web.Components.Werkgever.Insights", imports, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(imports, @"@using Jobsy\.Web\.Features\b").Count);

        var companyDetails = File.ReadAllText(Path.Combine(
            root, "Jobsy.Web", "Components", "Werkgever", "Sections", "CompanyDetailsSection.razor"));
        Assert.Contains("<RaamflyerTools", companyDetails, StringComparison.Ordinal);
        Assert.DoesNotContain("&lt;RaamflyerTools", companyDetails, StringComparison.Ordinal);
    }

    [Fact]
    public void Web_project_treats_RZ10012_as_error()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Jobsy.Web.csproj"));
        Assert.Contains("RZ10012", csproj, StringComparison.Ordinal);
        Assert.Contains("WarningsAsErrors", csproj, StringComparison.Ordinal);
    }

    private static void AssertNoRawTag(string markup, string tag)
        => Assert.DoesNotContain($"<{tag}", markup, StringComparison.OrdinalIgnoreCase);

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

    private sealed class FakeAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
