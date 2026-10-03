using System.Reflection;
using System.Security.Claims;
using Jobsy.Api.Models;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Features;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Jobsy.Tests;

public class YouthWageIndicatorTests
{
    [Theory]
    [InlineData(17, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    [InlineData(30, false)]
    public void Applies_uses_the_adult_wage_cutoff(int age, bool expected)
        => Assert.Equal(expected, YouthWageIndicator.Applies(age));

    [Fact]
    public void Unknown_age_is_null()
        => Assert.Null(YouthWageIndicator.Applies(null));

    [Fact]
    public void Cutoff_is_the_wage_table_constant()
        => Assert.Equal(21, AgeRules.AdultAgeYears);
}

public class EmployerAgeSurfaceTests
{
    [Fact]
    public void Employer_application_dtos_do_not_expose_age_or_date_of_birth()
    {
        foreach (var type in new[] { typeof(EmployerApplicationDto), typeof(EmployerApplicationItem) })
        {
            foreach (var property in type.GetProperties())
            {
                if (property.Name == "YouthWageApplies")
                {
                    continue;
                }

                Assert.DoesNotContain("Age", property.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Birth", property.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Wage_table_dtos_may_keep_age_rates()
    {
        var wageTypes = typeof(EmployerApplicationDto).Assembly.GetTypes()
            .Where(t => t.IsPublic && t.Name.Contains("Wage", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(wageTypes);
    }
}

public class EmployersRouteGateTests
{
    [Fact]
    public void Anonymous_employer_page_goes_to_binnenkort()
    {
        var off = new FeatureFlagSnapshot(false, true);
        Assert.Equal(
            FeatureRoutes.EmployersComingSoonPath,
            FeatureRoutes.EmployersOffRedirect(null, off, false, candidateVacancySurface: false, explicitFallback: null));
    }

    [Fact]
    public void Anonymous_candidate_vacancy_page_stays_on_home()
    {
        var off = new FeatureFlagSnapshot(false, true);
        Assert.Equal(
            "/",
            FeatureRoutes.EmployersOffRedirect(null, off, false, candidateVacancySurface: true, explicitFallback: null));
    }

    [Fact]
    public void Employer_side_user_goes_to_access_denied()
    {
        var off = new FeatureFlagSnapshot(false, true);
        var user = Principal(JobsyRoles.BranchManager);
        Assert.Equal(
            FeatureRoutes.EmployersOffAccessDeniedPath,
            FeatureRoutes.EmployersOffRedirect(user, off, false, false, "/werkgevers/binnenkort"));
    }

    [Fact]
    public void Explicit_fallback_is_kept_for_anonymous_public_pages()
    {
        var off = new FeatureFlagSnapshot(false, true);
        Assert.Equal(
            "/",
            FeatureRoutes.EmployersOffRedirect(null, off, false, false, "/"));
    }

    [Fact]
    public void Routable_employer_pages_require_the_employers_feature()
    {
        var assembly = typeof(Jobsy.Web.Navigation.RoleNavCatalog).Assembly;
        var missing = assembly.GetTypes()
            .Where(IsInScope)
            .Where(t => !HasEmployersGate(t))
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Domain_api_controllers_are_employers_gated()
    {
        var assembly = typeof(Jobsy.Api.Controllers.VacanciesController).Assembly;
        string[] names =
        [
            "VacanciesController", "VacancyEngagementController", "ExternalVacanciesController",
            "VacancyCsvImportController", "ApplicationsController", "CandidateActionsController",
            "TalentPoolController", "CandidateInsightsController", "CompanyCultureController",
            "CompaniesController", "CompanyUsersController", "CompanyApiKeysController",
            "PublicCompaniesController", "SupplierOnboardingController", "KvkController",
            "RegistrationController", "TokensController", "TokenLogsController",
            "SalaryTablesController", "WagesController", "EmployerFlyersController",
            "DashboardController", "MetricsController", "SalesManagersController",
            "SalesCommercialController", "AmbassadeursController", "PartnerAffiliateController",
            "RegionsController", "VacancyCategoriesController", "RegionHostsController",
            "AdminAtsController"
        ];

        var missing = new List<string>();
        foreach (var name in names)
        {
            var type = assembly.GetTypes().Single(t => t.Name == name);
            if (!HasEmployersGate(type))
            {
                missing.Add(name);
            }
        }

        Assert.Empty(missing);
    }

    [Fact]
    public async Task Switch_adapter_follows_the_flag()
    {
        var off = new FeatureFlagEmployersSwitch(new FakeFlags(false));
        Assert.False(await off.IsEnabledAsync());
        Assert.Equal(LandingVariant.Zw, await off.VariantAsync());

        var on = new FeatureFlagEmployersSwitch(new FakeFlags(true));
        Assert.True(await on.IsEnabledAsync());
        Assert.Equal(LandingVariant.On, await on.VariantAsync());
    }

    [Fact]
    public void Migration_turns_employers_off_once_and_down_does_not_flip_rows()
    {
        var dir = Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "Migrations");
        var file = Directory.EnumerateFiles(dir, "*SetEmployersDefaultOff.cs")
            .Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var text = File.ReadAllText(file);
        var up = text[..text.IndexOf("protected override void Down", StringComparison.Ordinal)];
        var down = text[text.IndexOf("protected override void Down", StringComparison.Ordinal)..];

        Assert.Contains("defaultValue: false", up, StringComparison.Ordinal);
        Assert.Contains("SET \"EmployersEnabled\" = FALSE", up, StringComparison.Ordinal);
        Assert.Contains("decision 20", up, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", down, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("defaultValue: true", down, StringComparison.Ordinal);
    }

    private static bool IsInScope(Type type)
    {
        if (type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Length == 0)
        {
            return false;
        }

        var ns = type.Namespace ?? "";
        if (ns.Contains(".Pages.Employer", StringComparison.Ordinal)
            || ns.Contains(".Pages.Werkgever", StringComparison.Ordinal)
            || ns.Contains(".Pages.Intermediary", StringComparison.Ordinal)
            || ns.Contains(".Pages.Sales", StringComparison.Ordinal)
            || ns.Contains(".Pages.Ambassadeur", StringComparison.Ordinal)
            || ns.Contains(".Pages.Partner", StringComparison.Ordinal))
        {
            return true;
        }

        return type.Name is "RegisterBedrijf" or "RegisterKoppelen" or "RegisterToegang"
            or "RegisterVerifieren" or "RegisterVerifierenBrief" or "MetricDrilldownPage";
    }

    private static bool HasEmployersGate(Type type)
    {
        var attrs = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true))
            .Concat(type.GetCustomAttributes(typeof(RequiresFeatureAttribute), inherit: true))
            .OfType<RequiresFeatureAttribute>();
        return attrs.Any(a => a.Feature == PlatformFeature.Employers && a.WhenEnabled);
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var id = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Name, "test")],
            "test");
        return new ClaimsPrincipal(id);
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

    private sealed class FakeFlags(bool employers) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(employers, true).IsEnabled(feature));

        public void Invalidate()
        {
        }
    }
}
