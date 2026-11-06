using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Web.Passport;

internal static class PassportSsrEmployerPreviewHtml
{
    internal static void Append(
        StringBuilder sb,
        CultureState culture,
        MeProfile profile,
        PassportSsrProofHtml.Panel? proof)
    {
        if (proof is null)
        {
            return;
        }

        var verified = proof.VerifiedCompetences;
        var employers = profile.Preferences?.Employers ?? [];
        var confirmedIds = proof.RefRows
            .Where(r => string.Equals(r.DisplayStatus, "confirmed", StringComparison.OrdinalIgnoreCase) && r.Active)
            .Select(r => r.WorkExperienceEntryId)
            .ToHashSet();
        var confirmedWork = employers
            .Where(e => e.EntryId is Guid id && confirmedIds.Contains(id))
            .Take(3)
            .ToList();
        if (verified.Count == 0 && confirmedWork.Count == 0)
        {
            return;
        }

        string? recognitionLine = verified.Count > 0 ? verified[0].RecognitionLine : null;
        var askHref = proof.FirstEmployerEntryId is Guid entryId
            ? $"/candidate/paspoort/referentie/vraag/{entryId:D}"
            : "/candidate/paspoort?tab=references";

        sb.Append("<section class=\"ref-verif-employer-preview detail-card\" data-testid=\"passport-employer-preview\">");
        sb.Append("<p class=\"ref-verif-employer-preview__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.EmployerPreview.CompactTitle"]));
        sb.Append("</p>");
        if (verified.Count > 0)
        {
            sb.Append("<div class=\"ref-verif-employer-preview__chips\" role=\"list\">");
            for (var i = 0; i < verified.Count; i++)
            {
                var item = verified[i];
                sb.Append("<span class=\"ref-verif-employer-preview__chip\" role=\"listitem\" style=\"--ref-verif-chip-i: ");
                sb.Append(i);
                sb.Append("\" title=\"");
                sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedBadge"]));
                sb.Append("\"><span class=\"ref-verif-employer-preview__check\" aria-hidden=\"true\">✓</span><span class=\"ref-verif-employer-preview__chip-label\">");
                sb.Append(WebUtility.HtmlEncode(item.Label));
                sb.Append("</span></span>");
            }

            sb.Append("</div>");
            if (!string.IsNullOrWhiteSpace(recognitionLine))
            {
                sb.Append("<p class=\"ref-verif-employer-preview__line\">");
                sb.Append(WebUtility.HtmlEncode(recognitionLine));
                sb.Append("</p>");
            }
        }

        if (confirmedWork.Count > 0)
        {
            sb.Append("<ul class=\"ref-verif-employer-preview__work\">");
            foreach (var work in confirmedWork)
            {
                sb.Append("<li><strong>");
                sb.Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(work.Role) ? work.EmployerName : work.Role));
                sb.Append("</strong><span class=\"ref-verif-experience-badge\"><span aria-hidden=\"true\">✓</span> ");
                sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedBadge"]));
                sb.Append("</span><span class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(work.EmployerName));
                sb.Append(" · ");
                sb.Append(WebUtility.HtmlEncode(FormatRange(culture, work.StartMonth, work.EndMonth, work.IsCurrent == true)));
                sb.Append("</span></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("<p class=\"ref-verif-disclaimer\">");
        sb.Append(WebUtility.HtmlEncode(culture["RefVerif.VerifiedCompetences.Disclaimer"]));
        sb.Append("</p>");
        if (proof.HasConfirmedReference)
        {
            sb.Append("<p class=\"ref-verif-employer-preview__footer-link\"><a href=\"");
            sb.Append(WebUtility.HtmlEncode(askHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["RefVerif.Tip.AskAnother"]));
            sb.Append("</a></p>");
        }

        sb.Append("</section>");
    }

    private static string FormatRange(CultureState culture, string? start, string? end, bool isCurrent)
    {
        static string Year(string? m)
            => string.IsNullOrWhiteSpace(m) ? "" : m.Length >= 4 ? m[..4] : m;

        var s = Year(start);
        var e = isCurrent
            ? culture["Profile.EmployerPresent"]
            : string.IsNullOrWhiteSpace(end) ? "" : Year(end);
        if (string.IsNullOrEmpty(s))
        {
            return e;
        }

        return string.IsNullOrEmpty(e) ? s : s + " - " + e;
    }
}
