using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

/// <summary>SSR markup for the groep 7/8 passport (read-only, no Blazor).</summary>
public static class PupilG78PassportSsrHtml
{
    public static string Render(PupilG78PassportWebDto model, CultureState culture)
    {
        if (!model.Enabled)
        {
            return "";
        }

        var sb = new StringBuilder();
        sb.Append("<div class=\"lp-g78\" data-testid=\"leerling-paspoort-g78\">");
        sb.Append("<header class=\"lp-g78__top\"><div class=\"lp-g78__brand\">");
        sb.Append("<img src=\"/images/leerling-paspoort-g78/g78-embleem.png\" width=\"28\" height=\"28\" alt=\"\" />");
        sb.Append(" Lobsy <small>");
        sb.Append(E(culture["LeerlingPassport.Brand.Sub"]));
        sb.Append("</small></div><div class=\"lp-g78__meta\"><span class=\"lp-g78__classline\">");
        sb.Append(E(model.ClassLabel));
        sb.Append("</span><br />");
        sb.Append(E(model.SchoolName));
        sb.Append("<br />");
        sb.Append(E(model.DateLabel));
        sb.Append("<span class=\"lp-g78__code\">");
        sb.Append(E(model.DisplayCode));
        sb.Append("</span></div></header><div class=\"lp-g78__body\">");

        sb.Append("<div class=\"lp-g78__title-row\"><div><div class=\"lp-g78__kicker\">");
        sb.Append(E(culture["LeerlingPassport.Kicker"]));
        sb.Append("</div><h1>");
        sb.Append(E(culture["LeerlingPassport.Page.Title"]));
        sb.Append("</h1><div class=\"lp-g78__nameline\">");
        sb.Append(E(culture["LeerlingPassport.NameIntro"]));
        if (!string.IsNullOrWhiteSpace(model.DisplayName))
        {
            sb.Append("<span class=\"lp-g78__own\"><b>");
            sb.Append(E(model.DisplayName));
            sb.Append("</b></span>");
        }

        sb.Append("</div><p class=\"lp-g78__note\">");
        sb.Append(E(culture["LeerlingPassport.NamePrivacy"]));
        sb.Append("</p></div>");
        sb.Append("<img class=\"lp-g78__kreeft\" src=\"/images/leerling-paspoort-g78/g78-kreeft-kijker.png\" alt=\"\" />");
        sb.Append("""
            <div class="lp-g78__badge"><span class="lp-g78__badge-mark" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="3.5"><path d="M5 12l5 5 9-10" /></svg>
            </span><div><b>
            """);
        sb.Append(E(culture["LeerlingPassport.Badge.Title"]));
        sb.Append("</b><span>");
        sb.Append(E(culture["LeerlingPassport.Badge.Sub"]));
        sb.Append("</span></div></div></div>");

        sb.Append("<section class=\"lp-g78__card\"><h3>");
        sb.Append(E(culture["LeerlingPassport.Section.DitBenIk"]));
        sb.Append("<span class=\"lp-g78__who\">");
        sb.Append(E(culture["LeerlingPassport.Section.DitBenIk.Who"]));
        sb.Append("</span></h3><p>");
        sb.Append(E(model.StoryBody));
        sb.Append("</p>");
        if (!string.IsNullOrWhiteSpace(model.MirrorBody))
        {
            sb.Append("<p class=\"lp-g78__mirror\">");
            sb.Append(E(model.MirrorBody));
            sb.Append("</p>");
        }

        sb.Append("</section>");

        if (model.SubjectGroups is { Count: > 0 })
        {
            sb.Append("<section class=\"lp-g78__card\"><h3>");
            sb.Append(E(culture["LeerlingPassport.G78.Page.Subjects"]));
            sb.Append("</h3>");
            foreach (var group in model.SubjectGroups)
            {
                sb.Append("<div class=\"lp-g78__vak-group\"><h4>");
                sb.Append(E(group.Title));
                sb.Append("</h4><p>");
                sb.Append(E(group.Intro));
                sb.Append("</p><p><b>");
                sb.Append(E(string.Join(" · ", group.SubjectLabels)));
                sb.Append("</b></p></div>");
            }

            sb.Append("</section>");
        }

        if (model.Worlds is { Count: > 0 })
        {
            sb.Append("<div class=\"lp-g78__worlds\">");
            foreach (var w in model.Worlds)
            {
                sb.Append("<article class=\"lp-g78__world\"><span class=\"lp-g78__world-ok\" aria-hidden=\"true\">");
                sb.Append("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"white\" stroke-width=\"3.5\"><path d=\"M5 12l5 5 9-10\" /></svg>");
                sb.Append("</span><div class=\"lp-g78__world-img\" style=\"background-image:url(/images/leerling-paspoort-g78/");
                sb.Append(E(w.ImageFile));
                sb.Append(")\"></div><div class=\"lp-g78__world-text\"><div class=\"lp-g78__world-k\">");
                sb.Append(E(w.SectionLabel));
                sb.Append("</div><h4>");
                sb.Append(E(w.Headline));
                sb.Append("</h4></div></article>");
            }

            sb.Append("</div>");
        }

        sb.Append("<div class=\"lp-g78__grid3\">");
        sb.Append("<section class=\"lp-g78__card lp-g78__like\"><h3>");
        sb.Append(E(culture["LeerlingPassport.Section.Likes"]));
        sb.Append("</h3><div class=\"lp-g78__chips\">");
        foreach (var chip in model.Likes ?? [])
        {
            sb.Append("<span class=\"lp-g78__chip is-on\">");
            sb.Append(E(chip));
            sb.Append("</span>");
        }

        sb.Append("</div>");
        if (!string.IsNullOrWhiteSpace(model.LikeOwnWord))
        {
            sb.Append("<div class=\"lp-g78__own\">");
            sb.Append(E(culture["LeerlingPassport.OwnWord"]));
            sb.Append("<b>");
            sb.Append(E(model.LikeOwnWord));
            sb.Append("</b></div>");
        }

        sb.Append("</section>");
        sb.Append("<section class=\"lp-g78__card lp-g78__dis\" data-testid=\"leerling-paspoort-dislikes\"><h3>");
        sb.Append(E(culture["LeerlingPassport.Section.Dislikes"]));
        sb.Append("</h3>");
        var hasDislikes = (model.Dislikes?.Count ?? 0) > 0 || !string.IsNullOrWhiteSpace(model.DislikeOwnWord);
        if (hasDislikes)
        {
            sb.Append("<div class=\"lp-g78__chips\">");
            foreach (var chip in model.Dislikes ?? [])
            {
                sb.Append("<span class=\"lp-g78__chip is-no\">");
                sb.Append(E(chip));
                sb.Append("</span>");
            }

            sb.Append("</div>");
            if (!string.IsNullOrWhiteSpace(model.DislikeOwnWord))
            {
                sb.Append("<div class=\"lp-g78__own\">");
                sb.Append(E(culture["LeerlingPassport.OwnWord"]));
                sb.Append("<b>");
                sb.Append(E(model.DislikeOwnWord));
                sb.Append("</b></div>");
            }
        }
        else
        {
            sb.Append("<p class=\"lp-g78__empty\">");
            sb.Append(E(culture["LeerlingPassport.Dislikes.Empty"]));
            sb.Append("</p>");
        }

        sb.Append("</section>");
        sb.Append("<section class=\"lp-g78__card lp-g78__str\"><h3>");
        sb.Append(E(culture["LeerlingPassport.Section.Strengths"]));
        sb.Append("</h3>");
        foreach (var s in model.Strengths ?? [])
        {
            sb.Append("<div class=\"lp-g78__strength\"><span class=\"lp-g78__strength-ic\" aria-hidden=\"true\">●</span><div><b>");
            sb.Append(E(s.Title));
            sb.Append("</b> <span>");
            sb.Append(E(s.Subtitle));
            sb.Append("</span></div></div>");
        }

        sb.Append("</section></div>");

        if (!string.IsNullOrWhiteSpace(model.DreamJobTitle))
        {
            var bubbleTitle = model.DreamJobTitle.TrimEnd('!', '.');
            var bubble = string.IsNullOrWhiteSpace(bubbleTitle)
                ? ""
                : culture.Format("LeerlingPassport.Dream.Bubble", bubbleTitle);
            sb.Append("<section class=\"lp-g78__card\"><div class=\"lp-g78__dream-head\"><span class=\"lp-g78__dream-hand\">");
            sb.Append(E(culture["LeerlingPassport.Section.Droom"]));
            sb.Append("</span><h3 class=\"lp-g78__dream-job\">");
            sb.Append(E(model.DreamJobTitle));
            sb.Append("</h3>");
            if (!string.IsNullOrWhiteSpace(bubble))
            {
                sb.Append("<span class=\"lp-g78__dream-bub\">");
                sb.Append(E(bubble));
                sb.Append("</span>");
            }

            sb.Append("<img class=\"lp-g78__dream-lob\" src=\"/images/leerling-paspoort-g78/g78-kreeft-kaart.png\" alt=\"\" /></div>");
            if (model.RouteSteps is { Count: > 0 })
            {
                sb.Append("<div class=\"lp-g78__route\">");
                foreach (var step in model.RouteSteps)
                {
                    sb.Append("<div class=\"lp-g78__route-step\"><div class=\"phase\">");
                    sb.Append(E(step.PhaseLabel));
                    sb.Append("</div><b>");
                    sb.Append(E(step.Title));
                    sb.Append("</b>");
                    if (!string.IsNullOrWhiteSpace(step.Detail))
                    {
                        sb.Append("<span>");
                        sb.Append(E(step.Detail));
                        sb.Append("</span>");
                    }

                    sb.Append("</div>");
                }

                sb.Append("</div>");
            }

            sb.Append("<div class=\"lp-g78__have\">");
            if (model.DreamHaveItems is { Count: > 0 })
            {
                sb.Append("<div><h4>");
                sb.Append(E(PupilPassportLocalizedCopy.Get("LeerlingDroom.HaveTitle", culture.Language)));
                sb.Append("</h4><ul>");
                foreach (var item in model.DreamHaveItems)
                {
                    sb.Append("<li>");
                    sb.Append(E(item));
                    sb.Append("</li>");
                }

                sb.Append("</ul></div>");
            }

            if (model.DreamLearnItems is { Count: > 0 })
            {
                sb.Append("<div><h4>");
                sb.Append(E(PupilPassportLocalizedCopy.Get("LeerlingDroom.NeedTitle", culture.Language)));
                sb.Append("</h4><ul>");
                foreach (var item in model.DreamLearnItems)
                {
                    sb.Append("<li>");
                    sb.Append(E(item));
                    sb.Append("</li>");
                }

                sb.Append("</ul></div>");
            }

            if (!string.IsNullOrWhiteSpace(model.AltRouteNote))
            {
                sb.Append("<p class=\"lp-g78__alt\">");
                sb.Append(E(model.AltRouteNote));
                sb.Append("</p>");
            }

            sb.Append("</div></section>");
        }

        if (model.OccupationCards is { Count: > 0 })
        {
            sb.Append("<h2 class=\"lp-g78__cards-head\">");
            sb.Append(E(culture["LeerlingPassport.Section.Cards"]));
            sb.Append("<small>");
            sb.Append(E(culture["LeerlingPassport.Cards.Sub"]));
            sb.Append("</small></h2><div class=\"lp-g78__jobs\">");
            foreach (var job in model.OccupationCards)
            {
                sb.Append("<article class=\"lp-g78__card lp-g78__job\"><div class=\"lp-g78__job-k\">");
                sb.Append(E(job.DomainLabel));
                sb.Append("</div><h4>");
                sb.Append(E(job.Title));
                sb.Append("</h4>");
                if (job.MatchPercent is >= 70)
                {
                    sb.Append("<p class=\"lp-g78__job-match\">");
                    sb.Append(E(culture.Format("LeerlingPassport.G78.Jobs.MatchSuggest", job.MatchPercent.Value)));
                    sb.Append("</p>");
                }

                sb.Append("<p>");
                sb.Append(E(job.Blurb));
                sb.Append("</p>");
                if (job.FitTags is { Count: > 0 })
                {
                    sb.Append("<div class=\"lp-g78__fit\"><b>");
                    sb.Append(E(culture["LeerlingPassport.Cards.Fit"]));
                    sb.Append(":</b> ");
                    sb.Append(E(string.Join(", ", job.FitTags)));
                    sb.Append("</div>");
                }

                sb.Append("</article>");
            }

            sb.Append("</div>");
        }

        sb.Append("<div class=\"lp-g78__grid2\">");
        if (model.DiscoveryQuestions is { Count: > 0 })
        {
            sb.Append("<section class=\"lp-g78__card lp-g78__disc\"><h3>");
            sb.Append(E(culture["LeerlingPassport.Section.Discover"]));
            sb.Append("</h3><ul>");
            foreach (var q in model.DiscoveryQuestions)
            {
                sb.Append("<li><span class=\"box\"></span>");
                sb.Append(E(q));
                sb.Append("</li>");
            }

            sb.Append("</ul></section>");
        }

        if (model.HomeClassTips is { Count: > 0 })
        {
            sb.Append("<section class=\"lp-g78__card\"><h3>");
            sb.Append(E(culture["LeerlingPassport.Section.Tip"]));
            sb.Append("</h3>");
            foreach (var tip in model.HomeClassTips)
            {
                sb.Append("<p>");
                sb.Append(E(tip));
                sb.Append("</p>");
            }

            sb.Append("</section>");
        }

        sb.Append("</div><footer class=\"lp-g78__foot\"><span>");
        sb.Append(E(model.FooterPrivacy));
        sb.Append("</span><span><b>lobsy.nl</b> ");
        sb.Append(E(model.DateLabel));
        sb.Append("</span></footer></div></div>");
        return sb.ToString();
    }

    private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
}
