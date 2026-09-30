using Jobsy.Web.KandidaatBanen;

namespace Jobsy.Tests;

public class KbMapStartTests
{
    [Fact]
    public void Resolve_prefers_url_then_session_then_home_then_stored_then_prompt()
    {
        var url = new KbMapStart.OriginPoint(52.1, 4.3, "Url");
        var session = new KbMapStart.OriginPoint(52.2, 4.4, "Session");
        var stored = new KbMapStart.OriginPoint(52.3, 4.5, "Stored");

        var fromUrl = KbMapStart.Resolve(new(
            url, session, 51.99, 4.22, "Home", stored, IsLoggedInCandidate: true));
        Assert.Equal("url", fromUrl.Source);
        Assert.Equal("Url", fromUrl.Origin!.Label);

        var fromSession = KbMapStart.Resolve(new(
            null, session, 51.99, 4.22, "Home", stored, IsLoggedInCandidate: true));
        Assert.Equal("session", fromSession.Source);

        var fromHome = KbMapStart.Resolve(new(
            null, null, 51.99, 4.22, "Herenstraat 20, Wateringen", stored, IsLoggedInCandidate: true));
        Assert.Equal("home", fromHome.Source);
        Assert.Equal(KbMapStart.DefaultTransport, fromHome.Transport);
        Assert.Equal(KbMapStart.DefaultMaxTravelMinutes, fromHome.MaxTravelMinutes);
        Assert.False(fromHome.ShowLocationPrompt);
        Assert.Equal("Herenstraat 20, Wateringen", fromHome.Origin!.Label);

        var fromStored = KbMapStart.Resolve(new(
            null, null, null, null, null, stored, IsLoggedInCandidate: false));
        Assert.Equal("stored", fromStored.Source);

        var prompt = KbMapStart.Resolve(new(
            null, null, null, null, null, null, IsLoggedInCandidate: false));
        Assert.Equal("prompt", prompt.Source);
        Assert.True(prompt.ShowLocationPrompt);
        Assert.Null(prompt.Origin);
    }

    [Fact]
    public void Resolve_candidate_without_home_gets_prompt_when_nothing_stored()
    {
        var r = KbMapStart.Resolve(new(
            null, null, null, null, null, null, IsLoggedInCandidate: true));
        Assert.True(r.ShowLocationPrompt);
        Assert.Equal("prompt", r.Source);
    }
}

public class KbFilterBadgeTests
{
    [Fact]
    public void Fresh_candidate_and_anonymous_have_zero_badge()
    {
        var candidateDefaults = new KbFilterDefaults(AgeYears: 28, MinHoursPerWeek: 16, MaxHoursPerWeek: 32);
        var candidateFresh = new KbFilterState(
            Transport: "Fiets",
            MaxTravelMinutes: 20,
            RadiusKm: 15,
            AgeYears: 28,
            MinHoursPerWeek: 16,
            MaxHoursPerWeek: 32,
            SearchQuery: null,
            WorkTypeCount: 0,
            CategoryCount: 0,
            HasMinWage: false,
            HasMaxWage: false,
            MyVacanciesOnly: false,
            MinMatchPercent: 0,
            HasOrigin: true);
        Assert.Equal(0, KbFilterBadge.Count(candidateFresh, candidateDefaults));

        var anonDefaults = new KbFilterDefaults();
        var anonFresh = new KbFilterState(
            Transport: "Fiets",
            MaxTravelMinutes: 20,
            RadiusKm: 15,
            AgeYears: null,
            MinHoursPerWeek: 0,
            MaxHoursPerWeek: 40,
            SearchQuery: null,
            WorkTypeCount: 0,
            CategoryCount: 0,
            HasMinWage: false,
            HasMaxWage: false,
            MyVacanciesOnly: false,
            MinMatchPercent: 0,
            HasOrigin: false);
        Assert.Equal(0, KbFilterBadge.Count(anonFresh, anonDefaults));
    }

    [Fact]
    public void Each_deviation_counts_once()
    {
        var defaults = new KbFilterDefaults(AgeYears: 20);
        var state = new KbFilterState(
            Transport: "Auto",
            MaxTravelMinutes: 45,
            RadiusKm: 25,
            AgeYears: 18,
            MinHoursPerWeek: 8,
            MaxHoursPerWeek: 16,
            SearchQuery: "zorg",
            WorkTypeCount: 1,
            CategoryCount: 1,
            HasMinWage: true,
            HasMaxWage: true,
            MyVacanciesOnly: true,
            MinMatchPercent: 60,
            HasOrigin: true);
        // work, category, search, age, minWage, maxWage, hours, myVac, minutes, transport, radius, match = 12
        Assert.Equal(12, KbFilterBadge.Count(state, defaults));
    }
}
