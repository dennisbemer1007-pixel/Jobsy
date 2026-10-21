namespace Jobsy.Web.Onboarding;

/// <summary>NL copy for onboarding start journey mockup (preview only).</summary>
public static class OnboardingStartJourneyMockCopy
{
    public sealed record Stop(string Number, string Title, string Subtitle, string? Meta, string VignettePath);

    public const string DemoUser = "Alex de Tester";

    public static string Title => "Hoi, fijn dat je er bent";

    public static string Lead =>
        "In ongeveer 12 minuten doorlopen we samen tien korte stappen. Daarna staat je Kompas klaar en zie je welk werk bij je past.";

    public static string StartMeta => "Start";

    public static string MinutesShort => "12 minuten";

    public static string Hint =>
        "Alles wordt meteen bewaard. Stoppen mag, je gaat later verder waar je was.";

    public static string Cta => "Aan de slag";

    public static string HereMarker => "Je bent hier";

    public static string Later => "Later verder";

    public static string AccountLink => "Mijn gegevens & account";

    public static readonly Stop[] Stops =
    [
        new("1", "Over jou", "Waar en wanneer", "3 stappen", "/images/candidate-illustrated/spots/kaart.jpg"),
        new("2", "Achtergrond", "Optioneel", null, "/images/candidate-illustrated/werkervaring.jpg"),
        new("3", "Droom", "1 stap", null, "/images/candidate-illustrated/droombaan.jpg"),
        new("4", "Wie ben jij", "4 korte tests", null, "/images/candidate-illustrated/spots/vragen.jpg"),
        new("5", "Jouw Kompas", "Je resultaat", null, "/images/candidate-illustrated/spots/dna.jpg"),
    ];

    public static readonly (string Number, string Title, string? Sub)[] SidebarNav =
        OnboardingRustigWelcomeContent.SidebarNav;
}
