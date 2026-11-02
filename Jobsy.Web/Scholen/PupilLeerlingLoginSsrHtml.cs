using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

/// <summary>SSR HTML for GET /leerling (login).</summary>
public static class PupilLeerlingLoginSsrHtml
{
    public static string RenderDocument(
        CultureState culture,
        IReadOnlyList<PupilSchoolOptionDto> schools,
        IReadOnlyList<PupilClassOptionDto> classes,
        Guid? schoolId,
        Guid? classId,
        string? errorCode,
        string? questionSetQuery,
        bool pupilSession,
        string? staffBlock,
        string antiforgeryHtml)
    {
        var body = RenderBody(
            culture, schools, classes, schoolId, classId, errorCode, questionSetQuery, pupilSession, staffBlock, antiforgeryHtml);
        var title = pupilSession ? culture["Leerling.Login.AlreadyTitle"] : culture["Leerling.LoginTitle"];
        return RenderLoginDocument(culture, title, body);
    }

    private static string RenderLoginDocument(CultureState culture, string pageTitle, string bodyHtml)
    {
        var styles = PageAssetCatalog.StylesheetsForPupilSsr("/leerling");
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

        sb.Append("</head><body><div class=\"ll-shell\" lang=\"");
        sb.Append(WebUtility.HtmlEncode(lang));
        sb.Append("\"><header class=\"ll-topbar\"><a class=\"ll-topbar__brand\" href=\"/leerling\">");
        sb.Append("<img class=\"ll-topbar__logo\" src=\"/");
        sb.Append(Jobsy.Web.Media.BrandImages.Webp64);
        sb.Append("\" width=\"28\" height=\"28\" alt=\"\" /><span>Lobsy</span></a></header><main class=\"ll-main\">");
        sb.Append(bodyHtml);
        sb.Append("</main><footer class=\"ll-legal\"><a href=\"");
        sb.Append(PublicRoutes.Privacy);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"");
        sb.Append(PublicRoutes.Accessibility);
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a></footer></div><script src=\"");
        sb.Append(WwwrootAssetCatalog.UrlLeadingSlash("js/leerling-login.js"));
        sb.Append("\" defer></script></body></html>");
        return sb.ToString();
    }

    private static string RenderBody(
        CultureState culture,
        IReadOnlyList<PupilSchoolOptionDto> schools,
        IReadOnlyList<PupilClassOptionDto> classes,
        Guid? schoolId,
        Guid? classId,
        string? errorCode,
        string? questionSetQuery,
        bool pupilSession,
        string? staffBlock,
        string antiforgeryHtml)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"ll-login\">");
        if (!string.IsNullOrEmpty(staffBlock))
        {
            sb.Append("<div class=\"ll-card ll-card--warn\" role=\"alert\"><p>");
            sb.Append(WebUtility.HtmlEncode(staffBlock));
            sb.Append("</p></div>");
        }
        else if (pupilSession)
        {
            sb.Append("<section class=\"ll-card\"><h1>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.AlreadyIn"]));
            sb.Append("</h1><p>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.AlreadyLead"]));
            sb.Append("</p><div class=\"ll-question__nav\"><a class=\"btn-primary\" href=\"/leerling/verder\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Continue"]));
            sb.Append("</a><a class=\"btn-secondary\" href=\"/leerling/stop\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Stop"]));
            sb.Append("</a></div></section>");
        }
        else
        {
            var error = MapError(culture, errorCode, questionSetQuery, classId, classes);
            sb.Append("<section class=\"ll-card\"><h1>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Heading"]));
            sb.Append("</h1><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Lead"]));
            sb.Append("</p>");
            if (!string.IsNullOrEmpty(error))
            {
                sb.Append("<p class=\"ll-error\" role=\"alert\">");
                sb.Append(WebUtility.HtmlEncode(error));
                sb.Append("</p>");
            }

            sb.Append("<form method=\"get\" action=\"/leerling\" class=\"ll-form ll-form--cascade\"><label class=\"ll-field\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.School"]));
            sb.Append("</span><select name=\"schoolId\" required data-leerling-school><option value=\"\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.SchoolPlaceholder"]));
            sb.Append("</option>");
            foreach (var s in schools)
            {
                sb.Append("<option value=\"");
                sb.Append(WebUtility.HtmlEncode(s.Id.ToString("D")));
                sb.Append('"');
                if (schoolId == s.Id)
                {
                    sb.Append(" selected");
                }

                sb.Append('>');
                sb.Append(WebUtility.HtmlEncode($"{s.SchoolName} · {s.City}"));
                sb.Append("</option>");
            }

            sb.Append("</select></label><button type=\"submit\" class=\"btn-secondary ll-login__choose\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.ChooseSchool"]));
            sb.Append("</button>");
            if (schools.Count == 0)
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.NoOpenSchool"]));
                sb.Append("</p>");
            }

            sb.Append("</form><form method=\"post\" action=\"/leerling/login\" class=\"ll-form\">");
            sb.Append(antiforgeryHtml);
            sb.Append("<input type=\"hidden\" name=\"schoolId\" value=\"");
            sb.Append(WebUtility.HtmlEncode(schoolId?.ToString("D") ?? ""));
            sb.Append("\" /><label class=\"ll-field\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Class"]));
            sb.Append("</span><select name=\"classId\" required");
            if (classes.Count == 0 || schoolId is null)
            {
                sb.Append(" disabled");
            }

            sb.Append("><option value=\"\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.ClassPlaceholder"]));
            sb.Append("</option>");
            foreach (var c in classes)
            {
                sb.Append("<option value=\"");
                sb.Append(WebUtility.HtmlEncode(c.Id.ToString("D")));
                sb.Append('"');
                if (classId == c.Id)
                {
                    sb.Append(" selected");
                }

                sb.Append('>');
                sb.Append(WebUtility.HtmlEncode(c.Label));
                sb.Append("</option>");
            }

            sb.Append("</select></label><label class=\"ll-field\"><span>");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Code"]));
            sb.Append("</span><input name=\"code\" type=\"text\" maxlength=\"7\" autocomplete=\"off\" autocapitalize=\"characters\" spellcheck=\"false\" inputmode=\"text\" placeholder=\"K7Q-M2P\" required /></label><p class=\"ll-privacy\">");
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.NoName"]));
            sb.Append("</p><button type=\"submit\" class=\"btn-primary ll-login__submit\"");
            if (schoolId is null)
            {
                sb.Append(" disabled");
            }

            sb.Append('>');
            sb.Append(WebUtility.HtmlEncode(culture["Leerling.Login.Start"]));
            sb.Append("</button></form></section>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string? MapError(
        CultureState culture,
        string? errorCode,
        string? questionSetQuery,
        Guid? classId,
        IReadOnlyList<PupilClassOptionDto> classes)
    {
        if (string.IsNullOrEmpty(errorCode))
        {
            return null;
        }

        var set = ResolveErrorSet(questionSetQuery, classId, classes);
        return errorCode switch
        {
            "invalid" => culture[PupilCopy.Key(set, "Leerling.Login.Error.Invalid")],
            "cooldown" => culture[PupilCopy.Key(set, "Leerling.Login.Error.Cooldown")],
            "window" => culture[PupilCopy.Key(set, "Leerling.Login.Error.Window")],
            "staff" => culture["Leerling.Login.Error.Staff"],
            "retry" => culture["Leerling.Login.Error.Retry"],
            "expired" => culture["Leerling.Login.Error.Expired"],
            _ => null
        };
    }

    private static PupilQuestionSet ResolveErrorSet(
        string? questionSetQuery,
        Guid? classId,
        IReadOnlyList<PupilClassOptionDto> classes)
    {
        if (string.Equals(questionSetQuery, "Vo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(questionSetQuery, "2", StringComparison.Ordinal))
        {
            return PupilQuestionSet.Vo;
        }

        if (string.Equals(questionSetQuery, "MboHbo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(questionSetQuery, "3", StringComparison.Ordinal))
        {
            return PupilQuestionSet.MboHbo;
        }

        if (classId is Guid cid)
        {
            foreach (var c in classes)
            {
                if (c.Id == cid)
                {
                    return c.QuestionSet;
                }
            }
        }

        return PupilQuestionSet.Groep78;
    }

    public static string StaffRoleLabel(CultureState culture, string? role) => role switch
    {
        JobsyRoles.Teacher => culture["Leraar.RoleChip"],
        JobsyRoles.SchoolAdmin => culture["School.RoleChip"],
        _ => culture["Leerling.Login.StaffOther"]
    };

    public static bool IsPupilSession(HttpContext http)
    {
        if (http.Items[Jobsy.Web.Security.LeerlingNoStoreMiddleware.PupilPrincipalItemKey] is ClaimsPrincipal stored
            && JobsyApiAuthHandler.IsPupilPrincipal(stored))
        {
            return true;
        }

        return JobsyApiAuthHandler.IsPupilPrincipal(http.User);
    }
}
