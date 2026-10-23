using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Seo;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Static candidate primary nav for <c>[NoBlazorRuntime]</c> pages (BottomNav / CandidateHeaderNav are interactive-only).
/// </summary>
internal static class CandidateNavSsrShell
{
    internal static string BuildHeaderNav(CultureState culture, IReadOnlyList<NavItem> items, string relativePath)
    {
        if (items.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        sb.Append("<nav class=\"app-header__nav candidate-header-nav\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Nav.Main"]))
            .Append("\" data-testid=\"candidate-header-nav\">");

        foreach (var item in items)
        {
            AppendNavLink(sb, culture, item, relativePath, items, header: true);
        }

        sb.Append("</nav>");
        return sb.ToString();
    }

    internal static string BuildBottomNav(CultureState culture, IReadOnlyList<NavItem> items, string relativePath)
    {
        if (items.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        sb.Append("<nav class=\"bottom-nav\" aria-label=\"")
            .Append(WebUtility.HtmlEncode(culture["Nav.Main"]))
            .Append("\" data-testid=\"candidate-bottom-nav-ssr\">");

        foreach (var item in items)
        {
            if (item.DesktopOnly)
            {
                continue;
            }

            AppendNavLink(sb, culture, item, relativePath, items, header: false);
        }

        sb.Append("</nav>");
        return sb.ToString();
    }

    internal static async Task<IReadOnlyList<NavItem>> ResolveItemsAsync(
        ClaimsPrincipal? user,
        IFeatureFlags featureFlags,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true
            || !RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return RoleNavCatalog.Anonymous;
        }

        FeatureFlagSnapshot flags;
        try
        {
            flags = await featureFlags.GetAsync(cancellationToken);
        }
        catch
        {
            flags = FeatureFlagSnapshot.Defaults;
        }

        return RoleNavCatalog.ForUser(user, flags);
    }

    private static void AppendNavLink(
        StringBuilder sb,
        CultureState culture,
        NavItem item,
        string relativePath,
        IReadOnlyList<NavItem> siblings,
        bool header)
    {
        var title = culture[item.TitleKey];
        var shortTitle = !string.IsNullOrWhiteSpace(item.ShortTitleKey)
            ? culture[item.ShortTitleKey!]
            : title;
        var active = RoleNavCatalog.IsActive(item, relativePath, siblings);
        var docTitle = PageSeoResolver.WithBrand(culture[PageSeoCatalog.Resolve(item.Href).TitleKey]);
        var testId = HeaderNavTestId(item);

        if (header)
        {
            sb.Append("<a class=\"candidate-header-nav__link");
            if (active)
            {
                sb.Append(" is-active");
            }

            sb.Append("\" href=\"")
                .Append(WebUtility.HtmlEncode(item.Href))
                .Append("\" title=\"")
                .Append(WebUtility.HtmlEncode(title))
                .Append("\" data-lobsy-candidate-nav=\"true\" data-enhance-nav=\"false\" data-lobsy-document-title=\"")
                .Append(WebUtility.HtmlEncode(docTitle))
                .Append('"');
            if (active)
            {
                sb.Append(" aria-current=\"page\"");
            }

            if (!string.IsNullOrEmpty(testId))
            {
                sb.Append(" data-testid=\"")
                    .Append(WebUtility.HtmlEncode(testId))
                    .Append('"');
            }

            sb.Append('>')
                .Append(WebUtility.HtmlEncode(shortTitle))
                .Append("</a>");
            return;
        }

        sb.Append("<a class=\"bottom-nav__item");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"")
            .Append(WebUtility.HtmlEncode(item.Href))
            .Append("\" title=\"")
            .Append(WebUtility.HtmlEncode(title))
            .Append("\" data-lobsy-candidate-nav=\"true\" data-enhance-nav=\"false\" data-lobsy-document-title=\"")
            .Append(WebUtility.HtmlEncode(docTitle))
            .Append('"');

        if (active)
        {
            sb.Append(" aria-current=\"page\"");
        }

        if (!string.IsNullOrEmpty(testId))
        {
            sb.Append(" data-testid=\"")
                .Append(WebUtility.HtmlEncode(testId))
                .Append('"');
        }

        sb.Append("><span class=\"bottom-nav__icon\" aria-hidden=\"true\">")
            .Append(item.Svg)
            .Append("</span><span class=\"bottom-nav__label bottom-nav__label--full\">")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</span><span class=\"bottom-nav__label bottom-nav__label--short\">")
            .Append(WebUtility.HtmlEncode(shortTitle))
            .Append("</span></a>");
    }

    private static string? HeaderNavTestId(NavItem item)
    {
        if (string.Equals(item.Href, RoleNavCatalog.HoursItem.Href, StringComparison.OrdinalIgnoreCase))
        {
            return "nav-maqqie-uren";
        }

        if (string.Equals(item.Href, RoleNavCatalog.DiscoveryItem.Href, StringComparison.OrdinalIgnoreCase))
        {
            return "header-nav-discovery";
        }

        if (string.Equals(item.Href, RoleNavCatalog.SearchItem.Href, StringComparison.OrdinalIgnoreCase))
        {
            return "header-nav-search";
        }

        return null;
    }
}
