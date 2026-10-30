using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

/// <summary>Visual shell theme (<c>t-zee</c>, <c>t-tuin</c>) for groep 7/8 layout pages.</summary>
public sealed class LeerlingGroep78ShellTheme
{
    private string _cssClass = "t-zee";
    private string _headerSmallKey = "Leerling.Wereld.HeaderGrade";

    public event Action? Changed;

    public string CssClass => _cssClass;

    public string HeaderSmallKey => _headerSmallKey;

    public void ApplyJourney(string journeyThemeKey, bool routeHeader)
    {
        _cssClass = journeyThemeKey switch
        {
            var k when string.Equals(k, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase) => "t-tuin",
            var k when string.Equals(k, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase) => "t-ster",
            _ => "t-zee",
        };

        _headerSmallKey = routeHeader
            ? HeaderKeyForRoute(journeyThemeKey)
            : "Leerling.Wereld.HeaderGrade";

        Changed?.Invoke();
    }

    public void Reset()
    {
        _cssClass = "t-zee";
        _headerSmallKey = "Leerling.Wereld.HeaderGrade";
        Changed?.Invoke();
    }

    private static string HeaderKeyForRoute(string journeyThemeKey)
    {
        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.Wereld.Tuin.Title";
        }

        if (string.Equals(journeyThemeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return "Leerling.Wereld.Ster.Title";
        }

        return "Leerling.Route.HeaderExpedition";
    }
}
