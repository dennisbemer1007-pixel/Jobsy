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
    public void FeatureFlagSnapshot_defaults_employers_on_passport_off()
    {
        var d = FeatureFlagSnapshot.Defaults;
        Assert.True(d.EmployersEnabled);
        Assert.False(d.CandidatePassportEnabled);
        Assert.True(d.IsEnabled(PlatformFeature.Employers));
        Assert.False(d.IsEnabled(PlatformFeature.CandidatePassport));
    }

    [Fact]
    public void PlatformFeatureSettings_entity_defaults()
    {
        var row = new PlatformFeatureSettings();
        Assert.True(row.EmployersEnabled);
        Assert.False(row.CandidatePassportEnabled);
    }

    [Fact]
    public async Task PlatformFeatureService_round_trips_employers_and_passport()
    {
        await using var db = CreateDb();
        var sut = CreateFeatureService(db);
        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: true,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            EmployersEnabled: false,
            CandidatePassportEnabled: true));
        Assert.False(updated.EmployersEnabled);
        Assert.True(updated.CandidatePassportEnabled);

        var again = await sut.GetAsync();
        Assert.False(again.EmployersEnabled);
        Assert.True(again.CandidatePassportEnabled);
    }

    [Fact]
    public async Task Migrated_row_without_explicit_flags_reads_employers_true()
    {
        await using var db = CreateDb();
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            // EmployersEnabled / CandidatePassportEnabled use CLR defaults
        });
        await db.SaveChangesAsync();

        var snap = await CreateFeatureService(db).GetAsync();
        Assert.True(snap.EmployersEnabled);
        Assert.False(snap.CandidatePassportEnabled);
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
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CandidateItems_employers_on_matches_today_order(bool passport, bool _)
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: passport);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(5, items.Count);
        Assert.Equal(["/", "/candidate/liked", "/candidate/applications", "/carriere", "/candidate/profile"],
            items.Select(i => i.Href).ToArray());
        Assert.True(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CandidateItems_employers_off_hides_search_saved_applications(bool passport)
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: passport);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(items, i => i.Href is "/" or "/candidate/liked" or "/candidate/applications");
        Assert.Equal(["/carriere", "/candidate/profile"], items.Select(i => i.Href).ToArray());
        Assert.False(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Fact]
    public void ForUser_employer_roles_empty_when_employers_off()
    {
        var flags = new FeatureFlagSnapshot(false, false);
        var branch = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.BranchManager)], "t"));
        Assert.Empty(RoleNavCatalog.ForUser(branch, flags));

        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, JobsyRoles.Admin)], "t"));
        Assert.NotEmpty(RoleNavCatalog.ForUser(admin, flags));
    }
}

public class FeatureFlagReflectionTests
{
    [Fact]
    public void Expected_pages_carry_RequiresFeature_Employers()
    {
        var assembly = typeof(Jobsy.Web.Navigation.RoleNavCatalog).Assembly;
        var pageTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(Microsoft.AspNetCore.Components.RouteAttribute), inherit: true).Any())
            .ToList();

        var gated = pageTypes
            .Where(t => t.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true)
                .OfType<RequiresFeatureAttribute>()
                .Any(a => a.Feature == PlatformFeature.Employers && a.WhenEnabled))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Contains(gated, n => n.Contains("Home", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("VacancyDetail", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("Register", StringComparison.Ordinal));
        Assert.Contains(gated, n => n.Contains("Applications", StringComparison.Ordinal));
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
