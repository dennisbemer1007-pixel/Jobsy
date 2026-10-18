using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class OnboardingFinaleCardsTests
{
    [Fact]
    public void Soon_is_empty_when_all_three_optional_features_enabled()
    {
        var input = new OnboardingFinaleCardsInput(
            CandidatePassportEnabled: true,
            ReferenceVerificationEnabled: true,
            ProactiveMatchingEnabled: true,
            KandidatenkaartEnabled: true,
            DeeperTestsComplete: false,
            HasDreamJob: true);

        Assert.Empty(OnboardingFinaleCards.BuildSoon(input));
        Assert.Equal(3, OnboardingFinaleCards.BuildNow(input).Count(c =>
            c.Id is OnboardingFinaleCardId.References
                or OnboardingFinaleCardId.MatchNotifications
                or OnboardingFinaleCardId.EmployerDiscovery));
    }

    [Fact]
    public void Single_enabled_feature_moves_from_soon_to_now()
    {
        var input = new OnboardingFinaleCardsInput(
            CandidatePassportEnabled: false,
            ReferenceVerificationEnabled: true,
            ProactiveMatchingEnabled: false,
            KandidatenkaartEnabled: false,
            DeeperTestsComplete: true,
            HasDreamJob: false);

        var now = OnboardingFinaleCards.BuildNow(input);
        var soon = OnboardingFinaleCards.BuildSoon(input);

        Assert.Contains(now, c => c.Id == OnboardingFinaleCardId.References);
        Assert.DoesNotContain(soon, c => c.Id == OnboardingFinaleCardId.References);
        Assert.Equal(2, soon.Count);
    }

    [Fact]
    public void All_optional_features_disabled_yields_three_soon_cards()
    {
        var input = new OnboardingFinaleCardsInput(false, false, false, false, false, false);
        Assert.Equal(3, OnboardingFinaleCards.BuildSoon(input).Count);
    }
}
