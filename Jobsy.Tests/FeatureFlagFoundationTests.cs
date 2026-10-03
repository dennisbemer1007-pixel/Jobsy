using System.Reflection;
using System.Security.Claims;
using Jobsy.Api.Filters;
using Jobsy.Core.Authorization;
using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Features;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jobsy.Tests;

public class FeatureFlagFoundationTests
{
    [Fact]
    public void FeatureFlagSnapshot_defaults_employers_on_passport_on()
    {
        var d = FeatureFlagSnapshot.Defaults;
        Assert.True(d.EmployersEnabled);
        Assert.True(d.CandidatePassportEnabled);
        Assert.True(d.IsEnabled(PlatformFeature.Employers));
        Assert.True(d.IsEnabled(PlatformFeature.CandidatePassport));
    }

    [Fact]
    public void PlatformFeatureSettings_entity_defaults()
    {
        var row = new PlatformFeatureSettings();
        Assert.True(row.EmployersEnabled);
        Assert.True(row.CandidatePassportEnabled);
    }

    [Fact]
    public async Task PlatformFeatureService_round_trips_employers_and_passport()
    {
        await using var db = CreateDb();
        var sut = CreateFeatureService(db);
        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: true,
            PublicWebBaseUrl: "http://localhost:5201",
            EmployersEnabled: false,
            CandidatePassportEnabled: false));
        Assert.False(updated.EmployersEnabled);
        Assert.False(updated.CandidatePassportEnabled);

        var again = await sut.GetAsync();
        Assert.False(again.EmployersEnabled);
        Assert.False(again.CandidatePassportEnabled);
    }

    [Fact]
    public async Task Migrated_row_without_explicit_flags_reads_employers_and_passport_true()
    {
        await using var db = CreateDb();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            // EmployersEnabled / CandidatePassportEnabled use CLR defaults (both true)
        });
        await db.SaveChangesAsync();

        var snap = await CreateFeatureService(db).GetAsync();
        Assert.True(snap.EmployersEnabled);
        Assert.True(snap.CandidatePassportEnabled);
    }

    [Fact]
    public async Task PlatformFeatureService_admin_off_stays_off()
    {
        await using var db = CreateDb();
        var sut = CreateFeatureService(db);
        await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: true,
            PublicWebBaseUrl: "http://localhost:5201",
            CandidatePassportEnabled: false));

        var again = await sut.GetAsync();
        Assert.False(again.CandidatePassportEnabled);

        // No startup re-enable: a second Get still reports OFF.
        Assert.False((await sut.GetAsync()).CandidatePassportEnabled);
    }

    [Fact]
    public async Task PlatformFeatureService_no_row_reports_passport_on()
    {
        await using var db = CreateDb();
        var snap = await CreateFeatureService(db).GetAsync();
        Assert.True(snap.EmployersEnabled);
        Assert.True(snap.CandidatePassportEnabled);
    }

    [Fact]
    public void Employer_email_templates_are_marked_RequiresEmployers()
    {
        Assert.Contains(TransactionalEmails.Templates, t => t.Key == "ApplicationConfirmation" && t.RequiresEmployers);
        Assert.Contains(TransactionalEmails.Templates, t => t.Key == "PushBom" && t.RequiresEmployers);
        Assert.Contains(TransactionalEmails.Templates, t => t.Key == "AccountUnsubscribeVerification" && !t.RequiresEmployers);
        Assert.Contains(TransactionalEmails.Templates, t => t.Key == "MailTest" && !t.RequiresEmployers);
    }

    [Fact]
    public async Task FeatureGateFilter_blocks_when_employers_off()
    {
        var flags = new FakeFlags(employers: false);
        var filter = new FeatureGateFilter(flags);
        var http = new DefaultHttpContext();
        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = new List<object> { new RequiresFeatureAttribute(PlatformFeature.Employers) }
        };
        var actionContext = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), descriptor);
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        await filter.OnActionExecutionAsync(executing, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object())));
        var blocked = Assert.IsType<ObjectResult>(executing.Result);
        Assert.Equal(StatusCodes.Status404NotFound, blocked.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(blocked.Value);
        Assert.Equal(FeatureGateFilter.FeatureDisabledType, problem.Type);
    }

    [Fact]
    public async Task FeatureGateFilter_allows_when_employers_on()
    {
        var flags = new FakeFlags(employers: true);
        var filter = new FeatureGateFilter(flags);
        var http = new DefaultHttpContext();
        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = new List<object> { new RequiresFeatureAttribute(PlatformFeature.Employers) }
        };
        var actionContext = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), descriptor);
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var ran = false;
        await filter.OnActionExecutionAsync(executing, () =>
        {
            ran = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
        });
        Assert.True(ran);
        Assert.Null(executing.Result);
    }

    [Fact]
    public void FeatureRoutes_home_for_anonymous_and_roles()
    {
        var on = new FeatureFlagSnapshot(true, false);
        var off = new FeatureFlagSnapshot(false, false);
        Assert.Equal("/", FeatureRoutes.HomeFor(null, on));
        Assert.Equal(FeatureRoutes.OntdekPath, FeatureRoutes.HomeFor(null, off));

        var candidate = Principal(JobsyRoles.Candidate);
        Assert.Equal("/", FeatureRoutes.HomeFor(candidate, on));
        Assert.Equal(FeatureRoutes.CandidateProfilePath, FeatureRoutes.HomeFor(candidate, off));

        var passportOn = new FeatureFlagSnapshot(true, true);
        Assert.Equal(
            FeatureRoutes.CandidateDiscoveryPath,
            FeatureRoutes.HomeFor(candidate, passportOn, passportReady: false));
        Assert.Equal(
            FeatureRoutes.CandidatePassportPath,
            FeatureRoutes.HomeFor(candidate, passportOn, passportReady: true));
        var passportEmployersOff = new FeatureFlagSnapshot(false, true);
        Assert.Equal(
            FeatureRoutes.CandidateDiscoveryPath,
            FeatureRoutes.HomeFor(candidate, passportEmployersOff, passportReady: false));
        Assert.Equal(
            FeatureRoutes.CandidatePassportPath,
            FeatureRoutes.HomeFor(candidate, passportEmployersOff, passportReady: true));

        var employer = Principal(JobsyRoles.BranchManager);
        Assert.Equal(FeatureRoutes.EmployersOffAccessDeniedPath, FeatureRoutes.HomeFor(employer, off));

        var admin = Principal(JobsyRoles.Admin);
        Assert.Equal(FeatureRoutes.AdminHomePath, FeatureRoutes.HomeFor(admin, off));
    }

    private static ClaimsPrincipal Principal(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        claims.Add(new Claim(ClaimTypes.Name, "test"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static PlatformFeatureService CreateFeatureService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            })
            .Build();
        return new PlatformFeatureService(
            db,
            Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            config,
            new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class FakeFlags(bool employers) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, false));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.Employers && employers);

        public void Invalidate()
        {
        }
    }
}

public class RoleNavCatalogFeatureFlagTests
{
    [Fact]
    public void CandidateItems_passport_off_employers_on_matches_today_order()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: false);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(5, items.Count);
        Assert.Equal(
            ["/banenkaart", "/candidate/liked", "/candidate/applications", "/carriere", "/candidate/profile"],
            items.Select(i => i.Href).ToArray());
        Assert.Equal("Nav.Search", items[0].TitleKey);
        Assert.True(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Fact]
    public void CandidateItems_passport_on_employers_on_uses_slot_order()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(5, items.Count);
        Assert.True(items.Count <= 5);
        Assert.Equal(
            ["/candidate/ontdekkingsreis", "/candidate/paspoort", "/banenkaart", "/candidate/applications", "/carriere"],
            items.Select(i => i.Href).ToArray());
        Assert.Equal(
            ["Nav.Discovery", "Nav.Passport", "Nav.Search", "Nav.Applications", "Nav.CareerPath"],
            items.Select(i => i.TitleKey).ToArray());
        Assert.Contains("/candidate/liked", items[3].ExtraActivePaths ?? []);
        Assert.Contains("/candidate/match", items[2].ExtraActivePaths ?? []);
        Assert.False(RoleNavCatalog.ShowsSavedInNav(flags));
        Assert.DoesNotContain(items, i => i.Href.Contains("match", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.TitleKey.Contains("Match", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CandidateItems_passport_off_employers_off_hides_search_saved_applications()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: false);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(2, items.Count);
        Assert.Equal(["/carriere", "/candidate/profile"], items.Select(i => i.Href).ToArray());
        Assert.False(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Fact]
    public void CandidateItems_passport_on_employers_off_is_discovery_passport_and_career()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: true);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(3, items.Count);
        Assert.Equal(
            ["/candidate/ontdekkingsreis", "/candidate/paspoort", "/carriere"],
            items.Select(i => i.Href).ToArray());
        Assert.Equal(
            ["Nav.Discovery", "Nav.Passport", "Nav.CareerPath"],
            items.Select(i => i.TitleKey).ToArray());
        Assert.False(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Fact]
    public void CandidateItems_never_includes_match_for_any_flag_combo()
    {
        foreach (var employers in new[] { true, false })
            foreach (var passport in new[] { true, false })
            {
                var items = RoleNavCatalog.CandidateItems(new FeatureFlagSnapshot(employers, passport));
                Assert.DoesNotContain(items, i =>
                    i.Href.Contains("match", StringComparison.OrdinalIgnoreCase)
                    || i.TitleKey.Contains("Match", StringComparison.OrdinalIgnoreCase));
            }
    }

    [Fact]
    public void ForUser_employer_roles_empty_when_employers_off()
    {
        var flags = new FeatureFlagSnapshot(false, false);
        var branch = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.BranchManager)], "t"));
        Assert.Empty(RoleNavCatalog.ForUser(branch, flags));

        // Admin bottom-nav catalog is intentionally empty (AdminNav sidebar owns chrome).
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.Admin)], "t"));
        Assert.Empty(RoleNavCatalog.ForUser(admin, flags));
    }
}

public class FeatureFlagReflectionTests
{
    [Fact]
    public void Expected_pages_carry_RequiresFeature_Employers()
    {
        var assembly = typeof(Jobsy.Web.Navigation.RoleNavCatalog).Assembly;
        var pageTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), inherit: true).Length > 0)
            .ToList();

        var gated = pageTypes
            .Where(t => t.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
                .OfType<RequiresFeatureAttribute>()
                .Any(a => a.Feature == PlatformFeature.Employers && a.WhenEnabled))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Contains(gated, n => n.Contains("VacancyDetail", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("Register", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("Applications", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("Werkgever", StringComparison.Ordinal));
        // /home (RoleHome) is multi-role and must not be Employers-gated.
        Assert.DoesNotContain(gated, n => n.EndsWith(".RoleHome", StringComparison.Ordinal));
        Assert.True(gated.Count >= 40, $"Expected many gated pages, got {gated.Count}");
    }

    [Fact]
    public void Expected_controllers_carry_RequiresFeature_Employers()
    {
        var assembly = typeof(Jobsy.Api.Controllers.VacanciesController).Assembly;
        var controllers = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal) && !t.IsAbstract)
            .ToList();

        var gatedControllers = controllers
            .Where(t => t.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
                .OfType<RequiresFeatureAttribute>()
                .Any(a => a.Feature == PlatformFeature.Employers))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Contains("VacanciesController", gatedControllers);
        Assert.Contains("ApplicationsController", gatedControllers);
        Assert.Contains("RegistrationController", gatedControllers);
        Assert.Contains("KvkController", gatedControllers);
        Assert.DoesNotContain("AuthController", gatedControllers);
        Assert.DoesNotContain("MollieWebhooksController", gatedControllers);
        Assert.DoesNotContain("SettingsController", gatedControllers);
    }
}
