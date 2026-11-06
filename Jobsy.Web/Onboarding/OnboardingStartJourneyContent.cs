using Jobsy.Web.Localization;

namespace Jobsy.Web.Onboarding;

/// <summary>Approved start journey route copy and vignettes (NL/EN via <see cref="CultureState"/>).</summary>
public static class OnboardingStartJourneyContent
{
    public sealed record Stop(string Number, string Title, string Subtitle, string VignettePath);

    public static string HereMarker(CultureState culture) => culture["Onboarding.StartJourney.Here"];

    public static IReadOnlyList<(string Number, string Title, string? Sub)> SidebarNav(CultureState culture) =>
    [
        ("1", culture["Onboarding.Phase.OverJou"], culture["Onboarding.StartJourney.Sidebar1"]),
        ("2", culture["Onboarding.Welcome.Background"], culture["Onboarding.StartJourney.Sidebar2"]),
        ("3", culture["Onboarding.Rustig.Phase.Droom"], culture["Onboarding.StartJourney.Sidebar3"]),
        ("4", culture["Onboarding.Rustig.Phase.WieBenJij"], culture["Onboarding.StartJourney.Sidebar4"]),
        ("5", culture["Onboarding.Rustig.Phase.Kompas"], culture["Onboarding.StartJourney.Sidebar5"]),
    ];

    public static IReadOnlyList<Stop> Stops(CultureState culture) =>
    [
        new("1", culture["Onboarding.Phase.OverJou"], culture["Onboarding.StartJourney.Stop1Sub"],
            "/images/candidate-illustrated/spots/kaart.jpg"),
        new("2", culture["Onboarding.Welcome.Background"], culture["Onboarding.StartJourney.Stop2Sub"],
            "/images/candidate-illustrated/werkervaring.jpg"),
        new("3", culture["Onboarding.Rustig.Phase.Droom"], culture["Onboarding.StartJourney.Stop3Sub"],
            "/images/candidate-illustrated/droombaan.jpg"),
        new("4", culture["Onboarding.Rustig.Phase.WieBenJij"], culture["Onboarding.StartJourney.Stop4Sub"],
            "/images/candidate-illustrated/spots/vragen.jpg"),
        new("5", culture["Onboarding.Rustig.Phase.Kompas"], culture["Onboarding.StartJourney.Stop5Sub"],
            "/images/candidate-illustrated/spots/dna.jpg"),
    ];
}
