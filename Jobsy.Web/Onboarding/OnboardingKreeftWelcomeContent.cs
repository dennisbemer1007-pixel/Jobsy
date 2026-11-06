namespace Jobsy.Web.Onboarding;

public static class OnboardingKreeftWelcomeContent
{
    public sealed record ZoneListItem(string TitleKey, string BodyKey);

    public static readonly ZoneListItem[] ZoneCards =
    [
        new("Onboarding.Kreeft.Welcome.Z1Title", "Onboarding.Kreeft.Welcome.Z1Body"),
        new("Onboarding.Kreeft.Welcome.Z2Title", "Onboarding.Kreeft.Welcome.Z2Body"),
        new("Onboarding.Kreeft.Welcome.Z3Title", "Onboarding.Kreeft.Welcome.Z3Body"),
        new("Onboarding.Kreeft.Welcome.Z4Title", "Onboarding.Kreeft.Welcome.Z4Body"),
        new("Onboarding.Kreeft.Welcome.Z5Title", "Onboarding.Kreeft.Welcome.Z5Body"),
    ];
}
