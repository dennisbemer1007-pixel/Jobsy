using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Jobsy.Api.Models;
using Jobsy.Core.Hosting;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Admin;
using Jobsy.Web.Components.Admin.Sections;
using Jobsy.Web.Components.Admin.Ui;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Jobsy.Tests;

public class PlatformSettingsCatalogTests
{
    [Fact]
    public void Keys_unique_groups_ordered_one_policy_no_absent_flags()
    {
        var entries = PlatformSettingsCatalog.Entries;
        Assert.Equal(entries.Count, entries.Select(e => e.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(entries, e => e.Kind == PlatformSettingKind.Policy && e.Key == "MfaPolicy");

        var groupOrder = PlatformSettingsCatalog.Groups.Select(g => g.Key).ToList();
        var seen = new List<string>();
        foreach (var e in entries)
        {
            if (seen.Count == 0 || seen[^1] != e.Group)
            {
                seen.Add(e.Group);
            }
        }

        Assert.Equal(seen, seen.OrderBy(g => groupOrder.IndexOf(g)).ToList());
        // EmployersEnabled / CandidatePassportEnabled are present on PlatformFeatureSettings —
        // catalog entries must exist (not slot comments only).
        Assert.True(PlatformSettingsCatalog.FieldExists("EmployersEnabled"));
        Assert.True(PlatformSettingsCatalog.FieldExists("CandidatePassportEnabled"));
        Assert.Contains(entries, e => e.Key == "EmployersEnabled");
        Assert.Contains(entries, e => e.Key == "CandidatePassportEnabled");

        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Jobsy.Web", "Admin", "PlatformSettingsCatalog.cs"));
        Assert.True(File.Exists(sourcePath), sourcePath);
        var source = File.ReadAllText(sourcePath);
        Assert.Contains("EmployersEnabled", source, StringComparison.Ordinal);
        Assert.Contains("CandidatePassportEnabled", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_write_roundtrips_through_platform_feature_update()
    {
        await using var db = CreateDb();
        var sut = CreateFeatures(db);
        var snap = await sut.GetAsync();

        foreach (var entry in PlatformSettingsCatalog.Entries.Where(e => e.Kind != PlatformSettingKind.Policy))
        {
            var original = entry.Read(snap);
            object? flipped = entry.Kind switch
            {
                PlatformSettingKind.Bool => !(original is true),
                PlatformSettingKind.Int => (int)original! == (entry.Min ?? 5)
                    ? (entry.Max ?? 100)
                    : (entry.Min ?? 5),
                PlatformSettingKind.Date => original is DateOnly d
                    ? d.AddDays(1)
                    : new DateOnly(2026, 12, 1),
                PlatformSettingKind.Text => "http://localhost:5201",
                _ => original
            };

            snap = await sut.UpdateAsync(entry.Write(flipped));
            Assert.Equal(Format(flipped), Format(entry.Read(snap)));
            snap = await sut.UpdateAsync(entry.Write(original));
        }
    }

    [Fact]
    public void Dashboard_rows_use_show_on_dashboard_entries()
    {
        var snap = new PlatformFeatureSnapshot(true, true, "http://localhost:5201", DateTime.UtcNow);
        var rows = PlatformSettingsCatalog.DashboardRows(snap);
        Assert.Contains(rows, r => r.Key == "VacancyContentModerationEnabled" && r.IsOn);
        Assert.Contains(rows, r => r.Key == "MfaPolicy" && r.IsPolicyReadonly);
        Assert.DoesNotContain(rows, r => r.Key == "AuthenticatorEnabled");
    }

    [Fact]
    public void Passport_descriptor_reads_on_for_default_row_and_write_off_works()
    {
        var entry = PlatformSettingsCatalog.Entries.Single(e => e.Key == "CandidatePassportEnabled");
        var defaults = new Jobsy.Core.Entities.PlatformFeatureSettings();
        var snap = new PlatformFeatureSnapshot(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: true,
            PublicWebBaseUrl: "http://localhost:5201",
            UpdatedAtUtc: DateTime.UtcNow,
            EmployersEnabled: true,
            CandidatePassportEnabled: defaults.CandidatePassportEnabled);
        Assert.True(defaults.CandidatePassportEnabled);
        Assert.True(entry.Read(snap) is true);

        var offUpdate = entry.Write(false);
        Assert.False(offUpdate.CandidatePassportEnabled);
    }

    private static string Format(object? v) => v switch
    {
        DateOnly d => d.ToString("O"),
        null => "",
        _ => v.ToString() ?? ""
    };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateFeatures(JobsyDbContext db)
        => new(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());
}

public class PlatformFeaturesEnvLockApiTests
{
    [Fact]
    public void Activation_links_setting_removed_from_catalog()
    {
        Assert.DoesNotContain(
            PlatformSettingsCatalog.Entries,
            d => string.Equals(d.Key, "ExposeRegistrationActivationLinks", StringComparison.Ordinal));
    }
}

public class PlatformSettingsEditorBunitTests : BunitContext
{
    private readonly CapturingHandler _handler = new();

    public PlatformSettingsEditorBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        Services.AddAuthorizationCore();
        this.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        Services.AddSingleton(new DeploymentEnvironmentLabel(DeploymentEnvironment.Lokaal));
        var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddSingleton(new JobsyApiClient(http));
    }

    [Fact]
    public async Task Two_changes_show_save_bar_and_one_partial_put()
    {
        _handler.Features = new PlatformFeatureItem
        {
            VacancyContentModerationEnabled = true,
            AuthenticatorEnabled = false,
            SessionInactivityTimeoutMinutes = 30,
            PublicWebBaseUrl = "http://localhost:5201",
            UpdatedAtUtc = DateTime.UtcNow
        };

        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));

        cut.WaitForElement(".admin-settings-group");
        cut.Instance.SetDraftForTests("VacancyContentModerationEnabled", false);
        cut.Instance.SetDraftForTests("AuthenticatorEnabled", true);
        cut.Render();

        Assert.Equal(2, cut.Instance.DirtyCount);
        Assert.Contains("2 wijziging", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-save-bar", cut.Markup, StringComparison.Ordinal);

        await cut.Instance.SaveForTestsAsync();
        Assert.Equal(1, _handler.PutCount);
        Assert.NotNull(_handler.LastPatch);
        Assert.False(_handler.LastPatch!.VacancyContentModerationEnabled);
        Assert.True(_handler.LastPatch.AuthenticatorEnabled);
        Assert.Null(_handler.LastPatch.SessionInactivityTimeoutMinutes);
    }

    [Fact]
    public async Task Toggle_off_confirm_then_save_sends_one_patch()
    {
        _handler.Features = new PlatformFeatureItem
        {
            SchoolsEnabled = true,
            VacancyContentModerationEnabled = true,
            PublicWebBaseUrl = "http://localhost:5201",
            UpdatedAtUtc = DateTime.UtcNow
        };

        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        cut.FindAll("button.admin-switch")
            .First(b => b.GetAttribute("aria-label") == "Scholen-portalen actief")
            .Click();

        Assert.Contains("role=\"dialog\"", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, cut.Instance.DirtyCount);

        cut.FindAll("button").First(b => b.TextContent.Contains("Bevestigen", StringComparison.Ordinal)).Click();
        Assert.Equal(1, cut.Instance.DirtyCount);
        Assert.DoesNotContain("role=\"dialog\"", cut.Markup, StringComparison.Ordinal);

        cut.Find(".admin-save-bar input").Input("portaal uit voor test");
        await cut.InvokeAsync(() =>
            cut.FindAll("button").First(b => b.TextContent.Contains("Opslaan en loggen", StringComparison.Ordinal)).Click());

        Assert.Equal(1, _handler.PutCount);
        Assert.NotNull(_handler.LastPatch);
        Assert.False(_handler.LastPatch!.SchoolsEnabled);
        Assert.Equal(0, cut.Instance.DirtyCount);

        await cut.InvokeAsync(() => cut.Instance.SaveForTestsAsync());
        Assert.Equal(1, _handler.PutCount);
    }

    [Fact]
    public void Switching_employers_on_asks_for_confirmation()
    {
        _handler.Features = new PlatformFeatureItem
        {
            EmployersEnabled = false,
            CandidatePassportEnabled = true,
            VacancyContentModerationEnabled = true,
            PublicWebBaseUrl = "http://localhost:5201"
        };

        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        var button = cut.FindAll("button.admin-switch")
            .First(b => b.GetAttribute("aria-label") == "Werkgevers actief");
        button.Click();

        Assert.Contains(
            "Fase 2 wordt opnieuw ontworpen. Als je dit aanzet, zien werkgevers weer matchpercentages, persoonlijkheidsscores in de talentpool en het AI-verhaal. Weet je het zeker?",
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Cancel_restores_draft()
    {
        _handler.Features = new PlatformFeatureItem
        {
            VacancyContentModerationEnabled = true,
            PublicWebBaseUrl = "http://localhost:5201"
        };
        var cut = Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");
        cut.Instance.SetDraftForTests("VacancyContentModerationEnabled", false);
        cut.Render();
        Assert.Equal(1, cut.Instance.DirtyCount);
        cut.Instance.CancelForTests();
        cut.Render();
        Assert.Equal(0, cut.Instance.DirtyCount);
        Assert.DoesNotContain("admin-save-bar", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Policy_row_has_lock_and_activation_disabled_in_productie()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        ctx.Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        ctx.Services.AddAuthorizationCore();
        ctx.AddAuthorization().SetAuthorized("admin").SetRoles("Admin");
        ctx.Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        ctx.Services.AddSingleton(new DeploymentEnvironmentLabel(DeploymentEnvironment.Productie));
        var handler = new CapturingHandler
        {
            Features = new PlatformFeatureItem
            {
                PublicWebBaseUrl = "http://localhost:5201"
            }
        };
        ctx.Services.AddSingleton(new JobsyApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }));

        var cut = ctx.Render<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        Assert.Contains("admin-settings-lock", cut.Markup, StringComparison.Ordinal);
        var locked = cut.FindAll("button.admin-switch[disabled]");
        Assert.NotEmpty(locked);
    }

    [Fact]
    public void Prices_tabs_include_sales()
    {
        var cut = Render<AdminTabs>(p => p
            .Add(x => x.BasePath, "/admin/financien/prijzen")
            .Add(x => x.ActiveKey, "tokens")
            .Add(x => x.Tabs, new List<AdminTabs.Tab>
            {
                new("tokens", "Tokenprijzen"),
                new("pushbom", "PushBom"),
                new("flex", "Flex & talent"),
                new("early", "Early adapters"),
                new("sales", "Sales & commissie"),
            }));
        Assert.Contains("Tokenprijzen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Sales", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("commissie", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/admin/financien/prijzen?tab=sales", cut.Markup, StringComparison.Ordinal);
    }

    private static ClaimsPrincipal CreateAdmin()
    {
        var id = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D"))
        ], "test");
        return new ClaimsPrincipal(id);
    }

    private sealed class FakeAuthStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class FakeHostEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public PlatformFeatureItem Features { get; set; } = new();
        public int PutCount { get; private set; }
        public PlatformFeaturePatch? LastPatch { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri!.AbsolutePath.Contains("platform-features", StringComparison.Ordinal))
            {
                return Json(Features);
            }

            if (request.Method == HttpMethod.Put
                && request.RequestUri!.AbsolutePath.Contains("platform-features", StringComparison.Ordinal))
            {
                PutCount++;
                var json = await request.Content!.ReadAsStringAsync(cancellationToken);
                LastPatch = JsonSerializer.Deserialize<PlatformFeaturePatch>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (LastPatch?.VacancyContentModerationEnabled is bool m)
                {
                    Features.VacancyContentModerationEnabled = m;
                }

                if (LastPatch?.AuthenticatorEnabled is bool a)
                {
                    Features.AuthenticatorEnabled = a;
                }

                if (LastPatch?.SchoolsEnabled is bool schools)
                {
                    Features.SchoolsEnabled = schools;
                }

                Features.UpdatedAtUtc = DateTime.UtcNow;
                return Json(Features);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(object body)
            => new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(body)
            };
    }
}
