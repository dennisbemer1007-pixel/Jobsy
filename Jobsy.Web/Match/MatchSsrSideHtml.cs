using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Match;

internal static class MatchSsrSideHtml
{
    internal static void AppendPanels(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<SwipeViewModel> items,
        int startIndex,
        bool occupationDayInLifeEnabled,
        MatchProfileGateViewModel gate)
    {
        sb.Append("<div class=\"match-page__side-panels\" data-match-side-panels>");
        for (var i = 0; i < items.Count; i++)
        {
            AppendSide(sb, culture, items[i], i, i == startIndex, occupationDayInLifeEnabled, gate);
        }

        sb.Append("</div>");
    }

    private static void AppendSide(
        StringBuilder sb,
        CultureState culture,
        SwipeViewModel model,
        int index,
        bool visible,
        bool occupationDayInLifeEnabled,
        MatchProfileGateViewModel gate)
    {
        sb.Append("<aside class=\"match-page__side\" data-match-side data-match-side-index=\"");
        sb.Append(index);
        sb.Append("\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.WhyYouFit"]));
        sb.Append('"');
        if (!visible)
        {
            sb.Append(" hidden");
        }

        sb.Append('>');
        AppendWhyCard(sb, culture, model, gate);
        AppendInfoCard(sb, culture, model, occupationDayInLifeEnabled);
        AppendSavedCard(sb, culture);
        sb.Append("</aside>");
    }

    private static void AppendWhyCard(StringBuilder sb, CultureState culture, SwipeViewModel model, MatchProfileGateViewModel gate)
    {
        var dnaRows = MatchDnaRows.Build(model.FitDimensions, model.FitWhyKinds, includeMissing: true, maxWithData: 4, gate);
        sb.Append("<section class=\"match-side-card\" data-testid=\"match-side-why\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Match.WhyYouFit"]));
        sb.Append("</h2>");
        if (dnaRows.Count > 0)
        {
            sb.Append("<ul class=\"kb-dna kb-dna--match kb-dna--match-dialog\" role=\"list\">");
            foreach (var row in dnaRows)
            {
                sb.Append("<li class=\"kb-dna__row\" data-dna=\"");
                sb.Append(WebUtility.HtmlEncode(row.Kind));
                sb.Append("\"><span class=\"kb-dna__label\">");
                sb.Append(WebUtility.HtmlEncode(culture[row.LabelKey]));
                sb.Append("</span>");
                if (row.Percent is int barPct)
                {
                    sb.Append("<span class=\"kb-dna__band\" aria-label=\"");
                    sb.Append(WebUtility.HtmlEncode(culture[row.LabelKey]));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(CandidateMatchDisplay.FitBandLabel(culture, barPct)));
                    sb.Append("</span><span class=\"kb-dna__reason\">");
                    sb.Append(WebUtility.HtmlEncode(culture[row.WhyKey]));
                    sb.Append("</span>");
                }
                else
                {
                    sb.Append("<a class=\"kb-dna__todo\" href=\"");
                    sb.Append(WebUtility.HtmlEncode(row.TestHref));
                    sb.Append("\" data-enhance-nav=\"false\">");
                    sb.Append(WebUtility.HtmlEncode(culture["Test.Status.NotDone"]));
                    sb.Append("</a>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }
        else if (!string.IsNullOrWhiteSpace(model.WhyYouFit))
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(model.WhyYouFit));
            sb.Append("</p>");
        }

        sb.Append("</section>");
    }

    private static void AppendInfoCard(
        StringBuilder sb,
        CultureState culture,
        SwipeViewModel model,
        bool occupationDayInLifeEnabled)
    {
        sb.Append("<section class=\"match-side-card match-side-card--info\" data-testid=\"match-side-more\"><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["Match.MoreInfo"]));
        sb.Append("</h2><dl class=\"match-side-facts\"><div><dt>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Detail.TravelTitle"]));
        sb.Append("</dt><dd>");
        MatchSsrTravelHtml.Append(sb, culture, model.TravelTimeMinutes, model.TransportMode, model.TravelToBureau);
        sb.Append("</dd></div><div><dt>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Detail.HoursFact"]));
        sb.Append("</dt><dd>");
        if (!string.IsNullOrWhiteSpace(model.HoursPrimary))
        {
            sb.Append(WebUtility.HtmlEncode(model.HoursPrimary));
            sb.Append(' ');
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.HoursUnit"]));
        }
        else
        {
            sb.Append('\u2014');
        }

        sb.Append("</dd></div><div><dt>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Detail.WageFact"]));
        sb.Append("</dt><dd>");
        if (!string.IsNullOrWhiteSpace(model.SalaryPrimary))
        {
            sb.Append(WebUtility.HtmlEncode(model.SalaryPrimary));
            if (!string.IsNullOrWhiteSpace(model.SalarySecondary))
            {
                sb.Append(' ');
                sb.Append(WebUtility.HtmlEncode(model.SalarySecondary));
            }
        }
        else
        {
            sb.Append('\u2014');
        }

        sb.Append("</dd></div>");

        sb.Append("<div><dt>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Detail.StartFact"]));
        sb.Append("</dt><dd>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Detail.StartFlexible"]));
        sb.Append("</dd></div></dl>");
        if (MatchOccupationDayResolve.TryBuild(culture, occupationDayInLifeEnabled, model.EscoId, model.JobTitle) is { } dayLink)
        {
            sb.Append("<p class=\"day-in-life-link\"><a href=\"");
            sb.Append(WebUtility.HtmlEncode(dayLink.Href));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(dayLink.LinkText));
            sb.Append("</a></p>");
        }

        sb.Append("<div class=\"match-side-links\">");
        if (model.VacancyId is Guid id)
        {
            sb.Append("<a class=\"match-side-links__link\" href=\"/vacancies/");
            sb.Append(id.ToString("D"));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Match.ViewVacancy"]));
            sb.Append("</a>");
        }

        sb.Append("</div></section>");
    }

    private static void AppendSavedCard(StringBuilder sb, CultureState culture)
    {
        sb.Append("<section class=\"match-side-card match-side-card--saved\"><p><strong>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Title"]));
        sb.Append("</strong> · ");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Saved.Lead"]));
        sb.Append("</p><a href=\"");
        sb.Append(KbRoutes.Saved);
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.ViewSaved"]));
        sb.Append("</a></section>");
    }
}
