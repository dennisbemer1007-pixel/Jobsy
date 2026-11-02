using System.Net;
using System.Text;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;

namespace Jobsy.Web.Scholen;

/// <summary>Full HTML documents for school / teacher SSR (no Blazor _framework).</summary>
public static class ScholenPortalSsrFrame
{
    public static string RenderDocument(
        string pageTitle,
        string bodyHtml,
        ScholenPortalShell shell,
        CultureState culture,
        string? routePath = null)
    {
        var path = PageSeoCatalog.Normalize(routePath ?? shell.CurrentPath);
        var styles = PageAssetCatalog.StylesheetsForSchoolSsr(path);
        var lang = culture.Language;

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>");
        sb.Append(WebUtility.HtmlEncode(pageTitle));
        sb.Append(" · Lobsy</title>");
        foreach (var href in styles)
        {
            sb.Append("<link rel=\"stylesheet\" href=\"");
            sb.Append(WebUtility.HtmlEncode(href));
            sb.Append("\" />");
        }

        sb.Append("</head><body><div class=\"sch-shell\" lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\">");

        AppendTopbar(sb, shell, culture);
        sb.Append("<div class=\"sch-body\">");
        if (shell.ShellKind != ScholenPortalShell.Kind.Unknown)
        {
            AppendSidebar(sb, shell, culture);
        }

        sb.Append("<main class=\"sch-main\">");
        if (shell.FeatureDisabled)
        {
            sb.Append("<p class=\"state-message error\">");
            sb.Append(WebUtility.HtmlEncode(culture["School.FeatureDisabled"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append(bodyHtml);
        }

        sb.Append("</main></div>");

        if (shell.ShellKind != ScholenPortalShell.Kind.Unknown)
        {
            AppendBottomNav(sb, shell, culture);
        }

        sb.Append("<footer class=\"sch-legal\"><a href=\"");
        sb.Append(PublicRoutes.Privacy);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"");
        sb.Append(PublicRoutes.Accessibility);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a></footer></div></body></html>");
        return sb.ToString();
    }

    private static void AppendTopbar(StringBuilder sb, ScholenPortalShell shell, CultureState culture)
    {
        sb.Append("<header class=\"sch-topbar\"><a class=\"sch-topbar__brand\" href=\"");
        sb.Append(WebUtility.HtmlEncode(shell.HomeHref));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["School.ProductLabel"]));
        sb.Append("</a><div class=\"sch-topbar__scope\"><div class=\"sch-topbar__scope-text\"><span class=\"sch-topbar__scope-name\">");
        sb.Append(WebUtility.HtmlEncode(shell.ScopeLabel));
        sb.Append("</span><span class=\"sch-topbar__scope-role\">");
        sb.Append(WebUtility.HtmlEncode(shell.RoleLabel));
        sb.Append("</span></div></div><div class=\"sch-topbar__account\"><span class=\"sch-topbar__role\">");
        sb.Append(WebUtility.HtmlEncode(shell.RoleLabel));
        sb.Append("</span><a class=\"sch-topbar__link\" href=\"/account/logout\">");
        sb.Append(WebUtility.HtmlEncode(culture["Auth.Logout"]));
        sb.Append("</a></div></header>");
    }

    private static void AppendSidebar(StringBuilder sb, ScholenPortalShell shell, CultureState culture)
    {
        sb.Append("<aside class=\"sch-sidebar\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Main"]));
        sb.Append("\">");
        foreach (var group in shell.Groups)
        {
            var visible = group.Items.Where(i => i.IsAvailable).ToList();
            if (visible.Count == 0)
            {
                continue;
            }

            sb.Append("<p class=\"sch-sidebar__group\">");
            sb.Append(WebUtility.HtmlEncode(culture[group.LabelKey]));
            sb.Append("</p><ul class=\"sch-sidebar__list\">");
            foreach (var item in visible)
            {
                var active = ScholenNavRouting.IsNavActive(shell.CurrentPath, item.Href);
                var label = !string.IsNullOrEmpty(item.FixedLabel)
                    ? item.FixedLabel
                    : culture[item.LabelKey];
                sb.Append("<li><a class=\"sch-sidebar__link");
                if (active)
                {
                    sb.Append(" is-active");
                }

                sb.Append("\" href=\"");
                sb.Append(WebUtility.HtmlEncode(item.Href));
                sb.Append('"');
                if (active)
                {
                    sb.Append(" aria-current=\"page\"");
                }

                sb.Append('>');
                sb.Append(WebUtility.HtmlEncode(label));
                sb.Append("</a></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<div class=\"sch-sidebar__footer\">");
        if (shell.ShellKind == ScholenPortalShell.Kind.Teacher)
        {
            sb.Append("<p class=\"sch-sidebar__note\">");
            sb.Append(WebUtility.HtmlEncode(culture["School.Footer.NoNames"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<p class=\"sch-sidebar__help\">");
            sb.Append(WebUtility.HtmlEncode(culture["School.Nav.Help"]));
            sb.Append("</p><p class=\"sch-sidebar__note\">");
            sb.Append(WebUtility.HtmlEncode(culture["School.Footer.NoNames"]));
            sb.Append("</p>");
        }

        sb.Append("</div></aside>");
    }

    private static void AppendBottomNav(StringBuilder sb, ScholenPortalShell shell, CultureState culture)
    {
        sb.Append("<nav class=\"sch-bottom-nav\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Main"]));
        sb.Append("\">");
        foreach (var item in shell.BottomNav)
        {
            var active = shell.ShellKind == ScholenPortalShell.Kind.Teacher
                ? ScholenNavRouting.IsTeacherBottomNavActive(
                    shell.CurrentPath, item.LabelKey, item.Href, shell.CurrentClassId)
                : ScholenNavRouting.IsSchoolBottomNavActive(shell.CurrentPath, item.LabelKey, item.Href);
            var label = BottomNavLabel(culture, item.LabelKey);
            if (shell.ShellKind == ScholenPortalShell.Kind.Teacher
                && string.Equals(item.LabelKey, "Leraar.Nav.More", StringComparison.Ordinal))
            {
                sb.Append("<details class=\"sch-bottom-nav__more-wrap\"><summary class=\"sch-bottom-nav__item sch-bottom-nav__item--btn");
                if (active)
                {
                    sb.Append(" is-active");
                }

                sb.Append("\">");
                sb.Append(WebUtility.HtmlEncode(label));
                sb.Append("</summary><div class=\"sch-teacher-meer__list sch-bottom-nav__more-panel\">");
                foreach (var more in shell.TeacherMoreItems)
                {
                    sb.Append("<a class=\"sch-teacher-meer__link\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(more.Href));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(culture[more.LabelKey]));
                    sb.Append("</a>");
                }

                sb.Append("</div></details>");
                continue;
            }

            sb.Append("<a class=\"sch-bottom-nav__item");
            if (active)
            {
                sb.Append(" is-active");
            }

            sb.Append("\" href=\"");
            sb.Append(WebUtility.HtmlEncode(item.Href));
            sb.Append('"');
            if (active)
            {
                sb.Append(" aria-current=\"page\"");
            }

            sb.Append('>');
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</a>");
        }

        sb.Append("</nav>");
    }

    private static string BottomNavLabel(CultureState culture, string labelKey)
    {
        var shortKey = labelKey + ".Short";
        var shortLabel = culture[shortKey];
        return string.Equals(shortLabel, shortKey, StringComparison.Ordinal) ? culture[labelKey] : shortLabel;
    }
}
