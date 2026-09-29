using System.Security.Claims;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Web.Components.Admin;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class AdminUsersBunitTests : TestContext
{
    public AdminUsersBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddTestAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton(new JobsyApiClient(new HttpClient { BaseAddress = new Uri("http://localhost") }));
    }

    private static ClaimsPrincipal CreateAdmin()
        => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Name, "Admin")
        ], "test"));

    [Fact]
    public void MfaResetDialog_disabled_until_reason_and_six_digits()
    {
        var cut = RenderComponent<MfaResetDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.UserId, Guid.NewGuid())
            .Add(x => x.MaskedName, "M. de V…")
            .Add(x => x.SessionCount, 2)
            .Add(x => x.RequireConfirmCode, true));

        Assert.False(cut.Instance.CanSubmit);
        Assert.True(cut.Find(".admin-mfa-reset__confirm").HasAttribute("disabled"));

        SetPrivate(cut.Instance, "_reason", "Nieuwe telefoon support");
        Assert.False(cut.Instance.CanSubmit);

        SetPrivate(cut.Instance, "_digits", new[] { "4", "8", "1", "9", "2", "0" });
        Assert.True(cut.Instance.CanSubmit);
        cut.Render();
        Assert.False(cut.Find(".admin-mfa-reset__confirm").HasAttribute("disabled"));
    }

    [Fact]
    public void MfaResetDialog_external_admin_has_no_code_field()
    {
        var cut = RenderComponent<MfaResetDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.UserId, Guid.NewGuid())
            .Add(x => x.MaskedName, "M. de V…")
            .Add(x => x.SessionCount, 1)
            .Add(x => x.RequireConfirmCode, false));

        Assert.Empty(cut.FindAll(".admin-mfa-reset__digit"));
        Assert.Contains("Microsoft/Google", cut.Markup, StringComparison.Ordinal);
        SetPrivate(cut.Instance, "_reason", "Reden lang genoeg");
        Assert.True(cut.Instance.CanSubmit);
    }

    [Fact]
    public void MfaResetDialog_paste_fills_all_digit_boxes()
    {
        var cut = RenderComponent<MfaResetDialog>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.UserId, Guid.NewGuid())
            .Add(x => x.MaskedName, "A. B…")
            .Add(x => x.RequireConfirmCode, true));

        Assert.Equal(6, cut.FindAll(".admin-mfa-reset__digit").Count);
        // Paste path: OnDigit redistributes multi-digit input into _digits.
        var onDigit = typeof(MfaResetDialog).GetMethod("OnDigit",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        onDigit.Invoke(cut.Instance, [0, new ChangeEventArgs { Value = "123456" }]);
        var digits = (string[])typeof(MfaResetDialog)
            .GetField("_digits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6" }, digits);
    }

    private static void SetPrivate(object instance, string name, object value)
    {
        var field = instance.GetType().GetField(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException(name);
        field.SetValue(instance, value);
    }

    [Fact]
    public void AdminBulkBar_appears_when_selection_positive()
    {
        var cut = RenderComponent<AdminBulkBar>(p => p
            .Add(x => x.SelectedCount, 2)
            .AddChildContent("<button type=\"button\">Actie</button>"));
        Assert.Contains("2 geselecteerd", cut.Markup, StringComparison.Ordinal);

        cut = RenderComponent<AdminBulkBar>(p => p.Add(x => x.SelectedCount, 0));
        Assert.DoesNotContain("admin-bulk-bar", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminTabs_render_role_group_labels()
    {
        var tabs = new List<AdminTabs.Tab>
        {
            new("alle", "Alle (10)"),
            new("kandidaten", "Kandidaten (8)"),
            new("beheerders", "Beheerders (2)"),
        };
        var cut = RenderComponent<AdminTabs>(p => p
            .Add(x => x.Tabs, tabs)
            .Add(x => x.ActiveKey, "alle")
            .Add(x => x.BasePath, "/admin/gebruikers"));
        Assert.Contains("Kandidaten (8)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Beheerders (2)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportAccessDialog_defaults_to_15_minutes()
    {
        Assert.Equal(15, Jobsy.Infrastructure.Services.SupportAccessService.DefaultDurationMinutes);
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "Jobsy.Web/Components/Admin/SupportAccessDialog.razor"));
        Assert.Contains("private string _duration = \"15\";", razor, StringComparison.Ordinal);
    }

    [Fact]
    public void UsersAdminSection_source_has_drawer_tabs_and_masked_pii_box()
    {
        var src = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "Jobsy.Web/Components/Admin/Sections/UsersAdminSection.razor"));
        Assert.Contains("AdminUsers.TabOverview", src, StringComparison.Ordinal);
        Assert.Contains("AdminUsers.TabSecurity", src, StringComparison.Ordinal);
        Assert.Contains("AdminUsers.PiiMaskedNote", src, StringComparison.Ordinal);
        Assert.Contains("MfaResetDialog", src, StringComparison.Ordinal);
        Assert.Contains("AdminBulkBar", src, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class FakeAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _user;
        public FakeAuthStateProvider(ClaimsPrincipal user) => _user = user;
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(_user));
    }
}

/// <summary>Soft Playwright layout checks when JOBSY_E2E_BASE_URL is set.</summary>
public class AdminUsersPlaywrightTests
{
    [Fact]
    public async Task Drawer_beveiliging_and_mobile_sheet_when_e2e_url_set()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        var root = FindRoot();
        var css = await File.ReadAllTextAsync(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/admin.css"));
        Assert.Contains("admin-drawer--sheet", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", css, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var desktop = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await desktop.GotoAsync($"{baseUrl.TrimEnd('/')}/admin/gebruikers");
        Assert.Contains("gebruikers", desktop.Url, StringComparison.OrdinalIgnoreCase);

        var mobile = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        await mobile.GotoAsync($"{baseUrl.TrimEnd('/')}/admin/gebruikers");
        Assert.Contains("gebruikers", mobile.Url, StringComparison.OrdinalIgnoreCase);
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
