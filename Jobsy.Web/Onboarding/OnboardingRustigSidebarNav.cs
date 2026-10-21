namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigSidebarNav
{
    public sealed record NavItem(string Number, string Title, string? Sub, bool Current);

    public static int ActiveSection(int wizardStep) =>
        wizardStep switch
        {
            <= 3 => 1,
            <= 5 => 2,
            6 => 3,
            <= 10 => 4,
            _ => 5,
        };

    public static IReadOnlyList<NavItem> Items(int wizardStep, int? activeSectionOverride = null)
    {
        var active = activeSectionOverride ?? ActiveSection(wizardStep);
        return
        [
            new("1", "Over jou", "3 stappen", active == 1),
            new("2", "Achtergrond", "optioneel", active == 2),
            new("3", "Droom", "1 stap", active == 3),
            new("4", "Wie ben jij", "4 korte tests", active == 4),
            new("5", "Jouw Kompas", "je resultaat", active == 5),
        ];
    }

    public static IReadOnlyList<double> PhaseWidths(int step) =>
        step switch
        {
            <= 0 => [0, 0, 0, 0],
            <= 3 => [100, 0, 0, 0],
            <= 6 => [100, 100, 0, 0],
            <= 9 => [100, 100, 100, 0],
            _ => [100, 100, 100, 100],
        };
}
