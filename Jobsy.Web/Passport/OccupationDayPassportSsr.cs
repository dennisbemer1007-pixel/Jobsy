using System.Net;
using System.Text;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Passport;

internal static class OccupationDayPassportSsr
{
    internal static void AppendCandidateButton(StringBuilder sb, CultureState culture, string escoId, string title)
        => AppendButton(sb, culture, escoId, title, $"/candidate/beroep/{escoId}/dag?from=paspoort");

    internal static void AppendLeerlingButton(StringBuilder sb, CultureState culture, string escoId, string title)
        => AppendButton(sb, culture, escoId, title, $"/leerling/beroep/{escoId}/dag?from=paspoort");

    private static void AppendButton(StringBuilder sb, CultureState culture, string escoId, string title, string href)
    {
        sb.Append("<a class=\"occupation-day-passport-btn\" href=\"");
        sb.Append(WebUtility.HtmlEncode(href));
        sb.Append("\" data-testid=\"occupation-day-passport-link\" data-esco-id=\"");
        sb.Append(WebUtility.HtmlEncode(escoId));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("FutureJobs.Day", MidSentenceOccupationTitle(title))));
        sb.Append(" →</a>");
    }

    /// <summary>Lowercase occupation labels in mid-sentence copy (e.g. “Een dag als kok”).</summary>
    internal static string MidSentenceOccupationTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var trimmed = title.Trim();
        if (LooksLikeProperNounOccupation(trimmed))
        {
            return trimmed;
        }

        return trimmed.ToLowerInvariant();
    }

    private static bool LooksLikeProperNounOccupation(string title)
    {
        if (title.Contains(' ') || title.Contains('-'))
        {
            return false;
        }

        // Acronyms / qualifications (IT, HBO, …) stay as written.
        return title.Length <= 4 && title.All(static c => char.IsUpper(c) || char.IsDigit(c));
    }
}
