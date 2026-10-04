using Jobsy.Web.Features;

namespace Jobsy.Web.Navigation;

public sealed record PublicNavItem(string LabelKey, string Href, bool IsAvailable = true);

public sealed record PublicFooterColumn(string TitleKey, IReadOnlyList<PublicNavItem> Items);

/// <summary>Pure ON/OFF-aware header and footer catalogs for the public shell.</summary>
public static class PublicNavCatalog
{
    public static IReadOnlyList<PublicNavItem> Header(LandingVariant variant)
        => variant == LandingVariant.Zw ? HeaderZw : HeaderOn;

    public static IReadOnlyList<PublicFooterColumn> Footer(LandingVariant variant)
        => variant == LandingVariant.Zw ? FooterZw : FooterOn;

    private static readonly PublicFooterColumn LegalColumnOn = new(
        "PublicFooter.Legal",
        [
            new("PublicFooter.Privacy", PublicRoutes.Privacy),
            new("PublicFooter.Cookies", PublicRoutes.PrivacyCookies),
            new("PublicFooter.Terms", PublicRoutes.Terms),
            new("PublicFooter.UsageTerms", PublicRoutes.UsageTerms),
            new("PublicFooter.About", PublicRoutes.About),
            new("PublicFooter.Accessibility", PublicRoutes.Accessibility)
        ]);

    /// <summary>
    /// OFF hides the employer terms from the nav (Dependency F). The document itself stays reachable
    /// at <see cref="PublicRoutes.Terms"/> and from the audience switch on the terms pages (04.2).
    /// </summary>
    private static readonly PublicFooterColumn LegalColumnZw = new(
        "PublicFooter.Legal",
        [
            new("PublicFooter.Privacy", PublicRoutes.Privacy),
            new("PublicFooter.Cookies", PublicRoutes.PrivacyCookies),
            new("PublicFooter.UsageTerms", PublicRoutes.UsageTerms),
            new("PublicFooter.About", PublicRoutes.About),
            new("PublicFooter.Accessibility", PublicRoutes.Accessibility)
        ]);

    private static readonly IReadOnlyList<PublicNavItem> HeaderOn =
    [
        new("PublicNav.HowItWorks", PublicRoutes.HowItWorks),
        // Available once file 04 moves the map.
        new("PublicNav.JobMap", PublicRoutes.Banenkaart, IsAvailable: true),
        // Available once file 08 builds /werkgevers.
        new("PublicNav.Employers", PublicRoutes.Employers, IsAvailable: false),
        // Available once file 08 builds /scholen.
        new("PublicNav.Schools", PublicRoutes.Schools, IsAvailable: false),
        new("PublicNav.Partners", PublicRoutes.Partner)
    ];

    private static readonly IReadOnlyList<PublicNavItem> HeaderZw =
    [
        new("PublicNav.HowItWorks", PublicRoutes.HowItWorks),
        new("PublicNav.MyPassport", PublicRoutes.PassportAnchor),
        new("PublicNav.Discovery", PublicRoutes.DiscoveryAnchor),
        new("PublicNav.Schools", PublicRoutes.Schools, IsAvailable: false)
    ];

    private static readonly IReadOnlyList<PublicFooterColumn> FooterOn =
    [
        new("PublicFooter.For",
        [
            new("PublicFooter.Candidates", PublicRoutes.Test),
            new("PublicNav.Employers", PublicRoutes.Employers, IsAvailable: false),
            new("PublicNav.Schools", PublicRoutes.Schools, IsAvailable: false),
            new("PublicNav.Partners", PublicRoutes.Partner)
        ]),
        new("PublicFooter.Account",
        [
            new("PublicNav.Login", PublicRoutes.Login),
            new("PublicNav.CreateAccount", PublicRoutes.CreateAccount),
            new("PublicFooter.CompanyRegister", PublicRoutes.CompanyRegister)
        ]),
        LegalColumnOn
    ];

    private static readonly IReadOnlyList<PublicFooterColumn> FooterZw =
    [
        new("PublicFooter.For",
        [
            new("PublicFooter.You", PublicRoutes.Test),
            // Opens the language menu (hash targets pub-lang details).
            new("PublicFooter.NewInNetherlands", "#pub-lang"),
            new("PublicNav.Schools", PublicRoutes.Schools, IsAvailable: false)
        ]),
        new("PublicFooter.Account",
        [
            new("PublicNav.Login", PublicRoutes.Login),
            new("PublicNav.CreateAccount", PublicRoutes.CreateAccount)
        ]),
        LegalColumnZw
    ];
}
