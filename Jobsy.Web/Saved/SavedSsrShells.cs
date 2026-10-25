using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Models;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Saved;

internal static class SavedSsrShells
{
    private static readonly CultureInfo Dutch = new("nl-NL");

    internal static string BuildBody(CultureState culture, SavedSsrData.PageData data, string antiforgeryToken, HttpContext http)
    {
        if (!data.IsAuthenticatedCandidate)
        {
            return BuildGuestGate(culture, http);
        }

        if (!data.EmployersEnabled)
        {
            return BuildFeatureOff(culture);
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"panel-page kb-saved kb-saved--illustrated panel-page--cand-ill\" data-testid=\"saved-page-ssr\">");
        AppendHero(sb, culture);
        if (data.ShowJobListTabs)
        {
            AppendJobListTabs(sb, culture, data.Items.Count);
        }

        if (!string.IsNullOrWhiteSpace(data.ErrorMessage))
        {
            sb.Append("<p class=\"state-message error\" role=\"alert\">");
            sb.Append(WebUtility.HtmlEncode(data.ErrorMessage));
            sb.Append("</p>");
        }
        else if (data.ActionSaved)
        {
            sb.Append("<p class=\"state-message\" role=\"status\">");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Saved"]));
            sb.Append("</p>");
        }

        if (data.Items.Count == 0)
        {
            AppendEmpty(sb, culture);
        }
        else
        {
            AppendToolbar(sb, culture, data);
            AppendGrid(sb, culture, data, antiforgeryToken);
            AppendRail(sb, culture, data);
        }

        AppendFooterNav(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendHero(StringBuilder sb, CultureState culture)
    {
        var art = KreeftHeaderStripAssets.BewaardDesk;
        sb.Append("<header class=\"cand-hero cand-hero--profession-band cand-hero--with-art\" style=\"");
        sb.Append("--cand-hero-art:url('");
        sb.Append(WebUtility.HtmlEncode(art));
        sb.Append("');--cand-hero-pos:right center;\"><div class=\"cand-hero__copy\"><span class=\"cand-hero__eyebrow cand-hero__eyebrow--coral\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.SavedTab"]));
        sb.Append("</span><h1 tabindex=\"-1\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Title"]));
        sb.Append("</h1><p class=\"cand-hero__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Lead"]));
        sb.Append("</p></div><div class=\"cand-hero__media\" style=\"--cand-hero-art:url('");
        sb.Append(WebUtility.HtmlEncode(art));
        sb.Append("');--cand-hero-pos:right center;\" aria-hidden=\"true\"></div></header>");
    }

    private static void AppendJobListTabs(StringBuilder sb, CultureState culture, int savedCount)
    {
        sb.Append("<nav class=\"candidate-job-list-tabs admin-sublinks\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Applications"]));
        sb.Append("\"><a class=\"admin-sublink\" href=\"/candidate/applications\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><a class=\"admin-sublink admin-sublink--active\" href=\"/candidate/liked\" aria-current=\"page\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.SavedTab"]));
        if (savedCount > 0)
        {
            sb.Append("<span class=\"candidate-job-list-tabs__count\" aria-hidden=\"true\">");
            sb.Append(savedCount);
            sb.Append("</span>");
        }

        sb.Append("</a><a class=\"admin-sublink\" href=\"/candidate/shared\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.TabShared"]));
        sb.Append("</a></nav>");
    }

    private static void AppendEmpty(StringBuilder sb, CultureState culture)
    {
        var spot = CandidateIllustratedImages.SpotKaart;
        sb.Append("<div class=\"kb-saved__empty-layout\"><div class=\"cand-empty cand-empty--apps-prominent\" style=\"--cand-empty-art:url('");
        sb.Append(WebUtility.HtmlEncode(spot));
        sb.Append("');\"><h2 class=\"cand-empty__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Title"]));
        sb.Append("</h2><p class=\"cand-empty__lead\">");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.EmptyLikes"]));
        sb.Append("</p><div class=\"cand-empty__actions\">");
        sb.Append("<a class=\"btn btn-primary cand-empty__cta\" href=\"/banenkaart\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["CandidateVacancies.BrowseMap"]));
        sb.Append("</a><a class=\"btn btn-secondary cand-empty__cta\" href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.CareerPath"]));
        sb.Append("</a></div></div></div>");
    }

    private static void AppendToolbar(StringBuilder sb, CultureState culture, SavedSsrData.PageData data)
    {
        sb.Append("<div class=\"kb-saved__toolbar\"><div class=\"kb-saved__filters\" role=\"group\">");
        AppendFilterChip(sb, data, SavedSsrQuery.FilterAll, culture["Kb.Saved.FilterAll"], data.Items.Count);
        AppendFilterChip(sb, data, SavedSsrQuery.FilterOpen, culture["Kb.Saved.FilterOpen"], data.CountOpen);
        AppendFilterChip(sb, data, SavedSsrQuery.FilterClosed, culture["Kb.Saved.FilterClosed"], data.CountClosed);
        sb.Append("</div><div class=\"kb-saved__meta\"><span class=\"kb-saved__count\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Kb.Saved.Count"], data.Filtered.Count)));
        sb.Append("</span><nav class=\"kb-saved__sort\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Filter.Sort"]));
        sb.Append("\">");
        AppendSortLink(sb, data, SavedSsrQuery.SortNewest, culture["Kb.Saved.SortNewest"]);
        AppendSortLink(sb, data, SavedSsrQuery.SortFit, culture["Kb.Saved.SortBestFit"]);
        AppendSortLink(sb, data, SavedSsrQuery.SortClosing, culture["Kb.Saved.SortClosing"]);
        sb.Append("</nav></div></div>");
    }

    private static void AppendFilterChip(
        StringBuilder sb,
        SavedSsrData.PageData data,
        string filter,
        string label,
        int count)
    {
        var active = string.Equals(data.Filter, filter, StringComparison.Ordinal);
        sb.Append("<a class=\"kb-chip");
        if (active)
        {
            sb.Append(" is-selected");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(SavedSsrQuery.BuildUrl(filter, data.Sort)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append(" · ");
        sb.Append(count);
        sb.Append("</a>");
    }

    private static void AppendSortLink(StringBuilder sb, SavedSsrData.PageData data, string sort, string label)
    {
        var active = string.Equals(data.Sort, sort, StringComparison.Ordinal);
        sb.Append("<a class=\"kb-saved__sort-link");
        if (active)
        {
            sb.Append(" is-active");
        }

        sb.Append("\" href=\"");
        sb.Append(WebUtility.HtmlEncode(SavedSsrQuery.BuildUrl(data.Filter, sort)));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</a>");
    }

    private static void AppendGrid(StringBuilder sb, CultureState culture, SavedSsrData.PageData data, string antiforgeryToken)
    {
        sb.Append("<div class=\"kb-saved__body\">");
        if (data.Filtered.Count == 0)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.FilterEmpty"]));
            sb.Append("</p>");
        }

        sb.Append("<div class=\"kb-saved__grid\">");
        foreach (var item in data.Filtered)
        {
            AppendItem(sb, culture, data, item, antiforgeryToken);
        }

        sb.Append("</div></div>");
    }

    private static void AppendItem(
        StringBuilder sb,
        CultureState culture,
        SavedSsrData.PageData data,
        CandidateEngagementItem item,
        string antiforgeryToken)
    {
        var closed = SavedSsrData.IsClosed(item);
        var rowHref = closed ? SimilarHref(item) : $"/vacancies/{item.VacancyId:D}";
        var companyPlace = string.IsNullOrWhiteSpace(item.LocationLabel)
            ? item.CompanyName
            : $"{item.CompanyName} · {item.LocationLabel}";

        sb.Append("<article class=\"kb-saved-row");
        if (closed)
        {
            sb.Append(" kb-saved-row--closed");
        }

        sb.Append("\"><div class=\"kb-saved-row__thumb\">");
        AppendPhoto(sb, item, "kb-saved-row__photo", "72px", 72, 72);
        sb.Append("</div><div class=\"kb-saved-row__body\"><h2 class=\"kb-saved-row__title\"><a class=\"kb-saved-row__link\" href=\"");
        sb.Append(WebUtility.HtmlEncode(rowHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(item.VacancyTitle));
        sb.Append("</a></h2><p class=\"kb-saved-row__place\">");
        sb.Append(WebUtility.HtmlEncode(companyPlace));
        sb.Append("</p>");
        AppendFitTags(sb, culture, item, row: true);
        sb.Append("</div>");
        AppendUnsaveForm(sb, culture, data, item, antiforgeryToken, "kb-saved-row__heart", heart: true);
        sb.Append("</article>");

        sb.Append("<article class=\"kb-saved-card");
        if (closed)
        {
            sb.Append(" kb-saved-card--closed");
        }

        sb.Append("\"><div class=\"kb-saved-card__media");
        if (string.IsNullOrWhiteSpace(item.ImageUrl) && string.IsNullOrWhiteSpace(item.CompanyLogoUrl))
        {
            sb.Append(" kb-saved-card__media--illustrated\" style=\"--kb-saved-illustrated:url('");
            sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SavedNoPhotoArt));
            sb.Append("')");
        }

        sb.Append("\"><a class=\"kb-saved-card__media-link\" href=\"/vacancies/");
        sb.Append(item.VacancyId.ToString("D"));
        sb.Append("\" data-enhance-nav=\"false\">");
        AppendPhoto(sb, item, "kb-saved-card__photo", "(max-width: 1024px) 33vw, 25vw", 0, 0);
        sb.Append("</a>");
        AppendSavedState(sb, culture, item);
        AppendUnsaveForm(sb, culture, data, item, antiforgeryToken, "kb-saved-card__heart", heart: true);
        sb.Append("</div><div class=\"kb-saved-card__body\"><h2 class=\"kb-saved-card__title\"><a href=\"/vacancies/");
        sb.Append(item.VacancyId.ToString("D"));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(item.VacancyTitle));
        sb.Append("</a></h2><p class=\"kb-saved-card__company\">");
        sb.Append(WebUtility.HtmlEncode(companyPlace));
        sb.Append("</p><p class=\"kb-saved-card__meta\">");
        if (item.HoursPerWeekMin.HasValue || item.HoursPerWeekMax.HasValue)
        {
            sb.Append("<span>");
            sb.Append(WebUtility.HtmlEncode(FormatHours(item.HoursPerWeekMin, item.HoursPerWeekMax)));
            sb.Append("</span>");
        }

        sb.Append("<span>");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Kb.Saved.SavedOn"], ShortDate(culture, item.CreatedAt))));
        sb.Append("</span></p>");
        AppendFitTags(sb, culture, item, row: false);
        sb.Append("</div><div class=\"kb-saved-card__footer\">");
        if (closed)
        {
            sb.Append("<a class=\"kb-saved-card__action\" href=\"");
            sb.Append(WebUtility.HtmlEncode(SimilarHref(item)));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Similar"]));
            sb.Append("</a>");
            AppendUnsaveForm(sb, culture, data, item, antiforgeryToken, "kb-saved-card__remove", heart: false, labelKey: "Kb.Saved.Remove");
        }
        else if (item.HasApplied)
        {
            sb.Append("<span class=\"kb-saved-card__applied\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Applied"]));
            sb.Append("</span><a class=\"kb-saved-card__link\" href=\"");
            sb.Append(KbRoutes.Applications);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.View"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<a class=\"btn btn-secondary kb-saved-card__apply\" href=\"/vacancies/");
            sb.Append(item.VacancyId.ToString("D"));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Apply"]));
            sb.Append("</a><a class=\"kb-saved-card__link\" href=\"/vacancies/");
            sb.Append(item.VacancyId.ToString("D"));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.View"]));
            sb.Append("</a>");
        }

        sb.Append("</div></article>");
    }

    private static void AppendPhoto(
        StringBuilder sb,
        CandidateEngagementItem item,
        string cssClass,
        string sizes,
        int width,
        int height)
    {
        var src = !string.IsNullOrWhiteSpace(item.ImageUrl) ? item.ImageUrl : item.CompanyLogoUrl;
        sb.Append("<img class=\"");
        sb.Append(cssClass);
        sb.Append("\" src=\"");
        sb.Append(WebUtility.HtmlEncode(src ?? ""));
        sb.Append("\" alt=\"\" loading=\"lazy\" decoding=\"async\" sizes=\"");
        sb.Append(WebUtility.HtmlEncode(sizes));
        sb.Append('"');
        if (width > 0)
        {
            sb.Append(" width=\"");
            sb.Append(width);
            sb.Append("\" height=\"");
            sb.Append(height);
            sb.Append('"');
        }

        sb.Append(" />");
    }

    private static void AppendFitTags(StringBuilder sb, CultureState culture, CandidateEngagementItem item, bool row)
    {
        if (row)
        {
            sb.Append("<div class=\"kb-saved-row__tags\">");
        }

        if (item.FitGateClosed)
        {
            sb.Append("<a class=\"kb-fit kb-fit--gate");
            if (row)
            {
                sb.Append(" kb-saved-row__fit");
            }

            sb.Append("\" href=\"");
            sb.Append(KbRoutes.PaspoortTests);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Fit.Gate"]));
            sb.Append("</a>");
        }
        else if (Enum.TryParse<KbFitBand>(item.FitBand, ignoreCase: true, out var band))
        {
            sb.Append("<span class=\"kb-fit kb-fit--");
            sb.Append(KbLabels.FitBandCss(band));
            if (row)
            {
                sb.Append(" kb-saved-row__fit");
            }

            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(KbLabels.FitBand(culture, band)));
            sb.Append("</span>");
        }

        if (!string.IsNullOrWhiteSpace(item.SavedStateKind)
            && !string.Equals(item.SavedStateKind, nameof(KbSavedJobStateKind.Hidden), StringComparison.Ordinal))
        {
            sb.Append("<span class=\"");
            sb.Append(row ? "kb-saved-row__status kb-saved-row__status--" : "kb-saved-card__state kb-saved-card__state--");
            sb.Append(KbLabels.SavedStateCss(item.SavedStateKind));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(KbLabels.SavedStateLabel(culture, item.SavedStateKind, item.DaysUntilEnd)));
            sb.Append("</span>");
        }

        if (item.HasApplied)
        {
            sb.Append("<span class=\"");
            sb.Append(row ? "kb-saved-row__applied" : "kb-saved-card__applied");
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Applied"]));
            sb.Append("</span>");
        }

        if (row)
        {
            sb.Append("</div>");
        }
    }

    private static void AppendSavedState(StringBuilder sb, CultureState culture, CandidateEngagementItem item)
    {
        if (string.IsNullOrWhiteSpace(item.SavedStateKind)
            || string.Equals(item.SavedStateKind, nameof(KbSavedJobStateKind.Hidden), StringComparison.Ordinal))
        {
            return;
        }

        sb.Append("<span class=\"kb-saved-card__state kb-saved-card__state--");
        sb.Append(KbLabels.SavedStateCss(item.SavedStateKind));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(KbLabels.SavedStateLabel(culture, item.SavedStateKind, item.DaysUntilEnd)));
        sb.Append("</span>");
    }

    private static void AppendUnsaveForm(
        StringBuilder sb,
        CultureState culture,
        SavedSsrData.PageData data,
        CandidateEngagementItem item,
        string antiforgeryToken,
        string cssClass,
        bool heart,
        string? labelKey = null)
    {
        sb.Append("<form method=\"post\" action=\"/api/saved-client/unsave\" class=\"");
        sb.Append(cssClass);
        sb.Append(" kb-saved__inline-form\" data-enhance=\"false\" data-enhance-nav=\"false\" data-testid=\"");
        sb.Append(UnsaveTestId(cssClass, item.VacancyId));
        sb.Append("\">");
        sb.Append("<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"");
        sb.Append(WebUtility.HtmlEncode(antiforgeryToken));
        sb.Append("\" /><input type=\"hidden\" name=\"vacancyId\" value=\"");
        sb.Append(item.VacancyId.ToString("D"));
        sb.Append("\" /><input type=\"hidden\" name=\"filter\" value=\"");
        sb.Append(WebUtility.HtmlEncode(data.Filter));
        sb.Append("\" /><input type=\"hidden\" name=\"sort\" value=\"");
        sb.Append(WebUtility.HtmlEncode(data.Sort));
        sb.Append("\" /><button type=\"submit\" class=\"");
        sb.Append(cssClass);
        sb.Append('"');
        sb.Append(" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Unsave"]));
        sb.Append("\">");
        if (heart)
        {
            sb.Append('\u2665');
        }
        else
        {
            sb.Append(WebUtility.HtmlEncode(culture[labelKey ?? "Kb.Saved.Remove"]));
        }

        sb.Append("</button></form>");
    }

    private static void AppendRail(StringBuilder sb, CultureState culture, SavedSsrData.PageData data)
    {
        sb.Append("<aside class=\"kb-saved-rail\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.TabShared"]));
        sb.Append("\"><section class=\"kb-saved-rail__card cand-side-card--spot\" style=\"--cand-side-spot:url('");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SpotKaart));
        sb.Append("');\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.TabShared"]));
        sb.Append("</h2>");
        if (data.SharedGroups.Count == 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["Saved.EmptyShared"]));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<ul class=\"kb-saved-rail__list\">");
            foreach (var group in data.SharedGroups.Take(5))
            {
                sb.Append("<li><a href=\"/vacancies/");
                sb.Append(group.VacancyId.ToString("D"));
                sb.Append("\" data-enhance-nav=\"false\"><strong>");
                sb.Append(WebUtility.HtmlEncode(group.VacancyTitle));
                sb.Append("</strong><span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(SharedWithLabel(culture, group.ShareCount)));
                sb.Append(" · ");
                sb.Append(WebUtility.HtmlEncode(group.LastSharedAt.ToLocalTime().ToString("d MMM", Dutch)));
                sb.Append("</span></a></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("</section>");
        if (data.ExternalVacanciesEnabled)
        {
            sb.Append("<section class=\"kb-saved-rail__card cand-side-card--spot\" style=\"--cand-side-spot:url('");
            sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SpotWerkgever));
            sb.Append("');\"><h2>");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Saved.Title"]));
            sb.Append("</h2><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Saved.Lead"]));
            sb.Append("</p><a class=\"btn btn-secondary\" href=\"");
            sb.Append(KbRoutes.ExternalVacancyAdd);
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Saved.Add"]));
            sb.Append("</a></section>");
        }

        sb.Append("</aside>");
    }

    private static void AppendFooterNav(StringBuilder sb, CultureState culture)
    {
        sb.Append("<nav class=\"passport-ssr-footer-nav saved-ssr-footer-nav\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Title"]));
        sb.Append("\" data-testid=\"saved-footer-nav\"><a href=\"");
        sb.Append(OnboardingRoutes.KompasHubPath);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Title"]));
        sb.Append("</a><a href=\"/carriere\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.CareerPath"]));
        sb.Append("</a><a href=\"/candidate/applications\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.Applications"]));
        sb.Append("</a></nav>");
    }

    private static string BuildGuestGate(CultureState culture, HttpContext http)
    {
        var returnUrl = Uri.EscapeDataString("/candidate/liked");
        var sb = new StringBuilder();
        sb.Append("<div class=\"saved-gate\" data-testid=\"saved-gate\"><header class=\"saved-gate__header\"><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Title"]));
        sb.Append("</h1></header><div class=\"saved-gate__body\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.Headline"]));
        sb.Append("</h2><p>");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.Lead"]));
        sb.Append("</p><div class=\"saved-gate__actions\"><a class=\"provider-btn\" href=\"/login?returnUrl=");
        sb.Append(returnUrl);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Saved.ContinueEmail"]));
        sb.Append("</a></div></div></div>");
        _ = http;
        return sb.ToString();
    }

    private static string BuildFeatureOff(CultureState culture)
    {
        var sb = new StringBuilder();
        sb.Append("<p class=\"state-message\">");
        sb.Append(WebUtility.HtmlEncode(culture["Panel.ErrorInline"]));
        sb.Append("</p>");
        return sb.ToString();
    }

    private static string UnsaveTestId(string cssClass, Guid vacancyId)
        => cssClass.Contains("row", StringComparison.Ordinal)
            ? $"saved-unsave-row-{vacancyId:D}"
            : cssClass.Contains("card", StringComparison.Ordinal)
                ? $"saved-unsave-card-{vacancyId:D}"
                : $"saved-unsave-remove-{vacancyId:D}";

    private static string SimilarHref(CandidateEngagementItem item)
        => item.CategoryId is Guid cat
            ? $"{KbRoutes.Map}?categorie={cat:D}"
            : KbRoutes.Map;

    private static string SharedWithLabel(CultureState culture, int count)
        => count <= 1
            ? culture["Saved.SharedWithOne"]
            : culture.Format("Saved.SharedWithMany", count);

    private static string FormatHours(int? min, int? max)
    {
        if (min is int a && max is int b && a != b)
        {
            return $"{a}-{b} uur";
        }

        return $"{min ?? max} uur";
    }

    private static string ShortDate(CultureState culture, DateTime dt)
    {
        var local = dt.ToLocalTime();
        var cultureInfo = culture.Language switch
        {
            "en" => new CultureInfo("en-GB"),
            "pl" => new CultureInfo("pl-PL"),
            "ro" => new CultureInfo("ro-RO"),
            "ar" => new CultureInfo("ar-SA"),
            _ => Dutch,
        };
        return local.ToString("d MMM", cultureInfo).Replace(".", "", StringComparison.Ordinal);
    }
}
