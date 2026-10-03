using System.Net;
using Jobsy.Core.Features;
using Jobsy.Core.Localization;
using Jobsy.Web.Admin;
using Jobsy.Web.Auth;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class BewaardRedirectTests
{
    [Theory]
    [InlineData("/bewaard")]
    [InlineData("/bewaard/")]
    [InlineData("/candidate/saved")]
    [InlineData("/candidate/bewaard")]
    public async Task Legacy_bewaard_urls_redirect_302_to_liked(string path)
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();

        using var response = await client.GetAsync(path + "?from=match&n=1");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/candidate/liked?from=match&n=1", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Candidate_liked_is_not_redirected()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var response = await client.GetAsync("/candidate/liked?x=1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseBewaardRedirect();
        app.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsync("fallback");
        });
        await app.StartAsync();
        return app;
    }
}

public class PlatformSettingsCatalogKeyGuardTests
{
    [Fact]
    public void Every_catalog_title_description_and_impact_key_resolves_in_all_languages()
    {
        var missing = new List<string>();
        foreach (var entry in PlatformSettingsCatalog.Entries)
        {
            foreach (var lang in JobsyLanguages.All.Select(l => l.Code))
            {
                AssertResolved(entry.TitleKey, lang, missing);
                AssertResolved(entry.DescriptionKey, lang, missing);
                if (!string.IsNullOrWhiteSpace(entry.ImpactKey))
                {
                    AssertResolved(entry.ImpactKey!, lang, missing);
                }
            }
        }

        Assert.True(missing.Count == 0, string.Join(Environment.NewLine, missing));
    }

    private static void AssertResolved(string key, string lang, List<string> missing)
    {
        var value = UiStrings.Get(key, lang);
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, key, StringComparison.Ordinal))
        {
            missing.Add($"{lang}: {key}");
        }
    }
}

public class CandidatePassportDefaultOnMigrationSourceTests
{
    [Fact]
    public void SetCandidatePassportDefaultOn_flips_rows_and_down_does_not()
    {
        var dir = Path.Combine(FindRepoRoot(), "Jobsy.Infrastructure", "Data", "Migrations");
        var file = Directory.GetFiles(dir, "*_SetCandidatePassportDefaultOn.cs")
            .Single(f => !f.EndsWith(".Designer.cs", StringComparison.Ordinal));
        var source = File.ReadAllText(file);

        Assert.Contains("defaultValue: true", source, StringComparison.Ordinal);
        Assert.Contains(
            """UPDATE "PlatformFeatureSettings" SET "CandidatePassportEnabled" = TRUE;""",
            source,
            StringComparison.Ordinal);

        var downIdx = source.IndexOf("protected override void Down", StringComparison.Ordinal);
        Assert.True(downIdx > 0);
        var down = source[downIdx..];
        Assert.Contains("defaultValue: false", down, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE", down, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not flip row data", down, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Model_snapshot_has_passport_default_true()
    {
        var snapshot = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Data",
            "Migrations",
            "JobsyDbContextModelSnapshot.cs");
        var text = File.ReadAllText(snapshot);
        var idx = text.IndexOf("\"CandidatePassportEnabled\"", StringComparison.Ordinal);
        Assert.True(idx > 0);
        var window = text.Substring(idx, Math.Min(200, text.Length - idx));
        Assert.Contains("HasDefaultValue(true)", window, StringComparison.Ordinal);
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

public class CandidatePassportNavLandingTests
{
    [Fact]
    public void Existing_candidate_without_onboarding_lands_on_discovery_with_full_nav()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true);
        Assert.True(flags.CandidatePassportEnabled);

        var home = FeatureRoutes.CandidateHome(flags, passportReady: false);
        Assert.Equal(FeatureRoutes.CandidateDiscoveryPath, home);

        var nav = RoleNavCatalog.CandidateItems(flags);
        Assert.Equal(
            ["Nav.Discovery", "Nav.Passport", "Nav.Search", "Nav.Applications", "Nav.CareerPath"],
            nav.Select(i => i.TitleKey).ToArray());
        Assert.Equal(5, nav.Count);
        Assert.False(RoleNavCatalog.ShowsSavedInNav(flags));
    }

    [Fact]
    public void Search_item_is_active_on_match_page()
    {
        Assert.True(RoleNavCatalog.IsActive(RoleNavCatalog.SearchItem, "/candidate/match"));
        var items = RoleNavCatalog.CandidateItems(
            new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true));
        var search = items.Single(i => i.TitleKey == "Nav.Search");
        Assert.True(RoleNavCatalog.IsActive(search, "/candidate/match", items));
        Assert.False(RoleNavCatalog.IsActive(items.First(i => i.TitleKey == "Nav.Applications"), "/candidate/match", items));
    }
}
