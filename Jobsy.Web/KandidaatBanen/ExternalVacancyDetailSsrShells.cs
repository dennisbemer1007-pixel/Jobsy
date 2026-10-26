using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Web.KandidaatBanen;

internal static class ExternalVacancyDetailSsrShells
{
    internal static string BuildBody(CultureState culture, ExternalVacancyDetailDto? detail)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"kb-ext\" data-testid=\"kb-ext-detail\" data-kb-ext-detail-ssr>");
        if (detail is null)
        {
            sb.Append("<p class=\"state-message\">");
            sb.Append(WebUtility.HtmlEncode(culture["Vacancy.NotFound"]));
            sb.Append("</p></div>");
            return sb.ToString();
        }

        if (detail.IsDismissed)
        {
            sb.Append("<div class=\"kb-ext__card\" role=\"status\"><h1 class=\"kb-ext__title\">");
            sb.Append(WebUtility.HtmlEncode(detail.Title));
            sb.Append("</h1><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.DismissedLead"]));
            sb.Append("</p><a class=\"kb-ext__btn kb-ext__btn--secondary\" href=\"");
            sb.Append(WebUtility.HtmlEncode(KbRoutes.Saved));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.ViewSaved"]));
            sb.Append("</a></div></div>");
            return sb.ToString();
        }

        sb.Append("<div class=\"kb-ext__header-row\"><div class=\"kb-ext__header-copy\">");
        if (detail.IsOnSavedList)
        {
            sb.Append("<span class=\"kb-ext__badge\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Saved"]));
            sb.Append("</span>");
        }

        var title = string.IsNullOrWhiteSpace(detail.Title)
            ? culture["ExtVac.Detail.Untitled"]
            : detail.Title.Trim();
        sb.Append("<h1 class=\"kb-ext__title\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</h1>");
        var subtitleParts = new[] { detail.CompanyName, detail.Place }
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p!.Trim())
            .ToList();
        if (subtitleParts.Count > 0)
        {
            sb.Append("<p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(string.Join(" · ", subtitleParts)));
            sb.Append("</p>");
        }

        sb.Append("</div></div>");

        if (detail.RequirementsBullets.Count > 0)
        {
            sb.Append("<section class=\"kb-ext__card\"><h2 class=\"kb-ext__method-title\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Requirements"]));
            sb.Append("</h2><ul class=\"kb-ext__bullets\">");
            foreach (var bullet in detail.RequirementsBullets)
            {
                sb.Append("<li>");
                sb.Append(WebUtility.HtmlEncode(bullet));
                sb.Append("</li>");
            }

            sb.Append("</ul></section>");
        }

        if (detail.TravelMinutesEstimate is int travel && travel > 0)
        {
            sb.Append("<p class=\"kb-ext__travel\" data-testid=\"kb-ext-travel\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("ExtVac.Detail.TravelKnown", travel)));
            sb.Append("</p>");
        }
        else
        {
            sb.Append("<p class=\"kb-ext__travel kb-ext__travel--hint muted\" data-testid=\"kb-ext-travel-hint\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.TravelNeedAddress"]));
            sb.Append("</p>");
        }

        sb.Append("<section class=\"kb-ext__card\"><h2 class=\"kb-ext__method-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Facts"]));
        sb.Append("</h2><table class=\"kb-ext__facts\"><tbody>");
        foreach (var row in FactRows(culture, detail))
        {
            sb.Append("<tr><th scope=\"row\">");
            sb.Append(WebUtility.HtmlEncode(row.Label));
            sb.Append("</th><td");
            if (row.Unknown)
            {
                sb.Append(" class=\"is-unknown\"");
            }

            sb.Append('>');
            sb.Append(WebUtility.HtmlEncode(row.Value));
            sb.Append("</td></tr>");
        }

        sb.Append("</tbody></table></section>");

        var insights = CandidateExternalVacancyRules.FilterMatchInsightsForDisplay(detail.MatchInsights);
        var strengths = insights.Strengths;
        var challenges = insights.Challenges;
        if (strengths.Count > 0 || challenges.Count > 0)
        {
            sb.Append("<section class=\"kb-ext__card\"><div class=\"kb-ext__match\">");
            if (strengths.Count > 0)
            {
                sb.Append("<div class=\"kb-ext__match-panel kb-ext__match-panel--good\"><h3>");
                sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Strengths"]));
                sb.Append("</h3><ul class=\"kb-ext__bullets\">");
                foreach (var s in strengths)
                {
                    sb.Append("<li>");
                    sb.Append(WebUtility.HtmlEncode(s));
                    sb.Append("</li>");
                }

                sb.Append("</ul></div>");
            }

            if (challenges.Count > 0)
            {
                sb.Append("<div class=\"kb-ext__match-panel\"><h3>");
                sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Challenges"]));
                sb.Append("</h3><ul class=\"kb-ext__bullets\">");
                foreach (var c in challenges)
                {
                    sb.Append("<li>");
                    sb.Append(WebUtility.HtmlEncode(c));
                    sb.Append("</li>");
                }

                sb.Append("</ul></div>");
            }

            sb.Append("</div><p class=\"kb-ext__privacy\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Privacy"]));
            sb.Append("</p></section>");
        }

        if (!string.IsNullOrWhiteSpace(detail.SourceUrl))
        {
            sb.Append("<p class=\"muted kb-ext__source\" data-testid=\"kb-ext-source\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Source"]));
            sb.Append(": <a href=\"");
            sb.Append(WebUtility.HtmlEncode(detail.SourceUrl));
            sb.Append("\" rel=\"noopener noreferrer\" target=\"_blank\">");
            sb.Append(WebUtility.HtmlEncode(SourceLinkLabel(detail.SourceUrl)));
            sb.Append("</a></p>");
        }

        sb.Append("<div class=\"kb-ext__actions\" data-testid=\"kb-ext-detail-actions\">");
        if (detail.CanApply)
        {
            sb.Append("<a class=\"kb-ext__btn\" href=\"");
            sb.Append(WebUtility.HtmlEncode(KbRoutes.ExternalVacancyApply(detail.Id)));
            sb.Append("\" data-testid=\"kb-ext-apply-cta\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["ExtVac.Detail.Apply"]));
            sb.Append("</a>");
        }

        sb.Append("<div class=\"kb-ext__actions-row\"><a class=\"kb-ext__btn kb-ext__btn--secondary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(KbRoutes.Saved));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.ViewSaved"]));
        sb.Append("</a></div></div></div>");
        return sb.ToString();
    }

    private static IEnumerable<(string Label, string Value, bool Unknown)> FactRows(
        CultureState culture,
        ExternalVacancyDetailDto detail)
    {
        var keys = new[] { "bedrijf", "plaats", "uren", "salaris", "start", "opleiding" };
        foreach (var key in keys)
        {
            detail.StructuredFacts.TryGetValue(key, out var value);
            var unknown = string.IsNullOrWhiteSpace(value);
            var labelKey = $"ExtVac.Fact.{key}";
            yield return (culture[labelKey], unknown ? culture["ExtVac.Detail.Unknown"] : value!, unknown);
        }
    }

    private static string SourceLinkLabel(string sourceUrl)
    {
        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                host = host[4..];
            }

            return host;
        }

        return sourceUrl.Trim();
    }
}
