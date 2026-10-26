using System.Net;
using System.Text;
using Jobsy.Core.Uwv;
using Jobsy.Web.Media;
using Jobsy.Web.Services;
using Jobsy.Web.UwvFeatures;

namespace Jobsy.Web.UwvMockups;

/// <summary>Static HTML for the UWV adviseur A4 print preview (shared by deel SSR and adviseur panel).</summary>
public static class UwvAdviseurShareDocumentHtml
{
    public static string Build(
        string lang,
        string candidateDisplayName,
        IReadOnlyList<string> sections,
        string? ondernemerOutcomeKey,
        IReadOnlyList<string> drijfverenDriverKeys,
        IReadOnlyList<string> competenceLabels,
        IUwvKansberoepenCatalog kansberoepenCatalog,
        IReadOnlyList<string>? visibleSections = null)
    {
        visibleSections ??= sections;
        var sb = new StringBuilder(2048);
        var previewTitle = WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.PreviewTitle(lang));
        sb.Append("<article class=\"uwv-a4-preview uwv-adviseur-print\" aria-label=\"");
        sb.Append(previewTitle);
        sb.Append("\"><header class=\"uwv-a4-preview__head\">");
        AppendMarkLogo(sb);
        sb.Append("<div class=\"uwv-a4-preview__head-copy\"><p class=\"uwv-a4-preview__eyebrow\">");
        sb.Append(WebUtility.HtmlEncode(
            UwvMockupCopy.IsEnglish(lang)
                ? "Summary for your adviser"
                : "Samenvatting voor je adviseur"));
        sb.Append("</p><h2>");
        sb.Append(WebUtility.HtmlEncode(candidateDisplayName));
        sb.Append("</h2></div></header><div class=\"uwv-a4-preview__body\">");

        void BeginSection(string key)
        {
            if (visibleSections is null)
            {
                return;
            }

            sb.Append("<div data-uwv-section=\"");
            sb.Append(WebUtility.HtmlEncode(key));
            sb.Append('"');
            if (!visibleSections.Contains(key, StringComparer.Ordinal))
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
        }

        void EndSection()
        {
            if (visibleSections is null)
            {
                return;
            }

            sb.Append("</div>");
        }

        if (sections.Contains(UwvAdviseurSectionKeys.Career, StringComparer.Ordinal))
        {
            BeginSection(UwvAdviseurSectionKeys.Career);
            sb.Append("<h3>");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang) ? "Self-employment check" : "Ondernemerscheck"));
            sb.Append("</h3>");
            var (title, lead) = UwvMockupCopy.Ondernemer.ResultCopy(ondernemerOutcomeKey, lang);
            sb.Append("<p><strong>").Append(WebUtility.HtmlEncode(title)).Append("</strong></p>");
            sb.Append("<p>").Append(WebUtility.HtmlEncode(lead)).Append("</p>");
            EndSection();
        }

        if (sections.Contains(UwvAdviseurSectionKeys.Kansberoepen, StringComparer.Ordinal))
        {
            BeginSection(UwvAdviseurSectionKeys.Kansberoepen);
            var catalog = kansberoepenCatalog.GetDefault();
            sb.Append("<h3>");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang) ? "Opportunity occupations" : "Kansberoepen"));
            sb.Append("</h3><ul class=\"uwv-a4-preview__skills\">");
            foreach (var entry in catalog.Entries.Take(3))
            {
                sb.Append("<li>");
                sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? entry.TitleEn : entry.TitleNl));
                sb.Append("</li>");
            }

            sb.Append("</ul><p class=\"muted\">");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang) ? catalog.SourceLineEn : catalog.SourceLineNl));
            sb.Append("</p>");
            EndSection();
        }

        if (sections.Contains(UwvAdviseurSectionKeys.Drijfveren, StringComparer.Ordinal))
        {
            BeginSection(UwvAdviseurSectionKeys.Drijfveren);
            sb.Append("<h3>");
            sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Drijfveren.ResultsTitle(lang)));
            sb.Append("</h3>");
            var chips = ResolveDriverChips(drijfverenDriverKeys);
            if (chips.Count == 0)
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(
                    UwvMockupCopy.IsEnglish(lang) ? "No drivers saved yet." : "Nog geen drijfveren opgeslagen."));
                sb.Append("</p>");
            }
            else
            {
                foreach (var driver in chips)
                {
                    sb.Append("<p>");
                    sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.IsEnglish(lang) ? driver.ExplainEn : driver.ExplainNl));
                    sb.Append("</p>");
                }
            }

            EndSection();
        }

        if (sections.Contains(UwvAdviseurSectionKeys.Competences, StringComparer.Ordinal))
        {
            BeginSection(UwvAdviseurSectionKeys.Competences);
            sb.Append("<h3>");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang) ? "Competencies" : "Competenties"));
            sb.Append("</h3>");
            if (competenceLabels.Count > 0)
            {
                sb.Append("<ul class=\"uwv-a4-preview__skills\">");
                foreach (var label in competenceLabels)
                {
                    sb.Append("<li><span class=\"uwv-verified\" aria-hidden=\"true\">✓</span> ");
                    sb.Append(WebUtility.HtmlEncode(label));
                    sb.Append("</li>");
                }

                sb.Append("</ul><p class=\"uwv-a4-preview__note muted\">");
                sb.Append(WebUtility.HtmlEncode(UwvMockupCopy.Adviseur.VerifiedNote(lang)));
                sb.Append("</p>");
            }
            else
            {
                sb.Append("<p class=\"muted\">");
                sb.Append(WebUtility.HtmlEncode(
                    UwvMockupCopy.IsEnglish(lang)
                        ? "No verified competencies yet."
                        : "Nog geen bevestigde competenties."));
                sb.Append("</p>");
            }

            EndSection();
        }

        if (sections.Contains(UwvAdviseurSectionKeys.Banenkaart, StringComparer.Ordinal))
        {
            BeginSection(UwvAdviseurSectionKeys.Banenkaart);
            sb.Append("<h3>");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang) ? "Jobs on the map" : "Banen op de kaart"));
            sb.Append("</h3><p>");
            sb.Append(WebUtility.HtmlEncode(
                UwvMockupCopy.IsEnglish(lang)
                    ? "Selection of saved and matching vacancies on your banenkaart (sample)."
                    : "Selectie van opgeslagen en passende vacatures op je banenkaart (voorbeeld)."));
            sb.Append("</p>");
            EndSection();
        }

        sb.Append("</div></article>");
        return sb.ToString();
    }

    /// <summary>Renders all sections; toggles hide blocks via <c>data-uwv-section</c> + <c>hidden</c>.</summary>
    public static string BuildAdviseurPanelPreview(
        string lang,
        string candidateDisplayName,
        IReadOnlyList<string> selectedSections,
        string? ondernemerOutcomeKey,
        IReadOnlyList<string> drijfverenDriverKeys,
        IReadOnlyList<string> competenceLabels,
        IUwvKansberoepenCatalog kansberoepenCatalog)
        => Build(
            lang,
            candidateDisplayName,
            UwvAdviseurSectionKeys.All,
            ondernemerOutcomeKey,
            drijfverenDriverKeys,
            competenceLabels,
            kansberoepenCatalog,
            selectedSections);

    public static IReadOnlyList<string> CompetenceLabelsFromView(UwvAdviseurShareViewDto view)
    {
        if (view.EndorsedCompetences.Count > 0)
        {
            return view.EndorsedCompetences.Select(c => c.Label).ToList();
        }

        return [];
    }

    private static void AppendMarkLogo(StringBuilder sb)
    {
        var src = BrandImages.ForVariant(LobsyLogoVariant.Mark);
        var css = BrandImages.CssModifier(LobsyLogoVariant.Mark);
        sb.Append("<span class=\"lobsy-logo ").Append(css).Append(" uwv-a4-preview__logo\" aria-hidden=\"true\">");
        sb.Append("<img class=\"lobsy-logo__img\" src=\"").Append(WebUtility.HtmlEncode(src));
        sb.Append("\" srcset=\"").Append(WebUtility.HtmlEncode(BrandImages.SrcSet56));
        sb.Append("\" sizes=\"56px\" alt=\"\" width=\"56\" height=\"56\" decoding=\"async\" draggable=\"false\" />");
        sb.Append("</span>");
    }

    private static IReadOnlyList<UwvDriverChip> ResolveDriverChips(IReadOnlyList<string> drijfverenDriverKeys)
    {
        var list = new List<UwvDriverChip>();
        foreach (var key in drijfverenDriverKeys)
        {
            var idx = -1;
            for (var i = 0; i < UwvDriverKeys.All.Count; i++)
            {
                if (string.Equals(UwvDriverKeys.All[i], key, StringComparison.Ordinal))
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0 && idx < UwvMockupData.Drivers.Count)
            {
                list.Add(UwvMockupData.Drivers[idx]);
            }
        }

        return list;
    }
}
