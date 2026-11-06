namespace Jobsy.Core.Rules;

/// <summary>Cards for the onboarding finale v2 screen (now vs binnenkort).</summary>
public enum OnboardingFinaleCardId
{
    Matching,
    Banenkaart,
    KompasWords,
    DreamRoute,
    Passport,
    Deeper,
    References,
    MatchNotifications,
    EmployerDiscovery
}

public sealed record OnboardingFinaleCard(
    OnboardingFinaleCardId Id,
    string TitleKey,
    string BodyKey,
    bool ComingSoon);

public sealed record OnboardingFinaleCardsInput(
    bool CandidatePassportEnabled,
    bool ReferenceVerificationEnabled,
    bool ProactiveMatchingEnabled,
    bool KandidatenkaartEnabled,
    bool DeeperTestsComplete,
    bool HasDreamJob);

public static class OnboardingFinaleCards
{
    public static IReadOnlyList<OnboardingFinaleCard> BuildNow(OnboardingFinaleCardsInput input)
    {
        var list = new List<OnboardingFinaleCard>
        {
            new(OnboardingFinaleCardId.Matching, "Onboarding.FinaleV2.Do.Match.Title", "Onboarding.FinaleV2.Do.Match.Body", false),
            new(OnboardingFinaleCardId.Banenkaart, "Onboarding.FinaleV2.Do.Map.Title", "Onboarding.FinaleV2.Do.Map.Body", false),
            new(OnboardingFinaleCardId.KompasWords, "Onboarding.FinaleV2.Do.Compass.Title", "Onboarding.FinaleV2.Do.Compass.Body", false),
            DreamRouteCard(input),
        };

        if (input.CandidatePassportEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.Passport,
                "Onboarding.FinaleV2.Do.Passport.Title",
                "Onboarding.FinaleV2.Do.Passport.Body",
                false));
        }

        if (!input.DeeperTestsComplete)
        {
            list.Add(new(
                OnboardingFinaleCardId.Deeper,
                "Onboarding.FinaleV2.Do.Deeper.Title",
                "Onboarding.FinaleV2.Do.Deeper.Body",
                false));
        }

        if (input.ReferenceVerificationEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.References,
                "Onboarding.FinaleV2.Do.References.Title",
                "Onboarding.FinaleV2.Do.References.Body",
                false));
        }

        if (input.ProactiveMatchingEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.MatchNotifications,
                "Onboarding.FinaleV2.Do.Notifications.Title",
                "Onboarding.FinaleV2.Do.Notifications.Body",
                false));
        }

        if (input.KandidatenkaartEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.EmployerDiscovery,
                "Onboarding.FinaleV2.Do.Employers.Title",
                "Onboarding.FinaleV2.Do.Employers.Body",
                false));
        }

        return list;
    }

    public static IReadOnlyList<OnboardingFinaleCard> BuildSoon(OnboardingFinaleCardsInput input)
    {
        var list = new List<OnboardingFinaleCard>();
        if (!input.ReferenceVerificationEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.References,
                "Onboarding.FinaleV2.Soon.References.Title",
                "Onboarding.FinaleV2.Soon.References.Body",
                true));
        }

        if (!input.ProactiveMatchingEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.MatchNotifications,
                "Onboarding.FinaleV2.Soon.Notifications.Title",
                "Onboarding.FinaleV2.Soon.Notifications.Body",
                true));
        }

        if (!input.KandidatenkaartEnabled)
        {
            list.Add(new(
                OnboardingFinaleCardId.EmployerDiscovery,
                "Onboarding.FinaleV2.Soon.Employers.Title",
                "Onboarding.FinaleV2.Soon.Employers.Body",
                true));
        }

        return list;
    }

    private static OnboardingFinaleCard DreamRouteCard(OnboardingFinaleCardsInput input)
        => input.HasDreamJob
            ? new(
                OnboardingFinaleCardId.DreamRoute,
                "Onboarding.FinaleV2.Do.Route.Title",
                "Onboarding.FinaleV2.Do.Route.Body",
                false)
            : new(
                OnboardingFinaleCardId.DreamRoute,
                "Onboarding.FinaleV2.Do.RouteGeneric.Title",
                "Onboarding.FinaleV2.Do.RouteGeneric.Body",
                false);
}
