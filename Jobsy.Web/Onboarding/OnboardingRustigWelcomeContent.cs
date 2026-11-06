namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigWelcomeContent
{
    public sealed record PhaseCard(string Number, string Title, string Sub, string? Meta);

    public static readonly PhaseCard[] JourneyPhases =
    [
        new("1", "Over jou", "Waar je woont en wanneer je kunt", "3 stappen"),
        new("2", "Achtergrond", "Optioneel", null),
        new("3", "Droom", "1 stap", null),
        new("4", "Wie ben jij", "4 korte tests", null),
        new("5", "Jouw Kompas", "je resultaat", null),
    ];

    public static readonly (string Number, string Title, string? Sub)[] SidebarNav =
    [
        ("1", "Over jou", "3 stappen"),
        ("2", "Achtergrond", "optioneel"),
        ("3", "Droom", "1 stap"),
        ("4", "Wie ben jij", "4 korte tests"),
        ("5", "Jouw Kompas", "je resultaat"),
    ];
}
