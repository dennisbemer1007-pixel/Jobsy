using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Uwv;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.UwvFeatures;

namespace Jobsy.Web.UwvMockups;

internal static class AdviseurWerkSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AdviseurWerkSsrData data,
        IUwvKansberoepenCatalog kansCatalog)
    {
        if (!data.UwvEnabled)
        {
            return $"<div class=\"panel-page\"><p class=\"state-message\">{WebUtility.HtmlEncode(culture["Common.Error"])}</p></div>";
        }

        var lang = data.Lang;
        var sb = new StringBuilder(8192);
        sb.Append("<div class=\"journey-page panel-page panel-page--cand-ill uwv-adviseur-page\">");
        sb.Append("<section class=\"uwv-adviseur uwv-adviseur--full opl-wrap\" aria-labelledby=\"uwv-adviseur-title\" data-testid=\"uwv-adviseur-share\" data-uwv-adviseur-panel=\"1\">");
        var boot = JsonSerializer.Serialize(new
        {
            shareUrl = "/api/uwv-client/adviseur-share",
            lang = UwvMockupCopy.IsEnglish(lang) ? "en" : "nl",
            pickOneMessage = UwvMockupCopy.IsEnglish(lang) ? "Select at least one section." : "Kies minimaal één onderdeel.",
            errorMessage = culture["Common.Error"],
        });
        JsonScriptEmbed.Append(sb, "uwv-adviseur-boot", boot);
        sb.Append("<div class=\"uwv-adviseur__main\"><div class=\"uwv-adviseur__intro\">");
        sb.Append("<h1 id=\"uwv-adviseur-title\" class=\"uwv-block__title uwv-adviseur__h1\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.Title(lang)));
        sb.Append("</h1><p class=\"uwv-block__lead\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.Lead(lang)));
        sb.Append("</p><p class=\"uwv-adviseur__benefit muted\">");
        sb.Append(WebUtility.HtmlEncode(
            UwvMockupCopy.IsEnglish(lang)
                ? "± 2 minutes · pick sections for your meeting"
                : "± 2 minuten · kies wat je meeneemt naar het gesprek"));
        sb.Append("</p></div><fieldset class=\"uwv-adviseur__pick\"><legend>");
        sb.Append(WebUtility.HtmlEncode(
            UwvMockupCopy.IsEnglish(lang) ? "Include in printout" : "Opnemen in printout"));
        sb.Append("</legend>");
        foreach (var section in UwvAdviseurSectionCatalog.All)
        {
            var label = UwvMockupCopy.IsEnglish(lang) ? section.LabelEn : section.LabelNl;
            var on = data.DefaultSelectedSections.Contains(section.Key, StringComparer.Ordinal);
            sb.Append("<label class=\"uwv-toggle\"><input type=\"checkbox\" data-uwv-section-toggle=\"");
            sb.Append(WebUtility.HtmlEncode(section.Key));
            sb.Append('"');
            if (on)
            {
                sb.Append(" checked");
            }

            sb.Append(" /><span class=\"uwv-toggle__track\" aria-hidden=\"true\"></span><span class=\"uwv-toggle__label\">");
            sb.Append(WebUtility.HtmlEncode(label));
            sb.Append("</span></label>");
        }

        sb.Append("</fieldset></div><div class=\"uwv-adviseur__preview-wrap\"><p class=\"uwv-adviseur__preview-label\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.PreviewTitle(lang)));
        sb.Append("</p><div data-uwv-adviseur-preview=\"1\">");
        sb.Append(AdviseurWerkPreviewHtml.BuildPanelPreview(
            lang,
            data.CandidateDisplayName,
            data.DefaultSelectedSections,
            data.OndernemerOutcomeKey,
            data.DrijfverenDriverKeys,
            data.CompetenceLabels,
            kansCatalog));
        sb.Append("</div><div class=\"uwv-adviseur__actions uwv-adviseur__actions--under-preview\">");
        sb.Append("<button type=\"button\" class=\"btn btn-primary\" data-uwv-adviseur-print=\"1\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.Print(lang)));
        sb.Append("</button><button type=\"button\" class=\"btn btn-ghost\" data-uwv-adviseur-share=\"1\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.ShareLink(lang)));
        sb.Append("</button></div><div class=\"uwv-adviseur__share-url\" hidden><label class=\"muted\" for=\"uwv-share-url\">");
        sb.Append(WebUtility.HtmlEncode(
            UwvMockupCopy.IsEnglish(lang) ? "Share link (48h)" : "Deellink (48 uur)"));
        sb.Append("</label><input id=\"uwv-share-url\" class=\"uwv-adviseur__share-input\" readonly data-uwv-share-url-input=\"1\" /></div>");
        sb.Append("<p class=\"state-message error uwv-adviseur__share-error\" role=\"alert\" hidden></p>");
        sb.Append("<p class=\"uwv-adviseur__tip muted\">");
        sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.MeetingTip(lang)));
        sb.Append("</p></div></section></div>");
        return sb.ToString();
    }
}
