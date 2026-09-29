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
        Assert.False(PlatformSettingsCatalog.FieldExists("EmployersEnabled"));
        Assert.False(PlatformSettingsCatalog.FieldExists("CandidatePassportEnabled"));
        Assert.DoesNotContain(entries, e => e.Key is "EmployersEnabled" or "CandidatePassportEnabled");

        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Jobsy.Web", "Admin", "PlatformSettingsCatalog.cs"));
        Assert.True(File.Exists(sourcePath), sourcePath);
        var source = File.ReadAllText(sourcePath);
        Assert.Contains("EmployersEnabled", source, StringComparison.Ordinal);
        Assert.Contains("CandidatePassportEnabled", source, StringComparison.Ordinal);
        Assert.Contains("// Slot:", source, StringComparison.Ordinal);
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
        var snap = new PlatformFeatureSnapshot(true, true, false, "http://localhost:5201", DateTime.UtcNow);
        var rows = PlatformSettingsCatalog.DashboardRows(snap);
        Assert.Contains(rows, r => r.Key == "VacancyContentModerationEnabled" && r.IsOn);
        Assert.Contains(rows, r => r.Key == "MfaPolicy" && r.IsPolicyReadonly);
        Assert.DoesNotContain(rows, r => r.Key == "AuthenticatorEnabled");
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
    public async Task Activation_links_refused_in_productie_allowed_in_acceptatie()
    {
        await using var dbProd = CreateDb();
        var prod = CreateController(dbProd, DeploymentEnvironment.Productie);
        var refuse = await prod.UpdatePlatformFeatures(
            new UpdatePlatformFeatureRequest(ExposeRegistrationActivationLinks: true),
            CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(refuse.Result);
        Assert.Contains("Acceptatie", bad.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var okOther = await prod.UpdatePlatformFeatures(
            new UpdatePlatformFeatureRequest(SessionInactivityTimeoutMinutes: 45),
            CancellationToken.None);
        Assert.IsType<OkObjectResult>(okOther.Result);

        await using var dbAcc = CreateDb();
        var acc = CreateController(dbAcc, DeploymentEnvironment.Acceptatie);
        var allow = await acc.UpdatePlatformFeatures(
            new UpdatePlatformFeatureRequest(ExposeRegistrationActivationLinks: true),
            CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(allow.Result);
        var dto = Assert.IsType<PlatformFeatureDto>(ok.Value);
        Assert.True(dto.ExposeRegistrationActivationLinks);
    }

    private static Jobsy.Api.Controllers.SettingsController CreateController(JobsyDbContext db, string env)
    {
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());
        return new Jobsy.Api.Controllers.SettingsController(
            db,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            features,
            new PlatformCompanySettingsService(db),
            new AboutPageSettingsService(db),
            new MarketingFlyerSettingsService(db),
            new MarketingFlyerPdfService(
                new MarketingFlyerSettingsService(db),
                new PlatformCompanySettingsService(db),
                features),
            new FlexCommercialService(db),
            new DeploymentEnvironmentLabel(env));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}

public class PlatformSettingsEditorBunitTests : TestContext
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
        this.AddTestAuthorization().SetAuthorized("admin").SetRoles("Admin");
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

        var cut = RenderComponent<PlatformSettingsEditor>(p => p
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
    public void Cancel_restores_draft()
    {
        _handler.Features = new PlatformFeatureItem
        {
            VacancyContentModerationEnabled = true,
            PublicWebBaseUrl = "http://localhost:5201"
        };
        var cut = RenderComponent<PlatformSettingsEditor>(p => p
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
        using var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            new FakeAuthStateProvider(CreateAdmin())));
        ctx.Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthStateProvider(CreateAdmin()));
        ctx.Services.AddAuthorizationCore();
        ctx.AddTestAuthorization().SetAuthorized("admin").SetRoles("Admin");
        ctx.Services.AddSingleton<IHostEnvironment>(new FakeHostEnv());
        ctx.Services.AddSingleton(new DeploymentEnvironmentLabel(DeploymentEnvironment.Productie));
        var handler = new CapturingHandler
        {
            Features = new PlatformFeatureItem
            {
                ExposeRegistrationActivationLinks = false,
                PublicWebBaseUrl = "http://localhost:5201"
            }
        };
        ctx.Services.AddSingleton(new JobsyApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }));

        var cut = ctx.RenderComponent<PlatformSettingsEditor>(p => p
            .Add(x => x.GroupKeys, PlatformSettingsCatalog.FeaturesGroupKeys));
        cut.WaitForElement(".admin-settings-group");

        Assert.Contains("admin-settings-lock", cut.Markup, StringComparison.Ordinal);
        var locked = cut.FindAll("button.admin-switch[disabled]");
        Assert.NotEmpty(locked);
    }

    [Fact]
    public void Prices_tabs_include_sales()
    {
        var cut = RenderComponent<AdminTabs>(p => p
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
        Assert.Contains("Sales & commissie", cut.Markup, StringComparison.Ordinal);
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
