using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminDebouncedSearchTests
{
    [Fact]
    public async Task Latest_query_wins_and_cancelled_work_does_not_apply()
    {
        var applied = new List<string>();
        var debounce = new DebouncedAction();
        var first = debounce.RunAsync(40, async ct =>
        {
            await Task.Delay(30, ct);
            applied.Add("old");
        });
        var second = debounce.RunAsync(40, ct =>
        {
            applied.Add("new");
            return Task.CompletedTask;
        });

        await Task.WhenAll(first, second);
        Assert.Equal(["new"], applied);
    }
}

public class AdminTabAndPagerBunitTests : BunitContext
{
    public AdminTabAndPagerBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new AuthenticationStateProviderStub()));
        Services.AddAuthorizationCore();
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost") }));
    }

    [Fact]
    public void Tabs_switch_when_the_query_changes_without_a_reload()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/admin/gebruikers");
        var cut = Render<TabHost>();

        Assert.Equal("alle", cut.Find("#panel").TextContent);
        Assert.Equal("true", cut.FindAll("a.admin-tabs__tab")[0].GetAttribute("aria-selected"));

        cut.FindAll("a.admin-tabs__tab")[1].Click();

        Assert.Contains("tab=geblokkeerd", nav.Uri, StringComparison.Ordinal);
        Assert.Equal("geblokkeerd", cut.Find("#panel").TextContent);
        Assert.Equal("false", cut.FindAll("a.admin-tabs__tab")[0].GetAttribute("aria-selected"));
        Assert.Equal("true", cut.FindAll("a.admin-tabs__tab")[1].GetAttribute("aria-selected"));

        nav.NavigateTo("/admin/gebruikers");
        cut.WaitForAssertion(() => Assert.Equal("alle", cut.Find("#panel").TextContent));
    }

    [Fact]
    public void Audit_pager_next_is_enabled_when_fifty_rows_are_paged_by_twenty_five()
    {
        var cut = Render<AdminPager>(p => p
            .Add(x => x.Page, 1)
            .Add(x => x.PageSize, 25)
            .Add(x => x.TotalCount, 50)
            .Add(x => x.TotalPages, 2));

        var buttons = cut.FindAll("button");
        Assert.True(buttons[0].HasAttribute("disabled"));
        Assert.False(buttons[^1].HasAttribute("disabled"));
        Assert.Contains("50", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class AuthenticationStateProviderStub : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new System.Security.Claims.ClaimsPrincipal()));
    }

    private sealed class TabHost : ComponentBase
    {
        private string _key = "alle";

        private static readonly IReadOnlyList<AdminTabs.Tab> Tabs =
        [
            new("alle", "Alle"),
            new("geblokkeerd", "Geblokkeerd")
        ];

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<AdminTabs>(0);
            builder.AddAttribute(1, "Tabs", Tabs);
            builder.AddAttribute(2, "ActiveKey", _key);
            builder.AddAttribute(3, "ActiveKeyChanged", EventCallback.Factory.Create<string>(this, value => _key = value));
            builder.AddAttribute(4, "BasePath", "/admin/gebruikers");
            builder.CloseComponent();
            builder.OpenElement(10, "p");
            builder.AddAttribute(11, "id", "panel");
            builder.AddContent(12, _key);
            builder.CloseElement();
        }
    }
}

public class AdminCspSourceTests
{
    [Fact]
    public void Web_has_no_eval_or_inline_handlers_or_unnonced_inline_scripts()
    {
        var root = Path.Combine(FindRepoRoot(), "Jobsy.Web");
        var failures = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var name = Path.GetFileName(file);
            if (name is not ("app-core.js" or "app-extras.js")
                && !file.EndsWith(".cs", StringComparison.Ordinal)
                && !file.EndsWith(".razor", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (text.Contains("InvokeVoidAsync(\"eval\"", StringComparison.Ordinal)
                || text.Contains("InvokeAsync(\"eval\"", StringComparison.Ordinal)
                || text.Contains("InvokeAsync<bool>(\"eval\"", StringComparison.Ordinal)
                || text.Contains("InvokeAsync<string>(\"eval\"", StringComparison.Ordinal)
                || text.Contains("InvokeAsync<string?>(\"eval\"", StringComparison.Ordinal))
            {
                failures.Add(file + " calls eval");
            }

            if (!file.EndsWith(".razor", StringComparison.Ordinal))
            {
                continue;
            }

            if (Regex.IsMatch(text, @"\son[a-zA-Z]+\s*=\s*[""']"))
            {
                failures.Add(file + " has an inline on*= handler");
            }

            if (text.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add(file + " has a javascript: url");
            }

            foreach (Match script in Regex.Matches(text, @"<script\b([^>]*)>", RegexOptions.IgnoreCase))
            {
                var attrs = script.Groups[1].Value;
                if (attrs.Contains("type=\"application/ld+json\"", StringComparison.OrdinalIgnoreCase)
                    || attrs.Contains("type=\"application/json\"", StringComparison.OrdinalIgnoreCase)
                    || attrs.Contains("nonce=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (attrs.Contains("src=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                failures.Add(file + " has an inline script without a nonce");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
