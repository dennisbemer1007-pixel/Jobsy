using Jobsy.Core.Features;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.UwvMockups;

/// <summary>Full candidate shell flags for UWV preview (paspoort ON + werkgevers ON).</summary>
public static class UwvMockupNav
{
    public static FeatureFlagSnapshot Flags { get; } = new(EmployersEnabled: true, CandidatePassportEnabled: true);

    public static IReadOnlyList<NavItem> Items => RoleNavCatalog.CandidateItems(Flags);

    public static string PreviewScreenForHref(string href)
    {
        if (string.Equals(href, PublicRoutes.Banenkaart, StringComparison.OrdinalIgnoreCase)
            || string.Equals(href, "/", StringComparison.OrdinalIgnoreCase))
        {
            return "banenkaart";
        }

        if (string.Equals(href, "/carriere", StringComparison.OrdinalIgnoreCase))
        {
            return "carriere";
        }

        if (string.Equals(href, "/carriere/leren", StringComparison.OrdinalIgnoreCase))
        {
            return "workshops";
        }

        if (string.Equals(href, "/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase)
            || string.Equals(href, OnboardingRoutes.ClassicStartPath, StringComparison.OrdinalIgnoreCase))
        {
            return "drijfveren";
        }

        if (string.Equals(href, "/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("/candidate/profile", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("/profiel", StringComparison.OrdinalIgnoreCase))
        {
            return "adviseur";
        }

        if (string.Equals(href, "/candidate/applications", StringComparison.OrdinalIgnoreCase)
            || string.Equals(href, "/candidate/liked", StringComparison.OrdinalIgnoreCase))
        {
            return "workshops";
        }

        return "carriere";
    }

    public static string PreviewHref(string href, string lang) =>
        $"/preview/uwv-mockups/{PreviewScreenForHref(href)}?lang={Uri.EscapeDataString(lang)}";
}
