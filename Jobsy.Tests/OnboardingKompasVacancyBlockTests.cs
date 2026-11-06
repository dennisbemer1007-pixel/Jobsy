using Jobsy.Core.Rules;

namespace Jobsy.Tests;

/// <summary>
/// Documents why the Kompas match bar can stay empty on acceptatie for an Amsterdam test home.
/// </summary>
public sealed class OnboardingKompasVacancyBlockTests
{
    [Fact]
    public void Test_account_seed_vacancies_sit_in_the_hague_not_amsterdam()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Infrastructure/Ops/TestAccountsSeedService.cs"));
        Assert.Contains("Horeca medewerker (test)", source, StringComparison.Ordinal);
        Assert.Contains("new GeoPoint(52.0780, 4.3100)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Amsterdam", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Early_nearby_signal_returns_zero_without_home_coordinates()
    {
        var prefs = MatchingProfileMapper.DeserializePrefs("""{"maxTravelMinutes":30}""");
        var result = OnboardingEarlyNearbySignal.Compute(null, null, prefs, [], null);
        Assert.Equal(0, result.Count);
        Assert.True(result.MissingHomeLocation);
    }
}
