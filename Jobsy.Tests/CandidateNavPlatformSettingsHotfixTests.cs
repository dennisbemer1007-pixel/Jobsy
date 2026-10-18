using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Jobsy.Core.Options;

namespace Jobsy.Tests;

public class CandidateDesktopNavCssTests
{
    [Fact]
    public void Desktop_candidate_banenkaart_hides_bottom_nav_in_favor_of_header_nav()
    {
        var css = File.ReadAllText(FindRepoRoot("Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains(".app-shell--candidate:has(.jobsy-discovery) .bottom-nav", css, StringComparison.Ordinal);
        Assert.Contains("display: none !important", css, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 1024px)", css, StringComparison.Ordinal);
    }

    private static string FindRepoRoot(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path))
            {
                return path;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException($"Could not find {relative}.");
    }
}

public class CandidateNavAllFlagCombinationsTests
{
    public static IEnumerable<object[]> FlagPairs()
    {
        foreach (var employers in new[] { true, false })
        {
            foreach (var passport in new[] { true, false })
            {
                yield return [employers, passport];
            }
        }
    }

    [Theory]
    [MemberData(nameof(FlagPairs))]
    public void CandidateItems_always_has_primary_nav(bool employersEnabled, bool candidatePassportEnabled)
    {
        var flags = new FeatureFlagSnapshot(
            EmployersEnabled: employersEnabled,
            CandidatePassportEnabled: candidatePassportEnabled);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.NotEmpty(items);
        Assert.True(items.Count is >= 2 and <= 5);
    }
}

public class PlatformSettingsMatchCalibrationApiWireTests
{
    [Fact]
    public async Task PlatformFeatureService_round_trips_match_calibration_v1()
    {
        await using var db = CreateDb();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(MatchCalibrationV1Enabled: true));
        Assert.True(updated.MatchCalibrationV1Enabled);

        var again = await sut.GetAsync();
        Assert.True(again.MatchCalibrationV1Enabled);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }
}
