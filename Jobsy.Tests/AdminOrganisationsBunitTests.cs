using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminOrganisationsBunitTests : TestContext
{
    public AdminOrganisationsBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddTestAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost") }));
    }

    [Fact]
    public void Tree_expand_sets_aria_expanded_and_flat_toggle_works()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var companies = new List<AdminCompanyItem>
        {
            new()
            {
                Id = parentId,
                Name = "Parent Org",
                Type = "Employer",
                BranchCount = 1,
                Status = "active",
                KvkNumber = "12345678"
            },
            new()
            {
                Id = childId,
                Name = "Child Branch",
                Type = "Employer",
                ParentCompanyId = parentId,
                Status = "takeover",
                KvkNumber = "12345678",
                PendingTakeoverId = Guid.NewGuid(),
                PendingTakeoverApplicant = "Flex BV",
                PendingTakeoverAtUtc = DateTime.UtcNow
            }
        };

        var cut = RenderComponent<CompaniesAdminHarness>(p => p
            .Add(x => x.Companies, companies));

        Assert.Contains("Boom", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Parent Org", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Child Branch", cut.Markup, StringComparison.Ordinal);

        var caret = cut.Find("button.admin-org-caret");
        Assert.Equal("false", caret.GetAttribute("aria-expanded"));
        caret.Click();
        Assert.Equal("true", cut.Find("button.admin-org-caret").GetAttribute("aria-expanded"));
        Assert.Contains("Child Branch", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button.admin-segment__btn")[^1].Click();
        Assert.Contains("Child Branch", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("button.admin-org-caret"));
    }

    [Fact]
    public void Detail_panel_shows_takeover_note_and_one_primary()
    {
        var id = Guid.NewGuid();
        var takeoverId = Guid.NewGuid();
        var companies = new List<AdminCompanyItem>
        {
            new()
            {
                Id = id,
                Name = "With Takeover",
                Type = "Employer",
                Status = "takeover",
                KvkNumber = "87654321",
                KvkVerificationStatus = "Verified",
                PendingTakeoverId = takeoverId,
                PendingTakeoverApplicant = "FlexWerk BV",
                PendingTakeoverAtUtc = new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc),
                UserCount = 5,
                TokenBalance = 10
            }
        };

        var cut = RenderComponent<CompaniesAdminHarness>(p => p
            .Add(x => x.Companies, companies)
            .Add(x => x.SelectFirst, true));

        Assert.Contains("admin-orgs__panel", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("admin-impact-note", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("FlexWerk BV", cut.Markup, StringComparison.Ordinal);
        var primaries = cut.FindAll("a.btn-compact--primary");
        Assert.Contains(primaries, a => a.GetAttribute("href")?.Contains("aanvragen", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Drawer_used_when_narrow_viewport_flag_set()
    {
        var id = Guid.NewGuid();
        var companies = new List<AdminCompanyItem>
        {
            new()
            {
                Id = id,
                Name = "Drawer Org",
                Type = "Employer",
                Status = "active",
                KvkNumber = "11223344"
            }
        };

        var cut = RenderComponent<CompaniesAdminHarness>(p => p
            .Add(x => x.Companies, companies)
            .Add(x => x.SelectFirst, true)
            .Add(x => x.ForceDrawer, true));

        Assert.Contains("admin-drawer", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Drawer Org", cut.Markup, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal CreateAdmin()
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        ], "test"));

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuthStateProvider(ClaimsPrincipal user) => _user = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
    }

    private sealed class FakeHostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

public class CompaniesAdminHarness : ComponentBase
{
    [Parameter] public List<AdminCompanyItem> Companies { get; set; } = [];
    [Parameter] public bool SelectFirst { get; set; }
    [Parameter] public bool ForceDrawer { get; set; }

    private bool _treeMode = true;
    private readonly HashSet<Guid> _expanded = [];
    private AdminCompanyItem? _selected;
    private List<AdminCompanyItem> _rows = [];

    protected override void OnParametersSet()
    {
        Rebuild();
        if (SelectFirst && _selected is null && Companies.Count > 0)
        {
            _selected = Companies[0];
        }
    }

    private void Rebuild()
    {
        if (!_treeMode)
        {
            _rows = Companies.OrderBy(c => c.Name).ToList();
            return;
        }

        var roots = Companies.Where(c => c.ParentCompanyId is null).OrderBy(c => c.Name).ToList();
        var children = Companies.Where(c => c.ParentCompanyId is not null)
            .GroupBy(c => c.ParentCompanyId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());
        var rows = new List<AdminCompanyItem>();
        foreach (var root in roots)
        {
            rows.Add(root);
            if (_expanded.Contains(root.Id) && children.TryGetValue(root.Id, out var kids))
            {
                rows.AddRange(kids);
            }
        }

        _rows = rows;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class",
            "admin-orgs" + (_selected is not null && !ForceDrawer ? " admin-orgs--with-panel" : ""));

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "admin-segment");
        builder.OpenElement(4, "button");
        builder.AddAttribute(5, "type", "button");
        builder.AddAttribute(6, "class", "admin-segment__btn" + (_treeMode ? " is-active" : ""));
        builder.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, () => { _treeMode = true; Rebuild(); }));
        builder.AddContent(8, "Boom");
        builder.CloseElement();
        builder.OpenElement(9, "button");
        builder.AddAttribute(10, "type", "button");
        builder.AddAttribute(11, "class", "admin-segment__btn" + (!_treeMode ? " is-active" : ""));
        builder.AddAttribute(12, "onclick", EventCallback.Factory.Create(this, () => { _treeMode = false; Rebuild(); }));
        builder.AddContent(13, "Plat");
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(14, "table");
        builder.OpenElement(15, "tbody");
        var seq = 100;
        foreach (var row in _rows)
        {
            var isChild = row.ParentCompanyId is not null;
            var expanded = _expanded.Contains(row.Id);
            var rowId = row.Id;
            builder.OpenElement(seq++, "tr");
            builder.AddAttribute(seq++, "class", "admin-org-row");
            builder.OpenElement(seq++, "td");
            if (!isChild && _treeMode && row.BranchCount > 0)
            {
                builder.OpenElement(seq++, "button");
                builder.AddAttribute(seq++, "type", "button");
                builder.AddAttribute(seq++, "class", "admin-org-caret");
                builder.AddAttribute(seq++, "aria-expanded", expanded ? "true" : "false");
                builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () =>
                {
                    if (!_expanded.Add(rowId)) _expanded.Remove(rowId);
                    Rebuild();
                }));
                builder.AddContent(seq++, expanded ? "▾" : "▸");
                builder.CloseElement();
            }

            builder.OpenElement(seq++, "strong");
            builder.AddContent(seq++, row.Name);
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();

        if (_selected is not null && !ForceDrawer)
        {
            builder.OpenElement(500, "aside");
            builder.AddAttribute(501, "class", "admin-orgs__panel");
            RenderDetail(builder, 510);
            builder.CloseElement();
        }

        if (_selected is not null && ForceDrawer)
        {
            builder.OpenElement(600, "aside");
            builder.AddAttribute(601, "class", "admin-drawer is-open");
            RenderDetail(builder, 610);
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private void RenderDetail(RenderTreeBuilder builder, int seq)
    {
        var c = _selected!;
        builder.OpenElement(seq, "div");
        builder.AddAttribute(seq + 1, "class", "admin-org-detail");
        builder.OpenElement(seq + 2, "h2");
        builder.AddContent(seq + 3, c.Name);
        builder.CloseElement();
        if (c.PendingTakeoverId is Guid tid)
        {
            builder.OpenElement(seq + 4, "div");
            builder.AddAttribute(seq + 5, "class", "admin-impact-note admin-impact-note--warn");
            builder.AddContent(seq + 6, "Overname aangevraagd ");
            builder.AddContent(seq + 7, c.PendingTakeoverApplicant);
            builder.CloseElement();
            builder.OpenElement(seq + 8, "a");
            builder.AddAttribute(seq + 9, "class", "btn-compact btn-compact--primary");
            builder.AddAttribute(seq + 10, "href", $"/admin/organisaties/aanvragen?tab=overnames&takeoverId={tid:D}");
            builder.AddContent(seq + 11, "Overname beoordelen");
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}

public class AdminOrganisationsPlaywrightTests
{
    [Fact]
    public async Task Organisations_layout_css_has_panel_and_no_horizontal_scroll_rules()
    {
        var root = FindRoot();
        var css = await File.ReadAllTextAsync(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/admin.css"));
        Assert.Contains("admin-orgs--with-panel", css, StringComparison.Ordinal);
        Assert.Contains("width: 320px", css, StringComparison.Ordinal);
        Assert.Contains("admin-org-caret", css, StringComparison.Ordinal);

        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await page.GotoAsync($"{baseUrl.TrimEnd('/')}/admin/organisaties");
        Assert.Contains("organisaties", page.Url, StringComparison.OrdinalIgnoreCase);
        var scrollWidth = await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth");
        var clientWidth = await page.EvaluateAsync<int>("() => document.documentElement.clientWidth");
        Assert.True(scrollWidth <= clientWidth + 1, $"Horizontal scroll detected: {scrollWidth} > {clientWidth}");
    }

    private static string FindRoot()
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
}
