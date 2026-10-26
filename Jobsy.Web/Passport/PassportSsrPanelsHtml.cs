using System.Net;
using System.Text;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Dna;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;
using Jobsy.Web.Navigation;

namespace Jobsy.Web.Passport;

internal static class PassportSsrPanelsHtml
{
    internal static void AppendLobsyTipCard(StringBuilder sb, CultureState culture)
    {
        sb.Append("<aside class=\"kompas-tip\" aria-labelledby=\"kompas-tip-title\" data-testid=\"passport-lobsy-tip\"><div class=\"kompas-tip__art\" style=\"background-image:url('");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.TipVragen));
        sb.Append("')\" aria-hidden=\"true\"></div><div class=\"kompas-tip__body\"><p class=\"kompas-tip__eyebrow\" id=\"kompas-tip-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Tip.Eyebrow"]));
        sb.Append("</p><p class=\"kompas-tip__text\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Tip.Body"]));
        sb.Append("</p><a class=\"btn btn-primary kompas-tip__cta\" href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
        sb.Append("\" data-passport-tab-link=\"tests\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kompas.Tip.Cta"]));
        sb.Append("</a></div></aside>");
    }

    internal static void AppendOverview(StringBuilder sb, CultureState culture, PassportSsrDisplay.OverviewModel o)
    {
        sb.Append("<section class=\"passport-overview\" aria-labelledby=\"passport-overview-title\">");
        sb.Append("<p class=\"profile-address__hint\" data-testid=\"passport-privacy-promise\"><span class=\"passport-data__icon\" aria-hidden=\"true\">");
        sb.Append(NavIcons.Shield);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.PrivacyPromise"]));
        sb.Append("</p><div class=\"passport-overview__ring-block\">");
        PassportSsrRingHtml.Append(sb, o.CompetenceArc, o.CareerArc, o.CultureArc, o.ValuesArc, culture["Passport.Overview.Title"]);
        sb.Append("<div class=\"passport-overview__ring-copy\"><h2 id=\"passport-overview-title\" class=\"passport-overview__title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Overview.Title"]));
        sb.Append("</h2><p class=\"passport-overview__stage\">");
        sb.Append(WebUtility.HtmlEncode(o.LobsterStage == 0
            ? culture["Passport.Overview.Egg"]
            : culture.Format("Passport.Overview.Stage", o.LobsterStage)));
        sb.Append("</p><ul class=\"passport-overview__legend\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Overview.Title"]));
        sb.Append("\"><li><span class=\"passport-overview__swatch passport-overview__swatch--brand\"></span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Legend.Competence"]));
        sb.Append("</li><li><span class=\"passport-overview__swatch passport-overview__swatch--success\"></span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Legend.Career"]));
        sb.Append("</li><li><span class=\"passport-overview__swatch passport-overview__swatch--gold\"></span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Legend.Culture"]));
        sb.Append("</li><li><span class=\"passport-overview__swatch passport-overview__swatch--warn\"></span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Legend.Values"]));
        sb.Append("</li></ul></div></div><div class=\"passport-overview__stats\">");
        AppendStat(sb, culture, "passport-stat--brand", NavIcons.Claw, "Passport.Stat.Strongest", o.Strongest);
        AppendStat(sb, culture, "passport-stat--success", NavIcons.Antenna, "Passport.Stat.Work", o.WorkFit);
        AppendStat(sb, culture, "passport-stat--gold", NavIcons.HomeHeart, "Passport.Stat.Home", o.CultureHome);
        AppendStat(sb, culture, "passport-stat--warn", NavIcons.Wave, "Passport.Stat.Important", o.Important);
        sb.Append("</div></section>");
    }

    internal static void AppendEncourage(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"profile-encourage\" data-testid=\"profile-encourage-paspoort-home\"><p class=\"profile-encourage__text\">");
        sb.Append(WebUtility.HtmlEncode(culture["Encourage.Profile.Message"]));
        sb.Append("</p><a class=\"btn btn-primary profile-encourage__cta\" href=\"");
        sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
        sb.Append("\" data-passport-tab-link=\"data\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Encourage.Profile.Link"]));
        sb.Append("</a></div>");
    }

    internal static void AppendDnaPanel(
        StringBuilder sb,
        CultureState culture,
        PassportSsrDisplay.DnaPanelModel dna,
        IReadOnlyList<string> keywords)
    {
        AppendDnaPanelCore(sb, culture, dna, keywords);
    }

    private static void AppendDnaPanelCore(
        StringBuilder sb,
        CultureState culture,
        PassportSsrDisplay.DnaPanelModel dna,
        IReadOnlyList<string> keywords)
    {
        var view = dna.View;
        var highlights = view?.Highlights ?? [];
        var testsComplete = dna.CompletedTests >= 4;
        var emptyStoryKey = testsComplete ? "Dna.StoryFallback" : "Dna.StoryEmpty";

        sb.Append("<div class=\"passport-dna\" data-testid=\"passport-panel-dna-content\"><div class=\"passport-dna__grid\">");

        sb.Append("<section class=\"detail-card passport-dna__card\" aria-labelledby=\"passport-dna-you-title\"><p class=\"passport-dna__eyebrow\"><span aria-hidden=\"true\">");
        sb.Append(NavIcons.Shell);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Dna.Eyebrow"]));
        sb.Append("</p><h3 id=\"passport-dna-you-title\" class=\"passport-dna__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Dna.YouTitle"]));
        sb.Append("</h3>");
        if (highlights.Count == 0)
        {
            sb.Append("<p class=\"login-lead\">");
            sb.Append(WebUtility.HtmlEncode(culture[emptyStoryKey]));
            sb.Append("</p><a class=\"btn btn-primary\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
            sb.Append("\" data-passport-tab-link=\"tests\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryEmptyCta"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<ul class=\"passport-dna__highlights\">");
            foreach (var h in highlights.Take(3))
            {
                sb.Append("<li class=\"passport-dna__highlight\"><span class=\"passport-dna__highlight-icon\" aria-hidden=\"true\">");
                sb.Append(HighlightIcon(h.LabelKey));
                sb.Append("</span><div><p class=\"passport-dna__highlight-title\">");
                sb.Append(WebUtility.HtmlEncode(h.Value));
                sb.Append("</p><p class=\"passport-dna__highlight-sub\">");
                sb.Append(WebUtility.HtmlEncode(culture[h.LabelKey]));
                sb.Append("</p></div></li>");
            }

            sb.Append("</ul>");
        }

        sb.Append("</section>");

        sb.Append("<section class=\"detail-card passport-dna__card\" aria-labelledby=\"passport-dna-story-title\"><div class=\"passport-dna__story-head\"><h3 id=\"passport-dna-story-title\" class=\"passport-dna__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryTitle"]));
        sb.Append("</h3>");
        if (!string.IsNullOrWhiteSpace(dna.Story))
        {
            sb.Append("<button type=\"button\" class=\"read-aloud read-aloud--icon\" data-read-aloud-text=\"");
            sb.Append(WebUtility.HtmlEncode(dna.Story));
            sb.Append("\" aria-label=\"");
            sb.Append(WebUtility.HtmlEncode(culture["ReadAloud.Play"]));
            sb.Append("\"><span class=\"read-aloud__icon\" aria-hidden=\"true\">🔊</span></button>");
        }

        sb.Append("</div>");
        if (string.IsNullOrWhiteSpace(dna.Story))
        {
            sb.Append("<p class=\"login-lead\">");
            sb.Append(WebUtility.HtmlEncode(culture[emptyStoryKey]));
            sb.Append("</p><a class=\"btn btn-primary\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
            sb.Append("\" data-passport-tab-link=\"tests\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryEmptyCta"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<div class=\"passport-dna__story is-clamped\" data-passport-story>");
            foreach (var para in StoryParagraphs(dna.Story))
            {
                sb.Append("<p>");
                sb.Append(WebUtility.HtmlEncode(para));
                sb.Append("</p>");
            }

            sb.Append("</div><button type=\"button\" class=\"btn btn-ghost\" data-passport-story-toggle data-label-more=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryMore"]));
            sb.Append("\" data-label-less=\"");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryLess"]));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryMore"]));
            sb.Append("</button>");
            if (keywords.Count > 0)
            {
                sb.Append("<ul class=\"passport-dna__chips\">");
                foreach (var kw in keywords)
                {
                    sb.Append("<li class=\"kompas-chip\">");
                    sb.Append(WebUtility.HtmlEncode(kw));
                    sb.Append("</li>");
                }

                sb.Append("</ul>");
            }

            if (dna.StoryGeneratedAt is DateTime generated)
            {
                sb.Append("<p class=\"passport-dna__meta\">");
                sb.Append(WebUtility.HtmlEncode(culture.Format("Dna.StoryUpdated", NlDate.Day(generated))));
                sb.Append("</p>");
            }
        }

        if (dna.Hobbies.Count > 0)
        {
            sb.Append("<div class=\"passport-dna__joy\"><p class=\"passport-dna__joy-label\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Dna.Joy"]));
            sb.Append("</p><ul class=\"passport-dna__chips\">");
            foreach (var hobby in dna.Hobbies.Take(4))
            {
                sb.Append("<li class=\"kompas-chip\">");
                sb.Append(WebUtility.HtmlEncode(hobby));
                sb.Append("</li>");
            }

            if (dna.Hobbies.Count > 4)
            {
                sb.Append("<li class=\"kompas-chip\">+");
                sb.Append(dna.Hobbies.Count - 4);
                sb.Append("</li>");
            }

            sb.Append("</ul></div>");
        }

        sb.Append("</section>");

        var cultureDetail = view?.CultureDetail;
        sb.Append("<section class=\"detail-card passport-dna__card\" aria-labelledby=\"passport-dna-home-title\"><h3 id=\"passport-dna-home-title\" class=\"passport-dna__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Dna.Home"]));
        sb.Append("</h3>");
        if (cultureDetail is null || cultureDetail.IsGhost)
        {
            sb.Append("<p class=\"login-lead\">");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.TeaserCulture"]));
            sb.Append("</p><a class=\"btn btn-secondary\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
            sb.Append("\" data-passport-tab-link=\"tests\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Dna.StoryEmptyCta"]));
            sb.Append("</a>");
        }
        else
        {
            AppendPoleSliders(sb, cultureDetail.Poles.Take(2).ToList());
            sb.Append("<p class=\"passport-dna__values-head\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Dna.ValuesProvisional"]));
            sb.Append(" <span class=\"muted\">· ");
            sb.Append(WebUtility.HtmlEncode(cultureDetail.Aside));
            sb.Append("</span></p>");
            var valuesDetail = view?.ValuesDetail;
            if (valuesDetail is not null && valuesDetail.Bars.Count > 0)
            {
                sb.Append("<ol class=\"passport-dna__values\">");
                foreach (var bar in valuesDetail.Bars.Where(b => b.Percent is not null).Take(3))
                {
                    sb.Append("<li>");
                    sb.Append(WebUtility.HtmlEncode(bar.Label));
                    sb.Append("</li>");
                }

                sb.Append("</ol>");
            }
        }

        sb.Append("</section></div>");
        AppendShellsSection(sb, culture, dna.Shells);
        sb.Append("</div>");
    }

    private static void AppendPoleSliders(
        StringBuilder sb,
        IReadOnlyList<(string LowLabel, string HighLabel, int? Percent)> poles)
    {
        if (poles.Count == 0)
        {
            return;
        }

        sb.Append("<div class=\"pole-sliders\">");
        foreach (var item in poles)
        {
            sb.Append("<div class=\"pole-sliders__row\"><span class=\"pole-sliders__end pole-sliders__end--low\">");
            sb.Append(WebUtility.HtmlEncode(item.LowLabel));
            sb.Append("</span><div class=\"pole-sliders__track\" aria-hidden=\"true\">");
            if (item.Percent is int pct)
            {
                sb.Append("<span class=\"pole-sliders__dot\" style=\"--p:");
                sb.Append(pct);
                sb.Append("%\"></span>");
            }

            sb.Append("</div><span class=\"pole-sliders__end pole-sliders__end--high\">");
            sb.Append(WebUtility.HtmlEncode(item.HighLabel));
            sb.Append("</span></div>");
        }

        sb.Append("</div>");
    }

    private static void AppendShellsSection(StringBuilder sb, CultureState culture, PassportShellRules.ShellProgress shells)
    {
        sb.Append("<section class=\"detail-card passport-shells\" aria-labelledby=\"passport-shells-title\"><div class=\"passport-shells__head\"><div><h3 id=\"passport-shells-title\" class=\"passport-dna__h\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Shells.Title"]));
        sb.Append("</h3><p class=\"passport-shells__sub\">");
        sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Shells.Sub", shells.EarnedCount, shells.TotalVisible)));
        sb.Append("</p><p class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Shells.Hint"]));
        sb.Append("</p></div><div class=\"passport-shells__aside\"><p>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Shells.Tagline"]));
        sb.Append("</p>");
        if (shells.QuestionsUntilNextShell > 0 && shells.EarnedCount < shells.TotalVisible)
        {
            sb.Append("<p class=\"passport-shells__next\">");
            sb.Append(WebUtility.HtmlEncode(culture.Format("Passport.Shells.Next", shells.QuestionsUntilNextShell)));
            sb.Append("</p>");
        }

        sb.Append("</div></div><ul class=\"passport-shells__row\">");
        foreach (var shell in shells.Shells)
        {
            sb.Append("<li class=\"passport-shell ");
            sb.Append(shell.Earned ? "passport-shell--earned" : "passport-shell--locked");
            sb.Append("\"><span class=\"passport-shell__ring\" aria-hidden=\"true\">");
            sb.Append(NavIcons.Shell);
            sb.Append("</span><span class=\"passport-shell__label\">");
            sb.Append(WebUtility.HtmlEncode(culture[shell.TitleKey]));
            sb.Append("</span></li>");
        }

        sb.Append("</ul></section>");
    }

    internal static void AppendCardFacts(StringBuilder sb, CultureState culture, PassportSsrDisplay.CardModel card, bool compact)
    {
        if (compact)
        {
            return;
        }

        sb.Append("<section class=\"passport-card__story\" aria-labelledby=\"passport-story-title\"><h3 id=\"passport-story-title\" class=\"passport-card__section-title\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.MyStory"]));
        sb.Append("</h3>");
        if (string.IsNullOrWhiteSpace(card.AboutMe))
        {
            sb.Append("<p class=\"passport-card__story-empty\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.MyStoryEmpty"]));
            sb.Append(" <a class=\"passport-card__fill-link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
            sb.Append("\" data-passport-tab-link=\"data\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fill"]));
            sb.Append("</a></p>");
        }
        else
        {
            sb.Append("<p class=\"passport-card__story-text\">");
            sb.Append(WebUtility.HtmlEncode(card.AboutMe));
            sb.Append("</p>");
        }

        sb.Append("</section><div class=\"passport-card__facts\">");
        AppendFact(sb, culture, "Passport.Facts.LivesIn", card.City, PassportTabs.Data);
        AppendFact(sb, culture, "Passport.Facts.Travel", card.Travel, PassportTabs.Data);
        AppendFact(sb, culture, "Passport.Facts.Hours", card.Hours, PassportTabs.Data);
        AppendFact(sb, culture, "Passport.Facts.Available", card.AvailableLabel, PassportTabs.Data);
        AppendFactWide(sb, culture, "Passport.Facts.Languages", card.LanguagesLabel, PassportTabs.Data);
        AppendLookingFor(sb, culture, card.LookingFor);
        AppendFact(sb, culture, "Passport.Facts.License", card.LicenseLabel, PassportTabs.Data);
        sb.Append("</div><p class=\"passport-card__tagline\"><span class=\"passport-card__tagline-icon\" aria-hidden=\"true\">");
        sb.Append(NavIcons.Shell);
        sb.Append("</span>");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Tagline"]));
        sb.Append("</p>");
    }

    private static void AppendFact(StringBuilder sb, CultureState culture, string labelKey, string? value, string tab)
    {
        sb.Append("<div class=\"passport-card__fact\"><span class=\"passport-card__fact-label\">");
        sb.Append(WebUtility.HtmlEncode(culture[labelKey]));
        sb.Append("</span>");
        if (string.IsNullOrWhiteSpace(value))
        {
            sb.Append("<a class=\"passport-card__fill-link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(tab));
            sb.Append("\" data-passport-tab-link=\"");
            sb.Append(WebUtility.HtmlEncode(tab));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fill"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<span class=\"passport-card__fact-value\">");
            sb.Append(WebUtility.HtmlEncode(value));
            sb.Append("</span>");
        }

        sb.Append("</div>");
    }

    private static void AppendFactWide(StringBuilder sb, CultureState culture, string labelKey, string? value, string tab)
    {
        sb.Append("<div class=\"passport-card__fact passport-card__fact--wide\"><span class=\"passport-card__fact-label\">");
        sb.Append(WebUtility.HtmlEncode(culture[labelKey]));
        sb.Append("</span>");
        if (string.IsNullOrWhiteSpace(value))
        {
            sb.Append("<a class=\"passport-card__fill-link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(tab));
            sb.Append("\" data-passport-tab-link=\"");
            sb.Append(WebUtility.HtmlEncode(tab));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fill"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<span class=\"passport-card__fact-value\">");
            sb.Append(WebUtility.HtmlEncode(value));
            sb.Append("</span>");
        }

        sb.Append("</div>");
    }

    private static void AppendLookingFor(StringBuilder sb, CultureState culture, IReadOnlyList<string> lookingFor)
    {
        sb.Append("<div class=\"passport-card__fact passport-card__fact--wide\"><span class=\"passport-card__fact-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Passport.Facts.LookingFor"]));
        sb.Append("</span>");
        if (lookingFor.Count == 0)
        {
            sb.Append("<a class=\"passport-card__fill-link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Data));
            sb.Append("\" data-passport-tab-link=\"data\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Fill"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<span class=\"passport-card__fact-chips\">");
            foreach (var role in lookingFor.Take(2))
            {
                sb.Append("<span class=\"kompas-chip\">");
                sb.Append(WebUtility.HtmlEncode(role));
                sb.Append("</span>");
            }

            if (lookingFor.Count > 2)
            {
                sb.Append("<span class=\"kompas-chip\">+");
                sb.Append(lookingFor.Count - 2);
                sb.Append("</span>");
            }

            sb.Append("</span>");
        }

        sb.Append("</div>");
    }

    private static void AppendStat(
        StringBuilder sb,
        CultureState culture,
        string tone,
        string icon,
        string labelKey,
        PassportSsrDisplay.StatField field)
    {
        sb.Append("<article class=\"passport-stat ");
        sb.Append(tone);
        sb.Append("\"><span class=\"passport-stat__icon\" aria-hidden=\"true\">");
        sb.Append(icon);
        sb.Append("</span><p class=\"passport-stat__label\">");
        sb.Append(WebUtility.HtmlEncode(culture[labelKey]));
        sb.Append("</p>");
        if (string.IsNullOrWhiteSpace(field.Value))
        {
            sb.Append("<p class=\"passport-stat__value\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Stat.NotYet"]));
            sb.Append("</p><a class=\"passport-stat__link\" href=\"");
            sb.Append(PassportRedirects.BuildPassportUrl(PassportTabs.Tests));
            sb.Append("\" data-passport-tab-link=\"tests\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["Passport.Stat.DoTest"]));
            sb.Append("</a>");
        }
        else
        {
            sb.Append("<p class=\"passport-stat__value\">");
            sb.Append(WebUtility.HtmlEncode(field.Value));
            sb.Append("</p>");
            if (!string.IsNullOrWhiteSpace(field.Status))
            {
                sb.Append("<p class=\"passport-stat__status\">");
                sb.Append(WebUtility.HtmlEncode(field.Status));
                sb.Append("</p>");
            }
        }

        sb.Append("</article>");
    }

    private static string HighlightIcon(string labelKey) => labelKey switch
    {
        "Dna.HighlightStrongest" => NavIcons.Target,
        "Dna.HighlightWork" => NavIcons.Match,
        "Dna.HighlightImportant" => NavIcons.HomeHeart,
        _ => NavIcons.Antenna
    };

    private static IEnumerable<string> StoryParagraphs(string story)
        => story.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
